using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Victor AI 行为日志记录器
/// 记录决策、市场影响、战场影响和收益变化
/// </summary>
public class VictorLogger
{
    private static VictorLogger _instance;
    public static VictorLogger Instance => _instance ??= new VictorLogger();

    private StringBuilder logBuffer = new StringBuilder();
    private List<VictorTurnLog> turnLogs = new List<VictorTurnLog>();
    private string logFilePath;
    private bool isEnabled = true;

    // 当前回合的临时记录
    private VictorTurnLog currentTurnLog;

    public bool IsEnabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    public VictorLogger()
    {
        // 日志文件保存在项目根目录的 Logs 文件夹
        string logDir = Path.Combine(Application.dataPath, "..", "Logs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logFilePath = Path.Combine(logDir, $"VictorAI_{timestamp}.log");
    }

    /// <summary>开始新回合的日志记录</summary>
    public void BeginTurn(int turnNumber, VictorStrategy strategy, string strategyReason,
                          float cashBefore, float debtBefore, float netWorthBefore,
                          Dictionary<OrderType, int> holdingsBefore,
                          Dictionary<OrderType, float> pricesBefore)
    {
        if (!isEnabled) return;

        currentTurnLog = new VictorTurnLog
        {
            TurnNumber = turnNumber,
            Strategy = strategy,
            StrategyReason = strategyReason,
            CashBefore = cashBefore,
            DebtBefore = debtBefore,
            NetWorthBefore = netWorthBefore,
            HoldingsBefore = new Dictionary<OrderType, int>(holdingsBefore),
            PricesBefore = new Dictionary<OrderType, float>(pricesBefore),
            SpotTrades = new List<SpotTradeLog>(),
            FuturesTrades = new List<FuturesTradeLog>(),
            GeneralActions = new List<GeneralActionLog>()
        };
    }

    /// <summary>记录现货交易</summary>
    public void LogSpotTrade(OrderType type, bool isBuy, int quantity, float pricePerUnit, float totalCost)
    {
        if (!isEnabled || currentTurnLog == null) return;

        currentTurnLog.SpotTrades.Add(new SpotTradeLog
        {
            Type = type,
            IsBuy = isBuy,
            Quantity = quantity,
            PricePerUnit = pricePerUnit,
            TotalCost = totalCost
        });
    }

    /// <summary>记录期货开仓</summary>
    public void LogFuturesOpen(int contractId, OrderType type, FuturesDirection direction,
                                int quantity, float openPrice, float margin)
    {
        if (!isEnabled || currentTurnLog == null) return;

        currentTurnLog.FuturesTrades.Add(new FuturesTradeLog
        {
            ContractId = contractId,
            Type = type,
            Direction = direction,
            IsOpen = true,
            Quantity = quantity,
            Price = openPrice,
            Margin = margin,
            PnL = 0
        });
    }

    /// <summary>记录期货平仓</summary>
    public void LogFuturesClose(int contractId, OrderType type, FuturesDirection direction,
                                 int quantity, float openPrice, float closePrice, float pnl)
    {
        if (!isEnabled || currentTurnLog == null) return;

        currentTurnLog.FuturesTrades.Add(new FuturesTradeLog
        {
            ContractId = contractId,
            Type = type,
            Direction = direction,
            IsOpen = false,
            Quantity = quantity,
            Price = closePrice,
            OpenPrice = openPrice,
            PnL = pnl
        });
    }

    /// <summary>记录将军干涉</summary>
    public void LogGeneralAction(string generalId, string generalName, string actionType,
                                  OrderType? orderType, int trustChange, int moraleChange)
    {
        if (!isEnabled || currentTurnLog == null) return;

        currentTurnLog.GeneralActions.Add(new GeneralActionLog
        {
            GeneralId = generalId,
            GeneralName = generalName,
            ActionType = actionType,
            OrderType = orderType,
            TrustChange = trustChange,
            MoraleChange = moraleChange
        });
    }

    /// <summary>记录借款</summary>
    public void LogBorrow(float amount)
    {
        if (!isEnabled || currentTurnLog == null) return;
        currentTurnLog.BorrowAmount = amount;
    }

    /// <summary>记录还款</summary>
    public void LogRepay(float amount)
    {
        if (!isEnabled || currentTurnLog == null) return;
        currentTurnLog.RepayAmount = amount;
    }

    /// <summary>记录战场状态</summary>
    public void LogBattlefieldStatus(List<FrontlineLogStatus> frontlines,
                                      List<GeneralLogStatus> allyGenerals,
                                      List<GeneralLogStatus> enemyGenerals)
    {
        if (!isEnabled || currentTurnLog == null) return;
        currentTurnLog.FrontlineStatuses = frontlines;
        currentTurnLog.AllyGeneralStatuses = allyGenerals;
        currentTurnLog.EnemyGeneralStatuses = enemyGenerals;
    }

    /// <summary>记录期货持仓状态</summary>
    public void LogFuturesPositions(List<FuturesPositionStatus> positions)
    {
        if (!isEnabled || currentTurnLog == null) return;
        currentTurnLog.FuturesPositions = positions;
    }

    /// <summary>记录陷阱状态</summary>
    public void LogTrapStatus(string status)
    {
        if (!isEnabled || currentTurnLog == null) return;
        currentTurnLog.TrapStatus = status;
    }

    /// <summary>记录市场库存</summary>
    public void LogMarketInventory(Dictionary<OrderType, int> inventoryBefore, Dictionary<OrderType, int> inventoryAfter)
    {
        if (!isEnabled || currentTurnLog == null) return;
        currentTurnLog.MarketInventoryBefore = new Dictionary<OrderType, int>(inventoryBefore);
        currentTurnLog.MarketInventoryAfter = new Dictionary<OrderType, int>(inventoryAfter);
    }

    /// <summary>记录市场三因子</summary>
    public void LogMarketFactors(Dictionary<OrderType, MarketFactors> factors)
    {
        if (!isEnabled || currentTurnLog == null) return;
        currentTurnLog.MarketFactorsBefore = new Dictionary<OrderType, MarketFactors>(factors);
    }

    /// <summary>记录市场三因子（回合结束时）</summary>
    public void LogMarketFactorsAfter(Dictionary<OrderType, MarketFactors> factors)
    {
        if (!isEnabled || currentTurnLog == null) return;
        currentTurnLog.MarketFactorsAfter = new Dictionary<OrderType, MarketFactors>(factors);
    }

    /// <summary>结束回合记录，计算最终状态</summary>
    public void EndTurn(float cashAfter, float debtAfter, float netWorthAfter,
                        Dictionary<OrderType, int> holdingsAfter,
                        Dictionary<OrderType, float> pricesAfter,
                        Dictionary<OrderType, int> marketInventoryChange)
    {
        if (!isEnabled || currentTurnLog == null) return;

        currentTurnLog.CashAfter = cashAfter;
        currentTurnLog.DebtAfter = debtAfter;
        currentTurnLog.NetWorthAfter = netWorthAfter;
        currentTurnLog.HoldingsAfter = new Dictionary<OrderType, int>(holdingsAfter);
        currentTurnLog.PricesAfter = new Dictionary<OrderType, float>(pricesAfter);
        currentTurnLog.MarketInventoryChange = new Dictionary<OrderType, int>(marketInventoryChange);

        // 计算收益
        currentTurnLog.CashChange = cashAfter - currentTurnLog.CashBefore;
        currentTurnLog.NetWorthChange = netWorthAfter - currentTurnLog.NetWorthBefore;

        // 计算价格变化
        currentTurnLog.PriceChanges = new Dictionary<OrderType, float>();
        foreach (var type in pricesAfter.Keys)
        {
            currentTurnLog.PriceChanges[type] = pricesAfter[type] - currentTurnLog.PricesBefore[type];
        }

        turnLogs.Add(currentTurnLog);

        // 写入日志文件
        WriteToFile(currentTurnLog);

        // 输出到 Unity Console
        Debug.Log(FormatTurnLog(currentTurnLog));

        currentTurnLog = null;
    }

    /// <summary>格式化单回合日志</summary>
    private string FormatTurnLog(VictorTurnLog log)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"══════════════════════════════════════════════════════════");
        sb.AppendLine($"[Victor Log] 回合 {log.TurnNumber}");
        sb.AppendLine($"══════════════════════════════════════════════════════════");

