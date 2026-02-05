using System.Collections.Generic;

/// <summary>
/// Victor 回合可视化数据
/// 从 VictorTurnPlan 提取的精简数据，用于信件对话生成
/// </summary>
public class VictorTurnVisualData
{
    /// <summary>当前回合数</summary>
    public int Turn;

    /// <summary>本回合主策略</summary>
    public VictorStrategy Strategy;

    /// <summary>策略选择原因（调试用）</summary>
    public string StrategyReason;

    /// <summary>
    /// 动作摘要列表
    /// 每个元素表示一个可视化动作：(动作类型, 指令类型, 数量, 规模)
    /// </summary>
    public List<VictorActionSummary> Actions = new List<VictorActionSummary>();

    /// <summary>将军干涉摘要：GeneralName → (是强化, 指令类型)</summary>
    public Dictionary<string, (bool isReinforce, OrderType orderType)> GeneralActions
        = new Dictionary<string, (bool, OrderType)>();

    /// <summary>本回合借款金额</summary>
    public float BorrowAmount;

    /// <summary>本回合还款金额</summary>
    public float RepayAmount;

    /// <summary>执行前现金</summary>
    public float CashBefore;

    /// <summary>执行后现金</summary>
    public float CashAfter;

    /// <summary>执行前净资产</summary>
    public float NetWorthBefore;

    /// <summary>执行后净资产</summary>
    public float NetWorthAfter;

    /// <summary>
    /// 从 VictorTurnPlan 构建可视化数据
    /// </summary>
    public static VictorTurnVisualData FromPlan(
        VictorTurnPlan plan,
        int turn,
        float cashBefore, float cashAfter,
        float netWorthBefore, float netWorthAfter,
        Dictionary<string, string> generalIdToName)
    {
        var data = new VictorTurnVisualData
        {
            Turn = turn,
            Strategy = plan.MainStrategy,
            StrategyReason = plan.StrategyReason,
            BorrowAmount = plan.BorrowAmount,
            RepayAmount = plan.RepayAmount,
            CashBefore = cashBefore,
            CashAfter = cashAfter,
            NetWorthBefore = netWorthBefore,
            NetWorthAfter = netWorthAfter
        };

        // 处理现货订单
        foreach (var (type, quantity, isBuy) in plan.SpotOrders)
        {
            data.Actions.Add(new VictorActionSummary
            {
                ActionType = isBuy ? VictorActionType.SpotBuy : VictorActionType.SpotSell,
                OrderType = type,
                Quantity = quantity,
                Scale = GetScale(quantity)
            });
        }

        // 处理期货开仓
        foreach (var (type, quantity, direction) in plan.FuturesOrders)
        {
            data.Actions.Add(new VictorActionSummary
            {
                ActionType = direction == FuturesDirection.Long
                    ? VictorActionType.FuturesLong
                    : VictorActionType.FuturesShort,
                OrderType = type,
                Quantity = quantity,
                Scale = GetScale(quantity)
            });
        }

        // 处理期货平仓
        if (plan.FuturesCloseOrders.Count > 0)
        {
            data.Actions.Add(new VictorActionSummary
            {
                ActionType = VictorActionType.FuturesClose,
                Quantity = plan.FuturesCloseOrders.Count,
                Scale = GetScale(plan.FuturesCloseOrders.Count)
            });
        }

        // 处理将军干涉
        foreach (var kvp in plan.GeneralOrders)
        {
            var (reinforce, tamper) = kvp.Value;
            if (reinforce.HasValue || tamper.HasValue)
            {
                string generalName = generalIdToName.ContainsKey(kvp.Key)
                    ? generalIdToName[kvp.Key]
                    : kvp.Key;

                if (reinforce.HasValue)
                {
                    data.GeneralActions[generalName] = (true, reinforce.Value);
                    data.Actions.Add(new VictorActionSummary
                    {
                        ActionType = VictorActionType.GeneralReinforce,
                        OrderType = reinforce.Value,
                        Quantity = 1,
                        Scale = ActionScale.Small,
                        TargetName = generalName
                    });
                }
                else if (tamper.HasValue)
                {
                    data.GeneralActions[generalName] = (false, tamper.Value);
                    data.Actions.Add(new VictorActionSummary
                    {
                        ActionType = VictorActionType.GeneralTamper,
                        OrderType = tamper.Value,
                        Quantity = 3,
                        Scale = ActionScale.Medium,
                        TargetName = generalName
                    });
                }
            }
        }

        // 处理借贷
        if (plan.BorrowAmount > 0)
        {
            data.Actions.Add(new VictorActionSummary
            {
                ActionType = VictorActionType.Borrow,
                Quantity = (int)plan.BorrowAmount,
                Scale = GetFinancialScale(plan.BorrowAmount)
            });
        }

        if (plan.RepayAmount > 0)
        {
            data.Actions.Add(new VictorActionSummary
            {
                ActionType = VictorActionType.Repay,
                Quantity = (int)plan.RepayAmount,
                Scale = GetFinancialScale(plan.RepayAmount)
            });
        }

        return data;
    }

    private static ActionScale GetScale(int quantity)
    {
        if (quantity <= 2) return ActionScale.Small;
        if (quantity <= 5) return ActionScale.Medium;
        return ActionScale.Large;
    }

    private static ActionScale GetFinancialScale(float amount)
    {
        if (amount <= 50) return ActionScale.Small;
        if (amount <= 150) return ActionScale.Medium;
        return ActionScale.Large;
    }
}

/// <summary>
/// Victor 单个动作摘要
/// </summary>
public class VictorActionSummary
{
    /// <summary>动作类型</summary>
    public VictorActionType ActionType;

    /// <summary>相关指令类型（部分动作可能为空）</summary>
    public OrderType OrderType;

    /// <summary>数量</summary>
    public int Quantity;

    /// <summary>规模</summary>
    public ActionScale Scale;

    /// <summary>目标名称（将军干涉时使用）</summary>
    public string TargetName;
}
