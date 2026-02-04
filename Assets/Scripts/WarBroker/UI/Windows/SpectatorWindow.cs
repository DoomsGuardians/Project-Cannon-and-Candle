using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 观战窗口：显示双方维克多对战状态
/// </summary>
public class SpectatorWindow : WindowBase
{
    // UI 引用
    private TMP_Text txtTurn, txtStatus;
    private TMP_Text txtAllyTitle, txtAllyCash, txtAllyNetWorth, txtAllyStrategy, txtAllyHoldings;
    private TMP_Text txtEnemyTitle, txtEnemyCash, txtEnemyNetWorth, txtEnemyStrategy, txtEnemyHoldings;
    private TMP_Text txtFrontlineInfo, txtPriceInfo;
    private Button btnStart, btnPause, btnStep, btnSpeedDown, btnSpeedUp;
    private TMP_Text txtSpeed;
    private TMP_Text txtAllyStats, txtEnemyStats;
    private GameObject resultPanel;
    private TMP_Text txtResult, txtResultDetails;
    private Button btnBackToMenu, btnRestart;

    // 系统引用
    private SpectatorBattleSystem battleSystem;
    private SpectatorModeConfig config;

    // 字符串构建器（避免 GC）
    private StringBuilder sb = new StringBuilder();

    public override void OnAwake()
    {
        base.OnAwake();

        var b = gameObject.GetComponent<SpectatorWindowBinder>();
        if (b != null)
        {
            // 顶部信息
            txtTurn = b.txtTurn;
            txtStatus = b.txtStatus;

            // 己方面板
            txtAllyTitle = b.txtAllyTitle;
            txtAllyCash = b.txtAllyCash;
            txtAllyNetWorth = b.txtAllyNetWorth;
            txtAllyStrategy = b.txtAllyStrategy;
            txtAllyHoldings = b.txtAllyHoldings;

            // 敌方面板
            txtEnemyTitle = b.txtEnemyTitle;
            txtEnemyCash = b.txtEnemyCash;
            txtEnemyNetWorth = b.txtEnemyNetWorth;
            txtEnemyStrategy = b.txtEnemyStrategy;
            txtEnemyHoldings = b.txtEnemyHoldings;

            // 战场信息
            txtFrontlineInfo = b.txtFrontlineInfo;
            txtPriceInfo = b.txtPriceInfo;

            // 控制按钮
            btnStart = b.btnStart;
            btnPause = b.btnPause;
            btnStep = b.btnStep;
            btnSpeedDown = b.btnSpeedDown;
            btnSpeedUp = b.btnSpeedUp;
            txtSpeed = b.txtSpeed;

            // 统计面板
            txtAllyStats = b.txtAllyStats;
            txtEnemyStats = b.txtEnemyStats;

            // 结果面板
            resultPanel = b.resultPanel;
            txtResult = b.txtResult;
            txtResultDetails = b.txtResultDetails;
            btnBackToMenu = b.btnBackToMenu;
            btnRestart = b.btnRestart;
        }

        // 获取系统引用
        var gameMode = GameRoot.Instance.currentGameMode as SpectatorGameMode;
        if (gameMode != null)
        {
            battleSystem = gameMode.BattleSystem;
        }

        config = resService.LoadResource<SpectatorModeConfig>(ConfigPaths.SPECTATOR_MODE);
    }

    public override void OnShow()
    {
        // 重新获取 battleSystem（因为 OnAwake 时可能还未初始化）
        var gameMode = GameRoot.Instance.currentGameMode as SpectatorGameMode;
        if (gameMode != null)
        {
            battleSystem = gameMode.BattleSystem;
        }

        // 绑定按钮事件
        AddButtonListener(btnStart, OnStartClicked);
        AddButtonListener(btnPause, OnPauseClicked);
        AddButtonListener(btnStep, OnStepClicked);
        AddButtonListener(btnSpeedDown, OnSpeedDownClicked);
        AddButtonListener(btnSpeedUp, OnSpeedUpClicked);
        AddButtonListener(btnBackToMenu, OnBackToMenuClicked);
        AddButtonListener(btnRestart, OnRestartClicked);

        // 订阅事件
        if (battleSystem != null)
        {
            battleSystem.OnTurnStarted += OnTurnStarted;
            battleSystem.OnTurnEnded += OnTurnEnded;
            battleSystem.OnBattleEnded += OnBattleEnded;
        }

        // 隐藏结果面板
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // 初始化 UI
        RefreshUI();
        UpdateControlButtons();
    }

