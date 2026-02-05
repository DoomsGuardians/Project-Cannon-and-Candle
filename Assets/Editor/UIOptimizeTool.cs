using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 批量优化 UI 组件的 Raycast Target 和 Maskable 选项
/// </summary>
public class UIOptimizeTool : EditorWindow
{
    private GameObject targetRoot;
    private bool optimizeRaycast = true;
    private bool optimizeMaskable = true;
    private Vector2 scrollPos;
    private List<OptimizeResult> results = new List<OptimizeResult>();

    private struct OptimizeResult
    {
        public string path;
        public string componentType;
        public bool raycastChanged;
        public bool maskableChanged;
        public string skipReason;
    }

    [MenuItem("Tools/UI Optimize Tool")]
    public static void ShowWindow()
    {
        GetWindow<UIOptimizeTool>("UI 优化工具");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("UI 组件批量优化", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "此工具会关闭不需要的 Raycast Target 和 Maskable 选项以提升性能。\n" +
            "• Raycast Target: 仅保留需要交互的组件\n" +
            "• Maskable: 仅保留在 Mask/RectMask2D 下的组件",
            MessageType.Info);

        EditorGUILayout.Space(10);

        targetRoot = (GameObject)EditorGUILayout.ObjectField(
            "目标根节点", targetRoot, typeof(GameObject), true);

        EditorGUILayout.Space(5);
        optimizeRaycast = EditorGUILayout.Toggle("优化 Raycast Target", optimizeRaycast);
        optimizeMaskable = EditorGUILayout.Toggle("优化 Maskable", optimizeMaskable);

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("预览更改", GUILayout.Height(30)))
        {
            PreviewChanges();
        }
        if (GUILayout.Button("应用更改", GUILayout.Height(30)))
        {
            ApplyChanges();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        if (results.Count > 0)
        {
            EditorGUILayout.LabelField($"结果预览 ({results.Count} 个组件)", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

            foreach (var result in results)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(result.path, GUILayout.Width(250));
                EditorGUILayout.LabelField(result.componentType, GUILayout.Width(100));

                if (!string.IsNullOrEmpty(result.skipReason))
                {
                    EditorGUILayout.LabelField($"[跳过] {result.skipReason}", EditorStyles.miniLabel);
                }
                else
                {
                    string changes = "";
                    if (result.raycastChanged) changes += "Raycast ";
                    if (result.maskableChanged) changes += "Maskable ";
                    if (!string.IsNullOrEmpty(changes))
                    {
                        EditorGUILayout.LabelField($"[关闭] {changes}", EditorStyles.miniLabel);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // 统计信息
            int raycastCount = results.Count(r => r.raycastChanged);
            int maskableCount = results.Count(r => r.maskableChanged);
            int skipCount = results.Count(r => !string.IsNullOrEmpty(r.skipReason));
            EditorGUILayout.LabelField(
                $"统计: 关闭 Raycast {raycastCount} 个, 关闭 Maskable {maskableCount} 个, 跳过 {skipCount} 个");
        }
    }

    private void PreviewChanges()
    {
        if (targetRoot == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择目标根节点", "确定");
            return;
        }

        results.Clear();
        AnalyzeGameObject(targetRoot, false);
    }

    private void ApplyChanges()
    {
        if (targetRoot == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择目标根节点", "确定");
            return;
        }

        results.Clear();
        Undo.RegisterCompleteObjectUndo(targetRoot, "UI Optimize");
        AnalyzeGameObject(targetRoot, true);

        int raycastCount = results.Count(r => r.raycastChanged);
        int maskableCount = results.Count(r => r.maskableChanged);

        EditorUtility.DisplayDialog("完成",
            $"优化完成!\n关闭 Raycast Target: {raycastCount} 个\n关闭 Maskable: {maskableCount} 个",
            "确定");

        // 标记为已修改
        if (PrefabUtility.IsPartOfPrefabInstance(targetRoot))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetRoot);
        }
        EditorUtility.SetDirty(targetRoot);
    }

    private void AnalyzeGameObject(GameObject go, bool apply)
    {
        // 分析 Image 组件
        var images = go.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            AnalyzeMaskableGraphic(img, apply);
        }

        // 分析 TextMeshProUGUI 组件
        var tmps = go.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmps)
        {
            AnalyzeMaskableGraphic(tmp, apply);
        }

