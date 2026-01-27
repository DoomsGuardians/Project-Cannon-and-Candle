using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// MarketSystem 纯数据逻辑测试
/// 注意：由于 MarketSystem 依赖 GameRoot (EventService/ResService)，
/// 这里测试的是运行时数据模型的核心计算逻辑，不依赖 MonoBehaviour。
/// </summary>
public class MarketDataTests
{
    private MarketData market;
    private PlayerData player;

    [SetUp]
    public void Setup()
    {
        market = new MarketData
        {
            CurrentPrices = new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 40f },
                { OrderType.DEF, 35f },
                { OrderType.RET, 25f }
            },
            MarketInventory = new Dictionary<OrderType, int>
            {
                { OrderType.ATK, 10 },
                { OrderType.DEF, 10 },
                { OrderType.RET, 8 }
            },
            PriceHistory = new List<Dictionary<OrderType, float>>()
        };

        player = new PlayerData
        {
            Cash = 500f,
            Inventory = new Dictionary<OrderType, int>
            {
                { OrderType.ATK, 2 },
                { OrderType.DEF, 2 },
                { OrderType.RET, 2 }
            },
            BankDebt = 0f,
            FuturesPositions = new List<FuturesContract>(),
            AuditValue = 0
        };
    }

    [Test]
    public void NetWorth_NoDebt_EqualsInvPlusCash()
    {
        float expected = 500f + 2 * 40f + 2 * 35f + 2 * 25f; // 500 + 80 + 70 + 50 = 700
        Assert.AreEqual(expected, player.CalculateNetWorth(market), 0.01f);
    }

    [Test]
    public void NetWorth_WithDebt_SubtractsDebt()
    {
        player.BankDebt = 100f;
        float expected = 500f + 2 * 40f + 2 * 35f + 2 * 25f - 100f; // 600
        Assert.AreEqual(expected, player.CalculateNetWorth(market), 0.01f);
    }

    [Test]
    public void NetWorth_WithFutures_IncludesPnL()
    {
        player.FuturesPositions.Add(new FuturesContract
        {
            ContractId = 1,
            TargetOrder = OrderType.ATK,
            Direction = FuturesDirection.Long,
            OpenPrice = 30f, // 当前40，盈利10 per unit
            Quantity = 2,
            Margin = 12f
        });

        // PnL = (40-30)*2 = 20
        float expectedPnL = 20f;
        float baseNetWorth = 500f + 2 * 40f + 2 * 35f + 2 * 25f;
        Assert.AreEqual(baseNetWorth + expectedPnL, player.CalculateNetWorth(market), 0.01f);
    }

    [Test]
    public void FuturesLong_PriceUp_PositivePnL()
    {
        var contract = new FuturesContract
        {
            Direction = FuturesDirection.Long,
            OpenPrice = 30f,
            Quantity = 3
        };
        // currentPrice=50 => PnL = (50-30)*3 = 60
        Assert.AreEqual(60f, contract.CalculatePnL(50f), 0.01f);
    }

    [Test]
    public void FuturesLong_PriceDown_NegativePnL()
    {
        var contract = new FuturesContract
        {
            Direction = FuturesDirection.Long,
            OpenPrice = 40f,
            Quantity = 2
        };
        // currentPrice=30 => PnL = (30-40)*2 = -20
        Assert.AreEqual(-20f, contract.CalculatePnL(30f), 0.01f);
    }

    [Test]
    public void FuturesShort_PriceDown_PositivePnL()
    {
        var contract = new FuturesContract
        {
            Direction = FuturesDirection.Short,
            OpenPrice = 40f,
            Quantity = 2
        };
        // currentPrice=30 => diff=30-40=-10, short reverses => PnL = 10*2 = 20
        Assert.AreEqual(20f, contract.CalculatePnL(30f), 0.01f);
    }

    [Test]
    public void FuturesShort_PriceUp_NegativePnL()
    {
        var contract = new FuturesContract
        {
            Direction = FuturesDirection.Short,
            OpenPrice = 30f,
            Quantity = 2
        };
        // currentPrice=50 => diff=50-30=20, short reverses => PnL = -20*2 = -40
        Assert.AreEqual(-40f, contract.CalculatePnL(50f), 0.01f);
    }
}
