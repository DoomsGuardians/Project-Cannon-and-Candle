using UnityEngine;

/// <summary>
/// UI文本配置
/// 用于配置游戏内各种UI显示的文本，可在Inspector中编辑
/// 文本风格模拟参谋提供的战报/情报
///
/// 按UI面板分类组织，每个字段都有详细说明其所属面板和使用场景
/// </summary>
[CreateAssetMenu(fileName = "UITextConfig", menuName = "WarBroker/UITextConfig")]
public class UITextConfig : ScriptableObject
{
    // ============================================================
    // GameplayWindow - 主游戏界面（顶部状态栏）
    // 游戏主界面，显示回合、阶段、资源等信息
    // ============================================================
    [Header("【GameplayWindow】主游戏界面 - 顶部状态栏的Tooltip提示")]

    [Tooltip("【GameplayWindow】鼠标悬停在「回合图标」上时显示的提示")]
    public string TooltipTurn = "当前星期 / 最大星期数";

    [Tooltip("【GameplayWindow】鼠标悬停在「现金图标」上时显示的提示\n说明现金的用途")]
    public string TooltipCash = "可用于采购军需、支付利息和仓储费用";

    [Tooltip("【GameplayWindow】鼠标悬停在「净资产图标」上时显示的提示\n说明净资产的计算方式")]
    public string TooltipNetWorth = "现金 + 库存价值 - 负债";

    // [已禁用] 审计值系统
    // [Tooltip("【GameplayWindow】鼠标悬停在「审计值图标」上时显示的提示\n说明审计值的作用")]
    // public string TooltipAudit = "初始资产，用于计算最终盈亏";

    [Tooltip("【GameplayWindow】鼠标悬停在「后备役图标/数值」上时显示的提示\n说明后备役的作用和消耗方式")]
    public string TooltipReserves = "后勤储备，用于补充前线兵员。将军撤退或在基地休整时消耗。";

    [Tooltip("【GameplayWindow】鼠标悬停在「ATK进攻令图标/数量」上时显示的提示\n说明进攻令的作用")]
    public string TooltipATK = "下达进攻指令，命令前线部队向敌阵推进";

    [Tooltip("【GameplayWindow】鼠标悬停在「DEF防守令图标/数量」上时显示的提示\n说明防守令的作用")]
    public string TooltipDEF = "下达防守指令，命令部队固守当前阵地";

    [Tooltip("【GameplayWindow】鼠标悬停在「RET撤退令图标/数量」上时显示的提示\n说明撤退令的作用")]
    public string TooltipRET = "下达撤退指令，命令部队后撤进行休整补充";

    [Header("【GameplayWindow】主游戏界面 - Tab按钮的Tooltip提示")]

    [Tooltip("【GameplayWindow】鼠标悬停在「市场」Tab按钮上时显示的提示")]
    public string TooltipBtnMarket = "打开市场面板，进行现货/期货交易";

    [Tooltip("【GameplayWindow】鼠标悬停在「情报」Tab按钮上时显示的提示")]
    public string TooltipBtnInfo = "打开情报面板，查看战场态势和K线图";

    // ============================================================
    // MarketPanel - 市场面板（现货/期货交易 + 银行借贷）
    // ============================================================
    [Header("【MarketPanel】市场面板 - 银行区域按钮的Tooltip提示")]

    [Tooltip("【MarketPanel】鼠标悬停在「减少金额」按钮上时显示的提示")]
    public string TooltipBtnDecrease = "减少金额 (-100)";

    [Tooltip("【MarketPanel】鼠标悬停在「增加金额」按钮上时显示的提示")]
    public string TooltipBtnIncrease = "增加金额 (+100)";

    [Tooltip("【MarketPanel】鼠标悬停在「100」快捷按钮上时显示的提示")]
    public string TooltipBtn100 = "设置金额为 100";

