// LevityFramework - 通用 Unity 游戏框架
// UI 组件 - UITransition 通用UI过渡动画组件

using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 过渡动画预设类型
/// </summary>
public enum TransitionPreset
{
    [InspectorName("无动画")]
    None,

    [InspectorName("淡入淡出")]
    Fade,

    [InspectorName("缩放")]
    Scale,

    [InspectorName("缩放 + 淡入淡出")]
    ScaleFade,

    [InspectorName("弹出 (带回弹)")]
    PopBounce,

    [InspectorName("弹出 + 淡入淡出")]
    PopBounceFade,

    [InspectorName("从左滑入")]
    SlideFromLeft,

    [InspectorName("从右滑入")]
    SlideFromRight,

    [InspectorName("从上滑入")]
    SlideFromTop,

    [InspectorName("从下滑入")]
    SlideFromBottom,

    [InspectorName("从左滑入 + 淡入淡出")]
    SlideFromLeftFade,

    [InspectorName("从右滑入 + 淡入淡出")]
    SlideFromRightFade,

    [InspectorName("从上滑入 + 淡入淡出")]
    SlideFromTopFade,

    [InspectorName("从下滑入 + 淡入淡出")]
    SlideFromBottomFade,

    [InspectorName("翻转 (水平)")]
    FlipHorizontal,

    [InspectorName("翻转 (垂直)")]
    FlipVertical,

    [InspectorName("旋转缩放")]
    RotateScale,

    [InspectorName("弹性缩放")]
    ElasticScale,

    [InspectorName("下落 (带回弹)")]
    DropBounce,

    [InspectorName("自定义")]
    Custom
}

