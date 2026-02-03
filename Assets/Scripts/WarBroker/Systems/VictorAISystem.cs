using System;
using System.Collections.Generic;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════
//  维克多策略枚举 (GDD v7 Section 7.5)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// 维克多五大策略 (GDD v7 Section 7.5)
/// 每回合动态选择一个主策略，所有行为服从该策略。
/// </summary>
public enum VictorStrategy
{
    /// <summary>军事优先：优先给将军采购指令并强化</summary>
    MilitaryFocus,

    /// <summary>趋势跟随：顺势建仓，将军配合仓位方向</summary>
    TrendFollowing,

    /// <summary>逆势狙击：推断玩家仓位，反向操作打击</summary>
    CounterStrike,

    /// <summary>设局：制造虚假市场信号，诱导玩家跟风</summary>
    DeceptionPlay,

    /// <summary>收割：平仓获利，抛售囤货，回归军事最优</summary>
    Harvest
}

// ═══════════════════════════════════════════════════════════════════
//  维克多回合执行计划 (GDD v7 Section 7.6)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// 维克多单回合执行计划 (GDD v7 Section 7.6)
/// 包含金融操作（现货/期货/借贷）和将军管理（强化/篡改/不作为）。
/// </summary>
public class VictorTurnPlan
{
    /// <summary>本回合主策略</summary>
    public VictorStrategy MainStrategy;

    /// <summary>现货买卖订单: (指令类型, 数量, 是否买入)</summary>
    public List<(OrderType type, int quantity, bool isBuy)> SpotOrders;

    /// <summary>期货订单: (指令类型, 数量, 方向)</summary>
    public List<(OrderType type, int quantity, FuturesDirection direction)> FuturesOrders;

    /// <summary>期货平仓订单: contractId列表</summary>
    public List<int> FuturesCloseOrders;

    /// <summary>本回合借款金额</summary>
    public float BorrowAmount;

    /// <summary>本回合还款金额</summary>
    public float RepayAmount;

    /// <summary>
    /// 将军管理指令: GeneralId → (强化指令类型, 篡改目标类型)
    /// reinforce != null → 强化该类型（消耗1份同类现货）
    /// tamper != null → 篡改为该类型（消耗3份该类现货）
    /// 两者均 null → 不干涉（将军执行默认意图）
    /// </summary>
    public Dictionary<string, (OrderType? reinforce, OrderType? tamper)> GeneralOrders;

    /// <summary>策略选择原因（调试用）</summary>
    public string StrategyReason;

    public VictorTurnPlan()
    {
        SpotOrders = new List<(OrderType, int, bool)>();
        FuturesOrders = new List<(OrderType, int, FuturesDirection)>();
        FuturesCloseOrders = new List<int>();
        GeneralOrders = new Dictionary<string, (OrderType?, OrderType?)>();
    }
}

// ═══════════════════════════════════════════════════════════════════
//  维克多 AI 系统 (GDD v7 Chapter 7, DevGuide Tasks 2.3–2.7)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// 维克多 AI 系统 (GDD v7 Chapter 7)
/// 
/// 维克多·克劳斯不是"敌方军事AI"，而是与玩家完全对称的战争掮客：
/// - 拥有自己的 Cash / Holdings / Futures / Debt
/// - 对敌方将军有强化/篡改权
/// - 目标：最大化自己的净资产
///
/// 架构:
///   SelectStrategy()  → 动态选择本回合主策略（五选一）
///   GeneratePlan()    → 根据策略生成具体执行计划
///   ExecutePlan()     → 执行金融操作+将军管理
///   UpdateMemory()    → 推断玩家仓位，推进陷阱状态
/// </summary>
public class VictorAISystem
{
    // ── 依赖引用 ──
    private VictorProfile profile;
    private VictorLedger ledger;
    private VictorMemory memory;
    private CampaignRuntimeData data;
    private MarketSystem marketSystem;
    private IntentSystem intentSystem;
    private GameBalanceConfig balanceConfig;
    private OrderConfig orderConfig;

    // ── 内部追踪 ──
    private int nextVictorContractId = 10000; // 维克多期货合约ID从10000起，避免与玩家冲突

    /// <summary>上一回合选择的策略（供外部调试查看）</summary>
    public VictorStrategy LastStrategy { get; private set; }

    /// <summary>上一回合的执行计划（供外部调试查看）</summary>
    public VictorTurnPlan LastPlan { get; private set; }

    // ═══════════════════════════════════════════════════════════
    //  初始化
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 初始化维克多AI系统
    /// 在 CampaignSystem.InitNewCampaign() 中调用
    /// </summary>
    public void Init(VictorProfile profile, CampaignRuntimeData data,
                     MarketSystem marketSystem, IntentSystem intentSystem,
                     GameBalanceConfig balanceConfig, OrderConfig orderConfig)
    {
        this.profile = profile;
        this.data = data;
        this.marketSystem = marketSystem;
        this.intentSystem = intentSystem;
        this.balanceConfig = balanceConfig;
        this.orderConfig = orderConfig;

        this.ledger = data.VictorLedger;
        this.memory = data.VictorMemory;

        // 确保内存已初始化
        if (memory.EstimatedPlayerPosition == null)
            memory.Init();
    }

    // ═══════════════════════════════════════════════════════════
    //  主入口 (DevGuide Task 2.7)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 维克多每回合主入口
    /// 在 CampaignSystem.EnterVictorPhase() 中调用
    /// 返回 VictorTurnPlan 供 CampaignSystem 提取将军指令
    /// </summary>
    public VictorTurnPlan ExecuteTurn()
    {
        // 0. 快照本回合开盘价（用于回合结束时推断玩家影响）
        memory.ResetTurnData(data.Market);

        // 记录回合开始前的状态
        var pricesBefore = new Dictionary<OrderType, float>(data.Market.CurrentPrices);
        var holdingsBefore = new Dictionary<OrderType, int>(ledger.Holdings);
        var inventoryBefore = new Dictionary<OrderType, int>(data.Market.MarketInventory);
        float cashBefore = ledger.Cash;
        float debtBefore = ledger.Debt;
        float netWorthBefore = GetVictorNetWorth();

        // 记录市场三因子（回合开始时）
        var marketFactorsBefore = marketSystem.GetPricingEngine().GetAllMarketFactors();

        // 1. 生成敌方将军默认意图
        GenerateEnemyGeneralIntents();

        // 2. 选择策略
        var strategy = SelectStrategy();
        LastStrategy = strategy;

        // 更新连续策略计数
        if (strategy == memory.LastStrategy)
        {
            memory.ConsecutiveStrategyCount++;
        }
        else
        {
            memory.ConsecutiveStrategyCount = 1;
        }
        memory.LastStrategy = strategy;

        // 3. 生成执行计划
        var plan = GeneratePlan(strategy);
        LastPlan = plan;

        // 开始日志记录
        VictorLogger.Instance.BeginTurn(
            data.CurrentTurn, strategy, plan.StrategyReason,
            cashBefore, debtBefore, netWorthBefore,
            holdingsBefore, pricesBefore);

        // 记录市场三因子
        VictorLogger.Instance.LogMarketFactors(marketFactorsBefore);

        // 记录战场状态
        LogBattlefieldStatus();

        // 记录期货持仓状态
        LogFuturesPositions();

        // 记录陷阱状态
        LogTrapStatus();

        // 4. 执行计划（金融操作 + 将军管理）
        ExecutePlan(plan);

        // 5. 更新跨回合记忆（推断玩家仓位、推进陷阱阶段）
        UpdateMemory();

        // 更新上回合净资产（用于下回合判断是否亏损）
        memory.LastNetWorth = GetVictorNetWorth();

        // 记录回合结束后的状态
        var inventoryChange = new Dictionary<OrderType, int>();
        foreach (OrderType type in System.Enum.GetValues(typeof(OrderType)))
        {
            inventoryChange[type] = data.Market.MarketInventory[type] - inventoryBefore[type];
        }

        // 记录市场三因子（回合结束时）
        var marketFactorsAfter = marketSystem.GetPricingEngine().GetAllMarketFactors();
        VictorLogger.Instance.LogMarketFactorsAfter(marketFactorsAfter);

        VictorLogger.Instance.EndTurn(
            ledger.Cash, ledger.Debt, GetVictorNetWorth(),
            ledger.Holdings, data.Market.CurrentPrices, inventoryChange);

        return plan;
    }

