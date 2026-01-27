using UnityEngine;

/// <summary>
/// 战斗游戏模式：初始化 Manager 和 UI 窗口
/// </summary>
public class BattleGameMode : GameModeBase
{
    public BattleGameMode() : base(GameMode.GamePlay) { }

    public override void EnterGameMode()
    {
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
        RegisterWindow<GameplayWindow>("Prefabs/WarBroker/UI/GameplayWindow", "GameplayWindow");
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

        var obj = GameObject.Instantiate(prefab);
        var window = new T();
        window.gameObject = obj;
        window.transform = obj.transform;
        window.Name = windowName;

        uIService.RegisterWindow(windowName, window);
    }
}
