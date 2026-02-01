#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 战役编辑器样式定义
/// </summary>
public static class CampaignEditorStyles
{
    // 颜色常量
    public static readonly Color CardBackground = new Color(0.22f, 0.22f, 0.22f, 1f);
    public static readonly Color CardBackgroundLight = new Color(0.28f, 0.28f, 0.28f, 1f);
    public static readonly Color HeaderBackground = new Color(0.18f, 0.18f, 0.18f, 1f);
    public static readonly Color SelectedColor = new Color(0.24f, 0.37f, 0.58f, 1f);
    public static readonly Color HoverColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public static readonly Color BorderColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    public static readonly Color ErrorColor = new Color(0.9f, 0.3f, 0.3f, 1f);
    public static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f, 1f);
    public static readonly Color SuccessColor = new Color(0.3f, 0.8f, 0.4f, 1f);
    public static readonly Color InfoColor = new Color(0.4f, 0.7f, 0.9f, 1f);

    public static readonly Color FanaticColor = new Color(0.9f, 0.4f, 0.3f, 1f);
    public static readonly Color ConservativeColor = new Color(0.3f, 0.6f, 0.9f, 1f);
    public static readonly Color OpportunistColor = new Color(0.8f, 0.7f, 0.3f, 1f);

    // 缓存的样式
    private static GUIStyle _cardStyle;
    private static GUIStyle _headerStyle;
    private static GUIStyle _titleStyle;
    private static GUIStyle _subtitleStyle;
    private static GUIStyle _listItemStyle;
    private static GUIStyle _listItemSelectedStyle;
    private static GUIStyle _toolbarButtonStyle;
    private static GUIStyle _statusBarStyle;
    private static GUIStyle _foldoutHeaderStyle;
    private static GUIStyle _miniButtonStyle;
    private static GUIStyle _searchFieldStyle;
    private static GUIStyle _centeredLabelStyle;
    private static GUIStyle _richTextLabelStyle;

    public static GUIStyle CardStyle
    {
        get
        {
            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(4, 4, 4, 4)
                };
                var tex = MakeTexture(2, 2, CardBackground);
                _cardStyle.normal.background = tex;
            }
            return _cardStyle;
        }
    }

    public static GUIStyle HeaderStyle
    {
        get
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 6, 6),
                    margin = new RectOffset(0, 0, 0, 0),
                    fontStyle = FontStyle.Bold,
                    fontSize = 12
                };
                _headerStyle.normal.textColor = Color.white;
                var tex = MakeTexture(2, 2, HeaderBackground);
                _headerStyle.normal.background = tex;
            }
            return _headerStyle;
        }
    }

    public static GUIStyle TitleStyle
    {
        get
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    padding = new RectOffset(0, 0, 4, 4)
                };
                _titleStyle.normal.textColor = Color.white;
            }
            return _titleStyle;
        }
    }

    public static GUIStyle SubtitleStyle
    {
        get
        {
            if (_subtitleStyle == null)
            {
                _subtitleStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Italic
                };
                _subtitleStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            }
            return _subtitleStyle;
        }
    }

    public static GUIStyle ListItemStyle
    {
        get
        {
            if (_listItemStyle == null)
            {
                _listItemStyle = new GUIStyle(GUI.skin.label)
                {
                    padding = new RectOffset(8, 8, 6, 6),
                    margin = new RectOffset(2, 2, 1, 1),
                    alignment = TextAnchor.MiddleLeft
                };
                _listItemStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            }
            return _listItemStyle;
        }
    }

    public static GUIStyle ListItemSelectedStyle
    {
        get
        {
            if (_listItemSelectedStyle == null)
            {
                _listItemSelectedStyle = new GUIStyle(ListItemStyle);
                var tex = MakeTexture(2, 2, SelectedColor);
                _listItemSelectedStyle.normal.background = tex;
                _listItemSelectedStyle.normal.textColor = Color.white;
            }
            return _listItemSelectedStyle;
        }
    }

    public static GUIStyle ToolbarButtonStyle
    {
        get
        {
            if (_toolbarButtonStyle == null)
            {
                _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fontSize = 11,
                    padding = new RectOffset(8, 8, 2, 2)
                };
            }
            return _toolbarButtonStyle;
        }
    }

    public static GUIStyle StatusBarStyle
    {
        get
        {
            if (_statusBarStyle == null)
            {
                _statusBarStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 4, 4),
                    margin = new RectOffset(0, 0, 0, 0),
                    fontSize = 11
                };
                var tex = MakeTexture(2, 2, HeaderBackground);
                _statusBarStyle.normal.background = tex;
                _statusBarStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
            return _statusBarStyle;
        }
    }

    public static GUIStyle FoldoutHeaderStyle
    {
        get
        {
            if (_foldoutHeaderStyle == null)
            {
                _foldoutHeaderStyle = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(16, 4, 4, 4)
                };
            }
            return _foldoutHeaderStyle;
        }
    }

    public static GUIStyle MiniButtonStyle
    {
        get
        {
            if (_miniButtonStyle == null)
            {
                _miniButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 10,
                    padding = new RectOffset(6, 6, 2, 2)
                };
            }
            return _miniButtonStyle;
        }
    }

    public static GUIStyle SearchFieldStyle
    {
        get
        {
            if (_searchFieldStyle == null)
            {
                _searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField)
                {
                    margin = new RectOffset(4, 4, 4, 4)
                };
            }
            return _searchFieldStyle;
        }
    }

    public static GUIStyle CenteredLabelStyle
    {
        get
        {
            if (_centeredLabelStyle == null)
            {
                _centeredLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }
            return _centeredLabelStyle;
        }
    }

    public static GUIStyle RichTextLabelStyle
    {
        get
        {
            if (_richTextLabelStyle == null)
            {
                _richTextLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    wordWrap = true
                };
            }
            return _richTextLabelStyle;
        }
    }

    /// <summary>
    /// 创建纯色纹理
    /// </summary>
    public static Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 获取性格对应的颜色
    /// </summary>
    public static Color GetPersonalityColor(GeneralPersonality personality)
    {
        switch (personality)
        {
            case GeneralPersonality.Fanatic: return FanaticColor;
            case GeneralPersonality.Conservative: return ConservativeColor;
            case GeneralPersonality.Opportunist: return OpportunistColor;
            default: return Color.white;
        }
    }

    /// <summary>
    /// 获取性格对应的图标
    /// </summary>
    public static string GetPersonalityIcon(GeneralPersonality personality)
    {
        switch (personality)
        {
            case GeneralPersonality.Fanatic: return "⚔";
            case GeneralPersonality.Conservative: return "🛡";
            case GeneralPersonality.Opportunist: return "⚖";
            default: return "?";
        }
    }

    /// <summary>
    /// 获取性格中文名称
    /// </summary>
    public static string GetPersonalityName(GeneralPersonality personality)
    {
        switch (personality)
        {
            case GeneralPersonality.Fanatic: return "狂热";
            case GeneralPersonality.Conservative: return "保守";
            case GeneralPersonality.Opportunist: return "投机";
            default: return "未知";
        }
    }

    /// <summary>
    /// 重置所有缓存的样式（在编辑器重新加载时调用）
    /// </summary>
    public static void ResetStyles()
    {
        _cardStyle = null;
        _headerStyle = null;
        _titleStyle = null;
        _subtitleStyle = null;
        _listItemStyle = null;
        _listItemSelectedStyle = null;
        _toolbarButtonStyle = null;
        _statusBarStyle = null;
        _foldoutHeaderStyle = null;
        _miniButtonStyle = null;
        _searchFieldStyle = null;
        _centeredLabelStyle = null;
        _richTextLabelStyle = null;
    }
}
#endif
