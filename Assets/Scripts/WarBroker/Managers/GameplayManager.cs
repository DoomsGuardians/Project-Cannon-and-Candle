using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏流程管理器：协调各系统，处理玩家输入
/// </summary>
public class GameplayManager : ManagerBase
{
    private MarketSystem marketSystem;
    private BattleSystem battleSystem;
    private CampaignSystem campaignSystem;
    private GameBalanceConfig balanceConfig;

    [Header("战役配置")]
    public string CampaignId = "Campaign_Tutorial";

    // 收集本回合的战斗结果
    private List<BattleResult> pendingBattleResults = new List<BattleResult>();

    public override void OnAwake()
    {
        base.OnAwake();

        marketSystem = GameRoot.Instance.marketSystem;
        battleSystem = GameRoot.Instance.battleSystem;
        campaignSystem = GameRoot.Instance.campaignSystem;

        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
    }

    public override void OnShow()
    {
        BeginGame();
    }

    public override void OnExit()
    {
        UnregisterEvents();
    }

    private void RegisterEvents()
    {
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnTurnStart);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnTurnEnd);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnGameEnd, OnGameEnd);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnBattleResult, OnBattleResult);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnBattleAnimationsComplete, OnBattleAnimationsComplete);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnRandomEvent, OnRandomEvent);
    }

    private void UnregisterEvents()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    public void BeginGame()
    {
        if (campaignSystem == null || marketSystem == null || battleSystem == null)
        {
            Debug.LogError("[GameplayManager] 系统未初始化，无法开始战役。");
            return;
        }

        campaignSystem.InitNewCampaign(CampaignId, marketSystem, battleSystem);
        if (campaignSystem.Data == null)
        {
            Debug.LogError($"[GameplayManager] 战役配置加载失败: {CampaignId}");
            return;
        }

        // 先注册事件，确保能接收到战斗结果
        RegisterEvents();

        // 先打开主界面，确保能接收到第一回合的阶段事件
        uiService.ShowWindow<GameplayWindow>("GameplayWindow");

        // 然后开始第一回合
        campaignSystem.StartTurn();
    }

    #region 玩家操作接口

    public bool BuyOrder(OrderType type, int quantity)
    {
        return marketSystem.BuyOrder(type, quantity, out _);
    }

    public bool SellOrder(OrderType type, int quantity)
    {
        return marketSystem.SellOrder(type, quantity, out _);
    }

    public bool OpenFutures(OrderType type, FuturesDirection dir, int qty, int turns)
    {
        // GDD v6.0: 期货固定 3 回合，忽略 turns 参数
        return marketSystem.OpenFutures(type, dir, qty, out _);
    }

    public bool CloseFutures(int contractId)
    {
        return marketSystem.CloseFutures(contractId, out _);
    }

    public bool Borrow(float amount)
    {
        return marketSystem.Borrow(amount);
    }

    public bool Repay(float amount)
    {
        return marketSystem.Repay(amount);
    }

    public bool AssignOrder(string generalId, OrderType order)
    {
        var data = campaignSystem.Data;
        var general = data.Battle.AllyGenerals.Find(g => g.GeneralId == generalId);
        if (general == null) return false;

        if (data.Player.Inventory[order] <= 0)
        {
            Debug.LogWarning("库存不足");
            return false;
        }

        float bid = general.CalculateBid(order, data.Market.CurrentPrices[order], balanceConfig);

        if (bid > 0)
        {
            data.Player.Cash += bid;
        }
        else
        {
            if (data.Player.Cash < -bid)
            {
                Debug.LogWarning("资金不足支付负出价");
                return false;
            }
            data.Player.Cash += bid;
        }

        data.Player.Inventory[order]--;
        general.AssignedOrder = order;

        if (bid > 0) general.Trust = Mathf.Min(100, general.Trust + 5);
        else if (bid < 0) general.Trust = Mathf.Max(0, general.Trust - 10);

        eventService.SendMessage((EventID)WarBrokerEventID.OnOrderAssigned, generalId, order);
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, data.Player.Cash, null);

        return true;
    }

    public void EndTurn()
    {
        campaignSystem.EndTurn();
    }

    #endregion

    #region 数据访问接口

    public CampaignRuntimeData GetCampaignData() => campaignSystem.Data;
    public PlayerData GetPlayerData() => campaignSystem.Data.Player;
    public MarketData GetMarketData() => campaignSystem.Data.Market;
    public BattleData GetBattleData() => campaignSystem.Data.Battle;
    public CommissionSystem GetCommissionSystem() => campaignSystem.GetCommissionSystem();

    #endregion

    #region 事件处理

    private void OnTurnStart(object param1, object param2)
    {
        int turn = (int)param1;
        Debug.Log($"回合 {turn} 开始");

        // 注意：不在这里清空 pendingBattleResults
        // 因为 OnBattleAnimationsComplete 可能在新回合开始后才被调用（事件监听顺序问题）
        // pendingBattleResults 会在 OnBattleAnimationsComplete 中显示战报后被清空
    }

    private void OnTurnEnd(object param1, object param2)
    {
        int turn = (int)param1;
        Debug.Log($"回合 {turn} 结束");
    }

    private void OnGameEnd(object param1, object param2)
    {
        bool isVictory = (bool)param1;
        Debug.Log($"游戏结束: {(isVictory ? "胜利" : "失败")}");

        // 缓存结果，等战报显示完毕后再显示结算弹窗
        pendingGameEndResult = isVictory;

        // 如果没有待显示的战报，直接显示结算弹窗
        if (pendingBattleResults.Count == 0)
        {
            ShowCampaignEndPopup(isVictory);
            pendingGameEndResult = null;
        }
        // 否则等 OnBattleAnimationsComplete 处理完战报后再显示
    }

    /// <summary>缓存的游戏结束结果</summary>
    private bool? pendingGameEndResult = null;

    /// <summary>显示战役结束弹窗</summary>
    private void ShowCampaignEndPopup(bool isVictory)
    {
        var popup = uiService.ShowWindow<CampaignEndPopup>("CampaignEndPopup");
        if (popup != null)
        {
            popup.SetResult(isVictory);
        }
    }

    private void OnBattleResult(object param1, object param2)
    {
        var result = param1 as BattleResult;
        if (result != null)
        {
            // 只收集有意义的战斗结果（有兵力损失才需要显示战报）
            bool hasCasualties = result.AllyTroopChange != 0 || result.EnemyTroopChange != 0;
            if (hasCasualties)
            {
                pendingBattleResults.Add(result);
                Debug.Log($"[GameplayManager] 收到战斗结果（有伤亡），当前待处理数量: {pendingBattleResults.Count}");
            }
            else
            {
                Debug.Log($"[GameplayManager] 忽略无伤亡的战斗结果");
            }
        }
    }

    private void OnBattleAnimationsComplete(object param1, object param2)
    {
        Debug.Log($"[GameplayManager] OnBattleAnimationsComplete 被调用，待处理战斗结果数量: {pendingBattleResults.Count}");

        // 所有战斗动画播放完成
        if (pendingBattleResults.Count > 0)
        {
            // 有战报需要显示，弹出战报弹窗
            var results = new List<BattleResult>(pendingBattleResults);
            var hasGameEnd = pendingGameEndResult.HasValue;
            var isVictory = pendingGameEndResult ?? false;

            // 清除待处理状态
            pendingBattleResults.Clear();
            if (hasGameEnd) pendingGameEndResult = null;

            Debug.Log($"[GameplayManager] 准备显示战报弹窗，战斗结果数量: {results.Count}");

            // 使用队列显示战报弹窗
            uiService.ShowPopupQueued<BattleResultPopup>("BattleResultPopup", popup =>
            {
                Debug.Log("[GameplayManager] BattleResultPopup 回调被执行");
                popup.SetBattleResults(results);

                // 设置关闭回调：发送战报确认事件
                popup.SetOnCloseCallback(() =>
                {
                    // 先发送战报确认事件，让 CampaignSystem 进入结算阶段
                    eventService.SendMessage((EventID)WarBrokerEventID.OnBattleReportConfirmed, null, null);

                    // 如果有待显示的结算弹窗，显示它
                    if (hasGameEnd)
                    {
                        ShowCampaignEndPopup(isVictory);
                    }
                });
            });
        }
        else
        {
            // 没有战报需要显示，直接发送战报确认事件
            Debug.Log("[GameplayManager] 无战报，直接发送战报确认事件");
            eventService.SendMessage((EventID)WarBrokerEventID.OnBattleReportConfirmed, null, null);
        }
    }

    private void OnRandomEvent(object param1, object param2)
    {
        var eventConfig = param1 as RandomEventConfig;
        if (eventConfig != null)
        {
            // 使用队列显示事件弹窗
            uiService.ShowPopupQueued<EventPopup>("EventPopup", popup =>
            {
                popup.SetEventData(eventConfig);
            });
        }
    }

    #endregion
}
