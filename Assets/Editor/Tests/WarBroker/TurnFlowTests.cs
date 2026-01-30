using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 回合流程测试：验证五阶段流程正确执行
/// </summary>
public class TurnFlowTests
{
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
    public void FrontlineData_IsAtEnemyBase_WhenLinePositionIs5()
    {
        var frontline = new FrontlineData
        {
            Position = FrontlinePosition.Center,
            LinePosition = 5
        };

        Assert.IsTrue(frontline.IsAtEnemyBase);
        Assert.IsFalse(frontline.IsAtAllyBase);
    }

    [Test]
    public void FrontlineData_IsAtAllyBase_WhenLinePositionIs1()
    {
        var frontline = new FrontlineData
        {
            Position = FrontlinePosition.Center,
            LinePosition = 1
        };

        Assert.IsFalse(frontline.IsAtEnemyBase);
        Assert.IsTrue(frontline.IsAtAllyBase);
    }

    [Test]
    public void FrontlineData_NotAtBase_WhenLinePositionIsMiddle()
    {
        var frontline = new FrontlineData
        {
            Position = FrontlinePosition.Center,
            LinePosition = 3
        };

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

        Assert.AreEqual(3, frontline.LinePosition);
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
}
