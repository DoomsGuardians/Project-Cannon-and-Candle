using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 情报面板：市场情报、战场预测
/// </summary>
public class IntelPanel : WindowBase
{
    private Text txtMarketIntel, txtBattleIntel, txtEnemyIntel;
    private GameplayManager gameplayManager;

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();

        var b = gameObject.GetComponent<IntelPanelBinder>();
        if (b != null)
        {
            txtMarketIntel = b.txtMarketIntel;
            txtBattleIntel = b.txtBattleIntel;
            txtEnemyIntel = b.txtEnemyIntel;
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
    }

    private void OnRefresh(object p1, object p2) => RefreshUI();
}
