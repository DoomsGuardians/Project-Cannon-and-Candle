# UI Prefab 创建操作指南

本文档详细列出在 Unity 编辑器中创建所有 UI Prefab 的步骤。

---

## 重要说明：脚本挂载规则

本项目 UI 框架中，`WindowBase` 及其子类（GameplayWindow、MarketPanel、InfoPanel 等）是**纯 C# 类**，不继承 MonoBehaviour，**不能作为组件挂载到 GameObject 上**。它们在运行时由框架 `new T()` 创建，然后通过 `gameObject.GetComponent<XXXBinder>()` 获取 Prefab 上的引用。

**Prefab 上只需要挂载 Binder 脚本**（继承 UIBinder → MonoBehaviour）。

例外：`SpotMarketTab` 和 `FuturesMarketTab` 是 MonoBehaviour，需要挂载到对应节点上。

---

## UI 架构说明：Binder 模式 vs Tab 组件

### Binder 模式（用于 Window/Panel）

```
WindowBase (逻辑层，非 MonoBehaviour)
    ↓ GetComponent<XXXBinder>()
UIBinder (数据层，MonoBehaviour，public 字段)
```

**设计意图：**
- 分离 UI 布局（Binder）和业务逻辑（Window）
- Window 由 UIService 通过 `new T()` 创建
- Binder 挂在 Prefab 上持有序列化引用

### Tab 组件模式

```
SpotMarketTab / FuturesMarketTab (MonoBehaviour)
    - [SerializeField] private 字段
    - Initialize(manager, parentWindow)
    - RefreshUI()
    - SetActive()
```

**为什么不同：**
1. Tab 不是独立 Window，是 Panel 的子组件
2. Tab 需要父 Window 引用来绑定事件（`parentWindow.AddButtonListener`）
3. Tab 可能需要动态生成列表项（如 FuturesPositionItem）
4. Tab 生命周期由 Panel 控制，不是 UIService

### 绑定分工总结

| 组件类型 | 绑定方式 | 说明 |
|---------|---------|------|
| Window/Panel | Binder (public 字段) | 由 UIService 管理，需要分离逻辑和布局 |
| Tab 子组件 | [SerializeField] | 由父 Panel 管理，直接在组件内绑定 |
| Item 子组件 | Binder (public 字段) | 动态生成的列表项，需要外部访问字段 |

---

## 前置准备

1. 确保项目已导入 TextMesh Pro（Window → TextMeshPro → Import TMP Essential Resources）
2. 创建 Prefab 目录结构：
   ```
   Assets/Resources/Prefabs/WarBroker/UI/
   ├── Items/
   ├── Panels/
   ├── Popups/
   └── Windows/
   ```

---

## 一、GameplayWindow（主游戏窗口）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Windows/GameplayWindow.prefab`

### 创建步骤

1. **创建 Canvas**
   - Hierarchy → 右键 → UI → Canvas
   - 重命名为 `GameplayWindow`

2. **配置 Canvas**
   - Render Mode: `Screen Space - Overlay`
   - 添加组件: `Canvas Scaler`
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`
     - Match: `0.5`

3. **添加脚本**
   - 添加组件: `GameplayWindowBinder`（仅 Binder，不挂 GameplayWindow）

4. **创建子结构**

```
GameplayWindow (Canvas)
├── TopBar (RectTransform)
│   ├── CashArea (HorizontalLayoutGroup)
│   │   ├── ImgCashIcon (Image) ← 悬停显示"现金含义"
│   │   └── TxtCash (TextMeshPro - Text) ← 悬停显示"现金详情"
│   │
│   ├── InventoryArea (HorizontalLayoutGroup)
│   │   ├── ATKArea
│   │   │   ├── ImgATKIcon (Image)
│   │   │   └── TxtATK (TextMeshPro - Text)
│   │   ├── DEFArea
│   │   │   ├── ImgDEFIcon (Image)
│   │   │   └── TxtDEF (TextMeshPro - Text)
│   │   └── RETArea
│   │       ├── ImgRETIcon (Image)
│   │       └── TxtRET (TextMeshPro - Text)
│   │
│   ├── TxtTurn (TextMeshPro - Text)
│   ├── TxtPhase (TextMeshPro - Text)
│   ├── TxtNetWorth (TextMeshPro - Text)
│   └── TxtAudit (TextMeshPro - Text)
│
├── ContentArea (RectTransform)
│   └── (用于加载 MarketPanel/InfoPanel 等)
│
├── RightPanelArea (RectTransform) - 右侧常驻面板区
│   ├── 锚点：右侧拉伸（右上到右下）
│   ├── Pivot：(1, 0.5)
│   ├── 宽度：260
│   ├── Top/Bottom：60（为状态栏和底部栏留空间）
│   └── (运行时加载 ObjectivePanel)
│
├── TabButtons (Horizontal Layout Group)
│   ├── BtnMarket (Button)
│   │   └── Text (TMP) - "市场"
│   └── BtnIntel (Button)
│       └── Text (TMP) - "情报"
│
└── BottomBar (RectTransform)
    ├── TxtEventInfo (TextMeshPro - Text)
    └── BtnEndTurn (Button)
        └── Text (TMP) - "结束回合"
```

5. **绑定字段** (在 GameplayWindowBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtTurn | TopBar/TxtTurn |
| txtPhase | TopBar/TxtPhase |
| txtCash | TopBar/CashArea/TxtCash |
| txtNetWorth | TopBar/TxtNetWorth |
| txtAudit | TopBar/TxtAudit |
| imgCashIcon | TopBar/CashArea/ImgCashIcon |
| txtATK | TopBar/InventoryArea/ATKArea/TxtATK |
| imgATKIcon | TopBar/InventoryArea/ATKArea/ImgATKIcon |
| txtDEF | TopBar/InventoryArea/DEFArea/TxtDEF |
| imgDEFIcon | TopBar/InventoryArea/DEFArea/ImgDEFIcon |
| txtRET | TopBar/InventoryArea/RETArea/TxtRET |
| imgRETIcon | TopBar/InventoryArea/RETArea/ImgRETIcon |
| btnMarket | TabButtons/BtnMarket |
| btnIntel | TabButtons/BtnIntel |
| contentArea | ContentArea |
| rightPanelArea | RightPanelArea |
| btnEndTurn | BottomBar/BtnEndTurn |
| txtEventInfo | BottomBar/TxtEventInfo |

6. **保存 Prefab**
   - 拖入 `Assets/Resources/Prefabs/WarBroker/UI/Windows/`

---

## 二、TopStatusBar（顶部状态栏）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Panels/TopStatusBar.prefab`

### 创建步骤

1. **创建空对象**
   - Hierarchy → 右键 → Create Empty
   - 重命名为 `TopStatusBar`
   - 添加 `RectTransform` 组件（如果没有）

2. **配置 RectTransform**
   - Anchor: 顶部拉伸 (Top Stretch)
   - Height: `60`
   - Pivot: `(0.5, 1)`

3. **添加脚本**
   - 添加组件: `TopStatusBarBinder`（仅 Binder）

4. **创建子结构**

```
TopStatusBar (RectTransform + Horizontal Layout Group)
├── TxtTurn (TextMeshPro - Text) - "回合 1/12"
├── TxtPhase (TextMeshPro - Text) - "交易阶段"
├── Spacer (Layout Element, Flexible Width)
├── TxtCash (TextMeshPro - Text) - "$1,000"
├── TxtNetWorth (TextMeshPro - Text) - "净资产: $1,000"
└── TxtAudit (TextMeshPro - Text) - "审计: $1,000"
```

