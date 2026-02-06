using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 委托任务系统 (配置化版本)
/// 从 CommissionConfig 加载委托定义，支持动态配置
/// 委托完成时即时发放奖励
/// </summary>
public class CommissionSystem
{
    private CampaignRuntimeData campaignData;
    private GameBalanceConfig balanceConfig;
    private List<CommissionConfig> commissions;

    // 已完成并发放奖励的委托ID集合
    private HashSet<string> completedCommissions = new HashSet<string>();

    public void Init(CampaignRuntimeData campaignData, GameBalanceConfig balanceConfig)
    {
        this.campaignData = campaignData;
        this.balanceConfig = balanceConfig;
        this.commissions = new List<CommissionConfig>();
        this.completedCommissions.Clear();
    }

    /// <summary>
    /// 从配置加载委托任务列表
    /// </summary>
    public void LoadCommissions(List<CommissionConfig> commissionConfigs)
    {
        commissions = commissionConfigs ?? new List<CommissionConfig>();
        Debug.Log($"[CommissionSystem] 加载了 {commissions.Count} 个委托任务");
    }

    /// <summary>
    /// 获取当前委托任务列表
    /// </summary>
    public List<CommissionConfig> GetCommissions() => commissions;

    /// <summary>
    /// 检查并结算所有委托任务
    /// 在战役结束时调用，返回最终结果（不再重复发放已发放的奖励）
    /// </summary>
    public Dictionary<string, bool> CheckAndSettleCommissions(out float totalBonus)
    {
        var results = new Dictionary<string, bool>();
        totalBonus = campaignData.CommissionTotalBonus; // 使用已累计的奖金

        foreach (var config in commissions)
        {
            bool achieved = CheckCommission(config);
            results[config.CommissionId] = achieved;
        }

        return results;
    }

    /// <summary>
    /// 每回合检查委托完成情况，新完成的委托立即发放奖励
    /// 在结算阶段调用
    /// </summary>
    /// <returns>本回合新完成的委托列表</returns>
    public List<CommissionConfig> CheckCommissionsThisTurn()
    {
        var newlyCompleted = new List<CommissionConfig>();

        foreach (var config in commissions)
        {
            // 跳过已完成的委托
            if (completedCommissions.Contains(config.CommissionId))
                continue;

            bool achieved = CheckCommission(config);
            if (achieved)
            {
                // 标记为已完成
                completedCommissions.Add(config.CommissionId);

                // 立即发放奖励
                campaignData.Player.Cash += config.BonusAmount;
                campaignData.CommissionTotalBonus += config.BonusAmount;

                newlyCompleted.Add(config);

                Debug.Log($"[委托达成] {config.DisplayName}: +${config.BonusAmount} (第{campaignData.CurrentTurn}周)");

                // 触发事件通知UI
                GameRoot.Instance.eventService.SendMessage(
                    (EventID)WarBrokerEventID.OnCommissionCompleted,
                    config, config.BonusAmount);
            }
        }

        return newlyCompleted;
    }

    /// <summary>
    /// 检查委托是否已完成（用于UI显示）
    /// </summary>
    public bool IsCommissionCompleted(string commissionId)
    {
        return completedCommissions.Contains(commissionId);
    }

    /// <summary>
    /// 根据配置类型检查委托是否达成
    /// </summary>
    private bool CheckCommission(CommissionConfig config)
    {
        return config.Type switch
        {
            CommissionType.OccupyGrid => CheckOccupyGrid(config.TargetValue),
            CommissionType.EnemyReachGrid => CheckEnemyReachGrid(config.TargetValue, config.SecondaryTargetValue),
            CommissionType.NotOccupyGrid => CheckNotOccupyGrid(config.TargetValue),
            CommissionType.TotalCasualties => CheckTotalCasualties(config.TargetValue),
            _ => false
        };
    }

    /// <summary>
    /// OccupyGrid: 占领指定 Grid
    /// 条件：任意一条战线己方将军到达指定 Grid
    /// </summary>
    private bool CheckOccupyGrid(int targetGrid)
    {
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            if (general.GridPosition >= targetGrid)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// EnemyReachGrid: 敌方到达指定 Grid
    /// 条件：至少 N 条战线敌方到达指定 Grid
    /// </summary>
    private bool CheckEnemyReachGrid(int targetGrid, int requiredCount)
    {
        int count = 0;
        foreach (var general in campaignData.Battle.EnemyGenerals)
        {
            if (general.GridPosition <= targetGrid)
            {
                count++;
            }
        }
        return count >= requiredCount;
    }

    /// <summary>
    /// NotOccupyGrid: 未占领指定 Grid
    /// 条件：战役结束时指定 Grid 未被己方占领
    /// </summary>
    private bool CheckNotOccupyGrid(int targetGrid)
    {
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            if (general.GridPosition >= targetGrid)
            {
                return false; // 有人到达目标 Grid，不满足条件
            }
        }
        return true; // 没有人到达目标 Grid
    }

