using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档系统
/// 管理局外存档（Meta-game Data）的读写和战役追踪
/// </summary>
public class SaveSystem : ILogic
{
    private const int CURRENT_SAVE_VERSION = 1;
    private const string SAVE_FOLDER = "saves";
    private const string META_SAVE_FILE = "meta.lvtsav";

    /// <summary>当前局外存档数据</summary>
    public MetaSaveData MetaData { get; private set; }

    /// <summary>当前战役追踪记录</summary>
    private CampaignHistoryRecord currentCampaignRecord;

    /// <summary>当前回合回放数据</summary>
    private TurnReplayData currentTurnReplay;

    /// <summary>是否正在追踪战役</summary>
    public bool IsTrackingCampaign => currentCampaignRecord != null;

    /// <summary>是否保存回放数据</summary>
    private bool saveReplayData = true;

    /// <summary>会话开始时间（用于计算游戏时长）</summary>
    private float sessionStartTime;

    #region ILogic 接口实现

    public void OnInit()
    {
        sessionStartTime = Time.realtimeSinceStartup;
        LoadMetaData();
    }

    public void OnEnterState() { }

    public void OnUpdate() { }

    public void UnInit()
    {
        // 退出时保存游戏时长
        UpdatePlayTime();
        SaveMetaData();
    }

    #endregion

    #region 存档路径

