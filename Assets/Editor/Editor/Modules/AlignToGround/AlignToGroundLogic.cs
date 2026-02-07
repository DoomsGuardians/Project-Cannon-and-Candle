using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 物理对齐逻辑 - 纯逻辑，与UI完全解耦
/// </summary>
public static class AlignToGroundLogic
{
    /// <summary>
    /// 对齐设置
    /// </summary>
    public class Settings
    {
        public int GroundLayerMask = -1;
        public bool AlignToNormal = false;  // 是否对齐到表面法线
        public float MaxRayDistance = 1000f;  // 最大射线距离
    }

    /// <summary>
    /// 对齐到表面
    /// </summary>
    public static void SnapToGround(Transform[] transforms, Settings settings)
    {
        if (transforms == null || transforms.Length == 0)
            return;

        UndoUtil.RecordObjects(transforms.Select(t => t.gameObject).ToArray(), "Snap To Surface");

        foreach (var t in transforms)
        {
            SnapSingleObject(t, settings);
        }
    }

    private static void SnapSingleObject(Transform t, Settings settings)
    {
        // 收集自身及子物体的所有 Collider，用于射线忽略
        var selfColliders = t.GetComponentsInChildren<Collider>();

        // 计算射线起始高度
        float height = 5.0f;
        Bounds? bounds = GetCombinedBounds(t);
        if (bounds.HasValue)
        {
            height = bounds.Value.size.y + 1.0f;
        }

        Vector3 rayOrigin = t.position + Vector3.up * height;

        // 发射射线向下检测
        RaycastHit? validHit = FindValidHit(rayOrigin, Vector3.down, settings.MaxRayDistance,
            settings.GroundLayerMask, selfColliders);

        if (!validHit.HasValue)
        {
            // 如果向下没找到，尝试从更高的位置
            rayOrigin = t.position + Vector3.up * (height + 50f);
            validHit = FindValidHit(rayOrigin, Vector3.down, settings.MaxRayDistance,
                settings.GroundLayerMask, selfColliders);
        }

        if (!validHit.HasValue)
        {
            Debug.LogWarning($"[AlignToGround] 未找到可吸附的表面: {t.name}，请检查目标物体是否有 Collider");
            return;
        }

        RaycastHit hit = validHit.Value;

        // 计算新位置
        Vector3 newPos = hit.point;

        // 如果有 Renderer，保持物体底部对齐到表面
        if (bounds.HasValue)
        {
            float bottomOffset = t.position.y - bounds.Value.min.y;
            newPos.y += bottomOffset;
        }

        // 应用位置
        t.position = newPos;

        // 对齐到表面法线
        if (settings.AlignToNormal)
        {
            AlignToSurfaceNormal(t, hit.normal);
        }
    }

    /// <summary>
    /// 找到有效的射线碰撞点（排除自身和子物体）
    /// </summary>
    private static RaycastHit? FindValidHit(Vector3 origin, Vector3 direction, float maxDistance,
        int layerMask, Collider[] ignoreColliders)
    {
        // 使用 RaycastAll 获取所有碰撞点
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
            return null;

        // 按距离排序
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // 找到第一个不属于自身的碰撞点
        HashSet<Collider> ignoreSet = new HashSet<Collider>(ignoreColliders);
        foreach (var hit in hits)
        {
            if (!ignoreSet.Contains(hit.collider))
            {
                return hit;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取物体及子物体的组合包围盒
    /// </summary>
    private static Bounds? GetCombinedBounds(Transform t)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return null;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }
        return combinedBounds;
    }

    /// <summary>
    /// 将物体的 up 方向对齐到表面法线
    /// </summary>
    private static void AlignToSurfaceNormal(Transform t, Vector3 surfaceNormal)
    {
        // 保持物体原来的前方朝向（投影到表面上）
        Vector3 currentForward = t.forward;

        // 将 forward 投影到表面平面上
        Vector3 projectedForward = Vector3.ProjectOnPlane(currentForward, surfaceNormal).normalized;

        // 如果投影后的 forward 太短（几乎垂直于表面），使用世界 forward
        if (projectedForward.sqrMagnitude < 0.001f)
        {
            projectedForward = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal).normalized;
            if (projectedForward.sqrMagnitude < 0.001f)
            {
                projectedForward = Vector3.ProjectOnPlane(Vector3.right, surfaceNormal).normalized;
            }
        }

        // 构建新的旋转：up 对齐法线，forward 对齐投影后的前方
        Quaternion newRotation = Quaternion.LookRotation(projectedForward, surfaceNormal);
        t.rotation = newRotation;
    }
}

