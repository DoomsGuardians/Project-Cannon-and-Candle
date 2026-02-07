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
    /// Alpha因子：战场态势 (五维重构版)
    /// A1: 战线位置（保留原有）
    /// A2: 兵力差异（新增）
    /// A3: 伤亡率冲击（新增）
    /// A4: 将军危机状态（增强）
    /// A5: 后备役压力（新增）
    /// </summary>
    private float CalculateAlpha(OrderType type)
    {
        float a1 = CalculateA1_Position(type);
        float a2 = CalculateA2_ForceDelta(type);
        float a3 = CalculateA3_CasualtyShock(type);
        float a4 = CalculateA4_GeneralCrisis(type);
        float a5 = CalculateA5_ReservePressure(type);

        // 加权求和
        float weightedAlpha = a1 * balanceConfig.AlphaWeight_Position
                            + a2 * balanceConfig.AlphaWeight_ForceDelta
                            + a3 * balanceConfig.AlphaWeight_Casualty
                            + a4 * balanceConfig.AlphaWeight_GeneralCrisis
                            + a5 * balanceConfig.AlphaWeight_ReservePressure;

        return weightedAlpha;
    }

    /// <summary>A1: 战线位置因子（保留原有逻辑）</summary>
    private float CalculateA1_Position(OrderType type)
    {
        float totalAlpha = 0f;
        int frontlineCount = 0;

        // 临界修正标志
        bool hasGrid1Crisis = false;
        bool hasGrid5Battle = false;

        foreach (var ally in data.Battle.AllyGenerals)
        {
            if (ally.Troops <= 0) continue;

            var enemy = GetOpposingGeneral(ally);
            if (enemy == null || enemy.Troops <= 0) continue;

            float frontlineAlpha = 0f;

            if (IsEngaged(ally, enemy))
            {
                // 接触状态基础修正
                frontlineAlpha = balanceConfig.AlphaContactBase;

                // 计算交战中心
                float center = (ally.GridPosition + enemy.GridPosition) / 2f;

                // 查询位置修正表
                frontlineAlpha += GetPositionModifier(type, center);

                // 濒死修正
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

        // 临界战役修正
        if (hasGrid1Crisis) totalAlpha += 0.15f * frontlineCount;
        if (hasGrid5Battle) totalAlpha += 0.15f * frontlineCount;

        if (frontlineCount > 0)
        {
            return totalAlpha / frontlineCount;
        }

        return 0f;
    }

    /// <summary>A2: 兵力差异因子</summary>
    private float CalculateA2_ForceDelta(OrderType type)
    {
        int totalAllyTroops = 0;
        int totalEnemyTroops = 0;

        foreach (var ally in data.Battle.AllyGenerals)
        {
            totalAllyTroops += Mathf.Max(0, ally.Troops);
        }
        foreach (var enemy in data.Battle.EnemyGenerals)
        {
            totalEnemyTroops += Mathf.Max(0, enemy.Troops);
        }

        if (totalAllyTroops + totalEnemyTroops == 0) return 0f;

        // 兵力比 = (敌方 - 己方) / (敌方 + 己方)
        // 敌方优势时为正，己方优势时为负
        float forceRatio = (float)(totalEnemyTroops - totalAllyTroops) / (totalAllyTroops + totalEnemyTroops);

        // 根据指令类型调整影响方向
        return type switch
        {
            OrderType.ATK => -forceRatio * 0.15f,  // 己方弱势时ATK需求下降
            OrderType.DEF => forceRatio * 0.20f,   // 己方弱势时DEF需求上升
            OrderType.RET => forceRatio * 0.15f,   // 己方弱势时RET需求上升
            _ => 0f
        };
    }

    /// <summary>A3: 伤亡率冲击因子（基于战场消耗量）</summary>
    private float CalculateA3_CasualtyShock(OrderType type)
    {
        float totalConsumption = 0f;
        foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
        {
            totalConsumption += data.Market.BattleConsumption[orderType];
        }

        // 高战场消耗 = 激烈战斗 = 需求冲击
        if (totalConsumption < 3f) return 0f;

        float shockFactor = Mathf.Min(totalConsumption / 10f, 0.3f);

        return type switch
        {
            OrderType.ATK => shockFactor * 0.5f,   // ATK消耗后需求小幅上升
            OrderType.DEF => shockFactor * 1.0f,   // DEF消耗后需求显著上升
            OrderType.RET => shockFactor * 0.8f,   // RET消耗后需求上升
            _ => 0f
        };
    }

    /// <summary>A4: 将军危机因子（溃败/濒死状态）</summary>
    private float CalculateA4_GeneralCrisis(OrderType type)
    {
        float crisisValue = 0f;
        int generalCount = data.Battle.AllyGenerals.Count;
        if (generalCount == 0) return 0f;

        foreach (var ally in data.Battle.AllyGenerals)
        {
            // 溃败状态
            if (ally.Troops <= 0)
            {
                crisisValue += 0.2f;
            }
            // 濒死状态 (1-5 HP)
            else if (ally.Troops <= 5)
            {
                crisisValue += 0.1f;
            }
            // 受伤状态 (6-10 HP)
            else if (ally.Troops <= 10)
            {
                crisisValue += 0.05f;
            }
        }

        // 根据指令类型返回修正
        return type switch
        {
            OrderType.ATK => -crisisValue * 0.3f,  // 危机时ATK需求下降
            OrderType.DEF => crisisValue * 0.8f,   // 危机时DEF需求激增
            OrderType.RET => crisisValue * 0.6f,   // 危机时RET需求上升
            _ => 0f
        };
    }

    /// <summary>A5: 后备役压力因子</summary>
    private float CalculateA5_ReservePressure(OrderType type)
    {
        int reserves = data.Battle.CurrentReserves;
        int initialReserves = data.Config.InitialReserves;

        if (initialReserves <= 0) return 0f;

        // 后备役比例
        float reserveRatio = (float)reserves / initialReserves;

        // 后备役充足时压力小，耗尽时压力大
        float pressure = 1f - reserveRatio;

        // 低于20%时产生显著影响
        if (reserveRatio > 0.2f)
        {
            pressure *= 0.3f; // 衰减
        }

        return type switch
        {
            OrderType.ATK => -pressure * 0.1f,   // 后备役不足时ATK需求下降
            OrderType.DEF => pressure * 0.15f,   // 后备役不足时DEF需求上升
            OrderType.RET => pressure * 0.25f,   // 后备役不足时RET需求显著上升
            _ => 0f
        };
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
    /// 应用交易冲击到Beta (GDD v6.0 + 反操纵修复)
    /// 每笔交易后调用，累积到 BetaCarry
    /// 使用回合开始流通盘作为固定基准，防止操纵
    /// </summary>
    public void ApplyTradeImpact(OrderType type, int quantity, bool isBuy)
    {
        var market = data.Market;

        // 反操纵修复：使用 TurnStartFloat 替代 MarketInventory
        int baseFloat = market.TurnStartFloat[type];
        if (baseFloat <= 0) return;

        // Impact = 交易量 / TurnStartFloat × ImpactCoefficient
        float impact = Mathf.Abs(quantity) / (float)baseFloat * balanceConfig.ImpactCoefficient;

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
    /// 回合开始时应用动量效应、恐慌效应和战场联动 (GDD v6.0 + 战场联动)
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

        // 战场联动效应
        ApplyBattlefieldLinkage();

        // 限制Beta范围，使用配置参数
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            data.Market.BetaCarry[type] = Mathf.Clamp(data.Market.BetaCarry[type], balanceConfig.BetaMin, balanceConfig.BetaMax);
        }
    }

    /// <summary>
    /// Beta 战场联动效应
    /// 战线突破、将军溃败会产生额外的价格冲击
    /// </summary>
    private void ApplyBattlefieldLinkage()
    {
        // 战线突破冲击：检查是否有战线压迫到 Grid 1 或 Grid 5
        foreach (var ally in data.Battle.AllyGenerals)
        {
            // 己方被压迫到 Grid 1（危机）
            if (ally.GridPosition == 1 && ally.Troops > 0)
            {
                data.Market.BetaCarry[OrderType.DEF] *= balanceConfig.BetaBreakthroughMultiplier;
                data.Market.BetaCarry[OrderType.RET] *= balanceConfig.BetaBreakthroughMultiplier;
            }

            // 己方推进到 Grid 5（胜利在望）
            if (ally.GridPosition >= 4 && ally.Troops > 0)
            {
                data.Market.BetaCarry[OrderType.ATK] *= balanceConfig.BetaBreakthroughMultiplier;
            }

            // 将军溃败冲击
            if (ally.Troops <= 0)
            {
                // 溃败导致 DEF/RET 需求激增，ATK 需求下降
                data.Market.BetaCarry[OrderType.DEF] *= 1.15f;
                data.Market.BetaCarry[OrderType.RET] *= 1.1f;
                data.Market.BetaCarry[OrderType.ATK] *= 0.9f;
            }
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

    /// <summary>
    /// 计算结算价（用于期货TWAP结算）
    /// 只用 Alpha 和 Beta，剥离 Gamma 以避免操纵
    /// </summary>
    public float CalculateSettlementPrice(OrderType type)
    {
        var basePrice = orderConfig.GetConfig(type).BasePrice;
        var alpha = CalculateAlpha(type) * balanceConfig.AlphaMultiplier;
        var beta = data.Market.BetaCarry[type];
        // 不使用 Gamma，避免流通盘操纵影响期货结算
        return basePrice * (1f + alpha) * beta;
    }
}
