# 炮火与K线 (Charta Bellum)

## Claude Code 开发指导文档 v2.0

**文档性质**：开发任务规划与实现指导  
**目标读者**：Claude Code / AI编程助手  
**配套文档**：系统设计策划案 v6.0  
**基于**：对现有代码库的全面分析 + GDD v6.0 差距评估  
**上一版**：DevGuide v1.0（基于GDD v5.0，已过时）

---

# 一、项目概述

## 1.1 项目定位

回合制金融投机策略游戏。玩家扮演军需掮客，通过操纵战局影响军需品价格，从价格波动中获利。

**核心幻想**：战场是K线图，士兵的生命是筹码。胜利的标准不是攻城略地，而是账户余额。

## 1.2 技术栈

- **引擎**：Unity (C#)
- **框架**：Levity Framework（自研通用框架）
- **插件**：Odin Inspector, DOTween, Naninovel (可选)
- **架构模式**：
  - 服务定位器 (GameRoot单例)
  - ScriptableObject配置驱动
  - 事件总线 (EventService)

## 1.3 Levity Framework 核心约束（重要！）

开发必须遵循框架已有组件，**禁止重复造轮子**：

### 输入系统
| 需求 | 框架组件 | 用法 |
|------|----------|------|
| 锁定/解锁输入通道 | `InputRouter` | `InputRouter.Acquire(channel, owner)` / `Release()` |
| 检查输入状态 | `InputService` | `inputService.InputEnabled` |
| 输入通道类型 | `InputChannel` | `Gameplay`, `UI`, `Naninovel` |

### UI系统
| 需求 | 框架组件 | 用法 |
|------|----------|------|
| 窗口基类 | `WindowBase` | 所有UI窗口必须继承 |
| 层级管理 | `UILayerManager` | `uIService.GetLayerRoot(UILayer.xxx)` |
| 窗口显示/隐藏 | `UIService` | `ShowWindow<T>()` / `HideWindow()` |
| 全屏遮挡 | `UIOcclusionManager` | 设置 `IsFullScreen = true` |
| 窗口动画 | `UIAnimator` | `PlayShowAnimation()` / `PlayHideAnimation()` |
| UI层级枚举 | `UILayer` | `Scene`, `Background`, `Normal`, `Info`, `Top`, `Tip` |

### 事件系统
| 需求 | 框架组件 | 用法 |
|------|----------|------|
| 监听事件 | `EventService` | `eventService.AddEventListening(id, handler)` |
| 发送事件 | `EventService` | `eventService.SendMessage(id, arg1, arg2)` |
| 清理监听 | `EventService` | `eventService.RemoveEventListeningByTarget(this)` |
| 事件ID定义 | `WarBrokerEventID` | 强转为 `EventID` 使用 |

### 开发模式约束
```csharp
// ✅ 正确：使用框架组件
public class MyPanel : WindowBase
{
    public override void OnShow()
    {
        base.OnShow();
        InputRouter.Acquire(InputChannel.Gameplay, this);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnXxx, OnXxx);
    }
    
    public override void OnHide()
    {
        base.OnHide();
        InputRouter.Release(InputChannel.Gameplay, this);
        eventService.RemoveEventListeningByTarget(this);
    }
}

// ❌ 错误：重复造轮子
public class InputLayerManager : MonoBehaviour { }     // 不要新建
public class PopupManager : MonoBehaviour { }          // 不要新建
public class MyWindow : MonoBehaviour { }              // 必须继承 WindowBase
```

## 1.4 代码规范

- 命名空间：无（框架未使用）
- 类命名：PascalCase
- 方法命名：PascalCase
- 字段命名：camelCase (私有), PascalCase (公有)
- 配置路径：`Assets/Resources/Config/WarBroker/`
- 脚本路径：`Assets/Scripts/WarBroker/`

---

# 二、当前代码库分析与GDD v6.0差距

## 2.1 已完成模块（可直接复用）

### 核心框架 (Levity Framework)
| 模块 | 状态 | 说明 |
|------|------|------|
| GameRoot | ✅ 完成 | 单例入口，服务注册 |
| EventService | ✅ 完成 | 事件总线 |
| ResService | ✅ 完成 | 资源加载 |
| UIService | ✅ 完成 | 窗口管理 |
| ManagerService | ✅ 完成 | 场景管理器 |

### 数据模型（已部分实现，需修改对齐v6.0）
| 模块 | 文件 | 现状 | 备注 |
|------|------|------|------|
| GeneralData | `Data/Models/GeneralData.cs` | ⚠️ | 字段名为`Troops`而非`HP`；状态判定使用CompositeScore复合评分而非GDD v6.0的纯HP阈值；有旧版`CalculateBid`/`AssignedOrder`冗余逻辑 |
| MarketData | `Data/Models/MarketData.cs` | ⚠️ | 字段名为`MarketInventory`而非`Float`；类型为`float`而非`int`；缺少K线数据结构 |
| PlayerData | `Data/Models/MarketData.cs` | ⚠️ | 缺少统一货币体系（局外Cash）；`AuditValue`概念已在v6.0中简化为入场费参照 |
| BattleData | `Data/Models/BattleData.cs` | ✅ | `CurrentReserves`已存在；`FrontlineData`含占领追踪 |
| CampaignRuntimeData | `Data/Models/CampaignData.cs` | ⚠️ | 缺少委托任务数据、K线历史数据 |

### 系统逻辑（已部分实现，需重构）
| 模块 | 文件 | 现状 | 备注 |
|------|------|------|------|
| PricingEngine | `Systems/PricingEngine.cs` | ⚠️ | 三因子框架存在但实现与v6.0不匹配：Alpha未按战线逐一计算交战中心和位置修正；Gamma使用线性公式而非v6.0的`InitialFloat/CurrentFloat`除法；Beta缺少交易冲击累积机制 |
| IntentSystem | `Systems/IntentSystem.cs` | ⚠️ | 基础意图生成+强化/篡改存在，但权重数值与v6.0不匹配（如狂热型ATK应为70而非0.6）；缺少濒死时的硬性规则（狂热者RET=0等） |
| MarketSystem | `Systems/MarketSystem.cs` | ⚠️ | 现货/期货/银行借贷框架存在，但交易冲击使用`PriceImpactRate`线性叠加而非v6.0的Beta因子累积；期货到期时间可变而非固定3回合 |
| BattleSystem | `Systems/BattleSystem.cs` | ⚠️ | 接触/脱离判定已正确实现(Gap=E-P-1)；对抗表部分实现但缺少完整9格映射；士气/联动修正不完整 |
| CampaignSystem | `Systems/CampaignSystem.cs` | ⚠️ | 回合流程存在但阶段划分与v6.0不完全对齐；胜利条件需要占领+保持验证 |

### 配置文件（需扩展）
| 配置 | 文件 | 现状 | 备注 |
|------|------|------|------|
| GameBalanceConfig | `Data/Configs/GameBalanceConfig.cs` | ⚠️ | 已有三因子参数但不完整（缺少交战位置修正表、恐慌效应完整参数）；含旧版审计系统参数需清理 |
| OrderConfig | `Data/Configs/OrderConfig.cs` | ⚠️ | `InitialStock`需改为初始流通盘50/50/50 |
| GeneralConfig | `Data/Configs/GeneralConfig.cs` | ⚠️ | 含旧版出价修正器(BidModifier)需清理 |
| CampaignConfig | `Data/Configs/CampaignConfig.cs` | ⚠️ | 缺少入场费/统一货币体系/委托任务配置 |

### UI界面（已部分实现）
| 界面 | 文件 | 现状 | 备注 |
|------|------|------|------|
| SpotMarketTab | `UI/SpotMarketTab.cs` | ⚠️ | 基础现货交易UI存在，需对接新流通盘字段 |
| FuturesMarketTab | `UI/FuturesMarketTab.cs` | ⚠️ | 期货UI存在，需对齐固定3回合 |
| GeneralDetailPanel | `UI/GeneralDetailPanel.cs` | ⚠️ | 将军详情面板存在，含意图显示和干涉按钮，需对齐v6.0 UI架构 |
| WarBrokerDebugConsole | `Debug/WarBrokerDebugConsole.cs` | ✅ | 调试控制台功能完善 |

## 2.2 GDD v6.0 新增/变更清单（与现有代码的差距）

### 🔴 高优先级（核心玩法变更）

| # | 变更项 | 当前实现 | GDD v6.0要求 | 影响范围 |
|---|--------|----------|-------------|----------|
| 1 | **Alpha因子重构** | 简单布尔判断(hasEngaged/hasCritical) | 逐战线计算交战中心+5级位置修正表+临界修正+多战线平均 | PricingEngine.cs |
| 2 | **Beta因子改为交易冲击累积** | 仅动量效应+恐慌效应，每回合重算 | Beta是回合内累积变量：每笔交易 `Beta_new = Beta_old × (1 + Impact)`，回合间携带 | PricingEngine.cs, MarketSystem.cs, MarketData.cs |
| 3 | **Gamma因子改为除法公式** | `Gamma = 1 + sensitivity × (1 - ratio)` 线性 | `Gamma = InitialFloat / Max(CurrentFloat, 1)` 非线性 | PricingEngine.cs |
| 4 | **将军状态判定改为纯HP阈值** | 复合评分(Troops×5×0.4 + Trust×0.3 + Morale×0.3) | 纯HP阈值：满编16-20/健康11-15/受伤6-10/濒死1-5/溃败0 | GeneralData.cs, GameBalanceConfig.cs |
| 5 | **将军意图权重数值对齐** | 权重为小数(0.6/0.3/0.1)，HP/Grid修正不完整 | 权重为整数(70/20/10)，完整HP×Grid修正表含硬性规则 | IntentSystem.cs |
| 6 | **统一货币体系** | 局内现金独立，有审计系统 | 入场费=起始资本，净资产全额带走，无目标净资产/审计 | PlayerData, CampaignConfig, CampaignSystem |
| 7 | **K线图数据系统** | 无 | 每回合记录Open/Close/High/Low，需实时追踪回合内价格极值 | 新建KLineData, 修改MarketData |
| 8 | **委托任务系统** | 无 | 4种委托(赢下战争/做空祖国/卖国求荣/绞肉机)+条件检测+奖金结算 | 新建CommissionSystem, CommissionConfig |

### 🟡 中优先级（系统完善）

| # | 变更项 | 当前实现 | GDD v6.0要求 | 影响范围 |
|---|--------|----------|-------------|----------|
| 9 | **指令对抗表完整化** | 部分实现 | 完整9格对抗表含HP变化+战线移动 | BattleSystem.cs |
| 10 | **战术系统（普通/精锐）** | 有旧版Skill系统 | 2级战术：普通(90%)/精锐(10%)，强化后权重修正 | BattleSystem.cs, 清理旧SkillConfig |
| 11 | **军工厂动态产能** | 固定产能`ProductionPerTurn` | `本周产能 = 上周总消耗 × 产能系数(0.9~1.1)`，保底3 | MarketSystem.cs, MarketData.cs |
| 12 | **士气系统完善** | 部分实现 | 完整士气变化表+战斗修正（造成/受到伤害修正+不战自溃） | BattleSystem.cs, GeneralData.cs |
| 13 | **政权崩溃清算** | 无 | Grid 1沦陷→ATK/DEF现货归$0→空单按$0结算 | MarketSystem.cs, CampaignSystem.cs |
| 14 | **维克多行为树完善** | 基础采购存在 | 3级优先级：军事刚需→成本控制→投机狙击；困难模式篡改 | CampaignSystem.cs |

### 🟢 低优先级（UI/表现）

| # | 变更项 | 说明 | 涉及文件 |
|---|--------|------|----------|
| 15 | **UI面板架构对齐v6.0** | TopStatusBar/BottomBar/MarketPanel/InfoPanel/ObjectivePanel/GeneralDetailPanel | 多个UI文件 |
| 16 | **K线图UI组件** | InfoPanel核心组件，绘制三条指数K线 | 新建KLineChart.cs |
| 17 | **公私情报分离** | InfoPanel=公开情报，GeneralDetailPanel=私有情报 | UI架构调整 |
| 18 | **局外循环（入场/结算）** | 完成选战役→扣入场费→局内→结算→回账户的完整流程 | 新建MetaGameSystem.cs |
| 19 | **破产保护** | `Cash < 最低入场费`时解锁免费教学关 | MetaGameSystem.cs |

## 2.3 需要清理的旧版代码

以下代码来自v5.0或更早版本，在v6.0中已不再需要：

| 文件/代码 | 原用途 | v6.0状态 | 处理 |
|-----------|--------|----------|------|
| `GeneralData.CalculateBid()` | 将军出价系统 | **已删除** | 移除方法及相关`BidModifier`字段 |
| `GeneralData.CalculateCompositeScore()` | 复合评分判断状态 | **改为纯HP判定** | 替换为HP阈值判定 |
| `GeneralData.AssignedOrder` | 旧版指令分配 | **改为意图系统** | 已有DefaultIntent/FinalIntent替代，确认AssignedOrder不再使用后移除 |
| `GeneralConfigItem.AtkBidModifier` 等 | 出价修正 | **已删除** | 从Config中移除 |
| `GameBalanceConfig.RoutScoreThreshold` | 复合评分溃败阈值 | **改为HP=0溃败** | 移除，溃败条件简化为HP<=0 |
| `GameBalanceConfig.AuditXxx` 字段 | 审计系统 | **已删除** | 移除所有审计相关参数 |
| `CampaignConfig.TargetNetWorth`（如存在）| 目标净资产 | **已删除** | v6.0无目标净资产 |
| `GameBalanceConfig.GammaSensitivity` | 旧版Gamma线性敏感度 | **改为除法公式** | 移除，Gamma不再需要此参数 |

---

# 三、开发任务规划

## 3.0 开发原则

1. **配置驱动**：所有数值参数必须可通过Inspector调整，禁止硬编码
2. **最小改动**：在现有代码基础上修改，不推倒重来
3. **逐步验证**：每个Task完成后可独立测试
4. **GDD权威**：当代码与GDD v6.0冲突时，以GDD为准

---

## 3.1 Phase 1: 数据模型对齐 (基础层)

### Task 1.1: GeneralData状态判定重构

**目标**：将军状态从复合评分改为纯HP阈值判定

**修改文件**：
- `Assets/Scripts/WarBroker/Data/Models/GeneralData.cs`
- `Assets/Scripts/WarBroker/Data/Configs/GameBalanceConfig.cs`
- `Assets/Scripts/WarBroker/Data/Configs/GeneralConfig.cs`

**具体改动**：

```csharp
// GeneralData.cs - 重构状态判定
public class GeneralData
{
    // 保留字段名Troops（与现有代码兼容），含义等同于GDD中的HP
    // 范围0-20，初始值16
    public int Troops;  // = HP in GDD
    
    // ======= 移除以下方法 =======
    // public float CalculateCompositeScore() → 删除
    // public float CalculateBid(...) → 删除
    // private float GetStatusBidModifier(...) → 删除
    
    // ======= 重构状态判定 =======
    public GeneralStatus GetStatus(GameBalanceConfig balance)
    {
        // v6.0: 纯HP阈值判定
        if (Troops <= 0) return GeneralStatus.Routed;
        if (Troops <= 5) return GeneralStatus.Critical;    // 濒死
        if (Troops <= 10) return GeneralStatus.Wounded;    // 受伤
        if (Troops <= 15) return GeneralStatus.Healthy;    // 健康
        return GeneralStatus.FullStrength;                  // 满编 16-20
    }
}

// GeneralConfigItem - 清理旧字段
public class GeneralConfigItem
{
    public string GeneralId;
    public string Name;
    public string Biography;
    public GeneralPersonality Personality;
    
    [Range(0, 20)]
    public int InitialTroops = 16;
    [Range(0, 100)]
    public int InitialTrust = 50;
    [Range(0, 100)]
    public int InitialMorale = 60;
    
    public string[] SkillIds;  // 保留用于战术系统
    
    // ======= 移除以下字段 =======
    // public float AtkBidModifier → 删除
    // public float DefBidModifier → 删除
    // public float RetBidModifier → 删除
}

// GameBalanceConfig.cs - 清理旧参数，添加新参数
// 移除: RoutTroopThreshold, RoutScoreThreshold（改为硬编码HP<=0溃败）
// 移除: 所有Audit相关字段
// 添加:
[Header("===== 兵力恢复参数 =====")]
public int RespawnHP = 10;           // 复活后HP
public int RespawnReserveCost = 10;  // 复活消耗后备役
public int BaseRecoveryHP = 2;       // 基地休整回血
public int BaseRecoveryCost = 2;     // 基地休整消耗后备役
public int RetHealHP = 1;            // RET回血
public int RetHealCost = 1;          // RET回血消耗后备役
```

**验证标准**：
- [ ] `GetStatus` 仅依赖 `Troops` 值判断状态
- [ ] Troops=0 → Routed, Troops=3 → Critical, Troops=8 → Wounded, Troops=12 → Healthy, Troops=18 → FullStrength
- [ ] 旧版 `CalculateBid` / `CalculateCompositeScore` 已移除
- [ ] 所有引用旧方法的代码已更新

### Task 1.2: MarketData重构与K线数据

**目标**：流通盘改为int类型 + 添加Beta累积变量 + K线数据结构

**修改文件**：
- `Assets/Scripts/WarBroker/Data/Models/MarketData.cs`

**具体改动**：

```csharp
[Serializable]
public class MarketData
{
    public Dictionary<OrderType, float> CurrentPrices;
    
    // 重命名 + 改类型：MarketInventory(float) → Float(int)
    public Dictionary<OrderType, int> Float;         // 当前流通盘
    public Dictionary<OrderType, int> InitialFloat;  // 初始流通盘（Gamma用）
    
    // 新增：Beta累积变量（回合间携带）
    public Dictionary<OrderType, float> BetaCarry;   // 初始=1.0
    
    // 新增：上周消耗量（军工厂动态产能用）
    public Dictionary<OrderType, int> LastWeekBurn;
    
    // 新增：K线历史
    public List<KLineData> KLineHistory;
    
    // 保留（改用途）：价格快照历史
    public List<Dictionary<OrderType, float>> PriceHistory;
    
    public void InitFromConfig(OrderConfig config)
    {
        CurrentPrices = new Dictionary<OrderType, float>();
        Float = new Dictionary<OrderType, int>();
        InitialFloat = new Dictionary<OrderType, int>();
        BetaCarry = new Dictionary<OrderType, float>();
        LastWeekBurn = new Dictionary<OrderType, int>();
        KLineHistory = new List<KLineData>();
        PriceHistory = new List<Dictionary<OrderType, float>>();
        
        foreach (var item in config.Orders)
        {
            CurrentPrices[item.OrderType] = item.BasePrice;
            Float[item.OrderType] = item.InitialFloat;       // 使用新字段名
            InitialFloat[item.OrderType] = item.InitialFloat;
            BetaCarry[item.OrderType] = 1f;
            LastWeekBurn[item.OrderType] = 0;
        }
    }
}

/// <summary>单根K线数据（一回合）</summary>
[Serializable]
public class KLineData
{
    public int Turn;
    public Dictionary<OrderType, KLineBar> Bars;  // 每个指数一根K线
}

[Serializable]
public class KLineBar
{
    public float Open;
    public float Close;
    public float High;
    public float Low;
    
    public void RecordTick(float price)
    {
        if (price > High) High = price;
        if (price < Low) Low = price;
    }
}
```

#### K线历史数据

MarketData 中需要添加 K线历史记录：

```csharp
public class MarketData
{
    // ... 现有字段 ...

    /// <summary>K线历史数据（每回合一条）</summary>
    public List<KLineData> KLineHistory;
}
```

每回合结束时，MarketSystem 会将当前回合的K线数据添加到历史记录中。

**注意**：现有代码中 `MarketInventory` 被多处引用（SpotMarketTab, MarketSystem, PricingEngine, DebugConsole等），重命名为 `Float` 后需全局搜索替换。可考虑保留 `MarketInventory` 作为 `Float` 的别名属性以减少改动量。

**验证标准**：
- [ ] 流通盘为int类型
- [ ] BetaCarry初始化为1.0
- [ ] KLineData可正确记录Open/Close/High/Low
- [ ] OrderConfig.InitialFloat（而非InitialStock）控制初始流通盘

### Task 1.3: PlayerData与统一货币体系

**目标**：实现v6.0统一货币体系——入场费=起始资本，无目标净资产

**修改文件**：
- `Assets/Scripts/WarBroker/Data/Models/MarketData.cs`（PlayerData在此文件中）
- `Assets/Scripts/WarBroker/Data/Configs/CampaignConfig.cs`

**具体改动**：

```csharp
// PlayerData - 添加统一货币体系字段
[Serializable]
public class PlayerData
{
    public float Cash;                          // 局内现金
    public Dictionary<OrderType, int> Inventory;
    public float BankDebt;
    public List<FuturesContract> FuturesPositions;
    
    // 重新定义：入场费 = 起始资本 = 盈亏参照锚点
    public float EntryFee;  // 替代旧版AuditValue
    
    public void InitFromConfig(CampaignConfig config)
    {
        Cash = config.EntryFee;  // v6.0: 入场费就是起始资金
        EntryFee = config.EntryFee;
        Inventory = new Dictionary<OrderType, int>
        {
            { OrderType.ATK, config.InitialInventory },
            { OrderType.DEF, config.InitialInventory },
            { OrderType.RET, config.InitialInventory }
        };
        BankDebt = 0f;
        FuturesPositions = new List<FuturesContract>();
    }
    
    // CalculateNetWorth 保持不变
}

// CampaignConfig - 统一货币体系
[CreateAssetMenu(fileName = "Campaign_XXX", menuName = "WarBroker/CampaignConfig")]
public class CampaignConfig : ScriptableObject
{
    [Header("===== 基础信息 =====")]
    public string CampaignId;
    public string CampaignName;
    [TextArea(3, 5)]
    public string Description;
    
    [Header("===== 经济配置 =====")]
    [Tooltip("入场费（= 起始资本）")]
    public float EntryFee = 500f;
    
    [Tooltip("初始各类指令库存")]
    public int InitialInventory = 2;
    
    // 移除: InitialCash（被EntryFee替代）
    // 移除: 分开的InitialAtkInventory等（统一为InitialInventory）
    
    [Header("===== 回合设置 =====")]
    [Range(6, 60)]
    public int MaxTurns = 20;
    
    [Header("===== 后备役设置 =====")]
    [Range(0, 100)]
    public int InitialReserves = 60;
    
    [Header("===== 战场设置 =====")]
    [Range(1, 5)]
    public int InitialFrontlinePosition = 3;
    
    [Header("===== 将军配置 =====")]
    public GeneralConfig GeneralConfig;
    public FrontlineAssignment[] AllyFrontlineAssignments;
    public FrontlineAssignment[] EnemyFrontlineAssignments;
    
    [Header("===== 维克多设置 =====")]
    public float VictorInitialCash = 500f;
    [Range(0, 2)]
    public int VictorDifficulty = 1;  // 0=简单, 1=普通, 2=困难
    
    [Header("===== 委托任务 =====")]
    public CommissionConfig[] AvailableCommissions;
    
    [Header("===== 随机事件 =====")]
    public RandomEventConfig[] AvailableEvents;
}
```

**验证标准**：
- [ ] 入场费和局内现金使用同一个值
- [ ] 无目标净资产、无审计系统
- [ ] 维克多难度改为整数枚举（0/1/2）

### Task 1.4: 委托任务配置

**目标**：新建委托任务配置数据结构

**新建文件**：
- `Assets/Scripts/WarBroker/Data/Configs/CommissionConfig.cs`

```csharp
[CreateAssetMenu(menuName = "WarBroker/CommissionConfig")]
public class CommissionConfig : ScriptableObject
{
    public string CommissionID;
    public string DisplayName;        // "赢下这场战争"
    [TextArea(2, 4)]
    public string Description;        // 条件描述
    [TextArea(2, 4)]
    public string Flavor;             // 氛围文本
    public float BonusReward;         // 奖金金额
    public CommissionType Type;       // 委托类型
}

public enum CommissionType
{
    WinWar,         // 斩首胜利
    ShortCountry,   // 至少2条战线敌方到达Grid 2
    Traitor,        // 战役结束时Grid 5未被占领
    MeatGrinder     // 双方总伤亡 ≥ 100
}

/// <summary>委托运行时追踪数据</summary>
[Serializable]
public class CommissionProgress
{
    public CommissionConfig Config;
    public bool IsCompleted;
    public float Progress;  // 用于MeatGrinder的伤亡累计等
}
```

---

## 3.2 Phase 2: 核心机制对齐 (逻辑层)

### Task 2.1: PricingEngine三因子重构

**目标**：完全按GDD v6.0重写三因子定价

**修改文件**：
- `Assets/Scripts/WarBroker/Systems/PricingEngine.cs`
- `Assets/Scripts/WarBroker/Data/Configs/GameBalanceConfig.cs`

**Alpha因子重构要点**：

```csharp
/// <summary>
/// Alpha因子重构：逐战线计算，然后取平均
/// GDD v6.0 公式：
///   Step 1: 接触基础 (+20%)
///   Step 2: 交战位置修正（5级表，仅接触时）
///   Step 3: 临界修正（Grid 1/5 +15%, 濒死 +10%）
///   Step 4: 多战线汇总取平均
/// </summary>
private float CalculateAlpha(OrderType type)
{
    float totalAlpha = 0f;
    int laneCount = 0;
    
    // 临界修正累积（全局性，但分摊到各战线）
    float globalCriticalBonus = 0f;
    
    foreach (var general in data.Battle.AllyGenerals)
    {
        if (general.Troops <= 0) continue;
        
        var enemy = GetOpposingGeneral(general);
        if (enemy == null || enemy.Troops <= 0) continue;
        
        int P = general.GridPosition;
        int E = enemy.GridPosition;
        int gap = E - P - 1;
        
        float laneAlpha = 0f;
        
        // Step 1: 接触状态基础
        if (gap == 0)
        {
            laneAlpha += 0.20f;
            
            // Step 2: 交战位置修正（仅接触状态）
            float center = (P + E) / 2f;
            laneAlpha += GetPositionAlpha(type, center);
        }
        // 脱离状态：Alpha基础 = 0%
        
        totalAlpha += laneAlpha;
        laneCount++;
        
        // Step 3: 临界修正检查
        if (P == 1 || E == 5) globalCriticalBonus += 0.15f;
        if (general.Troops <= 5) globalCriticalBonus += 0.10f;  // 该战线濒死
    }
    
    if (laneCount == 0) return 0f;
    
    // Step 4: 汇总（临界修正也分摊到平均中）
    return (totalAlpha + globalCriticalBonus) / laneCount;
}

/// <summary>
/// 交战位置修正表（GDD v6.0 第三章）
/// 交战中心 = (P + E) / 2
/// </summary>
private float GetPositionAlpha(OrderType type, float center)
{
    // center范围: 1.0-5.0
    if (center <= 1.5f)
    {
        // 压迫己方基地
        return type switch
        {
            OrderType.ATK => 0.05f,
            OrderType.DEF => 0.25f,
            OrderType.RET => 0.20f,
            _ => 0f
        };
    }
    else if (center <= 2.5f)
    {
        // 己方腹地
        return type switch
        {
            OrderType.ATK => 0.10f,
            OrderType.DEF => 0.20f,
            OrderType.RET => 0.10f,
            _ => 0f
        };
    }
    else if (center <= 3.5f)
    {
        // 中线对峙
        return type switch
        {
            OrderType.ATK => 0.15f,
            OrderType.DEF => 0.15f,
            OrderType.RET => 0.05f,
            _ => 0f
        };
    }
    else if (center <= 4.5f)
    {
        // 敌方腹地
        return type switch
        {
            OrderType.ATK => 0.20f,
            OrderType.DEF => 0.10f,
            OrderType.RET => 0.00f,
            _ => 0f
        };
    }
    else
    {
        // 压迫敌方基地
        return type switch
        {
            OrderType.ATK => 0.25f,
            OrderType.DEF => 0.05f,
            OrderType.RET => -0.05f,
            _ => 0f
        };
    }
}
```

**Beta因子重构要点**：

```csharp
/// <summary>
/// Beta因子：交易冲击累积（v6.0核心变更）
/// - Beta不再每回合重算，而是回合间携带
/// - 每笔交易实时推动Beta变化
/// - 动量效应和恐慌效应在回合开始时应用
/// </summary>

// 回合开始时应用动量和恐慌效应到BetaCarry
public void ApplyBetaEffects()
{
    foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
    {
        float beta = data.Market.BetaCarry[type];
        
        // 动量效应
        float priceChange = GetPriceChangeRatio(type);
        if (priceChange > 0.10f) beta *= 1.2f;
        else if (priceChange < -0.10f) beta *= 0.8f;
        
        // 恐慌效应
        if (data.Battle.CurrentReserves < 20)
        {
            beta *= type switch
            {
                OrderType.ATK => 0.6f,
                OrderType.DEF => 1.5f,
                OrderType.RET => 2.0f,
                _ => 1f
            };
        }
        
        data.Market.BetaCarry[type] = beta;
    }
}

/// <summary>
/// 每笔交易后调用：更新Beta
/// Impact = 交易量 / CurrentFloat × ImpactCoefficient
/// </summary>
public void ApplyTradeImpact(OrderType type, int quantity, bool isBuy)
{
    float currentFloat = Mathf.Max(data.Market.Float[type], 1);
    float impact = (float)quantity / currentFloat * balanceConfig.ImpactCoefficient;
    
    if (!isBuy) impact = -impact;
    
    data.Market.BetaCarry[type] *= (1f + impact);
}

// CalculatePrice中Beta直接使用BetaCarry
private float GetBeta(OrderType type)
{
    return data.Market.BetaCarry[type];
}
```

**Gamma因子重构**：

```csharp
/// <summary>
/// Gamma因子：流动性（v6.0除法公式）
/// Gamma = InitialFloat / Max(CurrentFloat, 1)
/// </summary>
private float CalculateGamma(OrderType type)
{
    int initial = data.Market.InitialFloat[type];
    int current = Mathf.Max(data.Market.Float[type], 1);
    return (float)initial / current;
    // 不再有上限截断——买断库存时Gamma→∞→价格暴涨
}
```

**GameBalanceConfig新增字段**：

```csharp
[Header("===== 三因子定价（v6.0） =====")]
[Tooltip("Beta交易冲击系数")]
[Range(0.01f, 0.2f)]
public float ImpactCoefficient = 0.05f;

// 移除: AlphaContactBase, AlphaCriticalBonus, AlphaLowHPBonus
//       （这些现在是硬编码在位置修正表中的常量）
// 移除: BetaMomentumThreshold, BetaMomentumMultiplier, BetaPanicReserveThreshold, BetaPanicMultiplier
//       （动量/恐慌效应按GDD v6.0硬编码阈值和乘数）
// 移除: GammaSensitivity（改为除法公式，不需要此参数）
```

**验证标准**：
- [ ] 接触状态下ATK/DEF/RET的Alpha因子各不相同
- [ ] 交战中心在1.5时DEF Alpha最高（+25%），在4.5时ATK Alpha最高（+25%）
- [ ] 脱离战线Alpha贡献为0
- [ ] 买入1张指令后Beta增加、价格上涨
- [ ] 流通盘为1时Gamma=初始流通盘（如50），价格暴涨50倍
- [ ] 恐慌效应在后备役<20时正确触发

### Task 2.2: IntentSystem数值对齐

**目标**：将军意图权重与GDD v6.0精确匹配

**修改文件**：
- `Assets/Scripts/WarBroker/Systems/IntentSystem.cs`

**关键改动**：

```csharp
/// <summary>基础权重（整数，GDD v6.0 §4.4）</summary>
private Dictionary<OrderType, float> GetBaseWeights(GeneralPersonality p)
{
    return p switch
    {
        GeneralPersonality.Fanatic => new() 
            { {OrderType.ATK, 70}, {OrderType.DEF, 20}, {OrderType.RET, 10} },
        GeneralPersonality.Conservative => new() 
            { {OrderType.ATK, 20}, {OrderType.DEF, 60}, {OrderType.RET, 20} },
        GeneralPersonality.Opportunist => new() 
            { {OrderType.ATK, 33}, {OrderType.DEF, 33}, {OrderType.RET, 34} },
        _ => new() 
            { {OrderType.ATK, 33}, {OrderType.DEF, 33}, {OrderType.RET, 34} }
    };
}

/// <summary>HP修正表（GDD v6.0 §4.5 完整表格）</summary>
private Dictionary<OrderType, float> GetHPModifier(GeneralPersonality p, int troops)
{
    // 满编 16-20
    if (troops >= 16)
    {
        return p switch
        {
            GeneralPersonality.Fanatic => new() 
                { {OrderType.ATK, 1.2f}, {OrderType.DEF, 0.8f}, {OrderType.RET, 0.5f} },
            GeneralPersonality.Conservative => new() 
                { {OrderType.ATK, 1.0f}, {OrderType.DEF, 1.0f}, {OrderType.RET, 1.0f} },
            GeneralPersonality.Opportunist => new() 
                { {OrderType.ATK, 1.1f}, {OrderType.DEF, 1.0f}, {OrderType.RET, 0.9f} },
            _ => new() 
                { {OrderType.ATK, 1f}, {OrderType.DEF, 1f}, {OrderType.RET, 1f} }
        };
    }
    // 健康 11-15
    if (troops >= 11)
    {
        return p switch
        {
            GeneralPersonality.Fanatic => new() 
                { {OrderType.ATK, 1.1f}, {OrderType.DEF, 0.9f}, {OrderType.RET, 0.7f} },
            GeneralPersonality.Conservative => new() 
                { {OrderType.ATK, 0.8f}, {OrderType.DEF, 1.1f}, {OrderType.RET, 1.2f} },
            GeneralPersonality.Opportunist => new() 
                { {OrderType.ATK, 1.0f}, {OrderType.DEF, 1.0f}, {OrderType.RET, 1.0f} },
            _ => new() 
                { {OrderType.ATK, 1f}, {OrderType.DEF, 1f}, {OrderType.RET, 1f} }
        };
    }
    // 受伤 6-10
    if (troops >= 6)
    {
        return p switch
        {
            GeneralPersonality.Fanatic => new() 
                { {OrderType.ATK, 1.0f}, {OrderType.DEF, 1.0f}, {OrderType.RET, 1.0f} },
            GeneralPersonality.Conservative => new() 
                { {OrderType.ATK, 0.5f}, {OrderType.DEF, 1.2f}, {OrderType.RET, 1.5f} },
            GeneralPersonality.Opportunist => new() 
                { {OrderType.ATK, 0.8f}, {OrderType.DEF, 1.0f}, {OrderType.RET, 1.2f} },
            _ => new() 
                { {OrderType.ATK, 1f}, {OrderType.DEF, 1f}, {OrderType.RET, 1f} }
        };
    }
    // 濒死 1-5（关键！含硬性规则）
    return p switch
    {
        GeneralPersonality.Fanatic => new() 
            { {OrderType.ATK, 1.5f}, {OrderType.DEF, 0.5f}, {OrderType.RET, 0f} },  // RET=0！
        GeneralPersonality.Conservative => new() 
            { {OrderType.ATK, 0f}, {OrderType.DEF, 1.5f}, {OrderType.RET, 3.0f} },   // ATK=0！
        GeneralPersonality.Opportunist => new() 
            { {OrderType.ATK, 0.2f}, {OrderType.DEF, 1.0f}, {OrderType.RET, 2.0f} },
        _ => new() 
            { {OrderType.ATK, 1f}, {OrderType.DEF, 1f}, {OrderType.RET, 1f} }
    };
}

/// <summary>位置修正表（GDD v6.0 §4.5 完整表格）</summary>
private Dictionary<OrderType, float> GetGridModifier(int gridPosition)
{
    return gridPosition switch
    {
        1 => new() { {OrderType.ATK, 1.5f}, {OrderType.DEF, 2.0f}, {OrderType.RET, 0f} },  // Grid 1: RET禁止！
        2 => new() { {OrderType.ATK, 1.0f}, {OrderType.DEF, 1.2f}, {OrderType.RET, 1.5f} },
        3 => new() { {OrderType.ATK, 1.0f}, {OrderType.DEF, 1.0f}, {OrderType.RET, 1.0f} },
        4 => new() { {OrderType.ATK, 1.2f}, {OrderType.DEF, 1.0f}, {OrderType.RET, 0.8f} },
        5 => new() { {OrderType.ATK, 3.0f}, {OrderType.DEF, 1.5f}, {OrderType.RET, 0.1f} },  // Grid 5: ATK极大
        _ => new() { {OrderType.ATK, 1f}, {OrderType.DEF, 1f}, {OrderType.RET, 1f} }
    };
}
```

**验证标准**：
- [ ] 狂热型濒死时：ATK概率极高，RET概率=0%
- [ ] 保守型濒死时：ATK概率=0%，RET概率极高
- [ ] Grid 1时：RET权重为0（退无可退）
- [ ] Grid 5时：ATK权重×3.0（试图终结）

### Task 2.3: MarketSystem交易冲击对齐

**目标**：交易时通过Beta累积影响价格，同时记录K线数据

**修改文件**：
- `Assets/Scripts/WarBroker/Systems/MarketSystem.cs`

**关键改动**：

```csharp
// 现货买入 - 重构
public bool BuyOrder(OrderType orderType, int quantity, out float totalCost)
{
    totalCost = 0f;
    var market = campaignData.Market;
    var player = campaignData.Player;
    
    if (market.Float[orderType] < quantity) return false;
    
    // 逐张计算（每张交易后价格变化）
    for (int i = 0; i < quantity; i++)
    {
        float price = pricingEngine.CalculatePrice(orderType);
        float commission = price * balanceConfig.CommissionRate;
        float cost = price + commission;
        
        if (player.Cash < totalCost + cost) return false;
        
        totalCost += cost;
        market.Float[orderType]--;
        
        // 通过Beta累积推高价格
        pricingEngine.ApplyTradeImpact(orderType, 1, true);
        
        // 更新K线极值
        float newPrice = pricingEngine.CalculatePrice(orderType);
        market.CurrentPrices[orderType] = newPrice;
        RecordKLineTick(orderType, newPrice);
    }
    
    player.Cash -= totalCost;
    player.Inventory[orderType] += quantity;
    market.LastWeekBurn[orderType] += quantity;  // 记录消耗
    
    // 广播事件...
    return true;
}

// K线记录辅助
private void RecordKLineTick(OrderType type, float price)
{
    var kline = GetCurrentKLine();
    if (kline.Bars.ContainsKey(type))
    {
        kline.Bars[type].RecordTick(price);
    }
}
```

**验证标准**：
- [ ] 买入5张ATK后，ATK价格明显高于买入前
- [ ] K线High/Low正确追踪回合内极值
- [ ] LastWeekBurn正确累积消耗量

### Task 2.4: BattleSystem对抗表完善

**目标**：实现完整9格指令对抗表 + 战术星级系统

**修改文件**：
- `Assets/Scripts/WarBroker/Systems/BattleSystem.cs`

**完整对抗表**：

```csharp
/// <summary>接触状态对抗表（GDD v6.0 §5.2）</summary>
private CombatOutcome GetCombatOutcome(OrderType allyOrder, OrderType enemyOrder)
{
    return (allyOrder, enemyOrder) switch
    {
        // ATK vs ...
        (OrderType.ATK, OrderType.ATK) => new CombatOutcome(-2, -2, false, false, false, false, "遭遇战"),
        (OrderType.ATK, OrderType.DEF) => new CombatOutcome(-4, -1, false, false, false, false, "攻坚战"),
        (OrderType.ATK, OrderType.RET) => new CombatOutcome(0, 0, true, false, false, true, "追击"),
        
        // DEF vs ...
        (OrderType.DEF, OrderType.ATK) => new CombatOutcome(-1, -4, false, false, false, false, "阻击"),
        (OrderType.DEF, OrderType.DEF) => new CombatOutcome(0, 0, false, false, false, false, "静坐"),
        (OrderType.DEF, OrderType.RET) => new CombatOutcome(0, +1, false, false, false, true, "目送"),
        
        // RET vs ...
        (OrderType.RET, OrderType.ATK) => new CombatOutcome(+1, 0, false, true, true, false, "撤离"),
        (OrderType.RET, OrderType.DEF) => new CombatOutcome(+1, 0, false, true, false, false, "休整"),
        (OrderType.RET, OrderType.RET) => new CombatOutcome(+1, +1, false, true, false, true, "脱离"),
        
        _ => new CombatOutcome(0, 0, false, false, false, false, "未知")
    };
}

public struct CombatOutcome
{
    public int AllyHPChange;      // 己方HP变化（基础值）
    public int EnemyHPChange;     // 敌方HP变化（基础值）
    public bool AllyAdvance;      // 己方前进 (P++)
    public bool AllyRetreat;      // 己方后撤 (P--)
    public bool EnemyAdvance;     // 敌方前进 (E--)
    public bool EnemyRetreat;     // 敌方后撤 (E++)
    public string Description;
    
    public CombatOutcome(int allyHP, int enemyHP, bool aAdv, bool aRet, bool eAdv, bool eRet, string desc)
    {
        AllyHPChange = allyHP; EnemyHPChange = enemyHP;
        AllyAdvance = aAdv; AllyRetreat = aRet;
        EnemyAdvance = eAdv; EnemyRetreat = eRet;
        Description = desc;
    }
}
```

**战术星级系统**：

```csharp
/// <summary>战术星级抽取（GDD v6.0 §5.4）</summary>
public enum TacticTier { Normal, Elite }

public TacticTier RollTactic(GeneralData general, bool isReinforced)
{
    float normalWeight = 90f;
    float eliteWeight = 10f;
    
    if (isReinforced)
    {
        normalWeight *= 0.5f;   // 90 → 45
        eliteWeight *= 5.0f;    // 10 → 50
    }
    
    // 性格加成（仅在强化时生效）
    if (isReinforced)
    {
        var intent = general.FinalIntent ?? general.DefaultIntent;
        if (intent == OrderType.ATK && general.Personality == GeneralPersonality.Fanatic)
            eliteWeight += normalWeight * 0.2f;  // 额外+20%
        if (intent == OrderType.DEF && general.Personality == GeneralPersonality.Conservative)
            eliteWeight += normalWeight * 0.2f;
    }
    
    float total = normalWeight + eliteWeight;
    return UnityEngine.Random.Range(0f, total) < normalWeight ? TacticTier.Normal : TacticTier.Elite;
}

/// <summary>精锐战术效果（GDD v6.0 §5.4）</summary>
public void ApplyEliteTactic(OrderType order, TacticTier tier, 
    ref int allyDamageBonus, ref int enemyDamageBonus, ref int allyHealBonus, BattleData battle)
{
    if (tier != TacticTier.Elite) return;
    
    switch (order)
    {
        case OrderType.ATK:
            // 精锐突击：伤害+2，无视DEF减伤
            enemyDamageBonus += 2;
            // TODO: 标记ignoreDefReduction用于后续计算
            break;
        case OrderType.DEF:
            // 战地医院：HP+3（消耗Reserves 3）
            if (battle.CurrentReserves >= 3)
            {
                allyHealBonus += 3;
                battle.CurrentReserves -= 3;
            }
            break;
        case OrderType.RET:
            // 焦土战术：敌方-2HP，己方正常回血
            allyDamageBonus += 2;  // 对追击敌人造成伤害
            break;
    }
}
```

**验证标准**：
- [ ] ATK vs ATK：双方各-2HP，不移动
- [ ] ATK vs RET：无伤害，己方推进敌方后撤
- [ ] RET vs DEF：己方+1HP后撤，敌方无伤
- [ ] 强化后精锐概率约53%（从10%提升）

### Task 2.5: 委托任务系统

**目标**：实现4种委托的条件检测和奖金结算

**新建文件**：
- `Assets/Scripts/WarBroker/Systems/CommissionSystem.cs`

```csharp
public class CommissionSystem
{
    private CampaignRuntimeData data;
    private List<CommissionProgress> activeCommissions;
    
    public void Init(CampaignRuntimeData data)
    {
        this.data = data;
        activeCommissions = new List<CommissionProgress>();
        
        if (data.Config.AvailableCommissions != null)
        {
            foreach (var config in data.Config.AvailableCommissions)
            {
                activeCommissions.Add(new CommissionProgress 
                { 
                    Config = config, 
                    IsCompleted = false, 
                    Progress = 0 
                });
            }
        }
    }
    
    /// <summary>每回合结束时检查</summary>
    public void CheckCommissions(GameResult gameResult)
    {
        foreach (var commission in activeCommissions)
        {
            if (commission.IsCompleted) continue;
            
            switch (commission.Config.Type)
            {
                case CommissionType.WinWar:
                    // 斩首胜利
                    commission.IsCompleted = (gameResult == GameResult.Victory);
                    break;
                    
                case CommissionType.ShortCountry:
                    // 至少2条战线敌方到达Grid 2
                    int threatenedLanes = 0;
                    foreach (var fl in data.Battle.Frontlines.Values)
                    {
                        // 敌方在Grid 2意味着己方战线被推回至LinePosition 2
                        // （需要根据你的战线位置表示法确认）
                        if (fl.LinePosition <= 2) threatenedLanes++;
                    }
                    commission.IsCompleted = threatenedLanes >= 2;
                    break;
                    
                case CommissionType.Traitor:
                    // 战役结束时Grid 5未被占领
                    if (gameResult != GameResult.InProgress)
                    {
                        bool anyAtGrid5 = false;
                        foreach (var fl in data.Battle.Frontlines.Values)
                        {
                            if (fl.IsAtEnemyBase) anyAtGrid5 = true;
                        }
                        commission.IsCompleted = !anyAtGrid5;
                    }
                    break;
                    
                case CommissionType.MeatGrinder:
                    // 双方总伤亡 ≥ 100（Progress累积伤亡值）
                    commission.IsCompleted = commission.Progress >= 100;
                    break;
            }
        }
    }
    
    /// <summary>战斗结算后更新伤亡统计</summary>
    public void RecordCasualties(int allyCasualties, int enemyCasualties)
    {
        foreach (var commission in activeCommissions)
        {
            if (commission.Config.Type == CommissionType.MeatGrinder)
            {
                commission.Progress += allyCasualties + enemyCasualties;
            }
        }
    }
    
    /// <summary>战役结束时计算奖金</summary>
    public float CalculateTotalBonus()
    {
        float total = 0f;
        foreach (var c in activeCommissions)
        {
            if (c.IsCompleted) total += c.Config.BonusReward;
        }
        return total;
    }
    
    public List<CommissionProgress> GetCommissions() => activeCommissions;
}
```

**验证标准**：
- [ ] 斩首胜利时"赢下战争"委托达成
- [ ] 2条战线LinePosition≤2时"做空祖国"达成
- [ ] 战役结束且无Grid 5占领时"卖国求荣"达成
- [ ] 总伤亡累计≥100时"绞肉机"达成

### Task 2.6: 军工厂动态产能

**目标**：产能改为动态计算

**修改位置**：`MarketSystem.cs` 的回合结算方法

```csharp
/// <summary>军工厂动态产能（GDD v6.0 §3.2）</summary>
public void ProduceOrders()
{
    var market = campaignData.Market;
    
    foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
    {
        int lastBurn = market.LastWeekBurn[type];
        
        // 本周产能 = 上周消耗 × 产能系数(0.9~1.1)
        float coefficient = UnityEngine.Random.Range(0.9f, 1.1f);
        int production = Mathf.RoundToInt(lastBurn * coefficient);
        
        // 保底产能
        production = Mathf.Max(production, 3);
        
        market.Float[type] += production;
        
        // 重置消耗计数
        market.LastWeekBurn[type] = 0;
    }
}
```

### Task 2.7: 政权崩溃清算

**目标**：Grid 1沦陷时特殊清算

**修改位置**：`CampaignSystem.cs` / `MarketSystem.cs`

```csharp
/// <summary>政权崩溃清算（GDD v6.0 §3.4）</summary>
public void ExecuteRegimeCollapse()
{
    var market = campaignData.Market;
    var player = campaignData.Player;
    
    // 1. ATK、DEF现货价格归零
    market.CurrentPrices[OrderType.ATK] = 0f;
    market.CurrentPrices[OrderType.DEF] = 0f;
    // RET价格保留（可配置）
    
    // 2. 玩家持有的ATK/DEF现货价值归零（库存保留但价值为0）
    
    // 3. 空单按$0结算（空单获得最大收益）
    var toSettle = new List<FuturesContract>(player.FuturesPositions);
    foreach (var contract in toSettle)
    {
        if (contract.TargetOrder == OrderType.ATK || contract.TargetOrder == OrderType.DEF)
        {
            float pnl = contract.CalculatePnL(0f);  // 按$0结算
            player.Cash += contract.Margin + pnl;
            player.FuturesPositions.Remove(contract);
        }
    }
}
```

---

## 3.3 Phase 3: 回合流程对齐

### Task 3.1: 五阶段回合流程

**目标**：对齐GDD v6.0的五阶段流程

**修改文件**：
- `Assets/Scripts/WarBroker/Systems/CampaignSystem.cs`

```
阶段 I：周报与开盘
  1. Week++
  2. 利息/仓储费结算
  3. 随机事件抽取
  4. 将军意图生成（灰色气泡）
  5. Alpha/Gamma更新 → 新价格 = Open
  
阶段 II：玩家操盘（唯一交互阶段）
  - 金融交易 → 每笔交易推动Beta → 价格实时变化
  - 政治干涉（强化/篡改）
  - 银行操作
  - 点击"结束本周"
  
阶段 III：维克多行动
  - 军事采购（推动Beta）
  - 投机操作（困难模式）
  
阶段 IV：战斗推演
  - 抗命检查 → 战术揭示 → 伤害计算 → 战线移动
  
阶段 V：回合结算
  - 记录Close价格
  - 基地恢复（消耗Reserves）
  - 溃败重组
  - 军工厂产出
  - 期货到期结算
  - 胜负检查
  - 生成K线
```

**K线生成时机**：
- **阶段I结束时**：记录 Open 价格（此时价格已根据供给调整）
- **阶段V结束时**：记录 Close 价格（此时价格已根据战斗结果调整）
- **High/Low**：在整个回合的交易过程中，每次价格变动时调用 `KLineBar.RecordTick(price)` 更新最高/最低价

#### K线数据记录实现

MarketSystem 中需要添加以下方法：

```csharp
/// <summary>记录K线开盘价（阶段I结束时调用）</summary>
public void RecordKLineOpen()
{
    var market = campaignData.Market;
    var klineData = new KLineData
    {
        Turn = campaignData.CurrentTurn,
        Bars = new Dictionary<OrderType, KLineBar>()
    };

    foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
    {
        float price = market.CurrentPrices[orderType];
        klineData.Bars[orderType] = new KLineBar
        {
            Open = price,
            Close = price,  // 初始值等于Open
            High = price,
            Low = price
        };
    }

    // 临时存储当前回合的K线数据
    currentKLineData = klineData;
}

/// <summary>更新K线实时价格（每次交易后调用）</summary>
public void UpdateKLineRealtime()
{
    if (currentKLineData == null) return;

    var market = campaignData.Market;
    foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
    {
        float price = market.CurrentPrices[orderType];
        currentKLineData.Bars[orderType].RecordTick(price);
    }

    // 触发实时更新事件
    eventService.SendMessage((EventID)WarBrokerEventID.OnKLineUpdate, currentKLineData, null);
}

/// <summary>记录K线收盘价（阶段V结束时调用）</summary>
public void RecordKLineClose()
{
    if (currentKLineData == null) return;

    var market = campaignData.Market;
    foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
    {
        currentKLineData.Bars[orderType].Close = market.CurrentPrices[orderType];
    }

    // 添加到历史记录
    market.KLineHistory.Add(currentKLineData);
    currentKLineData = null;

    // 触发K线完成事件
    eventService.SendMessage((EventID)WarBrokerEventID.OnKLineComplete, null, null);
}
```

**集成到回合流程**：
- TurnSystem.ExecutePhaseI() 结束时调用 `marketSystem.RecordKLineOpen()`
- MarketSystem.BuyOrder() 和 SellOrder() 中调用 `UpdateKLineRealtime()`
- TurnSystem.ExecutePhaseV() 结束时调用 `marketSystem.RecordKLineClose()`

### Task 3.2: 期货固定3回合

**目标**：期货到期时间固定为3回合

**修改文件**：
- `Assets/Scripts/WarBroker/Systems/MarketSystem.cs`

```csharp
// OpenFutures 中移除 expirationTurns 参数
public bool OpenFutures(OrderType orderType, FuturesDirection direction, int quantity, 
    out FuturesContract contract)
{
    // expirationTurns 强制为 3
    int expirationTurns = 3;
    // ... 其余逻辑不变
}
```

---

## 3.4 Phase 4: UI面板架构

### Task 4.1: TopStatusBar

常驻顶部：回合数、现金、净资产、审计价值（入场费）

### Task 4.2: BottomBar

常驻底部：事件通知滚动条、结束回合按钮

### Task 4.3: MarketPanel重构

整合现货+期货+银行借贷为单面板Tab切换。现有SpotMarketTab和FuturesMarketTab可作为子组件复用。

### Task 4.4: InfoPanel 与 K线图

#### 概述
InfoPanel 是公开信息面板，包含K线图、供给状态、公开战报、事件公告等内容。本节重点说明K线图的实现。

#### XCharts集成

项目已通过 `Packages/manifest.json` 集成 XCharts 插件：
```json
"com.monitor1394.xcharts.daemon": "https://github.com/XCharts-Team/XCharts-Daemon.git"
```

XCharts 提供了专业的K线图组件 `SimplifiedCandlestickChart`，支持：
- 标准OHLC（开高低收）数据格式
- 自动绘制K线柱体和影线
- X轴时间标签
- 缩放和拖拽交互

#### KLineChart 组件实现

**文件路径**：`Assets/Scripts/WarBroker/UI/KLineChart.cs`

```csharp
using UnityEngine;
using XCharts.Runtime;
using System.Collections.Generic;

/// <summary>
/// K线图UI组件 - 使用XCharts绘制K线图
/// </summary>
public class KLineChart : WindowBase
{
    [Header("XCharts组件")]
    [SerializeField] private SimplifiedCandlestickChart chart;

    [Header("指数切换")]
    [SerializeField] private Toggle toggleATK;
    [SerializeField] private Toggle toggleDEF;
    [SerializeField] private Toggle toggleRET;

    private OrderType currentOrderType = OrderType.ATK;
    private CampaignRuntimeData campaignData;
    private MarketSystem marketSystem;

    public override void OnAwake()
    {
        base.OnAwake();

        // 获取系统引用
        marketSystem = gameRoot.GetLogic<MarketSystem>();

        // 绑定切换按钮
        AddToggleListener(toggleATK, OnToggleChanged);
        AddToggleListener(toggleDEF, OnToggleChanged);
        AddToggleListener(toggleRET, OnToggleChanged);

        // 监听K线更新事件
        eventService.AddEventListener((EventID)WarBrokerEventID.OnKLineUpdate, OnKLineUpdate);
        eventService.AddEventListener((EventID)WarBrokerEventID.OnKLineComplete, OnKLineComplete);
    }

    public override void OnShow()
    {
        base.OnShow();

        // 获取运行时数据
        campaignData = gameRoot.GetLogic<CampaignSystem>().GetRuntimeData();

        // 初始化图表
        InitChart();

        // 加载历史数据
        LoadHistoryData();
    }

    private void InitChart()
    {
        if (chart == null) return;

        // 清空数据
        chart.ClearData();

        // 配置图表标题
        var title = chart.EnsureChartComponent<Title>();
        title.text = GetOrderTypeName(currentOrderType) + " K线图";

        // 配置X轴
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Category;

        // 配置Y轴
        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;

        // 配置系列
        var serie = chart.GetSerie<Candlestick>(0);
        if (serie == null)
        {
            serie = chart.AddSerie<Candlestick>("K线");
        }
    }

    private void LoadHistoryData()
    {
        if (campaignData == null || chart == null) return;

        var history = campaignData.Market.KLineHistory;

        // 添加历史K线数据
        for (int i = 0; i < history.Count; i++)
        {
            var klineData = history[i];
            var bar = klineData.Bars[currentOrderType];

            // 添加X轴标签（回合数）
            chart.AddXAxisData($"T{klineData.Turn}");

            // 添加K线数据：serieIndex, index, open, close, lowest, highest
            chart.AddData(0, i, bar.Open, bar.Close, bar.Low, bar.High);
        }
    }

    private void OnToggleChanged(Toggle toggle, bool isOn)
    {
        if (!isOn) return;

        // 确定选中的指数类型
        if (toggle == toggleATK) currentOrderType = OrderType.ATK;
        else if (toggle == toggleDEF) currentOrderType = OrderType.DEF;
        else if (toggle == toggleRET) currentOrderType = OrderType.RET;

        // 重新加载数据
        InitChart();
        LoadHistoryData();
    }

    private void OnKLineUpdate(EventID eventID, object param1, object param2)
    {
        // 实时更新当前K线（最后一根）
        var currentKLine = param1 as KLineData;
        if (currentKLine == null || chart == null) return;

        var bar = currentKLine.Bars[currentOrderType];
        int lastIndex = campaignData.Market.KLineHistory.Count;

        // 更新最后一根K线的数据
        chart.UpdateData(0, lastIndex, bar.Open, bar.Close, bar.Low, bar.High);
    }

    private void OnKLineComplete(EventID eventID, object param1, object param2)
    {
        // K线完成，添加新的X轴标签
        int turn = campaignData.CurrentTurn;
        chart.AddXAxisData($"T{turn}");
    }

    private string GetOrderTypeName(OrderType type)
    {
        switch (type)
        {
            case OrderType.ATK: return "进攻指令";
            case OrderType.DEF: return "防御指令";
            case OrderType.RET: return "撤退指令";
            default: return "";
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        // 移除事件监听
        eventService.RemoveEventListener((EventID)WarBrokerEventID.OnKLineUpdate, OnKLineUpdate);
        eventService.RemoveEventListener((EventID)WarBrokerEventID.OnKLineComplete, OnKLineComplete);
    }
}
```

#### InfoPanelBinder 实现

**文件路径**：`Assets/Scripts/WarBroker/UI/InfoPanelBinder.cs`

```csharp
using UnityEngine;

/// <summary>
/// InfoPanel 的 Binder 类 - 负责绑定UI组件
/// </summary>
public class InfoPanelBinder : WindowBinder
{
    [Header("子组件")]
    public KLineChart klineChart;
    public SupplyStatusPanel supplyPanel;
    public BattleReportPanel reportPanel;
    public EventAnnouncementPanel eventPanel;

    public override void Bind(WindowBase window)
    {
        var infoPanel = window as InfoPanel;
        if (infoPanel == null) return;

        infoPanel.klineChart = klineChart;
        infoPanel.supplyPanel = supplyPanel;
        infoPanel.reportPanel = reportPanel;
        infoPanel.eventPanel = eventPanel;
    }
}
```

#### 事件ID扩展

在 `WarBrokerEventID.cs` 中添加：

```csharp
public enum WarBrokerEventID
{
    // ... 现有事件 ...

    /// <summary>K线实时更新（回合内交易触发）</summary>
    OnKLineUpdate = 2001,

    /// <summary>K线完成（回合结束触发）</summary>
    OnKLineComplete = 2002,
}
```

#### 预制体结构

InfoPanel 预制体层级结构：
```
InfoPanel (Canvas)
├── KLineChart
│   ├── Chart (SimplifiedCandlestickChart)
│   └── ToggleGroup
│       ├── Toggle_ATK
│       ├── Toggle_DEF
│       └── Toggle_RET
├── SupplyStatusPanel
├── BattleReportPanel
└── EventAnnouncementPanel
```

#### 使用说明

1. **创建预制体**：在场景中创建 InfoPanel，添加 SimplifiedCandlestickChart 组件
2. **绑定组件**：将 Chart 和 Toggle 组件拖拽到 KLineChart 的序列化字段
3. **打开面板**：通过 UIService 打开 InfoPanel
4. **自动更新**：K线图会自动响应交易事件和回合结束事件

#### 性能优化建议

- 使用 `SimplifiedCandlestickChart` 而非 `CandlestickChart`（性能更好）
- 限制显示的K线数量（如最近30回合）
- 使用 `chart.SetMaxCache()` 限制缓存大小

### Task 4.5: ObjectivePanel（新建）

显示：主线目标（当前P&L）+ 委托任务状态列表

### Task 4.6: GeneralDetailPanel对齐

现有面板需要对齐v6.0 UI规范：从右侧滑出，显示意图+属性+操作按钮。

**框架遵循检查清单**：
- [ ] 所有UI窗口继承 `WindowBase`
- [ ] 使用 `InputRouter.Acquire/Release`
- [ ] 使用 `eventService.AddEventListening` / `RemoveEventListeningByTarget`
- [ ] 弹窗通过 `UIService.ShowWindow` + `UILayer.Top`
- [ ] 无PopupManager、无InputLayerManager

---

# 四、文件修改总清单

## 4.1 需要修改的现有文件

| 文件路径 | 修改类型 | Phase | 优先级 |
|----------|----------|-------|--------|
| `Data/Models/GeneralData.cs` | 重构状态判定，清理旧方法 | P1 | 🔴 |
| `Data/Models/MarketData.cs` | 流通盘改int、加Beta/KLine | P1 | 🔴 |
| `Data/Models/MarketData.cs`(PlayerData部分) | 统一货币体系 | P1 | 🔴 |
| `Data/Models/CampaignData.cs` | 添加CommissionProgress列表 | P1 | 🔴 |
| `Data/Configs/GameBalanceConfig.cs` | 清理旧参数、添加新参数 | P1 | 🔴 |
| `Data/Configs/GeneralConfig.cs` | 清理BidModifier | P1 | 🟡 |
| `Data/Configs/CampaignConfig.cs` | EntryFee、CommissionConfig | P1 | 🔴 |
| `Data/Configs/OrderConfig.cs` | InitialFloat改为50 | P1 | 🟡 |
| `Systems/PricingEngine.cs` | 三因子全面重构 | P2 | 🔴 |
| `Systems/IntentSystem.cs` | 权重数值对齐 | P2 | 🔴 |
| `Systems/MarketSystem.cs` | 交易冲击、产能、政权崩溃 | P2 | 🔴 |
| `Systems/BattleSystem.cs` | 对抗表完善、战术系统 | P2 | 🟡 |
| `Systems/CampaignSystem.cs` | 回合流程、K线、结算 | P3 | 🔴 |
| `UI/SpotMarketTab.cs` | 字段名对齐 | P4 | 🟢 |
| `UI/FuturesMarketTab.cs` | 固定3回合 | P4 | 🟢 |
| `UI/GeneralDetailPanel.cs` | UI规范对齐 | P4 | 🟢 |
| `Debug/WarBrokerDebugConsole.cs` | 字段名对齐 | P4 | 🟢 |
| `Editor/Tests/*.cs` | 测试用例更新 | P4 | 🟡 |

## 4.2 需要新建的文件

| 文件路径 | 用途 | Phase | 优先级 |
|----------|------|-------|--------|
| `Data/Configs/CommissionConfig.cs` | 委托任务配置 | P1 | 🔴 |
| `Systems/CommissionSystem.cs` | 委托任务逻辑 | P2 | 🟡 |
| `Systems/MetaGameSystem.cs` | 局外循环（入场/结算/破产保护）| P3 | 🟢 |
| `UI/TopStatusBar.cs` | 顶部状态栏 | P4 | 🟢 |
| `UI/BottomBar.cs` | 底部事件栏 | P4 | 🟢 |
| `UI/InfoPanel.cs` | 公开信息面板 | P4 | 🟢 |
| `UI/ObjectivePanel.cs` | 目标/委托面板 | P4 | 🟢 |
| `UI/KLineChart.cs` | K线图UI组件 | P4 | 🟢 |

## 4.3 无需新建（框架已提供）

- ~~InputLayerManager.cs~~ → 使用 `InputRouter`
- ~~PopupManager.cs~~ → 使用 `UIService` + `UILayer.Top`
- ~~SpotMarketTab.cs / FuturesMarketTab.cs~~ → 已存在

---

# 五、事件ID扩展

在 `WarBrokerEventID.cs` 中添加：

```csharp
public enum WarBrokerEventID
{
    // === 现有事件（保留） ===
    OnPhaseChange,
    OnTurnStart,
    OnTurnEnd,
    OnTradeExecuted,
    OnCashChange,
    OnPriceUpdate,
    OnOrderAssigned,
    OnFuturesOpened,
    OnFuturesClosed,
    OnForceLiquidation,
    OnVictoryConditionMet,
    OnDefeatConditionMet,
    OnDrawConditionMet,
    OnGameEnd,
    
    // === 意图系统（已存在，确认保留）===
    OnIntentChanged = 2001,
    
    // === 新增事件 ===
    OnKLineUpdated = 3001,         // K线数据更新
    OnBetaChanged = 3002,          // Beta因子变化（交易冲击）
    OnFloatChanged = 3003,         // 流通盘变化
    OnCommissionCompleted = 3004,  // 委托任务完成
    OnRegimeCollapse = 3005,       // 政权崩溃
    OnReservesChanged = 3006,      // 后备役变化
    OnReservesDepleted = 3007,     // 后备役耗尽
    OnVictorPurchase = 3008,       // 维克多采购
    OnVictorMonopolized = 3009,    // 维克多被垄断
    OnProductionComplete = 3010,   // 军工厂产出完成
}
```

---

# 六、开发顺序与时间估算

```
Week 1: Phase 1（数据模型对齐）
  ├─ Task 1.1 GeneralData重构      [2h]
  ├─ Task 1.2 MarketData重构       [3h]  
  ├─ Task 1.3 PlayerData+统一货币   [2h]
  └─ Task 1.4 委托任务配置          [1h]
  
Week 2: Phase 2A（核心机制-定价）
  ├─ Task 2.1 PricingEngine重构    [4h] ← 最关键
  ├─ Task 2.2 IntentSystem对齐     [2h]
  └─ Task 2.3 MarketSystem交易冲击  [3h]

Week 3: Phase 2B（核心机制-战斗）
  ├─ Task 2.4 BattleSystem对抗表   [3h]
  ├─ Task 2.5 CommissionSystem     [2h]
  ├─ Task 2.6 军工厂动态产能        [1h]
  └─ Task 2.7 政权崩溃清算          [1h]

Week 4: Phase 3（回合流程）
  ├─ Task 3.1 五阶段流程对齐       [3h]
  └─ Task 3.2 期货固定3回合        [0.5h]

Week 5: Phase 4（UI）
  ├─ Task 4.1-4.6 UI面板           [8h]
  └─ 测试更新                      [2h]
```

---

# 七、验收标准总表

## 核心循环验收

- [ ] 将军每回合自动生成默认意图（免费执行）
- [ ] 不干涉时将军执行默认意图，不消耗任何现货
- [ ] 强化消耗1份同类现货，篡改消耗3份异类现货
- [ ] 三因子定价公式 `Price = P_base × (1+Alpha) × Beta × Gamma` 正确
- [ ] Alpha基于交战中心的5级位置修正表
- [ ] Beta每笔交易后累积变化，回合间携带
- [ ] Gamma = InitialFloat / Max(CurrentFloat, 1)
- [ ] 买入实时影响流通盘和价格（Gamma↑ + Beta↑）
- [ ] K线每回合正确生成 Open/Close/High/Low

## 金融系统验收

- [ ] 期货固定3回合到期
- [ ] 做多爆仓点：价格≤开仓价×0.86
- [ ] 做空爆仓点：价格≥开仓价×1.14
- [ ] 银行利率5%/回合复利
- [ ] 仓储费$3/张/回合
- [ ] 手续费率2%
- [ ] 政权崩溃时ATK/DEF归$0，空单最大收益结算

## 将军系统验收

- [ ] 状态判定纯HP阈值（满编16+/健康11+/受伤6+/濒死1+/溃败0）
- [ ] 狂热者濒死时RET权重=0（玉碎冲锋）
- [ ] 保守者濒死时ATK权重=0（求生本能）
- [ ] Grid 1时RET权重=0（退无可退）
- [ ] Grid 5时ATK权重×3.0（决战）
- [ ] 信任度≤29时50%抗命概率

## 战斗系统验收

- [ ] 完整9格对抗表
- [ ] 脱离状态：ATK=推进，DEF=驻扎，RET=后撤+回血
- [ ] RET回血消耗Reserves
- [ ] 基地休整(Grid 1)每回合+2HP消耗2 Reserves
- [ ] 溃败重组消耗10 Reserves，HP=10
- [ ] 后备役耗尽时所有回血失效

## 委托任务验收

- [ ] 4种委托条件正确检测
- [ ] 奖金在战役结算时正确发放
- [ ] ObjectivePanel显示委托状态

## 统一货币验收

- [ ] 入场费从玩家账户扣除 = 局内起始现金
- [ ] 战役结束时净资产全额回到玩家账户
- [ ] 无"目标净资产"、无"审计"、无"没收"
- [ ] 破产保护正确触发

---

# 八、关键注意事项

## 8.1 字段命名兼容策略

当前代码使用 `Troops` 而GDD使用 `HP`，使用 `MarketInventory` 而GDD使用 `Float`。建议：

1. **内部保留现有字段名**（减少大规模重命名风险）
2. **在注释中标注GDD术语**
3. **仅在UI层和调试输出中使用GDD术语**

```csharp
public int Troops;  // = GDD中的HP, 范围0-20
```

## 8.2 ScriptableObject修改影响

修改Config字段时，Unity Inspector中的已有资产值会丢失。建议：

1. 修改Config前在Editor脚本中导出当前值
2. 添加新字段时使用合理默认值
3. 删除字段后运行一次 `WarBrokerConfigSetup` 重新配置

## 8.3 维克多AI的开发优先级

维克多AI是一个可以逐步增强的系统。建议先实现最简单的版本（仅军事采购），后续再添加成本控制和投机狙击：

```
Step 1: 对每个敌方将军尝试购买1份指令强化（简单模式）
Step 2: 购买失败时记录垄断事件（普通模式）
Step 3: 检查意图质量，尝试篡改愚蠢意图（困难模式）
Step 4: 根据玩家持仓决定是否投机狙击（困难模式）
```

## 8.4 测试策略

优先编写以下测试（最高投入产出比）：

1. **PricingEngine单元测试**：给定特定战局状态，验证Alpha/Beta/Gamma输出
2. **IntentSystem单元测试**：给定将军属性，验证意图概率分布
3. **对抗表测试**：验证9种指令组合的HP变化和战线移动
4. **委托条件测试**：模拟达成条件验证检测正确

---

**文档版本**：v2.0  
**基于**：GDD v6.0 + 现有代码库全面分析  
**v2.0变更**：基于GDD v6.0重写（统一货币、K线、委托、Alpha位置修正表、Beta交易冲击、Gamma除法公式、纯HP状态判定），标注所有与现有代码的具体差异和迁移路径  
**状态**：可指导Claude Code执行开发
