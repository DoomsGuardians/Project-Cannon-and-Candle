using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

/// <summary>
/// 阶段横幅组件
/// 类似炉石传说的回合开始横幅，用于展示阶段切换
/// 只显示3个玩家可感知的阶段：玩家阶段、对手阶段、战斗阶段
/// 动画播放期间会锁定玩家输入
/// </summary>
public class PhaseBanner : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform bannerRect;
    [SerializeField] private TMP_Text txtPhaseTitle;
    [SerializeField] private TMP_Text txtPhaseSubtitle;
    [SerializeField] private Image imgBackground;

    [Header("动画设置")]
    [SerializeField] private float slideInDuration = 0.3f;
    [SerializeField] private float displayDuration = 1.2f;
    [SerializeField] private float slideOutDuration = 0.3f;
    [SerializeField] private float slideDistance = 200f;

    private Sequence currentSequence;
    private UITextConfig textConfig;

    // 记录上一次显示的阶段，避免重复显示
    private string lastShownPhase = "";

    // 动画是否正在播放
    public bool IsPlaying { get; private set; }

    // 动画完成回调
    private Action onCompleteCallback;

    // 当前显示的阶段（用于发送完成事件）
    private TurnPhase currentPhase;

    private void Awake()
    {
        // 初始隐藏
        if (canvasGroup != null)
            canvasGroup.alpha = 0;

        // 加载文本配置
        var resService = GameRoot.Instance?.resService;
        if (resService != null)
            textConfig = resService.LoadResource<UITextConfig>(ConfigPaths.UI_TEXT);
    }

    /// <summary>
    /// 根据TurnPhase显示对应的玩家可感知阶段
    /// </summary>
    /// <param name="phase">当前阶段</param>
    /// <param name="turnNumber">回合数</param>
    /// <param name="onComplete">动画完成后的回调</param>
    /// <returns>是否显示了横幅（如果返回false，说明该阶段不需要显示横幅）</returns>
    public bool ShowPhase(TurnPhase phase, int turnNumber = 0, Action onComplete = null)
    {
        string phaseKey = GetPlayerVisiblePhase(phase);

        // 如果不是玩家可感知的阶段，或者与上次相同，不显示
        if (string.IsNullOrEmpty(phaseKey) || phaseKey == lastShownPhase)
        {
            // 直接调用回调并发送完成事件
            onComplete?.Invoke();
            GameRoot.Instance?.eventService?.SendMessage(
                (EventID)WarBrokerEventID.OnPhaseBannerComplete, phase, null);
            return false;
        }

        lastShownPhase = phaseKey;
        currentPhase = phase;
        onCompleteCallback = onComplete;

        // 设置文本
        SetPhaseText(phaseKey, turnNumber);

        // 锁定输入
        LockInput();

        // 播放动画
        PlayBannerAnimation();

        return true;
    }

    /// <summary>
    /// 将内部阶段映射到玩家可感知的阶段
    /// </summary>
    private string GetPlayerVisiblePhase(TurnPhase phase)
    {
        return phase switch
        {
            TurnPhase.MarketPhase => "Player",      // 玩家阶段
            TurnPhase.IntentPhase => "Opponent",    // 对手阶段
            TurnPhase.BattlePhase => "Battle",      // 战斗阶段
            _ => null  // 其他阶段不显示
        };
    }

    private void SetPhaseText(string phaseKey, int turnNumber)
    {
        string title = "";
        string subtitle = "";

        switch (phaseKey)
        {
            case "Player":
                title = textConfig?.PhasePlayer ?? "玩家阶段";
                subtitle = textConfig?.PhasePlayerSubtitle ?? "进行交易与部署";
                break;
            case "Opponent":
                title = textConfig?.PhaseOpponent ?? "对手阶段";
                subtitle = textConfig?.PhaseOpponentSubtitle ?? "敌方正在行动...";
                break;
            case "Battle":
                title = textConfig?.PhaseBattle ?? "战斗阶段";
                subtitle = textConfig?.PhaseBattleSubtitle ?? "前线战斗即将打响";
                break;
        }

        if (txtPhaseTitle != null)
            txtPhaseTitle.text = title;
        if (txtPhaseSubtitle != null)
            txtPhaseSubtitle.text = subtitle;
    }

    /// <summary>
    /// 重置阶段记录（新回合开始时调用）
    /// </summary>
    public void ResetPhaseTracking()
    {
        lastShownPhase = "";
    }

    private void LockInput()
    {
        IsPlaying = true;
        // 获取输入锁，阻止玩家操作
        InputRouter.Acquire(InputChannel.Gameplay, this);
    }

    private void UnlockInput()
    {
        IsPlaying = false;
        // 释放输入锁
        InputRouter.Release(InputChannel.Gameplay, this);
    }

    private void PlayBannerAnimation()
    {
        if (bannerRect == null || canvasGroup == null)
        {
            OnAnimationComplete();
            return;
        }

        // 停止当前动画
        currentSequence?.Kill();

        // 初始位置（从左侧滑入）
        Vector2 startPos = new Vector2(-slideDistance, 0);
        Vector2 centerPos = Vector2.zero;
        Vector2 endPos = new Vector2(slideDistance, 0);

        bannerRect.anchoredPosition = startPos;
        canvasGroup.alpha = 0;

        currentSequence = DOTween.Sequence();

        // 滑入 + 淡入
        currentSequence.Append(bannerRect.DOAnchorPos(centerPos, slideInDuration).SetEase(Ease.OutCubic));
        currentSequence.Join(canvasGroup.DOFade(1f, slideInDuration * 0.5f));

        // 停留
        currentSequence.AppendInterval(displayDuration);

        // 滑出 + 淡出
        currentSequence.Append(bannerRect.DOAnchorPos(endPos, slideOutDuration).SetEase(Ease.InCubic));
        currentSequence.Join(canvasGroup.DOFade(0f, slideOutDuration));

        // 动画完成回调
        currentSequence.OnComplete(OnAnimationComplete);

        currentSequence.Play();
    }

    private void OnAnimationComplete()
    {
        UnlockInput();

        // 发送横幅完成事件
        GameRoot.Instance?.eventService?.SendMessage(
            (EventID)WarBrokerEventID.OnPhaseBannerComplete, currentPhase, null);

        // 触发完成回调
        onCompleteCallback?.Invoke();
        onCompleteCallback = null;
    }

    /// <summary>
    /// 立即隐藏横幅并完成
    /// </summary>
    public void Hide()
    {
        currentSequence?.Kill();
        if (canvasGroup != null)
            canvasGroup.alpha = 0;

        OnAnimationComplete();
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
        // 确保释放输入锁
        if (IsPlaying)
        {
            InputRouter.Release(InputChannel.Gameplay, this);
        }
    }
}
