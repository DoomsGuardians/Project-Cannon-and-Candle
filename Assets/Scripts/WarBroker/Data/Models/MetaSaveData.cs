using System;
using System.Collections.Generic;

/// <summary>
/// 局外存档主数据（Meta-game Data）
/// 保存玩家的累计资产、统计数据和历史战役记录
/// </summary>
[Serializable]
public class MetaSaveData
{
    /// <summary>存档版本号（用于版本迁移）</summary>
    public int SaveVersion = 1;

    /// <summary>最后保存时间（ISO 8601 格式）</summary>
    public string LastSaveTime;

    /// <summary>累计财富（当前总资产）</summary>
    public float TotalWealth;

    /// <summary>历史总收入</summary>
    public float LifetimeEarnings;

    /// <summary>历史总亏损</summary>
    public float LifetimeLosses;

    /// <summary>玩家统计数据</summary>
    public PlayerStatistics Statistics;

    /// <summary>历史战役记录</summary>
    public List<CampaignHistoryRecord> CampaignHistory;

    /// <summary>解锁的战役ID列表（预留）</summary>
    public List<string> UnlockedCampaigns;

    /// <summary>创建默认的局外存档数据</summary>
    public static MetaSaveData CreateDefault()
    {
        return new MetaSaveData
        {
            SaveVersion = 1,
            LastSaveTime = DateTime.UtcNow.ToString("o"),
            TotalWealth = 0f,
            LifetimeEarnings = 0f,
            LifetimeLosses = 0f,
            Statistics = PlayerStatistics.CreateDefault(),
            CampaignHistory = new List<CampaignHistoryRecord>(),
            UnlockedCampaigns = new List<string>()
        };
    }
}

/// <summary>
/// 玩家统计数据
/// </summary>
[Serializable]
public class PlayerStatistics
{
    /// <summary>总游戏时长（秒）</summary>
    public float TotalPlayTimeSeconds;

    /// <summary>总战役数</summary>
    public int TotalCampaignsPlayed;

    /// <summary>胜利次数</summary>
    public int TotalVictories;

    /// <summary>失败次数</summary>
    public int TotalDefeats;

    /// <summary>平局次数</summary>
    public int TotalDraws;

    /// <summary>总交易次数</summary>
    public int TotalTradeCount;

    /// <summary>总回合数</summary>
    public int TotalTurnsPlayed;

    /// <summary>单战役最高盈利</summary>
    public float BestSingleCampaignProfit;

    /// <summary>单战役最大亏损</summary>
    public float WorstSingleCampaignLoss;

    /// <summary>创建默认统计数据</summary>
    public static PlayerStatistics CreateDefault()
    {
        return new PlayerStatistics
        {
            TotalPlayTimeSeconds = 0f,
            TotalCampaignsPlayed = 0,
            TotalVictories = 0,
            TotalDefeats = 0,
            TotalDraws = 0,
            TotalTradeCount = 0,
            TotalTurnsPlayed = 0,
            BestSingleCampaignProfit = 0f,
            WorstSingleCampaignLoss = 0f
        };
    }
}
