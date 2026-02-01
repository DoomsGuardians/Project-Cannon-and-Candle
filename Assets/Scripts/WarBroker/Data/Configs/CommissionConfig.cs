using UnityEngine;

/// <summary>
/// 委托任务配置 ScriptableObject
/// 定义单个委托任务的所有属性
/// </summary>
[CreateAssetMenu(fileName = "Commission_XXX", menuName = "WarBroker/CommissionConfig")]
public class CommissionConfig : ScriptableObject
{
    [Header("===== 基础信息 =====")]
    [Tooltip("唯一标识，如 WinWar、ShortCountry")]
    public string CommissionId;

    [Tooltip("显示名称，如 斩首胜利")]
    public string DisplayName;

    [TextArea(2, 4)]
    [Tooltip("详细描述/文案")]
    public string Description;

    [Header("===== 委托人信息 =====")]
    [Tooltip("委托人名称")]
    public string CommissionerName;

    [Tooltip("委托人头像")]
    public Sprite CommissionerAvatar;

    [TextArea(2, 4)]
    [Tooltip("委托人台词/引言")]
    public string CommissionerQuote;

    [Header("===== 奖励 =====")]
    [Tooltip("奖金金额")]
    public float BonusAmount;

    [Header("===== 条件类型 =====")]
    [Tooltip("委托类型（决定检查逻辑）")]
    public CommissionType Type;

    [Header("===== 条件参数 =====")]
    [Tooltip("目标值（如 Grid 位置、伤亡数量等）")]
    public int TargetValue;

    [Tooltip("次要目标值（如 EnemyReachGrid 需要的敌方数量）")]
    public int SecondaryTargetValue;

    [Header("===== Naninovel 集成 (TODO) =====")]
    [Tooltip("游戏开始时触发的对话脚本名")]
    public string NaninovelScriptName;
}
