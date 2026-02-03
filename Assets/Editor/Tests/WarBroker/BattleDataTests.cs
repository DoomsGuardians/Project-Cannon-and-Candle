using NUnit.Framework;
using UnityEngine;

/// <summary>
/// BattleSystem 数据模型测试：将军状态、战斗力计算
/// </summary>
public class BattleDataTests
{
    private GameBalanceConfig balanceConfig;

    [SetUp]
    public void Setup()
    {
        balanceConfig = ScriptableObject.CreateInstance<GameBalanceConfig>();
        // 使用默认值
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(balanceConfig);
    }

    private GeneralData CreateGeneral(int troops, int trust, int morale,
        GeneralPersonality personality = GeneralPersonality.Conservative)
    {
        var config = new GeneralConfigItem
        {
            GeneralId = "test",
            Name = "Test",
            Personality = personality,
            InitialTroops = troops,
            InitialTrust = trust,
            InitialMorale = morale,
            AtkBidModifier = personality == GeneralPersonality.Fanatic ? 1.5f : 1f,
            DefBidModifier = personality == GeneralPersonality.Conservative ? 1.5f : 1f,
            RetBidModifier = 1f
        };

        var general = new GeneralData();
        general.InitFromConfig(config);

        return general;
    }

    [Test]
    public void CompositeScore_Calculation()
    {
        var g = CreateGeneral(16, 50, 60);
        // (16*5)*0.4 + 50*0.3 + 60*0.3 = 80*0.4 + 15 + 18 = 32 + 15 + 18 = 65
        Assert.AreEqual(65f, g.CalculateCompositeScore(), 0.01f);
    }

    [Test]
    public void Status_FullStrength_WhenTroopsAbove15()
    {
        var g = CreateGeneral(16, 80, 80);
        // GDD v6.0: Troops > 15 => FullStrength
        Assert.AreEqual(GeneralStatus.FullStrength, g.GetStatus(balanceConfig));
    }

    [Test]
    public void Status_Healthy_WhenTroops11To15()
    {
        var g = CreateGeneral(15, 80, 80);
        // GDD v6.0: 10 < Troops <= 15 => Healthy
        Assert.AreEqual(GeneralStatus.Healthy, g.GetStatus(balanceConfig));
    }

    [Test]
    public void Status_Wounded_WhenTroops6To10()
    {
        var g = CreateGeneral(8, 50, 50);
        // GDD v6.0: 5 < Troops <= 10 => Wounded
        Assert.AreEqual(GeneralStatus.Wounded, g.GetStatus(balanceConfig));
    }

    [Test]
    public void Status_Critical_WhenTroops1To5()
    {
        var g = CreateGeneral(3, 80, 80);
        // GDD v6.0: 0 < Troops <= 5 => Critical
        Assert.AreEqual(GeneralStatus.Critical, g.GetStatus(balanceConfig));
    }

    [Test]
    public void Status_Routed_WhenTroopsZero()
    {
        var g = CreateGeneral(5, 10, 10);
        g.Troops = 0; // 强制设为0
        // GDD v6.0: Troops <= 0 => Routed
        Assert.AreEqual(GeneralStatus.Routed, g.GetStatus(balanceConfig));
    }

    [Test]
    public void Bid_HighTrust_HigherBid()
    {
        var g1 = CreateGeneral(16, 80, 80);
        var g2 = CreateGeneral(16, 30, 80);
        float bid1 = g1.CalculateBid(OrderType.ATK, 40f, balanceConfig);
        float bid2 = g2.CalculateBid(OrderType.ATK, 40f, balanceConfig);
        Assert.Greater(bid1, bid2);
    }

    [Test]
    public void Bid_AlwaysPositive_ForHealthyGeneral()
    {
        var g = CreateGeneral(16, 80, 80);
        float bid = g.CalculateBid(OrderType.DEF, 40f, balanceConfig);
        Assert.Greater(bid, 0f);
    }

    [Test]
    public void BattleResult_DefaultValues()
    {
        var result = new BattleResult();
        Assert.AreEqual(0, result.LineMovement);
        Assert.AreEqual(0, result.AllyTroopChange);
        Assert.AreEqual(0, result.EnemyTroopChange);
        Assert.IsFalse(result.WasCrit);
        Assert.IsFalse(result.WasFumble);
    }

    [Test]
    public void FrontlineData_InitFromConfig()
    {
        var campaignConfig = ScriptableObject.CreateInstance<CampaignConfig>();
        campaignConfig.InitialFrontlinePosition = 3;

        var frontline = new FrontlineData { Position = FrontlinePosition.Center };
        frontline.InitFromConfig(campaignConfig);

        // LinePosition 现在是计算属性，初始状态 [A,A,N,E,E] => 3.0f
        Assert.AreEqual(3f, frontline.LinePosition);
        Assert.AreEqual(0, frontline.StagnantTurns);

        Object.DestroyImmediate(campaignConfig);
    }
}
