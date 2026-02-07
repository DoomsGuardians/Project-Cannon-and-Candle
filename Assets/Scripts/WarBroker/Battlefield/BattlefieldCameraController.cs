using UnityEngine;
using Cinemachine;
using DG.Tweening;

/// <summary>
/// 战场相机控制器 - 使用 Cinemachine 多相机切换实现平滑相机过渡
/// 三个 VirtualCamera 的职责：
/// - vcamBattlefield: 战场总览，支持玩家旋转/缩放/平移
/// - vcamBattle: 战斗视角，跟随战线锚点，固定角度
/// - vcamFocusUnit: 聚焦单位，跟随目标单位，特写距离
/// </summary>
public class BattlefieldCameraController : MonoBehaviour
{
    public enum CameraMode { Battlefield, Battle, FocusUnit }

    [Header("Cinemachine 引用")]
    [SerializeField] private CinemachineVirtualCamera vcamBattlefield;
    [SerializeField] private CinemachineVirtualCamera vcamBattle;
    [SerializeField] private CinemachineVirtualCamera vcamFocusUnit;
    [SerializeField] private Transform battlefieldPivot;

    [Header("多目标设置")]
    [SerializeField] private CinemachineTargetGroup battleTargetGroup;
    [SerializeField] private float targetWeight = 1f;
    [SerializeField] private float targetRadius = 2f;

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
    [SerializeField] [Range(0f, 1f)] private float softBoundaryRatio = 0.2f;  // 软边界占总范围的比例（20%）

    [Header("平移聚焦设置")]
    [SerializeField] private float panDuration = 0.5f;  // 平移动画时长
    [SerializeField] private Ease panEase = Ease.InOutQuad;  // 平移缓动曲线

    [Header("优先级设置")]
    [SerializeField] private int priorityActive = 20;
    [SerializeField] private int priorityInactive = 0;

    [Header("地形跟随")]
    [SerializeField] private LayerMask groundLayer;           // 地形层
    [SerializeField] private float groundFollowSpeed = 5f;    // 高度跟随平滑速度
    [SerializeField] private float groundRaycastHeight = 100f; // 射线起始高度
    [SerializeField] private float pivotHeightOffset = 0f;    // pivot 距地面的偏移

    // 战场视角的轨道参数（运行时从 vcamBattlefield 初始化）
    private float currentYaw;
    private float currentPitch;
    private float currentDistance;

    private bool isDragging = false;
    private Vector2 lastPointerPosition;

    private InputService inputService;
    private CameraMode currentMode = CameraMode.Battlefield;

    private CinemachineTransposer battlefieldTransposer;
    private Tweener panTweener;  // 当前平移动画

    // 战斗前位置记录
    private Vector3 preBattlePivotPosition;

    private void Start()
    {
        if (GameRoot.Instance != null)
        {
            inputService = GameRoot.Instance.inputService;
        }

        // 获取 Battlefield 相机的 Transposer 并从中初始化轨道参数
        if (vcamBattlefield != null)
        {
            battlefieldTransposer = vcamBattlefield.GetCinemachineComponent<CinemachineTransposer>();

            if (battlefieldTransposer != null)
            {
                // 从 Cinemachine 配置初始化轨道参数
                InitializeFromVcamOffset(battlefieldTransposer.m_FollowOffset);

                // 禁用阻尼，避免手动控制时抖动
                battlefieldTransposer.m_XDamping = 0f;
                battlefieldTransposer.m_YDamping = 0f;
                battlefieldTransposer.m_ZDamping = 0f;

                // 立即同步一次，确保轨道参数和 vcam offset 完全一致
                // 避免 banner 消失后第一次更新时出现瞬间跳动
                UpdateOrbitalCamera();
            }
        }

        SetMode(CameraMode.Battlefield);
    }

    /// <summary>
    /// 从 Cinemachine FollowOffset 反推轨道参数
    /// </summary>
    private void InitializeFromVcamOffset(Vector3 offset)
    {
        currentDistance = offset.magnitude;
        currentDistance = Mathf.Clamp(currentDistance, minZoom, maxZoom);

        if (currentDistance > 0.01f)
        {
            currentPitch = Mathf.Asin(offset.y / currentDistance) * Mathf.Rad2Deg;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            float horizontalDist = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
            if (horizontalDist > 0.01f)
            {
                currentYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            }
            else
            {
                currentYaw = 180f;
            }
        }
        else
        {
            currentYaw = 180f;
            currentPitch = 45f;
            currentDistance = (minZoom + maxZoom) / 2f;
        }
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
            UpdatePivotGroundHeight();
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

        // 计算相机相对于 pivot 的 offset（使用当前的轨道参数）
        Vector3 cameraOffset = CalculateCameraOffset();

        // 计算当前相机位置
        Vector3 currentCameraPosition = battlefieldPivot.position + cameraOffset;

        // 使用方向性减速：只减速朝向边界的移动分量
        Vector3 adjustedMoveDirection = ApplyDirectionalDamping(currentCameraPosition, moveDirection);

        // 计算新的 pivot 位置
        Vector3 newPivotPosition = battlefieldPivot.position + adjustedMoveDirection * panSpeed * Time.deltaTime;

        // 计算相机的实际位置
        Vector3 newCameraPosition = newPivotPosition + cameraOffset;

        // 使用硬限制作为最终保险（防止完全超出边界）
        newCameraPosition = ClampToEllipse(newCameraPosition);

        // 反推 pivot 的最终位置（相机位置 - offset）
        Vector3 finalPivotPosition = newCameraPosition - cameraOffset;

        battlefieldPivot.position = finalPivotPosition;
    }

