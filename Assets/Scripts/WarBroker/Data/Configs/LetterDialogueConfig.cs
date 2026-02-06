using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 古斯塔夫·阿道夫 信件对话配置
/// 包含瑞典国王的个性设置和各种场景的对话文本
/// </summary>
[CreateAssetMenu(menuName = "WarBroker/LetterDialogueConfig")]
public class LetterDialogueConfig : ScriptableObject
{
    [Header("=== 角色设置 ===")]

    [Tooltip("角色 ID")]
    public string ActorId = "Gustavus";

    [Tooltip("显示名称")]
    public string DisplayName = "古斯塔夫·阿道夫";

    [Tooltip("完整头衔")]
    public string FullTitle = "瑞典国王 古斯塔夫二世·阿道夫";

    [Tooltip("信件收件人称呼")]
    public string RecipientTitle = "华伦斯坦将军";

    [Header("=== 表情映射 ===")]
    [Tooltip("策略对应的表情")]
    public StrategyExpressionMapping[] StrategyExpressions = new StrategyExpressionMapping[]
    {
        new StrategyExpressionMapping { Strategy = VictorStrategy.MilitaryFocus, Expression = "resolute" },
        new StrategyExpressionMapping { Strategy = VictorStrategy.TrendFollowing, Expression = "confident" },
        new StrategyExpressionMapping { Strategy = VictorStrategy.CounterStrike, Expression = "stern" },
        new StrategyExpressionMapping { Strategy = VictorStrategy.DeceptionPlay, Expression = "thoughtful" },
        new StrategyExpressionMapping { Strategy = VictorStrategy.Harvest, Expression = "pleased" }
    };

    [Header("=== 策略开场白 ===")]
    [Tooltip("每种策略的开场对话")]
    public StrategyDialogue[] StrategyOpenings = new StrategyDialogue[]
    {
        new StrategyDialogue
        {
            Strategy = VictorStrategy.MilitaryFocus,
            Dialogues = new string[]
            {
                "新教的旗帜不容倒下，我的将士们需要我的全力支持。",
                "上帝赐予我们正义之师，此刻当以军务为重。",
                "战场上的每一寸土地都关乎信仰的存亡，岂能懈怠？"
            }
        },
        new StrategyDialogue
        {
            Strategy = VictorStrategy.TrendFollowing,
            Dialogues = new string[]
            {
                "战局的走向已然明朗，智者当顺势而为。",
                "上帝的旨意通过战场的形势显现，我只是顺应天命。",
                "风向已定，扬帆正当时。"
            }
        },
        new StrategyDialogue
        {
            Strategy = VictorStrategy.CounterStrike,
            Dialogues = new string[]
            {
                "将军，你的布局虽巧妙，却也露出了破绽。",
                "帝国的鹰旗或许遮蔽了你的视野，但北方的雄狮看得清楚。",
                "你在战场上的动作，比你以为的更加显眼。"
            }
        },
        new StrategyDialogue
        {
            Strategy = VictorStrategy.DeceptionPlay,
            Dialogues = new string[]
            {
                "兵者，诡道也。即便是为了正义的事业，也需要一些策略。",
                "让帝国的谋士们猜测我的下一步吧。",
                "战争的迷雾中，真相往往藏在最后。"
            }
        },
        new StrategyDialogue
        {
            Strategy = VictorStrategy.Harvest,
            Dialogues = new string[]
            {
                "收获的季节到了，上帝眷顾着虔诚的人。",
                "播种已久，是时候将胜利的果实收入囊中。",
                "稳固已得之利，方能图谋更远大的目标。"
            }
        }
    };

    [Header("=== 动作暗示 ===")]
    [Tooltip("现货买入暗示")]
    [TextArea(2, 4)]
    public string[] SpotBuyHints = new string[]
    {
        "我对{0}物资颇有信心，已着手筹备。",
        "{0}物资乃当务之急，自当多加储备。",
        "战事所需，{0}物资不可或缺。"
    };

    [Tooltip("现货卖出暗示")]
    [TextArea(2, 4)]
    public string[] SpotSellHints = new string[]
    {
        "库中{0}物资已有富余，可作他用。",
        "{0}之储备暂且充足，不妨变现以充军资。",
        "是时候将部分{0}物资投入流通了。"
    };

    [Tooltip("期货做多暗示")]
    [TextArea(2, 4)]
    public string[] FuturesLongHints = new string[]
    {
        "依我所见，{0}物资的价值将会攀升。",
        "对{0}的前景，我抱有坚定的信心。",
        "我愿为{0}的未来押下赌注。"
    };

