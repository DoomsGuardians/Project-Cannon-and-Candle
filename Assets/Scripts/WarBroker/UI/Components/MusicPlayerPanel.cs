using UnityEngine;
using UnityEngine.UI;
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

        // 绑定按钮事件
        BindButtons();

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
