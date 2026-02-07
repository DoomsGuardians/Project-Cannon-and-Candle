using System;
using System.Collections.Generic;

/// <summary>
/// 战役存档数据（局内存档）
/// 用于保存和恢复进行中的战役状态
/// </summary>
[Serializable]
public class CampaignSaveData
{
    // 存档元信息
    public string SaveId;
    public string SaveName;
    public string SaveTime;  // ISO 8601 格式
    public int SaveVersion = 1;

    // 战役信息
    public string CampaignId;
    public int CurrentTurn;
    public int CurrentPhase;

    // TODO: 完整的局内存档数据结构
    // ============================================
    // 以下数据结构预留，待实现完整的局内存档功能
    // ============================================

    // public PlayerSaveData Player;
    // public MarketSaveData Market;
    // public BattleSaveData Battle;
    // public VictorSaveData Victor;
    // public List<CommissionSaveData> ActiveCommissions;
    // public RandomEventSaveData ActiveEvent;

    /// <summary>创建新的存档数据</summary>
    public static CampaignSaveData Create(string campaignId, string saveName)
    {
        return new CampaignSaveData
        {
            SaveId = Guid.NewGuid().ToString(),
            SaveName = saveName,
            SaveTime = DateTime.UtcNow.ToString("o"),
            SaveVersion = 1,
            CampaignId = campaignId,
            CurrentTurn = 1,
            CurrentPhase = 0
        };
    }
}

