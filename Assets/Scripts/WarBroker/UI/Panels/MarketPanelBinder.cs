using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarketPanelBinder : UIBinder
{
    [Header("Tab 切换")]
    public Toggle tabSpot;
    public Toggle tabFutures;
    public GameObject spotContent;   // SpotMarketTab 组件挂在此节点
    public GameObject futuresContent; // FuturesMarketTab 组件挂在此节点

    [Header("银行区 - 信息")]
    public TMP_Text txtDebt;
    public TMP_Text txtInterest;
    public TMP_Text txtLoanLimit;

    [Header("银行区 - 金额选择器")]
    public Button btnDecrease;
    public TMP_Text txtAmount;
    public Button btnIncrease;

    [Header("银行区 - 快捷按钮")]
    public Button btn100;
    public Button btn500;
    public Button btnMax;

    [Header("银行区 - 操作按钮")]
    public Button btnBorrow;
    public Button btnRepay;

    [Header("关闭按钮")]
    public Button btnClose;
}
