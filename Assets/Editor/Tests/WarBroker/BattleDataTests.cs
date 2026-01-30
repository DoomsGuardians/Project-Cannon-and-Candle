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
            SkillIds = new string[0],
            AtkBidModifier = personality == GeneralPersonality.Fanatic ? 1.5f : 1f,
            DefBidModifier = personality == GeneralPersonality.Conservative ? 1.5f : 1f,
            RetBidModifier = 1f
        };

        var skillConfig = ScriptableObject.CreateInstance<SkillConfig>();
        skillConfig.Skills = new SkillConfigItem[0];

        var general = new GeneralData();
        general.InitFromConfig(config, skillConfig);

        Object.DestroyImmediate(skillConfig);
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
    public void Status_HighStats_Healthy()
    {
        var g = CreateGeneral(16, 80, 80);
        // composite = (16*5)*0.4 + 80*0.3 + 80*0.3 = 80*0.4 + 24 + 24 = 32 + 48 = 80 => Healthy (70-85)
        Assert.AreEqual(GeneralStatus.Healthy, g.GetStatus(balanceConfig));
    }

    [Test]
    public void Status_LowTroops_Routed()
    {
        var g = CreateGeneral(3, 80, 80); // Troops=3 <= RoutTroopThreshold(4)
        Assert.AreEqual(GeneralStatus.Routed, g.GetStatus(balanceConfig));
    }

    [Test]
    public void Status_LowComposite_Routed()
    {
        var g = CreateGeneral(5, 10, 10);
        // composite = (5*5)*0.4 + 10*0.3 + 10*0.3 = 25*0.4 + 3 + 3 = 10 + 6 = 16 < RoutScoreThreshold(20)
        Assert.AreEqual(GeneralStatus.Routed, g.GetStatus(balanceConfig));
    }

    [Test]
    public void Status_MediumComposite_Critical()
    {
        var g = CreateGeneral(8, 30, 40);
        // composite = (8*5)*0.4 + 30*0.3 + 40*0.3 = 40*0.4 + 9 + 12 = 16 + 21 = 37 => Critical (20-50)
        Assert.AreEqual(GeneralStatus.Critical, g.GetStatus(balanceConfig));
    }

    [Test]
    public void Status_ModerateComposite_Wounded()
    {
        var g = CreateGeneral(12, 50, 50);
        // composite = (12*5)*0.4 + 50*0.3 + 50*0.3 = 60*0.4 + 15 + 15 = 24 + 30 = 54 => Wounded (50-70)
        Assert.AreEqual(GeneralStatus.Wounded, g.GetStatus(balanceConfig));
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
        Assert.IsFalse(result.SkillTriggered);
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

        Assert.AreEqual(3, frontline.LinePosition);
        Assert.AreEqual(0, frontline.StagnantTurns);

        Object.DestroyImmediate(campaignConfig);
    }
}