    [Tooltip("【MarketPanel】鼠标悬停在「500」快捷按钮上时显示的提示")]
    public string TooltipBtn500 = "设置金额为 500";

    [Tooltip("【MarketPanel】鼠标悬停在「MAX」快捷按钮上时显示的提示")]
    public string TooltipBtnMax = "设置为当前可借最大额度";

    [Tooltip("【MarketPanel】鼠标悬停在「借贷」按钮上时显示的提示")]
    public string TooltipBtnBorrow = "向银行借入指定金额，每星期需支付利息";

    [Tooltip("【MarketPanel】鼠标悬停在「还款」按钮上时显示的提示")]
    public string TooltipBtnRepay = "偿还银行贷款，减少利息支出";

    // ============================================================
    // SpotMarketTab - 现货市场Tab（买卖指令）
    // ============================================================
    [Header("【SpotMarketTab】现货市场 - 交易按钮的Tooltip提示")]

    [Tooltip("【SpotMarketTab】鼠标悬停在「买入」按钮上时显示的提示")]
    public string TooltipBtnBuy = "买入1单位指令，价格随需求上涨";

    [Tooltip("【SpotMarketTab】鼠标悬停在「卖出」按钮上时显示的提示")]
    public string TooltipBtnSell = "卖出1单位指令，价格随供给下跌";

    [Tooltip("【SpotMarketTab】鼠标悬停在「K线图」按钮上时显示的提示")]
    public string TooltipBtnChart = "切换显示该指令的K线图";

    // ============================================================
    // FuturesMarketTab - 期货市场Tab（做多/做空）
    // ============================================================
    [Header("【FuturesMarketTab】期货市场 - 交易按钮的Tooltip提示")]

    [Tooltip("【FuturesMarketTab】鼠标悬停在「做多」按钮上时显示的提示")]
    public string TooltipBtnLong = "开多仓，预期价格上涨获利。3星期后结算";

    [Tooltip("【FuturesMarketTab】鼠标悬停在「做空」按钮上时显示的提示")]
    public string TooltipBtnShort = "开空仓，预期价格下跌获利。3星期后结算";

    [Tooltip("【FuturesMarketTab】鼠标悬停在「平仓」按钮上时显示的提示")]
    public string TooltipBtnClose = "提前平仓，按当前价格结算盈亏";

    // ============================================================
    // GeneralDetailPanel - 将军详情面板（强化/篡改）
    // ============================================================
    [Header("【GeneralDetailPanel】将军详情面板 - 操作按钮的Tooltip提示")]

    [Tooltip("【GeneralDetailPanel】鼠标悬停在「强化」按钮上时显示的提示")]
    public string TooltipBtnReinforce = "消耗对应指令，强化该将军的意图（消耗数量见配置）";

    [Tooltip("【GeneralDetailPanel】鼠标悬停在「篡改」按钮上时显示的提示")]
    public string TooltipBtnOverride = "消耗指令，强制改变将军的行动意图（消耗数量见配置）";

    // ============================================================
    // MusicPlayerPanel - 音乐播放器面板
    // ============================================================
    [Header("【MusicPlayerPanel】音乐播放器 - 控制按钮的Tooltip提示")]

    [Tooltip("【MusicPlayerPanel】鼠标悬停在「上一首」按钮上时显示的提示")]
    public string TooltipBtnPrevious = "上一首";

    [Tooltip("【MusicPlayerPanel】鼠标悬停在「播放/暂停」按钮上时显示的提示")]
    public string TooltipBtnPlayPause = "播放 / 暂停";

    [Tooltip("【MusicPlayerPanel】鼠标悬停在「下一首」按钮上时显示的提示")]
    public string TooltipBtnNext = "下一首";

    [Tooltip("【MusicPlayerPanel】鼠标悬停在「随机播放」按钮上时显示的提示")]
    public string TooltipBtnShuffle = "随机播放";

