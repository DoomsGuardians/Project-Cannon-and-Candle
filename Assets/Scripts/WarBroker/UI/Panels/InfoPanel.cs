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

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();

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
            var sb = new System.Text.StringBuilder("战线态势:\n");
            foreach (var kvp in data.Battle.Frontlines)
                sb.AppendLine($"{kvp.Key}: 位置 {kvp.Value.LinePosition}/5");
            txtBattleIntel.text = sb.ToString();
        }

        if (txtEnemyIntel != null)
        {
            var sb = new System.Text.StringBuilder("敌方将军:\n");
            foreach (var enemy in data.Battle.EnemyGenerals)
                sb.AppendLine($"{enemy.Name} [{enemy.Personality}] 兵力≈{enemy.Troops}");
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
}
