using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗结算弹窗
/// 显示本回合所有战线的战斗结果
/// </summary>
public class BattleResultPopup : WindowBase
{
    public new UILayer uiLayer = UILayer.Top;
    public new bool IsFullScreen = false;

    private TMP_Text txtTitle;
    private Transform resultContainer;
    private GameObject resultItemPrefab;
    private Button btnConfirm;
    private TMP_Text txtBtnConfirm;
    private Image imgIllustration;

    private List<BattleResult> results;
    private List<GameObject> spawnedItems = new List<GameObject>();

    // 关闭回调
    private System.Action onCloseCallback;

    // UI文本配置
    private UITextConfig textConfig;
    private GameplayManager gameplayManager;

    public override void OnAwake()
    {
        base.OnAwake();

        // 加载文本配置
        textConfig = resService.LoadResource<UITextConfig>(ConfigPaths.UI_TEXT);
        gameplayManager = GameRoot.Instance.managerService.GetManager<GameplayManager>();

        var binder = gameObject.GetComponent<BattleResultPopupBinder>();
        if (binder != null)
        {
            txtTitle = binder.txtTitle;
            resultContainer = binder.resultContainer;
            resultItemPrefab = binder.resultItemPrefab;
            btnConfirm = binder.btnConfirm;
            txtBtnConfirm = binder.txtBtnConfirm;
            imgIllustration = binder.imgIllustration;
        }
    }

    public override void OnShow()
    {
        // 获取输入锁
        InputRouter.Acquire(InputChannel.Gameplay, this);

        AddButtonListener(btnConfirm, OnConfirm);

        RefreshUI();
    }

    public override void OnHide()
    {
        // 释放输入锁
        InputRouter.Release(InputChannel.Gameplay, this);

        ClearSpawnedItems();
        eventService.RemoveEventListeningByTarget(this);

        // 执行关闭回调
        if (onCloseCallback != null)
        {
            var callback = onCloseCallback;
            onCloseCallback = null; // 清除回调，避免重复调用
            callback.Invoke();
        }
    }

    /// <summary>设置战斗结果数据</summary>
    public void SetBattleResults(List<BattleResult> battleResults)
    {
        results = battleResults;
        UpdateIllustrationFromResults();
        RefreshUI();
    }

    /// <summary>设置关闭回调（弹窗关闭后执行）</summary>
    public void SetOnCloseCallback(System.Action callback)
    {
        onCloseCallback = callback;
    }

    /// <summary>设置插图（如果配置了插图则显示，否则保持默认）</summary>
    public void SetIllustration(Sprite illustration)
    {
        if (imgIllustration != null && illustration != null)
        {
            imgIllustration.sprite = illustration;
        }
    }

    /// <summary>根据战斗结果自动选择插图</summary>
    private void UpdateIllustrationFromResults()
    {
        if (results == null || results.Count == 0 || textConfig == null || imgIllustration == null)
            return;

        // 选择最激烈的战斗（伤亡最大的）来显示插图
        BattleResult selectedResult = null;
        int maxCasualties = 0;

        foreach (var result in results)
        {
            int casualties = Mathf.Abs(result.AllyTroopChange) + Mathf.Abs(result.EnemyTroopChange);
            if (casualties > maxCasualties)
            {
                maxCasualties = casualties;
                selectedResult = result;
            }
        }

        // 如果没有伤亡，使用第一个战斗结果
        if (selectedResult == null && results.Count > 0)
        {
            selectedResult = results[0];
        }

        if (selectedResult != null)
        {
            var sprite = textConfig.GetBattleImage(selectedResult.AllyOrder, selectedResult.EnemyOrder);
            if (sprite != null)
            {
                imgIllustration.sprite = sprite;
            }
        }
    }

    private void RefreshUI()
    {
        int currentTurn = gameplayManager?.GetCampaignData()?.CurrentTurn ?? 1;

        if (txtTitle != null)
            txtTitle.text = string.Format(textConfig?.BattleResultTitle ?? "第{0}周 战斗报告", currentTurn);

        if (results == null || results.Count == 0)
        {
            UpdateConfirmButtonText(false, false, true);
            return;
        }

        ClearSpawnedItems();
        SpawnResultItems();

        // 根据战斗结果更新确认按钮文本
        UpdateConfirmButtonFromResults();
    }

