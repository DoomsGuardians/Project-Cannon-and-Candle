using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Victor 信件弹窗 Binder
/// </summary>
public class VictorLetterPopupBinder : UIBinder
{
    [Header("立绘")]
    public Image imgPortrait;

    [Header("文本")]
    public TMP_Text txtTitle;
    public TMP_Text txtContent;

    [Header("按钮")]
    public Button btnConfirm;
    public TMP_Text txtBtnConfirm;
}
