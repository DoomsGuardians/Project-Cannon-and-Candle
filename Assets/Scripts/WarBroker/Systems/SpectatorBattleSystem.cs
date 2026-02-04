using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 观战战斗系统：管理两个维克多 AI 对战
/// 双方各自拥有独立的账本，共享战场和市场数据
/// </summary>
public class SpectatorBattleSystem
{
    // 依赖（从 GameRoot 获取）
    private EventService eventService;
    private ResService resService;
    private MarketSystem marketSystem;
    private BattleSystem battleSystem;

    // 配置
    private SpectatorModeConfig config;
    private GameBalanceConfig balanceConfig;
    private OrderConfig orderConfig;
    private CampaignConfig campaignConfig;

    // 共享数据（战场、市场）
    private CampaignRuntimeData sharedData;

    // 双方独立账本
    private VictorLedger allyLedger;
    private VictorLedger enemyLedger;
    private VictorMemory allyMemory;
    private VictorMemory enemyMemory;

    // 双方 AI 和意图系统
    private VictorAISystem allyVictor;
    private VictorAISystem enemyVictor;
    private IntentSystem allyIntentSystem;
    private IntentSystem enemyIntentSystem;

    // 统计
    private SpectatorBattleStats stats;

    // 将军状态跟踪
    private HashSet<string> allyRoutedGenerals = new HashSet<string>();
    private HashSet<string> enemyRoutedGenerals = new HashSet<string>();

    // 状态
    private bool isRunning;
    private bool isPaused;
    private float turnTimer;
    private float currentSpeed = 1.0f;

    public int CurrentTurn => sharedData?.CurrentTurn ?? 0;
    public bool IsRunning => isRunning;
    public bool IsPaused => isPaused;
    public float CurrentSpeed => currentSpeed;
    public SpectatorBattleStats Stats => stats;
    public CampaignRuntimeData Data => sharedData;
    public VictorLedger AllyLedger => allyLedger;
    public VictorLedger EnemyLedger => enemyLedger;
    public VictorAISystem AllyVictor => allyVictor;
    public VictorAISystem EnemyVictor => enemyVictor;

    public event Action<int> OnTurnStarted;
    public event Action<int, VictorTurnPlan, VictorTurnPlan> OnTurnEnded;
    public event Action<SpectatorBattleStats> OnBattleEnded;

    public void Initialize(SpectatorModeConfig config, string campaignId)
    {
        this.config = config;

        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;
        marketSystem = GameRoot.Instance.marketSystem;
        battleSystem = GameRoot.Instance.battleSystem;

        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        orderConfig = resService.LoadResource<OrderConfig>(ConfigPaths.ORDER_CONFIG);
        campaignConfig = resService.LoadResource<CampaignConfig>(ConfigPaths.CAMPAIGN_PREFIX + campaignId);

        if (campaignConfig == null)
        {
            Debug.LogError($"[SpectatorBattleSystem] 战役配置加载失败: {campaignId}");
            return;
        }

        stats = new SpectatorBattleStats();

        // 初始化共享数据
        sharedData = new CampaignRuntimeData();
        sharedData.InitFromConfig(campaignConfig, orderConfig);

        // 初始化双方独立账本
        allyLedger = new VictorLedger();
        allyLedger.Init(config.AllyStartingCash);
        allyMemory = new VictorMemory();
        allyMemory.Init();

        enemyLedger = new VictorLedger();
        enemyLedger.Init(config.EnemyStartingCash);
        enemyMemory = new VictorMemory();
        enemyMemory.Init();

        // 设置 sharedData 的账本为己方（用于 marketSystem 等）
        sharedData.VictorLedger.Init(config.AllyStartingCash);

        marketSystem.SetRuntimeData(sharedData);
        battleSystem.SetRuntimeData(sharedData);

        // 初始化双方 AI
        InitializeVictorAIs();

        currentSpeed = config.TurnSpeed;
        isRunning = false;
        isPaused = false;

        Debug.Log("[SpectatorBattleSystem] 初始化完成");
    }

