using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 委托任务项 UI Binder
/// 挂在 CommissionItem.prefab 上，持有 UI 元素引用
/// </summary>
public class CommissionItemBinder : UIBinder
{
    [Header("委托信息（静态）")]
    [Tooltip("委托名称，如 '斩首胜利'")]
    public TMP_Text txtName;

    [Tooltip("委托描述（含奖励），如 '占领敌方大本营，奖金 $200'")]
    public TMP_Text txtDescription;

    [Tooltip("委托人头像（可选）")]
    public Image imgAvatar;

    [Header("进度信息（动态）")]
    [Tooltip("进度状态，如 '[0%]' 或 '[完成]'")]
    public TMP_Text txtProgress;

    /// <summary>
    /// 运行时绑定的配置（由 ObjectivePanel 设置）
    /// </summary>
    [HideInInspector]
    public CommissionConfig Config;
}
