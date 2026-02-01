# WarBroker 业务逻辑架构文档

## 项目概述

WarBroker 是一个战争模拟与股票交易混合游戏。玩家通过买卖"指令"（ATK/DEF/RET）影响战场，同时进行期货投机和委托任务。

---

## 一、核心框架

### 1.1 GameRoot (全局管理器)

**路径**: `Assets/Scripts/Core/GameCommand/GameRoot.cs`

单例模式的服务定位器，管理所有服务和系统：

```
GameRoot
├── 服务 (Services)
│   ├── UIService      - UI窗口管理
│   ├── ResService     - 资源加载
│   ├── InputService   - 输入处理
│   ├── EventService   - 事件系统
│   └── DataService    - 数据持久化
│
└── 系统 (Systems)
    ├── MarketSystem   - 市场交易
    ├── BattleSystem   - 战斗结算
    └── CampaignSystem - 战役流程
```

### 1.2 事件系统

**路径**: `Assets/Scripts/Core/GameService/EventService/`

关键事件 ID (`WarBrokerEventID`):
- 回合: `OnTurnStart`, `OnPhaseChange`, `OnTurnEnd`
- 市场: `OnPriceUpdate`, `OnTradeExecuted`, `OnForceLiquidation`
- 战斗: `OnBattleResult`, `OnBattleAnimationsComplete`, `OnGeneralRouted`
- 玩家: `OnCashChange`, `OnIntentChanged`, `OnOrderAssigned`

---

## 二、核心系统

### 2.1 战役系统 (CampaignSystem)

**路径**: `Assets/Scripts/WarBroker/Systems/CampaignSystem.cs`

**五阶段回合流程**:

```
阶段 I: 周报与开盘 (TurnStart)
  └─ 费用结算 → 随机事件 → 意图生成 → 市场开盘

阶段 II: 玩家操盘 (MarketPhase)
  └─ 现货交易 → 期货操作 → 意图干涉 → 指令分配

阶段 III: 维克多行动 (IntentPhase)
  └─ AI决策 → 军事采购 → 投机操作

阶段 IV: 战斗推演 (BattlePhase)
  └─ 抗命检查 → 战术揭示 → 伤害计算 → 战线移动

阶段 V: 结算 (SettlementPhase)
  └─ 期货结算 → 审计检查 → 委托检查 → 胜负判定
```

### 2.2 市场系统 (MarketSystem)

**路径**: `Assets/Scripts/WarBroker/Systems/MarketSystem.cs`

**核心功能**:
- `BuyOrder(type, quantity)` - 买入指令
- `SellOrder(type, quantity)` - 卖出指令
- `OpenFutures(type, direction, quantity)` - 开期货仓
- `CloseFutures(contractId)` - 平仓
- `Borrow(amount)` / `Repay(amount)` - 银行借贷

**三因子定价模型**:
```
Price = P_base × (1 + Alpha) × Beta × Gamma

Alpha: 战场态势因子 (-0.25 ~ +0.25)
Beta:  交易冲击因子 (买入↑ 卖出↓)
Gamma: 流通盘因子 (InitialFloat / CurrentFloat)
```

### 2.3 战斗系统 (BattleSystem)

**路径**: `Assets/Scripts/WarBroker/Systems/BattleSystem.cs`

**战斗流程**:
1. 抗命检查 (信任度 < 50 时有概率)
2. 接触/脱离判断
3. 伤害计算 (基础伤害 × 随机修正 × 暴击/失误)
4. 战线移动 (ATK前进, DEF驻守, RET后撤)
5. 技能触发

**战线位置**: Grid 1(己方基地) ↔ Grid 5(敌方基地)

### 2.4 意图系统 (IntentSystem)

**路径**: `Assets/Scripts/WarBroker/Systems/IntentSystem.cs`

- **默认意图**: 根据性格+HP+位置生成 (灰色气泡)
- **强化**: 消耗1份同类指令, 信任+5 (金色气泡)
- **篡改**: 消耗3份目标指令, 信任-15 (红色气泡)

### 2.5 委托系统 (CommissionSystem)

**路径**: `Assets/Scripts/WarBroker/Systems/CommissionSystem.cs`

委托类型:
- `OccupyGrid` - 占领指定Grid
- `EnemyReachGrid` - 敌方到达指定Grid
- `NotOccupyGrid` - 未占领指定Grid
- `TotalCasualties` - 总伤亡达标

