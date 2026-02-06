#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 将军配置编辑器窗口
/// 菜单入口: WarBroker > General Editor
/// </summary>
public class GeneralEditorWindow : EditorWindow
{
    // 配置资产路径
    private const string ConfigPath = "Assets/Resources/Config/WarBroker/";

    // 将军配置
    private GeneralConfig generalConfig;
    private GeneralConfigItem selectedGeneral;
    private int selectedGeneralIndex = -1;
    private bool selectedIsAlly = true;

    // 滚动位置
    private Vector2 listScrollPos;
    private Vector2 detailScrollPos;

    // 折叠状态
    private bool showAllyGenerals = true;
    private bool showEnemyGenerals = true;

    // SerializedObject
    private SerializedObject serializedConfig;

    // 士兵网格编辑
    private static readonly Color[] SoldierColors = new Color[]
    {
        new Color(0.4f, 0.6f, 0.9f, 1f),  // Pikeman - 蓝色
        new Color(0.9f, 0.5f, 0.3f, 1f),  // Musketeer - 橙色
        new Color(0.5f, 0.8f, 0.4f, 1f),  // Cavalry - 绿色
        new Color(0.7f, 0.4f, 0.8f, 1f),  // FrontPikeman - 紫色
    };

    private static readonly string[] SoldierIcons = new string[]
    {
        "▲",  // Pikeman - 长枪兵
        "●",  // Musketeer - 火枪兵
        "◆",  // Cavalry - 骑兵
        "▼",  // FrontPikeman - 前排枪兵
    };

    private static readonly string[] SoldierNames = new string[]
    {
        "长枪兵",
        "火枪兵",
        "骑兵",
        "前排枪兵",
    };

    [MenuItem("WarBroker/General Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<GeneralEditorWindow>("将军编辑器");
        window.minSize = new Vector2(800, 500);
        window.Show();
    }

    private void OnEnable()
    {
        LoadGeneralConfig();
    }

    private void OnFocus()
    {
        LoadGeneralConfig();
    }

    private void LoadGeneralConfig()
    {
        string path = ConfigPath + "GeneralConfig.asset";
        generalConfig = AssetDatabase.LoadAssetAtPath<GeneralConfig>(path);

        if (generalConfig != null)
        {
            serializedConfig = new SerializedObject(generalConfig);
        }
        else
        {
            serializedConfig = null;
        }

        // 清除选择
        selectedGeneral = null;
        selectedGeneralIndex = -1;
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (generalConfig == null)
        {
            EditorGUILayout.HelpBox("未找到 GeneralConfig，请先创建配置文件", MessageType.Warning);
            if (GUILayout.Button("创建 GeneralConfig"))
            {
                WarBrokerConfigSetup.CreateEmptyGeneralConfig();
                LoadGeneralConfig();
            }
            return;
        }

        EditorGUILayout.BeginHorizontal();
        {
            // 左侧面板：将军列表
            DrawLeftPanel();

            // 分隔线
            EditorGUILayout.BeginVertical(GUILayout.Width(1));
            GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(1));
            EditorGUILayout.EndVertical();

            // 右侧面板：将军详情
            DrawRightPanel();
        }
        EditorGUILayout.EndHorizontal();
    }

    #region 工具栏

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            if (GUILayout.Button("刷新", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(50)))
            {
                LoadGeneralConfig();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("保存", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(50)))
            {
                SaveConfig();
            }

