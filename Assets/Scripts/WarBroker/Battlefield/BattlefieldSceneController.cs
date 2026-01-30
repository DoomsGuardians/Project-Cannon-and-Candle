using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private float unitSpacing = 2f;  // 单位间距
    [SerializeField] private float laneLength = 10f;  // 战线长度

    [Header("相机")]
    [SerializeField] private BattlefieldCamera battlefieldCamera;

    private Dictionary<string, GeneralUnit3D> allyUnits = new Dictionary<string, GeneralUnit3D>();
    private Dictionary<string, GeneralUnit3D> enemyUnits = new Dictionary<string, GeneralUnit3D>();

    private BattleData battleData;
    private GeneralUnit3D selectedUnit;

    private EventService eventService;
    private UIService uiService;

    public System.Action<GeneralData> OnGeneralSelected;

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

        // 根据 GridPosition (1-5) 计算位置
        // 己方从左侧开始，敌方从右侧开始
        float normalizedPos = (general.GridPosition - 1) / 4f;  // 0-1
        float xOffset = isAlly
            ? Mathf.Lerp(-laneLength / 2f, 0, normalizedPos)
            : Mathf.Lerp(0, laneLength / 2f, normalizedPos);

        // 己方和敌方在 Z 轴上有偏移
        float zOffset = isAlly ? -unitSpacing / 2f : unitSpacing / 2f;

        unit.transform.localPosition = new Vector3(xOffset, 0, zOffset);

        // 面向对方
        unit.transform.localRotation = Quaternion.Euler(0, isAlly ? 90 : -90, 0);
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

        // 相机聚焦
        if (battlefieldCamera != null)
        {
            battlefieldCamera.FocusOn(unit.transform.position);
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
        UpdateAllUnits();
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
