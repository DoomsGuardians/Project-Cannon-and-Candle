using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 将军详情面板 Binder
/// </summary>
public class GeneralDetailPanelBinder : UIBinder
{
    [Header("将军信息")]
    public TMP_Text txtName;
    public TMP_Text txtPersonality;
    public TMP_Text txtPosition;

    [Header("属性条")]
    public Slider sliderHP;
    public Slider sliderTrust;
    public Slider sliderMorale;
    public TMP_Text txtHP;
    public TMP_Text txtTrust;
    public TMP_Text txtMorale;

    [Header("状态")]
    public TMP_Text txtStatus;
    public TMP_Text txtSkills;

    [Header("意图显示")]
    public Image imgIntentBubble;
    public TMP_Text txtIntent;
    public TMP_Text txtIntentSource;

    [Header("操作按钮")]
    public Button btnReinforce;
    public TMP_Text txtReinforceCost;
    public Button btnOverride;
    public TMP_Dropdown ddOverrideType;
    public TMP_Text txtOverrideCost;

    [Header("关闭按钮")]
    public Button btnClose;
}
