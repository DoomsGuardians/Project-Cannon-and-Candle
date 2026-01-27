# 战争掮客 (The War Broker) 开发指导文档

## 基于 Levity Framework 的原型开发指南

---

# 第一章：项目架构

## 1.1 技术栈

- **引擎**：Unity 2022.3+ (LTS)
- **框架**：Levity Framework
- **语言**：C#
- **依赖**：DOTween, Odin Inspector (可选)

## 1.2 项目目录结构

```
Assets/
├── Scripts/
│   ├── Core/                    # Levity Framework (不修改)
│   │
│   ├── WarBroker/               # 游戏业务代码
│   │   ├── Data/                # 数据定义
│   │   │   ├── Configs/         # ScriptableObject 配置
│   │   │   ├── Models/          # 数据模型类
│   │   │   └── Enums/           # 枚举定义
│   │   │
│   │   ├── Systems/             # 全局系统 (继承 ILogic)
│   │   │   ├── MarketSystem.cs
│   │   │   ├── BattleSystem.cs
│   │   │   └── CampaignSystem.cs
│   │   │
│   │   ├── Managers/            # 场景管理器 (继承 ManagerBase)
│   │   │   ├── GameplayManager.cs
│   │   │   └── TurnManager.cs
│   │   │
│   │   ├── UI/                  # UI 窗口 (继承 WindowBase)
│   │   │   ├── Windows/
│   │   │   └── Components/
│   │   │
│   │   ├── Entities/            # 游戏实体
│   │   │   ├── General.cs
│   │   │   └── Victor.cs
│   │   │
│   │   └── GameModes/           # 游戏模式
│   │       └── BattleGameMode.cs
│   │
│   └── Generated/               # 自动生成代码
│
├── Resources/
│   ├── Configs/                 # 配置文件
│   ├── Prefabs/                 # 预制体
│   └── UI/                      # UI 预制体
│
└── Scenes/
    ├── Boot.unity               # 启动场景 (含 GameRoot)
    └── Battle.unity             # 战斗场景
```

## 1.3 架构层级

```
┌─────────────────────────────────────────────────────────┐
│                    GameRoot (单例)                       │
├─────────────────────────────────────────────────────────┤
│  Services (基础服务 - 框架提供)                          │
│  ├── EventService    事件系统                           │
│  ├── TimerService    定时器                             │
│  ├── UIService       UI管理                             │
│  ├── DataService     存档                               │
│  └── ResService      资源加载                           │
├─────────────────────────────────────────────────────────┤
│  Systems (全局系统 - 需要实现)                           │
│  ├── MarketSystem    市场系统 (价格/交易/期货)           │
│  ├── BattleSystem    战场系统 (战线/战斗结算)            │
│  └── CampaignSystem  战役系统 (回合/胜负/历史记录)       │
├─────────────────────────────────────────────────────────┤
│  Managers (场景管理器 - 需要实现)                        │
│  ├── GameplayManager 游戏流程控制                       │
│  └── TurnManager     回合流程控制                       │
├─────────────────────────────────────────────────────────┤
│  GameMode                                               │
│  └── BattleGameMode  战斗模式                           │
└─────────────────────────────────────────────────────────┘
```

---

# 第二章：配置系统设计

## 2.1 枚举定义

```csharp
// Assets/Scripts/WarBroker/Data/Enums/GameEnums.cs

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
    Fanatic,     // 狂热型
    Conservative, // 保守型
    Opportunist  // 投机型
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

## 2.2 全局游戏参数配置

```csharp
// Assets/Scripts/WarBroker/Data/Configs/GameBalanceConfig.cs

using System;
using UnityEngine;

/// <summary>
/// 全局游戏平衡参数配置
/// </summary>
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "WarBroker/GameBalanceConfig")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("===== 市场参数 =====")]
    
    [Tooltip("手续费率")]
    [Range(0f, 0.1f)]
    public float CommissionRate = 0.02f;
    
    [Tooltip("价格冲击率 (每张)")]
    [Range(0f, 0.1f)]
    public float PriceImpactRate = 0.02f;
    
    [Tooltip("仓储费 (每张/回合)")]
    public float StorageCostPerUnit = 3f;
    
    [Tooltip("价格随机波动范围")]
    [Range(0f, 0.2f)]
    public float PriceRandomRange = 0.05f;
    
    [Header("===== 银行参数 =====")]
    
    [Tooltip("银行利率 (每回合)")]
    [Range(0f, 0.2f)]
    public float BankInterestRate = 0.05f;
    
    [Tooltip("借款额度系数 (净资产倍数)")]
    [Range(1f, 3f)]
    public float LoanRatio = 1.5f;
    
    [Header("===== 期货参数 =====")]
    
    [Tooltip("期货保证金率")]
    [Range(0.1f, 0.5f)]
    public float FuturesMarginRate = 0.2f;
    
    [Tooltip("强平线 (亏损百分比)")]
    [Range(0.5f, 0.95f)]
    public float ForceLiquidationRate = 0.8f;
    
    [Tooltip("期货最长期限 (回合)")]
    [Range(1, 5)]
    public int MaxFuturesDuration = 3;
    
    [Header("===== 战斗参数 =====")]
    
    [Tooltip("随机修正最小值")]
    [Range(0.5f, 1f)]
    public float RandomModifierMin = 0.75f;
    
    [Tooltip("随机修正最大值")]
    [Range(1f, 1.5f)]
    public float RandomModifierMax = 1.25f;
    
    [Tooltip("暴击概率")]
    [Range(0f, 0.2f)]
    public float CritChance = 0.05f;
    
    [Tooltip("暴击倍率")]
    [Range(1f, 3f)]
    public float CritMultiplier = 1.5f;
    
    [Tooltip("失误概率")]
    [Range(0f, 0.2f)]
    public float FumbleChance = 0.05f;
    
    [Tooltip("失误倍率")]
    [Range(0f, 1f)]
    public float FumbleMultiplier = 0.5f;
    
    [Header("===== 将军参数 =====")]
    
    [Tooltip("基础补员 (每回合)")]
    [Range(0, 30)]
    public int BaseReinforcement = 10;
    
    [Tooltip("溃败兵力阈值")]
    [Range(0, 50)]
    public int RoutTroopThreshold = 20;
    
    [Tooltip("溃败综合评分阈值")]
    [Range(0, 50)]
    public int RoutScoreThreshold = 30;
    
    [Tooltip("重整所需回合")]
    [Range(1, 5)]
    public int ReorganizeTurns = 3;
    
    [Tooltip("抗命起始信任度阈值")]
    [Range(0, 100)]
    public int DisobeyTrustThreshold = 50;
    
    [Tooltip("低信任度抗命概率 (信任度30-49)")]
    [Range(0f, 1f)]
    public float DisobeyChanceLow = 0.2f;
    
    [Tooltip("极低信任度抗命概率 (信任度0-29)")]
    [Range(0f, 1f)]
    public float DisobeyChanceVeryLow = 0.5f;
    
    [Header("===== 战线联动参数 =====")]
    
    [Tooltip("侧翼支援战斗力加成")]
    [Range(0f, 0.5f)]
    public float FlankSupportBonus = 0.1f;
    
    [Tooltip("侧翼威胁战斗力惩罚")]
    [Range(0f, 0.5f)]
    public float FlankThreatPenalty = 0.1f;
    
    [Tooltip("侧翼威胁士气惩罚")]
    [Range(0, 20)]
    public int FlankThreatMoralePenalty = 5;
    
    [Tooltip("半包围战斗力加成")]
    [Range(0f, 0.5f)]
    public float SurroundBonus = 0.25f;
    
    [Tooltip("被包围士气惩罚")]
    [Range(0, 30)]
    public int SurroundedMoralePenalty = 15;
    
    [Header("===== 审计参数 =====")]
    
    [Tooltip("军需总部补足指令审计值")]
    [Range(0, 50)]
    public int AuditSupplyShortage = 30;
    
    [Tooltip("将军溃败审计值")]
    [Range(0, 50)]
    public int AuditGeneralRouted = 20;
    
    [Tooltip("审计失败阈值")]
    [Range(50, 200)]
    public int AuditFailureThreshold = 100;
    
    [Header("===== 随机事件参数 =====")]
    
    [Tooltip("随机事件触发概率")]
    [Range(0f, 1f)]
    public float RandomEventChance = 0.4f;
}
```

## 2.3 指令配置

```csharp
// Assets/Scripts/WarBroker/Data/Configs/OrderConfig.cs

using System;
using UnityEngine;

/// <summary>
/// 单个指令配置
/// </summary>
[Serializable]
public class OrderConfigItem
{
    public OrderType OrderType;
    
    [Tooltip("基础价格")]
    public float BasePrice;
    
    [Tooltip("每回合产能")]
    public int ProductionPerTurn;
    
    [Tooltip("初始市场库存")]
    public int InitialStock;
}

/// <summary>
/// 指令配置表
/// </summary>
[CreateAssetMenu(fileName = "OrderConfig", menuName = "WarBroker/OrderConfig")]
public class OrderConfig : ScriptableObject
{
    public OrderConfigItem[] Orders = new OrderConfigItem[]
    {
        new() { OrderType = OrderType.ATK, BasePrice = 40f, ProductionPerTurn = 3, InitialStock = 10 },
        new() { OrderType = OrderType.DEF, BasePrice = 35f, ProductionPerTurn = 3, InitialStock = 10 },
        new() { OrderType = OrderType.RET, BasePrice = 25f, ProductionPerTurn = 2, InitialStock = 8 }
    };
    
