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
    /// 买入现货 (GDD v6.0 + 反操纵修复)
    /// 逐张计算交易，每张交易后价格变化（通过 Beta 累积）
    /// 新增交易量上限检查
    /// </summary>
    public bool BuyOrder(OrderType orderType, int quantity, out float totalCost)
    {
        totalCost = 0f;
        var market = campaignData.Market;
        var player = campaignData.Player;

        // 反操纵修复：检查交易量上限
        int maxTradePerTurn = Mathf.CeilToInt(
            market.TurnStartFloat[orderType] * balanceConfig.MaxTradeVolumeRatio);
        int remainingAllowance = maxTradePerTurn - market.TurnTradeVolume[orderType];

        if (remainingAllowance <= 0)
        {
            Debug.LogWarning($"本回合 {orderType} 交易量已达上限");
            return false;
        }

        // 限制实际交易量
        int actualQuantity = Mathf.Min(quantity, remainingAllowance);
        if (actualQuantity < quantity)
        {
            Debug.Log($"交易量受限: 请求{quantity}, 实际可交易{actualQuantity}");
        }

        if (market.MarketInventory[orderType] < actualQuantity)
        {
            Debug.LogWarning($"市场库存不足: {orderType}");
            return false;
        }

        // 逐张计算成本
        for (int i = 0; i < actualQuantity; i++)
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
        player.Inventory[orderType] += actualQuantity;
        market.MarketInventory[orderType] -= actualQuantity;

        // 更新交易量统计
        market.TurnTradeVolume[orderType] += actualQuantity;

        // 记录消耗量（用于三回合补充计算）
        market.AccumulatedBurn[orderType] += actualQuantity;

        // 更新当前价格
        market.CurrentPrices[orderType] = pricingEngine.CalculatePrice(orderType);

        eventService.SendMessage((EventID)WarBrokerEventID.OnTradeExecuted,
            new TransactionRecord
            {
                Type = TransactionRecord.TransactionType.Buy,
                OrderType = orderType,
                Quantity = actualQuantity,
                Price = market.CurrentPrices[orderType],
                TotalAmount = totalCost
            }, null);
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, player.Cash, null);

        return true;
    }

    /// <summary>
    /// 卖出现货 (GDD v6.0 + 反操纵修复)
    /// 逐张计算交易，每张交易后价格变化（通过 Beta 累积）
    /// 新增交易量上限检查
    /// </summary>
    public bool SellOrder(OrderType orderType, int quantity, out float totalRevenue)
    {
        totalRevenue = 0f;
        var market = campaignData.Market;
        var player = campaignData.Player;

        // 反操纵修复：检查交易量上限
        int maxTradePerTurn = Mathf.CeilToInt(
            market.TurnStartFloat[orderType] * balanceConfig.MaxTradeVolumeRatio);
        int remainingAllowance = maxTradePerTurn - market.TurnTradeVolume[orderType];

        if (remainingAllowance <= 0)
        {
            Debug.LogWarning($"本回合 {orderType} 交易量已达上限");
            return false;
        }

        // 限制实际交易量
        int actualQuantity = Mathf.Min(quantity, remainingAllowance);
        if (actualQuantity < quantity)
        {
            Debug.Log($"交易量受限: 请求{quantity}, 实际可交易{actualQuantity}");
        }

        if (player.Inventory[orderType] < actualQuantity)
        {
            Debug.LogWarning($"库存不足: {orderType}");
            return false;
        }

        // 逐张计算收益
        for (int i = 0; i < actualQuantity; i++)
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
        player.Inventory[orderType] -= actualQuantity;
        market.MarketInventory[orderType] += actualQuantity;

        // 更新交易量统计
        market.TurnTradeVolume[orderType] += actualQuantity;

        // 更新当前价格
        market.CurrentPrices[orderType] = pricingEngine.CalculatePrice(orderType);

        eventService.SendMessage((EventID)WarBrokerEventID.OnTradeExecuted,
            new TransactionRecord
            {
                Type = TransactionRecord.TransactionType.Sell,
                OrderType = orderType,
                Quantity = actualQuantity,
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
    /// 开立期货合约 (GDD v6.0: 固定 3 回合 + TWAP结算)
    /// </summary>
    public bool OpenFutures(OrderType orderType, FuturesDirection direction,
        int quantity, out FuturesContract contract)
    {
        contract = null;
        var market = campaignData.Market;
        var player = campaignData.Player;

        // GDD v6.0: 期货固定 3 回合
        const int FUTURES_DURATION = 3;

        // 检查持仓数量上限
        if (player.FuturesPositions.Count >= balanceConfig.MaxFuturesPositions)
        {
            Debug.LogWarning($"期货持仓已达上限: {balanceConfig.MaxFuturesPositions}");
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
            OpenTurn = campaignData.CurrentTurn,
            ExpirationTurn = campaignData.CurrentTurn + FUTURES_DURATION,
            Margin = margin,
            // TWAP结算：记录开仓时结算价历史的索引
            SettlementStartIndex = market.SettlementPriceHistory[orderType].Count
        };

        player.Cash -= margin;
        player.FuturesPositions.Add(contract);

        eventService.SendMessage((EventID)WarBrokerEventID.OnFuturesOpened, contract, null);
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, player.Cash, null);

        return true;
    }

    /// <summary>
    /// 平仓期货合约（使用TWAP结算价）
    /// </summary>
    public bool CloseFutures(int contractId, out float pnl)
    {
        pnl = 0f;
        var player = campaignData.Player;
        var market = campaignData.Market;

        var contract = player.FuturesPositions.Find(c => c.ContractId == contractId);
        if (contract == null) return false;

        // 防止同回合套利：开仓当回合不能平仓
        if (contract.OpenTurn == campaignData.CurrentTurn)
        {
            Debug.LogWarning($"期货合约 {contractId} 开仓当回合不能平仓");
            return false;
        }

        // 使用 TWAP 结算价计算盈亏
        float settlementPrice = CalculateFuturesTWAP(contract);
        pnl = contract.CalculatePnLWithPrice(settlementPrice);

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

    /// <summary>
    /// 结算到期期货
    /// </summary>
    /// <returns>(结算数量, 总盈亏)</returns>
    public (int count, float pnl) SettleExpiredFutures()
    {
        var player = campaignData.Player;
        var toSettle = player.FuturesPositions
            .FindAll(c => c.ExpirationTurn <= campaignData.CurrentTurn);

        float totalPnl = 0f;
        foreach (var contract in toSettle)
        {
            CloseFutures(contract.ContractId, out float pnl);
            totalPnl += pnl;
        }

        return (toSettle.Count, totalPnl);
    }

    #endregion

    #region 维克多期货交易

    /// <summary>维克多开立期货合约（带TWAP结算）</summary>
    public bool VictorOpenFutures(OrderType orderType, FuturesDirection direction, int quantity, out FuturesContract contract)
    {
        contract = null;
        var market = campaignData.Market;
        var ledger = campaignData.VictorLedger;

        const int FUTURES_DURATION = 3;

        float openPrice = market.CurrentPrices[orderType];
        float margin = openPrice * quantity * balanceConfig.FuturesMarginRate;

        if (ledger.Cash < margin)
        {
            Debug.Log($"[Victor] 保证金不足: 需要{margin}, 当前{ledger.Cash}");
            return false;
        }

        contract = new FuturesContract
        {
            ContractId = nextContractId++,
            TargetOrder = orderType,
            Direction = direction,
            OpenPrice = openPrice,
            Quantity = quantity,
            OpenTurn = campaignData.CurrentTurn,
            ExpirationTurn = campaignData.CurrentTurn + FUTURES_DURATION,
            Margin = margin,
            // TWAP结算：记录开仓时结算价历史的索引
            SettlementStartIndex = market.SettlementPriceHistory[orderType].Count
        };

        ledger.Cash -= margin;
        ledger.FuturesPositions.Add(contract);

        Debug.Log($"[Victor] 开仓期货: {orderType} {direction} x{quantity} @ {openPrice:F1}");
        return true;
    }

    /// <summary>维克多平仓期货合约（使用TWAP结算）</summary>
    public bool VictorCloseFutures(int contractId, out float pnl)
    {
        pnl = 0f;
        var market = campaignData.Market;
        var ledger = campaignData.VictorLedger;

        var contract = ledger.FuturesPositions.Find(c => c.ContractId == contractId);
        if (contract == null) return false;

        // 使用 TWAP 结算价计算盈亏
        float settlementPrice = CalculateFuturesTWAP(contract);
        pnl = contract.CalculatePnLWithPrice(settlementPrice);

        ledger.Cash += contract.Margin + pnl;
        ledger.FuturesPositions.Remove(contract);

        Debug.Log($"[Victor] 平仓期货: {contract.TargetOrder} PnL={pnl:F1}");
        return true;
    }

    /// <summary>检查维克多强制平仓</summary>
    public void CheckVictorForceLiquidation()
    {
        var market = campaignData.Market;
        var ledger = campaignData.VictorLedger;
        var toClose = new List<int>();

        foreach (var contract in ledger.FuturesPositions)
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
            VictorCloseFutures(id, out float pnl);
            Debug.Log($"[Victor] 强制平仓: ContractId={id}, PnL={pnl:F1}");
        }
    }

    /// <summary>结算维克多到期期货</summary>
    public void SettleVictorExpiredFutures()
    {
        var ledger = campaignData.VictorLedger;
        var toSettle = ledger.FuturesPositions
            .FindAll(c => c.ExpirationTurn <= campaignData.CurrentTurn);

        foreach (var contract in toSettle)
        {
            VictorCloseFutures(contract.ContractId, out _);
        }
    }

    #endregion

    #region 维克多借贷

    /// <summary>计算维克多贷款额度</summary>
    public float CalculateVictorLoanLimit()
    {
        float netWorth = CalculateVictorNetWorth();
        return Mathf.Max(0, netWorth * balanceConfig.LoanRatio - campaignData.VictorLedger.Debt);
    }

    /// <summary>计算维克多净资产</summary>
    public float CalculateVictorNetWorth()
    {
        return campaignData.VictorLedger.GetNetWorth(campaignData.Market);
    }

    /// <summary>维克多借款</summary>
    public bool VictorBorrow(float amount)
    {
        if (amount > CalculateVictorLoanLimit()) return false;

        var ledger = campaignData.VictorLedger;
        ledger.Cash += amount;
        ledger.Debt += amount;

        Debug.Log($"[Victor] 借款: {amount:F1}, 总债务: {ledger.Debt:F1}");
        return true;
    }

    /// <summary>维克多还款</summary>
    public bool VictorRepay(float amount)
    {
        var ledger = campaignData.VictorLedger;
        amount = Mathf.Min(amount, ledger.Debt, ledger.Cash);
        if (amount <= 0) return false;

        ledger.Cash -= amount;
        ledger.Debt -= amount;

        Debug.Log($"[Victor] 还款: {amount:F1}, 剩余债务: {ledger.Debt:F1}");
        return true;
    }

    /// <summary>维克多利息计算</summary>
    public void ApplyVictorInterest()
    {
        var ledger = campaignData.VictorLedger;
        if (ledger.Debt > 0)
        {
            float interest = ledger.Debt * balanceConfig.BankInterestRate;
            ledger.Debt += interest;
            Debug.Log($"[Victor] 利息: {interest:F1}, 总债务: {ledger.Debt:F1}");
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

    /// <summary>
    /// 计算并应用银行利息
    /// </summary>
    /// <returns>本次支付的利息金额</returns>
    public float ApplyInterest()
    {
        float interestPaid = campaignData.Player.BankDebt * balanceConfig.BankInterestRate;
        campaignData.Player.BankDebt *= (1 + balanceConfig.BankInterestRate);
        return interestPaid;
    }

    #endregion

    #region 持有成本

    /// <summary>
    /// 计算并应用仓储费用
    /// </summary>
    /// <returns>本次支付的仓储费</returns>
    public float ApplyStorageCost()
    {
        var player = campaignData.Player;
        int totalInventory = 0;
        foreach (var kvp in player.Inventory)
        {
            totalInventory += kvp.Value;
        }
        float cost = totalInventory * balanceConfig.StorageCostPerUnit;
        player.Cash -= cost;
        eventService.SendMessage((EventID)WarBrokerEventID.OnCashChange, player.Cash, null);
        return cost;
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
    /// 军工厂动态产能（优化版 + 战场消耗联动）
    /// 每 3 回合补充一次，补充量 = 过去 3 回合的累计消耗量 + 战场消耗
    /// 战场消耗单独记录，与交易消耗分开计算
    /// </summary>
    private void ReplenishMarketInventory()
    {
        var market = campaignData.Market;
        int currentTurn = campaignData.CurrentTurn;

        // 每 3 回合补充一次
        if (currentTurn - market.LastReplenishTurn < 3)
        {
            return; // 还没到补充时间
        }

        market.LastReplenishTurn = currentTurn;

        foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
        {
            // 交易消耗 + 战场消耗
            float accumulatedBurn = market.AccumulatedBurn[orderType];
            float battleConsumption = market.BattleConsumption[orderType];
            float totalConsumption = accumulatedBurn + battleConsumption;

            // 产能系数随机 0.9~1.1，让供给略有波动
            float productionFactor = UnityEngine.Random.Range(
                balanceConfig.ProductionFactorMin,
                balanceConfig.ProductionFactorMax);

            // 动态产能 = 累计消耗 × 产能系数
            float dynamicProduction = totalConsumption * productionFactor;

            // 保底产能：每 3 回合至少补充 2 份（防止市场完全枯竭）
            int finalProduction = Mathf.Max(2, Mathf.RoundToInt(dynamicProduction));

            // 补充库存
            market.MarketInventory[orderType] += finalProduction;

            // 更新 LastWeekBurn（用于其他系统参考）
            market.LastWeekBurn[orderType] = totalConsumption;

            // 重置累计消耗（不重置BattleConsumption，在回合开始时重置）
            market.AccumulatedBurn[orderType] = 0f;
        }
    }

    /// <summary>
    /// 记录消耗量（在买入/卖出时调用）
    /// </summary>
    public void RecordConsumption(OrderType orderType, int quantity)
    {
        if (quantity > 0)
        {
            campaignData.Market.AccumulatedBurn[orderType] += quantity;
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
            if (frontline.LinePosition <= 2.5f && orderType == OrderType.DEF)
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

    #region TWAP结算（反操纵修复）

    /// <summary>
    /// 回合结束时记录结算价（用于期货TWAP结算）
    /// 使用 PricingEngine 的 CalculateSettlementPrice 计算（剥离 Gamma）
    /// </summary>
    public void RecordSettlementPrice()
    {
        var market = campaignData.Market;

        foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
        {
            // 使用结算价（剥离Gamma影响）
            float settlementPrice = pricingEngine.CalculateSettlementPrice(orderType);
            market.SettlementPriceHistory[orderType].Add(settlementPrice);
        }
    }

    /// <summary>
    /// 计算期货的TWAP结算价
    /// 使用合约开仓后的所有结算价均值
    /// </summary>
    public float CalculateFuturesTWAP(FuturesContract contract)
    {
        var market = campaignData.Market;
        var priceHistory = market.SettlementPriceHistory[contract.TargetOrder];

        int startIndex = contract.SettlementStartIndex;
        int endIndex = priceHistory.Count;

        // 如果没有足够的结算价历史，使用当前结算价
        if (endIndex <= startIndex)
        {
            return pricingEngine.CalculateSettlementPrice(contract.TargetOrder);
        }

        // 计算TWAP
        float sum = 0f;
        int count = 0;
        for (int i = startIndex; i < endIndex; i++)
        {
            sum += priceHistory[i];
            count++;
        }

        return sum / count;
    }

    #endregion
}

/// <summary>市场情报项</summary>
public class MarketIntelItem
{
    public bool IsPositive;
    public string Description;
}
