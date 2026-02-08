using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using WarBroker.Battlefield;

/// <summary>
/// 3D 战场场景控制器
/// 管理三条战线和将军单位的显示
/// </summary>
public class BattlefieldSceneController : MonoBehaviour
{
    [Header("单位配置")]
    [SerializeField] private GameObject generalUnitPrefab;

    [Header("动画配置")]
    [SerializeField] private float animationDelay = 0.3f;  // 动画间隔
    [SerializeField] private float actionPauseDuration = 0.6f;  // 每组行动后的停顿时间

    [Header("特殊效果配置")]
    [SerializeField] private float criticalTimeScale = 0.3f;  // 暴击时间减速
    [SerializeField] private float criticalSlowDuration = 0.2f;  // 暴击减速持续时间
    [SerializeField] private CanvasGroup screenFlashOverlay;  // 屏幕闪白遮罩（可选）

    [Header("战斗爆炸特效")]
    [SerializeField] private GameObject[] battleExplosionPrefabs;  // 爆炸特效预制体数组
    [SerializeField] private float explosionSpawnRadius = 1.5f;    // 爆炸生成半径
    [SerializeField] private int explosionCountMin = 1;            // 最少爆炸数量
    [SerializeField] private int explosionCountMax = 1;            // 最多爆炸数量
    [SerializeField] private float explosionTimeSpread = 0.8f;     // 爆炸时间分散范围
    [SerializeField] private float explosionScaleMin = 0.15f;      // 爆炸最小缩放
    [SerializeField] private float explosionScaleMax = 0.35f;      // 爆炸最大缩放
    [SerializeField] private float explosionAvoidCenterRadius = 1f; // 避开中心的半径

    [Header("音效配置")]
    [SerializeField] private AudioClip[] explosionSounds;          // 爆炸音效数组
    [SerializeField] private AudioClip collisionSound;             // 碰撞音效
    [SerializeField] private AudioClip chargeSound;                // 冲锋音效
    [SerializeField] private AudioClip landingSound;               // 落地音效
    [SerializeField] private AudioClip[] gunfireSounds;            // 枪声音效数组（齐射用）
    [SerializeField] private AudioClip[] cannonSounds;             // 开炮音效数组
    [SerializeField] private AudioClip soldierFallSound;           // 锡兵倒下音效
    [SerializeField] private AudioClip soldierRiseSound;           // 锡兵复活音效
    [SerializeField] private AudioClip moveSound;                  // 部队移动音效
    [SerializeField] private AudioClip selectGeneralSound;         // 选中将军音效

    [Header("齐射音效设置")]
    [SerializeField] private int gunfireVolleyCount = 2;           // 齐射次数
    [SerializeField] private float gunfireVolleySpread = 0.15f;    // 齐射时间分散范围

    [Header("相机")]
    [SerializeField] private BattlefieldCameraController battlefieldCamera;

    [Header("战场根节点")]
    [SerializeField] private BattlefieldRoot battlefieldRoot;

    // 单位容器（所有单位的父级）
    private Transform unitsContainer;

    private Dictionary<string, GeneralUnit3D> allyUnits = new Dictionary<string, GeneralUnit3D>();
    private Dictionary<string, GeneralUnit3D> enemyUnits = new Dictionary<string, GeneralUnit3D>();

    private BattleData battleData;
    private GeneralUnit3D selectedUnit;

    private EventService eventService;
    private UIService uiService;
    private ResService resService;
    private AudioService audioService;
    private GameBalanceConfig balanceConfig;

    // 动画队列
    private Queue<BattleResult> animationQueue = new Queue<BattleResult>();
    private bool isPlayingAnimation = false;
    private Sequence currentSequence;
    private int totalBattlesInQueue = 0;  // 当前批次的战斗总数
    private int currentBattleIndex = 0;   // 当前播放的战斗索引

    /// <summary>Grid 方向信息，用于动画计算</summary>
    private struct GridDirectionInfo
    {
        public Vector3 Forward;           // Grid 的前向（敌方方向）
        public Vector3 Back;              // Grid 的后向（己方大本营方向）
        public Quaternion BaseRotation;   // 己方单位的基准旋转
        public Quaternion FlippedRotation; // 敌方单位的基准旋转（加 180 度）
    }

    public System.Action<GeneralData> OnGeneralSelected;
    public System.Action OnAllAnimationsComplete;  // 所有动画播放完成回调

    private void Awake()
    {
        if (GameRoot.Instance != null)
        {
            eventService = GameRoot.Instance.eventService;
            uiService = GameRoot.Instance.uIService;
            resService = GameRoot.Instance.resService;
            audioService = GameRoot.Instance.audioService;
            if (resService != null)
            {
                balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
            }
        }
    }

