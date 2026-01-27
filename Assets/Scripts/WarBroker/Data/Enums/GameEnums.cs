/// <summary>指令类型</summary>
public enum OrderType
{
    ATK,  // 进攻令
    DEF,  // 防守令
    RET   // 撤退令
}

/// <summary>将军性格</summary>
public enum GeneralPersonality
{
    Fanatic,      // 狂热型
    Conservative, // 保守型
    Opportunist   // 投机型
}

/// <summary>将军状态</summary>
public enum GeneralStatus
{
    Healthy,   // 健康 (>70)
    Wounded,   // 受伤 (50-70)
    Critical,  // 濒死 (30-50)
    Routed     // 溃败 (<30 或 兵力<20)
}

/// <summary>战线位置</summary>
public enum FrontlinePosition
{
    Left,   // 左翼
    Center, // 中军
    Right   // 右翼
}

/// <summary>期货方向</summary>
public enum FuturesDirection
{
    Long,  // 做多
    Short  // 做空
}

/// <summary>回合阶段</summary>
public enum TurnPhase
{
    TurnStart,      // 回合开始
    PlayerAction,   // 玩家行动
    TurnEnd,        // 回合结算
    BattleResolve,  // 战斗结算
    MarketUpdate    // 市场更新
}
