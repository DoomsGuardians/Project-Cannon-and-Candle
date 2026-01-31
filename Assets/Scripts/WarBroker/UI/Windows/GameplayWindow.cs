using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主游戏界面：顶部状态栏 + Tab切换 + 底部操作栏
/// </summary>
public class GameplayWindow : WindowBase
{
    private TMP_Text txtTurn, txtPhase, txtCash, txtNetWorth, txtAudit, txtEventInfo;
    private Button btnMarket, btnInfo, btnEndTurn;
    private RectTransform contentArea;

    private GameplayManager gameplayManager;
    private string currentPanel = "MarketPanel";

    private static readonly string[] PanelNames = {
        "MarketPanel", "InfoPanel"
    };

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();

        var b = gameObject.GetComponent<GameplayWindowBinder>();
        if (b != null)
        {
            txtTurn = b.txtTurn; txtPhase = b.txtPhase; txtCash = b.txtCash;
            txtNetWorth = b.txtNetWorth; txtAudit = b.txtAudit; txtEventInfo = b.txtEventInfo;
            btnMarket = b.btnMarket; btnInfo = b.btnIntel; btnEndTurn = b.btnEndTurn;
            contentArea = b.contentArea;
        }
    }

    public override void OnShow()
    {
        AddButtonListener(btnMarket, () => SwitchPanel("MarketPanel"));
        AddButtonListener(btnInfo, () => SwitchPanel("InfoPanel"));
        AddButtonListener(btnEndTurn, OnEndTurnClicked);

        HideAllPanels();
        EnsurePanelsInContentArea();

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

    private void SwitchPanel(string panelName)
    {
        foreach (var name in PanelNames)
        {
            uIService.HideWindow(name);
        }
        uIService.ShowWindow<WindowBase>(panelName);
        var panel = uIService.GetWindow<WindowBase>(panelName);
        AttachPanelToContentArea(panel);
        currentPanel = panelName;
    }

    private void OnEndTurnClicked()
    {
        gameplayManager.EndTurn();
    }

    private void RefreshUI()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        if (txtTurn != null) txtTurn.text = $"回合 {data.CurrentTurn}/{data.MaxTurns}";
        if (txtPhase != null) txtPhase.text = data.CurrentPhase.ToString();
        if (txtCash != null) txtCash.text = $"现金: {data.Player.Cash:F0}";
        if (txtNetWorth != null) txtNetWorth.text = $"净资产: {data.Player.CalculateNetWorth(data.Market):F0}";
        if (txtAudit != null) txtAudit.text = $"审计: {data.Player.AuditValue:F0}";

        if (txtEventInfo != null)
        {
            txtEventInfo.text = data.ActiveEvent != null
                ? $"事件: {data.ActiveEvent.EventName} ({data.EventRemainingTurns}回合)"
                : "";
        }
    }

    private void OnTurnStart(object param1, object param2) => RefreshUI();
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
}
