#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// WarBroker 配置资产创建工具
/// 仅负责创建空的 ScriptableObject 资产
/// </summary>
public static class WarBrokerConfigSetup
{
    private const string BasePath = "Assets/Resources/Config/WarBroker/";

    [MenuItem("WarBroker/Create Empty Configs/All Configs")]
    public static void CreateAllEmptyConfigs()
    {
        CreateEmptyGeneralConfig();
        CreateEmptyGameBalanceConfig();
        CreateEmptyOrderConfig();
        CreateEmptyCampaignConfig();
        CreateAllVictorProfiles();
        CreateTutorialConfig();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WarBroker] 所有空配置已创建！请使用 Campaign Editor 进行编辑。");
    }

    [MenuItem("WarBroker/Create Empty Configs/General Config")]
    public static void CreateEmptyGeneralConfig()
    {
        var config = CreateOrLoad<GeneralConfig>("GeneralConfig");
        if (config.AllyGenerals == null)
        {
            config.AllyGenerals = new GeneralConfigItem[0];
            EditorUtility.SetDirty(config);
        }
        if (config.EnemyGenerals == null)
        {
            config.EnemyGenerals = new GeneralConfigItem[0];
            EditorUtility.SetDirty(config);
        }
        Debug.Log("[WarBroker] GeneralConfig 已就绪");
    }

    [MenuItem("WarBroker/Create Empty Configs/Game Balance Config")]
    public static void CreateEmptyGameBalanceConfig()
    {
        CreateOrLoad<GameBalanceConfig>("GameBalanceConfig");
        Debug.Log("[WarBroker] GameBalanceConfig 已就绪");
    }

    [MenuItem("WarBroker/Create Empty Configs/Order Config")]
    public static void CreateEmptyOrderConfig()
    {
        var config = CreateOrLoad<OrderConfig>("OrderConfig");
        if (config.Orders == null || config.Orders.Length == 0)
        {
            config.Orders = new OrderConfigItem[0];
            EditorUtility.SetDirty(config);
        }
        Debug.Log("[WarBroker] OrderConfig 已就绪");
    }

    [MenuItem("WarBroker/Create Empty Configs/Campaign Config")]
    public static void CreateEmptyCampaignConfig()
    {
        var config = CreateOrLoad<CampaignConfig>("Campaign_New");
        config.CampaignId = "Campaign_New";
        config.CampaignName = "新战役";
        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] CampaignConfig 已就绪");
    }

    [MenuItem("WarBroker/Create Empty Configs/Victor Profiles (All 4)")]
    public static void CreateAllVictorProfiles()
    {
        CreateVictorProfile_Default();
        CreateVictorProfile_Balanced();
        CreateVictorProfile_Hunter();
        CreateVictorProfile_Madman();
        Debug.Log("[WarBroker] 所有 VictorProfile 已创建");
    }

    [MenuItem("WarBroker/Create Empty Configs/Victor Profile - Default (教学关)")]
    public static void CreateVictorProfile_Default()
    {
        var config = CreateOrLoad<VictorProfile>("VictorProfile_Default");
        // GDD v7.3: 老实人维克多 - 纯军事采购，不投机不欺骗
        config.MilitaryPriority = 1.0f;
        config.SpeculationTendency = 0.0f;
        config.Deception = 0.0f;
        config.Adaptiveness = 0.0f;
        config.PriceToleranceMultiplier = 2.0f;
        config.CashReserveRatio = 0.2f;
        config.BetraySelfThreshold = 0.5f;
        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] VictorProfile_Default 已创建 (老实人)");
    }

    [MenuItem("WarBroker/Create Empty Configs/Victor Profile - Balanced (中期关)")]
    public static void CreateVictorProfile_Balanced()
    {
        var config = CreateOrLoad<VictorProfile>("VictorProfile_Balanced");
        // GDD v7.3: 精打细算的指挥官 - 优先军事但有预算意识
        config.MilitaryPriority = 0.7f;
        config.SpeculationTendency = 0.3f;
        config.Deception = 0.1f;
        config.Adaptiveness = 0.2f;
        config.PriceToleranceMultiplier = 1.8f;
        config.CashReserveRatio = 0.25f;
        config.BetraySelfThreshold = 0.4f;
        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] VictorProfile_Balanced 已创建 (精打细算)");
    }

    [MenuItem("WarBroker/Create Empty Configs/Victor Profile - Hunter (后期关)")]
    public static void CreateVictorProfile_Hunter()
    {
        var config = CreateOrLoad<VictorProfile>("VictorProfile_Hunter");
        // GDD v7.3: 金融猎手 - 军事只是手段，主动设计圈套
        config.MilitaryPriority = 0.4f;
        config.SpeculationTendency = 0.8f;
        config.Deception = 0.7f;
        config.Adaptiveness = 0.6f;
        config.PriceToleranceMultiplier = 1.5f;
        config.CashReserveRatio = 0.3f;
        config.BetraySelfThreshold = 0.3f;
        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] VictorProfile_Hunter 已创建 (金融猎手)");
    }

    [MenuItem("WarBroker/Create Empty Configs/Victor Profile - Madman (特殊关)")]
    public static void CreateVictorProfile_Madman()
    {
        var config = CreateOrLoad<VictorProfile>("VictorProfile_Madman");
        // GDD v7.3: 疯子 - 军事完全不管，全部资源搞金融战
        config.MilitaryPriority = 0.1f;
        config.SpeculationTendency = 1.0f;
        config.Deception = 0.5f;
        config.Adaptiveness = 0.9f;
        config.PriceToleranceMultiplier = 1.3f;
        config.CashReserveRatio = 0.1f;
        config.BetraySelfThreshold = 0.2f;
        EditorUtility.SetDirty(config);
        Debug.Log("[WarBroker] VictorProfile_Madman 已创建 (疯子)");
    }

    [MenuItem("WarBroker/Create Empty Configs/Tutorial Config")]
    public static void CreateTutorialConfig()
    {
        var config = CreateOrLoad<TutorialConfig>("TutorialConfig");

        // 填充示例教程步骤（使用 Naninovel 脚本）
        config.Steps = new List<TutorialStep>
        {
            // 步骤1：欢迎
            new TutorialStep
            {
                StepId = "welcome",
                Description = "欢迎语",
                ScriptName = "Tutorial",
                StartLabel = "welcome",
                Trigger = TutorialTrigger.Immediate
            },

            // 步骤2：介绍市场
            new TutorialStep
            {
                StepId = "intro_market",
                Description = "市场介绍",
                ScriptName = "Tutorial",
                StartLabel = "intro_market",
                Trigger = TutorialTrigger.Immediate,
                HighlightTarget = "MarketPanel"
            },

            // 步骤3：教玩家买入
            new TutorialStep
            {
                StepId = "teach_buy",
                Description = "教买入",
                ScriptName = "Tutorial",
                StartLabel = "teach_buy",
                Trigger = TutorialTrigger.WaitForBuy,
                TriggerParameter = "ATK",
                HighlightTarget = "BuyButton_ATK"
            },

            // 步骤4：买入成功
            new TutorialStep
            {
                StepId = "buy_success",
                Description = "买入成功反馈",
                ScriptName = "Tutorial",
                StartLabel = "buy_success",
                Trigger = TutorialTrigger.Immediate
            },

            // 步骤5：介绍将军
            new TutorialStep
            {
                StepId = "intro_generals",
                Description = "将军介绍",
                ScriptName = "Tutorial",
                StartLabel = "intro_generals",
                Trigger = TutorialTrigger.Immediate,
                HighlightTarget = "GeneralPanel"
            },

            // 步骤6：教分配指令
            new TutorialStep
            {
                StepId = "teach_assign",
                Description = "教分配指令",
                ScriptName = "Tutorial",
                StartLabel = "teach_assign",
                Trigger = TutorialTrigger.WaitForAssign,
                HighlightTarget = "GeneralCard_0"
            },

            // 步骤7：结束回合
            new TutorialStep
            {
                StepId = "teach_endturn",
                Description = "教结束回合",
                ScriptName = "Tutorial",
                StartLabel = "teach_endturn",
                Trigger = TutorialTrigger.WaitForEndTurn,
                HighlightTarget = "EndTurnButton"
            },

            // 步骤8：战斗说明
            new TutorialStep
            {
                StepId = "battle_intro",
                Description = "战斗说明",
                ScriptName = "Tutorial",
                StartLabel = "battle_intro",
                Trigger = TutorialTrigger.WaitForPhase,
                TriggerParameter = "MarketPhase"
            },

            // 步骤9：核心目标
            new TutorialStep
            {
                StepId = "goal",
                Description = "游戏目标",
                ScriptName = "Tutorial",
                StartLabel = "goal",
                Trigger = TutorialTrigger.Immediate
            },

            // 步骤10：结束
            new TutorialStep
            {
                StepId = "complete",
                Description = "教程结束",
                ScriptName = "Tutorial",
                StartLabel = "complete",
                Trigger = TutorialTrigger.Immediate
            }
        };

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("[WarBroker] TutorialConfig 已创建，包含10个教程步骤（使用 Naninovel 脚本）");

        // 选中并高亮创建的资产
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }

    /// <summary>
    /// 创建新资产或加载已有资产
    /// </summary>
    private static T CreateOrLoad<T>(string fileName) where T : ScriptableObject
    {
        EnsureDirectoryExists();

        string path = BasePath + fileName + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        Undo.RegisterCreatedObjectUndo(asset, $"Create {typeof(T).Name}");
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    /// <summary>
    /// 确保配置目录存在
    /// </summary>
    private static void EnsureDirectoryExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Config"))
            AssetDatabase.CreateFolder("Assets/Resources", "Config");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Config/WarBroker"))
            AssetDatabase.CreateFolder("Assets/Resources/Config", "WarBroker");
    }
}
#endif
