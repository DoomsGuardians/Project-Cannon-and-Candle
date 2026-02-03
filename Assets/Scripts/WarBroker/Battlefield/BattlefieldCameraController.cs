using UnityEngine;
using Cinemachine;
using DG.Tweening;

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

    [Header("键盘平移设置")]
    [SerializeField] private float panSpeed = 15f;
    [SerializeField] private Vector2 panLimitX = new Vector2(-30f, 30f);
    [SerializeField] private Vector2 panLimitZ = new Vector2(-30f, 30f);

    [Header("平移聚焦设置")]
    [SerializeField] private float panDuration = 0.5f;  // 平移动画时长
    [SerializeField] private Ease panEase = Ease.InOutQuad;  // 平移缓动曲线

    [Header("优先级设置")]
    [SerializeField] private int priorityActive = 20;
    [SerializeField] private int priorityBattle = 15;
    [SerializeField] private int priorityDefault = 10;
    [SerializeField] private int priorityInactive = 0;

    [Header("初始视角设置")]
    [SerializeField] private float initialYaw = 180f;    // 初始水平角度（180=面向敌方）
    [SerializeField] private float initialPitch = 45f;   // 初始俯仰角度
    [SerializeField] private float initialDistance = 15f; // 初始距离

    private float currentYaw;
    private float currentPitch;
    private float currentDistance;

    private bool isDragging = false;
    private Vector2 lastPointerPosition;

    private InputService inputService;
    private CameraMode currentMode = CameraMode.Battlefield;

    private CinemachineTransposer battlefieldTransposer;
    private Tweener panTweener;  // 当前平移动画

    private void Start()
    {
        // 使用 Inspector 中配置的初始值
        currentYaw = initialYaw;
        currentPitch = initialPitch;
        currentDistance = Mathf.Clamp(initialDistance, minZoom, maxZoom);

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
            HandleKeyboardPan();
            HandleRotation();
            HandleZoom();
            UpdateOrbitalCamera();
        }
    }

    /// <summary>
    /// 处理键盘平移输入 (WASD)
    /// </summary>
    private void HandleKeyboardPan()
    {
        Vector2 moveInput = inputService.MoveInput;
        if (moveInput == Vector2.zero) return;

        // 取消正在进行的平移动画，让玩家接管控制
        panTweener?.Kill();

        // 获取相机的水平朝向（忽略Y轴）
        var mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = mainCamera.transform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();

        // 根据相机朝向计算移动方向
        Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x);

        // 计算新位置
        Vector3 newPosition = battlefieldPivot.position + moveDirection * panSpeed * Time.deltaTime;

        // 限制在范围内
        newPosition.x = Mathf.Clamp(newPosition.x, panLimitX.x, panLimitX.y);
        newPosition.z = Mathf.Clamp(newPosition.z, panLimitZ.x, panLimitZ.y);

        battlefieldPivot.position = newPosition;
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
    /// 平滑聚焦到指定目标（将军单位）- 使用平移而非旋转
    /// </summary>
    public void SmoothFocusOn(Transform target)
    {
        if (battlefieldPivot == null || target == null) return;

        // 取消之前的平移动画
        panTweener?.Kill();

        // 平移 pivot 到目标位置，保持当前视角
        panTweener = battlefieldPivot.DOMove(target.position, panDuration)
            .SetEase(panEase);

        // 保持在 Battlefield 模式，不切换相机
        SetMode(CameraMode.Battlefield);
    }

    /// <summary>
    /// 平滑跟随战斗（跟随战线锚点）- 使用平移而非旋转
    /// </summary>
    /// <returns>返回平移动画的 Tweener，可用于等待完成</returns>
    public Tweener SmoothFollowBattle(Transform laneAnchor)
    {
        if (battlefieldPivot == null || laneAnchor == null) return null;

        // 取消之前的平移动画
        panTweener?.Kill();

        // 平移 pivot 到战线锚点位置，保持当前视角
        panTweener = battlefieldPivot.DOMove(laneAnchor.position, panDuration)
            .SetEase(panEase);

        // 保持在 Battlefield 模式，不切换相机
        SetMode(CameraMode.Battlefield);

        return panTweener;
    }

    /// <summary>
    /// 平滑返回默认视角（保持用户之前的旋转角度）
    /// </summary>
    public void SmoothReturnToDefault()
    {
        // 取消之前的平移动画
        panTweener?.Kill();

        // 平移回原点，不重置 Yaw/Pitch/Distance
        if (battlefieldPivot != null)
        {
            panTweener = battlefieldPivot.DOMove(Vector3.zero, panDuration)
                .SetEase(panEase);
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
