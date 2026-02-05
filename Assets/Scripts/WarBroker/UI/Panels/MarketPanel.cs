using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 市场面板：现货/期货 Tab 页 + 银行借贷
/// </summary>
public class MarketPanel : WindowBase
{
    private const float AMOUNT_STEP = 100f;

    [Header("Tab 切换")]
    private Toggle tabSpot;
    private Toggle tabFutures;
    private GameObject spotContent;
    private GameObject futuresContent;

    [Header("子 Tab")]
    private SpotMarketTab spotTab;
    private FuturesMarketTab futuresTab;

    [Header("银行区域 - 信息")]
    private TMP_Text txtDebt;
    private TMP_Text txtInterest;
    private TMP_Text txtLoanLimit;

    [Header("银行区域 - 金额选择器")]
    private Button btnDecrease;
    private TMP_Text txtAmount;
    private Button btnIncrease;
    private float selectedAmount = 100f;

    [Header("银行区域 - 快捷按钮")]
    private Button btn100;
    private Button btn500;
    private Button btnMax;

    [Header("银行区域 - 操作按钮")]
    private Button btnBorrow;
    private Button btnRepay;

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

            // 银行区域 - 信息
            txtDebt = b.txtDebt;
            txtInterest = b.txtInterest;
            txtLoanLimit = b.txtLoanLimit;

            // 银行区域 - 金额选择器
            btnDecrease = b.btnDecrease;
            txtAmount = b.txtAmount;
            btnIncrease = b.btnIncrease;

            // 银行区域 - 快捷按钮
            btn100 = b.btn100;
            btn500 = b.btn500;
            btnMax = b.btnMax;

            // 银行区域 - 操作按钮
            btnBorrow = b.btnBorrow;
            btnRepay = b.btnRepay;
        }
    }

    public override void OnShow()
    {
        // Tab 切换监听
        if (tabSpot != null)
            AddToggleListener(tabSpot, OnTabChanged);
        if (tabFutures != null)
            AddToggleListener(tabFutures, OnTabChanged);

        // 银行金额选择器按钮
        AddButtonListener(btnDecrease, OnDecrease);
        AddButtonListener(btnIncrease, OnIncrease);

        // 银行快捷按钮
        AddButtonListener(btn100, () => SetAmount(100f));
        AddButtonListener(btn500, () => SetAmount(500f));
        AddButtonListener(btnMax, OnMaxAmount);

        // 银行操作按钮
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

        // 初始化金额
        selectedAmount = 100f;
        RefreshUI();
    }

    public override void OnHide()
    {
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

    #region 银行金额选择器

    private void OnDecrease()
    {
        SetAmount(selectedAmount - AMOUNT_STEP);
    }

    private void OnIncrease()
    {
        SetAmount(selectedAmount + AMOUNT_STEP);
    }

    private void SetAmount(float amount)
    {
        selectedAmount = Mathf.Max(0f, amount);
        RefreshAmountDisplay();
    }

    private void OnMaxAmount()
    {
        var data = gameplayManager?.GetCampaignData();
        if (data == null) return;

        // 默认设为可借上限
        float maxBorrow = marketSystem?.CalculateLoanLimit() ?? 0f;
        SetAmount(maxBorrow);
    }

    private void RefreshAmountDisplay()
    {
        if (txtAmount != null)
            txtAmount.text = $"{selectedAmount:F0}";
    }

    #endregion

    #region 银行操作

    private void OnBorrow()
    {
        if (selectedAmount <= 0) return;
        gameplayManager?.Borrow(selectedAmount);
        RefreshUI();
    }

    private void OnRepay()
    {
        if (selectedAmount <= 0) return;

        var data = gameplayManager?.GetCampaignData();
        if (data == null) return;

        // 还款金额不能超过负债
        float repayAmount = Mathf.Min(selectedAmount, data.Player.BankDebt);
        if (repayAmount > 0)
        {
            gameplayManager.Repay(repayAmount);
            RefreshUI();
        }
    }

    #endregion

    private void RefreshUI()
    {
        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        // 刷新子 Tab
        spotTab?.RefreshUI();
        futuresTab?.RefreshUI();

        // 刷新银行信息
        RefreshBankInfo(data);

        // 刷新金额显示
        RefreshAmountDisplay();
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

    private void SetText(TMP_Text t, string s)
    {
        if (t != null) t.text = s;
    }

    private void OnDataChanged(object p1, object p2) => RefreshUI();
}
