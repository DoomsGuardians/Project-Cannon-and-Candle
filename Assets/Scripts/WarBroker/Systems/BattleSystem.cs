using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战场系统：管理战线、战斗结算、将军状态
/// </summary>
public class BattleSystem : ILogic
{
    private EventService eventService;
    private ResService resService;

    private GameBalanceConfig balanceConfig;
    private SkillConfig skillConfig;

    private CampaignRuntimeData campaignData;

    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;

        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        skillConfig = resService.LoadResource<SkillConfig>(ConfigPaths.SKILL_CONFIG);
    }

    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }

    public void SetRuntimeData(CampaignRuntimeData data)
    {
        campaignData = data;
    }

    #region 战斗结算

    public List<BattleResult> ResolveBattles(Dictionary<string, OrderType> enemyOrders)
    {
        var results = new List<BattleResult>();

        foreach (FrontlinePosition pos in Enum.GetValues(typeof(FrontlinePosition)))
        {
            var allyGeneral = campaignData.Battle.AllyGenerals.Find(g => g.Position == pos);
            var enemyGeneral = campaignData.Battle.EnemyGenerals.Find(g => g.Position == pos);

            if (allyGeneral == null || enemyGeneral == null) continue;
            if (allyGeneral.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;

            var allyOrder = allyGeneral.AssignedOrder ?? OrderType.DEF;

            var result = ResolveSingleBattle(allyGeneral, enemyGeneral,
                allyOrder,
                enemyOrders.GetValueOrDefault(enemyGeneral.GeneralId, OrderType.DEF));

            results.Add(result);
        }

        return results;
    }

    private BattleResult ResolveSingleBattle(GeneralData ally, GeneralData enemy,
        OrderType allyOrder, OrderType enemyOrder)
    {
        var result = new BattleResult
        {
            Position = ally.Position,
            AllyOrder = allyOrder,
            EnemyOrder = enemyOrder
        };

        // 记录战斗前的位置
        result.AllyOldPosition = ally.GridPosition;
        result.EnemyOldPosition = enemy.GridPosition;

        if (CheckDisobey(ally, allyOrder))
        {
            allyOrder = GetDisobeyOrder(ally);
            result.AllyOrder = allyOrder;
            result.Description = $"{ally.Name}抗命，自行执行{allyOrder}";
        }

        UpdateConsecutiveOrder(ally, allyOrder);

        // 判断接触/脱离状态
        if (IsEngaged(ally, enemy))
        {
            ProcessEngaged(ally, enemy, allyOrder, enemyOrder, result);
        }
        else
        {
            ProcessDisengaged(ally, enemy, allyOrder, enemyOrder, result);
        }

        CheckSkillTrigger(ally, allyOrder, result);
        ApplyBattleResult(ally, enemy, result);

        // 记录战斗后的位置
        result.AllyNewPosition = ally.GridPosition;
        result.EnemyNewPosition = enemy.GridPosition;

        // 发送战斗结果事件（在位置信息记录完成后）
        eventService.SendMessage((EventID)WarBrokerEventID.OnBattleResult, result, null);

        return result;
    }

    /// <summary>判断是否处于接触状态 (Gap = E - P - 1 <= 0)</summary>
    public bool IsEngaged(GeneralData ally, GeneralData enemy)
    {
        int gap = enemy.GridPosition - ally.GridPosition - 1;
        return gap <= 0;
    }

    /// <summary>获取两军之间的间隙</summary>
    public int GetGap(GeneralData ally, GeneralData enemy)
    {
        return enemy.GridPosition - ally.GridPosition - 1;
    }

    /// <summary>处理脱离状态战斗</summary>
    private void ProcessDisengaged(GeneralData ally, GeneralData enemy,
        OrderType allyOrder, OrderType enemyOrder, BattleResult result)
    {
        // 脱离状态：无直接战斗，只有移动
        result.AllyTroopChange = 0;
        result.EnemyTroopChange = 0;
        result.LineMovement = 0;

        // 计算双方预期移动后的位置
        int allyNewPos = ally.GridPosition;
        int enemyNewPos = enemy.GridPosition;

        // 计算己方预期位置
        switch (allyOrder)
        {
            case OrderType.ATK:
                allyNewPos = Mathf.Min(5, ally.GridPosition + 1);
                break;
            case OrderType.RET:
                if (ally.GridPosition > 1)
                    allyNewPos = ally.GridPosition - 1;
                break;
        }

        // 计算敌方预期位置
        switch (enemyOrder)
        {
            case OrderType.ATK:
                enemyNewPos = Mathf.Max(1, enemy.GridPosition - 1);
                break;
            case OrderType.RET:
                if (enemy.GridPosition < 5)
                    enemyNewPos = enemy.GridPosition + 1;
                break;
        }

        // 检查是否会重叠（双方都ATK且相隔一格时）
        if (allyNewPos >= enemyNewPos)
        {
            // 会重叠或交叉，双方在中间位置相遇，进入接触状态
            // 取两者中间位置，己方在较小位置，敌方在较大位置
            int meetPoint = (ally.GridPosition + enemy.GridPosition) / 2;
            allyNewPos = meetPoint;
            enemyNewPos = meetPoint + 1;

            // 确保边界
            allyNewPos = Mathf.Clamp(allyNewPos, 1, 4);
            enemyNewPos = Mathf.Clamp(enemyNewPos, 2, 5);
        }

        // 应用己方移动
        switch (allyOrder)
        {
            case OrderType.ATK:
                ally.GridPosition = allyNewPos;
                result.Description = $"{ally.Name}向前推进";
                break;
            case OrderType.DEF:
                result.Description = $"{ally.Name}原地驻扎";
                break;
            case OrderType.RET:
                if (ally.GridPosition > 1)
                {
                    ally.GridPosition = allyNewPos;
                    if (campaignData.Battle.CurrentReserves >= balanceConfig.RetHealCost)
                    {
                        result.AllyTroopChange = balanceConfig.RetHealHP;
                        campaignData.Battle.CurrentReserves -= balanceConfig.RetHealCost;
                        result.Description = $"{ally.Name}后撤休整，恢复{balanceConfig.RetHealHP}点兵力";
                    }
                    else
                    {
                        result.Description = $"{ally.Name}后撤，但后备役不足无法补员";
                    }
                }
                else
                {
                    // 在Grid 1时执行基地休整
                    if (campaignData.Battle.CurrentReserves >= balanceConfig.BaseRecoveryCost)
                    {
                        result.AllyTroopChange = balanceConfig.BaseRecoveryHP;
                        campaignData.Battle.CurrentReserves -= balanceConfig.BaseRecoveryCost;
                        result.Description = $"{ally.Name}在基地休整，恢复{balanceConfig.BaseRecoveryHP}点兵力";
                    }
                    else
                    {
                        result.Description = $"{ally.Name}已在最后方，后备役不足无法休整";
                    }
                }
                break;
        }

        // 应用敌方移动
        switch (enemyOrder)
        {
            case OrderType.ATK:
                enemy.GridPosition = enemyNewPos;
                break;
            case OrderType.DEF:
                // 不动
                break;
            case OrderType.RET:
                if (enemy.GridPosition < 5)
                {
                    enemy.GridPosition = enemyNewPos;
                    result.EnemyTroopChange = 1; // 敌方也回血
                }
                break;
        }

        // 检查是否因移动而进入接触状态
        if (IsEngaged(ally, enemy))
        {
            result.Description += "（双方进入接触状态）";
        }
    }

    /// <summary>处理接触状态战斗（使用对抗表 + 战术系统）(GDD v6.0)</summary>
    private void ProcessEngaged(GeneralData ally, GeneralData enemy,
        OrderType allyOrder, OrderType enemyOrder, BattleResult result)
    {
        // 战术星级抽取
        bool allyElite = RollTacticTier(ally);
        bool enemyElite = RollTacticTier(enemy);

        result.AllyTactic = GetTacticName(allyOrder, allyElite);
        result.EnemyTactic = GetTacticName(enemyOrder, enemyElite);

        // 查询对抗表基础值
        (int movement, int allyHPChange, int enemyHPChange) = GetCombatOutcome(allyOrder, enemyOrder, 0, 0);

        // 转换为伤害值（用于战术修正）
        float allyDamage = -enemyHPChange; // 己方造成的伤害
        float enemyDamage = -allyHPChange; // 敌方造成的伤害

        // 应用战术效果
        ApplyTacticEffects(ally, enemy, allyElite, enemyElite, ref allyDamage, ref enemyDamage);

        // 应用士气修正
        float allyMoraleModifier = ally.Morale / 100f;
        float enemyMoraleModifier = enemy.Morale / 100f;

        allyDamage *= allyMoraleModifier;
        enemyDamage *= enemyMoraleModifier;

        // 应用随机修正
        (allyDamage, result.WasCrit, result.WasFumble) = ApplyRandomModifier(allyDamage);
        (enemyDamage, _, _) = ApplyRandomModifier(enemyDamage);

        // 最终 HP 变化（负数表示受伤，正数表示回血）
        result.AllyTroopChange = allyHPChange - Mathf.RoundToInt(enemyDamage);
        result.EnemyTroopChange = enemyHPChange - Mathf.RoundToInt(allyDamage);

        // 处理 RET 回血时消耗后备役
        if (allyOrder == OrderType.RET && result.AllyTroopChange > 0)
        {
            if (campaignData.Battle.CurrentReserves >= result.AllyTroopChange)
            {
                campaignData.Battle.CurrentReserves -= result.AllyTroopChange;
            }
            else
            {
                result.AllyTroopChange = campaignData.Battle.CurrentReserves;
                campaignData.Battle.CurrentReserves = 0;
            }
        }

        // 战线移动
        result.LineMovement = movement;

        // 更新 GridPosition 基于战斗结果
        if (result.LineMovement > 0)
        {
            // 己方推进
            ally.GridPosition = Mathf.Min(5, ally.GridPosition + 1);
            enemy.GridPosition = Mathf.Min(5, enemy.GridPosition + 1);
        }
        else if (result.LineMovement < 0)
        {
            // 己方后撤
            ally.GridPosition = Mathf.Max(1, ally.GridPosition - 1);
            enemy.GridPosition = Mathf.Max(1, enemy.GridPosition - 1);
        }
        else if (result.LineMovement == -2)
        {
            // RET vs RET：双方拉开（特殊情况）
            ally.GridPosition = Mathf.Max(1, ally.GridPosition - 1);
            enemy.GridPosition = Mathf.Min(5, enemy.GridPosition + 1);
        }
    }

    private void UpdateConsecutiveOrder(GeneralData general, OrderType order)
    {
        if (general.LastOrder == order)
        {
            general.ConsecutiveOrderCount++;
        }
        else
        {
            general.ConsecutiveOrderCount = 1;
        }
        general.LastOrder = order;
    }

    private float CalculateCombatPower(GeneralData general, FrontlinePosition pos)
    {
        float basePower = 100f;

        float troopMod = general.Troops switch
        {
            >= 16 => 1.0f,   // 80% 兵力 (16/20)
            >= 10 => 0.9f,   // 50% 兵力 (10/20)
            >= 4 => 0.7f,    // 20% 兵力 (4/20)
            _ => 0.5f
        };

        float moraleMod = general.Morale switch
        {
            >= 80 => 1.1f,
            >= 50 => 1.0f,
            >= 30 => 0.9f,
            _ => 0.8f
        };

        float flankMod = CalculateFlankModifier(pos);

        return basePower * troopMod * moraleMod * flankMod;
    }

    private float CalculateFlankModifier(FrontlinePosition pos)
    {
        var frontlines = campaignData.Battle.Frontlines;
        float modifier = 1f;

        if (pos == FrontlinePosition.Center)
        {
            int leftPos = frontlines[FrontlinePosition.Left].LinePosition;
            int rightPos = frontlines[FrontlinePosition.Right].LinePosition;

            if (leftPos >= 4 && rightPos >= 4)
                modifier += balanceConfig.SurroundBonus;
            else if (leftPos <= 2 && rightPos <= 2)
                modifier -= balanceConfig.SurroundedMoralePenalty / 100f;
        }
        else
        {
            int centerPos = frontlines[FrontlinePosition.Center].LinePosition;
            int myPos = frontlines[pos].LinePosition;

            if (centerPos >= myPos + 1)
                modifier += balanceConfig.FlankSupportBonus;
            else if (centerPos <= myPos - 1)
                modifier -= balanceConfig.FlankThreatPenalty;
        }

        return modifier;
    }

    private (float power, bool isCrit, bool isFumble) ApplyRandomModifier(float basePower)
    {
        float roll = UnityEngine.Random.value;
        float modifier = UnityEngine.Random.Range(
            balanceConfig.RandomModifierMin,
            balanceConfig.RandomModifierMax);

        if (roll < balanceConfig.CritChance)
        {
            return (basePower * modifier * balanceConfig.CritMultiplier, true, false);
        }
        if (roll < balanceConfig.CritChance + balanceConfig.FumbleChance)
        {
            return (basePower * modifier * balanceConfig.FumbleMultiplier, false, true);
        }

        return (basePower * modifier, false, false);
    }

    /// <summary>
    /// 获取战斗结果 (GDD v6.0 完整 9 格对抗表)
    /// 返回：(战线移动, 己方HP变化, 敌方HP变化)
    /// </summary>
    private (int movement, int allyHP, int enemyHP) GetCombatOutcome(
        OrderType ally, OrderType enemy, float allyCombat, float enemyCombat)
    {
        // GDD v6.0 完整对抗表（基础值）
        var baseOutcome = (ally, enemy) switch
        {
            // 己方 ATK
            (OrderType.ATK, OrderType.ATK) => (0, -2, -2),   // 遭遇战
            (OrderType.ATK, OrderType.DEF) => (0, -4, -1),   // 攻坚战
            (OrderType.ATK, OrderType.RET) => (1, 0, 0),     // 追击（双方都推进）

            // 己方 DEF
            (OrderType.DEF, OrderType.ATK) => (0, -1, -4),   // 阻击
            (OrderType.DEF, OrderType.DEF) => (0, 0, 0),     // 静坐
            (OrderType.DEF, OrderType.RET) => (0, 0, 1),     // 目送（敌方回血）

            // 己方 RET
            (OrderType.RET, OrderType.ATK) => (-1, 1, 0),    // 撤离（己方回血）
            (OrderType.RET, OrderType.DEF) => (-1, 1, 0),    // 休整（己方回血）
            (OrderType.RET, OrderType.RET) => (-2, 1, 1),    // 脱离（双方回血，战线拉开）

            _ => (0, 0, 0)
        };

        // 注意：这里返回的是 HP 变化，负数表示受伤，正数表示回血
        return baseOutcome;
    }

    private void ApplyBattleResult(GeneralData ally, GeneralData enemy, BattleResult result)
    {
        var frontline = campaignData.Battle.Frontlines[result.Position];
        int newPos = Mathf.Clamp(frontline.LinePosition + result.LineMovement, 1, 5);

        if (newPos == frontline.LinePosition)
            frontline.StagnantTurns++;
        else
            frontline.StagnantTurns = 0;

        frontline.LinePosition = newPos;

        ally.Troops = Mathf.Clamp(ally.Troops + result.AllyTroopChange, 0, 20);
        enemy.Troops = Mathf.Clamp(enemy.Troops + result.EnemyTroopChange, 0, 20);

        if (result.LineMovement > 0)
        {
            ally.Morale = Mathf.Clamp(ally.Morale + 10, 0, 100);
            enemy.Morale = Mathf.Clamp(enemy.Morale - 10, 0, 100);
        }
        else if (result.LineMovement < 0)
        {
            ally.Morale = Mathf.Clamp(ally.Morale - 10, 0, 100);
            enemy.Morale = Mathf.Clamp(enemy.Morale + 10, 0, 100);
        }

        if (ally.GetStatus(balanceConfig) == GeneralStatus.Routed)
        {
            ally.ReorganizeTurns = balanceConfig.ReorganizeTurns;
            campaignData.Player.AuditValue += balanceConfig.AuditGeneralRouted;
            eventService.SendMessage((EventID)WarBrokerEventID.OnGeneralRouted, ally, null);
        }

        // 注意：OnBattleResult 事件在 ResolveSingleBattle 中发送，以确保位置信息已记录
    }

    #endregion

    #region 抗命检查

    private bool CheckDisobey(GeneralData general, OrderType order)
    {
        float bid = general.CalculateBid(order, 40f, balanceConfig);
        if (bid >= 0) return false;

        float disobeyChance = general.Trust switch
        {
            < 30 => balanceConfig.DisobeyChanceVeryLow,
            < 50 => balanceConfig.DisobeyChanceLow,
            _ => 0f
        };

        return UnityEngine.Random.value < disobeyChance;
    }

    private OrderType GetDisobeyOrder(GeneralData general)
    {
        foreach (var skill in general.Skills)
        {
            if (skill.DisobeyToOrder.HasValue && skill.DisobeyChance > 0)
            {
                if (UnityEngine.Random.value < skill.DisobeyChance)
                {
                    return skill.DisobeyToOrder.Value;
                }
            }
        }

        return general.Personality switch
        {
            GeneralPersonality.Fanatic => OrderType.ATK,
            GeneralPersonality.Conservative => OrderType.DEF,
            _ => OrderType.DEF
        };
    }

    #endregion

    #region 技能检查

    private void CheckSkillTrigger(GeneralData general, OrderType order, BattleResult result)
    {
        foreach (var skill in general.Skills)
        {
            if (!CheckSkillCondition(general, order, result, skill)) continue;

            ApplySkillEffect(skill, general, result);
            result.SkillTriggered = true;
            result.SkillName = skill.SkillName;
            eventService.SendMessage((EventID)WarBrokerEventID.OnSkillTriggered, general, skill);
        }
    }

    private bool CheckSkillCondition(GeneralData general, OrderType order,
        BattleResult result, SkillConfigItem skill)
    {
        if (skill.TriggerOrder.HasValue && skill.TriggerOrder.Value != order)
            return false;

        if (skill.TroopThreshold > 0 && general.Troops >= skill.TroopThreshold)
            return false;

        if (skill.MoraleThreshold > 0 && general.Morale < skill.MoraleThreshold)
            return false;

        if (skill.FrontlineThreshold > 0)
        {
            int linePos = campaignData.Battle.Frontlines[general.Position].LinePosition;
            if (linePos < skill.FrontlineThreshold)
                return false;
        }

        if (skill.RequireConsecutive && general.ConsecutiveOrderCount < 2)
            return false;

        if (skill.SkillId.Contains("charge") && result.LineMovement <= 0)
            return false;

        return true;
    }

    private void ApplySkillEffect(SkillConfigItem skill, GeneralData general, BattleResult result)
    {
        result.LineMovement += skill.BonusLineMovement;
        result.AllyTroopChange += skill.AllyTroopChange;
        result.EnemyTroopChange += skill.EnemyTroopChange;
    }

    #endregion

    #region 补员

    public void ApplyReinforcements()
    {
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed)
            {
                general.ReorganizeTurns--;
                continue;
            }

            int linePos = campaignData.Battle.Frontlines[general.Position].LinePosition;
            float positionMod = linePos switch
            {
                <= 2 => 1f,
                3 => 0.5f,
                _ => 0f
            };

            int reinforcement = Mathf.RoundToInt(balanceConfig.BaseReinforcement * positionMod);
            general.Troops = Mathf.Min(20, general.Troops + reinforcement);
        }
    }

    #endregion

    #region 战术系统 (GDD v6.0)

    /// <summary>
    /// 战术星级抽取 (GDD v6.0)
    /// 普通 90% / 精锐 10%
    /// 强化后：普通权重 × 0.5，精锐权重 × 5.0
    /// </summary>
    private bool RollTacticTier(GeneralData general)
    {
        float normalWeight = 90f;
        float eliteWeight = 10f;

        // 如果被强化，调整权重
        if (general.IntentSource == IntentSource.Reinforced)
        {
            normalWeight *= 0.5f;
            eliteWeight *= 5.0f;

            // 性格对战术的影响
            if (general.FinalIntent == OrderType.ATK)
            {
                if (general.Personality == GeneralPersonality.Fanatic)
                    eliteWeight *= 1.2f; // 额外 +20%
                else if (general.Personality == GeneralPersonality.Conservative)
                    eliteWeight *= 0.9f; // -10%
            }
            else if (general.FinalIntent == OrderType.DEF)
            {
                if (general.Personality == GeneralPersonality.Conservative)
                    eliteWeight *= 1.2f; // 额外 +20%
                else if (general.Personality == GeneralPersonality.Fanatic)
                    eliteWeight *= 0.9f; // -10%
            }
        }

        float totalWeight = normalWeight + eliteWeight;
        float roll = UnityEngine.Random.Range(0f, totalWeight);

        return roll >= normalWeight; // 精锐
    }

    /// <summary>获取战术名称</summary>
    private string GetTacticName(OrderType orderType, bool isElite)
    {
        return (orderType, isElite) switch
        {
            (OrderType.ATK, false) => "步兵冲锋",
            (OrderType.ATK, true) => "精锐突击",
            (OrderType.DEF, false) => "坚守阵地",
            (OrderType.DEF, true) => "战地医院",
            (OrderType.RET, false) => "有序撤退",
            (OrderType.RET, true) => "焦土战术",
            _ => "未知战术"
        };
    }

    /// <summary>
    /// 应用战术效果 (GDD v6.0)
    /// </summary>
    private void ApplyTacticEffects(GeneralData ally, GeneralData enemy,
        bool allyElite, bool enemyElite,
        ref float allyDamage, ref float enemyDamage)
    {
        // 己方战术效果
        if (allyElite)
        {
            switch (ally.FinalIntent)
            {
                case OrderType.ATK:
                    // 精锐突击：+2 伤害，无视 DEF 减伤
                    allyDamage += 2f;
                    if (enemy.FinalIntent == OrderType.DEF)
                    {
                        // 无视 DEF 减伤效果（在对抗表中 ATK vs DEF 是 -4 vs -1）
                        // 这里可以调整为更激进的伤害
                        enemyDamage += 2f; // 额外伤害
                    }
                    break;

                case OrderType.DEF:
                    // 战地医院：HP+3（消耗 Reserves 3）
                    if (campaignData.Battle.CurrentReserves >= 3)
                    {
                        ally.Troops += 3;
                        campaignData.Battle.CurrentReserves -= 3;
                    }
                    break;

                case OrderType.RET:
                    // 焦土战术：HP+1（标准），追击者受伤 -2
                    if (enemy.FinalIntent == OrderType.ATK)
                    {
                        enemyDamage = -2f; // 追击者受伤
                    }
                    break;
            }
        }

        // 敌方战术效果（镜像逻辑）
        if (enemyElite)
        {
            switch (enemy.FinalIntent)
            {
                case OrderType.ATK:
                    enemyDamage += 2f;
                    if (ally.FinalIntent == OrderType.DEF)
                    {
                        allyDamage += 2f;
                    }
                    break;

                case OrderType.DEF:
                    // 敌方战地医院（假设敌方也有后备役机制）
                    enemy.Troops += 3;
                    break;

                case OrderType.RET:
                    if (ally.FinalIntent == OrderType.ATK)
                    {
                        allyDamage = -2f;
                    }
                    break;
            }
        }
    }

    #endregion

    #region 胜负检查

    public bool CheckVictory()
    {
        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (frontline.LinePosition < 5) return false;
        }
        return true;
    }

    public bool CheckDefeat()
    {
        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (frontline.LinePosition > 1) return false;
        }
        return true;
    }

    public bool CheckNegotiation()
    {
        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (frontline.StagnantTurns < 3) return false;
        }
        return true;
    }

    #endregion
}
