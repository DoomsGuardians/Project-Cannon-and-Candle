using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>格子归属</summary>
public enum GridOwner
{
    Neutral,  // 中立
    Ally,     // 己方
    Enemy     // 敌方
}

/// <summary>战线运行时数据</summary>
[Serializable]
public class FrontlineData
{
    public FrontlinePosition Position;
    public GridOwner[] GridOwners;  // 5个格子的归属 [0]=Grid1, [4]=Grid5
    public int StagnantTurns;

    // 占领状态追踪
    public int TurnsAtEnemyBase;  // 在敌方本阵的回合数
    public int TurnsAtAllyBase;   // 在己方本阵的回合数

    /// <summary>计算属性：战线位置（从格子归属推算）</summary>
    public float LinePosition => CalculateLinePosition();

    /// <summary>是否在敌方本阵（己方占领Grid 5）</summary>
    public bool IsAtEnemyBase => GridOwners != null && GridOwners[4] == GridOwner.Ally;

    /// <summary>是否在己方本阵（敌方占领Grid 1）</summary>
    public bool IsAtAllyBase => GridOwners != null && GridOwners[0] == GridOwner.Enemy;

    public void InitFromConfig(CampaignConfig config)
    {
        // 初始化格子归属：Grid 1-2 己方，Grid 3 中立，Grid 4-5 敌方
        GridOwners = new GridOwner[5];
        GridOwners[0] = GridOwner.Ally;    // Grid 1
        GridOwners[1] = GridOwner.Ally;    // Grid 2
        GridOwners[2] = GridOwner.Neutral; // Grid 3
        GridOwners[3] = GridOwner.Enemy;   // Grid 4
        GridOwners[4] = GridOwner.Enemy;   // Grid 5
        StagnantTurns = 0;
        TurnsAtEnemyBase = 0;
        TurnsAtAllyBase = 0;
    }

    /// <summary>计算战线位置（己方占领区与敌方占领区的边界）</summary>
    private float CalculateLinePosition()
    {
        if (GridOwners == null) return 3f;

        // 找到最后一个己方格子（Grid编号）和第一个敌方格子（Grid编号）
        int lastAllyGrid = 0;   // 最后一个己方占领的Grid编号(1-5)
        int firstEnemyGrid = 6; // 第一个敌方占领的Grid编号(1-5)

        for (int i = 0; i < 5; i++)
        {
            int gridNumber = i + 1; // 转换为Grid编号(1-5)
            if (GridOwners[i] == GridOwner.Ally)
                lastAllyGrid = gridNumber;
            if (GridOwners[i] == GridOwner.Enemy && gridNumber < firstEnemyGrid)
                firstEnemyGrid = gridNumber;
        }

        // 如果没有己方占领区，返回1
        if (lastAllyGrid == 0) return 1f;
        // 如果没有敌方占领区，返回5
        if (firstEnemyGrid == 6) return 5f;

        // 战线位置 = 己方最后占领格和敌方第一占领格的中点
        return (lastAllyGrid + firstEnemyGrid) / 2f;
    }
}

/// <summary>战斗结果</summary>
[Serializable]
public class BattleResult
{
    public FrontlinePosition Position;
    public OrderType AllyOrder;
    public OrderType EnemyOrder;
    public int LineMovement;  // 保留用于UI显示（废弃，改用领土变化）
    public int AllyTroopChange;
    public int EnemyTroopChange;
    public int AllyBaseRecovery;   // 己方大本营恢复量（用于单独播放动画）
    public int EnemyBaseRecovery;  // 敌方大本营恢复量
    public string Description;
    public bool WasCrit;
    public bool WasFumble;

    // 位置变化（用于动画）
    public int AllyOldPosition;
    public int AllyNewPosition;
    public int EnemyOldPosition;
    public int EnemyNewPosition;

    // 新增：本次战斗的领土变化
    public int? AllyCapturedGrid;   // 己方占领的格子 (1-5)，null表示无变化
    public int? EnemyCapturedGrid;  // 敌方占领的格子 (1-5)，null表示无变化
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
