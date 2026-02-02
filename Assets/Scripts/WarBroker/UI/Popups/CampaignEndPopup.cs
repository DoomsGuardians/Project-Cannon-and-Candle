using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战役结束弹窗：胜负结果 + 统计 + 委托达成
/// </summary>
public class CampaignEndPopup : WindowBase
{
    private TMP_Text txtTitle, txtStats, txtCommissions;
    private Button btnRestart, btnMainMenu;

    private static readonly Dictionary<string, string> CommissionNames = new Dictionary<string, string>
    {
        { "WinWar", "赢下战争" },
        { "ShortCountry", "做空祖国" },
        { "Traitor", "卖国求荣" },
        { "MeatGrinder", "绞肉机" }
    };

    private static readonly Dictionary<string, float> CommissionBonuses = new Dictionary<string, float>
    {
        { "WinWar", 200f },
        { "ShortCountry", 500f },
        { "Traitor", 300f },
        { "MeatGrinder", 150f }
    };

    // 缓存的结果数据
    private bool? cachedIsVictory = null;

    public override void OnAwake()
    {
        base.OnAwake();
        uiLayer = UILayer.Top;

        var b = gameObject.GetComponent<CampaignEndPopupBinder>();
        if (b != null)
        {
            txtTitle = b.txtTitle;
            txtStats = b.txtStats;
            txtCommissions = b.txtCommissions;
            btnRestart = b.btnRestart;
            btnMainMenu = b.btnMainMenu;
        }
    }

    public override void OnShow()
    {
        AddButtonListener(btnRestart, OnRestart);
        AddButtonListener(btnMainMenu, OnMainMenu);

        // 如果有缓存的结果，立即更新UI
        if (cachedIsVictory.HasValue)
        {
            UpdateUI(cachedIsVictory.Value);
        }
    }

    public override void OnHide()
    {
        cachedIsVictory = null;
    }

    /// <summary>
    /// 设置战役结果（由外部调用）
    /// </summary>
    public void SetResult(bool isVictory)
    {
        cachedIsVictory = isVictory;

        // 如果弹窗已经显示，立即更新UI
        if (gameObject.activeInHierarchy)
        {
            UpdateUI(isVictory);
        }
    }

    private void UpdateUI(bool isVictory)
    {
        var manager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        var data = manager?.GetCampaignData();
        if (data == null)
        {
            Debug.LogWarning("[CampaignEndPopup] 无法获取战役数据");
            return;
        }

        // 标题
        if (txtTitle != null)
        {
            txtTitle.text = isVictory ? "胜利！" : "失败";
            Debug.Log($"[CampaignEndPopup] 设置标题: {txtTitle.text}, isVictory={isVictory}");
        }
        else
        {
            Debug.LogWarning("[CampaignEndPopup] txtTitle 为空");
        }

        // 财务统计
        if (txtStats != null)
        {
            float netWorth = data.Player.CalculateNetWorth(data.Market);
            float profit = netWorth - data.Player.AuditValue;

            txtStats.text = $"最终回合: {data.CurrentTurn}\n" +
                $"现金: {data.Player.Cash:F0}\n" +
                $"净资产: {netWorth:F0}\n" +
                $"交易盈亏: {(profit >= 0 ? "+" : "")}{profit:F0}\n" +
                $"委托奖金: +{data.CommissionTotalBonus:F0}\n" +
                $"总回报: {(profit + data.CommissionTotalBonus >= 0 ? "+" : "")}{profit + data.CommissionTotalBonus:F0}";
        }

        // 委托达成情况
        if (txtCommissions != null && data.CommissionResults != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("-- 委托任务 --");

            foreach (var kvp in data.CommissionResults)
            {
                string name = CommissionNames.ContainsKey(kvp.Key) ? CommissionNames[kvp.Key] : kvp.Key;
                float bonus = CommissionBonuses.ContainsKey(kvp.Key) ? CommissionBonuses[kvp.Key] : 0f;
                string mark = kvp.Value ? "v" : "x";
                string bonusText = kvp.Value ? $"+${bonus:F0}" : "--";
                sb.AppendLine($"[{mark}] {name}  {bonusText}");
            }

            txtCommissions.text = sb.ToString();
        }
    }

    private void OnRestart()
    {
        uIService.HideWindow(Name);
        GameRoot.Instance.stageSystem.ReloadCurrentStage();
    }

    private void OnMainMenu()
    {
        uIService.HideWindow(Name);
        GameRoot.Instance.stageSystem.LoadStage(1);
    }
}
