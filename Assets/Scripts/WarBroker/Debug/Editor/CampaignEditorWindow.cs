#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 战役配置编辑器主窗口
/// 菜单入口: WarBroker > Campaign Editor (Ctrl+Shift+C)
/// </summary>
public class CampaignEditorWindow : EditorWindow
{
    // 配置资产路径
    private const string ConfigPath = "Assets/Resources/Config/WarBroker/";

    // 战役列表
    private CampaignConfig[] allCampaigns;
    private CampaignConfig selectedCampaign;
    private int selectedCampaignIndex = -1;

    // 选项卡
    private int selectedTab = 0;
    private readonly string[] tabNames = { "基础", "玩家", "战场", "将军", "委托" };

    // 搜索和滚动
    private string searchFilter = "";
    private Vector2 listScrollPos;
    private Vector2 detailScrollPos;

    // 验证缓存
    private Dictionary<CampaignConfig, CampaignValidator.ValidationResult> validationCache = new Dictionary<CampaignConfig, CampaignValidator.ValidationResult>();

    // 折叠状态
    private bool showAllyAssignments = true;
    private bool showEnemyAssignments = true;
    private bool showCommissionList = true;

    // SerializedObject 用于 Undo 支持
    private SerializedObject serializedCampaign;

    [MenuItem("WarBroker/Campaign Editor %#c")]
    public static void ShowWindow()
    {
        var window = GetWindow<CampaignEditorWindow>("战役编辑器");
        window.minSize = new Vector2(900, 600);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshCampaignList();
    }

    private void OnFocus()
    {
        RefreshCampaignList();
    }

    private void RefreshCampaignList()
    {
        var guids = AssetDatabase.FindAssets("t:CampaignConfig", new[] { ConfigPath });
        allCampaigns = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<CampaignConfig>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null)
            .OrderBy(c => c.CampaignId)
            .ToArray();

        // 刷新验证缓存
        validationCache.Clear();
        foreach (var campaign in allCampaigns)
        {
            validationCache[campaign] = CampaignValidator.Validate(campaign, allCampaigns);
        }

        // 重新选择当前战役
        if (selectedCampaign != null)
        {
            selectedCampaignIndex = System.Array.IndexOf(allCampaigns, selectedCampaign);
            if (selectedCampaignIndex < 0)
            {
                selectedCampaign = null;
                serializedCampaign = null;
            }
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        {
            // 左侧面板：战役列表
            DrawLeftPanel();

            // 分隔线
            EditorGUILayout.BeginVertical(GUILayout.Width(1));
            GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(1));
            EditorGUILayout.EndVertical();

            // 右侧面板：详情编辑
            DrawRightPanel();
        }
        EditorGUILayout.EndHorizontal();