    public OrderConfigItem GetConfig(OrderType type)
    {
        foreach (var item in Orders)
        {
            if (item.OrderType == type) return item;
        }
        return null;
    }
}
```

## 2.4 技能配置

```csharp
// Assets/Scripts/WarBroker/Data/Configs/SkillConfig.cs

using System;
using UnityEngine;

/// <summary>
/// 技能配置
/// </summary>
[Serializable]
public class SkillConfigItem
{
    public string SkillId;
    public string SkillName;
    
    [TextArea(2, 4)]
    public string Description;
    
    [Tooltip("所属性格类型")]
    public GeneralPersonality Personality;
    
    [Header("触发条件")]
    public OrderType? TriggerOrder;
    
    [Tooltip("兵力阈值 (0=不检查)")]
    public int TroopThreshold;
    
    [Tooltip("士气阈值 (0=不检查)")]
    public int MoraleThreshold;
    
    [Tooltip("战线位置阈值 (0=不检查)")]
    public int FrontlineThreshold;
    
    [Tooltip("是否需要连续回合")]
    public bool RequireConsecutive;
    
    [Header("效果数值")]
    [Tooltip("额外战线移动")]
    public int BonusLineMovement;
    
    [Tooltip("战斗力加成%")]
    public float CombatBonus;
    
    [Tooltip("己方额外兵力变化")]
    public int AllyTroopChange;
    
    [Tooltip("敌方额外兵力变化")]
    public int EnemyTroopChange;
    
    [Tooltip("抗命改为此指令 (null=不抗命)")]
    public OrderType? DisobeyToOrder;
    
    [Tooltip("抗命概率")]
    [Range(0f, 1f)]
    public float DisobeyChance;
}

/// <summary>
/// 技能配置表
/// </summary>
[CreateAssetMenu(fileName = "SkillConfig", menuName = "WarBroker/SkillConfig")]
public class SkillConfig : ScriptableObject
{
    public SkillConfigItem[] Skills;
    
    public SkillConfigItem GetSkill(string skillId)
    {
        foreach (var skill in Skills)
        {
            if (skill.SkillId == skillId) return skill;
        }
        return null;
    }
    
    public SkillConfigItem[] GetSkillsByPersonality(GeneralPersonality personality)
    {
        return Array.FindAll(Skills, s => s.Personality == personality);
    }
}
```

## 2.5 将军配置

```csharp
// Assets/Scripts/WarBroker/Data/Configs/GeneralConfig.cs

using System;
using UnityEngine;

/// <summary>
/// 将军配置
/// </summary>
[Serializable]
public class GeneralConfigItem
{
    [Header("基础信息")]
    public string GeneralId;
    public string Name;
    
    [TextArea(2, 3)]
    public string Biography;
    
    public Sprite Portrait;
    
    [Header("属性")]
    public GeneralPersonality Personality;
    
    [Tooltip("初始兵力")]
    [Range(0, 100)]
    public int InitialTroops = 80;
    
    [Tooltip("初始信任度")]
    [Range(0, 100)]
    public int InitialTrust = 50;
    
    [Tooltip("初始士气")]
    [Range(0, 100)]
    public int InitialMorale = 60;
    
    [Header("技能")]
    [Tooltip("技能ID列表 (从SkillConfig中选择)")]
    public string[] SkillIds;
    
    [Header("出价系数")]
    [Tooltip("ATK出价系数")]
    public float AtkBidModifier = 1f;
    
    [Tooltip("DEF出价系数")]
    public float DefBidModifier = 1f;
    
    [Tooltip("RET出价系数")]
    public float RetBidModifier = 1f;
}

/// <summary>
/// 将军配置表
/// </summary>
[CreateAssetMenu(fileName = "GeneralConfig", menuName = "WarBroker/GeneralConfig")]
public class GeneralConfig : ScriptableObject
{
    [Header("己方将军")]
    public GeneralConfigItem[] AllyGenerals;
    
    [Header("敌方将军")]
    public GeneralConfigItem[] EnemyGenerals;
    
    public GeneralConfigItem GetGeneral(string generalId)
    {
        foreach (var g in AllyGenerals)
        {
            if (g.GeneralId == generalId) return g;
        }
        foreach (var g in EnemyGenerals)
        {
            if (g.GeneralId == generalId) return g;
        }
        return null;
    }
}
```

## 2.6 战役配置

```csharp
// Assets/Scripts/WarBroker/Data/Configs/CampaignConfig.cs

using System;
using UnityEngine;

/// <summary>
/// 战役配置
/// </summary>
[CreateAssetMenu(fileName = "Campaign_XXX", menuName = "WarBroker/CampaignConfig")]
public class CampaignConfig : ScriptableObject
{
    [Header("===== 基础信息 =====")]
    public string CampaignId;
    public string CampaignName;
    
    [TextArea(3, 5)]
    public string Description;
    
    [Header("===== 回合设置 =====")]
    [Tooltip("最大回合数")]
    [Range(6, 60)]
    public int MaxTurns = 20;
    
    [Header("===== 玩家初始状态 =====")]
    [Tooltip("初始现金")]
    public float InitialCash = 500f;
    
    [Tooltip("初始ATK库存")]
    public int InitialAtkInventory = 2;
    
    [Tooltip("初始DEF库存")]
    public int InitialDefInventory = 2;
    
    [Tooltip("初始RET库存")]
    public int InitialRetInventory = 2;
    
    [Header("===== 战场设置 =====")]
    [Tooltip("初始战线位置 (1-5)")]
    [Range(1, 5)]
    public int InitialFrontlinePosition = 3;
    
    [Header("===== 将军配置引用 =====")]
    [Tooltip("本战役使用的将军配置")]
    public GeneralConfig GeneralConfig;
    
    [Tooltip("将军战线分配 (左翼/中军/右翼)")]
    public FrontlineAssignment[] AllyFrontlineAssignments;
    
    public FrontlineAssignment[] EnemyFrontlineAssignments;
    
    [Header("===== 维克多设置 =====")]
    [Tooltip("维克多初始现金")]
    public float VictorInitialCash = 500f;
    
    [Tooltip("维克多AI难度 (0-1)")]
    [Range(0f, 1f)]
    public float VictorDifficulty = 0.5f;
    
    [Header("===== 可用随机事件 =====")]
    public RandomEventConfig[] AvailableEvents;
}

/// <summary>
/// 战线分配
/// </summary>
[Serializable]
public class FrontlineAssignment
{
    public FrontlinePosition Position;
    public string GeneralId;
}

/// <summary>
/// 随机事件配置
/// </summary>
[Serializable]
public class RandomEventConfig
{
    public string EventId;
    public string EventName;
    
    [TextArea(2, 3)]
    public string Description;
    
    [Header("效果")]
    [Tooltip("产能修正%")]
    public float ProductionModifier;
    
    [Tooltip("需求修正 (ATK)")]
    public float AtkDemandModifier;
    
    [Tooltip("需求修正 (DEF)")]
    public float DefDemandModifier;
    
    [Tooltip("需求修正 (RET)")]
    public float RetDemandModifier;
    
    [Tooltip("所有将军兵力变化")]
    public int AllTroopChange;
    
    [Tooltip("随机将军信任度变化")]
    public int RandomTrustChange;
    
    [Tooltip("持续回合数")]
    public int Duration = 1;
}
```

## 2.7 配置资源路径

```csharp
// Assets/Scripts/WarBroker/Data/Configs/ConfigPaths.cs

/// <summary>
/// 配置资源路径常量
/// </summary>
public static class ConfigPaths
{
    public const string GAME_BALANCE = "Configs/GameBalanceConfig";
    public const string ORDER_CONFIG = "Configs/OrderConfig";
    public const string SKILL_CONFIG = "Configs/SkillConfig";
    public const string GENERAL_CONFIG = "Configs/GeneralConfig";
    public const string CAMPAIGN_PREFIX = "Configs/Campaigns/";
}
```

# 第三章：运行时数据模型

## 3.1 市场运行时数据

```csharp
// Assets/Scripts/WarBroker/Data/Models/MarketData.cs

using System;
using System.Collections.Generic;

/// <summary>市场运行时数据</summary>
[Serializable]
public class MarketData
{
    // 当前价格
    public Dictionary<OrderType, float> CurrentPrices;
    
    // 市场库存
    public Dictionary<OrderType, int> MarketInventory;
    
    // 价格历史 (回合 -> 类型 -> 价格)
    public List<Dictionary<OrderType, float>> PriceHistory;
    
    /// <summary>从配置初始化</summary>
    public void InitFromConfig(OrderConfig config)
    {
        CurrentPrices = new Dictionary<OrderType, float>();
        MarketInventory = new Dictionary<OrderType, int>();
        PriceHistory = new List<Dictionary<OrderType, float>>();
        
        foreach (var item in config.Orders)
        {
            CurrentPrices[item.OrderType] = item.BasePrice;
            MarketInventory[item.OrderType] = item.InitialStock;
        }
    }
}

/// <summary>期货合约</summary>
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
    
    public float CalculatePnL(float currentPrice)
    {
        float diff = currentPrice - OpenPrice;
        if (Direction == FuturesDirection.Short) diff = -diff;
        return diff * Quantity;
    }
}

