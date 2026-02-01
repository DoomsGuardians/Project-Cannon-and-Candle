using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayWindowBinder : UIBinder
{
    [Header("顶部状态栏")]
    public TMP_Text txtTurn;
    public TMP_Text txtPhase;
    public TMP_Text txtCash;
    public TMP_Text txtNetWorth;
    public TMP_Text txtAudit;

    [Header("现金图标")]
    public Image imgCashIcon;

    [Header("指令库存")]
    public TMP_Text txtATK;
    public Image imgATKIcon;
    public TMP_Text txtDEF;
    public Image imgDEFIcon;
    public TMP_Text txtRET;
    public Image imgRETIcon;

    [Header("Tab 按钮")]
    public Button btnMarket;
    public Button btnIntel;

    [Header("内容区")]
    public RectTransform contentArea;

    [Header("底部操作栏")]
    public Button btnEndTurn;
    public TMP_Text txtEventInfo;

    [Header("右侧面板区")]
    public RectTransform rightPanelArea;
}