    /// <summary>
    /// TotalCasualties: 总伤亡达到数量
    /// 条件：双方总伤亡 >= 目标值
    /// </summary>
    private bool CheckTotalCasualties(int targetCasualties)
    {
        int totalCasualties = CalculateTotalCasualties();
        return totalCasualties >= targetCasualties;
    }

    /// <summary>
    /// 计算双方总伤亡
    /// </summary>
    private int CalculateTotalCasualties()
    {
        int totalCasualties = 0;

        // 计算己方伤亡（使用各将军配置的初始兵力）
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            int initialHP = general.Config?.InitialTroops ?? 16;
            int casualties = Mathf.Max(0, initialHP - general.Troops);
            totalCasualties += casualties;
        }

        // 计算敌方伤亡
        foreach (var general in campaignData.Battle.EnemyGenerals)
        {
            int initialHP = general.Config?.InitialTroops ?? 16;
            int casualties = Mathf.Max(0, initialHP - general.Troops);
            totalCasualties += casualties;
        }

        // 加上后备役消耗（使用战役配置的初始后备役）
        int initialReserves = campaignData.Config?.InitialReserves ?? 60;
        int reservesUsed = Mathf.Max(0, initialReserves - campaignData.Battle.CurrentReserves);
        totalCasualties += reservesUsed;

        return totalCasualties;
    }

    /// <summary>
    /// 实时检查委托进度（用于 UI 显示）
    /// </summary>
    public Dictionary<string, float> GetCommissionProgress()
    {
        var progress = new Dictionary<string, float>();

        foreach (var config in commissions)
        {
            progress[config.CommissionId] = GetProgress(config);
        }

        return progress;
    }

    /// <summary>
    /// 获取单个委托的进度
    /// </summary>
    private float GetProgress(CommissionConfig config)
    {
        return config.Type switch
        {
            CommissionType.OccupyGrid => GetOccupyGridProgress(config.TargetValue),
            CommissionType.EnemyReachGrid => GetEnemyReachGridProgress(config.TargetValue, config.SecondaryTargetValue),
            CommissionType.NotOccupyGrid => CheckNotOccupyGrid(config.TargetValue) ? 1f : 0f,
            CommissionType.TotalCasualties => GetTotalCasualtiesProgress(config.TargetValue),
            _ => 0f
        };
    }

    /// <summary>
    /// OccupyGrid 进度：最前线位置 / 目标位置
    /// </summary>
    private float GetOccupyGridProgress(int targetGrid)
    {
        int maxPosition = 0;
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            maxPosition = Mathf.Max(maxPosition, general.GridPosition);
        }
        return Mathf.Min(1f, (float)maxPosition / targetGrid);
    }

    /// <summary>
    /// EnemyReachGrid 进度：到达目标 Grid 的敌方数量 / 需要数量
    /// </summary>
    private float GetEnemyReachGridProgress(int targetGrid, int requiredCount)
    {
        int count = 0;
        foreach (var general in campaignData.Battle.EnemyGenerals)
        {
            if (general.GridPosition <= targetGrid)
            {
                count++;
            }
        }
        return Mathf.Min(1f, (float)count / requiredCount);
    }

    /// <summary>
    /// TotalCasualties 进度：当前伤亡 / 目标伤亡
    /// </summary>
    private float GetTotalCasualtiesProgress(int targetCasualties)
    {
        int totalCasualties = CalculateTotalCasualties();
        return Mathf.Min(1f, (float)totalCasualties / targetCasualties);
    }

    /// <summary>
    /// 获取委托任务描述（用于 UI 显示）
    /// </summary>
    public Dictionary<string, string> GetCommissionDescriptions()
    {
        var descriptions = new Dictionary<string, string>();
        foreach (var config in commissions)
        {
            descriptions[config.CommissionId] = $"{config.DisplayName}：{config.Description}（奖金 ${config.BonusAmount}）";
        }
        return descriptions;
    }
}