    private void InitializeVictorAIs()
    {
        // 己方意图系统
        allyIntentSystem = new IntentSystem();
        allyIntentSystem.Init(balanceConfig);
        allyIntentSystem.SetCampaignData(sharedData);

        // 敌方意图系统
        enemyIntentSystem = new IntentSystem();
        enemyIntentSystem.Init(balanceConfig);
        enemyIntentSystem.SetCampaignData(sharedData);

        // 创建己方数据包装（使用己方账本）
        var allyDataWrapper = CreateDataWrapper(allyLedger, allyMemory);

        // 己方维克多
        allyVictor = new VictorAISystem();
        var allyProfile = config.AllyVictorProfile ?? ScriptableObject.CreateInstance<VictorProfile>();
        allyVictor.Init(allyProfile, allyDataWrapper, marketSystem, allyIntentSystem, balanceConfig, orderConfig);

        // 创建敌方数据包装（使用敌方账本）
        var enemyDataWrapper = CreateDataWrapper(enemyLedger, enemyMemory);

        // 敌方维克多
        enemyVictor = new VictorAISystem();
        var enemyProfile = config.EnemyVictorProfile ?? ScriptableObject.CreateInstance<VictorProfile>();
        enemyVictor.Init(enemyProfile, enemyDataWrapper, marketSystem, enemyIntentSystem, balanceConfig, orderConfig);
    }

    /// <summary>
    /// 创建数据包装器，复用 sharedData 的战场和市场，但使用独立账本
    /// </summary>
    private CampaignRuntimeData CreateDataWrapper(VictorLedger ledger, VictorMemory memory)
    {
        // 先初始化以获取 Config（Config 是 private set）
        var wrapper = new CampaignRuntimeData();
        wrapper.InitFromConfig(campaignConfig, orderConfig);

        // 替换为共享的战场和市场数据
        wrapper.Market = sharedData.Market;
        wrapper.Battle = sharedData.Battle;

        // 使用独立账本
        wrapper.VictorLedger = ledger;
        wrapper.VictorMemory = memory;

        return wrapper;
    }

    public void StartBattle()
    {
        if (isRunning)
        {
            Debug.LogWarning("[SpectatorBattleSystem] Battle already running");
            return;
        }
        if (sharedData == null)
        {
            Debug.LogError("[SpectatorBattleSystem] Data is null");
            return;
        }

        isRunning = true;
        isPaused = false;
        turnTimer = 0f;

        Debug.Log("[SpectatorBattleSystem] Battle started");
        ExecuteTurn();
    }

    public void PauseBattle() { isPaused = true; }
    public void ResumeBattle() { isPaused = false; }
    public void TogglePause() { isPaused = !isPaused; }
    public void SetSpeed(float speed) { currentSpeed = Mathf.Clamp(speed, 0.1f, 5.0f); }

    public void StepOneTurn()
    {
        if (!isRunning || stats.EndReason != SpectatorGameEndReason.InProgress) return;
        ExecuteTurn();
    }

    public void OnUpdate()
    {
        if (!isRunning || isPaused || !config.AutoAdvance) return;
        if (stats.EndReason != SpectatorGameEndReason.InProgress) return;

        turnTimer += Time.deltaTime * currentSpeed;
        if (turnTimer >= config.TurnInterval)
        {
            turnTimer = 0f;
            ExecuteTurn();
        }
    }

    private void ExecuteTurn()
    {
        if (sharedData == null) return;

        if (sharedData.CurrentTurn > config.MaxTurns)
        {
            EndBattle(SpectatorGameEndReason.MaxTurnsReached);
            return;
        }

        OnTurnStarted?.Invoke(sharedData.CurrentTurn);

        // === 阶段 I: 回合开始 ===
        sharedData.CurrentPhase = TurnPhase.TurnStart;
        ApplyInterestAndStorage();
        GenerateAllIntents();

        var demandModifiers = CalculateDemandModifiers();
        marketSystem.UpdatePrices(demandModifiers);
        marketSystem.InitializeKLineForTurn();

        // === 阶段 II & III: 双方 AI 执行 ===
        sharedData.CurrentPhase = TurnPhase.MarketPhase;
        var allyPlan = ExecuteAllyVictor();
        var enemyPlan = ExecuteEnemyVictor();

        // === 阶段 IV: 战斗结算 ===
        sharedData.CurrentPhase = TurnPhase.BattlePhase;
        battleSystem.ProcessRespawns();

        var victorOrders = new Dictionary<string, OrderType>();
        foreach (var enemy in sharedData.Battle.EnemyGenerals)
            victorOrders[enemy.GeneralId] = enemy.FinalIntent ?? enemy.DefaultIntent ?? OrderType.DEF;

        var battleResults = battleSystem.ResolveBattles(victorOrders);
        UpdateBattleStats(battleResults);

        // === 阶段 V: 回合结算 ===
        sharedData.CurrentPhase = TurnPhase.SettlementPhase;
        marketSystem.FinalizeKLineForTurn();
        battleSystem.ApplyReinforcements();
        SettleExpiredFutures();
        UpdateOccupationStatus();
        RecordTurnHistory();

        var endReason = CheckVictoryConditions();
        if (endReason != SpectatorGameEndReason.InProgress)
        {
            EndBattle(endReason);
            return;
        }

        OnTurnEnded?.Invoke(sharedData.CurrentTurn, allyPlan, enemyPlan);
        sharedData.CurrentTurn++;
        stats.TotalTurns = sharedData.CurrentTurn - 1;
    }