---

## 三、数据模型

### 3.1 核心数据结构

**路径**: `Assets/Scripts/WarBroker/Data/Models/`

```csharp
PlayerData {
    Cash, Inventory, BankDebt, FuturesPositions, AuditValue
}

MarketData {
    CurrentPrices, MarketInventory, BetaCarry, KLineHistory
}

BattleData {
    Frontlines, AllyGenerals, EnemyGenerals, CurrentReserves
}

GeneralData {
    GridPosition, Troops, Trust, Morale,
    AssignedOrder, DefaultIntent, FinalIntent
}
```

### 3.2 枚举定义

**路径**: `Assets/Scripts/WarBroker/Data/Enums/GameEnums.cs`

```csharp
OrderType { ATK, DEF, RET }
TurnPhase { TurnStart, MarketPhase, IntentPhase, BattlePhase, Settlement }
FrontlinePosition { Left, Center, Right }
GeneralStatus { FullStrength, Healthy, Wounded, Critical, Routed }
```

---

## 四、配置系统

**路径**: `Assets/Resources/Config/WarBroker/`

| 配置文件 | 用途 |
|---------|------|
| `GameBalanceConfig.asset` | 全局平衡参数 |
| `OrderConfig.asset` | 指令基础价格/产量 |
| `GeneralConfig.asset` | 将军属性/技能 |
| `Campaign_*.asset` | 战役配置 |
| `Commission_*.asset` | 委托任务配置 |

---

## 五、UI架构

### 5.1 UI服务

**路径**: `Assets/Scripts/Core/GameService/UIService/UIService.cs`

```csharp
ShowWindow<T>(name)           // 显示窗口
ShowWindowWithAnimation<T>()  // 带动画显示
HideWindow(name)              // 隐藏窗口
```

### 5.2 UI层级

```csharp
UILayer { Background, Normal, Popup, Top }
```

### 5.3 主要UI组件

**路径**: `Assets/Scripts/WarBroker/UI/`

```
Windows/
├── GameplayWindow      - 主游戏界面
└── MarketWindow        - 市场界面

Panels/
├── ObjectivePanel      - 目标面板
├── GeneralDetailPanel  - 将军详情
├── TooltipPanel        - 提示面板
└── InfoPanel           - 信息面板

Tabs/
├── SpotMarketTab       - 现货交易
├── FuturesTab          - 期货交易
└── BankTab             - 银行操作

Popups/
├── CampaignEndPopup    - 战役结束
└── ConfirmPopup        - 确认弹窗
```

---

## 六、战场系统

**路径**: `Assets/Scripts/WarBroker/Battlefield/`

| 组件 | 职责 |
|------|------|
| `BattlefieldSceneController` | 战场场景管理、动画播放 |
| `BattlefieldCameraController` | Cinemachine相机控制 |
| `GeneralUnit3D` | 将军3D单位表现 |

---

## 七、数据流向

```
配置加载
    ↓
CampaignSystem.InitNewCampaign()
    ↓
┌─────────────────────────────────────┐
│           回合循环                   │
│  ┌─────────────────────────────┐   │
│  │ MarketSystem ←→ PlayerData  │   │
│  │      ↓                      │   │
│  │ BattleSystem → BattleData   │   │
│  │      ↓                      │   │
│  │ EventService → UI更新       │   │
│  └─────────────────────────────┘   │
└─────────────────────────────────────┘
    ↓
胜负判定 → 结算
```

---

## 八、关键API速查

### CampaignSystem
```csharp
InitNewCampaign(campaignId, market, battle)
StartTurn() / EndTurn()
EnterPlayerPhase() / EnterBattlePhase()
```

### MarketSystem
```csharp
BuyOrder(type, quantity) / SellOrder(type, quantity)
OpenFutures(type, direction, quantity) / CloseFutures(id)
Borrow(amount) / Repay(amount)
```

### BattleSystem
```csharp
ResolveBattles(enemyOrders) → List<BattleResult>
AssignOrder(general, order)
GetGeneralStatus(general) → GeneralStatus
```

### EventService
```csharp
AddEventListening(eventId, callback)
SendMessage(eventId, param1, param2)
RemoveEventListeningByTarget(target)
```