/// <summary>
/// 通用 UI 过渡动画组件
/// 提供多种预设的 In/Out 动画效果
/// 注意：滑动动画不支持拉伸锚点的 UI，如需滑动请使用固定锚点或套一层父物体
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UITransition : MonoBehaviour
{
    [Header("动画预设")]
    [SerializeField] private TransitionPreset inPreset = TransitionPreset.Fade;
    [SerializeField] private TransitionPreset outPreset = TransitionPreset.Fade;

    [Header("动画参数")]
    [SerializeField] private float inDuration = 0.3f;
    [SerializeField] private float outDuration = 0.2f;
    [SerializeField] private Ease inEase = Ease.OutQuad;
    [SerializeField] private Ease outEase = Ease.InQuad;

    [Header("滑动设置")]
    [Tooltip("滑动动画的偏移距离")]
    [SerializeField] private float slideOffset = 100f;

    [Header("缩放设置")]
    [Tooltip("缩放动画的起始/结束缩放值")]
    [SerializeField] private float scaleFrom = 0f;
    [SerializeField] private float scaleTo = 1f;

    [Header("旋转设置")]
    [Tooltip("旋转动画的角度")]
    [SerializeField] private float rotationAngle = 180f;

    [Header("自定义动画 (当预设为 Custom 时生效)")]
    [SerializeField] private bool customUseFade = true;
    [SerializeField] private bool customUseScale = false;
    [SerializeField] private bool customUseSlide = false;
    [SerializeField] private bool customUseRotation = false;
    [SerializeField] private Vector2 customSlideDirection = Vector2.down;

    [Header("自动播放")]
    [Tooltip("当 GameObject 被激活时自动播放 In 动画")]
    [SerializeField] private bool playInOnEnable = true;
    [Tooltip("跳过首次 OnEnable (避免场景加载时播放)")]
    [SerializeField] private bool skipFirstEnable = true;

    [Header("高级设置")]
    [Tooltip("In 动画完成后自动启用交互")]
    [SerializeField] private bool enableInteractionOnIn = true;
    [Tooltip("Out 动画开始时自动禁用交互")]
    [SerializeField] private bool disableInteractionOnOut = true;
    [Tooltip("Out 动画完成后自动隐藏 GameObject")]
    [SerializeField] private bool hideOnOutComplete = true;
    [Tooltip("动画是否不受 TimeScale 影响")]
    [SerializeField] private bool ignoreTimeScale = true;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private Vector2 originalOffsetMin;
    private Vector2 originalOffsetMax;
    private Vector3 originalScale;
    private Vector3 originalRotation;
    private Sequence currentSequence;
    private bool isInitialized;
    private bool isFirstEnable = true;
    private bool isStretchMode;

    public bool IsAnimating => currentSequence != null && currentSequence.IsActive() && currentSequence.IsPlaying();

    public event Action OnInComplete;
    public event Action OnOutComplete;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!playInOnEnable) return;

        if (skipFirstEnable && isFirstEnable)
        {
            isFirstEnable = false;
            return;
        }

        PlayIn();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = GetComponent<RectTransform>();
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalOffsetMin = rectTransform.offsetMin;
        originalOffsetMax = rectTransform.offsetMax;
        originalScale = transform.localScale;
        originalRotation = transform.localEulerAngles;

        // 检测是否为拉伸模式
        isStretchMode = !Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x) ||
                        !Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);

        isInitialized = true;
    }

    /// <summary>
    /// 播放 In 动画 (显示)
    /// </summary>
    public void PlayIn(Action onComplete = null)
    {
        Initialize();
        KillCurrentAnimation();

        gameObject.SetActive(true);

        if (inPreset == TransitionPreset.None)
        {
            SetVisibleState();
            onComplete?.Invoke();
            OnInComplete?.Invoke();
            return;
        }

        // 设置初始状态
        SetHiddenState(inPreset);

        // 创建动画序列
        currentSequence = DOTween.Sequence();
        BuildInAnimation(currentSequence, inPreset);

        currentSequence
            .SetEase(inEase)
            .SetUpdate(ignoreTimeScale)
            .OnComplete(() =>
            {
                if (enableInteractionOnIn)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                onComplete?.Invoke();
                OnInComplete?.Invoke();
            });
    }

    /// <summary>
    /// 播放 Out 动画 (隐藏)
    /// </summary>
    public void PlayOut(Action onComplete = null)
    {
        Initialize();
        KillCurrentAnimation();

        if (outPreset == TransitionPreset.None)
        {
            SetHiddenStateImmediate();
            onComplete?.Invoke();
            OnOutComplete?.Invoke();
            return;
        }

        if (disableInteractionOnOut)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // 创建动画序列
        currentSequence = DOTween.Sequence();
        BuildOutAnimation(currentSequence, outPreset);

        currentSequence
            .SetEase(outEase)
            .SetUpdate(ignoreTimeScale)
            .OnComplete(() =>
            {
                if (hideOnOutComplete)
                {
                    gameObject.SetActive(false);
                }
                ResetToOriginal();
                onComplete?.Invoke();
                OnOutComplete?.Invoke();
            });
    }

    /// <summary>
    /// 立即显示 (无动画)
    /// </summary>
    public void ShowImmediate()
    {
        Initialize();
        KillCurrentAnimation();
        gameObject.SetActive(true);
        SetVisibleState();
    }

    /// <summary>
    /// 立即隐藏 (无动画)
    /// </summary>
    public void HideImmediate()
    {
        Initialize();
        KillCurrentAnimation();
        SetHiddenStateImmediate();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 停止当前动画
    /// </summary>
    public void StopAnimation()
    {
        KillCurrentAnimation();
    }

    #region 滑动动画辅助方法

    /// <summary>
    /// 设置滑动偏移（立即）
    /// </summary>
    private void SetSlideOffset(Vector2 direction)
    {
        Vector2 offset = direction * slideOffset;
        if (isStretchMode)
        {
            rectTransform.offsetMin = originalOffsetMin + offset;
            rectTransform.offsetMax = originalOffsetMax + offset;
        }
        else
        {
            rectTransform.anchoredPosition = originalAnchoredPosition + offset;
        }
    }

    /// <summary>
    /// 添加滑动到原始位置的动画
    /// </summary>
    private void AppendSlideToOriginal(Sequence seq, float duration)
    {
        if (isStretchMode)
        {
            seq.Append(rectTransform.DOOffsetMin(originalOffsetMin, duration));
            seq.Join(rectTransform.DOOffsetMax(originalOffsetMax, duration));
        }
        else
        {
            seq.Append(rectTransform.DOAnchorPos(originalAnchoredPosition, duration));
        }
    }

    /// <summary>
    /// 添加滑动到原始位置的动画（带缓动）
    /// </summary>
    private void AppendSlideToOriginal(Sequence seq, float duration, Ease ease)
    {
        if (isStretchMode)
        {
            seq.Append(rectTransform.DOOffsetMin(originalOffsetMin, duration).SetEase(ease));
            seq.Join(rectTransform.DOOffsetMax(originalOffsetMax, duration).SetEase(ease));
        }
        else
        {
            seq.Append(rectTransform.DOAnchorPos(originalAnchoredPosition, duration).SetEase(ease));
        }
    }

    /// <summary>
    /// 添加滑动到偏移位置的动画
    /// </summary>
    private void AppendSlideToOffset(Sequence seq, Vector2 direction, float duration)
    {
        Vector2 offset = direction * slideOffset;
        if (isStretchMode)
        {
            seq.Append(rectTransform.DOOffsetMin(originalOffsetMin + offset, duration));
            seq.Join(rectTransform.DOOffsetMax(originalOffsetMax + offset, duration));
        }
        else
        {
            seq.Append(rectTransform.DOAnchorPos(originalAnchoredPosition + offset, duration));
        }
    }

    /// <summary>
    /// 添加滑动到偏移位置的动画（带缓动）
    /// </summary>
    private void AppendSlideToOffset(Sequence seq, Vector2 direction, float duration, Ease ease)
    {
        Vector2 offset = direction * slideOffset;
        if (isStretchMode)
        {
            seq.Append(rectTransform.DOOffsetMin(originalOffsetMin + offset, duration).SetEase(ease));
            seq.Join(rectTransform.DOOffsetMax(originalOffsetMax + offset, duration).SetEase(ease));
        }
        else
        {
            seq.Append(rectTransform.DOAnchorPos(originalAnchoredPosition + offset, duration).SetEase(ease));
        }
    }

    /// <summary>
    /// 重置滑动位置
    /// </summary>
    private void ResetSlidePosition()
    {
        if (isStretchMode)
        {
            rectTransform.offsetMin = originalOffsetMin;
            rectTransform.offsetMax = originalOffsetMax;
        }
        else
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }

    #endregion

    private void BuildInAnimation(Sequence seq, TransitionPreset preset)
    {
        switch (preset)
        {
            case TransitionPreset.Fade:
                seq.Append(canvasGroup.DOFade(1f, inDuration));
                break;

            case TransitionPreset.Scale:
                seq.Append(transform.DOScale(originalScale * scaleTo, inDuration));
                break;

            case TransitionPreset.ScaleFade:
                seq.Append(transform.DOScale(originalScale * scaleTo, inDuration));
                seq.Join(canvasGroup.DOFade(1f, inDuration));
                break;

            case TransitionPreset.PopBounce:
                seq.Append(transform.DOScale(originalScale * scaleTo, inDuration).SetEase(Ease.OutBack));
                break;

            case TransitionPreset.PopBounceFade:
                seq.Append(transform.DOScale(originalScale * scaleTo, inDuration).SetEase(Ease.OutBack));
                seq.Join(canvasGroup.DOFade(1f, inDuration * 0.5f));
                break;

            case TransitionPreset.SlideFromLeft:
            case TransitionPreset.SlideFromRight:
            case TransitionPreset.SlideFromTop:
            case TransitionPreset.SlideFromBottom:
                AppendSlideToOriginal(seq, inDuration);
                break;

            case TransitionPreset.SlideFromLeftFade:
            case TransitionPreset.SlideFromRightFade:
            case TransitionPreset.SlideFromTopFade:
            case TransitionPreset.SlideFromBottomFade:
                AppendSlideToOriginal(seq, inDuration);
                seq.Join(canvasGroup.DOFade(1f, inDuration));
                break;

            case TransitionPreset.FlipHorizontal:
            case TransitionPreset.FlipVertical:
                seq.Append(transform.DORotate(originalRotation, inDuration).SetEase(Ease.OutBack));
                seq.Join(canvasGroup.DOFade(1f, inDuration * 0.5f));
                break;

            case TransitionPreset.RotateScale:
                seq.Append(transform.DOScale(originalScale * scaleTo, inDuration));
                seq.Join(transform.DORotate(originalRotation, inDuration));
                seq.Join(canvasGroup.DOFade(1f, inDuration * 0.5f));
                break;

            case TransitionPreset.ElasticScale:
                seq.Append(transform.DOScale(originalScale * scaleTo, inDuration).SetEase(Ease.OutElastic));
                seq.Join(canvasGroup.DOFade(1f, inDuration * 0.3f));
                break;

            case TransitionPreset.DropBounce:
                AppendSlideToOriginal(seq, inDuration, Ease.OutBounce);
                seq.Join(canvasGroup.DOFade(1f, inDuration * 0.3f));
                break;

            case TransitionPreset.Custom:
                BuildCustomInAnimation(seq);
                break;
        }
    }

    private void BuildOutAnimation(Sequence seq, TransitionPreset preset)
    {
        switch (preset)
        {
            case TransitionPreset.Fade:
                seq.Append(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.Scale:
                seq.Append(transform.DOScale(originalScale * scaleFrom, outDuration));
                break;

            case TransitionPreset.ScaleFade:
                seq.Append(transform.DOScale(originalScale * scaleFrom, outDuration));
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.PopBounce:
                seq.Append(transform.DOScale(originalScale * scaleFrom, outDuration).SetEase(Ease.InBack));
                break;

            case TransitionPreset.PopBounceFade:
                seq.Append(transform.DOScale(originalScale * scaleFrom, outDuration).SetEase(Ease.InBack));
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.SlideFromLeft:
                AppendSlideToOffset(seq, Vector2.left, outDuration);
                break;

            case TransitionPreset.SlideFromRight:
                AppendSlideToOffset(seq, Vector2.right, outDuration);
                break;

            case TransitionPreset.SlideFromTop:
                AppendSlideToOffset(seq, Vector2.up, outDuration);
                break;

            case TransitionPreset.SlideFromBottom:
                AppendSlideToOffset(seq, Vector2.down, outDuration);
                break;

            case TransitionPreset.SlideFromLeftFade:
                AppendSlideToOffset(seq, Vector2.left, outDuration);
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.SlideFromRightFade:
                AppendSlideToOffset(seq, Vector2.right, outDuration);
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.SlideFromTopFade:
                AppendSlideToOffset(seq, Vector2.up, outDuration);
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.SlideFromBottomFade:
                AppendSlideToOffset(seq, Vector2.down, outDuration);
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.FlipHorizontal:
                seq.Append(transform.DORotate(new Vector3(0f, 90f, 0f), outDuration).SetEase(Ease.InBack));
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.FlipVertical:
                seq.Append(transform.DORotate(new Vector3(90f, 0f, 0f), outDuration).SetEase(Ease.InBack));
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.RotateScale:
                seq.Append(transform.DOScale(originalScale * scaleFrom, outDuration));
                seq.Join(transform.DORotate(originalRotation + new Vector3(0f, 0f, rotationAngle), outDuration));
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.ElasticScale:
                seq.Append(transform.DOScale(originalScale * scaleFrom, outDuration).SetEase(Ease.InBack));
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.DropBounce:
                AppendSlideToOffset(seq, Vector2.down, outDuration, Ease.InQuad);
                seq.Join(canvasGroup.DOFade(0f, outDuration));
                break;

            case TransitionPreset.Custom:
                BuildCustomOutAnimation(seq);
                break;
        }
    }

    private void BuildCustomInAnimation(Sequence seq)
    {
        bool hasAnimation = false;

        if (customUseFade)
        {
            if (!hasAnimation)
                seq.Append(canvasGroup.DOFade(1f, inDuration));
            else
                seq.Join(canvasGroup.DOFade(1f, inDuration));
            hasAnimation = true;
        }

        if (customUseScale)
        {
            if (!hasAnimation)
                seq.Append(transform.DOScale(originalScale * scaleTo, inDuration));
            else
                seq.Join(transform.DOScale(originalScale * scaleTo, inDuration));
            hasAnimation = true;
        }

        if (customUseSlide)
        {
            if (!hasAnimation)
            {
                AppendSlideToOriginal(seq, inDuration);
                hasAnimation = true;
            }
            else
            {
                // Join 版本需要单独处理
                if (isStretchMode)
                {
                    seq.Join(rectTransform.DOOffsetMin(originalOffsetMin, inDuration));
                    seq.Join(rectTransform.DOOffsetMax(originalOffsetMax, inDuration));
                }
                else
                {
                    seq.Join(rectTransform.DOAnchorPos(originalAnchoredPosition, inDuration));
                }
            }
        }

        if (customUseRotation)
        {
            if (!hasAnimation)
                seq.Append(transform.DORotate(originalRotation, inDuration));
            else
                seq.Join(transform.DORotate(originalRotation, inDuration));
            hasAnimation = true;
        }

        if (!hasAnimation)
        {
            seq.Append(canvasGroup.DOFade(1f, inDuration));
        }
    }

    private void BuildCustomOutAnimation(Sequence seq)
    {
        bool hasAnimation = false;

        if (customUseFade)
        {
            if (!hasAnimation)
                seq.Append(canvasGroup.DOFade(0f, outDuration));
            else
                seq.Join(canvasGroup.DOFade(0f, outDuration));
            hasAnimation = true;
        }

        if (customUseScale)
        {
            if (!hasAnimation)
                seq.Append(transform.DOScale(originalScale * scaleFrom, outDuration));
            else
                seq.Join(transform.DOScale(originalScale * scaleFrom, outDuration));
            hasAnimation = true;
        }

        if (customUseSlide)
        {
            Vector2 offset = customSlideDirection.normalized * slideOffset;
            if (!hasAnimation)
            {
                AppendSlideToOffset(seq, customSlideDirection.normalized, outDuration);
                hasAnimation = true;
            }
            else
            {
                // Join 版本需要单独处理
                if (isStretchMode)
                {
                    seq.Join(rectTransform.DOOffsetMin(originalOffsetMin + offset, outDuration));
                    seq.Join(rectTransform.DOOffsetMax(originalOffsetMax + offset, outDuration));
                }
                else
                {
                    seq.Join(rectTransform.DOAnchorPos(originalAnchoredPosition + offset, outDuration));
                }
            }
        }

        if (customUseRotation)
        {
            Vector3 targetRotation = originalRotation + new Vector3(0f, 0f, rotationAngle);
            if (!hasAnimation)
                seq.Append(transform.DORotate(targetRotation, outDuration));
            else
                seq.Join(transform.DORotate(targetRotation, outDuration));
            hasAnimation = true;
        }

        if (!hasAnimation)
        {
            seq.Append(canvasGroup.DOFade(0f, outDuration));
        }
    }

    private void SetHiddenState(TransitionPreset preset)
    {
        switch (preset)
        {
            case TransitionPreset.Fade:
                canvasGroup.alpha = 0f;
                break;

            case TransitionPreset.Scale:
            case TransitionPreset.PopBounce:
                transform.localScale = originalScale * scaleFrom;
                break;

            case TransitionPreset.ScaleFade:
            case TransitionPreset.PopBounceFade:
            case TransitionPreset.ElasticScale:
                canvasGroup.alpha = 0f;
                transform.localScale = originalScale * scaleFrom;
                break;

            case TransitionPreset.SlideFromLeft:
                SetSlideOffset(Vector2.left);
                break;

            case TransitionPreset.SlideFromRight:
                SetSlideOffset(Vector2.right);
                break;

            case TransitionPreset.SlideFromTop:
                SetSlideOffset(Vector2.up);
                break;

            case TransitionPreset.SlideFromBottom:
                SetSlideOffset(Vector2.down);
                break;

            case TransitionPreset.DropBounce:
                SetSlideOffset(Vector2.up);
                canvasGroup.alpha = 0f;
                break;

            case TransitionPreset.SlideFromLeftFade:
                SetSlideOffset(Vector2.left);
                canvasGroup.alpha = 0f;
                break;

            case TransitionPreset.SlideFromRightFade:
                SetSlideOffset(Vector2.right);
                canvasGroup.alpha = 0f;
                break;

            case TransitionPreset.SlideFromTopFade:
                SetSlideOffset(Vector2.up);
                canvasGroup.alpha = 0f;
                break;

            case TransitionPreset.SlideFromBottomFade:
                SetSlideOffset(Vector2.down);
                canvasGroup.alpha = 0f;
                break;

            case TransitionPreset.FlipHorizontal:
                transform.localEulerAngles = new Vector3(0f, 90f, 0f);
                canvasGroup.alpha = 0f;
                break;

            case TransitionPreset.FlipVertical:
                transform.localEulerAngles = new Vector3(90f, 0f, 0f);
                canvasGroup.alpha = 0f;
                break;

            case TransitionPreset.RotateScale:
                transform.localScale = originalScale * scaleFrom;
                transform.localEulerAngles = originalRotation + new Vector3(0f, 0f, -rotationAngle);
                canvasGroup.alpha = 0f;
                break;

            case TransitionPreset.Custom:
                SetCustomHiddenState();
                break;
        }

        if (enableInteractionOnIn)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void SetCustomHiddenState()
    {
        if (customUseFade)
        {
            canvasGroup.alpha = 0f;
        }

        if (customUseScale)
        {
            transform.localScale = originalScale * scaleFrom;
        }

        if (customUseSlide)
        {
            SetSlideOffset(-customSlideDirection.normalized);
        }

        if (customUseRotation)
        {
            transform.localEulerAngles = originalRotation + new Vector3(0f, 0f, -rotationAngle);
        }
    }

    private void SetVisibleState()
    {
        canvasGroup.alpha = 1f;
        transform.localScale = originalScale;
        transform.localEulerAngles = originalRotation;
        ResetSlidePosition();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void SetHiddenStateImmediate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ResetToOriginal()
    {
        transform.localScale = originalScale;
        transform.localEulerAngles = originalRotation;
        ResetSlidePosition();
    }

    private void KillCurrentAnimation()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
            currentSequence = null;
        }
    }

    private void OnDisable()
    {
        KillCurrentAnimation();
    }

    private void OnDestroy()
    {
        KillCurrentAnimation();
    }

#if UNITY_EDITOR
    [ContextMenu("测试 In 动画")]
    private void TestIn()
    {
        if (!Application.isPlaying) return;
        PlayIn();
    }

    [ContextMenu("测试 Out 动画")]
    private void TestOut()
    {
        if (!Application.isPlaying) return;
        PlayOut();
    }

    private void OnValidate()
    {
        inDuration = Mathf.Max(0.01f, inDuration);
        outDuration = Mathf.Max(0.01f, outDuration);
        slideOffset = Mathf.Max(0f, slideOffset);
    }
#endif
}