    [Tooltip("期货做空暗示")]
    [TextArea(2, 4)]
    public string[] FuturesShortHints = new string[]
    {
        "{0}的价格虚高，终将回落。",
        "市场对{0}的追捧不会持久。",
        "我并不看好{0}的后市。"
    };

    [Tooltip("期货平仓暗示")]
    [TextArea(2, 4)]
    public string[] FuturesCloseHints = new string[]
    {
        "先前的筹谋已见分晓，是时候了结了。",
        "契约已到履行之时。",
        "当收手时便收手，此乃明智之举。"
    };

    [Tooltip("将军强化暗示")]
    [TextArea(2, 4)]
    public string[] GeneralReinforceHints = new string[]
    {
        "{0}元帅已接获{1}之令，必能善加运用。",
        "我已将{1}物资交付{0}，相信他不会令我失望。",
        "{0}所需的{1}补给，我已妥善安排。"
    };

    [Tooltip("将军篡改暗示")]
    [TextArea(2, 4)]
    public string[] GeneralTamperHints = new string[]
    {
        "{0}的判断需要纠正，{1}才是正确的方向。",
        "我不得不干预{0}的决定，改以{1}行事。",
        "虽然{0}另有打算，但大局为重，{1}势在必行。"
    };

    [Tooltip("借款暗示")]
    [TextArea(2, 4)]
    public string[] BorrowHints = new string[]
    {
        "为了新教大业，暂且向银行家们借贷一二。",
        "军费开支浩大，需筹措额外资金。",
        "正义的事业需要金钱来支撑，这是必要的权宜之计。"
    };

    [Tooltip("还款暗示")]
    [TextArea(2, 4)]
    public string[] RepayHints = new string[]
    {
        "信用乃立身之本，当及时偿还债务。",
        "减轻负债，方能轻装上阵。",
        "该还的债，一分不少。"
    };

    [Header("=== 规模修饰语 ===")]
    [Tooltip("小规模动作的修饰语")]
    public string[] SmallScaleModifiers = new string[] { "少许", "些许", "一批" };

    [Tooltip("中等规模动作的修饰语")]
    public string[] MediumScaleModifiers = new string[] { "相当数量的", "一定规模的", "不少" };

    [Tooltip("大规模动作的修饰语")]
    public string[] LargeScaleModifiers = new string[] { "大批", "大量", "甚为可观的" };

    [Header("=== 结束语 ===")]
    [Tooltip("回合结束语")]
    [TextArea(2, 4)]
    public string[] ClosingRemarks = new string[]
    {
        "愿上帝保佑正义的一方。我们战场上见。",
        "北方的雄狮从不畏惧挑战。保重，将军。",
        "让我们看看，谁才是这场战争的主宰。",
        "历史会记住这一切。祝你好运，华伦斯坦。",
        "无论胜负，都请保持一个军人的尊严。战场上见。"
    };

    [Header("=== 华伦斯坦回应（确认按钮文本） ===")]
    [Tooltip("每种策略对应的华伦斯坦回应")]
    public StrategyDialogue[] WallensteinResponses = new StrategyDialogue[]
    {
        new StrategyDialogue
        {
            Strategy = VictorStrategy.MilitaryFocus,
            Dialogues = new string[]
            {
                "不过如此。",
                "尽管来吧。",
                "哼，雕虫小技。"
            }
        },
        new StrategyDialogue
        {
            Strategy = VictorStrategy.TrendFollowing,
            Dialogues = new string[]
            {
                "随波逐流罢了。",
                "看看谁笑到最后。",
                "瑞典人也学会投机了？"
            }
        },
        new StrategyDialogue
        {
            Strategy = VictorStrategy.CounterStrike,
            Dialogues = new string[]
            {
                "你高估自己了。",
                "帝国不会被恐吓。",
                "且让你得意片刻。"
            }
        },
        new StrategyDialogue
        {
            Strategy = VictorStrategy.DeceptionPlay,
            Dialogues = new string[]
            {
                "骗术而已。",
                "以为我看不透吗？",
                "小聪明……"
            }
        },
        new StrategyDialogue
        {
            Strategy = VictorStrategy.Harvest,
            Dialogues = new string[]
            {
                "贪心会害了你。",
                "且收你的蝇头小利。",
                "目光短浅。"
            }
        }
    };

    /// <summary>
    /// 获取华伦斯坦对该策略的回应（随机选择一条）
    /// </summary>
    public string GetWallensteinResponse(VictorStrategy strategy)
    {
        foreach (var dialogue in WallensteinResponses)
        {
            if (dialogue.Strategy == strategy && dialogue.Dialogues.Length > 0)
            {
                return dialogue.Dialogues[UnityEngine.Random.Range(0, dialogue.Dialogues.Length)];
            }
        }
        return "阅毕";
    }

