using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

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
    public Sequence PlaySoldierFallAnimation(int casualties)
    {
        if (soldierSlots == null || casualties <= 0) return null;

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
        if (actualCasualties <= 0) return null;

        currentAnimation?.Kill();
        currentAnimation = DOTween.Sequence();

        for (int i = 0; i < actualCasualties; i++)
        {
            int idx = aliveIndices[i];
            var soldier = soldierSlots[idx].gameObject;
            float delay = i * soldierFallDelay;

            // 锡兵倒下动画：旋转90度 + 缩小
            currentAnimation.Insert(delay, soldier.transform
                .DORotate(new Vector3(90f, 0f, Random.Range(-30f, 30f)), soldierFallDuration)
                .SetEase(Ease.OutQuad));
            currentAnimation.Insert(delay, soldier.transform
                .DOScale(0.5f, soldierFallDuration)
                .SetEase(Ease.OutQuad));

            int capturedIdx = idx;
            currentAnimation.InsertCallback(delay + soldierFallDuration, () =>
            {
                if (soldierSlots[capturedIdx] != null)
                    soldierSlots[capturedIdx].gameObject.SetActive(false);
            });
        }

        currentAnimation.OnComplete(() =>
        {
            currentSoldierCount = Mathf.Max(0, currentSoldierCount - actualCasualties);
        });

        return currentAnimation;
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
    public Sequence PlaySoldierRiseAnimation(int reinforcements)
    {
        if (soldierSlots == null || reinforcements <= 0) return null;

        // 找出当前未显示但需要显示的锡兵索引
        var inactiveIndices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < soldierSlots.Length; i++)
        {
            if (soldierSlots[i] != null && !soldierSlots[i].gameObject.activeSelf)
                inactiveIndices.Add(i);
        }

        int actualReinforcements = Mathf.Min(reinforcements, inactiveIndices.Count);
        if (actualReinforcements <= 0) return null;

        // 取前 actualReinforcements 个（按顺序填充）
        var toActivate = inactiveIndices.GetRange(0, actualReinforcements);

        var seq = DOTween.Sequence();

        for (int i = 0; i < toActivate.Count; i++)
        {
            int idx = toActivate[i];
            var soldier = soldierSlots[idx].gameObject;
            float delay = i * soldierRiseDelay;

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

        seq.OnComplete(() =>
        {
            currentSoldierCount = Mathf.Min(soldierSlots.Length, currentSoldierCount + actualReinforcements);
        });

        return seq;
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
