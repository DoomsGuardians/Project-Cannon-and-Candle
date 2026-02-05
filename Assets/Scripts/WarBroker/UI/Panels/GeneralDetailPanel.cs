using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 将军详情面板（简化版）
/// 显示将军基本信息、HP、意图、强化/篡改操作
/// </summary>
public class GeneralDetailPanel : WindowBase
{
    public new UILayer uiLayer = UILayer.Normal;
    public new bool IsFullScreen = false;

    [Header("将军信息")]
    private Image imgPortrait;
    private TMP_Text txtName;
    private TMP_Text txtPersonality;
    private TMP_Text txtPosition;

    [Header("HP")]
    private Slider sliderHP;
    private TMP_Text txtHP;

    [Header("状态属性")]
    private TMP_Text txtStatus;
    private GameObject trustArea;
    private TMP_Text txtTrust;
    private GameObject moraleArea;
    private TMP_Text txtMorale;
    private GameObject gridPositionArea;
    private TMP_Text txtGridPosition;

    [Header("意图显示")]
    private GameObject intentArea;
    private TMP_Text txtIntent;

    [Header("强化操作")]
    private GameObject buttonArea;
    private Button btnReinforce;
    private Image imgReinforceOrder;

    [Header("篡改操作")]
    private Button btnOverrideATK;
    private Button btnOverrideDEF;
    private Button btnOverrideRET;

    [Header("关闭按钮")]
    private Button btnClose;

    private GeneralData currentGeneral;
    private IntentSystem intentSystem;
    private GameplayManager gameplayManager;
    private GameBalanceConfig balanceConfig;
    private UITextConfig textConfig;
    private OrderConfig orderConfig;
    private TooltipPanel tooltipPanel;
    private bool isFullMode = true;  // 是否显示完整模式（己方将军）

    public override void OnAwake()
    {
        base.OnAwake();

        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        textConfig = resService.LoadResource<UITextConfig>(ConfigPaths.UI_TEXT);
        orderConfig = resService.LoadResource<OrderConfig>(ConfigPaths.ORDER_CONFIG);

        // 获取 IntentSystem
        if (GameRoot.Instance.campaignSystem != null)
        {
            intentSystem = GameRoot.Instance.campaignSystem.GetIntentSystem();
        }

        var b = gameObject.GetComponent<GeneralDetailPanelBinder>();
        if (b != null)
        {
            // 将军信息
            imgPortrait = b.imgPortrait;
            txtName = b.txtName;
            txtPersonality = b.txtPersonality;
            txtPosition = b.txtPosition;

            // HP
            sliderHP = b.sliderHP;
            txtHP = b.txtHP;

            // 状态属性
            // 状态属性
            txtStatus = b.txtStatus;
            trustArea = b.trustArea;
            txtTrust = b.txtTrust;
            moraleArea = b.moraleArea;
            txtMorale = b.txtMorale;
            gridPositionArea = b.gridPositionArea;
            txtGridPosition = b.txtGridPosition;

            // 意图
            intentArea = b.intentArea;
            txtIntent = b.txtIntent;

            // 强化操作
            buttonArea = b.buttonArea;
            btnReinforce = b.btnReinforce;
            imgReinforceOrder = b.imgReinforceOrder;

            // 篡改操作
            btnOverrideATK = b.btnOverrideATK;
            btnOverrideDEF = b.btnOverrideDEF;
            btnOverrideRET = b.btnOverrideRET;

            // 关闭
            btnClose = b.btnClose;
        }
    }

    public override void OnShow()
    {
        AddButtonListener(btnReinforce, OnReinforce);
        AddButtonListener(btnOverrideATK, () => OnOverride(OrderType.ATK));
        AddButtonListener(btnOverrideDEF, () => OnOverride(OrderType.DEF));
        AddButtonListener(btnOverrideRET, () => OnOverride(OrderType.RET));
        AddButtonListener(btnClose, OnClose);

        // 添加按钮悬停事件（显示指令预览变化）
        AddButtonHoverEvents(btnReinforce, OnReinforceHoverEnter, OnHoverExit);
        AddButtonHoverEvents(btnOverrideATK, () => OnOverrideHoverEnter(OrderType.ATK), OnHoverExit);
        AddButtonHoverEvents(btnOverrideDEF, () => OnOverrideHoverEnter(OrderType.DEF), OnHoverExit);
        AddButtonHoverEvents(btnOverrideRET, () => OnOverrideHoverEnter(OrderType.RET), OnHoverExit);

        // 获取 TooltipPanel
        tooltipPanel = uIService.GetWindow<TooltipPanel>("TooltipPanel");

        // 监听意图变化事件
        eventService.AddEventListening((EventID)WarBrokerEventID.OnIntentChanged, OnIntentChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnDataChanged);

        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
        tooltipPanel?.Hide();
    }

