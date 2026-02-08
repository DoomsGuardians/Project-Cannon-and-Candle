// LevityFramework - 通用 Unity 游戏框架
// 工具类 - 日志扩展方法

using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// 日志扩展方法
/// 使用 [Conditional("UNITY_EDITOR")] 或 [Conditional("DEVELOPMENT_BUILD")]
/// 确保发布版本中这些方法调用会被编译器完全移除
/// </summary>
public static class LogExtensions
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(this object obj, string message)
    {
        Debug.Log($"[{obj.GetType().Name}] {message}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogYellow(this object obj, string message)
    {
        Debug.Log($"<color=yellow>[{obj.GetType().Name}] {message}</color>");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogGreen(this object obj, string message)
    {
        Debug.Log($"<color=green>[{obj.GetType().Name}] {message}</color>");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogRed(this object obj, string message)
    {
        Debug.Log($"<color=red>[{obj.GetType().Name}] {message}</color>");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Warn(this object obj, string message)
    {
        Debug.LogWarning($"[{obj.GetType().Name}] {message}");
    }

    // Error 保留在发布版本中，因为错误信息对问题排查很重要
    public static void Error(this object obj, string message)
    {
        Debug.LogError($"[{obj.GetType().Name}] {message}");
    }
}
