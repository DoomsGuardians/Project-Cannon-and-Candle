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

    private CampaignRuntimeData campaignData;

    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;

        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
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

        // 新领土系统：追踪是否占领了新格子
        bool bothATK = (allyOrder == OrderType.ATK && enemyOrder == OrderType.ATK);

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
                int allyTargetGrid = allyNewPos;
                ally.GridPosition = allyNewPos;
                result.Description = $"{ally.Name}向前推进";
                // 只有对方不是 ATK 时才能占领
                if (!bothATK && allyTargetGrid > ally.GridPosition - 1)
                {
                    result.AllyCapturedGrid = allyTargetGrid;
                }
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
                int enemyTargetGrid = enemyNewPos;
                enemy.GridPosition = enemyNewPos;
                // 只有对方不是 ATK 时才能占领
                if (!bothATK && enemyTargetGrid < enemy.GridPosition + 1)
                {
                    result.EnemyCapturedGrid = enemyTargetGrid;
                }
                break;
            case OrderType.DEF:
                // 不动
                break;
            case OrderType.RET:
                if (enemy.GridPosition < 5)
                {
                    enemy.GridPosition = enemyNewPos;
                    // GDD v7.0: 敌方 RET 回血也消耗后备役
                    if (campaignData.Battle.EnemyReserves >= 1)
                    {
                        result.EnemyTroopChange = 1;
                        campaignData.Battle.EnemyReserves -= 1;
                    }
                }
                else
                {
                    // 敌方在 Grid 5（敌方基地）执行基地休整
                    if (campaignData.Battle.EnemyReserves >= 2)
                    {
                        result.EnemyTroopChange = 2;
                        campaignData.Battle.EnemyReserves -= 2;
                    }
                }
                break;
        }

        // 检查是否因移动而进入接触状态
        if (IsEngaged(ally, enemy))
        {
            result.Description += "（双方进入接触状态）";
        }
    }

    /// <summary>处理接触状态战斗（使用对抗表 + 确定性强化）(GDD v7.0)</summary>
    private void ProcessEngaged(GeneralData ally, GeneralData enemy,
        OrderType allyOrder, OrderType enemyOrder, BattleResult result)
    {
        // 查询对抗表基础值
        (int movement, int allyHPChange, int enemyHPChange) = GetCombatOutcome(allyOrder, enemyOrder, 0, 0);

        // GDD v7.0: 确定性强化效果
        // 己方强化效果
        if (ally.IntentSource == IntentSource.Reinforced)
        {
            switch (allyOrder)
            {
                case OrderType.ATK:
                    enemyHPChange -= 2; // 造成伤害 +2（敌方多扣2血）
                    break;
                case OrderType.DEF:
                    allyHPChange += 2; // 受到伤害 -2（己方少扣2血）
                    break;
                case OrderType.RET:
                    allyHPChange += 2; // 额外回血 +2
                    break;
            }
        }

        // 敌方强化效果
        if (enemy.IntentSource == IntentSource.Reinforced)
        {
            switch (enemyOrder)
            {
                case OrderType.ATK:
                    allyHPChange -= 2; // 敌方造成伤害 +2
                    break;
                case OrderType.DEF:
                    enemyHPChange += 2; // 敌方受到伤害 -2
                    break;
                case OrderType.RET:
                    enemyHPChange += 2; // 敌方额外回血 +2
                    break;
            }
        }

        // GDD v7.0: 士气修正（阶梯式加减法）
        int allyMoraleDamageBonus = GetMoraleDamageBonus(ally.Morale);
        int allyMoraleDefenseBonus = GetMoraleDefenseBonus(ally.Morale);
        int enemyMoraleDamageBonus = GetMoraleDamageBonus(enemy.Morale);
        int enemyMoraleDefenseBonus = GetMoraleDefenseBonus(enemy.Morale);

        // 应用士气修正到 HP 变化
        // 己方造成的伤害影响敌方 HP
        enemyHPChange -= allyMoraleDamageBonus;
        // 敌方造成的伤害影响己方 HP
        allyHPChange -= enemyMoraleDamageBonus;
        // 防御修正
        allyHPChange += allyMoraleDefenseBonus;
        enemyHPChange += enemyMoraleDefenseBonus;

        // 应用随机修正（仅对伤害部分）
        if (allyHPChange < 0)
        {
            float damage = -allyHPChange;
            (damage, _, _) = ApplyRandomModifier(damage);
            allyHPChange = -Mathf.RoundToInt(damage);
        }
        if (enemyHPChange < 0)
        {
            float damage = -enemyHPChange;
            (damage, result.WasCrit, result.WasFumble) = ApplyRandomModifier(damage);
            enemyHPChange = -Mathf.RoundToInt(damage);
        }

        // 处理回血时消耗后备役
        if (allyHPChange > 0)
        {
            int healAmount = allyHPChange;
            if (campaignData.Battle.CurrentReserves >= healAmount)
            {
                campaignData.Battle.CurrentReserves -= healAmount;
            }
            else
            {
                allyHPChange = campaignData.Battle.CurrentReserves;
                campaignData.Battle.CurrentReserves = 0;
            }
        }

        // 敌方回血也消耗后备役（从敌方视角，这里简化处理）
        // 注意：GDD 中敌方有自己的后备役系统，这里暂时不消耗
        // 如果需要完整实现，需要添加敌方后备役字段

        result.AllyTroopChange = allyHPChange;
        result.EnemyTroopChange = enemyHPChange;

        // 保留 LineMovement 用于兼容（但不再用于计算位置）
        result.LineMovement = movement;

        // 新领土系统：追踪是否占领了新格子
        bool bothATK = (allyOrder == OrderType.ATK && enemyOrder == OrderType.ATK);

        // 更新 GridPosition 并处理领土占领
        if (allyOrder == OrderType.RET && enemyOrder == OrderType.RET)
        {
            // RET vs RET：双方各自后撤一格，不占领任何格子
            ally.GridPosition = Mathf.Max(1, ally.GridPosition - 1);
            enemy.GridPosition = Mathf.Min(5, enemy.GridPosition + 1);
        }
        else if (bothATK)
        {
            // ATK vs ATK：双方僵持，都不移动，只交换伤害
            // 不改变任何位置，不占领任何格子
        }
        else
        {
            // 处理己方移动和占领
            if (allyOrder == OrderType.ATK)
            {
                int allyTargetGrid = Mathf.Min(5, ally.GridPosition + 1);
                // 确保不会越过敌方
                if (allyTargetGrid < enemy.GridPosition)
                {
                    ally.GridPosition = allyTargetGrid;
                    result.AllyCapturedGrid = allyTargetGrid;
                    // 敌方被推回
                    enemy.GridPosition = Mathf.Min(5, enemy.GridPosition + 1);
                }
            }
            else if (allyOrder == OrderType.RET)
            {
                // RET 只移动，不丢失领土
                ally.GridPosition = Mathf.Max(1, ally.GridPosition - 1);
            }
            // DEF: 不移动，不变化

            // 处理敌方移动和占领
            if (enemyOrder == OrderType.ATK)
            {
                int enemyTargetGrid = Mathf.Max(1, enemy.GridPosition - 1);
                // 确保不会越过己方
                if (enemyTargetGrid > ally.GridPosition)
                {
                    enemy.GridPosition = enemyTargetGrid;
                    result.EnemyCapturedGrid = enemyTargetGrid;
                    // 己方被推回（如果己方不是ATK）
                    if (allyOrder != OrderType.ATK)
                    {
                        ally.GridPosition = Mathf.Max(1, ally.GridPosition - 1);
                    }
                }
            }
            else if (enemyOrder == OrderType.RET)
            {
                // RET 只移动，不丢失领土
                enemy.GridPosition = Mathf.Min(5, enemy.GridPosition + 1);
            }
        }
    }

    /// <summary>GDD v7.0: 士气造成伤害加成</summary>
    private int GetMoraleDamageBonus(int morale)
    {
        if (morale >= 80) return 1;      // 士气 80-100：造成伤害 +1
        if (morale >= 50) return 0;      // 士气 50-79：无修正
        if (morale >= 30) return -1;     // 士气 30-49：造成伤害 -1
        return -1;                        // 士气 0-29：造成伤害 -1
    }

    /// <summary>GDD v7.0: 士气防御加成（减少受到的伤害）</summary>
    private int GetMoraleDefenseBonus(int morale)
    {
        if (morale >= 80) return 1;      // 士气 80-100：受到伤害 -1（即 HP 变化 +1）
        if (morale >= 30) return 0;      // 士气 30-79：无修正
        return -1;                        // 士气 0-29：受到伤害 +1（即 HP 变化 -1）
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

        return basePower * troopMod * moraleMod;
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
            (OrderType.RET, OrderType.RET) => (0, 1, 1),     // 脱离（双方回血，战线不动）

            _ => (0, 0, 0)
        };

        // 注意：这里返回的是 HP 变化，负数表示受伤，正数表示回血
        return baseOutcome;
    }

    private void ApplyBattleResult(GeneralData ally, GeneralData enemy, BattleResult result)
    {
        var frontline = campaignData.Battle.Frontlines[result.Position];

        // 应用领土变化
        if (result.AllyCapturedGrid.HasValue)
        {
            frontline.GridOwners[result.AllyCapturedGrid.Value - 1] = GridOwner.Ally;
        }
        if (result.EnemyCapturedGrid.HasValue)
        {
            frontline.GridOwners[result.EnemyCapturedGrid.Value - 1] = GridOwner.Enemy;
        }

        // 计算新的战线位置用于停滞检查
        float newLinePos = frontline.LinePosition;
        float oldLinePos = newLinePos; // 已经在上面更新过了，这里简化处理

        // 停滞检查（基于是否有领土变化）
        bool territoryChanged = result.AllyCapturedGrid.HasValue || result.EnemyCapturedGrid.HasValue;
        if (!territoryChanged)
            frontline.StagnantTurns++;
        else
            frontline.StagnantTurns = 0;

        ally.Troops = Mathf.Clamp(ally.Troops + result.AllyTroopChange, 0, 20);
        enemy.Troops = Mathf.Clamp(enemy.Troops + result.EnemyTroopChange, 0, 20);

        // 士气变化基于是否占领了新格子
        bool allyAdvanced = result.AllyCapturedGrid.HasValue;
        bool enemyAdvanced = result.EnemyCapturedGrid.HasValue;

        if (allyAdvanced && !enemyAdvanced)
        {
            ally.Morale = Mathf.Clamp(ally.Morale + 10, 0, 100);
            enemy.Morale = Mathf.Clamp(enemy.Morale - 10, 0, 100);
        }
        else if (enemyAdvanced && !allyAdvanced)
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
        // GDD v7.0: 抗命时根据性格决定执行的指令
        return general.Personality switch
        {
            GeneralPersonality.Fanatic => OrderType.ATK,
            GeneralPersonality.Conservative => OrderType.DEF,
            _ => OrderType.DEF
        };
    }

    #endregion

    #region 补员

    /// <summary>
    /// GDD v7.0: 基地休整
    /// 只有在基地位置的将军才能获得回血，且消耗后备役
    /// </summary>
    public void ApplyReinforcements()
    {
        // 玩家方将军（Grid 1 是玩家基地）
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            // 溃败将军处理重组倒计时
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed)
            {
                general.ReorganizeTurns--;
                continue;
            }

            // GDD v7.0: 只有在 Grid 1（己方基地）才能获得基地休整
            if (general.GridPosition == 1)
            {
                int healAmount = 2; // 基地休整 HP +2

                // 检查后备役是否足够
                if (campaignData.Battle.CurrentReserves >= healAmount)
                {
                    campaignData.Battle.CurrentReserves -= healAmount;
                    general.Troops = Mathf.Min(20, general.Troops + healAmount);
                }
                else if (campaignData.Battle.CurrentReserves > 0)
                {
                    // 后备役不足，只能恢复剩余的量
                    int actualHeal = campaignData.Battle.CurrentReserves;
                    campaignData.Battle.CurrentReserves = 0;
                    general.Troops = Mathf.Min(20, general.Troops + actualHeal);
                }
                // 后备役为 0 时无法恢复
            }
        }

        // 敌方将军（Grid 5 是敌方基地）
        foreach (var general in campaignData.Battle.EnemyGenerals)
        {
            // 溃败将军处理重组倒计时
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed)
            {
                general.ReorganizeTurns--;
                continue;
            }

            // GDD v7.0: 敌方在 Grid 5（敌方基地）才能获得基地休整
            if (general.GridPosition == 5)
            {
                int healAmount = 2; // 基地休整 HP +2

                // 检查敌方后备役是否足够
                if (campaignData.Battle.EnemyReserves >= healAmount)
                {
                    campaignData.Battle.EnemyReserves -= healAmount;
                    general.Troops = Mathf.Min(20, general.Troops + healAmount);
                }
                else if (campaignData.Battle.EnemyReserves > 0)
                {
                    int actualHeal = campaignData.Battle.EnemyReserves;
                    campaignData.Battle.EnemyReserves = 0;
                    general.Troops = Mathf.Min(20, general.Troops + actualHeal);
                }
            }
        }
    }

    #endregion

    #region 胜负检查

    public bool CheckVictory()
    {
        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (!frontline.IsAtEnemyBase) return false;  // 使用新属性：己方占领格5
        }
        return true;
    }

    public bool CheckDefeat()
    {
        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (!frontline.IsAtAllyBase) return false;  // 使用新属性：敌方占领格1
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
