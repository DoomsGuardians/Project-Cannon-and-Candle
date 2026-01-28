using UnityEngine;

/// <summary>
/// 战斗游戏模式：初始化 Manager 和 UI 窗口
/// </summary>
public class BattleGameMode : GameModeBase
{
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

        // 注册所有 UI 窗口
        var stageConfig = GameRoot.Instance.stageSystem?.currentStageConfig;
        if (stageConfig != null && stageConfig.UIWindowBase != null)
        {
            RegisterWindow<GameplayWindow>(stageConfig.UIWindowBase, "GameplayWindow");
        }
        else
        {
            RegisterWindow<GameplayWindow>("Prefabs/WarBroker/UI/GameplayWindow", "GameplayWindow");
        }
        RegisterWindow<MarketPanel>("Prefabs/WarBroker/UI/MarketPanel", "MarketPanel");
        RegisterWindow<BattlefieldPanel>("Prefabs/WarBroker/UI/BattlefieldPanel", "BattlefieldPanel");
        RegisterWindow<GeneralPanel>("Prefabs/WarBroker/UI/GeneralPanel", "GeneralPanel");
        RegisterWindow<IntelPanel>("Prefabs/WarBroker/UI/IntelPanel", "IntelPanel");
        RegisterWindow<HistoryPanel>("Prefabs/WarBroker/UI/HistoryPanel", "HistoryPanel");
        RegisterWindow<GameEndWindow>("Prefabs/WarBroker/UI/GameEndWindow", "GameEndWindow");
    }

    public override void StartGame()
    {
        base.StartGame();
        var gameplayManager = managerService.GetManager<GameplayManager>();
        if (gameplayManager != null)
        {
            gameplayManager.BeginGame();
        }
    }

    public override void OnUpdate() { }

    public override void UnOnInit()
    {
        managerService.OnSceneExit();
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
