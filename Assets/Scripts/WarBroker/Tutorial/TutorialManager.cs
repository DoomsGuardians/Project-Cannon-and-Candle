using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if NANINOVEL
using Naninovel;
using Naninovel.Async;
#endif

/// <summary>
/// 教程管理器
/// 控制教程流程、播放对话、监听触发事件
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TutorialConfig config;
    [SerializeField] private GameObject highlightOverlay;  // 高亮遮罩Prefab（可选）

    [Header("开发选项")]
    [Tooltip("忽略教程完成状态，每次都启动教程（仅用于开发测试）")]
    [SerializeField] private bool ignoreCompletedStatus = false;

    private int currentStepIndex = -1;
    private bool isRunning = false;
    private bool waitingForTrigger = false;
    private bool waitingForPhaseBanner = false;

    private EventService eventService;
    private ResService resService;
    private UIService uiService;

    // 按钮禁用状态管理
    private HashSet<string> currentlyDisabledButtons = new HashSet<string>();

#if NANINOVEL
    private NaninovelService naninovelService;
#endif

    // 单例
    public static TutorialManager Instance { get; private set; }

    /// <summary>教程是否正在运行</summary>
    public bool IsRunning => isRunning;

    /// <summary>当前步骤索引</summary>
    public int CurrentStepIndex => currentStepIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        eventService = GameRoot.Instance?.eventService;
        resService = GameRoot.Instance?.resService;
        uiService = GameRoot.Instance?.uIService;

#if NANINOVEL
        naninovelService = GameRoot.Instance?.naninovelService;
        Debug.Log($"[TutorialManager] NANINOVEL is defined, naninovelService = {(naninovelService != null ? "OK" : "NULL")}");
#else
        Debug.LogWarning("[TutorialManager] NANINOVEL is NOT defined!");
