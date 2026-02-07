using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// 物理对齐模块 - UI部分
/// </summary>
public class AlignToGroundModule : ToolModule
{
    public override string Name => "物理对齐";
    public override string Category => "Scene";
    public override int Order => 2;
    public override string IconName => "d_MoveTool";
    public override Color HeaderColor => new Color(0.2f, 0.8f, 0.3f);
    public override Color BackgroundColor => new Color(0.5f, 1f, 0.5f);

    private AlignToGroundLogic.Settings _settings = new AlignToGroundLogic.Settings();
    private const string SETTINGS_KEY_PREFIX = "AlignToGround_";

    public override void OnInitialize()
    {
        LoadSettings();
    }

    public override bool IsAvailable(ToolContext context)
    {
        return context.HasSelectedTransforms;
    }

    public override void OnGUI(ToolContext context)
    {
        if (!context.HasSelectedTransforms)
        {
            EditorGUILayout.HelpBox("请在 Hierarchy 窗口中选择要对齐的物体。\n注意：目标表面需要有 Collider 组件。", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("吸附设置", EditorStyles.boldLabel);
        _settings.GroundLayerMask = SceneUtil.LayerMaskField("目标层级", _settings.GroundLayerMask);
        _settings.AlignToNormal = EditorGUILayout.Toggle("对齐表面法线", _settings.AlignToNormal);

        if (_settings.AlignToNormal)
        {
            EditorGUILayout.HelpBox("启用后，物体的 Up 方向将对齐到表面法线方向。", MessageType.None);
        }

        EditorGUILayout.Space(5);

        if (DrawIconButton("⬇️ 吸附到表面", IconName, HeaderColor, 30))
        {
            AlignToGroundLogic.SnapToGround(context.SelectedTransforms, _settings);
            SaveSettings();
        }
    }

    private bool DrawIconButton(string text, string iconName, Color buttonColor, float height)
    {
        Color originalBgColor = GUI.backgroundColor;
        GUI.backgroundColor = buttonColor * 0.8f;
        GUI.contentColor = Color.white;

        GUIContent buttonContent = IconHelper.GetIconContent(iconName, text);
        GUIStyle buttonStyle = ToolboxStyles.ButtonStyle(buttonColor);

        bool clicked = GUILayout.Button(buttonContent, buttonStyle, GUILayout.Height(height));

        GUI.backgroundColor = originalBgColor;
        GUI.contentColor = Color.white;
        return clicked;
    }

    private void LoadSettings()
    {
        _settings.GroundLayerMask = ToolboxSettings.GetInt(SETTINGS_KEY_PREFIX + "GroundLayerMask", -1);
        _settings.AlignToNormal = ToolboxSettings.GetBool(SETTINGS_KEY_PREFIX + "AlignToNormal", false);
    }

    private void SaveSettings()
    {
        ToolboxSettings.SetInt(SETTINGS_KEY_PREFIX + "GroundLayerMask", _settings.GroundLayerMask);
        ToolboxSettings.SetBool(SETTINGS_KEY_PREFIX + "AlignToNormal", _settings.AlignToNormal);
    }
}

