using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectivePanelBinder : UIBinder
{
    [Header("主目标")]
    public TMP_Text txtObjectiveTitle;
    public TMP_Text txtObjectiveDescription;
    public TMP_Text txtProgress;
    public TMP_Text txtPnL;

    [Header("委托任务列表")]
    public Transform commissionListRoot;

    [Header("委托任务项（可选，4 个固定委托）")]
    public TMP_Text txtWinWar;
    public TMP_Text txtShortCountry;
    public TMP_Text txtTraitor;
    public TMP_Text txtMeatGrinder;
}
