using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayWindowBinder : UIBinder
{
    [Header("===== 顶部状态栏 - 回合 =====")]
    public Image imgTurnIcon;
    public TMP_Text txtTurn;

    [Header("===== 顶部状态栏 - 现金 =====")]
    public Image imgCashIcon;
    public TMP_Text txtCash;

    [Header("===== 顶部状态栏 - 净资产 =====")]
    public Image imgNetWorthIcon;
    public TMP_Text txtNetWorth;

    [Header("===== 顶部状态栏 - 审计值 =====")]
    public Image imgAuditIcon;
    public TMP_Text txtAudit;

    [Header("===== 顶部状态栏 - 后备役 =====")]
    public Image imgReservesIcon;
    public TMP_Text txtReserves;

    [Header("===== 顶部状态栏 - ATK指令 =====")]
    public Image imgATKIcon;
    public TMP_Text txtATK;

    [Header("===== 顶部状态栏 - DEF指令 =====")]
    public Image imgDEFIcon;
    public TMP_Text txtDEF;

    [Header("===== 顶部状态栏 - RET指令 =====")]
    public Image imgRETIcon;
    public TMP_Text txtRET;

    [Header("===== Tab按钮 =====")]
    public Button btnMarket;
    public Button btnIntel;

    [Header("===== 内容区域 =====")]
    public RectTransform contentArea;
    public RectTransform rightPanelArea;

    [Header("===== 底部操作栏 =====")]
    public Button btnEndTurn;
    public TMP_Text txtEventInfo;

    [Header("===== 阶段横幅 =====")]
    public PhaseBanner phaseBanner;
}
