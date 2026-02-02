using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 暂停弹窗
/// 显示暂停菜单：继续、设置、返回主菜单、退出
/// </summary>
public class PausePopup : WindowBase
{
    public new UILayer uiLayer = UILayer.Top;
    public new bool IsFullScreen = false;

    private TMP_Text txtTitle;
    private Button btnResume;
    private Button btnSettings;
    private Button btnMainMenu;
    private Button btnQuit;

    private PauseSystem pauseSystem;
    private InputService inputService;

    public override void OnAwake()
    {
        base.OnAwake();

        var binder = gameObject.GetComponent<PausePopupBinder>();
        if (binder != null)
        {
            txtTitle = binder.txtTitle;
            btnResume = binder.btnResume;
            btnSettings = binder.btnSettings;
            btnMainMenu = binder.btnMainMenu;
            btnQuit = binder.btnQuit;
        }

        pauseSystem = GameRoot.Instance.pauseSystem;
        inputService = GameRoot.Instance.inputService;
    }

    public override void OnShow()
    {
        // 获取输入锁
        InputRouter.Acquire(InputChannel.Gameplay, this);

        AddButtonListener(btnResume, OnResumeClick);
        AddButtonListener(btnSettings, OnSettingsClick);
        AddButtonListener(btnMainMenu, OnMainMenuClick);
        AddButtonListener(btnQuit, OnQuitClick);

        if (txtTitle != null)
        {
            txtTitle.text = "游戏暂停";
        }

        // 注册 Update 监听 Cancel 输入
        GameRoot.Instance.AddUpdateAction(HandleUpdate);
    }

    public override void OnHide()
    {
        // 移除 Update 监听
        GameRoot.Instance.RemoveUpdateAction(HandleUpdate);

        // 释放输入锁
        InputRouter.Release(InputChannel.Gameplay, this);

        eventService.RemoveEventListeningByTarget(this);
    }

    private void HandleUpdate()
    {
        // ESC 键关闭暂停面板
        if (inputService != null && inputService.CancelPressed)
        {
            OnResumeClick();
        }
    }

    private void OnResumeClick()
    {
        pauseSystem?.Resume();
    }

    private void OnSettingsClick()
    {
        uIService.ShowWindow<SettingsWindow>("SettingsWindow");
    }

    private void OnMainMenuClick()
    {
        pauseSystem?.ReturnToMainMenu();
    }

    private void OnQuitClick()
    {
        pauseSystem?.QuitGame();
    }
}
