#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// WarBroker Editor 调试窗口
/// </summary>
public class WarBrokerDebugWindow : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("WarBroker/Debug Window")]
    static void Open()
    {
        GetWindow<WarBrokerDebugWindow>("WarBroker Debug");
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("仅在 PlayMode 下可用", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("快速导航", EditorStyles.boldLabel);

            if (GUILayout.Button("打开 GameBalanceConfig"))
                SelectAsset("Assets/Resources/Config/WarBroker/GameBalanceConfig.asset");
            if (GUILayout.Button("打开 OrderConfig"))
                SelectAsset("Assets/Resources/Config/WarBroker/OrderConfig.asset");
            if (GUILayout.Button("打开 SkillConfig"))
                SelectAsset("Assets/Resources/Config/WarBroker/SkillConfig.asset");
            if (GUILayout.Button("打开 GeneralConfig"))
                SelectAsset("Assets/Resources/Config/WarBroker/GeneralConfig.asset");

            return;
        }

        var campaignSystem = GameRoot.Instance?.campaignSystem;
        if (campaignSystem?.Data == null)
        {
            EditorGUILayout.HelpBox("无活跃战役", MessageType.Warning);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        var data = campaignSystem.Data;

        EditorGUILayout.LabelField("战役信息", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  战役: {data.Config.CampaignName}");
        EditorGUILayout.LabelField($"  回合: {data.CurrentTurn}/{data.MaxTurns}  阶段: {data.CurrentPhase}");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("玩家", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  现金: {data.Player.Cash:F2}  负债: {data.Player.BankDebt:F2}");
        EditorGUILayout.LabelField($"  净资产: {data.Player.CalculateNetWorth(data.Market):F2}  审计: {data.Player.AuditValue}");
        EditorGUILayout.LabelField($"  后备役: {data.Battle.CurrentReserves}");
        foreach (var kvp in data.Player.Inventory)
            EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("市场", EditorStyles.boldLabel);
        foreach (var kvp in data.Market.CurrentPrices)
            EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value:F2} (库存: {data.Market.MarketInventory[kvp.Key]})");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("战线", EditorStyles.boldLabel);
        foreach (var kvp in data.Battle.Frontlines)
            EditorGUILayout.LabelField($"  {kvp.Key}: 位置={kvp.Value.LinePosition} 停滞={kvp.Value.StagnantTurns}");

        var balanceConfig = GameRoot.Instance.resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("己方将军", EditorStyles.boldLabel);
        foreach (var g in data.Battle.AllyGenerals)
        {
            EditorGUILayout.LabelField($"  [{g.Position}] {g.Name} ({g.Personality}) - {g.GetStatus(balanceConfig)}");
            EditorGUILayout.LabelField($"    兵:{g.Troops} 信:{g.Trust} 气:{g.Morale} 令:{g.AssignedOrder?.ToString() ?? "-"}");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("敌方将军", EditorStyles.boldLabel);
        foreach (var g in data.Battle.EnemyGenerals)
        {
            EditorGUILayout.LabelField($"  [{g.Position}] {g.Name} ({g.Personality}) - {g.GetStatus(balanceConfig)}");
            EditorGUILayout.LabelField($"    兵:{g.Troops} 信:{g.Trust} 气:{g.Morale}");
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("刷新"))
            Repaint();
    }

    private void SelectAsset(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (asset != null)
            Selection.activeObject = asset;
        else
            Debug.LogWarning($"资产不存在: {path}");
    }

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying)
            Repaint();
    }
}
#endif