/// <summary>玩家运行时数据</summary>
[Serializable]
public class PlayerData
{
    public float Cash;
    public Dictionary<OrderType, int> Inventory;
    public float BankDebt;
    public List<FuturesContract> FuturesPositions;
    public int AuditValue;
    
    /// <summary>从配置初始化</summary>
    public void InitFromConfig(CampaignConfig config)
    {
        Cash = config.InitialCash;
        Inventory = new Dictionary<OrderType, int>
        {
            { OrderType.ATK, config.InitialAtkInventory },
            { OrderType.DEF, config.InitialDefInventory },
            { OrderType.RET, config.InitialRetInventory }
        };
        BankDebt = 0f;
        FuturesPositions = new List<FuturesContract>();
        AuditValue = 0;
    }
    
    /// <summary>计算净资产</summary>
    public float CalculateNetWorth(MarketData market)
    {
        float inventoryValue = 0f;
        foreach (var kvp in Inventory)
        {
            inventoryValue += kvp.Value * market.CurrentPrices[kvp.Key];
        }
        
        float futuresPnL = 0f;
        foreach (var contract in FuturesPositions)
        {
            futuresPnL += contract.CalculatePnL(market.CurrentPrices[contract.TargetOrder]);
        }
        
        return Cash + inventoryValue + futuresPnL - BankDebt;
    }
}
```

## 3.2 将军运行时数据

```csharp
// Assets/Scripts/WarBroker/Data/Models/GeneralData.cs

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>将军运行时数据</summary>
[Serializable]
public class GeneralData
{
    // 配置引用
    public GeneralConfigItem Config { get; private set; }
    
    // 基础信息 (从配置读取)
    public string GeneralId => Config.GeneralId;
    public string Name => Config.Name;
    public GeneralPersonality Personality => Config.Personality;
    
    // 战线位置 (运行时分配)
    public FrontlinePosition Position;
    
    // 动态属性
    public int Troops;
    public int Trust;
    public int Morale;
    
    // 技能配置引用
    public List<SkillConfigItem> Skills;
    
    // 当前指令
    public OrderType? AssignedOrder;
    
    // 溃败重整倒计时
    public int ReorganizeTurns;
    
    // 连续指令追踪 (用于技能判定)
    public OrderType? LastOrder;
    public int ConsecutiveOrderCount;
    
    /// <summary>从配置初始化</summary>
    public void InitFromConfig(GeneralConfigItem config, SkillConfig skillConfig)
    {
        Config = config;
        Troops = config.InitialTroops;
        Trust = config.InitialTrust;
        Morale = config.InitialMorale;
        
        Skills = new List<SkillConfigItem>();
        foreach (var skillId in config.SkillIds)
        {
            var skill = skillConfig.GetSkill(skillId);
            if (skill != null) Skills.Add(skill);
        }
    }
    
    /// <summary>计算综合评分</summary>
    public float CalculateCompositeScore()
    {
        return Troops * 0.4f + Trust * 0.3f + Morale * 0.3f;
    }
    
    /// <summary>获取当前状态</summary>
    public GeneralStatus GetStatus(GameBalanceConfig balance)
    {
        if (Troops < balance.RoutTroopThreshold) return GeneralStatus.Routed;
        float score = CalculateCompositeScore();
        if (score < balance.RoutScoreThreshold) return GeneralStatus.Routed;
        if (score < 50) return GeneralStatus.Critical;
        if (score < 70) return GeneralStatus.Wounded;
        return GeneralStatus.Healthy;
    }
    
    /// <summary>计算对指令的出价</summary>
    public float CalculateBid(OrderType orderType, float marketPrice, GameBalanceConfig balance)
    {
        float baseValue = marketPrice * 0.3f;
        
        // 从配置读取性格系数
        float personalityMod = orderType switch
        {
            OrderType.ATK => Config.AtkBidModifier,
            OrderType.DEF => Config.DefBidModifier,
            OrderType.RET => Config.RetBidModifier,
            _ => 1f
        };
        
        // 状态系数
        float statusMod = GetStatusBidModifier(orderType, balance);
        
        // 信任度系数
        float trustMod = 0.5f + Trust / 100f;
        
        return baseValue * personalityMod * statusMod * trustMod;
    }
    
    private float GetStatusBidModifier(OrderType orderType, GameBalanceConfig balance)
    {
        var status = GetStatus(balance);
        return (status, orderType) switch
        {
            (GeneralStatus.Healthy, _) => 1.0f,
            (GeneralStatus.Wounded, OrderType.ATK) => 0.7f,
            (GeneralStatus.Wounded, OrderType.DEF) => 1.5f,
            (GeneralStatus.Wounded, OrderType.RET) => 1.3f,
            (GeneralStatus.Critical, OrderType.ATK) => 0.3f,
            (GeneralStatus.Critical, OrderType.DEF) => 2.5f,
            (GeneralStatus.Critical, OrderType.RET) => 2.0f,
            _ => 1.0f
        };
    }
}
```

## 3.3 战场运行时数据

```csharp
// Assets/Scripts/WarBroker/Data/Models/BattleData.cs

using System;
using System.Collections.Generic;

/// <summary>战线运行时数据</summary>
[Serializable]
public class FrontlineData
{
    public FrontlinePosition Position;
    public int LinePosition; // 1-5
    public int StagnantTurns;
    
    public void InitFromConfig(CampaignConfig config)
    {
        LinePosition = config.InitialFrontlinePosition;
        StagnantTurns = 0;
    }
}

/// <summary>战斗结果</summary>
[Serializable]
public class BattleResult
{
    public FrontlinePosition Position;
    public OrderType AllyOrder;
    public OrderType EnemyOrder;
    public int LineMovement;
    public int AllyTroopChange;
    public int EnemyTroopChange;
    public bool SkillTriggered;
    public string SkillName;
    public string Description;
    public bool WasCrit;
    public bool WasFumble;
}

/// <summary>战场运行时数据</summary>
[Serializable]
public class BattleData
{
    public Dictionary<FrontlinePosition, FrontlineData> Frontlines;
    public List<GeneralData> AllyGenerals;
    public List<GeneralData> EnemyGenerals;
    
    /// <summary>从配置初始化</summary>
    public void InitFromConfig(CampaignConfig campaignConfig, SkillConfig skillConfig)
    {
        // 初始化战线
        Frontlines = new Dictionary<FrontlinePosition, FrontlineData>();
        foreach (FrontlinePosition pos in Enum.GetValues(typeof(FrontlinePosition)))
        {
            var frontline = new FrontlineData { Position = pos };
            frontline.InitFromConfig(campaignConfig);
            Frontlines[pos] = frontline;
        }
        
        // 初始化己方将军
        AllyGenerals = new List<GeneralData>();
        foreach (var assignment in campaignConfig.AllyFrontlineAssignments)
        {
            var configItem = campaignConfig.GeneralConfig.GetGeneral(assignment.GeneralId);
            if (configItem != null)
            {
                var general = new GeneralData();
                general.InitFromConfig(configItem, skillConfig);
                general.Position = assignment.Position;
                AllyGenerals.Add(general);
            }
        }
        
        // 初始化敌方将军
        EnemyGenerals = new List<GeneralData>();
        foreach (var assignment in campaignConfig.EnemyFrontlineAssignments)
        {
            var configItem = campaignConfig.GeneralConfig.GetGeneral(assignment.GeneralId);
            if (configItem != null)
            {
                var general = new GeneralData();
                general.InitFromConfig(configItem, skillConfig);
                general.Position = assignment.Position;
                EnemyGenerals.Add(general);
            }
        }
    }
}
```

## 3.4 战役运行时数据

```csharp
// Assets/Scripts/WarBroker/Data/Models/CampaignData.cs

using System;
using System.Collections.Generic;

/// <summary>回合历史记录</summary>
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

/// <summary>交易记录</summary>
[Serializable]
public class TransactionRecord
{
    public enum TransactionType { Buy, Sell, FuturesOpen, FuturesClose, Borrow, Repay }
    
    public TransactionType Type;
    public OrderType? OrderType;
    public int Quantity;
    public float Price;
    public float TotalAmount;
    public string Description;
}

/// <summary>战役运行时数据</summary>
[Serializable]
public class CampaignRuntimeData
{
    // 配置引用
    public CampaignConfig Config { get; private set; }
    
    // 回合状态
    public int CurrentTurn;
    public int MaxTurns => Config.MaxTurns;
    public TurnPhase CurrentPhase;
    
    // 子系统数据
    public PlayerData Player;
    public MarketData Market;
    public BattleData Battle;
    
    // 维克多数据
    public float VictorCash;
    public Dictionary<OrderType, int> VictorInventory;
    
    // 历史记录
    public List<TurnRecord> TurnHistory;
    
    // 随机事件
    public RandomEventConfig ActiveEvent;
    public int EventRemainingTurns;
    