    public override void OnHide()
    {
        // 取消订阅事件
        if (battleSystem != null)
        {
            battleSystem.OnTurnStarted -= OnTurnStarted;
            battleSystem.OnTurnEnded -= OnTurnEnded;
            battleSystem.OnBattleEnded -= OnBattleEnded;
        }
    }

    public override void OnUpdate()
    {
        // 实时刷新 UI
        if (battleSystem != null && battleSystem.IsRunning)
        {
            RefreshUI();
        }
    }

    #region 事件回调

    private void OnTurnStarted(int turn)
    {
        RefreshUI();
    }

    private void OnTurnEnded(int turn, VictorTurnPlan allyPlan, VictorTurnPlan enemyPlan)
    {
        RefreshUI();
        RefreshStrategyDisplay(allyPlan, enemyPlan);
    }

    private void OnBattleEnded(SpectatorBattleStats stats)
    {
        ShowResult(stats);
        UpdateControlButtons();
    }

    #endregion

    #region UI 刷新

    private void RefreshUI()
    {
        if (battleSystem == null || battleSystem.Data == null)
            return;

        var data = battleSystem.Data;
        var stats = battleSystem.Stats;

        // 顶部信息
        if (txtTurn != null)
            txtTurn.text = $"回合 {data.CurrentTurn} / {config?.MaxTurns ?? 30}";

        if (txtStatus != null)
        {
            string status = battleSystem.IsPaused ? "已暂停" :
                           battleSystem.IsRunning ? "进行中" : "未开始";
            txtStatus.text = status;
        }

        // 速度显示
        if (txtSpeed != null)
            txtSpeed.text = $"{battleSystem.CurrentSpeed:F1}x";

        // 己方状态
        RefreshAllyPanel(data, stats);

        // 敌方状态
        RefreshEnemyPanel(data, stats);

        // 战场信息
        RefreshBattlefieldInfo(data);

        // 价格信息
        RefreshPriceInfo(data);

        // 统计信息
        RefreshStatsPanel(stats);
    }

    private void RefreshAllyPanel(CampaignRuntimeData data, SpectatorBattleStats stats)
    {
        if (txtAllyTitle != null && config?.AllyVictorProfile != null)
            txtAllyTitle.text = $"己方: {config.AllyVictorProfile.name}";

        // 使用 AllyLedger
        var allyLedger = battleSystem.AllyLedger;
        if (allyLedger == null) return;

        if (txtAllyCash != null)
            txtAllyCash.text = $"现金: {allyLedger.Cash:F0}";

        if (txtAllyNetWorth != null)
        {
            float netWorth = allyLedger.GetNetWorth(data.Market);
            txtAllyNetWorth.text = $"净资产: {netWorth:F0}";
        }

        if (txtAllyStrategy != null && battleSystem.AllyVictor != null)
            txtAllyStrategy.text = $"策略: {GetStrategyName(battleSystem.AllyVictor.LastStrategy)}";

        if (txtAllyHoldings != null)
        {
            sb.Clear();
            sb.Append("持仓: ");
            foreach (var kv in allyLedger.Holdings)
            {
                if (kv.Value > 0)
                    sb.Append($"{kv.Key}:{kv.Value} ");
            }
            txtAllyHoldings.text = sb.ToString();
        }
    }

    private void RefreshEnemyPanel(CampaignRuntimeData data, SpectatorBattleStats stats)
    {
        if (txtEnemyTitle != null && config?.EnemyVictorProfile != null)
            txtEnemyTitle.text = $"敌方: {config.EnemyVictorProfile.name}";

        // 使用 EnemyLedger
        var enemyLedger = battleSystem.EnemyLedger;
        if (enemyLedger == null) return;

        if (txtEnemyCash != null)
            txtEnemyCash.text = $"现金: {enemyLedger.Cash:F0}";

        if (txtEnemyNetWorth != null)
        {
            float netWorth = enemyLedger.GetNetWorth(data.Market);
            txtEnemyNetWorth.text = $"净资产: {netWorth:F0}";
        }

        if (txtEnemyStrategy != null && battleSystem.EnemyVictor != null)
            txtEnemyStrategy.text = $"策略: {GetStrategyName(battleSystem.EnemyVictor.LastStrategy)}";

        if (txtEnemyHoldings != null)
        {
            sb.Clear();
            sb.Append("持仓: ");
            foreach (var kv in enemyLedger.Holdings)
            {
                if (kv.Value > 0)
                    sb.Append($"{kv.Key}:{kv.Value} ");
            }
            txtEnemyHoldings.text = sb.ToString();
        }
    }

