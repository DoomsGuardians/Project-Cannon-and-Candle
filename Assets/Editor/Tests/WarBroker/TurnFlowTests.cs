using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 回合流程测试：验证五阶段流程正确执行
/// </summary>
public class TurnFlowTests
{
    /// <summary>创建指定格子归属的战线数据</summary>
    private FrontlineData CreateFrontline(FrontlinePosition pos, GridOwner[] gridOwners)
    {
        return new FrontlineData
        {
            Position = pos,
            GridOwners = gridOwners
        };
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

    [Test]
    public void TurnPhase_HasCorrectValues()
    {
        // 验证 TurnPhase 枚举包含所有五个阶段
        Assert.AreEqual(6, System.Enum.GetValues(typeof(TurnPhase)).Length);
        Assert.IsTrue(System.Enum.IsDefined(typeof(TurnPhase), TurnPhase.TurnStart));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TurnPhase), TurnPhase.EventPhase));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TurnPhase), TurnPhase.MarketPhase));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TurnPhase), TurnPhase.IntentPhase));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TurnPhase), TurnPhase.BattlePhase));
        Assert.IsTrue(System.Enum.IsDefined(typeof(TurnPhase), TurnPhase.SettlementPhase));
    }

    [Test]
    public void TurnPhase_OrderIsCorrect()
    {
        // 验证阶段顺序
        Assert.Less((int)TurnPhase.TurnStart, (int)TurnPhase.EventPhase);
        Assert.Less((int)TurnPhase.EventPhase, (int)TurnPhase.MarketPhase);
        Assert.Less((int)TurnPhase.MarketPhase, (int)TurnPhase.IntentPhase);
        Assert.Less((int)TurnPhase.IntentPhase, (int)TurnPhase.BattlePhase);
        Assert.Less((int)TurnPhase.BattlePhase, (int)TurnPhase.SettlementPhase);
    }

    [Test]
    public void GameResult_HasCorrectValues()
    {
        // 验证 GameResult 枚举
        Assert.AreEqual(4, System.Enum.GetValues(typeof(GameResult)).Length);
        Assert.IsTrue(System.Enum.IsDefined(typeof(GameResult), GameResult.InProgress));
        Assert.IsTrue(System.Enum.IsDefined(typeof(GameResult), GameResult.Victory));
        Assert.IsTrue(System.Enum.IsDefined(typeof(GameResult), GameResult.Defeat));
        Assert.IsTrue(System.Enum.IsDefined(typeof(GameResult), GameResult.Draw));
    }

    [Test]
    public void FrontlineData_IsAtEnemyBase_WhenAllyOwnsGrid5()
    {
        var frontline = CreateFrontline(FrontlinePosition.Center, CreateVictoryGrids());

        Assert.IsTrue(frontline.IsAtEnemyBase);
        Assert.IsFalse(frontline.IsAtAllyBase);
    }

    [Test]
    public void FrontlineData_IsAtAllyBase_WhenEnemyOwnsGrid1()
    {
        var frontline = CreateFrontline(FrontlinePosition.Center, CreateDefeatGrids());

        Assert.IsFalse(frontline.IsAtEnemyBase);
        Assert.IsTrue(frontline.IsAtAllyBase);
    }

    [Test]
    public void FrontlineData_NotAtBase_WhenInitialState()
    {
        var frontline = CreateFrontline(FrontlinePosition.Center, CreateInitialGrids());

        Assert.IsFalse(frontline.IsAtEnemyBase);
        Assert.IsFalse(frontline.IsAtAllyBase);
    }

    [Test]
    public void FrontlineData_InitFromConfig_ResetsOccupationCounters()
    {
        var campaignConfig = ScriptableObject.CreateInstance<CampaignConfig>();
        campaignConfig.InitialFrontlinePosition = 3;

        var frontline = new FrontlineData
        {
            Position = FrontlinePosition.Center,
            TurnsAtEnemyBase = 5,
            TurnsAtAllyBase = 3
        };

        frontline.InitFromConfig(campaignConfig);

        // LinePosition 现在是计算属性，基于 GridOwners
        // 初始状态: [A,A,N,E,E] → LinePosition = 3
        Assert.AreEqual(3f, frontline.LinePosition);
        Assert.AreEqual(0, frontline.StagnantTurns);
        Assert.AreEqual(0, frontline.TurnsAtEnemyBase);
        Assert.AreEqual(0, frontline.TurnsAtAllyBase);

        Object.DestroyImmediate(campaignConfig);
    }

    [Test]
    public void WarBrokerEventID_HasPhaseChangeEvent()
    {
        Assert.IsTrue(System.Enum.IsDefined(typeof(WarBrokerEventID), WarBrokerEventID.OnPhaseChange));
    }

    [Test]
    public void WarBrokerEventID_HasGameEndEvents()
    {
        Assert.IsTrue(System.Enum.IsDefined(typeof(WarBrokerEventID), WarBrokerEventID.OnVictoryConditionMet));
        Assert.IsTrue(System.Enum.IsDefined(typeof(WarBrokerEventID), WarBrokerEventID.OnDefeatConditionMet));
        Assert.IsTrue(System.Enum.IsDefined(typeof(WarBrokerEventID), WarBrokerEventID.OnDrawConditionMet));
        Assert.IsTrue(System.Enum.IsDefined(typeof(WarBrokerEventID), WarBrokerEventID.OnGameEnd));
    }

    [Test]
    public void FrontlineData_LinePosition_CalculatedCorrectly()
    {
        // 测试 LinePosition 计算逻辑
        var frontlineVictory = CreateFrontline(FrontlinePosition.Center, CreateVictoryGrids());
        var frontlineDefeat = CreateFrontline(FrontlinePosition.Center, CreateDefeatGrids());
        var frontlineInitial = CreateFrontline(FrontlinePosition.Center, CreateInitialGrids());

        Assert.AreEqual(5f, frontlineVictory.LinePosition);
        Assert.AreEqual(1f, frontlineDefeat.LinePosition);
        Assert.AreEqual(3f, frontlineInitial.LinePosition);
    }
}
