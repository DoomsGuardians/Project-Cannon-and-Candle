using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 随机事件弹窗
/// 显示随机事件的标题、描述和效果
/// </summary>
public class EventPopup : WindowBase
{
    public new UILayer uiLayer = UILayer.Top;
    public new bool IsFullScreen = false;

    private Text txtTitle;
    private Text txtDescription;
    private Text txtEffects;
    private Button btnConfirm;

    private RandomEventConfig eventConfig;

    public override void OnAwake()
    {
        base.OnAwake();

        var binder = gameObject.GetComponent<EventPopupBinder>();
        if (binder != null)
        {
            txtTitle = binder.txtTitle;
            txtDescription = binder.txtDescription;
            txtEffects = binder.txtEffects;
            btnConfirm = binder.btnConfirm;
        }
    }

    public override void OnShow()
    {
        // 获取输入锁
        InputRouter.Acquire(InputChannel.Gameplay, this);

        AddButtonListener(btnConfirm, OnConfirm);

        RefreshUI();
    }

    public override void OnHide()
    {
        // 释放输入锁
        InputRouter.Release(InputChannel.Gameplay, this);

        eventService.RemoveEventListeningByTarget(this);
    }

    /// <summary>设置事件数据</summary>
    public void SetEventData(RandomEventConfig config)
    {
        eventConfig = config;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (eventConfig == null) return;

        if (txtTitle != null)
            txtTitle.text = eventConfig.EventName;

        if (txtDescription != null)
            txtDescription.text = eventConfig.Description;

        if (txtEffects != null)
            txtEffects.text = BuildEffectsText();
    }

    private string BuildEffectsText()
    {
        if (eventConfig == null) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("【事件效果】");

        if (Mathf.Abs(eventConfig.ProductionModifier) > 0.001f)
            sb.AppendLine($"产能: {eventConfig.ProductionModifier:+0.#%;-0.#%}");

        if (Mathf.Abs(eventConfig.AtkDemandModifier) > 0.001f)
            sb.AppendLine($"ATK需求: {eventConfig.AtkDemandModifier:+0.#%;-0.#%}");

        if (Mathf.Abs(eventConfig.DefDemandModifier) > 0.001f)
            sb.AppendLine($"DEF需求: {eventConfig.DefDemandModifier:+0.#%;-0.#%}");

        if (Mathf.Abs(eventConfig.RetDemandModifier) > 0.001f)
            sb.AppendLine($"RET需求: {eventConfig.RetDemandModifier:+0.#%;-0.#%}");

        if (eventConfig.AllTroopChange != 0)
            sb.AppendLine($"全军兵力: {eventConfig.AllTroopChange:+#;-#}");

        if (eventConfig.RandomTrustChange != 0)
            sb.AppendLine($"随机将军信任: {eventConfig.RandomTrustChange:+#;-#}");

        if (eventConfig.Duration > 1)
            sb.AppendLine($"持续: {eventConfig.Duration} 回合");

        return sb.ToString();
    }

    private void OnConfirm()
    {
        uIService.HideWindow(Name);
    }
}
