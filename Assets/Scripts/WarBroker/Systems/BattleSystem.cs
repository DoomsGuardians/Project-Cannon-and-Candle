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

        // 我方行动
        switch (allyOrder)
        {
            case OrderType.ATK:
                // 推进：GridPosition++
                ally.GridPosition = Mathf.Min(5, ally.GridPosition + 1);
                result.Description = $"{ally.Name}向前推进";
                break;
            case OrderType.DEF:
                // 驻扎：不动
                result.Description = $"{ally.Name}原地驻扎";
                break;
            case OrderType.RET:
                // 后撤+回血：GridPosition--, HP+1, 消耗后备役
                if (ally.GridPosition > 1)
                {
                    ally.GridPosition--;
                    if (campaignData.Battle.CurrentReserves > 0)
                    {
                        result.AllyTroopChange = 1;
                        campaignData.Battle.CurrentReserves--;
                        result.Description = $"{ally.Name}后撤休整，恢复1点兵力";
                    }
                    else
                    {
                        result.Description = $"{ally.Name}后撤，但后备役不足无法补员";
                    }
                }
                else
                {
                    result.Description = $"{ally.Name}已在最后方，无法后撤";
                }
                break;
        }

        // 敌方行动
        switch (enemyOrder)
        {
            case OrderType.ATK:
                enemy.GridPosition = Mathf.Max(1, enemy.GridPosition - 1);
                break;
            case OrderType.DEF:
                // 不动
                break;
            case OrderType.RET:
                if (enemy.GridPosition < 5)
                {
                    enemy.GridPosition++;
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

    /// <summary>处理接触状态战斗（使用对抗表）</summary>
    private void ProcessEngaged(GeneralData ally, GeneralData enemy,
        OrderType allyOrder, OrderType enemyOrder, BattleResult result)
    {
        float allyCombat = CalculateCombatPower(ally, ally.Position);
        float enemyCombat = CalculateCombatPower(enemy, ally.Position);

        (allyCombat, result.WasCrit, result.WasFumble) = ApplyRandomModifier(allyCombat);
        (enemyCombat, _, _) = ApplyRandomModifier(enemyCombat);

        (result.LineMovement, result.AllyTroopChange, result.EnemyTroopChange) =
            GetCombatOutcome(allyOrder, enemyOrder, allyCombat, enemyCombat);

        // 处理RET回血时消耗后备役
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

        // 更新GridPosition基于战斗结果
        if (result.LineMovement > 0)
        {
            // 我方推进
            ally.GridPosition = Mathf.Min(5, ally.GridPosition + 1);
            enemy.GridPosition = Mathf.Min(5, enemy.GridPosition + 1);
        }
        else if (result.LineMovement < 0)
        {
            // 敌方推进
            ally.GridPosition = Mathf.Max(1, ally.GridPosition - 1);
            enemy.GridPosition = Mathf.Max(1, enemy.GridPosition - 1);
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

    private (int movement, int allyLoss, int enemyLoss) GetCombatOutcome(
        OrderType ally, OrderType enemy, float allyCombat, float enemyCombat)
    {
        var baseOutcome = (ally, enemy) switch
        {
            (OrderType.ATK, OrderType.ATK) => (0, -15, -15),
            (OrderType.ATK, OrderType.DEF) => (0, -10, -10),
            (OrderType.ATK, OrderType.RET) => (1, 0, 0),
            (OrderType.DEF, OrderType.ATK) => (0, -10, -10),
            (OrderType.DEF, OrderType.DEF) => (0, 0, 0),
            (OrderType.DEF, OrderType.RET) => (0, 0, 0),
            (OrderType.RET, OrderType.ATK) => (-1, 0, 0),
            (OrderType.RET, OrderType.DEF) => (0, 0, 0),
            (OrderType.RET, OrderType.RET) => (0, 0, 0),
            _ => (0, 0, 0)
        };

        float combatRatio = allyCombat / Mathf.Max(1, enemyCombat);
        int allyLoss = Mathf.RoundToInt(baseOutcome.Item2 * (2f - combatRatio));
        int enemyLoss = Mathf.RoundToInt(baseOutcome.Item3 * combatRatio);

        return (baseOutcome.Item1, allyLoss, enemyLoss);
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

        eventService.SendMessage((EventID)WarBrokerEventID.OnBattleResult, result, null);
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
