using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 设置窗口
/// 包含音频、画面、游戏、存档四个Tab页
/// </summary>
public class SettingsWindow : WindowBase
{
    public new UILayer uiLayer = UILayer.Top;
    public new bool IsFullScreen = false;

    private SettingsWindowBinder binder;
    private SettingsSystem settingsSystem;

    // 临时设置（未应用前的修改）
    private SettingsData tempSettings;

    // 当前激活的Tab
    private int currentTab = 0;
    private GameObject[] tabContents;

    public override void OnAwake()
    {
        base.OnAwake();

        binder = gameObject.GetComponent<SettingsWindowBinder>();
        settingsSystem = GameRoot.Instance.settingsSystem;
    }

    public override void OnShow()
    {
        InputRouter.Acquire(InputChannel.Gameplay, this);

        // 克隆当前设置作为临时设置
        tempSettings = settingsSystem.CurrentSettings.Clone();

        InitTabContents();
        InitAudioTab();
        InitDisplayTab();
        InitGameTab();
        InitSaveTab();
        InitFooter();

        // 默认显示第一个Tab
        SwitchTab(0);
    }

    public override void OnHide()
    {
        InputRouter.Release(InputChannel.Gameplay, this);
        eventService.RemoveEventListeningByTarget(this);
    }

    #region Tab Management

    private void InitTabContents()
    {
        if (binder == null) return;

        tabContents = new GameObject[]
        {
            binder.audioContent,
            binder.displayContent,
            binder.gameContent,
            binder.saveContent
        };

        // 设置Tab切换监听
        if (binder.tabAudio != null)
            binder.tabAudio.onValueChanged.AddListener(on => { if (on) SwitchTab(0); });
        if (binder.tabDisplay != null)
            binder.tabDisplay.onValueChanged.AddListener(on => { if (on) SwitchTab(1); });
        if (binder.tabGame != null)
            binder.tabGame.onValueChanged.AddListener(on => { if (on) SwitchTab(2); });
        if (binder.tabSave != null)
            binder.tabSave.onValueChanged.AddListener(on => { if (on) SwitchTab(3); });

        // 关闭按钮
        AddButtonListener(binder.btnClose, OnCancelClick);
    }

    private void SwitchTab(int index)
    {
        currentTab = index;

        for (int i = 0; i < tabContents.Length; i++)
        {
            if (tabContents[i] != null)
                tabContents[i].SetActive(i == index);
        }
    }

    #endregion

    #region Audio Tab

