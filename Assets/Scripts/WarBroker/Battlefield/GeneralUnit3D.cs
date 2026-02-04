using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using WarBroker.Battlefield;

/// <summary>
/// 3D 将军单位
/// 显示将军的锡兵、意图气泡等
/// </summary>
public class GeneralUnit3D : MonoBehaviour
{
    [Header("锡兵显示")]
    [SerializeField] private Transform[] soldierSlots;  // 20个锡兵Transform（Prefab中已有实例）
    [SerializeField] private GameObject soldierPrefab;  // 备用，动态创建时使用
    [SerializeField] private Material allySoldierMaterial;
    [SerializeField] private Material enemySoldierMaterial;

    [Header("意图气泡 (World Space UI)")]
    [SerializeField] private Canvas intentBubbleCanvas;
    [SerializeField] private CanvasGroup intentBubbleCanvasGroup;
    [SerializeField] private Image intentBubbleImage;
    [SerializeField] private TMP_Text intentText;

    [Header("选中效果")]
    [SerializeField] private GameObject selectionIndicator;

    [Header("颜色配置")]
    [SerializeField] private Color defaultIntentColor = Color.gray;
    [SerializeField] private Color reinforcedIntentColor = new Color(1f, 0.84f, 0f); // 金色
    [SerializeField] private Color overriddenIntentColor = Color.red;

    [Header("动画配置")]
    [SerializeField] private float soldierFallDuration = 0.3f;
    [SerializeField] private float soldierFallDelay = 0.05f;
    [SerializeField] private float soldierRiseDuration = 0.4f;
    [SerializeField] private float soldierRiseDelay = 0.08f;
    [SerializeField] private float moveDuration = 0.5f;

    [Header("增强动画设置")]
    [SerializeField] private float anticipationDistance = 0.2f;  // 预备动作后退距离
    [SerializeField] private float anticipationDuration = 0.1f;  // 预备动作时长
    [SerializeField] private float arcHeightMultiplier = 0.08f;  // 抛物线高度系数（降低）
    [SerializeField] private float landingSquashAmount = 0.15f;  // 落地压缩量
    [SerializeField] private float landingSquashDuration = 0.15f; // 落地压缩时长

    [Header("特效接口 - 由美术配置")]
    [SerializeField] private ParticleSystem gunfireEffect;      // 锡兵开枪火药
    [SerializeField] private ParticleSystem cannonEffect;       // 将军底座开炮
    [SerializeField] private ParticleSystem explosionEffect;    // 爆炸特效
    [SerializeField] private ParticleSystem landingDustEffect;  // 落地尘土
    [SerializeField] private ParticleSystem healEffect;         // 恢复特效
    [SerializeField] private ParticleSystem chargeEffect;       // 冲锋特效

    [Header("意图气泡渐隐")]
    [SerializeField] private float bubbleFadeStartDistance = 8f;  // 开始渐隐的距离
    [SerializeField] private float bubbleFadeEndDistance = 4f;    // 完全隐藏的距离

    public GeneralData Data { get; private set; }
    public bool IsAlly { get; private set; }

    private int currentSoldierCount = 0;
    private Sequence currentAnimation;

    public System.Action<GeneralUnit3D> OnClicked;

