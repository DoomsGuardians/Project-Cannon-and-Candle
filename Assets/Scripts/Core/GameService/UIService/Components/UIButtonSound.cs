// LevityFramework - 通用 Unity 游戏框架
// UI 组件 - UIButtonSound 按钮音效组件

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 交互音效组件
/// 提供悬停和点击的音效
/// 可挂载到 Button、Toggle 或任何可交互 UI 元素上
/// </summary>
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("悬停音效")]
    [SerializeField] private bool enableHoverSound = true;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] [Range(0f, 1f)] private float hoverVolume = 1f;

    [Header("点击音效")]
    [SerializeField] private bool enableClickSound = true;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] [Range(0f, 1f)] private float clickVolume = 1f;

    [Header("高级设置")]
    [Tooltip("是否在禁用状态下也播放音效")]
    [SerializeField] private bool playSoundWhenDisabled = false;

    private Selectable selectable;
    private AudioService audioService;
    private bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        selectable = GetComponent<Selectable>();
        audioService = GameRoot.Instance?.audioService;
        isInitialized = true;
    }

    private bool CanPlaySound()
    {
        if (!isInitialized) Initialize();
        if (playSoundWhenDisabled) return true;
        return selectable == null || selectable.interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHoverSound || !CanPlaySound()) return;
        if (hoverSound == null) return;

        PlaySound(hoverSound, hoverVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enableClickSound || !CanPlaySound()) return;
        if (clickSound == null) return;

        PlaySound(clickSound, clickVolume);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (audioService == null)
        {
            audioService = GameRoot.Instance?.audioService;
        }

        audioService?.PlaySFX(clip, volume);
    }

    /// <summary>
    /// 手动播放点击音效
    /// </summary>
    public void PlayClickSound()
    {
        if (clickSound != null)
        {
            PlaySound(clickSound, clickVolume);
        }
    }

    /// <summary>
    /// 手动播放悬停音效
    /// </summary>
    public void PlayHoverSound()
    {
        if (hoverSound != null)
        {
            PlaySound(hoverSound, hoverVolume);
        }
    }
}
