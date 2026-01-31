using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 现货市场 Tab
/// 显示 ATK/DEF/RET 三种指令的价格、持有量和买卖按钮
/// </summary>
public class SpotMarketTab : MonoBehaviour
{
    [Header("ATK 行")]
    [SerializeField] private TMP_Text txtAtkPrice;
    [SerializeField] private TMP_Text txtAtkHolding;
    [SerializeField] private Button btnAtkBuy;
    [SerializeField] private Button btnAtkSell;
    [SerializeField] private Button btnAtkChart;

    [Header("DEF 行")]
    [SerializeField] private TMP_Text txtDefPrice;
    [SerializeField] private TMP_Text txtDefHolding;
    [SerializeField] private Button btnDefBuy;
    [SerializeField] private Button btnDefSell;
    [SerializeField] private Button btnDefChart;

    [Header("RET 行")]
    [SerializeField] private TMP_Text txtRetPrice;
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

        RefreshOrderRow(OrderType.ATK, txtAtkPrice, txtAtkHolding, data);
        RefreshOrderRow(OrderType.DEF, txtDefPrice, txtDefHolding, data);
        RefreshOrderRow(OrderType.RET, txtRetPrice, txtRetHolding, data);

        RefreshChart();
    }

    private void RefreshOrderRow(OrderType type, TMP_Text priceText, TMP_Text holdingText, CampaignRuntimeData data)
    {
        var prices = data.Market.CurrentPrices;
        var playerInv = data.Player.Inventory;

        // 价格
        if (priceText != null)
            priceText.text = $"{type}: {prices[type]:F1}";

        // 持有量
        if (holdingText != null)
            holdingText.text = $"持有: {playerInv[type]}";
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
