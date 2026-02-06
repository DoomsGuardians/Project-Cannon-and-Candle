using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 音乐播放器面板组件
/// 挂载在 UI 上，提供播放控制按钮
/// </summary>
public class MusicPlayerPanel : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Button btnPrevious;
    [SerializeField] private Button btnPlayPause;
    [SerializeField] private Button btnNext;
    [SerializeField] private Button btnShuffle;
    [SerializeField] private TMP_Text txtTrackName;
    [SerializeField] private Image imgPlayPause;

    [Header("图标")]
    [SerializeField] private Sprite iconPlay;
    [SerializeField] private Sprite iconPause;
    [SerializeField] private Color shuffleOnColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color shuffleOffColor = Color.white;

    private MusicPlayerManager musicManager;
    private Image shuffleImage;
    private TooltipPanel tooltipPanel;
    private UITextConfig textConfig;

    private void Awake()
    {
        if (btnShuffle != null)
        {
            shuffleImage = btnShuffle.GetComponent<Image>();
        }
    }

    private void Start()
    {
        // 获取 MusicPlayerManager
        musicManager = GameRoot.Instance?.managerService?.GetManager<MusicPlayerManager>();

        if (musicManager == null)
        {
            Debug.LogWarning("[MusicPlayerPanel] MusicPlayerManager not found. Panel will be disabled.");
            gameObject.SetActive(false);
            return;
        }

        // 加载配置
        if (GameRoot.Instance != null)
        {
            textConfig = GameRoot.Instance.resService.LoadResource<UITextConfig>(ConfigPaths.UI_TEXT);
            tooltipPanel = GameRoot.Instance.uIService.GetWindow<TooltipPanel>("TooltipPanel");
        }

        // 绑定按钮事件
        BindButtons();

        // 绑定悬停事件（Tooltip）
        BindHoverEvents();

        // 订阅 Manager 事件
        musicManager.OnTrackChanged += OnTrackChanged;
        musicManager.OnPlayStateChanged += OnPlayStateChanged;
        musicManager.OnShuffleModeChanged += OnShuffleModeChanged;

        // 初始化 UI 状态
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (musicManager != null)
        {
            musicManager.OnTrackChanged -= OnTrackChanged;
            musicManager.OnPlayStateChanged -= OnPlayStateChanged;
            musicManager.OnShuffleModeChanged -= OnShuffleModeChanged;
        }
    }

    private void BindButtons()
    {
        if (btnPrevious != null)
            btnPrevious.onClick.AddListener(OnPreviousClicked);

        if (btnPlayPause != null)
            btnPlayPause.onClick.AddListener(OnPlayPauseClicked);

        if (btnNext != null)
            btnNext.onClick.AddListener(OnNextClicked);

        if (btnShuffle != null)
            btnShuffle.onClick.AddListener(OnShuffleClicked);
    }

    private void BindHoverEvents()
    {
        string prevTooltip = textConfig?.TooltipBtnPrevious ?? "上一首";
        string playPauseTooltip = textConfig?.TooltipBtnPlayPause ?? "播放 / 暂停";
        string nextTooltip = textConfig?.TooltipBtnNext ?? "下一首";
        string shuffleTooltip = textConfig?.TooltipBtnShuffle ?? "随机播放";

        AddButtonTooltip(btnPrevious, prevTooltip);
        AddButtonTooltip(btnPlayPause, playPauseTooltip);
        AddButtonTooltip(btnNext, nextTooltip);
        AddButtonTooltip(btnShuffle, shuffleTooltip);
    }

    private void AddButtonTooltip(Button button, string title, string content = null)
    {
        if (button == null) return;

        var trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        // 如果没有提供content，使用title作为content
        string tooltipContent = content ?? title;

        // 鼠标进入
        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) => tooltipPanel?.Show(title, tooltipContent));
        trigger.triggers.Add(entryEnter);

        // 鼠标离开
        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) => tooltipPanel?.Hide());
        trigger.triggers.Add(entryExit);
    }

    #region 按钮点击处理

    private void OnPreviousClicked()
    {
        musicManager?.Previous();
    }

    private void OnPlayPauseClicked()
    {
        musicManager?.TogglePlayPause();
    }

    private void OnNextClicked()
    {
        musicManager?.Next();
    }

    private void OnShuffleClicked()
    {
        musicManager?.ToggleShuffle();
    }

    #endregion

    #region 事件回调

    private void OnTrackChanged(MusicTrack track)
    {
        UpdateTrackName();
    }

    private void OnPlayStateChanged(bool isPlaying)
    {
        UpdatePlayPauseButton();
    }

    private void OnShuffleModeChanged(bool isShuffleMode)
    {
        UpdateShuffleButton();
    }

    #endregion

    #region UI更新

    private void UpdateUI()
    {
        UpdateTrackName();
        UpdatePlayPauseButton();
        UpdateShuffleButton();
    }

    private void UpdateTrackName()
    {
        if (txtTrackName != null && musicManager != null)
        {
            txtTrackName.text = musicManager.CurrentTrackName;
        }
    }

    private void UpdatePlayPauseButton()
    {
        if (imgPlayPause != null && musicManager != null)
        {
            imgPlayPause.sprite = musicManager.IsPlaying ? iconPause : iconPlay;
        }
    }

    private void UpdateShuffleButton()
    {
        if (shuffleImage != null && musicManager != null)
        {
            shuffleImage.color = musicManager.IsShuffleMode ? shuffleOnColor : shuffleOffColor;
        }
    }

    #endregion
}