        DrawStatusBar();
    }

    #region 工具栏

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            if (GUILayout.Button("+ 新建", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(60)))
            {
                CreateNewCampaign();
            }

            if (GUILayout.Button("复制", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(50)))
            {
                DuplicateSelectedCampaign();
            }

            // 从模板创建下拉菜单
            if (GUILayout.Button("从模板创建 ▼", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(90)))
            {
                ShowTemplateMenu();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("验证全部", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(70)))
            {
                ValidateAllCampaigns();
            }

            if (GUILayout.Button("刷新", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(50)))
            {
                RefreshCampaignList();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ShowTemplateMenu()
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Tutorial (教学关)"), false, () => CreateFromTemplate(CampaignTemplateType.Tutorial));
        menu.AddItem(new GUIContent("Standard (标准关)"), false, () => CreateFromTemplate(CampaignTemplateType.Standard));
        menu.AddItem(new GUIContent("Challenge (挑战关)"), false, () => CreateFromTemplate(CampaignTemplateType.Challenge));
        menu.AddItem(new GUIContent("Sandbox (沙盒关)"), false, () => CreateFromTemplate(CampaignTemplateType.Sandbox));
        menu.ShowAsContext();
    }

    private void CreateFromTemplate(CampaignTemplateType templateType)
    {
        var dialog = CreateTemplateDialog.ShowDialog(templateType, (id, name) =>
        {
            var config = CampaignTemplateFactory.CreateFromTemplate(templateType, id, name);
            if (config != null)
            {
                RefreshCampaignList();
                SelectCampaign(config);
            }
        });
    }

    #endregion

    #region 左侧面板 - 战役列表

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(220));
        {
            // 搜索框
            EditorGUILayout.BeginHorizontal();
            searchFilter = EditorGUILayout.TextField(searchFilter, CampaignEditorStyles.SearchFieldStyle);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // 战役列表
            listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);
            {
                var filteredCampaigns = FilterCampaigns();

                for (int i = 0; i < filteredCampaigns.Length; i++)
                {
                    var campaign = filteredCampaigns[i];
                    DrawCampaignListItem(campaign, campaign == selectedCampaign);
                }

                if (filteredCampaigns.Length == 0)
                {
                    EditorGUILayout.LabelField("无匹配的战役", CampaignEditorStyles.CenteredLabelStyle);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private CampaignConfig[] FilterCampaigns()
    {
        if (string.IsNullOrWhiteSpace(searchFilter))
            return allCampaigns;

        string filter = searchFilter.ToLowerInvariant();
        return allCampaigns
            .Where(c => c.CampaignId.ToLowerInvariant().Contains(filter) ||
                       c.CampaignName.ToLowerInvariant().Contains(filter))
            .ToArray();
    }

    private void DrawCampaignListItem(CampaignConfig campaign, bool isSelected)
    {
        var style = isSelected ? CampaignEditorStyles.ListItemSelectedStyle : CampaignEditorStyles.ListItemStyle;

        EditorGUILayout.BeginHorizontal(style);
        {
            // 验证状态图标
            string statusIcon = GetValidationIcon(campaign);
            GUILayout.Label(statusIcon, GUILayout.Width(20));

            // 战役信息
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.LabelField(campaign.CampaignId, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(campaign.CampaignName, CampaignEditorStyles.SubtitleStyle);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        // 点击选择
        var rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            SelectCampaign(campaign);
            Event.current.Use();
        }
    }

    private string GetValidationIcon(CampaignConfig campaign)
    {
        if (!validationCache.TryGetValue(campaign, out var result))
            return "?";

        if (result.IsValid && !result.HasWarnings)
            return "✓";
        if (result.IsValid && result.HasWarnings)
            return "⚠";
        return "✗";
    }

    private void SelectCampaign(CampaignConfig campaign)
    {
        selectedCampaign = campaign;
        selectedCampaignIndex = System.Array.IndexOf(allCampaigns, campaign);
        serializedCampaign = new SerializedObject(campaign);
        Repaint();
    }

    #endregion

    #region 右侧面板 - 详情编辑

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical();
        {
            if (selectedCampaign == null)
            {
                EditorGUILayout.LabelField("请从左侧列表选择一个战役", CampaignEditorStyles.CenteredLabelStyle, GUILayout.ExpandHeight(true));
            }
            else
            {
                // 选项卡
                selectedTab = GUILayout.Toolbar(selectedTab, tabNames, EditorStyles.toolbarButton);

                EditorGUILayout.Space(8);

                // 详情内容
                detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);
                {
                    serializedCampaign.Update();

                    switch (selectedTab)
                    {
                        case 0: DrawBasicInfoTab(); break;
                        case 1: DrawPlayerTab(); break;
                        case 2: DrawBattlefieldTab(); break;
                        case 3: DrawGeneralTab(); break;
                        case 4: DrawCommissionTab(); break;
                    }

                    if (serializedCampaign.ApplyModifiedProperties())
                    {
                        // 数据已修改，刷新验证
                        validationCache[selectedCampaign] = CampaignValidator.Validate(selectedCampaign, allCampaigns);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
        EditorGUILayout.EndVertical();
    }

    #region 基础信息选项卡

    private void DrawBasicInfoTab()
    {
        EditorGUILayout.LabelField("基础信息", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("CampaignId"), new GUIContent("战役 ID"));
        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("CampaignName"), new GUIContent("战役名称"));
        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("Description"), new GUIContent("描述"));
        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("Thumbnail"), new GUIContent("缩略图"));
        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("TicketPrice"), new GUIContent("门票价格"));

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("回合设置", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("MaxTurns"), new GUIContent("最大回合数"));

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("维克多设置", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("VictorInitialCash"), new GUIContent("维克多初始现金"));
        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("VictorProfile"), new GUIContent("维克多性格配置"));
    }

    #endregion

    #region 玩家设置选项卡

    private void DrawPlayerTab()
    {
        EditorGUILayout.LabelField("玩家初始状态", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("InitialCash"), new GUIContent("初始现金"));

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("初始库存", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("InitialAtkInventory"), new GUIContent("ATK 库存"));
        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("InitialDefInventory"), new GUIContent("DEF 库存"));
        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("InitialRetInventory"), new GUIContent("RET 库存"));

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("后备役设置", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("InitialReserves"), new GUIContent("初始后备役"));
    }

    #endregion

    #region 战场设置选项卡

    private void DrawBattlefieldTab()
    {
        EditorGUILayout.LabelField("战场设置", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("BattlefieldSceneName"), new GUIContent("战场场景名称"));

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("战线配置", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("LaneCount"), new GUIContent("战线数量"));
        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("GridCount"), new GUIContent("格子数量"));
        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("InitialFrontlinePosition"), new GUIContent("初始战线位置"));

        // 战线可视化
        EditorGUILayout.Space(16);
        DrawFrontlineVisualization();
    }

    private void DrawFrontlineVisualization()
    {
        EditorGUILayout.LabelField("战线预览", EditorStyles.boldLabel);

        int gridCount = selectedCampaign.GridCount;
        int initialPos = selectedCampaign.InitialFrontlinePosition;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        for (int i = gridCount; i >= 1; i--)
        {
            var bgColor = GUI.backgroundColor;
            if (i == initialPos)
                GUI.backgroundColor = CampaignEditorStyles.WarningColor;
            else if (i == gridCount)
                GUI.backgroundColor = CampaignEditorStyles.ErrorColor;
            else if (i == 1)
                GUI.backgroundColor = CampaignEditorStyles.SuccessColor;

            string label = i == gridCount ? $"敌方\nGrid {i}" :
                          i == 1 ? $"己方\nGrid {i}" :
                          i == initialPos ? $"战线\nGrid {i}" :
                          $"\nGrid {i}";

            GUILayout.Box(label, GUILayout.Width(60), GUILayout.Height(50));
            GUI.backgroundColor = bgColor;
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 将军配置选项卡

    private void DrawGeneralTab()
    {
        EditorGUILayout.LabelField("将军配置", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedCampaign.FindProperty("GeneralConfig"), new GUIContent("将军配置表"));

        if (selectedCampaign.GeneralConfig == null)
        {
            EditorGUILayout.HelpBox("请先指定将军配置表", MessageType.Warning);

            if (GUILayout.Button("打开将军编辑器"))
            {
                GeneralEditorWindow.ShowWindow();
            }
            return;
        }

        EditorGUILayout.Space(16);

        // 战线分配可视化
        DrawFrontlineAssignmentPanel();
    }

    private void DrawFrontlineAssignmentPanel()
    {
        int laneCount = selectedCampaign.LaneCount;
        var generalConfig = selectedCampaign.GeneralConfig;

        // 确保数组已初始化
        EnsureFrontlineAssignments(laneCount);

        EditorGUILayout.LabelField("战线分配", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("将将军分配到各战线位置", MessageType.Info);

        EditorGUILayout.Space(8);

        // 绘制战线分配网格
        DrawFrontlineGrid(laneCount, generalConfig);

        EditorGUILayout.Space(8);

        // 打开将军编辑器按钮
        if (GUILayout.Button("打开将军编辑器", GUILayout.Height(24)))
        {
            GeneralEditorWindow.ShowWindow();
        }
    }

    private void EnsureFrontlineAssignments(int laneCount)
    {
        // 根据 LaneCount 确定需要的位置
        var positions = GetRequiredPositions(laneCount);

        // 己方
        if (selectedCampaign.AllyFrontlineAssignments == null ||
            selectedCampaign.AllyFrontlineAssignments.Length != positions.Length)
        {
            var newAssignments = new FrontlineAssignment[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                newAssignments[i] = new FrontlineAssignment { Position = positions[i], GeneralId = "" };
            }

            // 保留已有分配
            if (selectedCampaign.AllyFrontlineAssignments != null)
            {
                foreach (var old in selectedCampaign.AllyFrontlineAssignments)
                {
                    for (int i = 0; i < positions.Length; i++)
                    {
                        if (newAssignments[i].Position == old.Position)
                        {
                            newAssignments[i].GeneralId = old.GeneralId;
                            break;
                        }
                    }
                }
            }

            selectedCampaign.AllyFrontlineAssignments = newAssignments;
            EditorUtility.SetDirty(selectedCampaign);
        }

        // 敌方
        if (selectedCampaign.EnemyFrontlineAssignments == null ||
            selectedCampaign.EnemyFrontlineAssignments.Length != positions.Length)
        {
            var newAssignments = new FrontlineAssignment[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                newAssignments[i] = new FrontlineAssignment { Position = positions[i], GeneralId = "" };
            }

            // 保留已有分配
            if (selectedCampaign.EnemyFrontlineAssignments != null)
            {
                foreach (var old in selectedCampaign.EnemyFrontlineAssignments)
                {
                    for (int i = 0; i < positions.Length; i++)
                    {
                        if (newAssignments[i].Position == old.Position)
                        {
                            newAssignments[i].GeneralId = old.GeneralId;
                            break;
                        }
                    }
                }
            }

            selectedCampaign.EnemyFrontlineAssignments = newAssignments;
            EditorUtility.SetDirty(selectedCampaign);
        }
    }

    private FrontlinePosition[] GetRequiredPositions(int laneCount)
    {
        switch (laneCount)
        {
            case 1: return new[] { FrontlinePosition.Center };
            case 2: return new[] { FrontlinePosition.Left, FrontlinePosition.Right };
            case 3: return new[] { FrontlinePosition.Left, FrontlinePosition.Center, FrontlinePosition.Right };
            case 4: return new[] { FrontlinePosition.Left, FrontlinePosition.LeftCenter, FrontlinePosition.RightCenter, FrontlinePosition.Right };
            case 5: return new[] { FrontlinePosition.Left, FrontlinePosition.LeftCenter, FrontlinePosition.Center, FrontlinePosition.RightCenter, FrontlinePosition.Right };
            default: return new[] { FrontlinePosition.Left, FrontlinePosition.Center, FrontlinePosition.Right };
        }
    }

    private void DrawFrontlineGrid(int laneCount, GeneralConfig generalConfig)
    {
        // 敌方基地标签
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("敌方基地", CampaignEditorStyles.CenteredLabelStyle, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 敌方将军行
        showEnemyAssignments = EditorGUILayout.Foldout(showEnemyAssignments, "敌方将军", true, CampaignEditorStyles.FoldoutHeaderStyle);
        if (showEnemyAssignments)
        {
            DrawGeneralAssignmentRow(selectedCampaign.EnemyFrontlineAssignments, generalConfig.EnemyGenerals, laneCount, false);
        }

        EditorGUILayout.Space(8);

        // 战线分隔
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Box($"═══ 初始战线 (Grid {selectedCampaign.InitialFrontlinePosition}) ═══", GUILayout.Width(250));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // 己方将军行
        showAllyAssignments = EditorGUILayout.Foldout(showAllyAssignments, "己方将军", true, CampaignEditorStyles.FoldoutHeaderStyle);
        if (showAllyAssignments)
        {
            DrawGeneralAssignmentRow(selectedCampaign.AllyFrontlineAssignments, generalConfig.AllyGenerals, laneCount, true);
        }

        // 己方基地标签
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("己方基地", CampaignEditorStyles.CenteredLabelStyle, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGeneralAssignmentRow(FrontlineAssignment[] assignments, GeneralConfigItem[] availableGenerals, int laneCount, bool isAlly)
    {
        if (assignments == null || availableGenerals == null) return;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        for (int i = 0; i < assignments.Length; i++)
        {
            DrawGeneralSlot(assignments[i], availableGenerals, GetPositionName(assignments[i].Position));
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGeneralSlot(FrontlineAssignment assignment, GeneralConfigItem[] availableGenerals, string positionLabel)
    {
        EditorGUILayout.BeginVertical(CampaignEditorStyles.CardStyle, GUILayout.Width(140));
        {
            EditorGUILayout.LabelField(positionLabel, CampaignEditorStyles.CenteredLabelStyle);

            // 构建下拉选项
            var options = new List<string> { "(未分配)" };
            var generalIds = new List<string> { "" };

            foreach (var general in availableGenerals)
            {
                if (general == null) continue;
                string icon = CampaignEditorStyles.GetPersonalityIcon(general.Personality);
                options.Add($"{icon} {general.Name} ({general.InitialTroops})");
                generalIds.Add(general.GeneralId);
            }

            int currentIndex = generalIds.IndexOf(assignment.GeneralId);
            if (currentIndex < 0) currentIndex = 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(currentIndex, options.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedCampaign, "Change General Assignment");
                assignment.GeneralId = generalIds[newIndex];
                EditorUtility.SetDirty(selectedCampaign);
                validationCache[selectedCampaign] = CampaignValidator.Validate(selectedCampaign, allCampaigns);
            }

            // 显示选中将军信息
            if (!string.IsNullOrEmpty(assignment.GeneralId))
            {
                var general = availableGenerals.FirstOrDefault(g => g?.GeneralId == assignment.GeneralId);
                if (general != null)
                {
                    var color = CampaignEditorStyles.GetPersonalityColor(general.Personality);
                    var oldColor = GUI.contentColor;
                    GUI.contentColor = color;
                    EditorGUILayout.LabelField($"{CampaignEditorStyles.GetPersonalityName(general.Personality)}", CampaignEditorStyles.CenteredLabelStyle);
                    GUI.contentColor = oldColor;
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private string GetPositionName(FrontlinePosition position)
    {
        switch (position)
        {
            case FrontlinePosition.Left: return "左翼";
            case FrontlinePosition.LeftCenter: return "左中";
            case FrontlinePosition.Center: return "中军";
            case FrontlinePosition.RightCenter: return "右中";
            case FrontlinePosition.Right: return "右翼";
            default: return position.ToString();
        }
    }

    #endregion

    #region 委托任务选项卡

    private void DrawCommissionTab()
    {
        EditorGUILayout.LabelField("委托任务", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        showCommissionList = EditorGUILayout.Foldout(showCommissionList, $"委托列表 ({selectedCampaign.Commissions?.Count ?? 0})", true, CampaignEditorStyles.FoldoutHeaderStyle);

        if (showCommissionList)
        {
            var commissionsProp = serializedCampaign.FindProperty("Commissions");
            EditorGUILayout.PropertyField(commissionsProp, true);
        }

        EditorGUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("添加委托", GUILayout.Height(24)))
            {
                AddCommissionSlot();
            }

            if (GUILayout.Button("创建新委托配置", GUILayout.Height(24)))
            {
                CreateNewCommissionConfig();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void AddCommissionSlot()
    {
        if (selectedCampaign.Commissions == null)
            selectedCampaign.Commissions = new List<CommissionConfig>();

        Undo.RecordObject(selectedCampaign, "Add Commission Slot");
        selectedCampaign.Commissions.Add(null);
        EditorUtility.SetDirty(selectedCampaign);
    }

    private void CreateNewCommissionConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "创建委托配置",
            "Commission_New",
            "asset",
            "选择保存位置",
            ConfigPath);

        if (!string.IsNullOrEmpty(path))
        {
            var config = ScriptableObject.CreateInstance<CommissionConfig>();
            config.CommissionId = System.IO.Path.GetFileNameWithoutExtension(path);
            config.DisplayName = "新委托";
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            // 添加到当前战役
            if (selectedCampaign.Commissions == null)
                selectedCampaign.Commissions = new List<CommissionConfig>();

            Undo.RecordObject(selectedCampaign, "Add New Commission");
            selectedCampaign.Commissions.Add(config);
            EditorUtility.SetDirty(selectedCampaign);

            EditorGUIUtility.PingObject(config);
        }
    }

    #endregion

    #endregion

    #region 状态栏

    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(CampaignEditorStyles.StatusBarStyle);
        {
            if (selectedCampaign != null && validationCache.TryGetValue(selectedCampaign, out var result))
            {
                // 状态图标和摘要
                string summary = CampaignValidator.GetStatusSummary(result);
                var statusColor = result.IsValid ? (result.HasWarnings ? CampaignEditorStyles.WarningColor : CampaignEditorStyles.SuccessColor) : CampaignEditorStyles.ErrorColor;
                var oldColor = GUI.contentColor;
                GUI.contentColor = statusColor;
                GUILayout.Label(summary);
                GUI.contentColor = oldColor;

                // 显示详细错误/警告
                if (result.Errors.Count > 0 || result.Warnings.Count > 0)
                {
                    string buttonText = result.Errors.Count > 0
                        ? $"查看 {result.Errors.Count} 个错误"
                        : $"查看 {result.Warnings.Count} 个警告";
                    if (GUILayout.Button(buttonText, CampaignEditorStyles.MiniButtonStyle, GUILayout.Width(100)))
                    {
                        ShowValidationDetails(result);
                    }

                    // 输出到 Console 按钮
                    if (GUILayout.Button("输出到Console", CampaignEditorStyles.MiniButtonStyle, GUILayout.Width(90)))
                    {
                        LogValidationToConsole(selectedCampaign, result);
                    }
                }
            }
            else
            {
                GUILayout.Label("请选择战役");
            }

            GUILayout.FlexibleSpace();

            if (selectedCampaign != null)
            {
                if (GUILayout.Button("保存", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(50)))
                {
                    SaveSelectedCampaign();
                }

                if (GUILayout.Button("验证", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(50)))
                {
                    ValidateSelectedCampaign();
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ShowValidationDetails(CampaignValidator.ValidationResult result)
    {
        var message = "";
        if (result.Errors.Count > 0)
        {
            message += "错误:\n" + string.Join("\n", result.Errors.Select(e => "• " + e)) + "\n\n";
        }
        if (result.Warnings.Count > 0)
        {
            message += "警告:\n" + string.Join("\n", result.Warnings.Select(w => "• " + w));
        }

        EditorUtility.DisplayDialog("验证结果", message, "确定");
    }

    private void LogValidationToConsole(CampaignConfig config, CampaignValidator.ValidationResult result)
    {
        string campaignId = config?.CampaignId ?? "未知";

        if (result.Errors.Count > 0)
        {
            foreach (var error in result.Errors)
            {
                Debug.LogError($"[战役验证] [{campaignId}] 错误: {error}");
            }
        }

        if (result.Warnings.Count > 0)
        {
            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning($"[战役验证] [{campaignId}] 警告: {warning}");
            }
        }

        if (result.IsValid && !result.HasWarnings)
        {
            Debug.Log($"[战役验证] [{campaignId}] 配置有效，无错误和警告");
        }
        else
        {
            Debug.Log($"[战役验证] [{campaignId}] 共 {result.Errors.Count} 个错误, {result.Warnings.Count} 个警告");
        }
    }

    #endregion

    #region 操作方法

    private void CreateNewCampaign()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "创建战役配置",
            "Campaign_New",
            "asset",
            "选择保存位置",
            ConfigPath);

        if (!string.IsNullOrEmpty(path))
        {
            var config = ScriptableObject.CreateInstance<CampaignConfig>();
            config.CampaignId = System.IO.Path.GetFileNameWithoutExtension(path);
            config.CampaignName = "新战役";
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            RefreshCampaignList();
            SelectCampaign(config);
        }
    }

    private void DuplicateSelectedCampaign()
    {
        if (selectedCampaign == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选择一个战役", "确定");
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(selectedCampaign);
        string directory = System.IO.Path.GetDirectoryName(sourcePath);
        string newPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{selectedCampaign.CampaignId}_Copy.asset");

        AssetDatabase.CopyAsset(sourcePath, newPath);
        AssetDatabase.SaveAssets();

        var newConfig = AssetDatabase.LoadAssetAtPath<CampaignConfig>(newPath);
        if (newConfig != null)
        {
            newConfig.CampaignId = System.IO.Path.GetFileNameWithoutExtension(newPath);
            newConfig.CampaignName = selectedCampaign.CampaignName + " (副本)";
            EditorUtility.SetDirty(newConfig);
            AssetDatabase.SaveAssets();
        }

        RefreshCampaignList();
        SelectCampaign(newConfig);
    }

    private void SaveSelectedCampaign()
    {
        if (selectedCampaign == null) return;

        EditorUtility.SetDirty(selectedCampaign);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CampaignEditor] 已保存: {selectedCampaign.CampaignId}");
    }

    private void ValidateSelectedCampaign()
    {
        if (selectedCampaign == null) return;

        var result = CampaignValidator.Validate(selectedCampaign, allCampaigns);
        validationCache[selectedCampaign] = result;
        LogValidationToConsole(selectedCampaign, result);
        ShowValidationDetails(result);
    }

    private void ValidateAllCampaigns()
    {
        int errorCount = 0;
        int warningCount = 0;

        Debug.Log("[战役验证] ===== 开始验证全部战役 =====");

        foreach (var campaign in allCampaigns)
        {
            var result = CampaignValidator.Validate(campaign, allCampaigns);
            validationCache[campaign] = result;
            errorCount += result.Errors.Count;
            warningCount += result.Warnings.Count;

            // 输出每个战役的验证结果到 Console
            if (result.Errors.Count > 0 || result.Warnings.Count > 0)
            {
                LogValidationToConsole(campaign, result);
            }
        }

        Debug.Log($"[战役验证] ===== 验证完成: {allCampaigns.Length} 个战役, {errorCount} 个错误, {warningCount} 个警告 =====");

        EditorUtility.DisplayDialog("验证完成",
            $"共验证 {allCampaigns.Length} 个战役\n错误: {errorCount}\n警告: {warningCount}\n\n详细信息已输出到 Console",
            "确定");

        Repaint();
    }

    #endregion
}

/// <summary>
/// 模板创建对话框
/// </summary>
public class CreateTemplateDialog : EditorWindow
{
    private CampaignTemplateType templateType;
    private string campaignId = "";
    private string campaignName = "";
    private System.Action<string, string> onConfirm;

    public static CreateTemplateDialog ShowDialog(CampaignTemplateType type, System.Action<string, string> callback)
    {
        var dialog = GetWindow<CreateTemplateDialog>(true, "从模板创建战役", true);
        dialog.templateType = type;
        dialog.campaignId = $"Campaign_{type}_{System.DateTime.Now:yyyyMMdd}";
        dialog.campaignName = CampaignTemplateFactory.GetTemplateName(type);
        dialog.onConfirm = callback;
        dialog.minSize = new Vector2(350, 150);
        dialog.maxSize = new Vector2(350, 150);
        dialog.ShowUtility();
        return dialog;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField($"模板: {templateType}", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        campaignId = EditorGUILayout.TextField("战役 ID", campaignId);
        campaignName = EditorGUILayout.TextField("战役名称", campaignName);

        EditorGUILayout.Space(20);

        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("取消", GUILayout.Width(80)))
            {
                Close();
            }

            if (GUILayout.Button("创建", GUILayout.Width(80)))
            {
                if (string.IsNullOrWhiteSpace(campaignId))
                {
                    EditorUtility.DisplayDialog("错误", "战役 ID 不能为空", "确定");
                    return;
                }

                onConfirm?.Invoke(campaignId, campaignName);
                Close();
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