    /// <summary>
    /// 应用方向性减速：只减速朝向边界的移动分量，平行于边界的移动不受影响
    /// 这样在边界附近仍然可以流畅地沿着边界移动
    /// </summary>
    private Vector3 ApplyDirectionalDamping(Vector3 cameraPosition, Vector3 moveDirection)
    {
        // 计算椭圆的半径和中心
        float radiusX = (panLimitX.y - panLimitX.x) / 2f;
        float radiusZ = (panLimitZ.y - panLimitZ.x) / 2f;
        float centerX = (panLimitX.x + panLimitX.y) / 2f;
        float centerZ = (panLimitZ.x + panLimitZ.y) / 2f;

        // 计算相对于中心的偏移
        float offsetX = cameraPosition.x - centerX;
        float offsetZ = cameraPosition.z - centerZ;

        // 计算归一化距离（椭圆公式）
        float normalizedDistance = Mathf.Sqrt(
            (offsetX * offsetX) / (radiusX * radiusX) +
            (offsetZ * offsetZ) / (radiusZ * radiusZ)
        );

        // 如果在核心区域内，不做任何处理
        float softBoundaryStart = 1f - softBoundaryRatio;
        if (normalizedDistance < softBoundaryStart)
        {
            return moveDirection;
        }

        // 计算从中心指向相机的方向（归一化）
        Vector3 directionFromCenter = new Vector3(
            offsetX / (radiusX * radiusX),
            0,
            offsetZ / (radiusZ * radiusZ)
        );

        if (directionFromCenter.sqrMagnitude > 0.001f)
        {
            directionFromCenter.Normalize();
        }
        else
        {
            return moveDirection;  // 在中心点，不做处理
        }

        // 计算移动方向在"朝向边界"方向上的投影
        float projectionOnBoundary = Vector3.Dot(moveDirection, directionFromCenter);

        // 如果是朝向中心移动（负投影），不做限制
        if (projectionOnBoundary <= 0)
        {
            return moveDirection;
        }

        // 计算减速系数（只影响朝向边界的分量）
        float t = (normalizedDistance - softBoundaryStart) / softBoundaryRatio;
        t = Mathf.Clamp01(t);

        // 使用更柔和的曲线（三次方）
        float dampingFactor = 1f - (t * t * t);

        // 分解移动方向：朝向边界的分量 + 平行于边界的分量
        Vector3 boundaryComponent = directionFromCenter * projectionOnBoundary;
        Vector3 tangentComponent = moveDirection - boundaryComponent;

        // 只对朝向边界的分量应用减速
        Vector3 dampedBoundaryComponent = boundaryComponent * dampingFactor;

        // 重新组合
        return dampedBoundaryComponent + tangentComponent;
    }

    /// <summary>
    /// 将相机位置限制在椭圆形范围内（支持圆形和椭圆形）
    /// </summary>
    private Vector3 ClampToEllipse(Vector3 position)
    {
        // 计算椭圆的半径（从 panLimit 的范围计算）
        float radiusX = (panLimitX.y - panLimitX.x) / 2f;
        float radiusZ = (panLimitZ.y - panLimitZ.x) / 2f;

        // 椭圆的中心点
        float centerX = (panLimitX.x + panLimitX.y) / 2f;
        float centerZ = (panLimitZ.x + panLimitZ.y) / 2f;

        // 计算相对于中心的偏移
        float offsetX = position.x - centerX;
        float offsetZ = position.z - centerZ;

        // 计算归一化距离（椭圆公式）
        float normalizedDistance = Mathf.Sqrt(
            (offsetX * offsetX) / (radiusX * radiusX) +
            (offsetZ * offsetZ) / (radiusZ * radiusZ)
        );

        // 如果在椭圆内，直接返回
        if (normalizedDistance <= 1f)
        {
            return position;
        }

        // 如果超出椭圆，将位置投影到椭圆边界上
        float scale = 1f / normalizedDistance;
        position.x = centerX + offsetX * scale;
        position.z = centerZ + offsetZ * scale;

        return position;
    }