    // ============================================================
    // InfoPanel - 信息面板（市场舆情 + 战场传闻）
    // 通过历史人物和消息人士的视角，间接暗示价格影响因子
    // ============================================================
    [Header("【InfoPanel】信息面板 - 市场舆情人物")]

    [Tooltip("【InfoPanel】评论战场态势(Alpha因子)的人物名称\n暗示战场局势对价格的影响")]
    public string IntelPersonAlpha = "红衣主教黎塞留";

    [Tooltip("【InfoPanel】评论市场情绪(Beta因子)的人物名称\n暗示交易活动对价格的影响")]
    public string IntelPersonBeta = "阿姆斯特丹交易所掮客";

    [Tooltip("【InfoPanel】评论供应情况(Gamma因子)的人物名称\n暗示库存流通对价格的影响")]
    public string IntelPersonGamma = "汉萨同盟军需承包商";

    [Header("【InfoPanel】信息面板 - Alpha因子评论（战场态势）")]

    [Tooltip("Alpha很高(>0.20)时的评论\n战局极度紧张")]
    public string AlphaVeryHigh = "自布赖滕费尔德会战以来，欧陆未见如此激战。各国宫廷都在疯狂采购军需，维也纳与巴黎的使者络绎于途。此乃千载难逢之良机。";

    [Tooltip("Alpha较高(0.10-0.20)时的评论\n战局紧张")]
    public string AlphaHigh = "波希米亚的战火正向萨克森蔓延，新教诸侯惶惶不安。军需价格必将水涨船高，明智之人当早做准备。";

    [Tooltip("Alpha中等(0.05-0.10)时的评论\n战局正常")]
    public string AlphaMedium = "战事胶着于日耳曼腹地，双方僵持不下。帝国军与瑞典人都在舔舐伤口，观望为上。";

    [Tooltip("Alpha较低(0-0.05)时的评论\n战局平静")]
    public string AlphaLow = "前线暂无大动作，据闻双方正在威斯特伐利亚秘密接触。军需需求一时难有起色。";

    [Tooltip("Alpha很低(<0)时的评论\n战局极度平静")]
    public string AlphaVeryLow = "和谈风声四起，据说奥斯纳布吕克已有使团抵达。若真议和，军需市场恐将遇冷，诸位当早做打算。";

    [Header("【InfoPanel】信息面板 - Beta因子评论（市场情绪）")]

    [Tooltip("Beta很高(>1.15)时的评论\n市场狂热追涨")]
    public string BetaVeryHigh = "自郁金香狂热以来，阿姆斯特丹未见如此盛况！买盘汹涌，连威尼斯的银行家都在抢购。诸君切勿错失良机，价格扶摇直上指日可待。";

    [Tooltip("Beta较高(1.05-1.15)时的评论\n市场乐观")]
    public string BetaHigh = "交易活跃，热那亚与安特卫普的商人都在积极入市。买方占据主动，行情稳步攀升。";

    [Tooltip("Beta中等(0.95-1.05)时的评论\n市场平稳")]
    public string BetaMedium = "买卖双方势均力敌，价格波动有限。法兰克福的商会持观望态度，市场暂且平静。";

    [Tooltip("Beta较低(0.85-0.95)时的评论\n市场悲观")]
    public string BetaLow = "抛售压力渐增，据闻奥格斯堡的富格尔家族正在清仓。买方持币观望，行情承压下行。";

    [Tooltip("Beta很低(<0.85)时的评论\n市场恐慌")]
    public string BetaVeryLow = "恐慌如瘟疫般蔓延！卖盘如潮，连科隆的主教都在抛售存货。价格一泻千里，血流成河。";

    [Header("【InfoPanel】信息面板 - Gamma因子评论（库存流通）")]

    [Tooltip("Gamma很高(>1.30)时的评论\n库存极度紧张")]
    public string GammaVeryHigh = "从吕贝克到但泽，货源奇缺！波罗的海航线受阻，仓库几近见底。有价无市，手中有货者待价而沽。";

