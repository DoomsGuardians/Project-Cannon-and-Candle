using UnityEngine;

/// <summary>
/// 锚点指示器 - 在 Scene 视图中显示位置和朝向
/// </summary>
public class AnchorGizmo : MonoBehaviour
{
    [Header("显示设置")]
    public Color gizmoColor = Color.yellow;
    public float arrowLength = 1f;
    public float sphereRadius = 0.1f;

    [Header("箭头样式")]
    public bool showArrowHead = true;
    public float arrowHeadLength = 0.2f;
    public float arrowHeadAngle = 25f;

    [Header("可选显示")]
    public bool showUpAxis = false;
    public bool showRightAxis = false;
    public bool showLabel = true;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawAnchorGizmo();
    }

    private void DrawAnchorGizmo()
    {
        // 绘制中心球体
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, sphereRadius);

        // 绘制前方箭头 (Z轴 - 蓝色或自定义颜色)
        DrawArrow(transform.position, transform.forward, arrowLength, gizmoColor);

        // 可选：绘制上方轴 (Y轴 - 绿色)
        if (showUpAxis)
        {
            DrawArrow(transform.position, transform.up, arrowLength * 0.5f, Color.green);
        }

        // 可选：绘制右方轴 (X轴 - 红色)
        if (showRightAxis)
        {
            DrawArrow(transform.position, transform.right, arrowLength * 0.5f, Color.red);
        }

        // 可选：绘制标签
        if (showLabel)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * (sphereRadius + 0.1f), gameObject.name);
        }
    }

    private void DrawArrow(Vector3 origin, Vector3 direction, float length, Color color)
    {
        Gizmos.color = color;
        Vector3 end = origin + direction.normalized * length;

        // 绘制主线
        Gizmos.DrawLine(origin, end);

        // 绘制箭头头部
        if (showArrowHead && length > 0)
        {
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;

            Gizmos.DrawLine(end, end + right * arrowHeadLength);
            Gizmos.DrawLine(end, end + left * arrowHeadLength);
        }
    }
#endif
}