    /// <summary>从配置初始化</summary>
    public void InitFromConfig(CampaignConfig campaignConfig, OrderConfig orderConfig, SkillConfig skillConfig)
    {
        Config = campaignConfig;
        CurrentTurn = 1;
        CurrentPhase = TurnPhase.TurnStart;
        
        // 初始化玩家
        Player = new PlayerData();
        Player.InitFromConfig(campaignConfig);
        
        // 初始化市场
        Market = new MarketData();
        Market.InitFromConfig(orderConfig);
        
        // 初始化战场
        Battle = new BattleData();
        Battle.InitFromConfig(campaignConfig, skillConfig);
        
        // 初始化维克多
        VictorCash = campaignConfig.VictorInitialCash;
        VictorInventory = new Dictionary<OrderType, int>
        {
            { OrderType.ATK, 2 },
            { OrderType.DEF, 2 },
            { OrderType.RET, 2 }
        };
        
        TurnHistory = new List<TurnRecord>();
    }
}
```

---

# 第四章：核心系统实现

## 4.1 事件定义

```csharp
// Assets/Scripts/WarBroker/Data/Enums/GameEvents.cs

/// <summary>游戏事件ID (添加到 EventService 的 EventID 枚举中)</summary>
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

## 4.2 MarketSystem 市场系统

```csharp
// Assets/Scripts/WarBroker/Systems/MarketSystem.cs

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 市场系统：管理价格计算、交易执行、期货结算
/// </summary>
public class MarketSystem : ILogic
{
    private EventService eventService;
    private ResService resService;
    
    // 配置引用
    private GameBalanceConfig balanceConfig;
    private OrderConfig orderConfig;
    
    // 运行时数据引用
    private CampaignRuntimeData campaignData;
    
    // 期货合约ID生成
    private int nextContractId = 1;
    
    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;
        
        // 加载配置
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        orderConfig = resService.LoadResource<OrderConfig>(ConfigPaths.ORDER_CONFIG);
    }
    
    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }
    
    /// <summary>设置运行时数据引用</summary>
    public void SetRuntimeData(CampaignRuntimeData data)
    {
        campaignData = data;
    }
    
    #region 现货交易
    
    /// <summary>买入指令</summary>
    public bool BuyOrder(OrderType orderType, int quantity, out float totalCost)
    {
        totalCost = 0f;
        var market = campaignData.Market;
        var player = campaignData.Player;
        
        if (market.MarketInventory[orderType] < quantity)
        {
            Debug.LogWarning($"市场库存不足: {orderType}");
            return false;
        }
        
        // 计算成本 (含价格冲击)
        float currentPrice = market.CurrentPrices[orderType];
        for (int i = 0; i < quantity; i++)
        {
            float price = currentPrice * (1 + balanceConfig.PriceImpactRate * i);
            float commission = price * balanceConfig.CommissionRate;
            totalCost += price + commission;
        }
        
        if (player.Cash < totalCost)
        {
            Debug.LogWarning($"资金不足: 需要{totalCost}, 当前{player.Cash}");
            return false;
        }
        
        // 执行交易
        player.Cash -= totalCost;
        player.Inventory[orderType] += quantity;
        market.MarketInventory[orderType] -= quantity;
        market.CurrentPrices[orderType] *= (1 + balanceConfig.PriceImpactRate * quantity);
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnTradeExecuted, 
            new TransactionRecord
            {
                Type = TransactionRecord.TransactionType.Buy,
                OrderType = orderType,
                Quantity = quantity,
                Price = market.CurrentPrices[orderType],
                TotalAmount = totalCost
            });
        
        return true;
    }
    
    /// <summary>卖出指令</summary>
    public bool SellOrder(OrderType orderType, int quantity, out float totalRevenue)
    {
        totalRevenue = 0f;
        var market = campaignData.Market;
        var player = campaignData.Player;
        
        if (player.Inventory[orderType] < quantity)
        {
            Debug.LogWarning($"库存不足: {orderType}");
            return false;
        }
        
        float currentPrice = market.CurrentPrices[orderType];
        for (int i = 0; i < quantity; i++)
        {
            float price = currentPrice * (1 - balanceConfig.PriceImpactRate * i);
            float commission = price * balanceConfig.CommissionRate;
            totalRevenue += price - commission;
        }
        
        player.Cash += totalRevenue;
        player.Inventory[orderType] -= quantity;
        market.MarketInventory[orderType] += quantity;
        market.CurrentPrices[orderType] *= (1 - balanceConfig.PriceImpactRate * quantity);
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnTradeExecuted,
            new TransactionRecord
            {
                Type = TransactionRecord.TransactionType.Sell,
                OrderType = orderType,
                Quantity = quantity,
                Price = market.CurrentPrices[orderType],
                TotalAmount = totalRevenue
            });
        
        return true;
    }
    
    #endregion
    
    #region 期货交易
    
    /// <summary>开期货仓位</summary>
    public bool OpenFutures(OrderType orderType, FuturesDirection direction, 
        int quantity, int expirationTurns, out FuturesContract contract)
    {
        contract = null;
        var market = campaignData.Market;
        var player = campaignData.Player;
        
        if (expirationTurns > balanceConfig.MaxFuturesDuration)
        {
            Debug.LogWarning($"期货期限超过最大值: {balanceConfig.MaxFuturesDuration}");
            return false;
        }
        
        float openPrice = market.CurrentPrices[orderType];
        float margin = openPrice * quantity * balanceConfig.FuturesMarginRate;
        
        if (player.Cash < margin)
        {
            Debug.LogWarning($"保证金不足: 需要{margin}");
            return false;
        }
        
        contract = new FuturesContract
        {
            ContractId = nextContractId++,
            TargetOrder = orderType,
            Direction = direction,
            OpenPrice = openPrice,
            Quantity = quantity,
            ExpirationTurn = campaignData.CurrentTurn + expirationTurns,
            Margin = margin
        };
        
        player.Cash -= margin;
        player.FuturesPositions.Add(contract);
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnFuturesOpened, contract);
        
        return true;
    }
    
    /// <summary>平仓</summary>
    public bool CloseFutures(int contractId, out float pnl)
    {
        pnl = 0f;
        var player = campaignData.Player;
        var market = campaignData.Market;
        
        var contract = player.FuturesPositions.Find(c => c.ContractId == contractId);
        if (contract == null) return false;
        
        float currentPrice = market.CurrentPrices[contract.TargetOrder];
        pnl = contract.CalculatePnL(currentPrice);
        
        player.Cash += contract.Margin + pnl;
        player.FuturesPositions.Remove(contract);
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnFuturesClosed, contract, pnl);
        
        return true;
    }
    
    /// <summary>检查强平</summary>
    public void CheckForceLiquidation()
    {
        var player = campaignData.Player;
        var market = campaignData.Market;
        var toClose = new List<int>();
        
        foreach (var contract in player.FuturesPositions)
        {
            float pnl = contract.CalculatePnL(market.CurrentPrices[contract.TargetOrder]);
            float remainingMargin = contract.Margin + pnl;
            
            if (remainingMargin < contract.Margin * (1 - balanceConfig.ForceLiquidationRate))
            {
                toClose.Add(contract.ContractId);
            }
        }
        
        foreach (var id in toClose)
        {
            CloseFutures(id, out float pnl);
            eventService.SendMessage((EventID)WarBrokerEventID.OnForceLiquidation, id, pnl);
        }
    }
    
    /// <summary>到期结算</summary>
    public void SettleExpiredFutures()
    {
        var player = campaignData.Player;
        var toSettle = player.FuturesPositions
            .FindAll(c => c.ExpirationTurn <= campaignData.CurrentTurn);
        
        foreach (var contract in toSettle)
        {
            CloseFutures(contract.ContractId, out _);
        }
    }
    
    #endregion
    
    #region 银行借贷
    
    /// <summary>计算可借额度</summary>
    public float CalculateLoanLimit()
    {
        float netWorth = campaignData.Player.CalculateNetWorth(campaignData.Market);
        return Mathf.Max(0, netWorth * balanceConfig.LoanRatio - campaignData.Player.BankDebt);
    }
    
    /// <summary>借款</summary>
    public bool Borrow(float amount)
    {
        if (amount > CalculateLoanLimit()) return false;
        
        campaignData.Player.Cash += amount;
        campaignData.Player.BankDebt += amount;
        
        return true;
    }
    
    /// <summary>还款</summary>
    public bool Repay(float amount)
    {
        var player = campaignData.Player;
        amount = Mathf.Min(amount, player.BankDebt, player.Cash);
        
        player.Cash -= amount;
        player.BankDebt -= amount;
        
        return true;
    }
    
    /// <summary>计算利息</summary>
    public void ApplyInterest()
    {
        campaignData.Player.BankDebt *= (1 + balanceConfig.BankInterestRate);
    }
    
    #endregion
    
    #region 持有成本
    
    /// <summary>扣除仓储费</summary>
    public void ApplyStorageCost()
    {
        var player = campaignData.Player;
        int totalInventory = 0;
        foreach (var kvp in player.Inventory)
        {
            totalInventory += kvp.Value;
        }
        player.Cash -= totalInventory * balanceConfig.StorageCostPerUnit;
    }
    
    #endregion
    
    #region 价格更新
    
    /// <summary>更新市场价格</summary>
    public void UpdatePrices(Dictionary<OrderType, float> demandModifiers)
    {
        var market = campaignData.Market;
        
        // 添加产能
        foreach (var item in orderConfig.Orders)
        {
            market.MarketInventory[item.OrderType] += item.ProductionPerTurn;
        }
        
        // 根据供需调整价格
        foreach (OrderType orderType in Enum.GetValues(typeof(OrderType)))
        {
            float demand = demandModifiers.GetValueOrDefault(orderType, 1f);
            float supply = market.MarketInventory[orderType];
            float supplyDemandRatio = demand / Mathf.Max(1, supply);
            
            float randomFactor = 1f + UnityEngine.Random.Range(
                -balanceConfig.PriceRandomRange, 
                balanceConfig.PriceRandomRange);
            
            market.CurrentPrices[orderType] *= supplyDemandRatio * randomFactor;
        }
        
        market.PriceHistory.Add(new Dictionary<OrderType, float>(market.CurrentPrices));
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnPriceUpdate);
    }
    
    #endregion
    
    #region 市场情报
    
    /// <summary>获取价格影响因素</summary>
    public List<MarketIntelItem> GetMarketIntelligence(OrderType orderType)
    {
        var intel = new List<MarketIntelItem>();
        
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            var status = general.GetStatus(balanceConfig);
            if (status == GeneralStatus.Wounded || status == GeneralStatus.Critical)
            {
                if (orderType == OrderType.DEF || orderType == OrderType.RET)
                {
                    intel.Add(new MarketIntelItem
                    {
                        IsPositive = true,
                        Description = $"{general.Name}状态恶化，{orderType}需求上升"
                    });
                }
            }
        }
        
        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (frontline.LinePosition <= 2 && orderType == OrderType.DEF)
            {
                intel.Add(new MarketIntelItem
                {
                    IsPositive = true,
                    Description = $"{frontline.Position}战线劣势，DEF需求上升"
                });
            }
        }
        
        return intel;
    }
    
    #endregion
}

/// <summary>市场情报项</summary>
public class MarketIntelItem
{
    public bool IsPositive;
    public string Description;
}
```