    [Tooltip("Gamma较高(1.10-1.30)时的评论\n库存紧张")]
    public string GammaHigh = "汉堡与不莱梅的库存消耗加快，补给船队迟迟未至。供不应求之势渐显，价格看涨。";

    [Tooltip("Gamma中等(0.95-1.10)时的评论\n供需平衡")]
    public string GammaMedium = "供需尚算平衡，莱茵河沿岸的仓储周转正常。斯特拉斯堡的商会报告库存稳定。";

    [Tooltip("Gamma较低(0.80-0.95)时的评论\n库存充裕")]
    public string GammaLow = "波西米亚的矿山复产，货源渐丰。纽伦堡的仓库堆积如山，卖方急于出货套现。";

    [Tooltip("Gamma很低(<0.80)时的评论\n库存过剩")]
    public string GammaVeryLow = "市场充斥存货！西里西亚的铁矿涌入市场，仓储压力剧增。若不尽快出手，恐将血本无归。";

    [Header("【InfoPanel】信息面板 - 战场传闻（中立消息人士视角）")]

    [Tooltip("【InfoPanel】战场传闻区域的标题")]
    public string IntelRumorHeader = "【战场传闻】";

    [Tooltip("【InfoPanel】消息来源人物名称")]
    public string IntelRumorSource = "途经莱比锡的商队";

    [Tooltip("战线在己方腹地(Grid 1-2)时的传闻")]
    public string RumorDefensive = "据从马格德堡逃来的难民称，帝国军节节败退，北方雄狮的铁蹄已逼近腹地，形势万分危急。维也纳宫廷人心惶惶。";

    [Tooltip("战线在中线(Grid 3)时的传闻")]
    public string RumorStalemate = "有从纽伦堡来的信使透露，双方主力在日耳曼腹地对峙，瑞典人占据有利地形，而帝国军固守城池。胜负尚难预料。";

    [Tooltip("战线在敌方腹地(Grid 4-5)时的传闻")]
    public string RumorOffensive = "驿站传来捷报，据称华伦斯坦元帅挥师北进，已深入敌境。瑞典人的补给线岌岌可危，或将被迫后撤。";

    [Tooltip("多条战线情况不一时的传闻")]
    public string RumorMixed = "各路战报真假难辨：有说瑞典人在萨克森大捷，又有传闻帝国军在巴伐利亚反攻。战局扑朔迷离，唯有静观其变。";

    [Tooltip("后备役充足时的补充")]
    public string RumorReservesHigh = "后方蒂罗尔的征兵顺利，波西米亚的粮草亦已筹齐，补给尚算充裕。";

    [Tooltip("后备役紧张时的补充")]
    public string RumorReservesLow = "但亦有传闻，连年征战已令农庄凋敝，后勤补给日渐捉襟见肘。";

    [Tooltip("后备役危急时的补充")]
    public string RumorReservesCritical = "更有流言称军中已断粮三日，士兵开始劫掠乡里。若无后援，恐将不战自溃。";

    [Header("【InfoPanel】信息面板 - 敌情传闻")]

    [Tooltip("【InfoPanel】敌情传闻区域的标题")]
    public string IntelEnemyRumorHeader = "【敌情传闻】";

    [Tooltip("敌方将军健康时的传闻模板\n{0}=将军名")]
    public string EnemyRumorHealthy = "{0}麾下兵强马壮，不可小觑。";

    [Tooltip("敌方将军受伤时的传闻模板\n{0}=将军名")]
    public string EnemyRumorWounded = "据闻{0}部有所减员，战力或已削弱。";

    [Tooltip("敌方将军危急时的传闻模板\n{0}=将军名")]
    public string EnemyRumorCritical = "有信使称{0}部伤亡惨重，溃散在即。";

