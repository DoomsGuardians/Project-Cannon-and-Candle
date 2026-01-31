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
│   ├── TxtTurn (TextMeshPro - Text)
│   ├── TxtPhase (TextMeshPro - Text)
│   ├── TxtCash (TextMeshPro - Text)
│   ├── TxtNetWorth (TextMeshPro - Text)
│   └── TxtAudit (TextMeshPro - Text)
│
├── ContentArea (RectTransform)
│   └── (用于加载 MarketPanel/InfoPanel 等)
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
| txtCash | TopBar/TxtCash |
| txtNetWorth | TopBar/TxtNetWorth |
| txtAudit | TopBar/TxtAudit |
| btnMarket | TabButtons/BtnMarket |
| btnIntel | TabButtons/BtnIntel |
| contentArea | ContentArea |
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
│   │   └── BtnAtkSell (Button) - "卖出"
│   │
│   ├── DefRow (同上结构)
│   │   ├── TxtDefLabel (TMP) - "DEF"
│   │   ├── TxtDefPrice (TMP)
│   │   ├── TxtDefHolding (TMP)
│   │   ├── BtnDefBuy (Button)
│   │   └── BtnDefSell (Button)
│   │
│   └── RetRow (同上结构)
│       ├── TxtRetLabel (TMP) - "RET"
│       ├── TxtRetPrice (TMP)
│       ├── TxtRetStock (TMP)
│       ├── BtnRetBuy (Button)
│       └── BtnRetSell (Button)
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
| txtAtkPrice | SpotContent/AtkRow/TxtAtkPrice |
| txtAtkHolding | SpotContent/AtkRow/TxtAtkHolding |
| btnAtkBuy | SpotContent/AtkRow/BtnAtkBuy |
| btnAtkSell | SpotContent/AtkRow/BtnAtkSell |
| txtDefPrice | SpotContent/DefRow/TxtDefPrice |
| txtDefHolding | SpotContent/DefRow/TxtDefHolding |
| btnDefBuy | SpotContent/DefRow/BtnDefBuy |
| btnDefSell | SpotContent/DefRow/BtnDefSell |
| txtRetPrice | SpotContent/RetRow/TxtRetPrice |
| txtRetHolding | SpotContent/RetRow/TxtRetHolding |
| btnRetBuy | SpotContent/RetRow/BtnRetBuy |
| btnRetSell | SpotContent/RetRow/BtnRetSell |

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
    │   │   - 添加组件: KLineChartView
    │   └── (XCharts CandlestickChart 运行时自动创建)
    │
    ├── ChartDef (RectTransform)
    │   │   - Height: 150
    │   │   - 添加组件: KLineChartView
    │   └── (XCharts CandlestickChart 运行时自动创建)
    │
    └── ChartRet (RectTransform)
        │   - Height: 150
        │   - 添加组件: KLineChartView
        └── (XCharts CandlestickChart 运行时自动创建)
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

## 六、GeneralDetailPanel（将军详情面板）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Panels/GeneralDetailPanel.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `GeneralDetailPanel`

2. **配置 RectTransform**
   - Anchor: Right Stretch
   - Width: `400`
   - 用于从右侧滑入

3. **添加脚本**
   - 添加组件: `GeneralDetailPanelBinder`（仅 Binder）

4. **创建子结构**

```
GeneralDetailPanel (RectTransform)
├── Header
│   ├── TxtName (TMP) - "张将军"
│   ├── TxtPersonality (TMP) - "狂热型"
│   └── TxtPosition (TMP) - "左翼"
│
├── StatsArea (Vertical Layout Group)
│   ├── HPRow
│   │   ├── TxtHP (TMP) - "兵力: 16/20"
│   │   └── SliderHP (Slider)
│   │
│   ├── TrustRow
│   │   ├── TxtTrust (TMP) - "信任: 50"
│   │   └── SliderTrust (Slider)
│   │
│   └── MoraleRow
│       ├── TxtMorale (TMP) - "士气: 60"
│       └── SliderMorale (Slider)
│
├── StatusArea
│   ├── TxtStatus (TMP) - "状态: 正常"
│   └── TxtSkills (TMP) - "技能: 突击"
│
├── IntentArea
│   ├── ImgIntentBubble (Image) - 意图气泡背景
│   ├── TxtIntent (TMP) - "ATK"
│   └── TxtIntentSource (TMP) - "来源: 性格倾向"
│
├── ActionArea (Vertical Layout Group)
│   ├── ReinforceRow
│   │   ├── BtnReinforce (Button) - "强化"
│   │   └── TxtReinforceCost (TMP) - "消耗: 1份现货"
│   │
│   └── OverrideRow
│       ├── BtnOverride (Button) - "篡改意图"
│       ├── DdOverrideType (TMP_Dropdown) - ATK/DEF/RET
│       └── TxtOverrideCost (TMP) - "消耗: 3份现货"
│
└── BtnClose (Button) - "X" 或 "关闭"
    └── Text (TMP)
```

