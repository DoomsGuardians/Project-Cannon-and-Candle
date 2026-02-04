using UnityEngine;

/// <summary>
/// 观战游戏模式：AI 对战观战
/// </summary>
public class SpectatorGameMode : GameModeBase
{
    private SpectatorBattleSystem battleSystem;
    private SpectatorModeConfig config;
    private BattlefieldSceneController battlefieldController;

    public SpectatorGameMode() : base(GameMode.Spectator) { }

    /// <summary>获取战斗系统</summary>
    public SpectatorBattleSystem BattleSystem => battleSystem;

    public override void EnterGameMode()
    {
        EnsureUILayerSystem();

        // 加载配置
        config = resService.LoadResource<SpectatorModeConfig>(ConfigPaths.SPECTATOR_MODE);
        if (config == null)
        {
            Debug.LogError("[SpectatorGameMode] SpectatorModeConfig not found!");
            return;
        }

        // 注册 UI 窗口
        RegisterWindow<SpectatorWindow>("Prefabs/WarBroker/UI/Windows/SpectatorWindow", "SpectatorWindow");
        RegisterWindow<TooltipPanel>("Prefabs/WarBroker/UI/Panels/TooltipPanel", "TooltipPanel");

        // 初始化 3D 战场（可选）
        if (config.ShowBattleAnimations)
        {
            InitBattlefield();
        }

        // 注册 Update 回调
        GameRoot.Instance.AddUpdateAction(OnUpdateInternal);
    }

    public override void StartGame()
    {
        base.StartGame();

        // 初始化战斗系统（在 StartGame 中初始化，确保场景已加载）
        battleSystem = new SpectatorBattleSystem();

        // 获取战役 ID（从 SpectatorModeConfig 的 BaseCampaignConfig 中获取）
        string campaignId = "Campaign_Tutorial"; // 默认值
        if (config.BaseCampaignConfig != null)
        {
            campaignId = config.BaseCampaignConfig.name;
        }

        battleSystem.Initialize(config, campaignId);

        // 显示观战窗口
        uIService.ShowWindow<SpectatorWindow>("SpectatorWindow");

        // 初始化 3D 战场数据
        if (battlefieldController != null && battleSystem?.Data != null)
        {
            var battleData = battleSystem.Data.Battle;
            if (battleData != null)
            {
                battlefieldController.Initialize(battleData);
            }
        }

        // 不自动开始，等待用户点击"开始对战"按钮
        // battleSystem.StartBattle() 由 SpectatorWindow 的按钮触发
    }

    public override void OnUpdate()
    {
        // 由 OnUpdateInternal 处理
    }

    private void OnUpdateInternal()
    {
        // 更新战斗系统
        battleSystem?.OnUpdate();

        // 处理 ESC 键
        HandleEscapeKey();
    }

    public override void UnOnInit()
    {
        // 移除 Update 回调
        GameRoot.Instance.RemoveUpdateAction(OnUpdateInternal);

        // 清理战斗系统
        battleSystem?.Cleanup();
        battleSystem = null;

        // 清理战场
        if (battlefieldController != null)
        {
            battlefieldController.Cleanup();
        }

        // 清理所有 Manager
        managerService.ClearAllManagers();

        // 清理所有 UI 窗口
        uIService.ClearAllWindows();
    }

    private void InitBattlefield()
    {
        // 查找场景中的 BattlefieldSceneController
        battlefieldController = GameObject.FindObjectOfType<BattlefieldSceneController>();
        if (battlefieldController == null)
        {
            // 如果场景中没有，尝试从 prefab 实例化
            var prefab = resService.LoadResource<GameObject>("Prefabs/WarBroker/Battlefield/BattlefieldSceneController");
            if (prefab != null)
            {
                var obj = GameObject.Instantiate(prefab);
                obj.name = "BattlefieldSceneController";
                battlefieldController = obj.GetComponent<BattlefieldSceneController>();
            }
            else
            {
                Debug.LogWarning("[SpectatorGameMode] BattlefieldSceneController not found in scene or prefab.");
            }
        }
    }

    private void HandleEscapeKey()
    {
        var inputService = GameRoot.Instance.inputService;
        if (inputService != null && inputService.CancelPressed)
        {
            // ESC 键切换暂停
            battleSystem?.TogglePause();
        }
    }

    private void RegisterWindow<T>(string prefabPath, string windowName) where T : WindowBase, new()
    {
        var prefab = resService.LoadResource<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[SpectatorGameMode] UI prefab not found: {prefabPath}");
            return;
        }

        RegisterWindow<T>(prefab, windowName);
    }

    private void RegisterWindow<T>(GameObject prefab, string windowName) where T : WindowBase, new()
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[SpectatorGameMode] UI prefab not found for: {windowName}");
            return;
        }

        var obj = GameObject.Instantiate(prefab);
        obj.name = prefab.name;
        obj.SetActive(false);
        var window = new T();
        window.gameObject = obj;
        window.transform = obj.transform;
        window.Name = windowName;

        uIService.RegisterWindow(windowName, window);

        var layerRoot = uIService.GetLayerRoot(window.uiLayer);
        if (layerRoot != null)
        {
            window.transform.SetParent(layerRoot, false);
        }
    }

    private void EnsureUILayerSystem()
    {
        if (uIService.GetLayerRoot(UILayer.Normal) != null)
        {
            return;
        }

        Transform uiRoot = GameObject.Find("UIRoot")?.transform;
        if (uiRoot == null)
        {
            var rootObject = new GameObject("UIRoot");
            rootObject.transform.SetParent(GameRoot.Instance.transform, false);
            uiRoot = rootObject.transform;
        }

        Camera uiCamera = uiRoot.GetComponentInChildren<Camera>();
        uIService.InitLayerSystem(uiRoot, uiCamera);
    }
}
