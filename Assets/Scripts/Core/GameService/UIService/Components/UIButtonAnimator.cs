// LevityFramework - 通用 Unity 游戏框架
// UI 组件 - UIButtonAnimator 按钮动画组件

using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 交互动画组件
/// 提供悬停和点击的动画效果
/// 可挂载到 Button、Toggle 或任何可交互 UI 元素上
/// </summary>
public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("悬停动画")]
    [SerializeField] private bool enableHover = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.05f;
    [SerializeField] private Ease hoverEase = Ease.OutQuad;

    [Header("点击动画")]
    [SerializeField] private bool enableClick = true;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float clickDuration = 0.1f;
    [SerializeField] private Ease clickEase = Ease.OutQuad;

    [Header("高级设置")]
    [Tooltip("是否在禁用状态下也播放动画")]
    [SerializeField] private bool animateWhenDisabled = false;

    private Vector3 originalScale;
    private Tweener currentTween;
    private Selectable selectable;
    private bool isPointerInside;
    private bool isPointerDown;
    private bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        originalScale = transform.localScale;
        selectable = GetComponent<Selectable>();
        isInitialized = true;
    }

    private bool CanAnimate()
    {
        if (!isInitialized) Initialize();
        if (animateWhenDisabled) return true;
        return selectable == null || selectable.interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHover || !CanAnimate()) return;

        isPointerInside = true;
        PlayScaleAnimation(hoverScale, hoverDuration, hoverEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableHover) return;

        isPointerInside = false;
        if (!isPointerDown)
        {
            PlayScaleAnimation(1f, hoverDuration, hoverEase);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableClick || !CanAnimate()) return;

        isPointerDown = true;
        PlayScaleAnimation(clickScale, clickDuration, clickEase);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!enableClick) return;

        isPointerDown = false;

        // 如果鼠标还在按钮内，恢复到悬停状态；否则恢复原始大小
        float targetScale = (enableHover && isPointerInside && CanAnimate()) ? hoverScale : 1f;
        PlayScaleAnimation(targetScale, clickDuration, Ease.OutBack);
    }

    private void PlayScaleAnimation(float targetScale, float duration, Ease ease)
    {
        if (!isInitialized) Initialize();

        // 终止当前动画
        currentTween?.Kill();

        // 播放新动画
        currentTween = transform
            .DOScale(originalScale * targetScale, duration)
            .SetEase(ease)
            .SetUpdate(true); // 不受 TimeScale 影响
    }

    /// <summary>
    /// 重置到原始状态
    /// </summary>
    public void ResetState()
    {
        if (!isInitialized) Initialize();

        currentTween?.Kill();
        transform.localScale = originalScale;
        isPointerInside = false;
        isPointerDown = false;
    }

    /// <summary>
    /// 手动触发点击动画（用于代码触发点击时）
    /// </summary>
    public void PlayClickAnimation()
    {
        if (!enableClick || !CanAnimate()) return;

        currentTween?.Kill();

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(originalScale * clickScale, clickDuration).SetEase(clickEase));
        seq.Append(transform.DOScale(originalScale, clickDuration).SetEase(Ease.OutBack));
        seq.SetUpdate(true);
    }

    private void OnDisable()
    {
        currentTween?.Kill();
        if (isInitialized)
        {
            transform.localScale = originalScale;
        }
        isPointerInside = false;
        isPointerDown = false;
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 确保缩放值合理
        hoverScale = Mathf.Clamp(hoverScale, 0.5f, 2f);
        clickScale = Mathf.Clamp(clickScale, 0.5f, 1.5f);
        hoverDuration = Mathf.Max(0.01f, hoverDuration);
        clickDuration = Mathf.Max(0.01f, clickDuration);
    }
#endif
}