    private void RefreshBattlefieldInfo(CampaignRuntimeData data)
    {
        if (txtFrontlineInfo == null) return;

        sb.Clear();
        sb.AppendLine("战线状态:");
        foreach (var kv in data.Battle.Frontlines)
        {
            var fl = kv.Value;
            sb.AppendLine($"  {kv.Key}: 位置 {fl.LinePosition:F1}");
        }

        // 将军状态
        sb.AppendLine("\n己方将军:");
        foreach (var g in data.Battle.AllyGenerals)
        {
            var balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
            var status = g.GetStatus(balanceConfig);
            sb.AppendLine($"  {g.Name}: HP {g.Troops} [{status}]");
        }

        sb.AppendLine("\n敌方将军:");
        foreach (var g in data.Battle.EnemyGenerals)
        {
            var balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
            var status = g.GetStatus(balanceConfig);
            sb.AppendLine($"  {g.Name}: HP {g.Troops} [{status}]");
        }

        txtFrontlineInfo.text = sb.ToString();
    }

    private void RefreshPriceInfo(CampaignRuntimeData data)
    {
        if (txtPriceInfo == null) return;

        sb.Clear();
        sb.AppendLine("当前价格:");
        foreach (var kv in data.Market.CurrentPrices)
        {
            sb.AppendLine($"  {kv.Key}: {kv.Value:F1}");
        }
        txtPriceInfo.text = sb.ToString();
    }

    private void RefreshStatsPanel(SpectatorBattleStats stats)
    {
        if (txtAllyStats != null)
        {
            sb.Clear();
            sb.AppendLine("己方统计:");
            sb.AppendLine($"  造成伤害: {stats.AllyStats.TotalDamageDealt}");
            sb.AppendLine($"  承受伤害: {stats.AllyStats.TotalDamageTaken}");
            sb.AppendLine($"  交易次数: {stats.AllyStats.SpotTradeCount}");
            sb.AppendLine($"  击杀将军: {stats.AllyStats.GeneralsKilled}");
            sb.AppendLine($"  损失将军: {stats.AllyStats.GeneralsLost}");
            txtAllyStats.text = sb.ToString();
        }

        if (txtEnemyStats != null)
        {
            sb.Clear();
            sb.AppendLine("敌方统计:");
            sb.AppendLine($"  造成伤害: {stats.EnemyStats.TotalDamageDealt}");
            sb.AppendLine($"  承受伤害: {stats.EnemyStats.TotalDamageTaken}");
            sb.AppendLine($"  交易次数: {stats.EnemyStats.SpotTradeCount}");
            sb.AppendLine($"  击杀将军: {stats.EnemyStats.GeneralsKilled}");
            sb.AppendLine($"  损失将军: {stats.EnemyStats.GeneralsLost}");
            txtEnemyStats.text = sb.ToString();
        }
    }

    private void RefreshStrategyDisplay(VictorTurnPlan allyPlan, VictorTurnPlan enemyPlan)
    {
        if (txtAllyStrategy != null && allyPlan != null)
            txtAllyStrategy.text = $"策略: {GetStrategyName(allyPlan.MainStrategy)}";

        if (txtEnemyStrategy != null && enemyPlan != null)
            txtEnemyStrategy.text = $"策略: {GetStrategyName(enemyPlan.MainStrategy)}";
    }

    private void ShowResult(SpectatorBattleStats stats)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (txtResult != null)
            txtResult.text = stats.AllyWon ? "己方获胜!" : "敌方获胜!";