    private void Awake()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        // 初始化时隐藏所有锡兵
        if (soldierSlots != null)
        {
            foreach (var slot in soldierSlots)
            {
                if (slot != null)
                    slot.gameObject.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        // 让意图气泡始终朝向相机（Billboard效果）
        if (intentBubbleCanvas != null && intentBubbleCanvas.gameObject.activeSelf)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                intentBubbleCanvas.transform.rotation = cam.transform.rotation;

                // 基于相机距离渐隐
                if (intentBubbleCanvasGroup != null)
                {
                    float distance = Vector3.Distance(cam.transform.position, transform.position);
                    float alpha = Mathf.InverseLerp(bubbleFadeEndDistance, bubbleFadeStartDistance, distance);
                    intentBubbleCanvasGroup.alpha = alpha;
                }
            }
        }
    }

    /// <summary>初始化将军单位</summary>
    public void Initialize(GeneralData data, bool isAlly)
    {
        Data = data;
        IsAlly = isAlly;

        // 给气泡添加点击事件
        SetupBubbleClick();

        UpdateDisplay();
    }

    /// <summary>设置气泡点击事件</summary>
    private void SetupBubbleClick()
    {
        if (intentBubbleImage == null) return;

        // 添加EventTrigger组件来处理点击
        var trigger = intentBubbleImage.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = intentBubbleImage.gameObject.AddComponent<EventTrigger>();
        }

        // 清除旧的事件
        trigger.triggers.Clear();

        // 添加点击事件
        var entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnClicked?.Invoke(this); });
        trigger.triggers.Add(entry);
    }

    /// <summary>更新显示</summary>
    public void UpdateDisplay()
    {
        if (Data == null) return;

        UpdateSoldierCount(Data.Troops);
        UpdateIntentBubble();
    }

    /// <summary>更新锡兵数量显示（直接使用Prefab中的锡兵实例）</summary>
    private void UpdateSoldierCount(int troops)
    {
        if (soldierSlots == null) return;

        int targetCount = Mathf.Clamp(troops, 0, soldierSlots.Length);

        for (int i = 0; i < soldierSlots.Length; i++)
        {
            if (soldierSlots[i] == null) continue;

            var soldier = soldierSlots[i].gameObject;
            bool shouldBeActive = i < targetCount;

            if (shouldBeActive && !soldier.activeSelf)
            {
                // 显示锡兵，重置状态
                soldier.transform.localRotation = Quaternion.identity;
                soldier.transform.localScale = Vector3.one;
                soldier.SetActive(true);

                // 设置材质
                var renderer = soldier.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material = IsAlly ? allySoldierMaterial : enemySoldierMaterial;
                }
            }
            else if (!shouldBeActive && soldier.activeSelf)
            {
                soldier.SetActive(false);
            }
        }

        currentSoldierCount = targetCount;
    }

    /// <summary>更新意图气泡</summary>
    public void UpdateIntentBubble()
    {
        if (intentBubbleCanvas == null) return;

        // 只有己方将军显示意图
        if (!IsAlly)
        {
            intentBubbleCanvas.gameObject.SetActive(false);
            return;
        }

        if (Data == null || !Data.FinalIntent.HasValue && !Data.DefaultIntent.HasValue)
        {
            intentBubbleCanvas.gameObject.SetActive(false);
            return;
        }

        intentBubbleCanvas.gameObject.SetActive(true);

        // 显示意图类型
        var intent = Data.FinalIntent ?? Data.DefaultIntent;
        if (intentText != null && intent.HasValue)
        {
            intentText.text = intent.Value.ToString();
        }

        // 根据意图来源设置颜色
        if (intentBubbleImage != null)
        {
            intentBubbleImage.color = Data.IntentSource switch
            {
                IntentSource.Reinforced => reinforcedIntentColor,
                IntentSource.Overridden => overriddenIntentColor,
                _ => defaultIntentColor
            };
        }
    }

    /// <summary>设置选中状态</summary>
    public void SetSelected(bool selected)
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(selected);
    }

    /// <summary>播放锡兵倒下动画（随机选择锡兵倒下）</summary>
    /// <param name="casualties">伤亡数量</param>
    /// <param name="externalSequence">外部 Sequence（可选，传入则直接插入动画）</param>
    /// <param name="startTime">插入起始时间（仅当 externalSequence 不为 null 时有效）</param>
    /// <returns>动画时长</returns>
    public float PlaySoldierFallAnimation(int casualties, Sequence externalSequence = null, float startTime = 0f)
    {
        if (soldierSlots == null || casualties <= 0) return 0f;

        // 收集当前存活的锡兵索引
        var aliveIndices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < soldierSlots.Length; i++)
        {
            if (soldierSlots[i] != null && soldierSlots[i].gameObject.activeSelf)
                aliveIndices.Add(i);
        }

        // 随机打乱
        Shuffle(aliveIndices);

        int actualCasualties = Mathf.Min(casualties, aliveIndices.Count);
        if (actualCasualties <= 0) return 0f;

        // 使用外部 Sequence 或创建新的
        Sequence seq = externalSequence ?? DOTween.Sequence();
        float baseTime = externalSequence != null ? startTime : 0f;

        for (int i = 0; i < actualCasualties; i++)
        {
            int idx = aliveIndices[i];
            var soldier = soldierSlots[idx].gameObject;
            float delay = baseTime + i * soldierFallDelay;

            // 锡兵倒下动画：旋转90度 + 缩小
            seq.Insert(delay, soldier.transform
                .DORotate(new Vector3(90f, 0f, Random.Range(-30f, 30f)), soldierFallDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(delay, soldier.transform
                .DOScale(0.5f, soldierFallDuration)
                .SetEase(Ease.OutQuad));

            int capturedIdx = idx;
            seq.InsertCallback(delay + soldierFallDuration, () =>
            {
                if (soldierSlots[capturedIdx] != null)
                    soldierSlots[capturedIdx].gameObject.SetActive(false);
            });
        }

        float totalDuration = (actualCasualties - 1) * soldierFallDelay + soldierFallDuration;

        // 只有独立 Sequence 才设置 OnComplete
        if (externalSequence == null)
        {
            int capturedCasualties = actualCasualties;
            seq.OnComplete(() =>
            {
                currentSoldierCount = Mathf.Max(0, currentSoldierCount - capturedCasualties);
            });
        }
        else
        {
            // 外部 Sequence 时，用 callback 更新计数
            int capturedCasualties = actualCasualties;
            seq.InsertCallback(baseTime + totalDuration, () =>
            {
                currentSoldierCount = Mathf.Max(0, currentSoldierCount - capturedCasualties);
            });
        }

        return totalDuration;
    }

    /// <summary>随机打乱列表</summary>
    private void Shuffle<T>(System.Collections.Generic.List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>播放锡兵恢复动画（锡兵从地面弹出）</summary>
    /// <param name="reinforcements">增援数量</param>
    /// <param name="externalSequence">外部 Sequence（可选，传入则直接插入动画）</param>
    /// <param name="startTime">插入起始时间（仅当 externalSequence 不为 null 时有效）</param>
    /// <returns>动画时长</returns>
    public float PlaySoldierRiseAnimation(int reinforcements, Sequence externalSequence = null, float startTime = 0f)
    {
        if (soldierSlots == null || reinforcements <= 0) return 0f;

        // 找出当前未显示但需要显示的锡兵索引
        var inactiveIndices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < soldierSlots.Length; i++)
        {
            if (soldierSlots[i] != null && !soldierSlots[i].gameObject.activeSelf)
                inactiveIndices.Add(i);
        }

        int actualReinforcements = Mathf.Min(reinforcements, inactiveIndices.Count);
        if (actualReinforcements <= 0) return 0f;

        // 取前 actualReinforcements 个（按顺序填充）
        var toActivate = inactiveIndices.GetRange(0, actualReinforcements);

        // 使用外部 Sequence 或创建新的
        Sequence seq = externalSequence ?? DOTween.Sequence();
        float baseTime = externalSequence != null ? startTime : 0f;

        for (int i = 0; i < toActivate.Count; i++)
        {
            int idx = toActivate[i];
            var soldier = soldierSlots[idx].gameObject;
            float delay = baseTime + i * soldierRiseDelay;

            // 先设置初始状态：缩小 + 稍微下沉
            soldier.transform.localScale = Vector3.zero;
            soldier.transform.localRotation = Quaternion.identity;

            // 设置材质
            var renderer = soldier.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material = IsAlly ? allySoldierMaterial : enemySoldierMaterial;
            }

            // 立即显示
            int capturedIdx = idx;
            seq.InsertCallback(delay, () =>
            {
                soldierSlots[capturedIdx].gameObject.SetActive(true);
            });

            // 弹出动画：从0放大到1，带弹性效果
            seq.Insert(delay, soldier.transform
                .DOScale(Vector3.one, soldierRiseDuration)
                .SetEase(Ease.OutBack));
        }

        float totalDuration = (actualReinforcements - 1) * soldierRiseDelay + soldierRiseDuration;

        // 只有独立 Sequence 才设置 OnComplete
        if (externalSequence == null)
        {
            int capturedReinforcements = actualReinforcements;
            seq.OnComplete(() =>
            {
                currentSoldierCount = Mathf.Min(soldierSlots.Length, currentSoldierCount + capturedReinforcements);
            });
        }
        else
        {
            // 外部 Sequence 时，用 callback 更新计数
            int capturedReinforcements = actualReinforcements;
            seq.InsertCallback(baseTime + totalDuration, () =>
            {
                currentSoldierCount = Mathf.Min(soldierSlots.Length, currentSoldierCount + capturedReinforcements);
            });
        }

        return totalDuration;
    }

    /// <summary>播放单位移动动画</summary>
    public Tween PlayMoveAnimation(Vector3 targetLocalPosition)
    {
        currentAnimation?.Kill();
        return transform.DOLocalMove(targetLocalPosition, moveDuration).SetEase(Ease.InOutQuad);
    }

    /// <summary>播放单位溃败动画（移出棋盘）</summary>
    public Sequence PlayRoutAnimation(Vector3 exitDirection)
    {
        currentAnimation?.Kill();
        currentAnimation = DOTween.Sequence();

        Vector3 exitPos = transform.position + exitDirection * 10f;

        // 先震动，然后移出
        currentAnimation.Append(transform.DOShakePosition(0.3f, 0.2f, 10));
        currentAnimation.Append(transform.DOMove(exitPos, 0.8f).SetEase(Ease.InQuad));
        currentAnimation.Join(transform.DOScale(0f, 0.8f).SetEase(Ease.InQuad));

        return currentAnimation;
    }

    /// <summary>播放受击震动效果</summary>
    public Tween PlayHitShake()
    {
        return transform.DOShakePosition(0.2f, 0.1f, 15);
    }

    /// <summary>隐藏整个单位（溃败时调用）</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>显示单位（复活时调用）</summary>
    public void Show()
    {
        gameObject.SetActive(true);
        // 重置变换状态
        transform.localScale = Vector3.one;
    }

    /// <summary>重置锡兵显示（复活后重新填充）</summary>
    public void ResetSoldiers(int troops)
    {
        if (soldierSlots == null) return;

        // 重置所有锡兵状态
        for (int i = 0; i < soldierSlots.Length; i++)
        {
            if (soldierSlots[i] != null)
            {
                soldierSlots[i].localRotation = Quaternion.identity;
                soldierSlots[i].localScale = Vector3.one;
                soldierSlots[i].gameObject.SetActive(false);
            }
        }
        currentSoldierCount = 0;

        // 重新填充
        UpdateSoldierCount(troops);
    }

    #region 增强动画方法

    /// <summary>
    /// 播放增强移动动画 - 带抛物线轨迹和落地冲击
    /// 返回独立的 Sequence（注意：不要嵌套到其他 Sequence 中，会导致 OnComplete 问题）
    /// </summary>
    public Sequence PlayEnhancedMoveAnimation(
        Vector3 targetLocalPosition,
        bool withAnticipation = true,
        bool withImpact = true,
        float? heightMultiplier = null)
    {
        var seq = DOTween.Sequence();
        InsertEnhancedMoveAnimation(seq, 0f, targetLocalPosition, withAnticipation, withImpact, heightMultiplier);
        return seq;
    }

    /// <summary>
    /// 将增强移动动画插入到外部 Sequence 中（推荐使用，避免嵌套 Sequence 问题）
    /// </summary>
    /// <param name="sequence">外部 Sequence</param>
    /// <param name="startTime">插入的起始时间</param>
    /// <param name="targetLocalPosition">目标本地坐标</param>
    /// <param name="withAnticipation">是否包含预备动作</param>
    /// <param name="withImpact">是否包含落地冲击</param>
    /// <param name="heightMultiplier">抛物线高度系数</param>
    /// <returns>动画结束时间（相对于 startTime）</returns>
    public float InsertEnhancedMoveAnimation(
        Sequence sequence,
        float startTime,
        Vector3 targetLocalPosition,
        bool withAnticipation = true,
        bool withImpact = true,
        float? heightMultiplier = null)
    {
        Vector3 startPos = transform.localPosition;
        Vector3 moveDirection = (targetLocalPosition - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetLocalPosition);
        float arcHeight = distance * (heightMultiplier ?? arcHeightMultiplier);

        float mainMoveDuration = moveDuration;
        float riseTime = mainMoveDuration * 0.6f;
        float slamTime = mainMoveDuration * 0.4f;

        float baseY = targetLocalPosition.y;
        float peakY = baseY + arcHeight;

        float t = startTime;

        // 1. 预备动作
        if (withAnticipation && moveDirection != Vector3.zero)
        {
            Vector3 anticipationPos = startPos - moveDirection * anticipationDistance;
            sequence.Insert(t, transform.DOLocalMove(anticipationPos, anticipationDuration).SetEase(Ease.OutQuad));
            sequence.Insert(t, transform.DOScaleY(0.9f, anticipationDuration).SetEase(Ease.OutQuad));
            t += anticipationDuration;
        }

        // 2. 上升阶段
        Vector3 peakPos = new Vector3(targetLocalPosition.x, peakY, targetLocalPosition.z);
        sequence.Insert(t, transform.DOLocalMove(peakPos, riseTime).SetEase(Ease.OutQuad));
        if (withAnticipation)
        {
            sequence.Insert(t, transform.DOScaleY(1f, riseTime * 0.5f).SetEase(Ease.OutQuad));
        }
        t += riseTime;

        // 3. 下砸阶段
        sequence.Insert(t, transform.DOLocalMoveY(baseY, slamTime).SetEase(Ease.InQuart));
        t += slamTime;

        // 4. 落地冲击
        if (withImpact)
        {
            sequence.InsertCallback(t, () =>
            {
                TriggerLandingDust();
                ScreenShakeController.Instance?.ShakeOnCollision();
            });
            float strongSquash = landingSquashAmount * 1.5f;
            sequence.Insert(t, transform.DOScaleY(1f - strongSquash, landingSquashDuration * 0.3f).SetEase(Ease.OutQuad));
            sequence.Insert(t + landingSquashDuration * 0.3f, transform.DOScaleY(1f, landingSquashDuration * 0.7f).SetEase(Ease.OutBounce));
            t += landingSquashDuration;
        }

        return t - startTime;
    }

    /// <summary>
    /// 获取增强移动动画的总时长（用于计算 delay）
    /// </summary>
    public float GetEnhancedMoveDuration(bool withAnticipation = true, bool withImpact = true)
    {
        float duration = moveDuration;
        if (withAnticipation) duration += anticipationDuration;
        if (withImpact) duration += landingSquashDuration;
        return duration;
    }

    #endregion

    #region 特效触发方法

    /// <summary>触发开枪火药特效</summary>
    public void TriggerGunfire()
    {
        gunfireEffect?.Play();
    }

    /// <summary>触发开炮特效</summary>
    public void TriggerCannon()
    {
        cannonEffect?.Play();
    }

    /// <summary>触发爆炸特效</summary>
    public void TriggerExplosion()
    {
        explosionEffect?.Play();
    }

    /// <summary>触发落地尘土特效</summary>
    public void TriggerLandingDust()
    {
        landingDustEffect?.Play();
    }

    /// <summary>触发恢复特效</summary>
    public void TriggerHeal()
    {
        healEffect?.Play();
    }

    /// <summary>触发冲锋特效</summary>
    public void TriggerCharge()
    {
        chargeEffect?.Play();
    }

    #endregion

    private void OnMouseDown()
    {
        // 使用InputService检查是否可以进行游戏输入
        var inputService = GameRoot.Instance?.inputService;
        if (inputService != null && !inputService.CanStartGameplayInput())
            return;

        OnClicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        currentAnimation?.Kill();
        transform.DOKill();

        // 停止所有锡兵的动画
        if (soldierSlots != null)
        {
            foreach (var slot in soldierSlots)
            {
                if (slot != null)
                {
                    slot.DOKill();
                }
            }
        }
    }
}
