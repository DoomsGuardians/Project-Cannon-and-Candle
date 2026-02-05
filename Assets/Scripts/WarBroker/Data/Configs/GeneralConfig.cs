using System;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 将军士兵配置
/// 定义将军麾下20个士兵的兵种分配（4行5列）
/// </summary>
[Serializable]
public class GeneralSoldierConfig
{
    [Tooltip("第1排士兵（5个）")]
    [LabelText("第1排")]
    public SoldierType[] row1 = new SoldierType[5];

    [Tooltip("第2排士兵（5个）")]
    [LabelText("第2排")]
    public SoldierType[] row2 = new SoldierType[5];

    [Tooltip("第3排士兵（5个）")]
    [LabelText("第3排")]
    public SoldierType[] row3 = new SoldierType[5];

    [Tooltip("第4排士兵（5个）")]
    [LabelText("第4排")]
    public SoldierType[] row4 = new SoldierType[5];

    /// <summary>
    /// 获取所有士兵类型（按行优先顺序，共20个）
    /// </summary>
    public SoldierType[] soldierTypes
    {
        get
        {
            var result = new SoldierType[20];
            for (int i = 0; i < 5; i++)
            {
                result[i] = row1[i];
                result[i + 5] = row2[i];
                result[i + 10] = row3[i];
                result[i + 15] = row4[i];
            }
            return result;
        }
    }

    public GeneralSoldierConfig()
    {
        // 默认全部为长枪兵
        row1 = new SoldierType[5];
        row2 = new SoldierType[5];
        row3 = new SoldierType[5];
        row4 = new SoldierType[5];
        for (int i = 0; i < 5; i++)
        {
            row1[i] = SoldierType.Pikeman;
            row2[i] = SoldierType.Pikeman;
            row3[i] = SoldierType.Pikeman;
            row4[i] = SoldierType.Pikeman;
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
