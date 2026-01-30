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
    FullStrength, // 满编 (HP > 15)
    Healthy,      // 健康 (HP 11-15)
    Wounded,      // 受伤 (HP 6-10)
    Critical,     // 濒死 (HP 1-5)
    Routed        // 溃败 (HP <= 0)
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
    TurnStart,       // 回合开始（内部状态）
    EventPhase,      // 事件阶段
    MarketPhase,     // 市场阶段
    IntentPhase,     // 意图阶段
    BattlePhase,     // 战斗阶段
    SettlementPhase  // 结算阶段
}

/// <summary>游戏结果</summary>
public enum GameResult
{
    InProgress, // 进行中
    Victory,    // 胜利
    Defeat,     // 失败
    Draw        // 平局
}

/// <summary>意图来源</summary>
public enum IntentSource
{
    Default,    // 默认意图（灰色气泡）
    Reinforced, // 强化（金色气泡）
    Overridden  // 篡改（红色气泡）
}
