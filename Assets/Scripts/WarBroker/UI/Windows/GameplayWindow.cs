using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 主游戏界面：顶部状态栏 + Tab切换 + 底部操作栏
/// </summary>
public class GameplayWindow : WindowBase
{
    // 顶部状态栏 - 文本
    private TMP_Text txtTurn, txtCash, txtNetWorth, txtAudit, txtEventInfo, txtReserves;
    private TMP_Text txtATK, txtDEF, txtRET;

    // 顶部状态栏 - 图标（用于Tooltip）
    private Image imgTurnIcon, imgCashIcon, imgNetWorthIcon, imgAuditIcon, imgReservesIcon;
    private Image imgATKIcon, imgDEFIcon, imgRETIcon;

    // 按钮和区域
    private Button btnMarket, btnInfo, btnEndTurn;
    private RectTransform contentArea;
    private RectTransform rightPanelArea;
    private ObjectivePanel objectivePanel;
    private PhaseBanner phaseBanner;

    private GameplayManager gameplayManager;
    private GameBalanceConfig balanceConfig;
    private UITextConfig textConfig;
    private string currentPanel = null;

    // 库存预览变化
    private OrderType? previewOrderType = null;
    private int previewDelta = 0;

    private static readonly string[] PanelNames = {
        "MarketPanel", "InfoPanel"
    };

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        textConfig = resService.LoadResource<UITextConfig>(ConfigPaths.UI_TEXT);

