using UnityEngine;

/// <summary>
/// 设置系统：管理游戏设置的读取、保存和应用
/// </summary>
public class SettingsSystem : ILogic
{
    private const string SETTINGS_KEY = "WarBroker_Settings";

    private AudioService audioService;

    public SettingsData CurrentSettings { get; private set; }

    public void OnInit()
    {
        audioService = GameRoot.Instance.audioService;
        LoadSettings();
        ApplySettings();
    }

    public void OnEnterState() { }
    public void OnUpdate() { }
    public void UnInit() { }

    /// <summary>从 PlayerPrefs 加载设置</summary>
    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey(SETTINGS_KEY))
        {
            string json = PlayerPrefs.GetString(SETTINGS_KEY);
            try
            {
                CurrentSettings = JsonUtility.FromJson<SettingsData>(json);
            }
            catch
            {
                Debug.LogWarning("[SettingsSystem] Failed to parse settings, using defaults.");
                CurrentSettings = SettingsData.CreateDefault();
            }
        }
        else
        {
            CurrentSettings = SettingsData.CreateDefault();
        }
    }

    /// <summary>保存设置到 PlayerPrefs</summary>
    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(CurrentSettings);
        PlayerPrefs.SetString(SETTINGS_KEY, json);
        PlayerPrefs.Save();
    }

    /// <summary>应用当前设置</summary>
    public void ApplySettings()
    {
        ApplyAudioSettings();
        ApplyDisplaySettings();
        ApplyGameSettings();
    }

    /// <summary>应用音频设置</summary>
    public void ApplyAudioSettings()
    {
        if (audioService == null) return;

        AudioListener.volume = CurrentSettings.MasterVolume;
        audioService.SetBGMVolume(CurrentSettings.BGMVolume);
        audioService.SetSFXVolume(CurrentSettings.SFXVolume);
    }

    /// <summary>应用画面设置</summary>
    public void ApplyDisplaySettings()
    {
        // 画质
        QualitySettings.SetQualityLevel(CurrentSettings.QualityLevel);

        // 全屏和分辨率
        if (CurrentSettings.ResolutionIndex >= 0 && CurrentSettings.ResolutionIndex < Screen.resolutions.Length)
        {
            var resolution = Screen.resolutions[CurrentSettings.ResolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, CurrentSettings.IsFullscreen);
        }
        else
        {
            Screen.fullScreen = CurrentSettings.IsFullscreen;
        }
    }

    /// <summary>应用游戏设置</summary>
    public void ApplyGameSettings()
    {
        Time.timeScale = CurrentSettings.GameSpeed;
    }

    /// <summary>更新设置（不保存）</summary>
    public void UpdateSettings(SettingsData newSettings)
    {
        CurrentSettings = newSettings;
        ApplySettings();
    }

    /// <summary>更新并保存设置</summary>
    public void UpdateAndSaveSettings(SettingsData newSettings)
    {
        UpdateSettings(newSettings);
        SaveSettings();
    }

    /// <summary>重置为默认设置</summary>
    public void ResetToDefault()
    {
        CurrentSettings = SettingsData.CreateDefault();
        ApplySettings();
        SaveSettings();
    }

    #region 便捷方法

    public void SetMasterVolume(float volume)
    {
        CurrentSettings.MasterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = CurrentSettings.MasterVolume;
    }

    public void SetBGMVolume(float volume)
    {
        CurrentSettings.BGMVolume = Mathf.Clamp01(volume);
        audioService?.SetBGMVolume(CurrentSettings.BGMVolume);
    }

    public void SetSFXVolume(float volume)
    {
        CurrentSettings.SFXVolume = Mathf.Clamp01(volume);
        audioService?.SetSFXVolume(CurrentSettings.SFXVolume);
    }

    public void SetFullscreen(bool fullscreen)
    {
        CurrentSettings.IsFullscreen = fullscreen;
        Screen.fullScreen = fullscreen;
    }

    public void SetQualityLevel(int level)
    {
        CurrentSettings.QualityLevel = level;
        QualitySettings.SetQualityLevel(level);
    }

    public void SetResolution(int index)
    {
        CurrentSettings.ResolutionIndex = index;
        if (index >= 0 && index < Screen.resolutions.Length)
        {
            var resolution = Screen.resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, CurrentSettings.IsFullscreen);
        }
    }

    public void SetGameSpeed(float speed)
    {
        CurrentSettings.GameSpeed = Mathf.Clamp(speed, 0.5f, 2f);
        Time.timeScale = CurrentSettings.GameSpeed;
    }

    #endregion
}
