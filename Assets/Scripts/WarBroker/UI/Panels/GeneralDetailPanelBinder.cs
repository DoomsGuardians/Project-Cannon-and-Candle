using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 将军详情面板 Binder（简化版）
/// </summary>
public class GeneralDetailPanelBinder : UIBinder
{
    [Header("将军信息")]
    public Image imgPortrait;
    public TMP_Text txtName;
    public TMP_Text txtPersonality;
    public TMP_Text txtPosition;

    [Header("HP")]
    public Slider sliderHP;
    public TMP_Text txtHP;

    [Header("状态属性")]
    public TMP_Text txtStatus;
    public GameObject trustArea;
    public TMP_Text txtTrust;
    public GameObject moraleArea;
    public TMP_Text txtMorale;
    public GameObject gridPositionArea;
    public TMP_Text txtGridPosition;

    [Header("意图显示")]
    public GameObject intentArea;
    public TMP_Text txtIntent;

    [Header("强化操作")]
    public GameObject buttonArea;
    public Button btnReinforce;
    public Image imgReinforceOrder;

    [Header("篡改操作")]
    public Button btnOverrideATK;
    public Button btnOverrideDEF;
    public Button btnOverrideRET;

    [Header("关闭按钮")]
    public Button btnClose;
}
