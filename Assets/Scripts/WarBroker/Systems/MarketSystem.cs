using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 市场系统：管理价格计算、交易执行、期货结算
/// </summary>
public class MarketSystem : ILogic
{
    private EventService eventService;
    private ResService resService;

    private GameBalanceConfig balanceConfig;
    private OrderConfig orderConfig;

    private CampaignRuntimeData campaignData;

    private int nextContractId = 1;

    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;

        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        orderConfig = resService.LoadResource<OrderConfig>(ConfigPaths.ORDER_CONFIG);
    }

    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }

    public void SetRuntimeData(CampaignRuntimeData data)
    {
        campaignData = data;
    }

    #region 现货交易

    public bool BuyOrder(OrderType orderType, int quantity, out float totalCost)
    {
        totalCost = 0f;
        var market = campaignData.Market;
        var player = campaignData.Player;

        if (market.MarketInventory[orderType] < quantity)
        {
            Debug.LogWarning($"市场库存不足: {orderType}");
            return false;
        }

        float currentPrice = market.CurrentPrices[orderType];
        for (int i = 0; i < quantity; i++)
        {
            float price = currentPrice * (1 + balanceConfig.PriceImpactRate * i);
            float commission = price * balanceConfig.CommissionRate;
            totalCost += price + commission;
        }

        if (player.Cash < totalCost)
        {
            Debug.LogWarning($"资金不足: 需要{totalCost}, 当前{player.Cash}");
            return false;
        }

        player.Cash -= totalCost;
        player.Inventory[orderType] += quantity;
        market.MarketInventory[orderType] -= quantity;
        market.CurrentPrices[orderType] *= (1 + balanceConfig.PriceImpactRate * quantity);

        eventService.SendMessage((EventID)WarBrokerEventID.OnTradeExecuted,
            new TransactionRecord
            {
                Type = TransactionRecord.TransactionType.Buy,
                OrderType = orderType,
                Quantity = quantity,
                Price = market.CurrentPrices[orderType],
                TotalAmount = totalCost
            }, null);

        return true;
    }

    public bool SellOrder(OrderType orderType, int quantity, out float totalRevenue)
    {
        totalRevenue = 0f;
        var market = campaignData.Market;
        var player = campaignData.Player;

        if (player.Inventory[orderType] < quantity)
        {
            Debug.LogWarning($"库存不足: {orderType}");
            return false;
        }

        float currentPrice = market.CurrentPrices[orderType];
        for (int i = 0; i < quantity; i++)
        {
            float price = currentPrice * (1 - balanceConfig.PriceImpactRate * i);
            float commission = price * balanceConfig.CommissionRate;
            totalRevenue += price - commission;
        }

        player.Cash += totalRevenue;
        player.Inventory[orderType] -= quantity;
        market.MarketInventory[orderType] += quantity;
        market.CurrentPrices[orderType] *= (1 - balanceConfig.PriceImpactRate * quantity);

        eventService.SendMessage((EventID)WarBrokerEventID.OnTradeExecuted,
            new TransactionRecord
            {
                Type = TransactionRecord.TransactionType.Sell,
                OrderType = orderType,
                Quantity = quantity,
                Price = market.CurrentPrices[orderType],
                TotalAmount = totalRevenue
            }, null);

        return true;
    }

    #endregion

    #region 期货交易

    public bool OpenFutures(OrderType orderType, FuturesDirection direction,
        int quantity, int expirationTurns, out FuturesContract contract)
    {
        contract = null;
        var market = campaignData.Market;
        var player = campaignData.Player;

        if (expirationTurns > balanceConfig.MaxFuturesDuration)
        {
            Debug.LogWarning($"期货期限超过最大值: {balanceConfig.MaxFuturesDuration}");
            return false;
        }

        float openPrice = market.CurrentPrices[orderType];
        float margin = openPrice * quantity * balanceConfig.FuturesMarginRate;

        if (player.Cash < margin)
        {
            Debug.LogWarning($"保证金不足: 需要{margin}");
            return false;
        }

        contract = new FuturesContract
        {
            ContractId = nextContractId++,
            TargetOrder = orderType,
            Direction = direction,
            OpenPrice = openPrice,
            Quantity = quantity,
            ExpirationTurn = campaignData.CurrentTurn + expirationTurns,
            Margin = margin
        };

        player.Cash -= margin;
        player.FuturesPositions.Add(contract);

        eventService.SendMessage((EventID)WarBrokerEventID.OnFuturesOpened, contract, null);

        return true;
    }

    public bool CloseFutures(int contractId, out float pnl)
    {
        pnl = 0f;
        var player = campaignData.Player;
        var market = campaignData.Market;

        var contract = player.FuturesPositions.Find(c => c.ContractId == contractId);
        if (contract == null) return false;

        float currentPrice = market.CurrentPrices[contract.TargetOrder];
        pnl = contract.CalculatePnL(currentPrice);

        player.Cash += contract.Margin + pnl;
        player.FuturesPositions.Remove(contract);

        eventService.SendMessage((EventID)WarBrokerEventID.OnFuturesClosed, contract, pnl);

        return true;
    }

    public void CheckForceLiquidation()
    {
        var player = campaignData.Player;
        var market = campaignData.Market;
        var toClose = new List<int>();

        foreach (var contract in player.FuturesPositions)
        {
            float pnl = contract.CalculatePnL(market.CurrentPrices[contract.TargetOrder]);
            float remainingMargin = contract.Margin + pnl;

            if (remainingMargin < contract.Margin * (1 - balanceConfig.ForceLiquidationRate))
            {
                toClose.Add(contract.ContractId);
            }
        }

        foreach (var id in toClose)
        {
            CloseFutures(id, out float pnl);
            eventService.SendMessage((EventID)WarBrokerEventID.OnForceLiquidation, id, pnl);
        }
    }

    public void SettleExpiredFutures()
    {
        var player = campaignData.Player;
        var toSettle = player.FuturesPositions
            .FindAll(c => c.ExpirationTurn <= campaignData.CurrentTurn);

        foreach (var contract in toSettle)
        {
            CloseFutures(contract.ContractId, out _);
        }
    }

    #endregion

    #region 银行借贷

    public float CalculateLoanLimit()
    {
        float netWorth = campaignData.Player.CalculateNetWorth(campaignData.Market);
        return Mathf.Max(0, netWorth * balanceConfig.LoanRatio - campaignData.Player.BankDebt);
    }

    public bool Borrow(float amount)
    {
        if (amount > CalculateLoanLimit()) return false;

        campaignData.Player.Cash += amount;
        campaignData.Player.BankDebt += amount;

        return true;
    }

    public bool Repay(float amount)
    {
        var player = campaignData.Player;
        amount = Mathf.Min(amount, player.BankDebt, player.Cash);

        player.Cash -= amount;
        player.BankDebt -= amount;

        return true;
    }

    public void ApplyInterest()
    {
        campaignData.Player.BankDebt *= (1 + balanceConfig.BankInterestRate);
    }

    #endregion

    #region 持有成本

    public void ApplyStorageCost()
    {
        var player = campaignData.Player;
        int totalInventory = 0;
        foreach (var kvp in player.Inventory)
        {
            totalInventory += kvp.Value;
        }
        player.Cash -= totalInventory * balanceConfig.StorageCostPerUnit;
    }

    #endregion

    #region 价格更新

    public void UpdatePrices(Dictionary<OrderType, float> demandModifiers)
    {
        var market = campaignData.Market;

        foreach (var item in orderConfig.Orders)
        {
            market.MarketInventory[item.OrderType] += item.ProductionPerTurn;
        }

        foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
        {
            float demand = demandModifiers.GetValueOrDefault(orderType, 1f);
            float supply = market.MarketInventory[orderType];
            float supplyDemandRatio = demand / Mathf.Max(1, supply);

            float randomFactor = 1f + UnityEngine.Random.Range(
                -balanceConfig.PriceRandomRange,
                balanceConfig.PriceRandomRange);

            market.CurrentPrices[orderType] *= supplyDemandRatio * randomFactor;
        }

        market.PriceHistory.Add(new Dictionary<OrderType, float>(market.CurrentPrices));

        eventService.SendMessage((EventID)WarBrokerEventID.OnPriceUpdate, null, null);
    }

    #endregion

    #region 市场情报

    public List<MarketIntelItem> GetMarketIntelligence(OrderType orderType)
    {
        var intel = new List<MarketIntelItem>();

        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            var status = general.GetStatus(balanceConfig);
            if (status == GeneralStatus.Wounded || status == GeneralStatus.Critical)
            {
                if (orderType == OrderType.DEF || orderType == OrderType.RET)
                {
                    intel.Add(new MarketIntelItem
                    {
                        IsPositive = true,
                        Description = $"{general.Name}状态恶化，{orderType}需求上升"
                    });
                }
            }
        }

        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (frontline.LinePosition <= 2 && orderType == OrderType.DEF)
            {
                intel.Add(new MarketIntelItem
                {
                    IsPositive = true,
                    Description = $"{frontline.Position}战线劣势，DEF需求上升"
                });
            }
        }

        return intel;
    }

    #endregion
}

/// <summary>市场情报项</summary>
public class MarketIntelItem
{
    public bool IsPositive;
    public string Description;
}
