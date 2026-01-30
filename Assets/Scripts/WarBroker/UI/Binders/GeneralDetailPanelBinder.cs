using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将军详情面板 Binder
/// </summary>
public class GeneralDetailPanelBinder : UIBinder
{
    [Header("将军信息")]
    public Text txtName;
    public Text txtPersonality;
    public Text txtPosition;

    [Header("属性条")]
    public Slider sliderHP;
    public Slider sliderTrust;
    public Slider sliderMorale;
    public Text txtHP;
    public Text txtTrust;
    public Text txtMorale;

    [Header("状态")]
    public Text txtStatus;
    public Text txtSkills;

    [Header("意图显示")]
    public Image imgIntentBubble;
    public Text txtIntent;
    public Text txtIntentSource;

    [Header("操作按钮")]
    public Button btnReinforce;
    public Text txtReinforceCost;
    public Button btnOverride;
    public Dropdown ddOverrideType;
    public Text txtOverrideCost;

    [Header("关闭按钮")]
    public Button btnClose;
}