    [Tooltip("敌方将军溃败时的传闻模板\n{0}=将军名")]
    public string EnemyRumorRouted = "{0}部已然溃散，不足为虑。";

    [Header("【InfoPanel】信息面板 - 我军传闻")]

    [Tooltip("【InfoPanel】我军传闻区域的标题")]
    public string IntelAllyRumorHeader = "【我军动态】";

    [Tooltip("我军将军健康时的传闻模板\n{0}=将军名")]
    public string AllyRumorHealthy = "{0}将军所部士气高昂，枕戈待旦。";

    [Tooltip("我军将军受伤时的传闻模板\n{0}=将军名")]
    public string AllyRumorWounded = "据后方辎重营回报，{0}部近日伤亡颇重，正在整补。";

    [Tooltip("我军将军危急时的传闻模板\n{0}=将军名")]
    public string AllyRumorCritical = "有伤兵逃回称{0}部已是残兵败将，亟需增援。";

    [Tooltip("我军将军溃败时的传闻模板\n{0}=将军名")]
    public string AllyRumorRouted = "{0}部已溃不成军，旗帜尽失，将军下落不明。";

    [Header("【InfoPanel】信息面板 - 战史评说")]

    [Tooltip("【InfoPanel】战史评说区域的标题")]
    public string IntelBattleHistoryHeader = "【战史评说】";

    [Tooltip("无战斗历史时的描述")]
    public string BattleHistoryNone = "战端方启，尚无可资评说之役。";

    [Tooltip("近期己方大胜的评说\n{0}=胜利次数")]
    public string BattleHistoryAllyVictory = "连战连捷！近来我军势如破竹，{0}战皆克，士气大振。坊间已有人将此比作古斯塔夫的辉煌战绩。";

    [Tooltip("近期敌方大胜的评说\n{0}=败北次数")]
    public string BattleHistoryEnemyVictory = "形势堪忧！近来{0}役皆失，军心动摇，后方已有流言蜚语。有人私下议论，说这是华伦斯坦复仇的开始。";

    [Tooltip("近期胶着的评说")]
    public string BattleHistoryStalemate = "近来战事胶着，双方各有斩获，难分轩轾。有老兵说这让他想起了当年的白山之战前夕。";

    [Tooltip("近期互有胜负的评说")]
    public string BattleHistoryMixed = "战局变幻莫测，胜败交替。有人说这是神意难测，也有人说这正是用兵者的良机。";

    [Tooltip("发生暴击的传说\n{0}=将军名")]
    public string BattleHistoryCritLegend = "茶馆里都在传颂{0}将军的神勇——据说那一战，他亲率骑兵冲锋，杀得敌军丢盔弃甲。";

    [Tooltip("发生失误的传说\n{0}=将军名")]
    public string BattleHistoryFumbleLegend = "有人悄悄议论{0}将军上次指挥失当——据说是辎重迷路，贻误了战机。";

    [Tooltip("战况综合评价人物")]
    public string IntelBattleHistorySource = "退役的雇佣兵队长";

    // ============================================================
    // InfoPanel & GeneralDetailPanel - 将军状态文本
    // 用于显示将军的健康/战斗状态
    // ============================================================
    [Header("【InfoPanel/GeneralDetailPanel】将军状态描述文本")]

    [Tooltip("【InfoPanel/GeneralDetailPanel】将军HP=100%时的状态描述\n显示在将军信息中")]
    public string StatusFullStrength = "满编待命";

    [Tooltip("【InfoPanel/GeneralDetailPanel】将军HP较高时的状态描述\n显示在将军信息中")]
    public string StatusHealthy = "战力充沛";

    [Tooltip("【InfoPanel/GeneralDetailPanel】将军HP中等时的状态描述\n显示在将军信息中")]
    public string StatusWounded = "减员中";

    [Tooltip("【InfoPanel/GeneralDetailPanel】将军HP较低时的状态描述\n显示在将军信息中")]
    public string StatusCritical = "亟需支援";

