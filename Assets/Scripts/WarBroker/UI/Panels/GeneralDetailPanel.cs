using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 将军详情面板（侧边栏）
/// 显示将军属性、意图、强化/篡改操作
/// </summary>
public class GeneralDetailPanel : WindowBase
{
    public new UILayer uiLayer = UILayer.Normal;
    public new bool IsFullScreen = false;

    [Header("将军信息")]
    private TMP_Text txtName;
    private TMP_Text txtPersonality;
    private TMP_Text txtPosition;

    [Header("属性条")]
    private Slider sliderHP;
    private Slider sliderTrust;
    private Slider sliderMorale;
    private TMP_Text txtHP;
    private TMP_Text txtTrust;
    private TMP_Text txtMorale;

    [Header("状态")]
    private TMP_Text txtStatus;
    private TMP_Text txtSkills;

    [Header("意图显示")]
    private Image imgIntentBubble;
    private TMP_Text txtIntent;
    private TMP_Text txtIntentSource;

    [Header("操作按钮")]
    private Button btnReinforce;
    private TMP_Text txtReinforceCost;
    private Button btnOverride;
    private TMP_Dropdown ddOverrideType;
    private TMP_Text txtOverrideCost;

    [Header("关闭按钮")]
    private Button btnClose;

    private GeneralData currentGeneral;
    private IntentSystem intentSystem;
    private GameplayManager gameplayManager;
    private GameBalanceConfig balanceConfig;

    // 意图气泡颜色
    private readonly Color defaultIntentColor = Color.gray;
    private readonly Color reinforcedIntentColor = new Color(1f, 0.84f, 0f); // 金色
    private readonly Color overriddenIntentColor = Color.red;

    public override void OnAwake()
    {
        base.OnAwake();

        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);

        // 获取 IntentSystem
        if (GameRoot.Instance.campaignSystem != null)
        {
            intentSystem = GameRoot.Instance.campaignSystem.GetIntentSystem();
        }

        var b = gameObject.GetComponent<GeneralDetailPanelBinder>();
        if (b != null)
        {
            // 将军信息
            txtName = b.txtName;
            txtPersonality = b.txtPersonality;
            txtPosition = b.txtPosition;

            // 属性条
            sliderHP = b.sliderHP;
            sliderTrust = b.sliderTrust;
            sliderMorale = b.sliderMorale;
            txtHP = b.txtHP;
            txtTrust = b.txtTrust;
            txtMorale = b.txtMorale;

            // 状态
            txtStatus = b.txtStatus;
            txtSkills = b.txtSkills;

            // 意图
            imgIntentBubble = b.imgIntentBubble;
            txtIntent = b.txtIntent;
            txtIntentSource = b.txtIntentSource;

            // 操作
            btnReinforce = b.btnReinforce;
            txtReinforceCost = b.txtReinforceCost;
            btnOverride = b.btnOverride;
            ddOverrideType = b.ddOverrideType;
            txtOverrideCost = b.txtOverrideCost;

            // 关闭
            btnClose = b.btnClose;
        }

