using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 市场面板：现货/期货 Tab 页 + 银行借贷
/// </summary>
public class MarketPanel : WindowBase
{
    [Header("Tab 切换")]
    private Toggle tabSpot;
    private Toggle tabFutures;
    private GameObject spotContent;
    private GameObject futuresContent;

    [Header("子 Tab")]
    private SpotMarketTab spotTab;
    private FuturesMarketTab futuresTab;

    [Header("银行区域")]
    private Text txtDebt;
    private Text txtInterest;
    private Text txtLoanLimit;
    private InputField inputBankAmount;
    private Button btnBorrow;
    private Button btnRepay;

    [Header("玩家信息")]
    private Text txtCash;
    private Text txtNetWorth;

    private GameplayManager gameplayManager;
    private MarketSystem marketSystem;

    public override void OnAwake()
    {
        base.OnAwake();
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();
        marketSystem = GameRoot.Instance.marketSystem;

        var b = gameObject.GetComponent<MarketPanelBinder>();
        if (b != null)
        {
            // Tab 切换
            tabSpot = b.tabSpot;
            tabFutures = b.tabFutures;
            spotContent = b.spotContent;
            futuresContent = b.futuresContent;

            // 子 Tab 组件
            if (spotContent != null)
                spotTab = spotContent.GetComponent<SpotMarketTab>();
            if (futuresContent != null)
                futuresTab = futuresContent.GetComponent<FuturesMarketTab>();

            // 银行区域
            txtDebt = b.txtDebt;
            txtInterest = b.txtInterest;
            txtLoanLimit = b.txtLoanLimit;
            inputBankAmount = b.inputBankAmount;
            btnBorrow = b.btnBorrow;
            btnRepay = b.btnRepay;

            // 玩家信息
            txtCash = b.txtCash;
            txtNetWorth = b.txtNetWorth;

            // 兼容旧版 Binder（如果没有新字段）
            if (tabSpot == null && b.txtAtkPrice != null)
            {
                // 旧版布局，使用兼容模式
                SetupLegacyMode(b);
            }
        }
    }

    /// <summary>兼容旧版 Binder 布局</summary>
    private void SetupLegacyMode(MarketPanelBinder b)
    {
        // 旧版直接使用原有字段
        txtDebt = b.txtDebt;
        txtInterest = b.txtInterest;
        inputBankAmount = b.inputBankAmount;
    }

    public override void OnShow()
    {
        // 获取输入锁，锁定战场相机输入
        InputRouter.Acquire(InputChannel.Gameplay, this);

        // Tab 切换监听
        if (tabSpot != null)
            AddToggleListener(tabSpot, OnTabChanged);
        if (tabFutures != null)
            AddToggleListener(tabFutures, OnTabChanged);

        // 银行按钮
        AddButtonListener(btnBorrow, OnBorrow);
        AddButtonListener(btnRepay, OnRepay);

        // 初始化子 Tab
        spotTab?.Initialize(gameplayManager, this);
        futuresTab?.Initialize(gameplayManager, this);

        // 事件监听
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnDataChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnDataChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnCashChange, OnDataChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnFuturesOpened, OnDataChanged);
        eventService.AddEventListening((EventID)WarBrokerEventID.OnFuturesClosed, OnDataChanged);

        // 默认显示现货 Tab
        SetActiveTab(true);
        RefreshUI();
    }

    public override void OnHide()
    {
        // 释放输入锁
        InputRouter.Release(InputChannel.Gameplay, this);

        eventService.RemoveEventListeningByTarget(this);
    }

    private void OnTabChanged(Toggle toggle, bool isOn)
    {
        if (!isOn) return;

        bool isSpot = toggle == tabSpot;
        SetActiveTab(isSpot);
    }

    private void SetActiveTab(bool isSpot)
    {
        if (spotContent != null)
            spotContent.SetActive(isSpot);
        if (futuresContent != null)
            futuresContent.SetActive(!isSpot);

        spotTab?.SetActive(isSpot);
        futuresTab?.SetActive(!isSpot);
    }

    private void OnBorrow()
    {
        if (inputBankAmount == null) return;
        if (float.TryParse(inputBankAmount.text, out float amount))
        {
            gameplayManager.Borrow(amount);
            RefreshUI();
        }
    }

    private void OnRepay()
    {
        if (inputBankAmount == null) return;
        if (float.TryParse(inputBankAmount.text, out float amount))
        {
            gameplayManager.Repay(amount);
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        // 刷新子 Tab
        spotTab?.RefreshUI();
        futuresTab?.RefreshUI();

        // 刷新银行信息
        RefreshBankInfo(data);

        // 刷新玩家信息
        RefreshPlayerInfo(data);
    }

    private void RefreshBankInfo(CampaignRuntimeData data)
    {
        var balanceConfig = resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);

        SetText(txtDebt, $"负债: {data.Player.BankDebt:F0}");
        SetText(txtInterest, $"利率: {(balanceConfig != null ? balanceConfig.BankInterestRate : 0f):P1}");

        if (txtLoanLimit != null && marketSystem != null)
        {
            float limit = marketSystem.CalculateLoanLimit();
            txtLoanLimit.text = $"可借: {limit:F0}";
        }
    }

    private void RefreshPlayerInfo(CampaignRuntimeData data)
    {
        SetText(txtCash, $"现金: {data.Player.Cash:F0}");

        if (txtNetWorth != null)
        {
            float netWorth = data.Player.CalculateNetWorth(data.Market);
            txtNetWorth.text = $"净值: {netWorth:F0}";
        }
    }

    private void SetText(Text t, string s)
    {
        if (t != null) t.text = s;
    }

    private void OnDataChanged(object p1, object p2) => RefreshUI();
}
