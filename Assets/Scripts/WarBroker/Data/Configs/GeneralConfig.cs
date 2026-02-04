using System;
using UnityEngine;

/// <summary>
/// 将军士兵配置
/// 定义将军麾下20个士兵的兵种分配
/// </summary>
[Serializable]
public class GeneralSoldierConfig
{
    [Tooltip("20个士兵的兵种配置")]
    public SoldierType[] soldierTypes = new SoldierType[20];

    public GeneralSoldierConfig()
    {
        // 默认全部为长枪兵
        soldierTypes = new SoldierType[20];
        for (int i = 0; i < 20; i++)
        {
            soldierTypes[i] = SoldierType.Pikeman;
        }
    }
}

/// <summary>
/// 将军配置
/// </summary>
[Serializable]
public class GeneralConfigItem
{
    [Header("基础信息")]
    public string GeneralId;
    public string Name;

    [TextArea(2, 3)]
    public string Biography;

    public Sprite Portrait;

    [Header("属性")]
    public GeneralPersonality Personality;

    [Tooltip("初始兵力")]
    [Range(0, 20)]
    public int InitialTroops = 16;

    [Tooltip("初始信任度")]
    [Range(0, 100)]
    public int InitialTrust = 50;

    [Tooltip("初始士气")]
    [Range(0, 100)]
    public int InitialMorale = 60;

    [Header("出价系数")]
    [Tooltip("ATK出价系数")]
    public float AtkBidModifier = 1f;

    [Tooltip("DEF出价系数")]
    public float DefBidModifier = 1f;

    [Tooltip("RET出价系数")]
    public float RetBidModifier = 1f;

    [Header("士兵配置")]
    [Tooltip("将军麾下士兵的兵种配置")]
    public GeneralSoldierConfig soldierConfig = new GeneralSoldierConfig();
}

/// <summary>
/// 将军配置表
/// </summary>
[CreateAssetMenu(fileName = "GeneralConfig", menuName = "WarBroker/GeneralConfig")]
public class GeneralConfig : ScriptableObject
{
    [Header("己方将军")]
    public GeneralConfigItem[] AllyGenerals;

    [Header("敌方将军")]
    public GeneralConfigItem[] EnemyGenerals;

    public GeneralConfigItem GetGeneral(string generalId)
    {
        foreach (var g in AllyGenerals)
        {
            if (g.GeneralId == generalId) return g;
        }
        foreach (var g in EnemyGenerals)
        {
            if (g.GeneralId == generalId) return g;
        }
        return null;
    }
}
