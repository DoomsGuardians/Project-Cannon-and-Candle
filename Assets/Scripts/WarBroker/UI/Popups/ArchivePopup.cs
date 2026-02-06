using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 档案弹窗：显示玩家历史战役记录和统计数据
/// </summary>
public class ArchivePopup : WindowBase
{
    private TMP_Text txtTitle;
    private TMP_Text txtTotalWealth;
    private TMP_Text txtStatistics;
    private Transform historyContainer;
    private GameObject historyItemPrefab;
    private TMP_Text txtNoHistory;
    private Button btnClose;
    private Button btnClearData;

    private SaveSystem saveSystem;

    public override void OnAwake()
    {
        base.OnAwake();
        uiLayer = UILayer.Top;

        var binder = gameObject.GetComponent<ArchivePopupBinder>();
        if (binder != null)
        {
            txtTitle = binder.txtTitle;
            txtTotalWealth = binder.txtTotalWealth;
            txtStatistics = binder.txtStatistics;
            historyContainer = binder.historyContainer;
            historyItemPrefab = binder.historyItemPrefab;
            txtNoHistory = binder.txtNoHistory;
            btnClose = binder.btnClose;
            btnClearData = binder.btnClearData;
        }

        saveSystem = GameRoot.Instance.saveSystem;
    }

    public override void OnShow()
    {
        AddButtonListener(btnClose, OnClose);
        AddButtonListener(btnClearData, OnClearData);
        RefreshUI();
    }

    public override void OnHide()
    {
        RemoveAllButtonListener();
        ClearHistoryItems();
    }

    private void RefreshUI()
    {
        if (saveSystem?.MetaData == null)
        {
            Debug.LogWarning("[ArchivePopup] SaveSystem or MetaData is null");
            return;
        }

        var meta = saveSystem.MetaData;
        var stats = meta.Statistics;

        // 标题
        if (txtTitle != null)
            txtTitle.text = "档案";

        // 总资产
        if (txtTotalWealth != null)
        {
            string sign = meta.TotalWealth >= 0 ? "" : "";
            txtTotalWealth.text = $"累计资产: {sign}{meta.TotalWealth:N0}";
        }

        // 统计数据
        if (txtStatistics != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"总战役数: {stats.TotalCampaignsPlayed}");
            sb.AppendLine($"胜/负/平: {stats.TotalVictories}/{stats.TotalDefeats}/{stats.TotalDraws}");
            sb.AppendLine($"总星期数: {stats.TotalTurnsPlayed}");
            sb.AppendLine($"总交易次数: {stats.TotalTradeCount}");
            sb.AppendLine($"游戏时长: {FormatPlayTime(stats.TotalPlayTimeSeconds)}");
            sb.AppendLine();
            sb.AppendLine($"历史总收入: +{meta.LifetimeEarnings:N0}");
            sb.AppendLine($"历史总亏损: -{meta.LifetimeLosses:N0}");
            sb.AppendLine($"单局最高盈利: +{stats.BestSingleCampaignProfit:N0}");
            sb.AppendLine($"单局最大亏损: {stats.WorstSingleCampaignLoss:N0}");
            txtStatistics.text = sb.ToString();
        }

        // 历史战役列表
        ClearHistoryItems();
        CreateHistoryItems();
    }

    private void ClearHistoryItems()
    {
        if (historyContainer == null) return;

        for (int i = historyContainer.childCount - 1; i >= 0; i--)
        {
            var child = historyContainer.GetChild(i);
            if (child.gameObject != historyItemPrefab)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }

    private void CreateHistoryItems()
    {
        if (historyContainer == null || historyItemPrefab == null) return;

        var history = saveSystem.MetaData.CampaignHistory;

        if (history == null || history.Count == 0)
        {
            if (txtNoHistory != null)
            {
                txtNoHistory.gameObject.SetActive(true);
                txtNoHistory.text = "暂无历史战役记录";
            }
            return;
        }

        if (txtNoHistory != null)
            txtNoHistory.gameObject.SetActive(false);

        // 按时间倒序显示（最新的在前）
        for (int i = history.Count - 1; i >= 0; i--)
        {
            var record = history[i];
            CreateHistoryItem(record);
        }

        // 隐藏模板
        if (historyItemPrefab != null)
            historyItemPrefab.SetActive(false);
    }

    private void CreateHistoryItem(CampaignHistoryRecord record)
    {
        var itemObj = UnityEngine.Object.Instantiate(historyItemPrefab, historyContainer);
        itemObj.SetActive(true);

        var binder = itemObj.GetComponent<ArchiveHistoryItemBinder>();
        if (binder == null) return;

        // 战役名称
        if (binder.txtCampaignName != null)
            binder.txtCampaignName.text = record.CampaignName;

        // 结果
        if (binder.txtResult != null)
        {
            string resultText = record.Result switch
            {
                GameResult.Victory => "<color=#4CAF50>胜利</color>",
                GameResult.Defeat => "<color=#F44336>失败</color>",
                GameResult.Draw => "<color=#FFC107>平局</color>",
                _ => "未知"
            };
            binder.txtResult.text = resultText;
        }

        // 盈亏
        if (binder.txtProfit != null)
        {
            float profit = record.Financial.TotalProfitLoss + record.Financial.CommissionBonus;
            string color = profit >= 0 ? "#4CAF50" : "#F44336";
            string sign = profit >= 0 ? "+" : "";
            binder.txtProfit.text = $"<color={color}>{sign}{profit:N0}</color>";
        }

        // 日期
        if (binder.txtDate != null)
        {
            if (DateTime.TryParse(record.CompletionTime, out var date))
            {
                binder.txtDate.text = date.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }
            else
            {
                binder.txtDate.text = "-";
            }
        }

        // 星期数
        if (binder.txtTurns != null)
            binder.txtTurns.text = $"第{record.FinalTurn}星期";
    }

    private string FormatPlayTime(float seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}小时{ts.Minutes}分钟";
        else if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}分钟";
        else
            return $"{(int)ts.TotalSeconds}秒";
    }

    private void OnClose()
    {
        uIService.HideWindow(Name);
    }

    private void OnClearData()
    {
        // TODO: 添加确认对话框
        saveSystem?.ClearAllData();
        RefreshUI();
    }
}
