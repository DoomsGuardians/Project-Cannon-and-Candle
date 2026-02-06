using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
/// 信息面板：市场舆情 + 战场传闻 + 我军动态 + 敌情传闻 + 战史评说
/// 通过历史人物和消息人士的视角，间接暗示价格影响因子
/// </summary>
public class InfoPanel : WindowBase
{
    private TMP_Text txtMarketIntel, txtBattleIntel, txtAllyIntel, txtEnemyIntel, txtBattleHistory;
    private KLineChartView chartAtk, chartDef, chartRet;
    private GameplayManager gameplayManager;
    private UITextConfig textConfig;
    private GameBalanceConfig balanceConfig;
    private PricingEngine pricingEngine;

    // 随机情报用的种子，每回合变化
    private int currentTurnSeed = 0;

    // 额外的随机情报模板
    private static readonly string[] ExtraRumors = new string[]
    {
        "有人在酒馆里说，法兰西宫廷正在秘密筹措军费，或有大动作。",
        "一位从维也纳来的旅人透露，皇帝陛下对战局颇为焦虑。",
        "据闻教廷特使已抵达慕尼黑，和谈之事或有眉目。",
        "波罗的海的商船近日频繁遇袭，海上贸易风险剧增。",
        "有消息称，丹麦国王正在观望，或将再度介入战局。",
        "莱茵河上的关税又涨了，运输成本恐将转嫁至军需价格。",
        "听闻西班牙的白银船队延误，市场银根或将收紧。",
        "萨克森选帝侯的立场似有动摇，新教联盟内部暗流涌动。",
        "有商人抱怨，瑞典人的通行税越来越重，利润大不如前。",
        "巴伐利亚的粮价飞涨，农民纷纷逃亡，征兵愈发困难。",
    };

    // 矛盾情报模板
    private static readonly string[] ConflictingRumors = new string[]
    {
        "然而亦有人持相反看法，认为此乃虚张声势。",
        "但酒馆里另一桌的客商却说，实情恰恰相反。",
        "不过，从德累斯顿来的信使带来了截然不同的消息。",
        "话虽如此，法兰克福的同行却对此嗤之以鼻。",
        "当然，也有人认为这不过是有心人散布的谣言罢了。",
    };

