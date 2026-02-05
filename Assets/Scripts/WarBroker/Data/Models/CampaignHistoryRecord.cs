using System;
using System.Collections.Generic;

/// <summary>
/// 战役历史记录
/// 保存单次战役的完整结果和统计
/// </summary>
[Serializable]
public class CampaignHistoryRecord
{
    /// <summary>记录唯一ID（GUID）</summary>
    public string RecordId;

    /// <summary>战役配置ID</summary>
    public string CampaignId;

    /// <summary>战役名称</summary>
    public string CampaignName;

    /// <summary>游戏结果</summary>
    public GameResult Result;

    /// <summary>完成时间（ISO 8601 格式）</summary>
    public string CompletionTime;

    /// <summary>结束回合</summary>
    public int FinalTurn;

    /// <summary>财务统计</summary>
    public FinancialSummary Financial;

    /// <summary>战斗统计</summary>
    public BattleSummary Battle;

    /// <summary>委托任务结果</summary>
    public List<CommissionResultRecord> Commissions;

    /// <summary>是否有回放数据</summary>
    public bool HasReplayData;

    /// <summary>回放数据（可选）</summary>
    public List<TurnReplayData> ReplayData;

    /// <summary>创建新的战役历史记录</summary>
    public static CampaignHistoryRecord Create(string campaignId, string campaignName)
    {
        return new CampaignHistoryRecord
        {
            RecordId = Guid.NewGuid().ToString(),
            CampaignId = campaignId,
            CampaignName = campaignName,
            Result = GameResult.InProgress,
            CompletionTime = "",
            FinalTurn = 0,
            Financial = new FinancialSummary(),
            Battle = new BattleSummary(),
            Commissions = new List<CommissionResultRecord>(),
            HasReplayData = false,
            ReplayData = new List<TurnReplayData>()
        };
    }
}

/// <summary>
/// 财务统计摘要
/// </summary>
[Serializable]
public class FinancialSummary
{
    /// <summary>初始资金</summary>
    public float InitialCash;

    /// <summary>最终现金</summary>
    public float FinalCash;

    /// <summary>最终净资产</summary>
    public float FinalNetWorth;

    /// <summary>总盈亏</summary>
    public float TotalProfitLoss;

    /// <summary>委托奖金总额</summary>
    public float CommissionBonus;

    /// <summary>总交易次数</summary>
    public int TradeCount;

    /// <summary>最大单笔盈利</summary>
    public float LargestSingleProfit;

    /// <summary>最大单笔亏损</summary>
    public float LargestSingleLoss;
}

/// <summary>
/// 战斗统计摘要
/// </summary>
[Serializable]
public class BattleSummary
{
    /// <summary>己方总伤亡</summary>
    public int AllyCasualties;

    /// <summary>敌方总伤亡</summary>
    public int EnemyCasualties;

    /// <summary>己方溃败将军数</summary>
    public int AllyGeneralsRouted;

    /// <summary>敌方溃败将军数</summary>
    public int EnemyGeneralsRouted;

    /// <summary>最终战线位置（左翼）</summary>
    public float FinalLinePositionLeft;

    /// <summary>最终战线位置（中军）</summary>
    public float FinalLinePositionCenter;

    /// <summary>最终战线位置（右翼）</summary>
    public float FinalLinePositionRight;
}

/// <summary>
/// 委托任务结果记录
/// </summary>
[Serializable]
public class CommissionResultRecord
{
    /// <summary>委托ID</summary>
    public string CommissionId;

    /// <summary>委托名称</summary>
    public string CommissionName;

    /// <summary>是否完成</summary>
    public bool IsCompleted;

    /// <summary>奖金金额</summary>
    public float BonusAmount;
}