#endif

        // 如果没有指定配置，尝试加载默认配置
        if (config == null)
        {
            config = resService?.LoadResource<TutorialConfig>(ConfigPaths.TUTORIAL_CONFIG);
        }
    }

    private void OnEnable()
    {
        // 监听游戏事件用于触发检测
        eventService?.AddEventListening((EventID)WarBrokerEventID.OnTradeExecuted, OnTradeExecuted);
        eventService?.AddEventListening((EventID)WarBrokerEventID.OnOrderAssigned, OnOrderAssigned);
        eventService?.AddEventListening((EventID)WarBrokerEventID.OnIntentChanged, OnIntentChanged);
        eventService?.AddEventListening((EventID)WarBrokerEventID.OnTurnEnd, OnTurnEnd);
        eventService?.AddEventListening((EventID)WarBrokerEventID.OnPhaseChange, OnPhaseChange);
        eventService?.AddEventListening((EventID)WarBrokerEventID.OnPhaseBannerComplete, OnPhaseBannerComplete);
    }

    private void OnDisable()
    {
        eventService?.RemoveEventListeningByTarget(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>设置教程配置</summary>
    public void SetConfig(TutorialConfig tutorialConfig)
    {
        config = tutorialConfig;
    }

    /// <summary>开始教程</summary>
    public void StartTutorial()
    {
        if (isRunning) return;

        if (config == null)
        {
            Debug.LogError("[TutorialManager] TutorialConfig is null, cannot start tutorial.");
            return;
        }

        if (config.Steps == null || config.Steps.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] No tutorial steps configured.");
            return;
        }

        Debug.Log("[TutorialManager] Starting tutorial...");
        currentStepIndex = -1;
        isRunning = true;
        NextStep();
    }

    /// <summary>跳过教程</summary>
    public void SkipTutorial()
    {
        if (!isRunning) return;

        Debug.Log("[TutorialManager] Tutorial skipped.");
        isRunning = false;
        waitingForTrigger = false;
        waitingForPhaseBanner = false;
        HideHighlight();

        // 标记教程已完成
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // 发送教程完成事件
        eventService?.SendMessage((EventID)WarBrokerEventID.OnTutorialComplete, false, null);
    }

    /// <summary>检查教程是否已完成</summary>
    public static bool IsTutorialCompleted()
    {
        // 如果实例存在且设置了忽略完成状态，则返回 false（允许教程启动）
        if (Instance != null && Instance.ignoreCompletedStatus)
        {
            return false;
        }
        return PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
    }

    /// <summary>重置教程完成状态（用于调试）</summary>
    public static void ResetTutorialProgress()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 0);
        PlayerPrefs.Save();
        Debug.Log("[TutorialManager] Tutorial progress reset.");
    }

    /// <summary>进入下一步</summary>
    private void NextStep()
    {
        if (!isRunning) return;

        currentStepIndex++;

        if (currentStepIndex >= config.Steps.Count)
        {
            // 教程完成
            CompleteTutorial();
            return;
        }

        var step = config.Steps[currentStepIndex];
        StartCoroutine(ExecuteStep(step));
    }

    /// <summary>执行单个步骤</summary>
    private IEnumerator ExecuteStep(TutorialStep step)
    {
        Debug.Log($"[TutorialManager] Executing step: {step.StepId}");

        // 1. 应用按钮禁用设置
        ApplyButtonRestrictions(step);

        // 2. 显示高亮（如果有）
        if (!string.IsNullOrEmpty(step.HighlightTarget))
        {
            ShowHighlight(step.HighlightTarget);
        }

        // 3. 播放 Naninovel 对话
        if (!string.IsNullOrEmpty(step.ScriptName))
        {
            yield return PlayDialogue(step.ScriptName, step.StartLabel);
        }

        // 4. welcome 步骤完成后，通知游戏开始回合，并等待 PhaseBanner 和弹窗
        if (step.StepId == "welcome")
        {
            Debug.Log("[TutorialManager] Welcome dialogue complete, starting turn and waiting for PhaseBanner...");
            eventService?.SendMessage((EventID)WarBrokerEventID.OnTutorialWelcomeComplete, null, null);

            // 等待 MarketPhase 的 PhaseBanner 完成
            waitingForPhaseBanner = true;
            while (waitingForPhaseBanner && isRunning)
            {
                yield return null;
            }

            // 等待弹窗关闭
            yield return WaitForPopupsClosed();

            Debug.Log("[TutorialManager] Welcome step complete, PhaseBanner and popups done");
        }
        // 5. 其他步骤：等待触发条件
        else if (step.Trigger != TutorialTrigger.Immediate)
        {
            waitingForTrigger = true;
            Debug.Log($"[TutorialManager] Waiting for trigger: {step.Trigger} ({step.TriggerParameter})");

            // 等待触发
            while (waitingForTrigger && isRunning)
            {
                yield return null;
            }
        }

        // 6. 隐藏高亮
        HideHighlight();

        // 7. 恢复按钮状态
        RestoreButtonStates();

        // 8. 进入下一步
        if (isRunning)
        {
            NextStep();
        }
    }

    /// <summary>播放 Naninovel 对话</summary>
    private IEnumerator PlayDialogue(string scriptName, string startLabel)
    {
#if NANINOVEL
        Debug.Log($"[TutorialManager] PlayDialogue called: script={scriptName}, label={startLabel}");

        if (naninovelService == null)
        {
            Debug.LogError("[TutorialManager] NaninovelService is NULL!");
            yield break;
        }

        // 等待 NaninovelService 初始化
        if (!naninovelService.IsInitialized)
        {
            Debug.Log("[TutorialManager] Waiting for NaninovelService to initialize...");
            var initTask = naninovelService.WaitForInitializationAsync();

            while (!initTask.Status.IsCompleted())
            {
                yield return null;
            }

            if (naninovelService.InitializationFailed)
            {
                Debug.LogError("[TutorialManager] NaninovelService initialization failed!");
                yield break;
            }

            Debug.Log("[TutorialManager] NaninovelService initialized");
        }

        Debug.Log($"[TutorialManager] Playing script: {scriptName}" +
                  (string.IsNullOrEmpty(startLabel) ? "" : $" @ {startLabel}"));

        // 使用 NaninovelService 播放脚本
        // pauseGameplayInput: true 确保对话期间阻止游戏交互
        var playTask = naninovelService.PlayScriptAsync(
            scriptName,
            startLabel,
            waitForCompletion: true,
            pauseGameplayInput: true,
            stopCurrent: true
        );

        // 等待播放完成
        while (!playTask.Status.IsCompleted())
        {
            yield return null;
        }

        Debug.Log($"[TutorialManager] Script completed: {scriptName}");
#else
        Debug.Log($"[TutorialManager] (Naninovel disabled) Script: {scriptName}");
        yield return new WaitForSeconds(1f);
#endif
    }

    /// <summary>教程完成</summary>
    private void CompleteTutorial()
    {
        isRunning = false;
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log("[TutorialManager] Tutorial completed!");

        eventService?.SendMessage((EventID)WarBrokerEventID.OnTutorialComplete, true, null);
    }

    #region 触发检测

    private TutorialStep CurrentStep =>
        currentStepIndex >= 0 && currentStepIndex < config.Steps.Count
            ? config.Steps[currentStepIndex]
            : null;

    private void OnTradeExecuted(object data, object unused)
    {
        if (!waitingForTrigger) return;
        var step = CurrentStep;
        if (step == null) return;

        if (data is TransactionRecord record)
        {
            bool match = false;

            if (step.Trigger == TutorialTrigger.WaitForBuy &&
                record.Type == TransactionRecord.TransactionType.Buy)
            {
                match = string.IsNullOrEmpty(step.TriggerParameter) ||
                        record.OrderType?.ToString() == step.TriggerParameter;
            }
            else if (step.Trigger == TutorialTrigger.WaitForSell &&
                     record.Type == TransactionRecord.TransactionType.Sell)
            {
                match = string.IsNullOrEmpty(step.TriggerParameter) ||
                        record.OrderType?.ToString() == step.TriggerParameter;
            }

            if (match)
            {
                Debug.Log($"[TutorialManager] Trigger matched: {step.Trigger}");
                waitingForTrigger = false;
            }
        }
    }

    private void OnOrderAssigned(object generalId, object order)
    {
        Debug.Log($"[TutorialManager] OnOrderAssigned received: generalId={generalId}, order={order}, waitingForTrigger={waitingForTrigger}, currentStep={CurrentStep?.StepId}");

        if (!waitingForTrigger) return;
        var step = CurrentStep;

        if (step?.Trigger == TutorialTrigger.WaitForAssign)
        {
            Debug.Log($"[TutorialManager] Trigger matched: WaitForAssign");
            waitingForTrigger = false;
        }
    }

    private void OnIntentChanged(object generalId, object unused)
    {
        Debug.Log($"[TutorialManager] OnIntentChanged received: generalId={generalId}, waitingForTrigger={waitingForTrigger}, currentStep={CurrentStep?.StepId}");

        if (!waitingForTrigger) return;
        var step = CurrentStep;

        // 强化/篡改意图也算作分配指令
        if (step?.Trigger == TutorialTrigger.WaitForAssign)
        {
            Debug.Log($"[TutorialManager] Trigger matched: WaitForAssign (via IntentChanged)");
            waitingForTrigger = false;
        }
    }

    private void OnTurnEnd(object p1, object p2)
    {
        if (!waitingForTrigger) return;
        var step = CurrentStep;

        if (step?.Trigger == TutorialTrigger.WaitForEndTurn)
        {
            Debug.Log($"[TutorialManager] Trigger matched: WaitForEndTurn");
            waitingForTrigger = false;
        }
    }

    private void OnPhaseChange(object phase, object unused)
    {
        if (!waitingForTrigger) return;
        var step = CurrentStep;

        if (step?.Trigger == TutorialTrigger.WaitForPhase &&
            phase?.ToString() == step.TriggerParameter)
        {
            Debug.Log($"[TutorialManager] Trigger matched: WaitForPhase ({step.TriggerParameter})");
            waitingForTrigger = false;
        }
    }

    private void OnPhaseBannerComplete(object phase, object unused)
    {
        // welcome 步骤等待 MarketPhase 的 PhaseBanner
        if (waitingForPhaseBanner && phase?.ToString() == "MarketPhase")
        {
            Debug.Log("[TutorialManager] MarketPhase PhaseBanner complete");
            waitingForPhaseBanner = false;
            return;
        }

        // 其他步骤的 WaitForPhaseBanner 触发
        if (!waitingForTrigger) return;
        var step = CurrentStep;

        if (step?.Trigger == TutorialTrigger.WaitForPhaseBanner)
        {
            if (string.IsNullOrEmpty(step.TriggerParameter) ||
                phase?.ToString() == step.TriggerParameter)
            {
                Debug.Log($"[TutorialManager] PhaseBanner complete: {phase}, waiting for popups...");
                StartCoroutine(WaitForPopupsAndContinue());
            }
        }
    }

    /// <summary>等待弹窗关闭（用于协程）</summary>
    private IEnumerator WaitForPopupsClosed()
    {
        var uiService = GameRoot.Instance?.uIService;
        if (uiService == null) yield break;

        // 等待几帧，让弹窗有机会显示
        yield return null;
        yield return null;

        // 等待弹窗队列清空
        while (uiService.HasQueuedPopups())
        {
            yield return null;
        }

        Debug.Log("[TutorialManager] All popups closed");
    }

    /// <summary>等待弹窗关闭后继续（设置 waitingForTrigger = false）</summary>
    private IEnumerator WaitForPopupsAndContinue()
    {
        yield return WaitForPopupsClosed();
        waitingForTrigger = false;
    }

    /// <summary>供UI按钮调用</summary>
    public void OnUIClicked(string uiName)
    {
        if (!waitingForTrigger) return;
        var step = CurrentStep;

        if (step?.Trigger == TutorialTrigger.WaitForClick &&
            step.TriggerParameter == uiName)
        {
            Debug.Log($"[TutorialManager] Trigger matched: WaitForClick ({uiName})");
            waitingForTrigger = false;
        }
    }

    #endregion

    #region 高亮

    private GameObject currentHighlight;
    private GameObject highlightTarget;

    private void ShowHighlight(string targetName)
    {
        Debug.Log($"[TutorialManager] ShowHighlight called for: {targetName}");

        // 找到目标 UI 并显示高亮框
        var target = GameObject.Find(targetName);
        if (target == null)
        {
            Debug.LogWarning($"[TutorialManager] Highlight target not found: {targetName}");
            return;
        }

        Debug.Log($"[TutorialManager] Found target: {target.name}");
        highlightTarget = target;

        // 如果有高亮 Prefab，实例化它
        if (highlightOverlay != null)
        {
            currentHighlight = Instantiate(highlightOverlay, target.transform);
            // 确保高亮在目标的最前面
            currentHighlight.transform.SetAsLastSibling();
            Debug.Log($"[TutorialManager] Highlight instantiated on {target.name}");
        }
        else
        {
            Debug.LogWarning($"[TutorialManager] No highlightOverlay prefab assigned!");
        }
    }

    private void HideHighlight()
    {
        if (currentHighlight != null)
        {
            Destroy(currentHighlight);
            currentHighlight = null;
        }
        highlightTarget = null;
    }

    #endregion

    #region 按钮控制

    /// <summary>应用步骤的按钮限制</summary>
    private void ApplyButtonRestrictions(TutorialStep step)
    {
        var gameplayWindow = GetGameplayWindow();
        if (gameplayWindow == null)
        {
            Debug.LogWarning("[TutorialManager] GameplayWindow not found, cannot apply button restrictions");
            return;
        }

        // 如果设置了 AllowedButtonsOnly，则禁用所有其他按钮
        if (step.AllowedButtonsOnly != null && step.AllowedButtonsOnly.Count > 0)
        {
            // 禁用常用按钮，只保留允许的
            var commonButtons = new[] { "Market", "Intel", "EndTurn" };
            foreach (var btnId in commonButtons)
            {
                if (!step.AllowedButtonsOnly.Contains(btnId))
                {
                    gameplayWindow.SetButtonInteractable(btnId, false);
                    currentlyDisabledButtons.Add(btnId);
                    Debug.Log($"[TutorialManager] Disabled button: {btnId}");
                }
            }
        }

        // 禁用指定的按钮
        if (step.DisabledButtons != null)
        {
            foreach (var btnId in step.DisabledButtons)
            {
                gameplayWindow.SetButtonInteractable(btnId, false);
                currentlyDisabledButtons.Add(btnId);
                Debug.Log($"[TutorialManager] Disabled button: {btnId}");
            }
        }
    }

    /// <summary>恢复所有被禁用的按钮</summary>
    private void RestoreButtonStates()
    {
        var gameplayWindow = GetGameplayWindow();
        if (gameplayWindow == null) return;

        foreach (var btnId in currentlyDisabledButtons)
        {
            gameplayWindow.SetButtonInteractable(btnId, true);
            Debug.Log($"[TutorialManager] Restored button: {btnId}");
        }
        currentlyDisabledButtons.Clear();
    }

    /// <summary>获取 GameplayWindow 实例</summary>
    private GameplayWindow GetGameplayWindow()
    {
        return uiService?.GetWindow<GameplayWindow>("GameplayWindow");
    }

    #endregion

    #region 面板监听

    /// <summary>供 UI 面板调用，通知面板已打开</summary>
    public void OnPanelOpened(string panelName)
    {
        Debug.Log($"[TutorialManager] OnPanelOpened called: {panelName}, waitingForTrigger={waitingForTrigger}, currentStep={CurrentStep?.StepId}");

        if (!waitingForTrigger)
        {
            Debug.Log($"[TutorialManager] Not waiting for trigger, ignoring panel open");
            return;
        }

        var step = CurrentStep;
        Debug.Log($"[TutorialManager] Current step trigger: {step?.Trigger}, parameter: {step?.TriggerParameter}");

        if (step?.Trigger == TutorialTrigger.WaitForPanelOpen &&
            step.TriggerParameter == panelName)
        {
            Debug.Log($"[TutorialManager] Trigger matched: WaitForPanelOpen ({panelName})");
            waitingForTrigger = false;
        }
        else
        {
            Debug.Log($"[TutorialManager] Trigger NOT matched. Expected: WaitForPanelOpen with {step?.TriggerParameter}");
        }
    }

    #endregion
}
