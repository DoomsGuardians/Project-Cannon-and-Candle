using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 市场面板：现货交易、期货操作、银行借贷
/// </summary>
public class MarketPanel : WindowBase
{
    private Text txtAtkPrice, txtAtkStock;
    private Button btnAtkBuy, btnAtkSell;
    private Text txtDefPrice, txtDefStock;
    private Button btnDefBuy, btnDefSell;
    private Text txtRetPrice, txtRetStock;
    private Button btnRetBuy, btnRetSell;

    private Dropdown ddFuturesType, ddFuturesDir;
    private InputField inputFuturesQty, inputFuturesTurns;
    private Button btnOpenFutures;

    private Text txtDebt, txtInterest;
    private InputField inputBankAmount;
    private Button btnBorrow, btnRepay;

    private GameplayManager gameplayManager;

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();

        var b = gameObject.GetComponent<MarketPanelBinder>();
        if (b != null)
        {
            txtAtkPrice = b.txtAtkPrice; txtAtkStock = b.txtAtkStock;
            btnAtkBuy = b.btnAtkBuy; btnAtkSell = b.btnAtkSell;
            txtDefPrice = b.txtDefPrice; txtDefStock = b.txtDefStock;
            btnDefBuy = b.btnDefBuy; btnDefSell = b.btnDefSell;
            txtRetPrice = b.txtRetPrice; txtRetStock = b.txtRetStock;
            btnRetBuy = b.btnRetBuy; btnRetSell = b.btnRetSell;
            ddFuturesType = b.ddFuturesType; ddFuturesDir = b.ddFuturesDir;
            inputFuturesQty = b.inputFuturesQty; inputFuturesTurns = b.inputFuturesTurns;
            btnOpenFutures = b.btnOpenFutures;
            txtDebt = b.txtDebt; txtInterest = b.txtInterest;
            inputBankAmount = b.inputBankAmount;
            btnBorrow = b.btnBorrow; btnRepay = b.btnRepay;
        }
    }

    public override void OnShow()
    {
        AddButtonListener(btnAtkBuy, () => Buy(OrderType.ATK));
        AddButtonListener(btnAtkSell, () => Sell(OrderType.ATK));
        AddButtonListener(btnDefBuy, () => Buy(OrderType.DEF));
        AddButtonListener(btnDefSell, () => Sell(OrderType.DEF));
        AddButtonListener(btnRetBuy, () => Buy(OrderType.RET));
        AddButtonListener(btnRetSell, () => Sell(OrderType.RET));
        AddButtonListener(btnOpenFutures, OnOpenFutures);
        AddButtonListener(btnBorrow, OnBorrow);
        AddButtonListener(btnRepay, OnRepay);

        eventService.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnDataChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnDataChanged);

        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    private void Buy(OrderType type)
    {
        gameplayManager.BuyOrder(type, 1);
        RefreshUI();
    }

    private void Sell(OrderType type)
    {
        gameplayManager.SellOrder(type, 1);
        RefreshUI();
    }

    private void OnOpenFutures()
    {
        if (ddFuturesType == null || ddFuturesDir == null) return;
        var type = (OrderType)ddFuturesType.value;
        var dir = (FuturesDirection)ddFuturesDir.value;
        int qty = 1, turns = 3;
        if (inputFuturesQty != null && int.TryParse(inputFuturesQty.text, out int q)) qty = q;
        if (inputFuturesTurns != null && int.TryParse(inputFuturesTurns.text, out int t)) turns = t;
        gameplayManager.OpenFutures(type, dir, qty, turns);
        RefreshUI();
    }

    private void OnBorrow()
    {
        if (inputBankAmount == null) return;
        if (float.TryParse(inputBankAmount.text, out float amount))
        {
            gameplayManager.Borrow(amount);
            RefreshUI();
        }
    }

    private void OnRepay()
    {
        if (inputBankAmount == null) return;
        if (float.TryParse(inputBankAmount.text, out float amount))
        {
            gameplayManager.Repay(amount);
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        var prices = data.Market.CurrentPrices;
        var stock = data.Market.MarketInventory;
        var inv = data.Player.Inventory;

        SetText(txtAtkPrice, $"ATK: {prices[OrderType.ATK]:F1}");
        SetText(txtAtkStock, $"库存:{inv[OrderType.ATK]} 市场:{stock[OrderType.ATK]}");
        SetText(txtDefPrice, $"DEF: {prices[OrderType.DEF]:F1}");
        SetText(txtDefStock, $"库存:{inv[OrderType.DEF]} 市场:{stock[OrderType.DEF]}");
        SetText(txtRetPrice, $"RET: {prices[OrderType.RET]:F1}");
        SetText(txtRetStock, $"库存:{inv[OrderType.RET]} 市场:{stock[OrderType.RET]}");

        SetText(txtDebt, $"负债: {data.Player.BankDebt:F0}");
        var balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        SetText(txtInterest, $"利率: {(balanceConfig != null ? balanceConfig.BankInterestRate : 0f):P1}");
    }

    private void SetText(Text t, string s) { if (t != null) t.text = s; }
    private void OnDataChanged(object p1, object p2) => RefreshUI();
}