## 4.3 BattleSystem 战场系统

```csharp
// Assets/Scripts/WarBroker/Systems/BattleSystem.cs

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战场系统：管理战线、战斗结算、将军状态
/// </summary>
public class BattleSystem : ILogic
{
    private EventService eventService;
    private ResService resService;
    
    // 配置引用
    private GameBalanceConfig balanceConfig;
    private SkillConfig skillConfig;
    
    // 运行时数据引用
    private CampaignRuntimeData campaignData;
    
    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;
        
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        skillConfig = resService.LoadResource<SkillConfig>(ConfigPaths.SKILL_CONFIG);
    }
    
    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }
    
    public void SetRuntimeData(CampaignRuntimeData data)
    {
        campaignData = data;
    }
    
    #region 战斗结算
    
    /// <summary>结算所有战线战斗</summary>
    public List<BattleResult> ResolveBattles(Dictionary<string, OrderType> enemyOrders)
    {
        var results = new List<BattleResult>();
        
        foreach (FrontlinePosition pos in Enum.GetValues(typeof(FrontlinePosition)))
        {
            var allyGeneral = campaignData.Battle.AllyGenerals.Find(g => g.Position == pos);
            var enemyGeneral = campaignData.Battle.EnemyGenerals.Find(g => g.Position == pos);
            
            if (allyGeneral == null || enemyGeneral == null) continue;
            if (allyGeneral.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;
            
            var result = ResolveSingleBattle(allyGeneral, enemyGeneral, 
                allyGeneral.AssignedOrder ?? OrderType.DEF,
                enemyOrders.GetValueOrDefault(enemyGeneral.GeneralId, OrderType.DEF));
            
            results.Add(result);
        }
        
        return results;
    }
    
    /// <summary>结算单场战斗</summary>
    private BattleResult ResolveSingleBattle(GeneralData ally, GeneralData enemy,
        OrderType allyOrder, OrderType enemyOrder)
    {
        var result = new BattleResult
        {
            Position = ally.Position,
            AllyOrder = allyOrder,
            EnemyOrder = enemyOrder
        };
        
        // 检查抗命
        if (CheckDisobey(ally, allyOrder))
        {
            allyOrder = GetDisobeyOrder(ally);
            result.AllyOrder = allyOrder;
            result.Description = $"{ally.Name}抗命，自行执行{allyOrder}";
        }
        
        // 更新连续指令追踪
        UpdateConsecutiveOrder(ally, allyOrder);
        
        // 计算战斗力
        float allyCombat = CalculateCombatPower(ally, ally.Position);
        float enemyCombat = CalculateCombatPower(enemy, ally.Position);
        
        // 应用随机修正
        (allyCombat, result.WasCrit, result.WasFumble) = ApplyRandomModifier(allyCombat);
        (enemyCombat, _, _) = ApplyRandomModifier(enemyCombat);
        
        // 根据指令对抗表确定结果
        (result.LineMovement, result.AllyTroopChange, result.EnemyTroopChange) = 
            GetCombatOutcome(allyOrder, enemyOrder, allyCombat, enemyCombat);
        
        // 检查技能触发
        CheckSkillTrigger(ally, allyOrder, result);
        
        // 应用结果
        ApplyBattleResult(ally, enemy, result);
        
        return result;
    }
    
    private void UpdateConsecutiveOrder(GeneralData general, OrderType order)
    {
        if (general.LastOrder == order)
        {
            general.ConsecutiveOrderCount++;
        }
        else
        {
            general.ConsecutiveOrderCount = 1;
        }
        general.LastOrder = order;
    }
    
    /// <summary>计算战斗力</summary>
    private float CalculateCombatPower(GeneralData general, FrontlinePosition pos)
    {
        float basePower = 100f;
        
        // 兵力修正
        float troopMod = general.Troops switch
        {
            >= 80 => 1.0f,
            >= 50 => 0.9f,
            >= 20 => 0.7f,
            _ => 0.5f
        };
        
        // 士气修正
        float moraleMod = general.Morale switch
        {
            >= 80 => 1.1f,
            >= 50 => 1.0f,
            >= 30 => 0.9f,
            _ => 0.8f
        };
        
        // 战线联动修正
        float flankMod = CalculateFlankModifier(pos);
        
        return basePower * troopMod * moraleMod * flankMod;
    }
    
    /// <summary>计算战线联动修正</summary>
    private float CalculateFlankModifier(FrontlinePosition pos)
    {
        var frontlines = campaignData.Battle.Frontlines;
        float modifier = 1f;
        
        if (pos == FrontlinePosition.Center)
        {
            int leftPos = frontlines[FrontlinePosition.Left].LinePosition;
            int rightPos = frontlines[FrontlinePosition.Right].LinePosition;
            
            if (leftPos >= 4 && rightPos >= 4) 
                modifier += balanceConfig.SurroundBonus;
            else if (leftPos <= 2 && rightPos <= 2) 
                modifier -= balanceConfig.SurroundedMoralePenalty / 100f;
        }
        else
        {
            int centerPos = frontlines[FrontlinePosition.Center].LinePosition;
            int myPos = frontlines[pos].LinePosition;
            
            if (centerPos >= myPos + 1) 
                modifier += balanceConfig.FlankSupportBonus;
            else if (centerPos <= myPos - 1) 
                modifier -= balanceConfig.FlankThreatPenalty;
        }
        
        return modifier;
    }
    
    /// <summary>应用随机修正</summary>
    private (float power, bool isCrit, bool isFumble) ApplyRandomModifier(float basePower)
    {
        float roll = UnityEngine.Random.value;
        float modifier = UnityEngine.Random.Range(
            balanceConfig.RandomModifierMin, 
            balanceConfig.RandomModifierMax);
        
        if (roll < balanceConfig.CritChance)
        {
            return (basePower * modifier * balanceConfig.CritMultiplier, true, false);
        }
        if (roll < balanceConfig.CritChance + balanceConfig.FumbleChance)
        {
            return (basePower * modifier * balanceConfig.FumbleMultiplier, false, true);
        }
        
        return (basePower * modifier, false, false);
    }
    
    /// <summary>根据指令对抗表获取结果</summary>
    private (int movement, int allyLoss, int enemyLoss) GetCombatOutcome(
        OrderType ally, OrderType enemy, float allyCombat, float enemyCombat)
    {
        var baseOutcome = (ally, enemy) switch
        {
            (OrderType.ATK, OrderType.ATK) => (0, -15, -15),
            (OrderType.ATK, OrderType.DEF) => (0, -10, -10),
            (OrderType.ATK, OrderType.RET) => (1, 0, 0),
            (OrderType.DEF, OrderType.ATK) => (0, -10, -10),
            (OrderType.DEF, OrderType.DEF) => (0, 0, 0),
            (OrderType.DEF, OrderType.RET) => (0, 0, 0),
            (OrderType.RET, OrderType.ATK) => (-1, 0, 0),
            (OrderType.RET, OrderType.DEF) => (0, 0, 0),
            (OrderType.RET, OrderType.RET) => (0, 0, 0),
            _ => (0, 0, 0)
        };
        
        float combatRatio = allyCombat / Mathf.Max(1, enemyCombat);
        int allyLoss = Mathf.RoundToInt(baseOutcome.Item2 * (2f - combatRatio));
        int enemyLoss = Mathf.RoundToInt(baseOutcome.Item3 * combatRatio);
        
        return (baseOutcome.Item1, allyLoss, enemyLoss);
    }
    
    /// <summary>应用战斗结果</summary>
    private void ApplyBattleResult(GeneralData ally, GeneralData enemy, BattleResult result)
    {
        var frontline = campaignData.Battle.Frontlines[result.Position];
        int newPos = Mathf.Clamp(frontline.LinePosition + result.LineMovement, 1, 5);
        
        if (newPos == frontline.LinePosition)
            frontline.StagnantTurns++;
        else
            frontline.StagnantTurns = 0;
        
        frontline.LinePosition = newPos;
        
        ally.Troops = Mathf.Clamp(ally.Troops + result.AllyTroopChange, 0, 100);
        enemy.Troops = Mathf.Clamp(enemy.Troops + result.EnemyTroopChange, 0, 100);
        
        if (result.LineMovement > 0)
        {
            ally.Morale = Mathf.Clamp(ally.Morale + 10, 0, 100);
            enemy.Morale = Mathf.Clamp(enemy.Morale - 10, 0, 100);
        }
        else if (result.LineMovement < 0)
        {
            ally.Morale = Mathf.Clamp(ally.Morale - 10, 0, 100);
            enemy.Morale = Mathf.Clamp(enemy.Morale + 10, 0, 100);
        }
        
        if (ally.GetStatus(balanceConfig) == GeneralStatus.Routed)
        {
            ally.ReorganizeTurns = balanceConfig.ReorganizeTurns;
            campaignData.Player.AuditValue += balanceConfig.AuditGeneralRouted;
            eventService.SendMessage((EventID)WarBrokerEventID.OnGeneralRouted, ally);
        }
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnBattleResult, result);
    }
    
    #endregion
    
    #region 抗命检查
    
    private bool CheckDisobey(GeneralData general, OrderType order)
    {
        float bid = general.CalculateBid(order, 40f, balanceConfig);
        if (bid >= 0) return false;
        
        float disobeyChance = general.Trust switch
        {
            < 30 => balanceConfig.DisobeyChanceVeryLow,
            < 50 => balanceConfig.DisobeyChanceLow,
            _ => 0f
        };
        
        return UnityEngine.Random.value < disobeyChance;
    }
    
    private OrderType GetDisobeyOrder(GeneralData general)
    {
        // 检查技能是否有抗命指定
        foreach (var skill in general.Skills)
        {
            if (skill.DisobeyToOrder.HasValue && skill.DisobeyChance > 0)
            {
                if (UnityEngine.Random.value < skill.DisobeyChance)
                {
                    return skill.DisobeyToOrder.Value;
                }
            }
        }
        
        return general.Personality switch
        {
            GeneralPersonality.Fanatic => OrderType.ATK,
            GeneralPersonality.Conservative => OrderType.DEF,
            _ => OrderType.DEF
        };
    }
    
    #endregion
    
    #region 技能检查
    
    private void CheckSkillTrigger(GeneralData general, OrderType order, BattleResult result)
    {
        foreach (var skill in general.Skills)
        {
            if (!CheckSkillCondition(general, order, result, skill)) continue;
            
            ApplySkillEffect(skill, general, result);
            result.SkillTriggered = true;
            result.SkillName = skill.SkillName;
            eventService.SendMessage((EventID)WarBrokerEventID.OnSkillTriggered, general, skill);
        }
    }
    
    private bool CheckSkillCondition(GeneralData general, OrderType order, 
        BattleResult result, SkillConfigItem skill)
    {
        // 检查指令要求
        if (skill.TriggerOrder.HasValue && skill.TriggerOrder.Value != order)
            return false;
        
        // 检查兵力阈值
        if (skill.TroopThreshold > 0 && general.Troops >= skill.TroopThreshold)
            return false;
        
        // 检查士气阈值
        if (skill.MoraleThreshold > 0 && general.Morale < skill.MoraleThreshold)
            return false;
        
        // 检查战线位置
        if (skill.FrontlineThreshold > 0)
        {
            int linePos = campaignData.Battle.Frontlines[general.Position].LinePosition;
            if (linePos < skill.FrontlineThreshold)
                return false;
        }
        
        // 检查连续回合
        if (skill.RequireConsecutive && general.ConsecutiveOrderCount < 2)
            return false;
        
        // 检查胜利条件 (用于"先登"类技能)
        if (skill.SkillId.Contains("charge") && result.LineMovement <= 0)
            return false;
        
        return true;
    }
    
    private void ApplySkillEffect(SkillConfigItem skill, GeneralData general, BattleResult result)
    {
        result.LineMovement += skill.BonusLineMovement;
        result.AllyTroopChange += skill.AllyTroopChange;
        result.EnemyTroopChange += skill.EnemyTroopChange;
        
        // 战斗力加成在战斗计算中已处理
    }
    
    #endregion
    
    #region 补员
    
    public void ApplyReinforcements()
    {
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed)
            {
                general.ReorganizeTurns--;
                continue;
            }
            
            int linePos = campaignData.Battle.Frontlines[general.Position].LinePosition;
            float positionMod = linePos switch
            {
                <= 2 => 1f,
                3 => 0.5f,
                _ => 0f
            };
            
            int reinforcement = Mathf.RoundToInt(balanceConfig.BaseReinforcement * positionMod);
            general.Troops = Mathf.Min(100, general.Troops + reinforcement);
        }
    }
    
    #endregion
    
    #region 胜负检查
    
    public bool CheckVictory()
    {
        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (frontline.LinePosition < 5) return false;
        }
        return true;
    }
    
    public bool CheckDefeat()
    {
        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (frontline.LinePosition > 1) return false;
        }
        return true;
    }
    
    public bool CheckNegotiation()
    {
        foreach (var frontline in campaignData.Battle.Frontlines.Values)
        {
            if (frontline.StagnantTurns < 3) return false;
        }
        return true;
    }
    
    #endregion
}
```

