using UnityEngine;

/// <summary>
/// 战斗游戏模式：初始化 Manager 和 UI 窗口
/// </summary>
public class BattleGameMode : GameModeBase
{
    private BattlefieldSceneController battlefieldController;

    public BattleGameMode() : base(GameMode.GamePlay) { }

    public override void EnterGameMode()
    {
        EnsureUILayerSystem();

        // 实例化 GameplayManager
        var prefab = resService.LoadResource<GameObject>("Prefabs/WarBroker/GameplayManager");
        if (prefab != null)
        {
            var obj = GameObject.Instantiate(prefab);
            var manager = obj.GetComponent<GameplayManager>();
            managerService.RegisterManager(manager);
        }
        else
        {
            Debug.LogError("[BattleGameMode] GameplayManager prefab not found!");
        }

        // 实例化 MusicPlayerManager
        var musicObj = new GameObject("MusicPlayerManager");
        var musicManager = musicObj.AddComponent<MusicPlayerManager>();
        managerService.RegisterManager(musicManager);

        // 注册所有 UI 窗口
        var stageConfig = GameRoot.Instance.stageSystem?.currentStageConfig;
        if (stageConfig != null && stageConfig.UIWindowBase != null)
        {
            RegisterWindow<GameplayWindow>(stageConfig.UIWindowBase, "GameplayWindow");
        }
        else
        {
            RegisterWindow<GameplayWindow>("Prefabs/WarBroker/UI/Windows/GamePlayWindow", "GameplayWindow");
        }
        RegisterWindow<MarketPanel>("Prefabs/WarBroker/UI/Panels/MarketPanel", "MarketPanel");
        RegisterWindow<InfoPanel>("Prefabs/WarBroker/UI/Panels/InfoPanel", "InfoPanel");
        RegisterWindow<ObjectivePanel>("Prefabs/WarBroker/UI/Panels/ObjectivePanel", "ObjectivePanel");
        RegisterWindow<TooltipPanel>("Prefabs/WarBroker/UI/Panels/TooltipPanel", "TooltipPanel");
        RegisterWindow<GeneralDetailPanel>("Prefabs/WarBroker/UI/Panels/GeneralDetailPanel", "GeneralDetailPanel");
        RegisterWindow<CampaignEndPopup>("Prefabs/WarBroker/UI/Popups/CampaignEndPopup", "CampaignEndPopup");
        RegisterWindow<EventPopup>("Prefabs/WarBroker/UI/Popups/EventPopup", "EventPopup");
        RegisterWindow<BattleResultPopup>("Prefabs/WarBroker/UI/Popups/BattleResultPopup", "BattleResultPopup");
        RegisterWindow<PausePopup>("Prefabs/WarBroker/UI/Popups/PausePopup", "PausePopup");
        RegisterWindow<SettingsWindow>("Prefabs/WarBroker/UI/Windows/SettingsWindow", "SettingsWindow");

        // 初始化3D战场
        InitBattlefield();

        // 注册 Update 回调用于监听 ESC 键
        GameRoot.Instance.AddUpdateAction(HandleEscapeKey);
    }

    private void InitBattlefield()
    {
        // 查找场景中的BattlefieldSceneController
        battlefieldController = GameObject.FindObjectOfType<BattlefieldSceneController>();
        if (battlefieldController == null)
        {
            // 如果场景中没有，尝试从prefab实例化
            var prefab = resService.LoadResource<GameObject>("Prefabs/WarBroker/Battlefield/BattlefieldSceneController");
            if (prefab != null)
            {
                var obj = GameObject.Instantiate(prefab);
                obj.name = "BattlefieldSceneController";
                battlefieldController = obj.GetComponent<BattlefieldSceneController>();
            }
            else
            {
                Debug.LogWarning("[BattleGameMode] BattlefieldSceneController not found in scene or prefab.");
            }
        }
    }

    public override void StartGame()
    {
        base.StartGame();
        var gameplayManager = managerService.GetManager<GameplayManager>();
        if (gameplayManager != null)
        {
            gameplayManager.BeginGame();

            // 初始化3D战场数据
            if (battlefieldController != null)
            {
                var battleData = gameplayManager.GetBattleData();
                if (battleData != null)
                {
                    battlefieldController.Initialize(battleData);
                }
            }
        }
    }

    public override void OnUpdate() { }

    public override void UnOnInit()
    {
        // 移除 Update 回调
        GameRoot.Instance.RemoveUpdateAction(HandleEscapeKey);

        // 清理战场
        if (battlefieldController != null)
        {
            battlefieldController.Cleanup();
        }

        // 清理所有Manager
        managerService.ClearAllManagers();

        // 清理所有UI窗口
        uIService.ClearAllWindows();
    }

    /// <summary>处理 ESC 键暂停</summary>
    private void HandleEscapeKey()
    {
        var inputService = GameRoot.Instance.inputService;
        if (inputService != null && inputService.CancelPressed)
        {
            var pauseSystem = GameRoot.Instance.pauseSystem;
            if (pauseSystem != null)
            {
                pauseSystem.TogglePause();
            }
        }
    }

    private void RegisterWindow<T>(string prefabPath, string windowName) where T : WindowBase, new()
    {
        var prefab = resService.LoadResource<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[BattleGameMode] UI prefab not found: {prefabPath}");
            return;
        }

        RegisterWindow<T>(prefab, windowName);
    }

    private void RegisterWindow<T>(GameObject prefab, string windowName) where T : WindowBase, new()
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[BattleGameMode] UI prefab not found for: {windowName}");
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
