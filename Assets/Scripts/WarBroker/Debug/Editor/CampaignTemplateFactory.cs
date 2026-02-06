#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 战役模板类型
/// </summary>
public enum CampaignTemplateType
{
    Tutorial,   // 教学关
    Standard,   // 标准关
    Challenge,  // 挑战关
    Sandbox     // 沙盒关
}

/// <summary>
/// 战役模板工厂
/// 用于从预设模板快速创建新战役配置
/// </summary>
public static class CampaignTemplateFactory
{
    private const string ConfigPath = "Assets/Resources/Config/WarBroker/";

    /// <summary>
    /// 从模板创建战役配置
    /// </summary>
    /// <param name="templateType">模板类型</param>
    /// <param name="campaignId">战役 ID</param>
    /// <param name="campaignName">战役名称</param>
    /// <returns>创建的战役配置</returns>
    public static CampaignConfig CreateFromTemplate(CampaignTemplateType templateType, string campaignId, string campaignName)
    {
        // 确保目录存在
        EnsureDirectoryExists();

        // 检查 ID 是否已存在
        string path = ConfigPath + campaignId + ".asset";
        if (AssetDatabase.LoadAssetAtPath<CampaignConfig>(path) != null)
        {
            if (!EditorUtility.DisplayDialog("文件已存在", $"战役 ID \"{campaignId}\" 已存在，是否覆盖？", "覆盖", "取消"))
            {
                return null;
            }
            AssetDatabase.DeleteAsset(path);
        }

        // 创建配置
        var config = ScriptableObject.CreateInstance<CampaignConfig>();
        config.CampaignId = campaignId;
        config.CampaignName = campaignName;

        // 应用模板参数
        ApplyTemplateParams(config, templateType);

        // 尝试加载默认资源
        LoadDefaultResources(config, templateType);

        // 保存资产
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CampaignTemplateFactory] 从模板 {templateType} 创建战役: {campaignId}");