## 4.4 CampaignSystem 战役系统

```csharp
// Assets/Scripts/WarBroker/Systems/CampaignSystem.cs

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战役系统：管理回合流程、历史记录、胜负判定
/// </summary>
public class CampaignSystem : ILogic
{
    private EventService eventService;
    private ResService resService;
    private MarketSystem marketSystem;
    private BattleSystem battleSystem;
    
    // 配置引用
    private GameBalanceConfig balanceConfig;
    private OrderConfig orderConfig;
    private SkillConfig skillConfig;
    
    // 运行时数据
    public CampaignRuntimeData Data { get; private set; }
    
    public void OnInit()
    {
        eventService = GameRoot.Instance.eventService;
        resService = GameRoot.Instance.resService;
        
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        orderConfig = resService.LoadResource<OrderConfig>(ConfigPaths.ORDER_CONFIG);
        skillConfig = resService.LoadResource<SkillConfig>(ConfigPaths.SKILL_CONFIG);
    }
    
    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }
    
    /// <summary>初始化新战役</summary>
    public void InitNewCampaign(string campaignId, MarketSystem market, BattleSystem battle)
    {
        marketSystem = market;
        battleSystem = battle;
        
        // 加载战役配置
        var campaignConfig = resService.LoadResource<CampaignConfig>(
            ConfigPaths.CAMPAIGN_PREFIX + campaignId);
        
        if (campaignConfig == null)
        {
            Debug.LogError($"Campaign config not found: {campaignId}");
            return;
        }
        
        // 初始化运行时数据
        Data = new CampaignRuntimeData();
        Data.InitFromConfig(campaignConfig, orderConfig, skillConfig);
        
        // 设置系统引用
        marketSystem.SetRuntimeData(Data);
        battleSystem.SetRuntimeData(Data);
    }
    
    #region 回合流程
    
    public void StartTurn()
    {
        Data.CurrentPhase = TurnPhase.TurnStart;
        
        TryTriggerRandomEvent();
        
        foreach (var general in Data.Battle.AllyGenerals)
        {
            general.AssignedOrder = null;
        }
        
        Data.CurrentPhase = TurnPhase.PlayerAction;
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnTurnStart, Data.CurrentTurn);
    }
    
    public void EndTurn()
    {
        foreach (var general in Data.Battle.AllyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;
            
            if (general.AssignedOrder == null)
            {
                Debug.LogWarning($"将军{general.Name}未分配指令");
                return;
            }
        }
        
        Data.CurrentPhase = TurnPhase.TurnEnd;
        
        RecordTurn();
        ExecuteTurnSettlement();
        
        if (CheckGameEnd()) return;
        
        Data.CurrentTurn++;
        StartTurn();
    }
    
    private void ExecuteTurnSettlement()
    {
        var victorOrders = ExecuteVictorAI();
        
        Data.CurrentPhase = TurnPhase.BattleResolve;
        var battleResults = battleSystem.ResolveBattles(victorOrders);
        
        battleSystem.ApplyReinforcements();
        marketSystem.ApplyInterest();
        marketSystem.ApplyStorageCost();
        marketSystem.SettleExpiredFutures();
        marketSystem.CheckForceLiquidation();
        
        Data.CurrentPhase = TurnPhase.MarketUpdate;
        var demandModifiers = CalculateDemandModifiers();
        marketSystem.UpdatePrices(demandModifiers);
        
        // 更新事件持续时间
        if (Data.ActiveEvent != null)
        {
            Data.EventRemainingTurns--;
            if (Data.EventRemainingTurns <= 0)
            {
                Data.ActiveEvent = null;
            }
        }
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnTurnEnd, Data.CurrentTurn);
    }
    
    private Dictionary<OrderType, float> CalculateDemandModifiers()
    {
        var modifiers = new Dictionary<OrderType, float>
        {
            { OrderType.ATK, 1f },
            { OrderType.DEF, 1f },
            { OrderType.RET, 1f }
        };
        
        foreach (var general in Data.Battle.AllyGenerals)
        {
            var status = general.GetStatus(balanceConfig);
            if (status == GeneralStatus.Wounded)
            {
                modifiers[OrderType.DEF] += 0.2f;
                modifiers[OrderType.RET] += 0.1f;
            }
            else if (status == GeneralStatus.Critical)
            {
                modifiers[OrderType.DEF] += 0.5f;
                modifiers[OrderType.RET] += 0.3f;
            }
        }
        
        foreach (var frontline in Data.Battle.Frontlines.Values)
        {
            if (frontline.LinePosition <= 2)
            {
                modifiers[OrderType.DEF] += 0.15f;
            }
            else if (frontline.LinePosition >= 4)
            {
                modifiers[OrderType.ATK] += 0.1f;
            }
        }
        
        // 应用随机事件修正
        if (Data.ActiveEvent != null)
        {
            modifiers[OrderType.ATK] += Data.ActiveEvent.AtkDemandModifier;
            modifiers[OrderType.DEF] += Data.ActiveEvent.DefDemandModifier;
            modifiers[OrderType.RET] += Data.ActiveEvent.RetDemandModifier;
        }
        
        return modifiers;
    }
    
    #endregion
    
    #region 维克多AI
    
    private Dictionary<string, OrderType> ExecuteVictorAI()
    {
        var orders = new Dictionary<string, OrderType>();
        float difficulty = Data.Config.VictorDifficulty;
        
        foreach (var general in Data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed) continue;
            
            OrderType order;
            
            // 根据难度决定AI智能程度
            if (UnityEngine.Random.value > difficulty)
            {
                // 低难度：随机或按性格
                order = general.Personality switch
                {
                    GeneralPersonality.Fanatic => OrderType.ATK,
                    GeneralPersonality.Conservative => OrderType.DEF,
                    _ => (OrderType)UnityEngine.Random.Range(0, 3)
                };
            }
            else
            {
                // 高难度：根据战局判断
                var frontline = Data.Battle.Frontlines[general.Position];
                order = frontline.LinePosition switch
                {
                    <= 2 => OrderType.ATK,  // 优势时进攻
                    >= 4 => OrderType.DEF,  // 劣势时防守
                    _ => general.Troops > 50 ? OrderType.ATK : OrderType.DEF
                };
            }
            
            orders[general.GeneralId] = order;
            general.AssignedOrder = order;
        }
        
        return orders;
    }
    
    #endregion
    
    #region 随机事件
    
    private void TryTriggerRandomEvent()
    {
        if (Data.ActiveEvent != null) return; // 已有事件
        if (UnityEngine.Random.value > balanceConfig.RandomEventChance) return;
        
        var availableEvents = Data.Config.AvailableEvents;
        if (availableEvents == null || availableEvents.Length == 0) return;
        
        var selectedEvent = availableEvents[UnityEngine.Random.Range(0, availableEvents.Length)];
        Data.ActiveEvent = selectedEvent;
        Data.EventRemainingTurns = selectedEvent.Duration;
        
        // 应用即时效果
        if (selectedEvent.AllTroopChange != 0)
        {
            foreach (var general in Data.Battle.AllyGenerals)
            {
                general.Troops = Mathf.Clamp(general.Troops + selectedEvent.AllTroopChange, 0, 100);
            }
            foreach (var general in Data.Battle.EnemyGenerals)
            {
                general.Troops = Mathf.Clamp(general.Troops + selectedEvent.AllTroopChange, 0, 100);
            }
        }
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnRandomEvent, selectedEvent);
    }
    
    #endregion
    
    #region 历史记录
    
    private void RecordTurn()
    {
        var record = new TurnRecord
        {
            TurnNumber = Data.CurrentTurn,
            OrderAssignments = new Dictionary<string, OrderType>(),
            Transactions = new List<TransactionRecord>(),
            BattleResults = new List<BattleResult>(),
            PriceSnapshot = new Dictionary<OrderType, float>(Data.Market.CurrentPrices),
            PlayerNetWorth = Data.Player.CalculateNetWorth(Data.Market)
        };
        
        foreach (var general in Data.Battle.AllyGenerals)
        {
            if (general.AssignedOrder.HasValue)
            {
                record.OrderAssignments[general.GeneralId] = general.AssignedOrder.Value;
            }
        }
        
        Data.TurnHistory.Add(record);
    }
    
    #endregion
    
    #region 胜负判定
    
    private bool CheckGameEnd()
    {
        if (Data.Player.CalculateNetWorth(Data.Market) < 0)
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnDefeatConditionMet, "破产");
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false);
            return true;
        }
        
        if (Data.Player.AuditValue >= balanceConfig.AuditFailureThreshold)
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnDefeatConditionMet, "战争问责");
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false);
            return true;
        }
        
        if (battleSystem.CheckVictory())
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnVictoryConditionMet, "战争胜利");
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, true);
            return true;
        }
        
        if (battleSystem.CheckDefeat())
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnDefeatConditionMet, "战争失败");
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false);
            return true;
        }
        
        if (battleSystem.CheckNegotiation())
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnVictoryConditionMet, "谈判停战");
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, true);
            return true;
        }
        
        if (Data.CurrentTurn >= Data.MaxTurns)
        {
            eventService.SendMessage((EventID)WarBrokerEventID.OnDefeatConditionMet, "回合耗尽");
            eventService.SendMessage((EventID)WarBrokerEventID.OnGameEnd, false);
            return true;
        }
        
        return false;
    }
    
    #endregion
}
```

