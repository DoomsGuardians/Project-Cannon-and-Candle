using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将军面板：3张己方将军卡片 + 指令分配
/// </summary>
public class GeneralPanel : WindowBase
{
    public class GeneralCard
    {
        public Text txtName;
        public Text txtPersonality;
        public Slider sliderTroops;
        public Slider sliderTrust;
        public Slider sliderMorale;
        public Text txtStatus;
        public Button btnATK;
        public Button btnDEF;
        public Button btnRET;
        public Text txtSkills;
        public Image imgATKHighlight;
        public Image imgDEFHighlight;
        public Image imgRETHighlight;
    }

    private GeneralCard[] generalCards;
    private GameplayManager gameplayManager;
    private GameBalanceConfig balanceConfig;

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);

        var b = gameObject.GetComponent<GeneralPanelBinder>();
        if (b != null && b.generalCards != null)
        {
            generalCards = new GeneralCard[b.generalCards.Length];
            for (int i = 0; i < b.generalCards.Length; i++)
            {
                var src = b.generalCards[i];
                generalCards[i] = new GeneralCard
                {
                    txtName = src.txtName, txtPersonality = src.txtPersonality,
                    sliderTroops = src.sliderTroops, sliderTrust = src.sliderTrust, sliderMorale = src.sliderMorale,
                    txtStatus = src.txtStatus,
                    btnATK = src.btnATK, btnDEF = src.btnDEF, btnRET = src.btnRET,
                    txtSkills = src.txtSkills,
                    imgATKHighlight = src.imgATKHighlight, imgDEFHighlight = src.imgDEFHighlight, imgRETHighlight = src.imgRETHighlight
                };
            }
        }
    }

    public override void OnShow()
    {
        eventService.AddEventListening((EventID)WarBrokerEventID.OnOrderAssigned, OnOrderAssigned);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnStart, OnRefresh);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnRefresh);

        BindButtons();
        RefreshUI();
    }

    public override void OnHide()
    {
        eventService.RemoveEventListeningByTarget(this);
    }

    private void BindButtons()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null || generalCards == null) return;

        for (int i = 0; i < generalCards.Length && i < data.Battle.AllyGenerals.Count; i++)
        {
            var card = generalCards[i];
            var general = data.Battle.AllyGenerals[i];
            string gid = general.GeneralId;

            if (card.btnATK != null) AddButtonListener(card.btnATK, () => AssignOrder(gid, OrderType.ATK));
            if (card.btnDEF != null) AddButtonListener(card.btnDEF, () => AssignOrder(gid, OrderType.DEF));
            if (card.btnRET != null) AddButtonListener(card.btnRET, () => AssignOrder(gid, OrderType.RET));
        }
    }

    private void AssignOrder(string generalId, OrderType order)
    {
        gameplayManager.AssignOrder(generalId, order);
        RefreshUI();
    }

    private void RefreshUI()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null || generalCards == null) return;

        for (int i = 0; i < generalCards.Length && i < data.Battle.AllyGenerals.Count; i++)
        {
            var card = generalCards[i];
            var general = data.Battle.AllyGenerals[i];

            if (card.txtName != null) card.txtName.text = general.Name;
            if (card.txtPersonality != null) card.txtPersonality.text = general.Personality.ToString();
            if (card.sliderTroops != null) { card.sliderTroops.maxValue = 100; card.sliderTroops.value = general.Troops; }
            if (card.sliderTrust != null) { card.sliderTrust.maxValue = 100; card.sliderTrust.value = general.Trust; }
            if (card.sliderMorale != null) { card.sliderMorale.maxValue = 100; card.sliderMorale.value = general.Morale; }
            if (card.txtStatus != null) card.txtStatus.text = general.GetStatus(balanceConfig).ToString();

            bool hasOrder = general.AssignedOrder.HasValue;
            SetHighlight(card.imgATKHighlight, hasOrder && general.AssignedOrder.Value == OrderType.ATK);
            SetHighlight(card.imgDEFHighlight, hasOrder && general.AssignedOrder.Value == OrderType.DEF);
            SetHighlight(card.imgRETHighlight, hasOrder && general.AssignedOrder.Value == OrderType.RET);

            if (card.txtSkills != null)
            {
                card.txtSkills.text = string.Join(", ", general.Skills.ConvertAll(s => s.SkillName));
            }
        }
    }

    private void SetHighlight(Image img, bool active)
    {
        if (img != null) img.enabled = active;
    }

    private void OnOrderAssigned(object p1, object p2) => RefreshUI();
    private void OnRefresh(object p1, object p2) => RefreshUI();
}
