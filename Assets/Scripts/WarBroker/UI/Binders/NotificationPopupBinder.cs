using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// NotificationPopup 的 Binder
/// </summary>
public class NotificationPopupBinder : UIBinder
{
    public TMP_Text txtTitle;
    public TMP_Text txtContent;
    public Button btnConfirm;
    public TMP_Text txtBtnConfirm;

    [Header("音效")]
    public AudioClip sfxEnter;
}
