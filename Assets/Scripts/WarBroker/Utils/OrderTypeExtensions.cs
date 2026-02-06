/// <summary>
/// OrderType 枚举的扩展方法
/// </summary>
public static class OrderTypeExtensions
{
    /// <summary>
    /// 获取指令类型的显示名称
    /// </summary>
    public static string ToDisplayName(this OrderType orderType)
    {
        return orderType switch
        {
            OrderType.ATK => "进攻令",
            OrderType.DEF => "防守令",
            OrderType.RET => "撤退令",
            _ => orderType.ToString()
        };
    }

    /// <summary>
    /// 获取指令类型的简短名称（不带"令"）
    /// </summary>
    public static string ToShortName(this OrderType orderType)
    {
        return orderType switch
        {
            OrderType.ATK => "进攻",
            OrderType.DEF => "防守",
            OrderType.RET => "撤退",
            _ => orderType.ToString()
        };
    }
}

/// <summary>
/// FrontlinePosition 枚举的扩展方法
/// </summary>
public static class FrontlinePositionExtensions
{
    /// <summary>
    /// 获取战线位置的显示名称（根据总战线数量动态调整）
    /// </summary>
    /// <param name="position">战线位置</param>
    /// <param name="totalLanes">总战线数量（1-5）</param>
    public static string ToDisplayName(this FrontlinePosition position, int totalLanes = 3)
    {
        // 1条战线：主战线
        if (totalLanes == 1)
        {
            return "主战线";
        }

        // 2条战线：左翼、右翼
        if (totalLanes == 2)
        {
            return position switch
            {
                FrontlinePosition.Left => "左翼",
                FrontlinePosition.Right => "右翼",
                _ => position.ToString()
            };
        }

        // 3条战线：左翼、中军、右翼
        if (totalLanes == 3)
        {
            return position switch
            {
                FrontlinePosition.Left => "左翼",
                FrontlinePosition.Center => "中军",
                FrontlinePosition.Right => "右翼",
                _ => position.ToString()
            };
        }

        // 4条战线：左翼、左中、右中、右翼
        if (totalLanes == 4)
        {
            return position switch
            {
                FrontlinePosition.Left => "左翼",
                FrontlinePosition.LeftCenter => "左中",
                FrontlinePosition.RightCenter => "右中",
                FrontlinePosition.Right => "右翼",
                _ => position.ToString()
            };
        }

        // 5条战线：左翼、左中、中军、右中、右翼
        return position switch
        {
            FrontlinePosition.Left => "左翼",
            FrontlinePosition.LeftCenter => "左中",
            FrontlinePosition.Center => "中军",
            FrontlinePosition.RightCenter => "右中",
            FrontlinePosition.Right => "右翼",
            _ => position.ToString()
        };
    }

    /// <summary>
    /// 根据战线数量获取应该使用的战线位置数组
    /// </summary>
    public static FrontlinePosition[] GetPositionsForLaneCount(int laneCount)
    {
        return laneCount switch
        {
            1 => new[] { FrontlinePosition.Center },
            2 => new[] { FrontlinePosition.Left, FrontlinePosition.Right },
            3 => new[] { FrontlinePosition.Left, FrontlinePosition.Center, FrontlinePosition.Right },
            4 => new[] { FrontlinePosition.Left, FrontlinePosition.LeftCenter, FrontlinePosition.RightCenter, FrontlinePosition.Right },
            5 => new[] { FrontlinePosition.Left, FrontlinePosition.LeftCenter, FrontlinePosition.Center, FrontlinePosition.RightCenter, FrontlinePosition.Right },
            _ => new[] { FrontlinePosition.Left, FrontlinePosition.Center, FrontlinePosition.Right }
        };
    }
}