    private void ApplyInterestAndStorage()
    {
        // 己方
        if (allyLedger.Debt > 0)
            allyLedger.Cash -= allyLedger.Debt * balanceConfig.BankInterestRate;
        allyLedger.Cash -= allyLedger.TotalHoldings * balanceConfig.StorageCostPerUnit;

        // 敌方
        if (enemyLedger.Debt > 0)
            enemyLedger.Cash -= enemyLedger.Debt * balanceConfig.BankInterestRate;
        enemyLedger.Cash -= enemyLedger.TotalHoldings * balanceConfig.StorageCostPerUnit;
    }

    private void SettleExpiredFutures()
    {
        int turn = sharedData.CurrentTurn;

        for (int i = allyLedger.FuturesPositions.Count - 1; i >= 0; i--)
        {
            var c = allyLedger.FuturesPositions[i];
            if (c.ExpirationTurn <= turn)
            {
                allyLedger.Cash += c.Margin + c.CalculatePnL(sharedData.Market.CurrentPrices[c.TargetOrder]);
                allyLedger.FuturesPositions.RemoveAt(i);
            }
        }

        for (int i = enemyLedger.FuturesPositions.Count - 1; i >= 0; i--)
        {
            var c = enemyLedger.FuturesPositions[i];
            if (c.ExpirationTurn <= turn)
            {
                enemyLedger.Cash += c.Margin + c.CalculatePnL(sharedData.Market.CurrentPrices[c.TargetOrder]);
                enemyLedger.FuturesPositions.RemoveAt(i);
            }
        }
    }

