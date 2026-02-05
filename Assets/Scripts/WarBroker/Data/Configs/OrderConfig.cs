using System;
using UnityEngine;

/// <summary>
/// 单个指令配置
/// </summary>
[Serializable]
public class OrderConfigItem
{
    public OrderType OrderType;

    [Tooltip("指令图标")]
    public Sprite Icon;

    [Tooltip("基础价格")]
    public float BasePrice;

    [Tooltip("每回合产能")]
    public int ProductionPerTurn;

    [Tooltip("初始流通盘 (GDD v6.0: InitialFloat)")]
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
        new() { OrderType = OrderType.ATK, BasePrice = 40f, ProductionPerTurn = 3, InitialStock = 50 },
        new() { OrderType = OrderType.DEF, BasePrice = 35f, ProductionPerTurn = 3, InitialStock = 50 },
        new() { OrderType = OrderType.RET, BasePrice = 25f, ProductionPerTurn = 2, InitialStock = 50 }
    };

    public OrderConfigItem GetConfig(OrderType type)
    {
        foreach (var item in Orders)
        {
            if (item.OrderType == type) return item;
        }
        return null;
    }

    public OrderConfigItem GetOrder(OrderType type) => GetConfig(type);
}
