using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 期货市场 Tab
/// 三行并列结构（ATK/DEF/RET），每行显示价格、保证金、做多/做空按钮、持仓汇总
/// 固定3回合期限
/// </summary>
public class FuturesMarketTab : MonoBehaviour
{
    private const int FIXED_TURNS = 3;

    [Header("ATK 行")]
    [SerializeField] private TMP_Text txtAtkInfo;
    [SerializeField] private Button btnAtkLong;
    [SerializeField] private Button btnAtkShort;
    [SerializeField] private TMP_Text txtAtkPosition;

    [Header("DEF 行")]
    [SerializeField] private TMP_Text txtDefInfo;
    [SerializeField] private Button btnDefLong;
    [SerializeField] private Button btnDefShort;
    [SerializeField] private TMP_Text txtDefPosition;

    [Header("RET 行")]
    [SerializeField] private TMP_Text txtRetInfo;
    [SerializeField] private Button btnRetLong;
    [SerializeField] private Button btnRetShort;
    [SerializeField] private TMP_Text txtRetPosition;

    [Header("持仓列表")]
    [SerializeField] private Transform positionContainer;
    [SerializeField] private GameObject positionItemPrefab;

    [Header("汇总")]
    [SerializeField] private TMP_Text txtTotalMargin;
    [SerializeField] private TMP_Text txtTotalPnL;

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

        BindButtons();
        RefreshUI();
    }

    private void BindButtons()
    {
        if (parentWindow == null) return;

        // ATK 行
        parentWindow.AddButtonListener(btnAtkLong, () => OpenPosition(OrderType.ATK, FuturesDirection.Long));
        parentWindow.AddButtonListener(btnAtkShort, () => OpenPosition(OrderType.ATK, FuturesDirection.Short));

        // DEF 行
        parentWindow.AddButtonListener(btnDefLong, () => OpenPosition(OrderType.DEF, FuturesDirection.Long));
        parentWindow.AddButtonListener(btnDefShort, () => OpenPosition(OrderType.DEF, FuturesDirection.Short));

        // RET 行
        parentWindow.AddButtonListener(btnRetLong, () => OpenPosition(OrderType.RET, FuturesDirection.Long));
        parentWindow.AddButtonListener(btnRetShort, () => OpenPosition(OrderType.RET, FuturesDirection.Short));
    }

    private void OpenPosition(OrderType type, FuturesDirection direction)
    {
        if (gameplayManager == null) return;

        if (gameplayManager.OpenFutures(type, direction, 1, FIXED_TURNS))
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

        RefreshOrderRow(OrderType.ATK, txtAtkInfo, txtAtkPosition, data);
        RefreshOrderRow(OrderType.DEF, txtDefInfo, txtDefPosition, data);
        RefreshOrderRow(OrderType.RET, txtRetInfo, txtRetPosition, data);

        RefreshPositionList(data);
        RefreshSummary(data);
    }

    private void RefreshOrderRow(OrderType type, TMP_Text infoText, TMP_Text positionText, CampaignRuntimeData data)
    {
        float price = data.Market.CurrentPrices[type];
        float marginRate = balanceConfig != null ? balanceConfig.FuturesMarginRate : 0.3f;
        float margin = price * marginRate;

        // 价格和保证金信息
        if (infoText != null)
        {
            infoText.text = $"{type}: 价格 {price:F1} | 保证金 {margin:F1}";
        }

        // 持仓汇总
        if (positionText != null)
        {
            int longCount = 0;
            int shortCount = 0;

            foreach (var contract in data.Player.FuturesPositions)
            {
                if (contract.TargetOrder == type)
                {
                    if (contract.Direction == FuturesDirection.Long)
                        longCount += contract.Quantity;
                    else
                        shortCount += contract.Quantity;
                }
            }

            positionText.text = $"持仓: 多{longCount}/空{shortCount}";
        }
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
