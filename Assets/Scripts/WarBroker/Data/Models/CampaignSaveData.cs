using System;
using System.Collections.Generic;

/// <summary>
/// 战役存档数据
/// TODO: 完整实现存档系统
/// </summary>
[Serializable]
public class CampaignSaveData
{
    // 存档元信息
    public string SaveId;
    public string SaveName;
    public DateTime SaveTime;
    public int SaveVersion = 1;

    // 战役信息
    public string CampaignId;
    public int CurrentTurn;
    public int CurrentPhase;

    // TODO: 完整的存档数据结构
    // public PlayerSaveData Player;
    // public MarketSaveData Market;
    // public BattleSaveData Battle;
}

/// <summary>
/// 存档系统
/// TODO: 完整实现
/// </summary>
public class SaveSystem : ILogic
{
    public void OnInit() { }
    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }

    // TODO: 实现存档功能
    public void SaveGame(string saveName) { }
    public void LoadGame(string saveId) { }
    public void DeleteSave(string saveId) { }
    public List<CampaignSaveData> GetSaveList() { return new List<CampaignSaveData>(); }
}