    private void Start()
    {
        if (eventService != null)
        {
            eventService.AddEventListening((EventID)WarBrokerEventID.OnBattleResult, OnBattleResult);
            eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnTurnEnd);
            eventService.AddEventListening((EventID)WarBrokerEventID.OnIntentChanged, OnIntentChanged);
            eventService.AddEventListening((EventID)WarBrokerEventID.OnGeneralRespawned, OnGeneralRespawned);
            eventService.AddEventListening((EventID)WarBrokerEventID.OnPhaseBannerComplete, OnPhaseBannerComplete);
        }
    }

    private void Update()
    {
        // 统一处理将军单位的点击检测
        HandleUnitClick();
    }

    /// <summary>处理将军单位点击</summary>
    private void HandleUnitClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        // 检查是否可以进行游戏输入
        var inputService = GameRoot.Instance?.inputService;
        if (inputService != null && !inputService.CanStartGameplayInput())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            var unit = hit.collider.GetComponent<GeneralUnit3D>();
            if (unit != null)
            {
                OnGeneralClicked(unit);
            }
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    /// <summary>清理战场资源</summary>
    public void Cleanup()
    {
        if (eventService != null)
        {
            eventService.RemoveEventListeningByTarget(this);
        }

        ClearAllUnits();
    }

    /// <summary>设置战场根节点引用</summary>
    public void SetBattlefieldRoot(BattlefieldRoot root)
    {
        battlefieldRoot = root;

        // 创建单位容器
        if (unitsContainer == null)
        {
            var containerGO = new GameObject("UnitsContainer");
            unitsContainer = containerGO.transform;
            if (root != null)
            {
                unitsContainer.SetParent(root.transform);
            }
        }
    }

    /// <summary>初始化战场</summary>
    public void Initialize(BattleData data)
    {
        battleData = data;

        // 如果没有在 Inspector 中配置，尝试自动查找
        if (battlefieldRoot == null)
        {
            battlefieldRoot = FindObjectOfType<BattlefieldRoot>();
            if (battlefieldRoot == null)
            {
                Debug.LogError("[BattlefieldSceneController] BattlefieldRoot not found in scene!");
                return;
            }
        }

        // 从 BattlefieldRoot 获取单位容器
        unitsContainer = battlefieldRoot.unitsContainer;
        if (unitsContainer == null)
        {
            // 如果没有配置，自动创建
            var containerGO = new GameObject("UnitsContainer");
            unitsContainer = containerGO.transform;
            unitsContainer.SetParent(battlefieldRoot.transform);
        }

        ClearAllUnits();
        SpawnAllUnits();
        UpdateAllUnits();
    }

    /// <summary>更新所有单位</summary>
    public void UpdateUnits(BattleData data)
    {
        battleData = data;
        UpdateAllUnits();
    }

    private void SpawnAllUnits()
    {
        if (battleData == null || generalUnitPrefab == null) return;

        // 生成己方将军
        foreach (var general in battleData.AllyGenerals)
        {
            var unit = SpawnUnit(general, true);
            if (unit != null)
            {
                allyUnits[general.GeneralId] = unit;
            }
        }

        // 生成敌方将军
        foreach (var general in battleData.EnemyGenerals)
        {
            var unit = SpawnUnit(general, false);
            if (unit != null)
            {
                enemyUnits[general.GeneralId] = unit;
            }
        }
    }

    private GeneralUnit3D SpawnUnit(GeneralData general, bool isAlly)
    {
        if (unitsContainer == null) return null;

        var go = Instantiate(generalUnitPrefab, unitsContainer);
        go.name = $"{(isAlly ? "Ally" : "Enemy")}_{general.Name}";
        var unit = go.GetComponent<GeneralUnit3D>();

        if (unit != null)
        {
            unit.Initialize(general, isAlly);

            // 设置位置
            UpdateUnitPosition(unit, general, isAlly);
        }

        return unit;
    }

    /// <summary>将 FrontlinePosition 映射到场景战线索引（支持动态战线数量）</summary>
    private int GetSceneLaneIndex(FrontlinePosition position)
    {
        if (battlefieldRoot == null) return 0;

        int laneCount = battlefieldRoot.LaneCount;
        var positions = FrontlinePositionExtensions.GetPositionsForLaneCount(laneCount);

        // 找到该 position 在当前配置中的索引
        // 例如 3 条战线 [Left, Center, Right] 映射到 [0, 1, 2]
        for (int i = 0; i < positions.Length; i++)
        {
            if (positions[i] == position)
                return i;
        }

        // 回退：限制在有效范围内
        Debug.LogWarning($"[BattleAnim] Position {position} not in current lane config (laneCount={laneCount})");
        return Mathf.Clamp((int)position, 0, laneCount - 1);
    }

    private void UpdateUnitPosition(GeneralUnit3D unit, GeneralData general, bool isAlly)
    {
        if (unit == null || battlefieldRoot == null) return;

        int laneIndex = GetSceneLaneIndex(general.Position);
        // GridPosition 是 1-based，转换为 0-based 索引
        int gridIndex = general.GridPosition - 1;
        Vector3 gridPos = battlefieldRoot.GetGridPosition(laneIndex, gridIndex);
        unit.transform.localPosition = gridPos;

        // 使用格子的旋转，敌方额外旋转 180 度
        Transform gridAnchor = battlefieldRoot.GetGridAnchor(laneIndex, gridIndex);
        if (gridAnchor != null)
        {
            unit.transform.rotation = isAlly ? gridAnchor.rotation : gridAnchor.rotation * Quaternion.Euler(0, 180, 0);
        }
        else
        {
            // 后备：使用固定朝向
            unit.transform.localRotation = Quaternion.Euler(0, isAlly ? 0 : 180, 0);
        }
    }

    /// <summary>获取指定 Grid 的方向信息</summary>
    private GridDirectionInfo GetGridDirection(int laneIndex, int gridIndex)
    {
        Transform anchor = battlefieldRoot?.GetGridAnchor(laneIndex, gridIndex);
        if (anchor == null)
        {
            // 默认方向：Z 轴为前向
            return new GridDirectionInfo
            {
                Forward = Vector3.forward,
                Back = Vector3.back,
                BaseRotation = Quaternion.identity,
                FlippedRotation = Quaternion.Euler(0, 180, 0)
            };
        }

        return new GridDirectionInfo
        {
            Forward = anchor.forward,
            Back = -anchor.forward,
            BaseRotation = anchor.rotation,
            FlippedRotation = anchor.rotation * Quaternion.Euler(0, 180, 0)
        };
    }

    /// <summary>获取格子的本地坐标</summary>
    private Vector3 GetGridLocalPosition(int gridPosition, bool isAlly, GeneralUnit3D unit)
    {
        if (battlefieldRoot == null || unit == null)
        {
            return Vector3.zero;
        }

        // 从单位数据获取战线索引
        int laneIndex = GetSceneLaneIndex(unit.Data?.Position ?? FrontlinePosition.Center);

        // GridPosition 是 1-based，转换为 0-based 索引
        // UnitsContainer 在原点，所以世界坐标 = 本地坐标
        return battlefieldRoot.GetGridPosition(laneIndex, gridPosition - 1);
    }

    private void UpdateAllUnits()
    {
        if (battleData == null) return;

        // 更新己方单位
        foreach (var general in battleData.AllyGenerals)
        {
            if (allyUnits.TryGetValue(general.GeneralId, out var unit))
            {
                unit.Initialize(general, true);
                UpdateUnitPosition(unit, general, true);
            }
        }

        // 更新敌方单位
        foreach (var general in battleData.EnemyGenerals)
        {
            if (enemyUnits.TryGetValue(general.GeneralId, out var unit))
            {
                unit.Initialize(general, false);
                UpdateUnitPosition(unit, general, false);
            }
        }
    }

    private void ClearAllUnits()
    {
        foreach (var kvp in allyUnits)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        allyUnits.Clear();

        foreach (var kvp in enemyUnits)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        enemyUnits.Clear();

        selectedUnit = null;
    }

    /// <summary>将军被点击</summary>
    public void OnGeneralClicked(GeneralUnit3D unit)
    {
        if (unit == null) return;

        // 检查当前阶段，非玩家阶段不允许操作
        var campaignSystem = GameRoot.Instance?.campaignSystem;
        var currentPhase = campaignSystem?.Data?.CurrentPhase ?? TurnPhase.BattlePhase;
        bool isPlayerPhase = currentPhase == TurnPhase.TurnStart
                          || currentPhase == TurnPhase.EventPhase
                          || currentPhase == TurnPhase.MarketPhase;

        if (!isPlayerPhase)
        {
            // 非玩家阶段，不允许打开面板和聚焦
            return;
        }

        // 播放选中将军音效
        PlaySelectGeneralSound();

        // 取消之前的选中
        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
        }

        // 选中新单位
        selectedUnit = unit;
        selectedUnit.SetSelected(true);

        if (unit.Data != null)
        {
            if (unit.IsAlly)
            {
                // 己方将军：打开完整详情面板
                OnGeneralSelected?.Invoke(unit.Data);

                var detailPanel = uiService?.ShowWindow<GeneralDetailPanel>("GeneralDetailPanel");
                if (detailPanel != null)
                {
                    detailPanel.SetGeneral(unit.Data, true);
                    // 通知教程系统面板已打开
                    TutorialManager.Instance?.OnPanelOpened("GeneralDetailPanel");
                }
            }
            else
            {
                // 敌方将军：打开简略面板
                var detailPanel = uiService?.ShowWindow<GeneralDetailPanel>("GeneralDetailPanel");
                if (detailPanel != null)
                {
                    detailPanel.SetGeneral(unit.Data, false);
                }
            }
        }

        // 相机平滑聚焦
        if (battlefieldCamera != null)
        {
            battlefieldCamera.SmoothFocusOn(unit.transform);
        }
    }

    /// <summary>取消选中</summary>
    public void DeselectAll()
    {
        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
            selectedUnit = null;
        }
    }

    /// <summary>获取选中的将军</summary>
    public GeneralData GetSelectedGeneral()
    {
        return selectedUnit?.Data;
    }

    #region 事件处理

    private void OnBattleResult(object p1, object p2)
    {
        var result = p1 as BattleResult;
        if (result != null)
        {
            // 将战斗结果加入队列
            animationQueue.Enqueue(result);
            totalBattlesInQueue++;

            // 如果当前没有播放动画，开始播放
            if (!isPlayingAnimation)
            {
                PlayNextAnimation();
            }
        }
        else
        {
            UpdateAllUnits();
        }
    }

    /// <summary>播放队列中的下一个动画</summary>
    private void PlayNextAnimation()
    {
        if (animationQueue.Count == 0)
        {
            isPlayingAnimation = false;
            totalBattlesInQueue = 0;
            currentBattleIndex = 0;
            OnAllAnimationsComplete?.Invoke();

            // 发送事件通知战斗动画播放完成
            eventService?.SendMessage((EventID)WarBrokerEventID.OnBattleAnimationsComplete, null, null);
            return;
        }

        isPlayingAnimation = true;
        currentBattleIndex++;
        var result = animationQueue.Dequeue();

        bool isFirstBattle = (currentBattleIndex == 1);
        bool isLastBattle = (animationQueue.Count == 0);

        PlayBattleAnimation(result, isFirstBattle, isLastBattle);
    }

    /// <summary>播放战斗动画</summary>
    private void PlayBattleAnimation(BattleResult result, bool isFirstBattle, bool isLastBattle)
    {
        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        // 第一个战斗：保存战斗前位置
        if (isFirstBattle && battlefieldCamera != null)
        {
            battlefieldCamera.SavePreBattlePosition();
        }

        // 通过Position查找参战单位
        GeneralUnit3D allyUnit = FindUnitByPosition(allyUnits, result.Position);
        GeneralUnit3D enemyUnit = FindUnitByPosition(enemyUnits, result.Position);

        // 相机跟随双方将军单位
        if (battlefieldCamera != null)
        {
            Transform allyTransform = allyUnit?.transform;
            Transform enemyTransform = enemyUnit?.transform;
            battlefieldCamera.SmoothFollowBattle(allyTransform, enemyTransform);
        }

        int laneCount = battlefieldRoot?.LaneCount ?? 3;
        Debug.Log($"[BattleAnim] {result.Position.ToDisplayName(laneCount)}: {result.AllyOrder.ToDisplayName()} vs {result.EnemyOrder.ToDisplayName()}, Ally {result.AllyOldPosition}->{result.AllyNewPosition}, Enemy {result.EnemyOldPosition}->{result.EnemyNewPosition}");

        // 等待 Cinemachine 过渡完成后再开始战斗动画
        float cameraDelay = battlefieldCamera != null ? battlefieldCamera.GetBlendDuration() : 0.5f;
        float delay = cameraDelay;

        // 根据指令组合选择不同的动画序列
        delay = AnimateByOrderCombination(result, allyUnit, enemyUnit, delay);

        // 受伤动画（锡兵倒下）
        delay = AnimateTroopChanges(result, allyUnit, enemyUnit, delay);

        // 添加动画间隔和行动后停顿 - 使用 Insert 确保在所有动画之后
        float totalDuration = delay + animationDelay + actionPauseDuration;
        currentSequence.InsertCallback(totalDuration, () => { }); // 确保序列持续到这个时间点

        // 动画完成后播放下一个
        currentSequence.OnComplete(() =>
        {
            // 更新单位显示
            if (allyUnit != null) allyUnit.UpdateDisplay();
            if (enemyUnit != null) enemyUnit.UpdateDisplay();

            // 检查溃败，播放溃败动画并隐藏单位
            CheckAndPlayRoutAnimation(allyUnit, true);
            CheckAndPlayRoutAnimation(enemyUnit, false);

            // 播放下一个动画
            PlayNextAnimation();
        });
    }

    /// <summary>根据指令组合选择动画</summary>
    private float AnimateByOrderCombination(BattleResult result, GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, float delay)
    {
        // 计算目标位置（使用 BattlefieldRoot 获取格子位置）
        Vector3 allyTargetLocal = GetGridLocalPosition(result.AllyNewPosition, true, allyUnit);
        Vector3 enemyTargetLocal = GetGridLocalPosition(result.EnemyNewPosition, false, enemyUnit);

        // 获取起始和目标 Grid 的方向信息（双方在同一战线，使用相同的 laneIndex）
        int laneIndex = GetSceneLaneIndex(result.Position);
        GridDirectionInfo allyOldGridDir = GetGridDirection(laneIndex, result.AllyOldPosition - 1);
        GridDirectionInfo allyNewGridDir = GetGridDirection(laneIndex, result.AllyNewPosition - 1);
        GridDirectionInfo enemyOldGridDir = GetGridDirection(laneIndex, result.EnemyOldPosition - 1);
        GridDirectionInfo enemyNewGridDir = GetGridDirection(laneIndex, result.EnemyNewPosition - 1);

        // 计算实际战斗伤害（不含大本营恢复）用于判断是否有接触
        int allyBattleDamage = result.AllyTroopChange - result.AllyBaseRecovery;
        int enemyBattleDamage = result.EnemyTroopChange - result.EnemyBaseRecovery;

        // 判断是否发生了战斗接触（有伤亡 = 有接触，或者接触状态下有 RET 指令）
        bool hasRET = result.AllyOrder == OrderType.RET || result.EnemyOrder == OrderType.RET;
        bool hasContact = allyBattleDamage < 0 || enemyBattleDamage < 0 || hasRET;

        // 如果没有接触，只播放简单移动动画
        if (!hasContact)
        {
            return AnimateSimpleMove(allyUnit, enemyUnit, allyTargetLocal, enemyTargetLocal, delay, result, allyOldGridDir, allyNewGridDir, enemyOldGridDir, enemyNewGridDir);
        }

        // 有接触时，根据指令组合分发到不同的动画方法
        switch (result.AllyOrder)
        {
            case OrderType.ATK:
                switch (result.EnemyOrder)
                {
                    case OrderType.ATK:
                        return AnimateATKvsATK(allyUnit, enemyUnit, allyTargetLocal, enemyTargetLocal, delay, result, allyOldGridDir, enemyOldGridDir);
                    case OrderType.DEF:
                        return AnimateATKvsDEF(allyUnit, enemyUnit, allyTargetLocal, enemyTargetLocal, delay, result, allyOldGridDir, enemyOldGridDir);
                    case OrderType.RET:
                        return AnimateATKvsRET(allyUnit, enemyUnit, allyTargetLocal, enemyTargetLocal, delay, result, allyOldGridDir, allyNewGridDir, enemyOldGridDir, enemyNewGridDir);
                }
                break;
            case OrderType.DEF:
                switch (result.EnemyOrder)
                {
                    case OrderType.ATK:
                        return AnimateDEFvsATK(allyUnit, enemyUnit, allyTargetLocal, enemyTargetLocal, delay, result, allyOldGridDir, enemyOldGridDir);
                    case OrderType.DEF:
                        return AnimateDEFvsDEF(allyUnit, enemyUnit, delay, result, allyOldGridDir, enemyOldGridDir);
                    case OrderType.RET:
                        return AnimateDEFvsRET(allyUnit, enemyUnit, enemyTargetLocal, delay, result, allyOldGridDir, enemyOldGridDir, enemyNewGridDir);
                }
                break;
            case OrderType.RET:
                switch (result.EnemyOrder)
                {
                    case OrderType.ATK:
                        return AnimateRETvsATK(allyUnit, enemyUnit, allyTargetLocal, enemyTargetLocal, delay, result, allyOldGridDir, allyNewGridDir, enemyOldGridDir, enemyNewGridDir);
                    case OrderType.DEF:
                        return AnimateRETvsDEF(allyUnit, enemyUnit, allyTargetLocal, delay, result, allyOldGridDir, allyNewGridDir, enemyOldGridDir);
                    case OrderType.RET:
                        return AnimateRETvsRET(allyUnit, enemyUnit, allyTargetLocal, enemyTargetLocal, delay, result, allyOldGridDir, allyNewGridDir, enemyOldGridDir, enemyNewGridDir);
                }
                break;
        }

        // 默认：简单移动
        return AnimateSimpleMove(allyUnit, enemyUnit, allyTargetLocal, enemyTargetLocal, delay, result, allyOldGridDir, allyNewGridDir, enemyOldGridDir, enemyNewGridDir);
    }

    /// <summary>处理兵力变化动画</summary>
    private float AnimateTroopChanges(BattleResult result, GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, float delay)
    {
        // 暴击特效
        if (result.WasCrit)
        {
            delay = PlayCriticalHitEffect(allyUnit, enemyUnit, delay, result);
        }
        // 失误特效
        else if (result.WasFumble)
        {
            delay = PlayFumbleEffect(allyUnit, enemyUnit, delay, result);
        }

        // 计算实际战斗伤害（不含大本营恢复）
        int allyBattleDamage = result.AllyTroopChange - result.AllyBaseRecovery;
        int enemyBattleDamage = result.EnemyTroopChange - result.EnemyBaseRecovery;

        // 锡兵倒下动画（战斗伤害）
        float fallDuration = 0f;
        bool hasFallAnimation = false;
        if (allyUnit != null && allyBattleDamage < 0)
        {
            float duration = allyUnit.PlaySoldierFallAnimation(-allyBattleDamage, currentSequence, delay);
            fallDuration = Mathf.Max(fallDuration, duration);
            hasFallAnimation = true;
        }
        if (enemyUnit != null && enemyBattleDamage < 0)
        {
            float duration = enemyUnit.PlaySoldierFallAnimation(-enemyBattleDamage, currentSequence, delay);
            fallDuration = Mathf.Max(fallDuration, duration);
            hasFallAnimation = true;
        }
        if (hasFallAnimation)
        {
            currentSequence.InsertCallback(delay, () => PlaySoldierFallSound());
        }
        if (fallDuration > 0)
        {
            delay += fallDuration;
        }

        // 锡兵恢复动画（战斗回血，如 RET vs DEF）
        float riseDuration = 0f;
        bool hasRiseAnimation = false;
        if (allyUnit != null && allyBattleDamage > 0)
        {
            float duration = allyUnit.PlaySoldierRiseAnimation(allyBattleDamage, currentSequence, delay);
            riseDuration = Mathf.Max(riseDuration, duration);
            currentSequence.InsertCallback(delay, () => allyUnit.TriggerHeal());
            hasRiseAnimation = true;
        }
        if (enemyUnit != null && enemyBattleDamage > 0)
        {
            float duration = enemyUnit.PlaySoldierRiseAnimation(enemyBattleDamage, currentSequence, delay);
            riseDuration = Mathf.Max(riseDuration, duration);
            currentSequence.InsertCallback(delay, () => enemyUnit.TriggerHeal());
            hasRiseAnimation = true;
        }
        if (hasRiseAnimation)
        {
            currentSequence.InsertCallback(delay, () => PlaySoldierRiseSound());
        }
        if (riseDuration > 0)
        {
            delay += riseDuration;
        }

        // 大本营恢复动画（单独播放）
        float baseRecoveryDuration = 0f;
        bool hasBaseRecovery = false;
        if (allyUnit != null && result.AllyBaseRecovery > 0)
        {
            float duration = allyUnit.PlaySoldierRiseAnimation(result.AllyBaseRecovery, currentSequence, delay);
            baseRecoveryDuration = Mathf.Max(baseRecoveryDuration, duration);
            currentSequence.InsertCallback(delay, () => allyUnit.TriggerHeal());
            hasBaseRecovery = true;
        }
        if (enemyUnit != null && result.EnemyBaseRecovery > 0)
        {
            float duration = enemyUnit.PlaySoldierRiseAnimation(result.EnemyBaseRecovery, currentSequence, delay);
            baseRecoveryDuration = Mathf.Max(baseRecoveryDuration, duration);
            currentSequence.InsertCallback(delay, () => enemyUnit.TriggerHeal());
            hasBaseRecovery = true;
        }
        if (hasBaseRecovery)
        {
            currentSequence.InsertCallback(delay, () => PlaySoldierRiseSound());
        }
        if (baseRecoveryDuration > 0)
        {
            delay += baseRecoveryDuration;
        }

        return delay;
    }

    /// <summary>播放暴击特效：时间减速 + 屏幕闪白 + 强震动</summary>
    private float PlayCriticalHitEffect(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, float delay, BattleResult result)
    {
        // 确定攻击方和受击方
        bool allyIsAttacker = result.AllyOrder == OrderType.ATK;
        GeneralUnit3D attacker = allyIsAttacker ? allyUnit : enemyUnit;
        GeneralUnit3D victim = allyIsAttacker ? enemyUnit : allyUnit;

        // 1. 时间减速效果
        currentSequence.InsertCallback(delay, () =>
        {
            StartCoroutine(CriticalTimeSlowCoroutine());
        });

        // 2. 屏幕闪白
        if (screenFlashOverlay != null)
        {
            currentSequence.Insert(delay, screenFlashOverlay.DOFade(0.6f, 0.05f));
            currentSequence.Insert(delay + 0.05f, screenFlashOverlay.DOFade(0f, 0.15f));
        }

        // 3. 屏幕震动
        currentSequence.InsertCallback(delay, () =>
        {
            ScreenShakeController.Instance?.ShakeOnCollision();
        });

        // 4. 受击方剧烈震动
        if (victim != null)
        {
            currentSequence.Insert(delay, victim.transform.DOShakePosition(0.3f, 0.4f, 20));
            currentSequence.Insert(delay, victim.transform.DOShakeRotation(0.3f, new Vector3(10f, 5f, 10f), 15));
        }

        // 5. 攻击方爆炸特效
        if (attacker != null)
        {
            currentSequence.InsertCallback(delay, () =>
            {
                attacker.TriggerCannon();
                attacker.TriggerExplosion();
                PlayCannonSound();
            });
        }

        delay += 0.35f;
        return delay;
    }

    /// <summary>播放失误特效：绊倒动画 + 尴尬停顿</summary>
    private float PlayFumbleEffect(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, float delay, BattleResult result)
    {
        // 确定失误方（攻击方失误）
        bool allyIsAttacker = result.AllyOrder == OrderType.ATK;
        GeneralUnit3D fumbler = allyIsAttacker ? allyUnit : enemyUnit;

        if (fumbler != null)
        {
            // 直接插入绊倒动画，避免嵌套 Sequence
            // 向前倾斜
            currentSequence.Insert(delay, fumbler.transform
                .DORotate(new Vector3(25f, 0f, Random.Range(-10f, 10f)), 0.15f)
                .SetEase(Ease.OutQuad));
            delay += 0.15f;

            // 尴尬停顿
            delay += 0.2f;

            // 恢复
            currentSequence.Insert(delay, fumbler.transform
                .DORotate(Vector3.zero, 0.25f)
                .SetEase(Ease.OutBack));
            delay += 0.25f;
        }

        return delay;
    }

    /// <summary>暴击时间减速协程</summary>
    private System.Collections.IEnumerator CriticalTimeSlowCoroutine()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = criticalTimeScale;

        // 使用真实时间等待
        float elapsed = 0f;
        while (elapsed < criticalSlowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 平滑恢复时间
        float recoverDuration = 0.1f;
        float recoverElapsed = 0f;
        while (recoverElapsed < recoverDuration)
        {
            recoverElapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(criticalTimeScale, originalTimeScale, recoverElapsed / recoverDuration);
            yield return null;
        }

        Time.timeScale = originalTimeScale;
    }

    /// <summary>在两个单位周围播放随机爆炸特效</summary>
    private void PlayBattleExplosions(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, float startDelay)
    {
        if (battleExplosionPrefabs == null || battleExplosionPrefabs.Length == 0) return;

        // 播放开炮音效
        currentSequence.InsertCallback(startDelay, () => PlayCannonSound());

        int explosionCount = Random.Range(explosionCountMin, explosionCountMax + 1);

        // 计算中心点（两单位之间）
        Vector3 centerPoint = Vector3.zero;
        if (allyUnit != null && enemyUnit != null)
        {
            centerPoint = (allyUnit.transform.position + enemyUnit.transform.position) / 2f;
        }

        for (int i = 0; i < explosionCount; i++)
        {
            // 随机选择在哪个单位周围生成
            GeneralUnit3D targetUnit = Random.value > 0.5f ? allyUnit : enemyUnit;
            if (targetUnit == null) targetUnit = allyUnit ?? enemyUnit;
            if (targetUnit == null) continue;

            // 随机位置偏移，避开中心冲锋路线（X轴偏移更大）
            float xOffset = Random.Range(explosionAvoidCenterRadius, explosionSpawnRadius) * (Random.value > 0.5f ? 1f : -1f);
            Vector3 randomOffset = new Vector3(
                xOffset,
                Random.Range(0f, explosionSpawnRadius * 0.3f),
                Random.Range(-explosionSpawnRadius * 0.5f, explosionSpawnRadius * 0.5f)
            );
            Vector3 spawnPos = targetUnit.transform.position + randomOffset;

            // 随机时间偏移
            float timeOffset = Random.Range(0f, explosionTimeSpread);

            // 随机选择爆炸特效
            GameObject explosionPrefab = battleExplosionPrefabs[Random.Range(0, battleExplosionPrefabs.Length)];

            // 在 Sequence 中插入生成爆炸的回调
            float spawnTime = startDelay + timeOffset;
            currentSequence.InsertCallback(spawnTime, () =>
            {
                SpawnExplosionEffect(explosionPrefab, spawnPos);
            });
        }
    }

    /// <summary>生成单个爆炸特效</summary>
    private void SpawnExplosionEffect(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;

        GameObject explosion = Instantiate(prefab, position, Quaternion.identity);

        // 随机旋转
        explosion.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        // 随机缩放（使用可配置参数）
        float scale = Random.Range(explosionScaleMin, explosionScaleMax);
        explosion.transform.localScale = Vector3.one * scale;

        // 播放爆炸音效
        PlayExplosionSound(position);

        // 自动销毁
        Destroy(explosion, 3f);
    }

    /// <summary>播放爆炸音效</summary>
    private void PlayExplosionSound(Vector3 position)
    {
        if (explosionSounds == null || explosionSounds.Length == 0) return;

        AudioClip clip = explosionSounds[Random.Range(0, explosionSounds.Length)];
        audioService?.PlaySFX(clip, Random.Range(0.2f, 0.25f));
    }

    /// <summary>播放碰撞音效</summary>
    private void PlayCollisionSound()
    {
        if (collisionSound == null) return;
        audioService?.PlaySFX(collisionSound);
    }

    /// <summary>播放冲锋音效</summary>
    private void PlayChargeSound()
    {
        if (chargeSound == null) return;
        if (audioService == null) audioService = GameRoot.Instance?.audioService;
        audioService?.PlaySFX(chargeSound, 1f);
    }

    /// <summary>播放落地音效</summary>
    private void PlayLandingSound()
    {
        if (landingSound == null) return;
        if (audioService == null) audioService = GameRoot.Instance?.audioService;
        audioService?.PlaySFX(landingSound);
    }

    /// <summary>播放枪声齐射音效</summary>
    private void PlayGunfireSound()
    {
        if (gunfireSounds == null || gunfireSounds.Length == 0) return;

        // 播放多次枪声，模拟齐射效果
        for (int i = 0; i < gunfireVolleyCount; i++)
        {
            float delay = Random.Range(0f, gunfireVolleySpread);
            AudioClip clip = gunfireSounds[Random.Range(0, gunfireSounds.Length)];
            float volume = Random.Range(0.12f, 0.18f);

            // 使用协程延迟播放
            StartCoroutine(PlaySoundDelayed(clip, delay, volume));
        }
    }

    /// <summary>延迟播放音效</summary>
    private System.Collections.IEnumerator PlaySoundDelayed(AudioClip clip, float delay, float volume)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        audioService?.PlaySFX(clip, volume);
    }

    /// <summary>播放开炮音效</summary>
    private void PlayCannonSound()
    {
        if (cannonSounds == null || cannonSounds.Length == 0) return;
        var clip = cannonSounds[Random.Range(0, cannonSounds.Length)];
        audioService?.PlaySFX(clip, 0.4f);
    }

    /// <summary>播放锡兵倒下音效</summary>
    private void PlaySoldierFallSound()
    {
        if (soldierFallSound == null) return;
        audioService?.PlaySFX(soldierFallSound, 0.7f);
    }

    /// <summary>播放锡兵复活音效</summary>
    private void PlaySoldierRiseSound()
    {
        if (soldierRiseSound == null) return;
        audioService?.PlaySFX(soldierRiseSound, 0.7f);
    }

    /// <summary>播放部队移动音效</summary>
    private void PlayMoveSound()
    {
        if (moveSound == null) return;
        audioService?.PlaySFX(moveSound, 0.6f);
    }

    /// <summary>播放选中将军音效</summary>
    public void PlaySelectGeneralSound()
    {
        if (selectGeneralSound == null) return;
        audioService?.PlaySFX(selectGeneralSound, 0.8f);
    }

    /// <summary>检查溃败并播放溃败动画</summary>
    private void CheckAndPlayRoutAnimation(GeneralUnit3D unit, bool isAlly)
    {
        if (unit == null || unit.Data == null || balanceConfig == null) return;

        if (unit.Data.GetStatus(balanceConfig) == GeneralStatus.Routed)
        {
            // 播放逃跑移动音效
            PlayMoveSound();

            // 获取 Grid 方向信息
            int laneIndex = GetSceneLaneIndex(unit.Data.Position);
            int gridIndex = unit.Data.GridPosition - 1;
            GridDirectionInfo gridDir = GetGridDirection(laneIndex, gridIndex);

            // 己方向后方（Back）撤退，敌方向前方（Forward，即己方视角的后方）撤退
            Vector3 exitDir = isAlly ? gridDir.Back : gridDir.Forward;
            var seq = unit.PlayRoutAnimation(exitDir);
            seq.OnComplete(() => unit.Hide());
        }
    }

    private GeneralUnit3D FindUnitByPosition(Dictionary<string, GeneralUnit3D> units, FrontlinePosition position)
    {
        foreach (var kvp in units)
        {
            if (kvp.Value != null && kvp.Value.Data != null && kvp.Value.Data.Position == position)
            {
                return kvp.Value;
            }
        }
        return null;
    }

    private void OnTurnEnd(object p1, object p2)
    {
        UpdateAllUnits();
    }

    private void OnIntentChanged(object p1, object p2)
    {
        // 更新所有己方单位的意图气泡和士兵姿态
        foreach (var kvp in allyUnits)
        {
            kvp.Value?.UpdateDisplay();
        }
    }

    private void OnPhaseBannerComplete(object p1, object p2)
    {
        var phase = (TurnPhase)p1;

        if (phase == TurnPhase.BattlePhase)
        {
            // 战斗阶段横幅结束后隐藏气泡
            foreach (var kvp in allyUnits)
            {
                kvp.Value?.HideIntentBubble();
            }
        }
        else if (phase == TurnPhase.MarketPhase)
        {
            // 玩家阶段横幅结束后：更新士兵姿态并显示新的意图气泡
            foreach (var kvp in allyUnits)
            {
                kvp.Value?.UpdateDisplay();
                kvp.Value?.ShowIntentBubble();
            }
        }
    }

    /// <summary>将军复活事件处理</summary>
    private void OnGeneralRespawned(object p1, object p2)
    {
        var general = p1 as GeneralData;
        if (general == null) return;

        bool isAlly = (bool)p2;
        var units = isAlly ? allyUnits : enemyUnits;

        if (units.TryGetValue(general.GeneralId, out var unit))
        {
            // 重置位置到大本营
            UpdateUnitPosition(unit, general, isAlly);

            // 重置锡兵并显示
            unit.ResetSoldiers(general.Troops);
            unit.Show();
        }
    }

    #endregion

    #region 指令组合动画

    /// <summary>ATK vs ATK - 遭遇战：双方冲向中间猛烈撞击，弹飞后落地</summary>
    private float AnimateATKvsATK(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, Vector3 allyTarget, Vector3 enemyTarget, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo enemyOldGridDir)
    {
        if (allyUnit == null || enemyUnit == null) return delay;

        Vector3 allyStart = allyUnit.transform.localPosition;
        Vector3 enemyStart = enemyUnit.transform.localPosition;
        Vector3 collisionPoint = (allyStart + enemyStart) / 2f;

        // 1. 蓄力 + 炮火同时进行（使用 Grid 后向）
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMove(allyStart + allyOldGridDir.Back * 0.4f, 0.5f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMove(enemyStart + enemyOldGridDir.Back * 0.4f, 0.5f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.8f, 0.5f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.8f, 0.5f).SetEase(Ease.OutQuad));

        // 炮火在蓄力期间播放
        PlayBattleExplosions(allyUnit, enemyUnit, delay);
        currentSequence.InsertCallback(delay, () => { PlayGunfireSound(); });
        delay += 0.5f;

        // 2. 猛烈冲锋：在炮火中冲向中点（使用 Grid 后向计算偏移）
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMove(collisionPoint + allyOldGridDir.Back * 0.3f, 0.12f).SetEase(Ease.InQuart));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMove(collisionPoint + enemyOldGridDir.Back * 0.3f, 0.12f).SetEase(Ease.InQuart));
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(1f, 0.08f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(1f, 0.08f).SetEase(Ease.OutQuad));
        currentSequence.InsertCallback(delay + 0.05f, () => { allyUnit.TriggerCharge(); enemyUnit.TriggerCharge(); PlayChargeSound(); });
        delay += 0.12f;

        // 3. 碰撞瞬间：震动 + 特效 + 音效
        currentSequence.InsertCallback(delay, () =>
        {
            ScreenShakeController.Instance?.ShakeOnCollision();
            allyUnit.TriggerGunfire();
            enemyUnit.TriggerGunfire();
            allyUnit.TriggerExplosion();
            PlayCollisionSound();
        });

        delay += 0.02f;

        // 4. 弹飞抛物线：快速上升到最高点（减速）
        float flyHeight = 2.5f;
        float riseTime = 0.2f;
        float hangTime = 0.08f; // 最高点停顿
        float fallTime = 0.18f;

        // 水平方向快速弹开（弹回原位）
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveX(allyStart.x, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveZ(allyStart.z, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveX(enemyStart.x, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveZ(enemyStart.z, riseTime).SetEase(Ease.OutQuad));

        // 垂直方向：快速上升减速到最高点
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveY(flyHeight, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveY(flyHeight, riseTime).SetEase(Ease.OutQuad));

        // 空中旋转（使用 Grid 基准旋转）
        Quaternion allyAirRotation = allyOldGridDir.BaseRotation * Quaternion.Euler(-25f, 0, Random.Range(-15f, 15f));
        Quaternion enemyAirRotation = enemyOldGridDir.FlippedRotation * Quaternion.Euler(-25f, 0, Random.Range(-15f, 15f));
        currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyAirRotation, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyAirRotation, riseTime).SetEase(Ease.OutQuad));
        delay += riseTime;

        // 5. 最高点停顿
        delay += hangTime;

        // 6. 重力加速下落（恢复到 Grid 基准旋转，落到各自 grid 的 Y 坐标）
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveY(allyStart.y, fallTime).SetEase(Ease.InQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveY(enemyStart.y, fallTime).SetEase(Ease.InQuad));
        currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyOldGridDir.BaseRotation, fallTime * 0.8f).SetEase(Ease.InQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyOldGridDir.FlippedRotation, fallTime * 0.8f).SetEase(Ease.InQuad));
        delay += fallTime;

        // 7. 落地冲击 + 精确位置修正
        currentSequence.InsertCallback(delay, () =>
        {
            ScreenShakeController.Instance?.ShakeOnCollision();
            allyUnit.TriggerLandingDust();
            enemyUnit.TriggerLandingDust();
            allyUnit.transform.localPosition = allyStart;
            enemyUnit.transform.localPosition = enemyStart;
            PlayLandingSound();
        });
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.7f, 0.06f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.7f, 0.06f).SetEase(Ease.OutQuad));
        delay += 0.06f;
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(1f, 0.18f).SetEase(Ease.OutBounce));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(1f, 0.18f).SetEase(Ease.OutBounce));
        delay += 0.18f;

        return delay;
    }

    /// <summary>ATK vs DEF - 攻城战：进攻方猛烈撞击防守方，ATK弹飞，DEF被击退后回位</summary>
    private float AnimateATKvsDEF(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, Vector3 allyTarget, Vector3 enemyTarget, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo enemyOldGridDir)
    {
        if (allyUnit == null || enemyUnit == null) return delay;

        Vector3 allyStart = allyUnit.transform.localPosition;
        Vector3 enemyStart = enemyUnit.transform.localPosition;
        // 碰撞点在敌方位置前方（敌方的后向）
        Vector3 collisionPoint = enemyStart + enemyOldGridDir.Back * 0.5f;

        // 1. DEF准备防御姿态 + ATK蓄力 + 炮火同时进行
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.85f, 0.5f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMove(allyStart + allyOldGridDir.Back * 0.4f, 0.5f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.8f, 0.5f).SetEase(Ease.OutQuad));

        // 炮火在蓄力期间播放
        PlayBattleExplosions(allyUnit, enemyUnit, delay);
        currentSequence.InsertCallback(delay, () => { PlayGunfireSound(); });
        delay += 0.5f;

        // 2. ATK猛烈冲锋：在炮火中冲向DEF
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMove(collisionPoint, 0.1f).SetEase(Ease.InQuart));
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(1f, 0.06f).SetEase(Ease.OutQuad));
        currentSequence.InsertCallback(delay + 0.04f, () => { allyUnit.TriggerCharge(); PlayChargeSound(); });
        delay += 0.1f;

        // 3. 碰撞瞬间
        currentSequence.InsertCallback(delay, () =>
        {
            ScreenShakeController.Instance?.ShakeOnCollision();
            allyUnit.TriggerGunfire();
            enemyUnit.TriggerGunfire();
            allyUnit.TriggerExplosion();
            PlayCollisionSound();
        });

        delay += 0.02f;

        // 4. DEF被击退 + ATK弹飞抛物线
        float flyHeight = 2.2f;
        float knockbackDist = 1.8f;
        float riseTime = 0.18f;
        float hangTime = 0.06f;
        float fallTime = 0.15f;

        // ATK水平弹回（弹回原位）
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveX(allyStart.x, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveZ(allyStart.z, riseTime).SetEase(Ease.OutQuad));

        // ATK垂直上升（减速到最高点），使用 Grid 基准旋转
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveY(flyHeight, riseTime).SetEase(Ease.OutQuad));
        Quaternion allyAirRotation = allyOldGridDir.BaseRotation * Quaternion.Euler(-30f, 0, Random.Range(-20f, 20f));
        currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyAirRotation, riseTime).SetEase(Ease.OutQuad));

        // DEF被击退（沿敌方后向击退）
        Vector3 knockbackTarget = enemyStart + enemyOldGridDir.Back * knockbackDist;
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMove(knockbackTarget, 0.12f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.9f, 0.08f).SetEase(Ease.OutQuad));
        delay += riseTime;

        // 5. 最高点停顿
        delay += hangTime;

        // 6. ATK重力加速下落 + DEF回位（恢复到 Grid 基准旋转，落到 grid 的 Y 坐标）
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveY(allyStart.y, fallTime).SetEase(Ease.InQuad));
        currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyOldGridDir.BaseRotation, fallTime * 0.8f).SetEase(Ease.InQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMove(enemyStart, 0.2f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(1f, 0.15f).SetEase(Ease.OutBack));
        delay += fallTime;

        // 7. ATK落地冲击 + 精确位置修正
        currentSequence.InsertCallback(delay, () =>
        {
            ScreenShakeController.Instance?.ShakeOnCollision();
            allyUnit.TriggerLandingDust();
            allyUnit.transform.localPosition = allyStart;
            PlayLandingSound();
        });
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.7f, 0.06f).SetEase(Ease.OutQuad));
        delay += 0.06f;
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(1f, 0.18f).SetEase(Ease.OutBounce));
        delay += 0.18f;

        return delay;
    }

    /// <summary>ATK vs RET - 追击战：进攻方追击撤退方</summary>
    private float AnimateATKvsRET(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, Vector3 allyTarget, Vector3 enemyTarget, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo allyNewGridDir, GridDirectionInfo enemyOldGridDir, GridDirectionInfo enemyNewGridDir)
    {
        if (allyUnit == null || enemyUnit == null) return delay;

        Vector3 enemyStart = enemyUnit.transform.localPosition;

        // 1. 撤退方转身 + 蓄力准备逃跑
        float turnDuration = 0.25f;
        currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyOldGridDir.BaseRotation, turnDuration).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.85f, turnDuration).SetEase(Ease.OutQuad));

        float retreatStartTime = delay + turnDuration;

        // 播放移动音效
        currentSequence.InsertCallback(retreatStartTime, () => PlayMoveSound());

        // 2. RET 先开始跳跃逃跑：抛物线轨迹
        float retreatRiseTime = 0.22f;
        float retreatFallTime = 0.18f;
        float retreatHeight = 0.85f;
        float maxY = Mathf.Max(enemyStart.y, enemyTarget.y);

        // 弹跳起跳 + 恢复 scale
        currentSequence.Insert(retreatStartTime, enemyUnit.transform.DOScaleY(1.1f, 0.08f).SetEase(Ease.OutQuad));
        currentSequence.Insert(retreatStartTime + 0.08f, enemyUnit.transform.DOScaleY(1f, retreatRiseTime - 0.08f).SetEase(Ease.OutQuad));

        // 上升到最高点（XZ 移动到目标）
        Vector3 retreatPeak = new Vector3(enemyTarget.x, maxY + retreatHeight, enemyTarget.z);
        currentSequence.Insert(retreatStartTime, enemyUnit.transform.DOLocalMove(retreatPeak, retreatRiseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(retreatStartTime, enemyUnit.transform.DORotateQuaternion(enemyNewGridDir.BaseRotation, retreatRiseTime).SetEase(Ease.InOutQuad));

        // 下落到目标位置
        currentSequence.Insert(retreatStartTime + retreatRiseTime, enemyUnit.transform.DOLocalMoveY(enemyTarget.y, retreatFallTime).SetEase(Ease.InQuad));

        float retreatLandTime = retreatStartTime + retreatRiseTime + retreatFallTime;

        // 3. ATK 追击（延迟开始，让撤退方先跑远一点）
        float chaseDelay = retreatStartTime + 0.15f;
        float atkMoveDuration = allyUnit.InsertEnhancedMoveAnimation(currentSequence, chaseDelay, allyTarget, true, false, 0.1f);
        currentSequence.Insert(chaseDelay, allyUnit.transform.DORotateQuaternion(allyNewGridDir.BaseRotation, atkMoveDuration).SetEase(Ease.InOutQuad));
        currentSequence.InsertCallback(chaseDelay + 0.1f, () => allyUnit.TriggerCharge());

        // RET 落地冲击
        currentSequence.InsertCallback(retreatLandTime, () =>
        {
            enemyUnit.TriggerLandingDust();
            enemyUnit.transform.localPosition = enemyTarget;
            PlayLandingSound();
        });
        currentSequence.Insert(retreatLandTime, enemyUnit.transform.DOScaleY(0.8f, 0.06f).SetEase(Ease.OutQuad));
        currentSequence.Insert(retreatLandTime + 0.06f, enemyUnit.transform.DOScaleY(1f, 0.12f).SetEase(Ease.OutBounce));

        delay = Mathf.Max(chaseDelay + atkMoveDuration, retreatLandTime + 0.18f);

        // 4. 撤退方转回面向敌方
        currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyNewGridDir.FlippedRotation, 0.18f).SetEase(Ease.OutBack));

        // 5. ATK 胜利姿态：轻微跳跃
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveY(allyTarget.y + 0.3f, 0.1f).SetEase(Ease.OutQuad));
        delay += 0.1f;
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMoveY(allyTarget.y, 0.1f).SetEase(Ease.InQuad));
        currentSequence.InsertCallback(delay + 0.1f, () =>
        {
            allyUnit.transform.localPosition = allyTarget;
            allyUnit.TriggerLandingDust();
            PlayLandingSound();
        });
        currentSequence.Insert(delay + 0.1f, allyUnit.transform.DOScaleY(0.9f, 0.05f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay + 0.15f, allyUnit.transform.DOScaleY(1f, 0.1f).SetEase(Ease.OutBounce));
        delay += 0.25f;

        return delay;
    }

    /// <summary>DEF vs ATK - 防守反击：敌方猛烈撞击，敌方弹飞，己方被击退后回位</summary>
    private float AnimateDEFvsATK(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, Vector3 allyTarget, Vector3 enemyTarget, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo enemyOldGridDir)
    {
        if (allyUnit == null || enemyUnit == null) return delay;

        Vector3 allyStart = allyUnit.transform.localPosition;
        Vector3 enemyStart = enemyUnit.transform.localPosition;
        // 碰撞点在己方位置前方（己方的前向）
        Vector3 collisionPoint = allyStart + allyOldGridDir.Forward * 0.5f;

        // 1. 己方准备防御姿态 + 敌方蓄力 + 炮火同时进行
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.85f, 0.5f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMove(enemyStart + enemyOldGridDir.Back * 0.4f, 0.5f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.8f, 0.5f).SetEase(Ease.OutQuad));

        // 炮火在蓄力期间播放
        PlayBattleExplosions(allyUnit, enemyUnit, delay);
        currentSequence.InsertCallback(delay, () => { PlayGunfireSound(); });
        delay += 0.5f;

        // 2. 敌方猛烈冲锋：在炮火中冲向DEF
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMove(collisionPoint, 0.1f).SetEase(Ease.InQuart));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(1f, 0.06f).SetEase(Ease.OutQuad));
        currentSequence.InsertCallback(delay + 0.04f, () => { enemyUnit.TriggerCharge(); PlayChargeSound(); });
        delay += 0.1f;

        // 3. 碰撞瞬间
        currentSequence.InsertCallback(delay, () =>
        {
            ScreenShakeController.Instance?.ShakeOnCollision();
            allyUnit.TriggerGunfire();
            enemyUnit.TriggerGunfire();
            enemyUnit.TriggerExplosion();
            PlayCollisionSound();
        });

        delay += 0.02f;

        // 4. 己方被击退 + 敌方弹飞抛物线
        float flyHeight = 2.2f;
        float knockbackDist = 1.8f;
        float riseTime = 0.18f;
        float hangTime = 0.06f;
        float fallTime = 0.15f;

        // 敌方水平弹回（弹回原位）
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveX(enemyStart.x, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveZ(enemyStart.z, riseTime).SetEase(Ease.OutQuad));

        // 敌方垂直上升（减速到最高点），使用 Grid 基准旋转
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveY(flyHeight, riseTime).SetEase(Ease.OutQuad));
        Quaternion enemyAirRotation = enemyOldGridDir.FlippedRotation * Quaternion.Euler(-30f, 0, Random.Range(-20f, 20f));
        currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyAirRotation, riseTime).SetEase(Ease.OutQuad));

        // 己方被击退（沿己方后向击退）
        Vector3 knockbackTarget = allyStart + allyOldGridDir.Back * knockbackDist;
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMove(knockbackTarget, 0.12f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.9f, 0.08f).SetEase(Ease.OutQuad));
        delay += riseTime;

        // 5. 最高点停顿
        delay += hangTime;

        // 6. 敌方重力加速下落 + 己方回位（恢复到 Grid 基准旋转，落到 grid 的 Y 坐标）
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveY(enemyStart.y, fallTime).SetEase(Ease.InQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyOldGridDir.FlippedRotation, fallTime * 0.8f).SetEase(Ease.InQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMove(allyStart, 0.2f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(1f, 0.15f).SetEase(Ease.OutBack));
        delay += fallTime;

        // 7. 敌方落地冲击 + 精确位置修正
        currentSequence.InsertCallback(delay, () =>
        {
            ScreenShakeController.Instance?.ShakeOnCollision();
            enemyUnit.TriggerLandingDust();
            enemyUnit.transform.localPosition = enemyStart;
            PlayLandingSound();
        });
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.7f, 0.06f).SetEase(Ease.OutQuad));
        delay += 0.06f;
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(1f, 0.18f).SetEase(Ease.OutBounce));
        delay += 0.18f;

        return delay;
    }

    /// <summary>DEF vs DEF - 对峙：双方原地摇摆，无接触</summary>
    private float AnimateDEFvsDEF(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo enemyOldGridDir)
    {
        if (allyUnit == null || enemyUnit == null) return delay;

        Vector3 allyStart = allyUnit.transform.localPosition;
        Vector3 enemyStart = enemyUnit.transform.localPosition;

        // 对峙摇摆动画（使用 Grid 前向/后向）
        float swayDuration = 0.15f;
        float swayDistance = 0.12f;

        for (int i = 0; i < 3; i++)
        {
            float t = delay + i * swayDuration * 2;
            // 己方向前摇摆，敌方向前摇摆（敌方的前向是己方视角的后方）
            currentSequence.Insert(t, allyUnit.transform.DOLocalMove(allyStart + allyOldGridDir.Forward * swayDistance, swayDuration).SetEase(Ease.InOutSine));
            currentSequence.Insert(t, enemyUnit.transform.DOLocalMove(enemyStart + enemyOldGridDir.Forward * swayDistance, swayDuration).SetEase(Ease.InOutSine));
            currentSequence.Insert(t + swayDuration, allyUnit.transform.DOLocalMove(allyStart + allyOldGridDir.Back * swayDistance * 0.5f, swayDuration).SetEase(Ease.InOutSine));
            currentSequence.Insert(t + swayDuration, enemyUnit.transform.DOLocalMove(enemyStart + enemyOldGridDir.Back * swayDistance * 0.5f, swayDuration).SetEase(Ease.InOutSine));
        }
        delay += swayDuration * 6;

        // 回到原位
        currentSequence.Insert(delay, allyUnit.transform.DOLocalMove(allyStart, swayDuration).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMove(enemyStart, swayDuration).SetEase(Ease.OutQuad));
        delay += swayDuration + 0.1f;

        return delay;
    }

    /// <summary>DEF vs RET - 安全撤退：DEF警戒，RET有序后撤（带跳跃）</summary>
    private float AnimateDEFvsRET(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, Vector3 enemyTarget, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo enemyOldGridDir, GridDirectionInfo enemyNewGridDir)
    {
        if (enemyUnit == null) return delay;

        Vector3 allyStart = allyUnit?.transform.localPosition ?? Vector3.zero;
        Vector3 enemyStart = enemyUnit.transform.localPosition;

        // 播放移动音效
        currentSequence.InsertCallback(delay, () => PlayMoveSound());

        // 1. DEF 保持警戒：小幅前压 + 压低姿态
        if (allyUnit != null)
        {
            currentSequence.Insert(delay, allyUnit.transform.DOLocalMove(allyStart + allyOldGridDir.Forward * 0.2f, 0.15f).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.9f, 0.15f).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay + 0.3f, allyUnit.transform.DOLocalMove(allyStart, 0.2f).SetEase(Ease.InOutQuad));
            currentSequence.Insert(delay + 0.3f, allyUnit.transform.DOScaleY(1f, 0.2f).SetEase(Ease.OutQuad));
        }

        // 2. RET 转身 + 蓄力
        float turnDuration = 0.22f;
        currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyOldGridDir.BaseRotation, turnDuration).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.85f, turnDuration).SetEase(Ease.OutQuad));

        // 3. RET 跳跃撤退
        float jumpDelay = delay + turnDuration;
        float riseTime = 0.18f;
        float fallTime = 0.15f;
        float jumpHeight = 0.6f;
        float maxY = Mathf.Max(enemyStart.y, enemyTarget.y);

        // 起跳
        currentSequence.Insert(jumpDelay, enemyUnit.transform.DOScaleY(1.05f, 0.06f).SetEase(Ease.OutQuad));
        currentSequence.Insert(jumpDelay + 0.06f, enemyUnit.transform.DOScaleY(1f, riseTime - 0.06f).SetEase(Ease.OutQuad));

        // 上升到最高点
        Vector3 peakPos = new Vector3(enemyTarget.x, maxY + jumpHeight, enemyTarget.z);
        currentSequence.Insert(jumpDelay, enemyUnit.transform.DOLocalMove(peakPos, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(jumpDelay, enemyUnit.transform.DORotateQuaternion(enemyNewGridDir.BaseRotation, riseTime).SetEase(Ease.InOutQuad));

        // 下落
        currentSequence.Insert(jumpDelay + riseTime, enemyUnit.transform.DOLocalMoveY(enemyTarget.y, fallTime).SetEase(Ease.InQuad));

        float landTime = jumpDelay + riseTime + fallTime;

        // 落地冲击
        currentSequence.InsertCallback(landTime, () =>
        {
            enemyUnit.TriggerLandingDust();
            enemyUnit.transform.localPosition = enemyTarget;
            PlayLandingSound();
        });
        currentSequence.Insert(landTime, enemyUnit.transform.DOScaleY(0.85f, 0.05f).SetEase(Ease.OutQuad));
        currentSequence.Insert(landTime + 0.05f, enemyUnit.transform.DOScaleY(1f, 0.1f).SetEase(Ease.OutBounce));

        // 4. 转回面向敌方
        currentSequence.Insert(landTime + 0.15f, enemyUnit.transform.DORotateQuaternion(enemyNewGridDir.FlippedRotation, 0.1f).SetEase(Ease.OutBack));

        // 触发恢复特效
        currentSequence.InsertCallback(landTime + 0.1f, () => enemyUnit.TriggerHeal());

        delay = landTime + 0.3f;
        return delay;
    }

    /// <summary>RET vs ATK - 被追击：己方狼狈撤退（带慌张跳跃）</summary>
    private float AnimateRETvsATK(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, Vector3 allyTarget, Vector3 enemyTarget, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo allyNewGridDir, GridDirectionInfo enemyOldGridDir, GridDirectionInfo enemyNewGridDir)
    {
        if (allyUnit == null || enemyUnit == null) return delay;

        Vector3 allyStart = allyUnit.transform.localPosition;

        // 1. 己方慌张转身 + 蓄力准备逃跑
        float turnDuration = 0.25f;
        currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyOldGridDir.FlippedRotation, turnDuration).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.85f, turnDuration).SetEase(Ease.OutQuad));
        // 慌张的小幅摇晃
        currentSequence.Insert(delay, allyUnit.transform.DOShakeRotation(turnDuration, new Vector3(0, 5f, 3f), 8));

        float retreatStartTime = delay + turnDuration;

        // 播放移动音效
        currentSequence.InsertCallback(retreatStartTime, () => PlayMoveSound());

        // 2. 己方先开始狼狈跳跃逃跑（快速跳跃）
        float riseTime = 0.22f;
        float fallTime = 0.18f;
        float jumpHeight = 0.8f;
        float maxY = Mathf.Max(allyStart.y, allyTarget.y);

        // 慌张起跳
        currentSequence.Insert(retreatStartTime, allyUnit.transform.DOScaleY(1.1f, 0.05f).SetEase(Ease.OutQuad));
        currentSequence.Insert(retreatStartTime + 0.05f, allyUnit.transform.DOScaleY(1f, riseTime - 0.05f).SetEase(Ease.OutQuad));

        // 上升到最高点（带摇晃）
        Vector3 peakPos = new Vector3(allyTarget.x, maxY + jumpHeight, allyTarget.z);
        currentSequence.Insert(retreatStartTime, allyUnit.transform.DOLocalMove(peakPos, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(retreatStartTime, allyUnit.transform.DORotateQuaternion(allyNewGridDir.FlippedRotation, riseTime).SetEase(Ease.InOutQuad));
        currentSequence.Insert(retreatStartTime, allyUnit.transform.DOShakeRotation(riseTime + fallTime, new Vector3(0, 10f, 8f), 6));

        // 下落
        currentSequence.Insert(retreatStartTime + riseTime, allyUnit.transform.DOLocalMoveY(allyTarget.y, fallTime).SetEase(Ease.InQuad));

        float allyLandTime = retreatStartTime + riseTime + fallTime;

        // 3. 敌方追击（延迟开始，让撤退方先跑远一点）
        float chaseDelay = retreatStartTime + 0.15f;  // 撤退方起跳后才开始追
        float enemyMoveDuration = enemyUnit.InsertEnhancedMoveAnimation(currentSequence, chaseDelay, enemyTarget, true, false, 0.1f);
        currentSequence.Insert(chaseDelay, enemyUnit.transform.DORotateQuaternion(enemyNewGridDir.FlippedRotation, enemyMoveDuration).SetEase(Ease.InOutQuad));
        currentSequence.InsertCallback(chaseDelay + 0.08f, () => enemyUnit.TriggerCharge());

        // 己方落地（狼狈落地，压得更低）
        currentSequence.InsertCallback(allyLandTime, () =>
        {
            allyUnit.TriggerLandingDust();
            allyUnit.transform.localPosition = allyTarget;
            PlayLandingSound();
        });
        currentSequence.Insert(allyLandTime, allyUnit.transform.DOScaleY(0.75f, 0.06f).SetEase(Ease.OutQuad));
        currentSequence.Insert(allyLandTime + 0.06f, allyUnit.transform.DOScaleY(1f, 0.15f).SetEase(Ease.OutBounce));

        delay = Mathf.Max(chaseDelay + enemyMoveDuration, allyLandTime + 0.2f);

        // 4. 己方转回面向敌方（喘息）
        currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyNewGridDir.BaseRotation, 0.18f).SetEase(Ease.OutBack));

        // 5. 敌方胜利姿态
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveY(enemyTarget.y + 0.3f, 0.1f).SetEase(Ease.OutQuad));
        delay += 0.1f;
        currentSequence.Insert(delay, enemyUnit.transform.DOLocalMoveY(enemyTarget.y, 0.1f).SetEase(Ease.InQuad));
        currentSequence.InsertCallback(delay + 0.1f, () =>
        {
            enemyUnit.transform.localPosition = enemyTarget;
            enemyUnit.TriggerLandingDust();
            PlayLandingSound();
        });
        currentSequence.Insert(delay + 0.1f, enemyUnit.transform.DOScaleY(0.9f, 0.05f).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay + 0.15f, enemyUnit.transform.DOScaleY(1f, 0.1f).SetEase(Ease.OutBounce));
        delay += 0.25f;

        return delay;
    }

    /// <summary>RET vs DEF - 从容撤退：己方优雅后撤（带轻盈跳跃）</summary>
    private float AnimateRETvsDEF(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, Vector3 allyTarget, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo allyNewGridDir, GridDirectionInfo enemyOldGridDir)
    {
        if (allyUnit == null) return delay;

        Vector3 allyStart = allyUnit.transform.localPosition;
        Vector3 enemyStart = enemyUnit?.transform.localPosition ?? Vector3.zero;

        // 播放移动音效
        currentSequence.InsertCallback(delay, () => PlayMoveSound());

        // 1. 己方优雅转身 + 轻微蓄力
        float turnDuration = 0.22f;
        currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyOldGridDir.FlippedRotation, turnDuration).SetEase(Ease.OutQuad));
        currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.9f, turnDuration).SetEase(Ease.OutQuad));

        // 2. 敌方原地观望（前压姿态）
        if (enemyUnit != null)
        {
            currentSequence.Insert(delay, enemyUnit.transform.DOLocalMove(enemyStart + enemyOldGridDir.Forward * 0.15f, 0.2f).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.95f, 0.2f).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay + 0.35f, enemyUnit.transform.DOLocalMove(enemyStart, 0.2f).SetEase(Ease.InOutQuad));
            currentSequence.Insert(delay + 0.35f, enemyUnit.transform.DOScaleY(1f, 0.2f).SetEase(Ease.OutQuad));
        }

        // 3. 己方轻盈跳跃后撤
        float jumpDelay = delay + turnDuration;
        float riseTime = 0.2f;
        float fallTime = 0.15f;
        float jumpHeight = 0.5f;
        float maxY = Mathf.Max(allyStart.y, allyTarget.y);

        // 轻盈起跳
        currentSequence.Insert(jumpDelay, allyUnit.transform.DOScaleY(1.05f, 0.06f).SetEase(Ease.OutQuad));
        currentSequence.Insert(jumpDelay + 0.06f, allyUnit.transform.DOScaleY(1f, riseTime - 0.06f).SetEase(Ease.OutQuad));

        // 优雅的抛物线
        Vector3 peakPos = new Vector3(allyTarget.x, maxY + jumpHeight, allyTarget.z);
        currentSequence.Insert(jumpDelay, allyUnit.transform.DOLocalMove(peakPos, riseTime).SetEase(Ease.OutQuad));
        currentSequence.Insert(jumpDelay, allyUnit.transform.DORotateQuaternion(allyNewGridDir.FlippedRotation, riseTime).SetEase(Ease.InOutQuad));

        // 下落
        currentSequence.Insert(jumpDelay + riseTime, allyUnit.transform.DOLocalMoveY(allyTarget.y, fallTime).SetEase(Ease.InQuad));

        float landTime = jumpDelay + riseTime + fallTime;

        // 优雅落地
        currentSequence.InsertCallback(landTime, () =>
        {
            allyUnit.TriggerLandingDust();
            allyUnit.transform.localPosition = allyTarget;
            PlayLandingSound();
        });
        currentSequence.Insert(landTime, allyUnit.transform.DOScaleY(0.88f, 0.05f).SetEase(Ease.OutQuad));
        currentSequence.Insert(landTime + 0.05f, allyUnit.transform.DOScaleY(1f, 0.1f).SetEase(Ease.OutBounce));

        // 4. 转回面向敌方 + 恢复特效
        currentSequence.Insert(landTime + 0.15f, allyUnit.transform.DORotateQuaternion(allyNewGridDir.BaseRotation, 0.1f).SetEase(Ease.OutBack));
        currentSequence.InsertCallback(landTime + 0.1f, () => allyUnit.TriggerHeal());

        delay = landTime + 0.3f;
        return delay;
    }

    /// <summary>RET vs RET - 双方脱离：同时跳跃后撤</summary>
    private float AnimateRETvsRET(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, Vector3 allyTarget, Vector3 enemyTarget, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo allyNewGridDir, GridDirectionInfo enemyOldGridDir, GridDirectionInfo enemyNewGridDir)
    {
        Vector3 allyStart = allyUnit?.transform.localPosition ?? Vector3.zero;
        Vector3 enemyStart = enemyUnit?.transform.localPosition ?? Vector3.zero;

        // 1. 双方同时转身 + 蓄力
        float turnDuration = 0.22f;
        if (allyUnit != null)
        {
            currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyOldGridDir.FlippedRotation, turnDuration).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay, allyUnit.transform.DOScaleY(0.88f, turnDuration).SetEase(Ease.OutQuad));
        }
        if (enemyUnit != null)
        {
            currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyOldGridDir.BaseRotation, turnDuration).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(0.88f, turnDuration).SetEase(Ease.OutQuad));
        }
        delay += turnDuration;

        // 播放移动音效
        currentSequence.InsertCallback(delay, () => PlayMoveSound());

        // 2. 双方同时跳跃后撤
        float riseTime = 0.2f;
        float fallTime = 0.15f;
        float jumpHeight = 0.55f;

        // 己方跳跃
        if (allyUnit != null)
        {
            float allyMaxY = Mathf.Max(allyStart.y, allyTarget.y);

            // 起跳
            currentSequence.Insert(delay, allyUnit.transform.DOScaleY(1.05f, 0.05f).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay + 0.05f, allyUnit.transform.DOScaleY(1f, riseTime - 0.05f).SetEase(Ease.OutQuad));

            // 抛物线
            Vector3 allyPeak = new Vector3(allyTarget.x, allyMaxY + jumpHeight, allyTarget.z);
            currentSequence.Insert(delay, allyUnit.transform.DOLocalMove(allyPeak, riseTime).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyNewGridDir.FlippedRotation, riseTime).SetEase(Ease.InOutQuad));

            // 下落
            currentSequence.Insert(delay + riseTime, allyUnit.transform.DOLocalMoveY(allyTarget.y, fallTime).SetEase(Ease.InQuad));
        }

        // 敌方跳跃
        if (enemyUnit != null)
        {
            float enemyMaxY = Mathf.Max(enemyStart.y, enemyTarget.y);

            // 起跳
            currentSequence.Insert(delay, enemyUnit.transform.DOScaleY(1.05f, 0.05f).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay + 0.05f, enemyUnit.transform.DOScaleY(1f, riseTime - 0.05f).SetEase(Ease.OutQuad));

            // 抛物线
            Vector3 enemyPeak = new Vector3(enemyTarget.x, enemyMaxY + jumpHeight, enemyTarget.z);
            currentSequence.Insert(delay, enemyUnit.transform.DOLocalMove(enemyPeak, riseTime).SetEase(Ease.OutQuad));
            currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyNewGridDir.BaseRotation, riseTime).SetEase(Ease.InOutQuad));

            // 下落
            currentSequence.Insert(delay + riseTime, enemyUnit.transform.DOLocalMoveY(enemyTarget.y, fallTime).SetEase(Ease.InQuad));
        }

        float landTime = delay + riseTime + fallTime;

        // 3. 双方落地冲击
        currentSequence.InsertCallback(landTime, () =>
        {
            allyUnit?.TriggerLandingDust();
            enemyUnit?.TriggerLandingDust();
            if (allyUnit != null) allyUnit.transform.localPosition = allyTarget;
            if (enemyUnit != null) enemyUnit.transform.localPosition = enemyTarget;
            PlayLandingSound();
        });

        if (allyUnit != null)
        {
            currentSequence.Insert(landTime, allyUnit.transform.DOScaleY(0.85f, 0.05f).SetEase(Ease.OutQuad));
            currentSequence.Insert(landTime + 0.05f, allyUnit.transform.DOScaleY(1f, 0.1f).SetEase(Ease.OutBounce));
        }
        if (enemyUnit != null)
        {
            currentSequence.Insert(landTime, enemyUnit.transform.DOScaleY(0.85f, 0.05f).SetEase(Ease.OutQuad));
            currentSequence.Insert(landTime + 0.05f, enemyUnit.transform.DOScaleY(1f, 0.1f).SetEase(Ease.OutBounce));
        }

        // 4. 双方转回面向敌方 + 恢复特效
        float turnBackTime = landTime + 0.15f;
        if (allyUnit != null)
            currentSequence.Insert(turnBackTime, allyUnit.transform.DORotateQuaternion(allyNewGridDir.BaseRotation, 0.1f).SetEase(Ease.OutBack));
        if (enemyUnit != null)
            currentSequence.Insert(turnBackTime, enemyUnit.transform.DORotateQuaternion(enemyNewGridDir.FlippedRotation, 0.1f).SetEase(Ease.OutBack));

        currentSequence.InsertCallback(landTime + 0.1f, () =>
        {
            allyUnit?.TriggerHeal();
            enemyUnit?.TriggerHeal();
        });

        delay = landTime + 0.3f;
        return delay;
    }

    /// <summary>简单移动动画（无战斗接触时使用）</summary>
    private float AnimateSimpleMove(GeneralUnit3D allyUnit, GeneralUnit3D enemyUnit, Vector3 allyTarget, Vector3 enemyTarget, float delay, BattleResult result, GridDirectionInfo allyOldGridDir, GridDirectionInfo allyNewGridDir, GridDirectionInfo enemyOldGridDir, GridDirectionInfo enemyNewGridDir)
    {
        bool allyMoved = result.AllyOldPosition != result.AllyNewPosition;
        bool enemyMoved = result.EnemyOldPosition != result.EnemyNewPosition;

        if (allyMoved || enemyMoved)
        {
            // 播放移动音效（只播放一次）
            currentSequence.InsertCallback(delay, () => PlayMoveSound());

            float maxDuration = 0f;
            if (allyUnit != null && allyMoved)
            {
                float duration = allyUnit.InsertEnhancedMoveAnimation(currentSequence, delay, allyTarget, true, true);
                maxDuration = Mathf.Max(maxDuration, duration);
                // 移动过程中旋转补间（从旧 Grid 旋转到新 Grid 旋转）
                currentSequence.Insert(delay, allyUnit.transform.DORotateQuaternion(allyNewGridDir.BaseRotation, duration).SetEase(Ease.InOutQuad));
            }
            if (enemyUnit != null && enemyMoved)
            {
                float duration = enemyUnit.InsertEnhancedMoveAnimation(currentSequence, delay, enemyTarget, true, true);
                maxDuration = Mathf.Max(maxDuration, duration);
                // 移动过程中旋转补间（敌方使用 FlippedRotation）
                currentSequence.Insert(delay, enemyUnit.transform.DORotateQuaternion(enemyNewGridDir.FlippedRotation, duration).SetEase(Ease.InOutQuad));
            }

            // 在落地时播放落地音效（落地时间约为动画结束前0.15秒）
            float landingTime = delay + Mathf.Max(0, maxDuration - 0.15f);
            currentSequence.InsertCallback(landingTime, () => PlayLandingSound());

            delay += maxDuration > 0 ? maxDuration : 0.8f;
        }

        return delay;
    }

    #endregion
}
