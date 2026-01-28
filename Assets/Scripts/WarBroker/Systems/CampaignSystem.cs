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

    public CampaignRuntimeData Data { get; private set; }

    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;

        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        orderConfig = resService.LoadResource<OrderConfig>(ConfigPaths.ORDER_CONFIG);
        skillConfig = resService.LoadResource<SkillConfig>(ConfigPaths.SKILL_CONFIG);
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

    #region 回合流程

    public void StartTurn()
    {
        if (Data == null)
        {
            Debug.LogError("[CampaignSystem] Campaign data is null, cannot start turn.");
            return;
        }

        Data.CurrentPhase = TurnPhase.TurnStart;

        TryTriggerRandomEvent();

        foreach (var general in Data.Battle.AllyGenerals)
        {
            general.AssignedOrder = null;
        }

        Data.CurrentPhase = TurnPhase.PlayerAction;

        eventService.SendMessage((EventID)WarBrokerEventID.OnTurnStart, Data.CurrentTurn, null);
    }

    public void EndTurn()
    {
        foreach (var general in Data.Battle.AllyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            if (general.AssignedOrder == null)
            {
                Debug.LogWarning($"将军{general.Name}未分配指令");
                return;
            }
        }

        Data.CurrentPhase = TurnPhase.TurnEnd;

        RecordTurn();
        ExecuteTurnSettlement();

        if (CheckGameEnd()) return;

        Data.CurrentTurn++;
        StartTurn();
    }

    private void ExecuteTurnSettlement()
    {
        var victorOrders = ExecuteVictorAI();

        Data.CurrentPhase = TurnPhase.BattleResolve;
        var battleResults = battleSystem.ResolveBattles(victorOrders);

        battleSystem.ApplyReinforcements();
        marketSystem.ApplyInterest();
        marketSystem.ApplyStorageCost();
        marketSystem.SettleExpiredFutures();
        marketSystem.CheckForceLiquidation();

        Data.CurrentPhase = TurnPhase.MarketUpdate;
        var demandModifiers = CalculateDemandModifiers();
        marketSystem.UpdatePrices(demandModifiers);

        if (Data.ActiveEvent != null)
        {
            Data.EventRemainingTurns--;
            if (Data.EventRemainingTurns <= 0)
            {
                Data.ActiveEvent = null;
            }
        }

        eventService.SendMessage((EventID)WarBrokerEventID.OnTurnEnd, Data.CurrentTurn, null);
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
        float difficulty = Data.Config.VictorDifficulty;

        foreach (var general in Data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            OrderType order;

            if (UnityEngine.Random.value > difficulty)
            {
                order = general.Personality switch
                {
                    GeneralPersonality.Fanatic => OrderType.ATK,
                    GeneralPersonality.Conservative => OrderType.DEF,
                    _ => (OrderType)UnityEngine.Random.Range(0, 3)
                };
            }
            else
            {
                var frontline = Data.Battle.Frontlines[general.Position];
                order = frontline.LinePosition switch
                {
                    <= 2 => OrderType.ATK,
                    >= 4 => OrderType.DEF,
                    _ => general.Troops > 50 ? OrderType.ATK : OrderType.DEF
                };
            }

            orders[general.GeneralId] = order;
            general.AssignedOrder = order;
        }

        return orders;
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
                general.Troops = Mathf.Clamp(general.Troops + selectedEvent.AllTroopChange, 0, 100);
            }
            foreach (var general in Data.Battle.EnemyGenerals)
            {
                general.Troops = Mathf.Clamp(general.Troops + selectedEvent.AllTroopChange, 0, 100);
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

    private bool CheckGameEnd()
    {
        if (Data.Player.CalculateNetWorth(Data.Market) < 0)
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnDefeatConditionMet, "破产", null);
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false, null);
            return true;
        }

        if (Data.Player.AuditValue >= balanceConfig.AuditFailureThreshold)
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnDefeatConditionMet, "战争问责", null);
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false, null);
            return true;
        }

        if (battleSystem.CheckVictory())
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnVictoryConditionMet, "战争胜利", null);
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, true, null);
            return true;
        }

        if (battleSystem.CheckDefeat())
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnDefeatConditionMet, "战争失败", null);
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false, null);
            return true;
        }

        if (battleSystem.CheckNegotiation())
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnVictoryConditionMet, "谈判停战", null);
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, true, null);
            return true;
        }

        if (Data.CurrentTurn >= Data.MaxTurns)
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnDefeatConditionMet, "回合耗尽", null);
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false, null);
            return true;
        }

        return false;
    }

    #endregion
}