    /// <summary>设置当前显示的将军</summary>
    /// <param name="general">将军数据</param>
    /// <param name="fullMode">是否显示完整模式（己方将军为true，敌方将军为false）</param>
    public void SetGeneral(GeneralData general, bool fullMode = true)
    {
        currentGeneral = general;
        isFullMode = fullMode;
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

    private void OnOverride(OrderType targetType)
    {
        if (currentGeneral == null || intentSystem == null) return;

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

        // 相机平滑返回默认视角
        var battlefieldCamera = GameObject.FindObjectOfType<BattlefieldCameraController>();
        if (battlefieldCamera != null)
        {
            battlefieldCamera.SmoothReturnToDefault();
        }
    }

    private void RefreshUI()
    {
        if (currentGeneral == null) return;

        RefreshGeneralInfo();
        RefreshHP();
        RefreshStatusAttributes();  // 状态始终显示

        if (isFullMode)
        {
            // 完整模式（己方将军）：显示所有信息
            RefreshIntent();
            RefreshButtons();

            // 显示所有区域
            SetActiveIfNotNull(trustArea, true);
            SetActiveIfNotNull(moraleArea, true);
            SetActiveIfNotNull(gridPositionArea, true);
            SetActiveIfNotNull(intentArea, true);
            SetActiveIfNotNull(buttonArea, true);
        }
        else
        {
            // 简略模式（敌方将军）：隐藏信任、士气、位置、意图和按钮
            SetActiveIfNotNull(trustArea, false);
            SetActiveIfNotNull(moraleArea, false);
            SetActiveIfNotNull(gridPositionArea, false);
            SetActiveIfNotNull(intentArea, false);
            SetActiveIfNotNull(buttonArea, false);
        }
    }

    private void SetActiveIfNotNull(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }

    private void RefreshGeneralInfo()
    {
        if (txtName != null)
            txtName.text = currentGeneral.Name;

        if (txtPersonality != null)
            txtPersonality.text = GetPersonalityText(currentGeneral.Personality);

        if (txtPosition != null)
            txtPosition.text = GetPositionText(currentGeneral.Position);

        // 设置头像
        if (imgPortrait != null && currentGeneral.Config?.Portrait != null)
        {
            imgPortrait.sprite = currentGeneral.Config.Portrait;
        }

        // 设置头像和名字的 Tooltip（显示将军生平介绍）
        SetupBiographyTooltip();
    }

    private void SetupBiographyTooltip()
    {
        string biography = currentGeneral.Config?.Biography;
        if (string.IsNullOrEmpty(biography)) return;

        string title = currentGeneral.Name;

        // 头像 Tooltip
        if (imgPortrait != null)
        {
            AddTooltipEvents(imgPortrait.gameObject, title, biography);
        }

        // 名字 Tooltip
        if (txtName != null)
        {
            AddTooltipEvents(txtName.gameObject, title, biography);
        }
    }

    private void AddTooltipEvents(GameObject target, string title, string content)
    {
        if (target == null || tooltipPanel == null) return;

        var trigger = target.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // 清除旧的事件
        trigger.triggers.Clear();

        // PointerEnter
        var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener((data) => tooltipPanel?.Show(title, content));
        trigger.triggers.Add(enterEntry);

        // PointerExit
        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
        };
        exitEntry.callback.AddListener((data) => tooltipPanel?.Hide());
        trigger.triggers.Add(exitEntry);
    }

    private void RefreshHP()
    {
        // HP (Troops)
        if (sliderHP != null)
        {
            sliderHP.maxValue = 20;
            sliderHP.value = currentGeneral.Troops;
        }
        if (txtHP != null)
            txtHP.text = $"{currentGeneral.Troops}/20";
    }

    private void RefreshStatusAttributes()
    {
        // 状态
        if (txtStatus != null)
        {
            var status = currentGeneral.GetStatus(balanceConfig);
            txtStatus.text = GetStatusText(status);
            txtStatus.color = GetStatusColor(status);
        }

        // 信任
        if (txtTrust != null)
            txtTrust.text = $"{currentGeneral.Trust}";

        // 士气
        if (txtMorale != null)
            txtMorale.text = $"{currentGeneral.Morale}";

        // 格位置
        if (txtGridPosition != null)
            txtGridPosition.text = $"格{currentGeneral.GridPosition}";
    }

    private string GetStatusText(GeneralStatus status)
    {
        if (textConfig != null)
        {
            return textConfig.GetStatusText(status);
        }
        return status switch
        {
            GeneralStatus.FullStrength => "满编",
            GeneralStatus.Healthy => "健康",
            GeneralStatus.Wounded => "受伤",
            GeneralStatus.Critical => "危急",
            GeneralStatus.Routed => "溃败",
            _ => status.ToString()
        };
    }

    private Color GetStatusColor(GeneralStatus status)
    {
        return status switch
        {
            GeneralStatus.FullStrength => Color.green,
            GeneralStatus.Healthy => new Color(0.5f, 1f, 0.5f), // 浅绿
            GeneralStatus.Wounded => Color.yellow,
            GeneralStatus.Critical => new Color(1f, 0.5f, 0f), // 橙色
            GeneralStatus.Routed => Color.red,
            _ => Color.white
        };
    }