        // 策略
        sb.AppendLine($"【策略】{log.Strategy}");
        sb.AppendLine($"  原因: {log.StrategyReason}");

        // 财务变化
        sb.AppendLine($"【财务变化】");
        sb.AppendLine($"  现金: ${log.CashBefore:F2} → ${log.CashAfter:F2} ({(log.CashChange >= 0 ? "+" : "")}{log.CashChange:F2})");
        sb.AppendLine($"  负债: ${log.DebtBefore:F2} → ${log.DebtAfter:F2}");
        sb.AppendLine($"  净资产: ${log.NetWorthBefore:F2} → ${log.NetWorthAfter:F2} ({(log.NetWorthChange >= 0 ? "+" : "")}{log.NetWorthChange:F2})");

        // 借贷
        if (log.BorrowAmount > 0)
            sb.AppendLine($"  借款: +${log.BorrowAmount:F2}");
        if (log.RepayAmount > 0)
            sb.AppendLine($"  还款: -${log.RepayAmount:F2}");

        // 持仓变化
        sb.AppendLine($"【持仓变化】");
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            int before = log.HoldingsBefore.ContainsKey(type) ? log.HoldingsBefore[type] : 0;
            int after = log.HoldingsAfter.ContainsKey(type) ? log.HoldingsAfter[type] : 0;
            int change = after - before;
            if (change != 0)
                sb.AppendLine($"  {type}: {before} → {after} ({(change > 0 ? "+" : "")}{change})");
        }

        // 现货交易
        if (log.SpotTrades.Count > 0)
        {
            sb.AppendLine($"【现货交易】({log.SpotTrades.Count}笔)");
            foreach (var trade in log.SpotTrades)
            {
                string action = trade.IsBuy ? "买入" : "卖出";
                sb.AppendLine($"  {action} {trade.Type} x{trade.Quantity} @ ${trade.PricePerUnit:F2} = ${trade.TotalCost:F2}");
            }
        }

        // 期货交易
        if (log.FuturesTrades.Count > 0)
        {
            sb.AppendLine($"【期货交易】({log.FuturesTrades.Count}笔)");
            foreach (var trade in log.FuturesTrades)
            {
                if (trade.IsOpen)
                {
                    string dir = trade.Direction == FuturesDirection.Long ? "做多" : "做空";
                    sb.AppendLine($"  开仓 #{trade.ContractId} {dir} {trade.Type} x{trade.Quantity} @ ${trade.Price:F2} 保证金:${trade.Margin:F2}");
                }
                else
                {
                    string pnlStr = trade.PnL >= 0 ? $"+${trade.PnL:F2}" : $"-${-trade.PnL:F2}";
                    sb.AppendLine($"  平仓 #{trade.ContractId} {trade.Type} x{trade.Quantity} 开仓:${trade.OpenPrice:F2} 平仓:${trade.Price:F2} PnL:{pnlStr}");
                }
            }
        }

        // 将军干涉
        if (log.GeneralActions.Count > 0)
        {
            sb.AppendLine($"【将军干涉】({log.GeneralActions.Count}次)");
            foreach (var action in log.GeneralActions)
            {
                sb.AppendLine($"  {action.GeneralName}: {action.ActionType} {action.OrderType?.ToString() ?? ""}");
                if (action.TrustChange != 0 || action.MoraleChange != 0)
                    sb.AppendLine($"    信任{(action.TrustChange >= 0 ? "+" : "")}{action.TrustChange}, 士气{(action.MoraleChange >= 0 ? "+" : "")}{action.MoraleChange}");
            }
        }

        // 市场影响
        sb.AppendLine($"【市场影响】");
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            float priceBefore = log.PricesBefore.ContainsKey(type) ? log.PricesBefore[type] : 0;
            float priceAfter = log.PricesAfter.ContainsKey(type) ? log.PricesAfter[type] : 0;
            float priceChange = log.PriceChanges.ContainsKey(type) ? log.PriceChanges[type] : 0;
            int invChange = log.MarketInventoryChange.ContainsKey(type) ? log.MarketInventoryChange[type] : 0;

            string priceDir = priceChange > 0 ? "↑" : (priceChange < 0 ? "↓" : "→");
            sb.AppendLine($"  {type}: ${priceBefore:F2} → ${priceAfter:F2} {priceDir} ({(priceChange >= 0 ? "+" : "")}{priceChange:F2})  库存变化:{(invChange >= 0 ? "+" : "")}{invChange}");
        }

        // 市场三因子
        if (log.MarketFactorsBefore != null && log.MarketFactorsAfter != null)
        {
            sb.AppendLine($"【市场三因子】");
            foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
            {
                if (log.MarketFactorsBefore.TryGetValue(type, out var before) &&
                    log.MarketFactorsAfter.TryGetValue(type, out var after))
                {
                    sb.AppendLine($"  {type}:");
                    sb.AppendLine($"    Before: α={before.Alpha:F3} β={before.Beta:F3} γ={before.Gamma:F3}");
                    sb.AppendLine($"    After:  α={after.Alpha:F3} β={after.Beta:F3} γ={after.Gamma:F3}");
                    sb.AppendLine($"    Δα={after.Alpha - before.Alpha:+0.000;-0.000} Δβ={after.Beta - before.Beta:+0.000;-0.000} Δγ={after.Gamma - before.Gamma:+0.000;-0.000}");
                }
            }
        }

        // 战场状态
        if (log.FrontlineStatuses != null && log.FrontlineStatuses.Count > 0)
        {
            sb.AppendLine($"【战场状态】");
            foreach (var fl in log.FrontlineStatuses)
            {
                string engagedStr = fl.IsEngaged ? "交战中" : $"间隙={fl.Gap}";
                sb.AppendLine($"  {fl.Position}: 战线位置={fl.LinePosition}/5 ({engagedStr})");
            }
        }

        // 将军状态
        if (log.EnemyGeneralStatuses != null && log.EnemyGeneralStatuses.Count > 0)
        {
            sb.AppendLine($"【敌方将军(Victor)】");
            foreach (var g in log.EnemyGeneralStatuses)
            {
                sb.AppendLine($"  {g.Name}({g.Position}): HP={g.Troops} Grid={g.GridPosition} 状态={g.Status} 意图={g.DefaultIntent}");
            }
        }

        // 期货持仓状态
        if (log.FuturesPositions != null && log.FuturesPositions.Count > 0)
        {
            sb.AppendLine($"【期货持仓】({log.FuturesPositions.Count}个)");
            foreach (var pos in log.FuturesPositions)
            {
                string dir = pos.Direction == FuturesDirection.Long ? "多" : "空";
                string pnlStr = pos.PnL >= 0 ? $"+${pos.PnL:F2}" : $"-${-pos.PnL:F2}";
                sb.AppendLine($"  #{pos.ContractId} {dir}{pos.TargetOrder} x{pos.Quantity} 开仓:${pos.OpenPrice:F2} 现价:${pos.CurrentPrice:F2} PnL:{pnlStr}({pos.PnLRatio:P0}) 剩余{pos.TurnsRemaining}回合");
            }
        }

        // 陷阱状态
        if (!string.IsNullOrEmpty(log.TrapStatus))
        {
            sb.AppendLine($"【陷阱状态】{log.TrapStatus}");
        }

        sb.AppendLine($"══════════════════════════════════════════════════════════");
        return sb.ToString();
    }

    /// <summary>写入日志文件</summary>
    private void WriteToFile(VictorTurnLog log)
    {
        try
        {
            File.AppendAllText(logFilePath, FormatTurnLog(log) + "\n");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VictorLogger] 写入日志文件失败: {e.Message}");
        }
    }

    /// <summary>导出完整日志到文件</summary>
    public string ExportFullLog()
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
        sb.AppendLine("║           VICTOR AI 完整行为日志                          ║");
        sb.AppendLine($"║           导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}                 ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        foreach (var log in turnLogs)
        {
            sb.AppendLine(FormatTurnLog(log));
        }

        // 汇总统计
        if (turnLogs.Count > 0)
        {
            sb.AppendLine("╔══════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                      汇总统计                              ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════╝");

            var first = turnLogs[0];
            var last = turnLogs[turnLogs.Count - 1];

            sb.AppendLine($"总回合数: {turnLogs.Count}");
            sb.AppendLine($"初始净资产: ${first.NetWorthBefore:F2}");
            sb.AppendLine($"最终净资产: ${last.NetWorthAfter:F2}");
            sb.AppendLine($"总收益: ${last.NetWorthAfter - first.NetWorthBefore:F2}");

            int totalSpotTrades = 0;
            int totalFuturesTrades = 0;
            int totalGeneralActions = 0;
            float totalBorrow = 0;
            float totalRepay = 0;

            foreach (var log in turnLogs)
            {
                totalSpotTrades += log.SpotTrades.Count;
                totalFuturesTrades += log.FuturesTrades.Count;
                totalGeneralActions += log.GeneralActions.Count;
                totalBorrow += log.BorrowAmount;
                totalRepay += log.RepayAmount;
            }

            sb.AppendLine($"总现货交易: {totalSpotTrades}笔");
            sb.AppendLine($"总期货交易: {totalFuturesTrades}笔");
            sb.AppendLine($"总将军干涉: {totalGeneralActions}次");
            sb.AppendLine($"总借款: ${totalBorrow:F2}");
            sb.AppendLine($"总还款: ${totalRepay:F2}");
        }

        return sb.ToString();
    }

    /// <summary>获取日志文件路径</summary>
    public string GetLogFilePath() => logFilePath;

    /// <summary>获取所有回合日志</summary>
    public List<VictorTurnLog> GetTurnLogs() => turnLogs;

    /// <summary>清空日志</summary>
    public void Clear()
    {
        turnLogs.Clear();
        logBuffer.Clear();
    }
}

