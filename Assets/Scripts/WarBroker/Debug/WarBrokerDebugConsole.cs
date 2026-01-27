using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// WarBroker 运行时调试面板 (F12 切换)
/// </summary>
public class WarBrokerDebugConsole : MonoBehaviour
{
    private bool showPanel = false;
    private int selectedTab = 0;
    private readonly string[] tabs = { "Market", "Battle", "Campaign", "Actions", "Log" };

    private Vector2 scrollPos;
    private readonly List<string> eventLog = new List<string>();
    private const int MaxLogEntries = 30;

    // Actions tab
    private float setCashValue = 500f;
    private int setInventoryValue = 5;

    private void Start()
    {
        RegisterEventListeners();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            showPanel = !showPanel;
        }
    }

    private void OnGUI()
    {
        if (!showPanel) return;

        var campaignSystem = GameRoot.Instance?.campaignSystem;
        if (campaignSystem?.Data == null)
        {
            GUI.Box(new Rect(10, 10, 300, 50), "WarBroker Debug: No active campaign");
            return;
        }

        GUI.Box(new Rect(10, 10, 600, 500), "");
        GUILayout.BeginArea(new Rect(15, 15, 590, 490));

        GUILayout.Label("=== WarBroker Debug Console ===", GUI.skin.box);
        selectedTab = GUILayout.Toolbar(selectedTab, tabs);

        scrollPos = GUILayout.BeginScrollView(scrollPos);

        switch (selectedTab)
        {
            case 0: DrawMarketTab(campaignSystem.Data); break;
            case 1: DrawBattleTab(campaignSystem.Data); break;
            case 2: DrawCampaignTab(campaignSystem.Data); break;
            case 3: DrawActionsTab(campaignSystem.Data); break;
            case 4: DrawLogTab(); break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawMarketTab(CampaignRuntimeData data)
    {
        GUILayout.Label("--- 市场价格 ---");
        foreach (var kvp in data.Market.CurrentPrices)
        {
            GUILayout.Label($"  {kvp.Key}: {kvp.Value:F2} (库存: {data.Market.MarketInventory[kvp.Key]})");
        }

        GUILayout.Label("--- 玩家 ---");
        GUILayout.Label($"  现金: {data.Player.Cash:F2}");
        GUILayout.Label($"  负债: {data.Player.BankDebt:F2}");
        GUILayout.Label($"  净资产: {data.Player.CalculateNetWorth(data.Market):F2}");

        GUILayout.Label("--- 玩家库存 ---");
        foreach (var kvp in data.Player.Inventory)
        {
            GUILayout.Label($"  {kvp.Key}: {kvp.Value}");
        }

        GUILayout.Label($"--- 期货合约 ({data.Player.FuturesPositions.Count}) ---");
        foreach (var c in data.Player.FuturesPositions)
        {
            float pnl = c.CalculatePnL(data.Market.CurrentPrices[c.TargetOrder]);
            GUILayout.Label($"  #{c.ContractId} {c.Direction} {c.TargetOrder} x{c.Quantity} | 开仓:{c.OpenPrice:F2} PnL:{pnl:F2} | 到期回合:{c.ExpirationTurn}");
        }
    }

    private void DrawBattleTab(CampaignRuntimeData data)
    {
        var balanceConfig = GameRoot.Instance.resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);

        GUILayout.Label("--- 战线 ---");
        foreach (var kvp in data.Battle.Frontlines)
        {
            string bar = new string('>', kvp.Value.LinePosition) + new string('.', 5 - kvp.Value.LinePosition);
            GUILayout.Label($"  {kvp.Key}: [{bar}] pos={kvp.Value.LinePosition} stag={kvp.Value.StagnantTurns}");
        }

        GUILayout.Label("--- 己方将军 ---");
        foreach (var g in data.Battle.AllyGenerals)
        {
            var status = g.GetStatus(balanceConfig);
            GUILayout.Label($"  [{g.Position}] {g.Name} ({g.Personality})");
            GUILayout.Label($"    兵力:{g.Troops} 信任:{g.Trust} 士气:{g.Morale} 状态:{status}");
            GUILayout.Label($"    指令:{g.AssignedOrder?.ToString() ?? "无"} 重整:{g.ReorganizeTurns}");
        }

        GUILayout.Label("--- 敌方将军 ---");
        foreach (var g in data.Battle.EnemyGenerals)
        {
            var status = g.GetStatus(balanceConfig);
            GUILayout.Label($"  [{g.Position}] {g.Name} ({g.Personality})");
            GUILayout.Label($"    兵力:{g.Troops} 信任:{g.Trust} 士气:{g.Morale} 状态:{status}");
        }
    }

    private void DrawCampaignTab(CampaignRuntimeData data)
    {
        GUILayout.Label($"战役: {data.Config.CampaignName}");
        GUILayout.Label($"回合: {data.CurrentTurn} / {data.MaxTurns}");
        GUILayout.Label($"阶段: {data.CurrentPhase}");
        GUILayout.Label($"审计值: {data.Player.AuditValue}");
        GUILayout.Label($"净资产: {data.Player.CalculateNetWorth(data.Market):F2}");

        if (data.ActiveEvent != null)
        {
            GUILayout.Label($"活跃事件: {data.ActiveEvent.EventName} (剩余{data.EventRemainingTurns}回合)");
        }

        GUILayout.Label($"--- 历史记录 ({data.TurnHistory.Count} 回合) ---");
        for (int i = data.TurnHistory.Count - 1; i >= Mathf.Max(0, data.TurnHistory.Count - 5); i--)
        {
            var r = data.TurnHistory[i];
            GUILayout.Label($"  回合{r.TurnNumber}: 净资产={r.PlayerNetWorth:F2}");
        }
    }

    private void DrawActionsTab(CampaignRuntimeData data)
    {
        GUILayout.Label("--- 快捷操作 ---");

        if (GUILayout.Button("强制结束回合"))
        {
            GameRoot.Instance.campaignSystem.EndTurn();
        }

        GUILayout.Space(10);
        GUILayout.Label("设置现金:");
        setCashValue = float.Parse(GUILayout.TextField(setCashValue.ToString("F0")));
        if (GUILayout.Button($"设置现金为 {setCashValue}"))
        {
            data.Player.Cash = setCashValue;
        }

        GUILayout.Space(10);
        GUILayout.Label("设置库存 (所有类型):");
        setInventoryValue = int.Parse(GUILayout.TextField(setInventoryValue.ToString()));
        if (GUILayout.Button($"设置所有库存为 {setInventoryValue}"))
        {
            data.Player.Inventory[OrderType.ATK] = setInventoryValue;
            data.Player.Inventory[OrderType.DEF] = setInventoryValue;
            data.Player.Inventory[OrderType.RET] = setInventoryValue;
        }

        GUILayout.Space(10);
        if (GUILayout.Button("所有己方将军满状态"))
        {
            foreach (var g in data.Battle.AllyGenerals)
            {
                g.Troops = 100;
                g.Trust = 100;
                g.Morale = 100;
                g.ReorganizeTurns = 0;
            }
        }

        if (GUILayout.Button("所有战线推至5 (胜利)"))
        {
            foreach (var f in data.Battle.Frontlines.Values)
            {
                f.LinePosition = 5;
            }
        }

        if (GUILayout.Button("所有战线退至1 (失败)"))
        {
            foreach (var f in data.Battle.Frontlines.Values)
            {
                f.LinePosition = 1;
            }
        }
    }

    private void DrawLogTab()
    {
        GUILayout.Label($"--- 事件日志 ({eventLog.Count}) ---");
        for (int i = eventLog.Count - 1; i >= 0; i--)
        {
            GUILayout.Label(eventLog[i]);
        }
    }

    private void AddLog(string msg)
    {
        eventLog.Add($"[{Time.frameCount}] {msg}");
        if (eventLog.Count > MaxLogEntries)
            eventLog.RemoveAt(0);
    }

    private void RegisterEventListeners()
    {
        var es = GameRoot.Instance?.eventService;
        if (es == null) return;

        es.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, (p1, p2) => AddLog($"回合开始: {p1}"));
        es.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, (p1, p2) => AddLog($"回合结束: {p1}"));
        es.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, (p1, p2) =>
        {
            var t = p1 as TransactionRecord;
            if (t != null) AddLog($"交易: {t.Type} {t.OrderType} x{t.Quantity} = {t.TotalAmount:F2}");
        });
        es.AddEventListening((EventID)WarBrokerEventID.OnBattleResult, (p1, p2) =>
        {
            var r = p1 as BattleResult;
            if (r != null) AddLog($"战斗: {r.Position} {r.AllyOrder}vs{r.EnemyOrder} 移动:{r.LineMovement}");
        });
        es.AddEventListening((EventID)WarBrokerEventID.OnGeneralRouted, (p1, p2) =>
        {
            var g = p1 as GeneralData;
            if (g != null) AddLog($"溃败: {g.Name}");
        });
        es.AddEventListening((EventID)WarBrokerEventID.OnSkillTriggered, (p1, p2) =>
        {
            var g = p1 as GeneralData;
            var s = p2 as SkillConfigItem;
            if (g != null && s != null) AddLog($"技能: {g.Name} -> {s.SkillName}");
        });
        es.AddEventListening((EventID)WarBrokerEventID.OnRandomEvent, (p1, p2) =>
        {
            var e = p1 as RandomEventConfig;
            if (e != null) AddLog($"事件: {e.EventName}");
        });
        es.AddEventListening((EventID)WarBrokerEventID.OnGameEnd, (p1, p2) => AddLog($"游戏结束: {((bool)p1 ? "胜利" : "失败")}"));
        es.AddEventListening((EventID)WarBrokerEventID.OnPriceUpdate, (p1, p2) => AddLog("市场价格更新"));
        es.AddEventListening((EventID)WarBrokerEventID.OnForceLiquidation, (p1, p2) => AddLog($"强平: 合约#{p1} PnL:{p2}"));
    }
}
