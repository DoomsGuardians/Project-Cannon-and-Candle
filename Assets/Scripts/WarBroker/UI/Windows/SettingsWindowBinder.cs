using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 设置窗口 Binder
/// </summary>
public class SettingsWindowBinder : UIBinder
{
    [Header("Header")]
    public TMP_Text txtTitle;
    public Button btnClose;

    [Header("Tab Bar")]
    public Toggle tabAudio;
    public Toggle tabDisplay;
    public Toggle tabGame;
    public Toggle tabSave;

    [Header("Content Areas")]
    public GameObject audioContent;
    public GameObject displayContent;
    public GameObject gameContent;
    public GameObject saveContent;

    [Header("Audio Settings")]
    public Slider sliderMasterVolume;
    public Slider sliderBGMVolume;
    public Slider sliderSFXVolume;
    public TMP_Text txtMasterVolume;
    public TMP_Text txtBGMVolume;
    public TMP_Text txtSFXVolume;

    [Header("Display Settings")]
    public Toggle toggleFullscreen;
    public TMP_Dropdown dropdownQuality;
    public TMP_Dropdown dropdownResolution;

    [Header("Game Settings")]
    public Slider sliderGameSpeed;
    public TMP_Text txtGameSpeed;

    [Header("Save Settings")]
    public Toggle toggleAutoSave;
    public Button btnManualSave;
    public Transform saveListContent;
    public TMP_Text txtSaveStatus;

    [Header("Footer")]
    public Button btnApply;
    public Button btnCancel;
    public Button btnReset;
}
