#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 一键配置所有 WarBroker SO 资产
/// 菜单: WarBroker > Setup All Configs
/// </summary>
public static class WarBrokerConfigSetup
{
    private const string BasePath = "Assets/Resources/Config/WarBroker/";

    [MenuItem("WarBroker/Setup All Configs")]
    public static void SetupAll()
    {
        SetupSkillConfig();
        SetupGeneralConfig();
        SetupGameBalanceConfig();
        SetupOrderConfig();
        SetupCampaignConfig();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WarBroker] 所有配置已填充完毕！");
    }

    [MenuItem("WarBroker/Setup Skill Config")]
    public static void SetupSkillConfig()
    {
        var config = LoadOrCreate<SkillConfig>("SkillConfig");
        config.Skills = new SkillConfigItem[]
        {
            // === 狂热型 ===
            new SkillConfigItem
            {
                SkillId = "fanatic_charge", SkillName = "先登",
                Description = "ATK获胜时战线额外+1",
                Personality = GeneralPersonality.Fanatic,
                TriggerOrder = OrderType.ATK,
                BonusLineMovement = 1
            },
            new SkillConfigItem
            {
                SkillId = "fanatic_bloodlust", SkillName = "嗜血",
                Description = "连续ATK战斗力+20%",
                Personality = GeneralPersonality.Fanatic,
                TriggerOrder = OrderType.ATK,
                RequireConsecutive = true,
                CombatBonus = 0.2f
            },
            new SkillConfigItem
            {
                SkillId = "fanatic_laststand", SkillName = "死战",
                Description = "兵力<30时ATK战斗力+50%",
                Personality = GeneralPersonality.Fanatic,
                TriggerOrder = OrderType.ATK,
                TroopThreshold = 30,
                CombatBonus = 0.5f
            },
            new SkillConfigItem
            {
                SkillId = "fanatic_defiant", SkillName = "不退",
                Description = "被指派RET时30%抗命改DEF",
                Personality = GeneralPersonality.Fanatic,
                TriggerOrder = OrderType.RET,
                DisobeyToOrder = OrderType.DEF,
                DisobeyChance = 0.3f
            },
            // === 保守型 ===
            new SkillConfigItem
            {
                SkillId = "conservative_ironwall", SkillName = "铁壁",
                Description = "DEF成功时敌方-5兵力",
                Personality = GeneralPersonality.Conservative,
                TriggerOrder = OrderType.DEF,
                EnemyTroopChange = -5
            },
            new SkillConfigItem
            {
                SkillId = "conservative_patience", SkillName = "以逸待劳",
                Description = "连续DEF战斗力+50%",
                Personality = GeneralPersonality.Conservative,
                TriggerOrder = OrderType.DEF,
                RequireConsecutive = true,
                CombatBonus = 0.5f
            },
            new SkillConfigItem
            {
                SkillId = "conservative_rearguard", SkillName = "断后",
                Description = "RET时己方损失减半(+5兵力补偿)",
                Personality = GeneralPersonality.Conservative,
                TriggerOrder = OrderType.RET,
                AllyTroopChange = 5
            },
            new SkillConfigItem
            {
                SkillId = "conservative_timid", SkillName = "怯战",
                Description = "兵力<50时ATK可能抗命改DEF",
                Personality = GeneralPersonality.Conservative,
                TriggerOrder = OrderType.ATK,
                TroopThreshold = 50,
                DisobeyToOrder = OrderType.DEF,
                DisobeyChance = 0.3f
            },
            // === 投机型 ===
            new SkillConfigItem
            {
                SkillId = "opportunist_momentum", SkillName = "顺风浪",
                Description = "战线≥4时ATK战斗力+30%",
                Personality = GeneralPersonality.Opportunist,
                TriggerOrder = OrderType.ATK,
                FrontlineThreshold = 4,
                CombatBonus = 0.3f
            },
            new SkillConfigItem
            {
                SkillId = "opportunist_turtle", SkillName = "逆风苟",
                Description = "战线≤2时DEF战斗力+30%",
                Personality = GeneralPersonality.Opportunist,
                TriggerOrder = OrderType.DEF,
                CombatBonus = 0.3f
            },
            new SkillConfigItem
            {
                SkillId = "opportunist_feint", SkillName = "诈败",
                Description = "RET诱敌追击，敌方额外-10兵力",
                Personality = GeneralPersonality.Opportunist,
                TriggerOrder = OrderType.RET,
                EnemyTroopChange = -10
            },
            new SkillConfigItem
            {
                SkillId = "opportunist_adapt", SkillName = "见机行事",
                Description = "所有指令战斗力+10%",
                Personality = GeneralPersonality.Opportunist,
                CombatBonus = 0.1f
            }
        };

        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] SkillConfig 已配置 12 个技能");
    }

    [MenuItem("WarBroker/Setup General Config")]
    public static void SetupGeneralConfig()
    {
        var config = LoadOrCreate<GeneralConfig>("GeneralConfig");

        config.AllyGenerals = new GeneralConfigItem[]
        {
            new GeneralConfigItem
            {
                GeneralId = "ally_left", Name = "冯·布吕歇尔",
                Biography = "普鲁士元帅，以勇猛著称",
                Personality = GeneralPersonality.Fanatic,
                InitialTroops = 80, InitialTrust = 50, InitialMorale = 60,
                SkillIds = new[] { "fanatic_charge", "fanatic_bloodlust" },
                AtkBidModifier = 1.5f, DefBidModifier = 0.7f, RetBidModifier = 0.5f
            },
            new GeneralConfigItem
            {
                GeneralId = "ally_center", Name = "库图佐夫",
                Biography = "俄国元帅，老谋深算的防守大师",
                Personality = GeneralPersonality.Conservative,
                InitialTroops = 80, InitialTrust = 50, InitialMorale = 60,
                SkillIds = new[] { "conservative_ironwall", "conservative_rearguard" },
                AtkBidModifier = 0.5f, DefBidModifier = 1.5f, RetBidModifier = 1.2f
            },
            new GeneralConfigItem
            {
                GeneralId = "ally_right", Name = "塔列朗",
                Biography = "法国外交官，见风使舵的机会主义者",
                Personality = GeneralPersonality.Opportunist,
                InitialTroops = 80, InitialTrust = 50, InitialMorale = 60,
                SkillIds = new[] { "opportunist_momentum", "opportunist_adapt" },
                AtkBidModifier = 1.0f, DefBidModifier = 1.0f, RetBidModifier = 1.0f
            }
        };

        config.EnemyGenerals = new GeneralConfigItem[]
        {
            new GeneralConfigItem
            {
                GeneralId = "enemy_left", Name = "拿破仑",
                Biography = "法兰西皇帝，军事天才",
                Personality = GeneralPersonality.Opportunist,
                InitialTroops = 80, InitialTrust = 60, InitialMorale = 70,
                SkillIds = new[] { "opportunist_momentum", "opportunist_feint" },
                AtkBidModifier = 1.0f, DefBidModifier = 1.0f, RetBidModifier = 1.0f
            },
            new GeneralConfigItem
            {
                GeneralId = "enemy_center", Name = "威灵顿",
                Biography = "英国公爵，防御战术大师",
                Personality = GeneralPersonality.Conservative,
                InitialTroops = 80, InitialTrust = 60, InitialMorale = 70,
                SkillIds = new[] { "conservative_ironwall", "conservative_patience" },
                AtkBidModifier = 0.5f, DefBidModifier = 1.5f, RetBidModifier = 1.2f
            },
            new GeneralConfigItem
            {
                GeneralId = "enemy_right", Name = "内伊",
                Biography = "法国元帅，勇者中的勇者",
                Personality = GeneralPersonality.Fanatic,
                InitialTroops = 80, InitialTrust = 60, InitialMorale = 70,
                SkillIds = new[] { "fanatic_laststand", "fanatic_defiant" },
                AtkBidModifier = 1.5f, DefBidModifier = 0.7f, RetBidModifier = 0.5f
            }
        };

        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] GeneralConfig 已配置 3+3 名将军");
    }

    [MenuItem("WarBroker/Setup Game Balance Config")]
    public static void SetupGameBalanceConfig()
    {
        var config = LoadOrCreate<GameBalanceConfig>("GameBalanceConfig");
        // 使用默认值即可，已在类定义中设置
        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] GameBalanceConfig 已确认（使用默认值）");
    }

    [MenuItem("WarBroker/Setup Order Config")]
    public static void SetupOrderConfig()
    {
        var config = LoadOrCreate<OrderConfig>("OrderConfig");
        config.Orders = new OrderConfigItem[]
        {
            new OrderConfigItem { OrderType = OrderType.ATK, BasePrice = 40f, ProductionPerTurn = 3, InitialStock = 10 },
            new OrderConfigItem { OrderType = OrderType.DEF, BasePrice = 35f, ProductionPerTurn = 3, InitialStock = 10 },
            new OrderConfigItem { OrderType = OrderType.RET, BasePrice = 25f, ProductionPerTurn = 2, InitialStock = 8 }
        };
        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] OrderConfig 已配置 3 种指令");
    }

    [MenuItem("WarBroker/Setup Campaign Config")]
    public static void SetupCampaignConfig()
    {
        var generalConfig = AssetDatabase.LoadAssetAtPath<GeneralConfig>(BasePath + "GeneralConfig.asset");
        if (generalConfig == null)
        {
            Debug.LogError("请先运行 Setup General Config！");
            return;
        }

        var config = LoadOrCreate<CampaignConfig>("Campaign_Tutorial");
        config.CampaignId = "Campaign_Tutorial";
        config.CampaignName = "教程战役：滑铁卢";
        config.Description = "初始战役，学习市场交易、指令分配与战场管理的基本操作。";
        config.MaxTurns = 20;
        config.InitialCash = 500f;
        config.InitialAtkInventory = 2;
        config.InitialDefInventory = 2;
        config.InitialRetInventory = 2;
        config.InitialFrontlinePosition = 3;
        config.GeneralConfig = generalConfig;
        config.VictorInitialCash = 500f;
        config.VictorDifficulty = 0.5f;

        config.AllyFrontlineAssignments = new FrontlineAssignment[]
        {
            new FrontlineAssignment { Position = FrontlinePosition.Left, GeneralId = "ally_left" },
            new FrontlineAssignment { Position = FrontlinePosition.Center, GeneralId = "ally_center" },
            new FrontlineAssignment { Position = FrontlinePosition.Right, GeneralId = "ally_right" }
        };

        config.EnemyFrontlineAssignments = new FrontlineAssignment[]
        {
            new FrontlineAssignment { Position = FrontlinePosition.Left, GeneralId = "enemy_left" },
            new FrontlineAssignment { Position = FrontlinePosition.Center, GeneralId = "enemy_center" },
            new FrontlineAssignment { Position = FrontlinePosition.Right, GeneralId = "enemy_right" }
        };

        config.AvailableEvents = new RandomEventConfig[]
        {
            new RandomEventConfig
            {
                EventId = "evt_plague", EventName = "瘟疫爆发",
                Description = "军中瘟疫蔓延，所有将军兵力-10",
                AllTroopChange = -10, Duration = 2,
                DefDemandModifier = 0.3f, RetDemandModifier = 0.2f
            },
            new RandomEventConfig
            {
                EventId = "evt_supply", EventName = "补给线畅通",
                Description = "后方补给充足，产能提升",
                ProductionModifier = 0.5f, Duration = 2,
                AtkDemandModifier = 0.1f
            },
            new RandomEventConfig
            {
                EventId = "evt_morale_boost", EventName = "捷报传来",
                Description = "友军取得大捷，全军士气提升",
                RandomTrustChange = 10, Duration = 1,
                AtkDemandModifier = 0.2f
            }
        };

        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] Campaign_Tutorial 已配置完成");
    }

    /// <summary>加载已有资产或创建新资产</summary>
    private static T LoadOrCreate<T>(string fileName) where T : ScriptableObject
    {
        string path = BasePath + fileName + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        // 确保目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Config"))
            AssetDatabase.CreateFolder("Assets/Resources", "Config");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Config/WarBroker"))
            AssetDatabase.CreateFolder("Assets/Resources/Config", "WarBroker");

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
#endif
