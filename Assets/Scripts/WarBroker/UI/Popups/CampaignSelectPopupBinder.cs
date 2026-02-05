using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战役选择弹窗 Binder
/// </summary>
public class CampaignSelectPopupBinder : MonoBehaviour
{
    [Header("容器")]
    public Transform itemContainer;
    public GameObject itemPrefab;

    [Header("按钮")]
    public Button btnClose;

    [Header("文本")]
    public TMP_Text txtTitle;
}
