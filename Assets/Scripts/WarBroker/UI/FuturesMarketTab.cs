using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 期货市场 Tab
/// 开仓区、持仓列表、汇总信息
/// </summary>
public class FuturesMarketTab : MonoBehaviour
{
    [Header("开仓区")]
    [SerializeField] private Dropdown ddType;
    [SerializeField] private Dropdown ddDirection;
    [SerializeField] private InputField inputQuantity;
    [SerializeField] private InputField inputTurns;
    [SerializeField] private Text txtMarginPreview;
    [SerializeField] private Button btnOpen;

    [Header("持仓列表")]
    [SerializeField] private Transform positionContainer;
    [SerializeField] private GameObject positionItemPrefab;

    [Header("汇总")]
    [SerializeField] private Text txtTotalMargin;
    [SerializeField] private Text txtTotalPnL;

    private GameplayManager gameplayManager;
    private GameBalanceConfig balanceConfig;
    private WindowBase parentWindow;

    private List<GameObject> spawnedItems = new List<GameObject>();

    public void Initialize(GameplayManager manager, WindowBase parent)
    {
        gameplayManager = manager;
        parentWindow = parent;

        if (GameRoot.Instance != null)
        {
            balanceConfig = GameRoot.Instance.resService.LoadResource<GameBalanceConfig>(ConfigPaths.GAME_BALANCE);
        }

        SetupDropdowns();
        BindEvents();
        RefreshUI();
    }

    private void SetupDropdowns()
    {
        if (ddType != null)
        {
            ddType.ClearOptions();
            ddType.AddOptions(new List<string> { "ATK", "DEF", "RET" });
        }

        if (ddDirection != null)
        {
            ddDirection.ClearOptions();
            ddDirection.AddOptions(new List<string> { "做多 (Long)", "做空 (Short)" });
        }
    }

    private void BindEvents()
    {
        if (parentWindow == null) return;

        parentWindow.AddButtonListener(btnOpen, OnOpenPosition);

        // 输入变化时更新保证金预览
        if (inputQuantity != null)
            inputQuantity.onValueChanged.AddListener(_ => UpdateMarginPreview());
        if (inputTurns != null)
            inputTurns.onValueChanged.AddListener(_ => UpdateMarginPreview());
        if (ddType != null)
            ddType.onValueChanged.AddListener(_ => UpdateMarginPreview());
    }

    private void UpdateMarginPreview()
    {
        if (txtMarginPreview == null || gameplayManager == null) return;

        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        int qty = 1;
        if (inputQuantity != null && int.TryParse(inputQuantity.text, out int q))
            qty = Mathf.Max(1, q);

        var type = (OrderType)(ddType?.value ?? 0);
        float price = data.Market.CurrentPrices[type];
        float marginRate = balanceConfig != null ? balanceConfig.FuturesMarginRate : 0.3f;
        float margin = price * qty * marginRate;

        txtMarginPreview.text = $"预计保证金: {margin:F1}";

        // 检查是否有足够资金
        bool canAfford = data.Player.Cash >= margin;
        txtMarginPreview.color = canAfford ? Color.white : Color.red;
    }

    private void OnOpenPosition()
    {
        if (gameplayManager == null) return;

        var type = (OrderType)(ddType?.value ?? 0);
        var dir = (FuturesDirection)(ddDirection?.value ?? 0);

        int qty = 1;
        if (inputQuantity != null && int.TryParse(inputQuantity.text, out int q))
            qty = Mathf.Max(1, q);

        int turns = 3;
        if (inputTurns != null && int.TryParse(inputTurns.text, out int t))
            turns = Mathf.Max(1, t);

        if (gameplayManager.OpenFutures(type, dir, qty, turns))
        {
            RefreshUI();
        }
    }

    private void OnClosePosition(int contractId)
    {
        if (gameplayManager == null) return;

        if (gameplayManager.CloseFutures(contractId))
        {
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (gameplayManager == null) return;

        var data = gameplayManager.GetCampaignData();
        if (data == null) return;

        UpdateMarginPreview();
        RefreshPositionList(data);
        RefreshSummary(data);
    }

    private void RefreshPositionList(CampaignRuntimeData data)
    {
        ClearSpawnedItems();

        if (positionContainer == null || positionItemPrefab == null) return;

        foreach (var contract in data.Player.FuturesPositions)
        {
            var item = GameObject.Instantiate(positionItemPrefab, positionContainer);
            item.SetActive(true);
            spawnedItems.Add(item);

            SetupPositionItem(item, contract, data);
        }
    }

    private void SetupPositionItem(GameObject item, FuturesContract contract, CampaignRuntimeData data)
    {
        var binder = item.GetComponent<FuturesPositionItemBinder>();
        if (binder == null) return;

        float currentPrice = data.Market.CurrentPrices[contract.TargetOrder];
        float pnl = contract.CalculatePnL(currentPrice);

        // 合约信息
        if (binder.txtContract != null)
        {
            string dirText = contract.Direction == FuturesDirection.Long ? "多" : "空";
            binder.txtContract.text = $"#{contract.ContractId} {contract.TargetOrder} {dirText}×{contract.Quantity}";
        }

        // 开仓价
        if (binder.txtOpenPrice != null)
            binder.txtOpenPrice.text = $"开仓: {contract.OpenPrice:F1}";

        // 当前价
        if (binder.txtCurrentPrice != null)
            binder.txtCurrentPrice.text = $"现价: {currentPrice:F1}";

        // 到期回合
        if (binder.txtExpiration != null)
        {
            int remaining = contract.ExpirationTurn - data.CurrentTurn;
            binder.txtExpiration.text = $"剩余: {remaining} 回合";
        }

        // 保证金
        if (binder.txtMargin != null)
            binder.txtMargin.text = $"保证金: {contract.Margin:F1}";

        // 浮动盈亏
        if (binder.txtPnL != null)
        {
            binder.txtPnL.text = $"盈亏: {pnl:+0.0;-0.0}";
            binder.txtPnL.color = pnl >= 0 ? Color.green : Color.red;
        }

        // 平仓按钮
        if (binder.btnClose != null && parentWindow != null)
        {
            int contractId = contract.ContractId;
            parentWindow.AddButtonListener(binder.btnClose, () => OnClosePosition(contractId));
        }
    }

    private void RefreshSummary(CampaignRuntimeData data)
    {
        float totalMargin = 0f;
        float totalPnL = 0f;

        foreach (var contract in data.Player.FuturesPositions)
        {
            totalMargin += contract.Margin;
            totalPnL += contract.CalculatePnL(data.Market.CurrentPrices[contract.TargetOrder]);
        }

        if (txtTotalMargin != null)
            txtTotalMargin.text = $"总保证金: {totalMargin:F1}";

        if (txtTotalPnL != null)
        {
            txtTotalPnL.text = $"总浮盈: {totalPnL:+0.0;-0.0}";
            txtTotalPnL.color = totalPnL >= 0 ? Color.green : Color.red;
        }
    }

    private void ClearSpawnedItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
                GameObject.Destroy(item);
        }
        spawnedItems.Clear();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (active)
        {
            RefreshUI();
        }
    }

    private void OnDestroy()
    {
        ClearSpawnedItems();
    }
}