    private void GenerateAllIntents()
    {
        foreach (var g in sharedData.Battle.AllyGenerals)
        {
            if (g.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;
            g.DefaultIntent = allyIntentSystem.GenerateDefaultIntent(g);
            g.FinalIntent = g.DefaultIntent;
            g.IntentSource = IntentSource.Default;
        }

        foreach (var g in sharedData.Battle.EnemyGenerals)
        {
            if (g.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;
            g.DefaultIntent = enemyIntentSystem.GenerateDefaultIntent(g);
            g.FinalIntent = g.DefaultIntent;
            g.IntentSource = IntentSource.Default;
        }
    }

    private Dictionary<OrderType, float> CalculateDemandModifiers()
    {
        var m = new Dictionary<OrderType, float> { { OrderType.ATK, 1f }, { OrderType.DEF, 1f }, { OrderType.RET, 1f } };

        foreach (var g in sharedData.Battle.AllyGenerals)
        {
            var s = g.GetStatus(balanceConfig);
            if (s == GeneralStatus.Wounded) { m[OrderType.DEF] += 0.2f; m[OrderType.RET] += 0.1f; }
            else if (s == GeneralStatus.Critical) { m[OrderType.DEF] += 0.5f; m[OrderType.RET] += 0.3f; }
        }

        foreach (var fl in sharedData.Battle.Frontlines.Values)
        {
            if (fl.LinePosition <= 2.5f) m[OrderType.DEF] += 0.15f;
            else if (fl.LinePosition >= 3.5f) m[OrderType.ATK] += 0.1f;
        }

        return m;
    }

    private VictorTurnPlan ExecuteAllyVictor()
    {
        var plan = allyVictor.ExecuteTurn();
        stats.AllyStats.StrategiesUsed[plan.MainStrategy]++;

        foreach (var kv in plan.GeneralOrders)
        {
            var g = sharedData.Battle.AllyGenerals.Find(x => x.GeneralId == kv.Key);
            if (g != null)
            {
                if (kv.Value.tamper.HasValue)
                {
                    g.FinalIntent = kv.Value.tamper.Value;
                    g.IntentSource = IntentSource.Overridden;
                    stats.AllyStats.TamperCount++;
                }
                else if (kv.Value.reinforce.HasValue)
                {
                    g.IntentSource = IntentSource.Reinforced;
                    stats.AllyStats.ReinforceCount++;
                }
                g.AssignedOrder = g.FinalIntent;
                if (g.FinalIntent.HasValue) stats.AllyStats.OrdersUsed[g.FinalIntent.Value]++;
            }
        }

        foreach (var g in sharedData.Battle.AllyGenerals)
        {
            if (g.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;
            g.AssignedOrder ??= g.FinalIntent ?? g.DefaultIntent ?? OrderType.DEF;
        }

        stats.AllyStats.SpotTradeCount += plan.SpotOrders.Count;
        stats.AllyStats.FuturesTradeCount += plan.FuturesOrders.Count;
        if (plan.BorrowAmount > 0) { stats.AllyStats.BorrowCount++; stats.AllyStats.TotalBorrowed += plan.BorrowAmount; }

        return plan;
    }

    private VictorTurnPlan ExecuteEnemyVictor()
    {
        var plan = enemyVictor.ExecuteTurn();
        stats.EnemyStats.StrategiesUsed[plan.MainStrategy]++;

        foreach (var kv in plan.GeneralOrders)
        {
            var g = sharedData.Battle.EnemyGenerals.Find(x => x.GeneralId == kv.Key);
            if (g != null)
            {
                if (kv.Value.tamper.HasValue)
                {
                    g.FinalIntent = kv.Value.tamper.Value;
                    g.IntentSource = IntentSource.Overridden;
                    stats.EnemyStats.TamperCount++;
                }
                else if (kv.Value.reinforce.HasValue)
                {
                    g.IntentSource = IntentSource.Reinforced;
                    stats.EnemyStats.ReinforceCount++;
                }
                if (g.FinalIntent.HasValue) stats.EnemyStats.OrdersUsed[g.FinalIntent.Value]++;
            }
        }

        stats.EnemyStats.SpotTradeCount += plan.SpotOrders.Count;
        stats.EnemyStats.FuturesTradeCount += plan.FuturesOrders.Count;
        if (plan.BorrowAmount > 0) { stats.EnemyStats.BorrowCount++; stats.EnemyStats.TotalBorrowed += plan.BorrowAmount; }

        return plan;
    }

    private void UpdateOccupationStatus()
    {
        foreach (var fl in sharedData.Battle.Frontlines.Values)
        {
            if (fl.IsAtEnemyBase) { fl.TurnsAtEnemyBase++; fl.TurnsAtAllyBase = 0; }
            else if (fl.IsAtAllyBase) { fl.TurnsAtAllyBase++; fl.TurnsAtEnemyBase = 0; }
            else { fl.TurnsAtEnemyBase = 0; fl.TurnsAtAllyBase = 0; }
        }
    }

    private void UpdateBattleStats(List<BattleResult> results)
    {
        foreach (var r in results)
        {
            int ad = Mathf.Abs(Mathf.Min(0, r.AllyTroopChange));
            int ed = Mathf.Abs(Mathf.Min(0, r.EnemyTroopChange));
            stats.AllyStats.TotalDamageDealt += ed;
            stats.AllyStats.TotalDamageTaken += ad;
            stats.EnemyStats.TotalDamageDealt += ad;
            stats.EnemyStats.TotalDamageTaken += ed;
        }

        foreach (var g in sharedData.Battle.AllyGenerals)
        {
            if (g.GetStatus(balanceConfig) == GeneralStatus.Routed && !allyRoutedGenerals.Contains(g.GeneralId))
            {
                allyRoutedGenerals.Add(g.GeneralId);
                stats.AllyStats.GeneralsLost++;
                stats.EnemyStats.GeneralsKilled++;
            }
        }

        foreach (var g in sharedData.Battle.EnemyGenerals)
        {
            if (g.GetStatus(balanceConfig) == GeneralStatus.Routed && !enemyRoutedGenerals.Contains(g.GeneralId))
            {
                enemyRoutedGenerals.Add(g.GeneralId);
                stats.EnemyStats.GeneralsLost++;
                stats.AllyStats.GeneralsKilled++;
            }
        }
    }

    private void RecordTurnHistory()
    {
        float avg = 0f; int c = 0;
        foreach (var fl in sharedData.Battle.Frontlines.Values) { avg += fl.LinePosition; c++; }
        if (c > 0) avg /= c;
        stats.FrontlineHistory.Add(avg);

        float anw = allyLedger.GetNetWorth(sharedData.Market);
        float enw = enemyLedger.GetNetWorth(sharedData.Market);
        stats.AllyNetWorthHistory.Add(anw);
        stats.EnemyNetWorthHistory.Add(enw);
        stats.AllyStats.MaxNetWorth = Mathf.Max(stats.AllyStats.MaxNetWorth, anw);
        stats.AllyStats.MinNetWorth = Mathf.Min(stats.AllyStats.MinNetWorth, anw);
        stats.EnemyStats.MaxNetWorth = Mathf.Max(stats.EnemyStats.MaxNetWorth, enw);
        stats.EnemyStats.MinNetWorth = Mathf.Min(stats.EnemyStats.MinNetWorth, enw);

        foreach (OrderType t in Enum.GetValues(typeof(OrderType)))
            stats.PriceHistory[t].Add(sharedData.Market.CurrentPrices[t]);
    }

    private SpectatorGameEndReason CheckVictoryConditions()
    {
        foreach (var fl in sharedData.Battle.Frontlines.Values)
        {
            if (fl.TurnsAtEnemyBase >= 1) return SpectatorGameEndReason.EnemyBaseOccupied;
            if (fl.TurnsAtAllyBase >= 1) return SpectatorGameEndReason.AllyBaseOccupied;
        }

        if (allyLedger.GetNetWorth(sharedData.Market) < 0) return SpectatorGameEndReason.AllyBankrupt;
        if (enemyLedger.GetNetWorth(sharedData.Market) < 0) return SpectatorGameEndReason.EnemyBankrupt;

        bool ah = false, eh = false;
        foreach (var g in sharedData.Battle.AllyGenerals)
            if (g.GetStatus(balanceConfig) != GeneralStatus.Routed) { ah = true; break; }
        foreach (var g in sharedData.Battle.EnemyGenerals)
            if (g.GetStatus(balanceConfig) != GeneralStatus.Routed) { eh = true; break; }

        if (!ah) return SpectatorGameEndReason.AllyArmyDestroyed;
        if (!eh) return SpectatorGameEndReason.EnemyArmyDestroyed;
        if (sharedData.CurrentTurn >= config.MaxTurns) return SpectatorGameEndReason.MaxTurnsReached;

        return SpectatorGameEndReason.InProgress;
    }

    private void EndBattle(SpectatorGameEndReason reason)
    {
        isRunning = false;
        stats.EndReason = reason;
        stats.TotalTurns = sharedData.CurrentTurn;

        stats.AllyStats.FinalCash = allyLedger.Cash;
        stats.AllyStats.FinalNetWorth = allyLedger.GetNetWorth(sharedData.Market);
        stats.EnemyStats.FinalCash = enemyLedger.Cash;
        stats.EnemyStats.FinalNetWorth = enemyLedger.GetNetWorth(sharedData.Market);

        stats.AllyWon = reason switch
        {
            SpectatorGameEndReason.EnemyBaseOccupied or
            SpectatorGameEndReason.EnemyBankrupt or
            SpectatorGameEndReason.EnemyArmyDestroyed => true,
            SpectatorGameEndReason.MaxTurnsReached => stats.AllyStats.FinalNetWorth > stats.EnemyStats.FinalNetWorth,
            _ => false
        };

        stats.AllyStats.TotalProfit = Mathf.Max(0, stats.AllyStats.FinalNetWorth - config.AllyStartingCash);
        stats.AllyStats.TotalLoss = Mathf.Max(0, config.AllyStartingCash - stats.AllyStats.FinalNetWorth);
        stats.EnemyStats.TotalProfit = Mathf.Max(0, stats.EnemyStats.FinalNetWorth - config.EnemyStartingCash);
        stats.EnemyStats.TotalLoss = Mathf.Max(0, config.EnemyStartingCash - stats.EnemyStats.FinalNetWorth);

        Debug.Log($"[SpectatorBattleSystem] Battle ended: {stats.GetResultDescription()}");
        OnBattleEnded?.Invoke(stats);
    }

    public void Cleanup()
    {
        isRunning = false;
        isPaused = false;
        OnTurnStarted = null;
        OnTurnEnded = null;
        OnBattleEnded = null;
    }
}