    [Tooltip("【InfoPanel/GeneralDetailPanel】将军HP=0已溃败时的状态描述\n显示在将军信息中")]
    public string StatusRouted = "已溃散";

    // ============================================================
    // BattleResultPopup - 战斗结算弹窗
    // 每回合战斗阶段结束后弹出，显示各战线的战斗结果
    // ============================================================
    [Header("【BattleResultPopup】战斗结算弹窗 - 标题")]

    [Tooltip("【BattleResultPopup】弹窗标题\n{0}=当前回合/周数\n例如：\"第3周 战斗报告\"")]
    public string BattleResultTitle = "第{0}周 战斗报告";

    [Header("【BattleResultPopup】战斗结算弹窗 - 战线结果描述")]

    [Tooltip("【BattleResultPopup】双方都发起进攻时的结果描述\n当己方和敌方同时占领对方格子时显示")]
    public string BattleBothEngaged = "双方激烈交火，战线僵持";

    [Tooltip("【BattleResultPopup】己方占领敌方格子时的结果描述\n{0}=占领的格子编号（1-5）\n例如：\"我军成功推进至3号阵地\"")]
    public string BattleAllyCaptured = "我军成功推进至{0}号阵地";

    [Tooltip("【BattleResultPopup】敌方占领己方格子时的结果描述\n{0}=被占领的格子编号（1-5）\n例如：\"敌军占领了2号阵地\"")]
    public string BattleEnemyCaptured = "敌军占领了{0}号阵地";

    [Tooltip("【BattleResultPopup】战线无变化时的结果描述\n当双方都未能占领对方格子时显示")]
    public string BattleStalemate = "战线无变化，双方对峙中";

    [Tooltip("【BattleResultPopup】暴击发生时的特殊标记\n当战斗触发暴击时，在结果旁显示（黄色）")]
    public string BattleCriticalHit = "战果显著！";

    [Tooltip("【BattleResultPopup】失误发生时的特殊标记\n当战斗触发失误时，在结果旁显示（红色）")]
    public string BattleFumble = "指挥失误！";

    [Header("【BattleResultPopup】战斗结算弹窗 - 确认按钮")]

    [Tooltip("【BattleResultPopup】默认确认按钮文本\n触发条件：双方互有推进（己方占领了敌方格子，同时敌方也占领了己方格子）\n点击后关闭弹窗，继续游戏")]
    public string BattleResultBtnConfirm = "知悉";

    [Tooltip("【BattleResultPopup】己方大胜时的确认按钮文本\n触发条件：至少一条战线己方占领了敌方格子，且没有任何战线被敌方占领")]
    public string BattleResultBtnVictory = "大捷！";

    [Tooltip("【BattleResultPopup】己方惨败时的确认按钮文本\n触发条件：至少一条战线被敌方占领，且己方没有占领任何敌方格子")]
    public string BattleResultBtnDefeat = "...继续";

    [Tooltip("【BattleResultPopup】战线僵持时的确认按钮文本\n触发条件：所有战线都没有格子变化（双方都未能占领对方格子）")]
    public string BattleResultBtnStalemate = "继续观望";

    [Header("【BattleResultPopup】战斗结算弹窗 - 指令对决插图")]

    [Tooltip("【BattleResultPopup】进攻 vs 进攻 的插图")]
    public Sprite BattleImageAtkVsAtk;

    [Tooltip("【BattleResultPopup】进攻 vs 防守 的插图")]
    public Sprite BattleImageAtkVsDef;

    [Tooltip("【BattleResultPopup】进攻 vs 撤退 的插图")]
    public Sprite BattleImageAtkVsRet;

    [Tooltip("【BattleResultPopup】防守 vs 进攻 的插图")]
    public Sprite BattleImageDefVsAtk;

