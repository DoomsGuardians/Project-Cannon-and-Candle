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
