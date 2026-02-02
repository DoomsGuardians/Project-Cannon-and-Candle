using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战役配置
/// </summary>
[CreateAssetMenu(fileName = "Campaign_XXX", menuName = "WarBroker/CampaignConfig")]
public class CampaignConfig : ScriptableObject
{
    [Header("===== 基础信息 =====")]
    public string CampaignId;
    public string CampaignName;

    [TextArea(3, 5)]
    public string Description;

    [Header("===== 回合设置 =====")]
    [Tooltip("最大回合数")]
    [Range(6, 60)]
    public int MaxTurns = 20;

    [Header("===== 玩家初始状态 =====")]
    [Tooltip("初始现金")]
    public float InitialCash = 500f;

    [Tooltip("初始ATK库存")]
    public int InitialAtkInventory = 2;

    [Tooltip("初始DEF库存")]
    public int InitialDefInventory = 2;

    [Tooltip("初始RET库存")]
    public int InitialRetInventory = 2;

    [Header("===== 后备役设置 =====")]
    [Tooltip("初始后备役兵力")]
    [Range(0, 100)]
    public int InitialReserves = 60;

    [Header("===== 战场设置 =====")]
    [Tooltip("初始战线位置 (1-5)")]
    [Range(1, 5)]
    public int InitialFrontlinePosition = 3;

    [Header("===== 将军配置引用 =====")]
    [Tooltip("本战役使用的将军配置")]
    public GeneralConfig GeneralConfig;

    [Tooltip("将军战线分配 (左翼/中军/右翼)")]
    public FrontlineAssignment[] AllyFrontlineAssignments;

    public FrontlineAssignment[] EnemyFrontlineAssignments;

    [Header("===== 维克多设置 =====")]
    [Tooltip("维克多初始现金")]
    public float VictorInitialCash = 500f;

    [Tooltip("维克多性格配置（不设置则使用默认 VictorProfile_Default）")]
    public VictorProfile VictorProfile;

    [Header("===== 可用随机事件 =====")]
    public RandomEventConfig[] AvailableEvents;

    [Header("===== 委托任务 =====")]
    [Tooltip("该战役可用的委托任务列表")]
    public List<CommissionConfig> Commissions;
}

/// <summary>
/// 战线分配
/// </summary>
[Serializable]
public class FrontlineAssignment
{
    public FrontlinePosition Position;
    public string GeneralId;
}

/// <summary>
/// 随机事件配置
/// </summary>
[Serializable]
public class RandomEventConfig
{
    public string EventId;
    public string EventName;

    [TextArea(2, 3)]
    public string Description;

    [Header("效果")]
    [Tooltip("产能修正%")]
    public float ProductionModifier;

    [Tooltip("需求修正 (ATK)")]
    public float AtkDemandModifier;

    [Tooltip("需求修正 (DEF)")]
    public float DefDemandModifier;

    [Tooltip("需求修正 (RET)")]
    public float RetDemandModifier;

    [Tooltip("所有将军兵力变化")]
    public int AllTroopChange;

    [Tooltip("随机将军信任度变化")]
    public int RandomTrustChange;

    [Tooltip("持续回合数")]
    public int Duration = 1;
}
