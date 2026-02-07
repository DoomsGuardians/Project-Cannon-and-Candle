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
    private readonly string[] tabs = { "Market", "Battle", "Campaign", "Victor", "Actions", "Log" };

    private Vector2 scrollPos;
    private readonly List<string> eventLog = new List<string>();
    private const int MaxLogEntries = 30;

    // Actions tab
    private float setCashValue = 500f;
    private int setInventoryValue = 5;
    private float setVictorCashValue = 500f;

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
            case 3: DrawVictorTab(campaignSystem); break;
            case 4: DrawActionsTab(campaignSystem.Data); break;
            case 5: DrawLogTab(); break;
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
            // 显示格子归属状态
            string gridStr = "";
            if (kvp.Value.GridOwners != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    gridStr += kvp.Value.GridOwners[i] switch
                    {
                        GridOwner.Ally => "A",
                        GridOwner.Enemy => "E",
                        _ => "."
                    };
                }
            }
            else
            {
                gridStr = ".....";
            }
            GUILayout.Label($"  {kvp.Key}: [{gridStr}] pos={kvp.Value.LinePosition:F1} stag={kvp.Value.StagnantTurns}");
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
        // GUILayout.Label($"审计值: {data.Player.AuditValue}");  // [已禁用] 审计值系统
        GUILayout.Label($"净资产: {data.Player.CalculateNetWorth(data.Market):F2}");
        GUILayout.Label($"后备役: {data.Battle.CurrentReserves}");

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

    private void DrawVictorTab(CampaignSystem campaignSystem)
    {
        var victorAI = campaignSystem.GetVictorAISystem();
        if (victorAI == null)
        {
            GUILayout.Label("Victor AI 未初始化");
            return;
        }

        var ledger = victorAI.GetLedger();
        var memory = victorAI.GetMemory();
        var data = campaignSystem.Data;

        GUILayout.Label("=== Victor 状态 ===");
        GUILayout.Label($"当前策略: {victorAI.LastStrategy}");

        GUILayout.Space(5);
        GUILayout.Label("--- 财务状况 ---");
        GUILayout.Label($"  现金: ${ledger.Cash:F2}");
        GUILayout.Label($"  负债: ${ledger.Debt:F2}");
        GUILayout.Label($"  净资产: ${victorAI.GetVictorNetWorth():F2}");

        GUILayout.Space(5);
        GUILayout.Label("--- 现货持仓 ---");
        foreach (OrderType type in System.Enum.GetValues(typeof(OrderType)))
        {
            float value = ledger.Holdings[type] * data.Market.CurrentPrices[type];
            GUILayout.Label($"  {type}: {ledger.Holdings[type]} (价值: ${value:F2})");
        }

        GUILayout.Space(5);
        GUILayout.Label($"--- 期货仓位 ({ledger.FuturesPositions.Count}) ---");
        foreach (var c in ledger.FuturesPositions)
        {
            float pnl = c.CalculatePnL(data.Market.CurrentPrices[c.TargetOrder]);
            string pnlColor = pnl >= 0 ? "+" : "";
            GUILayout.Label($"  #{c.ContractId} {c.Direction} {c.TargetOrder} x{c.Quantity}");
            GUILayout.Label($"    开仓:{c.OpenPrice:F2} 现价:{data.Market.CurrentPrices[c.TargetOrder]:F2} PnL:{pnlColor}{pnl:F2}");
            GUILayout.Label($"    到期回合:{c.ExpirationTurn}");
        }

        GUILayout.Space(5);
        GUILayout.Label("--- 陷阱状态 ---");
        GUILayout.Label($"  当前陷阱: {memory.ActiveTrap}");
        GUILayout.Label($"  陷阱阶段: {memory.TrapPhase}");
        GUILayout.Label($"  陷阱目标: {memory.TrapTargetType}");

        GUILayout.Space(5);
        GUILayout.Label("--- 对玩家仓位推断 ---");
        foreach (OrderType type in System.Enum.GetValues(typeof(OrderType)))
        {
            float est = memory.EstimatedPlayerPosition[type];
            string direction = est > 0.1f ? "做多" : (est < -0.1f ? "做空" : "中性");
            GUILayout.Label($"  {type}: {est:F2} ({direction})");
        }

        GUILayout.Space(5);
        GUILayout.Label("--- 本回合交易量 ---");
        foreach (OrderType type in System.Enum.GetValues(typeof(OrderType)))
        {
            int vol = memory.VictorTurnTradeVolume[type];
            if (vol != 0)
            {
                string action = vol > 0 ? $"买入{vol}" : $"卖出{-vol}";
                GUILayout.Label($"  {type}: {action}");
            }
        }

        // 上一回合计划详情
        var lastPlan = victorAI.LastPlan;
        if (lastPlan != null)
        {
            GUILayout.Space(5);
            GUILayout.Label("--- 上回合计划 ---");
            GUILayout.Label($"  策略原因: {lastPlan.StrategyReason}");
            GUILayout.Label($"  现货订单: {lastPlan.SpotOrders.Count}");
            GUILayout.Label($"  期货开仓: {lastPlan.FuturesOrders.Count}");
            GUILayout.Label($"  期货平仓: {lastPlan.FuturesCloseOrders.Count}");
            GUILayout.Label($"  将军干涉: {lastPlan.GeneralOrders.Count}");
            if (lastPlan.BorrowAmount > 0)
                GUILayout.Label($"  借款: ${lastPlan.BorrowAmount:F2}");
            if (lastPlan.RepayAmount > 0)
                GUILayout.Label($"  还款: ${lastPlan.RepayAmount:F2}");
        }

        // Victor 现金修改
        GUILayout.Space(10);
        GUILayout.Label("--- 调试操作 ---");
        GUILayout.BeginHorizontal();
        GUILayout.Label("设置Victor现金:", GUILayout.Width(120));
        setVictorCashValue = float.Parse(GUILayout.TextField(setVictorCashValue.ToString("F0"), GUILayout.Width(80)));
        if (GUILayout.Button("应用", GUILayout.Width(60)))
        {
            ledger.Cash = setVictorCashValue;
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("清空Victor所有持仓"))
        {
            ledger.Holdings[OrderType.ATK] = 0;
            ledger.Holdings[OrderType.DEF] = 0;
            ledger.Holdings[OrderType.RET] = 0;
        }

        if (GUILayout.Button("清空Victor所有期货"))
        {
            ledger.FuturesPositions.Clear();
        }

        if (GUILayout.Button("重置Victor债务"))
        {
            ledger.Debt = 0;
        }

        GUILayout.Space(10);
        GUILayout.Label("--- 日志导出 ---");

        GUILayout.BeginHorizontal();
        GUILayout.Label("日志状态:", GUILayout.Width(80));
        bool logEnabled = VictorLogger.Instance.IsEnabled;
        if (GUILayout.Button(logEnabled ? "已启用" : "已禁用", GUILayout.Width(80)))
        {
            VictorLogger.Instance.IsEnabled = !logEnabled;
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("导出完整日志到文件"))
        {
            string fullLog = VictorLogger.Instance.ExportFullLog();
            string exportPath = VictorLogger.Instance.GetLogFilePath().Replace(".log", "_full.log");
            System.IO.File.WriteAllText(exportPath, fullLog);
            Debug.Log($"[VictorLogger] 完整日志已导出到: {exportPath}");
        }

        if (GUILayout.Button("在资源管理器中打开日志目录"))
        {
            string logPath = VictorLogger.Instance.GetLogFilePath();
            string logDir = System.IO.Path.GetDirectoryName(logPath);
            System.Diagnostics.Process.Start("explorer.exe", logDir);
        }

        var turnLogs = VictorLogger.Instance.GetTurnLogs();
        GUILayout.Label($"已记录回合数: {turnLogs.Count}");
        if (turnLogs.Count > 0)
        {
            var lastLog = turnLogs[turnLogs.Count - 1];
            GUILayout.Label($"最近记录: 回合{lastLog.TurnNumber} 净资产变化:{(lastLog.NetWorthChange >= 0 ? "+" : "")}{lastLog.NetWorthChange:F2}");
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
                g.Troops = 20;
                g.Trust = 100;
                g.Morale = 100;
                g.ReorganizeTurns = 0;
            }
        }

        if (GUILayout.Button("所有战线推至5 (胜利)"))
        {
            foreach (var f in data.Battle.Frontlines.Values)
            {
                // 所有格子归己方占领
                if (f.GridOwners != null)
                {
                    for (int i = 0; i < 5; i++)
                        f.GridOwners[i] = GridOwner.Ally;
                }
            }
        }

        if (GUILayout.Button("所有战线退至1 (失败)"))
        {
            foreach (var f in data.Battle.Frontlines.Values)
            {
                // 所有格子归敌方占领
                if (f.GridOwners != null)
                {
                    for (int i = 0; i < 5; i++)
                        f.GridOwners[i] = GridOwner.Enemy;
                }
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
            if (r != null)
            {
                string captureInfo = "";
                if (r.AllyCapturedGrid.HasValue) captureInfo += $" 占格{r.AllyCapturedGrid.Value}";
                if (r.EnemyCapturedGrid.HasValue) captureInfo += $" 敌占格{r.EnemyCapturedGrid.Value}";
                AddLog($"战斗: {r.Position} {r.AllyOrder}vs{r.EnemyOrder}{captureInfo}");
            }
        });
        es.AddEventListening((EventID)WarBrokerEventID.OnGeneralRouted, (p1, p2) =>
        {
            var g = p1 as GeneralData;
            if (g != null) AddLog($"溃败: {g.Name}");
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
