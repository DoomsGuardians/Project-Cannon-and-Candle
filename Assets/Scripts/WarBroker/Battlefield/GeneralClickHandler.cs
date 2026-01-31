using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 将军点击处理器：点击将军底座时打开详情面板
/// </summary>
public class GeneralClickHandler : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("将军 ID，与 GeneralConfig 中的 ID 对应")]
    public string GeneralId;

    [Tooltip("是否为敌方将军（敌方将军不可点击详情）")]
    public bool IsEnemy = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsEnemy)
        {
            Debug.Log($"[GeneralClickHandler] 敌方将军 {GeneralId} 被点击，但不可查看详情");
            return;
        }

        if (string.IsNullOrEmpty(GeneralId))
        {
            Debug.LogWarning("[GeneralClickHandler] GeneralId 未设置");
            return;
        }

        var uiService = GameRoot.Instance?.uIService;
        if (uiService == null)
        {
            Debug.LogError("[GeneralClickHandler] UIService 未找到");
            return;
        }

        // 显示将军详情面板
        var panel = uiService.ShowWindow<GeneralDetailPanel>("GeneralDetailPanel");
        if (panel == null)
        {
            Debug.LogWarning("[GeneralClickHandler] GeneralDetailPanel 未注册");
            return;
        }

        // 获取将军数据
        var gameplayManager = GameRoot.Instance?.managerService?.GetManager<GameplayManager>();
        if (gameplayManager == null)
        {
            Debug.LogError("[GeneralClickHandler] GameplayManager 未找到");
            return;
        }

        var battleData = gameplayManager.GetBattleData();
        if (battleData == null)
        {
            Debug.LogError("[GeneralClickHandler] BattleData 未找到");
            return;
        }

        var general = battleData.AllyGenerals.Find(g => g.GeneralId == GeneralId);
        if (general == null)
        {
            Debug.LogWarning($"[GeneralClickHandler] 将军 {GeneralId} 未在 AllyGenerals 中找到");
            return;
        }

        panel.SetGeneral(general);
        Debug.Log($"[GeneralClickHandler] 打开将军详情: {general.Name}");
    }
}
