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

    public int Troops;
    public int Trust;
    public int Morale;

    public List<SkillConfigItem> Skills;

    public OrderType? AssignedOrder;

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
        return Troops * 0.4f + Trust * 0.3f + Morale * 0.3f;
    }

    public GeneralStatus GetStatus(GameBalanceConfig balance)
    {
        if (Troops < balance.RoutTroopThreshold) return GeneralStatus.Routed;
        float score = CalculateCompositeScore();
        if (score < balance.RoutScoreThreshold) return GeneralStatus.Routed;
        if (score < 50) return GeneralStatus.Critical;
        if (score < 70) return GeneralStatus.Wounded;
        return GeneralStatus.Healthy;
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