    // 消息来源变体
    private static readonly string[] RumorSources = new string[]
    {
        "途经莱比锡的商队",
        "从布拉格逃来的难民",
        "一位不愿透露姓名的信使",
        "驻纽伦堡的商会代表",
        "某位宫廷侍从的仆人",
        "威尼斯驻维也纳的密探",
        "一位退役的雇佣兵队长",
    };

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        textConfig = resService.LoadResource<UITextConfig>(ConfigPaths.UI_TEXT);
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);

        // 获取 PricingEngine
        if (GameRoot.Instance.marketSystem != null)
        {
            pricingEngine = GameRoot.Instance.marketSystem.GetPricingEngine();
        }

        var b = gameObject.GetComponent<InfoPanelBinder>();
        if (b != null)
        {
            txtMarketIntel = b.txtMarketIntel;
            txtBattleIntel = b.txtBattleIntel;
            txtAllyIntel = b.txtAllyIntel;
            txtEnemyIntel = b.txtEnemyIntel;
            txtBattleHistory = b.txtBattleHistory;

            // K线图
            chartAtk = b.chartAtk;
            chartDef = b.chartDef;
            chartRet = b.chartRet;
        }

        InitializeCharts();
    }

    private void InitializeCharts()
    {
        if (chartAtk != null)
        {
            chartAtk.Initialize(OrderType.ATK.ToDisplayName());
            chartAtk.SetOrderType(OrderType.ATK);
        }
        if (chartDef != null)
        {
            chartDef.Initialize(OrderType.DEF.ToDisplayName());
            chartDef.SetOrderType(OrderType.DEF);
        }
        if (chartRet != null)
        {
            chartRet.Initialize(OrderType.RET.ToDisplayName());
            chartRet.SetOrderType(OrderType.RET);
        }
    }

    public override void OnShow()
    {
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnRefresh);
        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    private void RefreshUI()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        // 更新随机种子（基于回合数，保证同一回合内情报一致）
        currentTurnSeed = data.CurrentTurn * 17 + 31;

        RefreshMarketIntel(data);
        RefreshBattleRumors(data);
        RefreshAllyRumors(data);
        RefreshEnemyRumors(data);
        RefreshBattleHistory(data);
        RefreshKLineCharts(data);
    }

    /// <summary>
    /// 刷新市场舆情 - 通过历史人物评论暗示三个价格因子
    /// </summary>
    private void RefreshMarketIntel(CampaignRuntimeData data)
    {
        if (txtMarketIntel == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("<b>【市场舆情】</b>");
        sb.AppendLine();

        // 获取三个因子的平均值（用于整体评估）
        float avgAlpha = 0f, avgBeta = 0f, avgGamma = 0f;
        int count = 0;

        foreach (OrderType type in System.Enum.GetValues(typeof(OrderType)))
        {
            if (pricingEngine != null)
            {
                var factors = pricingEngine.GetMarketFactors(type);
                avgAlpha += factors.Alpha;
                avgBeta += factors.Beta;
                avgGamma += factors.Gamma;
                count++;
            }
        }

        if (count > 0)
        {
            avgAlpha /= count;
            avgBeta /= count;
            avgGamma /= count;
        }

        // 随机决定评论顺序和表述方式
        int orderSeed = currentTurnSeed % 6;

        // Alpha评论 - 战场态势对价格的影响
        string personAlpha = textConfig?.IntelPersonAlpha ?? "红衣主教黎塞留";
        string commentAlpha = GetAlphaComment(avgAlpha);
        string prefixAlpha = GetRandomPrefix(0);

        // Beta评论 - 市场情绪对价格的影响
        string personBeta = textConfig?.IntelPersonBeta ?? "阿姆斯特丹交易所掮客";
        string commentBeta = GetBetaComment(avgBeta);
        string prefixBeta = GetRandomPrefix(1);

        // Gamma评论 - 库存流通对价格的影响
        string personGamma = textConfig?.IntelPersonGamma ?? "汉萨同盟军需承包商";
        string commentGamma = GetGammaComment(avgGamma);
        string prefixGamma = GetRandomPrefix(2);

        // 根据种子决定顺序
        if (orderSeed < 2)
        {
            AppendPersonComment(sb, personAlpha, commentAlpha, prefixAlpha, "#CC9900");
            AppendPersonComment(sb, personBeta, commentBeta, prefixBeta, "#4682B4");
            AppendPersonComment(sb, personGamma, commentGamma, prefixGamma, "#228B22");
        }
        else if (orderSeed < 4)
        {
            AppendPersonComment(sb, personBeta, commentBeta, prefixBeta, "#4682B4");
            AppendPersonComment(sb, personGamma, commentGamma, prefixGamma, "#228B22");
            AppendPersonComment(sb, personAlpha, commentAlpha, prefixAlpha, "#CC9900");
        }
        else
        {
            AppendPersonComment(sb, personGamma, commentGamma, prefixGamma, "#228B22");
            AppendPersonComment(sb, personAlpha, commentAlpha, prefixAlpha, "#CC9900");
            AppendPersonComment(sb, personBeta, commentBeta, prefixBeta, "#4682B4");
        }

        // 有概率添加矛盾情报
        if ((currentTurnSeed % 5) == 0)
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#666666><i>{GetRandomConflictingRumor()}</i></color>");
        }

        txtMarketIntel.text = sb.ToString();
    }

    private void AppendPersonComment(StringBuilder sb, string person, string comment, string prefix, string color)
    {
        sb.AppendLine($"<color={color}>{prefix}{person}:</color>");
        sb.AppendLine($"  {comment}");
        sb.AppendLine();
    }

    private string GetRandomPrefix(int index)
    {
        string[] prefixes = new string[]
        {
            "", "据闻", "有人转述", "消息称", "坊间传言"
        };
        int idx = (currentTurnSeed + index * 7) % prefixes.Length;
        string prefix = prefixes[idx];
        return string.IsNullOrEmpty(prefix) ? "" : prefix + "，";
    }

    /// <summary>
    /// 刷新战场传闻 - 中立消息人士视角，模糊描述战场态势
    /// </summary>
    private void RefreshBattleRumors(CampaignRuntimeData data)
    {
        if (txtBattleIntel == null) return;

        var sb = new StringBuilder();
        string header = textConfig?.IntelRumorHeader ?? "【战场传闻】";
        string source = GetRandomSource();
        sb.AppendLine($"<b>{header}</b>");
        sb.AppendLine($"<color=#666666>— 消息来源: {source}</color>");
        sb.AppendLine();

        // 分析各战线情况
        int offensiveCount = 0;  // 在敌方腹地
        int defensiveCount = 0;  // 在己方腹地
        int stalemateCount = 0;  // 在中线
        int totalFrontlines = 0;

        foreach (var ally in data.Battle.AllyGenerals)
        {
            if (ally.Troops <= 0) continue;
            totalFrontlines++;

            if (ally.GridPosition >= 4)
                offensiveCount++;
            else if (ally.GridPosition <= 2)
                defensiveCount++;
            else
                stalemateCount++;
        }

        // 生成战线传闻
        string frontlineRumor;
        if (totalFrontlines == 0)
        {
            frontlineRumor = "战场情况不明，尚无可靠消息传来。诸位只能静待后续。";
        }
        else if (defensiveCount > offensiveCount && defensiveCount > stalemateCount)
        {
            frontlineRumor = textConfig?.RumorDefensive ?? "据从马格德堡逃来的难民称，帝国军节节败退，形势万分危急。";
        }
        else if (offensiveCount > defensiveCount && offensiveCount > stalemateCount)
        {
            frontlineRumor = textConfig?.RumorOffensive ?? "驿站传来捷报，据称我军挥师北进，已深入敌境。";
        }
        else if (stalemateCount >= offensiveCount && stalemateCount >= defensiveCount)
        {
            frontlineRumor = textConfig?.RumorStalemate ?? "有从纽伦堡来的信使透露，双方主力在日耳曼腹地对峙，胜负尚难预料。";
        }
        else
        {
            frontlineRumor = textConfig?.RumorMixed ?? "各路战报真假难辨，战局扑朔迷离，唯有静观其变。";
        }

        sb.AppendLine($"\"{frontlineRumor}\"");
        sb.AppendLine();

        // 后备役传闻
        int reserves = data.Battle.CurrentReserves;
        string reserveRumor;
        if (reserves >= 15)
        {
            reserveRumor = textConfig?.RumorReservesHigh ?? "后方蒂罗尔的征兵顺利，补给尚算充裕。";
        }
        else if (reserves >= 5)
        {
            reserveRumor = textConfig?.RumorReservesLow ?? "但亦有传闻，连年征战已令农庄凋敝，后勤日渐捉襟见肘。";
        }
        else
        {
            reserveRumor = textConfig?.RumorReservesCritical ?? "更有流言称军中已断粮三日，若无后援，恐将不战自溃。";
        }
        sb.AppendLine($"<color=#555555>{reserveRumor}</color>");

        // 添加额外随机情报
        sb.AppendLine();
        sb.AppendLine($"<color=#555555>> {GetRandomExtraRumor()}</color>");

        // 偶尔添加第二条额外情报
        if ((currentTurnSeed % 3) == 0)
        {
            sb.AppendLine($"<color=#555555>> {GetRandomExtraRumor(1)}</color>");
        }

        txtBattleIntel.text = sb.ToString();
    }

    /// <summary>
    /// 刷新我军动态 - 模糊描述我方将军状态
    /// </summary>
    private void RefreshAllyRumors(CampaignRuntimeData data)
    {
        if (txtAllyIntel == null) return;

        var sb = new StringBuilder();
        string header = textConfig?.IntelAllyRumorHeader ?? "【我军动态】";
        sb.AppendLine($"<b>{header}</b>");
        sb.AppendLine();

        int index = 0;
        foreach (var ally in data.Battle.AllyGenerals)
        {
            var status = ally.GetStatus(balanceConfig);
            string rumor = GetAllyRumor(ally.Name, status, index);
            sb.AppendLine($"- {rumor}");
            index++;
        }

        // 添加总体评价
        sb.AppendLine();
        sb.AppendLine($"<color=#555555>{GetOverallAllyAssessment(data)}</color>");

        txtAllyIntel.text = sb.ToString();
    }

    /// <summary>
    /// 刷新敌情传闻 - 模糊描述敌方将军状态
    /// </summary>
    private void RefreshEnemyRumors(CampaignRuntimeData data)
    {
        if (txtEnemyIntel == null) return;

        var sb = new StringBuilder();
        string header = textConfig?.IntelEnemyRumorHeader ?? "【敌情传闻】";
        sb.AppendLine($"<b>{header}</b>");
        sb.AppendLine();

        int index = 0;
        foreach (var enemy in data.Battle.EnemyGenerals)
        {
            var status = enemy.GetStatus(balanceConfig);
            string rumor = GetEnemyRumor(enemy.Name, status, index);
            sb.AppendLine($"- {rumor}");
            index++;
        }

        // 添加总体评价
        sb.AppendLine();
        sb.AppendLine($"<color=#555555>{GetOverallEnemyAssessment(data)}</color>");

        txtEnemyIntel.text = sb.ToString();
    }

    /// <summary>
    /// 刷新战史评说 - 基于历史战斗结果生成评论
    /// </summary>
    private void RefreshBattleHistory(CampaignRuntimeData data)
    {
        if (txtBattleHistory == null) return;

        var sb = new StringBuilder();
        string header = textConfig?.IntelBattleHistoryHeader ?? "【战史评说】";
        string source = textConfig?.IntelBattleHistorySource ?? "退役的雇佣兵队长";
        sb.AppendLine($"<b>{header}</b>");
        sb.AppendLine($"<color=#666666>- {source} 评曰:</color>");
        sb.AppendLine();

        // 检查是否有战斗历史
        if (data.TurnHistory == null || data.TurnHistory.Count == 0)
        {
            string noHistory = textConfig?.BattleHistoryNone ?? "战端方启，尚无可资评说之役。";
            sb.AppendLine($"\"{noHistory}\"");
            txtBattleHistory.text = sb.ToString();
            return;
        }

        // 分析最近几回合的战斗结果（最多3回合）
        int lookbackTurns = Mathf.Min(3, data.TurnHistory.Count);
        int allyWins = 0;      // 己方推进次数
        int enemyWins = 0;     // 敌方推进次数
        int stalemates = 0;    // 僵持次数
        string lastCritGeneral = null;
        string lastFumbleGeneral = null;

        for (int i = data.TurnHistory.Count - lookbackTurns; i < data.TurnHistory.Count; i++)
        {
            var turnRecord = data.TurnHistory[i];
            if (turnRecord.BattleResults == null) continue;

            foreach (var result in turnRecord.BattleResults)
            {
                if (result.AllyCapturedGrid.HasValue)
                    allyWins++;
                else if (result.EnemyCapturedGrid.HasValue)
                    enemyWins++;
                else
                    stalemates++;

                // 记录暴击/失误的将军（用于传说）
                if (result.WasCrit)
                {
                    // 尝试找到这个战线上的将军名
                    var ally = data.Battle.AllyGenerals.Find(g => g.Position == result.Position);
                    if (ally != null)
                        lastCritGeneral = ally.Name;
                }
                if (result.WasFumble)
                {
                    var ally = data.Battle.AllyGenerals.Find(g => g.Position == result.Position);
                    if (ally != null)
                        lastFumbleGeneral = ally.Name;
                }
            }
        }

        // 生成综合评价
        string mainComment;
        if (allyWins >= 3 && enemyWins == 0)
        {
            mainComment = textConfig?.BattleHistoryAllyVictory ?? "连战连捷！近来我军势如破竹，{0}战皆克，士气大振。坊间已有人将此比作古斯塔夫的辉煌战绩。";
            mainComment = string.Format(mainComment, allyWins);
        }
        else if (enemyWins >= 3 && allyWins == 0)
        {
            mainComment = textConfig?.BattleHistoryEnemyVictory ?? "形势堪忧！近来{0}役皆失，军心动摇，后方已有流言蜚语。有人私下议论，说这是华伦斯坦复仇的开始。";
            mainComment = string.Format(mainComment, enemyWins);
        }
        else if (allyWins > enemyWins)
        {
            mainComment = textConfig?.BattleHistoryAllyVictory ?? "近来我军略占上风，{0}战告捷。将士们信心渐增，士气可用。";
            mainComment = string.Format(mainComment, allyWins);
        }
        else if (enemyWins > allyWins)
        {
            mainComment = textConfig?.BattleHistoryEnemyVictory ?? "近来战事不利，{0}役失守。但老兵们说，胜败乃兵家常事，切莫气馁。";
            mainComment = string.Format(mainComment, enemyWins);
        }
        else if (stalemates >= lookbackTurns)
        {
            mainComment = textConfig?.BattleHistoryStalemate ?? "近来战事胶着，双方各有斩获，难分轩轾。有老兵说这让他想起了当年的白山之战前夕。";
        }
        else
        {
            mainComment = textConfig?.BattleHistoryMixed ?? "战局变幻莫测，胜败交替。有人说这是神意难测，也有人说这正是用兵者的良机。";
        }

        sb.AppendLine($"\"{mainComment}\"");

        // 添加暴击/失误的传说（如果有）
        if (!string.IsNullOrEmpty(lastCritGeneral) && (currentTurnSeed % 2) == 0)
        {
            sb.AppendLine();
            string critLegend = textConfig?.BattleHistoryCritLegend ?? "茶馆里都在传颂{0}将军的神勇——据说那一战，他亲率骑兵冲锋，杀得敌军丢盔弃甲。";
            critLegend = string.Format(critLegend, lastCritGeneral);
            sb.AppendLine($"<color=#CCAA00>{critLegend}</color>");
        }
        else if (!string.IsNullOrEmpty(lastFumbleGeneral) && (currentTurnSeed % 2) == 1)
        {
            sb.AppendLine();
            string fumbleLegend = textConfig?.BattleHistoryFumbleLegend ?? "有人悄悄议论{0}将军上次指挥失当——据说是辎重迷路，贻误了战机。";
            fumbleLegend = string.Format(fumbleLegend, lastFumbleGeneral);
            sb.AppendLine($"<color=#AA5555>{fumbleLegend}</color>");
        }

        txtBattleHistory.text = sb.ToString();
    }

    #region 评论生成方法

    private string GetAlphaComment(float alpha)
    {
        if (alpha > 0.20f)
            return textConfig?.AlphaVeryHigh ?? "自布赖滕费尔德会战以来，欧陆未见如此激战。各国宫廷都在疯狂采购军需，此乃千载难逢之良机。";
        if (alpha > 0.10f)
            return textConfig?.AlphaHigh ?? "波希米亚的战火正向萨克森蔓延，军需价格必将水涨船高，明智之人当早做准备。";
        if (alpha > 0.05f)
            return textConfig?.AlphaMedium ?? "战事胶着于日耳曼腹地，双方僵持不下。观望为上。";
        if (alpha > 0f)
            return textConfig?.AlphaLow ?? "前线暂无大动作，据闻双方正在威斯特伐利亚秘密接触。军需需求一时难有起色。";
        return textConfig?.AlphaVeryLow ?? "和谈风声四起，据说奥斯纳布吕克已有使团抵达。若真议和，军需市场恐将遇冷。";
    }

    private string GetBetaComment(float beta)
    {
        if (beta > 1.15f)
            return textConfig?.BetaVeryHigh ?? "自郁金香狂热以来，阿姆斯特丹未见如此盛况！买盘汹涌，价格扶摇直上指日可待。";
        if (beta > 1.05f)
            return textConfig?.BetaHigh ?? "交易活跃，热那亚与安特卫普的商人都在积极入市。行情稳步攀升。";
        if (beta > 0.95f)
            return textConfig?.BetaMedium ?? "买卖双方势均力敌，法兰克福的商会持观望态度，市场暂且平静。";
        if (beta > 0.85f)
            return textConfig?.BetaLow ?? "抛售压力渐增，据闻奥格斯堡的富格尔家族正在清仓。行情承压下行。";
        return textConfig?.BetaVeryLow ?? "恐慌如瘟疫般蔓延！卖盘如潮，连科隆的主教都在抛售存货。";
    }

    private string GetGammaComment(float gamma)
    {
        if (gamma > 1.30f)
            return textConfig?.GammaVeryHigh ?? "从吕贝克到但泽，货源奇缺！波罗的海航线受阻，仓库几近见底。";
        if (gamma > 1.10f)
            return textConfig?.GammaHigh ?? "汉堡与不莱梅的库存消耗加快，补给船队迟迟未至。供不应求之势渐显。";
        if (gamma > 0.95f)
            return textConfig?.GammaMedium ?? "供需尚算平衡，斯特拉斯堡的商会报告库存稳定。";
        if (gamma > 0.80f)
            return textConfig?.GammaLow ?? "波西米亚的矿山复产，货源渐丰。纽伦堡的仓库堆积如山。";
        return textConfig?.GammaVeryLow ?? "市场充斥存货！西里西亚的铁矿涌入市场，若不尽快出手，恐将血本无归。";
    }

    private string GetEnemyRumor(string generalName, GeneralStatus status, int index)
    {
        // 根据index选择不同的表述方式
        string[] healthyTemplates = new string[]
        {
            "{0}麾下兵强马壮，不可小觑。",
            "据斥候回报，{0}部士气高昂，装备精良。",
            "有俘虏交代，{0}将军治军甚严，其部战力可观。",
        };

        string[] woundedTemplates = new string[]
        {
            "据闻{0}部有所减员，战力或已削弱。",
            "有逃兵称{0}部近日伤亡不小，军心略有动摇。",
            "从前线传来的消息说，{0}所部已显疲态。",
        };

        string[] criticalTemplates = new string[]
        {
            "有信使称{0}部伤亡惨重，溃散在即。",
            "据可靠消息，{0}部已近崩溃，士兵逃亡者众。",
            "听闻{0}将军负伤，其部群龙无首，岌岌可危。",
        };

        string[] routedTemplates = new string[]
        {
            "{0}部已然溃散，不足为虑。",
            "{0}所部已作鸟兽散，残兵正四处逃窜。",
            "据报{0}部已不复存在，将军本人生死不明。",
        };

        int templateIndex = (currentTurnSeed + index * 3) % 3;

        return status switch
        {
            GeneralStatus.FullStrength or GeneralStatus.Healthy =>
                string.Format(healthyTemplates[templateIndex], generalName),
            GeneralStatus.Wounded =>
                string.Format(woundedTemplates[templateIndex], generalName),
            GeneralStatus.Critical =>
                string.Format(criticalTemplates[templateIndex], generalName),
            GeneralStatus.Routed =>
                string.Format(routedTemplates[templateIndex], generalName),
            _ => $"{generalName}的情况尚不明朗。"
        };
    }

    private string GetOverallEnemyAssessment(CampaignRuntimeData data)
    {
        int healthy = 0, wounded = 0, critical = 0, routed = 0;
        foreach (var enemy in data.Battle.EnemyGenerals)
        {
            var status = enemy.GetStatus(balanceConfig);
            switch (status)
            {
                case GeneralStatus.FullStrength:
                case GeneralStatus.Healthy:
                    healthy++;
                    break;
                case GeneralStatus.Wounded:
                    wounded++;
                    break;
                case GeneralStatus.Critical:
                    critical++;
                    break;
                case GeneralStatus.Routed:
                    routed++;
                    break;
            }
        }

        if (routed >= 2)
            return "总体而言，敌军已是强弩之末，胜利或许近在咫尺。";
        if (critical >= 2)
            return "敌军虽仍在抵抗，但颓势已现，宜乘胜追击。";
        if (wounded >= 2)
            return "敌军实力有所折损，但仍不可掉以轻心。";
        if (healthy >= 2)
            return "敌军主力尚存，实力不容小觑，诸位务必谨慎。";
        return "敌军虚实难测，还需进一步刺探。";
    }

    private string GetAllyRumor(string generalName, GeneralStatus status, int index)
    {
        // 根据index选择不同的表述方式
        string[] healthyTemplates = new string[]
        {
            "{0}将军所部士气高昂，枕戈待旦。",
            "后方传来消息，{0}部补给充足，将士们求战心切。",
            "据辎重营回报，{0}将军深得军心，麾下精兵强将。",
        };

        string[] woundedTemplates = new string[]
        {
            "据后方辎重营回报，{0}部近日伤亡颇重，正在整补。",
            "有伤兵被送回后方，说{0}部激战连连，损失不小。",
            "听闻{0}将军在前线苦战，部队减员明显。",
        };

        string[] criticalTemplates = new string[]
        {
            "有伤兵逃回称{0}部已是残兵败将，亟需增援。",
            "后方人心惶惶，都说{0}部即将不支，望速派援军。",
            "据逃回的哨兵说，{0}将军的旗帜已不见踪影，恐有不测。",
        };

        string[] routedTemplates = new string[]
        {
            "{0}部已溃不成军，旗帜尽失，将军下落不明。",
            "噩耗传来，{0}所部全军覆没，幸存者寥寥。",
            "有人说在战场上看到{0}将军的战马无人骑乘，凶多吉少。",
        };

        int templateIndex = (currentTurnSeed + index * 3) % 3;

        return status switch
        {
            GeneralStatus.FullStrength or GeneralStatus.Healthy =>
                string.Format(healthyTemplates[templateIndex], generalName),
            GeneralStatus.Wounded =>
                string.Format(woundedTemplates[templateIndex], generalName),
            GeneralStatus.Critical =>
                string.Format(criticalTemplates[templateIndex], generalName),
            GeneralStatus.Routed =>
                string.Format(routedTemplates[templateIndex], generalName),
            _ => $"{generalName}将军的消息暂时还未传来。"
        };
    }

    private string GetOverallAllyAssessment(CampaignRuntimeData data)
    {
        int healthy = 0, wounded = 0, critical = 0, routed = 0;
        foreach (var ally in data.Battle.AllyGenerals)
        {
            var status = ally.GetStatus(balanceConfig);
            switch (status)
            {
                case GeneralStatus.FullStrength:
                case GeneralStatus.Healthy:
                    healthy++;
                    break;
                case GeneralStatus.Wounded:
                    wounded++;
                    break;
                case GeneralStatus.Critical:
                    critical++;
                    break;
                case GeneralStatus.Routed:
                    routed++;
                    break;
            }
        }

        if (routed >= 2)
            return "我军伤亡惨重，多位将军阵亡或失踪。若不及时整顿，恐难再战。";
        if (critical >= 2)
            return "多路告急！将士们血战至今已疲惫不堪，亟需休整补充。";
        if (wounded >= 2)
            return "我军虽有损失，但将士们仍在坚守。当务之急是补充兵员。";
        if (healthy >= 2)
            return "我军士气尚可，诸将整装待发，只待一声令下。";
        return "各部情况参差不齐，宜从长计议。";
    }

    private string GetRandomSource()
    {
        int idx = currentTurnSeed % RumorSources.Length;
        return RumorSources[idx];
    }

    private string GetRandomExtraRumor(int offset = 0)
    {
        int idx = (currentTurnSeed + offset * 5) % ExtraRumors.Length;
        return ExtraRumors[idx];
    }

    private string GetRandomConflictingRumor()
    {
        int idx = (currentTurnSeed * 3) % ConflictingRumors.Length;
        return ConflictingRumors[idx];
    }

    #endregion

    private void RefreshKLineCharts(CampaignRuntimeData data)
    {
        if (data?.Market?.KLineHistory == null) return;

        var klineHistory = data.Market.KLineHistory;

        if (chartAtk != null && klineHistory.ContainsKey(OrderType.ATK))
            chartAtk.RefreshData(klineHistory[OrderType.ATK]);

        if (chartDef != null && klineHistory.ContainsKey(OrderType.DEF))
            chartDef.RefreshData(klineHistory[OrderType.DEF]);

        if (chartRet != null && klineHistory.ContainsKey(OrderType.RET))
            chartRet.RefreshData(klineHistory[OrderType.RET]);
    }

    private void OnRefresh(object p1, object p2) => RefreshUI();
}
