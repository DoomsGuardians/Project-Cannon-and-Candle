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
    OnGameEnd
}
