using UnityEngine;

/// <summary>
/// 观战模式配置：双方维克多对战的参数设置
/// </summary>
[CreateAssetMenu(fileName = "SpectatorModeConfig", menuName = "WarBroker/SpectatorModeConfig")]
public class SpectatorModeConfig : ScriptableObject
{
    [Header("=== 双方配置 ===")]
    [Tooltip("己方维克多性格配置")]
    public VictorProfile AllyVictorProfile;

    [Tooltip("敌方维克多性格配置")]
    public VictorProfile EnemyVictorProfile;

    [Header("=== 对战设置 ===")]
    [Tooltip("最大回合数")]
    public int MaxTurns = 30;

    [Tooltip("回合播放速度 (1.0 = 正常)")]
    [Range(0.1f, 5.0f)]
    public float TurnSpeed = 1.0f;

    [Tooltip("自动推进回合")]
    public bool AutoAdvance = true;

    [Tooltip("显示战斗动画")]
    public bool ShowBattleAnimations = true;

    [Tooltip("回合间隔时间（秒）")]
    [Range(0.5f, 5.0f)]
    public float TurnInterval = 1.5f;

    [Header("=== 初始资源 ===")]
    [Tooltip("己方初始现金")]
    public int AllyStartingCash = 1000;

    [Tooltip("敌方初始现金")]
    public int EnemyStartingCash = 1000;

    [Header("=== 战场配置 ===")]
    [Tooltip("使用的战役配置（用于初始化战场和将军）")]
    public CampaignConfig BaseCampaignConfig;
}