---

# 第五章：场景管理器

## 5.1 GameplayManager

```csharp
// Assets/Scripts/WarBroker/Managers/GameplayManager.cs

using UnityEngine;

/// <summary>
/// 游戏流程管理器：协调各系统，处理玩家输入
/// </summary>
public class GameplayManager : ManagerBase
{
    private MarketSystem marketSystem;
    private BattleSystem battleSystem;
    private CampaignSystem campaignSystem;
    private GameBalanceConfig balanceConfig;
    
    [Header("战役配置")]
    public string CampaignId = "Campaign_Tutorial";
    
    public override void OnAwake()
    {
        base.OnAwake();
        
        // 获取系统引用 (需要在GameRoot中注册)
        // marketSystem = GameRoot.Instance.GetSystem<MarketSystem>();
        // battleSystem = GameRoot.Instance.GetSystem<BattleSystem>();
        // campaignSystem = GameRoot.Instance.GetSystem<CampaignSystem>();
        
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
    }
    
    public override void OnShow()
    {
        // 初始化新战役
        campaignSystem.InitNewCampaign(CampaignId, marketSystem, battleSystem);
        campaignSystem.StartTurn();
        
        RegisterEvents();
        
        uiService.OpenWindow<GameplayWindow>(UILayer.Normal);
    }
    
    public override void OnExit()
    {
        UnregisterEvents();
    }
    
    private void RegisterEvents()
    {
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnTurnStart);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnTurnEnd);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnGameEnd, OnGameEnd);
    }
    
    private void UnregisterEvents()
    {
        eventService.RemoveEventListeningByTarget(this);
    }
    
    #region 玩家操作接口
    
    public bool BuyOrder(OrderType type, int quantity)
    {
        return marketSystem.BuyOrder(type, quantity, out _);
    }
    
    public bool SellOrder(OrderType type, int quantity)
    {
        return marketSystem.SellOrder(type, quantity, out _);
    }
    
    public bool OpenFutures(OrderType type, FuturesDirection dir, int qty, int turns)
    {
        return marketSystem.OpenFutures(type, dir, qty, turns, out _);
    }
    
    public bool CloseFutures(int contractId)
    {
        return marketSystem.CloseFutures(contractId, out _);
    }
    
    public bool Borrow(float amount)
    {
        return marketSystem.Borrow(amount);
    }
    
    public bool Repay(float amount)
    {
        return marketSystem.Repay(amount);
    }
    
    /// <summary>分配指令给将军</summary>
    public bool AssignOrder(string generalId, OrderType order)
    {
        var data = campaignSystem.Data;
        var general = data.Battle.AllyGenerals.Find(g => g.GeneralId == generalId);
        if (general == null) return false;
        
        if (data.Player.Inventory[order] <= 0)
        {
            Debug.LogWarning("库存不足");
            return false;
        }
        
        float bid = general.CalculateBid(order, data.Market.CurrentPrices[order], balanceConfig);
        
        if (bid > 0)
        {
            data.Player.Cash += bid;
        }
        else
        {
            if (data.Player.Cash < -bid)
            {
                Debug.LogWarning("资金不足支付负出价");
                return false;
            }
            data.Player.Cash += bid;
        }
        
        data.Player.Inventory[order]--;
        general.AssignedOrder = order;
        
        if (bid > 0) general.Trust = Mathf.Min(100, general.Trust + 5);
        else if (bid < 0) general.Trust = Mathf.Max(0, general.Trust - 10);
        
        eventService.SendMessage((EventID)WarBrokerEventID.OnOrderAssigned, generalId, order);
        
        return true;
    }
    
    public void EndTurn()
    {
        campaignSystem.EndTurn();
    }
    
    #endregion
    
    #region 数据访问接口
    
    public CampaignRuntimeData GetCampaignData() => campaignSystem.Data;
    public PlayerData GetPlayerData() => campaignSystem.Data.Player;
    public MarketData GetMarketData() => campaignSystem.Data.Market;
    public BattleData GetBattleData() => campaignSystem.Data.Battle;
    
    #endregion
    
    #region 事件处理
    
    private void OnTurnStart(object param1, object param2)
    {
        int turn = (int)param1;
        Debug.Log($"回合 {turn} 开始");
    }
    
    private void OnTurnEnd(object param1, object param2)
    {
        int turn = (int)param1;
        Debug.Log($"回合 {turn} 结束");
    }
    
    private void OnGameEnd(object param1, object param2)
    {
        bool isVictory = (bool)param1;
        Debug.Log($"游戏结束: {(isVictory ? "胜利" : "失败")}");
        
        uiService.OpenWindow<GameEndWindow>(UILayer.Popup);
    }
    
    #endregion
}
```

