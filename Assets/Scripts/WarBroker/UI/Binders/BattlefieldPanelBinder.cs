using UnityEngine;
using UnityEngine.UI;

public class BattlefieldPanelBinder : UIBinder
{
    [Header("左翼")]
    public Slider sliderLeft;
    public Text txtLeftAlly;
    public Text txtLeftEnemy;

    [Header("中军")]
    public Slider sliderCenter;
    public Text txtCenterAlly;
    public Text txtCenterEnemy;

    [Header("右翼")]
    public Slider sliderRight;
    public Text txtRightAlly;
    public Text txtRightEnemy;

    [Header("战斗结果")]
    public Text txtBattleResults;
    public Text txtEventInfo;
}