    /// <summary>为敌方所有将军生成默认意图</summary>
    private void GenerateEnemyGeneralIntents()
    {
        foreach (var general in data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            var defaultIntent = intentSystem.GenerateDefaultIntent(general);
            general.DefaultIntent = defaultIntent;
            general.FinalIntent = defaultIntent;
            general.IntentSource = IntentSource.Default;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  策略选择 (GDD v7 Section 7.5, DevGuide Task 2.3)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 策略选择算法 (GDD v7 Section 7.5)
    /// 分数 = 性格权重 × 情境权重 × 随机扰动(0.8~1.2)
    /// 选择分数最高的策略
    /// </summary>
    private VictorStrategy SelectStrategy()
    {
        // ── 陷阱覆写：Phase 2 强制收割 ──
        if (memory.ActiveTrap != DeceptionPlanType.None && memory.TrapPhase >= 2)
        {
            return VictorStrategy.Harvest;
        }

        var scores = new Dictionary<VictorStrategy, float>();

        // 1. 军事优先: MilitaryPriority × 战况紧急度
        float militaryUrgency = CalculateMilitaryUrgency();
        scores[VictorStrategy.MilitaryFocus] =
            profile.MilitaryPriority * militaryUrgency * RandomPerturb();

        // 2. 趋势跟随: SpeculationTendency × 趋势明确度
        float trendClarity = CalculateTrendClarity();
        scores[VictorStrategy.TrendFollowing] =
            profile.SpeculationTendency * trendClarity * RandomPerturb();

        // 3. 逆势狙击: Adaptiveness × 玩家仓位暴露度
        float playerExposure = CalculatePlayerExposure();
        scores[VictorStrategy.CounterStrike] =
            profile.Adaptiveness * playerExposure * RandomPerturb();

        // 4. 设局: Deception × 资金充裕度（仅在非陷阱阶段可选）
        // 优化：前 20% 回合不触发设局（需要先观察市场）
        float cashAbundance = Mathf.Clamp01(ledger.Cash / (data.Config.VictorInitialCash + 1f));
        float gameProgress = (float)data.CurrentTurn / data.MaxTurns;
        float deceptionViability = (gameProgress > 0.2f && gameProgress < 0.8f) ? 1f : 0f; // 前20%和后20%禁止设局
        scores[VictorStrategy.DeceptionPlay] =
            profile.Deception * cashAbundance * deceptionViability * RandomPerturb();

        // 5. 收割: 多种触发条件
        float harvestScore = CalculateHarvestScore();
        scores[VictorStrategy.Harvest] = harvestScore * RandomPerturb();

        // ── 连续策略惩罚：避免连续使用同一策略超过 3 回合 ──
        if (memory.ConsecutiveStrategyCount >= 3)
        {
            // 降低当前连续策略的分数
            if (scores.ContainsKey(memory.LastStrategy))
            {
                scores[memory.LastStrategy] *= 0.5f;
            }
        }
        else if (memory.ConsecutiveStrategyCount >= 2)
        {
            // 轻微降低
            if (scores.ContainsKey(memory.LastStrategy))
            {
                scores[memory.LastStrategy] *= 0.8f;
            }
        }

        // ── 选最高分 ──
        VictorStrategy best = VictorStrategy.MilitaryFocus;
        float bestScore = -1f;
        foreach (var kv in scores)
        {
            if (kv.Value > bestScore)
            {
                bestScore = kv.Value;
                best = kv.Key;
            }
        }

        return best;
    }

    /// <summary>随机扰动 (0.8~1.2)，让维克多行为不完全可预测</summary>
    private float RandomPerturb() => UnityEngine.Random.Range(0.8f, 1.2f);

    // ── 策略评估辅助 ──

    /// <summary>
    /// 军事紧急度 (0~1)
    /// 检查敌方将军HP、战线位置是否危急
    /// </summary>
    private float CalculateMilitaryUrgency()
    {
        // 如果没有任何战线处于交战状态，军事紧急度很低
        if (!IsAnyFrontlineEngaged())
        {
            return 0.2f; // 未交战时基础紧急度很低
        }

        float urgency = 0.5f; // 基础值

        foreach (var general in data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed)
            {
                urgency += 0.15f; // 有将军溃败，军事压力大
                continue;
            }

            var status = general.GetStatus(balanceConfig);

            // 将军HP低 → 需要支援
            if (status == GeneralStatus.Critical)
                urgency += 0.15f;
            else if (status == GeneralStatus.Wounded)
                urgency += 0.08f;
        }

        // 战线位置检查
        foreach (var frontline in data.Battle.Frontlines.Values)
        {
            // 对维克多来说，Grid 5 是他的大本营（敌方大本营=己方视角Grid 5）
            // 己方将军在Grid 4-5 → 即将突破，好事（低紧急度）
            // 己方将军在Grid 1-2 → 被压制（高紧急度）
            if (frontline.LinePosition <= 2.5f)
                urgency += 0.1f;  // 己方被压
            else if (frontline.LinePosition >= 3.5f)
                urgency -= 0.05f; // 己方推进顺利
        }

        return Mathf.Clamp01(urgency);
    }

    /// <summary>
    /// 检查是否有任何战线处于交战状态
    /// 用于判断是否需要进行军事干涉
    /// </summary>
    private bool IsAnyFrontlineEngaged()
    {
        foreach (var general in data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            // 找到对应的己方将军
            var allyGeneral = data.Battle.AllyGenerals.Find(g => g.Position == general.Position);
            if (allyGeneral == null || allyGeneral.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            // 检查是否接触 (Gap = EnemyPos - AllyPos - 1 <= 0)
            int gap = general.GridPosition - allyGeneral.GridPosition - 1;
            if (gap <= 0)
            {
                return true; // 有战线已接触
            }
        }
        return false;
    }

    /// <summary>
    /// 获取指定将军所在战线的交战状态
    /// </summary>
    private bool IsGeneralEngaged(GeneralData enemyGeneral)
    {
        var allyGeneral = data.Battle.AllyGenerals.Find(g => g.Position == enemyGeneral.Position);
        if (allyGeneral == null || allyGeneral.GetStatus(balanceConfig) == GeneralStatus.Routed)
            return false;

        int gap = enemyGeneral.GridPosition - allyGeneral.GridPosition - 1;
        return gap <= 0;
    }

    /// <summary>
    /// 获取指定将军所在战线的间隙距离
    /// 返回 -1 表示无法计算（对方将军不存在或已溃败）
    /// </summary>
    private int GetGeneralGap(GeneralData enemyGeneral)
    {
        var allyGeneral = data.Battle.AllyGenerals.Find(g => g.Position == enemyGeneral.Position);
        if (allyGeneral == null || allyGeneral.GetStatus(balanceConfig) == GeneralStatus.Routed)
            return -1;

        return enemyGeneral.GridPosition - allyGeneral.GridPosition - 1;
    }

    /// <summary>
    /// 趋势明确度 (0~1)
    /// 检查近2-3回合价格是否有明确单边趋势
    /// </summary>
    private float CalculateTrendClarity()
    {
        float maxTrend = 0f;

        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            float trend = Mathf.Abs(GetPriceTrend(type));
            if (trend > maxTrend)
                maxTrend = trend;
        }

        // trend > 0.1 认为有明确趋势
        return Mathf.Clamp01(maxTrend / 0.15f);
    }

    /// <summary>
    /// 玩家仓位暴露度 (0~1)
    /// 根据 VictorMemory.EstimatedPlayerPosition 判断
    /// </summary>
    private float CalculatePlayerExposure()
    {
        float maxExposure = 0f;

        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            float exposure = Mathf.Abs(memory.EstimatedPlayerPosition[type]);
            if (exposure > maxExposure)
                maxExposure = exposure;
        }

        // 归一化：暴露度 > 5 认为高度暴露
        return Mathf.Clamp01(maxExposure / 5f);
    }

    /// <summary>
    /// 未实现浮盈 (绝对金额)
    /// 计算维克多当前持仓+期货的浮动盈亏
    /// </summary>
    private float CalculateUnrealizedProfit()
    {
        float profit = 0f;

        // 持仓浮盈（相对于基础价）
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            float basePrice = GetBasePrice(type);
            float currentPrice = data.Market.CurrentPrices[type];
            profit += ledger.Holdings[type] * (currentPrice - basePrice);
        }

        // 期货浮盈
        foreach (var contract in ledger.FuturesPositions)
        {
            float currentPrice = data.Market.CurrentPrices[contract.TargetOrder];
            profit += contract.CalculatePnL(currentPrice);
        }

