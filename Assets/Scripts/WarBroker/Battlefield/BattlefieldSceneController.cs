using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 3D 战场场景控制器
/// 管理三条战线和将军单位的显示
/// </summary>
public class BattlefieldSceneController : MonoBehaviour
{
    [Header("战线锚点")]
    [SerializeField] private Transform leftLaneAnchor;
    [SerializeField] private Transform centerLaneAnchor;
    [SerializeField] private Transform rightLaneAnchor;

    [Header("单位配置")]
    [SerializeField] private GameObject generalUnitPrefab;
    [SerializeField] private float gridGap = 4f;  // Grid间距（Z轴方向）

    [Header("动画配置")]
    [SerializeField] private float animationDelay = 0.3f;  // 动画间隔
    [SerializeField] private float actionPauseDuration = 0.6f;  // 每组行动后的停顿时间

    [Header("相机")]
    [SerializeField] private BattlefieldCameraController battlefieldCamera;

    private Dictionary<string, GeneralUnit3D> allyUnits = new Dictionary<string, GeneralUnit3D>();
    private Dictionary<string, GeneralUnit3D> enemyUnits = new Dictionary<string, GeneralUnit3D>();

    private BattleData battleData;
    private GeneralUnit3D selectedUnit;

    private EventService eventService;
    private UIService uiService;

    // 动画队列
    private Queue<BattleResult> animationQueue = new Queue<BattleResult>();
    private bool isPlayingAnimation = false;
    private Sequence currentSequence;

    public System.Action<GeneralData> OnGeneralSelected;
    public System.Action OnAllAnimationsComplete;  // 所有动画播放完成回调

    private void Awake()
    {
        if (GameRoot.Instance != null)
        {
            eventService = GameRoot.Instance.eventService;
            uiService = GameRoot.Instance.uIService;
        }
    }