5. **绑定字段** (在 TopStatusBarBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtTurn | TxtTurn |
| txtPhase | TxtPhase |
| txtCash | TxtCash |
| txtNetWorth | TxtNetWorth |
| txtAudit | TxtAudit |

6. **保存 Prefab**

---

## 三、BottomBar（底部操作栏）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Panels/BottomBar.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `BottomBar`

2. **配置 RectTransform**
   - Anchor: 底部拉伸 (Bottom Stretch)
   - Height: `80`
   - Pivot: `(0.5, 0)`

3. **添加脚本**
   - 添加组件: `BottomBarBinder`（仅 Binder）

4. **创建子结构**

```
BottomBar (RectTransform)
├── TxtEventInfo (TextMeshPro - Text)
│   - Anchor: Left Stretch
│   - Width: 占据左侧大部分空间
│   - 文本: "事件信息显示区域..."
│
└── BtnEndTurn (Button)
    - Anchor: Right
    - Width: 150, Height: 50
    └── Text (TMP) - "结束回合"
```

5. **绑定字段** (在 BottomBarBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| btnEndTurn | BtnEndTurn |
| txtEventInfo | TxtEventInfo |

6. **保存 Prefab**

---

## 四、MarketPanel（市场面板）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Panels/MarketPanel.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `MarketPanel`

2. **添加脚本**
   - 添加组件: `MarketPanelBinder`（仅 Binder）
   - 在 `SpotContent` 节点上添加: `SpotMarketTab`（这是 MonoBehaviour）
   - 在 `FuturesContent` 节点上添加: `FuturesMarketTab`（这是 MonoBehaviour）

3. **创建子结构**

```
MarketPanel (RectTransform)
├── TabBar (Horizontal Layout Group)
│   ├── TabSpot (Toggle) - "现货"
│   │   └── Label (TMP)
│   └── TabFutures (Toggle) - "期货"
│       └── Label (TMP)
│
├── SpotContent (RectTransform) - 现货区域
│   │   ※ 在此节点添加 SpotMarketTab 组件
│   │
│   ├── Header (TMP) - "现货市场"
│   │
│   ├── AtkRow (Horizontal Layout Group)
│   │   ├── TxtAtkLabel (TMP) - "ATK"
│   │   ├── TxtAtkPrice (TMP) - "$42.50"
│   │   ├── TxtAtkHolding (TMP) - "持有: 0"
│   │   ├── BtnAtkBuy (Button) - "买入"
│   │   ├── BtnAtkSell (Button) - "卖出"
│   │   └── BtnAtkChart (Button) - "K线"
│   │
│   ├── DefRow (同上结构)
│   │   ├── TxtDefLabel (TMP) - "DEF"
│   │   ├── TxtDefPrice (TMP)
│   │   ├── TxtDefHolding (TMP)
│   │   ├── BtnDefBuy (Button)
│   │   ├── BtnDefSell (Button)
│   │   └── BtnDefChart (Button) - "K线"
│   │
│   └── RetRow (同上结构)
│       ├── TxtRetLabel (TMP) - "RET"
│       ├── TxtRetPrice (TMP)
│       ├── TxtRetStock (TMP)
│       ├── BtnRetBuy (Button)
│       ├── BtnRetSell (Button)
│       └── BtnRetChart (Button) - "K线"
│
│   └── ChartArea (RectTransform)
│       │   - Height: 200
│       │
│       └── KLineChart (RectTransform)
│           │   - Stretch to fill ChartArea
│           │   - 添加组件: CandlestickChart（预先配置样式）
│           │   - 添加组件: KLineChartView（chart 字段绑定上面的 CandlestickChart）
│           └── (图表样式在 Prefab 中配置，运行时直接使用)
│
├── FuturesContent (RectTransform) - 期货区域 (默认隐藏)
│   │   ※ 在此节点添加 FuturesMarketTab 组件
│   │
│   ├── Header (TMP) - "期货市场 (固定3回合期限)"
│   │
│   ├── AtkRow (Horizontal Layout Group)
│   │   ├── TxtAtkInfo (TMP) - "ATK: 价格 42.5 | 保证金 12.8"
│   │   ├── BtnAtkLong (Button) - "做多 +1"
│   │   ├── BtnAtkShort (Button) - "做空 +1"
│   │   └── TxtAtkPosition (TMP) - "持仓: 多0/空0"
│   │
│   ├── DefRow (同上结构)
│   │   ├── TxtDefInfo (TMP)
│   │   ├── BtnDefLong (Button)
│   │   ├── BtnDefShort (Button)
│   │   └── TxtDefPosition (TMP)
│   │
│   ├── RetRow (同上结构)
│   │   ├── TxtRetInfo (TMP)
│   │   ├── BtnRetLong (Button)
│   │   ├── BtnRetShort (Button)
│   │   └── TxtRetPosition (TMP)
│   │
│   ├── SummaryArea
│   │   ├── TxtTotalMargin (TMP) - "总保证金: 0"
│   │   └── TxtTotalPnL (TMP) - "总浮盈: +0"
│   │
│   └── PositionList (Scroll View) - 持仓列表（用于查看详情和平仓）
│       └── Content (Vertical Layout Group)
│           └── (动态生成 FuturesPositionItem)
│
└── BankArea (RectTransform) - 银行区
    ├── Header (TMP) - "银行借贷"
    │
    ├── InfoRow (Horizontal Layout Group)
    │   ├── TxtDebt (TMP) - "负债: 0"
    │   ├── TxtInterest (TMP) - "利率: 10%"
    │   └── TxtLoanLimit (TMP) - "可借: 500"
    │
    ├── AmountSelector (Horizontal Layout Group)
    │   ├── BtnDecrease (Button) - "-"
    │   ├── TxtAmount (TMP) - "100"
    │   └── BtnIncrease (Button) - "+"
    │
    ├── QuickButtons (Horizontal Layout Group)
    │   ├── Btn100 (Button) - "100"
    │   ├── Btn500 (Button) - "500"
    │   └── BtnMax (Button) - "最大"
    │
    └── ActionButtons (Horizontal Layout Group)
        ├── BtnBorrow (Button) - "借款"
        └── BtnRepay (Button) - "还款"
```

4. **配置 Toggle Group**
   - 在 TabBar 上添加 `Toggle Group` 组件
   - 将 TabSpot 和 TabFutures 的 Group 字段指向 TabBar

5. **绑定字段**

**MarketPanelBinder** (挂在 MarketPanel 根节点，只负责 Tab 切换、银行区、玩家信息)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| tabSpot | TabBar/TabSpot |
| tabFutures | TabBar/TabFutures |
| spotContent | SpotContent |
| futuresContent | FuturesContent |
| txtDebt | BankArea/InfoRow/TxtDebt |
| txtInterest | BankArea/InfoRow/TxtInterest |
| txtLoanLimit | BankArea/InfoRow/TxtLoanLimit |
| btnDecrease | BankArea/AmountSelector/BtnDecrease |
| txtAmount | BankArea/AmountSelector/TxtAmount |
| btnIncrease | BankArea/AmountSelector/BtnIncrease |
| btn100 | BankArea/QuickButtons/Btn100 |
| btn500 | BankArea/QuickButtons/Btn500 |
| btnMax | BankArea/QuickButtons/BtnMax |
| btnBorrow | BankArea/ActionButtons/BtnBorrow |
| btnRepay | BankArea/ActionButtons/BtnRepay |

**SpotMarketTab** (挂在 SpotContent 节点，负责现货区域)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtAtkPrice | SpotContent/TableArea/AtkRow/TxtAtkPrice |
| txtAtkMarketStock | SpotContent/TableArea/AtkRow/TxtAtkMarketStock |
| txtAtkHolding | SpotContent/TableArea/AtkRow/TxtAtkHolding |
| btnAtkBuy | SpotContent/TableArea/AtkRow/ActionsArea/BtnAtkBuy |
| btnAtkSell | SpotContent/TableArea/AtkRow/ActionsArea/BtnAtkSell |
| btnAtkChart | SpotContent/TableArea/AtkRow/ActionsArea/BtnAtkChart |
| txtDefPrice | SpotContent/TableArea/DefRow/TxtDefPrice |
| txtDefMarketStock | SpotContent/TableArea/DefRow/TxtDefMarketStock |
| txtDefHolding | SpotContent/TableArea/DefRow/TxtDefHolding |
| btnDefBuy | SpotContent/TableArea/DefRow/ActionsArea/BtnDefBuy |
| btnDefSell | SpotContent/TableArea/DefRow/ActionsArea/BtnDefSell |
| btnDefChart | SpotContent/TableArea/DefRow/ActionsArea/BtnDefChart |
| txtRetPrice | SpotContent/TableArea/RetRow/TxtRetPrice |
| txtRetMarketStock | SpotContent/TableArea/RetRow/TxtRetMarketStock |
| txtRetHolding | SpotContent/TableArea/RetRow/TxtRetHolding |
| btnRetBuy | SpotContent/TableArea/RetRow/ActionsArea/BtnRetBuy |
| btnRetSell | SpotContent/TableArea/RetRow/ActionsArea/BtnRetSell |
| btnRetChart | SpotContent/TableArea/RetRow/ActionsArea/BtnRetChart |
| klineChart | SpotContent/ChartArea/KLineChart (KLineChartView 组件) |

**FuturesMarketTab** (挂在 FuturesContent 节点，负责期货区域)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtAtkInfo | FuturesContent/AtkRow/TxtAtkInfo |
| btnAtkLong | FuturesContent/AtkRow/BtnAtkLong |
| btnAtkShort | FuturesContent/AtkRow/BtnAtkShort |
| txtAtkPosition | FuturesContent/AtkRow/TxtAtkPosition |
| txtDefInfo | FuturesContent/DefRow/TxtDefInfo |
| btnDefLong | FuturesContent/DefRow/BtnDefLong |
| btnDefShort | FuturesContent/DefRow/BtnDefShort |
| txtDefPosition | FuturesContent/DefRow/TxtDefPosition |
| txtRetInfo | FuturesContent/RetRow/TxtRetInfo |
| btnRetLong | FuturesContent/RetRow/BtnRetLong |
| btnRetShort | FuturesContent/RetRow/BtnRetShort |
| txtRetPosition | FuturesContent/RetRow/TxtRetPosition |
| positionContainer | FuturesContent/PositionList/Content |
| positionItemPrefab | (拖入 FuturesPositionItem prefab) |
| txtTotalMargin | FuturesContent/SummaryArea/TxtTotalMargin |
| txtTotalPnL | FuturesContent/SummaryArea/TxtTotalPnL |

6. **保存 Prefab**

---

## 五、InfoPanel（情报面板）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Panels/InfoPanel.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `InfoPanel`

2. **添加脚本**
   - 添加组件: `InfoPanelBinder`（仅 Binder）

3. **创建子结构**

```
InfoPanel (RectTransform)
├── MarketIntelArea (RectTransform)
│   ├── Header (TMP) - "市场情报"
│   └── TxtMarketIntel (TMP) - 多行文本区
│
├── BattleIntelArea (RectTransform)
│   ├── Header (TMP) - "战场情报"
│   └── TxtBattleIntel (TMP) - 多行文本区
│
├── EnemyIntelArea (RectTransform)
│   ├── Header (TMP) - "敌情分析"
│   └── TxtEnemyIntel (TMP) - 多行文本区
│
└── ChartArea (RectTransform + Vertical Layout Group)
    │   - Spacing: 10
    │   - Child Force Expand: Width=true, Height=false
    │
    ├── ChartAtk (RectTransform)
    │   │   - Height: 150
    │   │   - 添加组件: CandlestickChart（预先配置样式）
    │   │   - 添加组件: KLineChartView（chart 字段绑定上面的 CandlestickChart）
    │   └── (图表样式在 Prefab 中配置，运行时直接使用)
    │
    ├── ChartDef (RectTransform)
    │   │   - Height: 150
    │   │   - 添加组件: CandlestickChart（预先配置样式）
    │   │   - 添加组件: KLineChartView（chart 字段绑定上面的 CandlestickChart）
    │   └── (图表样式在 Prefab 中配置，运行时直接使用)
    │
    └── ChartRet (RectTransform)
        │   - Height: 150
        │   - 添加组件: CandlestickChart（预先配置样式）
        │   - 添加组件: KLineChartView（chart 字段绑定上面的 CandlestickChart）
        └── (图表样式在 Prefab 中配置，运行时直接使用)
```

4. **绑定字段** (在 InfoPanelBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtMarketIntel | MarketIntelArea/TxtMarketIntel |
| txtBattleIntel | BattleIntelArea/TxtBattleIntel |
| txtEnemyIntel | EnemyIntelArea/TxtEnemyIntel |
| chartAtk | ChartArea/ChartAtk (KLineChartView 组件) |
| chartDef | ChartArea/ChartDef (KLineChartView 组件) |
| chartRet | ChartArea/ChartRet (KLineChartView 组件) |

5. **保存 Prefab**

---

## 六、GeneralDetailPanel（将军详情面板 - 简化版）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Panels/GeneralDetailPanel.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `GeneralDetailPanel`

2. **配置 RectTransform**
   - Anchor: Right Stretch
   - Width: `300`
   - 用于从右侧滑入

3. **添加脚本**
   - 添加组件: `GeneralDetailPanelBinder`（仅 Binder）

4. **创建子结构**

```
GeneralDetailPanel (RectTransform)
├── Header (Vertical Layout Group)
│   ├── TxtName (TMP) - "马塞纳"
│   └── InfoRow (Horizontal Layout Group)
│       ├── TxtPersonality (TMP) - "狂热型"
│       ├── Separator (TMP) - "|"
│       └── TxtPosition (TMP) - "左翼"
│
├── HPArea (Vertical Layout Group)
│   ├── SliderHP (Slider)
│   └── TxtHP (TMP) - "16/20"
│
├── IntentArea (Vertical Layout Group)
│   └── TxtIntent (TMP) - "意图: 🔴 ATK"
│
├── ActionsArea (Vertical Layout Group)
│   ├── BtnReinforce (Button)
│   │   └── Text (TMP) - "强化 🔴×3"
│   ├── BtnOverrideATK (Button)
│   │   └── Text (TMP) - "篡改 🔴×2"
│   ├── BtnOverrideDEF (Button)
│   │   └── Text (TMP) - "篡改 🔵×3"
│   └── BtnOverrideRET (Button)
│       └── Text (TMP) - "篡改 🟡×1"
│
└── BtnClose (Button) - "X" 或 "关闭"
    └── Text (TMP)
```

5. **绑定字段** (在 GeneralDetailPanelBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtName | Header/TxtName |
| txtPersonality | Header/InfoRow/TxtPersonality |
| txtPosition | Header/InfoRow/TxtPosition |
| sliderHP | HPArea/SliderHP |
| txtHP | HPArea/TxtHP |
| txtIntent | IntentArea/TxtIntent |
| btnReinforce | ActionsArea/BtnReinforce |
| btnOverrideATK | ActionsArea/BtnOverrideATK |
| btnOverrideDEF | ActionsArea/BtnOverrideDEF |
| btnOverrideRET | ActionsArea/BtnOverrideRET |
| btnClose | BtnClose |

6. **保存 Prefab**

### 设计说明

**简化内容**:
- ✅ 移除了 Trust 和 Morale 属性条
- ✅ 移除了状态和技能文本
- ✅ 移除了意图来源文本和气泡图片
- ✅ 移除了消耗文本（固定数值，悬停有 Tooltip）
- ✅ 将篡改下拉框改为 3 个独立按钮
- ✅ 按钮文本直接显示指令类型和持有数量

**交互逻辑**:
- 强化按钮：显示 "强化 🔴×3"（持有数量）或 "强化 🔴×3"（已强化次数）
- 篡改按钮：显示 "篡改 🔴×2"（持有数量）
- 运行时只显示与默认意图不同的 2 个篡改按钮（例如默认是 ATK，则只显示 DEF 和 RET）
- 持有数量不足时按钮自动灰显
- 已强化或篡改后，对应操作按钮禁用

---

## 七、ObjectivePanel（委托任务面板）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Panels/ObjectivePanel.prefab`

### 设计说明

ObjectivePanel 采用**动态生成**方式显示委托任务，支持从 `CampaignConfig.Commissions` 配置不同战役的委托列表。

### 创建步骤

1. **创建空对象** → 重命名为 `ObjectivePanel`

2. **添加脚本**
   - 添加组件: `ObjectivePanelBinder`（仅 Binder）

3. **创建子结构**

```
ObjectivePanel (RectTransform)
├── Header
│   └── TxtTitle (TMP) - "委托任务"
│
└── CommissionListRoot (Vertical Layout Group)
    │   - Spacing: 5
    │   - Child Force Expand: Width=true, Height=false
    │   - 添加 Content Size Fitter (Vertical Fit: Preferred Size)
    └── (运行时动态生成 CommissionItem)
```

4. **绑定字段** (在 ObjectivePanelBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtTitle | Header/TxtTitle |
| commissionListRoot | CommissionListRoot |
| commissionItemPrefab | CommissionItem.prefab (创建后拖入) |

5. **保存 Prefab**

---

## 七-A、CommissionItem（委托任务项）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Items/CommissionItem.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `CommissionItem`

2. **配置 RectTransform**
   - Width: 拉伸 (Stretch)
   - Height: 自适应（使用 Layout Element）

3. **添加脚本**
   - 添加组件: `CommissionItemBinder`

4. **创建子结构**

```
CommissionItem (RectTransform + Horizontal Layout Group)
│   - Spacing: 8
│   - Child Alignment: Middle Left
│   - Child Force Expand: Width=false, Height=false
│
├── ImgAvatar (Image, 可选)
│   - Width: 32, Height: 32
│   - 添加 Layout Element (Preferred Width: 32, Preferred Height: 32)
│   - 用于显示委托人头像（如果配置了）
│
└── ContentArea (Vertical Layout Group)
    │   - Spacing: 2
    │   - Child Force Expand: Width=true, Height=false
    │
    ├── NameRow (Horizontal Layout Group)
    │   │   - Spacing: 8
    │   │   - Child Force Expand: Width=false, Height=false
    │   │
    │   ├── TxtName (TMP) - "斩首胜利"
    │   │   - Font Size: 16
    │   │   - Color: 白色（完成时变绿色）
    │   │
    │   └── TxtProgress (TMP) - "[0%]" 或 "[完成]"
    │       - Font Size: 14
    │       - Color: 白色（完成时变绿色）
    │
    └── TxtDescription (TMP) - "占领敌方大本营，奖金 $200"
        - Font Size: 12
        - Color: 浅灰色
```

5. **绑定字段** (在 CommissionItemBinder 组件上)

| Inspector 字段 | 拖入对象 | 说明 |
|---------------|---------|------|
| txtName | ContentArea/NameRow/TxtName | 静态：委托名称 |
| txtProgress | ContentArea/NameRow/TxtProgress | 动态：进度状态 |
| txtDescription | ContentArea/TxtDescription | 静态：委托描述 + 奖励 |
| imgAvatar | ImgAvatar | 静态：委托人头像（可选） |

6. **保存 Prefab**

### 字段分类说明

| 类型 | 字段 | 更新时机 |
|------|------|---------|
| 静态 | txtName, txtDescription, imgAvatar | 仅在创建时设置一次 |
| 动态 | txtProgress | 每次刷新 UI 时更新 |

### 运行时行为

- `ObjectivePanel.OnShow()` 时根据 `CommissionSystem.GetCommissions()` 动态生成
- 静态字段在 `CreateCommissionItems()` 中设置
- `txtDescription` 自动拼接描述和奖金：`"{Description}，奖金 ${BonusAmount}"`
- 动态字段在 `RefreshCommissions()` 中更新
- 完成时 txtName 和 txtProgress 都变为绿色

---

## 七-B、CommissionConfig 资产创建

**保存路径**: `Assets/Resources/Config/WarBroker/Commissions/`

### 创建默认委托配置

在 Unity 中：Project 窗口 → 右键 → Create → WarBroker → CommissionConfig

创建以下 4 个默认配置：

| 文件名 | CommissionId | DisplayName | Type | TargetValue | SecondaryTargetValue | BonusAmount |
|--------|--------------|-------------|------|-------------|---------------------|-------------|
| Commission_WinWar.asset | WinWar | 斩首胜利 | OccupyGrid | 5 | 0 | 200 |
| Commission_ShortCountry.asset | ShortCountry | 做空国运 | EnemyReachGrid | 2 | 2 | 500 |
| Commission_Traitor.asset | Traitor | 卖国求荣 | NotOccupyGrid | 5 | 0 | 300 |
| Commission_MeatGrinder.asset | MeatGrinder | 绞肉机 | TotalCasualties | 100 | 0 | 150 |

### 配置字段说明

| 字段 | 说明 |
|------|------|
| CommissionId | 唯一标识符 |
| DisplayName | UI 显示名称 |
| Description | 详细描述文案 |
| CommissionerName | 委托人名称（可选） |
| CommissionerAvatar | 委托人头像 Sprite（可选） |
| CommissionerQuote | 委托人台词（可选） |
| BonusAmount | 完成奖金 |
| Type | 委托类型（决定检查逻辑） |
| TargetValue | 主目标值 |
| SecondaryTargetValue | 次要目标值（如 EnemyReachGrid 需要的敌方数量） |
| NaninovelScriptName | Naninovel 对话脚本名（TODO） |

### CommissionType 枚举说明

| 类型 | 说明 | TargetValue | SecondaryTargetValue |
|------|------|-------------|---------------------|
| OccupyGrid | 己方占领指定 Grid | Grid 位置 (1-5) | 不使用 |
| EnemyReachGrid | 敌方到达指定 Grid | Grid 位置 (1-5) | 需要的敌方数量 |
| NotOccupyGrid | 未占领指定 Grid | Grid 位置 (1-5) | 不使用 |
| TotalCasualties | 总伤亡达到数量 | 伤亡数量 | 不使用 |

### 在 CampaignConfig 中配置

1. 打开 `Campaign_Tutorial.asset`
2. 找到 `Commissions` 列表
3. 将创建的 CommissionConfig 资产拖入列表
4. 不同战役可配置不同的委托组合

---

## 八、BattleResultPopup（战斗结算弹窗）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Popups/BattleResultPopup.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `BattleResultPopup`

2. **添加脚本**
   - 添加组件: `BattleResultPopupBinder`（仅 Binder）

3. **创建子结构**

```
BattleResultPopup (RectTransform, 全屏)
├── Background (Image)
│   - Color: 黑色半透明 (0, 0, 0, 0.5)
│   - Raycast Target: true (点击背景不关闭)
│
└── Panel (RectTransform, 居中)
    - Width: 600, Height: 500
    - 添加 Image 组件作为背景
    │
    ├── TxtTitle (TMP) - "第3回合战斗结算"
    │
    ├── ResultContainer (Scroll View)
    │   └── Viewport
    │       └── Content (Vertical Layout Group)
    │           └── (动态生成 BattleResultItem)
    │
    └── BtnConfirm (Button) - "确认"
        └── Text (TMP)
```

4. **绑定字段** (在 BattleResultPopupBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtTitle | Panel/TxtTitle |
| resultContainer | Panel/ResultContainer/Viewport/Content |
| resultItemPrefab | BattleResultItem.prefab (创建后拖入) |
| btnConfirm | Panel/BtnConfirm |

5. **保存 Prefab**

---

## 九、BattleResultItem（战斗结果项）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Items/BattleResultItem.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `BattleResultItem`

2. **配置 RectTransform**
   - Width: 拉伸 (Stretch)
   - Height: `120`

3. **添加脚本**
   - 添加组件: `BattleResultItemBinder`

4. **创建子结构**

```
BattleResultItem (RectTransform + Vertical Layout Group)
├── TxtPosition (TMP) - "左翼"
├── TxtOrders (TMP) - "己方: ATK vs 敌方: DEF"
├── TxtLineMove (TMP) - "战线移动: 3 → 4"
├── TxtTroopChange (TMP) - "兵力变化: 己方 -2, 敌方 -4"
├── TxtSkill (TMP) - "技能: 无"
├── TxtSpecial (TMP) - "特殊效果: 无"
└── TxtDescription (TMP) - "详细描述..."
```

5. **绑定字段** (在 BattleResultItemBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtPosition | TxtPosition |
| txtOrders | TxtOrders |
| txtLineMove | TxtLineMove |
| txtTroopChange | TxtTroopChange |
| txtSkill | TxtSkill |
| txtSpecial | TxtSpecial |
| txtDescription | TxtDescription |

6. **保存 Prefab**

---

## 十、EventPopup（事件弹窗）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Popups/EventPopup.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `EventPopup`

2. **添加脚本**
   - 添加组件: `EventPopupBinder`（仅 Binder）

3. **创建子结构**

```
EventPopup (RectTransform, 全屏)
├── Background (Image)
│   - Color: 黑色半透明
│
└── Panel (RectTransform, 居中)
    - Width: 500, Height: 350
    │
    ├── TxtTitle (TMP) - "突发事件!"
    ├── TxtDescription (TMP) - "敌军获得补给，士气大增！"
    ├── TxtEffects (TMP) - "效果: 敌方士气 +20"
    └── BtnConfirm (Button) - "知道了"
        └── Text (TMP)
```

4. **绑定字段** (在 EventPopupBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtTitle | Panel/TxtTitle |
| txtDescription | Panel/TxtDescription |
| txtEffects | Panel/TxtEffects |
| btnConfirm | Panel/BtnConfirm |

5. **保存 Prefab**

---

## 十一、CampaignEndPopup（战役结束弹窗）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Popups/CampaignEndPopup.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `CampaignEndPopup`

2. **添加脚本**
   - 添加组件: `CampaignEndPopupBinder`（仅 Binder）

3. **创建子结构**

```
CampaignEndPopup (RectTransform, 全屏)
├── Background (Image)
│   - Color: 黑色半透明
│
└── Panel (RectTransform, 居中)
    - Width: 600, Height: 550
    │
    ├── TxtTitle (TMP) - "战役结束 - 胜利!"
    │
    ├── TxtStats (TMP, 多行)
    │   "最终现金: $2,500
    │    交易盈亏: +$800
    │    委托奖金: +$350
    │    总回报: +$1,150"
    │
    ├── TxtCommissions (TMP, 多行)
    │   "-- 委托任务 --
    │    [v] 赢下战争  +$200
    │    [x] 做空祖国  --
    │    [x] 卖国求荣  --
    │    [v] 绞肉机    +$150"
    │
    ├── BtnRestart (Button) - "重新开始"
    │   └── Text (TMP)
    │
    └── BtnMainMenu (Button) - "返回主菜单"
        └── Text (TMP)
```

4. **绑定字段** (在 CampaignEndPopupBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtTitle | Panel/TxtTitle |
| txtStats | Panel/TxtStats |
| txtCommissions | Panel/TxtCommissions |
| btnRestart | Panel/BtnRestart |
| btnMainMenu | Panel/BtnMainMenu |

5. **保存 Prefab**

---

## 十二、FuturesPositionItem（期货持仓项）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Items/FuturesPositionItem.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `FuturesPositionItem`

2. **配置 RectTransform**
   - Width: 拉伸 (Stretch)
   - Height: `80`

3. **添加脚本**
   - 添加组件: `FuturesPositionItemBinder`

4. **创建子结构**

```
FuturesPositionItem (RectTransform + Horizontal Layout Group)
├── TxtContract (TMP) - "ATK 多"
├── TxtOpenPrice (TMP) - "开仓: $42"
├── TxtCurrentPrice (TMP) - "现价: $48"
├── TxtExpiration (TMP) - "剩余: 2回合"
├── TxtMargin (TMP) - "保证金: $50"
├── TxtPnL (TMP) - "+$30" (绿色/红色)
└── BtnClose (Button) - "平仓"
    └── Text (TMP)
```

5. **绑定字段** (在 FuturesPositionItemBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtContract | TxtContract |
| txtOpenPrice | TxtOpenPrice |
| txtCurrentPrice | TxtCurrentPrice |
| txtExpiration | TxtExpiration |
| txtMargin | TxtMargin |
| txtPnL | TxtPnL |
| btnClose | BtnClose |

6. **保存 Prefab**

---

## 通用配置建议

### TextMeshPro 文本设置

| 用途 | 字号 | 对齐 | 溢出处理 |
|-----|-----|-----|---------|
| 标题 | 28-36 | Center | Ellipsis |
| 正文 | 18-22 | Left | Overflow/Linked |
| 按钮文字 | 20-24 | Center | Ellipsis |
| 数值显示 | 20-24 | Right | Overflow |
| 小标签 | 14-16 | Left/Center | Ellipsis |

### 按钮通用设置

- Navigation: None (避免键盘导航问题)
- Transition: Color Tint 或 Sprite Swap
- 确保所有按钮都有 Raycast Target

### Layout 建议

- 使用 `Content Size Fitter` 配合 `Layout Group` 实现自适应
- 弹窗使用 `Canvas Group` 控制透明度和交互

---

## 创建顺序建议

1. **基础 Items**（先创建，因为其他 Prefab 需要引用）
   - BattleResultItem
   - FuturesPositionItem
   - CommissionItem ← 新增

2. **配置资产**
   - Commission_WinWar.asset
   - Commission_ShortCountry.asset
   - Commission_Traitor.asset
   - Commission_MeatGrinder.asset
   - 在 Campaign_Tutorial.asset 中配置 Commissions 列表

3. **主窗口**
   - GameplayWindow

4. **面板**（按使用频率）
   - MarketPanel
   - InfoPanel
   - TopStatusBar
   - BottomBar
   - GeneralDetailPanel
   - ObjectivePanel（需要先创建 CommissionItem）

5. **弹窗**
   - EventPopup
   - BattleResultPopup
   - CampaignEndPopup

---

## 十三、SpotMarketTab 子组件

**位置**: 嵌入在 MarketPanel 的 SpotContent 节点中

SpotMarketTab 是一个独立的 MonoBehaviour，需要在 SpotContent 上添加并绑定字段。

### SpotMarketTab Prefab 结构（表格化布局）

```
SpotContent (RectTransform)
│   ※ 在此节点添加 SpotMarketTab 组件
│
├── Header (TMP) - "现货市场"
│
├── TableArea (Vertical Layout Group)
│   │   - Spacing: 5
│   │   - Child Force Expand: Width=true, Height=false
│   │
│   ├── TableHeader (Horizontal Layout Group)
│   │   │   - Spacing: 10
│   │   │   - Child Force Expand: Width=false, Height=false
│   │   │
│   │   ├── TxtHeaderType (TMP) - "指令"
│   │   │   - Layout Element: Preferred Width: 60
│   │   │   - Font Style: Bold
│   │   │   - Alignment: Center
│   │   │
│   │   ├── TxtHeaderPrice (TMP) - "价格"
│   │   │   - Layout Element: Preferred Width: 80
│   │   │   - Font Style: Bold
│   │   │   - Alignment: Center
│   │   │
│   │   ├── TxtHeaderMarketStock (TMP) - "市场库存"
│   │   │   - Layout Element: Preferred Width: 100
│   │   │   - Font Style: Bold
│   │   │   - Alignment: Center
│   │   │
│   │   ├── TxtHeaderHolding (TMP) - "持有量"
│   │   │   - Layout Element: Preferred Width: 80
│   │   │   - Font Style: Bold
│   │   │   - Alignment: Center
│   │   │
│   │   └── TxtHeaderActions (TMP) - "操作"
│   │       - Layout Element: Flexible Width: 1
│   │       - Font Style: Bold
│   │       - Alignment: Center
│   │
│   ├── AtkRow (Horizontal Layout Group)
│   │   │   - Spacing: 10
│   │   │   - Child Alignment: Middle Left
│   │   │
│   │   ├── TxtAtkLabel (TMP) - "ATK"
│   │   │   - Layout Element: Preferred Width: 60
│   │   │   - Alignment: Center
│   │   │
│   │   ├── TxtAtkPrice (TMP) - "42.5"
│   │   │   - Layout Element: Preferred Width: 80
│   │   │   - Alignment: Center
│   │   │
│   │   ├── TxtAtkMarketStock (TMP) - "150"  ← 新增
│   │   │   - Layout Element: Preferred Width: 100
│   │   │   - Alignment: Center
│   │   │
│   │   ├── TxtAtkHolding (TMP) - "2"
│   │   │   - Layout Element: Preferred Width: 80
│   │   │   - Alignment: Center
│   │   │
│   │   └── ActionsArea (Horizontal Layout Group)
│   │       │   - Spacing: 5
│   │       │   - Layout Element: Flexible Width: 1
│   │       │
│   │       ├── BtnAtkBuy (Button) - "买入"
│   │       ├── BtnAtkSell (Button) - "卖出"
│   │       └── BtnAtkChart (Button) - "K线"
│   │
│   ├── DefRow (同上结构)
│   │   ├── TxtDefLabel, TxtDefPrice, TxtDefMarketStock, TxtDefHolding
│   │   └── ActionsArea (BtnDefBuy, BtnDefSell, BtnDefChart)
│   │
│   └── RetRow (同上结构)
│       ├── TxtRetLabel, TxtRetPrice, TxtRetMarketStock, TxtRetHolding
│       └── ActionsArea (BtnRetBuy, BtnRetSell, BtnRetChart)
│
└── ChartArea (RectTransform)
    │   - Height: 200
    │
    └── KLineChart (RectTransform)
        │   - Stretch to fill ChartArea
        │   - 添加组件: CandlestickChart（预先配置样式）
        │   - 添加组件: KLineChartView（chart 字段绑定上面的 CandlestickChart）
        └── (图表样式在 Prefab 中配置，运行时直接使用)
```

### SpotMarketTab 需绑定的字段

| Inspector 字段 | 类型 | 说明 |
|---------------|------|------|
| txtAtkPrice | TMP_Text | ATK 价格 |
| txtAtkMarketStock | TMP_Text | ATK 市场库存（新增） |
| txtAtkHolding | TMP_Text | ATK 持有量 |
| btnAtkBuy | Button | ATK 买入 |
| btnAtkSell | Button | ATK 卖出 |
| btnAtkChart | Button | ATK K线切换按钮 |
| txtDefPrice | TMP_Text | DEF 价格 |
| txtDefMarketStock | TMP_Text | DEF 市场库存（新增） |
| txtDefHolding | TMP_Text | DEF 持有量 |
| btnDefBuy | Button | DEF 买入 |
| btnDefSell | Button | DEF 卖出 |
| btnDefChart | Button | DEF K线切换按钮 |
| txtRetPrice | TMP_Text | RET 价格 |
| txtRetMarketStock | TMP_Text | RET 市场库存（新增） |
| txtRetHolding | TMP_Text | RET 持有量 |
| btnRetBuy | Button | RET 买入 |
| btnRetSell | Button | RET 卖出 |
| btnRetChart | Button | RET K线切换按钮 |
| klineChart | KLineChartView | K线图组件 |

---

## 十四、KLineChartView 组件说明

**脚本路径**: `Assets/Scripts/WarBroker/UI/Base/KLineChartView.cs`

KLineChartView 是封装 XCharts CandlestickChart 的通用 K 线图组件，用于显示 ATK/DEF/RET 指令的价格走势。

### 创建步骤（Prefab 预配置方式 - 推荐）

1. **创建空 GameObject**
   - 添加 `RectTransform` 组件（默认已有）
   - 设置合适的尺寸（建议最小 200x150）

2. **添加 CandlestickChart 组件**
   - 在 Inspector 中 Add Component → 搜索 `CandlestickChart`
   - 在 XCharts Inspector 中配置图表样式：
     - **Title**: 设置标题文字、字体大小
     - **XAxis**: Category 类型，配置标签样式
     - **YAxis**: Value 类型，配置标签样式、分割数
     - **Grid**: 配置边距（left, right, top, bottom）
     - **Serie - Candlestick**: 配置涨跌颜色
       - itemStyle.color: 涨色（如红色 #EB5454）
       - itemStyle.color0: 跌色（如绿色 #44C67F）
       - itemStyle.borderColor: 涨边框色
       - itemStyle.borderColor0: 跌边框色

3. **添加 KLineChartView 组件**
   - 在 Inspector 中 Add Component → 搜索 `KLineChartView`
   - 将同节点的 `CandlestickChart` 拖入 `chart` 字段

4. **配置 Inspector 属性**（可选）

| 属性 | 类型 | 说明 |
|------|------|------|
| chart | CandlestickChart | 预绑定的图表组件（推荐在 Prefab 中配置） |
| chartTitle | string | 图表标题（如 "ATK"），也可在 CandlestickChart 中配置 |

### 创建步骤（运行时创建方式 - 兼容旧用法）

1. **创建空 GameObject**
   - 添加 `RectTransform` 组件（默认已有）
   - 设置合适的尺寸（建议最小 200x150）

2. **添加 KLineChartView 组件**
   - 在 Inspector 中 Add Component → 搜索 `KLineChartView`
   - **不绑定** chart 字段，运行时会自动创建 CandlestickChart

注意：运行时创建方式无法在编辑器中预览图表样式。

### API 说明

```csharp
// 初始化图表（可选传入标题）
void Initialize(string title = "")

// 设置/获取当前显示的指令类型
void SetOrderType(OrderType type)
OrderType GetOrderType()

// 刷新 K 线数据
void RefreshData(List<KLineData> klineHistory)

// 设置图表标题
void SetTitle(string title)

// 清空数据
void Clear()
```

### 使用示例

**InfoPanel 中（三个并列图表）**：
```csharp
// 初始化
chartAtk.Initialize("ATK");
chartAtk.SetOrderType(OrderType.ATK);

// 刷新数据
chartAtk.RefreshData(data.Market.KLineHistory[OrderType.ATK]);
```

**SpotMarketTab 中（单个可切换图表）**：
```csharp
// 初始化
klineChart.Initialize();

// 切换显示类型
klineChart.SetOrderType(OrderType.DEF);
klineChart.SetTitle("DEF");
klineChart.RefreshData(data.Market.KLineHistory[OrderType.DEF]);
```

### 注意事项

1. **Prefab 预配置（推荐）**：在 Prefab 中预先添加 CandlestickChart 并配置样式，可在编辑器中预览
2. **运行时兼容**：如果未绑定 chart 字段，`Initialize()` 时会自动创建 CandlestickChart
3. **RectTransform 尺寸**：确保父容器有足够空间，图表会自动填充 RectTransform 区域
4. **数据格式**：使用 `MarketData.KLineHistory[OrderType]` 中的 `List<KLineData>`

---

## 十五、SpotMarketTab 和 FuturesMarketTab 子组件

这两个组件是 MonoBehaviour，分别挂在 MarketPanel 的 SpotContent 和 FuturesContent 节点上。

**绑定字段详见第四节 MarketPanel 的绑定说明。**

---

# 第二部分：场景配置

## 一、Battle.unity 场景配置

### 1.1 UIRoot 层级结构

在场景中创建 UI 层级结构（由 UIService 自动管理，但需要手动创建 UIRoot）：

```
Battle (Scene)
├── GameRoot (已存在)
│   └── ...
│
├── UIRoot (新建空对象)
│   └── (UI Canvas 将在运行时由 UIService 自动创建)
│
├── EventSystem (确保存在且唯一)
│   └── Standalone Input Module
│
└── Battlefield (战场 3D 内容)
    └── ...
```

### 1.2 创建 UIRoot

1. Hierarchy → Create Empty → 重命名为 `UIRoot`
2. Position: (0, 0, 0)
3. 不需要添加任何组件，UIService 会自动创建 Canvas 层级

### 1.3 EventSystem 检查

1. 确保场景中有且仅有一个 `EventSystem`
2. 如果没有：Hierarchy → UI → Event System
3. 如果有多个：删除多余的

---

## 二、战场 3D 对象配置

### 2.1 已有 Prefab

以下 Prefab 已存在于 `Assets/Resources/Prefabs/WarBroker/Battlefield/`：

| Prefab | 用途 |
|--------|------|
| GeneralBase.prefab | 将军底座 |
| TinSoldier.prefab | 锡兵模型 |
| FrontlineGrid.prefab | 战线格子 |

### 2.2 战场层级结构

在 Battle.unity 场景中创建：

```
Battlefield (空对象)
├── Camera (战场俯视镜头)
│   - 可选：添加 CinemachineVirtualCamera 实现平滑旋转
│
├── Table (桌面模型 - 需要创建或导入)
│
├── LeftFlank (左翼战线)
│   ├── Grid_1 (FrontlineGrid 实例)
│   ├── Grid_2
│   ├── Grid_3 (中心位置)
│   ├── Grid_4
│   ├── Grid_5
│   ├── AllyGeneral (GeneralBase 实例 + TinSoldier 子对象)
│   └── EnemyGeneral (GeneralBase 实例 + TinSoldier 子对象)
│
├── Center (中军战线，同上结构)
│
└── RightFlank (右翼战线，同上结构)
```

### 2.3 将军对象配置

每个将军对象需要：

1. **底座** (GeneralBase 实例)
   - 添加 `Collider` 组件（用于点击检测）
   - 添加点击事件脚本（见下方）

2. **锡兵子对象** (最多 20 个 TinSoldier)
   - 数量 = 当前兵力
   - 运行时由代码动态生成/隐藏

### 2.4 将军点击交互脚本

需要创建一个点击检测脚本：

```csharp
// 建议路径: Assets/Scripts/WarBroker/Battlefield/GeneralClickHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class GeneralClickHandler : MonoBehaviour, IPointerClickHandler
{
    public string GeneralId; // 在 Inspector 中设置

    public void OnPointerClick(PointerEventData eventData)
    {
        var uiService = GameRoot.Instance.uIService;
        var panel = uiService.ShowWindow<GeneralDetailPanel>("GeneralDetailPanel");

        // 获取将军数据并设置
        var gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        var battleData = gameplayManager.GetBattleData();
        var general = battleData.AllyGenerals.Find(g => g.GeneralId == GeneralId);

        if (panel != null && general != null)
        {
            panel.SetGeneral(general);
        }
    }
}
```

**注意**: 这个脚本需要手动创建。

---

# 第三部分：配置文件检查

## 已存在的配置文件

以下配置文件已存在于 `Assets/Resources/Config/WarBroker/`：

| 文件 | 状态 | 说明 |
|------|------|------|
| GameBalanceConfig.asset | ✅ 已存在 | 游戏平衡参数 |
| OrderConfig.asset | ✅ 已存在 | 指令配置（ATK/DEF/RET） |
| SkillConfig.asset | ✅ 已存在 | 技能配置 |
| GeneralConfig.asset | ✅ 已存在 | 将军配置 |
| Campaign_Tutorial.asset | ✅ 已存在 | 教程战役配置 |

**无需额外创建配置文件。**

---

# 第四部分：美术资源需求

## 一、必需的 UI 图片资源

| 资源 | 用途 | 建议规格 | 优先级 |
|------|------|---------|--------|
| panel_bg.png | 面板背景 | 9-Slice, 100x100+ | 高 |
| popup_bg.png | 弹窗背景 | 9-Slice, 100x100+ | 高 |
| button_normal.png | 按钮正常态 | 9-Slice, 80x40+ | 高 |
| button_hover.png | 按钮悬停态 | 9-Slice, 80x40+ | 中 |
| button_pressed.png | 按钮按下态 | 9-Slice, 80x40+ | 中 |
| toggle_on.png | Toggle 开启态 | 32x32 | 高 |
| toggle_off.png | Toggle 关闭态 | 32x32 | 高 |
| slider_bg.png | 滑动条背景 | 9-Slice, 200x20 | 中 |
| slider_fill.png | 滑动条填充 | 9-Slice, 200x20 | 中 |
| intent_bubble.png | 意图气泡背景 | 64x64 | 中 |

## 二、将军头像

| 资源 | 用途 | 建议规格 |
|------|------|---------|
| portrait_general_01.png | 己方将军1 | 256x256 PNG |
| portrait_general_02.png | 己方将军2 | 256x256 PNG |
| portrait_general_03.png | 己方将军3 | 256x256 PNG |
| portrait_enemy_01.png | 敌方将军1 | 256x256 PNG |
| portrait_enemy_02.png | 敌方将军2 | 256x256 PNG |
| portrait_enemy_03.png | 敌方将军3 | 256x256 PNG |

## 三、字体

| 资源 | 用途 | 说明 |
|------|------|------|
| MainFont SDF | 主要 UI 文本 | 需要创建 TMP Font Asset |
| NumberFont SDF | 数字显示 | 用于价格、金额（可选） |

### 创建 TMP Font Asset

1. Window → TextMeshPro → Font Asset Creator
2. 选择源字体文件 (.ttf/.otf)
3. Atlas Resolution: 2048x2048
4. Character Set: Unicode Range (包含中文)
5. 点击 Generate Font Atlas
6. 保存到 `Assets/Fonts/` 目录

## 四、3D 模型/材质（可选升级）

| 资源 | 当前状态 | 说明 |
|------|---------|------|
| 锡兵模型 | 有基础 | 可替换为精细模型 |
| 将军底座 | 有基础 | 可添加材质特效 |
| 桌面/沙盘 | 待创建 | 战场背景装饰 |
| 锡兵材质（蓝色） | 待创建 | 己方单位 |
| 锡兵材质（红色） | 待创建 | 敌方单位 |
| 格子高亮材质 | 待创建 | 选中/悬停效果 |

---

# 第五部分：完整操作清单

## 执行顺序总览

### 阶段 1：环境准备
- [ ] 导入 TextMesh Pro Essential Resources
- [ ] 创建 Prefab 目录结构
- [ ] 创建基础 UI 图片资源（或使用占位图）
- [ ] 创建/导入字体并生成 TMP Font Asset

### 阶段 2：基础 Items
- [ ] 创建 BattleResultItem.prefab
- [ ] 创建 FuturesPositionItem.prefab

### 阶段 3：主窗口
- [ ] 创建 GameplayWindow.prefab

### 阶段 4：面板
- [ ] 创建 MarketPanel.prefab（含 SpotMarketTab、FuturesMarketTab）
- [ ] 创建 InfoPanel.prefab
- [ ] 创建 GeneralDetailPanel.prefab
- [ ] 创建 ObjectivePanel.prefab
- [ ] （可选）创建 TopStatusBar.prefab
- [ ] （可选）创建 BottomBar.prefab

### 阶段 5：弹窗
- [ ] 创建 EventPopup.prefab
- [ ] 创建 BattleResultPopup.prefab
- [ ] 创建 CampaignEndPopup.prefab

### 阶段 6：场景配置
- [ ] 在 Battle.unity 创建 UIRoot 对象
- [ ] 确保 EventSystem 存在且唯一
- [ ] 搭建战场 3D 层级结构
- [ ] 创建 GeneralClickHandler.cs 脚本
- [ ] 配置将军点击交互

### 阶段 7：测试验证
- [ ] 运行场景，检查 UI 显示
- [ ] 测试按钮点击响应
- [ ] 测试 Tab 切换
- [ ] 测试弹窗显示/关闭
- [ ] 测试将军点击→详情面板
- [ ] 完整游戏流程测试

---

## 验证清单

创建完成后，在 Unity 中运行以下检查：

- [ ] 所有 Prefab 都保存到正确路径
- [ ] 所有 Binder 字段都已绑定（Inspector 中无空引用）
- [ ] 运行场景无 NullReferenceException
- [ ] 点击按钮有响应
- [ ] Toggle 切换正常工作
- [ ] 弹窗正确显示和关闭
- [ ] 文本内容正确显示（无乱码）
- [ ] 点击将军可打开详情面板
- [ ] 现货买卖功能正常
- [ ] 期货开仓/平仓功能正常
- [ ] 银行借贷功能正常
- [ ] 回合结束按钮正常
- [ ] 战斗结算弹窗正常显示
- [ ] Tooltip 悬停显示正常

---

## 十六、TooltipPanel（轻量 Tooltip 面板）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Panels/TooltipPanel.prefab`

TooltipPanel 是一个轻量级的 Paradox 风格 Tooltip 系统，利用现有的 UIListener 组件实现悬停显示。

### 创建步骤

1. **创建空对象** → 重命名为 `TooltipPanel`

2. **配置 RectTransform**
   - Anchor: 左下角 (Left-Bottom)
   - Pivot: (0, 1) - 左上角为锚点，便于跟随鼠标
   - Width: 自适应（使用 Content Size Fitter）
   - Height: 自适应

3. **添加脚本**
   - 添加组件: `TooltipPanelBinder`（仅 Binder）

4. **创建子结构**

```
TooltipPanel (RectTransform)
│   - 添加 Image 组件作为背景
│   - 添加 Vertical Layout Group
│     - Padding: 10
│     - Spacing: 5
│     - Child Force Expand: Width=true, Height=false
│   - 添加 Content Size Fitter
│     - Horizontal Fit: Preferred Size
│     - Vertical Fit: Preferred Size
│
├── TxtTitle (TextMeshPro - Text)
│   - Font Size: 16
│   - Font Style: Bold
│   - Color: 白色或高亮色
│   - 添加 Layout Element
│     - Preferred Width: 200 (最小宽度)
│
└── TxtContent (TextMeshPro - Text)
    - Font Size: 14
    - Color: 浅灰色
    - Rich Text: true (支持颜色标签)
    - 添加 Layout Element
      - Preferred Width: 200
      - Flexible Width: 1
```

5. **绑定字段** (在 TooltipPanelBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtTitle | TxtTitle |
| txtContent | TxtContent |
| panelRect | TooltipPanel (自身的 RectTransform) |

6. **保存 Prefab**

### 使用方式

TooltipPanel 通过 UIListener 组件的 `onEnter` 和 `onExit` 事件触发。在 GameplayWindow 中已预设以下 Tooltip：

| 目标元素 | Tooltip 内容 |
|---------|-------------|
| 现金图标 (ImgCashIcon) | 静态说明："现金含义" |
| 现金数值 (TxtCash) | 动态内容：下回合固定支出（利息+仓储费） |
| ATK 图标/数值 | 静态说明："进攻令 (ATK)" |
| DEF 图标/数值 | 静态说明："防守令 (DEF)" |
| RET 图标/数值 | 静态说明："撤退令 (RET)" |
| 结束回合按钮 | 动态内容：预计扣除费用 |

### 添加自定义 Tooltip

在任意 WindowBase 子类中，可以使用以下方式添加 Tooltip：

```csharp
// 静态内容
AddTooltip(targetGameObject, "标题", "内容描述");

// 动态内容（每次悬停时重新计算）
AddTooltip(targetGameObject, "标题", () => GetDynamicContent());
```

### 注意事项

1. **UILayer**: TooltipPanel 使用 `UILayer.Popup` 确保显示在最上层
2. **边界检测**: 自动检测屏幕边界，防止 Tooltip 超出屏幕
3. **跟随鼠标**: 每帧更新位置，跟随鼠标移动
4. **Rich Text**: 支持 TextMeshPro 富文本标签（如 `<color=#FF6666>红色文字</color>`）
