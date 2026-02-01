using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

/// <summary>
/// K线图视图组件
/// 封装 XCharts CandlestickChart，提供简单的 API 供 UI 调用
///
/// 使用方式：
/// 1. Prefab 预配置（推荐）：在 Prefab 中预先添加 CandlestickChart，并在 Inspector 中绑定到 chart 字段
/// 2. 运行时创建（兼容旧用法）：如果未绑定 chart，会自动创建 CandlestickChart 组件
/// </summary>
public class KLineChartView : MonoBehaviour
{
    [SerializeField] private CandlestickChart chart;  // 可在 Prefab 中预绑定
    [SerializeField] private string chartTitle = "";

    private OrderType currentOrderType;

    /// <summary>
    /// 初始化图表
    /// </summary>
    public void Initialize(string title = "")
    {
        if (!string.IsNullOrEmpty(title))
            chartTitle = title;

        // 优先使用已绑定的 Chart
        if (chart == null)
        {
            chart = GetComponent<CandlestickChart>();
        }

        // 仅在完全没有时才动态创建（兼容旧用法）
        if (chart == null)
        {
            chart = gameObject.AddComponent<CandlestickChart>();
            chart.Init();
        }

        // 设置标题（如果传入）
        if (!string.IsNullOrEmpty(chartTitle))
        {
            SetTitle(chartTitle);
        }
    }

    /// <summary>
    /// 设置当前显示的指令类型
    /// </summary>
    public void SetOrderType(OrderType type)
    {
        currentOrderType = type;
    }

    /// <summary>
    /// 获取当前显示的指令类型
    /// </summary>
    public OrderType GetOrderType()
    {
        return currentOrderType;
    }

    /// <summary>
    /// 刷新 K 线数据
    /// </summary>
    /// <param name="klineHistory">K线历史数据列表</param>
    public void RefreshData(List<KLineData> klineHistory)
    {
        if (chart == null)
        {
            Initialize();
        }

        chart.ClearData();

        if (klineHistory == null || klineHistory.Count == 0)
            return;

        for (int i = 0; i < klineHistory.Count; i++)
        {
            var kline = klineHistory[i];
            // XCharts AddData 参数顺序: serieIndex, index, open, close, low, high
            chart.AddXAxisData($"T{kline.Turn}");
            chart.AddData(0, i, kline.Open, kline.Close, kline.Low, kline.High);
        }
    }

    /// <summary>
    /// 清空图表数据
    /// </summary>
    public void Clear()
    {
        if (chart != null)
        {
            chart.ClearData();
        }
    }

    /// <summary>
    /// 设置图表标题
    /// </summary>
    public void SetTitle(string title)
    {
        chartTitle = title;
        if (chart != null)
        {
            var titleComponent = chart.GetChartComponent<Title>();
            if (titleComponent != null)
            {
                titleComponent.show = !string.IsNullOrEmpty(title);
                titleComponent.text = title;
            }
        }
    }
}
