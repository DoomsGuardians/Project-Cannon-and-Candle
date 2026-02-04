using UnityEngine;

/// <summary>
/// 战场配置 - 决定使用哪个战场、战线数量和格子数量
/// </summary>
[CreateAssetMenu(fileName = "BattlefieldConfig", menuName = "WarBroker/BattlefieldConfig")]
public class BattlefieldConfig : ScriptableObject
{
    [Tooltip("战场名称/ID")]
    public string battlefieldName;

    [Tooltip("战线数量")]
    public int laneCount = 3;

    [Tooltip("每条战线的格子数量")]
    public int gridCount = 5;

    [Tooltip("战场 Prefab 引用")]
    public GameObject battlefieldPrefab;
}
