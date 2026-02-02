using System.Collections.Generic;

/// <summary>
/// 交易者接口 (GDD v7 Section 7.1 — 对称掮客)
/// 玩家和维克多都实现此接口，使 MarketSystem 的交易方法可通用调用。
/// PlayerData 和 VictorLedger 均需实现此接口。
/// </summary>
public interface ITrader
{
    float Cash { get; set; }
    Dictionary<OrderType, int> Holdings { get; }
    List<FuturesContract> FuturesPositions { get; }
    float Debt { get; set; }
    float GetNetWorth(MarketData market);
}
