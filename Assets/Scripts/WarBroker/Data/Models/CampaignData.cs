using System;
using System.Collections.Generic;

/// <summary>交易记录</summary>
[Serializable]
public class TransactionRecord
{
    public enum TransactionType { Buy, Sell, FuturesOpen, FuturesClose, Borrow, Repay }

    public TransactionType Type;
    public OrderType? OrderType;
    public int Quantity;
    public float Price;
    public float TotalAmount;
    public string Description;
}

/// <summary>回合历史记录</summary>
[Serializable]
public class TurnRecord
{
    public int TurnNumber;
    public Dictionary<string, OrderType> OrderAssignments;
    public List<TransactionRecord> Transactions;
    public List<BattleResult> BattleResults;
    public Dictionary<OrderType, float> PriceSnapshot;
    public float PlayerNetWorth;
}

/// <summary>战役运行时数据</summary>
[Serializable]
public class CampaignRuntimeData
{
    public CampaignConfig Config { get; private set; }

    public int CurrentTurn;
    public int MaxTurns => Config.MaxTurns;
    public TurnPhase CurrentPhase;

    public PlayerData Player;
    public MarketData Market;
    public BattleData Battle;

    public float VictorCash;
    public Dictionary<OrderType, int> VictorInventory;

    public List<TurnRecord> TurnHistory;

    public RandomEventConfig ActiveEvent;
    public int EventRemainingTurns;

    public void InitFromConfig(CampaignConfig campaignConfig, OrderConfig orderConfig, SkillConfig skillConfig)
    {
        Config = campaignConfig;
        CurrentTurn = 1;
        CurrentPhase = TurnPhase.TurnStart;

        Player = new PlayerData();
        Player.InitFromConfig(campaignConfig);

        Market = new MarketData();
        Market.InitFromConfig(orderConfig);

        Battle = new BattleData();
        Battle.InitFromConfig(campaignConfig, skillConfig);

        VictorCash = campaignConfig.VictorInitialCash;
        VictorInventory = new Dictionary<OrderType, int>
        {
            { OrderType.ATK, 2 },
            { OrderType.DEF, 2 },
            { OrderType.RET, 2 }
        };

        TurnHistory = new List<TurnRecord>();
    }
}