/// <summary>单回合日志数据</summary>
public class VictorTurnLog
{
    public int TurnNumber;
    public VictorStrategy Strategy;
    public string StrategyReason;

    // 财务状态
    public float CashBefore, CashAfter, CashChange;
    public float DebtBefore, DebtAfter;
    public float NetWorthBefore, NetWorthAfter, NetWorthChange;
    public float BorrowAmount, RepayAmount;

    // 持仓
    public Dictionary<OrderType, int> HoldingsBefore;
    public Dictionary<OrderType, int> HoldingsAfter;

    // 市场
    public Dictionary<OrderType, float> PricesBefore;
    public Dictionary<OrderType, float> PricesAfter;
    public Dictionary<OrderType, float> PriceChanges;
    public Dictionary<OrderType, int> MarketInventoryChange;
    public Dictionary<OrderType, int> MarketInventoryBefore;
    public Dictionary<OrderType, int> MarketInventoryAfter;

    // 市场三因子 (Alpha, Beta, Gamma)
    public Dictionary<OrderType, MarketFactors> MarketFactorsBefore;
    public Dictionary<OrderType, MarketFactors> MarketFactorsAfter;

    // 战场状态
    public List<FrontlineLogStatus> FrontlineStatuses;
    public List<GeneralLogStatus> AllyGeneralStatuses;
    public List<GeneralLogStatus> EnemyGeneralStatuses;