// ############################################################################
// #                        局内存档实现计划                                    #
// ############################################################################
//
// ## 概述
//
// 实现回合开始时的自动存档功能，保留最近3个回合的存档（滚动覆盖）。
// 存档时机：每回合 StartTurn() 执行完毕后，在进入 MarketPhase 之前。
//
// ## 存档文件结构
//
// {Application.persistentDataPath}/saves/
// ├── meta.lvtsav                    // 局外存档（已实现）
// ├── autosave_slot0.lvtsav          // 自动存档槽位 0（最新）
// ├── autosave_slot1.lvtsav          // 自动存档槽位 1（次新）
// └── autosave_slot2.lvtsav          // 自动存档槽位 2（最旧）
//
// ## 滚动存档逻辑
//
// 每次自动保存时：
// 1. 删除 slot2（最旧）
// 2. slot1 → slot2
// 3. slot0 → slot1
// 4. 新存档 → slot0
//
// ## 数据结构设计
//
// ### CampaignSaveData（主结构，已定义）
// - SaveId: string                   // GUID
// - SaveName: string                 // "自动存档 - 第X回合"
// - SaveTime: string                 // ISO 8601
// - SaveVersion: int                 // 版本号
// - CampaignId: string               // 战役配置ID
// - CurrentTurn: int                 // 当前回合
// - CurrentPhase: int                // 当前阶段（TurnPhase枚举值）
// - Player: PlayerSaveData           // 玩家数据
// - Market: MarketSaveData           // 市场数据
// - Battle: BattleSaveData           // 战场数据
// - Victor: VictorSaveData           // 维克多数据
// - Commissions: CommissionsSaveData // 委托任务数据
// - Event: EventSaveData             // 随机事件数据
// - TurnHistory: List<TurnRecord>    // 回合历史（可选，用于K线显示）
//
// ### PlayerSaveData
// - Cash: float
// - Inventory: List<OrderInventoryItem>      // ATK/DEF/RET 库存
// - BankDebt: float
// - FuturesPositions: List<FuturesContractSaveData>
// - AuditValue: int  // [已禁用] 审计值系统
// - EntryFee: float
//
// ### MarketSaveData
// - CurrentPrices: List<OrderPriceItem>      // 当前价格
// - MarketInventory: List<OrderInventoryItem> // 市场库存
// - InitialFloat: List<OrderInventoryItem>   // 初始流通盘
// - BetaCarry: List<OrderPriceItem>          // Beta累积值
// - LastWeekBurn: List<OrderPriceItem>       // 上周消耗
// - AccumulatedBurn: List<OrderPriceItem>    // 累计消耗
// - KLineHistory: List<KLineHistorySaveData> // K线历史（按OrderType分组）
// - LastReplenishTurn: int
//
// ### BattleSaveData
// - Frontlines: List<FrontlineSaveData>      // 3条战线
// - AllyGenerals: List<GeneralSaveData>      // 己方将军
// - EnemyGenerals: List<GeneralSaveData>     // 敌方将军
// - CurrentReserves: int                     // 己方后备役
// - EnemyReserves: int                       // 敌方后备役
//
// ### FrontlineSaveData
// - Position: FrontlinePosition              // Left/Center/Right
// - GridOwners: GridOwner[]                  // 5个格子归属
// - StagnantTurns: int
// - TurnsAtEnemyBase: int
// - TurnsAtAllyBase: int
//
// ### GeneralSaveData
// - GeneralId: string                        // 用于重新绑定Config
// - Position: FrontlinePosition
// - GridPosition: int
// - Troops: int
// - Trust: int
// - Morale: int
// - AssignedOrder: OrderType?
// - DefaultIntent: OrderType?
// - FinalIntent: OrderType?
// - IntentSource: IntentSource
// - ReorganizeTurns: int
// - LastOrder: OrderType?
// - ConsecutiveOrderCount: int
//
// ### FuturesContractSaveData
// - ContractId: int
// - TargetOrder: OrderType
// - Direction: FuturesDirection
// - OpenPrice: float
// - Quantity: int
// - ExpirationTurn: int
// - Margin: float
//
// ### VictorSaveData
// - Cash: float
// - Holdings: List<OrderInventoryItem>
// - FuturesPositions: List<FuturesContractSaveData>
// - Debt: float
// - NextContractId: int
// - Memory: VictorMemorySaveData             // 维克多记忆状态
//
// ### VictorMemorySaveData
// - CurrentStrategy: VictorStrategy
// - StrategyLockedUntilTurn: int
// - LastPlayerNetWorth: float
// - PlayerProfitStreak: int
// - SuspicionLevel: float
// - ActiveDeceptionPlan: DeceptionPlanSaveData?
// - RecentPriceChanges: List<PriceChangeSaveData>
//
// ### CommissionsSaveData
// - ActiveCommissions: List<CommissionStateSaveData>
// - CompletedThisCampaign: List<string>      // 已完成的委托ID
//
// ### CommissionStateSaveData
// - CommissionId: string
// - IsCompleted: bool
// - Progress: int                            // 进度值（如适用）
//
// ### EventSaveData
// - ActiveEventId: string                    // 当前事件ID（null表示无事件）
// - RemainingTurns: int
//
// ### KLineHistorySaveData
// - OrderType: OrderType
// - KLines: List<KLineData>
//
// ## SaveSystem 新增方法
//
// ### 自动存档相关
// ```csharp
// // 自动存档（回合开始时调用）
// public void AutoSaveCampaign(CampaignRuntimeData data)
// {
//     // 1. 滚动存档槽位
//     RotateAutoSaveSlots();
//
//     // 2. 创建存档数据
//     var saveData = CreateCampaignSaveData(data);
//
//     // 3. 保存到 slot0
//     SaveToSlot(0, saveData);
// }
//
// // 滚动存档槽位
// private void RotateAutoSaveSlots()
// {
//     DeleteSlot(2);
//     MoveSlot(1, 2);
//     MoveSlot(0, 1);
// }
//
// // 获取自动存档列表
// public List<CampaignSaveData> GetAutoSaveList()
//
// // 从自动存档加载
// public CampaignRuntimeData LoadAutoSave(int slotIndex)
// ```
//
// ### 序列化方法
// ```csharp
// // 从运行时数据创建存档数据
// private CampaignSaveData CreateCampaignSaveData(CampaignRuntimeData data)
// {
//     return new CampaignSaveData
//     {
//         SaveId = Guid.NewGuid().ToString(),
//         SaveName = $"自动存档 - 第{data.CurrentTurn}回合",
//         SaveTime = DateTime.UtcNow.ToString("o"),
//         CampaignId = data.Config.CampaignId,
//         CurrentTurn = data.CurrentTurn,
//         CurrentPhase = (int)data.CurrentPhase,
//         Player = SerializePlayer(data.Player),
//         Market = SerializeMarket(data.Market),
//         Battle = SerializeBattle(data.Battle),
//         Victor = SerializeVictor(data.VictorLedger, data.VictorMemory),
//         // ...
//     };
// }
//
// // Dictionary → List 转换辅助方法
// private List<OrderInventoryItem> SerializeInventory(Dictionary<OrderType, int> dict)
// private List<OrderPriceItem> SerializePrices(Dictionary<OrderType, float> dict)
// ```
//
// ### 反序列化方法
// ```csharp
// // 从存档数据恢复运行时数据
// public CampaignRuntimeData RestoreCampaignData(CampaignSaveData saveData)
// {
//     // 1. 加载战役配置
//     var config = LoadCampaignConfig(saveData.CampaignId);
//
//     // 2. 创建运行时数据（不调用InitFromConfig）
//     var data = new CampaignRuntimeData();
//     data.Config = config;
//     data.CurrentTurn = saveData.CurrentTurn;
//     data.CurrentPhase = (TurnPhase)saveData.CurrentPhase;
//
//     // 3. 恢复各子系统数据
//     data.Player = DeserializePlayer(saveData.Player);
//     data.Market = DeserializeMarket(saveData.Market);
//     data.Battle = DeserializeBattle(saveData.Battle, config);
//     // ...
//
//     return data;
// }
//
// // List → Dictionary 转换辅助方法
// private Dictionary<OrderType, int> DeserializeInventory(List<OrderInventoryItem> list)
// private Dictionary<OrderType, float> DeserializePrices(List<OrderPriceItem> list)
// ```
//
// ## CampaignSystem 集成
//
// ### StartTurn() 修改
// ```csharp
// public void StartTurn()
// {
//     // ... 现有逻辑 ...
//
//     // 在进入玩家阶段前自动存档
//     var saveSystem = GameRoot.Instance.saveSystem;
//     if (saveSystem != null && Data.CurrentTurn > 1)  // 第1回合不存档
//     {
//         saveSystem.AutoSaveCampaign(Data);
//     }
//
//     EnterPlayerPhase();
// }
// ```
//
// ### 新增 LoadFromSave() 方法
// ```csharp
// public void LoadFromSave(int autoSaveSlot)
// {
//     var saveSystem = GameRoot.Instance.saveSystem;
//     var saveData = saveSystem.GetAutoSave(autoSaveSlot);
//     if (saveData == null) return;
//
//     // 恢复数据
//     Data = saveSystem.RestoreCampaignData(saveData);
//
//     // 重新绑定系统引用
//     marketSystem.SetRuntimeData(Data);
//     battleSystem.SetRuntimeData(Data);
//     intentSystem.SetCampaignData(Data);
//     commissionSystem.RestoreState(saveData.Commissions);
//     victorAISystem.RestoreState(saveData.Victor);
//
//     // 刷新UI
//     eventService.SendMessage(OnSaveLoaded, Data.CurrentTurn, null);
//
//     // 进入玩家阶段
//     EnterPlayerPhase();
// }
// ```
//
// ## UI 集成（可选）
//
// ### 暂停菜单添加"读取存档"按钮
// - 显示最近3个自动存档
// - 显示存档信息：回合数、时间、净资产
// - 确认对话框：读取后当前进度将丢失
//
// ## 实现步骤
//
// ### 第一阶段：数据结构（约1小时）
// 1. 取消注释并完善 CampaignSaveData 中的子数据结构
// 2. 添加缺失的 SaveData 类（VictorMemorySaveData 等）
// 3. 确保所有字段都是可序列化的
//
// ### 第二阶段：序列化（约2小时）
// 1. 实现 SaveSystem.CreateCampaignSaveData()
// 2. 实现各子系统的序列化方法
// 3. 实现 Dictionary ↔ List 转换辅助方法
// 4. 实现文件读写和槽位管理
//
// ### 第三阶段：反序列化（约2小时）
// 1. 实现 SaveSystem.RestoreCampaignData()
// 2. 实现各子系统的反序列化方法
// 3. 处理 ScriptableObject 引用重新绑定
// 4. 实现 CampaignSystem.LoadFromSave()
//
// ### 第四阶段：集成测试（约1小时）
// 1. 在 StartTurn() 中添加自动存档调用
// 2. 测试存档文件生成
// 3. 测试读取存档后游戏状态正确性
// 4. 测试滚动覆盖逻辑
//
// ### 第五阶段：UI（可选，约1小时）
// 1. 暂停菜单添加读取存档入口
// 2. 存档列表显示
// 3. 确认对话框
//
// ## 注意事项
//
// 1. **存档时机**：必须在回合开始、所有初始化完成后存档，确保状态一致
// 2. **第1回合不存档**：避免存档无意义的初始状态
// 3. **Config引用**：只保存ID，加载时重新从Resources加载
// 4. **K线历史**：可选保存，用于恢复后显示历史图表
// 5. **版本兼容**：SaveVersion 字段用于未来数据结构变更时的迁移
// 6. **错误处理**：存档损坏时应能优雅降级，不影响游戏继续
//
// ############################################################################
