/// <summary>
/// 回合结算信息
/// 用于通知玩家本回合的各种费用扣除情况
/// </summary>
public class TurnSettlementInfo
{
    /// <summary>支付的银行利息</summary>
    public float InterestPaid;

    /// <summary>支付的仓储费用</summary>
    public float StorageCost;

    /// <summary>到期期货数量</summary>
    public int ExpiredFuturesCount;

    /// <summary>到期期货总盈亏</summary>
    public float ExpiredFuturesPnL;

    /// <summary>是否有值得显示的变化</summary>
    public bool HasSignificantChanges =>
        InterestPaid > 0.01f ||
        StorageCost > 0.01f ||
        ExpiredFuturesCount > 0;
}
