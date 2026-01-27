#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// 一键生成 WarBroker 所有 UI Prefab
/// 菜单: WarBroker > Generate UI Prefabs
/// </summary>
public static class WarBrokerUIPrefabGenerator
{
    private const string UIPath = "Assets/Resources/Prefabs/WarBroker/UI/";
    private const string ManagerPath = "Assets/Resources/Prefabs/WarBroker/";

    [MenuItem("WarBroker/Generate All Prefabs")]
    public static void GenerateAll()
    {
        // 强制刷新脚本编译，确保所有 Binder 类型可用
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        // 验证 Binder 类型是否已编译
        if (typeof(GameplayWindowBinder) == null || typeof(MarketPanelBinder) == null)
        {
            Debug.LogError("[WarBroker] Binder 脚本未编译！请等待 Unity 编译完成后重试。");
            return;
        }

        EnsureDirectories();
        GenerateGameplayManagerPrefab();
        GenerateGameplayWindow();
        GenerateMarketPanel();
        GenerateBattlefieldPanel();
        GenerateGeneralPanel();
        GenerateIntelPanel();
        GenerateHistoryPanel();
        GenerateGameEndWindow();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WarBroker] 所有 Prefab 已生成！请在 Inspector 中调整布局。");
    }

    [MenuItem("WarBroker/Generate UI Prefabs Only")]
    public static void GenerateUIOnly()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureDirectories();
        GenerateGameplayWindow();
        GenerateMarketPanel();
        GenerateBattlefieldPanel();
        GenerateGeneralPanel();
        GenerateIntelPanel();
        GenerateHistoryPanel();
        GenerateGameEndWindow();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WarBroker] 7 个 UI Prefab 已生成！");
    }

    private static void EnsureDirectories()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Prefabs");
        EnsureFolder("Assets/Resources/Prefabs/WarBroker");
        EnsureFolder("Assets/Resources/Prefabs/WarBroker/UI");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            var folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    #region GameplayManager Prefab

    private static void GenerateGameplayManagerPrefab()
    {
        var go = new GameObject("GameplayManager");
        var mgr = go.AddComponent<GameplayManager>();
        mgr.CampaignId = "Campaign_Tutorial";
        SavePrefab(go, ManagerPath + "GameplayManager.prefab");
        Object.DestroyImmediate(go);
        Debug.Log("[WarBroker] GameplayManager Prefab 已生成");
    }

    #endregion

    #region GameplayWindow

    private static void GenerateGameplayWindow()
    {
        var root = CreateUICanvas("GameplayWindow");
        root.AddComponent<GameplayWindowBinder>();
        var rt = root.GetComponent<RectTransform>();

        // === TopBar ===
        var topBar = CreatePanel("TopBar", root.transform);
        SetAnchors(topBar, new Vector2(0, 0.9f), Vector2.one);

        var txtTurn = CreateText("TxtTurn", topBar.transform, "回合 1/20");
        SetAnchors(txtTurn, new Vector2(0, 0), new Vector2(0.2f, 1));

        var txtPhase = CreateText("TxtPhase", topBar.transform, "PlayerAction");
        SetAnchors(txtPhase, new Vector2(0.2f, 0), new Vector2(0.4f, 1));

        var txtCash = CreateText("TxtCash", topBar.transform, "现金: 500");
        SetAnchors(txtCash, new Vector2(0.4f, 0), new Vector2(0.6f, 1));

        var txtNetWorth = CreateText("TxtNetWorth", topBar.transform, "净资产: 500");
        SetAnchors(txtNetWorth, new Vector2(0.6f, 0), new Vector2(0.8f, 1));

        var txtAudit = CreateText("TxtAudit", topBar.transform, "审计: 0");
        SetAnchors(txtAudit, new Vector2(0.8f, 0), new Vector2(1, 1));

        // === TabBar ===
        var tabBar = CreatePanel("TabBar", root.transform);
        SetAnchors(tabBar, new Vector2(0, 0.83f), new Vector2(1, 0.9f));
        var hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 4;

        var btnMarket = CreateButton("BtnMarket", tabBar.transform, "市场");
        var btnBattle = CreateButton("BtnBattle", tabBar.transform, "战场");
        var btnGeneral = CreateButton("BtnGeneral", tabBar.transform, "将军");
        var btnIntel = CreateButton("BtnIntel", tabBar.transform, "情报");
        var btnHistory = CreateButton("BtnHistory", tabBar.transform, "历史");

        // === ContentArea ===
        var contentArea = CreatePanel("ContentArea", root.transform);
        SetAnchors(contentArea, new Vector2(0, 0.08f), new Vector2(1, 0.83f));

        // === BottomBar ===
        var bottomBar = CreatePanel("BottomBar", root.transform);
        SetAnchors(bottomBar, Vector2.zero, new Vector2(1, 0.08f));

        var btnEndTurn = CreateButton("BtnEndTurn", bottomBar.transform, "结束回合");
        SetAnchors(btnEndTurn, new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.9f));

        var txtEventInfo = CreateText("TxtEventInfo", bottomBar.transform, "");
        SetAnchors(txtEventInfo, new Vector2(0.02f, 0), new Vector2(0.65f, 1));

        // Bind SerializeFields
        var window = root.GetComponent<GameplayWindowBinder>();
        SetField(window, "txtTurn", txtTurn.GetComponent<Text>());
        SetField(window, "txtPhase", txtPhase.GetComponent<Text>());
        SetField(window, "txtCash", txtCash.GetComponent<Text>());
        SetField(window, "txtNetWorth", txtNetWorth.GetComponent<Text>());
        SetField(window, "txtAudit", txtAudit.GetComponent<Text>());
        SetField(window, "btnMarket", btnMarket.GetComponent<Button>());
        SetField(window, "btnBattle", btnBattle.GetComponent<Button>());
        SetField(window, "btnGeneral", btnGeneral.GetComponent<Button>());
        SetField(window, "btnIntel", btnIntel.GetComponent<Button>());
        SetField(window, "btnHistory", btnHistory.GetComponent<Button>());
        SetField(window, "btnEndTurn", btnEndTurn.GetComponent<Button>());
        SetField(window, "txtEventInfo", txtEventInfo.GetComponent<Text>());

        SavePrefab(root, UIPath + "GameplayWindow.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("[WarBroker] GameplayWindow Prefab 已生成");
    }

    #endregion

    #region MarketPanel

    private static void GenerateMarketPanel()
    {
        var root = CreateUIPanel("MarketPanel");
        root.AddComponent<MarketPanelBinder>();

        // === SpotTradeArea ===
        var spotArea = CreatePanel("SpotTradeArea", root.transform);
        SetAnchors(spotArea, new Vector2(0, 0.5f), new Vector2(1, 1));

        float[] rowY = { 0.66f, 0.33f, 0f };
        string[] names = { "ATK", "DEF", "RET" };
        string[] priceFields = { "txtAtkPrice", "txtDefPrice", "txtRetPrice" };
        string[] stockFields = { "txtAtkStock", "txtDefStock", "txtRetStock" };
        string[] buyFields = { "btnAtkBuy", "btnDefBuy", "btnRetBuy" };
        string[] sellFields = { "btnAtkSell", "btnDefSell", "btnRetSell" };

        var panel = root.GetComponent<MarketPanelBinder>();

        for (int i = 0; i < 3; i++)
        {
            var row = CreatePanel($"{names[i]}Row", spotArea.transform);
            SetAnchors(row, new Vector2(0, rowY[i]), new Vector2(1, rowY[i] + 0.33f));

            var price = CreateText($"Txt{names[i]}Price", row.transform, $"{names[i]}: 40.0");
            SetAnchors(price, new Vector2(0, 0), new Vector2(0.3f, 1));

            var stock = CreateText($"Txt{names[i]}Stock", row.transform, "库存:2 市场:10");
            SetAnchors(stock, new Vector2(0.3f, 0), new Vector2(0.6f, 1));

            var buy = CreateButton($"Btn{names[i]}Buy", row.transform, "买入");
            SetAnchors(buy, new Vector2(0.6f, 0.1f), new Vector2(0.8f, 0.9f));

            var sell = CreateButton($"Btn{names[i]}Sell", row.transform, "卖出");
            SetAnchors(sell, new Vector2(0.8f, 0.1f), new Vector2(1, 0.9f));

            SetField(panel, priceFields[i], price.GetComponent<Text>());
            SetField(panel, stockFields[i], stock.GetComponent<Text>());
            SetField(panel, buyFields[i], buy.GetComponent<Button>());
            SetField(panel, sellFields[i], sell.GetComponent<Button>());
        }

        // === FuturesArea ===
        var futuresArea = CreatePanel("FuturesArea", root.transform);
        SetAnchors(futuresArea, new Vector2(0, 0.2f), new Vector2(1, 0.5f));

        var ddType = CreateDropdown("DdFuturesType", futuresArea.transform, new[] { "ATK", "DEF", "RET" });
        SetAnchors(ddType, new Vector2(0, 0.5f), new Vector2(0.2f, 1));

        var ddDir = CreateDropdown("DdFuturesDir", futuresArea.transform, new[] { "做多", "做空" });
        SetAnchors(ddDir, new Vector2(0.2f, 0.5f), new Vector2(0.4f, 1));

        var inputQty = CreateInputField("InputFuturesQty", futuresArea.transform, "数量");
        SetAnchors(inputQty, new Vector2(0.4f, 0.5f), new Vector2(0.6f, 1));

        var inputTurns = CreateInputField("InputFuturesTurns", futuresArea.transform, "期限");
        SetAnchors(inputTurns, new Vector2(0.6f, 0.5f), new Vector2(0.8f, 1));

        var btnOpen = CreateButton("BtnOpenFutures", futuresArea.transform, "开仓");
        SetAnchors(btnOpen, new Vector2(0.8f, 0.5f), new Vector2(1, 1));

        SetField(panel, "ddFuturesType", ddType.GetComponent<Dropdown>());
        SetField(panel, "ddFuturesDir", ddDir.GetComponent<Dropdown>());
        SetField(panel, "inputFuturesQty", inputQty.GetComponent<InputField>());
        SetField(panel, "inputFuturesTurns", inputTurns.GetComponent<InputField>());
        SetField(panel, "btnOpenFutures", btnOpen.GetComponent<Button>());

        // === BankArea ===
        var bankArea = CreatePanel("BankArea", root.transform);
        SetAnchors(bankArea, new Vector2(0, 0), new Vector2(1, 0.2f));

        var txtDebt = CreateText("TxtDebt", bankArea.transform, "负债: 0");
        SetAnchors(txtDebt, new Vector2(0, 0), new Vector2(0.25f, 1));

        var txtInterest = CreateText("TxtInterest", bankArea.transform, "利率: 5%");
        SetAnchors(txtInterest, new Vector2(0.25f, 0), new Vector2(0.5f, 1));

        var inputAmount = CreateInputField("InputBankAmount", bankArea.transform, "金额");
        SetAnchors(inputAmount, new Vector2(0.5f, 0.1f), new Vector2(0.7f, 0.9f));

        var btnBorrow = CreateButton("BtnBorrow", bankArea.transform, "借入");
        SetAnchors(btnBorrow, new Vector2(0.7f, 0.1f), new Vector2(0.85f, 0.9f));

        var btnRepay = CreateButton("BtnRepay", bankArea.transform, "还款");
        SetAnchors(btnRepay, new Vector2(0.85f, 0.1f), new Vector2(1, 0.9f));

        SetField(panel, "txtDebt", txtDebt.GetComponent<Text>());
        SetField(panel, "txtInterest", txtInterest.GetComponent<Text>());
        SetField(panel, "inputBankAmount", inputAmount.GetComponent<InputField>());
        SetField(panel, "btnBorrow", btnBorrow.GetComponent<Button>());
        SetField(panel, "btnRepay", btnRepay.GetComponent<Button>());

        SavePrefab(root, UIPath + "MarketPanel.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("[WarBroker] MarketPanel Prefab 已生成");
    }

    #endregion

    #region BattlefieldPanel

    private static void GenerateBattlefieldPanel()
    {
        var root = CreateUIPanel("BattlefieldPanel");
        root.AddComponent<BattlefieldPanelBinder>();
        var panel = root.GetComponent<BattlefieldPanelBinder>();

        string[] positions = { "Left", "Center", "Right" };
        string[] labels = { "左翼", "中军", "右翼" };
        string[] sliderFields = { "sliderLeft", "sliderCenter", "sliderRight" };
        string[] allyFields = { "txtLeftAlly", "txtCenterAlly", "txtRightAlly" };
        string[] enemyFields = { "txtLeftEnemy", "txtCenterEnemy", "txtRightEnemy" };

        for (int i = 0; i < 3; i++)
        {
            float yMin = 1f - (i + 1) * 0.28f;
            float yMax = 1f - i * 0.28f;

            var row = CreatePanel($"Frontline{positions[i]}", root.transform);
            SetAnchors(row, new Vector2(0, yMin), new Vector2(1, yMax));

            var label = CreateText($"Label{positions[i]}", row.transform, labels[i]);
            SetAnchors(label, new Vector2(0, 0), new Vector2(0.1f, 1));

            var allyTxt = CreateText($"TxtAlly{positions[i]}", row.transform, "己方将军");
            SetAnchors(allyTxt, new Vector2(0.1f, 0), new Vector2(0.3f, 1));

            var slider = CreateSlider($"Slider{positions[i]}", row.transform);
            SetAnchors(slider, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f));

            var enemyTxt = CreateText($"TxtEnemy{positions[i]}", row.transform, "敌方将军");
            SetAnchors(enemyTxt, new Vector2(0.7f, 0), new Vector2(0.9f, 1));

            SetField(panel, sliderFields[i], slider.GetComponent<Slider>());
            SetField(panel, allyFields[i], allyTxt.GetComponent<Text>());
            SetField(panel, enemyFields[i], enemyTxt.GetComponent<Text>());
        }

        var resultArea = CreateText("TxtBattleResults", root.transform, "战斗结果将在此显示");
        SetAnchors(resultArea, new Vector2(0, 0), new Vector2(1, 0.16f));

        var eventInfo = CreateText("TxtEventInfo", root.transform, "");
        SetAnchors(eventInfo, new Vector2(0, 0.16f), new Vector2(1, 0.22f));

        SetField(panel, "txtBattleResults", resultArea.GetComponent<Text>());
        SetField(panel, "txtEventInfo", eventInfo.GetComponent<Text>());

        SavePrefab(root, UIPath + "BattlefieldPanel.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("[WarBroker] BattlefieldPanel Prefab 已生成");
    }

    #endregion

    #region GeneralPanel

    private static void GenerateGeneralPanel()
    {
        var root = CreateUIPanel("GeneralPanel");
        root.AddComponent<GeneralPanelBinder>();
        var panel = root.GetComponent<GeneralPanelBinder>();

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 8;
        hlg.padding = new RectOffset(8, 8, 8, 8);

        // 通过反射获取 generalCards 数组并设置
        var cards = new GeneralPanelBinder.GeneralCardRef[3];

        for (int i = 0; i < 3; i++)
        {
            var card = new GeneralPanelBinder.GeneralCardRef();

            var cardGo = CreatePanel($"GeneralCard_{i}", root.transform);
            var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            cardGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var txtName = CreateLayoutText("TxtName", cardGo.transform, $"将军 {i + 1}", 24);
            card.txtName = txtName.GetComponent<Text>();

            var txtPers = CreateLayoutText("TxtPersonality", cardGo.transform, "性格", 16);
            card.txtPersonality = txtPers.GetComponent<Text>();

            var slTroops = CreateLayoutSlider("SliderTroops", cardGo.transform, "兵力");
            card.sliderTroops = slTroops.GetComponent<Slider>();

            var slTrust = CreateLayoutSlider("SliderTrust", cardGo.transform, "信任");
            card.sliderTrust = slTrust.GetComponent<Slider>();

            var slMorale = CreateLayoutSlider("SliderMorale", cardGo.transform, "士气");
            card.sliderMorale = slMorale.GetComponent<Slider>();

            var txtStatus = CreateLayoutText("TxtStatus", cardGo.transform, "正常", 16);
            card.txtStatus = txtStatus.GetComponent<Text>();

            // 指令按钮行
            var btnRow = CreatePanel("ButtonRow", cardGo.transform);
            var btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.childForceExpandWidth = true;
            btnHlg.childForceExpandHeight = true;
            btnHlg.spacing = 4;
            var btnLE = btnRow.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 40;

            var btnATK = CreateButton("BtnATK", btnRow.transform, "ATK");
            card.btnATK = btnATK.GetComponent<Button>();
            card.imgATKHighlight = CreateHighlight(btnATK, new Color(1, 0.3f, 0.3f, 0.5f));

            var btnDEF = CreateButton("BtnDEF", btnRow.transform, "DEF");
            card.btnDEF = btnDEF.GetComponent<Button>();
            card.imgDEFHighlight = CreateHighlight(btnDEF, new Color(0.3f, 0.5f, 1, 0.5f));

            var btnRET = CreateButton("BtnRET", btnRow.transform, "RET");
            card.btnRET = btnRET.GetComponent<Button>();
            card.imgRETHighlight = CreateHighlight(btnRET, new Color(0.8f, 0.8f, 0.3f, 0.5f));

            var txtSkills = CreateLayoutText("TxtSkills", cardGo.transform, "技能列表", 14);
            card.txtSkills = txtSkills.GetComponent<Text>();

            cards[i] = card;
        }

        panel.generalCards = cards;

        SavePrefab(root, UIPath + "GeneralPanel.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("[WarBroker] GeneralPanel Prefab 已生成");
    }

    #endregion

    #region IntelPanel

    private static void GenerateIntelPanel()
    {
        var root = CreateUIPanel("IntelPanel");
        root.AddComponent<IntelPanelBinder>();
        var panel = root.GetComponent<IntelPanelBinder>();

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;
        vlg.spacing = 8;
        vlg.padding = new RectOffset(16, 16, 16, 16);

        var txtMarket = CreateLayoutText("TxtMarketIntel", root.transform, "市场情报", 16);
        var txtBattle = CreateLayoutText("TxtBattleIntel", root.transform, "战场情报", 16);
        var txtEnemy = CreateLayoutText("TxtEnemyIntel", root.transform, "敌方情报", 16);

        SetField(panel, "txtMarketIntel", txtMarket.GetComponent<Text>());
        SetField(panel, "txtBattleIntel", txtBattle.GetComponent<Text>());
        SetField(panel, "txtEnemyIntel", txtEnemy.GetComponent<Text>());

        SavePrefab(root, UIPath + "IntelPanel.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("[WarBroker] IntelPanel Prefab 已生成");
    }

    #endregion

    #region HistoryPanel

    private static void GenerateHistoryPanel()
    {
        var root = CreateUIPanel("HistoryPanel");
        root.AddComponent<HistoryPanelBinder>();
        var panel = root.GetComponent<HistoryPanelBinder>();

        // ScrollView
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(root.transform, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.3f);
        scrollGo.AddComponent<Mask>().showMaskGraphic = true;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.sizeDelta = new Vector2(0, 800);
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var txtHistory = CreateLayoutText("TxtHistory", content.transform, "历史记录将在此显示", 14);
        txtHistory.GetComponent<Text>().alignment = TextAnchor.UpperLeft;

        scrollRect.content = cRT;
        scrollRect.viewport = vpRT;
        scrollRect.horizontal = false;

        SetField(panel, "txtHistory", txtHistory.GetComponent<Text>());
        SetField(panel, "scrollRect", scrollRect);

        SavePrefab(root, UIPath + "HistoryPanel.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("[WarBroker] HistoryPanel Prefab 已生成");
    }

    #endregion

    #region GameEndWindow

    private static void GenerateGameEndWindow()
    {
        var root = CreateUICanvas("GameEndWindow");
        root.AddComponent<GameEndWindowBinder>();

        // Overlay
        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(root.transform, false);
        var olRT = overlay.AddComponent<RectTransform>();
        olRT.anchorMin = Vector2.zero;
        olRT.anchorMax = Vector2.one;
        olRT.offsetMin = Vector2.zero;
        olRT.offsetMax = Vector2.zero;
        var olImg = overlay.AddComponent<Image>();
        olImg.color = new Color(0, 0, 0, 0.6f);

        // DialogPanel
        var dialog = CreatePanel("DialogPanel", root.transform);
        SetAnchors(dialog, new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f));
        dialog.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        var vlg = dialog.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 16;
        vlg.padding = new RectOffset(32, 32, 32, 32);
        vlg.childAlignment = TextAnchor.MiddleCenter;

        var txtTitle = CreateLayoutText("TxtTitle", dialog.transform, "胜利！", 36);
        txtTitle.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        txtTitle.GetComponent<LayoutElement>().preferredHeight = 60;

        var txtStats = CreateLayoutText("TxtStats", dialog.transform, "统计信息", 18);
        txtStats.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        txtStats.GetComponent<LayoutElement>().preferredHeight = 120;

        var btnRow = CreatePanel("ButtonRow", dialog.transform);
        var btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHlg.childForceExpandWidth = true;
        btnHlg.childForceExpandHeight = true;
        btnHlg.spacing = 16;
        var btnLE = btnRow.AddComponent<LayoutElement>();
        btnLE.preferredHeight = 50;

        var btnRestart = CreateButton("BtnRestart", btnRow.transform, "重新开始");
        var btnMainMenu = CreateButton("BtnMainMenu", btnRow.transform, "返回主菜单");

        var window = root.GetComponent<GameEndWindowBinder>();
        SetField(window, "txtTitle", txtTitle.GetComponent<Text>());
        SetField(window, "txtStats", txtStats.GetComponent<Text>());
        SetField(window, "btnRestart", btnRestart.GetComponent<Button>());
        SetField(window, "btnMainMenu", btnMainMenu.GetComponent<Button>());

        // 默认隐藏
        root.SetActive(false);

        SavePrefab(root, UIPath + "GameEndWindow.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("[WarBroker] GameEndWindow Prefab 已生成");
    }

    #endregion

    #region UI 工具方法

    private static GameObject CreateUICanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        go.AddComponent<CanvasGroup>();
        return go;
    }

    private static GameObject CreateUIPanel(string name)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);
        return go;
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 0.5f);
        return go;
    }

    private static GameObject CreateText(string name, Transform parent, string defaultText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.text = defaultText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 24;
        return go;
    }

    private static GameObject CreateLayoutText(string name, Transform parent, string defaultText, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var text = go.AddComponent<Text>();
        text.text = defaultText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 12;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.4f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRT = txtGo.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        return go;
    }

    private static GameObject CreateSlider(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.25f);
        bgRT.anchorMax = new Vector2(1, 0.75f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1);

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.25f);
        faRT.anchorMax = new Vector2(1, 0.75f);
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fRT = fill.AddComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.offsetMin = Vector2.zero;
        fRT.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1, 1);

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fRT;
        slider.minValue = 1;
        slider.maxValue = 5;
        slider.wholeNumbers = true;
        slider.value = 3;
        slider.interactable = false;

        return go;
    }

    private static GameObject CreateLayoutSlider(string name, Transform parent, string label)
    {
        var row = new GameObject(name + "Row");
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 8;
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 30;

        // Label
        var lblGo = new GameObject("Label");
        lblGo.transform.SetParent(row.transform, false);
        lblGo.AddComponent<RectTransform>();
        var lbl = lblGo.AddComponent<Text>();
        lbl.text = label;
        lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize = 14;
        lbl.color = Color.gray;
        var lblLE = lblGo.AddComponent<LayoutElement>();
        lblLE.preferredWidth = 40;

        // Slider
        var sliderGo = CreateSlider(name, row.transform);
        var sliderLE = sliderGo.AddComponent<LayoutElement>();
        sliderLE.flexibleWidth = 1;
        sliderLE.preferredHeight = 20;

        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.wholeNumbers = true;
        slider.value = 80;
        slider.interactable = false;

        return sliderGo;
    }

    private static GameObject CreateDropdown(string name, Transform parent, string[] options)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.3f, 1);

        // Label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var lRT = labelGo.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(10, 0);
        lRT.offsetMax = new Vector2(-25, 0);
        var labelText = labelGo.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 14;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;

        // Template (minimal)
        var template = new GameObject("Template");
        template.transform.SetParent(go.transform, false);
        var tRT = template.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 0);
        tRT.anchorMax = new Vector2(1, 0);
        tRT.pivot = new Vector2(0.5f, 1);
        tRT.sizeDelta = new Vector2(0, 150);
        template.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1);
        var scroll = template.AddComponent<ScrollRect>();

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        var vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.sizeDelta = new Vector2(0, 28);

        // Item template
        var item = new GameObject("Item");
        item.transform.SetParent(content.transform, false);
        var iRT = item.AddComponent<RectTransform>();
        iRT.anchorMin = Vector2.zero;
        iRT.anchorMax = new Vector2(1, 0);
        iRT.sizeDelta = new Vector2(0, 28);
        item.AddComponent<Toggle>();

        var itemLabel = new GameObject("Item Label");
        itemLabel.transform.SetParent(item.transform, false);
        var ilRT = itemLabel.AddComponent<RectTransform>();
        ilRT.anchorMin = Vector2.zero;
        ilRT.anchorMax = Vector2.one;
        ilRT.offsetMin = Vector2.zero;
        ilRT.offsetMax = Vector2.zero;
        var itemText = itemLabel.AddComponent<Text>();
        itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemText.fontSize = 14;
        itemText.color = Color.white;

        scroll.content = cRT;
        scroll.viewport = vpRT;

        var dropdown = go.AddComponent<Dropdown>();
        dropdown.captionText = labelText;
        dropdown.itemText = itemText;
        dropdown.template = tRT;

        template.SetActive(false);

        dropdown.ClearOptions();
        var optList = new System.Collections.Generic.List<Dropdown.OptionData>();
        foreach (var opt in options) optList.Add(new Dropdown.OptionData(opt));
        dropdown.AddOptions(optList);

        return go;
    }

    private static GameObject CreateInputField(string name, Transform parent, string placeholder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 1);

        // Text
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tRT = textGo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(8, 0);
        tRT.offsetMax = new Vector2(-8, 0);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.supportRichText = false;

        // Placeholder
        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(go.transform, false);
        var phRT = phGo.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(8, 0);
        phRT.offsetMax = new Vector2(-8, 0);
        var phText = phGo.AddComponent<Text>();
        phText.text = placeholder;
        phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phText.fontSize = 14;
        phText.fontStyle = FontStyle.Italic;
        phText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);

        var inputField = go.AddComponent<InputField>();
        inputField.textComponent = text;
        inputField.placeholder = phText;
        inputField.contentType = InputField.ContentType.IntegerNumber;

        return go;
    }

    private static Image CreateHighlight(GameObject parent, Color color)
    {
        var go = new GameObject("Highlight");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.enabled = false;
        img.raycastTarget = false;
        return img;
    }

    private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SavePrefab(GameObject go, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        // 标记所有组件为脏，确保序列化数据写入
        foreach (var comp in go.GetComponentsInChildren<Component>(true))
        {
            if (comp != null)
                EditorUtility.SetDirty(comp);
        }

        bool success;
        PrefabUtility.SaveAsPrefabAsset(go, path, out success);
        if (!success)
            Debug.LogError($"[WarBroker] 保存 Prefab 失败: {path}");
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"[WarBroker] Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }

    #endregion
}
#endif