    /// <summary>
    /// 获取策略对应的表情
    /// </summary>
    public string GetExpressionForStrategy(VictorStrategy strategy)
    {
        foreach (var mapping in StrategyExpressions)
        {
            if (mapping.Strategy == strategy)
                return mapping.Expression;
        }
        return "neutral";
    }

    /// <summary>
    /// 获取策略对应的立绘
    /// </summary>
    public Sprite GetPortraitForStrategy(VictorStrategy strategy)
    {
        foreach (var mapping in StrategyExpressions)
        {
            if (mapping.Strategy == strategy)
                return mapping.Portrait;
        }
        return null;
    }

    /// <summary>
    /// 获取策略开场白（随机选择一条）
    /// </summary>
    public string GetStrategyOpening(VictorStrategy strategy)
    {
        foreach (var dialogue in StrategyOpenings)
        {
            if (dialogue.Strategy == strategy && dialogue.Dialogues.Length > 0)
            {
                return dialogue.Dialogues[UnityEngine.Random.Range(0, dialogue.Dialogues.Length)];
            }
        }
        return "...";
    }

    /// <summary>
    /// 获取动作暗示（随机选择一条）
    /// </summary>
    public string GetActionHint(VictorActionType actionType, OrderType orderType = OrderType.ATK, string targetName = null)
    {
        string[] hints = GetHintArrayForAction(actionType);
        if (hints == null || hints.Length == 0)
            return "";

        string hint = hints[UnityEngine.Random.Range(0, hints.Length)];

        // 格式化占位符
        switch (actionType)
        {
            case VictorActionType.SpotBuy:
            case VictorActionType.SpotSell:
            case VictorActionType.FuturesLong:
            case VictorActionType.FuturesShort:
                return string.Format(hint, GetOrderTypeName(orderType));

            case VictorActionType.GeneralReinforce:
            case VictorActionType.GeneralTamper:
                return string.Format(hint, targetName ?? "将军", GetOrderTypeName(orderType));

            default:
                return hint;
        }
    }

    /// <summary>
    /// 获取规模修饰语
    /// </summary>
    public string GetScaleModifier(ActionScale scale)
    {
        string[] modifiers;
        switch (scale)
        {
            case ActionScale.Small:
                modifiers = SmallScaleModifiers;
                break;
            case ActionScale.Medium:
                modifiers = MediumScaleModifiers;
                break;
            case ActionScale.Large:
                modifiers = LargeScaleModifiers;
                break;
            default:
                modifiers = SmallScaleModifiers;
                break;
        }

        if (modifiers == null || modifiers.Length == 0)
            return "";

        return modifiers[UnityEngine.Random.Range(0, modifiers.Length)];
    }

    /// <summary>
    /// 获取结束语（随机选择一条）
    /// </summary>
    public string GetClosingRemark()
    {
        if (ClosingRemarks == null || ClosingRemarks.Length == 0)
            return "";

        return ClosingRemarks[UnityEngine.Random.Range(0, ClosingRemarks.Length)];
    }

    private string[] GetHintArrayForAction(VictorActionType actionType)
    {
        switch (actionType)
        {
            case VictorActionType.SpotBuy: return SpotBuyHints;
            case VictorActionType.SpotSell: return SpotSellHints;
            case VictorActionType.FuturesLong: return FuturesLongHints;
            case VictorActionType.FuturesShort: return FuturesShortHints;
            case VictorActionType.FuturesClose: return FuturesCloseHints;
            case VictorActionType.GeneralReinforce: return GeneralReinforceHints;
            case VictorActionType.GeneralTamper: return GeneralTamperHints;
            case VictorActionType.Borrow: return BorrowHints;
            case VictorActionType.Repay: return RepayHints;
            default: return null;
        }
    }

    private string GetOrderTypeName(OrderType orderType)
    {
        return orderType.ToDisplayName();
    }
}

/// <summary>
/// 策略-表情映射
/// </summary>
[Serializable]
public class StrategyExpressionMapping
{
    public VictorStrategy Strategy;
    public string Expression;

    [Tooltip("该策略对应的立绘")]
    [PreviewField(50)]
    public Sprite Portrait;
}

/// <summary>
/// 策略对话配置
/// </summary>
[Serializable]
public class StrategyDialogue
{
    public VictorStrategy Strategy;

    [TextArea(2, 4)]
    public string[] Dialogues;
}