    private void UpdateConfirmButtonFromResults()
    {
        if (results == null || results.Count == 0)
        {
            UpdateConfirmButtonText(false, false, true);
            return;
        }

        bool hasAllyCapture = false;
        bool hasEnemyCapture = false;

        foreach (var result in results)
        {
            if (result.AllyCapturedGrid.HasValue)
                hasAllyCapture = true;
            if (result.EnemyCapturedGrid.HasValue)
                hasEnemyCapture = true;
        }

        bool isStalemate = !hasAllyCapture && !hasEnemyCapture;
        UpdateConfirmButtonText(hasAllyCapture, hasEnemyCapture, isStalemate);
    }

    private void UpdateConfirmButtonText(bool hasAllyCapture, bool hasEnemyCapture, bool isStalemate)
    {
        if (txtBtnConfirm == null) return;

        if (textConfig != null)
        {
            txtBtnConfirm.text = textConfig.GetBattleConfirmButtonText(hasAllyCapture, hasEnemyCapture, isStalemate);
        }
        else
        {
            txtBtnConfirm.text = "确认";
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

    private void SpawnResultItems()
    {
        if (resultContainer == null || resultItemPrefab == null || results == null)
            return;

        foreach (var result in results)
        {
            var item = GameObject.Instantiate(resultItemPrefab, resultContainer);
            item.SetActive(true);
            spawnedItems.Add(item);

            SetupResultItem(item, result);
        }
    }

    private void SetupResultItem(GameObject item, BattleResult result)
    {
        var binder = item.GetComponent<BattleResultItemBinder>();
        if (binder == null) return;

        // 战线名称
        if (binder.txtPosition != null)
            binder.txtPosition.text = GetPositionName(result.Position);

        // 指令对决
        if (binder.txtOrders != null)
            binder.txtOrders.text = $"{result.AllyOrder} vs {result.EnemyOrder}";

        // 战线移动 / 领土占领
        if (binder.txtLineMove != null)
        {
            string moveText;
            if (result.AllyCapturedGrid.HasValue && result.EnemyCapturedGrid.HasValue)
            {
                moveText = textConfig?.BattleBothEngaged ?? "双方交战";
            }
            else if (result.AllyCapturedGrid.HasValue)
            {
                moveText = string.Format(textConfig?.BattleAllyCaptured ?? "占领格{0}", result.AllyCapturedGrid.Value);
            }
            else if (result.EnemyCapturedGrid.HasValue)
            {
                moveText = string.Format(textConfig?.BattleEnemyCaptured ?? "敌占格{0}", result.EnemyCapturedGrid.Value);
            }
            else
            {
                moveText = textConfig?.BattleStalemate ?? "僵持";
            }
            binder.txtLineMove.text = moveText;
        }

        // 兵力变化
        if (binder.txtTroopChange != null)
        {
            string allyChange = result.AllyTroopChange >= 0 ? $"+{result.AllyTroopChange}" : $"{result.AllyTroopChange}";
            string enemyChange = result.EnemyTroopChange >= 0 ? $"+{result.EnemyTroopChange}" : $"{result.EnemyTroopChange}";
            binder.txtTroopChange.text = $"我军: {allyChange} | 敌军: {enemyChange}";
        }

        // 暴击/失误
        if (binder.txtSpecial != null)
        {
            if (result.WasCrit)
            {
                binder.txtSpecial.gameObject.SetActive(true);
                binder.txtSpecial.text = textConfig?.BattleCriticalHit ?? "暴击!";
                binder.txtSpecial.color = Color.yellow;
            }
            else if (result.WasFumble)
            {
                binder.txtSpecial.gameObject.SetActive(true);
                binder.txtSpecial.text = textConfig?.BattleFumble ?? "失误!";
                binder.txtSpecial.color = Color.red;
            }
            else
            {
                binder.txtSpecial.gameObject.SetActive(false);
            }
        }

        // 描述
        if (binder.txtDescription != null)
        {
            binder.txtDescription.gameObject.SetActive(!string.IsNullOrEmpty(result.Description));
            binder.txtDescription.text = result.Description;
        }
    }

    private string GetPositionName(FrontlinePosition position)
    {
        int laneCount = gameplayManager?.GetCampaignData()?.Battle?.Frontlines?.Count ?? 3;
        return position.ToDisplayName(laneCount);
    }

    private void OnConfirm()
    {
        uIService.HideWindow(Name);
    }
}