    private void RefreshIntent()
    {
        var intent = currentGeneral.FinalIntent ?? currentGeneral.DefaultIntent;

        if (txtIntent != null)
        {
            string intentIcon = intent.HasValue ? GetIntentIcon(intent.Value) : "?";
            txtIntent.text = $"{intentIcon} {(intent.HasValue ? intent.Value.ToString() : "未知")}";
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

            // 更新按钮文本和图标
            if (currentGeneral.DefaultIntent.HasValue)
            {
                var intentType = currentGeneral.DefaultIntent.Value;
                int held = player.Inventory.ContainsKey(intentType) ? player.Inventory[intentType] : 0;

                // 设置指令图标
                if (imgReinforceOrder != null && orderConfig != null)
                {
                    var orderData = orderConfig.GetOrder(intentType);
                    if (orderData != null && orderData.Icon != null)
                    {
                        imgReinforceOrder.sprite = orderData.Icon;
                        imgReinforceOrder.gameObject.SetActive(true);
                    }
                }

                // 更新按钮文本
                var btnText = btnReinforce.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    string icon = GetIntentIcon(intentType);
                    btnText.text = $"强化 {icon}×{held}";
                }
            }
        }

        // 篡改按钮 - 只显示与默认意图不同的两个类型
        RefreshOverrideButtons(player, overrideCost);
    }

    private void RefreshOverrideButtons(PlayerData player, int cost)
    {
        if (currentGeneral == null || !currentGeneral.DefaultIntent.HasValue)
        {
            // 隐藏所有篡改按钮
            if (btnOverrideATK != null) btnOverrideATK.gameObject.SetActive(false);
            if (btnOverrideDEF != null) btnOverrideDEF.gameObject.SetActive(false);
            if (btnOverrideRET != null) btnOverrideRET.gameObject.SetActive(false);
            return;
        }

        var defaultIntent = currentGeneral.DefaultIntent.Value;

        // 根据默认意图，只显示另外两个类型的按钮
        RefreshOverrideButton(btnOverrideATK, OrderType.ATK, player, cost, defaultIntent != OrderType.ATK);
        RefreshOverrideButton(btnOverrideDEF, OrderType.DEF, player, cost, defaultIntent != OrderType.DEF);
        RefreshOverrideButton(btnOverrideRET, OrderType.RET, player, cost, defaultIntent != OrderType.RET);
    }

    private void RefreshOverrideButton(Button btn, OrderType type, PlayerData player, int cost, bool shouldShow)
    {
        if (btn == null) return;

        // 显示/隐藏按钮
        btn.gameObject.SetActive(shouldShow);

        if (!shouldShow) return;

        bool canOverride = CanOverride(player, type, cost);
        btn.interactable = canOverride;

        // 更新按钮文本
        var btnText = btn.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            int held = player.Inventory.ContainsKey(type) ? player.Inventory[type] : 0;
            string icon = GetIntentIcon(type);
            btnText.text = $"篡改 {icon}×{held}";
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
        int held = player.Inventory.ContainsKey(intentType) ? player.Inventory[intentType] : 0;
        return held >= cost;
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

        int held = player.Inventory.ContainsKey(targetType) ? player.Inventory[targetType] : 0;
        return held >= cost;
    }

    private string GetIntentIcon(OrderType type)
    {
        return type switch
        {
            OrderType.ATK => "🔴",
            OrderType.DEF => "🔵",
            OrderType.RET => "🟡",
            _ => "?"
        };
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

    #region 按钮悬停事件 - 指令预览

    private void AddButtonHoverEvents(Button btn, System.Action onEnter, System.Action onExit)
    {
        if (btn == null) return;

        var trigger = btn.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
            trigger = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // PointerEnter
        var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener((data) => onEnter?.Invoke());
        trigger.triggers.Add(enterEntry);

        // PointerExit
        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
        };
        exitEntry.callback.AddListener((data) => onExit?.Invoke());
        trigger.triggers.Add(exitEntry);
    }

    private void OnReinforceHoverEnter()
    {
        if (currentGeneral == null || !currentGeneral.DefaultIntent.HasValue) return;

        int cost = intentSystem?.GetReinforceCost() ?? 1;
        var orderType = currentGeneral.DefaultIntent.Value;
        eventService.SendMessage((EventID)WarBrokerEventID.OnInventoryPreview, orderType, -cost);
    }

    private void OnOverrideHoverEnter(OrderType orderType)
    {
        int cost = intentSystem?.GetOverrideCost() ?? 3;
        eventService.SendMessage((EventID)WarBrokerEventID.OnInventoryPreview, orderType, -cost);
    }

    private void OnHoverExit()
    {
        // 发送0表示清除预览
        eventService.SendMessage((EventID)WarBrokerEventID.OnInventoryPreview, OrderType.ATK, 0);
    }

    #endregion

    private void OnIntentChanged(object p1, object p2) => RefreshUI();
    private void OnDataChanged(object p1, object p2) => RefreshUI();
}
