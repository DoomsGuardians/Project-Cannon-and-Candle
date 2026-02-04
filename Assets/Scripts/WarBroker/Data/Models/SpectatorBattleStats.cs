using System.Collections.Generic;

/// <summary>
/// 游戏结束原因
/// </summary>
public enum SpectatorGameEndReason
{
    /// <summary>游戏进行中</summary>
    InProgress,
    /// <summary>达到最大回合数</summary>
    MaxTurnsReached,
    /// <summary>己方基地被占领</summary>
    AllyBaseOccupied,
    /// <summary>敌方基地被占领</summary>
    EnemyBaseOccupied,
    /// <summary>己方破产</summary>
    AllyBankrupt,
    /// <summary>敌方破产</summary>
    EnemyBankrupt,
    /// <summary>己方全军覆没</summary>
    AllyArmyDestroyed,
    /// <summary>敌方全军覆没</summary>
    EnemyArmyDestroyed
}

/// <summary>
/// 单方维克多统计数据
/// </summary>
public class VictorBattleStats
{
    /// <summary>造成的总伤害</summary>
    public int TotalDamageDealt;

    /// <summary>承受的总伤害</summary>
    public int TotalDamageTaken;

    /// <summary>总盈利</summary>
    public float TotalProfit;

    /// <summary>总亏损</summary>
    public float TotalLoss;

    /// <summary>最终现金</summary>
    public float FinalCash;

    /// <summary>最终净资产</summary>
    public float FinalNetWorth;

    /// <summary>最大净资产</summary>
    public float MaxNetWorth;

    /// <summary>最小净资产</summary>
    public float MinNetWorth = float.MaxValue;

    /// <summary>各类型指令使用次数</summary>
    public Dictionary<OrderType, int> OrdersUsed = new Dictionary<OrderType, int>
    {
        { OrderType.ATK, 0 },
        { OrderType.DEF, 0 },
        { OrderType.RET, 0 }
    };

    /// <summary>各策略使用次数</summary>
    public Dictionary<VictorStrategy, int> StrategiesUsed = new Dictionary<VictorStrategy, int>
    {
        { VictorStrategy.MilitaryFocus, 0 },
        { VictorStrategy.TrendFollowing, 0 },
        { VictorStrategy.CounterStrike, 0 },
        { VictorStrategy.DeceptionPlay, 0 },
        { VictorStrategy.Harvest, 0 }
    };

    /// <summary>现货交易次数</summary>
    public int SpotTradeCount;

    /// <summary>期货交易次数</summary>
    public int FuturesTradeCount;

    /// <summary>将军强化次数</summary>
    public int ReinforceCount;

    /// <summary>将军篡改次数</summary>
    public int TamperCount;

    /// <summary>借款次数</summary>
    public int BorrowCount;

    /// <summary>总借款金额</summary>
    public float TotalBorrowed;

    /// <summary>将军阵亡次数</summary>
    public int GeneralsLost;

    /// <summary>击杀敌方将军次数</summary>
    public int GeneralsKilled;
}

/// <summary>
/// 观战模式对战统计数据
/// </summary>
public class SpectatorBattleStats
{
    /// <summary>总回合数</summary>
    public int TotalTurns;

    /// <summary>游戏结束原因</summary>
    public SpectatorGameEndReason EndReason = SpectatorGameEndReason.InProgress;

    /// <summary>己方是否获胜</summary>
    public bool AllyWon;

    /// <summary>己方统计</summary>
    public VictorBattleStats AllyStats = new VictorBattleStats();

    /// <summary>敌方统计</summary>
    public VictorBattleStats EnemyStats = new VictorBattleStats();

    /// <summary>每回合的战线位置历史</summary>
    public List<float> FrontlineHistory = new List<float>();

    /// <summary>每回合己方净资产历史</summary>
    public List<float> AllyNetWorthHistory = new List<float>();

    /// <summary>每回合敌方净资产历史</summary>
    public List<float> EnemyNetWorthHistory = new List<float>();

    /// <summary>每回合价格历史 (OrderType -> List of prices)</summary>
    public Dictionary<OrderType, List<float>> PriceHistory = new Dictionary<OrderType, List<float>>
    {
        { OrderType.ATK, new List<float>() },
        { OrderType.DEF, new List<float>() },
        { OrderType.RET, new List<float>() }
    };

    /// <summary>获取胜负描述</summary>
    public string GetResultDescription()
    {
        string winner = AllyWon ? "己方维克多" : "敌方维克多";
        string reason = EndReason switch
        {
            SpectatorGameEndReason.MaxTurnsReached => "达到最大回合数（按净资产判定）",
            SpectatorGameEndReason.AllyBaseOccupied => "己方基地被占领",
            SpectatorGameEndReason.EnemyBaseOccupied => "敌方基地被占领",
            SpectatorGameEndReason.AllyBankrupt => "己方破产",
            SpectatorGameEndReason.EnemyBankrupt => "敌方破产",
            SpectatorGameEndReason.AllyArmyDestroyed => "己方全军覆没",
            SpectatorGameEndReason.EnemyArmyDestroyed => "敌方全军覆没",
            _ => "未知原因"
        };
        return $"{winner}获胜 - {reason}";
    }
}
