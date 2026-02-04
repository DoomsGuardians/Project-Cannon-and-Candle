using System;
using UnityEngine;

/// <summary>
/// 全局游戏平衡参数配置
/// </summary>
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "WarBroker/GameBalanceConfig")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("===== 市场参数 =====")]

    [Tooltip("手续费率")]
    [Range(0f, 0.1f)]
    public float CommissionRate = 0.02f;

    [Tooltip("价格冲击率 (每张)")]
    [Range(0f, 0.1f)]
    public float PriceImpactRate = 0.02f;

    [Tooltip("仓储费 (每张/回合)")]
    public float StorageCostPerUnit = 3f;

    [Tooltip("价格随机波动范围")]
    [Range(0f, 0.2f)]
    public float PriceRandomRange = 0.05f;

    [Header("===== 银行参数 =====")]

    [Tooltip("银行利率 (每回合)")]
    [Range(0f, 0.2f)]
    public float BankInterestRate = 0.05f;

    [Tooltip("借款额度系数 (净资产倍数)")]
    [Range(1f, 3f)]
    public float LoanRatio = 1.5f;

    [Header("===== 期货参数 =====")]

    [Tooltip("期货保证金率")]
    [Range(0.1f, 0.5f)]
    public float FuturesMarginRate = 0.2f;

    [Tooltip("强平线 (亏损百分比)")]
    [Range(0.5f, 0.95f)]
    public float ForceLiquidationRate = 0.8f;

    [Tooltip("期货最长期限 (回合)")]
    [Range(1, 5)]
    public int MaxFuturesDuration = 3;

    [Header("===== 战斗参数 =====")]

    [Tooltip("随机修正最小值")]
    [Range(0.5f, 1f)]
    public float RandomModifierMin = 0.75f;

    [Tooltip("随机修正最大值")]
    [Range(1f, 1.5f)]
    public float RandomModifierMax = 1.25f;

    [Tooltip("暴击概率")]
    [Range(0f, 0.2f)]
    public float CritChance = 0.05f;

    [Tooltip("暴击倍率")]
    [Range(1f, 3f)]
    public float CritMultiplier = 1.5f;

    [Tooltip("失误概率")]
    [Range(0f, 0.2f)]
    public float FumbleChance = 0.05f;

    [Tooltip("失误倍率")]
    [Range(0f, 1f)]
    public float FumbleMultiplier = 0.5f;

    [Header("===== 将军参数 =====")]

    [Tooltip("基础补员 (每回合)")]
    [Range(0, 5)]
    public int BaseReinforcement = 2;

    [Tooltip("复活后HP")]
    [Range(1, 20)]
    public int RespawnHP = 10;

    [Tooltip("复活消耗后备役")]
    [Range(1, 20)]
    public int RespawnReserveCost = 10;

    [Tooltip("基地休整回血")]
    [Range(1, 5)]
    public int BaseRecoveryHP = 2;

    [Tooltip("基地休整消耗后备役")]
    [Range(1, 5)]
    public int BaseRecoveryCost = 2;

    [Tooltip("RET回血")]
    [Range(1, 3)]
    public int RetHealHP = 1;

    [Tooltip("RET消耗后备役")]
    [Range(1, 3)]
    public int RetHealCost = 1;

    [Tooltip("重整所需回合")]
    [Range(1, 5)]
    public int ReorganizeTurns = 3;

    [Tooltip("抗命起始信任度阈值")]
    [Range(0, 100)]
    public int DisobeyTrustThreshold = 50;

    [Tooltip("低信任度抗命概率 (信任度30-49)")]
    [Range(0f, 1f)]
    public float DisobeyChanceLow = 0.2f;

    [Tooltip("极低信任度抗命概率 (信任度0-29)")]
    [Range(0f, 1f)]
    public float DisobeyChanceVeryLow = 0.5f;

    [Header("===== 意图干涉参数 =====")]

    [Tooltip("强化消耗指令数")]
    [Range(1, 5)]
    public int ReinforceCost = 1;

    [Tooltip("篡改消耗指令数")]
    [Range(1, 10)]
    public int OverrideCost = 3;

    [Tooltip("强化信任度变化")]
    [Range(0, 20)]
    public int ReinforceTrustChange = 5;

    [Tooltip("强化士气变化")]
    [Range(0, 20)]
    public int ReinforceMoraleChange = 5;

    [Tooltip("篡改信任度变化")]
    [Range(-50, 0)]
    public int OverrideTrustChange = -15;

    [Tooltip("篡改士气变化")]
    [Range(-20, 0)]
    public int OverrideMoraleChange = -5;

    [Header("===== 审计参数 =====")]

    [Tooltip("军需总部补足指令审计值")]
    [Range(0, 50)]
    public int AuditSupplyShortage = 30;

    [Tooltip("将军溃败审计值")]
    [Range(0, 50)]
    public int AuditGeneralRouted = 20;

    [Tooltip("审计失败阈值")]
    [Range(50, 200)]
    public int AuditFailureThreshold = 100;

    [Header("===== 随机事件参数 =====")]

    [Tooltip("随机事件触发概率")]
    [Range(0f, 1f)]
    public float RandomEventChance = 0.4f;

    [Header("===== 三因子定价 =====")]

    [Tooltip("Alpha基础值：接触状态")]
    [Range(0f, 0.5f)]
    public float AlphaContactBase = 0.20f;

    [Tooltip("Alpha临界修正")]
    [Range(0f, 0.3f)]
    public float AlphaCriticalBonus = 0.15f;

    [Tooltip("Alpha低血量修正")]
    [Range(0f, 0.2f)]
    public float AlphaLowHPBonus = 0.10f;

    [Tooltip("Alpha战场影响倍率（增强战场对价格的影响）")]
    [Range(1f, 3f)]
    public float AlphaMultiplier = 1.5f;

    [Tooltip("Beta动量阈值")]
    [Range(0f, 0.3f)]
    public float BetaMomentumThreshold = 0.10f;

    [Tooltip("Beta动量乘数")]
    [Range(1f, 2f)]
    public float BetaMomentumMultiplier = 1.2f;

    [Tooltip("Beta恐慌阈值（后备役）")]
    [Range(0, 50)]
    public int BetaPanicReserveThreshold = 20;

    [Tooltip("Beta恐慌乘数")]
    [Range(1f, 2f)]
    public float BetaPanicMultiplier = 1.3f;

    [Tooltip("Beta交易冲击系数（降低可减少交易对价格的影响）")]
    [Range(0f, 0.2f)]
    public float ImpactCoefficient = 0.03f;

    [Tooltip("Beta最小值（限制价格下跌幅度）")]
    [Range(0.1f, 0.5f)]
    public float BetaMin = 0.3f;

    [Tooltip("Beta最大值（限制价格上涨幅度）")]
    [Range(2f, 10f)]
    public float BetaMax = 4f;

    [Tooltip("Gamma最大值（限制流通盘对价格的影响）")]
    [Range(1f, 5f)]
    public float GammaMax = 2.5f;

    [Header("===== Alpha位置修正表 =====")]

    [Tooltip("Grid 1 修正")]
    public float AlphaGrid1Modifier = 0.15f;

    [Tooltip("Grid 2 修正")]
    public float AlphaGrid2Modifier = 0.10f;

    [Tooltip("Grid 3 修正")]
    public float AlphaGrid3Modifier = 0.05f;

    [Tooltip("Grid 4 修正")]
    public float AlphaGrid4Modifier = -0.05f;

    [Tooltip("Grid 5 修正")]
    public float AlphaGrid5Modifier = -0.10f;

    [Header("===== 军工厂产能 =====")]

    [Tooltip("产能系数最小值")]
    [Range(0.5f, 1f)]
    public float ProductionFactorMin = 0.9f;

    [Tooltip("产能系数最大值")]
    [Range(1f, 1.5f)]
    public float ProductionFactorMax = 1.1f;

    [Header("===== 战斗对抗表 (己方HP, 敌方HP) =====")]

    [Tooltip("ATK vs ATK: 遭遇战")]
    public Vector2Int Combat_ATK_ATK = new Vector2Int(-2, -2);

    [Tooltip("ATK vs DEF: 攻坚战")]
    public Vector2Int Combat_ATK_DEF = new Vector2Int(-4, -1);

    [Tooltip("ATK vs RET: 追击")]
    public Vector2Int Combat_ATK_RET = new Vector2Int(0, -2);

    [Tooltip("DEF vs ATK: 阻击")]
    public Vector2Int Combat_DEF_ATK = new Vector2Int(-1, -4);

    [Tooltip("DEF vs DEF: 静坐")]
    public Vector2Int Combat_DEF_DEF = new Vector2Int(0, 0);

    [Tooltip("DEF vs RET: 目送")]
    public Vector2Int Combat_DEF_RET = new Vector2Int(0, 1);

    [Tooltip("RET vs ATK: 被追击")]
    public Vector2Int Combat_RET_ATK = new Vector2Int(-2, 0);

    [Tooltip("RET vs DEF: 安全撤退")]
    public Vector2Int Combat_RET_DEF = new Vector2Int(1, 0);

    [Tooltip("RET vs RET: 双方脱离")]
    public Vector2Int Combat_RET_RET = new Vector2Int(1, 1);

    /// <summary>获取对抗表结果</summary>
    public (int allyHP, int enemyHP) GetCombatOutcome(OrderType ally, OrderType enemy)
    {
        var result = (ally, enemy) switch
        {
            (OrderType.ATK, OrderType.ATK) => Combat_ATK_ATK,
            (OrderType.ATK, OrderType.DEF) => Combat_ATK_DEF,
            (OrderType.ATK, OrderType.RET) => Combat_ATK_RET,
            (OrderType.DEF, OrderType.ATK) => Combat_DEF_ATK,
            (OrderType.DEF, OrderType.DEF) => Combat_DEF_DEF,
            (OrderType.DEF, OrderType.RET) => Combat_DEF_RET,
            (OrderType.RET, OrderType.ATK) => Combat_RET_ATK,
            (OrderType.RET, OrderType.DEF) => Combat_RET_DEF,
            (OrderType.RET, OrderType.RET) => Combat_RET_RET,
            _ => Vector2Int.zero
        };
        return (result.x, result.y);
    }
}
