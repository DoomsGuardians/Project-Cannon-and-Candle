using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 动态 Lilliput 景深控制器
/// 根据相机模式自动调整焦点距离：
/// - Battlefield 模式：焦点跟随鼠标位置
/// - Battle 模式：焦点跟随玩家侧正在执行动作的单位
/// - FocusUnit 模式：焦点跟随聚焦的目标单位
/// </summary>
public class LilliputController : MonoBehaviour
{
    [Header("过渡设置")]
    [SerializeField] private float transitionSpeed = 5f;

    [Header("射线检测")]
    [SerializeField] private LayerMask groundLayer;

    [Header("调试")]
    [SerializeField] private bool debugMode = false;

    private BattlefieldCameraController cameraController;
    private float targetFocusDistance;
    private float currentFocusDistance;

    // 当前跟随的目标（Battle/FocusUnit 模式）
    private Transform focusTarget;

    // 静态属性供渲染器读取
    public static float? DynamicFocusDistance { get; private set; }

    void Start()
    {
        cameraController = GetComponent<BattlefieldCameraController>();

        // 初始化焦点距离
        currentFocusDistance = 20f;
        targetFocusDistance = 20f;
    }

    void LateUpdate()
    {
        // 根据相机模式更新焦点距离
        UpdateFocusDistance();

        // 平滑过渡
        currentFocusDistance = Mathf.Lerp(currentFocusDistance, targetFocusDistance, Time.deltaTime * transitionSpeed);

        // 设置静态属性供渲染器读取
        DynamicFocusDistance = currentFocusDistance;

        if (debugMode)
        {
            Debug.Log($"[Lilliput] Focus: {currentFocusDistance:F2} -> {targetFocusDistance:F2}");
        }
    }

    void OnDisable()
    {
        // 禁用时清除动态值
        DynamicFocusDistance = null;
    }

    private void UpdateFocusDistance()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null) return;

        var currentMode = cameraController?.CurrentMode ?? BattlefieldCameraController.CameraMode.Battlefield;

        switch (currentMode)
        {
            case BattlefieldCameraController.CameraMode.Battlefield:
                // 鼠标射线与地面交点的距离
                UpdateFromMouseRaycast(mainCamera);
                break;

            case BattlefieldCameraController.CameraMode.Battle:
            case BattlefieldCameraController.CameraMode.FocusUnit:
                // 跟随目标单位的距离
                if (focusTarget != null)
                {
                    targetFocusDistance = Vector3.Distance(mainCamera.transform.position, focusTarget.position);
                }
                break;
        }
    }

    private void UpdateFromMouseRaycast(Camera camera)
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            targetFocusDistance = Vector3.Distance(camera.transform.position, hit.point);
        }
    }

    /// <summary>
    /// 设置焦点目标（由 BattlefieldCameraController 调用）
    /// </summary>
    public void SetFocusTarget(Transform target)
    {
        focusTarget = target;
    }

    /// <summary>
    /// 清除焦点目标
    /// </summary>
    public void ClearFocusTarget()
    {
        focusTarget = null;
    }
}
