using UnityEngine;
using UnityEngine.UI;

public class GameplayWindowBinder : UIBinder
{
    [Header("顶部状态栏")]
    public Text txtTurn;
    public Text txtPhase;
    public Text txtCash;
    public Text txtNetWorth;
    public Text txtAudit;

    [Header("Tab 按钮")]
    public Button btnMarket;
    public Button btnBattle;
    public Button btnGeneral;
    public Button btnIntel;
    public Button btnHistory;

    [Header("底部操作栏")]
    public Button btnEndTurn;
    public Text txtEventInfo;
}
