using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 通用通知弹窗
/// 用于显示强平、费用扣除等重要信息
/// </summary>
public class NotificationPopup : WindowBase
{
    public new UILayer uiLayer = UILayer.Top;
    public new bool IsFullScreen = false;

    private TMP_Text txtTitle;
    private TMP_Text txtContent;
    private Button btnConfirm;
    private TMP_Text txtBtnConfirm;

    private string title;
    private string content;

    public override void OnAwake()
    {
        base.OnAwake();

        var binder = gameObject.GetComponent<NotificationPopupBinder>();
        if (binder != null)
        {
            txtTitle = binder.txtTitle;
            txtContent = binder.txtContent;
            btnConfirm = binder.btnConfirm;
            txtBtnConfirm = binder.txtBtnConfirm;
        }
    }

    public override void OnShow()
    {
        InputRouter.Acquire(InputChannel.Gameplay, this);
        AddButtonListener(btnConfirm, OnConfirm);
        RefreshUI();
    }

    public override void OnHide()
    {
        InputRouter.Release(InputChannel.Gameplay, this);
    }

    /// <summary>设置通知内容</summary>
    public void SetNotification(string title, string content, string buttonText = "了解")
    {
        this.title = title;
        this.content = content;

        if (txtTitle != null)
            txtTitle.text = title;
        if (txtContent != null)
            txtContent.text = content;
        if (txtBtnConfirm != null)
            txtBtnConfirm.text = buttonText;
    }

    private void RefreshUI()
    {
        if (txtTitle != null)
            txtTitle.text = title ?? "";
        if (txtContent != null)
            txtContent.text = content ?? "";
    }

    private void OnConfirm()
    {
        uIService.HideWindow(Name);
    }
}
