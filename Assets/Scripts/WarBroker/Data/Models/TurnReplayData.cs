using System;
using System.Collections.Generic;

/// <summary>
/// 回合回放数据
/// 记录单个回合的完整状态和操作
/// </summary>
[Serializable]
public class TurnReplayData
{
    /// <summary>回合数</summary>
    public int TurnNumber;

    /// <summary>回合开始时的快照</summary>
    public TurnSnapshot StartSnapshot;

    /// <summary>玩家操作记录</summary>
    public List<PlayerAction> PlayerActions;

    /// <summary>战斗结果记录</summary>
    public List<BattleResultRecord> BattleResults;

    /// <summary>回合结束时的快照</summary>
    public TurnSnapshot EndSnapshot;

    public TurnReplayData()
    {
        PlayerActions = new List<PlayerAction>();
        BattleResults = new List<BattleResultRecord>();
    }
}

/// <summary>
/// 回合快照
/// 记录某一时刻的完整游戏状态
/// </summary>
[Serializable]
public class TurnSnapshot
{
    /// <summary>玩家现金</summary>
    public float PlayerCash;

    /// <summary>玩家净资产</summary>
    public float PlayerNetWorth;

    /// <summary>玩家库存（序列化用列表）</summary>
    public List<OrderInventoryItem> PlayerInventory;

    /// <summary>市场价格（序列化用列表）</summary>
    public List<OrderPriceItem> MarketPrices;

    /// <summary>将军状态快照</summary>
    public List<GeneralSnapshot> AllyGenerals;
    public List<GeneralSnapshot> EnemyGenerals;

    /// <summary>战线状态快照</summary>
    public List<FrontlineSnapshot> Frontlines;

    public TurnSnapshot()
    {
        PlayerInventory = new List<OrderInventoryItem>();
        MarketPrices = new List<OrderPriceItem>();
        AllyGenerals = new List<GeneralSnapshot>();
        EnemyGenerals = new List<GeneralSnapshot>();
        Frontlines = new List<FrontlineSnapshot>();
    }
}

/// <summary>
/// 订单库存项（用于序列化Dictionary）
/// </summary>
[Serializable]
public class OrderInventoryItem
{
    public OrderType OrderType;
    public int Quantity;
}

/// <summary>
/// 订单价格项（用于序列化Dictionary）
/// </summary>
[Serializable]
public class OrderPriceItem
{
    public OrderType OrderType;
    public float Price;
}

/// <summary>
/// 玩家操作类型
/// </summary>
public enum PlayerActionType
{
    BuyOrder,           // 买入订单
    SellOrder,          // 卖出订单
    OpenFutures,        // 开仓期货
    CloseFutures,       // 平仓期货
    Borrow,             // 借款
    Repay,              // 还款
    AssignOrder,        // 分配订单给将军
    ReinforceIntent,    // 强化意图
    OverrideIntent      // 篡改意图
}

/// <summary>
/// 玩家操作记录
/// </summary>
[Serializable]
public class PlayerAction
{
    /// <summary>操作类型</summary>
    public PlayerActionType ActionType;

    /// <summary>操作时间戳（ISO 8601 格式）</summary>
    public string Timestamp;

    /// <summary>订单类型（如适用）</summary>
    public OrderType? OrderType;

    /// <summary>数量</summary>
    public int Quantity;

    /// <summary>金额</summary>
    public float Amount;

    /// <summary>目标将军ID（如适用）</summary>
    public string TargetGeneralId;

    /// <summary>期货方向（如适用）</summary>
    public FuturesDirection? FuturesDirection;

    /// <summary>操作描述</summary>
    public string Description;

    public static PlayerAction CreateTradeAction(PlayerActionType type, OrderType orderType, int quantity, float amount)
    {
        return new PlayerAction
        {
            ActionType = type,
            Timestamp = DateTime.UtcNow.ToString("o"),
            OrderType = orderType,
            Quantity = quantity,
            Amount = amount
        };
    }

    public static PlayerAction CreateFuturesAction(PlayerActionType type, OrderType orderType, FuturesDirection direction, int quantity, float margin)
    {
        return new PlayerAction
        {
            ActionType = type,
            Timestamp = DateTime.UtcNow.ToString("o"),
            OrderType = orderType,
            FuturesDirection = direction,
            Quantity = quantity,
            Amount = margin
        };
    }

    public static PlayerAction CreateIntentAction(PlayerActionType type, string generalId, OrderType orderType)
    {
        return new PlayerAction
        {
            ActionType = type,
            Timestamp = DateTime.UtcNow.ToString("o"),
            TargetGeneralId = generalId,
            OrderType = orderType
        };
    }
}

/// <summary>
/// 战斗结果记录（用于回放）
/// </summary>
[Serializable]
public class BattleResultRecord
{
    /// <summary>战线位置</summary>
    public FrontlinePosition Position;

    /// <summary>己方指令</summary>
    public OrderType AllyOrder;

    /// <summary>敌方指令</summary>
    public OrderType EnemyOrder;

    /// <summary>己方伤亡</summary>
    public int AllyCasualties;

    /// <summary>敌方伤亡</summary>
    public int EnemyCasualties;

    /// <summary>战线移动</summary>
    public int LineMovement;

    /// <summary>是否暴击</summary>
    public bool WasCrit;

    /// <summary>是否失误</summary>
    public bool WasFumble;

    /// <summary>结果描述</summary>
    public string Description;
}

/// <summary>
/// 将军状态快照
/// </summary>
[Serializable]
public class GeneralSnapshot
{
    /// <summary>将军ID</summary>
    public string GeneralId;

    /// <summary>将军名称</summary>
    public string Name;

    /// <summary>当前兵力</summary>
    public int Troops;

    /// <summary>信任度</summary>
    public int Trust;

    /// <summary>士气</summary>
    public int Morale;

    /// <summary>当前意图</summary>
    public OrderType? CurrentIntent;

    /// <summary>意图来源</summary>
    public IntentSource IntentSource;

    /// <summary>战线位置</summary>
    public FrontlinePosition Position;

    /// <summary>格子位置</summary>
    public int GridPosition;

    /// <summary>状态</summary>
    public GeneralStatus Status;
}

/// <summary>
/// 战线状态快照
/// </summary>
[Serializable]
public class FrontlineSnapshot
{
    /// <summary>战线位置</summary>
    public FrontlinePosition Position;

    /// <summary>战线位置值</summary>
    public float LinePosition;

    /// <summary>格子归属（5个格子）</summary>
    public GridOwner[] GridOwners;

    /// <summary>停滞回合数</summary>
    public int StagnantTurns;
}
