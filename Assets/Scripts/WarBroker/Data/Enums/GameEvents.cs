/// <summary>游戏事件ID (通过强转为 EventID 使用)</summary>
public enum WarBrokerEventID
{
    // 回合事件
    OnTurnStart = 1000,
    OnTurnEnd,
    OnPhaseChange,

    // 市场事件
    OnPriceUpdate,
    OnTradeExecuted,
    OnFuturesOpened,
    OnFuturesClosed,
    OnForceLiquidation,

    // 战斗事件
    OnBattleStart,
    OnBattleResult,
    OnBattleAnimationsComplete,  // 战斗动画播放完成
    OnFrontlineMove,
    OnGeneralStatusChange,
    OnGeneralRouted,
    OnGeneralRespawned,  // 将军复活

    // 玩家事件
    OnOrderAssigned,
    OnCashChange,
    OnNetWorthChange,
    OnAuditValueChange,
    OnIntentChanged,

    // 游戏事件
    OnRandomEvent,
    OnVictoryConditionMet,
    OnDefeatConditionMet,
    OnDrawConditionMet,
    OnGameEnd,
    OnRegimeCollapse,  // GDD v6.0: 政权崩溃清算
    OnCommissionCompleted,  // 委托任务完成（即时发放奖励时触发）

    // UI预览事件
    OnInventoryPreview,  // 库存预览变化（鼠标悬停买入/卖出按钮时），param1=OrderType, param2=int变化量(+1/-1/0)
    OnPhaseBannerComplete,  // 阶段横幅动画完成，param1=TurnPhase

    // 暂停/设置事件
    OnGamePaused,
    OnGameResumed,
    OnSettingsChanged,

    // === 新增事件（放在末尾避免影响已有 ID）===
    OnReinforcementsComplete = 1100,  // 基地休整动画播放完成
    OnGeneralReinforced,              // 将军获得基地休整 (param1=GeneralData, param2=int恢复量)
    OnBattleReportConfirmed,          // 战报确认完成（玩家点击确认或无战报时自动触发）

    // 信件对话事件
    OnVictorTurnStart = 1200,         // Victor 回合开始（param1=VictorTurnVisualData）
    OnVictorTurnComplete,             // Victor 回合信件播放完成

    // 结算通知事件
    OnTurnSettlement = 1300,          // 回合结算通知（param1=TurnSettlementInfo）

    // 教程事件
    OnTutorialComplete = 1400,        // 教程完成（param1=bool是否完整完成）
    OnTutorialWelcomeComplete,        // 教程开场白对话完成，可以开始回合
}
