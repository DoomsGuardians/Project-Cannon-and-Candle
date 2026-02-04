using System;
using UnityEngine;

/// <summary>
/// 兵种配置
/// 定义每种兵种的显示名称和Prefab路径
/// </summary>
[CreateAssetMenu(fileName = "SoldierTypeConfig", menuName = "WarBroker/SoldierTypeConfig")]
public class SoldierTypeConfig : ScriptableObject
{
    [Serializable]
    public class SoldierTypeData
    {
        public SoldierType type;
        public string displayName;
        [Tooltip("Prefab路径，如 Prefabs/WarBroker/Battlefield/TinSoldier_Pikeman")]
        public string prefabPath;
    }

    [Header("兵种配置列表")]
    public SoldierTypeData[] soldierTypes;

    /// <summary>
    /// 获取指定兵种的Prefab路径
    /// </summary>
    public string GetPrefabPath(SoldierType type)
    {
        if (soldierTypes == null) return null;

        foreach (var data in soldierTypes)
        {
            if (data.type == type)
                return data.prefabPath;
        }

        Debug.LogWarning($"[SoldierTypeConfig] 未找到兵种 {type} 的配置");
        return null;
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