            if (GUILayout.Button("选中配置", CampaignEditorStyles.ToolbarButtonStyle, GUILayout.Width(70)))
            {
                if (generalConfig != null)
                {
                    Selection.activeObject = generalConfig;
                    EditorGUIUtility.PingObject(generalConfig);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 左侧面板 - 将军列表

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        {
            listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);
            {
                // 己方将军
                DrawGeneralSection("己方将军", ref showAllyGenerals, generalConfig.AllyGenerals, true);

                EditorGUILayout.Space(8);

                // 敌方将军
                DrawGeneralSection("敌方将军", ref showEnemyGenerals, generalConfig.EnemyGenerals, false);
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawGeneralSection(string title, ref bool foldout, GeneralConfigItem[] generals, bool isAlly)
    {
        foldout = EditorGUILayout.Foldout(foldout, $"===== {title} =====", true, CampaignEditorStyles.FoldoutHeaderStyle);

        if (foldout)
        {
            if (generals != null)
            {
                for (int i = 0; i < generals.Length; i++)
                {
                    var general = generals[i];
                    if (general == null) continue;

                    bool isSelected = selectedGeneral == general;
                    DrawGeneralListItem(general, isSelected, isAlly, i);
                }
            }

            // 添加按钮
            if (GUILayout.Button("+ 添加将军", CampaignEditorStyles.MiniButtonStyle))
            {
                AddNewGeneral(isAlly);
            }
        }
    }

    private void DrawGeneralListItem(GeneralConfigItem general, bool isSelected, bool isAlly, int index)
    {
        var style = isSelected ? CampaignEditorStyles.ListItemSelectedStyle : CampaignEditorStyles.ListItemStyle;

        EditorGUILayout.BeginHorizontal(style);
        {
            // 性格图标
            var color = CampaignEditorStyles.GetPersonalityColor(general.Personality);
            var oldColor = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(CampaignEditorStyles.GetPersonalityIcon(general.Personality), GUILayout.Width(20));
            GUI.contentColor = oldColor;

            // 将军名称和兵力
            GUILayout.Label($"{general.Name} ({general.InitialTroops})");

            GUILayout.FlexibleSpace();

            // 删除按钮
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除将军 \"{general.Name}\" 吗？", "删除", "取消"))
                {
                    RemoveGeneral(isAlly, index);
                    return;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // 点击选择
        var rect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            SelectGeneral(general, isAlly, index);
            Event.current.Use();
        }
    }

    private void SelectGeneral(GeneralConfigItem general, bool isAlly, int index)
    {
        selectedGeneral = general;
        selectedIsAlly = isAlly;
        selectedGeneralIndex = index;
        Repaint();
    }

    private void AddNewGeneral(bool isAlly)
    {
        Undo.RecordObject(generalConfig, "Add General");

        var newGeneral = new GeneralConfigItem
        {
            GeneralId = $"General_{System.DateTime.Now.Ticks}",
            Name = "新将军",
            Personality = GeneralPersonality.Opportunist,
            InitialTroops = 16,
            InitialTrust = 50,
            InitialMorale = 60,
            AtkBidModifier = 1f,
            DefBidModifier = 1f,
            RetBidModifier = 1f,
            soldierConfig = new GeneralSoldierConfig()
        };

        if (isAlly)
        {
            var list = generalConfig.AllyGenerals?.ToList() ?? new List<GeneralConfigItem>();
            list.Add(newGeneral);
            generalConfig.AllyGenerals = list.ToArray();
        }
        else
        {
            var list = generalConfig.EnemyGenerals?.ToList() ?? new List<GeneralConfigItem>();
            list.Add(newGeneral);
            generalConfig.EnemyGenerals = list.ToArray();
        }

        EditorUtility.SetDirty(generalConfig);
        serializedConfig = new SerializedObject(generalConfig);

        // 选中新将军
        SelectGeneral(newGeneral, isAlly, isAlly ? generalConfig.AllyGenerals.Length - 1 : generalConfig.EnemyGenerals.Length - 1);
    }

    private void RemoveGeneral(bool isAlly, int index)
    {
        Undo.RecordObject(generalConfig, "Remove General");

        if (isAlly)
        {
            var list = generalConfig.AllyGenerals.ToList();
            list.RemoveAt(index);
            generalConfig.AllyGenerals = list.ToArray();
        }
        else
        {
            var list = generalConfig.EnemyGenerals.ToList();
            list.RemoveAt(index);
            generalConfig.EnemyGenerals = list.ToArray();
        }

        EditorUtility.SetDirty(generalConfig);
        serializedConfig = new SerializedObject(generalConfig);

        // 清除选择
        selectedGeneral = null;
        selectedGeneralIndex = -1;
        Repaint();
    }

    #endregion

    #region 右侧面板 - 将军详情

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical();
        {
            if (selectedGeneral == null)
            {
                EditorGUILayout.LabelField("请从左侧列表选择一个将军", CampaignEditorStyles.CenteredLabelStyle, GUILayout.ExpandHeight(true));
            }
            else
            {
                detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);
                {
                    DrawGeneralDetail();
                }
                EditorGUILayout.EndScrollView();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawGeneralDetail()
    {
        // 头像和基础信息
        EditorGUILayout.BeginHorizontal();
        {
            // 头像
            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            {
                if (selectedGeneral.Portrait != null)
                {
                    var rect = GUILayoutUtility.GetRect(64, 64);
                    GUI.DrawTexture(rect, selectedGeneral.Portrait.texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUILayout.Box("无头像", GUILayout.Width(64), GUILayout.Height(64));
                }

                EditorGUI.BeginChangeCheck();
                var newPortrait = (Sprite)EditorGUILayout.ObjectField(selectedGeneral.Portrait, typeof(Sprite), false, GUILayout.Width(64));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(generalConfig, "Change Portrait");
                    selectedGeneral.Portrait = newPortrait;
                    EditorUtility.SetDirty(generalConfig);
                }
            }
            EditorGUILayout.EndVertical();

            // 基础信息
            EditorGUILayout.BeginVertical();
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("ID: " + selectedGeneral.GeneralId, EditorStyles.boldLabel);

                var newId = EditorGUILayout.TextField("将军 ID", selectedGeneral.GeneralId);
                var newName = EditorGUILayout.TextField("名称", selectedGeneral.Name);
                var newPersonality = (GeneralPersonality)EditorGUILayout.EnumPopup("性格", selectedGeneral.Personality);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(generalConfig, "Change General Info");
                    selectedGeneral.GeneralId = newId;
                    selectedGeneral.Name = newName;
                    selectedGeneral.Personality = newPersonality;
                    EditorUtility.SetDirty(generalConfig);
                }

                // 显示性格信息
                var personalityColor = CampaignEditorStyles.GetPersonalityColor(selectedGeneral.Personality);
                var oldColor = GUI.contentColor;
                GUI.contentColor = personalityColor;
                EditorGUILayout.LabelField($"{CampaignEditorStyles.GetPersonalityIcon(selectedGeneral.Personality)} {CampaignEditorStyles.GetPersonalityName(selectedGeneral.Personality)}");
                GUI.contentColor = oldColor;
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(16);

        // 初始属性
        EditorGUILayout.LabelField("初始属性", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();

        int newTroops = EditorGUILayout.IntSlider("兵力", selectedGeneral.InitialTroops, 0, 20);
        int newTrust = EditorGUILayout.IntSlider("信任度", selectedGeneral.InitialTrust, 0, 100);
        int newMorale = EditorGUILayout.IntSlider("士气", selectedGeneral.InitialMorale, 0, 100);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(generalConfig, "Change General Stats");
            selectedGeneral.InitialTroops = newTroops;
            selectedGeneral.InitialTrust = newTrust;
            selectedGeneral.InitialMorale = newMorale;
            EditorUtility.SetDirty(generalConfig);
        }

        EditorGUILayout.Space(16);

        // 出价系数
        EditorGUILayout.LabelField("出价系数", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();

        float newAtkBid = EditorGUILayout.FloatField("ATK 系数", selectedGeneral.AtkBidModifier);
        float newDefBid = EditorGUILayout.FloatField("DEF 系数", selectedGeneral.DefBidModifier);
        float newRetBid = EditorGUILayout.FloatField("RET 系数", selectedGeneral.RetBidModifier);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(generalConfig, "Change Bid Modifiers");
            selectedGeneral.AtkBidModifier = newAtkBid;
            selectedGeneral.DefBidModifier = newDefBid;
            selectedGeneral.RetBidModifier = newRetBid;
            EditorUtility.SetDirty(generalConfig);
        }

        EditorGUILayout.Space(16);

        // 简介
        EditorGUILayout.LabelField("简介", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();
        string newBio = EditorGUILayout.TextArea(selectedGeneral.Biography, GUILayout.Height(60));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(generalConfig, "Change Biography");
            selectedGeneral.Biography = newBio;
            EditorUtility.SetDirty(generalConfig);
        }

        EditorGUILayout.Space(16);

        // 士兵网格
        DrawSoldierGrid();
    }

    #endregion

    #region 士兵网格编辑

    private void DrawSoldierGrid()
    {
        EditorGUILayout.LabelField("士兵配置 (2x10)", CampaignEditorStyles.TitleStyle);
        EditorGUILayout.Space(4);

        // 快捷填充按钮
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("快捷填充:", GUILayout.Width(60));

            if (GUILayout.Button("全部长枪", CampaignEditorStyles.MiniButtonStyle))
            {
                FillAllSoldiers(SoldierType.Pikeman);
            }
            if (GUILayout.Button("全部火枪", CampaignEditorStyles.MiniButtonStyle))
            {
                FillAllSoldiers(SoldierType.Musketeer);
            }
            if (GUILayout.Button("全部骑兵", CampaignEditorStyles.MiniButtonStyle))
            {
                FillAllSoldiers(SoldierType.Cavalry);
            }
            if (GUILayout.Button("全部前排枪", CampaignEditorStyles.MiniButtonStyle))
            {
                FillAllSoldiers(SoldierType.FrontPikeman);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // 确保 soldierConfig 存在
        if (selectedGeneral.soldierConfig == null)
        {
            selectedGeneral.soldierConfig = new GeneralSoldierConfig();
            EditorUtility.SetDirty(generalConfig);
        }

        var soldierConfig = selectedGeneral.soldierConfig;

        // 绘制 2 排（每排显示 row1+row2 或 row3+row4）
        DrawCombinedSoldierRow("第1排", soldierConfig.row1, soldierConfig.row2, 0);
        DrawCombinedSoldierRow("第2排", soldierConfig.row3, soldierConfig.row4, 1);

        EditorGUILayout.Space(8);

        // 图例
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Label("图例:", GUILayout.Width(40));

            for (int i = 0; i < SoldierIcons.Length; i++)
            {
                var oldColor = GUI.contentColor;
                GUI.contentColor = SoldierColors[i];
                GUILayout.Label($"{SoldierIcons[i]} {SoldierNames[i]}");
                GUI.contentColor = oldColor;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("点击格子切换兵种：长枪兵 → 火枪兵 → 骑兵 → 前排枪兵 → 长枪兵", MessageType.Info);
    }

    /// <summary>绘制合并的士兵排（两个数据行合并显示为一排）</summary>
    private void DrawCombinedSoldierRow(string label, SoldierType[] rowA, SoldierType[] rowB, int rowIndex)
    {
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Label(label, GUILayout.Width(50));

            // 绘制第一个数据行的5个格子
            for (int col = 0; col < 5; col++)
            {
                DrawSoldierCell(rowA, col, rowIndex * 2);
            }

            // 绘制第二个数据行的5个格子
            for (int col = 0; col < 5; col++)
            {
                DrawSoldierCell(rowB, col, rowIndex * 2 + 1);
            }

            // 行填充按钮
            if (GUILayout.Button("▼", GUILayout.Width(20)))
            {
                ShowCombinedRowFillMenu(rowA, rowB, rowIndex);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSoldierCell(SoldierType[] row, int col, int rowIndex)
    {
        if (row == null || col >= row.Length) return;

        var soldierType = row[col];
        int typeIndex = (int)soldierType;

        var bgColor = GUI.backgroundColor;
        GUI.backgroundColor = SoldierColors[typeIndex];

        var content = new GUIContent(SoldierIcons[typeIndex], SoldierNames[typeIndex]);

        if (GUILayout.Button(content, GUILayout.Width(30), GUILayout.Height(30)))
        {
            // 点击切换到下一个兵种
            Undo.RecordObject(generalConfig, "Change Soldier Type");
            row[col] = (SoldierType)(((int)row[col] + 1) % 4);
            EditorUtility.SetDirty(generalConfig);
        }

        GUI.backgroundColor = bgColor;
    }

    private void ShowCombinedRowFillMenu(SoldierType[] rowA, SoldierType[] rowB, int rowIndex)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("填充长枪兵"), false, () => FillCombinedRow(rowA, rowB, SoldierType.Pikeman));
        menu.AddItem(new GUIContent("填充火枪兵"), false, () => FillCombinedRow(rowA, rowB, SoldierType.Musketeer));
        menu.AddItem(new GUIContent("填充骑兵"), false, () => FillCombinedRow(rowA, rowB, SoldierType.Cavalry));
        menu.AddItem(new GUIContent("填充前排枪兵"), false, () => FillCombinedRow(rowA, rowB, SoldierType.FrontPikeman));
        menu.ShowAsContext();
    }

    private void FillCombinedRow(SoldierType[] rowA, SoldierType[] rowB, SoldierType type)
    {
        Undo.RecordObject(generalConfig, "Fill Row");
        FillRowDirect(rowA, type);
        FillRowDirect(rowB, type);
        EditorUtility.SetDirty(generalConfig);
    }

    private void FillAllSoldiers(SoldierType type)
    {
        if (selectedGeneral?.soldierConfig == null) return;

        Undo.RecordObject(generalConfig, "Fill All Soldiers");

        var config = selectedGeneral.soldierConfig;
        FillRowDirect(config.row1, type);
        FillRowDirect(config.row2, type);
        FillRowDirect(config.row3, type);
        FillRowDirect(config.row4, type);

        EditorUtility.SetDirty(generalConfig);
    }

    private void FillRowDirect(SoldierType[] row, SoldierType type)
    {
        if (row == null) return;
        for (int i = 0; i < row.Length; i++)
        {
            row[i] = type;
        }
    }

    #endregion

    #region 保存

    private void SaveConfig()
    {
        if (generalConfig == null) return;

        EditorUtility.SetDirty(generalConfig);
        AssetDatabase.SaveAssets();
        Debug.Log("[GeneralEditor] 配置已保存");
    }

    #endregion
}
#endif
