# 炮火与K线 (Charta Bellum)

## Claude Code 开发指导文档

**文档性质**：开发任务规划与实现指导  
**目标读者**：Claude Code / AI编程助手  
**配套文档**：系统设计策划案 v5.0、原型开发策划案

---

# 一、项目概述

## 1.1 项目定位

回合制金融投机策略游戏。玩家扮演军需掮客，通过操纵战局影响军需品价格，从价格波动中获利。

## 1.2 技术栈

- **引擎**：Unity (C#)
- **框架**：Levity Framework（自研通用框架）
- **插件**：Odin Inspector, DOTween, Naninovel (可选)
- **架构模式**：
  - 服务定位器 (GameRoot单例)
  - ScriptableObject配置驱动
  - 事件总线 (EventService)

## 1.3 代码规范

- 命名空间：无（框架未使用）
- 类命名：PascalCase
- 方法命名：PascalCase
- 字段命名：camelCase (私有), PascalCase (公有)
- 配置路径：`Assets/Resources/Config/WarBroker/`
- 脚本路径：`Assets/Scripts/WarBroker/`

---

# 二、当前开发进度分析

## 2.1 已完成模块

### 核心框架 (Levity Framework)
| 模块 | 状态 | 说明 |
|------|------|------|
| GameRoot | ✅ 完成 | 单例入口，服务注册 |
| EventService | ✅ 完成 | 事件总线 |
| ResService | ✅ 完成 | 资源加载 |
| UIService | ✅ 完成 | 窗口管理 |
| ManagerService | ✅ 完成 | 场景管理器 |

### WarBroker 系统
| 模块 | 状态 | 说明 |
|------|------|------|
| MarketSystem | ⚠️ 部分完成 | 现货/期货交易基础逻辑存在，但定价模型不符合GDD |
| BattleSystem | ⚠️ 部分完成 | 战斗结算存在，但兵力/接触判定不符合GDD |
| CampaignSystem | ⚠️ 部分完成 | 回合流程存在，但缺少将军意图系统 |

### 数据模型
| 模块 | 状态 | 说明 |
|------|------|------|
| CampaignRuntimeData | ✅ 完成 | 战役运行时数据容器 |
| PlayerData | ✅ 完成 | 玩家资产数据 |
| MarketData | ⚠️ 需修改 | 缺少流通盘(Float)字段 |
| BattleData | ⚠️ 需修改 | 缺少后备役字段 |
| GeneralData | ⚠️ 需修改 | 兵力用0-100而非0-20 |

### 配置文件
| 配置 | 状态 | 说明 |
|------|------|------|
| GameBalanceConfig | ⚠️ 需扩展 | 缺少三因子参数 |
| OrderConfig | ✅ 完成 | 基础价格、产能 |
| SkillConfig | ✅ 完成 | 技能触发条件与效果 |
| GeneralConfig | ✅ 完成 | 将军属性配置 |
| CampaignConfig | ⚠️ 需扩展 | 缺少后备役、目标净资产 |

### UI界面
| 界面 | 状态 | 说明 |
|------|------|------|
| GameplayWindow | ⚠️ 部分完成 | 框架存在，需完善 |
| MarketPanel | ⚠️ 部分完成 | 交易UI存在，需对接新系统 |
| GeneralPanel | ❌ 未实现 | 将军管理与干涉 |
| BattlefieldPanel | ❌ 未实现 | 战场可视化 |

## 2.2 与GDD v5.0的差距

### 关键差距清单

| 差距项 | 当前实现 | GDD要求 | 优先级 |
|--------|----------|---------|--------|
| **兵力单位制** | 0-100百分比 | 0-20锡兵 | 🔴 高 |
| **定价模型** | 简单需求修正 | 三因子(Alpha/Beta/Gamma) | 🔴 高 |
| **流通盘系统** | MarketInventory | Float实时变动 | 🔴 高 |
| **将军默认意图** | 无，需玩家分配 | AI自动生成，玩家可干涉 | 🔴 高 |
| **强化/篡改机制** | 无 | 消耗现货干涉将军 | 🔴 高 |
| **接触/脱离判定** | LinePosition差值 | Gap = E - P - 1 | 🟡 中 |
| **后备役系统** | 无 | 全局资源池 | 🟡 中 |
| **维克多采购** | 直接分配指令 | 必须从市场购买 | 🟡 中 |
| **期货到期** | 可变回合数 | 固定3回合 | 🟢 低 |
| **胜利条件** | 简单检查 | 占领+保持1回合 | 🟢 低 |

---

# 三、开发任务规划

## 3.1 Phase 1: 数据模型重构 (基础层)

### Task 1.1: 兵力系统重构

**目标**：将兵力从0-100百分比改为0-20单位制

**修改文件**：
- `Assets/Scripts/WarBroker/Data/Models/GeneralData.cs`
- `Assets/Scripts/WarBroker/Data/Configs/GeneralConfig.cs`
- `Assets/Scripts/WarBroker/Data/Configs/GameBalanceConfig.cs`

**具体改动**：

```csharp
// GeneralData.cs - 修改字段类型和范围
public class GeneralData
{
    // 修改: Troops 从 int(0-100) 改为 int(0-20)
    public int HP;  // 重命名为HP，范围0-20
    
    // 新增: 状态阈值使用固定值
    public GeneralStatus GetStatus(GameBalanceConfig balance)
    {
        if (HP <= 0) return GeneralStatus.Routed;
        if (HP <= 5) return GeneralStatus.Critical;
        if (HP <= 10) return GeneralStatus.Wounded;
        if (HP <= 15) return GeneralStatus.Healthy;
        return GeneralStatus.FullStrength;  // 新增状态
    }
}

// GeneralConfig.cs - 修改初始值范围
public class GeneralConfigItem
{
    [Range(0, 20)]
    public int InitialHP = 16;  // 重命名，默认16
}

// GameBalanceConfig.cs - 添加兵力相关参数
public class GameBalanceConfig
{
    [Header("兵力系统")]
    public int MaxHP = 20;
    public int RespawnHP = 10;  // 复活后HP
    public int RespawnReserveCost = 10;  // 复活消耗后备役
}
```

### Task 1.2: 后备役系统

**目标**：添加全局后备役资源

**修改文件**：
- `Assets/Scripts/WarBroker/Data/Models/BattleData.cs`
- `Assets/Scripts/WarBroker/Data/Configs/CampaignConfig.cs`

**具体改动**：

```csharp
// BattleData.cs - 添加后备役字段
public class BattleData
{
    public int CurrentReserves;  // 当前后备役
    
    public void InitFromConfig(CampaignConfig config, ...)
    {
        CurrentReserves = config.InitialReserves;
        // ... existing code
    }
}

// CampaignConfig.cs - 添加后备役配置
public class CampaignConfig
{
    [Header("后备役")]
    public int InitialReserves = 60;
}
```

### Task 1.3: 流通盘系统

**目标**：将MarketInventory改为Float系统

**修改文件**：
- `Assets/Scripts/WarBroker/Data/Models/MarketData.cs`
- `Assets/Scripts/WarBroker/Data/Configs/OrderConfig.cs`

**具体改动**：

```csharp
// MarketData.cs - 重构为流通盘系统
public class MarketData
{
    // 重命名: MarketInventory -> Float
    public Dictionary<OrderType, int> Float;  // 流通盘
    public Dictionary<OrderType, int> InitialFloat;  // 初始流通盘(用于Gamma计算)
    
    public void InitFromConfig(OrderConfig config)
    {
        Float = new Dictionary<OrderType, int>();
        InitialFloat = new Dictionary<OrderType, int>();
        
        foreach (var item in config.Orders)
        {
            Float[item.OrderType] = item.InitialFloat;
            InitialFloat[item.OrderType] = item.InitialFloat;
        }
        // ... prices init
    }
}

// OrderConfig.cs - 添加流通盘配置
public class OrderConfigItem
{
    [Tooltip("初始流通盘")]
    public int InitialFloat = 50;
}
```

---

## 3.2 Phase 2: 核心机制实现 (逻辑层)

### Task 2.1: 三因子定价模型

**目标**：实现 Price = P_base × (1 + Alpha) × Beta × Gamma

**新建文件**：
- `Assets/Scripts/WarBroker/Systems/PricingEngine.cs`

**修改文件**：
- `Assets/Scripts/WarBroker/Systems/MarketSystem.cs`
- `Assets/Scripts/WarBroker/Data/Configs/GameBalanceConfig.cs`

**实现要点**：

```csharp
// PricingEngine.cs - 新建定价引擎
public class PricingEngine
{
    private GameBalanceConfig config;
    private CampaignRuntimeData data;
    
    /// <summary>
    /// 计算指定指令的当前价格
    /// </summary>
    public float CalculatePrice(OrderType type)
    {
        float basePrice = GetBasePrice(type);
        float alpha = CalculateAlpha(type);
        float beta = CalculateBeta(type);
        float gamma = CalculateGamma(type);
        
        return basePrice * (1 + alpha) * beta * gamma;
    }
    
    /// <summary>
    /// Alpha因子：基于接触状态和交战位置
    /// </summary>
    private float CalculateAlpha(OrderType type)
    {
        float totalAlpha = 0f;
        int laneCount = 0;
        
        foreach (var lane in data.Battle.Frontlines.Values)
        {
            var ally = GetAllyAtLane(lane.Position);
            var enemy = GetEnemyAtLane(lane.Position);
            if (ally == null || enemy == null) continue;
            
            int P = ally.GridPosition;  // 需要添加此字段
            int E = enemy.GridPosition;
            int gap = E - P - 1;
            
            float laneAlpha = 0f;
            
            // Step 1: 接触状态基础
            if (gap == 0) laneAlpha += 0.20f;  // 接触+20%
            
            // Step 2: 交战位置修正 (仅接触状态)
            if (gap == 0)
            {
                float center = (P + E) / 2f;
                laneAlpha += GetPositionModifier(type, center);
            }
            
            // Step 3: 临界修正
            if (P == 1 || E == 5) laneAlpha += 0.15f;
            if (ally.HP <= 5) laneAlpha += 0.10f;
            
            totalAlpha += laneAlpha;
            laneCount++;
        }
        
        return laneCount > 0 ? totalAlpha / laneCount : 0f;
    }
    
    /// <summary>
    /// Beta因子：市场情绪
    /// </summary>
    private float CalculateBeta(OrderType type)
    {
        float beta = 1f;
        
        // 动量效应
        float lastChange = GetPriceChangeRatio(type);
        if (lastChange > 0.10f) beta *= 1.2f;
        else if (lastChange < -0.10f) beta *= 0.8f;
        
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
        
        return beta;
    }
    
    /// <summary>
    /// Gamma因子：流动性
    /// </summary>
    private float CalculateGamma(OrderType type)
    {
        int initial = data.Market.InitialFloat[type];
        int current = Mathf.Max(data.Market.Float[type], 1);
        return (float)initial / current;
    }
}
```

### Task 2.2: 将军意图系统

**目标**：实现将军默认意图生成 + 玩家干涉机制

**新建文件**：
- `Assets/Scripts/WarBroker/Systems/IntentSystem.cs`

**修改文件**：
- `Assets/Scripts/WarBroker/Data/Models/GeneralData.cs`
- `Assets/Scripts/WarBroker/Systems/CampaignSystem.cs`

**实现要点**：

```csharp
// GeneralData.cs - 添加意图相关字段
public class GeneralData
{
    public OrderType? DefaultIntent;  // AI生成的默认意图
    public OrderType? FinalIntent;    // 最终执行的意图
    public IntentSource IntentSource; // 意图来源
    public int GridPosition;          // 战线位置 (1-5)
}

public enum IntentSource
{
    Default,    // 默认意图（灰色气泡）
    Reinforced, // 强化（金色气泡）
    Overridden  // 篡改（红色气泡）
}

// IntentSystem.cs - 新建意图系统
public class IntentSystem
{
    private GameBalanceConfig config;
    
    /// <summary>
    /// 为将军生成默认意图
    /// </summary>
    public OrderType GenerateDefaultIntent(GeneralData general)
    {
        var weights = CalculateWeights(general);
        return SelectByWeight(weights);
    }
    
    /// <summary>
    /// 计算意图权重
    /// FinalWeight = BaseWeight × HP_Modifier × Grid_Modifier
    /// </summary>
    private Dictionary<OrderType, float> CalculateWeights(GeneralData general)
    {
        var baseWeights = GetBaseWeights(general.Personality);
        var hpMod = GetHPModifier(general.Personality, general.HP);
        var gridMod = GetGridModifier(general.GridPosition);
        
        var final = new Dictionary<OrderType, float>();
        foreach (OrderType type in Enum.GetValues(typeof(OrderType)))
        {
            final[type] = baseWeights[type] * hpMod[type] * gridMod[type];
        }
        
        return final;
    }
    
    /// <summary>
    /// 基础权重（性格决定）
    /// </summary>
    private Dictionary<OrderType, float> GetBaseWeights(GeneralPersonality p)
    {
        return p switch
        {
            GeneralPersonality.Fanatic => new() { {OrderType.ATK, 70}, {OrderType.DEF, 20}, {OrderType.RET, 10} },
            GeneralPersonality.Conservative => new() { {OrderType.ATK, 20}, {OrderType.DEF, 60}, {OrderType.RET, 20} },
            GeneralPersonality.Opportunist => new() { {OrderType.ATK, 33}, {OrderType.DEF, 33}, {OrderType.RET, 34} },
            _ => new() { {OrderType.ATK, 33}, {OrderType.DEF, 33}, {OrderType.RET, 34} }
        };
    }
    
    /// <summary>
    /// 尝试强化将军意图
    /// </summary>
    public bool TryReinforce(GeneralData general, OrderType orderType, PlayerData player)
    {
        if (general.DefaultIntent != orderType) return false;
        if (player.Inventory[orderType] < 1) return false;
        
        player.Inventory[orderType] -= 1;
        general.FinalIntent = orderType;
        general.IntentSource = IntentSource.Reinforced;
        general.Trust += 5;
        general.Morale += 5;
        
        return true;
    }
    
    /// <summary>
    /// 尝试篡改将军意图
    /// </summary>
    public bool TryOverride(GeneralData general, OrderType orderType, PlayerData player)
    {
        if (general.DefaultIntent == orderType) return false;  // 同类型应该用强化
        if (player.Inventory[orderType] < 3) return false;
        
        player.Inventory[orderType] -= 3;
        general.FinalIntent = orderType;
        general.IntentSource = IntentSource.Overridden;
        general.Trust -= 15;
        general.Morale -= 5;
        
        return true;
    }
}
```

### Task 2.3: 接触/脱离状态判定

**目标**：实现 Gap = E - P - 1 判定

**修改文件**：
- `Assets/Scripts/WarBroker/Systems/BattleSystem.cs`
- `Assets/Scripts/WarBroker/Data/Models/BattleData.cs`

**实现要点**：

```csharp
// BattleSystem.cs - 修改战斗结算逻辑
public class BattleSystem
{
    /// <summary>
    /// 判断接触状态
    /// </summary>
    private bool IsEngaged(int allyPos, int enemyPos)
    {
        // Gap = E - P - 1
        // Gap > 0 = 脱离, Gap == 0 = 接触
        int gap = enemyPos - allyPos - 1;
        return gap == 0;
    }
    
    /// <summary>
    /// 处理单条战线的战斗/移动
    /// </summary>
    private BattleResult ProcessLane(GeneralData ally, GeneralData enemy, 
        OrderType allyOrder, OrderType enemyOrder)
    {
        int gap = enemy.GridPosition - ally.GridPosition - 1;
        
        if (gap > 0)
        {
            // 脱离状态：指令用于移动
            return ProcessDisengaged(ally, enemy, allyOrder, enemyOrder);
        }
        else
        {
            // 接触状态：指令用于战斗
            return ProcessEngaged(ally, enemy, allyOrder, enemyOrder);
        }
    }
    
    /// <summary>
    /// 脱离状态处理
    /// </summary>
    private BattleResult ProcessDisengaged(...)
    {
        var result = new BattleResult();
        
        // ATK = 推进, DEF = 驻扎, RET = 后撤+回血
        switch (allyOrder)
        {
            case OrderType.ATK:
                ally.GridPosition++;  // P++
                break;
            case OrderType.DEF:
                // 不动
                break;
            case OrderType.RET:
                ally.GridPosition--;  // P--
                ally.HP = Mathf.Min(ally.HP + 1, 20);
                data.Battle.CurrentReserves--;  // 消耗后备役
                break;
        }
        
        // 敌方同理...
        
        return result;
    }
    
    /// <summary>
    /// 接触状态处理（对抗表）
    /// </summary>
    private BattleResult ProcessEngaged(...)
    {
        // 使用GDD v5.0的对抗表
        var outcome = GetCombatOutcome(allyOrder, enemyOrder);
        
        ally.HP = Mathf.Clamp(ally.HP + outcome.AllyHPChange, 0, 20);
        enemy.HP = Mathf.Clamp(enemy.HP + outcome.EnemyHPChange, 0, 20);
        
        // 战线移动
        if (outcome.AllyAdvance) ally.GridPosition++;
        if (outcome.AllyRetreat) ally.GridPosition--;
        if (outcome.EnemyAdvance) enemy.GridPosition--;
        if (outcome.EnemyRetreat) enemy.GridPosition++;
        
        // 消耗后备役（RET回血时）
        if (outcome.AllyHPChange > 0)
            data.Battle.CurrentReserves -= outcome.AllyHPChange;
            
        return result;
    }
}
```

### Task 2.4: 维克多采购系统

**目标**：维克多必须从市场购买指令

**修改文件**：
- `Assets/Scripts/WarBroker/Systems/CampaignSystem.cs`

**实现要点**：

```csharp
// CampaignSystem.cs - 重构维克多AI
private Dictionary<string, OrderType> ExecuteVictorAI()
{
    var orders = new Dictionary<string, OrderType>();
    var intentSystem = new IntentSystem();
    
    foreach (var general in Data.Battle.EnemyGenerals)
    {
        if (general.HP <= 0) continue;
        
        // Step 1: 生成默认意图
        var defaultIntent = intentSystem.GenerateDefaultIntent(general);
        general.DefaultIntent = defaultIntent;
        
        // Step 2: 尝试强化（需要购买1份指令）
        bool canReinforce = TryVictorPurchase(defaultIntent, 1);
        
        if (canReinforce)
        {
            general.FinalIntent = defaultIntent;
            general.IntentSource = IntentSource.Reinforced;
        }
        else
        {
            // 买不到/买不起：只能执行默认意图，无强化
            general.FinalIntent = defaultIntent;
            general.IntentSource = IntentSource.Default;
        }
        
        orders[general.GeneralId] = general.FinalIntent.Value;
    }
    
    return orders;
}

/// <summary>
/// 维克多尝试从市场购买
/// </summary>
private bool TryVictorPurchase(OrderType type, int quantity)
{
    var market = Data.Market;
    
    // 检查流通盘
    if (market.Float[type] < quantity) return false;
    
    // 检查资金
    float price = pricingEngine.CalculatePrice(type);
    float totalCost = price * quantity * (1 + balanceConfig.CommissionRate);
    
    if (Data.VictorCash < totalCost) return false;
    
    // 执行购买
    Data.VictorCash -= totalCost;
    market.Float[type] -= quantity;
    
    // 触发价格更新
    eventService.SendMessage((EventID)WarBrokerEventID.OnPriceUpdate, type, null);
    
    return true;
}
```

---

## 3.3 Phase 3: UI实现 (表现层)

### Task 3.1: GeneralPanel (将军管理面板)

**目标**：显示将军状态，支持拖拽干涉

**新建文件**：
- `Assets/Scripts/WarBroker/UI/GeneralPanel.cs`
- `Assets/Scripts/WarBroker/UI/GeneralCard.cs`

**实现要点**：

```csharp
// GeneralPanel.cs
public class GeneralPanel : WindowBase
{
    public Transform generalCardContainer;
    public List<GeneralCard> generalCards;
    
    public override void OnShow()
    {
        RefreshAllCards();
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnTurnStart);
    }
    
    private void RefreshAllCards()
    {
        var data = campaignSystem.Data;
        foreach (var general in data.Battle.AllyGenerals)
        {
            var card = GetOrCreateCard(general.GeneralId);
            card.Refresh(general);
        }
    }
}

// GeneralCard.cs - 单个将军卡片
public class GeneralCard : MonoBehaviour, IDropHandler
{
    public Text txtName, txtHP, txtMorale;
    public Image imgIntentBubble;
    public Text txtIntent;
    
    private GeneralData generalData;
    
    public void Refresh(GeneralData data)
    {
        generalData = data;
        txtName.text = data.Name;
        txtHP.text = $"HP: {data.HP}/20";
        txtMorale.text = $"士气: {data.Morale}";
        
        // 意图气泡
        if (data.DefaultIntent.HasValue)
        {
            txtIntent.text = data.DefaultIntent.Value.ToString();
            imgIntentBubble.color = data.IntentSource switch
            {
                IntentSource.Default => Color.gray,
                IntentSource.Reinforced => Color.yellow,
                IntentSource.Overridden => Color.red,
                _ => Color.gray
            };
        }
    }
    
    // 拖拽干涉
    public void OnDrop(PointerEventData eventData)
    {
        var draggedOrder = eventData.pointerDrag.GetComponent<OrderDragItem>();
        if (draggedOrder == null) return;
        
        var orderType = draggedOrder.OrderType;
        var intentSystem = GameRoot.Instance.GetSystem<IntentSystem>();
        
        if (orderType == generalData.DefaultIntent)
        {
            // 同类型 = 强化
            intentSystem.TryReinforce(generalData, orderType, playerData);
        }
        else
        {
            // 异类型 = 篡改
            intentSystem.TryOverride(generalData, orderType, playerData);
        }
        
        Refresh(generalData);
    }
}
```

### Task 3.2: BattlefieldPanel (战场面板)

**目标**：可视化三条战线和单位位置

**新建文件**：
- `Assets/Scripts/WarBroker/UI/BattlefieldPanel.cs`
- `Assets/Scripts/WarBroker/UI/LaneDisplay.cs`

### Task 3.3: MarketPanel改造

**目标**：显示三因子分解、流通盘状态

**修改文件**：
- `Assets/Scripts/WarBroker/UI/MarketPanel.cs`

---

## 3.4 Phase 4: 流程完善

### Task 4.1: 回合流程对齐

**目标**：实现GDD v5.0的五阶段流程

### Task 4.2: 胜利条件

**目标**：Grid 1/5 占领 + 保持1回合

### Task 4.3: 单元测试更新

**目标**：更新测试用例以匹配新机制

---

# 四、文件修改清单

## 4.1 需要修改的现有文件

| 文件路径 | 修改类型 | 优先级 |
|----------|----------|--------|
| `Data/Models/GeneralData.cs` | 重构 | P0 |
| `Data/Models/MarketData.cs` | 重构 | P0 |
| `Data/Models/BattleData.cs` | 扩展 | P0 |
| `Data/Configs/GameBalanceConfig.cs` | 扩展 | P0 |
| `Data/Configs/GeneralConfig.cs` | 修改 | P0 |
| `Data/Configs/OrderConfig.cs` | 扩展 | P0 |
| `Data/Configs/CampaignConfig.cs` | 扩展 | P0 |
| `Systems/MarketSystem.cs` | 重构 | P1 |
| `Systems/BattleSystem.cs` | 重构 | P1 |
| `Systems/CampaignSystem.cs` | 重构 | P1 |
| `UI/MarketPanel.cs` | 重构 | P2 |
| `UI/GameplayWindow.cs` | 扩展 | P2 |

## 4.2 需要新建的文件

| 文件路径 | 用途 | 优先级 |
|----------|------|--------|
| `Systems/PricingEngine.cs` | 三因子定价 | P1 |
| `Systems/IntentSystem.cs` | 将军意图 | P1 |
| `UI/GeneralPanel.cs` | 将军管理 | P2 |
| `UI/GeneralCard.cs` | 将军卡片 | P2 |
| `UI/BattlefieldPanel.cs` | 战场显示 | P2 |
| `UI/LaneDisplay.cs` | 战线显示 | P2 |
| `UI/OrderDragItem.cs` | 指令拖拽 | P2 |

---

# 五、配置参数对照表

## 5.1 GameBalanceConfig 需要添加的字段

```csharp
[Header("=== 兵力系统 ===")]
public int MaxHP = 20;
public int RespawnHP = 10;
public int RespawnReserveCost = 10;
public int BaseRecoveryHP = 2;
public int BaseRecoveryCost = 2;

[Header("=== 将军状态阈值 ===")]
public int FullStrengthThreshold = 16;
public int HealthyThreshold = 11;
public int WoundedThreshold = 6;
public int CriticalThreshold = 1;

[Header("=== 信任度系统 ===")]
public int ReinforceBonus = 5;
public int OverridePenalty = 15;
public int VictoryBonus = 5;
public int DefeatPenalty = 5;
public float DisobeyChanceLow = 0.2f;     // Trust 30-49
public float DisobeyChanceVeryLow = 0.5f; // Trust 0-29

[Header("=== 士气系统 ===")]
public int HighMoraleDamageBonus = 1;
public int HighMoraleDamageReduction = 1;
public int LowMoraleDamagePenalty = 1;
public int VeryLowMoraleDamageTaken = 1;
public float SpontaneousRoutChance = 0.05f;

[Header("=== 三因子定价 ===")]
public float EngagedAlphaBonus = 0.20f;
public float CrisisAlphaBonus = 0.15f;
public float MomentumThreshold = 0.10f;
public float MomentumMultiplier = 1.2f;
public int PanicReserveThreshold = 20;
public float PanicAtkMultiplier = 0.6f;
public float PanicDefMultiplier = 1.5f;
public float PanicRetMultiplier = 2.0f;

[Header("=== 期货系统 ===")]
public int FuturesExpiryTurns = 3;
public float FuturesMarginRate = 0.20f;
public float FuturesMaintenanceRate = 0.30f;
public float LiquidationThreshold = 0.14f;
```

## 5.2 CampaignConfig 需要添加的字段

```csharp
[Header("=== 后备役 ===")]
public int InitialReserves = 60;

[Header("=== 胜利条件 ===")]
public float TargetNetWorth = 2000f;
public int OccupationHoldTurns = 1;
```

---

# 六、事件ID扩展

需要在 `WarBrokerEventID.cs` 中添加：

```csharp
public enum WarBrokerEventID
{
    // 现有事件...
    
    // 新增事件
    OnIntentGenerated = 2001,    // 将军意图生成
    OnIntentReinforced = 2002,   // 意图被强化
    OnIntentOverridden = 2003,   // 意图被篡改
    OnDisobey = 2004,            // 将军抗命
    OnReservesChanged = 2005,    // 后备役变化
    OnReservesDepeleted = 2006,  // 后备役耗尽
    OnFloatChanged = 2007,       // 流通盘变化
    OnVictorPurchase = 2008,     // 维克多采购
    OnVictorMonopolized = 2009,  // 维克多被垄断
}
```

---

# 七、开发顺序建议

## 推荐执行顺序

```
Week 1: Phase 1 (数据模型)
  ├─ Task 1.1 兵力系统重构
  ├─ Task 1.2 后备役系统
  └─ Task 1.3 流通盘系统

Week 2: Phase 2.1-2.2 (核心机制 A)
  ├─ Task 2.1 三因子定价模型
  └─ Task 2.2 将军意图系统

Week 3: Phase 2.3-2.4 (核心机制 B)
  ├─ Task 2.3 接触/脱离判定
  └─ Task 2.4 维克多采购系统

Week 4: Phase 3 (UI)
  ├─ Task 3.1 GeneralPanel
  ├─ Task 3.2 BattlefieldPanel
  └─ Task 3.3 MarketPanel改造

Week 5: Phase 4 (流程完善)
  ├─ Task 4.1 回合流程
  ├─ Task 4.2 胜利条件
  └─ Task 4.3 测试更新
```

---

# 八、验收标准

## 8.1 Phase 1 验收

- [ ] 将军HP显示为0-20，20个锡兵模型概念
- [ ] 后备役在BattleData中正确初始化
- [ ] 流通盘在MarketData中正确初始化
- [ ] 所有配置文件字段完整

## 8.2 Phase 2 验收

- [ ] 价格随交战状态变化（接触时上涨）
- [ ] 价格随流通盘变化（买光时暴涨）
- [ ] 将军在回合开始自动生成灰色意图
- [ ] 玩家可以用1份同类现货强化
- [ ] 玩家可以用3份异类现货篡改
- [ ] 维克多必须购买才能干涉敌方将军
- [ ] 玩家买光流通盘后维克多无法购买

## 8.3 Phase 3 验收

- [ ] GeneralPanel显示所有己方将军
- [ ] 意图气泡颜色正确（灰/金/红）
- [ ] 可以拖拽指令到将军卡片上
- [ ] BattlefieldPanel显示三条战线
- [ ] 单位位置正确显示

## 8.4 Phase 4 验收

- [ ] 回合流程符合GDD五阶段
- [ ] Grid 1被占领+保持1回合触发战败
- [ ] Grid 5被占领+保持1回合触发胜利
- [ ] 所有单元测试通过

---

**文档版本**：v1.0  
**最后更新**：基于代码分析生成  
**配套文档**：《炮火与K线系统设计策划案 v5.0》
