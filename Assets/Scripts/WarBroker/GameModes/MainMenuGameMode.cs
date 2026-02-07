using UnityEngine;

/// <summary>
/// 主菜单游戏模式：显示主菜单界面
/// </summary>
public class MainMenuGameMode : GameModeBase
{
    public MainMenuGameMode() : base(GameMode.MainMenu) { }

    public override void EnterGameMode()
    {
        EnsureUILayerSystem();
        RegisterWindow<MainMenuWindow>("Prefabs/WarBroker/UI/Windows/MainMenuWindow", "MainMenuWindow");
        RegisterWindow<SettingsWindow>("Prefabs/WarBroker/UI/Windows/SettingsWindow", "SettingsWindow");
        RegisterWindow<CampaignSelectPopup>("Prefabs/WarBroker/UI/Popups/CampaignSelectPopup", "CampaignSelectPopup");
        RegisterWindow<ArchivePopup>("Prefabs/WarBroker/UI/Popups/ArchivePopup", "ArchivePopup");
        RegisterWindow<NotificationPopup>("Prefabs/WarBroker/UI/Popups/NotificationPopup", "NotificationPopup");
        RegisterWindow<TooltipPanel>("Prefabs/WarBroker/UI/Panels/TooltipPanel", "TooltipPanel");
    }

    public override void StartGame()
    {
        base.StartGame();
        uIService.ShowWindow<MainMenuWindow>("MainMenuWindow");

        // 播放主题曲
        PlayTitleTheme();
    }

    private void PlayTitleTheme()
    {
        var musicConfig = resService.LoadResource<MusicConfig>(ConfigPaths.MUSIC_CONFIG);
        if (musicConfig?.TitleTheme != null)
        {
            GameRoot.Instance.audioService.PlayBGM(musicConfig.TitleTheme);
        }
    }

    public override void OnUpdate() { }

    public override void UnOnInit()
    {
        uIService.ClearAllWindows();
    }

    private void RegisterWindow<T>(string prefabPath, string windowName) where T : WindowBase, new()
    {
        var prefab = resService.LoadResource<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[MainMenuGameMode] UI prefab not found: {prefabPath}");
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

            // 确保RectTransform正确拉伸以适应不同分辨率
            var rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }
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
