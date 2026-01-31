using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

/// <summary>
/// K线图视图组件
/// 封装 XCharts CandlestickChart，提供简单的 API 供 UI 调用
/// </summary>
public class KLineChartView : MonoBehaviour
{
    [SerializeField] private string chartTitle = "";
    [SerializeField] private Color32 riseColor = new Color32(235, 84, 84, 255);   // 涨 - 红色
    [SerializeField] private Color32 fallColor = new Color32(68, 198, 127, 255);  // 跌 - 绿色

    private CandlestickChart chart;
    private OrderType currentOrderType;

    /// <summary>
    /// 初始化图表
    /// </summary>
    public void Initialize(string title = "")
    {
        if (!string.IsNullOrEmpty(title))
            chartTitle = title;

        CreateChart();
    }

    private void CreateChart()
    {
        chart = GetComponent<CandlestickChart>();
        if (chart == null)
        {
            chart = gameObject.AddComponent<CandlestickChart>();
            chart.Init();
        }

        // 配置图表
        ConfigureChart();
    }

    private void ConfigureChart()
    {
        if (chart == null) return;

        // 设置标题
        var title = chart.EnsureChartComponent<Title>();
        title.show = !string.IsNullOrEmpty(chartTitle);
        title.text = chartTitle;
        title.labelStyle.textStyle.fontSize = 14;

        // 配置 X 轴
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Category;
        xAxis.boundaryGap = true;
        xAxis.axisLabel.show = true;
        xAxis.axisLabel.textStyle.fontSize = 10;

        // 配置 Y 轴
        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;
        yAxis.axisLabel.show = true;
        yAxis.axisLabel.textStyle.fontSize = 10;
        yAxis.splitNumber = 4;

        // 配置网格
        var grid = chart.EnsureChartComponent<GridCoord>();
        grid.left = 50;
        grid.right = 20;
        grid.top = 30;
        grid.bottom = 30;

        // 确保有 Candlestick Serie
        if (chart.GetSerie(0) == null)
        {
            var serie = chart.AddSerie<Candlestick>();
            serie.itemStyle.color = riseColor;
            serie.itemStyle.color0 = fallColor;
            serie.itemStyle.borderColor = riseColor;
            serie.itemStyle.borderColor0 = fallColor;
        }
        else
        {
            var serie = chart.GetSerie(0);
            serie.itemStyle.color = riseColor;
            serie.itemStyle.color0 = fallColor;
            serie.itemStyle.borderColor = riseColor;
            serie.itemStyle.borderColor0 = fallColor;
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
            CreateChart();
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
