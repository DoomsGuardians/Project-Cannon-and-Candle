using UnityEngine;
using Cinemachine;

/// <summary>
/// 战场相机控制器 - 使用 Cinemachine 实现平滑相机过渡
/// 支持手动旋转/缩放，以及平滑聚焦和返回
/// </summary>
public class BattlefieldCameraController : MonoBehaviour
{
    public enum CameraMode { Battlefield, Battle, FocusUnit }

    [Header("Cinemachine 引用")]
    [SerializeField] private CinemachineVirtualCamera vcamBattlefield;
    [SerializeField] private CinemachineVirtualCamera vcamBattle;
    [SerializeField] private CinemachineVirtualCamera vcamFocusUnit;
    [SerializeField] private Transform battlefieldPivot;

    [Header("旋转设置")]
    [SerializeField] private float rotateSpeed = 5f;
    [SerializeField] private float minPitch = 10f;
    [SerializeField] private float maxPitch = 80f;

    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    [Header("优先级设置")]
    [SerializeField] private int priorityActive = 20;
    [SerializeField] private int priorityBattle = 15;
    [SerializeField] private int priorityDefault = 10;
    [SerializeField] private int priorityInactive = 0;

    private float currentYaw = 0f;
    private float currentPitch = 45f;
    private float currentDistance = 10f;

    private bool isDragging = false;
    private Vector2 lastPointerPosition;

    private InputService inputService;
    private CameraMode currentMode = CameraMode.Battlefield;

    private CinemachineTransposer battlefieldTransposer;

    private void Start()
    {
        currentDistance = (minZoom + maxZoom) / 2f;

        if (GameRoot.Instance != null)
        {
            inputService = GameRoot.Instance.inputService;
        }

        // 获取 Battlefield 相机的 Transposer
        if (vcamBattlefield != null)
        {
            battlefieldTransposer = vcamBattlefield.GetCinemachineComponent<CinemachineTransposer>();

            // 禁用阻尼，避免手动控制时抖动
            if (battlefieldTransposer != null)
            {
                battlefieldTransposer.m_XDamping = 0f;
                battlefieldTransposer.m_YDamping = 0f;
                battlefieldTransposer.m_ZDamping = 0f;
            }
        }

        // 初始化相机位置
        UpdateOrbitalCamera();
        SetMode(CameraMode.Battlefield);
    }

    private void LateUpdate()
    {
        if (inputService == null) return;

        // 检查输入是否被锁定
        if (!InputRouter.IsEnabled(InputChannel.Gameplay))
            return;

        // 只在 Battlefield 模式下允许手动控制
        if (currentMode == CameraMode.Battlefield)
        {
            HandleRotation();
            HandleZoom();
            UpdateOrbitalCamera();
        }
    }

    private void HandleRotation()
    {
        // 开始拖拽：只有在可以开始相机输入时才开始（不被Screen Space UI阻挡）
        if (inputService.CameraRotatePressed && inputService.CanStartCameraInput())
        {
            isDragging = true;
            lastPointerPosition = inputService.PointerPosition;
        }

        // 结束拖拽
        if (!inputService.CameraRotateHeld)
        {
            isDragging = false;
        }

        // 拖拽中（即使移到UI上也继续）
        if (isDragging)
        {
            Vector2 currentPos = inputService.PointerPosition;
            Vector2 delta = currentPos - lastPointerPosition;
            lastPointerPosition = currentPos;

            currentYaw += delta.x * rotateSpeed * Time.deltaTime * 10f;
            currentPitch -= delta.y * rotateSpeed * Time.deltaTime * 10f;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }
    }

    private void HandleZoom()
    {
        // 只有在可以开始相机输入时才处理缩放（不被Screen Space UI阻挡）
        if (!inputService.CanStartCameraInput())
            return;

        float scroll = inputService.CameraZoom;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed * 0.1f;
            currentDistance = Mathf.Clamp(currentDistance, minZoom, maxZoom);
        }
    }

    private void UpdateOrbitalCamera()
    {
        if (battlefieldTransposer == null) return;

        // 球面坐标转换为 Follow Offset
        float pitchRad = currentPitch * Mathf.Deg2Rad;
        float yawRad = currentYaw * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            currentDistance * Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            currentDistance * Mathf.Sin(pitchRad),
            currentDistance * Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        );

        battlefieldTransposer.m_FollowOffset = offset;
    }

    /// <summary>
    /// 平滑聚焦到指定目标（将军单位）
    /// </summary>
    public void SmoothFocusOn(Transform target)
    {
        if (vcamFocusUnit == null || target == null) return;

        vcamFocusUnit.Follow = target;
        vcamFocusUnit.LookAt = target;
        SetMode(CameraMode.FocusUnit);
    }

    /// <summary>
    /// 平滑跟随战斗（跟随战线锚点）
    /// </summary>
    public void SmoothFollowBattle(Transform laneAnchor)
    {
        if (vcamBattle == null || laneAnchor == null) return;

        vcamBattle.Follow = laneAnchor;
        vcamBattle.LookAt = laneAnchor;
        SetMode(CameraMode.Battle);
    }

    /// <summary>
    /// 平滑返回默认视角（保持用户之前的旋转角度）
    /// </summary>
    public void SmoothReturnToDefault()
    {
        // 不重置 Yaw/Pitch/Distance，只切换回默认相机
        if (battlefieldPivot != null)
        {
            battlefieldPivot.position = Vector3.zero;
        }
        SetMode(CameraMode.Battlefield);
    }

    /// <summary>
    /// 完全重置相机（仅在用户明确要求时使用）
    /// </summary>
    public void FullReset()
    {
        currentYaw = 0f;
        currentPitch = 45f;
        currentDistance = (minZoom + maxZoom) / 2f;

        if (battlefieldPivot != null)
        {
            battlefieldPivot.position = Vector3.zero;
        }

        UpdateOrbitalCamera();
        SetMode(CameraMode.Battlefield);
    }

    /// <summary>
    /// 聚焦到指定位置（兼容旧接口）
    /// </summary>
    public void FocusOn(Vector3 position, float distance = -1f)
    {
        if (battlefieldPivot != null)
        {
            battlefieldPivot.position = position;
        }

        if (distance > 0)
        {
            currentDistance = Mathf.Clamp(distance, minZoom, maxZoom);
            UpdateOrbitalCamera();
        }

        SetMode(CameraMode.Battlefield);
    }

    /// <summary>
    /// 重置视角（兼容旧接口，现在改为平滑返回）
    /// </summary>
    public void ResetView()
    {
        SmoothReturnToDefault();
    }

    private void SetMode(CameraMode mode)
    {
        currentMode = mode;

        if (vcamBattlefield != null)
            vcamBattlefield.Priority = (mode == CameraMode.Battlefield) ? priorityActive : priorityDefault;

        if (vcamBattle != null)
            vcamBattle.Priority = (mode == CameraMode.Battle) ? priorityBattle : priorityInactive;

        if (vcamFocusUnit != null)
            vcamFocusUnit.Priority = (mode == CameraMode.FocusUnit) ? priorityActive : priorityInactive;
    }

    /// <summary>
    /// 获取当前相机模式
    /// </summary>
    public CameraMode CurrentMode => currentMode;
}
