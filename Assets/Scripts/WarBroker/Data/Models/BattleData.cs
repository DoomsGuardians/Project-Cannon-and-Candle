using System;
using System.Collections.Generic;

/// <summary>战线运行时数据</summary>
[Serializable]
public class FrontlineData
{
    public FrontlinePosition Position;
    public int LinePosition; // 1-5
    public int StagnantTurns;

    public void InitFromConfig(CampaignConfig config)
    {
        LinePosition = config.InitialFrontlinePosition;
        StagnantTurns = 0;
    }
}

/// <summary>战斗结果</summary>
[Serializable]
public class BattleResult
{
    public FrontlinePosition Position;
    public OrderType AllyOrder;
    public OrderType EnemyOrder;
    public int LineMovement;
    public int AllyTroopChange;
    public int EnemyTroopChange;
    public bool SkillTriggered;
    public string SkillName;
    public string Description;
    public bool WasCrit;
    public bool WasFumble;
}

/// <summary>战场运行时数据</summary>
[Serializable]
public class BattleData
{
    public Dictionary<FrontlinePosition, FrontlineData> Frontlines;
    public List<GeneralData> AllyGenerals;
    public List<GeneralData> EnemyGenerals;

    public void InitFromConfig(CampaignConfig campaignConfig, SkillConfig skillConfig)
    {
        Frontlines = new Dictionary<FrontlinePosition, FrontlineData>();
        foreach (FrontlinePosition pos in Enum.GetValues(typeof(FrontlinePosition)))
        {
            var frontline = new FrontlineData { Position = pos };
            frontline.InitFromConfig(campaignConfig);
            Frontlines[pos] = frontline;
        }

        AllyGenerals = new List<GeneralData>();
        if (campaignConfig.AllyFrontlineAssignments != null)
        {
            foreach (var assignment in campaignConfig.AllyFrontlineAssignments)
            {
                var configItem = campaignConfig.GeneralConfig.GetGeneral(assignment.GeneralId);
                if (configItem != null)
                {
                    var general = new GeneralData();
                    general.InitFromConfig(configItem, skillConfig);
                    general.Position = assignment.Position;
                    AllyGenerals.Add(general);
                }
            }
        }

        EnemyGenerals = new List<GeneralData>();
        if (campaignConfig.EnemyFrontlineAssignments != null)
        {
            foreach (var assignment in campaignConfig.EnemyFrontlineAssignments)
            {
                var configItem = campaignConfig.GeneralConfig.GetGeneral(assignment.GeneralId);
                if (configItem != null)
                {
                    var general = new GeneralData();
                    general.InitFromConfig(configItem, skillConfig);
                    general.Position = assignment.Position;
                    EnemyGenerals.Add(general);
                }
            }
        }
    }
}
