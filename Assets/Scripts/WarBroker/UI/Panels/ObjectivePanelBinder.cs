using UnityEngine;
using TMPro;

/// <summary>
/// ObjectivePanel UI Binder
/// 委托任务面板，支持动态生成委托项
/// </summary>
public class ObjectivePanelBinder : UIBinder
{
    [Header("面板标题")]
    public TMP_Text txtTitle;

    [Header("委托任务列表")]
    [Tooltip("委托项的父容器（Vertical Layout Group）")]
    public Transform commissionListRoot;

    [Header("委托项 Prefab")]
    [Tooltip("CommissionItem.prefab 引用")]
    public GameObject commissionItemPrefab;
}
