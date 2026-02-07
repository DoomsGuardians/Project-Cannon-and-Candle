using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教程配置
/// </summary>
[CreateAssetMenu(fileName = "TutorialConfig", menuName = "WarBroker/Config/TutorialConfig")]
public class TutorialConfig : ScriptableObject
{
    [Header("教程步骤")]
    public List<TutorialStep> Steps = new List<TutorialStep>();
}

/// <summary>
/// 教程步骤定义
/// </summary>
[Serializable]
public class TutorialStep
{
    [Header("基本信息")]
    public string StepId;
    [TextArea(2, 4)]
    public string Description;  // 编辑器备注用

    [Header("Naninovel 对话")]
    [Tooltip("Naninovel 脚本名称（不含扩展名）")]
    public string ScriptName;
    [Tooltip("可选：从指定标签开始播放")]
    public string StartLabel;

    [Header("触发条件")]
    public TutorialTrigger Trigger = TutorialTrigger.Immediate;
    public string TriggerParameter;  // 根据Trigger类型使用

    [Header("UI 控制")]
    [Tooltip("高亮的 UI 元素名称")]
    public string HighlightTarget;

    [Tooltip("此步骤期间禁用的按钮（防止误操作）")]
    public List<string> DisabledButtons = new List<string>();

    [Tooltip("此步骤期间只允许点击的按钮（其他按钮自动禁用）")]
    public List<string> AllowedButtonsOnly = new List<string>();
}

/// <summary>
/// 教程触发条件类型
/// </summary>
public enum TutorialTrigger
{
    Immediate,              // 对话结束后立即进入下一步
    WaitForBuy,             // 等待玩家买入（TriggerParameter = 指令类型，如"ATK"）
    WaitForSell,            // 等待玩家卖出
    WaitForAssign,          // 等待玩家分配指令给将军
    WaitForEndTurn,         // 等待玩家结束回合
    WaitForClick,           // 等待点击特定UI（TriggerParameter = UI名称）
    WaitForPhase,           // 等待进入特定阶段（TriggerParameter = 阶段名）
    WaitForPhaseBanner,     // 等待阶段横幅完成且弹窗关闭（TriggerParameter = 阶段名，如"MarketPhase"）
    WaitForPanelOpen,       // 等待面板打开（TriggerParameter = 面板名，如"MarketPanel"）
}
