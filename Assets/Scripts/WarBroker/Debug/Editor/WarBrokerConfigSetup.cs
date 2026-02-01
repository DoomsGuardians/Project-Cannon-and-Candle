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
        CreateEmptySkillConfig();
        CreateEmptyGeneralConfig();
        CreateEmptyGameBalanceConfig();
        CreateEmptyOrderConfig();
        CreateEmptyCampaignConfig();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WarBroker] 所有空配置已创建！请使用 Campaign Editor 进行编辑。");
    }

    [MenuItem("WarBroker/Create Empty Configs/Skill Config")]
    public static void CreateEmptySkillConfig()
    {
        var config = CreateOrLoad<SkillConfig>("SkillConfig");
        if (config.Skills == null || config.Skills.Length == 0)
        {
            config.Skills = new SkillConfigItem[0];
            EditorUtility.SetDirty(config);
        }
        Debug.Log("[WarBroker] SkillConfig 已就绪");
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
