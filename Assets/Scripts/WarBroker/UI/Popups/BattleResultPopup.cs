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

    private List<BattleResult> results;
    private List<GameObject> spawnedItems = new List<GameObject>();

    public override void OnAwake()
    {
        base.OnAwake();

        var binder = gameObject.GetComponent<BattleResultPopupBinder>();
        if (binder != null)
        {
            txtTitle = binder.txtTitle;
            resultContainer = binder.resultContainer;
            resultItemPrefab = binder.resultItemPrefab;
            btnConfirm = binder.btnConfirm;
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
    }

    /// <summary>设置战斗结果数据</summary>
    public void SetBattleResults(List<BattleResult> battleResults)
    {
        results = battleResults;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (results == null || results.Count == 0)
        {
            if (txtTitle != null)
                txtTitle.text = "本回合无战斗";
            return;
        }

        if (txtTitle != null)
            txtTitle.text = $"战斗结算 - {results.Count} 条战线";

        ClearSpawnedItems();
        SpawnResultItems();
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

        // 战线移动
        if (binder.txtLineMove != null)
        {
            string moveText = result.LineMovement > 0 ? $"推进 +{result.LineMovement}"
                            : result.LineMovement < 0 ? $"后退 {result.LineMovement}"
                            : "僵持";
            binder.txtLineMove.text = moveText;
        }

        // 兵力变化
        if (binder.txtTroopChange != null)
        {
            string allyChange = result.AllyTroopChange >= 0 ? $"+{result.AllyTroopChange}" : $"{result.AllyTroopChange}";
            string enemyChange = result.EnemyTroopChange >= 0 ? $"+{result.EnemyTroopChange}" : $"{result.EnemyTroopChange}";
            binder.txtTroopChange.text = $"我军: {allyChange} | 敌军: {enemyChange}";
        }

        // 技能触发
        if (binder.txtSkill != null)
        {
            binder.txtSkill.gameObject.SetActive(result.SkillTriggered);
            if (result.SkillTriggered)
                binder.txtSkill.text = $"技能触发: {result.SkillName}";
        }

        // 暴击/失误
        if (binder.txtSpecial != null)
        {
            if (result.WasCrit)
            {
                binder.txtSpecial.gameObject.SetActive(true);
                binder.txtSpecial.text = "暴击!";
                binder.txtSpecial.color = Color.yellow;
            }
            else if (result.WasFumble)
            {
                binder.txtSpecial.gameObject.SetActive(true);
                binder.txtSpecial.text = "失误!";
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
        return position switch
        {
            FrontlinePosition.Left => "左翼",
            FrontlinePosition.Center => "中军",
            FrontlinePosition.Right => "右翼",
            _ => position.ToString()
        };
    }

    private void OnConfirm()
    {
        uIService.HideWindow(Name);
    }
}
