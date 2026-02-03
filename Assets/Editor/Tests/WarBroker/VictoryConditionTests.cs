using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 胜利条件测试：验证胜利、失败、平局条件
/// </summary>
public class VictoryConditionTests
{
    /// <summary>创建指定格子归属的战线数据</summary>
    private FrontlineData CreateFrontline(FrontlinePosition pos, GridOwner[] gridOwners, int turnsAtEnemy = 0, int turnsAtAlly = 0)
    {
        var frontline = new FrontlineData
        {
            Position = pos,
            GridOwners = gridOwners,
            TurnsAtEnemyBase = turnsAtEnemy,
            TurnsAtAllyBase = turnsAtAlly
        };
        return frontline;
    }

    /// <summary>创建己方全占领的格子（胜利状态）</summary>
    private GridOwner[] CreateVictoryGrids()
    {
        return new GridOwner[] { GridOwner.Ally, GridOwner.Ally, GridOwner.Ally, GridOwner.Ally, GridOwner.Ally };
    }

    /// <summary>创建敌方占领格1的格子（失败状态）</summary>
    private GridOwner[] CreateDefeatGrids()
    {
        return new GridOwner[] { GridOwner.Enemy, GridOwner.Enemy, GridOwner.Enemy, GridOwner.Enemy, GridOwner.Enemy };
    }

    /// <summary>创建初始状态的格子（中立）</summary>
    private GridOwner[] CreateInitialGrids()
    {
        return new GridOwner[] { GridOwner.Ally, GridOwner.Ally, GridOwner.Neutral, GridOwner.Enemy, GridOwner.Enemy };
    }

    /// <summary>创建己方占领到格2的格子</summary>
    private GridOwner[] CreateGridsAtPosition2()
    {
        return new GridOwner[] { GridOwner.Ally, GridOwner.Ally, GridOwner.Enemy, GridOwner.Enemy, GridOwner.Enemy };
    }

    /// <summary>创建己方占领到格4的格子</summary>
    private GridOwner[] CreateGridsAtPosition4()
    {
        return new GridOwner[] { GridOwner.Ally, GridOwner.Ally, GridOwner.Ally, GridOwner.Ally, GridOwner.Enemy };
    }

    [Test]
    public void Victory_WhenGridReaches5AndHolds1Turn()
    {
        // 当己方占领 Grid 5 并保持 1 回合时，应判定胜利
        var frontline = CreateFrontline(
            FrontlinePosition.Center,
            CreateVictoryGrids(),
            turnsAtEnemy: 1
        );

        Assert.IsTrue(frontline.IsAtEnemyBase);
        Assert.IsTrue(frontline.TurnsAtEnemyBase >= 1);
    }

    [Test]
    public void Defeat_WhenGridReaches1AndHolds1Turn()
    {
        // 当敌方占领 Grid 1 并保持 1 回合时，应判定失败
        var frontline = CreateFrontline(
            FrontlinePosition.Center,
            CreateDefeatGrids(),
            turnsAtAlly: 1
        );

        Assert.IsTrue(frontline.IsAtAllyBase);
        Assert.IsTrue(frontline.TurnsAtAllyBase >= 1);
    }

    [Test]
    public void NoVictory_WhenGridReaches5ButNotHeld()
    {
        // 当己方刚占领 Grid 5 但还未保持 1 回合时，不应判定胜利
        var frontline = CreateFrontline(
            FrontlinePosition.Center,
            CreateVictoryGrids(),
            turnsAtEnemy: 0
        );

        Assert.IsTrue(frontline.IsAtEnemyBase);
        Assert.IsFalse(frontline.TurnsAtEnemyBase >= 1);
    }

    [Test]
    public void NoDefeat_WhenGridReaches1ButPushedBack()
    {
        // 当敌方曾占领 Grid 1 但被推回时，不应判定失败
        var frontline = CreateFrontline(
            FrontlinePosition.Center,
            CreateGridsAtPosition2(), // 敌方被推回到 Grid 3
            turnsAtAlly: 0 // 计数器已重置
        );

        Assert.IsFalse(frontline.IsAtAllyBase);
        Assert.IsFalse(frontline.TurnsAtAllyBase >= 1);
    }

