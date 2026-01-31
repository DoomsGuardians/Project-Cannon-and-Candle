using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 委托任务系统 (GDD v6.0)
/// 4 种委托：WinWar, ShortCountry, Traitor, MeatGrinder
/// </summary>
public class CommissionSystem
{
    private CampaignRuntimeData campaignData;
    private GameBalanceConfig balanceConfig;

    // 委托奖金配置 (GDD v6.0)
    private const float WinWarBonus = 200f;
    private const float ShortCountryBonus = 500f;
    private const float TraitorBonus = 300f;
    private const float MeatGrinderBonus = 150f;

    public void Init(CampaignRuntimeData campaignData, GameBalanceConfig balanceConfig)
    {
        this.campaignData = campaignData;
        this.balanceConfig = balanceConfig;
    }

    /// <summary>
    /// 检查并结算所有委托任务
    /// 在战役结束时调用
    /// </summary>
    public Dictionary<string, bool> CheckAndSettleCommissions(out float totalBonus)
    {
        var results = new Dictionary<string, bool>();
        totalBonus = 0f;

        // 检查 WinWar（斩首胜利）
        bool winWar = CheckWinWar();
        results["WinWar"] = winWar;
        if (winWar)
        {
            totalBonus += WinWarBonus;
            Debug.Log($"[委托达成] WinWar: +${WinWarBonus}");
        }

        // 检查 ShortCountry（至少 2 条战线敌方到达 Grid 2）
        bool shortCountry = CheckShortCountry();
        results["ShortCountry"] = shortCountry;
        if (shortCountry)
        {
            totalBonus += ShortCountryBonus;
            Debug.Log($"[委托达成] ShortCountry: +${ShortCountryBonus}");
        }

        // 检查 Traitor（战役结束时 Grid 5 未被占领）
        bool traitor = CheckTraitor();
        results["Traitor"] = traitor;
        if (traitor)
        {
            totalBonus += TraitorBonus;
            Debug.Log($"[委托达成] Traitor: +${TraitorBonus}");
        }

        // 检查 MeatGrinder（双方总伤亡 ≥ 100）
        bool meatGrinder = CheckMeatGrinder();
        results["MeatGrinder"] = meatGrinder;
        if (meatGrinder)
        {
            totalBonus += MeatGrinderBonus;
            Debug.Log($"[委托达成] MeatGrinder: +${MeatGrinderBonus}");
        }

        // 将奖金加入玩家现金
        if (totalBonus > 0)
        {
            campaignData.Player.Cash += totalBonus;
            Debug.Log($"[委托结算] 总奖金: ${totalBonus}");
        }

        return results;
    }

    /// <summary>
    /// WinWar: 斩首胜利
    /// 条件：任意一条战线 Grid 5 被占领并保持 1 回合
    /// </summary>
    private bool CheckWinWar()
    {
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            if (general.GridPosition == 5)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// ShortCountry: 做空国运
    /// 条件：至少 2 条战线敌方到达 Grid 2
    /// </summary>
    private bool CheckShortCountry()
    {
        int count = 0;
        foreach (var general in campaignData.Battle.EnemyGenerals)
        {
            if (general.GridPosition <= 2)
            {
                count++;
            }
        }
        return count >= 2;
    }

    /// <summary>
    /// Traitor: 卖国求荣
    /// 条件：战役结束时 Grid 5 未被占领（即未达成斩首胜利）
    /// </summary>
    private bool CheckTraitor()
    {
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            if (general.GridPosition == 5)
            {
                return false; // 有人到达 Grid 5，不满足条件
            }
        }
        return true; // 没有人到达 Grid 5
    }

    /// <summary>
    /// MeatGrinder: 绞肉机
    /// 条件：双方总伤亡 ≥ 100
    /// </summary>
    private bool CheckMeatGrinder()
    {
        int totalCasualties = 0;

        // 计算己方伤亡
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            int initialHP = 16; // 假设初始 HP 为 16
            int casualties = Mathf.Max(0, initialHP - general.Troops);
            totalCasualties += casualties;
        }

        // 计算敌方伤亡
        foreach (var general in campaignData.Battle.EnemyGenerals)
        {
            int initialHP = 16;
            int casualties = Mathf.Max(0, initialHP - general.Troops);
            totalCasualties += casualties;
        }

        // 加上后备役消耗（假设初始后备役为 60）
        int reservesUsed = Mathf.Max(0, 60 - campaignData.Battle.CurrentReserves);
        totalCasualties += reservesUsed;

        Debug.Log($"[MeatGrinder] 总伤亡: {totalCasualties}");
        return totalCasualties >= 100;
    }

    /// <summary>
    /// 获取委托任务描述（用于 UI 显示）
    /// </summary>
    public Dictionary<string, string> GetCommissionDescriptions()
    {
        return new Dictionary<string, string>
        {
            { "WinWar", $"斩首胜利：任意战线占领 Grid 5 并保持 1 回合（奖金 ${WinWarBonus}）" },
            { "ShortCountry", $"做空国运：至少 2 条战线敌方到达 Grid 2（奖金 ${ShortCountryBonus}）" },
            { "Traitor", $"卖国求荣：战役结束时 Grid 5 未被占领（奖金 ${TraitorBonus}）" },
            { "MeatGrinder", $"绞肉机：双方总伤亡 ≥ 100（奖金 ${MeatGrinderBonus}）" }
        };
    }

    /// <summary>
    /// 实时检查委托进度（用于 UI 显示）
    /// </summary>
    public Dictionary<string, float> GetCommissionProgress()
    {
        var progress = new Dictionary<string, float>();

        // WinWar 进度：最前线位置 / 5
        int maxPosition = 0;
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            maxPosition = Mathf.Max(maxPosition, general.GridPosition);
        }
        progress["WinWar"] = maxPosition / 5f;

        // ShortCountry 进度：敌方到达 Grid 2 的战线数 / 2
        int shortCount = 0;
        foreach (var general in campaignData.Battle.EnemyGenerals)
        {
            if (general.GridPosition <= 2) shortCount++;
        }
        progress["ShortCountry"] = Mathf.Min(1f, shortCount / 2f);

        // Traitor 进度：反向进度（Grid 5 未被占领）
        progress["Traitor"] = CheckTraitor() ? 1f : 0f;

        // MeatGrinder 进度：总伤亡 / 100
        int totalCasualties = 0;
        foreach (var general in campaignData.Battle.AllyGenerals)
        {
            totalCasualties += Mathf.Max(0, 16 - general.Troops);
        }
        foreach (var general in campaignData.Battle.EnemyGenerals)
        {
            totalCasualties += Mathf.Max(0, 16 - general.Troops);
        }
        totalCasualties += Mathf.Max(0, 60 - campaignData.Battle.CurrentReserves);
        progress["MeatGrinder"] = Mathf.Min(1f, totalCasualties / 100f);

        return progress;
    }
}
