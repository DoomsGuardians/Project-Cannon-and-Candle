using UnityEngine;
using TMPro;

/// <summary>
/// 轻量 Tooltip 面板：Paradox 风格，跟随鼠标显示
/// </summary>
public class TooltipPanel : WindowBase
{
    private TMP_Text txtTitle;
    private TMP_Text txtContent;
    private RectTransform panelRect;
    private Canvas rootCanvas;

    private bool isShowing;
    private Vector2 offset = new Vector2(15f, -15f);

    public TooltipPanel()
    {
        uiLayer = UILayer.Tip;  // 最高层级，确保不被其他 UI 遮挡
    }

    public override void OnAwake()
    {
        base.OnAwake();

        var b = gameObject.GetComponent<TooltipPanelBinder>();
        if (b != null)
        {
            txtTitle = b.txtTitle;
            txtContent = b.txtContent;
            panelRect = b.panelRect;
        }

        rootCanvas = gameObject.GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            rootCanvas = rootCanvas.rootCanvas;
        }
    }

    public override void OnShow()
    {
        isShowing = true;
    }

    public override void OnHide()
    {
        isShowing = false;
    }

    public override void OnUpdate()
    {
        if (isShowing)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// 显示 Tooltip
    /// </summary>
    public void Show(string title, string content)
    {
        if (txtTitle != null)
        {
            txtTitle.text = title;
            txtTitle.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        if (txtContent != null)
        {
            txtContent.text = content;
        }

        gameObject.SetActive(true);
        isShowing = true;

        // 强制刷新布局
        if (panelRect != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        UpdatePosition();
    }

    /// <summary>
    /// 隐藏 Tooltip
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        isShowing = false;
    }

    private void UpdatePosition()
    {
        if (panelRect == null) return;

        Vector2 mousePos = Input.mousePosition;
        Vector2 targetPos = mousePos + offset;

        // 边界检测，防止超出屏幕
        // Pivot在左上角(0,1)，position是面板左上角的位置
        // 面板向下、向右展开
        float panelWidth = panelRect.rect.width;
        float panelHeight = panelRect.rect.height;

        // 右边界检测：如果右边超出，则显示在鼠标左侧
        if (targetPos.x + panelWidth > Screen.width)
        {
            targetPos.x = mousePos.x - panelWidth - offset.x;
        }

        // 底部边界检测：如果底部超出，则显示在鼠标上方
        if (targetPos.y - panelHeight < 0)
        {
            targetPos.y = mousePos.y + panelHeight - offset.y;
        }

        // 确保不超出左边界和顶部边界
        targetPos.x = Mathf.Max(0, targetPos.x);
        targetPos.y = Mathf.Min(Screen.height, targetPos.y);

        panelRect.position = targetPos;
    }
}
