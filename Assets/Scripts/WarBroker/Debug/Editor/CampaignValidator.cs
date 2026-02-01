#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 战役配置验证器
/// </summary>
public static class CampaignValidator
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();

        public bool IsValid => Errors.Count == 0;
        public bool HasWarnings => Warnings.Count > 0;

        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);

        public void Clear()
        {
            Errors.Clear();
            Warnings.Clear();
        }
    }

    /// <summary>
    /// 验证单个战役配置
    /// </summary>
    public static ValidationResult Validate(CampaignConfig config, CampaignConfig[] allConfigs = null)
    {
        var result = new ValidationResult();

        if (config == null)
        {
            result.AddError("配置为空");
            return result;
        }

        // 基础信息验证
        ValidateBasicInfo(config, result);

        // ID 唯一性验证
        if (allConfigs != null)
        {
            ValidateIdUniqueness(config, allConfigs, result);
        }

        // 数值范围验证
        ValidateNumericRanges(config, result);

        // 将军配置验证
        ValidateGeneralConfig(config, result);

        // 战线分配验证
        ValidateFrontlineAssignments(config, result);

        // 随机事件验证
        ValidateRandomEvents(config, result);

        // 委托任务验证
        ValidateCommissions(config, result);

        return result;
    }

    /// <summary>
    /// 验证基础信息
    /// </summary>
    private static void ValidateBasicInfo(CampaignConfig config, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(config.CampaignId))
        {
            result.AddError("战役 ID 不能为空");
        }
        else if (config.CampaignId.Contains(" "))
        {
            result.AddWarning("战役 ID 包含空格，建议使用下划线");
        }

        if (string.IsNullOrWhiteSpace(config.CampaignName))
        {
            result.AddWarning("战役名称为空");
        }

        if (string.IsNullOrWhiteSpace(config.Description))
        {
            result.AddWarning("战役描述为空");
        }
    }

    /// <summary>
    /// 验证 ID 唯一性
    /// </summary>
    private static void ValidateIdUniqueness(CampaignConfig config, CampaignConfig[] allConfigs, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(config.CampaignId)) return;

        int count = allConfigs.Count(c => c != null && c.CampaignId == config.CampaignId);
        if (count > 1)
        {
            result.AddError($"战役 ID '{config.CampaignId}' 重复出现 {count} 次");
        }
    }

    /// <summary>
    /// 验证数值范围
    /// </summary>
    private static void ValidateNumericRanges(CampaignConfig config, ValidationResult result)
    {
        // 回合数
        if (config.MaxTurns < 6)
        {
            result.AddError($"最大回合数 ({config.MaxTurns}) 不能小于 6");
        }
        else if (config.MaxTurns > 60)
        {
            result.AddError($"最大回合数 ({config.MaxTurns}) 不能大于 60");
        }

        // 初始现金
        if (config.InitialCash < 0)
        {
            result.AddError("初始现金不能为负数");
        }
        else if (config.InitialCash == 0)
        {
            result.AddWarning("初始现金为 0");
        }

        // 库存
        if (config.InitialAtkInventory < 0 || config.InitialDefInventory < 0 || config.InitialRetInventory < 0)
        {
            result.AddError("初始库存不能为负数");
        }

        // 后备役
        if (config.InitialReserves < 0)
        {
            result.AddError("初始后备役不能为负数");
        }
        else if (config.InitialReserves > 100)
        {
            result.AddWarning($"初始后备役 ({config.InitialReserves}) 超过 100");
        }

        // 战线位置
        if (config.InitialFrontlinePosition < 1 || config.InitialFrontlinePosition > 5)
        {
            result.AddError($"初始战线位置 ({config.InitialFrontlinePosition}) 必须在 1-5 之间");
        }

        // 维克多设置
        if (config.VictorInitialCash < 0)
        {
            result.AddError("维克多初始现金不能为负数");
        }

        if (config.VictorDifficulty < 0 || config.VictorDifficulty > 1)
        {
            result.AddError($"维克多 AI 难度 ({config.VictorDifficulty}) 必须在 0-1 之间");
        }
    }

    /// <summary>
    /// 验证将军配置引用
    /// </summary>
    private static void ValidateGeneralConfig(CampaignConfig config, ValidationResult result)
    {
        if (config.GeneralConfig == null)
        {
            result.AddError("未指定将军配置 (GeneralConfig)");
        }
    }

    /// <summary>
    /// 验证战线分配
    /// </summary>
    private static void ValidateFrontlineAssignments(CampaignConfig config, ValidationResult result)
    {
        if (config.GeneralConfig == null) return;

        // 验证己方战线分配
        ValidateSideFrontlineAssignments(config.AllyFrontlineAssignments, config.GeneralConfig.AllyGenerals, "己方", result);

        // 验证敌方战线分配
        ValidateSideFrontlineAssignments(config.EnemyFrontlineAssignments, config.GeneralConfig.EnemyGenerals, "敌方", result);
    }

    private static void ValidateSideFrontlineAssignments(FrontlineAssignment[] assignments, GeneralConfigItem[] generals, string side, ValidationResult result)
    {
        if (assignments == null || assignments.Length == 0)
        {
            result.AddWarning($"{side}战线分配为空");
            return;
        }

        if (assignments.Length != 3)
        {
            result.AddWarning($"{side}战线分配数量 ({assignments.Length}) 不等于 3");
        }

        // 检查位置重复
        var positions = new HashSet<FrontlinePosition>();
        foreach (var assignment in assignments)
        {
            if (!positions.Add(assignment.Position))
            {
                result.AddError($"{side}战线位置 {assignment.Position} 重复分配");
            }
        }

        // 检查将军 ID 有效性
        if (generals != null)
        {
            var validIds = new HashSet<string>(generals.Where(g => g != null).Select(g => g.GeneralId));
            foreach (var assignment in assignments)
            {
                if (string.IsNullOrWhiteSpace(assignment.GeneralId))
                {
                    result.AddWarning($"{side}战线 {assignment.Position} 未分配将军");
                }
                else if (!validIds.Contains(assignment.GeneralId))
                {
                    result.AddError($"{side}战线 {assignment.Position} 的将军 ID '{assignment.GeneralId}' 在 GeneralConfig 中不存在");
                }
            }
        }

        // 检查将军重复分配
        var assignedGenerals = new HashSet<string>();
        foreach (var assignment in assignments)
        {
            if (!string.IsNullOrWhiteSpace(assignment.GeneralId))
            {
                if (!assignedGenerals.Add(assignment.GeneralId))
                {
                    result.AddError($"{side}将军 '{assignment.GeneralId}' 被分配到多个战线");
                }
            }
        }
    }

    /// <summary>
    /// 验证随机事件
    /// </summary>
    private static void ValidateRandomEvents(CampaignConfig config, ValidationResult result)
    {
        if (config.AvailableEvents == null || config.AvailableEvents.Length == 0)
        {
            result.AddWarning("未配置随机事件");
            return;
        }

        var eventIds = new HashSet<string>();
        foreach (var evt in config.AvailableEvents)
        {
            if (evt == null) continue;

            if (string.IsNullOrWhiteSpace(evt.EventId))
            {
                result.AddError("存在事件 ID 为空的随机事件");
            }
            else if (!eventIds.Add(evt.EventId))
            {
                result.AddError($"随机事件 ID '{evt.EventId}' 重复");
            }

            if (string.IsNullOrWhiteSpace(evt.EventName))
            {
                result.AddWarning($"随机事件 '{evt.EventId}' 名称为空");
            }

            if (evt.Duration <= 0)
            {
                result.AddWarning($"随机事件 '{evt.EventId}' 持续回合数 ({evt.Duration}) 应大于 0");
            }
        }
    }

    /// <summary>
    /// 验证委托任务
    /// </summary>
    private static void ValidateCommissions(CampaignConfig config, ValidationResult result)
    {
        if (config.Commissions == null || config.Commissions.Count == 0)
        {
            result.AddWarning("未配置委托任务");
            return;
        }

        var commissionIds = new HashSet<string>();
        foreach (var commission in config.Commissions)
        {
            if (commission == null)
            {
                result.AddWarning("委托任务列表中存在空引用");
                continue;
            }

            if (string.IsNullOrWhiteSpace(commission.CommissionId))
            {
                result.AddError("存在委托任务 ID 为空");
            }
            else if (!commissionIds.Add(commission.CommissionId))
            {
                result.AddError($"委托任务 ID '{commission.CommissionId}' 重复");
            }

            if (commission.BonusAmount <= 0)
            {
                result.AddWarning($"委托任务 '{commission.CommissionId}' 奖金 ({commission.BonusAmount}) 应大于 0");
            }
        }
    }

    /// <summary>
    /// 获取验证状态摘要
    /// </summary>
    public static string GetStatusSummary(ValidationResult result)
    {
        if (result == null) return "未验证";

        if (result.IsValid && !result.HasWarnings)
        {
            return "✓ 配置有效";
        }
        else if (result.IsValid && result.HasWarnings)
        {
            return $"✓ 配置有效 | ⚠ {result.Warnings.Count} 个警告";
        }
        else
        {
            return $"✗ {result.Errors.Count} 个错误" + (result.HasWarnings ? $" | ⚠ {result.Warnings.Count} 个警告" : "");
        }
    }
}
#endif