        return config;
    }

    /// <summary>
    /// 获取模板的默认名称
    /// </summary>
    public static string GetTemplateName(CampaignTemplateType templateType)
    {
        switch (templateType)
        {
            case CampaignTemplateType.Tutorial: return "教学关";
            case CampaignTemplateType.Standard: return "标准关";
            case CampaignTemplateType.Challenge: return "挑战关";
            case CampaignTemplateType.Sandbox: return "沙盒关";
            default: return "新战役";
        }
    }

    /// <summary>
    /// 获取模板描述
    /// </summary>
    public static string GetTemplateDescription(CampaignTemplateType templateType)
    {
        switch (templateType)
        {
            case CampaignTemplateType.Tutorial:
                return "教学关模板\n- 10 回合\n- 800 初始现金\n- 3 条战线\n- 老实人维克多 (Default)\n适合新手教学和简单体验";

            case CampaignTemplateType.Standard:
                return "标准关模板\n- 20 回合\n- 500 初始现金\n- 3 条战线\n- 精打细算维克多 (Balanced)\n适合正常游戏流程";

            case CampaignTemplateType.Challenge:
                return "挑战关模板\n- 30 回合\n- 300 初始现金\n- 3 条战线\n- 金融猎手维克多 (Hunter)\n适合高难度挑战";

            case CampaignTemplateType.Sandbox:
                return "沙盒关模板\n- 40 回合\n- 2000 初始现金\n- 5 条战线\n- 老实人维克多 (Default)\n适合自由探索和测试";

            default:
                return "未知模板";
        }
    }

    /// <summary>
    /// 应用模板参数
    /// </summary>
    private static void ApplyTemplateParams(CampaignConfig config, CampaignTemplateType templateType)
    {
        switch (templateType)
        {
            case CampaignTemplateType.Tutorial:
                ApplyTutorialTemplate(config);
                break;

            case CampaignTemplateType.Standard:
                ApplyStandardTemplate(config);
                break;

            case CampaignTemplateType.Challenge:
                ApplyChallengeTemplate(config);
                break;

            case CampaignTemplateType.Sandbox:
                ApplySandboxTemplate(config);
                break;
        }
    }

    /// <summary>
    /// 教学关模板
    /// </summary>
    private static void ApplyTutorialTemplate(CampaignConfig config)
    {
        config.Description = "教学关卡，帮助玩家熟悉游戏基本操作。";

        // 回合设置
        config.MaxTurns = 10;

        // 玩家初始状态
        config.InitialCash = 800f;
        config.InitialAtkInventory = 3;
        config.InitialDefInventory = 3;
        config.InitialRetInventory = 2;

        // 后备役
        config.InitialReserves = 80;

        // 战场设置
        config.LaneCount = 3;
        config.GridCount = 5;
        config.InitialFrontlinePosition = 3;

        // 维克多设置
        config.VictorInitialCash = 300f;

        // 门票
        config.TicketPrice = 0f;

        // 初始化战线分配
        InitializeFrontlineAssignments(config, 3);
    }

    /// <summary>
    /// 标准关模板
    /// </summary>
    private static void ApplyStandardTemplate(CampaignConfig config)
    {
        config.Description = "标准难度关卡，平衡的游戏体验。";

        // 回合设置
        config.MaxTurns = 20;

        // 玩家初始状态
        config.InitialCash = 500f;
        config.InitialAtkInventory = 2;
        config.InitialDefInventory = 2;
        config.InitialRetInventory = 2;

        // 后备役
        config.InitialReserves = 60;

        // 战场设置
        config.LaneCount = 3;
        config.GridCount = 5;
        config.InitialFrontlinePosition = 3;

        // 维克多设置
        config.VictorInitialCash = 500f;

        // 门票
        config.TicketPrice = 100f;

        // 初始化战线分配
        InitializeFrontlineAssignments(config, 3);
    }

    /// <summary>
    /// 挑战关模板
    /// </summary>
    private static void ApplyChallengeTemplate(CampaignConfig config)
    {
        config.Description = "高难度挑战关卡，适合有经验的玩家。";

        // 回合设置
        config.MaxTurns = 30;

        // 玩家初始状态
        config.InitialCash = 300f;
        config.InitialAtkInventory = 1;
        config.InitialDefInventory = 1;
        config.InitialRetInventory = 1;

        // 后备役
        config.InitialReserves = 40;

        // 战场设置
        config.LaneCount = 3;
        config.GridCount = 5;
        config.InitialFrontlinePosition = 3;

        // 维克多设置
        config.VictorInitialCash = 800f;

        // 门票
        config.TicketPrice = 200f;

        // 初始化战线分配
        InitializeFrontlineAssignments(config, 3);
    }

    /// <summary>
    /// 沙盒关模板
    /// </summary>
    private static void ApplySandboxTemplate(CampaignConfig config)
    {
        config.Description = "沙盒模式，充足的资源用于自由探索。";

        // 回合设置
        config.MaxTurns = 40;

        // 玩家初始状态
        config.InitialCash = 2000f;
        config.InitialAtkInventory = 5;
        config.InitialDefInventory = 5;
        config.InitialRetInventory = 5;

        // 后备役
        config.InitialReserves = 100;

        // 战场设置
        config.LaneCount = 5;
        config.GridCount = 5;
        config.InitialFrontlinePosition = 3;

        // 维克多设置
        config.VictorInitialCash = 1000f;

        // 门票
        config.TicketPrice = 50f;

        // 初始化战线分配
        InitializeFrontlineAssignments(config, 5);
    }

    /// <summary>
    /// 初始化战线分配数组
    /// </summary>
    private static void InitializeFrontlineAssignments(CampaignConfig config, int laneCount)
    {
        FrontlinePosition[] positions;

        switch (laneCount)
        {
            case 1:
                positions = new[] { FrontlinePosition.Center };
                break;
            case 2:
                positions = new[] { FrontlinePosition.Left, FrontlinePosition.Right };
                break;
            case 3:
                positions = new[] { FrontlinePosition.Left, FrontlinePosition.Center, FrontlinePosition.Right };
                break;
            case 4:
                positions = new[] { FrontlinePosition.Left, FrontlinePosition.LeftCenter, FrontlinePosition.RightCenter, FrontlinePosition.Right };
                break;
            case 5:
                positions = new[] { FrontlinePosition.Left, FrontlinePosition.LeftCenter, FrontlinePosition.Center, FrontlinePosition.RightCenter, FrontlinePosition.Right };
                break;
            default:
                positions = new[] { FrontlinePosition.Left, FrontlinePosition.Center, FrontlinePosition.Right };
                break;
        }

        // 己方
        config.AllyFrontlineAssignments = new FrontlineAssignment[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            config.AllyFrontlineAssignments[i] = new FrontlineAssignment
            {
                Position = positions[i],
                GeneralId = ""
            };
        }

        // 敌方
        config.EnemyFrontlineAssignments = new FrontlineAssignment[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            config.EnemyFrontlineAssignments[i] = new FrontlineAssignment
            {
                Position = positions[i],
                GeneralId = ""
            };
        }
    }

    /// <summary>
    /// 加载默认资源（维克多配置等）
    /// </summary>
    private static void LoadDefaultResources(CampaignConfig config, CampaignTemplateType templateType)
    {
        // 加载将军配置
        var generalConfig = AssetDatabase.LoadAssetAtPath<GeneralConfig>(ConfigPath + "GeneralConfig.asset");
        if (generalConfig != null)
        {
            config.GeneralConfig = generalConfig;
        }

        // 根据模板类型加载对应的维克多配置
        string victorProfileName;
        switch (templateType)
        {
            case CampaignTemplateType.Tutorial:
                victorProfileName = "VictorProfile_Default";
                break;
            case CampaignTemplateType.Standard:
                victorProfileName = "VictorProfile_Balanced";
                break;
            case CampaignTemplateType.Challenge:
                victorProfileName = "VictorProfile_Hunter";
                break;
            case CampaignTemplateType.Sandbox:
                victorProfileName = "VictorProfile_Default";
                break;
            default:
                victorProfileName = "VictorProfile_Default";
                break;
        }

        var victorProfile = AssetDatabase.LoadAssetAtPath<VictorProfile>(ConfigPath + victorProfileName + ".asset");
        if (victorProfile != null)
        {
            config.VictorProfile = victorProfile;
        }
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
