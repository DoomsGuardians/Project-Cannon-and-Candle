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
    private TMP_Text txtBtnRestart, txtBtnMainMenu;

    // UI文本配置
    private UITextConfig textConfig;

    // 缓存的结果数据
    private bool? cachedIsVictory = null;

    public override void OnAwake()
    {
        base.OnAwake();
        uiLayer = UILayer.Top;

        // 加载文本配置
        textConfig = resService.LoadResource<UITextConfig>(ConfigPaths.UI_TEXT);

        var b = gameObject.GetComponent<CampaignEndPopupBinder>();
        if (b != null)
        {
            txtTitle = b.txtTitle;
            txtStats = b.txtStats;
            txtCommissions = b.txtCommissions;
            btnRestart = b.btnRestart;
            btnMainMenu = b.btnMainMenu;
            txtBtnRestart = b.txtBtnRestart;
            txtBtnMainMenu = b.txtBtnMainMenu;
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
            if (isVictory)
                txtTitle.text = textConfig?.CampaignVictory ?? "胜利！";
            else
                txtTitle.text = textConfig?.CampaignDefeat ?? "失败";
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

            string turnLabel = textConfig?.CampaignStatsTurn ?? "最终星期";
            string cashLabel = textConfig?.CampaignStatsCash ?? "现金";
            string netWorthLabel = textConfig?.CampaignStatsNetWorth ?? "净资产";

            txtStats.text = $"{turnLabel}: {data.CurrentTurn}\n" +
                $"{cashLabel}: {data.Player.Cash:F0}\n" +
                $"{netWorthLabel}: {netWorth:F0}\n" +
                $"交易盈亏: {(profit >= 0 ? "+" : "")}{profit:F0}\n" +
                $"委托奖金: +{data.CommissionTotalBonus:F0}\n" +
                $"总回报: {(profit + data.CommissionTotalBonus >= 0 ? "+" : "")}{profit + data.CommissionTotalBonus:F0}";
        }

        // 委托达成情况
        if (txtCommissions != null && data.CommissionResults != null)
        {
            var commissions = manager.GetCommissionSystem()?.GetCommissions();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"-- {textConfig?.CampaignStatsCommissions ?? "委托任务"} --");

            foreach (var kvp in data.CommissionResults)
            {
                // 从委托配置中获取名称和奖金
                string name = kvp.Key;
                float bonus = 0f;

                if (commissions != null)
                {
                    var config = commissions.Find(c => c.CommissionId == kvp.Key);
                    if (config != null)
                    {
                        name = config.DisplayName;
                        bonus = config.BonusAmount;
                    }
                }

                string mark = kvp.Value ? "v" : "x";
                string bonusText = kvp.Value ? $"+${bonus:F0}" : "--";
                sb.AppendLine($"[{mark}] {name}  {bonusText}");
            }

            txtCommissions.text = sb.ToString();
        }

        // 更新按钮文本
        if (txtBtnRestart != null)
            txtBtnRestart.text = textConfig?.CampaignBtnRestart ?? "再来一局";
        if (txtBtnMainMenu != null)
            txtBtnMainMenu.text = textConfig?.CampaignBtnMainMenu ?? "返回主菜单";
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