        // 分析 RawImage 组件
        var rawImages = go.GetComponentsInChildren<RawImage>(true);
        foreach (var raw in rawImages)
        {
            AnalyzeMaskableGraphic(raw, apply);
        }
    }

    private void AnalyzeMaskableGraphic(MaskableGraphic graphic, bool apply)
    {
        var go = graphic.gameObject;
        var result = new OptimizeResult
        {
            path = GetGameObjectPath(go),
            componentType = graphic.GetType().Name,
            raycastChanged = false,
            maskableChanged = false,
            skipReason = ""
        };

        // 检查是否需要保留 Raycast Target
        bool needRaycast = CheckNeedRaycast(go);

        // 检查是否需要保留 Maskable
        bool needMaskable = CheckNeedMaskable(go);

        // 处理 Raycast Target
        if (optimizeRaycast && graphic.raycastTarget && !needRaycast)
        {
            result.raycastChanged = true;
            if (apply)
            {
                Undo.RecordObject(graphic, "Disable Raycast");
                graphic.raycastTarget = false;
                EditorUtility.SetDirty(graphic);
            }
        }
        else if (optimizeRaycast && graphic.raycastTarget && needRaycast)
        {
            result.skipReason = GetRaycastReason(go);
        }

        // 处理 Maskable
        if (optimizeMaskable && graphic.maskable && !needMaskable)
        {
            result.maskableChanged = true;
            if (apply)
            {
                Undo.RecordObject(graphic, "Disable Maskable");
                graphic.maskable = false;
                EditorUtility.SetDirty(graphic);
            }
        }
        else if (optimizeMaskable && graphic.maskable && needMaskable)
        {
            if (string.IsNullOrEmpty(result.skipReason))
                result.skipReason = "在 Mask 下";
            else
                result.skipReason += ", 在 Mask 下";
        }

        // 只记录有变化或被跳过的结果
        if (result.raycastChanged || result.maskableChanged || !string.IsNullOrEmpty(result.skipReason))
        {
            results.Add(result);
        }
    }

    /// <summary>
    /// 检查是否需要保留 Raycast Target
    /// </summary>
    private bool CheckNeedRaycast(GameObject go)
    {
        // 1. 自身有可交互组件
        if (go.GetComponent<Selectable>() != null) return true;
        if (go.GetComponent<UnityEngine.EventSystems.IPointerClickHandler>() != null) return true;
        if (go.GetComponent<UnityEngine.EventSystems.IPointerDownHandler>() != null) return true;
        if (go.GetComponent<UnityEngine.EventSystems.IPointerUpHandler>() != null) return true;
        if (go.GetComponent<UnityEngine.EventSystems.IDragHandler>() != null) return true;
        if (go.GetComponent<UnityEngine.EventSystems.IDropHandler>() != null) return true;
        if (go.GetComponent<UnityEngine.EventSystems.IScrollHandler>() != null) return true;
        if (go.GetComponent<UnityEngine.EventSystems.EventTrigger>() != null) return true;

        // 2. 检查是否是 ScrollRect 的关键子物体
        var scrollRect = go.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            if (scrollRect.viewport == go.transform) return true;
            if (scrollRect.content == go.transform) return true;
        }

        // 3. 检查是否有 UIListener 组件（项目自定义的交互组件）
        var uiListener = go.GetComponent("UIListener");
        if (uiListener != null) return true;

        // 4. 检查父级是否有 Button 等组件，且自身是 targetGraphic
        var parentSelectable = go.GetComponentInParent<Selectable>();
        if (parentSelectable != null)
        {
            if (parentSelectable.targetGraphic != null &&
                parentSelectable.targetGraphic.gameObject == go)
            {
                return true;
            }
            // Button 的直接子 Image 通常需要接收点击
            if (parentSelectable.gameObject == go.transform.parent?.gameObject)
            {
                var img = go.GetComponent<Image>();
                if (img != null && parentSelectable.targetGraphic == img)
                {
                    return true;
                }
            }
        }

        // 5. 检查是否是 Toggle 的 graphic
        var toggle = go.GetComponentInParent<Toggle>();
        if (toggle != null && toggle.graphic != null && toggle.graphic.gameObject == go)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取需要保留 Raycast 的原因
    /// </summary>
    private string GetRaycastReason(GameObject go)
    {
        if (go.GetComponent<Selectable>() != null) return "Selectable";
        if (go.GetComponent<UnityEngine.EventSystems.EventTrigger>() != null) return "EventTrigger";
        if (go.GetComponent("UIListener") != null) return "UIListener";

        var scrollRect = go.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            if (scrollRect.viewport == go.transform) return "ScrollRect.viewport";
            if (scrollRect.content == go.transform) return "ScrollRect.content";
        }

        var parentSelectable = go.GetComponentInParent<Selectable>();
        if (parentSelectable != null)
        {
            if (parentSelectable.targetGraphic?.gameObject == go) return "targetGraphic";
        }

        return "需要交互";
    }

    /// <summary>
    /// 检查是否需要保留 Maskable（在 Mask 或 RectMask2D 下）
    /// </summary>
    private bool CheckNeedMaskable(GameObject go)
    {
        Transform current = go.transform.parent;
        while (current != null)
        {
            if (current.GetComponent<Mask>() != null) return true;
            if (current.GetComponent<RectMask2D>() != null) return true;
            current = current.parent;
        }
        return false;
    }

    private string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        int depth = 0;
        while (parent != null && depth < 3)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
            depth++;
        }
        if (parent != null)
        {
            path = ".../" + path;
        }
        return path;
    }
}
