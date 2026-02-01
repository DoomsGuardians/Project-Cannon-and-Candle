using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 将军详情面板 Binder（简化版）
/// </summary>
public class GeneralDetailPanelBinder : UIBinder
{
    [Header("将军信息")]
    public TMP_Text txtName;
    public TMP_Text txtPersonality;
    public TMP_Text txtPosition;

    [Header("HP")]
    public Slider sliderHP;
    public TMP_Text txtHP;

    [Header("意图显示")]
    public TMP_Text txtIntent;

    [Header("强化操作")]
    public Button btnReinforce;

    [Header("篡改操作")]
    public Button btnOverrideATK;
    public Button btnOverrideDEF;
    public Button btnOverrideRET;

    [Header("关闭按钮")]
    public Button btnClose;
}