    /// <summary>
    /// 根据当前轨道参数计算相机相对于 pivot 的 offset
    /// </summary>
    private Vector3 CalculateCameraOffset()
    {
        float pitchRad = currentPitch * Mathf.Deg2Rad;
        float yawRad = currentYaw * Mathf.Deg2Rad;

        return new Vector3(
            currentDistance * Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
            currentDistance * Mathf.Sin(pitchRad),
            currentDistance * Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
        );
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

    /// <summary>
    /// 更新 Pivot 的 Y 坐标以跟随地形高度
    /// </summary>
    private void UpdatePivotGroundHeight()
    {
        if (battlefieldPivot == null) return;

        Vector3 pivotPos = battlefieldPivot.position;

        // 从 pivot 上方发射向下的射线
        Vector3 rayOrigin = new Vector3(pivotPos.x, pivotPos.y + groundRaycastHeight, pivotPos.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRaycastHeight * 2f, groundLayer))
        {
            float targetHeight = hit.point.y + pivotHeightOffset;

            // 平滑插值到目标高度
            float newY = Mathf.Lerp(pivotPos.y, targetHeight, Time.deltaTime * groundFollowSpeed);

            battlefieldPivot.position = new Vector3(pivotPos.x, newY, pivotPos.z);
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
    /// 平滑聚焦到指定目标（将军单位）- 靠近单位但保持相同视角角度
    /// </summary>
    public void SmoothFocusOn(Transform target)
    {
        if (vcamFocusUnit == null || target == null) return;

        // 设置 vcamFocusUnit 的 Follow 和 LookAt
        vcamFocusUnit.Follow = target;
        vcamFocusUnit.LookAt = target;

        // 切换到 FocusUnit 模式
        // vcamFocusUnit 在 Inspector 中配置：
        // - Body: Transposer，Follow Offset 与 vcamBattlefield 相同角度但距离更近
        // - Aim: Composer 或 Do Nothing 保持角度
        SetMode(CameraMode.FocusUnit);
    }

    /// <summary>
    /// 平滑跟随战斗目标（双方将军单位）- 使用 CinemachineTargetGroup 框定双方
    /// </summary>
    public void SmoothFollowBattle(Transform allyUnit, Transform enemyUnit)
    {
        if (vcamBattle == null || battleTargetGroup == null) return;
        if (allyUnit == null && enemyUnit == null) return;

        // 清空目标组
        while (battleTargetGroup.m_Targets.Length > 0)
        {
            battleTargetGroup.RemoveMember(battleTargetGroup.m_Targets[0].target);
        }

        // 添加参战单位作为目标
        if (allyUnit != null)
            battleTargetGroup.AddMember(allyUnit, targetWeight, targetRadius);
        if (enemyUnit != null)
            battleTargetGroup.AddMember(enemyUnit, targetWeight, targetRadius);

        // vcamBattle 的 Follow/LookAt 在 Inspector 中设为 battleTargetGroup
        SetMode(CameraMode.Battle);
    }

    /// <summary>
    /// 平滑跟随战斗（跟随战线锚点）- 兼容旧接口
    /// </summary>
    public Tweener SmoothFollowBattle(Transform laneAnchor)
    {
        if (vcamBattle == null || laneAnchor == null) return null;

        // 使用单目标版本
        SmoothFollowBattle(laneAnchor, null);

        return null;
    }

    /// <summary>
    /// 平滑返回默认视角 - 切换回 Battlefield 模式
    /// </summary>
    /// <param name="restorePreBattlePosition">是否恢复战斗前的 pivot 位置</param>
    public void SmoothReturnToDefault(bool restorePreBattlePosition = false)
    {
        // 取消之前的平移动画
        panTweener?.Kill();

        // 切换回 Battlefield 模式
        SetMode(CameraMode.Battlefield);

        // 如果需要恢复战斗前位置
        if (restorePreBattlePosition && battlefieldPivot != null)
        {
            panTweener = battlefieldPivot.DOMove(preBattlePivotPosition, panDuration)
                .SetEase(panEase);
        }
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

        // 设置优先级 - 只有当前模式的相机有高优先级
        if (vcamBattlefield != null)
            vcamBattlefield.Priority = (mode == CameraMode.Battlefield) ? priorityActive : priorityInactive;

        if (vcamBattle != null)
            vcamBattle.Priority = (mode == CameraMode.Battle) ? priorityActive : priorityInactive;

        if (vcamFocusUnit != null)
            vcamFocusUnit.Priority = (mode == CameraMode.FocusUnit) ? priorityActive : priorityInactive;
    }

    /// <summary>
    /// 保存战斗前的 pivot 位置，用于战斗结束后恢复
    /// </summary>
    public void SavePreBattlePosition()
    {
        if (battlefieldPivot != null)
        {
            preBattlePivotPosition = battlefieldPivot.position;
        }
    }

    /// <summary>
    /// 获取 Cinemachine Brain 的默认混合时间
    /// </summary>
    public float GetBlendDuration()
    {
        var brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            return brain.m_DefaultBlend.m_Time;
        }
        return 0.5f; // 默认值
    }

    /// <summary>
    /// 获取当前相机模式
    /// </summary>
    public CameraMode CurrentMode => currentMode;
}