        SetupDropdown();
    }

    private void SetupDropdown()
    {
        if (ddOverrideType != null)
        {
            ddOverrideType.ClearOptions();
            ddOverrideType.AddOptions(new List<string> { "ATK", "DEF", "RET" });
        }
    }

    public override void OnShow()
    {
        // 获取输入锁
        InputRouter.Acquire(InputChannel.Gameplay, this);

        AddButtonListener(btnReinforce, OnReinforce);
        AddButtonListener(btnOverride, OnOverride);
        AddButtonListener(btnClose, OnClose);

        // 监听意图变化事件
        eventService.AddEventListening((EventID)WarBrokerEventID.OnIntentChanged, OnIntentChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnDataChanged);

        RefreshUI();
    }

    public override void OnHide()
    {
        // 释放输入锁
        InputRouter.Release(InputChannel.Gameplay, this);

        eventService.RemoveEventListeningByTarget(this);
    }

    /// <summary>设置当前显示的将军</summary>
    public void SetGeneral(GeneralData general)
    {
        currentGeneral = general;
        RefreshUI();
    }

    private void OnReinforce()
    {
        if (currentGeneral == null || intentSystem == null) return;
        if (!currentGeneral.DefaultIntent.HasValue) return;

        var player = gameplayManager.GetCampaignData()?.Player;
        if (player == null) return;

        if (intentSystem.TryReinforce(currentGeneral, currentGeneral.DefaultIntent.Value, player))
        {
            RefreshUI();
            eventService.SendMessage((EventID)WarBrokerEventID.OnIntentChanged, currentGeneral.GeneralId, null);
        }
    }

    private void OnOverride()
    {
        if (currentGeneral == null || intentSystem == null) return;
        if (ddOverrideType == null) return;

        var targetType = (OrderType)ddOverrideType.value;
        var player = gameplayManager.GetCampaignData()?.Player;
        if (player == null) return;

        if (intentSystem.TryOverride(currentGeneral, targetType, player))
        {
            RefreshUI();
            eventService.SendMessage((EventID)WarBrokerEventID.OnIntentChanged, currentGeneral.GeneralId, null);
        }
    }

    private void OnClose()
    {
        uIService.HideWindow(Name);
    }

    private void RefreshUI()
    {
        if (currentGeneral == null) return;

        RefreshGeneralInfo();
        RefreshAttributes();
        RefreshIntent();
        RefreshButtons();
    }

    private void RefreshGeneralInfo()
    {
        if (txtName != null)
            txtName.text = currentGeneral.Name;

        if (txtPersonality != null)
            txtPersonality.text = GetPersonalityText(currentGeneral.Personality);

        if (txtPosition != null)
            txtPosition.text = GetPositionText(currentGeneral.Position);

        if (txtStatus != null)
            txtStatus.text = GetStatusText(currentGeneral.GetStatus(balanceConfig));

        if (txtSkills != null && currentGeneral.Skills != null)
        {
            var skillNames = currentGeneral.Skills.ConvertAll(s => s.SkillName);
            txtSkills.text = skillNames.Count > 0 ? string.Join(", ", skillNames) : "无技能";
        }
    }

    private void RefreshAttributes()
    {
        // HP (Troops)
        if (sliderHP != null)
        {
            sliderHP.maxValue = 20;
            sliderHP.value = currentGeneral.Troops;
        }
        if (txtHP != null)
            txtHP.text = $"兵力: {currentGeneral.Troops}/20";

        // Trust
        if (sliderTrust != null)
        {
            sliderTrust.maxValue = 100;
            sliderTrust.value = currentGeneral.Trust;
        }
        if (txtTrust != null)
            txtTrust.text = $"信任: {currentGeneral.Trust}/100";

        // Morale
        if (sliderMorale != null)
        {
            sliderMorale.maxValue = 100;
            sliderMorale.value = currentGeneral.Morale;
        }
        if (txtMorale != null)
            txtMorale.text = $"士气: {currentGeneral.Morale}/100";
    }

    private void RefreshIntent()
    {
        var intent = currentGeneral.FinalIntent ?? currentGeneral.DefaultIntent;

        if (txtIntent != null)
        {
            txtIntent.text = intent.HasValue ? intent.Value.ToString() : "未知";
        }

        if (imgIntentBubble != null)
        {
            imgIntentBubble.color = currentGeneral.IntentSource switch
            {
                IntentSource.Reinforced => reinforcedIntentColor,
                IntentSource.Overridden => overriddenIntentColor,
                _ => defaultIntentColor
            };
        }

        if (txtIntentSource != null)
        {
            txtIntentSource.text = currentGeneral.IntentSource switch
            {
                IntentSource.Reinforced => "已强化",
                IntentSource.Overridden => "已篡改",
                _ => "默认意图"
            };
        }
    }

    private void RefreshButtons()
    {
        var player = gameplayManager.GetCampaignData()?.Player;
        if (player == null) return;

        int reinforceCost = intentSystem?.GetReinforceCost() ?? 1;
        int overrideCost = intentSystem?.GetOverrideCost() ?? 3;

        // 强化按钮
        if (btnReinforce != null)
        {
            bool canReinforce = CanReinforce(player, reinforceCost);
            btnReinforce.interactable = canReinforce;
        }

        if (txtReinforceCost != null && currentGeneral.DefaultIntent.HasValue)
        {
            var intentType = currentGeneral.DefaultIntent.Value;
            int held = player.Inventory.GetValueOrDefault(intentType, 0);
            txtReinforceCost.text = $"消耗 {reinforceCost} {intentType} (持有: {held})";
        }

        // 篡改按钮
        if (btnOverride != null && ddOverrideType != null)
        {
            var targetType = (OrderType)ddOverrideType.value;
            bool canOverride = CanOverride(player, targetType, overrideCost);
            btnOverride.interactable = canOverride;
        }

        if (txtOverrideCost != null && ddOverrideType != null)
        {
            var targetType = (OrderType)ddOverrideType.value;
            int held = player.Inventory.GetValueOrDefault(targetType, 0);
            txtOverrideCost.text = $"消耗 {overrideCost} {targetType} (持有: {held})";
        }
    }

    private bool CanReinforce(PlayerData player, int cost)
    {
        if (currentGeneral == null || !currentGeneral.DefaultIntent.HasValue)
            return false;

        // 已经强化或篡改过
        if (currentGeneral.IntentSource != IntentSource.Default)
            return false;

        var intentType = currentGeneral.DefaultIntent.Value;
        return player.Inventory.GetValueOrDefault(intentType, 0) >= cost;
    }

    private bool CanOverride(PlayerData player, OrderType targetType, int cost)
    {
        if (currentGeneral == null)
            return false;

        // 已经篡改过
        if (currentGeneral.IntentSource == IntentSource.Overridden)
            return false;

        // 目标类型与默认意图相同
        if (currentGeneral.DefaultIntent.HasValue && currentGeneral.DefaultIntent.Value == targetType)
            return false;

        return player.Inventory.GetValueOrDefault(targetType, 0) >= cost;
    }

    private string GetPersonalityText(GeneralPersonality personality)
    {
        return personality switch
        {
            GeneralPersonality.Fanatic => "狂热型",
            GeneralPersonality.Conservative => "保守型",
            GeneralPersonality.Opportunist => "投机型",
            _ => personality.ToString()
        };
    }

    private string GetPositionText(FrontlinePosition position)
    {
        return position switch
        {
            FrontlinePosition.Left => "左翼",
            FrontlinePosition.Center => "中军",
            FrontlinePosition.Right => "右翼",
            _ => position.ToString()
        };
    }

    private string GetStatusText(GeneralStatus status)
    {
        return status switch
        {
            GeneralStatus.FullStrength => "满编",
            GeneralStatus.Healthy => "健康",
            GeneralStatus.Wounded => "受伤",
            GeneralStatus.Critical => "濒死",
            GeneralStatus.Routed => "溃败",
            _ => status.ToString()
        };
    }

    private void OnIntentChanged(object p1, object p2) => RefreshUI();
    private void OnDataChanged(object p1, object p2) => RefreshUI();
}
