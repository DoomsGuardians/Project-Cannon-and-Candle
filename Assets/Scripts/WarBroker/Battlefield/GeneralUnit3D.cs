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
    [SerializeField] private Transform[] soldierSlots;  // 20个锡兵位置
    [SerializeField] private GameObject soldierPrefab;
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
    [SerializeField] private float moveDuration = 0.5f;

    [Header("意图气泡渐隐")]
    [SerializeField] private float bubbleFadeStartDistance = 8f;  // 开始渐隐的距离
    [SerializeField] private float bubbleFadeEndDistance = 4f;    // 完全隐藏的距离

    public GeneralData Data { get; private set; }
    public bool IsAlly { get; private set; }

    private GameObject[] spawnedSoldiers;
    private int currentSoldierCount = 0;
    private Sequence currentAnimation;

    public System.Action<GeneralUnit3D> OnClicked;

    private void Awake()
    {
        if (soldierSlots != null && soldierSlots.Length > 0)
        {
            spawnedSoldiers = new GameObject[soldierSlots.Length];
        }

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
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

    /// <summary>更新锡兵数量显示</summary>
    private void UpdateSoldierCount(int troops)
    {
        if (soldierSlots == null || soldierPrefab == null) return;

        int targetCount = Mathf.Clamp(troops, 0, soldierSlots.Length);

        // 增加锡兵
        while (currentSoldierCount < targetCount)
        {
            if (currentSoldierCount < spawnedSoldiers.Length && spawnedSoldiers[currentSoldierCount] == null)
            {
                var soldier = Instantiate(soldierPrefab, soldierSlots[currentSoldierCount]);
                soldier.transform.localPosition = Vector3.zero;
                soldier.transform.localRotation = Quaternion.identity;

                // 设置材质
                var renderer = soldier.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material = IsAlly ? allySoldierMaterial : enemySoldierMaterial;
                }

                spawnedSoldiers[currentSoldierCount] = soldier;
            }
            else if (currentSoldierCount < spawnedSoldiers.Length && spawnedSoldiers[currentSoldierCount] != null)
            {
                spawnedSoldiers[currentSoldierCount].SetActive(true);
            }
            currentSoldierCount++;
        }

        // 减少锡兵
        while (currentSoldierCount > targetCount)
        {
            currentSoldierCount--;
            if (currentSoldierCount < spawnedSoldiers.Length && spawnedSoldiers[currentSoldierCount] != null)
            {
                spawnedSoldiers[currentSoldierCount].SetActive(false);
            }
        }
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

    /// <summary>播放锡兵倒下动画</summary>
    public Sequence PlaySoldierFallAnimation(int casualties)
    {
        if (spawnedSoldiers == null || casualties <= 0) return null;

        currentAnimation?.Kill();
        currentAnimation = DOTween.Sequence();

        int startIndex = currentSoldierCount - 1;
        int endIndex = Mathf.Max(0, currentSoldierCount - casualties);

        for (int i = startIndex; i >= endIndex; i--)
        {
            if (i < spawnedSoldiers.Length && spawnedSoldiers[i] != null)
            {
                var soldier = spawnedSoldiers[i];
                int index = i;
                float delay = (startIndex - i) * soldierFallDelay;

                // 锡兵倒下动画：旋转90度 + 缩小
                currentAnimation.Insert(delay, soldier.transform
                    .DORotate(new Vector3(90f, 0f, Random.Range(-30f, 30f)), soldierFallDuration)
                    .SetEase(Ease.OutQuad));
                currentAnimation.Insert(delay, soldier.transform
                    .DOScale(0.5f, soldierFallDuration)
                    .SetEase(Ease.OutQuad));
                currentAnimation.InsertCallback(delay + soldierFallDuration, () =>
                {
                    if (spawnedSoldiers[index] != null)
                        spawnedSoldiers[index].SetActive(false);
                });
            }
        }

        currentAnimation.OnComplete(() =>
        {
            currentSoldierCount = Mathf.Max(0, currentSoldierCount - casualties);
        });

        return currentAnimation;
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

        if (spawnedSoldiers != null)
        {
            foreach (var soldier in spawnedSoldiers)
            {
                if (soldier != null)
                {
                    soldier.transform.DOKill();
                    Destroy(soldier);
                }
            }
        }
    }
}