    private void Start()
    {
        if (eventService != null)
        {
            eventService.AddEventListening((EventID)WarBrokerEventID.OnBattleResult, OnBattleResult);
            eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnTurnEnd);
            eventService.AddEventListening((EventID)WarBrokerEventID.OnIntentChanged, OnIntentChanged);
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

    /// <summary>初始化战场</summary>
    public void Initialize(BattleData data)
    {
        battleData = data;

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
        Transform anchor = GetLaneAnchor(general.Position);
        if (anchor == null) return null;

        var go = Instantiate(generalUnitPrefab, anchor);
        go.name = $"{(isAlly ? "Ally" : "Enemy")}_{general.Name}";
        var unit = go.GetComponent<GeneralUnit3D>();

        if (unit != null)
        {
            unit.Initialize(general, isAlly);
            unit.OnClicked = OnGeneralClicked;

            // 设置位置
            UpdateUnitPosition(unit, general, isAlly);
        }

        return unit;
    }

    private void UpdateUnitPosition(GeneralUnit3D unit, GeneralData general, bool isAlly)
    {
        if (unit == null) return;

        // Grid沿Z轴排列，Grid 1-5 对应 Z: -8, -4, 0, +4, +8
        // 己方初始Grid1在Z=-8（己方大本营）
        // 敌方初始Grid5在Z=+8（敌方大本营）
        // 双方都用同样的公式，GridPosition直接映射到Z坐标
        float gridZ = (general.GridPosition - 3) * gridGap;

        unit.transform.localPosition = new Vector3(0, 0, gridZ);

        // 面向对方（沿Z轴对峙）
        unit.transform.localRotation = Quaternion.Euler(0, isAlly ? 0 : 180, 0);
    }

    private Transform GetLaneAnchor(FrontlinePosition position)
    {
        return position switch
        {
            FrontlinePosition.Left => leftLaneAnchor,
            FrontlinePosition.Center => centerLaneAnchor,
            FrontlinePosition.Right => rightLaneAnchor,
            _ => centerLaneAnchor
        };
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

        // 取消之前的选中
        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
        }

        // 选中新单位
        selectedUnit = unit;
        selectedUnit.SetSelected(true);

        // 只有己方将军可以打开详情面板
        if (unit.IsAlly && unit.Data != null)
        {
            OnGeneralSelected?.Invoke(unit.Data);

            // 打开将军详情面板
            var detailPanel = uiService?.ShowWindow<GeneralDetailPanel>("GeneralDetailPanel");
            if (detailPanel != null)
            {
                detailPanel.SetGeneral(unit.Data);
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
            OnAllAnimationsComplete?.Invoke();

            // 平滑返回默认视角（保持用户之前的旋转角度）
            if (battlefieldCamera != null)
            {
                battlefieldCamera.SmoothReturnToDefault();
            }

            // 发送事件通知战斗动画播放完成
            eventService?.SendMessage((EventID)WarBrokerEventID.OnBattleAnimationsComplete, null, null);
            return;
        }

        isPlayingAnimation = true;
        var result = animationQueue.Dequeue();
        PlayBattleAnimation(result);
    }

    /// <summary>播放战斗动画</summary>
    private void PlayBattleAnimation(BattleResult result)
    {
        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        // 通过Position查找参战单位
        GeneralUnit3D allyUnit = FindUnitByPosition(allyUnits, result.Position);
        GeneralUnit3D enemyUnit = FindUnitByPosition(enemyUnits, result.Position);

        // 相机跟随到当前战线
        Transform laneAnchor = GetLaneAnchor(result.Position);
        Tweener cameraTweener = null;
        if (battlefieldCamera != null && laneAnchor != null)
        {
            cameraTweener = battlefieldCamera.SmoothFollowBattle(laneAnchor);
        }

        Debug.Log($"[BattleAnim] {result.Position}: Ally {result.AllyOldPosition}->{result.AllyNewPosition}, Enemy {result.EnemyOldPosition}->{result.EnemyNewPosition}");

        // 等待相机聚焦完成后再开始战斗动画
        float cameraDelay = cameraTweener != null ? cameraTweener.Duration() : 0f;

        float delay = cameraDelay;

        // 1. 移动动画（使用新的位置信息）
        bool allyMoved = result.AllyOldPosition != result.AllyNewPosition;
        bool enemyMoved = result.EnemyOldPosition != result.EnemyNewPosition;

        if (allyMoved || enemyMoved)
        {
            if (allyUnit != null && allyMoved)
            {
                // 使用本地坐标，与 UpdateUnitPosition 一致
                float targetZ = (result.AllyNewPosition - 3) * gridGap;
                Vector3 newLocalPos = new Vector3(0, 0, targetZ);
                Debug.Log($"[BattleAnim] Ally moving to localPos: {newLocalPos}, current: {allyUnit.transform.localPosition}");
                currentSequence.Insert(delay, allyUnit.PlayMoveAnimation(newLocalPos));
            }
            if (enemyUnit != null && enemyMoved)
            {
                float targetZ = (result.EnemyNewPosition - 3) * gridGap;
                Vector3 newLocalPos = new Vector3(0, 0, targetZ);
                Debug.Log($"[BattleAnim] Enemy moving to localPos: {newLocalPos}, current: {enemyUnit.transform.localPosition}");
                currentSequence.Insert(delay, enemyUnit.PlayMoveAnimation(newLocalPos));
            }
            delay += 0.6f;
        }

        // 2. 受击震动
        if (allyUnit != null && result.AllyTroopChange < 0)
        {
            currentSequence.Insert(delay, allyUnit.PlayHitShake());
        }
        if (enemyUnit != null && result.EnemyTroopChange < 0)
        {
            currentSequence.Insert(delay, enemyUnit.PlayHitShake());
        }
        if (result.AllyTroopChange < 0 || result.EnemyTroopChange < 0)
        {
            delay += 0.3f;
        }

        // 3. 锡兵倒下动画
        if (allyUnit != null && result.AllyTroopChange < 0)
        {
            var fallAnim = allyUnit.PlaySoldierFallAnimation(-result.AllyTroopChange);
            if (fallAnim != null)
                currentSequence.Insert(delay, fallAnim);
        }
        if (enemyUnit != null && result.EnemyTroopChange < 0)
        {
            var fallAnim = enemyUnit.PlaySoldierFallAnimation(-result.EnemyTroopChange);
            if (fallAnim != null)
                currentSequence.Insert(delay, fallAnim);
        }
        if (result.AllyTroopChange < 0 || result.EnemyTroopChange < 0)
        {
            delay += 0.5f;
        }

        // 4. 锡兵增加动画（恢复兵力）
        if (allyUnit != null && result.AllyTroopChange > 0)
        {
            currentSequence.InsertCallback(delay, () => allyUnit.UpdateDisplay());
        }
        if (enemyUnit != null && result.EnemyTroopChange > 0)
        {
            currentSequence.InsertCallback(delay, () => enemyUnit.UpdateDisplay());
        }

        // 添加动画间隔
        currentSequence.AppendInterval(animationDelay);

        // 添加行动后停顿，让玩家看清结果
        currentSequence.AppendInterval(actionPauseDuration);

        // 动画完成后播放下一个
        currentSequence.OnComplete(() =>
        {
            // 更新单位显示
            if (allyUnit != null) allyUnit.UpdateDisplay();
            if (enemyUnit != null) enemyUnit.UpdateDisplay();

            // 播放下一个动画
            PlayNextAnimation();
        });
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
        // 更新所有己方单位的意图气泡
        foreach (var kvp in allyUnits)
        {
            kvp.Value?.UpdateIntentBubble();
        }
    }

    #endregion
}
