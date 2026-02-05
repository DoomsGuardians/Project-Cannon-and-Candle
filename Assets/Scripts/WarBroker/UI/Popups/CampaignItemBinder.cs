using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战役列表项 Binder
/// </summary>
public class CampaignItemBinder : MonoBehaviour
{
    [Header("显示")]
    public TMP_Text txtName;
    public TMP_Text txtTicket;
    public Image imgThumbnail;

    [Header("交互")]
    public Button btnSelect;
}
