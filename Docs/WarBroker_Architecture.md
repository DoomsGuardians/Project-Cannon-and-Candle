# The War Broker - 架构设计与代码文档

> 本文档整理了"战争掮客"(The War Broker) 从阶段 1 到阶段 5 的全部代码，按架构层次分类说明。

---

## 目录

1. [架构总览](#1-架构总览)
2. [阶段 1：配置系统 (Config)](#2-阶段-1配置系统)
3. [阶段 2：运行时数据 (Runtime Data)](#3-阶段-2运行时数据)
4. [阶段 3：核心系统 (Core Systems)](#4-阶段-3核心系统)
5. [阶段 4：场景与游戏模式 (Scene & GameMode)](#5-阶段-4场景与游戏模式)
6. [阶段 5：UI 系统 (UI)](#6-阶段-5ui-系统)
7. [调试工具 (Debug & Editor)](#7-调试工具)
8. [文件清单](#8-文件清单)

---

## 1. 架构总览

```
┌──────────────────────────────────────────────────────────┐
│                    Levity Framework                       │
│  GameRoot ─── GameModeBase ─── ManagerBase ─── WindowBase │
│  EventService / UIService / ResService / ManagerService   │
└──────────────────────────────────────────────────────────┘
        │
        ▼
┌──────────────────────────────────────────────────────────┐
│                    WarBroker 游戏层                        │
│                                                          │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────┐ │
│  │ 配置层       │  │ 数据层        │  │ 系统层           │ │
│  │ (SO Config) │  │ (Runtime)    │  │ (ILogic)        │ │
│  │             │  │              │  │                 │ │
│  │ GameBalance │  │ CampaignData │  │ MarketSystem    │ │
│  │ OrderConfig │  │ PlayerData   │  │ BattleSystem    │ │
│  │ SkillConfig │  │ MarketData   │  │ CampaignSystem  │ │
│  │ GeneralConf │  │ BattleData   │  │                 │ │
│  │ CampaignCfg │  │ GeneralData  │  │                 │ │
│  └─────────────┘  └──────────────┘  └─────────────────┘ │
│                                                          │
│  ┌─────────────┐  ┌──────────────────────────────────┐   │
│  │ 场景层       │  │ UI 层                             │   │
│  │ (GameMode)  │  │ (WindowBase)                     │   │
│  │             │  │                                  │   │
│  │ BattleGame  │  │ GameplayWindow  MarketPanel      │   │
│  │   Mode      │  │ BattlefieldPanel GeneralPanel    │   │
│  │ Gameplay    │  │ IntelPanel  HistoryPanel          │   │
│  │   Manager   │  │ GameEndWindow                    │   │
│  └─────────────┘  └──────────────────────────────────┘   │
│                                                          │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ 调试层 (Editor / Runtime)                             │ │
│  │ WarBrokerConfigSetup  WarBrokerDebugWindow           │ │
│  │ WarBrokerDebugConsole WarBrokerUIPrefabGenerator     │ │
│  └──────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
```

### 数据流

```
ScriptableObject 配置 ──读取──► 运行时数据 ◄──修改── 核心系统 (ILogic)
                                    │
                                    ▼
                              GameplayManager (ManagerBase)
                                    │
                            ┌───────┼───────┐
                            ▼       ▼       ▼
                         UI窗口  事件广播  胜负判定
```

### 事件驱动

所有层通过 `EventService` 解耦通信，事件 ID 定义在 `WarBrokerEventID` 枚举中（起始值 1000）。

---

## 2. 阶段 1：配置系统

### 2.1 枚举定义

**文件**: `Data/Enums/GameEnums.cs`

```csharp
/// <summary>指令类型</summary>
public enum OrderType
{
    ATK,  // 进攻令
    DEF,  // 防守令
    RET   // 撤退令
}

/// <summary>将军性格</summary>
public enum GeneralPersonality
{
    Fanatic,      // 狂热型
    Conservative, // 保守型
    Opportunist   // 投机型
}

/// <summary>将军状态</summary>
public enum GeneralStatus
{
    Healthy,   // 健康 (>70)
    Wounded,   // 受伤 (50-70)
    Critical,  // 濒死 (30-50)
    Routed     // 溃败 (<30 或 兵力<20)
}

/// <summary>战线位置</summary>
public enum FrontlinePosition
{
    Left,   // 左翼
    Center, // 中军
    Right   // 右翼
}

/// <summary>期货方向</summary>
public enum FuturesDirection
{
    Long,  // 做多
    Short  // 做空
}

/// <summary>回合阶段</summary>
public enum TurnPhase
{
    TurnStart,      // 回合开始
    PlayerAction,   // 玩家行动
    TurnEnd,        // 回合结算
    BattleResolve,  // 战斗结算
    MarketUpdate    // 市场更新
}
```

### 2.2 事件定义

**文件**: `Data/Enums/GameEvents.cs`

```csharp
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

    // 游戏事件
    OnRandomEvent,
    OnVictoryConditionMet,
    OnDefeatConditionMet,
    OnGameEnd
}
```

### 2.3 配置路径常量

**文件**: `Data/Configs/ConfigPaths.cs`

```csharp
public static class ConfigPaths
{
    public const string GAME_BALANCE = "Config/WarBroker/GameBalanceConfig";
    public const string ORDER_CONFIG = "Config/WarBroker/OrderConfig";
    public const string SKILL_CONFIG = "Config/WarBroker/SkillConfig";
    public const string GENERAL_CONFIG = "Config/WarBroker/GeneralConfig";
    public const string CAMPAIGN_PREFIX = "Config/WarBroker/Campaigns/";
}
```

### 2.4 GameBalanceConfig

**文件**: `Data/Configs/GameBalanceConfig.cs`

全局游戏平衡参数，ScriptableObject。包含：

| 分类 | 参数 | 默认值 | 说明 |
|------|------|--------|------|
| 市场 | CommissionRate | 0.02 | 手续费率 |
| 市场 | PriceImpactRate | 0.02 | 每张价格冲击 |
| 市场 | StorageCostPerUnit | 3.0 | 仓储费/张/回合 |
| 市场 | PriceRandomRange | 0.05 | 价格波动范围 |
| 银行 | BankInterestRate | 0.05 | 每回合利率 |
| 银行 | LoanRatio | 1.5 | 借款倍数 |
| 期货 | FuturesMarginRate | 0.2 | 保证金率 |
| 期货 | ForceLiquidationRate | 0.8 | 强平线 |
| 期货 | MaxFuturesDuration | 3 | 最长期限 |
| 战斗 | RandomModifierMin/Max | 0.75/1.25 | 随机修正 |
| 战斗 | CritChance/Multiplier | 0.05/1.5 | 暴击 |
| 战斗 | FumbleChance/Multiplier | 0.05/0.5 | 失误 |
| 将军 | BaseReinforcement | 10 | 每回合补员 |
| 将军 | RoutTroopThreshold | 20 | 溃败兵力阈值 |
| 将军 | RoutScoreThreshold | 30 | 溃败综合评分阈值 |
| 将军 | ReorganizeTurns | 3 | 重整回合 |
| 将军 | DisobeyTrustThreshold | 50 | 抗命信任阈值 |
| 战线 | FlankSupportBonus | 0.1 | 侧翼支援加成 |
| 战线 | FlankThreatPenalty | 0.1 | 侧翼威胁惩罚 |
| 战线 | SurroundBonus | 0.25 | 半包围加成 |
| 审计 | AuditSupplyShortage | 30 | 补给不足审计值 |
| 审计 | AuditGeneralRouted | 20 | 将军溃败审计值 |
| 审计 | AuditFailureThreshold | 100 | 审计失败阈值 |
| 事件 | RandomEventChance | 0.4 | 随机事件概率 |

```csharp
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "WarBroker/GameBalanceConfig")]
public class GameBalanceConfig : ScriptableObject
{
    // ... 所有字段见上表
}
```

### 2.5 OrderConfig

**文件**: `Data/Configs/OrderConfig.cs`

```csharp
[Serializable]
public class OrderConfigItem
{
    public OrderType OrderType;
    public float BasePrice;
    public int ProductionPerTurn;
    public int InitialStock;
}

[CreateAssetMenu(fileName = "OrderConfig", menuName = "WarBroker/OrderConfig")]
public class OrderConfig : ScriptableObject
{
    public OrderConfigItem[] Orders = new OrderConfigItem[]
    {
        new() { OrderType = OrderType.ATK, BasePrice = 40f, ProductionPerTurn = 3, InitialStock = 10 },
        new() { OrderType = OrderType.DEF, BasePrice = 35f, ProductionPerTurn = 3, InitialStock = 10 },
        new() { OrderType = OrderType.RET, BasePrice = 25f, ProductionPerTurn = 2, InitialStock = 8 }
    };

    public OrderConfigItem GetConfig(OrderType type) { ... }
}
```

### 2.6 SkillConfig

**文件**: `Data/Configs/SkillConfig.cs`

```csharp
[Serializable]
public class SkillConfigItem
{
    public string SkillId;
    public string SkillName;
    public string Description;
    public GeneralPersonality Personality;

    // 触发条件
    public OrderType? TriggerOrder;
    public int TroopThreshold;
    public int MoraleThreshold;
    public int FrontlineThreshold;
    public bool RequireConsecutive;

    // 效果数值
    public int BonusLineMovement;
    public float CombatBonus;
    public int AllyTroopChange;
    public int EnemyTroopChange;
    public OrderType? DisobeyToOrder;
    public float DisobeyChance;
}

[CreateAssetMenu(fileName = "SkillConfig", menuName = "WarBroker/SkillConfig")]
public class SkillConfig : ScriptableObject
{
    public SkillConfigItem[] Skills;
    public SkillConfigItem GetSkill(string skillId) { ... }
    public SkillConfigItem[] GetSkillsByPersonality(GeneralPersonality personality) { ... }
}
```

**预设技能（12个）**：

| 性格 | 技能ID | 技能名 | 效果 |
|------|--------|--------|------|
| 狂热 | fanatic_charge | 先登 | ATK获胜时战线额外+1 |
| 狂热 | fanatic_bloodlust | 嗜血 | 连续ATK战斗力+20% |
| 狂热 | fanatic_laststand | 死战 | 兵力<30时ATK+50% |
| 狂热 | fanatic_defiant | 不退 | RET时30%抗命改DEF |
| 保守 | conservative_ironwall | 铁壁 | DEF成功敌方-5兵力 |
| 保守 | conservative_patience | 以逸待劳 | 连续DEF+50% |
| 保守 | conservative_rearguard | 断后 | RET时己方+5兵力 |
| 保守 | conservative_timid | 怯战 | 兵力<50时ATK可能抗命 |
| 投机 | opportunist_momentum | 顺风浪 | 战线>=4时ATK+30% |
| 投机 | opportunist_turtle | 逆风苟 | 战线<=2时DEF+30% |
| 投机 | opportunist_feint | 诈败 | RET时敌方-10兵力 |
| 投机 | opportunist_adapt | 见机行事 | 所有指令+10% |

### 2.7 GeneralConfig

**文件**: `Data/Configs/GeneralConfig.cs`

```csharp
[Serializable]
public class GeneralConfigItem
{
    public string GeneralId;
    public string Name;
    public string Biography;
    public Sprite Portrait;
    public GeneralPersonality Personality;
    public int InitialTroops, InitialTrust, InitialMorale;
    public string[] SkillIds;
    public float AtkBidModifier, DefBidModifier, RetBidModifier;
}

[CreateAssetMenu(fileName = "GeneralConfig", menuName = "WarBroker/GeneralConfig")]
public class GeneralConfig : ScriptableObject
{
    public GeneralConfigItem[] AllyGenerals;
    public GeneralConfigItem[] EnemyGenerals;
    public GeneralConfigItem GetGeneral(string generalId) { ... }
}
```

**预设将军（6名）**：

| 阵营 | 位置 | 名称 | 性格 | 技能 |
|------|------|------|------|------|
| 己方 | 左翼 | 冯·布吕歇尔 | 狂热 | 先登, 嗜血 |
| 己方 | 中军 | 库图佐夫 | 保守 | 铁壁, 断后 |
| 己方 | 右翼 | 塔列朗 | 投机 | 顺风浪, 见机行事 |
| 敌方 | 左翼 | 拿破仑 | 投机 | 顺风浪, 诈败 |
| 敌方 | 中军 | 威灵顿 | 保守 | 铁壁, 以逸待劳 |
| 敌方 | 右翼 | 内伊 | 狂热 | 死战, 不退 |

### 2.8 CampaignConfig

**文件**: `Data/Configs/CampaignConfig.cs`

```csharp
[CreateAssetMenu(fileName = "Campaign_XXX", menuName = "WarBroker/CampaignConfig")]
public class CampaignConfig : ScriptableObject
{
    public string CampaignId, CampaignName, Description;
    public int MaxTurns = 20;
    public float InitialCash = 500f;
    public int InitialAtkInventory = 2, InitialDefInventory = 2, InitialRetInventory = 2;
    public int InitialFrontlinePosition = 3;
    public GeneralConfig GeneralConfig;
    public FrontlineAssignment[] AllyFrontlineAssignments, EnemyFrontlineAssignments;
    public float VictorInitialCash = 500f;
    public float VictorDifficulty = 0.5f;
    public RandomEventConfig[] AvailableEvents;
}

[Serializable]
public class FrontlineAssignment
{
    public FrontlinePosition Position;
    public string GeneralId;
}

[Serializable]
public class RandomEventConfig
{
    public string EventId, EventName, Description;
    public float ProductionModifier;
    public float AtkDemandModifier, DefDemandModifier, RetDemandModifier;
    public int AllTroopChange, RandomTrustChange;
    public int Duration = 1;
}
```

---

## 3. 阶段 2：运行时数据

### 3.1 MarketData

**文件**: `Data/Models/MarketData.cs`

```csharp
[Serializable]
public class MarketData
{
    public Dictionary<OrderType, float> CurrentPrices;
    public Dictionary<OrderType, int> MarketInventory;
    public List<Dictionary<OrderType, float>> PriceHistory;

    public void InitFromConfig(OrderConfig config) { ... }
}

[Serializable]
public class FuturesContract
{
    public int ContractId;
    public OrderType TargetOrder;
    public FuturesDirection Direction;
    public float OpenPrice;
    public int Quantity;
    public int ExpirationTurn;
    public float Margin;

    public float CalculatePnL(float currentPrice) { ... }
}

[Serializable]
public class PlayerData
{
    public float Cash;
    public Dictionary<OrderType, int> Inventory;
    public float BankDebt;
    public List<FuturesContract> FuturesPositions;
    public int AuditValue;

    public void InitFromConfig(CampaignConfig config) { ... }
    public float CalculateNetWorth(MarketData market) { ... }
}
```

**净资产计算**：`Cash + 库存市值 + 期货PnL - 负债`

### 3.2 BattleData

**文件**: `Data/Models/BattleData.cs`

```csharp
[Serializable]
public class FrontlineData
{
    public FrontlinePosition Position;
    public int LinePosition; // 1-5
    public int StagnantTurns;
}

[Serializable]
public class BattleResult
{
    public FrontlinePosition Position;
    public OrderType AllyOrder, EnemyOrder;
    public int LineMovement;
    public int AllyTroopChange, EnemyTroopChange;
    public bool SkillTriggered;
    public string SkillName, Description;
    public bool WasCrit, WasFumble;
}

[Serializable]
public class BattleData
{
    public Dictionary<FrontlinePosition, FrontlineData> Frontlines;
    public List<GeneralData> AllyGenerals;
    public List<GeneralData> EnemyGenerals;

    public void InitFromConfig(CampaignConfig campaignConfig, SkillConfig skillConfig) { ... }
}
```

### 3.3 GeneralData

**文件**: `Data/Models/GeneralData.cs`

```csharp
[Serializable]
public class GeneralData
{
    public GeneralConfigItem Config { get; private set; }
    public string GeneralId => Config.GeneralId;
    public string Name => Config.Name;
    public GeneralPersonality Personality => Config.Personality;

    public FrontlinePosition Position;
    public int Troops, Trust, Morale;
    public List<SkillConfigItem> Skills;
    public OrderType? AssignedOrder;
    public int ReorganizeTurns;
    public OrderType? LastOrder;
    public int ConsecutiveOrderCount;

    public float CalculateCompositeScore()
        => Troops * 0.4f + Trust * 0.3f + Morale * 0.3f;

    public GeneralStatus GetStatus(GameBalanceConfig balance) { ... }

    public float CalculateBid(OrderType orderType, float marketPrice, GameBalanceConfig balance) { ... }
}
```

**出价公式**: `基础值 × 性格修正 × 状态修正 × 信任修正`

### 3.4 CampaignData

**文件**: `Data/Models/CampaignData.cs`

```csharp
[Serializable]
public class TransactionRecord
{
    public enum TransactionType { Buy, Sell, FuturesOpen, FuturesClose, Borrow, Repay }
    public TransactionType Type;
    public OrderType? OrderType;
    public int Quantity;
    public float Price, TotalAmount;
    public string Description;
}

[Serializable]
public class TurnRecord
{
    public int TurnNumber;
    public Dictionary<string, OrderType> OrderAssignments;
    public List<TransactionRecord> Transactions;
    public List<BattleResult> BattleResults;
    public Dictionary<OrderType, float> PriceSnapshot;
    public float PlayerNetWorth;
}

[Serializable]
public class CampaignRuntimeData
{
    public CampaignConfig Config { get; private set; }
    public int CurrentTurn;
    public int MaxTurns => Config.MaxTurns;
    public TurnPhase CurrentPhase;
    public PlayerData Player;
    public MarketData Market;
    public BattleData Battle;
    public float VictorCash;
    public Dictionary<OrderType, int> VictorInventory;
    public List<TurnRecord> TurnHistory;
    public RandomEventConfig ActiveEvent;
    public int EventRemainingTurns;

    public void InitFromConfig(CampaignConfig campaignConfig, OrderConfig orderConfig, SkillConfig skillConfig) { ... }
}
```

---

## 4. 阶段 3：核心系统

所有系统实现 `ILogic` 接口（`OnInit`, `OnEnterState`, `OnUpdate`, `UnInit`），在 `GameRoot` 中统一初始化。

### 4.1 MarketSystem

**文件**: `Systems/MarketSystem.cs`

职责：价格计算、交易执行、期货结算、银行借贷

```csharp
public class MarketSystem : ILogic
{
    // === 现货交易 ===
    public bool BuyOrder(OrderType orderType, int quantity, out float totalCost);
    public bool SellOrder(OrderType orderType, int quantity, out float totalRevenue);

    // === 期货交易 ===
    public bool OpenFutures(OrderType, FuturesDirection, int qty, int turns, out FuturesContract);
    public bool CloseFutures(int contractId, out float pnl);
    public void CheckForceLiquidation();
    public void SettleExpiredFutures();

    // === 银行借贷 ===
    public float CalculateLoanLimit();
    public bool Borrow(float amount);
    public bool Repay(float amount);
    public void ApplyInterest();

    // === 价格更新 ===
    public void ApplyStorageCost();
    public void UpdatePrices(Dictionary<OrderType, float> demandModifiers);

    // === 市场情报 ===
    public List<MarketIntelItem> GetMarketIntelligence(OrderType orderType);
}
```

**关键机制**：
- 买入价格冲击：每张 `+PriceImpactRate`
- 卖出价格冲击：每张 `-PriceImpactRate`
- 强平条件：`剩余保证金 < 初始保证金 × (1 - ForceLiquidationRate)`

### 4.2 BattleSystem

**文件**: `Systems/BattleSystem.cs`

职责：战线管理、战斗结算、将军状态、技能检查

```csharp
public class BattleSystem : ILogic
{
    // === 战斗结算 ===
    public List<BattleResult> ResolveBattles(Dictionary<string, OrderType> enemyOrders);

    // === 补员 ===
    public void ApplyReinforcements();

    // === 胜负检查 ===
    public bool CheckVictory();   // 所有战线 == 5
    public bool CheckDefeat();    // 所有战线 == 1
    public bool CheckNegotiation(); // 所有战线停滞 >= 3 回合
}
```

**战斗矩阵**:

| 己方\敌方 | ATK | DEF | RET |
|-----------|-----|-----|-----|
| **ATK** | 0 / -15 / -15 | 0 / -10 / -10 | **+1** / 0 / 0 |
| **DEF** | 0 / -10 / -10 | 0 / 0 / 0 | 0 / 0 / 0 |
| **RET** | **-1** / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 |

> 格式: 战线移动 / 己方兵力变化 / 敌方兵力变化（实际值受战斗力比修正）

**战斗力计算**: `100 × 兵力修正 × 士气修正 × 侧翼修正 × 随机修正`

### 4.3 CampaignSystem

**文件**: `Systems/CampaignSystem.cs`

职责：回合流程、维克多AI、随机事件、历史记录、胜负判定

```csharp
public class CampaignSystem : ILogic
{
    public CampaignRuntimeData Data { get; private set; }

    public void InitNewCampaign(string campaignId, MarketSystem market, BattleSystem battle);
    public void StartTurn();
    public void EndTurn();
}
```

**回合流程**:

```
StartTurn()
  ├── TryTriggerRandomEvent()
  ├── 清空将军指令
  └── 进入 PlayerAction 阶段

(玩家操作: 交易、分配指令)

EndTurn()
  ├── 验证指令完整
  ├── RecordTurn() 记录历史
  ├── ExecuteTurnSettlement()
  │   ├── ExecuteVictorAI()       ← 维克多选择指令
  │   ├── BattleSystem.ResolveBattles()
  │   ├── BattleSystem.ApplyReinforcements()
  │   ├── MarketSystem.ApplyInterest()
  │   ├── MarketSystem.ApplyStorageCost()
  │   ├── MarketSystem.SettleExpiredFutures()
  │   ├── MarketSystem.CheckForceLiquidation()
  │   ├── CalculateDemandModifiers()
  │   └── MarketSystem.UpdatePrices()
  ├── CheckGameEnd()
  │   ├── 破产检查 (净资产 < 0)
  │   ├── 审计检查 (>= 100)
  │   ├── 胜利检查 (所有战线 == 5)
  │   ├── 失败检查 (所有战线 == 1)
  │   ├── 停战检查 (所有战线停滞 >= 3)
  │   └── 回合耗尽检查
  └── StartTurn() 下一回合
```

**维克多AI 决策逻辑**:
- 低难度 (随机): 按性格选择默认指令
- 高难度 (策略): 根据战线位置选择最优指令

---

## 5. 阶段 4：场景与游戏模式

### 5.1 BattleGameMode

**文件**: `GameModes/BattleGameMode.cs`

```csharp
public class BattleGameMode : GameModeBase
{
    public BattleGameMode() : base(GameMode.GamePlay) { }

    public override void EnterGameMode()
    {
        // 1. 实例化 GameplayManager prefab
        var prefab = resService.LoadResource<GameObject>("Prefabs/WarBroker/GameplayManager");
        var obj = GameObject.Instantiate(prefab);
        var manager = obj.GetComponent<GameplayManager>();
        managerService.RegisterManager(manager);

        // 2. 注册 7 个 UI 窗口
        RegisterWindow<GameplayWindow>("Prefabs/WarBroker/UI/GameplayWindow", "GameplayWindow");
        RegisterWindow<MarketPanel>("Prefabs/WarBroker/UI/MarketPanel", "MarketPanel");
        RegisterWindow<BattlefieldPanel>("Prefabs/WarBroker/UI/BattlefieldPanel", "BattlefieldPanel");
        RegisterWindow<GeneralPanel>("Prefabs/WarBroker/UI/GeneralPanel", "GeneralPanel");
        RegisterWindow<IntelPanel>("Prefabs/WarBroker/UI/IntelPanel", "IntelPanel");
        RegisterWindow<HistoryPanel>("Prefabs/WarBroker/UI/HistoryPanel", "HistoryPanel");
        RegisterWindow<GameEndWindow>("Prefabs/WarBroker/UI/GameEndWindow", "GameEndWindow");
    }

    private void RegisterWindow<T>(string prefabPath, string windowName) where T : WindowBase, new()
    {
        var prefab = resService.LoadResource<GameObject>(prefabPath);
        var obj = GameObject.Instantiate(prefab);
        var window = new T();
        window.gameObject = obj;
        window.transform = obj.transform;
        window.Name = windowName;
        uIService.RegisterWindow(windowName, window);
        // RegisterWindow 内部会自动调用 window.OnAwake()
    }
}
```

> **注意**: `WindowBase` 不是 MonoBehaviour，而是纯 C# 类。窗口通过 `new T()` 创建，手动绑定 GameObject。

### 5.2 GameplayManager

**文件**: `Managers/GameplayManager.cs`

```csharp
public class GameplayManager : ManagerBase
{
    private MarketSystem marketSystem;
    private BattleSystem battleSystem;
    private CampaignSystem campaignSystem;

    public string CampaignId = "Campaign_Tutorial";

    public override void OnShow()
    {
        campaignSystem.InitNewCampaign(CampaignId, marketSystem, battleSystem);
        campaignSystem.StartTurn();
        RegisterEvents();
        uiService.ShowWindow<GameplayWindow>("GameplayWindow");
        uiService.ShowWindow<GeneralPanel>("GeneralPanel");
    }

    // === 玩家操作接口 ===
    public bool BuyOrder(OrderType type, int quantity);
    public bool SellOrder(OrderType type, int quantity);
    public bool OpenFutures(OrderType type, FuturesDirection dir, int qty, int turns);
    public bool CloseFutures(int contractId);
    public bool Borrow(float amount);
    public bool Repay(float amount);
    public bool AssignOrder(string generalId, OrderType order);
    public void EndTurn();

    // === 数据访问接口 ===
    public CampaignRuntimeData GetCampaignData();
    public PlayerData GetPlayerData();
    public MarketData GetMarketData();
    public BattleData GetBattleData();
}
```

**指令分配流程** (`AssignOrder`):
1. 检查库存是否足够
2. 计算将军出价 (bid)
3. 正出价 → 将军付钱 (Cash += bid)；负出价 → 需额外支付
4. 扣减库存
5. 设置 `general.AssignedOrder`
6. 根据出价正负调整信任度
7. 发送 `OnOrderAssigned` 事件

---

## 6. 阶段 5：UI 系统

### 6.1 Binder 模式

由于 `WindowBase` 不是 MonoBehaviour，无法直接在 Prefab 上序列化字段。采用 Binder 模式：

```
Prefab 上挂载: XxxBinder : UIBinder (MonoBehaviour)
                ↓ 持有序列化的 UI 引用
运行时创建:    XxxWindow : WindowBase (纯 C# 类)
                ↓ 在 OnAwake() 中读取 Binder
                GetComponent<XxxBinder>() → 绑定字段
```

**文件**: `UI/Binders/UIBinder.cs`

```csharp
public class UIBinder : MonoBehaviour { }
```

每个 Binder 独立文件（Unity 要求文件名 == 类名）：
- `GameplayWindowBinder.cs`
- `MarketPanelBinder.cs`
- `BattlefieldPanelBinder.cs`
- `GeneralPanelBinder.cs`
- `IntelPanelBinder.cs`
- `HistoryPanelBinder.cs`
- `GameEndWindowBinder.cs`

### 6.2 GameplayWindow

**文件**: `UI/GameplayWindow.cs`

主游戏界面，包含：
- 顶部状态栏：回合数、阶段、现金、净资产、审计值
- Tab 切换：市场/战场/将军/情报/历史
- 底部操作栏：结束回合按钮、事件提示

```csharp
public class GameplayWindow : WindowBase
{
    public override void OnAwake()
    {
        var b = gameObject.GetComponent<GameplayWindowBinder>();
        // ... 读取 UI 引用
    }

    public override void OnShow()
    {
        // 绑定 Tab 按钮 → SwitchPanel()
        // 绑定结束回合按钮 → gameplayManager.EndTurn()
        // 监听 OnTurnStart, OnTurnEnd, OnRandomEvent
    }

    private void SwitchPanel(string panelName)
    {
        // 隐藏所有子面板，显示指定面板
        foreach (var name in PanelNames) uIService.HideWindow(name);
        uIService.ShowWindow<WindowBase>(panelName);
    }
}
```

### 6.3 MarketPanel

**文件**: `UI/MarketPanel.cs`

市场交易面板：
- 现货交易（ATK/DEF/RET 买卖）
- 期货操作（开仓/平仓）
- 银行借贷（借入/还款）

监听事件：`OnTradeExecuted`, `OnTurnEnd`

### 6.4 BattlefieldPanel

**文件**: `UI/BattlefieldPanel.cs`

战场可视化面板：
- 3 条战线 Slider (1-5)
- 每条战线显示己方/敌方将军信息
- 活跃事件显示

监听事件：`OnBattleResult`, `OnTurnStart`, `OnTurnEnd`

### 6.5 GeneralPanel

**文件**: `UI/GeneralPanel.cs`

将军管理面板：
- 3 张己方将军卡片
- 每张卡片：名称、性格、兵力/信任/士气 Slider、状态
- ATK/DEF/RET 指令分配按钮（高亮显示已分配指令）
- 技能列表

监听事件：`OnOrderAssigned`, `OnTurnStart`, `OnTurnEnd`

### 6.6 IntelPanel

**文件**: `UI/IntelPanel.cs`

情报面板：市场趋势、战线态势、敌方将军信息

### 6.7 HistoryPanel

**文件**: `UI/HistoryPanel.cs`

历史面板：可滚动的回合历史记录（净资产、指令、价格快照）

### 6.8 GameEndWindow

**文件**: `UI/GameEndWindow.cs`

游戏结束弹窗（Top 层）：
- 胜利/失败标题
- 统计信息（回合数、净资产、现金、审计值）
- 重新开始 / 返回主菜单按钮

监听事件：`OnGameEnd`

---

## 7. 调试工具

### 7.1 WarBrokerConfigSetup (Editor)

**文件**: `Debug/Editor/WarBrokerConfigSetup.cs`

菜单 `WarBroker > Setup All Configs`，一键创建并填充所有 ScriptableObject 配置：
- SkillConfig (12 个技能)
- GeneralConfig (6 名将军)
- GameBalanceConfig (默认值)
- OrderConfig (3 种指令)
- CampaignConfig (教程战役：滑铁卢)

### 7.2 WarBrokerDebugWindow (Editor)

**文件**: `Debug/Editor/WarBrokerDebugWindow.cs`

菜单 `WarBroker > Debug Window`，EditorWindow 显示：
- 战役信息（回合/阶段）
- 玩家状态（现金/负债/净资产/审计/库存）
- 市场价格
- 战线位置
- 双方将军属性

### 7.3 WarBrokerDebugConsole (Runtime)

**文件**: `Debug/WarBrokerDebugConsole.cs`

运行时 IMGUI 调试面板（F12 切换）：
- **Market Tab**: 价格、库存、期货合约
- **Battle Tab**: 战线、双方将军详细状态
- **Campaign Tab**: 战役进度、历史摘要
- **Actions Tab**: 快捷操作（强制结束回合、设置现金/库存、满状态、推进战线）
- **Log Tab**: 实时事件日志

### 7.4 WarBrokerUIPrefabGenerator (Editor)

**文件**: `Debug/Editor/WarBrokerUIPrefabGenerator.cs`

菜单 `WarBroker > Generate All Prefabs`，程序化生成所有 UI Prefab：
- GameplayManager Prefab
- 7 个 UI 窗口 Prefab（含完整 UI 层级结构和 Binder 组件）

---

## 8. 文件清单

```
Assets/Scripts/WarBroker/
├── Data/
│   ├── Enums/
│   │   ├── GameEnums.cs          ← 6 个枚举定义
│   │   └── GameEvents.cs         ← WarBrokerEventID 事件枚举
│   ├── Configs/
│   │   ├── ConfigPaths.cs        ← 配置路径常量
│   │   ├── GameBalanceConfig.cs  ← 全局平衡参数 SO
│   │   ├── OrderConfig.cs        ← 指令配置 SO
│   │   ├── SkillConfig.cs        ← 技能配置 SO
│   │   ├── GeneralConfig.cs      ← 将军配置 SO
│   │   └── CampaignConfig.cs     ← 战役配置 SO
│   └── Models/
│       ├── CampaignData.cs       ← 战役/回合运行时数据
│       ├── MarketData.cs         ← 市场/玩家/期货运行时数据
│       ├── BattleData.cs         ← 战场/战线/战斗结果数据
│       └── GeneralData.cs        ← 将军运行时数据
├── Systems/
│   ├── MarketSystem.cs           ← 市场交易系统
│   ├── BattleSystem.cs           ← 战斗结算系统
│   └── CampaignSystem.cs         ← 战役流程系统
├── Managers/
│   └── GameplayManager.cs        ← 游戏流程管理器
├── GameModes/
│   └── BattleGameMode.cs         ← 战斗场景 GameMode
├── UI/
│   ├── GameplayWindow.cs         ← 主界面
│   ├── MarketPanel.cs            ← 市场面板
│   ├── BattlefieldPanel.cs       ← 战场面板
│   ├── GeneralPanel.cs           ← 将军面板
│   ├── IntelPanel.cs             ← 情报面板
│   ├── HistoryPanel.cs           ← 历史面板
│   ├── GameEndWindow.cs          ← 游戏结束弹窗
│   └── Binders/
│       ├── UIBinder.cs           ← Binder 基类
│       ├── GameplayWindowBinder.cs
│       ├── MarketPanelBinder.cs
│       ├── BattlefieldPanelBinder.cs
│       ├── GeneralPanelBinder.cs
│       ├── IntelPanelBinder.cs
│       ├── HistoryPanelBinder.cs
│       └── GameEndWindowBinder.cs
└── Debug/
    ├── WarBrokerDebugConsole.cs  ← 运行时调试面板
    └── Editor/
        ├── WarBrokerConfigSetup.cs       ← 配置一键填充
        ├── WarBrokerDebugWindow.cs       ← Editor 调试窗口
        └── WarBrokerUIPrefabGenerator.cs ← UI Prefab 生成器
```

**总计：36 个 C# 文件**
