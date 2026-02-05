/// <summary>
/// Victor 回合动作类型（用于信件对话系统）
/// 将 VictorTurnPlan 中的具体操作抽象为可视化类别
/// </summary>
public enum VictorActionType
{
    /// <summary>现货买入</summary>
    SpotBuy,

    /// <summary>现货卖出</summary>
    SpotSell,

    /// <summary>开立期货（做多）</summary>
    FuturesLong,

    /// <summary>开立期货（做空）</summary>
    FuturesShort,

    /// <summary>平仓期货</summary>
    FuturesClose,

    /// <summary>强化将军</summary>
    GeneralReinforce,

    /// <summary>篡改将军意图</summary>
    GeneralTamper,

    /// <summary>借款</summary>
    Borrow,

    /// <summary>还款</summary>
    Repay
}

/// <summary>
/// 动作规模（用于信件对话系统的语气调整）
/// </summary>
public enum ActionScale
{
    /// <summary>小规模（1-2份）</summary>
    Small,

    /// <summary>中等规模（3-5份）</summary>
    Medium,

    /// <summary>大规模（6+份）</summary>
    Large
}
