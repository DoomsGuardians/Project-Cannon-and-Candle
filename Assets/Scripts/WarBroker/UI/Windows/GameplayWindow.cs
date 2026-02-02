using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 主游戏界面：顶部状态栏 + Tab切换 + 底部操作栏
/// </summary>
public class GameplayWindow : WindowBase
{
    private TMP_Text txtTurn, txtPhase, txtCash, txtNetWorth, txtAudit, txtEventInfo;
    private TMP_Text txtATK, txtDEF, txtRET;
    private Image imgCashIcon, imgATKIcon, imgDEFIcon, imgRETIcon;
    private Button btnMarket, btnInfo, btnEndTurn;
    private RectTransform contentArea;
    private RectTransform rightPanelArea;
    private ObjectivePanel objectivePanel;

    private GameplayManager gameplayManager;
    private GameBalanceConfig balanceConfig;
    private string currentPanel = null;

    private static readonly string[] PanelNames = {
        "MarketPanel", "InfoPanel"
    };

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);

        var b = gameObject.GetComponent<GameplayWindowBinder>();
        if (b != null)
        {
            txtTurn = b.txtTurn; txtPhase = b.txtPhase; txtCash = b.txtCash;
            txtNetWorth = b.txtNetWorth; txtAudit = b.txtAudit; txtEventInfo = b.txtEventInfo;
            btnMarket = b.btnMarket; btnInfo = b.btnIntel; btnEndTurn = b.btnEndTurn;
            contentArea = b.contentArea;
            rightPanelArea = b.rightPanelArea;

            // 指令库存
            txtATK = b.txtATK; txtDEF = b.txtDEF; txtRET = b.txtRET;
            imgATKIcon = b.imgATKIcon; imgDEFIcon = b.imgDEFIcon; imgRETIcon = b.imgRETIcon;
            imgCashIcon = b.imgCashIcon;
        }
    }

    public override void OnShow()
    {
        AddButtonListener(btnMarket, () => SwitchPanel("MarketPanel"));
        AddButtonListener(btnInfo, () => SwitchPanel("InfoPanel"));
        AddButtonListener(btnEndTurn, OnEndTurnClicked);

        // 设置 Tooltip
        SetupTooltips();

        HideAllPanels();
        EnsurePanelsInContentArea();
        LoadObjectivePanel();

        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnTurnStart);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnTurnEnd);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnRandomEvent, OnRandomEvent);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnFinanceChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnFuturesOpened, OnFinanceChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnFuturesClosed, OnFinanceChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnForceLiquidation, OnFinanceChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnPriceUpdate, OnFinanceChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnOrderAssigned, OnFinanceChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnCashChange, OnFinanceChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnNetWorthChange, OnFinanceChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnAuditValueChange, OnFinanceChanged);

        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    private void SetupTooltips()
    {
        // 现金图标 Tooltip
        if (imgCashIcon != null)
        {
            AddTooltip(imgCashIcon.gameObject, "现金", "可用于购买指令、支付利息和仓储费");
        }

        // 现金数值 Tooltip（显示下回合预计变化）
        if (txtCash != null)
        {
            AddTooltip(txtCash.gameObject, "", GetCashTooltipContent);
        }

        // 指令库存 Tooltip
        if (imgATKIcon != null)
            AddTooltip(imgATKIcon.gameObject, "进攻令 (ATK)", "用于命令将军发起进攻");
        if (txtATK != null)
            AddTooltip(txtATK.gameObject, "进攻令 (ATK)", "用于命令将军发起进攻");

        if (imgDEFIcon != null)
            AddTooltip(imgDEFIcon.gameObject, "防守令 (DEF)", "用于命令将军坚守阵地");
        if (txtDEF != null)
            AddTooltip(txtDEF.gameObject, "防守令 (DEF)", "用于命令将军坚守阵地");

        if (imgRETIcon != null)
            AddTooltip(imgRETIcon.gameObject, "撤退令 (RET)", "用于命令将军撤退休整");
        if (txtRET != null)
            AddTooltip(txtRET.gameObject, "撤退令 (RET)", "用于命令将军撤退休整");

        // 结束回合按钮 Tooltip
        if (btnEndTurn != null)
        {
            AddTooltip(btnEndTurn.gameObject, "结束回合", GetEndTurnTooltipContent);
        }
    }

    private string GetCashTooltipContent()
    {
        var data = gameplayManager?.GetCampaignData();
        if (data == null) return "";

        float interest = data.Player.BankDebt * (balanceConfig?.BankInterestRate ?? 0.05f);
        int totalInventory = 0;
        foreach (var kvp in data.Player.Inventory)
        {
            totalInventory += kvp.Value;
        }
        float storageCost = totalInventory * (balanceConfig?.StorageCostPerUnit ?? 3f);
        float totalCost = interest + storageCost;

        return $"当前: {data.Player.Cash:F0}\n" +
               $"<color=#FF6666>下回合固定支出:</color>\n" +
               $"  利息: -{interest:F0}\n" +
               $"  仓储费: -{storageCost:F0}\n" +
               $"  <color=#FFCC00>合计: -{totalCost:F0}</color>";
    }

    private string GetEndTurnTooltipContent()
    {
        var data = gameplayManager?.GetCampaignData();
        if (data == null) return "进入下一回合";

        float interest = data.Player.BankDebt * (balanceConfig?.BankInterestRate ?? 0.05f);
        int totalInventory = 0;
        foreach (var kvp in data.Player.Inventory)
        {
            totalInventory += kvp.Value;
        }
        float storageCost = totalInventory * (balanceConfig?.StorageCostPerUnit ?? 3f);
        float totalCost = interest + storageCost;

        if (totalCost > 0)
        {
            return $"进入下一回合\n<color=#FF6666>将扣除: -{totalCost:F0}</color>";
        }
        return "进入下一回合";
    }

    /// <summary>
    /// 添加 Tooltip（静态内容）
    /// </summary>
    private void AddTooltip(GameObject target, string title, string content)
    {
        if (target == null) return;

        var listener = target.GetComponent<UIListener>();
        if (listener == null)
        {
            listener = target.AddComponent<UIListener>();
        }

        listener.onEnter = (eventData, l, args) =>
        {
            var tooltip = uIService.GetWindow<TooltipPanel>("TooltipPanel");
            tooltip?.Show(title, content);
        };

        listener.onExit = (eventData, l, args) =>
        {
            var tooltip = uIService.GetWindow<TooltipPanel>("TooltipPanel");
            tooltip?.Hide();
        };
    }

    /// <summary>
    /// 添加 Tooltip（动态内容）
    /// </summary>
    private void AddTooltip(GameObject target, string title, System.Func<string> contentGetter)
    {
        if (target == null) return;

        var listener = target.GetComponent<UIListener>();
        if (listener == null)
        {
            listener = target.AddComponent<UIListener>();
        }

        listener.onEnter = (eventData, l, args) =>
        {
            var tooltip = uIService.GetWindow<TooltipPanel>("TooltipPanel");
            tooltip?.Show(title, contentGetter());
        };

        listener.onExit = (eventData, l, args) =>
        {
            var tooltip = uIService.GetWindow<TooltipPanel>("TooltipPanel");
            tooltip?.Hide();
        };
    }

    private void SwitchPanel(string panelName)
    {
        // 如果点击的是当前面板，则关闭它
        if (currentPanel == panelName)
        {
            uIService.HideWindow(panelName);
            currentPanel = null;
            return;
        }

        // 关闭其他面板
        foreach (var name in PanelNames)
        {
            uIService.HideWindow(name);
        }

        // 打开目标面板
        uIService.ShowWindow<WindowBase>(panelName);
        var panel = uIService.GetWindow<WindowBase>(panelName);
        AttachPanelToContentArea(panel);
        currentPanel = panelName;
    }

    private void OnEndTurnClicked()
    {
        // 禁用按钮，防止重复点击
        if (btnEndTurn != null)
        {
            btnEndTurn.interactable = false;
        }

        gameplayManager.EndTurn();
    }

    private void RefreshUI()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        if (txtTurn != null) txtTurn.text = $"回合 {data.CurrentTurn}/{data.MaxTurns}";
        if (txtPhase != null) txtPhase.text = data.CurrentPhase.ToString();

        // 现金显示（含下回合预计变化）
        RefreshCashDisplay(data);

        if (txtNetWorth != null) txtNetWorth.text = $"净资产: {data.Player.CalculateNetWorth(data.Market):F0}";
        if (txtAudit != null) txtAudit.text = $"审计: {data.Player.AuditValue:F0}";

        // 指令库存显示
        RefreshInventoryDisplay(data);

        if (txtEventInfo != null)
        {
            txtEventInfo.text = data.ActiveEvent != null
                ? $"事件: {data.ActiveEvent.EventName} ({data.EventRemainingTurns}回合)"
                : "";
        }
    }

    private void RefreshCashDisplay(CampaignRuntimeData data)
    {
        if (txtCash == null) return;

        float interest = data.Player.BankDebt * (balanceConfig?.BankInterestRate ?? 0.05f);
        int totalInventory = 0;
        foreach (var kvp in data.Player.Inventory)
        {
            totalInventory += kvp.Value;
        }
        float storageCost = totalInventory * (balanceConfig?.StorageCostPerUnit ?? 3f);
        float totalCost = interest + storageCost;

        if (totalCost > 0)
        {
            txtCash.text = $"现金: {data.Player.Cash:F0} <color=#FF6666>(-{totalCost:F0})</color>";
        }
        else
        {
            txtCash.text = $"现金: {data.Player.Cash:F0}";
        }
    }

    private void RefreshInventoryDisplay(CampaignRuntimeData data)
    {
        if (txtATK != null && data.Player.Inventory.TryGetValue(OrderType.ATK, out int atkCount))
        {
            txtATK.text = atkCount.ToString();
        }

        if (txtDEF != null && data.Player.Inventory.TryGetValue(OrderType.DEF, out int defCount))
        {
            txtDEF.text = defCount.ToString();
        }

        if (txtRET != null && data.Player.Inventory.TryGetValue(OrderType.RET, out int retCount))
        {
            txtRET.text = retCount.ToString();
        }
    }

    private void OnTurnStart(object param1, object param2)
    {
        // 重新启用结束回合按钮
        if (btnEndTurn != null)
        {
            btnEndTurn.interactable = true;
        }
        RefreshUI();
    }

    private void OnTurnEnd(object param1, object param2) => RefreshUI();
    private void OnRandomEvent(object param1, object param2) => RefreshUI();
    private void OnFinanceChanged(object param1, object param2) => RefreshUI();

    private void HideAllPanels()
    {
        foreach (var name in PanelNames)
        {
            uIService.HideWindow(name);
        }
    }

    private void EnsurePanelsInContentArea()
    {
        foreach (var name in PanelNames)
        {
            var panel = uIService.GetWindow<WindowBase>(name);
            AttachPanelToContentArea(panel);
        }
    }

    private void AttachPanelToContentArea(WindowBase panel)
    {
        if (panel == null || panel.transform == null || contentArea == null)
        {
            return;
        }

        panel.transform.SetParent(contentArea, false);
        panel.transform.SetAsLastSibling();

        if (panel.transform is RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    private void LoadObjectivePanel()
    {
        if (rightPanelArea == null) return;

        objectivePanel = uIService.ShowWindow<ObjectivePanel>("ObjectivePanel");
        if (objectivePanel != null)
        {
            AttachPanelToArea(objectivePanel.gameObject.GetComponent<RectTransform>(), rightPanelArea);
        }
    }

    private void AttachPanelToArea(RectTransform panel, RectTransform area)
    {
        if (panel == null || area == null) return;

        panel.SetParent(area, false);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
    }
}
