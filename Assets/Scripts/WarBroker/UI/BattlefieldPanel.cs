using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战场面板：3条战线可视化
/// </summary>
public class BattlefieldPanel : WindowBase
{
    private Slider sliderLeft, sliderCenter, sliderRight;
    private Text txtLeftAlly, txtLeftEnemy;
    private Text txtCenterAlly, txtCenterEnemy;
    private Text txtRightAlly, txtRightEnemy;
    private Text txtBattleResults, txtEventInfo;

    private GameplayManager gameplayManager;

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();

        var b = gameObject.GetComponent<BattlefieldPanelBinder>();
        if (b != null)
        {
            sliderLeft = b.sliderLeft; sliderCenter = b.sliderCenter; sliderRight = b.sliderRight;
            txtLeftAlly = b.txtLeftAlly; txtLeftEnemy = b.txtLeftEnemy;
            txtCenterAlly = b.txtCenterAlly; txtCenterEnemy = b.txtCenterEnemy;
            txtRightAlly = b.txtRightAlly; txtRightEnemy = b.txtRightEnemy;
            txtBattleResults = b.txtBattleResults; txtEventInfo = b.txtEventInfo;
        }

        if (sliderLeft != null) { sliderLeft.minValue = 1; sliderLeft.maxValue = 5; sliderLeft.interactable = false; }
        if (sliderCenter != null) { sliderCenter.minValue = 1; sliderCenter.maxValue = 5; sliderCenter.interactable = false; }
        if (sliderRight != null) { sliderRight.minValue = 1; sliderRight.maxValue = 5; sliderRight.interactable = false; }
    }

    public override void OnShow()
    {
        eventService.AddEventListening((EventID)WarBrokerEventID.OnBattleResult, OnBattleResult);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnRefresh);
        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    private void RefreshUI()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null) return;
        var battle = data.Battle;

        RefreshFrontline(FrontlinePosition.Left, sliderLeft, txtLeftAlly, txtLeftEnemy, battle);
        RefreshFrontline(FrontlinePosition.Center, sliderCenter, txtCenterAlly, txtCenterEnemy, battle);
        RefreshFrontline(FrontlinePosition.Right, sliderRight, txtRightAlly, txtRightEnemy, battle);

        if (txtEventInfo != null)
        {
            string info = $"后备役: {data.Battle.CurrentReserves}";
            if (data.ActiveEvent != null)
                info += $"\n事件: {data.ActiveEvent.EventName} - {data.ActiveEvent.Description}";
            txtEventInfo.text = info;
        }
    }

    private void RefreshFrontline(FrontlinePosition pos, Slider slider, Text allyText, Text enemyText, BattleData battle)
    {
        if (!battle.Frontlines.ContainsKey(pos)) return;
        var frontline = battle.Frontlines[pos];
        if (slider != null) slider.value = frontline.LinePosition;

        var ally = battle.AllyGenerals.Find(g => g.Position == pos);
        var enemy = battle.EnemyGenerals.Find(g => g.Position == pos);
        if (allyText != null && ally != null) allyText.text = $"{ally.Name}\n兵力:{ally.Troops} 士气:{ally.Morale}";
        if (enemyText != null && enemy != null) enemyText.text = $"{enemy.Name}\n兵力:{enemy.Troops}";
    }

    private void OnBattleResult(object p1, object p2) => RefreshUI();
    private void OnRefresh(object p1, object p2) => RefreshUI();
}