        if (txtResultDetails != null)
        {
            sb.Clear();
            sb.AppendLine(stats.GetResultDescription());
            sb.AppendLine();
            sb.AppendLine($"总回合数: {stats.TotalTurns}");
            sb.AppendLine();
            sb.AppendLine("=== 己方最终状态 ===");
            sb.AppendLine($"净资产: {stats.AllyStats.FinalNetWorth:F0}");
            sb.AppendLine($"盈亏: {(stats.AllyStats.TotalProfit > 0 ? "+" : "")}{stats.AllyStats.TotalProfit - stats.AllyStats.TotalLoss:F0}");
            sb.AppendLine();
            sb.AppendLine("=== 敌方最终状态 ===");
            sb.AppendLine($"净资产: {stats.EnemyStats.FinalNetWorth:F0}");
            sb.AppendLine($"盈亏: {(stats.EnemyStats.TotalProfit > 0 ? "+" : "")}{stats.EnemyStats.TotalProfit - stats.EnemyStats.TotalLoss:F0}");
            sb.AppendLine();
            sb.AppendLine("=== 策略使用统计 ===");
            sb.AppendLine("己方:");
            foreach (var kv in stats.AllyStats.StrategiesUsed)
            {
                if (kv.Value > 0)
                    sb.AppendLine($"  {GetStrategyName(kv.Key)}: {kv.Value}次");
            }
            sb.AppendLine("敌方:");
            foreach (var kv in stats.EnemyStats.StrategiesUsed)
            {
                if (kv.Value > 0)
                    sb.AppendLine($"  {GetStrategyName(kv.Key)}: {kv.Value}次");
            }

            txtResultDetails.text = sb.ToString();
        }
    }

    private void UpdateControlButtons()
    {
        bool isRunning = battleSystem?.IsRunning ?? false;
        bool isPaused = battleSystem?.IsPaused ?? false;
        bool isEnded = battleSystem?.Stats?.EndReason != SpectatorGameEndReason.InProgress;

        if (btnStart != null)
            btnStart.gameObject.SetActive(!isRunning || isEnded);

        if (btnPause != null)
        {
            btnPause.gameObject.SetActive(isRunning && !isEnded);
            var pauseText = btnPause.GetComponentInChildren<TMP_Text>();
            if (pauseText != null)
                pauseText.text = isPaused ? "继续" : "暂停";
        }

        if (btnStep != null)
            btnStep.interactable = isRunning && !isEnded;
    }

    #endregion

    #region 按钮回调

    private void OnStartClicked()
    {
        Debug.Log($"[SpectatorWindow] OnStartClicked - battleSystem: {(battleSystem != null ? "exists" : "null")}");

        if (battleSystem == null)
        {
            // 再次尝试获取
            var gameMode = GameRoot.Instance.currentGameMode as SpectatorGameMode;
            Debug.Log($"[SpectatorWindow] gameMode: {(gameMode != null ? "exists" : "null")}");
            if (gameMode != null)
            {
                battleSystem = gameMode.BattleSystem;
                Debug.Log($"[SpectatorWindow] BattleSystem from gameMode: {(battleSystem != null ? "exists" : "null")}");
            }
        }

        if (battleSystem != null && !battleSystem.IsRunning)
        {
            Debug.Log("[SpectatorWindow] Starting battle...");
            battleSystem.StartBattle();
            UpdateControlButtons();
        }
        else if (battleSystem == null)
        {
            Debug.LogError("[SpectatorWindow] battleSystem is still null!");
        }
        else
        {
            Debug.Log($"[SpectatorWindow] Battle already running: {battleSystem.IsRunning}");
        }
    }

    private void OnPauseClicked()
    {
        if (battleSystem != null)
        {
            battleSystem.TogglePause();
            UpdateControlButtons();
        }
    }

    private void OnStepClicked()
    {
        if (battleSystem != null)
        {
            // 暂停后单步执行
            if (!battleSystem.IsPaused)
                battleSystem.PauseBattle();

            battleSystem.StepOneTurn();
            RefreshUI();
        }
    }

    private void OnSpeedDownClicked()
    {
        if (battleSystem != null)
        {
            float newSpeed = Mathf.Max(0.1f, battleSystem.CurrentSpeed - 0.5f);
            battleSystem.SetSpeed(newSpeed);
            RefreshUI();
        }
    }

    private void OnSpeedUpClicked()
    {
        if (battleSystem != null)
        {
            float newSpeed = Mathf.Min(5.0f, battleSystem.CurrentSpeed + 0.5f);
            battleSystem.SetSpeed(newSpeed);
            RefreshUI();
        }
    }

    private void OnBackToMenuClicked()
    {
        // 返回主菜单
        GameRoot.Instance.ChangeGameMode(GameMode.MainMenu);
    }

    private void OnRestartClicked()
    {
        // 重新开始观战
        GameRoot.Instance.ChangeGameMode(GameMode.Spectator);
    }

    #endregion

    #region 辅助方法

    private string GetStrategyName(VictorStrategy strategy)
    {
        return strategy switch
        {
            VictorStrategy.MilitaryFocus => "军事优先",
            VictorStrategy.TrendFollowing => "趋势跟随",
            VictorStrategy.CounterStrike => "逆势狙击",
            VictorStrategy.DeceptionPlay => "设局欺骗",
            VictorStrategy.Harvest => "收割获利",
            _ => "未知"
        };
    }

    #endregion
}
