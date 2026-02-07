using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
/// Victor 信件弹窗
/// 显示对手 Victor 本回合的行动暗示
/// 每 3 回合触发一次（与指令补给周期相同）
/// 在对手阶段横幅完成后显示
/// 玩家点击确认按钮后结束对手回合
/// </summary>
public class VictorLetterPopup : WindowBase
{
    public new UILayer uiLayer = UILayer.Top;
    public new bool IsFullScreen = false;

    /// <summary>信件触发周期（每N回合一次）</summary>
    private const int LETTER_CYCLE = 3;

    private Image imgPortrait;
    private TMP_Text txtTitle;
    private TMP_Text txtContent;
    private Button btnConfirm;
    private TMP_Text txtBtnConfirm;

    private LetterDialogueConfig config;
    private VictorTurnVisualData currentData;

    // 待显示的信件数据（在 OnVictorTurnStart 时缓存，等待横幅完成后显示）
    private VictorTurnVisualData pendingLetterData;

    // 上次显示信件的回合
    private int lastLetterTurn = 0;

    // 音效
    private AudioClip sfxEnter;

    public override void OnAwake()
    {
        base.OnAwake();

        // 重置状态（修复重新进入战役时的状态残留问题）
        lastLetterTurn = 0;
        pendingLetterData = null;
        currentData = null;

        // 加载配置
        config = resService.LoadResource<LetterDialogueConfig>(ConfigPaths.LETTER_DIALOGUE);

        var binder = gameObject.GetComponent<VictorLetterPopupBinder>();
        if (binder != null)
        {
            imgPortrait = binder.imgPortrait;
            txtTitle = binder.txtTitle;
            txtContent = binder.txtContent;
            btnConfirm = binder.btnConfirm;
            txtBtnConfirm = binder.txtBtnConfirm;
            sfxEnter = binder.sfxEnter;
        }

        // 监听 Victor 回合开始事件（缓存数据）
        eventService.AddEventListening(
            (EventID)(int)WarBrokerEventID.OnVictorTurnStart,
            OnVictorTurnStart);

        // 监听对手阶段横幅完成事件（显示信件）
        eventService.AddEventListening(
            (EventID)(int)WarBrokerEventID.OnPhaseBannerComplete,
            OnPhaseBannerComplete);
    }

    public override void OnShow()
    {
        // 播放入场音效
        if (sfxEnter != null)
        {
            audioService?.PlaySFX(sfxEnter);
        }

        // 获取输入锁
        InputRouter.Acquire(InputChannel.Gameplay, this);

        AddButtonListener(btnConfirm, OnConfirm);
    }

    public override void OnHide()
    {
        // 释放输入锁
        InputRouter.Release(InputChannel.Gameplay, this);
    }

    // 注意：事件监听会在 WindowBase.OnDestroy() 中通过
    // eventService.RemoveEventListeningByTarget(this) 自动清理

    /// <summary>
    /// 监听 Victor 回合开始事件
    /// 缓存数据，等待横幅完成后再显示
    /// </summary>
    private void OnVictorTurnStart(object param1, object param2)
    {
        pendingLetterData = null;

        if (param1 is VictorTurnVisualData data)
        {
            int currentTurn = data.Turn;

            // 判断是否满足3回合周期
            if (ShouldShowLetter(currentTurn))
            {
                // 缓存数据，等待横幅完成后显示
                pendingLetterData = data;
                Debug.Log($"[VictorLetterPopup] 信件已缓存，等待对手阶段横幅完成后显示 (Turn {currentTurn})");
            }
            else
            {
                // 不是信件回合，直接发送完成事件
                Debug.Log($"[VictorLetterPopup] 跳过信件 (Turn {currentTurn}，下次在 Turn {GetNextLetterTurn(currentTurn)})");
                SendCompletionEvent();
            }
        }
        else
        {
            Debug.LogWarning("[VictorLetterPopup] Invalid data received for OnVictorTurnStart.");
            SendCompletionEvent();
        }
    }

    /// <summary>
    /// 监听横幅完成事件
    /// 当对手阶段横幅完成时，显示信件
    /// </summary>
    private void OnPhaseBannerComplete(object param1, object param2)
    {
        // 检查是否是对手阶段（IntentPhase）
        if (param1 is TurnPhase phase && phase == TurnPhase.IntentPhase)
        {
            if (pendingLetterData != null)
            {
                // 显示信件
                SetLetterData(pendingLetterData);
                lastLetterTurn = pendingLetterData.Turn;
                pendingLetterData = null;

                uIService.ShowWindow<VictorLetterPopup>(Name);
            }
        }
    }