    // 期货持仓状态
    public List<FuturesPositionStatus> FuturesPositions;

    // 陷阱状态
    public string TrapStatus;

    // 交易记录
    public List<SpotTradeLog> SpotTrades;
    public List<FuturesTradeLog> FuturesTrades;
    public List<GeneralActionLog> GeneralActions;
}

public class FrontlineLogStatus
{
    public string Position;
    public int LinePosition;
    public int Gap;
    public bool IsEngaged;
}

public class GeneralLogStatus
{
    public string Name;
    public string Position;
    public int Troops;
    public int GridPosition;
    public string Status;
    public string DefaultIntent;
}

public class SpotTradeLog
{
    public OrderType Type;
    public bool IsBuy;
    public int Quantity;
    public float PricePerUnit;
    public float TotalCost;
}

public class FuturesTradeLog
{
    public int ContractId;
    public OrderType Type;
    public FuturesDirection Direction;
    public bool IsOpen;
    public int Quantity;
    public float Price;
    public float OpenPrice;
    public float Margin;
    public float PnL;
}

public class GeneralActionLog
{
    public string GeneralId;
    public string GeneralName;
    public string ActionType; // "Reinforce", "Tamper", "NoAction"
    public OrderType? OrderType;
    public int TrustChange;
    public int MoraleChange;
}

public class FuturesPositionStatus
{
    public int ContractId;
    public OrderType TargetOrder;
    public FuturesDirection Direction;
    public int Quantity;
    public float OpenPrice;
    public float CurrentPrice;
    public float Margin;
    public float PnL;
    public float PnLRatio; // PnL / Margin
    public int ExpirationTurn;
    public int TurnsRemaining;
}

/// <summary>市场三因子数据</summary>
public class MarketFactors
{
    public float Alpha;      // 战场态势因子
    public float Beta;       // 交易冲击因子
    public float Gamma;      // 流通盘因子
    public float BasePrice;  // 基础价格
    public float FinalPrice; // 最终价格

    public override string ToString()
    {
        return $"α={Alpha:F3} β={Beta:F3} γ={Gamma:F3} → ${FinalPrice:F2}";
    }
}
