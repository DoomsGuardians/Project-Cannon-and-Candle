using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 信息面板：市场情报、战场预测
/// </summary>
public class InfoPanel : WindowBase
{
    private TMP_Text txtMarketIntel, txtBattleIntel, txtEnemyIntel;
    private KLineChartView chartAtk, chartDef, chartRet;
    private GameplayManager gameplayManager;
    private UITextConfig textConfig;

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        textConfig = resService.LoadResource<UITextConfig>(ConfigPaths.UI_TEXT);

        var b = gameObject.GetComponent<InfoPanelBinder>();
        if (b != null)
        {
            txtMarketIntel = b.txtMarketIntel;
            txtBattleIntel = b.txtBattleIntel;
            txtEnemyIntel = b.txtEnemyIntel;

            // K线图
            chartAtk = b.chartAtk;
            chartDef = b.chartDef;
            chartRet = b.chartRet;
        }

        InitializeCharts();
    }

    private void InitializeCharts()
    {
        if (chartAtk != null)
        {
            chartAtk.Initialize("ATK");
            chartAtk.SetOrderType(OrderType.ATK);
        }
        if (chartDef != null)
        {
            chartDef.Initialize("DEF");
            chartDef.SetOrderType(OrderType.DEF);
        }
        if (chartRet != null)
        {
            chartRet.Initialize("RET");
            chartRet.SetOrderType(OrderType.RET);
        }
    }

    public override void OnShow()
    {
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnRefresh);
        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    private void RefreshUI()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        if (txtMarketIntel != null)
        {
            var prices = data.Market.CurrentPrices;
            txtMarketIntel.text = $"市场趋势:\nATK: {prices[OrderType.ATK]:F1}\nDEF: {prices[OrderType.DEF]:F1}\nRET: {prices[OrderType.RET]:F1}";
        }

        if (txtBattleIntel != null)
        {
            string header = textConfig?.IntelFrontlineHeader ?? "战线态势:";
            var sb = new System.Text.StringBuilder(header + "\n");
            foreach (var kvp in data.Battle.Frontlines)
            {
                var fl = kvp.Value;
                // 显示格子归属: A=己方, E=敌方, .=中立
                string gridStr = "";
                if (fl.GridOwners != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        gridStr += fl.GridOwners[i] switch
                        {
                            GridOwner.Ally => "A",
                            GridOwner.Enemy => "E",
                            _ => "."
                        };
                    }
                }
                sb.AppendLine($"{kvp.Key}: [{gridStr}] {fl.LinePosition:F1}");
            }
            sb.AppendLine();
            string enemyEstimate = EstimateEnemyReserves(data.Battle.EnemyReserves);
            if (textConfig != null)
                sb.AppendLine(string.Format(textConfig.IntelReservesFormat, data.Battle.CurrentReserves, enemyEstimate));
            else
                sb.AppendLine($"后备役: 己方 {data.Battle.CurrentReserves} / 敌方 {enemyEstimate}");
            txtBattleIntel.text = sb.ToString();
        }

        if (txtEnemyIntel != null)
        {
            var balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
            string header = textConfig?.IntelEnemyHeader ?? "敌方将军:";
            var sb = new System.Text.StringBuilder(header + "\n");
            foreach (var enemy in data.Battle.EnemyGenerals)
            {
                var status = enemy.GetStatus(balanceConfig);
                string statusStr = textConfig?.GetStatusText(status) ?? status switch
                {
                    GeneralStatus.FullStrength => "满编",
                    GeneralStatus.Healthy => "健康",
                    GeneralStatus.Wounded => "受伤",
                    GeneralStatus.Critical => "危急",
                    GeneralStatus.Routed => "溃败",
                    _ => status.ToString()
                };
                sb.AppendLine($"{enemy.Name} [{enemy.Personality}]");
                sb.AppendLine($"  HP:{enemy.Troops} 格:{enemy.GridPosition} {statusStr}");
            }
            txtEnemyIntel.text = sb.ToString();
        }

        // 刷新 K 线图
        RefreshKLineCharts(data);
    }

    private void RefreshKLineCharts(CampaignRuntimeData data)
    {
        if (data?.Market?.KLineHistory == null) return;

        var klineHistory = data.Market.KLineHistory;

        if (chartAtk != null && klineHistory.ContainsKey(OrderType.ATK))
            chartAtk.RefreshData(klineHistory[OrderType.ATK]);

        if (chartDef != null && klineHistory.ContainsKey(OrderType.DEF))
            chartDef.RefreshData(klineHistory[OrderType.DEF]);

        if (chartRet != null && klineHistory.ContainsKey(OrderType.RET))
            chartRet.RefreshData(klineHistory[OrderType.RET]);
    }

    private void OnRefresh(object p1, object p2) => RefreshUI();

    /// <summary>将敌方后备役精确值转换为模糊的数字范围</summary>
    private string EstimateEnemyReserves(int actual)
    {
        if (actual <= 0) return "0";
        if (actual <= 5) return "0-5";
        if (actual <= 10) return "5-15";
        if (actual <= 20) return "10-25";
        return "20+";
    }
}