    private void InitAudioTab()
    {
        if (binder == null) return;

        // 主音量
        if (binder.sliderMasterVolume != null)
        {
            binder.sliderMasterVolume.value = tempSettings.MasterVolume;
            binder.sliderMasterVolume.onValueChanged.AddListener(OnMasterVolumeChanged);
            UpdateVolumeText(binder.txtMasterVolume, tempSettings.MasterVolume);
        }

        // BGM音量
        if (binder.sliderBGMVolume != null)
        {
            binder.sliderBGMVolume.value = tempSettings.BGMVolume;
            binder.sliderBGMVolume.onValueChanged.AddListener(OnBGMVolumeChanged);
            UpdateVolumeText(binder.txtBGMVolume, tempSettings.BGMVolume);
        }

        // SFX音量
        if (binder.sliderSFXVolume != null)
        {
            binder.sliderSFXVolume.value = tempSettings.SFXVolume;
            binder.sliderSFXVolume.onValueChanged.AddListener(OnSFXVolumeChanged);
            UpdateVolumeText(binder.txtSFXVolume, tempSettings.SFXVolume);
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        tempSettings.MasterVolume = value;
        UpdateVolumeText(binder.txtMasterVolume, value);
        // 实时预览
        settingsSystem.SetMasterVolume(value);
    }

    private void OnBGMVolumeChanged(float value)
    {
        tempSettings.BGMVolume = value;
        UpdateVolumeText(binder.txtBGMVolume, value);
        settingsSystem.SetBGMVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        tempSettings.SFXVolume = value;
        UpdateVolumeText(binder.txtSFXVolume, value);
        settingsSystem.SetSFXVolume(value);
    }

    private void UpdateVolumeText(TMP_Text text, float value)
    {
        if (text != null)
            text.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    #endregion

    #region Display Tab

    private void InitDisplayTab()
    {
        if (binder == null) return;

        // 全屏
        if (binder.toggleFullscreen != null)
        {
            binder.toggleFullscreen.isOn = tempSettings.IsFullscreen;
            binder.toggleFullscreen.onValueChanged.AddListener(OnFullscreenChanged);
        }

        // 画质
        if (binder.dropdownQuality != null)
        {
            binder.dropdownQuality.ClearOptions();
            var qualityNames = QualitySettings.names;
            binder.dropdownQuality.AddOptions(new List<string>(qualityNames));
            binder.dropdownQuality.value = tempSettings.QualityLevel;
            binder.dropdownQuality.onValueChanged.AddListener(OnQualityChanged);
        }

        // 分辨率
        if (binder.dropdownResolution != null)
        {
            binder.dropdownResolution.ClearOptions();
            var resolutions = Screen.resolutions;
            var options = new List<string>();
            int currentIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                var res = resolutions[i];
                options.Add($"{res.width} x {res.height} @ {res.refreshRateRatio}Hz");

                if (res.width == Screen.currentResolution.width &&
                    res.height == Screen.currentResolution.height)
                {
                    currentIndex = i;
                }
            }

            binder.dropdownResolution.AddOptions(options);
            binder.dropdownResolution.value = tempSettings.ResolutionIndex >= 0
                ? tempSettings.ResolutionIndex
                : currentIndex;
            binder.dropdownResolution.onValueChanged.AddListener(OnResolutionChanged);
        }
    }

    private void OnFullscreenChanged(bool value)
    {
        tempSettings.IsFullscreen = value;
    }

    private void OnQualityChanged(int index)
    {
        tempSettings.QualityLevel = index;
    }

    private void OnResolutionChanged(int index)
    {
        tempSettings.ResolutionIndex = index;
    }

    #endregion

    #region Game Tab

    private void InitGameTab()
    {
        if (binder == null) return;

        // 游戏速度
        if (binder.sliderGameSpeed != null)
        {
            // 将 0.5-2.0 映射到 0-1
            float normalizedSpeed = (tempSettings.GameSpeed - 0.5f) / 1.5f;
            binder.sliderGameSpeed.value = normalizedSpeed;
            binder.sliderGameSpeed.onValueChanged.AddListener(OnGameSpeedChanged);
            UpdateGameSpeedText(tempSettings.GameSpeed);
        }
    }

    private void OnGameSpeedChanged(float normalizedValue)
    {
        // 将 0-1 映射回 0.5-2.0
        float speed = 0.5f + normalizedValue * 1.5f;
        // 量化到 0.5x, 1x, 1.5x, 2x
        speed = Mathf.Round(speed * 2) / 2f;
        tempSettings.GameSpeed = speed;
        UpdateGameSpeedText(speed);
    }

    private void UpdateGameSpeedText(float speed)
    {
        if (binder.txtGameSpeed != null)
            binder.txtGameSpeed.text = $"{speed:0.#}x";
    }

    #endregion

    #region Save Tab

    private void InitSaveTab()
    {
        if (binder == null) return;

        // 自动存档
        if (binder.toggleAutoSave != null)
        {
            binder.toggleAutoSave.isOn = tempSettings.AutoSaveEnabled;
            binder.toggleAutoSave.onValueChanged.AddListener(on => tempSettings.AutoSaveEnabled = on);
        }

        // 手动存档按钮
        AddButtonListener(binder.btnManualSave, OnManualSaveClick);

        // TODO: 加载存档列表
        if (binder.txtSaveStatus != null)
            binder.txtSaveStatus.text = "存档功能开发中...";
    }

    private void OnManualSaveClick()
    {
        // TODO: 实现手动存档
        Debug.Log("[SettingsWindow] Manual save - TODO");
        if (binder.txtSaveStatus != null)
            binder.txtSaveStatus.text = "存档功能开发中...";
    }

    #endregion

    #region Footer

    private void InitFooter()
    {
        AddButtonListener(binder.btnApply, OnApplyClick);
        AddButtonListener(binder.btnCancel, OnCancelClick);
        AddButtonListener(binder.btnReset, OnResetClick);
    }

    private void OnApplyClick()
    {
        // 保存并应用设置
        settingsSystem.UpdateAndSaveSettings(tempSettings);
        uIService.HideWindow(Name);
    }

    private void OnCancelClick()
    {
        // 恢复原设置
        settingsSystem.ApplySettings();
        uIService.HideWindow(Name);
    }

    private void OnResetClick()
    {
        // 重置为默认
        tempSettings = SettingsData.CreateDefault();
        settingsSystem.UpdateSettings(tempSettings);

        // 刷新UI
        RefreshAllTabs();
    }

    private void RefreshAllTabs()
    {
        // 刷新音频Tab
        if (binder.sliderMasterVolume != null)
            binder.sliderMasterVolume.value = tempSettings.MasterVolume;
        if (binder.sliderBGMVolume != null)
            binder.sliderBGMVolume.value = tempSettings.BGMVolume;
        if (binder.sliderSFXVolume != null)
            binder.sliderSFXVolume.value = tempSettings.SFXVolume;

        // 刷新画面Tab
        if (binder.toggleFullscreen != null)
            binder.toggleFullscreen.isOn = tempSettings.IsFullscreen;
        if (binder.dropdownQuality != null)
            binder.dropdownQuality.value = tempSettings.QualityLevel;

        // 刷新游戏Tab
        if (binder.sliderGameSpeed != null)
        {
            float normalizedSpeed = (tempSettings.GameSpeed - 0.5f) / 1.5f;
            binder.sliderGameSpeed.value = normalizedSpeed;
        }

        // 刷新存档Tab
        if (binder.toggleAutoSave != null)
            binder.toggleAutoSave.isOn = tempSettings.AutoSaveEnabled;
    }

    #endregion
}