    /// <summary>
    /// 判断是否应该显示信件（3回合周期）
    /// </summary>
    private bool ShouldShowLetter(int currentTurn)
    {
        // 第一回合总是显示
        if (lastLetterTurn == 0)
            return true;

        // 每 3 回合显示一次
        return currentTurn - lastLetterTurn >= LETTER_CYCLE;
    }

    /// <summary>
    /// 获取下一次信件回合
    /// </summary>
    private int GetNextLetterTurn(int currentTurn)
    {
        if (lastLetterTurn == 0)
            return currentTurn;

        return lastLetterTurn + LETTER_CYCLE;
    }

    /// <summary>
    /// 设置信件数据
    /// </summary>
    public void SetLetterData(VictorTurnVisualData data)
    {
        currentData = data;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (currentData == null || config == null)
        {
            if (txtTitle != null)
                txtTitle.text = "来自对手的信件";
            if (txtContent != null)
                txtContent.text = "......";
            if (imgPortrait != null)
                imgPortrait.gameObject.SetActive(false);
            return;
        }

        // 设置立绘（根据策略显示不同表情）
        if (imgPortrait != null)
        {
            Sprite portrait = config.GetPortraitForStrategy(currentData.Strategy);
            if (portrait != null)
            {
                imgPortrait.sprite = portrait;
                imgPortrait.gameObject.SetActive(true);
            }
            else
            {
                imgPortrait.gameObject.SetActive(false);
            }
        }

        // 设置标题
        if (txtTitle != null)
        {
            txtTitle.text = $"来自 {config.DisplayName} 的信件";
        }

        // 生成内容
        if (txtContent != null)
        {
            txtContent.text = GenerateLetterContent();
        }

        // 更新按钮文本（华伦斯坦的回应）
        if (txtBtnConfirm != null)
        {
            txtBtnConfirm.text = config.GetWallensteinResponse(currentData.Strategy);
        }
    }

    /// <summary>
    /// 生成信件内容文本
    /// </summary>
    private string GenerateLetterContent()
    {
        if (currentData == null || config == null)
            return "......";

        var sb = new StringBuilder();

        // 信件开头：称呼
        sb.AppendLine($"{config.RecipientTitle}：");
        sb.AppendLine();

        // 策略开场白
        string opening = config.GetStrategyOpening(currentData.Strategy);
        sb.AppendLine($"    {opening}");
        sb.AppendLine();

        // 根据动作生成暗示
        int hintCount = 0;
        int maxHints = 3;

        foreach (var action in currentData.Actions)
        {
            if (hintCount >= maxHints) break;

            string hint = GenerateActionHint(action);
            if (!string.IsNullOrEmpty(hint))
            {
                sb.AppendLine($"    {hint}");
                hintCount++;
            }
        }

        // 如果没有有效动作暗示，添加默认对话
        if (hintCount == 0)
        {
            sb.AppendLine("    此番并无特别之事相告。");
        }

        sb.AppendLine();

        // 结束语
        string closing = config.GetClosingRemark();
        if (!string.IsNullOrEmpty(closing))
        {
            sb.AppendLine($"    {closing}");
        }

        sb.AppendLine();

        // 落款
        sb.AppendLine($"<align=right>{config.FullTitle}</align>");

        return sb.ToString();
    }

    /// <summary>
    /// 为单个动作生成暗示文本
    /// </summary>
    private string GenerateActionHint(VictorActionSummary action)
    {
        string hint = config.GetActionHint(action.ActionType, action.OrderType, action.TargetName);

        if (string.IsNullOrEmpty(hint))
            return null;

        // 对于有数量意义的动作，添加规模修饰语
        if (ShouldAddScaleModifier(action.ActionType) && action.Scale != ActionScale.Small)
        {
            string modifier = config.GetScaleModifier(action.Scale);
            if (!string.IsNullOrEmpty(modifier))
            {
                hint = $"{modifier}...{hint}";
            }
        }

        return hint;
    }

    /// <summary>
    /// 判断动作是否需要添加规模修饰语
    /// </summary>
    private bool ShouldAddScaleModifier(VictorActionType actionType)
    {
        switch (actionType)
        {
            case VictorActionType.SpotBuy:
            case VictorActionType.SpotSell:
            case VictorActionType.FuturesLong:
            case VictorActionType.FuturesShort:
            case VictorActionType.FuturesClose:
            case VictorActionType.Borrow:
            case VictorActionType.Repay:
                return true;
            default:
                return false;
        }
    }

    private void OnConfirm()
    {
        uIService.HideWindow(Name);
        SendCompletionEvent();
    }

    private void SendCompletionEvent()
    {
        eventService?.SendMessage(
            (EventID)(int)WarBrokerEventID.OnVictorTurnComplete,
            null, null);
    }
}
