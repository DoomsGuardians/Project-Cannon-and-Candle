using UnityEngine;

/// <summary>
/// 锡兵控制脚本
/// 管理单个士兵的兵种和姿态切换
/// </summary>
public class TinSoldier : MonoBehaviour
{
    [Header("姿态模型")]
    [SerializeField] private GameObject atkModel;
    [SerializeField] private GameObject defModel;
    [SerializeField] private GameObject retModel;

    public SoldierType SoldierType { get; private set; }

    private OrderType? currentPose;

    /// <summary>
    /// 初始化士兵
    /// </summary>
    public void Initialize(SoldierType type)
    {
        SoldierType = type;
        // 默认显示防守姿态
        SetPose(null);
    }

    /// <summary>
    /// 根据指令切换姿态
    /// </summary>
    /// <param name="intent">将军的最终意图，null表示默认姿态(DEF)</param>
    public void SetPose(OrderType? intent)
    {
        if (currentPose == intent) return;
        currentPose = intent;

        // 隐藏所有模型
        if (atkModel != null) atkModel.SetActive(false);
        if (defModel != null) defModel.SetActive(false);
        if (retModel != null) retModel.SetActive(false);

        // 根据意图显示对应模型
        switch (intent)
        {
            case OrderType.ATK:
                if (atkModel != null) atkModel.SetActive(true);
                break;
            case OrderType.RET:
                if (retModel != null) retModel.SetActive(true);
                break;
            case OrderType.DEF:
            case null:
            default:
                // 默认显示防守姿态
                if (defModel != null) defModel.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// 设置士兵材质（用于区分敌我）
    /// </summary>
    public void SetMaterial(Material mat)
    {
        if (mat == null) return;

        SetModelMaterial(atkModel, mat);
        SetModelMaterial(defModel, mat);
        SetModelMaterial(retModel, mat);
    }

    private void SetModelMaterial(GameObject model, Material mat)
    {
        if (model == null) return;

        var renderer = model.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = mat;
        }

        // 也处理子物体的渲染器
        var childRenderers = model.GetComponentsInChildren<Renderer>();
        foreach (var r in childRenderers)
        {
            r.material = mat;
        }
    }
}
