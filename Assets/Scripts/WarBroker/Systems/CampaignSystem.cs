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

    public CampaignRuntimeData Data { get; private set; }

    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;

        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        orderConfig = resService.LoadResource<OrderConfig>(ConfigPaths.ORDER_CONFIG);
        skillConfig = resService.LoadResource<SkillConfig>(ConfigPaths.SKILL_CONFIG);

        intentSystem = new IntentSystem();
        intentSystem.Init(balanceConfig);
    }

    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }

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
    }

    public IntentSystem GetIntentSystem() => intentSystem;

    #region 回合流程

    /// <summary>游戏结果</summary>
    public GameResult CurrentGameResult { get; private set; } = GameResult.InProgress;

    /// <summary>游戏结束原因</summary>
    public string GameEndReason { get; private set; }

    /// <summary>开始新回合</summary>
    public void StartTurn()
    {
        if (Data == null)
        {
            Debug.LogError("[CampaignSystem] Campaign data is null, cannot start turn.");
            return;
        }

        Data.CurrentPhase = TurnPhase.TurnStart;

        // 重置将军指令
        foreach (var general in Data.Battle.AllyGenerals)
        {
            general.AssignedOrder = null;
        }

        eventService.SendMessage((EventID)WarBrokerEventID.OnTurnStart, Data.CurrentTurn, null);

        // 自动进入事件阶段
        EnterEventPhase();
    }

    /// <summary>事件阶段：随机事件触发</summary>
    public void EnterEventPhase()
    {
        Data.CurrentPhase = TurnPhase.EventPhase;
        TryTriggerRandomEvent();
        eventService.SendMessage((EventID)WarBrokerEventID.OnPhaseChange, TurnPhase.EventPhase, null);
    }

    /// <summary>进入市场阶段：价格更新，等待玩家交易</summary>
    public void EnterMarketPhase()
    {
        Data.CurrentPhase = TurnPhase.MarketPhase;
        var demandModifiers = CalculateDemandModifiers();
        marketSystem.UpdatePrices(demandModifiers);
        eventService.SendMessage((EventID)WarBrokerEventID.OnPhaseChange, TurnPhase.MarketPhase, null);
        // 等待玩家交易操作，玩家完成后调用 EnterIntentPhase()
    }

    /// <summary>进入意图阶段：生成将军意图，等待玩家强化/篡改</summary>
    public void EnterIntentPhase()
    {
        Data.CurrentPhase = TurnPhase.IntentPhase;
        GenerateAllIntents();
        eventService.SendMessage((EventID)WarBrokerEventID.OnPhaseChange, TurnPhase.IntentPhase, null);
        // 等待玩家强化/篡改操作，玩家完成后调用 EnterBattlePhase()
    }

    /// <summary>进入战斗阶段：战斗结算</summary>
    public void EnterBattlePhase()
    {
        // 检查所有将军是否已分配指令
        foreach (var general in Data.Battle.AllyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            if (general.AssignedOrder == null)
            {
                Debug.LogWarning($"将军{general.Name}未分配指令");
                return;
            }
        }

        Data.CurrentPhase = TurnPhase.BattlePhase;

        // 执行维克多AI
        var victorOrders = ExecuteVictorAI();

        // 战斗结算
        var battleResults = battleSystem.ResolveBattles(victorOrders);
        battleSystem.ApplyReinforcements();

        eventService.SendMessage((EventID)WarBrokerEventID.OnPhaseChange, TurnPhase.BattlePhase, null);
    }

    /// <summary>进入结算阶段：利息计算、期货结算、胜负判定</summary>
    public void EnterSettlementPhase()
    {
        Data.CurrentPhase = TurnPhase.SettlementPhase;

        // 记录回合历史
        RecordTurn();

        // 市场结算
        marketSystem.ApplyInterest();
        marketSystem.ApplyStorageCost();
        marketSystem.SettleExpiredFutures();
        marketSystem.CheckForceLiquidation();

        // 事件持续时间处理
        if (Data.ActiveEvent != null)
        {
            Data.EventRemainingTurns--;
            if (Data.EventRemainingTurns <= 0)
            {
                Data.ActiveEvent = null;
            }
        }

        // 更新占领状态
        UpdateOccupationStatus();

        // 胜负判定
        CheckVictoryConditions();

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
                EnterMarketPhase();
                break;
            case TurnPhase.MarketPhase:
                EnterIntentPhase();
                break;
            case TurnPhase.IntentPhase:
                EnterBattlePhase();
                break;
            case TurnPhase.BattlePhase:
                EnterSettlementPhase();
                break;
            default:
                EnterBattlePhase();
                EnterSettlementPhase();
                break;
        }
    }

    /// <summary>推进到下一阶段</summary>
    public void AdvancePhase()
    {
        switch (Data.CurrentPhase)
        {
            case TurnPhase.TurnStart:
                EnterEventPhase();
                break;
            case TurnPhase.EventPhase:
                EnterMarketPhase();
                break;
            case TurnPhase.MarketPhase:
                EnterIntentPhase();
                break;
            case TurnPhase.IntentPhase:
                EnterBattlePhase();
                break;
            case TurnPhase.BattlePhase:
                EnterSettlementPhase();
                break;
            case TurnPhase.SettlementPhase:
                // 结算阶段结束后自动进入下一回合
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
            if (frontline.TurnsAtAllyBase >= 1)
            {
                EndGame(GameResult.Defeat, $"战争失败 - {GetFrontlineName(frontline.Position)}沦陷");
                return;
            }
        }

        // 检查谈判停战（所有战线僵持 3 回合）
        if (battleSystem.CheckNegotiation())
        {
            EndGame(GameResult.Victory, "谈判停战");
            return;
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

    #endregion
}
