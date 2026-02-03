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
    private TMP_Text txtName;
    private TMP_Text txtPersonality;
    private TMP_Text txtPosition;

    [Header("HP")]
    private Slider sliderHP;
    private TMP_Text txtHP;

    [Header("状态属性")]
    private TMP_Text txtStatus;
    private TMP_Text txtTrust;
    private TMP_Text txtMorale;
    private TMP_Text txtGridPosition;

    [Header("意图显示")]
    private TMP_Text txtIntent;

    [Header("强化操作")]
    private Button btnReinforce;

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

    public override void OnAwake()
    {
        base.OnAwake();

        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        textConfig = resService.LoadResource<UITextConfig>(ConfigPaths.UI_TEXT);

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

            // HP
            sliderHP = b.sliderHP;
            txtHP = b.txtHP;

            // 状态属性
            txtStatus = b.txtStatus;
            txtTrust = b.txtTrust;
            txtMorale = b.txtMorale;
            txtGridPosition = b.txtGridPosition;

            // 意图
            txtIntent = b.txtIntent;

            // 强化操作
            btnReinforce = b.btnReinforce;

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

        // 监听意图变化事件
        eventService.AddEventListening((EventID)WarBrokerEventID.OnIntentChanged, OnIntentChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnDataChanged);

        RefreshUI();
    }

    public override void OnHide()
    {
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
        RefreshStatusAttributes();
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
            txtStatus.text = $"状态: {GetStatusText(status)}";
            txtStatus.color = GetStatusColor(status);
        }

        // 信任
        if (txtTrust != null)
            txtTrust.text = $"信任: {currentGeneral.Trust}";

        // 士气
        if (txtMorale != null)
            txtMorale.text = $"士气: {currentGeneral.Morale}";

        // 格位置
        if (txtGridPosition != null)
            txtGridPosition.text = $"位置: 格{currentGeneral.GridPosition}";
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

            // 更新按钮文本
            if (currentGeneral.DefaultIntent.HasValue)
            {
                var btnText = btnReinforce.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    var intentType = currentGeneral.DefaultIntent.Value;
                    int held = player.Inventory.ContainsKey(intentType) ? player.Inventory[intentType] : 0;
                    string icon = GetIntentIcon(intentType);

                    // 显示持有数量
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

    private void OnIntentChanged(object p1, object p2) => RefreshUI();
    private void OnDataChanged(object p1, object p2) => RefreshUI();
}
