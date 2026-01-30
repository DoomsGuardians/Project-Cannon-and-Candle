using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 三因子定价引擎
/// Price = P_base × (1 + Alpha) × Beta × Gamma
/// Alpha: 战场态势因子（接触状态、血量）
/// Beta: 市场动量因子（价格趋势、恐慌）
/// Gamma: 流通盘因子（供需关系）
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
        var alpha = CalculateAlpha(type);
        var beta = CalculateBeta(type);
        var gamma = CalculateGamma(type);

        return basePrice * (1f + alpha) * beta * gamma;
    }

    /// <summary>
    /// Alpha因子：战场态势
    /// - 接触状态基础加成
    /// - 临界状态修正
    /// - 低血量修正
    /// </summary>
    private float CalculateAlpha(OrderType type)
    {
        float alpha = 0f;

        // 检查是否有战线处于接触状态
        bool hasEngaged = false;
        bool hasCritical = false;
        bool hasLowHP = false;

        foreach (var general in data.Battle.AllyGenerals)
        {
            if (general.Troops <= 0) continue;

            var enemy = GetOpposingGeneral(general);
            if (enemy != null && enemy.Troops > 0)
            {
                if (IsEngaged(general, enemy))
                {
                    hasEngaged = true;
                }
            }

            var status = general.GetStatus(balanceConfig);
            if (status == GeneralStatus.Critical)
            {
                hasCritical = true;
            }
            else if (status == GeneralStatus.Wounded)
            {
                hasLowHP = true;
            }
        }

        // 接触状态基础加成
        if (hasEngaged)
        {
            alpha += balanceConfig.AlphaContactBase;
        }

        // 根据指令类型和战场状态调整
        if (type == OrderType.ATK)
        {
            // 进攻令在接触状态下需求更高
            if (hasEngaged) alpha += GetPositionModifier(type, 0.1f);
        }
        else if (type == OrderType.DEF)
        {
            // 防守令在临界状态下需求更高
            if (hasCritical) alpha += balanceConfig.AlphaCriticalBonus;
            if (hasLowHP) alpha += balanceConfig.AlphaLowHPBonus;
        }
        else if (type == OrderType.RET)
        {
            // 撤退令在临界状态下需求最高
            if (hasCritical) alpha += balanceConfig.AlphaCriticalBonus * 1.5f;
            if (hasLowHP) alpha += balanceConfig.AlphaLowHPBonus;
        }

        return alpha;
    }

    /// <summary>
    /// Beta因子：市场动量
    /// - 价格趋势（连续上涨/下跌）
    /// - 恐慌效应（后备役不足）
    /// </summary>
    private float CalculateBeta(OrderType type)
    {
        float beta = 1f;

        // 价格动量
        float priceChangeRatio = GetPriceChangeRatio(type);
        if (Mathf.Abs(priceChangeRatio) > balanceConfig.BetaMomentumThreshold)
        {
            // 价格上涨时继续推高，下跌时继续压低
            beta *= priceChangeRatio > 0
                ? balanceConfig.BetaMomentumMultiplier
                : 2f - balanceConfig.BetaMomentumMultiplier;
        }

        // 恐慌效应：后备役不足时RET价格飙升
        if (type == OrderType.RET && data.Battle.CurrentReserves < balanceConfig.BetaPanicReserveThreshold)
        {
            beta *= balanceConfig.BetaPanicMultiplier;
        }

        return beta;
    }

    /// <summary>
    /// Gamma因子：流通盘
    /// - 库存/初始流通盘比例
    /// - 库存越少价格越高
    /// </summary>
    private float CalculateGamma(OrderType type)
    {
        var market = data.Market;
        float currentInventory = market.MarketInventory[type];
        float initialFloat = market.InitialFloat[type];

        if (initialFloat <= 0) return 1f;

        // 流通盘比例：当前库存 / 初始流通盘
        float ratio = currentInventory / initialFloat;

        // Gamma = 1 + sensitivity * (1 - ratio)
        // 当库存为初始值时，Gamma = 1
        // 当库存为0时，Gamma = 1 + sensitivity
        float gamma = 1f + balanceConfig.GammaSensitivity * (1f - ratio);

        return Mathf.Max(0.5f, gamma); // 最低0.5倍
    }

    /// <summary>获取位置修正（基于战线位置）</summary>
    private float GetPositionModifier(OrderType type, float center)
    {
        float modifier = 0f;
        int engagedCount = 0;

        foreach (var general in data.Battle.AllyGenerals)
        {
            if (general.Troops <= 0) continue;

            var enemy = GetOpposingGeneral(general);
            if (enemy != null && enemy.Troops > 0 && IsEngaged(general, enemy))
            {
                engagedCount++;
            }
        }

        // 多条战线接触时加成更高
        modifier = center * engagedCount;

        return modifier;
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
        return (CalculateAlpha(type), CalculateBeta(type), CalculateGamma(type));
    }
}
