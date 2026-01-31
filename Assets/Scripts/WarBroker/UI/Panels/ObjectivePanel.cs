using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 委托任务面板：显示当前盈亏和委托任务进度
/// </summary>
public class ObjectivePanel : WindowBase
{
    private TMP_Text txtObjectiveTitle, txtObjectiveDescription, txtProgress, txtPnL;
    private TMP_Text txtWinWar, txtShortCountry, txtTraitor, txtMeatGrinder;
    private Transform commissionListRoot;

    private GameplayManager gameplayManager;
    private CommissionSystem commissionSystem;

    // 委托显示名称
    private static readonly Dictionary<string, string> CommissionNames = new Dictionary<string, string>
    {
        { "WinWar", "赢下战争" },
        { "ShortCountry", "做空祖国" },
        { "Traitor", "卖国求荣" },
        { "MeatGrinder", "绞肉机" }
    };

    // 委托奖金
    private static readonly Dictionary<string, float> CommissionBonuses = new Dictionary<string, float>
    {
        { "WinWar", 200f },
        { "ShortCountry", 500f },
        { "Traitor", 300f },
        { "MeatGrinder", 150f }
    };

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();

        var b = gameObject.GetComponent<ObjectivePanelBinder>();
        if (b != null)
        {
            txtObjectiveTitle = b.txtObjectiveTitle;
            txtObjectiveDescription = b.txtObjectiveDescription;
            txtProgress = b.txtProgress;
            txtPnL = b.txtPnL;
            commissionListRoot = b.commissionListRoot;
            txtWinWar = b.txtWinWar;
            txtShortCountry = b.txtShortCountry;
            txtTraitor = b.txtTraitor;
            txtMeatGrinder = b.txtMeatGrinder;
        }
    }

    public override void OnShow()
    {
        commissionSystem = gameplayManager?.GetCommissionSystem();

        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnBattleResult, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnRefresh);
        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    private void RefreshUI()
    {
        var data = gameplayManager?.GetCampaignData();
        if (data == null) return;

        // 主目标标题
        if (txtObjectiveTitle != null)
            txtObjectiveTitle.text = "战役目标";

        // 当前盈亏
        float netWorth = data.Player.CalculateNetWorth(data.Market);
        float profit = netWorth - data.Player.AuditValue;
        float profitPercent = data.Player.AuditValue > 0 ? (profit / data.Player.AuditValue) * 100f : 0f;

        if (txtObjectiveDescription != null)
            txtObjectiveDescription.text = $"净资产: {netWorth:F0}";

        if (txtPnL != null)
        {
            string sign = profit >= 0 ? "+" : "";
            txtPnL.text = $"P&L: {sign}{profit:F0} ({sign}{profitPercent:F1}%)";
            txtPnL.color = profit >= 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
        }

        if (txtProgress != null)
            txtProgress.text = $"回合 {data.CurrentTurn}/{data.MaxTurns}";

        // 更新委托任务进度
        RefreshCommissions();
    }

    private void RefreshCommissions()
    {
        if (commissionSystem == null) return;

        var progress = commissionSystem.GetCommissionProgress();

        // WinWar
        if (txtWinWar != null)
        {
            float p = progress.ContainsKey("WinWar") ? progress["WinWar"] : 0f;
            string status = p >= 1f ? "[完成]" : $"[{p * 100:F0}%]";
            txtWinWar.text = $"{CommissionNames["WinWar"]} ${CommissionBonuses["WinWar"]:F0} {status}";
            txtWinWar.color = p >= 1f ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
        }

        // ShortCountry
        if (txtShortCountry != null)
        {
            float p = progress.ContainsKey("ShortCountry") ? progress["ShortCountry"] : 0f;
            string status = p >= 1f ? "[完成]" : $"[{p * 100:F0}%]";
            txtShortCountry.text = $"{CommissionNames["ShortCountry"]} ${CommissionBonuses["ShortCountry"]:F0} {status}";
            txtShortCountry.color = p >= 1f ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
        }

        // Traitor
        if (txtTraitor != null)
        {
            float p = progress.ContainsKey("Traitor") ? progress["Traitor"] : 0f;
            string status = p >= 1f ? "[完成]" : "[未达成]";
            txtTraitor.text = $"{CommissionNames["Traitor"]} ${CommissionBonuses["Traitor"]:F0} {status}";
            txtTraitor.color = p >= 1f ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
        }

        // MeatGrinder
        if (txtMeatGrinder != null)
        {
            float p = progress.ContainsKey("MeatGrinder") ? progress["MeatGrinder"] : 0f;
            string status = p >= 1f ? "[完成]" : $"[{p * 100:F0}%]";
            txtMeatGrinder.text = $"{CommissionNames["MeatGrinder"]} ${CommissionBonuses["MeatGrinder"]:F0} {status}";
            txtMeatGrinder.color = p >= 1f ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
        }
    }

    private void OnRefresh(object p1, object p2) => RefreshUI();
}
