using UnityEngine;

/// <summary>
/// 暂停系统：管理游戏暂停状态
/// 简化版：只在玩家回合（MarketPhase）允许暂停
/// </summary>
public class PauseSystem : ILogic
{
    private EventService eventService;
    private UIService uiService;
    private CampaignSystem campaignSystem;

    public bool IsPaused { get; private set; }

    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        uiService = GameRoot.Instance.uIService;
        campaignSystem = GameRoot.Instance.campaignSystem;
    }

    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }

    /// <summary>检查当前是否可以暂停</summary>
    public bool CanPause()
    {
        // 已经暂停则不能再暂停
        if (IsPaused) return false;

        // 只在玩家回合（MarketPhase）允许暂停
        if (campaignSystem?.Data == null) return true; // 非战役场景允许暂停
        return campaignSystem.Data.CurrentPhase == TurnPhase.MarketPhase;
    }

    /// <summary>切换暂停状态</summary>
    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
        }
        else if (CanPause())
        {
            Pause();
        }
    }

    /// <summary>暂停游戏</summary>
    public void Pause()
    {
        if (IsPaused) return;
        if (!CanPause()) return;

        IsPaused = true;

        // 显示暂停面板
        uiService.ShowWindow<PausePopup>("PausePopup");

        // 广播暂停事件
        eventService.SendMessage((EventID)WarBrokerEventID.OnGamePaused, null, null);

        Debug.Log("[PauseSystem] Game paused.");
    }

    /// <summary>恢复游戏</summary>
    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;

        // 隐藏暂停面板
        uiService.HideWindow("PausePopup");

        // 广播恢复事件
        eventService.SendMessage((EventID)WarBrokerEventID.OnGameResumed, null, null);

        Debug.Log("[PauseSystem] Game resumed.");
    }

    /// <summary>返回主菜单</summary>
    public void ReturnToMainMenu()
    {
        // 先隐藏暂停面板
        uiService.HideWindow("PausePopup");
        IsPaused = false;

        // 切换到主菜单模式
        GameRoot.Instance.ChangeGameMode(GameMode.MainMenu);

        // 主菜单模式启动后显示主菜单窗口
        GameRoot.Instance.currentGameMode?.StartGame();
    }

    /// <summary>退出游戏</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
