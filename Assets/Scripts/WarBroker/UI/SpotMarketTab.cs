using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 现货市场 Tab
/// 显示 ATK/DEF/RET 三种指令的价格、三因子分解、流通盘、持有量和买卖按钮
/// </summary>
public class SpotMarketTab : MonoBehaviour
{
    [Header("ATK 行")]
    [SerializeField] private Text txtAtkPrice;
    [SerializeField] private Text txtAtkFactors;
    [SerializeField] private Text txtAtkFloat;
    [SerializeField] private Text txtAtkHolding;
    [SerializeField] private Button btnAtkBuy;
    [SerializeField] private Button btnAtkSell;

    [Header("DEF 行")]
    [SerializeField] private Text txtDefPrice;
    [SerializeField] private Text txtDefFactors;
    [SerializeField] private Text txtDefFloat;
    [SerializeField] private Text txtDefHolding;
    [SerializeField] private Button btnDefBuy;
    [SerializeField] private Button btnDefSell;

    [Header("RET 行")]
    [SerializeField] private Text txtRetPrice;
    [SerializeField] private Text txtRetFactors;
    [SerializeField] private Text txtRetFloat;
    [SerializeField] private Text txtRetHolding;
    [SerializeField] private Button btnRetBuy;
    [SerializeField] private Button btnRetSell;

    private GameplayManager gameplayManager;
    private PricingEngine pricingEngine;
    private WindowBase parentWindow;

    public void Initialize(GameplayManager manager, WindowBase parent)
    {
        gameplayManager = manager;
        parentWindow = parent;

        // 获取 PricingEngine
        if (GameRoot.Instance != null && GameRoot.Instance.marketSystem != null)
        {
            pricingEngine = GameRoot.Instance.marketSystem.GetPricingEngine();
        }

        BindButtons();
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

        RefreshOrderRow(OrderType.ATK, txtAtkPrice, txtAtkFactors, txtAtkFloat, txtAtkHolding, data);
        RefreshOrderRow(OrderType.DEF, txtDefPrice, txtDefFactors, txtDefFloat, txtDefHolding, data);
        RefreshOrderRow(OrderType.RET, txtRetPrice, txtRetFactors, txtRetFloat, txtRetHolding, data);
    }

    private void RefreshOrderRow(OrderType type, Text priceText, Text factorsText,
                                  Text floatText, Text holdingText, CampaignRuntimeData data)
    {
        var prices = data.Market.CurrentPrices;
        var inventory = data.Market.MarketInventory;
        var initialFloat = data.Market.InitialFloat;
        var playerInv = data.Player.Inventory;

        // 价格
        if (priceText != null)
            priceText.text = $"{type}: {prices[type]:F1}";

        // 三因子分解
        if (factorsText != null && pricingEngine != null)
        {
            var (alpha, beta, gamma) = pricingEngine.GetFactors(type);
            factorsText.text = $"α:{alpha:+0.00;-0.00} β:{beta:F2} γ:{gamma:F2}";
        }

        // 流通盘
        if (floatText != null)
        {
            float current = inventory[type];
            float initial = initialFloat[type];
            float ratio = initial > 0 ? current / initial : 0;
            floatText.text = $"流通: {current:F1}/{initial:F0} ({ratio:P0})";
        }

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
