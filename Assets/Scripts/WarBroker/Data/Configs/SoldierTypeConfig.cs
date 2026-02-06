using System;
using UnityEngine;

/// <summary>
/// 兵种配置
/// 定义每种兵种的显示名称和Prefab（支持按阵营区分）
/// </summary>
[CreateAssetMenu(fileName = "SoldierTypeConfig", menuName = "WarBroker/SoldierTypeConfig")]
public class SoldierTypeConfig : ScriptableObject
{
    [Serializable]
    public class SoldierTypeData
    {
        public SoldierType type;
        public string displayName;
        [Tooltip("己方阵营使用的Prefab")]
        public GameObject allyPrefab;
        [Tooltip("敌方阵营使用的Prefab（留空则使用己方Prefab）")]
        public GameObject enemyPrefab;

        [HideInInspector]
        [Obsolete("使用 allyPrefab 和 enemyPrefab 代替")]
        public GameObject prefab;
    }

    [Header("兵种配置列表")]
    public SoldierTypeData[] soldierTypes;

    /// <summary>
    /// 获取指定兵种和阵营的Prefab
    /// </summary>
    /// <param name="type">兵种类型</param>
    /// <param name="isAlly">是否为己方阵营</param>
    public GameObject GetPrefab(SoldierType type, bool isAlly = true)
    {
        if (soldierTypes == null) return null;

        foreach (var data in soldierTypes)
        {
            if (data.type == type)
            {
                if (isAlly)
                {
                    // 己方：优先使用 allyPrefab，兼容旧的 prefab 字段
#pragma warning disable 0618
                    return data.allyPrefab != null ? data.allyPrefab : data.prefab;
#pragma warning restore 0618
                }
                else
                {
                    // 敌方：优先使用 enemyPrefab，如果没有则回退到己方 prefab
#pragma warning disable 0618
                    if (data.enemyPrefab != null)
                        return data.enemyPrefab;
                    if (data.allyPrefab != null)
                        return data.allyPrefab;
                    return data.prefab;
#pragma warning restore 0618
                }
            }
        }

        Debug.LogWarning($"[SoldierTypeConfig] 未找到兵种 {type} 的配置");
        return null;
    }

    /// <summary>
    /// 获取指定兵种的Prefab（兼容旧接口，默认返回己方）
    /// </summary>
    [Obsolete("使用 GetPrefab(SoldierType type, bool isAlly) 代替")]
    public GameObject GetPrefab(SoldierType type)
    {
        return GetPrefab(type, true);
    }

    /// <summary>
    /// 获取指定兵种的显示名称
    /// </summary>
    public string GetDisplayName(SoldierType type)
    {
        if (soldierTypes == null) return type.ToString();

        foreach (var data in soldierTypes)
        {
            if (data.type == type)
                return data.displayName;
        }

        return type.ToString();
    }
}
