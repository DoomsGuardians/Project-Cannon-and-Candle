using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 档案弹窗 UI 引用绑定器
/// </summary>
public class ArchivePopupBinder : MonoBehaviour
{
    [Header("标题")]
    public TMP_Text txtTitle;

    [Header("总资产")]
    public TMP_Text txtTotalWealth;

    [Header("统计数据")]
    public TMP_Text txtStatistics;

    [Header("历史战役列表")]
    public Transform historyContainer;
    public GameObject historyItemPrefab;
    public TMP_Text txtNoHistory;

    [Header("按钮")]
    public Button btnClose;
    public Button btnClearData;
}
