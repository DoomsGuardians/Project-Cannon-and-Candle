using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// 编辑器工具：创建 SpectatorWindow Prefab 和配置文件
/// </summary>
public class SpectatorWindowCreator : EditorWindow
{
    // 颜色配置（参考 GameplayWindow）
    private static readonly Color TextGold = new Color(0.949f, 0.875f, 0.655f, 1f);
    private static readonly Color TextWhite = new Color(1f, 1f, 1f, 1f);
    private static readonly Color PanelBg = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    private static readonly Color ButtonNormal = new Color(0.2f, 0.2f, 0.25f, 1f);
    private static readonly Color ButtonHighlight = new Color(0.3f, 0.3f, 0.35f, 1f);
    private static readonly Color AllyColor = new Color(0.2f, 0.6f, 0.9f, 1f);
    private static readonly Color EnemyColor = new Color(0.9f, 0.3f, 0.3f, 1f);

    [MenuItem("WarBroker/Create SpectatorWindow Prefab")]
    public static void CreateSpectatorWindowPrefab()
    {
        // 创建根对象
        var root = new GameObject("SpectatorWindow");
        var rootRect = root.AddComponent<RectTransform>();
        root.AddComponent<CanvasRenderer>();
        var rootImage = root.AddComponent<Image>();
        rootImage.color = PanelBg;

        // 设置全屏
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // 添加 Binder 组件
        var binder = root.AddComponent<SpectatorWindowBinder>();

        // 创建主布局
        var mainLayout = root.AddComponent<VerticalLayoutGroup>();
        mainLayout.padding = new RectOffset(20, 20, 20, 20);
        mainLayout.spacing = 10;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = false;
        mainLayout.childForceExpandWidth = true;
        mainLayout.childForceExpandHeight = false;

        // === 顶部信息栏 ===
        var topBar = CreatePanel(root.transform, "TopBar", 60);
        var topLayout = topBar.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(20, 20, 10, 10);
        topLayout.spacing = 30;
        topLayout.childAlignment = TextAnchor.MiddleCenter;
        topLayout.childControlWidth = false;
        topLayout.childControlHeight = true;

        binder.txtTurn = CreateText(topBar.transform, "TurnText", "回合 1 / 30", 24, TextGold, 200);
        binder.txtStatus = CreateText(topBar.transform, "StatusText", "未开始", 24, TextWhite, 150);

        // 速度控制区
        var speedArea = CreatePanel(topBar.transform, "SpeedArea", 40);
        speedArea.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);
        var speedLayout = speedArea.AddComponent<HorizontalLayoutGroup>();
        speedLayout.spacing = 10;
        speedLayout.childAlignment = TextAnchor.MiddleCenter;
        speedLayout.childControlWidth = false;
        speedLayout.childControlHeight = true;

        binder.btnSpeedDown = CreateButton(speedArea.transform, "BtnSpeedDown", "-", 50, 35);
        binder.txtSpeed = CreateText(speedArea.transform, "SpeedText", "1.0x", 20, TextWhite, 80);
        binder.btnSpeedUp = CreateButton(speedArea.transform, "BtnSpeedUp", "+", 50, 35);

        // === 中间内容区 ===
        var contentArea = CreatePanel(root.transform, "ContentArea", 500);
        var contentLayout = contentArea.AddComponent<HorizontalLayoutGroup>();
        contentLayout.padding = new RectOffset(10, 10, 10, 10);
        contentLayout.spacing = 20;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = true;

        // 己方面板
        var allyPanel = CreateVictorPanel(contentArea.transform, "AllyPanel", "己方维克多", AllyColor, binder, true);

        // 中间战场信息
        var battlePanel = CreatePanel(contentArea.transform, "BattlePanel", 0);
        var battleLayout = battlePanel.AddComponent<VerticalLayoutGroup>();
        battleLayout.padding = new RectOffset(10, 10, 10, 10);
        battleLayout.spacing = 10;
        battleLayout.childControlWidth = true;
        battleLayout.childControlHeight = false;
        battleLayout.childForceExpandWidth = true;

