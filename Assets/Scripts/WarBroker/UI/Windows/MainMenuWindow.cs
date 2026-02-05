using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主菜单窗口：开始游戏、观战模式、档案、设置、退出
/// </summary>
public class MainMenuWindow : WindowBase
{
    private Button btnStart, btnSpectator, btnSettings, btnArchive, btnQuit;
    private TMP_Text txtTitle, txtVersion;

    public override void OnAwake()
    {
        base.OnAwake();
        uiLayer = UILayer.Normal;

        var binder = gameObject.GetComponent<MainMenuWindowBinder>();
        if (binder != null)
        {
            btnStart = binder.btnStart;
            btnSpectator = binder.btnSpectator;
            btnSettings = binder.btnSettings;
            btnArchive = binder.btnArchive;
            btnQuit = binder.btnQuit;
            txtTitle = binder.txtTitle;
            txtVersion = binder.txtVersion;
        }

        if (txtVersion != null)
        {
            txtVersion.text = $"v{Application.version}";
        }
    }

    public override void OnShow()
    {
        AddButtonListener(btnStart, OnStartGame);
        AddButtonListener(btnSpectator, OnSpectatorMode);
        AddButtonListener(btnSettings, OnSettings);
        AddButtonListener(btnArchive, OnArchive);
        AddButtonListener(btnQuit, OnQuit);
    }

    public override void OnHide()
    {
        RemoveAllButtonListener();
    }

    private void OnStartGame()
    {
        uIService.ShowWindow<CampaignSelectPopup>("CampaignSelectPopup");
    }

    private void OnSpectatorMode()
    {
        // 加载观战模式关卡（需要在 GameStageConfig 中配置 stageID=3, gameMode=Spectator）
        GameRoot.Instance.stageSystem.LoadStage(3);
    }

    private void OnSettings()
    {
        uIService.ShowWindow<SettingsWindow>("SettingsWindow");
    }

    private void OnArchive()
    {
        uIService.ShowWindow<ArchivePopup>("ArchivePopup");
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
