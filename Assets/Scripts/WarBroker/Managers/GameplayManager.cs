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

        campaignSystem.StartTurn();

        RegisterEvents();

        // 打开主界面
        uiService.ShowWindow<GameplayWindow>("GameplayWindow");
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
    }

    #endregion
}
