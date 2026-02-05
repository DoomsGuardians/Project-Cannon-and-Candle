using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战役列表配置 - 管理所有可用战役
/// </summary>
[CreateAssetMenu(fileName = "CampaignListConfig", menuName = "WarBroker/CampaignListConfig")]
public class CampaignListConfig : ScriptableObject
{
    [Tooltip("所有可用的战役配置")]
    public List<CampaignConfig> Campaigns = new();
}
