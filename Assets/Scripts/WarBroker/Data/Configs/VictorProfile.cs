using UnityEngine;

/// <summary>
/// 维克多性格配置档案 (GDD v7 Section 7.2)
/// 每个战役可独立配置维克多的行为倾向。
/// 
/// 预设模板 (GDD v7 Section 7.3):
///   老实人:   Military=1.0, Speculation=0.0, Deception=0.0, Adaptive=0.0
///   精打细算: Military=0.7, Speculation=0.3, Deception=0.1, Adaptive=0.2
///   金融猎手: Military=0.4, Speculation=0.8, Deception=0.7, Adaptive=0.6
///   疯子:     Military=0.1, Speculation=1.0, Deception=0.5, Adaptive=0.9
/// </summary>
[CreateAssetMenu(menuName = "WarBroker/VictorProfile")]
public class VictorProfile : ScriptableObject
{
    [Header("=== 性格轴 (0.0~1.0) ===")]

    [Tooltip("军事优先度: 0=将军自生自灭全力搞金融, 1=优先保证将军作战")]
    [Range(0f, 1f)]
    public float MilitaryPriority = 0.8f;

    [Tooltip("投机倾向: 0=纯军事采购者, 1=主动囤货、抛售、做空、加杠杆")]
    [Range(0f, 1f)]
    public float SpeculationTendency = 0.3f;

    [Tooltip("欺骗性: 0=交易行为与军事意图一致, 1=故意制造虚假市场信号")]
    [Range(0f, 1f)]
    public float Deception = 0.1f;

    [Tooltip("适应性: 0=不关注玩家行为, 1=通过K线反推玩家仓位并反向操作")]
    [Range(0f, 1f)]
    public float Adaptiveness = 0.2f;

    [Header("=== 行为阈值 ===")]

    [Tooltip("指令价超过基础价几倍时放弃采购")]
    public float PriceToleranceMultiplier = 2.0f;

    [Tooltip("保留多少比例现金不动用（作为金融操作资金储备）")]
    [Range(0f, 1f)]
    public float CashReserveRatio = 0.2f;

    [Tooltip("仓位盈利预期超过多少时愿意牺牲将军利益")]
    [Range(0f, 1f)]
    public float BetraySelfThreshold = 0.5f;
}
