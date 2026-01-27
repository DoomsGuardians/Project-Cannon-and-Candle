using System;
using UnityEngine;

/// <summary>
/// 技能配置
/// </summary>
[Serializable]
public class SkillConfigItem
{
    public string SkillId;
    public string SkillName;

    [TextArea(2, 4)]
    public string Description;

    [Tooltip("所属性格类型")]
    public GeneralPersonality Personality;

    [Header("触发条件")]
    public OrderType? TriggerOrder;

    [Tooltip("兵力阈值 (0=不检查)")]
    public int TroopThreshold;

    [Tooltip("士气阈值 (0=不检查)")]
    public int MoraleThreshold;

    [Tooltip("战线位置阈值 (0=不检查)")]
    public int FrontlineThreshold;

    [Tooltip("是否需要连续回合")]
    public bool RequireConsecutive;

    [Header("效果数值")]
    [Tooltip("额外战线移动")]
    public int BonusLineMovement;

    [Tooltip("战斗力加成%")]
    public float CombatBonus;

    [Tooltip("己方额外兵力变化")]
    public int AllyTroopChange;

    [Tooltip("敌方额外兵力变化")]
    public int EnemyTroopChange;

    [Tooltip("抗命改为此指令 (null=不抗命)")]
    public OrderType? DisobeyToOrder;

    [Tooltip("抗命概率")]
    [Range(0f, 1f)]
    public float DisobeyChance;
}

/// <summary>
/// 技能配置表
/// </summary>
[CreateAssetMenu(fileName = "SkillConfig", menuName = "WarBroker/SkillConfig")]
public class SkillConfig : ScriptableObject
{
    public SkillConfigItem[] Skills;

    public SkillConfigItem GetSkill(string skillId)
    {
        foreach (var skill in Skills)
        {
            if (skill.SkillId == skillId) return skill;
        }
        return null;
    }

    public SkillConfigItem[] GetSkillsByPersonality(GeneralPersonality personality)
    {
        return Array.FindAll(Skills, s => s.Personality == personality);
    }
}
