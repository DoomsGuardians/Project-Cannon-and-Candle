using System;
using System.Collections.Generic;

/// <summary>战线运行时数据</summary>
[Serializable]
public class FrontlineData
{
    public FrontlinePosition Position;
    public int LinePosition; // 1-5
    public int StagnantTurns;

    // 占领状态追踪
    public int TurnsAtEnemyBase;  // 在敌方本阵的回合数
    public int TurnsAtAllyBase;   // 在己方本阵的回合数

    /// <summary>是否在敌方本阵（Grid 5）</summary>
    public bool IsAtEnemyBase => LinePosition >= 5;

    /// <summary>是否在己方本阵（Grid 1）</summary>
    public bool IsAtAllyBase => LinePosition <= 1;

    public void InitFromConfig(CampaignConfig config)
    {
        LinePosition = config.InitialFrontlinePosition;
        StagnantTurns = 0;
        TurnsAtEnemyBase = 0;
        TurnsAtAllyBase = 0;
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
    public string Description;
    public bool WasCrit;
    public bool WasFumble;

    // 位置变化（用于动画）
    public int AllyOldPosition;
    public int AllyNewPosition;
    public int EnemyOldPosition;
    public int EnemyNewPosition;
}

/// <summary>战场运行时数据</summary>
[Serializable]
public class BattleData
{
    public Dictionary<FrontlinePosition, FrontlineData> Frontlines;
    public List<GeneralData> AllyGenerals;
    public List<GeneralData> EnemyGenerals;
    public int CurrentReserves;  // 玩家后备役
    public int EnemyReserves;    // 敌方后备役（Victor）

    public void InitFromConfig(CampaignConfig campaignConfig)
    {
        CurrentReserves = campaignConfig.InitialReserves;
        EnemyReserves = campaignConfig.InitialReserves; // 敌方初始后备役与玩家相同

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
                    general.InitFromConfig(configItem);
                    general.Position = assignment.Position;
                    general.GridPosition = 1;  // 己方大本营
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
                    general.InitFromConfig(configItem);
                    general.Position = assignment.Position;
                    general.GridPosition = 5;  // 敌方大本营
                    EnemyGenerals.Add(general);
                }
            }
        }
    }
}
