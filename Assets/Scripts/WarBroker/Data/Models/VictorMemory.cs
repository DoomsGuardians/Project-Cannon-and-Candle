using System;
using System.Collections.Generic;

/// <summary>
/// 陷阱类型 (GDD v7 Section 7.7)
/// </summary>
public enum DeceptionPlanType
{
    None,              // 无活跃陷阱
    PumpAndDump,       // 拉高出货：Phase1买入推高 → Phase2抛售砸盘
    FalseSignal,       // 虚假信号：大量买入某类指令但不给将军用
    DelayedPurchase    // 延迟采购：积攒需求后突然大量买入
}

/// <summary>
/// 维克多跨回合记忆 (GDD v7 Section 7.7-7.8)
/// 高 Deception 维克多拥有跨回合记忆，可执行多回合陷阱。
/// 高 Adaptiveness 维克多可累积推断玩家仓位方向。
/// </summary>
[Serializable]
public class VictorMemory
{
    // ── 陷阱状态 (GDD v7 Section 7.7) ──

    /// <summary>正在执行的陷阱类型</summary>
    public DeceptionPlanType ActiveTrap = DeceptionPlanType.None;

    /// <summary>陷阱目标指数（ATK/DEF/RET）</summary>
    public OrderType TrapTargetType;

    /// <summary>
    /// 陷阱阶段：
    /// 0 = 无活跃陷阱
    /// 1 = 布局中（Phase 1：买入推高/制造假信号）
    /// 2 = 该收割了（Phase 2：下回合强制进入 Harvest 策略）
    /// </summary>
    public int TrapPhase;

    /// <summary>为陷阱囤积的库存量</summary>
    public int TrapHoldings;

    /// <summary>陷阱开始的回合</summary>
    public int TrapStartTurn;

    // ── 玩家仓位推断 (GDD v7 Section 7.8) ──

    /// <summary>
    /// 推测玩家对各指数的净仓位方向
    /// 正值 = 推测玩家做多, 负值 = 推测玩家做空
    /// 通过排除维克多自身交易冲击后的残差信号累积推断
    /// </summary>
    public Dictionary<OrderType, float> EstimatedPlayerPosition;

    /// <summary>近几回合玩家对各指数的交易冲击方向（用于短期推断）</summary>
    public Dictionary<OrderType, float> RecentPlayerImpact;

    // ── 维克多本回合交易记录（用于推断玩家影响的排除项）──

    /// <summary>本回合维克多在各指数上的净买入量（正=买, 负=卖）</summary>
    public Dictionary<OrderType, int> VictorTurnTradeVolume;

    // ── 回合开盘价快照（用于推断价格变化）──

    /// <summary>当前回合开盘时的价格快照</summary>
    public Dictionary<OrderType, float> TurnOpenPrices;

    // ── 策略追踪（用于避免连续使用同一策略）──

    /// <summary>上回合的净资产（用于判断是否亏损）</summary>
    public float LastNetWorth;

    /// <summary>上回合使用的策略</summary>
    public VictorStrategy LastStrategy;

    /// <summary>连续使用同一策略的回合数</summary>
    public int ConsecutiveStrategyCount;

    // ── 初始化 ──

    public void Init()
    {
        ActiveTrap = DeceptionPlanType.None;
        TrapPhase = 0;
        TrapHoldings = 0;
        TrapStartTurn = 0;

        EstimatedPlayerPosition = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 0f },
            { OrderType.DEF, 0f },
            { OrderType.RET, 0f }
        };

        RecentPlayerImpact = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 0f },
            { OrderType.DEF, 0f },
            { OrderType.RET, 0f }
        };

        VictorTurnTradeVolume = new Dictionary<OrderType, int>
        {
            { OrderType.ATK, 0 },
            { OrderType.DEF, 0 },
            { OrderType.RET, 0 }
        };

        TurnOpenPrices = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 0f },
            { OrderType.DEF, 0f },
            { OrderType.RET, 0f }
        };

        // 策略追踪初始化
        LastNetWorth = 0f;
        LastStrategy = VictorStrategy.MilitaryFocus;
        ConsecutiveStrategyCount = 0;
    }

    // ── 便捷方法 ──

    /// <summary>是否有活跃陷阱</summary>
    public bool HasActiveTrap => ActiveTrap != DeceptionPlanType.None && TrapPhase > 0;

    /// <summary>推进陷阱到收割阶段（Phase 1 → Phase 2）</summary>
    public void AdvanceTrapToHarvest()
    {
        if (TrapPhase == 1)
            TrapPhase = 2;
    }

    /// <summary>清除陷阱状态</summary>
    public void ClearTrap()
    {
        ActiveTrap = DeceptionPlanType.None;
        TrapPhase = 0;
        TrapHoldings = 0;
        TrapStartTurn = 0;
    }

    /// <summary>重置本回合交易记录（在回合开始时调用）</summary>
    public void ResetTurnData(MarketData market)
    {
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            VictorTurnTradeVolume[type] = 0;
            TurnOpenPrices[type] = market.CurrentPrices[type];
        }
    }
}
