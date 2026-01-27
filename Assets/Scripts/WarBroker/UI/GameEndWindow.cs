using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏结束弹窗：胜负结果 + 统计
/// </summary>
public class GameEndWindow : WindowBase
{
    private Text txtTitle, txtStats;
    private Button btnRestart, btnMainMenu;

    public override void OnAwake()
    {
        base.OnAwake();
        uiLayer = UILayer.Top;

        var b = gameObject.GetComponent<GameEndWindowBinder>();
        if (b != null)
        {
            txtTitle = b.txtTitle; txtStats = b.txtStats;
            btnRestart = b.btnRestart; btnMainMenu = b.btnMainMenu;
        }
    }

    public override void OnShow()
    {
        AddButtonListener(btnRestart, OnRestart);
        AddButtonListener(btnMainMenu, OnMainMenu);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnGameEnd, OnGameEnd);
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    private void OnGameEnd(object param1, object param2)
    {
        bool isVictory = (bool)param1;
        gameObject.SetActive(true);

        if (txtTitle != null)
            txtTitle.text = isVictory ? "胜利！" : "失败";

        if (txtStats != null)
        {
            var manager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
            var data = manager.GetCampaignData();
            if (data != null)
            {
                txtStats.text = $"最终回合: {data.CurrentTurn}\n" +
                    $"净资产: {data.Player.CalculateNetWorth(data.Market):F0}\n" +
                    $"现金: {data.Player.Cash:F0}\n" +
                    $"审计值: {data.Player.AuditValue:F0}";
            }
        }
    }

    private void OnRestart()
    {
        GameRoot.Instance.stageSystem.ReloadCurrentStage();
    }

    private void OnMainMenu()
    {
        GameRoot.Instance.stageSystem.LoadStage(1);
    }
}
