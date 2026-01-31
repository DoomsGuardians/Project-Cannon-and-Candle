using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>将军运行时数据</summary>
[Serializable]
public class GeneralData
{
    public GeneralConfigItem Config { get; private set; }

    public string GeneralId => Config.GeneralId;
    public string Name => Config.Name;
    public GeneralPersonality Personality => Config.Personality;

    public FrontlinePosition Position;
    public int GridPosition = 3;  // 战线位置 (1-5)，初始为中间

    public int Troops;
    public int Trust;
    public int Morale;

    public List<SkillConfigItem> Skills;

    public OrderType? AssignedOrder;

    // 意图系统
    public OrderType? DefaultIntent;  // AI生成的默认意图
    public OrderType? FinalIntent;    // 最终执行的意图
    public IntentSource IntentSource; // 意图来源

    public int ReorganizeTurns;

    public OrderType? LastOrder;
    public int ConsecutiveOrderCount;

    public void InitFromConfig(GeneralConfigItem config, SkillConfig skillConfig)
    {
        Config = config;
        Troops = config.InitialTroops;
        Trust = config.InitialTrust;
        Morale = config.InitialMorale;

        Skills = new List<SkillConfigItem>();
        if (config.SkillIds != null)
        {
            foreach (var skillId in config.SkillIds)
            {
                var skill = skillConfig.GetSkill(skillId);
                if (skill != null) Skills.Add(skill);
            }
        }
    }

    public float CalculateCompositeScore()
    {
        // Troops 范围从 0-100 变为 0-20，需要乘以 5 归一化到 0-100 范围
        return (Troops * 5) * 0.4f + Trust * 0.3f + Morale * 0.3f;
    }

    /// <summary>
    /// 获取将军状态 (GDD v6.0: 纯HP阈值判定)
    /// HP=0 溃败，HP≤5 危急，HP≤10 受伤，HP≤15 健康，HP>15 满编
    /// </summary>
    public GeneralStatus GetStatus(GameBalanceConfig balance)
    {
        // GDD v6.0: 纯HP阈值判定，不再使用综合评分
        if (Troops <= 0) return GeneralStatus.Routed;
        if (Troops <= 5) return GeneralStatus.Critical;
        if (Troops <= 10) return GeneralStatus.Wounded;
        if (Troops <= 15) return GeneralStatus.Healthy;
        return GeneralStatus.FullStrength;
    }

    public float CalculateBid(OrderType orderType, float marketPrice, GameBalanceConfig balance)
    {
        float baseValue = marketPrice * 0.3f;

        float personalityMod = orderType switch
        {
            OrderType.ATK => Config.AtkBidModifier,
            OrderType.DEF => Config.DefBidModifier,
            OrderType.RET => Config.RetBidModifier,
            _ => 1f
        };

        float statusMod = GetStatusBidModifier(orderType, balance);
        float trustMod = 0.5f + Trust / 100f;

        return baseValue * personalityMod * statusMod * trustMod;
    }

    private float GetStatusBidModifier(OrderType orderType, GameBalanceConfig balance)
    {
        var status = GetStatus(balance);
        return (status, orderType) switch
        {
            (GeneralStatus.Healthy, _) => 1.0f,
            (GeneralStatus.Wounded, OrderType.ATK) => 0.7f,
            (GeneralStatus.Wounded, OrderType.DEF) => 1.5f,
            (GeneralStatus.Wounded, OrderType.RET) => 1.3f,
            (GeneralStatus.Critical, OrderType.ATK) => 0.3f,
            (GeneralStatus.Critical, OrderType.DEF) => 2.5f,
            (GeneralStatus.Critical, OrderType.RET) => 2.0f,
            _ => 1.0f
        };
    }
}
