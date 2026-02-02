using System;
using UnityEngine;

/// <summary>
/// 游戏设置数据（通过 PlayerPrefs 持久化）
/// </summary>
[Serializable]
public class SettingsData
{
    // 音频设置
    public float MasterVolume = 1f;
    public float BGMVolume = 1f;
    public float SFXVolume = 1f;

    // 画面设置
    public bool IsFullscreen = true;
    public int QualityLevel = 2;
    public int ResolutionIndex = -1; // -1 表示使用当前分辨率

    // 游戏设置
    public float GameSpeed = 1f;

    // 存档设置
    public bool AutoSaveEnabled = true;

    /// <summary>创建默认设置</summary>
    public static SettingsData CreateDefault()
    {
        return new SettingsData
        {
            MasterVolume = 1f,
            BGMVolume = 1f,
            SFXVolume = 1f,
            IsFullscreen = Screen.fullScreen,
            QualityLevel = QualitySettings.GetQualityLevel(),
            ResolutionIndex = -1,
            GameSpeed = 1f,
            AutoSaveEnabled = true
        };
    }

    /// <summary>克隆设置</summary>
    public SettingsData Clone()
    {
        return new SettingsData
        {
            MasterVolume = this.MasterVolume,
            BGMVolume = this.BGMVolume,
            SFXVolume = this.SFXVolume,
            IsFullscreen = this.IsFullscreen,
            QualityLevel = this.QualityLevel,
            ResolutionIndex = this.ResolutionIndex,
            GameSpeed = this.GameSpeed,
            AutoSaveEnabled = this.AutoSaveEnabled
        };
    }
}