    [Tooltip("【BattleResultPopup】防守 vs 防守 的插图")]
    public Sprite BattleImageDefVsDef;

    [Tooltip("【BattleResultPopup】防守 vs 撤退 的插图")]
    public Sprite BattleImageDefVsRet;

    [Tooltip("【BattleResultPopup】撤退 vs 进攻 的插图")]
    public Sprite BattleImageRetVsAtk;

    [Tooltip("【BattleResultPopup】撤退 vs 防守 的插图")]
    public Sprite BattleImageRetVsDef;

    [Tooltip("【BattleResultPopup】撤退 vs 撤退 的插图")]
    public Sprite BattleImageRetVsRet;

    // ============================================================
    // CampaignEndPopup - 战役结束弹窗
    // 战役胜利或失败时弹出，显示最终统计
    // ============================================================
    [Header("【CampaignEndPopup】战役结束弹窗 - 标题")]

    [Tooltip("【CampaignEndPopup】战役胜利时的标题\n当达成胜利条件（敌方基地被占领）时显示")]
    public string CampaignVictory = "胜利！";

    [Tooltip("【CampaignEndPopup】战役失败时的标题\n当达成失败条件（己方基地被占领）时显示")]
    public string CampaignDefeat = "战败";

    [Header("【CampaignEndPopup】战役结束弹窗 - 统计标签")]

    [Tooltip("【CampaignEndPopup】最终回合数的标签\n显示战役持续了多少星期")]
    public string CampaignStatsTurn = "最终星期";

    [Tooltip("【CampaignEndPopup】现金结余的标签\n显示玩家剩余现金")]
    public string CampaignStatsCash = "现金结余";

    [Tooltip("【CampaignEndPopup】总资产的标签\n显示玩家净资产（现金+库存价值-负债）")]
    public string CampaignStatsNetWorth = "总资产";

    [Tooltip("【CampaignEndPopup】委托任务区域的标题\n显示在委托完成情况列表上方")]
    public string CampaignStatsCommissions = "委托任务";

    [Header("【CampaignEndPopup】战役结束弹窗 - 按钮")]

    [Tooltip("【CampaignEndPopup】重新开始按钮文本\n点击后重新开始当前战役")]
    public string CampaignBtnRestart = "再来一局";

    [Tooltip("【CampaignEndPopup】返回主菜单按钮文本\n点击后返回游戏主菜单")]
    public string CampaignBtnMainMenu = "返回主菜单";

    // ============================================================
    // EventPopup - 随机事件弹窗
    // 回合开始时可能触发的随机事件
    // ============================================================
    [Header("【EventPopup】随机事件弹窗")]

    [Tooltip("【EventPopup】事件效果区域的标题\n显示在效果列表上方")]
    public string EventEffectsHeader = "【事件效果】";

    [Tooltip("【EventPopup】确认按钮文本\n点击后关闭事件弹窗")]
    public string EventBtnConfirm = "了解";

    // ============================================================
    // PausePopup - 暂停弹窗
    // 按ESC键时弹出的暂停菜单
    // ============================================================
    [Header("【PausePopup】暂停弹窗")]

    [Tooltip("【PausePopup】弹窗标题\n显示在暂停菜单顶部")]
    public string PauseTitle = "游戏暂停";

    [Tooltip("【PausePopup】继续游戏按钮文本\n点击后关闭暂停菜单，继续游戏")]
    public string PauseBtnResume = "继续";

    [Tooltip("【PausePopup】设置按钮文本\n点击后打开设置界面")]
    public string PauseBtnSettings = "设置";

    [Tooltip("【PausePopup】返回主菜单按钮文本\n点击后返回游戏主菜单")]
    public string PauseBtnMainMenu = "返回主菜单";

    [Tooltip("【PausePopup】退出游戏按钮文本\n点击后退出游戏")]
    public string PauseBtnQuit = "退出游戏";