    /// <summary>获取存档目录路径</summary>
    private string GetSaveDirectory()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
    }

    /// <summary>获取局外存档文件路径</summary>
    private string GetMetaSavePath()
    {
        return Path.Combine(GetSaveDirectory(), META_SAVE_FILE);
    }

    /// <summary>确保存档目录存在</summary>
    private void EnsureSaveDirectory()
    {
        string dir = GetSaveDirectory();
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    #endregion

    #region 局外存档读写

    /// <summary>加载局外存档数据</summary>
    public void LoadMetaData()
    {
        string path = GetMetaSavePath();

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                MetaData = JsonUtility.FromJson<MetaSaveData>(json);

                // 版本迁移
                if (MetaData.SaveVersion < CURRENT_SAVE_VERSION)
                {
                    MigrateMetaData(MetaData);
                }

                Debug.Log($"[SaveSystem] 加载局外存档成功: {path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 加载局外存档失败，创建新存档: {e.Message}");
                MetaData = MetaSaveData.CreateDefault();
            }
        }
        else
        {
            Debug.Log("[SaveSystem] 局外存档不存在，创建新存档");
            MetaData = MetaSaveData.CreateDefault();
        }

        // 确保列表不为null
        MetaData.CampaignHistory ??= new List<CampaignHistoryRecord>();
        MetaData.UnlockedCampaigns ??= new List<string>();
        MetaData.Statistics ??= PlayerStatistics.CreateDefault();
    }

    /// <summary>保存局外存档数据</summary>
    public void SaveMetaData()
    {
        EnsureSaveDirectory();

        MetaData.LastSaveTime = DateTime.UtcNow.ToString("o");

        try
        {
            string json = JsonUtility.ToJson(MetaData, true);
            string path = GetMetaSavePath();
            File.WriteAllText(path, json);
            Debug.Log($"[SaveSystem] 保存局外存档成功: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] 保存局外存档失败: {e.Message}");
        }
    }

    /// <summary>版本迁移</summary>
    private void MigrateMetaData(MetaSaveData data)
    {
        // 未来版本迁移逻辑
        // if (data.SaveVersion < 2) { ... }

        data.SaveVersion = CURRENT_SAVE_VERSION;
        Debug.Log($"[SaveSystem] 存档版本迁移完成: v{data.SaveVersion}");
    }

    #endregion

    #region 战役追踪

    /// <summary>开始追踪战役</summary>
    public void BeginCampaignTracking(string campaignId, string campaignName, bool recordReplay = true)
    {
        if (currentCampaignRecord != null)
        {
            Debug.LogWarning("[SaveSystem] 已有战役正在追踪，先结束当前追踪");
            EndCampaignTracking(GameResult.InProgress);
        }

        currentCampaignRecord = CampaignHistoryRecord.Create(campaignId, campaignName);
        saveReplayData = recordReplay;

        Debug.Log($"[SaveSystem] 开始追踪战役: {campaignName} ({campaignId})");
    }

    /// <summary>记录回合开始</summary>
    public void RecordTurnStart(int turnNumber, CampaignRuntimeData data)
    {
        if (currentCampaignRecord == null) return;

        currentTurnReplay = new TurnReplayData
        {
            TurnNumber = turnNumber,
            StartSnapshot = CreateSnapshot(data)
        };
    }

    /// <summary>记录回合结束</summary>
    public void RecordTurnEnd(CampaignRuntimeData data)
    {
        if (currentCampaignRecord == null || currentTurnReplay == null) return;

        currentTurnReplay.EndSnapshot = CreateSnapshot(data);

        if (saveReplayData)
        {
            currentCampaignRecord.ReplayData.Add(currentTurnReplay);
        }

        currentTurnReplay = null;
    }

    /// <summary>记录玩家操作</summary>
    public void RecordPlayerAction(PlayerAction action)
    {
        if (currentTurnReplay == null) return;

        currentTurnReplay.PlayerActions.Add(action);
    }

    /// <summary>记录战斗结果</summary>
    public void RecordBattleResult(BattleResult result)
    {
        if (currentTurnReplay == null) return;

        var record = new BattleResultRecord
        {
            Position = result.Position,
            AllyOrder = result.AllyOrder,
            EnemyOrder = result.EnemyOrder,
            AllyCasualties = -result.AllyTroopChange,
            EnemyCasualties = -result.EnemyTroopChange,
            LineMovement = result.LineMovement,
            WasCrit = result.WasCrit,
            WasFumble = result.WasFumble,
            Description = result.Description
        };

        currentTurnReplay.BattleResults.Add(record);
    }

    /// <summary>结束战役追踪并保存记录</summary>
    public void EndCampaignTracking(GameResult result, CampaignRuntimeData data = null)
    {
        if (currentCampaignRecord == null)
        {
            Debug.LogWarning("[SaveSystem] 没有正在追踪的战役");
            return;
        }

        // 填充战役结果
        currentCampaignRecord.Result = result;
        currentCampaignRecord.CompletionTime = DateTime.UtcNow.ToString("o");
        currentCampaignRecord.HasReplayData = saveReplayData && currentCampaignRecord.ReplayData.Count > 0;

        if (data != null)
        {
            FillCampaignSummary(data);
        }

        // 更新统计数据
        UpdateStatistics(currentCampaignRecord);

        // 添加到历史记录
        MetaData.CampaignHistory.Add(currentCampaignRecord);

        // 保存
        SaveMetaData();

        Debug.Log($"[SaveSystem] 战役追踪结束: {currentCampaignRecord.CampaignName}, 结果: {result}");

        currentCampaignRecord = null;
        currentTurnReplay = null;
    }

    /// <summary>填充战役摘要数据</summary>
    private void FillCampaignSummary(CampaignRuntimeData data)
    {
        currentCampaignRecord.FinalTurn = data.CurrentTurn;

        // 财务摘要
        var financial = currentCampaignRecord.Financial;
        financial.InitialCash = data.Config.InitialCash;
        financial.FinalCash = data.Player.Cash;
        financial.FinalNetWorth = data.Player.CalculateNetWorth(data.Market);
        financial.TotalProfitLoss = financial.FinalNetWorth - data.Player.AuditValue;
        financial.CommissionBonus = data.CommissionTotalBonus;

        // 统计交易次数
        int tradeCount = 0;
        foreach (var turn in data.TurnHistory)
        {
            if (turn.Transactions != null)
            {
                tradeCount += turn.Transactions.Count;
            }
        }
        financial.TradeCount = tradeCount;

        // 战斗摘要
        var battle = currentCampaignRecord.Battle;
        int allyCasualties = 0;
        int enemyCasualties = 0;
        int allyRouted = 0;
        int enemyRouted = 0;

        var balanceConfig = GameRoot.Instance.resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);

        foreach (var general in data.Battle.AllyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed)
                allyRouted++;
        }
        foreach (var general in data.Battle.EnemyGenerals)
        {
            if (general.GetStatus(balanceConfig) == GeneralStatus.Routed)
                enemyRouted++;
        }

        // 从回合历史统计伤亡
        foreach (var turn in data.TurnHistory)
        {
            if (turn.BattleResults != null)
            {
                foreach (var result in turn.BattleResults)
                {
                    allyCasualties -= result.AllyTroopChange;
                    enemyCasualties -= result.EnemyTroopChange;
                }
            }
        }

        battle.AllyCasualties = allyCasualties;
        battle.EnemyCasualties = enemyCasualties;
        battle.AllyGeneralsRouted = allyRouted;
        battle.EnemyGeneralsRouted = enemyRouted;

        // 最终战线位置
        if (data.Battle.Frontlines.TryGetValue(FrontlinePosition.Left, out var left))
            battle.FinalLinePositionLeft = left.LinePosition;
        if (data.Battle.Frontlines.TryGetValue(FrontlinePosition.Center, out var center))
            battle.FinalLinePositionCenter = center.LinePosition;
        if (data.Battle.Frontlines.TryGetValue(FrontlinePosition.Right, out var right))
            battle.FinalLinePositionRight = right.LinePosition;

        // 委托结果
        if (data.CommissionResults != null)
        {
            foreach (var kvp in data.CommissionResults)
            {
                currentCampaignRecord.Commissions.Add(new CommissionResultRecord
                {
                    CommissionId = kvp.Key,
                    IsCompleted = kvp.Value
                });
            }
        }
    }

    /// <summary>更新统计数据</summary>
    private void UpdateStatistics(CampaignHistoryRecord record)
    {
        var stats = MetaData.Statistics;

        stats.TotalCampaignsPlayed++;
        stats.TotalTurnsPlayed += record.FinalTurn;
        stats.TotalTradeCount += record.Financial.TradeCount;

        switch (record.Result)
        {
            case GameResult.Victory:
                stats.TotalVictories++;
                break;
            case GameResult.Defeat:
                stats.TotalDefeats++;
                break;
            case GameResult.Draw:
                stats.TotalDraws++;
                break;
        }

        // 更新最佳/最差战役记录
        float profit = record.Financial.TotalProfitLoss + record.Financial.CommissionBonus;
        if (profit > stats.BestSingleCampaignProfit)
        {
            stats.BestSingleCampaignProfit = profit;
        }
        if (profit < stats.WorstSingleCampaignLoss)
        {
            stats.WorstSingleCampaignLoss = profit;
        }

        // 更新累计财富
        if (profit > 0)
        {
            MetaData.LifetimeEarnings += profit;
        }
        else
        {
            MetaData.LifetimeLosses += Mathf.Abs(profit);
        }
        MetaData.TotalWealth += profit;
    }

    /// <summary>更新游戏时长</summary>
    private void UpdatePlayTime()
    {
        float sessionTime = Time.realtimeSinceStartup - sessionStartTime;
        MetaData.Statistics.TotalPlayTimeSeconds += sessionTime;
        sessionStartTime = Time.realtimeSinceStartup;
    }

    #endregion

    #region 快照创建

    /// <summary>创建当前状态快照</summary>
    private TurnSnapshot CreateSnapshot(CampaignRuntimeData data)
    {
        var snapshot = new TurnSnapshot
        {
            PlayerCash = data.Player.Cash,
            PlayerNetWorth = data.Player.CalculateNetWorth(data.Market)
        };

        // 玩家库存
        foreach (var kvp in data.Player.Inventory)
        {
            snapshot.PlayerInventory.Add(new OrderInventoryItem
            {
                OrderType = kvp.Key,
                Quantity = kvp.Value
            });
        }

        // 市场价格
        foreach (var kvp in data.Market.CurrentPrices)
        {
            snapshot.MarketPrices.Add(new OrderPriceItem
            {
                OrderType = kvp.Key,
                Price = kvp.Value
            });
        }

        // 将军快照
        foreach (var general in data.Battle.AllyGenerals)
        {
            snapshot.AllyGenerals.Add(CreateGeneralSnapshot(general));
        }
        foreach (var general in data.Battle.EnemyGenerals)
        {
            snapshot.EnemyGenerals.Add(CreateGeneralSnapshot(general));
        }

        // 战线快照
        foreach (var kvp in data.Battle.Frontlines)
        {
            snapshot.Frontlines.Add(new FrontlineSnapshot
            {
                Position = kvp.Key,
                LinePosition = kvp.Value.LinePosition,
                GridOwners = (GridOwner[])kvp.Value.GridOwners.Clone(),
                StagnantTurns = kvp.Value.StagnantTurns
            });
        }

        return snapshot;
    }

    /// <summary>创建将军快照</summary>
    private GeneralSnapshot CreateGeneralSnapshot(GeneralData general)
    {
        return new GeneralSnapshot
        {
            GeneralId = general.GeneralId,
            Name = general.Name,
            Troops = general.Troops,
            Trust = general.Trust,
            Morale = general.Morale,
            CurrentIntent = general.FinalIntent,
            IntentSource = general.IntentSource,
            Position = general.Position,
            GridPosition = general.GridPosition,
            Status = general.GetStatus(GameRoot.Instance.resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE))
        };
    }

    #endregion

    #region 便捷方法

    /// <summary>获取战役历史记录数量</summary>
    public int GetCampaignHistoryCount()
    {
        return MetaData?.CampaignHistory?.Count ?? 0;
    }

    /// <summary>获取指定战役的历史记录</summary>
    public List<CampaignHistoryRecord> GetCampaignHistoryByCampaignId(string campaignId)
    {
        if (MetaData?.CampaignHistory == null) return new List<CampaignHistoryRecord>();

        return MetaData.CampaignHistory.FindAll(r => r.CampaignId == campaignId);
    }

    /// <summary>清除所有存档数据（用于调试）</summary>
    public void ClearAllData()
    {
        MetaData = MetaSaveData.CreateDefault();
        SaveMetaData();
        Debug.Log("[SaveSystem] 已清除所有存档数据");
    }

    #endregion
}