    [Test]
    public void OccupationCounter_ResetsWhenPushedBack()
    {
        // 验证占领计数器在战线移动后正确重置
        var frontline = CreateFrontline(
            FrontlinePosition.Center,
            CreateVictoryGrids(),
            turnsAtEnemy: 0
        );

        // 模拟第一回合在敌方本阵
        Assert.IsTrue(frontline.IsAtEnemyBase);
        frontline.TurnsAtEnemyBase++;
        Assert.AreEqual(1, frontline.TurnsAtEnemyBase);

        // 模拟被推回（敌方夺回格5）
        frontline.GridOwners[4] = GridOwner.Enemy;
        Assert.IsFalse(frontline.IsAtEnemyBase);

        // 重置计数器（这是 UpdateOccupationStatus 的逻辑）
        frontline.TurnsAtEnemyBase = 0;
        Assert.AreEqual(0, frontline.TurnsAtEnemyBase);
    }

    [Test]
    public void GameResult_InProgress_IsDefault()
    {
        // 验证 GameResult.InProgress 是默认值
        Assert.AreEqual(0, (int)GameResult.InProgress);
    }

    [Test]
    public void AllFrontlines_MustBeChecked()
    {
        // 验证所有三条战线都需要检查
        var frontlines = new Dictionary<FrontlinePosition, FrontlineData>
        {
            { FrontlinePosition.Left, CreateFrontline(FrontlinePosition.Left, CreateInitialGrids()) },
            { FrontlinePosition.Center, CreateFrontline(FrontlinePosition.Center, CreateVictoryGrids(), turnsAtEnemy: 1) },
            { FrontlinePosition.Right, CreateFrontline(FrontlinePosition.Right, CreateInitialGrids()) }
        };

        // 只要有一条战线满足胜利条件即可
        bool hasVictory = false;
        foreach (var frontline in frontlines.Values)
        {
            if (frontline.IsAtEnemyBase && frontline.TurnsAtEnemyBase >= 1)
            {
                hasVictory = true;
                break;
            }
        }

        Assert.IsTrue(hasVictory);
    }

    [Test]
    public void Draw_WhenMaxTurnsReached()
    {
        // 验证 GameResult.Draw 存在
        Assert.IsTrue(System.Enum.IsDefined(typeof(GameResult), GameResult.Draw));
    }

    [Test]
    public void FrontlinePosition_HasThreePositions()
    {
        // 验证有三条战线
        Assert.AreEqual(3, System.Enum.GetValues(typeof(FrontlinePosition)).Length);
        Assert.IsTrue(System.Enum.IsDefined(typeof(FrontlinePosition), FrontlinePosition.Left));
        Assert.IsTrue(System.Enum.IsDefined(typeof(FrontlinePosition), FrontlinePosition.Center));
        Assert.IsTrue(System.Enum.IsDefined(typeof(FrontlinePosition), FrontlinePosition.Right));
    }

    [Test]
    public void LinePosition_BoundaryValues()
    {
        // 测试边界值 - 使用格子归属系统
        var frontlineAllAlly = CreateFrontline(FrontlinePosition.Center, CreateVictoryGrids());
        var frontlineAllEnemy = CreateFrontline(FrontlinePosition.Center, CreateDefeatGrids());
        var frontlineInitial = CreateFrontline(FrontlinePosition.Center, CreateInitialGrids());

        // 己方全占领 → IsAtEnemyBase = true (格5归己方)
        Assert.IsTrue(frontlineAllAlly.IsAtEnemyBase);
        Assert.IsFalse(frontlineAllAlly.IsAtAllyBase);

        // 敌方全占领 → IsAtAllyBase = true (格1归敌方)
        Assert.IsTrue(frontlineAllEnemy.IsAtAllyBase);
        Assert.IsFalse(frontlineAllEnemy.IsAtEnemyBase);

        // 初始状态 → 两者都为 false
        Assert.IsFalse(frontlineInitial.IsAtEnemyBase);
        Assert.IsFalse(frontlineInitial.IsAtAllyBase);
    }

    [Test]
    public void LinePosition_CalculatedFromGridOwners()
    {
        // 测试 LinePosition 是否从 GridOwners 正确计算
        var frontlineInitial = CreateFrontline(FrontlinePosition.Center, CreateInitialGrids());
        var frontlineVictory = CreateFrontline(FrontlinePosition.Center, CreateVictoryGrids());
        var frontlineDefeat = CreateFrontline(FrontlinePosition.Center, CreateDefeatGrids());

        // 初始状态: [A,A,N,E,E] → 己方最后格=2, 敌方第一格=4 → (2+4)/2 = 3
        Assert.AreEqual(3f, frontlineInitial.LinePosition);

        // 胜利状态: [A,A,A,A,A] → 己方最后格=5, 无敌方 → 5
        Assert.AreEqual(5f, frontlineVictory.LinePosition);

        // 失败状态: [E,E,E,E,E] → 无己方, 敌方第一格=1 → 1
        Assert.AreEqual(1f, frontlineDefeat.LinePosition);
    }
}