    // ============================================================
    // PhaseBanner - 阶段横幅
    // 阶段切换时从屏幕中央滑过的横幅，类似炉石传说的回合提示
    // 只显示3个玩家可感知的阶段
    // ============================================================
    [Header("【PhaseBanner】阶段横幅 - 玩家阶段")]

    [Tooltip("【PhaseBanner】玩家阶段的横幅标题\n对应MarketPhase，玩家进行交易和部署")]
    public string PhasePlayer = "玩家阶段";

    [Tooltip("【PhaseBanner】玩家阶段的横幅副标题")]
    public string PhasePlayerSubtitle = "进行交易与部署";

    [Header("【PhaseBanner】阶段横幅 - 对手阶段")]

    [Tooltip("【PhaseBanner】对手阶段的横幅标题\n对应IntentPhase，AI对手行动")]
    public string PhaseOpponent = "对手阶段";

    [Tooltip("【PhaseBanner】对手阶段的横幅副标题")]
    public string PhaseOpponentSubtitle = "敌方正在行动...";

    [Header("【PhaseBanner】阶段横幅 - 战斗阶段")]

    [Tooltip("【PhaseBanner】战斗阶段的横幅标题\n对应BattlePhase，战斗结算")]
    public string PhaseBattle = "战斗阶段";

    [Tooltip("【PhaseBanner】战斗阶段的横幅副标题")]
    public string PhaseBattleSubtitle = "前线战斗即将打响";

    // ============================================================
    // 辅助方法
    // ============================================================

    /// <summary>
    /// 获取将军状态文本
    /// </summary>
    public string GetStatusText(GeneralStatus status)
    {
        return status switch
        {
            GeneralStatus.FullStrength => StatusFullStrength,
            GeneralStatus.Healthy => StatusHealthy,
            GeneralStatus.Wounded => StatusWounded,
            GeneralStatus.Critical => StatusCritical,
            GeneralStatus.Routed => StatusRouted,
            _ => status.ToString()
        };
    }

    /// <summary>
    /// 根据战斗结果获取确认按钮文本
    /// </summary>
    /// <param name="hasAllyCapture">己方是否占领了格子</param>
    /// <param name="hasEnemyCapture">敌方是否占领了格子</param>
    /// <param name="isStalemate">是否僵持（无任何占领）</param>
    public string GetBattleConfirmButtonText(bool hasAllyCapture, bool hasEnemyCapture, bool isStalemate)
    {
        if (hasEnemyCapture && !hasAllyCapture)
            return BattleResultBtnDefeat;
        if (hasAllyCapture && !hasEnemyCapture)
            return BattleResultBtnVictory;
        if (isStalemate)
            return BattleResultBtnStalemate;
        return BattleResultBtnConfirm;
    }

    /// <summary>
    /// 根据指令对决组合获取战斗插图
    /// </summary>
    /// <param name="allyOrder">己方指令</param>
    /// <param name="enemyOrder">敌方指令</param>
    public Sprite GetBattleImage(OrderType allyOrder, OrderType enemyOrder)
    {
        return (allyOrder, enemyOrder) switch
        {
            (OrderType.ATK, OrderType.ATK) => BattleImageAtkVsAtk,
            (OrderType.ATK, OrderType.DEF) => BattleImageAtkVsDef,
            (OrderType.ATK, OrderType.RET) => BattleImageAtkVsRet,
            (OrderType.DEF, OrderType.ATK) => BattleImageDefVsAtk,
            (OrderType.DEF, OrderType.DEF) => BattleImageDefVsDef,
            (OrderType.DEF, OrderType.RET) => BattleImageDefVsRet,
            (OrderType.RET, OrderType.ATK) => BattleImageRetVsAtk,
            (OrderType.RET, OrderType.DEF) => BattleImageRetVsDef,
            (OrderType.RET, OrderType.RET) => BattleImageRetVsRet,
            _ => null
        };
    }
}
