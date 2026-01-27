using System;
using System.Collections.Generic;

/// <summary>市场运行时数据</summary>
[Serializable]
public class MarketData
{
    public Dictionary<OrderType, float> CurrentPrices;
    public Dictionary<OrderType, int> MarketInventory;
    public List<Dictionary<OrderType, float>> PriceHistory;

    public void InitFromConfig(OrderConfig config)
    {
        CurrentPrices = new Dictionary<OrderType, float>();
        MarketInventory = new Dictionary<OrderType, int>();
        PriceHistory = new List<Dictionary<OrderType, float>>();

        foreach (var item in config.Orders)
        {
            CurrentPrices[item.OrderType] = item.BasePrice;
            MarketInventory[item.OrderType] = item.InitialStock;
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
    }

    public float CalculateNetWorth(MarketData market)
    {
        float inventoryValue = 0f;
        foreach (var kvp in Inventory)
        {
            inventoryValue += kvp.Value * market.CurrentPrices[kvp.Key];
        }

        float futuresPnL = 0f;
        foreach (var contract in FuturesPositions)
        {
            futuresPnL += contract.CalculatePnL(market.CurrentPrices[contract.TargetOrder]);
        }

        return Cash + inventoryValue + futuresPnL - BankDebt;
    }
}
