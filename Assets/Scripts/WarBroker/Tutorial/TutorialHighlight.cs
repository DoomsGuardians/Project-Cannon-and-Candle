using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 教程高亮效果组件
/// 挂在高亮 Prefab 上，提供简单的闪烁效果
/// </summary>
public class TutorialHighlight : MonoBehaviour
{
    [Header("边框图片")]
    [SerializeField] private Image borderImage;

    [Header("动画设置")]
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 1f;

    [Header("可选：缩放动画")]
    [SerializeField] private bool enableScalePulse = false;
    [SerializeField] private float scaleAmount = 1.05f;

    private Tween fadeTween;
    private Tween scaleTween;

    private void Start()
    {
        StartAnimations();
    }

    private void OnEnable()
    {
        StartAnimations();
    }

    private void StartAnimations()
    {
        // 脉冲透明度动画
        if (borderImage != null)
        {
            // 确保初始状态
            var color = borderImage.color;
            color.a = maxAlpha;
            borderImage.color = color;

            fadeTween?.Kill();
            fadeTween = borderImage.DOFade(minAlpha, pulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        // 可选：缩放脉冲动画
        if (enableScalePulse)
        {
            transform.localScale = Vector3.one;

            scaleTween?.Kill();
            scaleTween = transform.DOScale(scaleAmount, pulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    private void OnDisable()
    {
        StopAnimations();
    }

    private void OnDestroy()
    {
        StopAnimations();
    }

    private void StopAnimations()
    {
        fadeTween?.Kill();
        fadeTween = null;

        scaleTween?.Kill();
        scaleTween = null;
    }
}
