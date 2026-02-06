using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoPanelBinder : UIBinder
{
    public TMP_Text txtMarketIntel;
    public TMP_Text txtBattleIntel;
    public TMP_Text txtAllyIntel;
    public TMP_Text txtEnemyIntel;
    public TMP_Text txtBattleHistory;

    [Header("K线图")]
    public KLineChartView chartAtk;
    public KLineChartView chartDef;
    public KLineChartView chartRet;
}
