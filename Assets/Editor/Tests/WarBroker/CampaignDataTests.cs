using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// CampaignSystem 数据模型测试
/// </summary>
public class CampaignDataTests
{
    [Test]
    public void CampaignRuntimeData_InitFromConfig()
    {
        var campaignConfig = ScriptableObject.CreateInstance<CampaignConfig>();
        campaignConfig.CampaignId = "test";
        campaignConfig.CampaignName = "Test Campaign";
        campaignConfig.MaxTurns = 20;
        campaignConfig.InitialCash = 500f;
        campaignConfig.InitialAtkInventory = 2;
        campaignConfig.InitialDefInventory = 2;
        campaignConfig.InitialRetInventory = 2;
        campaignConfig.InitialFrontlinePosition = 3;
        campaignConfig.VictorInitialCash = 500f;
        campaignConfig.AllyFrontlineAssignments = new FrontlineAssignment[0];
        campaignConfig.EnemyFrontlineAssignments = new FrontlineAssignment[0];

        var generalConfig = ScriptableObject.CreateInstance<GeneralConfig>();
        generalConfig.AllyGenerals = new GeneralConfigItem[0];
        generalConfig.EnemyGenerals = new GeneralConfigItem[0];
        campaignConfig.GeneralConfig = generalConfig;

        var orderConfig = ScriptableObject.CreateInstance<OrderConfig>();
        var skillConfig = ScriptableObject.CreateInstance<SkillConfig>();
        skillConfig.Skills = new SkillConfigItem[0];

        var data = new CampaignRuntimeData();
        data.InitFromConfig(campaignConfig, orderConfig, skillConfig);

        Assert.AreEqual(1, data.CurrentTurn);
        Assert.AreEqual(TurnPhase.TurnStart, data.CurrentPhase);
        Assert.AreEqual(500f, data.Player.Cash);
        Assert.AreEqual(2, data.Player.Inventory[OrderType.ATK]);
        Assert.AreEqual(0, data.Player.BankDebt);
        Assert.AreEqual(500f, data.VictorCash);
        Assert.IsNotNull(data.TurnHistory);
        Assert.AreEqual(0, data.TurnHistory.Count);

        Object.DestroyImmediate(campaignConfig);
        Object.DestroyImmediate(generalConfig);
        Object.DestroyImmediate(orderConfig);
        Object.DestroyImmediate(skillConfig);
    }

    [Test]
    public void TurnRecord_StoresCorrectData()
    {
        var record = new TurnRecord
        {
            TurnNumber = 5,
            OrderAssignments = new Dictionary<string, OrderType>
            {
                { "general_1", OrderType.ATK },
                { "general_2", OrderType.DEF }
            },
            Transactions = new List<TransactionRecord>(),
            BattleResults = new List<BattleResult>(),
            PriceSnapshot = new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 45f },
                { OrderType.DEF, 38f },
                { OrderType.RET, 28f }
            },
            PlayerNetWorth = 650f
        };

        Assert.AreEqual(5, record.TurnNumber);
        Assert.AreEqual(OrderType.ATK, record.OrderAssignments["general_1"]);
        Assert.AreEqual(650f, record.PlayerNetWorth);
        Assert.AreEqual(45f, record.PriceSnapshot[OrderType.ATK]);
    }

    [Test]
    public void TransactionRecord_AllTypesExist()
    {
        Assert.AreEqual(6, System.Enum.GetValues(typeof(TransactionRecord.TransactionType)).Length);
    }

    [Test]
    public void PlayerData_InitFromConfig()
    {
        var config = ScriptableObject.CreateInstance<CampaignConfig>();
        config.InitialCash = 1000f;
        config.InitialAtkInventory = 5;
        config.InitialDefInventory = 3;
        config.InitialRetInventory = 1;

        var player = new PlayerData();
        player.InitFromConfig(config);

        Assert.AreEqual(1000f, player.Cash);
        Assert.AreEqual(5, player.Inventory[OrderType.ATK]);
        Assert.AreEqual(3, player.Inventory[OrderType.DEF]);
        Assert.AreEqual(1, player.Inventory[OrderType.RET]);
        Assert.AreEqual(0, player.BankDebt);
        Assert.AreEqual(0, player.AuditValue);

        Object.DestroyImmediate(config);
    }
}
