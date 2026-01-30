using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将军意图系统
/// 负责生成默认意图、处理强化和篡改
/// </summary>
public class IntentSystem
{
    private GameBalanceConfig balanceConfig;

    // 强化消耗
    private const int ReinforceCost = 1;
    // 篡改消耗
    private const int OverrideCost = 3;
    // 强化信任度变化
    private const int ReinforceTrustChange = 5;
    // 强化士气变化
    private const int ReinforceMoraleChange = 5;
    // 篡改信任度变化
    private const int OverrideTrustChange = -15;
    // 篡改士气变化
    private const int OverrideMoraleChange = -5;

    public void Init(GameBalanceConfig config)
    {
        balanceConfig = config;
    }

    /// <summary>生成将军的默认意图</summary>
    public OrderType GenerateDefaultIntent(GeneralData general)
    {
        var weights = CalculateWeights(general);
        return SelectByWeight(weights);
    }

    /// <summary>
    /// 尝试强化将军意图
    /// 强化规则：消耗1份同类型指令，信任度+5，士气+5
    /// 只能强化与默认意图相同的指令
    /// </summary>
    public bool TryReinforce(GeneralData general, OrderType orderType, PlayerData player)
    {
        // 检查是否与默认意图相同
        if (general.DefaultIntent != orderType)
        {
            Debug.LogWarning($"强化失败：指令类型 {orderType} 与默认意图 {general.DefaultIntent} 不同");
            return false;
        }

        // 检查玩家库存
        if (!player.Inventory.ContainsKey(orderType) || player.Inventory[orderType] < ReinforceCost)
        {
            Debug.LogWarning($"强化失败：{orderType} 库存不足");
            return false;
        }

        // 执行强化
        player.Inventory[orderType] -= ReinforceCost;
        general.FinalIntent = orderType;
        general.IntentSource = IntentSource.Reinforced;
        general.Trust = Mathf.Clamp(general.Trust + ReinforceTrustChange, 0, 100);
        general.Morale = Mathf.Clamp(general.Morale + ReinforceMoraleChange, 0, 100);

        return true;
    }

    /// <summary>
    /// 尝试篡改将军意图
    /// 篡改规则：消耗3份目标类型指令，信任度-15，士气-5
    /// 只能篡改为与默认意图不同的指令
    /// </summary>
    public bool TryOverride(GeneralData general, OrderType orderType, PlayerData player)
    {
        // 检查是否与默认意图不同
        if (general.DefaultIntent == orderType)
        {
            Debug.LogWarning($"篡改失败：指令类型 {orderType} 与默认意图相同，请使用强化");
            return false;
        }

        // 检查玩家库存
        if (!player.Inventory.ContainsKey(orderType) || player.Inventory[orderType] < OverrideCost)
        {
            Debug.LogWarning($"篡改失败：{orderType} 库存不足（需要 {OverrideCost}）");
            return false;
        }

        // 执行篡改
        player.Inventory[orderType] -= OverrideCost;
        general.FinalIntent = orderType;
        general.IntentSource = IntentSource.Overridden;
        general.Trust = Mathf.Clamp(general.Trust + OverrideTrustChange, 0, 100);
        general.Morale = Mathf.Clamp(general.Morale + OverrideMoraleChange, 0, 100);

        return true;
    }

    /// <summary>计算各指令类型的权重</summary>
    private Dictionary<OrderType, float> CalculateWeights(GeneralData general)
    {
        var baseWeights = GetBaseWeights(general.Personality);
        var hpModifier = GetHPModifier(general.Personality, general.Troops);
        var gridModifier = GetGridModifier(general.GridPosition);

        var finalWeights = new Dictionary<OrderType, float>();

        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            float weight = baseWeights[type];
            weight *= hpModifier.GetValueOrDefault(type, 1f);
            weight *= gridModifier.GetValueOrDefault(type, 1f);
            finalWeights[type] = Mathf.Max(0.01f, weight);
        }

        return finalWeights;
    }

    /// <summary>获取性格基础权重</summary>
    private Dictionary<OrderType, float> GetBaseWeights(GeneralPersonality personality)
    {
        return personality switch
        {
            GeneralPersonality.Fanatic => new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 0.6f },
                { OrderType.DEF, 0.3f },
                { OrderType.RET, 0.1f }
            },
            GeneralPersonality.Conservative => new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 0.2f },
                { OrderType.DEF, 0.5f },
                { OrderType.RET, 0.3f }
            },
            GeneralPersonality.Opportunist => new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 0.4f },
                { OrderType.DEF, 0.35f },
                { OrderType.RET, 0.25f }
            },
            _ => new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 0.33f },
                { OrderType.DEF, 0.34f },
                { OrderType.RET, 0.33f }
            }
        };
    }

    /// <summary>获取血量修正</summary>
    private Dictionary<OrderType, float> GetHPModifier(GeneralPersonality personality, int troops)
    {
        var modifier = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 1f },
            { OrderType.DEF, 1f },
            { OrderType.RET, 1f }
        };

        // 低血量时调整权重
        if (troops <= 5) // Critical
        {
            modifier[OrderType.ATK] = personality == GeneralPersonality.Fanatic ? 0.8f : 0.3f;
            modifier[OrderType.DEF] = 1.5f;
            modifier[OrderType.RET] = personality == GeneralPersonality.Fanatic ? 1.2f : 2.0f;
        }
        else if (troops <= 10) // Wounded
        {
            modifier[OrderType.ATK] = personality == GeneralPersonality.Fanatic ? 1.0f : 0.6f;
            modifier[OrderType.DEF] = 1.3f;
            modifier[OrderType.RET] = personality == GeneralPersonality.Fanatic ? 1.0f : 1.4f;
        }
        else if (troops >= 18) // Full Strength
        {
            modifier[OrderType.ATK] = 1.3f;
            modifier[OrderType.DEF] = 0.9f;
            modifier[OrderType.RET] = 0.7f;
        }

        return modifier;
    }

    /// <summary>获取战线位置修正</summary>
    private Dictionary<OrderType, float> GetGridModifier(int gridPosition)
    {
        var modifier = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 1f },
            { OrderType.DEF, 1f },
            { OrderType.RET, 1f }
        };

        // 位置1-2（靠近己方）：更倾向进攻
        if (gridPosition <= 2)
        {
            modifier[OrderType.ATK] = 1.3f;
            modifier[OrderType.RET] = 0.7f;
        }
        // 位置4-5（靠近敌方）：更倾向防守/撤退
        else if (gridPosition >= 4)
        {
            modifier[OrderType.ATK] = 0.8f;
            modifier[OrderType.DEF] = 1.2f;
            modifier[OrderType.RET] = 1.3f;
        }

        return modifier;
    }

    /// <summary>根据权重随机选择</summary>
    private OrderType SelectByWeight(Dictionary<OrderType, float> weights)
    {
        float totalWeight = 0f;
        foreach (var w in weights.Values)
        {
            totalWeight += w;
        }

        float random = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (random <= cumulative)
            {
                return kvp.Key;
            }
        }

        return OrderType.DEF; // 默认返回防守
    }

    /// <summary>获取强化消耗</summary>
    public int GetReinforceCost() => ReinforceCost;

    /// <summary>获取篡改消耗</summary>
    public int GetOverrideCost() => OverrideCost;
}