        var b = gameObject.GetComponent<GameplayWindowBinder>();
        if (b != null)
        {
            // 文本
            txtTurn = b.txtTurn;
            txtCash = b.txtCash;
            txtNetWorth = b.txtNetWorth;
            txtAudit = b.txtAudit;
            txtReserves = b.txtReserves;
            txtATK = b.txtATK;
            txtDEF = b.txtDEF;
            txtRET = b.txtRET;
            txtEventInfo = b.txtEventInfo;

            // 图标
            imgTurnIcon = b.imgTurnIcon;
            imgCashIcon = b.imgCashIcon;
            imgNetWorthIcon = b.imgNetWorthIcon;
            imgAuditIcon = b.imgAuditIcon;
            imgReservesIcon = b.imgReservesIcon;
            imgATKIcon = b.imgATKIcon;
            imgDEFIcon = b.imgDEFIcon;
            imgRETIcon = b.imgRETIcon;

            // 按钮和区域
            btnMarket = b.btnMarket;
            btnInfo = b.btnIntel;
            btnEndTurn = b.btnEndTurn;
            contentArea = b.contentArea;
            rightPanelArea = b.rightPanelArea;
            phaseBanner = b.phaseBanner;
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
        eventService.AddEventListening((EventID)WarBrokerEventID.OnPhaseChange, OnPhaseChange);
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
        eventService.AddEventListening((EventID)WarBrokerEventID.OnInventoryPreview, OnInventoryPreview);

        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    private void SetupTooltips()
    {
        // 回合图标 Tooltip
        if (imgTurnIcon != null)
            AddTooltip(imgTurnIcon.gameObject, "回合", textConfig?.TooltipTurn ?? "当前回合 / 最大回合数");

        // 现金图标 Tooltip
        if (imgCashIcon != null)
            AddTooltip(imgCashIcon.gameObject, "现金", textConfig?.TooltipCash ?? "可用于购买指令、支付利息和仓储费");
        if (txtCash != null)
            AddTooltip(txtCash.gameObject, "", GetCashTooltipContent);

        // 净资产图标 Tooltip
        if (imgNetWorthIcon != null)
            AddTooltip(imgNetWorthIcon.gameObject, "净资产", textConfig?.TooltipNetWorth ?? "现金 + 库存价值 - 负债");

        // 审计值图标 Tooltip
        if (imgAuditIcon != null)
            AddTooltip(imgAuditIcon.gameObject, "审计值", textConfig?.TooltipAudit ?? "初始资产，用于计算最终盈亏");

        // 后备役图标 Tooltip
        if (imgReservesIcon != null)
            AddTooltip(imgReservesIcon.gameObject, "后备役", textConfig?.TooltipReserves ?? "用于补充将军兵力。将军撤退或在基地休整时消耗。");
        if (txtReserves != null)
            AddTooltip(txtReserves.gameObject, "后备役", textConfig?.TooltipReserves ?? "用于补充将军兵力。将军撤退或在基地休整时消耗。");

        // ATK指令 Tooltip
        if (imgATKIcon != null)
            AddTooltip(imgATKIcon.gameObject, "进攻令", textConfig?.TooltipATK ?? "下达进攻指令，命令前线部队向敌阵推进");
        if (txtATK != null)
            AddTooltip(txtATK.gameObject, "进攻令", textConfig?.TooltipATK ?? "下达进攻指令，命令前线部队向敌阵推进");

        // DEF指令 Tooltip
        if (imgDEFIcon != null)
            AddTooltip(imgDEFIcon.gameObject, "防守令", textConfig?.TooltipDEF ?? "下达防守指令，命令部队固守当前阵地");
        if (txtDEF != null)
            AddTooltip(txtDEF.gameObject, "防守令", textConfig?.TooltipDEF ?? "下达防守指令，命令部队固守当前阵地");

        // RET指令 Tooltip
        if (imgRETIcon != null)
            AddTooltip(imgRETIcon.gameObject, "撤退令", textConfig?.TooltipRET ?? "下达撤退指令，命令部队后撤进行休整补充");
        if (txtRET != null)
            AddTooltip(txtRET.gameObject, "撤退令", textConfig?.TooltipRET ?? "下达撤退指令，命令部队后撤进行休整补充");

        // 结束回合按钮 Tooltip
        if (btnEndTurn != null)
            AddTooltip(btnEndTurn.gameObject, "结束回合", GetEndTurnTooltipContent);
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

        // 回合数：只显示数字
        if (txtTurn != null) txtTurn.text = $"{data.CurrentTurn}/{data.MaxTurns}";

        // 现金显示（含下回合预计变化）
        RefreshCashDisplay(data);

        // 净资产：只显示数字
        if (txtNetWorth != null) txtNetWorth.text = $"{data.Player.CalculateNetWorth(data.Market):F0}";

        // 审计值：只显示数字
        if (txtAudit != null) txtAudit.text = $"{data.Player.AuditValue:F0}";

        // 指令库存显示
        RefreshInventoryDisplay(data);

        // 后备役显示
        RefreshReservesDisplay(data);

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

        // 只显示数字，预计消耗用颜色标记
        if (totalCost > 0)
        {
            txtCash.text = $"{data.Player.Cash:F0} <color=#FF6666>(-{totalCost:F0})</color>";
        }
        else
        {
            txtCash.text = $"{data.Player.Cash:F0}";
        }
    }

    private void RefreshInventoryDisplay(CampaignRuntimeData data)
    {
        RefreshSingleInventory(txtATK, OrderType.ATK, data);
        RefreshSingleInventory(txtDEF, OrderType.DEF, data);
        RefreshSingleInventory(txtRET, OrderType.RET, data);
    }

    private void RefreshSingleInventory(TMP_Text txt, OrderType orderType, CampaignRuntimeData data)
    {
        if (txt == null) return;
        if (!data.Player.Inventory.TryGetValue(orderType, out int count)) return;

        // 检查是否有预览变化
        if (previewOrderType == orderType && previewDelta != 0)
        {
            string deltaColor = previewDelta > 0 ? "#66FF66" : "#FF6666";
            string deltaSign = previewDelta > 0 ? "+" : "";
            txt.text = $"{count} <color={deltaColor}>({deltaSign}{previewDelta})</color>";
        }
        else
        {
            txt.text = count.ToString();
        }
    }

    private void RefreshReservesDisplay(CampaignRuntimeData data)
    {
        if (txtReserves == null) return;

        int allyReserves = data.Battle.CurrentReserves;
        int estimatedCost = CalculateEstimatedReservesCost(data);

        if (estimatedCost > 0)
        {
            txtReserves.text = $"{allyReserves} <color=#FF6666>(-{estimatedCost})</color>";
        }
        else
        {
            txtReserves.text = allyReserves.ToString();
        }
    }

    /// <summary>
    /// 计算预计的后备役消耗
    /// 基于当前将军的指令分配和位置
    /// </summary>
    private int CalculateEstimatedReservesCost(CampaignRuntimeData data)
    {
        int totalCost = 0;

        foreach (var general in data.Battle.AllyGenerals)
        {
            // 跳过已溃败的将军
            if (general.Troops <= 0) continue;

            // RET指令消耗后备役
            if (general.FinalIntent == OrderType.RET)
            {
                totalCost += balanceConfig?.RetHealCost ?? 1;
            }
            // 在基地(Grid 1)休整消耗后备役
            else if (general.GridPosition == 1 && general.Troops < 20)
            {
                totalCost += balanceConfig?.BaseRecoveryCost ?? 1;
            }
        }

        // 不能超过当前后备役
        return Mathf.Min(totalCost, data.Battle.CurrentReserves);
    }

    /// <summary>将敌方后备役精确值转换为模糊的数字范围</summary>
    private string EstimateEnemyReserves(int actual)
    {
        if (actual <= 0) return "0";
        if (actual <= 5) return "0-5";
        if (actual <= 10) return "5-15";
        if (actual <= 20) return "10-25";
        return "20+";
    }

    private void OnTurnStart(object param1, object param2)
    {
        // 重新启用结束回合按钮
        if (btnEndTurn != null)
        {
            btnEndTurn.interactable = true;
        }

        // 重置阶段横幅追踪
        if (phaseBanner != null)
        {
            phaseBanner.ResetPhaseTracking();
        }

        RefreshUI();
    }

    private void OnTurnEnd(object param1, object param2) => RefreshUI();
    private void OnRandomEvent(object param1, object param2) => RefreshUI();
    private void OnFinanceChanged(object param1, object param2) => RefreshUI();

    private void OnPhaseChange(object param1, object param2)
    {
        if (phaseBanner != null && param1 is TurnPhase phase)
        {
            var data = gameplayManager.GetCampaignData();
            int turnNumber = data?.CurrentTurn ?? 1;
            // 显示横幅，动画期间会自动锁定输入
            phaseBanner.ShowPhase(phase, turnNumber, () =>
            {
                // 动画完成后刷新UI
                RefreshUI();
            });
        }
        else
        {
            RefreshUI();
        }
    }

    private void OnInventoryPreview(object param1, object param2)
    {
        if (param1 is OrderType orderType && param2 is int delta)
        {
            if (delta == 0)
            {
                // 清除预览
                previewOrderType = null;
                previewDelta = 0;
            }
            else
            {
                previewOrderType = orderType;
                previewDelta = delta;
            }

            // 只刷新库存显示
            var data = gameplayManager?.GetCampaignData();
            if (data != null)
            {
                RefreshInventoryDisplay(data);
            }
        }
    }

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
