using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 维克多财务账本 (GDD v7 Section 7.10)
/// 维克多拥有与玩家完全对称的财务系统：现金、持仓、期货、债务。
/// 战役过程中对玩家完全不可见，仅在战役结束时揭晓。
/// 实现 ITrader 接口，使 MarketSystem 的交易方法可通用调用。
/// </summary>
[Serializable]
public class VictorLedger : ITrader
{
    [SerializeField] private float cash;
    [SerializeField] private float debt;

    public Dictionary<OrderType, int> holdings;
    public List<FuturesContract> futures;

    // ── ITrader 接口实现 ──

    public float Cash
    {
        get => cash;
        set => cash = value;
    }

    public Dictionary<OrderType, int> Holdings => holdings;
    public List<FuturesContract> FuturesPositions => futures;

    public float Debt
    {
        get => debt;
        set => debt = value;
    }

    // ── 初始化 ──

    public void Init(float initialCash)
    {
        cash = initialCash;
        debt = 0f;
        holdings = new Dictionary<OrderType, int>
        {
            { OrderType.ATK, 0 },
            { OrderType.DEF, 0 },
            { OrderType.RET, 0 }
        };
        futures = new List<FuturesContract>();
    }

    // ── 净资产计算 (GDD v7 Section 2.3) ──

    /// <summary>
    /// 净资产 = 现金 + 指令库存市值 + 期货浮动盈亏 - 银行债务
    /// </summary>
    public float GetNetWorth(MarketData market)
    {
        float holdingsValue = 0f;
        foreach (var kvp in holdings)
            holdingsValue += kvp.Value * market.CurrentPrices[kvp.Key];

        float futuresPnL = 0f;
        foreach (var contract in futures)
            futuresPnL += contract.CalculatePnL(market.CurrentPrices[contract.TargetOrder]);

        return cash + holdingsValue + futuresPnL - debt;
    }

    // ── 便捷查询 ──

    /// <summary>总持仓数量（用于仓储费计算）</summary>
    public int TotalHoldings
    {
        get
        {
            int total = 0;
            foreach (var kvp in holdings)
                total += kvp.Value;
            return total;
        }
    }

    /// <summary>计算当前可借额度 (GDD v7 Section 3.5): 净资产×1.5 - 当前债务</summary>
    public float GetBorrowCapacity(MarketData market, float loanRatio)
    {
        float netWorth = GetNetWorth(market);
        if (netWorth <= 0f) return 0f;
        return Mathf.Max(0f, netWorth * loanRatio - debt);
    }

    /// <summary>未实现持仓+期货浮盈</summary>
    public float GetUnrealizedProfit(MarketData market, float initialCash)
    {
        return GetNetWorth(market) - initialCash;
    }
}
