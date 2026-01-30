using UnityEngine;

/// <summary>
/// 战场相机控制器
/// 支持旋转和缩放，受 InputRouter 控制
/// </summary>
public class BattlefieldCamera : MonoBehaviour
{
    [Header("旋转设置")]
    [SerializeField] private float rotateSpeed = 5f;
    [SerializeField] private float minPitch = 10f;
    [SerializeField] private float maxPitch = 80f;

    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    [Header("目标设置")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = Vector3.zero;

    private float currentYaw = 0f;
    private float currentPitch = 45f;
    private float currentDistance = 10f;

    private bool isDragging = false;
    private Vector3 lastMousePosition;

    private void Start()
    {
        currentDistance = (minZoom + maxZoom) / 2f;
        UpdateCameraPosition();
    }

    private void Update()
    {
        // 检查输入是否被锁定
        if (!InputRouter.IsEnabled(InputChannel.Gameplay))
            return;

        HandleRotation();
        HandleZoom();
        UpdateCameraPosition();
    }

    private void HandleRotation()
    {
        // 右键拖拽旋转
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            currentYaw += delta.x * rotateSpeed * Time.deltaTime * 10f;
            currentPitch -= delta.y * rotateSpeed * Time.deltaTime * 10f;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed * 10f;
            currentDistance = Mathf.Clamp(currentDistance, minZoom, maxZoom);
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPos = target != null ? target.position + targetOffset : targetOffset;

        // 球面坐标转换
        float pitchRad = currentPitch * Mathf.Deg2Rad;
        float yawRad = currentYaw * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            currentDistance * Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            currentDistance * Mathf.Sin(pitchRad),
            currentDistance * Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        );

        transform.position = targetPos + offset;
        transform.LookAt(targetPos);
    }

    /// <summary>设置相机目标</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>重置相机视角</summary>
    public void ResetView()
    {
        currentYaw = 0f;
        currentPitch = 45f;
        currentDistance = (minZoom + maxZoom) / 2f;
        UpdateCameraPosition();
    }

    /// <summary>聚焦到指定位置</summary>
    public void FocusOn(Vector3 position, float distance = -1f)
    {
        targetOffset = position;
        if (distance > 0)
        {
            currentDistance = Mathf.Clamp(distance, minZoom, maxZoom);
        }
        UpdateCameraPosition();
    }
}
