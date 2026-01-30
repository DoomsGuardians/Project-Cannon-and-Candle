using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Image intentBubbleImage;
    [SerializeField] private Text intentText;

    [Header("选中效果")]
    [SerializeField] private GameObject selectionIndicator;

    [Header("颜色配置")]
    [SerializeField] private Color defaultIntentColor = Color.gray;
    [SerializeField] private Color reinforcedIntentColor = new Color(1f, 0.84f, 0f); // 金色
    [SerializeField] private Color overriddenIntentColor = Color.red;

    public GeneralData Data { get; private set; }
    public bool IsAlly { get; private set; }

    private GameObject[] spawnedSoldiers;
    private int currentSoldierCount = 0;

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

    /// <summary>初始化将军单位</summary>
    public void Initialize(GeneralData data, bool isAlly)
    {
        Data = data;
        IsAlly = isAlly;

        UpdateDisplay();
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

    private void OnMouseDown()
    {
        // 检查输入是否被锁定
        if (!InputRouter.IsEnabled(InputChannel.Gameplay))
            return;

        OnClicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (spawnedSoldiers != null)
        {
            foreach (var soldier in spawnedSoldiers)
            {
                if (soldier != null)
                    Destroy(soldier);
            }
        }
    }
}
