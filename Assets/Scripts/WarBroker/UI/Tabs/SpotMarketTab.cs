using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 现货市场 Tab
/// 显示 ATK/DEF/RET 三种指令的价格、持有量和买卖按钮
/// </summary>
public class SpotMarketTab : MonoBehaviour
{
    [Header("ATK 行")]
    [SerializeField] private TMP_Text txtAtkPrice;
    [SerializeField] private TMP_Text txtAtkMarketStock;
    [SerializeField] private TMP_Text txtAtkHolding;
    [SerializeField] private Button btnAtkBuy;
    [SerializeField] private Button btnAtkSell;
    [SerializeField] private Button btnAtkChart;

    [Header("DEF 行")]
    [SerializeField] private TMP_Text txtDefPrice;
    [SerializeField] private TMP_Text txtDefMarketStock;
    [SerializeField] private TMP_Text txtDefHolding;
    [SerializeField] private Button btnDefBuy;
    [SerializeField] private Button btnDefSell;
    [SerializeField] private Button btnDefChart;

    [Header("RET 行")]
    [SerializeField] private TMP_Text txtRetPrice;
    [SerializeField] private TMP_Text txtRetMarketStock;
    [SerializeField] private TMP_Text txtRetHolding;
    [SerializeField] private Button btnRetBuy;
    [SerializeField] private Button btnRetSell;
    [SerializeField] private Button btnRetChart;

    [Header("K线图")]
    [SerializeField] private KLineChartView klineChart;

    private GameplayManager gameplayManager;
    private WindowBase parentWindow;
    private OrderType selectedOrderType = OrderType.ATK;

    public void Initialize(GameplayManager manager, WindowBase parent)
    {
        gameplayManager = manager;
        parentWindow = parent;

        BindButtons();
        BindHoverEvents();
        InitializeChart();
        RefreshUI();
    }

    private void BindButtons()
    {
        if (parentWindow == null) return;

        parentWindow.AddButtonListener(btnAtkBuy, () => Buy(OrderType.ATK));
        parentWindow.AddButtonListener(btnAtkSell, () => Sell(OrderType.ATK));
        parentWindow.AddButtonListener(btnDefBuy, () => Buy(OrderType.DEF));
        parentWindow.AddButtonListener(btnDefSell, () => Sell(OrderType.DEF));
        parentWindow.AddButtonListener(btnRetBuy, () => Buy(OrderType.RET));
        parentWindow.AddButtonListener(btnRetSell, () => Sell(OrderType.RET));

        // K线图切换按钮
        parentWindow.AddButtonListener(btnAtkChart, () => SelectOrderType(OrderType.ATK));
        parentWindow.AddButtonListener(btnDefChart, () => SelectOrderType(OrderType.DEF));
        parentWindow.AddButtonListener(btnRetChart, () => SelectOrderType(OrderType.RET));
    }

    private void BindHoverEvents()
    {
        // 为买入/卖出按钮添加悬停事件
        AddHoverEvent(btnAtkBuy, OrderType.ATK, 1);
        AddHoverEvent(btnAtkSell, OrderType.ATK, -1);
        AddHoverEvent(btnDefBuy, OrderType.DEF, 1);
        AddHoverEvent(btnDefSell, OrderType.DEF, -1);
        AddHoverEvent(btnRetBuy, OrderType.RET, 1);
        AddHoverEvent(btnRetSell, OrderType.RET, -1);
    }

    private void AddHoverEvent(Button button, OrderType orderType, int delta)
    {
        if (button == null) return;

        var trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        // 鼠标进入
        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) => SendPreviewEvent(orderType, delta));
        trigger.triggers.Add(entryEnter);

        // 鼠标离开
        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) => SendPreviewEvent(orderType, 0));
        trigger.triggers.Add(entryExit);
    }

    private void SendPreviewEvent(OrderType orderType, int delta)
    {
        GameRoot.Instance?.eventService?.SendMessage(
            (EventID)WarBrokerEventID.OnInventoryPreview,
            orderType, delta);
    }

    private void InitializeChart()
    {
        if (klineChart != null)
        {
            klineChart.Initialize();
            klineChart.SetOrderType(selectedOrderType);
            klineChart.SetTitle(selectedOrderType.ToString());
            RefreshChart();  // 默认显示 ATK 的 K 线图
        }
    }

    /// <summary>
    /// 切换显示的指令类型 K 线图
    /// </summary>
    public void SelectOrderType(OrderType type)
    {
        selectedOrderType = type;
        if (klineChart != null)
        {
            klineChart.SetOrderType(type);
            klineChart.SetTitle(type.ToString());
        }
        RefreshChart();
    }

    private void RefreshChart()
    {
        if (klineChart == null || gameplayManager == null) return;

        var data = gameplayManager.GetCampaignData();
        if (data?.Market?.KLineHistory == null) return;

        var klineHistory = data.Market.KLineHistory;
        if (klineHistory.ContainsKey(selectedOrderType))
        {
            klineChart.RefreshData(klineHistory[selectedOrderType]);
        }
    }

    private void Buy(OrderType type)
    {
        if (gameplayManager == null) return;
        gameplayManager.BuyOrder(type, 1);
        RefreshUI();
    }

    private void Sell(OrderType type)
    {
        if (gameplayManager == null) return;
        gameplayManager.SellOrder(type, 1);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (gameplayManager == null) return;

        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        RefreshOrderRow(OrderType.ATK, txtAtkPrice, txtAtkMarketStock, txtAtkHolding, data);
        RefreshOrderRow(OrderType.DEF, txtDefPrice, txtDefMarketStock, txtDefHolding, data);
        RefreshOrderRow(OrderType.RET, txtRetPrice, txtRetMarketStock, txtRetHolding, data);

        RefreshChart();
    }

    private void RefreshOrderRow(OrderType type, TMP_Text priceText, TMP_Text marketStockText, TMP_Text holdingText, CampaignRuntimeData data)
    {
        var prices = data.Market.CurrentPrices;
        var marketInv = data.Market.MarketInventory;
        var playerInv = data.Player.Inventory;

        // 价格
        if (priceText != null)
            priceText.text = $"{prices[type]:F1}";

        // 市场库存
        if (marketStockText != null)
        {
            int stock = marketInv[type];
            marketStockText.text = $"{stock}";

            // 库存不足时变色提示
            if (stock <= 0)
            {
                marketStockText.color = Color.red;
            }
            else if (stock < 50)
            {
                marketStockText.color = new Color(1f, 0.6f, 0f); // 橙色
            }
            else
            {
                marketStockText.color = Color.white;
            }
        }

        // 持有量
        if (holdingText != null)
            holdingText.text = $"{playerInv[type]}";
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (active)
        {
            RefreshUI();
        }
    }
}
