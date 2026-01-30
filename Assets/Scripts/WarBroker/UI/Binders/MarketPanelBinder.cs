using UnityEngine;
using UnityEngine.UI;

public class MarketPanelBinder : UIBinder
{
    [Header("Tab 切换")]
    public Toggle tabSpot;
    public Toggle tabFutures;
    public GameObject spotContent;
    public GameObject futuresContent;

    [Header("ATK 行")]
    public Text txtAtkPrice;
    public Text txtAtkStock;
    public Button btnAtkBuy;
    public Button btnAtkSell;

    [Header("DEF 行")]
    public Text txtDefPrice;
    public Text txtDefStock;
    public Button btnDefBuy;
    public Button btnDefSell;

    [Header("RET 行")]
    public Text txtRetPrice;
    public Text txtRetStock;
    public Button btnRetBuy;
    public Button btnRetSell;

    [Header("期货区")]
    public Dropdown ddFuturesType;
    public Dropdown ddFuturesDir;
    public InputField inputFuturesQty;
    public InputField inputFuturesTurns;
    public Button btnOpenFutures;

    [Header("银行区")]
    public Text txtDebt;
    public Text txtInterest;
    public Text txtLoanLimit;
    public InputField inputBankAmount;
    public Button btnBorrow;
    public Button btnRepay;

    [Header("玩家信息")]
    public Text txtCash;
    public Text txtNetWorth;
}
