using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战役系统：管理回合流程、历史记录、胜负判定
/// </summary>
public class CampaignSystem : ILogic
{
    private EventService eventService;
    private ResService resService;
    private MarketSystem marketSystem;
    private BattleSystem battleSystem;

    private GameBalanceConfig balanceConfig;
    private OrderConfig orderConfig;
    private SkillConfig skillConfig;

    private IntentSystem intentSystem;
    private CommissionSystem commissionSystem;

    public CampaignRuntimeData Data { get; private set; }

    // 是否等待战斗动画完成
    private bool waitingForBattleAnimations = false;

    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;

        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        orderConfig = resService.LoadResource<OrderConfig>(ConfigPaths.ORDER_CONFIG);
        skillConfig = resService.LoadResource<SkillConfig>(ConfigPaths.SKILL_CONFIG);

        intentSystem = new IntentSystem();
        intentSystem.Init(balanceConfig);

        commissionSystem = new CommissionSystem();

        // 监听战斗动画完成事件
        eventService.AddEventListening((EventID)WarBrokerEventID.OnBattleAnimationsComplete, OnBattleAnimationsComplete);
    }

    public void OnEnterState() { }
    public void OnUpdate() { }

    public void UnInit()
    {
        eventService?.RemoveEventListeningByTarget(this);
    }

    public void InitNewCampaign(string campaignId, MarketSystem market, BattleSystem battle)
    {
        marketSystem = market;
        battleSystem = battle;

        var campaignConfig = resService.LoadResource<CampaignConfig>(
            ConfigPaths.CAMPAIGN_PREFIX + campaignId);

        if (campaignConfig == null)
        {
            Debug.LogError($"Campaign config not found: {campaignId}");
            return;
        }

        Data = new CampaignRuntimeData();
        Data.InitFromConfig(campaignConfig, orderConfig, skillConfig);

        marketSystem.SetRuntimeData(Data);
        battleSystem.SetRuntimeData(Data);

        // 初始化委托系统
        commissionSystem.Init(Data, balanceConfig);

        // 从战役配置加载委托任务
        commissionSystem.LoadCommissions(campaignConfig.Commissions);
    }

    public IntentSystem GetIntentSystem() => intentSystem;
    public CommissionSystem GetCommissionSystem() => commissionSystem;

    #region 回合流程

    /// <summary>游戏结果</summary>
    public GameResult CurrentGameResult { get; private set; } = GameResult.InProgress;

    /// <summary>游戏结束原因</summary>
    public string GameEndReason { get; private set; }

    /// <summary>
    /// 开始新回合 (GDD v6.0 五阶段流程)
    /// 阶段 I：周报与开盘
    /// </summary>
    public void StartTurn()
    {
        if (Data == null)
        {
            Debug.LogError("[CampaignSystem] Campaign data is null, cannot start turn.");
            return;
        }

        Data.CurrentPhase = TurnPhase.TurnStart;

        // Step 1: 时间推进
        // Week++ 已在上一回合结束时完成

        // Step 2: 费用结算
        marketSystem.ApplyInterest();      // 银行利息
        marketSystem.ApplyStorageCost();   // 仓储费

        // Step 3: 随机事件抽取与公示
        TryTriggerRandomEvent();

        // Step 4: 将军意图生成（灰色气泡）
        GenerateAllIntents();

        // Step 5: 市场开盘（价格刷新 + 记录 Open）
        var demandModifiers = CalculateDemandModifiers();
        marketSystem.UpdatePrices(demandModifiers);
        marketSystem.InitializeKLineForTurn();  // 记录 Open 价格

        eventService.SendMessage((EventID)WarBrokerEventID.OnTurnStart, Data.CurrentTurn, null);

        // 自动进入阶段 II：玩家操盘
        EnterPlayerPhase();
    }

    /// <summary>
    /// 阶段 II：玩家操盘 (GDD v6.0)
    /// 玩家进行金融交易、政治干涉、银行操作
    /// </summary>
    public void EnterPlayerPhase()
    {
        Data.CurrentPhase = TurnPhase.MarketPhase;

        // 重置将军指令（允许玩家重新分配）
        foreach (var general in Data.Battle.AllyGenerals)
        {
            general.AssignedOrder = null;
        }

        eventService.SendMessage((EventID)WarBrokerEventID.OnPhaseChange, TurnPhase.MarketPhase, null);
        // 等待玩家操作完成，玩家点击"结束本周"后调用 EnterVictorPhase()
    }

    /// <summary>
    /// 阶段 III：维克多行动 (GDD v6.0)
    /// 维克多进行军事采购和投机操作
    /// </summary>
    public void EnterVictorPhase()
    {
        Data.CurrentPhase = TurnPhase.IntentPhase;

        // 执行维克多 AI
        ExecuteVictorAI();

        eventService.SendMessage((EventID)WarBrokerEventID.OnPhaseChange, TurnPhase.IntentPhase, null);

        // 自动进入阶段 IV：战斗推演
        EnterBattlePhase();
    }

    /// <summary>
    /// 阶段 IV：战斗推演 (GDD v6.0)
    /// 抗命检查 → 战术揭示 → 伤害计算 → 战线移动
    /// </summary>
    public void EnterBattlePhase()
    {
        // 检查所有将军是否已分配指令
        foreach (var general in Data.Battle.AllyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            if (general.AssignedOrder == null)
            {
                Debug.LogWarning($"将军{general.Name}未分配指令，使用默认意图");
                general.AssignedOrder = general.DefaultIntent ?? OrderType.DEF;
            }
        }

        Data.CurrentPhase = TurnPhase.BattlePhase;

        // 战斗结算（包含抗命检查、战术揭示、伤害计算、战线移动）
        var victorOrders = new Dictionary<string, OrderType>();
        foreach (var enemy in Data.Battle.EnemyGenerals)
        {
            victorOrders[enemy.GeneralId] = enemy.DefaultIntent ?? OrderType.DEF;
        }

        var battleResults = battleSystem.ResolveBattles(victorOrders);

        eventService.SendMessage((EventID)WarBrokerEventID.OnPhaseChange, TurnPhase.BattlePhase, null);

        // 等待战斗动画完成后再进入结算阶段
        // 如果有战斗结果，等待动画；否则直接进入结算
        if (battleResults.Count > 0)
        {
            waitingForBattleAnimations = true;
        }
        else
        {
            EnterSettlementPhase();
        }
    }

    /// <summary>战斗动画完成回调</summary>
    private void OnBattleAnimationsComplete(object p1, object p2)
    {
        if (waitingForBattleAnimations)
        {
            waitingForBattleAnimations = false;
            EnterSettlementPhase();
        }
    }

    /// <summary>
    /// 阶段 V：回合结算 (GDD v6.0)
    /// 记录 Close → 基地恢复 → 溃败重组 → 军工厂产出 → 期货到期 → 胜负检查
    /// </summary>
    public void EnterSettlementPhase()
    {
        Data.CurrentPhase = TurnPhase.SettlementPhase;

        // Step 1: 记录 K 线 Close 价格和成交量
        marketSystem.FinalizeKLineForTurn();

        // Step 2: 基地恢复（Grid 1 部队 HP+2，消耗 Reserves）
        battleSystem.ApplyReinforcements();

        // Step 3: 溃败重组（处理复活逻辑）
        // 已在 ApplyReinforcements 中处理

        // Step 4: 军工厂产出（补充流通盘）
        // 已在下一回合 StartTurn 中的 UpdatePrices 调用

        // Step 5: 期货到期（3 回合前的期货强制交割）
        marketSystem.SettleExpiredFutures();
        marketSystem.CheckForceLiquidation();

        // Step 6: 胜负检查
        UpdateOccupationStatus();
        CheckVictoryConditions();

        // 记录回合历史
        RecordTurn();

        // 事件持续时间处理
        if (Data.ActiveEvent != null)
        {
            Data.EventRemainingTurns--;
            if (Data.EventRemainingTurns <= 0)
            {
                Data.ActiveEvent = null;
            }
        }

        eventService.SendMessage((EventID)WarBrokerEventID.OnPhaseChange, TurnPhase.SettlementPhase, null);
        eventService.SendMessage((EventID)WarBrokerEventID.OnTurnEnd, Data.CurrentTurn, null);

        // 如果游戏未结束，进入下一回合
        if (CurrentGameResult == GameResult.InProgress)
        {
            Data.CurrentTurn++;
            StartTurn();
        }
    }

    /// <summary>结束当前回合（兼容旧接口）</summary>
    public void EndTurn()
    {
        // 根据当前阶段决定下一步
        switch (Data.CurrentPhase)
        {
            case TurnPhase.EventPhase:
            case TurnPhase.MarketPhase:
                // 玩家操盘阶段结束，进入维克多行动
                EnterVictorPhase();
                break;
            case TurnPhase.IntentPhase:
                EnterBattlePhase();
                break;
            case TurnPhase.BattlePhase:
                EnterSettlementPhase();
                break;
            default:
                EnterBattlePhase();
                break;
        }
    }

    /// <summary>推进到下一阶段</summary>
    public void AdvancePhase()
    {
        switch (Data.CurrentPhase)
        {
            case TurnPhase.TurnStart:
                EnterPlayerPhase();
                break;
            case TurnPhase.EventPhase:
            case TurnPhase.MarketPhase:
                EnterVictorPhase();
                break;
            case TurnPhase.IntentPhase:
                EnterBattlePhase();
                break;
            case TurnPhase.BattlePhase:
                EnterSettlementPhase();
                break;
        }
    }

    /// <summary>生成所有我方将军的意图</summary>
    private void GenerateAllIntents()
    {
        foreach (var general in Data.Battle.AllyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            var defaultIntent = intentSystem.GenerateDefaultIntent(general);
            general.DefaultIntent = defaultIntent;
            general.FinalIntent = defaultIntent;
            general.IntentSource = IntentSource.Default;

            eventService.SendMessage((EventID)WarBrokerEventID.OnIntentChanged, general, null);
        }
    }

    /// <summary>更新占领状态</summary>
    private void UpdateOccupationStatus()
    {
        foreach (var frontline in Data.Battle.Frontlines.Values)
        {
            if (frontline.IsAtEnemyBase)
            {
                frontline.TurnsAtEnemyBase++;
                frontline.TurnsAtAllyBase = 0;
            }
            else if (frontline.IsAtAllyBase)
            {
                frontline.TurnsAtAllyBase++;
                frontline.TurnsAtEnemyBase = 0;
            }
            else
            {
                frontline.TurnsAtEnemyBase = 0;
                frontline.TurnsAtAllyBase = 0;
            }
        }
    }

    private Dictionary<OrderType, float> CalculateDemandModifiers()
    {
        var modifiers = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 1f },
            { OrderType.DEF, 1f },
            { OrderType.RET, 1f }
        };

        foreach (var general in Data.Battle.AllyGenerals)
        {
            var status = general.GetStatus(balanceConfig);
            if (status == GeneralStatus.Wounded)
            {
                modifiers[OrderType.DEF] += 0.2f;
                modifiers[OrderType.RET] += 0.1f;
            }
            else if (status == GeneralStatus.Critical)
            {
                modifiers[OrderType.DEF] += 0.5f;
                modifiers[OrderType.RET] += 0.3f;
            }
        }

        foreach (var frontline in Data.Battle.Frontlines.Values)
        {
            if (frontline.LinePosition <= 2)
            {
                modifiers[OrderType.DEF] += 0.15f;
            }
            else if (frontline.LinePosition >= 4)
            {
                modifiers[OrderType.ATK] += 0.1f;
            }
        }

        if (Data.ActiveEvent != null)
        {
            modifiers[OrderType.ATK] += Data.ActiveEvent.AtkDemandModifier;
            modifiers[OrderType.DEF] += Data.ActiveEvent.DefDemandModifier;
            modifiers[OrderType.RET] += Data.ActiveEvent.RetDemandModifier;
        }

        return modifiers;
    }

    #endregion

    #region 维克多AI

    private Dictionary<string, OrderType> ExecuteVictorAI()
    {
        var orders = new Dictionary<string, OrderType>();
        var pricingEngine = marketSystem.GetPricingEngine();

        foreach (var general in Data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            // Step 1: 生成默认意图
            var defaultIntent = intentSystem.GenerateDefaultIntent(general);
            general.DefaultIntent = defaultIntent;

            // Step 2: 尝试强化（需要从市场购买1份指令）
            bool canReinforce = TryVictorPurchase(pricingEngine, defaultIntent, 1);

            if (canReinforce)
            {
                general.FinalIntent = defaultIntent;
                general.IntentSource = IntentSource.Reinforced;
            }
            else
            {
                general.FinalIntent = defaultIntent;
                general.IntentSource = IntentSource.Default;
            }

            orders[general.GeneralId] = general.FinalIntent.Value;
            general.AssignedOrder = general.FinalIntent.Value;
        }

        return orders;
    }

    /// <summary>
    /// 维克多尝试从市场购买指令
    /// </summary>
    private bool TryVictorPurchase(PricingEngine pricingEngine, OrderType type, int quantity)
    {
        var market = Data.Market;

        // 检查流通盘
        if (market.MarketInventory[type] < quantity)
        {
            return false;
        }

        // 检查资金
        float price = pricingEngine.CalculatePrice(type);
        float totalCost = price * quantity * (1 + balanceConfig.CommissionRate);

        if (Data.VictorCash < totalCost)
        {
            return false;
        }

        // 执行购买
        Data.VictorCash -= totalCost;
        market.MarketInventory[type] -= quantity;

        return true;
    }

    #endregion

    #region 随机事件

    private void TryTriggerRandomEvent()
    {
        if (Data.ActiveEvent != null) return;
        if (UnityEngine.Random.value > balanceConfig.RandomEventChance) return;

        var availableEvents = Data.Config.AvailableEvents;
        if (availableEvents == null || availableEvents.Length == 0) return;

        var selectedEvent = availableEvents[UnityEngine.Random.Range(0, availableEvents.Length)];
        Data.ActiveEvent = selectedEvent;
        Data.EventRemainingTurns = selectedEvent.Duration;

        if (selectedEvent.AllTroopChange != 0)
        {
            foreach (var general in Data.Battle.AllyGenerals)
            {
                general.Troops = Mathf.Clamp(general.Troops + selectedEvent.AllTroopChange, 0, 20);
            }
            foreach (var general in Data.Battle.EnemyGenerals)
            {
                general.Troops = Mathf.Clamp(general.Troops + selectedEvent.AllTroopChange, 0, 20);
            }
        }

        eventService.SendMessage((EventID)WarBrokerEventID.OnRandomEvent, selectedEvent, null);
    }

    #endregion

    #region 历史记录

    private void RecordTurn()
    {
        var record = new TurnRecord
        {
            TurnNumber = Data.CurrentTurn,
            OrderAssignments = new Dictionary<string, OrderType>(),
            Transactions = new List<TransactionRecord>(),
            BattleResults = new List<BattleResult>(),
            PriceSnapshot = new Dictionary<OrderType, float>(Data.Market.CurrentPrices),
            PlayerNetWorth = Data.Player.CalculateNetWorth(Data.Market)
        };

        foreach (var general in Data.Battle.AllyGenerals)
        {
            if (general.AssignedOrder.HasValue)
            {
                record.OrderAssignments[general.GeneralId] = general.AssignedOrder.Value;
            }
        }

        Data.TurnHistory.Add(record);
    }

    #endregion

    #region 胜负判定

    /// <summary>检查胜利条件</summary>
    private void CheckVictoryConditions()
    {
        // 检查破产
        if (Data.Player.CalculateNetWorth(Data.Market) < 0)
        {
            EndGame(GameResult.Defeat, "破产");
            return;
        }

        // 检查战争问责
        if (Data.Player.AuditValue >= balanceConfig.AuditFailureThreshold)
        {
            EndGame(GameResult.Defeat, "战争问责");
            return;
        }

        // 检查战线占领状态
        foreach (var frontline in Data.Battle.Frontlines.Values)
        {
            // 胜利：占领敌方本阵（Grid 5）并保持 1 回合
            if (frontline.TurnsAtEnemyBase >= 1)
            {
                EndGame(GameResult.Victory, $"战争胜利 - {GetFrontlineName(frontline.Position)}突破");
                return;
            }

            // 失败：敌方占领己方本阵（Grid 1）并保持 1 回合
            // GDD v6.0: 政权崩溃清算
            if (frontline.TurnsAtAllyBase >= 1)
            {
                ExecuteRegimeCollapseSettlement();
                EndGame(GameResult.Defeat, $"战争失败 - {GetFrontlineName(frontline.Position)}沦陷");
                return;
            }
        }

        // 检查回合数限制
        if (Data.CurrentTurn >= Data.MaxTurns)
        {
            EndGame(GameResult.Draw, "回合耗尽");
            return;
        }
    }

    /// <summary>结束游戏</summary>
    private void EndGame(GameResult result, string reason)
    {
        CurrentGameResult = result;
        GameEndReason = reason;

        // 输出胜负原因到控制台
        string resultText = result switch
        {
            GameResult.Victory => "胜利",
            GameResult.Defeat => "失败",
            GameResult.Draw => "平局",
            _ => result.ToString()
        };
        Debug.Log($"[游戏结束] {resultText} - 原因: {reason}");

        // 委托任务结算
        if (commissionSystem != null)
        {
            Data.CommissionResults = commissionSystem.CheckAndSettleCommissions(out float totalBonus);
            Data.CommissionTotalBonus = totalBonus;
        }

        switch (result)
        {
            case GameResult.Victory:
                eventService.SendMessage((EventID)WarBrokerEventID.OnVictoryConditionMet, reason, null);
                eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, true, null);
                break;
            case GameResult.Defeat:
                eventService.SendMessage((EventID)WarBrokerEventID.OnDefeatConditionMet, reason, null);
                eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false, null);
                break;
            case GameResult.Draw:
                eventService.SendMessage((EventID)WarBrokerEventID.OnDrawConditionMet, reason, null);
                eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false, null);
                break;
        }
    }

    /// <summary>获取战线名称</summary>
    private string GetFrontlineName(FrontlinePosition position)
    {
        return position switch
        {
            FrontlinePosition.Left => "左翼",
            FrontlinePosition.Center => "中军",
            FrontlinePosition.Right => "右翼",
            _ => position.ToString()
        };
    }

    /// <summary>旧接口兼容</summary>
    private bool CheckGameEnd()
    {
        CheckVictoryConditions();
        return CurrentGameResult != GameResult.InProgress;
    }

    /// <summary>
    /// 政权崩溃清算 (GDD v6.0)
    /// Grid 1 沦陷时特殊清算：
    /// - ATK、DEF 现货价格归零
    /// - 空单按 $0 结算（最大收益）
    /// </summary>
    private void ExecuteRegimeCollapseSettlement()
    {
        Debug.Log("[政权崩溃] 执行特殊清算...");

        var market = Data.Market;
        var player = Data.Player;

        // ATK、DEF 现货价格归零
        market.CurrentPrices[OrderType.ATK] = 0f;
        market.CurrentPrices[OrderType.DEF] = 0f;
        // RET 价格保持不变（或可能暴涨）

        Debug.Log($"[政权崩溃] ATK 价格: ${market.CurrentPrices[OrderType.ATK]}");
        Debug.Log($"[政权崩溃] DEF 价格: ${market.CurrentPrices[OrderType.DEF]}");

        // 玩家持有的 ATK、DEF 现货价值归零
        int atkLoss = player.Inventory[OrderType.ATK];
        int defLoss = player.Inventory[OrderType.DEF];
        Debug.Log($"[政权崩溃] 现货损失: ATK×{atkLoss}, DEF×{defLoss}");

        // 期货结算：空单按 $0 结算（最大收益）
        float futuresPnL = 0f;
        var contractsToSettle = new List<FuturesContract>();

        foreach (var contract in player.FuturesPositions)
        {
            if (contract.TargetOrder == OrderType.ATK || contract.TargetOrder == OrderType.DEF)
            {
                float settlementPrice = 0f; // 按 $0 结算
                float pnl = contract.CalculatePnL(settlementPrice);
                futuresPnL += pnl;

                // 返还保证金 + 盈亏
                player.Cash += contract.Margin + pnl;

                Debug.Log($"[政权崩溃] 期货结算: {contract.TargetOrder} {contract.Direction} " +
                          $"开仓价${contract.OpenPrice} → 结算价${settlementPrice}, 盈亏${pnl}");

                contractsToSettle.Add(contract);
            }
        }

        // 移除已结算的期货合约
        foreach (var contract in contractsToSettle)
        {
            player.FuturesPositions.Remove(contract);
        }

        Debug.Log($"[政权崩溃] 期货总盈亏: ${futuresPnL}");
        Debug.Log($"[政权崩溃] 清算后现金: ${player.Cash}");

        // 发送事件通知
        eventService.SendMessage((EventID)WarBrokerEventID.OnRegimeCollapse, futuresPnL, null);
    }

    #endregion
}
