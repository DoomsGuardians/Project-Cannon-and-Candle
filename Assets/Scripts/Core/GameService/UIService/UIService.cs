// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - UIService UI 服务

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// UI 服务：管理 UI 窗口的生命周期、显示隐藏、层级管理
/// 集成了层级管理、Order 分配、覆盖管理等功能
/// </summary>
public class UIService : ILogic
{
    private Dictionary<string, WindowBase> windowDic = new Dictionary<string, WindowBase>();
    private List<WindowBase> windowList = new List<WindowBase>();

    // Popup队列系统
    private Queue<QueuedPopupRequest> popupQueue = new Queue<QueuedPopupRequest>();
    private WindowBase currentQueuedPopup = null;
    private bool isProcessingQueue = false;

    /// <summary>
    /// 队列中的Popup请求
    /// </summary>
    private class QueuedPopupRequest
    {
        public string WindowName;
        public Action<WindowBase> OnShowCallback;
    }

    // 新增管理器
    private UILayerManager layerManager;
    private UIOrderManager orderManager;
    private UIOcclusionManager occlusionManager;

    // UI 根节点
    private Transform uiRoot;
    private Camera uiCamera;

    /// <summary>
    /// 层级管理器
    /// </summary>
    public UILayerManager LayerManager => layerManager;

    /// <summary>
    /// Order 管理器
    /// </summary>
    public UIOrderManager OrderManager => orderManager;

    /// <summary>
    /// 覆盖管理器
    /// </summary>
    public UIOcclusionManager OcclusionManager => occlusionManager;

    public void OnInit()
    {
        windowDic.Clear();
        windowList.Clear();

        // 初始化管理器
        orderManager = new UIOrderManager();
        occlusionManager = new UIOcclusionManager();
    }

    /// <summary>
    /// 初始化 UI 层级系统
    /// 需要在场景准备好后调用
    /// </summary>
    /// <param name="uiRoot">UI 根节点</param>
    /// <param name="uiCamera">UI 相机（可选，用于 Screen Space - Camera 模式）</param>
    public void InitLayerSystem(Transform uiRoot, Camera uiCamera = null)
    {
        this.uiRoot = uiRoot;
        this.uiCamera = uiCamera;

        layerManager = new UILayerManager(uiRoot, uiCamera);
        layerManager.Initialize();

        Debug.Log("[UIService] 层级系统初始化完成");
    }

    public void OnEnterState() { }

    public void OnUpdate()
    {
        foreach (var window in windowList)
        {
            if (window.isVisible)
            {
                window.OnUpdate();
            }
        }
    }

    public void UnInit()
    {
        foreach (var window in windowList)
        {
            window.OnDestroy();
        }
        windowDic.Clear();
        windowList.Clear();

        // 清理管理器
        layerManager?.Cleanup();
        occlusionManager?.Clear();
        orderManager?.ResetAll();
    }

    /// <summary>
    /// 注册窗口
    /// </summary>
    public void RegisterWindow(string name, WindowBase window)
    {
        if (!windowDic.ContainsKey(name))
        {
            windowDic[name] = window;
            windowList.Add(window);
            window.Name = name;
            window.OnAwake();

            // 分配 Order
            if (orderManager != null)
            {
                window.AllocatedOrder = orderManager.AllocateOrder(window.uiLayer);
                if (window.canvas != null)
                {
                    window.canvas.sortingOrder = window.AllocatedOrder;
                }
            }
        }
    }

    /// <summary>
    /// 显示窗口
    /// </summary>
    public T ShowWindow<T>(string name) where T : WindowBase
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            window.SetVisible(true);
            window.OnShow();

            // 处理全屏窗口覆盖
            if (window.IsFullScreen && occlusionManager != null)
            {
                var visibleWindows = GetVisibleWindows();
                occlusionManager.OnFullScreenWindowOpened(window, visibleWindows);
            }

