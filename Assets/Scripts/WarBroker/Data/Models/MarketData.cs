using System;
using System.Collections.Generic;

/// <summary>K线数据</summary>
[Serializable]
public class KLineData
{
    public int Turn;           // 回合数
    public float Open;         // 开盘价
    public float High;         // 最高价
    public float Low;          // 最低价
    public float Close;        // 收盘价
    public float Volume;       // 成交量
}

/// <summary>市场运行时数据</summary>
[Serializable]
public class MarketData
{
    public Dictionary<OrderType, float> CurrentPrices;
    public Dictionary<OrderType, int> MarketInventory;      // GDD v6.0: 改为int
    public Dictionary<OrderType, int> InitialFloat;         // GDD v6.0: 改为int，初始流通盘（用于Gamma计算）
    public List<Dictionary<OrderType, float>> PriceHistory;
    public Dictionary<OrderType, float> BetaCarry;          // Beta累积值（初始值1.0）
    public Dictionary<OrderType, float> LastWeekBurn;       // 上周消耗量（用于需求预测）
    public Dictionary<OrderType, float> AccumulatedBurn;    // 累计消耗量（三回合累计）
    public Dictionary<OrderType, List<KLineData>> KLineHistory;  // K线历史数据
    public int LastReplenishTurn;                           // 上次补充的回合

    public void InitFromConfig(OrderConfig config)
    {
        CurrentPrices = new Dictionary<OrderType, float>();
        MarketInventory = new Dictionary<OrderType, int>();
        InitialFloat = new Dictionary<OrderType, int>();
        PriceHistory = new List<Dictionary<OrderType, float>>();
        BetaCarry = new Dictionary<OrderType, float>();
        LastWeekBurn = new Dictionary<OrderType, float>();
        AccumulatedBurn = new Dictionary<OrderType, float>();
        KLineHistory = new Dictionary<OrderType, List<KLineData>>();
        LastReplenishTurn = 0;

        foreach (var item in config.Orders)
        {
            CurrentPrices[item.OrderType] = item.BasePrice;
            MarketInventory[item.OrderType] = item.InitialStock;
            InitialFloat[item.OrderType] = item.InitialStock;
            BetaCarry[item.OrderType] = 1.0f;  // GDD v6.0: Beta初始值为1.0
            LastWeekBurn[item.OrderType] = 0f;
            AccumulatedBurn[item.OrderType] = 0f;
            KLineHistory[item.OrderType] = new List<KLineData>();
        }
    }
}

/// <summary>期货合约</summary>
[Serializable]
public class FuturesContract
{
    public int ContractId;
    public OrderType TargetOrder;
    public FuturesDirection Direction;
    public float OpenPrice;
    public int Quantity;
    public int ExpirationTurn;
    public float Margin;

    public float CalculatePnL(float currentPrice)
    {
        float diff = currentPrice - OpenPrice;
        if (Direction == FuturesDirection.Short) diff = -diff;
        return diff * Quantity;
    }
}

/// <summary>玩家运行时数据</summary>
[Serializable]
public class PlayerData
{
    public float Cash;
    public Dictionary<OrderType, int> Inventory;
    public float BankDebt;
    public List<FuturesContract> FuturesPositions;
    public int AuditValue;
    public float EntryFee;  // 入场费（用于计算总成本）

    public void InitFromConfig(CampaignConfig config)
    {
        Cash = config.InitialCash;
        Inventory = new Dictionary<OrderType, int>
        {
            { OrderType.ATK, config.InitialAtkInventory },
            { OrderType.DEF, config.InitialDefInventory },
            { OrderType.RET, config.InitialRetInventory }
        };
        BankDebt = 0f;
        FuturesPositions = new List<FuturesContract>();
        AuditValue = 0;
        EntryFee = 0f;  // 初始化入场费
    }

    public float CalculateNetWorth(MarketData market)
    {
        float inventoryValue = 0f;
        foreach (var kvp in Inventory)
        {
            inventoryValue += kvp.Value * market.CurrentPrices[kvp.Key];
        }

        // 期货价值 = 保证金 + 浮动盈亏 (GDD v7.0)
        float futuresValue = 0f;
        foreach (var contract in FuturesPositions)
        {
            futuresValue += contract.Margin + contract.CalculatePnL(market.CurrentPrices[contract.TargetOrder]);
        }

        return Cash + inventoryValue + futuresValue - BankDebt;
    }
}
