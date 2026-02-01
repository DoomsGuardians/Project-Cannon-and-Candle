using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 委托任务面板：动态显示委托任务进度
/// </summary>
public class ObjectivePanel : WindowBase
{
    private TMP_Text txtTitle;
    private Transform commissionListRoot;
    private GameObject commissionItemPrefab;

    private GameplayManager gameplayManager;
    private CommissionSystem commissionSystem;

    private List<CommissionItemBinder> commissionItems = new List<CommissionItemBinder>();

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();

        var b = gameObject.GetComponent<ObjectivePanelBinder>();
        if (b != null)
        {
            txtTitle = b.txtTitle;
            commissionListRoot = b.commissionListRoot;
            commissionItemPrefab = b.commissionItemPrefab;
        }
    }

    public override void OnShow()
    {
        commissionSystem = gameplayManager?.GetCommissionSystem();

        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnBattleResult, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnRefresh);

        // 清理旧项并动态生成委托项
        ClearCommissionItems();
        CreateCommissionItems();
        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    /// <summary>
    /// 清理所有委托项
    /// </summary>
    private void ClearCommissionItems()
    {
        foreach (var item in commissionItems)
        {
            if (item != null && item.gameObject != null)
            {
                Object.Destroy(item.gameObject);
            }
        }
        commissionItems.Clear();
    }

    /// <summary>
    /// 根据配置动态生成委托项
    /// </summary>
    private void CreateCommissionItems()
    {
        if (commissionSystem == null || commissionListRoot == null || commissionItemPrefab == null)
            return;

        var commissions = commissionSystem.GetCommissions();
        foreach (var config in commissions)
        {
            var itemGO = Object.Instantiate(commissionItemPrefab, commissionListRoot);
            var binder = itemGO.GetComponent<CommissionItemBinder>();
            if (binder != null)
            {
                binder.Config = config;

                // 设置静态信息（只需设置一次）
                if (binder.txtName != null)
                {
                    binder.txtName.text = config.DisplayName;
                }

                // 设置描述（含奖励）
                if (binder.txtDescription != null)
                {
                    binder.txtDescription.text = $"{config.Description}，奖金 ${config.BonusAmount:F0}";
                }

                // 设置委托人头像（如果有）
                if (binder.imgAvatar != null && config.CommissionerAvatar != null)
                {
                    binder.imgAvatar.sprite = config.CommissionerAvatar;
                    binder.imgAvatar.gameObject.SetActive(true);
                }
                else if (binder.imgAvatar != null)
                {
                    binder.imgAvatar.gameObject.SetActive(false);
                }

                commissionItems.Add(binder);
            }
        }
    }

    private void RefreshUI()
    {
        if (txtTitle != null)
            txtTitle.text = "委托任务";

        RefreshCommissions();
    }

    private void RefreshCommissions()
    {
        if (commissionSystem == null) return;

        var progress = commissionSystem.GetCommissionProgress();

        foreach (var item in commissionItems)
        {
            if (item == null || item.Config == null) continue;

            var config = item.Config;
            float p = progress.ContainsKey(config.CommissionId) ? progress[config.CommissionId] : 0f;
            bool completed = p >= 1f;

            // 格式化进度文本
            string progressText;
            if (completed)
            {
                progressText = "[完成]";
            }
            else if (config.Type == CommissionType.NotOccupyGrid)
            {
                // NotOccupyGrid 类型显示 [达成] 或 [未达成]
                progressText = "[未达成]";
            }
            else
            {
                progressText = $"[{p * 100:F0}%]";
            }

            // 更新进度文本
            if (item.txtProgress != null)
            {
                item.txtProgress.text = progressText;
                item.txtProgress.color = completed ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
            }

            // 更新名称颜色（可选，表示完成状态）
            if (item.txtName != null)
            {
                item.txtName.color = completed ? new Color(0.2f, 0.8f, 0.2f) : Color.white;
            }
        }
    }

    private void OnRefresh(object p1, object p2) => RefreshUI();
}
