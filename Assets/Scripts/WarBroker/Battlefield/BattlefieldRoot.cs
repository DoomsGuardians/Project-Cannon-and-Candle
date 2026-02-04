using UnityEngine;

/// <summary>
/// 战场实例 - 持有格子锚点的引用
/// </summary>
public class BattlefieldRoot : MonoBehaviour
{
    /// <summary>
    /// 单条战线的锚点配置
    /// </summary>
    [System.Serializable]
    public class LaneAnchors
    {
        [Tooltip("格子锚点（索引0是红方大本营，最后一个是蓝方大本营）")]
        public Transform[] gridAnchors;
    }

    [Header("战线配置")]
    [Tooltip("所有战线的锚点配置")]
    public LaneAnchors[] lanes;

    /// <summary>
    /// 获取战线数量
    /// </summary>
    public int LaneCount => lanes?.Length ?? 0;

    /// <summary>
    /// 获取指定格子的世界坐标
    /// </summary>
    /// <param name="laneIndex">战线索引（0-based）</param>
    /// <param name="gridIndex">格子索引（0-based，0是红方大本营）</param>
    /// <returns>格子世界坐标</returns>
    public Vector3 GetGridPosition(int laneIndex, int gridIndex)
    {
        if (lanes == null || laneIndex < 0 || laneIndex >= lanes.Length)
        {
            Debug.LogError($"[BattlefieldRoot] Lane index {laneIndex} out of range (0-{(lanes?.Length ?? 0) - 1})");
            return Vector3.zero;
        }

        var lane = lanes[laneIndex];
        var anchors = lane.gridAnchors;

        if (anchors == null || anchors.Length == 0)
        {
            Debug.LogError($"[BattlefieldRoot] Lane {laneIndex} grid anchors not set");
            return Vector3.zero;
        }
        if (gridIndex < 0 || gridIndex >= anchors.Length)
        {
            Debug.LogError($"[BattlefieldRoot] Grid index {gridIndex} out of range (0-{anchors.Length - 1}) in lane {laneIndex}");
            return Vector3.zero;
        }
        if (anchors[gridIndex] == null)
        {
            Debug.LogError($"[BattlefieldRoot] Grid anchor at index {gridIndex} in lane {laneIndex} is null");
            return Vector3.zero;
        }
        return anchors[gridIndex].position;
    }

    /// <summary>
    /// 验证战场配置是否与期望匹配
    /// </summary>
    /// <param name="expectedLaneCount">期望的战线数量</param>
    /// <param name="expectedGridCount">期望的每条战线格子数量</param>
    public void ValidateConfig(int expectedLaneCount, int expectedGridCount)
    {
        if (lanes == null || lanes.Length != expectedLaneCount)
        {
            Debug.LogError($"[BattlefieldRoot] Lane count mismatch: expected {expectedLaneCount}, got {lanes?.Length ?? 0}");
            return;
        }

        for (int i = 0; i < lanes.Length; i++)
        {
            var lane = lanes[i];
            if (lane.gridAnchors == null || lane.gridAnchors.Length != expectedGridCount)
            {
                Debug.LogError($"[BattlefieldRoot] Lane {i} grid count mismatch: expected {expectedGridCount}, got {lane.gridAnchors?.Length ?? 0}");
            }
        }
    }
}
