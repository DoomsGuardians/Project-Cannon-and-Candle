// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - InputService 输入服务

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// 输入服务：统一的输入服务层
/// 基于 Unity Input System，提供游戏和UI输入接口
/// </summary>
public class InputService : ILogic
{
    private WarBrokerInputActions inputActions;

    // Gameplay 输入状态
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public Vector2 PointerPosition { get; private set; }
    public float CameraZoom { get; private set; }
    public bool CameraRotatePressed { get; private set; }
    public bool CameraRotateHeld { get; private set; }
    public bool SelectPressed { get; private set; }
    public bool SelectHeld { get; private set; }
    public bool EndTurnPressed { get; private set; }
    public bool CancelPressed { get; private set; }

    // UI交互状态
    public bool IsPointerOverUI { get; private set; }
    public bool IsUIInteracting { get; private set; }

    // 输入启用状态
    public bool InputEnabled { get; private set; } = true;

    // 内部状态
    private bool wasSelectPressedOnUI = false;
    private bool wasCameraRotatePressedOnUI = false;

    public void OnInit()
    {
        inputActions = new WarBrokerInputActions();
        inputActions.Enable();

        // 绑定输入回调用于UI交互状态追踪
        inputActions.Gameplay.CameraRotate.started += ctx => OnCameraRotateStarted();
        inputActions.Gameplay.CameraRotate.canceled += ctx => OnCameraRotateCanceled();
        inputActions.Gameplay.Select.started += ctx => OnSelectStarted();
        inputActions.Gameplay.Select.canceled += ctx => OnSelectCanceled();
    }

    public void OnEnterState() { }

    public void OnUpdate()
    {
        UpdatePointerOverUI();

        if (!InputEnabled)
        {
            ResetInputs();
            return;
        }

        UpdateInputs();
        UpdateUIInteractionState();
    }

    public void UnInit()
    {
        if (inputActions != null)
        {
            inputActions.UI.Disable();
            inputActions.Gameplay.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
    }

    /// <summary>
    /// 更新输入状态
    /// </summary>
    private void UpdateInputs()
    {
        if (inputActions == null) return;

        // 移动输入
        MoveInput = inputActions.Gameplay.Move.ReadValue<Vector2>();

        // 视角输入
        LookInput = inputActions.Gameplay.Look.ReadValue<Vector2>();

        // 鼠标位置（从UI Action Map获取）
        PointerPosition = inputActions.UI.Point.ReadValue<Vector2>();

        // 相机缩放
        CameraZoom = inputActions.Gameplay.CameraZoom.ReadValue<float>();

        // 相机旋转
        CameraRotatePressed = inputActions.Gameplay.CameraRotate.WasPressedThisFrame();
        CameraRotateHeld = inputActions.Gameplay.CameraRotate.IsPressed();

        // 选择
        SelectPressed = inputActions.Gameplay.Select.WasPressedThisFrame();
        SelectHeld = inputActions.Gameplay.Select.IsPressed();

        // 结束回合（单次触发）
        EndTurnPressed = inputActions.Gameplay.EndTurn.WasPressedThisFrame();

        // 取消（单次触发）
        CancelPressed = inputActions.Gameplay.Cancel.WasPressedThisFrame();
    }

    /// <summary>
    /// 更新鼠标是否在UI上
    /// </summary>
    private void UpdatePointerOverUI()
    {
        if (EventSystem.current != null)
        {
            IsPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }
        else
        {
            IsPointerOverUI = false;
        }
    }

    /// <summary>
    /// 更新UI交互状态
    /// </summary>
    private void UpdateUIInteractionState()
    {
        // 如果在UI上开始了按键操作，标记为UI交互中
        // 直到所有鼠标按键都松开才结束
        if (!SelectHeld && !CameraRotateHeld)
        {
            IsUIInteracting = false;
            wasSelectPressedOnUI = false;
            wasCameraRotatePressedOnUI = false;
        }
        else
        {
            IsUIInteracting = wasSelectPressedOnUI || wasCameraRotatePressedOnUI;
        }
    }

    private void OnCameraRotateStarted()
    {
        // 只有在Screen Space UI上开始的操作才标记为UI交互
        if (IsPointerOverScreenSpaceUI())
        {
            wasCameraRotatePressedOnUI = true;
        }
    }

    private void OnCameraRotateCanceled()
    {
        wasCameraRotatePressedOnUI = false;
    }

    private void OnSelectStarted()
    {
        // 只有在Screen Space UI上开始的操作才标记为UI交互
        if (IsPointerOverScreenSpaceUI())
        {
            wasSelectPressedOnUI = true;
        }
    }

    private void OnSelectCanceled()
    {
        wasSelectPressedOnUI = false;
    }

    /// <summary>
    /// 检查是否可以开始新的游戏输入（不在Screen Space UI上且没有进行UI交互）
    /// World Space UI（如将军气泡）不会阻挡游戏输入
    /// </summary>
    public bool CanStartGameplayInput()
    {
        return !IsPointerOverScreenSpaceUI() && !IsUIInteracting;
    }

    /// <summary>
    /// 检查是否可以进行相机控制（右键拖拽/滚轮缩放）
    /// 只被Screen Space UI阻挡，不被World Space UI（如将军气泡）阻挡
    /// </summary>
    public bool CanStartCameraInput()
    {
        if (IsUIInteracting)
            return false;

        return !IsPointerOverScreenSpaceUI();
    }

    /// <summary>
    /// 检查鼠标是否在Screen Space UI上（排除World Space Canvas）
    /// </summary>
    private bool IsPointerOverScreenSpaceUI()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            return false;

        var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = PointerPosition
        };

        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            var canvas = result.gameObject.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var rootCanvas = canvas.rootCanvas;
                if (rootCanvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 启用输入
    /// </summary>
    public void EnableInput()
    {
        InputEnabled = true;
        inputActions?.Enable();
    }

    /// <summary>
    /// 禁用输入
    /// </summary>
    public void DisableInput()
    {
        InputEnabled = false;
        inputActions?.Disable();
        ResetInputs();
    }

    /// <summary>
    /// 切换到Gameplay输入
    /// </summary>
    public void SwitchToGameplay()
    {
        inputActions?.UI.Disable();
        inputActions?.Gameplay.Enable();
    }

    /// <summary>
    /// 切换到UI输入
    /// </summary>
    public void SwitchToUI()
    {
        inputActions?.Gameplay.Disable();
        inputActions?.UI.Enable();
    }

    /// <summary>
    /// 启用所有输入
    /// </summary>
    public void EnableAllMaps()
    {
        inputActions?.Gameplay.Enable();
        inputActions?.UI.Enable();
    }

    /// <summary>
    /// 重置所有输入状态
    /// </summary>
    private void ResetInputs()
    {
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        CameraZoom = 0f;
        CameraRotatePressed = false;
        CameraRotateHeld = false;
        SelectPressed = false;
        SelectHeld = false;
        EndTurnPressed = false;
        CancelPressed = false;
    }
}