            return window as T;
        }
        return null;
    }

    /// <summary>
    /// 显示窗口（带动画）
    /// </summary>
    public T ShowWindowWithAnimation<T>(string name, Action onComplete = null) where T : WindowBase
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            window.SetVisible(true);
            window.OnShow();

            // 处理全屏窗口覆盖
            if (window.IsFullScreen && occlusionManager != null)
            {
                var visibleWindows = GetVisibleWindows();
                occlusionManager.OnFullScreenWindowOpened(window, visibleWindows);
            }

            // 播放显示动画
            window.PlayShowAnimation(onComplete);

            return window as T;
        }
        return null;
    }

    /// <summary>
    /// 隐藏窗口
    /// </summary>
    public void HideWindow(string name)
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            // 处理全屏窗口关闭
            if (window.IsFullScreen && occlusionManager != null)
            {
                occlusionManager.OnFullScreenWindowClosed(window);
            }

            window.OnHide();
            window.SetVisible(false);

            // 检查是否是队列中的Popup，如果是则处理下一个
            if (currentQueuedPopup != null && currentQueuedPopup.Name == name)
            {
                currentQueuedPopup = null;
                ProcessNextQueuedPopup();
            }
        }
    }

    /// <summary>
    /// 隐藏窗口（带动画）
    /// </summary>
    public void HideWindowWithAnimation(string name, Action onComplete = null)
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            // 播放隐藏动画，动画完成后再隐藏
            window.PlayHideAnimation(() =>
            {
                // 处理全屏窗口关闭
                if (window.IsFullScreen && occlusionManager != null)
                {
                    occlusionManager.OnFullScreenWindowClosed(window);
                }

                window.OnHide();
                window.SetVisible(false);

                // 检查是否是队列中的Popup，如果是则处理下一个
                if (currentQueuedPopup != null && currentQueuedPopup.Name == name)
                {
                    currentQueuedPopup = null;
                    ProcessNextQueuedPopup();
                }

                onComplete?.Invoke();
            });
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 获取窗口
    /// </summary>
    public T GetWindow<T>(string name) where T : WindowBase
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            return window as T;
        }
        return null;
    }

    /// <summary>
    /// 销毁窗口
    /// </summary>
    public void DestroyWindow(string name)
    {
        if (windowDic.TryGetValue(name, out var window))
        {
            // 释放 Order
            if (orderManager != null)
            {
                orderManager.ReleaseOrder(window.uiLayer, window.AllocatedOrder);
            }

            window.OnDestroy();
            windowDic.Remove(name);
            windowList.Remove(window);
        }
    }

    #region 新增功能方法

    /// <summary>
    /// 获取层级根节点
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>层级的 Transform</returns>
    public Transform GetLayerRoot(UILayer layer)
    {
        return layerManager?.GetLayerRoot(layer);
    }

    /// <summary>
    /// 获取层级 Canvas
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>层级的 Canvas</returns>
    public Canvas GetLayerCanvas(UILayer layer)
    {
        return layerManager?.GetLayerCanvas(layer);
    }

    /// <summary>
    /// 分配 Order
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <returns>分配的 Order 值</returns>
    public int AllocateOrder(UILayer layer)
    {
        return orderManager?.AllocateOrder(layer) ?? (int)layer;
    }

    /// <summary>
    /// 释放 Order
    /// </summary>
    /// <param name="layer">UI 层级</param>
    /// <param name="order">要释放的 Order 值</param>
    public void ReleaseOrder(UILayer layer, int order)
    {
        orderManager?.ReleaseOrder(layer, order);
    }

    /// <summary>
    /// 将窗口置顶
    /// </summary>
    /// <param name="name">窗口名称</param>
    public void BringWindowToTop(string name)
    {
        if (windowDic.TryGetValue(name, out var window) && orderManager != null)
        {
            int newOrder = orderManager.BringToTop(window.uiLayer, window.AllocatedOrder);
            window.AllocatedOrder = newOrder;
            if (window.canvas != null)
            {
                window.canvas.sortingOrder = newOrder;
            }
        }
    }

    /// <summary>
    /// 获取所有可见窗口
    /// </summary>
    /// <returns>可见窗口列表</returns>
    public List<WindowBase> GetVisibleWindows()
    {
        return windowList.Where(w => w.isVisible).ToList();
    }

    /// <summary>
    /// 获取所有窗口
    /// </summary>
    /// <returns>窗口列表</returns>
    public List<WindowBase> GetAllWindows()
    {
        return new List<WindowBase>(windowList);
    }

    /// <summary>
    /// 检查是否有全屏窗口正在显示
    /// </summary>
    public bool HasFullScreenWindow => occlusionManager?.HasFullScreenWindow ?? false;

    /// <summary>
    /// 获取当前全屏窗口
    /// </summary>
    public WindowBase CurrentFullScreenWindow => occlusionManager?.CurrentFullScreenWindow;

    /// <summary>
    /// 隐藏所有窗口
    /// </summary>
    public void HideAllWindows()
    {
        foreach (var window in windowList)
        {
            if (window.isVisible)
            {
                window.OnHide();
                window.SetVisible(false);
            }
        }
    }

    /// <summary>
    /// 隐藏指定层级的所有窗口
    /// </summary>
    /// <param name="layer">UI 层级</param>
    public void HideAllWindowsInLayer(UILayer layer)
    {
        foreach (var window in windowList)
        {
            if (window.isVisible && window.uiLayer == layer)
            {
                window.OnHide();
                window.SetVisible(false);
            }
        }
    }

    /// <summary>
    /// 清理所有窗口（场景切换时调用）
    /// </summary>
    public void ClearAllWindows()
    {
        foreach (var window in windowList)
        {
            if (window.isVisible)
            {
                window.OnHide();
            }
            window.OnDestroy();
            if (window.gameObject != null)
            {
                GameObject.Destroy(window.gameObject);
            }
        }
        windowDic.Clear();
        windowList.Clear();

        // 清理管理器状态
        occlusionManager?.Clear();
        orderManager?.ResetAll();

        // 清理Popup队列
        ClearPopupQueue();
    }

    #endregion

    #region Popup队列系统

    /// <summary>
    /// 将Popup加入队列显示
    /// 如果当前没有队列中的Popup正在显示，则立即显示
    /// 否则等待当前Popup关闭后再显示
    /// </summary>
    /// <typeparam name="T">窗口类型</typeparam>
    /// <param name="name">窗口名称</param>
    /// <param name="onShowCallback">窗口显示后的回调，用于设置数据</param>
    public void ShowPopupQueued<T>(string name, Action<T> onShowCallback = null) where T : WindowBase
    {
        var request = new QueuedPopupRequest
        {
            WindowName = name,
            OnShowCallback = onShowCallback != null ? (w) => onShowCallback(w as T) : null
        };

        popupQueue.Enqueue(request);
        Debug.Log($"[UIService] Popup入队: {name}, 队列长度: {popupQueue.Count}");

        // 如果当前没有正在处理的队列Popup，开始处理
        if (!isProcessingQueue)
        {
            ProcessNextQueuedPopup();
        }
    }

    /// <summary>
    /// 处理队列中的下一个Popup
    /// </summary>
    private void ProcessNextQueuedPopup()
    {
        if (popupQueue.Count == 0)
        {
            isProcessingQueue = false;
            currentQueuedPopup = null;
            Debug.Log("[UIService] Popup队列已清空");
            return;
        }

        isProcessingQueue = true;
        var request = popupQueue.Dequeue();

        if (windowDic.TryGetValue(request.WindowName, out var window))
        {
            currentQueuedPopup = window;

            window.SetVisible(true);
            window.OnShow();

            // 处理全屏窗口覆盖
            if (window.IsFullScreen && occlusionManager != null)
            {
                var visibleWindows = GetVisibleWindows();
                occlusionManager.OnFullScreenWindowOpened(window, visibleWindows);
            }

            // 调用回调设置数据
            request.OnShowCallback?.Invoke(window);

            Debug.Log($"[UIService] 显示队列Popup: {request.WindowName}, 剩余队列: {popupQueue.Count}");
        }
        else
        {
            Debug.LogWarning($"[UIService] 队列Popup未找到: {request.WindowName}");
            // 继续处理下一个
            ProcessNextQueuedPopup();
        }
    }

    /// <summary>
    /// 通知队列系统当前Popup已关闭
    /// 在Popup的HideWindow调用后自动检测，或手动调用
    /// </summary>
    public void OnQueuedPopupClosed(string name)
    {
        if (currentQueuedPopup != null && currentQueuedPopup.Name == name)
        {
            Debug.Log($"[UIService] 队列Popup已关闭: {name}");
            currentQueuedPopup = null;
            ProcessNextQueuedPopup();
        }
    }

    /// <summary>
    /// 清空Popup队列
    /// </summary>
    public void ClearPopupQueue()
    {
        popupQueue.Clear();
        currentQueuedPopup = null;
        isProcessingQueue = false;
        Debug.Log("[UIService] Popup队列已清空");
    }

    /// <summary>
    /// 获取当前队列中的Popup数量
    /// </summary>
    public int QueuedPopupCount => popupQueue.Count;

    /// <summary>
    /// 是否有队列Popup正在显示
    /// </summary>
    public bool HasQueuedPopupShowing => currentQueuedPopup != null && currentQueuedPopup.isVisible;

    #endregion
}