        return profit;
    }

    /// <summary>
    /// 计算收割策略分数
    /// 多种触发条件：浮盈、现金危机、债务压力、游戏末期、持仓过重、连续亏损
    /// </summary>
    private float CalculateHarvestScore()
    {
        float score = 0f;
        float initialCash = data.Config.VictorInitialCash + 1f;

        // 1. 浮盈触发（原逻辑）
        float unrealizedProfit = CalculateUnrealizedProfit();
        if (unrealizedProfit > 0)
        {
            score += unrealizedProfit / initialCash;
        }

        // 2. 现金危机触发：现金低于初始资金的 20%
        float cashRatio = ledger.Cash / initialCash;
        if (cashRatio < 0.2f)
        {
            score += 0.6f * (1f - cashRatio / 0.2f); // 现金越少分数越高
        }

        // 3. 债务压力触发：债务超过净资产的 50%
        float netWorth = GetVictorNetWorth();
        if (netWorth > 0 && ledger.Debt > netWorth * 0.5f)
        {
            score += 0.4f;
        }

        // 4. 期货亏损止损触发：有期货亏损超过保证金的 50%
        foreach (var contract in ledger.FuturesPositions)
        {
            float pnl = contract.CalculatePnL(data.Market.CurrentPrices[contract.TargetOrder]);
            if (pnl < -contract.Margin * 0.5f)
            {
                score += 0.3f; // 有严重亏损的期货，应该考虑止损
                break;
            }
        }

        // 5. 持仓过重触发：持仓价值超过净资产的 50%（降低阈值）
        float holdingsValue = 0f;
        int totalHoldings = 0;
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            holdingsValue += ledger.Holdings[type] * data.Market.CurrentPrices[type];
            totalHoldings += ledger.Holdings[type];
        }
        if (netWorth > 0 && holdingsValue > netWorth * 0.5f)
        {
            score += 0.4f;
        }

        // 6. 持仓数量过多触发：总持仓超过 5 份
        if (totalHoldings > 5)
        {
            score += 0.3f * (totalHoldings - 5) / 5f;
        }

        // 7. 游戏末期触发
        float gameProgress = (float)data.CurrentTurn / data.MaxTurns;
        if (gameProgress >= 0.8f)
        {
            score += 0.5f * (gameProgress - 0.8f) / 0.2f; // 最后20%回合逐渐增加
        }

        // 8. 连续亏损触发：净资产连续下降
        if (memory.LastNetWorth > 0 && netWorth < memory.LastNetWorth * 0.9f)
        {
            score += 0.3f; // 净资产下降超过 10%，考虑止损
        }

        return score;
    }

    // ═══════════════════════════════════════════════════════════
    //  计划生成 (GDD v7 Section 7.5-7.7, DevGuide Task 2.4)
    // ═══════════════════════════════════════════════════════════

    /// <summary>根据策略生成执行计划</summary>
    private VictorTurnPlan GeneratePlan(VictorStrategy strategy)
    {
        var plan = new VictorTurnPlan { MainStrategy = strategy };

        switch (strategy)
        {
            case VictorStrategy.MilitaryFocus:
                PlanMilitaryFocus(plan);
                break;
            case VictorStrategy.TrendFollowing:
                PlanTrendFollowing(plan);
                break;
            case VictorStrategy.CounterStrike:
                PlanCounterStrike(plan);
                break;
            case VictorStrategy.DeceptionPlay:
                PlanDeceptionPlay(plan);
                break;
            case VictorStrategy.Harvest:
                PlanHarvest(plan);
                break;
        }

        // 所有策略都需要评估借贷需求
        EvaluateLendingDecision(plan, strategy);

        // 所有策略都需要检查期货管理（除了 Harvest 已经处理过）
        if (strategy != VictorStrategy.Harvest)
        {
            EvaluateFuturesManagement(plan);
        }

        return plan;
    }

    /// <summary>
    /// 通用期货管理逻辑
    /// 检查是否需要平仓盈利期货或止损亏损期货
    /// </summary>
    private void EvaluateFuturesManagement(VictorTurnPlan plan)
    {
        foreach (var contract in ledger.FuturesPositions)
        {
            // 跳过已经在计划中要平仓的
            if (plan.FuturesCloseOrders.Contains(contract.ContractId)) continue;

            float currentPrice = data.Market.CurrentPrices[contract.TargetOrder];
            float pnl = contract.CalculatePnL(currentPrice);
            float pnlRatio = pnl / contract.Margin;

            // 盈利超过保证金的 50% → 考虑平仓获利
            if (pnlRatio > 0.5f)
            {
                plan.FuturesCloseOrders.Add(contract.ContractId);
            }
            // 亏损超过保证金的 60% → 止损
            else if (pnlRatio < -0.6f)
            {
                plan.FuturesCloseOrders.Add(contract.ContractId);
            }
            // 即将到期（2 回合内）→ 平仓
            else if (contract.ExpirationTurn <= data.CurrentTurn + 2)
            {
                plan.FuturesCloseOrders.Add(contract.ContractId);
            }
        }
    }

    // ── 军事优先 (MilitaryFocus) ──

    /// <summary>
    /// 军事优先策略 (GDD v7 Section 7.5)
    /// 优先给将军采购指令并强化，金融操作仅限于为军事采购筹资。
    /// 优化：未交战时减少不必要的消费，只在真正需要时才强化将军。
    /// </summary>
    private void PlanMilitaryFocus(VictorTurnPlan plan)
    {
        plan.StrategyReason = "优先保障战场需求";

        // 检查是否有任何战线处于交战状态
        bool anyEngaged = IsAnyFrontlineEngaged();

        foreach (var general in data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            // 检查该将军是否处于交战状态
            bool isEngaged = IsGeneralEngaged(general);
            int gap = GetGeneralGap(general);

            // 未交战且间隙较大时，跳过强化（节省资源）
            // gap > 1 表示双方还有较大距离，不需要立即强化
            if (!isEngaged && gap > 1)
            {
                plan.GeneralOrders[general.GeneralId] = (null, null); // 让将军自行决定
                continue;
            }

            // 即将交战（gap == 1）或已交战（gap <= 0）时才考虑强化
            var intent = general.DefaultIntent ?? OrderType.DEF;
            float price = marketSystem.GetPricingEngine().CalculatePrice(intent);
            float basePrice = GetBasePrice(intent);

            // 价格容忍度检查 (GDD v7 Section 11.7)
            // 未交战但即将交战时，价格容忍度降低（更谨慎）
            float toleranceMultiplier = isEngaged ? profile.PriceToleranceMultiplier : profile.PriceToleranceMultiplier * 0.8f;

            if (price <= basePrice * toleranceMultiplier)
            {
                // 需要从市场买入 1 份用于强化
                if (NeedToBuyForGeneral(intent, plan))
                {
                    plan.SpotOrders.Add((intent, 1, true));
                }
                plan.GeneralOrders[general.GeneralId] = (intent, null); // 强化
            }
            else
            {
                // 价格太高，放弃采购，将军执行默认意图
                plan.GeneralOrders[general.GeneralId] = (null, null);
            }
        }
    }

    // ── 趋势跟随 (TrendFollowing) ──

    /// <summary>
    /// 趋势跟随策略 (GDD v7 Section 7.5)
    /// 识别价格趋势，顺势建仓。将军管理配合仓位方向。
    /// 优化：避免在价格高点做多、低点做空
    /// </summary>
    private void PlanTrendFollowing(VictorTurnPlan plan)
    {
        plan.StrategyReason = "跟随市场趋势";

        // 识别趋势最强的指数（同时考虑价格位置）
        OrderType trendType = OrderType.ATK;
        float maxTrend = 0f;
        bool foundValidTrend = false;

        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            float trend = GetPriceTrend(type);
            float priceLevel = GetPriceLevel(type);

            // 跳过不合适的趋势：
            // - 涨势但价格已经很高（> 1.3 倍基础价）→ 不追涨
            // - 跌势但价格已经很低（< 0.7 倍基础价）→ 不追跌
            if (trend > 0 && priceLevel > 1.3f) continue;
            if (trend < 0 && priceLevel < 0.7f) continue;

            if (Mathf.Abs(trend) > Mathf.Abs(maxTrend))
            {
                maxTrend = trend;
                trendType = type;
                foundValidTrend = true;
            }
        }

        if (!foundValidTrend || Mathf.Abs(maxTrend) < 0.05f)
        {
            // 没有合适的趋势，退回军事优先
            PlanMilitaryFocus(plan);
            plan.StrategyReason = "无合适趋势，退回军事优先";
            return;
        }

        float currentPriceLevel = GetPriceLevel(trendType);

        if (maxTrend > 0)
        {
            // 涨势 → 做多（买入现货 + 开多头期货）
            // 优化：价格越高，买入量越少
            int maxBuyThisTurn = currentPriceLevel > 1.2f ? 1 : (currentPriceLevel > 1.1f ? 2 : 3);
            int totalBought = 0;

            int available = data.Market.MarketInventory[trendType];
            int buyQty = Mathf.Min(2, available, maxBuyThisTurn);

            // 资金检查：确保有足够现金
            float estimatedCost = buyQty * data.Market.CurrentPrices[trendType] * 1.1f;
            if (ledger.Cash < estimatedCost * 2)
            {
                buyQty = Mathf.Min(buyQty, 1);
            }

            if (buyQty > 0)
            {
                plan.SpotOrders.Add((trendType, buyQty, true));
                totalBought += buyQty;
            }

            // 期货：价格不太高时才开仓
            if (ledger.FuturesPositions.Count < 3 && currentPriceLevel < 1.25f)
            {
                plan.FuturesOrders.Add((trendType, 2, FuturesDirection.Long));
            }

            // 将军管理
            PlanGeneralsForTrend(plan, trendType, maxBuyThisTurn, ref totalBought);
        }
        else
        {
            // 跌势 → 做空（卖出现货 + 开空头期货）
            // 优化：价格越低，卖出量越少
            int sellQty = currentPriceLevel < 0.8f ? 1 : (currentPriceLevel < 0.9f ? 2 : 3);
            sellQty = Mathf.Min(sellQty, ledger.Holdings[trendType]);

            if (sellQty > 0)
            {
                plan.SpotOrders.Add((trendType, sellQty, false));
            }

            // 期货：价格不太低时才开空仓
            if (ledger.FuturesPositions.Count < 3 && currentPriceLevel > 0.75f)
            {
                plan.FuturesOrders.Add((trendType, 2, FuturesDirection.Short));
            }

            // 将军管理：跌势时不干涉将军，让他们自行决定
            foreach (var general in data.Battle.EnemyGenerals)
            {
                if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;
                plan.GeneralOrders[general.GeneralId] = (null, null);
            }
        }
    }

    /// <summary>趋势跟随时的将军管理</summary>
    private void PlanGeneralsForTrend(VictorTurnPlan plan, OrderType trendType, int maxBuyThisTurn, ref int totalBought)
    {
        bool anyEngaged = IsAnyFrontlineEngaged();

        foreach (var general in data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            // 已达到本回合买入上限
            if (totalBought >= maxBuyThisTurn)
            {
                plan.GeneralOrders[general.GeneralId] = (null, null);
                continue;
            }

            bool isEngaged = IsGeneralEngaged(general);
            int gap = GetGeneralGap(general);

            // 未交战且间隙较大时，不干涉将军
            if (!isEngaged && gap > 1)
            {
                plan.GeneralOrders[general.GeneralId] = (null, null);
                continue;
            }

            // 尝试让将军执行趋势类型指令
            if (general.DefaultIntent.HasValue && general.DefaultIntent.Value == trendType)
            {
                if (isEngaged || gap <= 1)
                {
                    if (NeedToBuyForGeneral(trendType, plan) && totalBought < maxBuyThisTurn)
                    {
                        plan.SpotOrders.Add((trendType, 1, true));
                        totalBought++;
                    }
                    plan.GeneralOrders[general.GeneralId] = (trendType, null);
                }
                else
                {
                    plan.GeneralOrders[general.GeneralId] = (null, null);
                }
            }
            else
            {
                // 不匹配时不干涉
                plan.GeneralOrders[general.GeneralId] = (null, null);
            }
        }
    }

    // ── 逆势狙击 (CounterStrike) ──

    /// <summary>
    /// 逆势狙击策略 (GDD v7 Section 7.5)
    /// 推断玩家仓位方向，反向操作打击。宁可让将军打败仗也要让玩家亏钱。
    /// 优化：考虑价格位置，避免在不利位置建仓
    /// </summary>
    private void PlanCounterStrike(VictorTurnPlan plan)
    {
        plan.StrategyReason = "打击玩家仓位";

        // 找到玩家暴露最大的方向
        OrderType targetType = OrderType.ATK;
        float maxExposure = 0f;

        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            float exposure = Mathf.Abs(memory.EstimatedPlayerPosition[type]);
            if (exposure > maxExposure)
            {
                maxExposure = exposure;
                targetType = type;
            }
        }

        float playerDirection = memory.EstimatedPlayerPosition[targetType];

        if (Mathf.Abs(playerDirection) < 0.5f)
        {
            // 玩家仓位信号太弱，退回军事优先
            PlanMilitaryFocus(plan);
            plan.StrategyReason = "玩家仓位信号弱，退回军事优先";
            return;
        }

        float priceLevel = GetPriceLevel(targetType);

        if (playerDirection > 0)
        {
            // 玩家做多 → 维克多做空 + 操纵将军减少该类需求
            // 优化：价格太低时不做空（玩家可能已经亏损，没必要追击）
            if (priceLevel < 0.7f)
            {
                PlanMilitaryFocus(plan);
                plan.StrategyReason = "价格已低，无需追击做空";
                return;
            }

            if (ledger.FuturesPositions.Count < 3 && priceLevel > 0.8f)
                plan.FuturesOrders.Add((targetType, 3, FuturesDirection.Short));

            // 将军管理：故意不给将军该类指令，减少消耗压低价格
            foreach (var general in data.Battle.EnemyGenerals)
            {
                if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

                // 检查是否满足背叛阈值 (BetraySelfThreshold)
                float potentialProfit = maxExposure * 3f; // 粗略估算
                float profitRatio = potentialProfit / (data.Config.VictorInitialCash + 1f);

                if (profitRatio > profile.BetraySelfThreshold)
                {
                    // 利润足够大，故意违背军事最优解
                    plan.GeneralOrders[general.GeneralId] = (null, null); // 不干涉
                }
                else
                {
                    PlanGeneralMilitary(plan, general);
                }
            }
        }
        else
        {
            // 玩家做空 → 维克多做多（买入推高价格）
            // 优化：价格太高时不做多（玩家可能已经亏损）
            if (priceLevel > 1.3f)
            {
                PlanMilitaryFocus(plan);
                plan.StrategyReason = "价格已高，无需追击做多";
                return;
            }

            // 根据价格位置调整买入量
            int maxBuy = priceLevel > 1.1f ? 1 : (priceLevel > 1.0f ? 2 : 3);
            int available = data.Market.MarketInventory[targetType];
            int buyQty = Mathf.Min(maxBuy, available);

            // 资金检查
            float estimatedCost = buyQty * data.Market.CurrentPrices[targetType] * 1.1f;
            if (ledger.Cash < estimatedCost * 2)
            {
                buyQty = Mathf.Min(buyQty, 1);
            }

            if (buyQty > 0)
                plan.SpotOrders.Add((targetType, buyQty, true));

            if (ledger.FuturesPositions.Count < 3 && priceLevel < 1.2f)
                plan.FuturesOrders.Add((targetType, 3, FuturesDirection.Long));

            // 将军管理：配合做多，但不额外买入（已经买了足够的）
            foreach (var general in data.Battle.EnemyGenerals)
            {
                if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

                // 不再为将军额外买入，只使用已有库存
                plan.GeneralOrders[general.GeneralId] = (null, null);
            }
        }
    }

    // ── 设局 (DeceptionPlay) ──

    /// <summary>
    /// 设局策略 (GDD v7 Section 7.7)
    /// Phase 1: 大量买入制造假信号 + 悄悄开空头
    /// Phase 2: 由 SelectStrategy() 强制转为 Harvest，集中抛售
    /// </summary>
    private void PlanDeceptionPlay(VictorTurnPlan plan)
    {
        if (memory.TrapPhase == 0)
        {
            // ── 初始化新陷阱 ──

            // 选择目标指数：优先流通盘最大的（容易操纵且有库存吸收买入）
            OrderType target = OrderType.ATK;
            int maxFloat = 0;
            foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
            {
                if (data.Market.MarketInventory[type] > maxFloat)
                {
                    maxFloat = data.Market.MarketInventory[type];
                    target = type;
                }
            }

            // Phase 1: 大量买入制造假信号
            int buyQty = Mathf.Min(8, maxFloat / 2);
            buyQty = Mathf.Max(buyQty, 2); // 至少买2份才有信号效果

            float estimatedCost = data.Market.CurrentPrices[target] * buyQty * 1.2f;
            if (ledger.Cash < estimatedCost)
            {
                // 资金不足以设局，退回军事优先
                PlanMilitaryFocus(plan);
                plan.StrategyReason = "资金不足以设局，退回军事优先";
                return;
            }

            plan.SpotOrders.Add((target, buyQty, true));

            // 同时悄悄开空头（准备在Phase 2获利）
            if (ledger.FuturesPositions.Count < 3)
                plan.FuturesOrders.Add((target, 3, FuturesDirection.Short));

            // 将军管理：不给将军用这些指令（囤着，制造"需求"假象）
            foreach (var general in data.Battle.EnemyGenerals)
            {
                if (general.GetStatus(balanceConfig) != GeneralStatus.Routed)
                    plan.GeneralOrders[general.GeneralId] = (null, null);
            }

            // 记录陷阱状态
            memory.ActiveTrap = DeceptionPlanType.PumpAndDump;
            memory.TrapTargetType = target;
            memory.TrapPhase = 1;
            memory.TrapHoldings = buyQty;
            memory.TrapStartTurn = data.CurrentTurn;

            plan.StrategyReason = $"设局: PumpAndDump on {target}, Phase 1 买入{buyQty}份";
        }
        else
        {
            // 正在执行中的陷阱：不应到这里（Phase 2 由 SelectStrategy 强制走 Harvest）
            PlanMilitaryFocus(plan);
            plan.StrategyReason = "陷阱执行中，军事兜底";
        }
    }

    // ── 收割 (Harvest) ──

    /// <summary>
    /// 收割策略 (GDD v7 Section 7.5)
    /// 平仓获利，抛售囤积的库存，还款减少利息，将军管理回归军事最优。
    /// 也用于陷阱 Phase 2 的集中抛售。
    /// 增强：现金危机时也会止损平仓亏损期货。
    /// </summary>
    private void PlanHarvest(VictorTurnPlan plan)
    {
        plan.StrategyReason = "获利了结";

        bool isTrapHarvest = memory.HasActiveTrap && memory.TrapPhase >= 2;
        bool isCashCrisis = ledger.Cash < data.Config.VictorInitialCash * 0.2f;

        if (isTrapHarvest)
        {
            // ── 陷阱收割：集中抛售陷阱目标指数 ──
            OrderType trapTarget = memory.TrapTargetType;
            int sellQty = ledger.Holdings[trapTarget];
            if (sellQty > 0)
            {
                plan.SpotOrders.Add((trapTarget, sellQty, false)); // 抛售
            }
            plan.StrategyReason = $"陷阱收割: 抛售 {trapTarget} × {sellQty}";
        }
        else
        {
            // ── 正常收割：抛售所有囤积的现货 ──
            foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
            {
                // 保留一定库存用于将军（军事最优数量）
                int militaryNeed = isCashCrisis ? 0 : CountMilitaryNeed(type); // 现金危机时全卖
                int sellable = ledger.Holdings[type] - militaryNeed;
                if (sellable > 0)
                {
                    plan.SpotOrders.Add((type, sellable, false));
                }
            }

            if (isCashCrisis)
            {
                plan.StrategyReason = "现金危机: 清仓回血";
            }
        }

        // 平仓期货：盈利的平仓获利，亏损严重的止损
        foreach (var contract in ledger.FuturesPositions)
        {
            float pnl = contract.CalculatePnL(data.Market.CurrentPrices[contract.TargetOrder]);

            // 盈利 → 平仓获利
            if (pnl > 0)
            {
                plan.FuturesCloseOrders.Add(contract.ContractId);
            }
            // 亏损超过保证金50% 或 现金危机 或 即将到期 → 止损
            else if (pnl < -contract.Margin * 0.5f ||
                     isCashCrisis ||
                     contract.ExpirationTurn <= data.CurrentTurn + 1)
            {
                plan.FuturesCloseOrders.Add(contract.ContractId);
            }
        }

        // 还债（优先还债以减少利息）
        if (ledger.Debt > 0)
        {
            // 预估卖出后的现金
            float estimatedCashAfterSell = ledger.Cash;
            foreach (var order in plan.SpotOrders)
            {
                if (!order.isBuy)
                {
                    estimatedCashAfterSell += order.quantity * data.Market.CurrentPrices[order.type] * 0.95f;
                }
            }

            plan.RepayAmount = Mathf.Min(
                ledger.Debt,
                estimatedCashAfterSell * (1f - profile.CashReserveRatio)
            );
        }

        // 清除陷阱状态
        memory.ClearTrap();

        // 将军管理回归军事最优（现金危机时不买新指令）
        if (!isCashCrisis)
        {
            PlanMilitaryBase(plan);
        }
        else
        {
            // 现金危机时，将军自生自灭
            foreach (var general in data.Battle.EnemyGenerals)
            {
                if (general.GetStatus(balanceConfig) != GeneralStatus.Routed)
                    plan.GeneralOrders[general.GeneralId] = (null, null);
            }
        }
    }

    // ── 将军管理辅助 ──

    /// <summary>为所有将军安排军事最优指令（不考虑金融因素）</summary>
    private void PlanMilitaryBase(VictorTurnPlan plan)
    {
        foreach (var general in data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;
            if (plan.GeneralOrders.ContainsKey(general.GeneralId)) continue;

            PlanGeneralMilitary(plan, general);
        }
    }

    /// <summary>
    /// 为单个将军安排军事最优指令
    /// 优化：未交战时不强化，节省资源
    /// </summary>
    private void PlanGeneralMilitary(VictorTurnPlan plan, GeneralData general)
    {
        // 检查该将军是否处于交战状态
        bool isEngaged = IsGeneralEngaged(general);
        int gap = GetGeneralGap(general);

        // 未交战且间隙较大时，跳过强化
        if (!isEngaged && gap > 1)
        {
            plan.GeneralOrders[general.GeneralId] = (null, null);
            return;
        }

        var intent = general.DefaultIntent ?? OrderType.DEF;
        float price = marketSystem.GetPricingEngine().CalculatePrice(intent);
        float basePrice = GetBasePrice(intent);

        // 未交战但即将交战时，价格容忍度降低
        float toleranceMultiplier = isEngaged ? profile.PriceToleranceMultiplier : profile.PriceToleranceMultiplier * 0.8f;

        if (price <= basePrice * toleranceMultiplier)
        {
            if (NeedToBuyForGeneral(intent, plan))
                plan.SpotOrders.Add((intent, 1, true));
            plan.GeneralOrders[general.GeneralId] = (intent, null);
        }
        else
        {
            plan.GeneralOrders[general.GeneralId] = (null, null);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  借贷决策 (GDD v7 Section 7.9)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 评估借贷需求 (GDD v7 Section 7.9)
    /// 根据当前策略决定是否借款。
    /// </summary>
    private void EvaluateLendingDecision(VictorTurnPlan plan, VictorStrategy strategy)
    {
        // 计算计划所需总资金
        float plannedSpending = EstimatePlanCost(plan);
        float availableCash = ledger.Cash;

        switch (strategy)
        {
            case VictorStrategy.MilitaryFocus:
                // 仅在现金不足以采购关键指令时借款
                if (availableCash < plannedSpending)
                {
                    float shortfall = plannedSpending - availableCash;
                    float buffer = data.Config.VictorInitialCash * 0.1f;
                    plan.BorrowAmount = shortfall + buffer;
                }
                break;

            case VictorStrategy.TrendFollowing:
            case VictorStrategy.CounterStrike:
                // 趋势强烈时借款加杠杆
                if (profile.SpeculationTendency > 0.5f)
                {
                    float capacity = ledger.GetBorrowCapacity(data.Market, balanceConfig.LoanRatio);
                    if (capacity > 0 && availableCash < plannedSpending * 1.5f)
                    {
                        plan.BorrowAmount = capacity * profile.SpeculationTendency * 0.5f;
                    }
                }
                break;

            case VictorStrategy.DeceptionPlay:
                // 布局阶段可能需要大量资金
                if (availableCash < plannedSpending)
                {
                    float shortfall = plannedSpending - availableCash;
                    plan.BorrowAmount = shortfall;
                }
                break;

            case VictorStrategy.Harvest:
                // 收割时不借款（在还款）
                break;
        }

        // 限制借款额度
        if (plan.BorrowAmount > 0)
        {
            float maxBorrow = ledger.GetBorrowCapacity(data.Market, balanceConfig.LoanRatio);
            plan.BorrowAmount = Mathf.Min(plan.BorrowAmount, maxBorrow);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  计划执行
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 执行维克多回合计划
    /// 操作顺序：借款 → 现货交易 → 期货交易 → 将军管理 → 还款
    /// </summary>
    private void ExecutePlan(VictorTurnPlan plan)
    {
        // ── Step 1: 借款（先借款才有资金执行操作）──
        if (plan.BorrowAmount > 0)
        {
            ExecuteBorrow(plan.BorrowAmount);
        }

        // ── Step 2: 现货交易 ──
        foreach (var order in plan.SpotOrders)
        {
            if (order.isBuy)
                ExecuteVictorBuy(order.type, order.quantity);
            else
                ExecuteVictorSell(order.type, order.quantity);
        }

        // ── Step 3: 期货交易 ──
        // 平仓
        foreach (var contractId in plan.FuturesCloseOrders)
        {
            ExecuteVictorCloseFutures(contractId);
        }
        // 开仓
        foreach (var order in plan.FuturesOrders)
        {
            ExecuteVictorOpenFutures(order.type, order.quantity, order.direction);
        }

        // ── Step 4: 将军管理（强化/篡改/不作为）──
        foreach (var kvp in plan.GeneralOrders)
        {
            var general = data.Battle.EnemyGenerals.Find(g => g.GeneralId == kvp.Key);
            if (general == null) continue;
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            var (reinforce, tamper) = kvp.Value;

            if (reinforce.HasValue)
            {
                ExecuteVictorReinforce(general, reinforce.Value);
            }
            else if (tamper.HasValue)
            {
                ExecuteVictorTamper(general, tamper.Value);
            }
            // else: 不干涉，将军执行默认意图
        }

        // ── Step 5: 还款 ──
        if (plan.RepayAmount > 0)
        {
            ExecuteRepay(plan.RepayAmount);
        }
    }

    // ── 现货交易执行 ──

    /// <summary>
    /// 维克多买入现货
    /// 逐张计算，每张交易后价格变化（通过Beta累积），与玩家买入完全对称。
    /// 增加资金保护：保留最低现金储备。
    /// </summary>
    private void ExecuteVictorBuy(OrderType type, int quantity)
    {
        var market = data.Market;
        var pricingEngine = marketSystem.GetPricingEngine();

        int actualBuy = Mathf.Min(quantity, market.MarketInventory[type]);
        if (actualBuy <= 0) return;

        // 计算最低现金储备
        float minCashReserve = data.Config.VictorInitialCash * profile.CashReserveRatio;

        float totalCost = 0f;
        int actualBought = 0;
        float avgPrice = 0f;

        for (int i = 0; i < actualBuy; i++)
        {
            float price = pricingEngine.CalculatePrice(type);
            float commission = price * balanceConfig.CommissionRate;
            float unitCost = price + commission;

            // 资金保护：保留最低现金储备
            if (ledger.Cash - unitCost < minCashReserve) break;

            ledger.Cash -= unitCost;
            ledger.Holdings[type] += 1;
            market.MarketInventory[type] -= 1;
            totalCost += unitCost;
            actualBought++;
            avgPrice = totalCost / actualBought;

            // 交易冲击 → Beta累积
            pricingEngine.ApplyTradeImpact(type, 1, true);

            // 记录消耗量（用于三回合补充计算）
            market.AccumulatedBurn[type] += 1;

            // 记录维克多交易量（用于推断排除）
            memory.VictorTurnTradeVolume[type] += 1;
        }

        // 记录日志
        if (actualBought > 0)
        {
            VictorLogger.Instance.LogSpotTrade(type, true, actualBought, avgPrice, totalCost);
        }
    }

    /// <summary>
    /// 维克多卖出现货
    /// 逐张计算，卖出压低价格。
    /// </summary>
    private void ExecuteVictorSell(OrderType type, int quantity)
    {
        var market = data.Market;
        var pricingEngine = marketSystem.GetPricingEngine();

        int actualSell = Mathf.Min(quantity, ledger.Holdings[type]);
        if (actualSell <= 0) return;

        float totalRevenue = 0f;
        int actualSold = 0;

        for (int i = 0; i < actualSell; i++)
        {
            float price = pricingEngine.CalculatePrice(type);
            float commission = price * balanceConfig.CommissionRate;
            float revenue = price - commission;

            ledger.Cash += revenue;
            ledger.Holdings[type] -= 1;
            market.MarketInventory[type] += 1;
            totalRevenue += revenue;
            actualSold++;

            // 交易冲击 → Beta累积（卖出压低价格）
            pricingEngine.ApplyTradeImpact(type, 1, false);

            // 记录维克多交易量
            memory.VictorTurnTradeVolume[type] -= 1;
        }

        // 记录日志
        if (actualSold > 0)
        {
            float avgPrice = totalRevenue / actualSold;
            VictorLogger.Instance.LogSpotTrade(type, false, actualSold, avgPrice, totalRevenue);
        }
    }

    // ── 期货交易执行 ──

    /// <summary>维克多开立期货合约</summary>
    private void ExecuteVictorOpenFutures(OrderType type, int quantity, FuturesDirection direction)
    {
        var market = data.Market;
        float openPrice = market.CurrentPrices[type];
        float margin = openPrice * quantity * balanceConfig.FuturesMarginRate;

        if (ledger.Cash < margin) return; // 保证金不足

        var contract = new FuturesContract
        {
            ContractId = nextVictorContractId++,
            TargetOrder = type,
            Direction = direction,
            OpenPrice = openPrice,
            Quantity = quantity,
            ExpirationTurn = data.CurrentTurn + balanceConfig.MaxFuturesDuration,
            Margin = margin
        };

        ledger.Cash -= margin;
        ledger.FuturesPositions.Add(contract);

        // 记录日志
        VictorLogger.Instance.LogFuturesOpen(contract.ContractId, type, direction, quantity, openPrice, margin);
    }

    /// <summary>维克多平仓期货合约</summary>
    private void ExecuteVictorCloseFutures(int contractId)
    {
        var contract = ledger.FuturesPositions.Find(c => c.ContractId == contractId);
        if (contract == null) return;

        float currentPrice = data.Market.CurrentPrices[contract.TargetOrder];
        float pnl = contract.CalculatePnL(currentPrice);

        // 记录日志（在移除前记录）
        VictorLogger.Instance.LogFuturesClose(contractId, contract.TargetOrder, contract.Direction,
            contract.Quantity, contract.OpenPrice, currentPrice, pnl);

        ledger.Cash += contract.Margin + pnl;
        ledger.FuturesPositions.Remove(contract);
    }

    // ── 将军干涉执行 ──

    /// <summary>
    /// 维克多强化将军 (GDD v7 Section 4.6)
    /// 消耗1份同类现货，信任度+5，士气+5
    /// </summary>
    private void ExecuteVictorReinforce(GeneralData general, OrderType orderType)
    {
        // 检查库存
        if (ledger.Holdings[orderType] < 1) return;

        // 检查是否匹配默认意图
        if (!general.DefaultIntent.HasValue || general.DefaultIntent.Value != orderType) return;

        // 执行强化
        ledger.Holdings[orderType] -= 1;
        general.FinalIntent = orderType;
        general.IntentSource = IntentSource.Reinforced;
        general.Trust = Mathf.Clamp(general.Trust + 5, 0, 100);
        general.Morale = Mathf.Clamp(general.Morale + 5, 0, 100);

        // 记录日志
        VictorLogger.Instance.LogGeneralAction(general.GeneralId, general.Name, "Reinforce", orderType, 5, 5);
    }

    /// <summary>
    /// 维克多篡改将军意图 (GDD v7 Section 4.6)
    /// 消耗3份目标类型现货，信任度-15，士气-5
    /// </summary>
    private void ExecuteVictorTamper(GeneralData general, OrderType targetType)
    {
        // 检查库存
        if (ledger.Holdings[targetType] < 3) return;

        // 不能篡改为同类
        if (general.DefaultIntent.HasValue && general.DefaultIntent.Value == targetType) return;

        // 执行篡改
        ledger.Holdings[targetType] -= 3;
        general.FinalIntent = targetType;
        general.IntentSource = IntentSource.Overridden;
        general.Trust = Mathf.Clamp(general.Trust - 15, 0, 100);
        general.Morale = Mathf.Clamp(general.Morale - 5, 0, 100);

        // 记录日志
        VictorLogger.Instance.LogGeneralAction(general.GeneralId, general.Name, "Tamper", targetType, -15, -5);
    }

    // ── 借贷执行 ──

    /// <summary>维克多借款 (GDD v7 Section 3.5)</summary>
    private void ExecuteBorrow(float amount)
    {
        float maxBorrow = ledger.GetBorrowCapacity(data.Market, balanceConfig.LoanRatio);
        float actualBorrow = Mathf.Min(amount, maxBorrow);
        if (actualBorrow <= 0) return;

        ledger.Cash += actualBorrow;
        ledger.Debt += actualBorrow;

        // 记录日志
        VictorLogger.Instance.LogBorrow(actualBorrow);
    }

    /// <summary>维克多还款</summary>
    private void ExecuteRepay(float amount)
    {
        float actualRepay = Mathf.Min(amount, Mathf.Min(ledger.Debt, ledger.Cash));
        if (actualRepay <= 0) return;

        ledger.Cash -= actualRepay;
        ledger.Debt -= actualRepay;

        // 记录日志
        VictorLogger.Instance.LogRepay(actualRepay);
    }

    // ═══════════════════════════════════════════════════════════
    //  回合结束：记忆更新 (DevGuide Tasks 2.5-2.6)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 回合结束时更新跨回合记忆
    /// 1. 推断玩家仓位方向
    /// 2. 推进陷阱阶段
    /// </summary>
    private void UpdateMemory()
    {
        // ── 推断玩家仓位 (GDD v7 Section 7.8, DevGuide Task 2.6) ──
        UpdatePlayerPositionEstimate();

        // ── 推进陷阱阶段 (DevGuide Task 2.5) ──
        // 优化：陷阱至少持续 2 回合才能进入收割阶段
        if (memory.ActiveTrap != DeceptionPlanType.None && memory.TrapPhase == 1)
        {
            int trapDuration = data.CurrentTurn - memory.TrapStartTurn;
            if (trapDuration >= 2) // 至少持续 2 回合
            {
                memory.AdvanceTrapToHarvest(); // Phase 1 → Phase 2，下回合强制收割
            }
        }
    }

    /// <summary>
    /// 推断玩家仓位方向 (GDD v7 Section 7.8)
    /// 
    /// 核心算法：
    ///   本回合价格总变化 = ΔPrice
    ///   维克多自己造成的变化 = 维克多本回合交易量 × 冲击系数
    ///   玩家造成的变化（推测）= ΔPrice - 维克多造成的变化
    ///   累积多回合推测 → 形成对玩家仓位方向的判断
    ///   判断置信度 = Adaptiveness × 累积信号强度
    /// </summary>
    private void UpdatePlayerPositionEstimate()
    {
        if (profile.Adaptiveness < 0.01f) return; // 低适应性维克多不做此分析

        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            // 本回合价格总变化
            float openPrice = memory.TurnOpenPrices[type];
            if (openPrice <= 0f) continue;

            float closePrice = data.Market.CurrentPrices[type];
            float totalDelta = closePrice - openPrice;

            // 维克多自己造成的变化（估算）
            int victorVolume = memory.VictorTurnTradeVolume[type];
            float currentFloat = Mathf.Max(data.Market.MarketInventory[type], 1f);
            float victorImpact = victorVolume / currentFloat * openPrice * 0.5f;

            // 差值 = 玩家造成的变化（推测）
            float playerImpact = totalDelta - victorImpact;

            // 累积推断（指数衰减旧信号）
            // new = old × 0.7 + playerImpact × Adaptiveness
            memory.EstimatedPlayerPosition[type] =
                memory.EstimatedPlayerPosition[type] * 0.7f
                + playerImpact * profile.Adaptiveness;

            // 更新近期冲击记录
            memory.RecentPlayerImpact[type] =
                memory.RecentPlayerImpact[type] * 0.5f + playerImpact * 0.5f;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  回合间结算：利息 & 仓储费 & 期货到期
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 维克多回合间结算（在 CampaignSystem 结算阶段调用）
    /// 与玩家享受完全相同的费率和规则。
    /// </summary>
    public void ApplyTurnSettlement()
    {
        // 银行利息 (GDD v7 Section 3.5): Debt *= 1.05
        if (ledger.Debt > 0)
        {
            ledger.Debt *= (1f + balanceConfig.BankInterestRate);
        }

        // 仓储费 (GDD v7 Section 3.3): 每张 $3
        float storageCost = ledger.TotalHoldings * balanceConfig.StorageCostPerUnit;
        ledger.Cash -= storageCost;

        // 期货到期强制结算
        SettleExpiredFutures();

        // 期货爆仓检查
        CheckForceLiquidation();
    }

    /// <summary>结算到期期货</summary>
    private void SettleExpiredFutures()
    {
        var expired = new List<FuturesContract>();

        foreach (var contract in ledger.FuturesPositions)
        {
            if (contract.ExpirationTurn <= data.CurrentTurn)
            {
                expired.Add(contract);
            }
        }

        foreach (var contract in expired)
        {
            float currentPrice = data.Market.CurrentPrices[contract.TargetOrder];
            float pnl = contract.CalculatePnL(currentPrice);
            ledger.Cash += contract.Margin + pnl;
            ledger.FuturesPositions.Remove(contract);
        }
    }

    /// <summary>
    /// 检查期货爆仓 (GDD v7 Section 3.4)
    /// 做多爆仓点：价格 ≤ 开仓价 × 0.86
    /// 做空爆仓点：价格 ≥ 开仓价 × 1.14
    /// </summary>
    private void CheckForceLiquidation()
    {
        var liquidated = new List<FuturesContract>();

        foreach (var contract in ledger.FuturesPositions)
        {
            float currentPrice = data.Market.CurrentPrices[contract.TargetOrder];
            float pnl = contract.CalculatePnL(currentPrice);
            float equity = contract.Margin + pnl;
            float maintenanceMargin = contract.Margin * balanceConfig.ForceLiquidationRate;

            // 原始公式: 维持保证金率30%，即 equity < margin × 0.3
            // balanceConfig.ForceLiquidationRate = 0.8 对应 "亏损80%时爆仓"
            // 但按 GDD v7: 维持线 = 占用保证金 × 30%
            // → equity < margin × 0.3 时爆仓
            if (equity < contract.Margin * 0.3f)
            {
                liquidated.Add(contract);
            }
        }

        foreach (var contract in liquidated)
        {
            float currentPrice = data.Market.CurrentPrices[contract.TargetOrder];
            float pnl = contract.CalculatePnL(currentPrice);
            ledger.Cash += contract.Margin + pnl; // 可能为负值
            ledger.FuturesPositions.Remove(contract);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  辅助方法
    // ═══════════════════════════════════════════════════════════

    /// <summary>获取指数基础价格</summary>
    private float GetBasePrice(OrderType type)
    {
        var config = orderConfig.GetConfig(type);
        return config != null ? config.BasePrice : 30f;
    }

    /// <summary>
    /// 计算当前价格相对于基础价格的偏离度
    /// 返回值：1.0 = 基础价格，1.5 = 高于基础价格 50%，0.5 = 低于基础价格 50%
    /// </summary>
    private float GetPriceLevel(OrderType type)
    {
        float basePrice = GetBasePrice(type);
        float currentPrice = data.Market.CurrentPrices[type];
        return basePrice > 0 ? currentPrice / basePrice : 1f;
    }

    /// <summary>
    /// 判断价格是否处于高位（不适合做多）
    /// 高于基础价格 40% 以上认为是高位
    /// </summary>
    private bool IsPriceHigh(OrderType type)
    {
        return GetPriceLevel(type) > 1.4f;
    }

    /// <summary>
    /// 判断价格是否处于低位（不适合做空）
    /// 低于基础价格 40% 以下认为是低位
    /// </summary>
    private bool IsPriceLow(OrderType type)
    {
        return GetPriceLevel(type) < 0.6f;
    }

    /// <summary>
    /// 计算价格趋势 (正=涨, 负=跌)
    /// 基于最近2回合的价格变化率
    /// </summary>
    private float GetPriceTrend(OrderType type)
    {
        var history = data.Market.PriceHistory;
        if (history.Count < 2) return 0f;

        float current = data.Market.CurrentPrices[type];
        float prev1 = history[history.Count - 1][type];

        if (prev1 <= 0f) return 0f;

        float trend = (current - prev1) / prev1;

        // 如果有更早的数据，使用加权平均
        if (history.Count >= 3)
        {
            float prev2 = history[history.Count - 2][type];
            if (prev2 > 0f)
            {
                float olderTrend = (prev1 - prev2) / prev2;
                trend = trend * 0.7f + olderTrend * 0.3f; // 近期权重更大
            }
        }

        return trend;
    }

    /// <summary>判断是否需要为将军额外购买指令</summary>
    private bool NeedToBuyForGeneral(OrderType type, VictorTurnPlan plan)
    {
        // 计算计划中已安排买入的数量
        int plannedBuy = 0;
        foreach (var order in plan.SpotOrders)
        {
            if (order.type == type && order.isBuy)
                plannedBuy += order.quantity;
        }

        // 计算计划中强化/篡改需要消耗的数量
        int plannedConsume = 0;
        foreach (var kvp in plan.GeneralOrders)
        {
            var (reinforce, tamper) = kvp.Value;
            if (reinforce.HasValue && reinforce.Value == type)
                plannedConsume += 1;
            if (tamper.HasValue && tamper.Value == type)
                plannedConsume += 3;
        }

        int currentHoldings = ledger.Holdings[type];
        int netAvailable = currentHoldings + plannedBuy - plannedConsume;

        return netAvailable < 1; // 如果没有富余，需要额外购买
    }

    /// <summary>判断是否有足够资金和库存执行篡改</summary>
    private bool CanAffordTamper(OrderType type, VictorTurnPlan plan)
    {
        float price = marketSystem.GetPricingEngine().CalculatePrice(type);
        float cost = price * 3 * (1 + balanceConfig.CommissionRate);

        // 保留现金储备
        float reserveCash = ledger.Cash * profile.CashReserveRatio;
        if (ledger.Cash - reserveCash < cost) return false;

        // 检查流通盘
        int available = data.Market.MarketInventory[type];
        int plannedBuy = 0;
        foreach (var order in plan.SpotOrders)
        {
            if (order.type == type && order.isBuy)
                plannedBuy += order.quantity;
        }
        if (available - plannedBuy < 3) return false;

        return true;
    }

    /// <summary>统计当前需要该类型指令的将军数量</summary>
    private int CountMilitaryNeed(OrderType type)
    {
        int count = 0;
        foreach (var general in data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;
            if (general.DefaultIntent.HasValue && general.DefaultIntent.Value == type)
                count++;
        }
        return count;
    }

    /// <summary>估算计划的总现金消耗</summary>
    private float EstimatePlanCost(VictorTurnPlan plan)
    {
        float cost = 0f;

        // 现货买入成本
        foreach (var order in plan.SpotOrders)
        {
            if (order.isBuy)
            {
                float price = data.Market.CurrentPrices[order.type];
                cost += price * order.quantity * (1 + balanceConfig.CommissionRate);
            }
        }

        // 期货保证金
        foreach (var order in plan.FuturesOrders)
        {
            float price = data.Market.CurrentPrices[order.type];
            cost += price * order.quantity * balanceConfig.FuturesMarginRate;
        }

        return cost;
    }

    // ═══════════════════════════════════════════════════════════
    //  外部查询接口（供 CampaignSystem / Debug 调用）
    // ═══════════════════════════════════════════════════════════

    /// <summary>获取维克多当前净资产</summary>
    public float GetVictorNetWorth()
    {
        return ledger.GetNetWorth(data.Market);
    }

    /// <summary>获取维克多账本（战役结算用）</summary>
    public VictorLedger GetLedger() => ledger;

    /// <summary>获取维克多记忆（调试用）</summary>
    public VictorMemory GetMemory() => memory;

    // ═══════════════════════════════════════════════════════════
    //  日志辅助方法
    // ═══════════════════════════════════════════════════════════

    /// <summary>记录战场状态到日志</summary>
    private void LogBattlefieldStatus()
    {
        var frontlines = new List<FrontlineLogStatus>();
        var allyGenerals = new List<GeneralLogStatus>();
        var enemyGenerals = new List<GeneralLogStatus>();

        // 记录战线状态
        foreach (var kvp in data.Battle.Frontlines)
        {
            var ally = data.Battle.AllyGenerals.Find(g => g.Position == kvp.Key);
            var enemy = data.Battle.EnemyGenerals.Find(g => g.Position == kvp.Key);

            int gap = -1;
            bool isEngaged = false;
            if (ally != null && enemy != null)
            {
                gap = enemy.GridPosition - ally.GridPosition - 1;
                isEngaged = gap <= 0;
            }

            frontlines.Add(new FrontlineLogStatus
            {
                Position = kvp.Key.ToString(),
                LinePosition = kvp.Value.LinePosition,
                Gap = gap,
                IsEngaged = isEngaged
            });
        }

        // 记录己方将军状态
        foreach (var g in data.Battle.AllyGenerals)
        {
            allyGenerals.Add(new GeneralLogStatus
            {
                Name = g.Name,
                Position = g.Position.ToString(),
                Troops = g.Troops,
                GridPosition = g.GridPosition,
                Status = g.GetStatus(balanceConfig).ToString(),
                DefaultIntent = g.DefaultIntent?.ToString() ?? "None"
            });
        }

        // 记录敌方将军状态（Victor 控制的）
        foreach (var g in data.Battle.EnemyGenerals)
        {
            enemyGenerals.Add(new GeneralLogStatus
            {
                Name = g.Name,
                Position = g.Position.ToString(),
                Troops = g.Troops,
                GridPosition = g.GridPosition,
                Status = g.GetStatus(balanceConfig).ToString(),
                DefaultIntent = g.DefaultIntent?.ToString() ?? "None"
            });
        }

        VictorLogger.Instance.LogBattlefieldStatus(frontlines, allyGenerals, enemyGenerals);
    }

    /// <summary>记录期货持仓状态到日志</summary>
    private void LogFuturesPositions()
    {
        var positions = new List<FuturesPositionStatus>();

        foreach (var contract in ledger.FuturesPositions)
        {
            float currentPrice = data.Market.CurrentPrices[contract.TargetOrder];
            float pnl = contract.CalculatePnL(currentPrice);

            positions.Add(new FuturesPositionStatus
            {
                ContractId = contract.ContractId,
                TargetOrder = contract.TargetOrder,
                Direction = contract.Direction,
                Quantity = contract.Quantity,
                OpenPrice = contract.OpenPrice,
                CurrentPrice = currentPrice,
                Margin = contract.Margin,
                PnL = pnl,
                PnLRatio = contract.Margin > 0 ? pnl / contract.Margin : 0,
                ExpirationTurn = contract.ExpirationTurn,
                TurnsRemaining = contract.ExpirationTurn - data.CurrentTurn
            });
        }

        VictorLogger.Instance.LogFuturesPositions(positions);
    }

    /// <summary>记录陷阱状态到日志</summary>
    private void LogTrapStatus()
    {
        if (memory.ActiveTrap == DeceptionPlanType.None)
        {
            VictorLogger.Instance.LogTrapStatus("无活跃陷阱");
        }
        else
        {
            int duration = data.CurrentTurn - memory.TrapStartTurn;
            string status = $"{memory.ActiveTrap} Phase={memory.TrapPhase} 目标={memory.TrapTargetType} " +
                           $"囤积={memory.TrapHoldings} 持续={duration}回合 开始于回合{memory.TrapStartTurn}";
            VictorLogger.Instance.LogTrapStatus(status);
        }
    }

    /// <summary>获取策略选择调试信息</summary>
    public string GetDebugInfo()
    {
        string info = $"[Viktor AI] Turn {data.CurrentTurn}\n";
        info += $"  Strategy: {LastStrategy}\n";
        info += $"  Cash: ${ledger.Cash:F1}, Debt: ${ledger.Debt:F1}\n";
        info += $"  Holdings: ATK={ledger.Holdings[OrderType.ATK]}, ";
        info += $"DEF={ledger.Holdings[OrderType.DEF]}, ";
        info += $"RET={ledger.Holdings[OrderType.RET]}\n";
        info += $"  Futures: {ledger.FuturesPositions.Count} positions\n";
        info += $"  NetWorth: ${GetVictorNetWorth():F1}\n";
        info += $"  Trap: {memory.ActiveTrap} Phase={memory.TrapPhase}\n";
        info += $"  Player Est: ATK={memory.EstimatedPlayerPosition[OrderType.ATK]:F2}, ";
        info += $"DEF={memory.EstimatedPlayerPosition[OrderType.DEF]:F2}, ";
        info += $"RET={memory.EstimatedPlayerPosition[OrderType.RET]:F2}\n";

        if (LastPlan != null)
        {
            info += $"  Reason: {LastPlan.StrategyReason}\n";

            // 显示具体的现货交易详情
            if (LastPlan.SpotOrders.Count > 0)
            {
                info += $"  Spot Orders ({LastPlan.SpotOrders.Count}):\n";
                foreach (var order in LastPlan.SpotOrders)
                {
                    string action = order.isBuy ? "BUY" : "SELL";
                    info += $"    - {action} {order.type} x{order.quantity}\n";
                }
            }

            // 显示具体的期货交易详情
            if (LastPlan.FuturesOrders.Count > 0)
            {
                info += $"  Futures Open ({LastPlan.FuturesOrders.Count}):\n";
                foreach (var order in LastPlan.FuturesOrders)
                {
                    info += $"    - {order.direction} {order.type} x{order.quantity}\n";
                }
            }

            // 显示期货平仓详情
            if (LastPlan.FuturesCloseOrders.Count > 0)
            {
                info += $"  Futures Close ({LastPlan.FuturesCloseOrders.Count}):\n";
                foreach (var contractId in LastPlan.FuturesCloseOrders)
                {
                    var contract = ledger.FuturesPositions.Find(c => c.ContractId == contractId);
                    if (contract != null)
                    {
                        float pnl = contract.CalculatePnL(data.Market.CurrentPrices[contract.TargetOrder]);
                        info += $"    - Close #{contractId} {contract.Direction} {contract.TargetOrder} PnL:{pnl:F1}\n";
                    }
                    else
                    {
                        info += $"    - Close #{contractId} (已平仓)\n";
                    }
                }
            }

            // 显示将军干涉详情
            if (LastPlan.GeneralOrders.Count > 0)
            {
                info += $"  General Orders ({LastPlan.GeneralOrders.Count}):\n";
                foreach (var kvp in LastPlan.GeneralOrders)
                {
                    var general = data.Battle.EnemyGenerals.Find(g => g.GeneralId == kvp.Key);
                    string generalName = general?.Name ?? kvp.Key;
                    var (reinforce, tamper) = kvp.Value;
                    if (reinforce.HasValue)
                        info += $"    - Reinforce {generalName} with {reinforce.Value}\n";
                    else if (tamper.HasValue)
                        info += $"    - Tamper {generalName} to {tamper.Value}\n";
                    else
                        info += $"    - {generalName}: No intervention\n";
                }
            }

            if (LastPlan.BorrowAmount > 0)
                info += $"  Borrow: ${LastPlan.BorrowAmount:F1}\n";
            if (LastPlan.RepayAmount > 0)
                info += $"  Repay: ${LastPlan.RepayAmount:F1}\n";
        }

        return info;
    }
}
