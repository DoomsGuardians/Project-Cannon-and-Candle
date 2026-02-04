using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 三因子定价引擎 (GDD v6.0)
/// Price = P_base × (1 + Alpha) × Beta × Gamma
/// Alpha: 战场态势因子（交战中心+位置修正+临界修正）
/// Beta: 交易冲击因子（回合间携带，交易时累积）
/// Gamma: 流通盘因子（InitialFloat / CurrentFloat）
/// </summary>
public class PricingEngine
{
    private GameBalanceConfig balanceConfig;
    private OrderConfig orderConfig;
    private CampaignRuntimeData data;

    public void Init(GameBalanceConfig balanceConfig, OrderConfig orderConfig, CampaignRuntimeData data)
    {
        this.balanceConfig = balanceConfig;
        this.orderConfig = orderConfig;
        this.data = data;
    }

    /// <summary>计算指定指令类型的当前价格</summary>
    public float CalculatePrice(OrderType type)
    {
        var basePrice = orderConfig.GetConfig(type).BasePrice;
        var alpha = CalculateAlpha(type) * balanceConfig.AlphaMultiplier; // 应用战场影响倍率
        var beta = data.Market.BetaCarry[type]; // Beta从市场数据中读取
        var gamma = CalculateGamma(type);

        return basePrice * (1f + alpha) * beta * gamma;
    }

    /// <summary>
    /// Alpha因子：战场态势 (GDD v6.0)
    /// Step 1: 逐战线判断是否接触
    /// Step 2: 计算交战中心，查询5级位置修正表
    /// Step 3: 应用临界战役修正（Grid 1/5、濒死）
    /// Step 4: 多战线平均
    /// </summary>
    private float CalculateAlpha(OrderType type)
    {
        float totalAlpha = 0f;
        int frontlineCount = 0;

        // 临界修正标志
        bool hasGrid1Crisis = false;
        bool hasGrid5Battle = false;

        // Step 1-2: 逐战线计算
        foreach (var ally in data.Battle.AllyGenerals)
        {
            if (ally.Troops <= 0) continue;

            var enemy = GetOpposingGeneral(ally);
            if (enemy == null || enemy.Troops <= 0) continue;

            float frontlineAlpha = 0f;

            // 检查是否接触
            if (IsEngaged(ally, enemy))
            {
                // Step 1: 接触状态基础修正 +20% (GDD v7.0)
                frontlineAlpha = balanceConfig.AlphaContactBase;

                // 计算交战中心
                float center = (ally.GridPosition + enemy.GridPosition) / 2f;

                // Step 2: 查询位置修正表
                frontlineAlpha += GetPositionModifier(type, center);

                // 濒死修正（该战线）
                if (ally.Troops >= 1 && ally.Troops <= 5)
                {
                    frontlineAlpha += balanceConfig.AlphaLowHPBonus;
                }
            }

            // 检查临界位置
            if (ally.GridPosition == 1) hasGrid1Crisis = true;
            if (ally.GridPosition == 5) hasGrid5Battle = true;

            totalAlpha += frontlineAlpha;
            frontlineCount++;
        }

        // Step 3: 临界战役修正（全局叠加）
        if (hasGrid1Crisis) totalAlpha += 0.15f * frontlineCount;
        if (hasGrid5Battle) totalAlpha += 0.15f * frontlineCount;

        // Step 4: 多战线平均
        if (frontlineCount > 0)
        {
            return totalAlpha / frontlineCount;
        }

        return 0f;
    }

    /// <summary>
    /// 根据交战中心查询位置修正表 (GDD v6.0)
    /// </summary>
    private float GetPositionModifier(OrderType type, float center)
    {
        // 5级位置修正表
        if (center >= 1.0f && center < 1.5f)
        {
            // Grid 1: 压迫己方基地 (ATK+5%, DEF+25%, RET+20%)
            return type switch
            {
                OrderType.ATK => 0.05f,
                OrderType.DEF => 0.25f,
                OrderType.RET => 0.20f,
                _ => 0f
            };
        }
        else if (center >= 1.5f && center < 2.5f)
        {
            // Grid 2: 己方腹地 (ATK+10%, DEF+20%, RET+10%)
            return type switch
            {
                OrderType.ATK => 0.10f,
                OrderType.DEF => 0.20f,
                OrderType.RET => 0.10f,
                _ => 0f
            };
        }
        else if (center >= 2.5f && center < 3.5f)
        {
            // Grid 3: 中线对峙 (ATK+15%, DEF+15%, RET+5%)
            return type switch
            {
                OrderType.ATK => 0.15f,
                OrderType.DEF => 0.15f,
                OrderType.RET => 0.05f,
                _ => 0f
            };
        }
        else if (center >= 3.5f && center < 4.5f)
        {
            // Grid 4: 敌方腹地 (ATK+20%, DEF+10%, RET+0%)
            return type switch
            {
                OrderType.ATK => 0.20f,
                OrderType.DEF => 0.10f,
                OrderType.RET => 0.00f,
                _ => 0f
            };
        }
        else if (center >= 4.5f && center <= 5.0f)
        {
            // Grid 5: 压迫敌方基地 (ATK+25%, DEF+5%, RET-5%)
            return type switch
            {
                OrderType.ATK => 0.25f,
                OrderType.DEF => 0.05f,
                OrderType.RET => -0.05f,
                _ => 0f
            };
        }

        return 0f;
    }

