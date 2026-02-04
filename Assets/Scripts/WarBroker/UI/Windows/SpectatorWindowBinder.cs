using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 观战窗口 Binder：持有 UI 序列化引用
/// </summary>
public class SpectatorWindowBinder : UIBinder
{
    [Header("===== 顶部信息栏 =====")]
    public TMP_Text txtTurn;
    public TMP_Text txtStatus;

    [Header("===== 己方状态面板 =====")]
    public TMP_Text txtAllyTitle;
    public TMP_Text txtAllyCash;
    public TMP_Text txtAllyNetWorth;
    public TMP_Text txtAllyStrategy;
    public TMP_Text txtAllyHoldings;

    [Header("===== 敌方状态面板 =====")]
    public TMP_Text txtEnemyTitle;
    public TMP_Text txtEnemyCash;
    public TMP_Text txtEnemyNetWorth;
    public TMP_Text txtEnemyStrategy;
    public TMP_Text txtEnemyHoldings;

    [Header("===== 战场信息 =====")]
    public TMP_Text txtFrontlineInfo;
    public TMP_Text txtPriceInfo;

    [Header("===== 控制按钮 =====")]
    public Button btnStart;
    public Button btnPause;
    public Button btnStep;
    public Button btnSpeedDown;
    public Button btnSpeedUp;
    public TMP_Text txtSpeed;

    [Header("===== 统计面板 =====")]
    public TMP_Text txtAllyStats;
    public TMP_Text txtEnemyStats;

    [Header("===== 结果面板 =====")]
    public GameObject resultPanel;
    public TMP_Text txtResult;
    public TMP_Text txtResultDetails;
    public Button btnBackToMenu;
    public Button btnRestart;
}