        var battleTitle = CreateText(battlePanel.transform, "BattleTitle", "战场状态", 22, TextGold, 0);
        battleTitle.alignment = TextAlignmentOptions.Center;
        battleTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);

        binder.txtFrontlineInfo = CreateText(battlePanel.transform, "FrontlineInfo", "战线信息...", 16, TextWhite, 0);
        binder.txtFrontlineInfo.alignment = TextAlignmentOptions.TopLeft;
        binder.txtFrontlineInfo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 200);
        binder.txtFrontlineInfo.GetComponent<LayoutElement>().flexibleHeight = 1;

        binder.txtPriceInfo = CreateText(battlePanel.transform, "PriceInfo", "价格信息...", 16, TextWhite, 0);
        binder.txtPriceInfo.alignment = TextAlignmentOptions.TopLeft;
        binder.txtPriceInfo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 100);

        // 敌方面板
        var enemyPanel = CreateVictorPanel(contentArea.transform, "EnemyPanel", "敌方维克多", EnemyColor, binder, false);

        // === 底部控制栏 ===
        var bottomBar = CreatePanel(root.transform, "BottomBar", 80);
        var bottomLayout = bottomBar.AddComponent<HorizontalLayoutGroup>();
        bottomLayout.padding = new RectOffset(20, 20, 15, 15);
        bottomLayout.spacing = 20;
        bottomLayout.childAlignment = TextAnchor.MiddleCenter;
        bottomLayout.childControlWidth = false;
        bottomLayout.childControlHeight = true;

        binder.btnStart = CreateButton(bottomBar.transform, "BtnStart", "开始对战", 150, 50);
        binder.btnPause = CreateButton(bottomBar.transform, "BtnPause", "暂停", 120, 50);
        binder.btnStep = CreateButton(bottomBar.transform, "BtnStep", "单步执行", 120, 50);

        // === 结果面板（默认隐藏）===
        binder.resultPanel = CreateResultPanel(root.transform, binder);
        binder.resultPanel.SetActive(false);

        // 保存 Prefab
        string prefabPath = "Assets/Resources/Prefabs/WarBroker/UI/Windows/SpectatorWindow.prefab";

        // 确保目录存在
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));

        // 保存
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        Debug.Log($"SpectatorWindow Prefab 已创建: {prefabPath}");
        AssetDatabase.Refresh();
    }

    private static GameObject CreatePanel(Transform parent, string name, float height)
    {
        var panel = new GameObject(name);
        var rect = panel.AddComponent<RectTransform>();
        panel.AddComponent<CanvasRenderer>();
        var image = panel.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        rect.SetParent(parent, false);

        if (height > 0)
        {
            var layoutElement = panel.AddComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
        }

        return panel;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, int fontSize, Color color, float width)
    {
        var obj = new GameObject(name);
        var rect = obj.AddComponent<RectTransform>();
        obj.AddComponent<CanvasRenderer>();
        var tmp = obj.AddComponent<TextMeshProUGUI>();

        rect.SetParent(parent, false);

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12;
        tmp.fontSizeMax = fontSize;

        if (width > 0)
        {
            var layoutElement = obj.AddComponent<LayoutElement>();
            layoutElement.minWidth = width;
            layoutElement.preferredWidth = width;
        }
        else
        {
            var layoutElement = obj.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1;
        }

        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string text, float width, float height)
    {
        var obj = new GameObject(name);
        var rect = obj.AddComponent<RectTransform>();
        obj.AddComponent<CanvasRenderer>();
        var image = obj.AddComponent<Image>();
        var button = obj.AddComponent<Button>();

        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(width, height);

        image.color = ButtonNormal;

        var colors = button.colors;
        colors.normalColor = ButtonNormal;
        colors.highlightedColor = ButtonHighlight;
        colors.pressedColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        button.colors = colors;

        // 添加文本
        var textObj = new GameObject("Text");
        var textRect = textObj.AddComponent<RectTransform>();
        textObj.AddComponent<CanvasRenderer>();
        var tmp = textObj.AddComponent<TextMeshProUGUI>();

        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = TextWhite;
        tmp.alignment = TextAlignmentOptions.Center;

        var layoutElement = obj.AddComponent<LayoutElement>();
        layoutElement.minWidth = width;
        layoutElement.minHeight = height;

        return button;
    }

    private static GameObject CreateVictorPanel(Transform parent, string name, string title, Color titleColor, SpectatorWindowBinder binder, bool isAlly)
    {
        var panel = CreatePanel(parent, name, 0);
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        // 标题
        var titleText = CreateText(panel.transform, "Title", title, 24, titleColor, 0);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        titleText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);

        // 现金
        var cashText = CreateText(panel.transform, "Cash", "现金: 1000", 18, TextWhite, 0);
        cashText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

        // 净资产
        var netWorthText = CreateText(panel.transform, "NetWorth", "净资产: 1000", 18, TextWhite, 0);
        netWorthText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

        // 策略
        var strategyText = CreateText(panel.transform, "Strategy", "策略: -", 18, TextGold, 0);
        strategyText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

        // 持仓
        var holdingsText = CreateText(panel.transform, "Holdings", "持仓: -", 16, TextWhite, 0);
        holdingsText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

        // 统计
        var statsText = CreateText(panel.transform, "Stats", "统计信息...", 14, TextWhite, 0);
        statsText.alignment = TextAlignmentOptions.TopLeft;
        statsText.GetComponent<LayoutElement>().flexibleHeight = 1;
        statsText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 150);

        // 绑定到 Binder
        if (isAlly)
        {
            binder.txtAllyTitle = titleText;
            binder.txtAllyCash = cashText;
            binder.txtAllyNetWorth = netWorthText;
            binder.txtAllyStrategy = strategyText;
            binder.txtAllyHoldings = holdingsText;
            binder.txtAllyStats = statsText;
        }
        else
        {
            binder.txtEnemyTitle = titleText;
            binder.txtEnemyCash = cashText;
            binder.txtEnemyNetWorth = netWorthText;
            binder.txtEnemyStrategy = strategyText;
            binder.txtEnemyHoldings = holdingsText;
            binder.txtEnemyStats = statsText;
        }

        return panel;
    }

    private static GameObject CreateResultPanel(Transform parent, SpectatorWindowBinder binder)
    {
        var panel = new GameObject("ResultPanel");
        var rect = panel.AddComponent<RectTransform>();
        panel.AddComponent<CanvasRenderer>();
        var image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.9f);

        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 内容容器
        var content = new GameObject("Content");
        var contentRect = content.AddComponent<RectTransform>();
        content.AddComponent<CanvasRenderer>();
        var contentImage = content.AddComponent<Image>();
        contentImage.color = PanelBg;

        contentRect.SetParent(rect, false);
        contentRect.anchorMin = new Vector2(0.2f, 0.15f);
        contentRect.anchorMax = new Vector2(0.8f, 0.85f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 30, 30);
        layout.spacing = 20;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperCenter;

        // 结果标题
        binder.txtResult = CreateText(content.transform, "ResultTitle", "对战结束", 36, TextGold, 0);
        binder.txtResult.alignment = TextAlignmentOptions.Center;
        binder.txtResult.fontStyle = FontStyles.Bold;
        binder.txtResult.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);

        // 结果详情
        binder.txtResultDetails = CreateText(content.transform, "ResultDetails", "详细信息...", 18, TextWhite, 0);
        binder.txtResultDetails.alignment = TextAlignmentOptions.TopLeft;
        binder.txtResultDetails.GetComponent<LayoutElement>().flexibleHeight = 1;
        binder.txtResultDetails.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 300);

        // 按钮区
        var buttonArea = new GameObject("ButtonArea");
        var buttonRect = buttonArea.AddComponent<RectTransform>();
        buttonRect.SetParent(content.transform, false);
        buttonRect.sizeDelta = new Vector2(0, 60);

        var buttonLayout = buttonArea.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 30;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = false;
        buttonLayout.childControlHeight = true;

        var buttonLayoutElement = buttonArea.AddComponent<LayoutElement>();
        buttonLayoutElement.minHeight = 60;

        binder.btnRestart = CreateButton(buttonArea.transform, "BtnRestart", "重新开始", 150, 50);
        binder.btnBackToMenu = CreateButton(buttonArea.transform, "BtnBackToMenu", "返回菜单", 150, 50);

        return panel;
    }
}
