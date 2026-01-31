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

    private PricingEngine pricingEngine;

    private int nextContractId = 1;

    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;

        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        orderConfig = resService.LoadResource<OrderConfig>(ConfigPaths.ORDER_CONFIG);

        pricingEngine = new PricingEngine();
    }

    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }

    public void SetRuntimeData(CampaignRuntimeData data)
    {
        campaignData = data;
        pricingEngine.Init(balanceConfig, orderConfig, data);
    }

    public PricingEngine GetPricingEngine() => pricingEngine;

    #region 现货交易

    /// <summary>
    /// 买入现货 (GDD v6.0)
    /// 逐张计算交易，每张交易后价格变化（通过 Beta 累积）
    /// </summary>
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

        // 逐张计算成本
        for (int i = 0; i < quantity; i++)
        {
            float currentPrice = pricingEngine.CalculatePrice(orderType);
            float commission = currentPrice * balanceConfig.CommissionRate;
            totalCost += currentPrice + commission;

            // 每张交易后应用交易冲击（通过 Beta 累积）
            pricingEngine.ApplyTradeImpact(orderType, 1, true);

            // 更新 K 线极值
            UpdateKLineHighLow(orderType, currentPrice);
        }

        if (player.Cash < totalCost)
        {
            Debug.LogWarning($"资金不足: 需要{totalCost}, 当前{player.Cash}");
            return false;
        }

        // 执行交易
        player.Cash -= totalCost;
        player.Inventory[orderType] += quantity;
        market.MarketInventory[orderType] -= quantity;

        // 记录消耗量
        market.LastWeekBurn[orderType] += quantity;

        // 更新当前价格
        market.CurrentPrices[orderType] = pricingEngine.CalculatePrice(orderType);

        eventService.SendMessage((EventID)WarBrokerEventID.OnTradeExecuted,
            new TransactionRecord
            {
                Type = TransactionRecord.TransactionType.Buy,
                OrderType = orderType,
                Quantity = quantity,
                Price = market.CurrentPrices[orderType],
                TotalAmount = totalCost
            }, null);
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, player.Cash, null);

        return true;
    }

    /// <summary>
    /// 卖出现货 (GDD v6.0)
    /// 逐张计算交易，每张交易后价格变化（通过 Beta 累积）
    /// </summary>
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

        // 逐张计算收益
        for (int i = 0; i < quantity; i++)
        {
            float currentPrice = pricingEngine.CalculatePrice(orderType);
            float commission = currentPrice * balanceConfig.CommissionRate;
            totalRevenue += currentPrice - commission;

            // 每张交易后应用交易冲击（通过 Beta 累积）
            pricingEngine.ApplyTradeImpact(orderType, 1, false);

            // 更新 K 线极值
            UpdateKLineHighLow(orderType, currentPrice);
        }

        // 执行交易
        player.Cash += totalRevenue;
        player.Inventory[orderType] -= quantity;
        market.MarketInventory[orderType] += quantity;

        // 更新当前价格
        market.CurrentPrices[orderType] = pricingEngine.CalculatePrice(orderType);

        eventService.SendMessage((EventID)WarBrokerEventID.OnTradeExecuted,
            new TransactionRecord
            {
                Type = TransactionRecord.TransactionType.Sell,
                OrderType = orderType,
                Quantity = quantity,
                Price = market.CurrentPrices[orderType],
                TotalAmount = totalRevenue
            }, null);
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, player.Cash, null);

        return true;
    }

    /// <summary>更新 K 线的 High/Low (GDD v6.0)</summary>
    private void UpdateKLineHighLow(OrderType orderType, float price)
    {
        var market = campaignData.Market;
        var klineHistory = market.KLineHistory[orderType];

        if (klineHistory.Count == 0) return;

        var currentKLine = klineHistory[klineHistory.Count - 1];
        if (currentKLine.Turn != campaignData.CurrentTurn)
        {
            // 新回合，创建新 K 线
            return;
        }

        // 更新当前回合的 High/Low
        currentKLine.High = Mathf.Max(currentKLine.High, price);
        currentKLine.Low = Mathf.Min(currentKLine.Low, price);
    }

    #endregion

    #region 期货交易

    /// <summary>
    /// 开立期货合约 (GDD v6.0: 固定 3 回合)
    /// </summary>
    public bool OpenFutures(OrderType orderType, FuturesDirection direction,
        int quantity, out FuturesContract contract)
    {
        contract = null;
        var market = campaignData.Market;
        var player = campaignData.Player;

        // GDD v6.0: 期货固定 3 回合
        const int FUTURES_DURATION = 3;

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
            ExpirationTurn = campaignData.CurrentTurn + FUTURES_DURATION,
            Margin = margin
        };

        player.Cash -= margin;
        player.FuturesPositions.Add(contract);

        eventService.SendMessage((EventID)WarBrokerEventID.OnFuturesOpened, contract, null);
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, player.Cash, null);

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
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, player.Cash, null);

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
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, campaignData.Player.Cash, null);

        return true;
    }

    public bool Repay(float amount)
    {
        var player = campaignData.Player;
        amount = Mathf.Min(amount, player.BankDebt, player.Cash);

        player.Cash -= amount;
        player.BankDebt -= amount;
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, player.Cash, null);

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
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, player.Cash, null);
    }

    #endregion

    #region 价格更新

    public void UpdatePrices(Dictionary<OrderType, float> demandModifiers)
    {
        var market = campaignData.Market;

        // 记录历史价格（在更新前）
        market.PriceHistory.Add(new Dictionary<OrderType, float>(market.CurrentPrices));

        // 军工厂动态产能 (GDD v6.0)
        ReplenishMarketInventory();

        // 使用三因子定价引擎计算新价格
        foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
        {
            float basePrice = pricingEngine.CalculatePrice(orderType);

            // 应用需求修正
            float demand = demandModifiers.GetValueOrDefault(orderType, 1f);
            basePrice *= demand;

            // 应用随机波动
            float randomFactor = 1f + UnityEngine.Random.Range(
                -balanceConfig.PriceRandomRange,
                balanceConfig.PriceRandomRange);

            market.CurrentPrices[orderType] = basePrice * randomFactor;
        }

        eventService.SendMessage((EventID)WarBrokerEventID.OnPriceUpdate, null, null);
    }

    /// <summary>
    /// 军工厂动态产能 (GDD v6.0)
    /// 本周产能 = 上周总消耗 × 产能系数(0.9~1.1)
    /// 保底产能 3
    /// </summary>
    private void ReplenishMarketInventory()
    {
        var market = campaignData.Market;

        foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
        {
            float lastWeekBurn = market.LastWeekBurn[orderType];

            // 产能系数随机 0.9~1.1
            float productionFactor = UnityEngine.Random.Range(
                balanceConfig.ProductionFactorMin,
                balanceConfig.ProductionFactorMax);

            // 动态产能 = 上周消耗 × 产能系数
            float dynamicProduction = lastWeekBurn * productionFactor;

            // 保底产能 3
            int finalProduction = Mathf.Max(3, Mathf.RoundToInt(dynamicProduction));

            // 补充库存 (MarketInventory 是 int 类型)
            market.MarketInventory[orderType] += finalProduction;

            // 重置消耗计数
            market.LastWeekBurn[orderType] = 0f;
        }
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

    #region K线管理 (GDD v6.0)

    /// <summary>回合开始时初始化 K 线（Open 价格）</summary>
    public void InitializeKLineForTurn()
    {
        var market = campaignData.Market;
        int currentTurn = campaignData.CurrentTurn;

        foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
        {
            float openPrice = pricingEngine.CalculatePrice(orderType);

            var kline = new KLineData
            {
                Turn = currentTurn,
                Open = openPrice,
                High = openPrice,
                Low = openPrice,
                Close = openPrice,
                Volume = 0f
            };

            market.KLineHistory[orderType].Add(kline);
        }
    }

    /// <summary>回合结束时记录 K 线（Close 价格和成交量）</summary>
    public void FinalizeKLineForTurn()
    {
        var market = campaignData.Market;
        int currentTurn = campaignData.CurrentTurn;

        foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
        {
            var klineHistory = market.KLineHistory[orderType];
            if (klineHistory.Count == 0) continue;

            var currentKLine = klineHistory[klineHistory.Count - 1];
            if (currentKLine.Turn != currentTurn) continue;

            // 记录 Close 价格
            currentKLine.Close = pricingEngine.CalculatePrice(orderType);

            // 记录成交量（本回合消耗量）
            currentKLine.Volume = market.LastWeekBurn[orderType];
        }
    }

    #endregion
}

/// <summary>市场情报项</summary>
public class MarketIntelItem
{
    public bool IsPositive;
    public string Description;
}