    /// <summary>
    /// Gamma因子：流通盘 (GDD v6.0)
    /// Gamma = InitialFloat / Max(CurrentFloat, 1)
    /// 流通盘越少，价格越高（逼空效应）
    /// 增加上限以避免极端价格
    /// </summary>
    private float CalculateGamma(OrderType type)
    {
        var market = data.Market;
        int currentFloat = market.MarketInventory[type];
        int initialFloat = market.InitialFloat[type];

        if (initialFloat <= 0) return 1f;

        // Gamma = InitialFloat / Max(CurrentFloat, 1)
        float gamma = (float)initialFloat / Mathf.Max(currentFloat, 1);

        // 应用上限，避免流通盘过低导致价格暴涨
        gamma = Mathf.Min(gamma, balanceConfig.GammaMax);

        return gamma;
    }

    /// <summary>
    /// 应用交易冲击到Beta (GDD v6.0)
    /// 每笔交易后调用，累积到 BetaCarry
    /// </summary>
    public void ApplyTradeImpact(OrderType type, int quantity, bool isBuy)
    {
        var market = data.Market;
        int currentFloat = market.MarketInventory[type];

        if (currentFloat <= 0) return;

        // Impact = 交易量 / CurrentFloat × ImpactCoefficient
        float impact = Mathf.Abs(quantity) / (float)currentFloat * balanceConfig.ImpactCoefficient;

        // 买入推高价格，卖出压低价格
        if (isBuy)
        {
            market.BetaCarry[type] *= (1f + impact);
        }
        else
        {
            market.BetaCarry[type] *= (1f - impact);
        }

        // 限制Beta范围，使用配置参数
        market.BetaCarry[type] = Mathf.Clamp(market.BetaCarry[type], balanceConfig.BetaMin, balanceConfig.BetaMax);
    }

    /// <summary>
    /// 回合开始时应用动量效应和恐慌效应 (GDD v6.0)
    /// </summary>
    public void ApplyMomentumAndPanic()
    {
        // 动量效应：上周价格涨跌幅 > 10%
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            float priceChangeRatio = GetPriceChangeRatio(type);

            if (priceChangeRatio > 0.10f)
            {
                // 追涨
                data.Market.BetaCarry[type] *= balanceConfig.BetaMomentumMultiplier;
            }
            else if (priceChangeRatio < -0.10f)
            {
                // 杀跌
                data.Market.BetaCarry[type] *= (2f - balanceConfig.BetaMomentumMultiplier);
            }
        }

        // 恐慌效应：后备役 < 20
        if (data.Battle.CurrentReserves < balanceConfig.BetaPanicReserveThreshold)
        {
            data.Market.BetaCarry[OrderType.ATK] *= 0.6f;  // 抛售进攻资产
            data.Market.BetaCarry[OrderType.DEF] *= 1.5f;  // 避险
            data.Market.BetaCarry[OrderType.RET] *= 2.0f;  // 逃命
        }

        // 限制Beta范围，使用配置参数
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            data.Market.BetaCarry[type] = Mathf.Clamp(data.Market.BetaCarry[type], balanceConfig.BetaMin, balanceConfig.BetaMax);
        }
    }

    /// <summary>获取价格变化率</summary>
    private float GetPriceChangeRatio(OrderType type)
    {
        var history = data.Market.PriceHistory;
        if (history.Count < 2) return 0f;

        var current = data.Market.CurrentPrices[type];
        var previous = history[history.Count - 1][type];

        if (previous <= 0) return 0f;

        return (current - previous) / previous;
    }

    /// <summary>判断是否处于接触状态</summary>
    private bool IsEngaged(GeneralData ally, GeneralData enemy)
    {
        int allyGrid = ally.GridPosition;
        int enemyGrid = enemy.GridPosition;
        int gap = enemyGrid - allyGrid - 1;
        return gap <= 0;
    }

    /// <summary>获取对位敌方将军</summary>
    private GeneralData GetOpposingGeneral(GeneralData ally)
    {
        foreach (var enemy in data.Battle.EnemyGenerals)
        {
            if (enemy.Position == ally.Position)
            {
                return enemy;
            }
        }
        return null;
    }

    /// <summary>获取各因子的调试信息</summary>
    public (float alpha, float beta, float gamma) GetFactors(OrderType type)
    {
        return (CalculateAlpha(type), data.Market.BetaCarry[type], CalculateGamma(type));
    }

    /// <summary>获取完整的市场因子数据（用于日志）</summary>
    public MarketFactors GetMarketFactors(OrderType type)
    {
        float alpha = CalculateAlpha(type) * balanceConfig.AlphaMultiplier;
        float beta = data.Market.BetaCarry[type];
        float gamma = CalculateGamma(type);
        float basePrice = orderConfig.GetConfig(type)?.BasePrice ?? 30f;
        float finalPrice = basePrice * (1f + alpha) * beta * gamma;

        return new MarketFactors
        {
            Alpha = alpha,
            Beta = beta,
            Gamma = gamma,
            BasePrice = basePrice,
            FinalPrice = finalPrice
        };
    }

    /// <summary>获取所有指令类型的市场因子</summary>
    public Dictionary<OrderType, MarketFactors> GetAllMarketFactors()
    {
        var result = new Dictionary<OrderType, MarketFactors>();
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            result[type] = GetMarketFactors(type);
        }
        return result;
    }
}
