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
    public string TooltipTurn = "当前回合 / 最大回合数";

    [Tooltip("【GameplayWindow】鼠标悬停在「现金图标」上时显示的提示\n说明现金的用途")]
    public string TooltipCash = "可用于采购军需、支付利息和仓储费用";

    [Tooltip("【GameplayWindow】鼠标悬停在「净资产图标」上时显示的提示\n说明净资产的计算方式")]
    public string TooltipNetWorth = "现金 + 库存价值 - 负债";

    [Tooltip("【GameplayWindow】鼠标悬停在「审计值图标」上时显示的提示\n说明审计值的作用")]
    public string TooltipAudit = "初始资产，用于计算最终盈亏";

    [Tooltip("【GameplayWindow】鼠标悬停在「后备役图标/数值」上时显示的提示\n说明后备役的作用和消耗方式")]
    public string TooltipReserves = "后勤储备，用于补充前线兵员。将军撤退或在基地休整时消耗。";

    [Tooltip("【GameplayWindow】鼠标悬停在「ATK进攻令图标/数量」上时显示的提示\n说明进攻令的作用")]
    public string TooltipATK = "下达进攻指令，命令前线部队向敌阵推进";

    [Tooltip("【GameplayWindow】鼠标悬停在「DEF防守令图标/数量」上时显示的提示\n说明防守令的作用")]
    public string TooltipDEF = "下达防守指令，命令部队固守当前阵地";

    [Tooltip("【GameplayWindow】鼠标悬停在「RET撤退令图标/数量」上时显示的提示\n说明撤退令的作用")]
    public string TooltipRET = "下达撤退指令，命令部队后撤进行休整补充";

    // ============================================================
    // InfoPanel - 信息面板（战场情报）
    // 显示战线态势、敌方将军信息、K线图等
    // ============================================================
    [Header("【InfoPanel】信息面板 - 战场情报区域的标题和格式")]

    [Tooltip("【InfoPanel】战线态势区域的标题\n显示在战线格子状态列表的上方")]
    public string IntelFrontlineHeader = "【前线战报】";

    [Tooltip("【InfoPanel】敌方将军信息区域的标题\n显示在敌方将军列表的上方")]
    public string IntelEnemyHeader = "【敌情通报】";

    [Tooltip("【InfoPanel】后备役显示格式\n{0}=己方后备役数量，{1}=敌方后备役估计值\n显示在战线态势区域底部")]
    public string IntelReservesFormat = "后勤储备: 我军 {0} / 敌军约 {1}";

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

    [Tooltip("【CampaignEndPopup】最终回合数的标签\n显示战役持续了多少回合")]
    public string CampaignStatsTurn = "最终回合";

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
}