---

# 第六章：开发任务清单

## 6.1 阶段一：配置系统 (Week 1)

| 任务 | 优先级 | 说明 |
|------|--------|------|
| 导入 Levity Framework | P0 | 克隆仓库，导入Unity |
| 创建项目目录结构 | P0 | 按照1.2节创建 |
| 定义所有枚举 | P0 | GameEnums.cs |
| 创建 GameBalanceConfig | P0 | 全局数值配置SO |
| 创建 MarketConfig | P0 | 市场配置SO |
| 创建 SkillConfig | P0 | 技能配置SO |
| 创建 GeneralConfig | P0 | 将军配置SO |
| 创建 CampaignConfig | P0 | 战役配置SO |
| 创建配置实例 | P0 | 在Resources/Configs下创建 |

## 6.2 阶段二：运行时数据 (Week 1)

| 任务 | 优先级 | 说明 |
|------|--------|------|
| 实现 MarketRuntimeData | P0 | 市场运行时数据 |
| 实现 GeneralRuntimeData | P0 | 将军运行时数据 |
| 实现 BattleRuntimeData | P0 | 战场运行时数据 |
| 实现 CampaignRuntimeData | P0 | 战役运行时数据 |
| 注册自定义事件ID | P0 | 扩展 EventID 枚举 |

## 6.3 阶段三：核心系统 (Week 2)

| 任务 | 优先级 | 说明 |
|------|--------|------|
| 实现 MarketSystem | P0 | 现货/期货/借贷 |
| 实现 BattleSystem | P0 | 战斗结算/技能/联动 |
| 实现 CampaignSystem | P0 | 回合流程/胜负判定 |
| 在 GameRoot 中注册系统 | P0 | 添加到 systemList |
| 单元测试 | P1 | 验证核心逻辑 |

## 6.4 阶段四：场景管理器 (Week 3)

| 任务 | 优先级 | 说明 |
|------|--------|------|
| 实现 GameplayManager | P0 | 玩家操作接口 |
| 创建 BattleGameMode | P0 | 游戏模式 |
| 场景搭建 | P0 | Boot + Battle 场景 |

## 6.5 阶段五：UI系统 (Week 4)

| 任务 | 优先级 | 说明 |
|------|--------|------|
| GameplayWindow | P0 | 主游戏界面 |
| MarketPanel | P0 | 市场交易面板 |
| BattlefieldPanel | P0 | 战场显示 |
| GeneralPanel | P0 | 将军状态/指令分配 |
| IntelPanel | P1 | 市场情报面板 |
| HistoryPanel | P1 | 历史记录 |
| GameEndWindow | P0 | 结算界面 |

## 6.6 阶段六：完善与测试 (Week 5)

| 任务 | 优先级 | 说明 |
|------|--------|------|
| 维克多AI优化 | P1 | 更智能的决策 |
| 随机事件系统 | P1 | 完整实现 |
| 战场沙盘动画 | P2 | 视觉表现 |
| 数值平衡 | P1 | 调整配置参数 |
| 存档系统 | P2 | 使用 DataService |
| 创建更多战役配置 | P2 | 多样化关卡 |

---

# 第七章：配置文件示例

## 7.1 Resources目录结构

```
Resources/
├── Configs/
│   ├── GameBalanceConfig.asset      # 全局数值
│   ├── MarketConfig.asset           # 市场配置
│   ├── SkillConfig.asset            # 技能库
│   ├── Generals/
│   │   ├── GeneralConfig_Act1.asset # 第一章将军
│   │   └── GeneralConfig_Act2.asset # 第二章将军
│   └── Campaigns/
│       ├── Campaign_Tutorial.asset  # 教程战役
│       ├── Campaign_Act1_1.asset    # 第一章第一关
│       └── Campaign_Act1_2.asset    # 第一章第二关
```

## 7.2 预设技能配置参考

```
狂热型技能池:
- 先登 (fanatic_charge): ATK获胜时战线+1
- 嗜血 (fanatic_bloodlust): 连续ATK战斗力+20%
- 死战 (fanatic_laststand): 兵力<30时ATK战斗力+50%
- 不退 (fanatic_defiant): 被指派RET时30%抗命改DEF

保守型技能池:
- 铁壁 (conservative_ironwall): DEF成功时敌方-5兵力
- 以逸待劳 (conservative_patience): 连续DEF反伤50%
- 断后 (conservative_rearguard): RET时损失减半
- 怯战 (conservative_timid): 兵力<50时ATK可能抗命

投机型技能池:
- 顺风浪 (opportunist_momentum): 战线≥4时ATK+30%
- 逆风苟 (opportunist_turtle): 战线≤2时DEF+30%
- 诈败 (opportunist_feint): RET诱敌追击额外损伤
- 见机行事 (opportunist_adapt): 根据敌方指令微调
```

## 7.3 预设将军配置参考

```
己方将军:
- 左翼: 冯·布吕歇尔 (狂热型) - 先登/嗜血
- 中军: 库图佐夫 (保守型) - 铁壁/断后  
- 右翼: 塔列朗 (投机型) - 顺风浪/见机行事

敌方将军:
- 左翼: 拿破仑 (投机型) - 顺风浪/诈败
- 中军: 威灵顿 (保守型) - 铁壁/以逸待劳
- 右翼: 内伊 (狂热型) - 死战/不退
```

---

# 第八章：注意事项

## 8.1 Levity Framework 使用规范

1. **不要修改 Core 目录下的代码**
2. **System 必须实现 ILogic 接口**
3. **Manager 必须继承 ManagerBase**
4. **Window 必须继承 WindowBase**
5. **使用 EventService 进行模块间通信**
6. **使用 TimerService 处理延迟和定时任务**

## 8.2 配置系统使用规范

```csharp
// 访问全局配置
var balance = ConfigManager.Balance;
float rate = balance.CommissionRate;

// 加载战役配置
var campaign = ConfigManager.LoadCampaign("Campaign_Tutorial");

// 配置中引用其他配置
var generalConfig = campaign.GeneralConfig;
var marketConfig = campaign.MarketConfig;

// 技能查询
var skill = ConfigManager.Skills.GetSkill("fanatic_charge");
```

**配置修改原则**：
- 数值调整优先修改 GameBalanceConfig
- 新增将军需要创建/修改 GeneralConfig
- 新增战役需要创建新的 CampaignConfig
- 技能效果变化修改 SkillConfig

## 8.3 事件使用规范

```csharp
// 注册事件 (在 OnAwake/OnShow 中)
eventService.AddEventListening(EventID.XXX, OnXXX);

// 发送事件
eventService.SendMessage(EventID.XXX, param1, param2);

// 注销事件 (在 OnExit/UnInit 中)
eventService.RemoveEventListeningByTarget(this);
```

## 8.4 数据访问规范

```csharp
// 通过 CampaignSystem 访问运行时数据
var data = campaignSystem.Data;

// 通过配置访问静态数据
var generalConfig = data.Config.GeneralConfig.GetGeneral("ally_left");

// 通过 MarketSystem 执行交易
marketSystem.BuyOrder(OrderType.ATK, 1, out float cost);

// 通过 BattleSystem 查询战斗状态
battleSystem.CheckVictory();
```

## 8.5 UI 开发规范

```csharp
// 打开窗口
uiService.OpenWindow<MyWindow>(UILayer.Normal);

// 关闭窗口
uiService.CloseWindow<MyWindow>();

// 窗口内访问数据
var manager = managerService.GetManager<GameplayManager>();
var data = manager.GetCampaignData();
```

## 8.6 配置与运行时数据分离原则

| 类型 | 用途 | 修改时机 |
|------|------|----------|
| **Config (SO)** | 静态设计数据 | 开发/策划阶段 |
| **RuntimeData** | 动态游戏状态 | 运行时 |

```csharp
// 配置数据 (只读)
GeneralData config = campaignConfig.GeneralConfig.AllyGenerals[0];
string name = config.Name;           // 永不变化
int initialTroops = config.InitialTroops; // 初始值

// 运行时数据 (读写)
GeneralRuntimeData runtime = campaignData.Battle.AllyGenerals[0];
int currentTroops = runtime.Troops;  // 当前值，会变化
runtime.Troops -= 10;                // 可以修改
```

---

**文档版本**：v2.0  
**更新内容**：添加配置系统，将军固定配置，全局参数提取  
**目标**：指导 Claude Code 进行原型开发
