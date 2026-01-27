using System;
using UnityEngine;

/// <summary>
/// 全局游戏平衡参数配置
/// </summary>
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "WarBroker/GameBalanceConfig")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("===== 市场参数 =====")]

    [Tooltip("手续费率")]
    [Range(0f, 0.1f)]
    public float CommissionRate = 0.02f;

    [Tooltip("价格冲击率 (每张)")]
    [Range(0f, 0.1f)]
    public float PriceImpactRate = 0.02f;

    [Tooltip("仓储费 (每张/回合)")]
    public float StorageCostPerUnit = 3f;

    [Tooltip("价格随机波动范围")]
    [Range(0f, 0.2f)]
    public float PriceRandomRange = 0.05f;

    [Header("===== 银行参数 =====")]

    [Tooltip("银行利率 (每回合)")]
    [Range(0f, 0.2f)]
    public float BankInterestRate = 0.05f;

    [Tooltip("借款额度系数 (净资产倍数)")]
    [Range(1f, 3f)]
    public float LoanRatio = 1.5f;

    [Header("===== 期货参数 =====")]

    [Tooltip("期货保证金率")]
    [Range(0.1f, 0.5f)]
    public float FuturesMarginRate = 0.2f;

    [Tooltip("强平线 (亏损百分比)")]
    [Range(0.5f, 0.95f)]
    public float ForceLiquidationRate = 0.8f;

    [Tooltip("期货最长期限 (回合)")]
    [Range(1, 5)]
    public int MaxFuturesDuration = 3;

    [Header("===== 战斗参数 =====")]

    [Tooltip("随机修正最小值")]
    [Range(0.5f, 1f)]
    public float RandomModifierMin = 0.75f;

    [Tooltip("随机修正最大值")]
    [Range(1f, 1.5f)]
    public float RandomModifierMax = 1.25f;

    [Tooltip("暴击概率")]
    [Range(0f, 0.2f)]
    public float CritChance = 0.05f;

    [Tooltip("暴击倍率")]
    [Range(1f, 3f)]
    public float CritMultiplier = 1.5f;

    [Tooltip("失误概率")]
    [Range(0f, 0.2f)]
    public float FumbleChance = 0.05f;

    [Tooltip("失误倍率")]
    [Range(0f, 1f)]
    public float FumbleMultiplier = 0.5f;

    [Header("===== 将军参数 =====")]

    [Tooltip("基础补员 (每回合)")]
    [Range(0, 30)]
    public int BaseReinforcement = 10;

    [Tooltip("溃败兵力阈值")]
    [Range(0, 50)]
    public int RoutTroopThreshold = 20;

    [Tooltip("溃败综合评分阈值")]
    [Range(0, 50)]
    public int RoutScoreThreshold = 30;

    [Tooltip("重整所需回合")]
    [Range(1, 5)]
    public int ReorganizeTurns = 3;

    [Tooltip("抗命起始信任度阈值")]
    [Range(0, 100)]
    public int DisobeyTrustThreshold = 50;

    [Tooltip("低信任度抗命概率 (信任度30-49)")]
    [Range(0f, 1f)]
    public float DisobeyChanceLow = 0.2f;

    [Tooltip("极低信任度抗命概率 (信任度0-29)")]
    [Range(0f, 1f)]
    public float DisobeyChanceVeryLow = 0.5f;

    [Header("===== 战线联动参数 =====")]

    [Tooltip("侧翼支援战斗力加成")]
    [Range(0f, 0.5f)]
    public float FlankSupportBonus = 0.1f;

    [Tooltip("侧翼威胁战斗力惩罚")]
    [Range(0f, 0.5f)]
    public float FlankThreatPenalty = 0.1f;

    [Tooltip("侧翼威胁士气惩罚")]
    [Range(0, 20)]
    public int FlankThreatMoralePenalty = 5;

    [Tooltip("半包围战斗力加成")]
    [Range(0f, 0.5f)]
    public float SurroundBonus = 0.25f;

    [Tooltip("被包围士气惩罚")]
    [Range(0, 30)]
    public int SurroundedMoralePenalty = 15;

    [Header("===== 审计参数 =====")]

    [Tooltip("军需总部补足指令审计值")]
    [Range(0, 50)]
    public int AuditSupplyShortage = 30;

    [Tooltip("将军溃败审计值")]
    [Range(0, 50)]
    public int AuditGeneralRouted = 20;

    [Tooltip("审计失败阈值")]
    [Range(50, 200)]
    public int AuditFailureThreshold = 100;

    [Header("===== 随机事件参数 =====")]

    [Tooltip("随机事件触发概率")]
    [Range(0f, 1f)]
    public float RandomEventChance = 0.4f;
}
