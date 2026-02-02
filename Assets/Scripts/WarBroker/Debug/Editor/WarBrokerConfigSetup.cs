#if UNITY_EDITOR
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
