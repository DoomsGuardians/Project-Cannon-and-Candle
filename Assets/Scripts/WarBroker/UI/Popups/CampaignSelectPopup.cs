using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 战役选择弹窗
/// 展示可用战役列表，选择后进入对应战役
/// </summary>
public class CampaignSelectPopup : WindowBase
{
    public new UILayer uiLayer = UILayer.Top;
    public new bool IsFullScreen = false;

    private Transform itemContainer;
    private GameObject itemPrefab;
    private Button btnClose;
    private TMP_Text txtTitle;

    private CampaignListConfig campaignListConfig;
    private CampaignConfig selectedCampaign;
    private TooltipPanel tooltipPanel;

    public override void OnAwake()
    {
        base.OnAwake();

        // 加载战役列表配置
        campaignListConfig = resService.LoadResource<CampaignListConfig>(ConfigPaths.CAMPAIGN_LIST);

        var binder = gameObject.GetComponent<CampaignSelectPopupBinder>();
        if (binder != null)
        {
            itemContainer = binder.itemContainer;
            itemPrefab = binder.itemPrefab;
            btnClose = binder.btnClose;
            txtTitle = binder.txtTitle;
        }
    }

    public override void OnShow()
    {
        AddButtonListener(btnClose, OnClose);
        tooltipPanel = uIService.GetWindow<TooltipPanel>("TooltipPanel");
        RefreshUI();
    }

    public override void OnHide()
    {
        RemoveAllButtonListener();
        ClearItems();
        tooltipPanel?.Hide();
    }

    private void RefreshUI()
    {
        if (txtTitle != null)
            txtTitle.text = "选择战役";

        ClearItems();
        CreateCampaignItems();
    }

    private void ClearItems()
    {
        if (itemContainer == null) return;

        for (int i = itemContainer.childCount - 1; i >= 0; i--)
        {
            var child = itemContainer.GetChild(i);
            if (child.gameObject != itemPrefab)
            {
                Object.Destroy(child.gameObject);
            }
        }
    }

    private void CreateCampaignItems()
    {
        if (campaignListConfig == null || itemContainer == null || itemPrefab == null)
            return;

        foreach (var campaign in campaignListConfig.Campaigns)
        {
            if (campaign == null) continue;

            var itemObj = Object.Instantiate(itemPrefab, itemContainer);
            itemObj.SetActive(true);

            var itemBinder = itemObj.GetComponent<CampaignItemBinder>();
            if (itemBinder != null)
            {
                // 设置显示内容
                if (itemBinder.txtName != null)
                    itemBinder.txtName.text = campaign.CampaignName;

                if (itemBinder.txtTicket != null)
                    itemBinder.txtTicket.text = $"门票: {campaign.TicketPrice:F0}";

                if (itemBinder.imgThumbnail != null && campaign.Thumbnail != null)
                    itemBinder.imgThumbnail.sprite = campaign.Thumbnail;

                // 绑定点击事件
                if (itemBinder.btnSelect != null)
                {
                    var config = campaign; // 闭包捕获
                    itemBinder.btnSelect.onClick.AddListener(() => OnSelectCampaign(config));

                    // 添加 Tooltip 事件
                    AddTooltipEvents(itemBinder.btnSelect.gameObject, config.CampaignName, config.Description);
                }
            }
        }

        // 隐藏模板
        if (itemPrefab != null)
            itemPrefab.SetActive(false);
    }

    private void AddTooltipEvents(GameObject target, string title, string content)
    {
        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        // PointerEnter
        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) => tooltipPanel?.Show(title, content));
        trigger.triggers.Add(enterEntry);

        // PointerExit
        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) => tooltipPanel?.Hide());
        trigger.triggers.Add(exitEntry);
    }

    private void OnSelectCampaign(CampaignConfig campaign)
    {
        if (campaign == null) return;

        selectedCampaign = campaign;
        tooltipPanel?.Hide();

        // 保存选中的战役配置到全局，供后续使用
        CampaignSelectManager.SelectedCampaign = campaign;

        // 加载战役场景
        if (!string.IsNullOrEmpty(campaign.BattlefieldSceneName))
        {
            // 通过 StageSystem 加载场景，确保正确的初始化流程
            GameRoot.Instance.stageSystem.LoadScene(campaign.BattlefieldSceneName, GameMode.GamePlay);
        }
        else
        {
            Debug.LogWarning($"[CampaignSelectPopup] Campaign {campaign.CampaignId} has no scene configured.");
        }
    }

    private void OnClose()
    {
        uIService.HideWindow(Name);
    }
}

/// <summary>
/// 战役选择管理器 - 保存当前选中的战役配置
/// </summary>
public static class CampaignSelectManager
{
    public static CampaignConfig SelectedCampaign { get; set; }
}
