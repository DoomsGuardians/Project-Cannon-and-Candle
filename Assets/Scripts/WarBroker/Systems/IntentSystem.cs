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
    private CampaignRuntimeData campaignData;

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

    /// <summary>设置战役数据引用，用于判断交战状态</summary>
    public void SetCampaignData(CampaignRuntimeData data)
    {
        campaignData = data;
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

        // 判断是己方还是敌方将军
        bool isAlly = IsAllyGeneral(general);
        var gridModifier = GetGridModifier(general.GridPosition, isAlly);
        var engagementModifier = GetEngagementModifier(general);

        var finalWeights = new Dictionary<OrderType, float>();

        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            float weight = baseWeights[type];
            weight *= hpModifier.GetValueOrDefault(type, 1f);
            weight *= gridModifier.GetValueOrDefault(type, 1f);
            weight *= engagementModifier.GetValueOrDefault(type, 1f);
            finalWeights[type] = Mathf.Max(0.01f, weight);
        }

        return finalWeights;
    }

    /// <summary>判断将军是否为己方（玩家方）</summary>
    private bool IsAllyGeneral(GeneralData general)
    {
        if (campaignData == null) return true; // 默认假设是己方

        return campaignData.Battle.AllyGenerals.Contains(general);
    }

    /// <summary>获取性格基础权重 (GDD v6.0 - 整数权重)</summary>
    private Dictionary<OrderType, float> GetBaseWeights(GeneralPersonality personality)
    {
        return personality switch
        {
            GeneralPersonality.Fanatic => new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 70f },
                { OrderType.DEF, 20f },
                { OrderType.RET, 10f }
            },
            GeneralPersonality.Conservative => new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 20f },
                { OrderType.DEF, 50f },
                { OrderType.RET, 30f }
            },
            GeneralPersonality.Opportunist => new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 40f },
                { OrderType.DEF, 35f },
                { OrderType.RET, 25f }
            },
            _ => new Dictionary<OrderType, float>
            {
                { OrderType.ATK, 33f },
                { OrderType.DEF, 34f },
                { OrderType.RET, 33f }
            }
        };
    }

    /// <summary>获取血量修正 (GDD v6.0 完整修正表)</summary>
    private Dictionary<OrderType, float> GetHPModifier(GeneralPersonality personality, int troops)
    {
        var modifier = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 1f },
            { OrderType.DEF, 1f },
            { OrderType.RET, 1f }
        };

        // 根据 HP 和性格应用修正
        if (troops >= 16 && troops <= 20) // 满编
        {
            if (personality == GeneralPersonality.Fanatic)
            {
                modifier[OrderType.ATK] = 1.2f;
                modifier[OrderType.DEF] = 0.8f;
                modifier[OrderType.RET] = 0.5f;
            }
            else if (personality == GeneralPersonality.Conservative)
            {
                modifier[OrderType.ATK] = 1.0f;
                modifier[OrderType.DEF] = 1.0f;
                modifier[OrderType.RET] = 1.0f;
            }
            else if (personality == GeneralPersonality.Opportunist)
            {
                modifier[OrderType.ATK] = 1.1f;
                modifier[OrderType.DEF] = 1.0f;
                modifier[OrderType.RET] = 0.9f;
            }
        }
        else if (troops >= 11 && troops <= 15) // 健康
        {
            if (personality == GeneralPersonality.Fanatic)
            {
                modifier[OrderType.ATK] = 1.1f;
                modifier[OrderType.DEF] = 0.9f;
                modifier[OrderType.RET] = 0.7f;
            }
            else if (personality == GeneralPersonality.Conservative)
            {
                modifier[OrderType.ATK] = 0.8f;
                modifier[OrderType.DEF] = 1.1f;
                modifier[OrderType.RET] = 1.2f;
            }
            else if (personality == GeneralPersonality.Opportunist)
            {
                modifier[OrderType.ATK] = 1.0f;
                modifier[OrderType.DEF] = 1.0f;
                modifier[OrderType.RET] = 1.0f;
            }
        }
        else if (troops >= 6 && troops <= 10) // 受伤
        {
            if (personality == GeneralPersonality.Fanatic)
            {
                modifier[OrderType.ATK] = 1.0f;
                modifier[OrderType.DEF] = 1.0f;
                modifier[OrderType.RET] = 1.0f;
            }
            else if (personality == GeneralPersonality.Conservative)
            {
                modifier[OrderType.ATK] = 0.5f;
                modifier[OrderType.DEF] = 1.2f;
                modifier[OrderType.RET] = 1.5f;
            }
            else if (personality == GeneralPersonality.Opportunist)
            {
                modifier[OrderType.ATK] = 0.8f;
                modifier[OrderType.DEF] = 1.0f;
                modifier[OrderType.RET] = 1.2f;
            }
        }
        else if (troops >= 1 && troops <= 5) // 濒死（硬性规则）
        {
            if (personality == GeneralPersonality.Fanatic)
            {
                // 狂热者濒死：ATK暴增，RET归零（玉碎冲锋）
                modifier[OrderType.ATK] = 1.5f;
                modifier[OrderType.DEF] = 0.5f;
                modifier[OrderType.RET] = 0f; // 硬性规则：RET归零
            }
            else if (personality == GeneralPersonality.Conservative)
            {
                // 保守者濒死：ATK归零，RET暴增（求生本能）
                modifier[OrderType.ATK] = 0f; // 硬性规则：ATK归零
                modifier[OrderType.DEF] = 1.5f;
                modifier[OrderType.RET] = 3.0f;
            }
            else if (personality == GeneralPersonality.Opportunist)
            {
                modifier[OrderType.ATK] = 0.2f;
                modifier[OrderType.DEF] = 1.0f;
                modifier[OrderType.RET] = 2.0f;
            }
        }

        return modifier;
    }

    /// <summary>
    /// 获取战线位置修正 (GDD v7.0)
    /// 硬性规则：
    /// - 己方将军在 Grid 1（己方基地）禁止 RET
    /// - 己方将军在 Grid 5（敌方基地）禁止 ATK（已到头）
    /// - 敌方将军在 Grid 5（敌方基地=他们的己方基地）禁止 RET
    /// - 敌方将军在 Grid 1（己方基地=他们的敌方基地）禁止 ATK
    /// </summary>
    private Dictionary<OrderType, float> GetGridModifier(int gridPosition, bool isAlly = true)
    {
        var modifier = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 1f },
            { OrderType.DEF, 1f },
            { OrderType.RET, 1f }
        };

        if (isAlly)
        {
            // 己方将军视角
            switch (gridPosition)
            {
                case 1: // 己方基地（硬性规则：RET禁止）
                    modifier[OrderType.ATK] = 1.5f;
                    modifier[OrderType.DEF] = 2.0f;
                    modifier[OrderType.RET] = 0f; // 硬性规则：退无可退
                    break;

                case 2: // 腹地
                    modifier[OrderType.ATK] = 1.0f;
                    modifier[OrderType.DEF] = 1.2f;
                    modifier[OrderType.RET] = 1.5f;
                    break;

                case 3: // 中线
                    modifier[OrderType.ATK] = 1.0f;
                    modifier[OrderType.DEF] = 1.0f;
                    modifier[OrderType.RET] = 1.0f;
                    break;

                case 4: // 敌方腹地
                    modifier[OrderType.ATK] = 1.2f;
                    modifier[OrderType.DEF] = 1.0f;
                    modifier[OrderType.RET] = 0.8f;
                    break;

                case 5: // 敌方基地（硬性规则：ATK禁止，已到头）
                    modifier[OrderType.ATK] = 0f; // 硬性规则：已到敌方大本营，无法再进攻
                    modifier[OrderType.DEF] = 2.0f;
                    modifier[OrderType.RET] = 1.0f;
                    break;
            }
        }
        else
        {
            // 敌方将军视角（Grid 坐标相反）
            switch (gridPosition)
            {
                case 5: // 敌方基地（对敌方来说是己方基地，RET禁止）
                    modifier[OrderType.ATK] = 1.5f;
                    modifier[OrderType.DEF] = 2.0f;
                    modifier[OrderType.RET] = 0f; // 硬性规则：退无可退
                    break;

                case 4: // 敌方腹地（对敌方来说是己方腹地）
                    modifier[OrderType.ATK] = 1.0f;
                    modifier[OrderType.DEF] = 1.2f;
                    modifier[OrderType.RET] = 1.5f;
                    break;

                case 3: // 中线
                    modifier[OrderType.ATK] = 1.0f;
                    modifier[OrderType.DEF] = 1.0f;
                    modifier[OrderType.RET] = 1.0f;
                    break;

                case 2: // 己方腹地（对敌方来说是敌方腹地）
                    modifier[OrderType.ATK] = 1.2f;
                    modifier[OrderType.DEF] = 1.0f;
                    modifier[OrderType.RET] = 0.8f;
                    break;

                case 1: // 己方基地（对敌方来说是敌方基地，ATK禁止）
                    modifier[OrderType.ATK] = 0f; // 硬性规则：已到敌方大本营，无法再进攻
                    modifier[OrderType.DEF] = 2.0f;
                    modifier[OrderType.RET] = 1.0f;
                    break;
            }
        }

        return modifier;
    }

    /// <summary>
    /// 获取交战状态修正
    /// 未交战时减少防守和撤退的概率，鼓励前进接敌
    /// </summary>
    private Dictionary<OrderType, float> GetEngagementModifier(GeneralData general)
    {
        var modifier = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 1f },
            { OrderType.DEF, 1f },
            { OrderType.RET, 1f }
        };

        // 如果没有战役数据，无法判断交战状态，返回默认修正
        if (campaignData == null) return modifier;

        // 判断该将军是否处于交战状态
        int gap = GetGapToEnemy(general);

        // gap == -1 表示无法计算（对方将军不存在或已溃败）
        if (gap == -1) return modifier;

        // 已交战 (gap <= 0)：不修正，正常决策
        if (gap <= 0) return modifier;

        // 未交战：根据间隙距离调整权重
        // gap 越大，越应该进攻接近敌人，越不应该防守/撤退
        if (gap >= 3)
        {
            // 距离很远：大幅鼓励进攻，大幅抑制防守/撤退
            modifier[OrderType.ATK] = 2.0f;
            modifier[OrderType.DEF] = 0.3f;
            modifier[OrderType.RET] = 0.1f;
        }
        else if (gap == 2)
        {
            // 距离较远：鼓励进攻，抑制防守/撤退
            modifier[OrderType.ATK] = 1.5f;
            modifier[OrderType.DEF] = 0.5f;
            modifier[OrderType.RET] = 0.3f;
        }
        else if (gap == 1)
        {
            // 即将交战：轻微鼓励进攻
            modifier[OrderType.ATK] = 1.2f;
            modifier[OrderType.DEF] = 0.8f;
            modifier[OrderType.RET] = 0.6f;
        }

        return modifier;
    }

    /// <summary>
    /// 获取将军与对面敌人的间隙距离
    /// 返回 -1 表示无法计算
    /// </summary>
    private int GetGapToEnemy(GeneralData general)
    {
        if (campaignData == null) return -1;

        // 判断这个将军是己方还是敌方
        bool isAlly = campaignData.Battle.AllyGenerals.Contains(general);
        bool isEnemy = campaignData.Battle.EnemyGenerals.Contains(general);

        GeneralData opponent = null;

        if (isAlly)
        {
            // 己方将军，找对面的敌方将军
            opponent = campaignData.Battle.EnemyGenerals.Find(g => g.Position == general.Position);
        }
        else if (isEnemy)
        {
            // 敌方将军，找对面的己方将军
            opponent = campaignData.Battle.AllyGenerals.Find(g => g.Position == general.Position);
        }

        if (opponent == null) return -1;
        if (balanceConfig != null && opponent.GetStatus(balanceConfig) == GeneralStatus.Routed) return -1;

        // 计算间隙：敌方位置 - 己方位置 - 1
        // 对于己方将军：gap = enemy.GridPosition - ally.GridPosition - 1
        // 对于敌方将军：gap = ally.GridPosition - enemy.GridPosition - 1 (从敌方视角)
        // 但实际上敌方的 GridPosition 是从敌方视角的，所以统一用 opponent - general
        if (isAlly)
        {
            return opponent.GridPosition - general.GridPosition - 1;
        }
        else
        {
            // 敌方将军的视角：己方将军在他的"前方"
            return general.GridPosition - opponent.GridPosition - 1;
        }
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