5. **绑定字段** (在 GeneralDetailPanelBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtName | Header/TxtName |
| txtPersonality | Header/TxtPersonality |
| txtPosition | Header/TxtPosition |
| sliderHP | StatsArea/HPRow/SliderHP |
| sliderTrust | StatsArea/TrustRow/SliderTrust |
| sliderMorale | StatsArea/MoraleRow/SliderMorale |
| txtHP | StatsArea/HPRow/TxtHP |
| txtTrust | StatsArea/TrustRow/TxtTrust |
| txtMorale | StatsArea/MoraleRow/TxtMorale |
| txtStatus | StatusArea/TxtStatus |
| txtSkills | StatusArea/TxtSkills |
| imgIntentBubble | IntentArea/ImgIntentBubble |
| txtIntent | IntentArea/TxtIntent |
| txtIntentSource | IntentArea/TxtIntentSource |
| btnReinforce | ActionArea/ReinforceRow/BtnReinforce |
| txtReinforceCost | ActionArea/ReinforceRow/TxtReinforceCost |
| btnOverride | ActionArea/OverrideRow/BtnOverride |
| ddOverrideType | ActionArea/OverrideRow/DdOverrideType |
| txtOverrideCost | ActionArea/OverrideRow/TxtOverrideCost |
| btnClose | BtnClose |

6. **保存 Prefab**

---

## 七、ObjectivePanel（目标面板）

**保存路径**: `Assets/Resources/Prefabs/WarBroker/UI/Panels/ObjectivePanel.prefab`

### 创建步骤

1. **创建空对象** → 重命名为 `ObjectivePanel`

2. **添加脚本**
   - 添加组件: `ObjectivePanelBinder`（仅 Binder）

3. **创建子结构**

```
ObjectivePanel (RectTransform)
├── Header
│   └── TxtObjectiveTitle (TMP) - "战役目标"
│
├── MainObjective
│   ├── TxtObjectiveDescription (TMP) - "净资产: 1500"
│   ├── TxtPnL (TMP) - "P&L: +350 (+35%)"
│   └── TxtProgress (TMP) - "回合 3/12"
│
└── CommissionList
    ├── TxtWinWar (TMP) - "赢下战争 $200 [0%]"
    ├── TxtShortCountry (TMP) - "做空祖国 $500 [0%]"
    ├── TxtTraitor (TMP) - "卖国求荣 $300 [未达成]"
    └── TxtMeatGrinder (TMP) - "绞肉机 $150 [0%]"
```

4. **绑定字段** (在 ObjectivePanelBinder 组件上)

| Inspector 字段 | 拖入对象 |
|---------------|---------|
| txtObjectiveTitle | Header/TxtObjectiveTitle |
| txtObjectiveDescription | MainObjective/TxtObjectiveDescription |
| txtProgress | MainObjective/TxtProgress |
| txtPnL | MainObjective/TxtPnL |
| commissionListRoot | CommissionList |
| txtWinWar | CommissionList/TxtWinWar |
| txtShortCountry | CommissionList/TxtShortCountry |
| txtTraitor | CommissionList/TxtTraitor |
| txtMeatGrinder | CommissionList/TxtMeatGrinder |

5. **保存 Prefab**

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

2. **主窗口**
   - GameplayWindow

3. **面板**（按使用频率）
   - MarketPanel
   - InfoPanel
   - TopStatusBar
   - BottomBar
   - GeneralDetailPanel
   - ObjectivePanel

4. **弹窗**
   - EventPopup
   - BattleResultPopup
   - CampaignEndPopup

---

## 十三、SpotMarketTab 子组件

**位置**: 嵌入在 MarketPanel 的 SpotContent 节点中

SpotMarketTab 是一个独立的 MonoBehaviour，需要在 SpotContent 上添加并绑定字段。

### SpotMarketTab Prefab 结构

```
SpotContent (RectTransform)
│   ※ 在此节点添加 SpotMarketTab 组件
│
├── Header (TMP) - "现货市场"
│
├── AtkRow (Horizontal Layout Group)
│   ├── TxtAtkLabel (TMP) - "ATK"
│   ├── TxtAtkPrice (TMP) - 价格
│   ├── TxtAtkHolding (TMP) - 持有量
│   ├── BtnAtkBuy (Button) - "买入"
│   ├── BtnAtkSell (Button) - "卖出"
│   └── BtnAtkChart (Button) - "K线"  ← 新增
│
├── DefRow (Horizontal Layout Group)
│   ├── TxtDefLabel (TMP) - "DEF"
│   ├── TxtDefPrice (TMP) - 价格
│   ├── TxtDefHolding (TMP) - 持有量
│   ├── BtnDefBuy (Button) - "买入"
│   ├── BtnDefSell (Button) - "卖出"
│   └── BtnDefChart (Button) - "K线"  ← 新增
│
├── RetRow (Horizontal Layout Group)
│   ├── TxtRetLabel (TMP) - "RET"
│   ├── TxtRetPrice (TMP) - 价格
│   ├── TxtRetHolding (TMP) - 持有量
│   ├── BtnRetBuy (Button) - "买入"
│   ├── BtnRetSell (Button) - "卖出"
│   └── BtnRetChart (Button) - "K线"  ← 新增
│
└── ChartArea (RectTransform)
    │   - Height: 200
    │
    └── KLineChart (RectTransform)
        │   - 添加组件: KLineChartView
        │   - Stretch to fill ChartArea
        └── (XCharts CandlestickChart 运行时自动创建)
```

### SpotMarketTab 需绑定的字段

| Inspector 字段 | 类型 | 说明 |
|---------------|------|------|
| txtAtkPrice | TMP_Text | ATK 价格 |
| txtAtkHolding | TMP_Text | ATK 持有量 |
| btnAtkBuy | Button | ATK 买入 |
| btnAtkSell | Button | ATK 卖出 |
| btnAtkChart | Button | ATK K线切换按钮 |
| txtDefPrice | TMP_Text | DEF 价格 |
| txtDefHolding | TMP_Text | DEF 持有量 |
| btnDefBuy | Button | DEF 买入 |
| btnDefSell | Button | DEF 卖出 |
| btnDefChart | Button | DEF K线切换按钮 |
| txtRetPrice | TMP_Text | RET 价格 |
| txtRetHolding | TMP_Text | RET 持有量 |
| btnRetBuy | Button | RET 买入 |
| btnRetSell | Button | RET 卖出 |
| btnRetChart | Button | RET K线切换按钮 |
| klineChart | KLineChartView | K线图组件 |

---

## 十四、KLineChartView 组件说明

**脚本路径**: `Assets/Scripts/WarBroker/UI/Base/KLineChartView.cs`

KLineChartView 是封装 XCharts CandlestickChart 的通用 K 线图组件，用于显示 ATK/DEF/RET 指令的价格走势。

### 创建步骤

1. **创建空 GameObject**
   - 添加 `RectTransform` 组件（默认已有）
   - 设置合适的尺寸（建议最小 200x150）

2. **添加 KLineChartView 组件**
   - 在 Inspector 中 Add Component → 搜索 `KLineChartView`
   - **不需要**手动添加 CandlestickChart，运行时会自动创建

3. **配置 Inspector 属性**（可选）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| chartTitle | string | "" | 图表标题（如 "ATK"） |
| riseColor | Color32 | 红色 (235,84,84) | 上涨颜色 |
| fallColor | Color32 | 绿色 (68,198,127) | 下跌颜色 |

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

1. **RectTransform 尺寸**：确保父容器有足够空间，图表会自动填充 RectTransform 区域
2. **运行时创建**：CandlestickChart 组件在 `Initialize()` 时自动添加，无需手动添加
3. **数据格式**：使用 `MarketData.KLineHistory[OrderType]` 中的 `List<KLineData>`

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
