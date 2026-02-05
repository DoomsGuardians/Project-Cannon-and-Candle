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
        // 默认显示防守姿态，强制刷新
        SetPose(null, true);
    }

    /// <summary>
    /// 根据指令切换姿态
    /// </summary>
    /// <param name="intent">将军的最终意图，null表示默认姿态(DEF)</param>
    /// <param name="force">是否强制刷新</param>
    public void SetPose(OrderType? intent, bool force = false)
    {
        if (!force && currentPose == intent) return;
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
}
