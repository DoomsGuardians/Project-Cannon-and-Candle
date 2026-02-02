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
    OnSkillTriggered,

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

    // 暂停/设置事件
    OnGamePaused,
    OnGameResumed,
    OnSettingsChanged
}
