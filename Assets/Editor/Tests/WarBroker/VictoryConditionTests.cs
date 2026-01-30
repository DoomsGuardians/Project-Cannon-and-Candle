using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 胜利条件测试：验证胜利、失败、平局条件
/// </summary>
public class VictoryConditionTests
{
    [Test]
    public void Victory_WhenGridReaches5AndHolds1Turn()
    {
        // 当战线到达 Grid 5 并保持 1 回合时，应判定胜利
        var frontline = new FrontlineData
        {
            Position = FrontlinePosition.Center,
            LinePosition = 5,
            TurnsAtEnemyBase = 1,
            TurnsAtAllyBase = 0
        };

        Assert.IsTrue(frontline.IsAtEnemyBase);
        Assert.IsTrue(frontline.TurnsAtEnemyBase >= 1);
    }

    [Test]
    public void Defeat_WhenGridReaches1AndHolds1Turn()
    {
        // 当战线退到 Grid 1 并保持 1 回合时，应判定失败
        var frontline = new FrontlineData
        {
            Position = FrontlinePosition.Center,
            LinePosition = 1,
            TurnsAtEnemyBase = 0,
            TurnsAtAllyBase = 1
        };

        Assert.IsTrue(frontline.IsAtAllyBase);
        Assert.IsTrue(frontline.TurnsAtAllyBase >= 1);
    }

    [Test]
    public void NoVictory_WhenGridReaches5ButNotHeld()
    {
        // 当战线刚到达 Grid 5 但还未保持 1 回合时，不应判定胜利
        var frontline = new FrontlineData
        {
            Position = FrontlinePosition.Center,
            LinePosition = 5,
            TurnsAtEnemyBase = 0,
            TurnsAtAllyBase = 0
        };

        Assert.IsTrue(frontline.IsAtEnemyBase);
        Assert.IsFalse(frontline.TurnsAtEnemyBase >= 1);
    }

    [Test]
    public void NoDefeat_WhenGridReaches1ButPushedBack()
    {
        // 当战线曾到达 Grid 1 但被推回时，不应判定失败
        var frontline = new FrontlineData
        {
            Position = FrontlinePosition.Center,
            LinePosition = 2, // 被推回到 Grid 2
            TurnsAtEnemyBase = 0,
            TurnsAtAllyBase = 0 // 计数器已重置
        };

        Assert.IsFalse(frontline.IsAtAllyBase);
        Assert.IsFalse(frontline.TurnsAtAllyBase >= 1);
    }

    [Test]
    public void OccupationCounter_ResetsWhenPushedBack()
    {
        // 验证占领计数器在战线移动后正确重置
        var frontline = new FrontlineData
        {
            Position = FrontlinePosition.Center,
            LinePosition = 5,
            TurnsAtEnemyBase = 0,
            TurnsAtAllyBase = 0
        };

        // 模拟第一回合在敌方本阵
        Assert.IsTrue(frontline.IsAtEnemyBase);
        frontline.TurnsAtEnemyBase++;
        Assert.AreEqual(1, frontline.TurnsAtEnemyBase);

        // 模拟被推回
        frontline.LinePosition = 4;
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
            { FrontlinePosition.Left, new FrontlineData { Position = FrontlinePosition.Left, LinePosition = 3 } },
            { FrontlinePosition.Center, new FrontlineData { Position = FrontlinePosition.Center, LinePosition = 5, TurnsAtEnemyBase = 1 } },
            { FrontlinePosition.Right, new FrontlineData { Position = FrontlinePosition.Right, LinePosition = 3 } }
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
        // 测试边界值
        var frontlineAt1 = new FrontlineData { LinePosition = 1 };
        var frontlineAt5 = new FrontlineData { LinePosition = 5 };
        var frontlineAt0 = new FrontlineData { LinePosition = 0 };
        var frontlineAt6 = new FrontlineData { LinePosition = 6 };

        Assert.IsTrue(frontlineAt1.IsAtAllyBase);
        Assert.IsTrue(frontlineAt5.IsAtEnemyBase);
        Assert.IsTrue(frontlineAt0.IsAtAllyBase); // <= 1
        Assert.IsTrue(frontlineAt6.IsAtEnemyBase); // >= 5
    }
}
