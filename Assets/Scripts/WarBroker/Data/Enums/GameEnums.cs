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
    Left,       // 左翼 / 第一战线
    LeftCenter, // 左中 / 第二战线
    Center,     // 中军 / 第三战线
    RightCenter,// 右中 / 第四战线
    Right       // 右翼 / 第五战线
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

/// <summary>委托任务类型</summary>
public enum CommissionType
{
    OccupyGrid,       // 占领指定 Grid（如 WinWar：占领 Grid 5）
    EnemyReachGrid,   // 敌方到达指定 Grid 数量（如 ShortCountry：敌方到达 Grid 2）
    NotOccupyGrid,    // 未占领指定 Grid（如 Traitor：未占领 Grid 5）
    TotalCasualties   // 总伤亡达到数量（如 MeatGrinder：伤亡 >= 100）
}

// VictorStrategy 已移至 VictorAISystem.cs

/// <summary>交易方向</summary>
public enum TradeDirection
{
    Buy,   // 买入
    Sell   // 卖出
}

// DeceptionType 已移至 VictorMemory.cs (DeceptionPlanType)
