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

    // 维克多数据 (GDD v7: 使用 VictorLedger 统一管理)
    public VictorLedger VictorLedger;
    public VictorMemory VictorMemory;

    // 兼容旧代码的属性访问器
    public float VictorCash => VictorLedger?.Cash ?? 0f;
    public Dictionary<OrderType, int> VictorInventory => VictorLedger?.Holdings;
    public List<FuturesContract> VictorFuturesPositions => VictorLedger?.FuturesPositions;
    public float VictorBankDebt => VictorLedger?.Debt ?? 0f;

    public List<TurnRecord> TurnHistory;

    public RandomEventConfig ActiveEvent;
    public int EventRemainingTurns;

    // 委托任务结算结果（战役结束时填充）
    public Dictionary<string, bool> CommissionResults;
    public float CommissionTotalBonus;

    public void InitFromConfig(CampaignConfig campaignConfig, OrderConfig orderConfig)
    {
        Config = campaignConfig;
        CurrentTurn = 1;
        CurrentPhase = TurnPhase.TurnStart;

        Player = new PlayerData();
        Player.InitFromConfig(campaignConfig);

        Market = new MarketData();
        Market.InitFromConfig(orderConfig);

        Battle = new BattleData();
        Battle.InitFromConfig(campaignConfig);

        // 初始化维克多账本 (GDD v7)
        VictorLedger = new VictorLedger();
        VictorLedger.Init(campaignConfig.VictorInitialCash);

        // 初始化维克多记忆
        VictorMemory = new VictorMemory();
        VictorMemory.Init();

        TurnHistory = new List<TurnRecord>();
    }
}
