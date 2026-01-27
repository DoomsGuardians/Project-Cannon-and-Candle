using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 历史面板：回合历史记录
/// </summary>
public class HistoryPanel : WindowBase
{
    private Text txtHistory;
    private ScrollRect scrollRect;
    private GameplayManager gameplayManager;

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();

        var b = gameObject.GetComponent<HistoryPanelBinder>();
        if (b != null)
        {
            txtHistory = b.txtHistory;
            scrollRect = b.scrollRect;
        }
    }

    public override void OnShow()
    {
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
        if (data == null || txtHistory == null) return;

        var sb = new System.Text.StringBuilder();
        for (int i = data.TurnHistory.Count - 1; i >= 0; i--)
        {
            var record = data.TurnHistory[i];
            sb.AppendLine($"=== 回合 {record.TurnNumber} ===");
            sb.AppendLine($"净资产: {record.PlayerNetWorth:F0}");

            sb.Append("指令: ");
            foreach (var kvp in record.OrderAssignments)
                sb.Append($"{kvp.Key}→{kvp.Value} ");
            sb.AppendLine();

            sb.Append("价格: ");
            foreach (var kvp in record.PriceSnapshot)
                sb.Append($"{kvp.Key}:{kvp.Value:F1} ");
            sb.AppendLine();
            sb.AppendLine();
        }
        txtHistory.text = sb.ToString();
    }

    private void OnRefresh(object p1, object p2) => RefreshUI();
}
