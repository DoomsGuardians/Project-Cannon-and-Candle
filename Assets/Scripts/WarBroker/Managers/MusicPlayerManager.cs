using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音乐播放器管理器
/// 管理BGM播放列表，根据游戏状态智能推荐曲目
/// </summary>
public class MusicPlayerManager : ManagerBase
{
    private MusicConfig musicConfig;
    private AudioService audioService;
    private CampaignSystem campaignSystem;

    // 播放状态
    private List<MusicTrack> currentPlaylist = new List<MusicTrack>();
    private int currentTrackIndex = 0;
    private bool isPlaying = false;
    private bool isShuffleMode = false;
    private MusicMood currentMood = MusicMood.Calm;

    // 用于随机播放的已播放队列
    private List<int> shuffleHistory = new List<int>();

    // 事件：曲目变化、播放状态变化
    public event Action<MusicTrack> OnTrackChanged;
    public event Action<bool> OnPlayStateChanged;
    public event Action<bool> OnShuffleModeChanged;

    /// <summary>当前曲目名称</summary>
    public string CurrentTrackName => currentPlaylist.Count > 0 && currentTrackIndex < currentPlaylist.Count
        ? currentPlaylist[currentTrackIndex].TrackName
        : "无曲目";

    /// <summary>是否正在播放</summary>
    public bool IsPlaying => isPlaying;

    /// <summary>是否随机播放模式</summary>
    public bool IsShuffleMode => isShuffleMode;

    /// <summary>当前氛围</summary>
    public MusicMood CurrentMood => currentMood;

    public override void OnAwake()
    {
        base.OnAwake();

        audioService = gameRoot.audioService;
        musicConfig = resService.LoadResource<MusicConfig>(ConfigPaths.MUSIC_CONFIG);

        if (musicConfig == null)
        {
            Debug.LogWarning("[MusicPlayerManager] MusicConfig not found. Music player will be disabled.");
            return;
        }

        // 获取 CampaignSystem 引用（用于智能推荐）
        campaignSystem = gameRoot.campaignSystem;

        // 初始化播放列表：包含所有非胜利/失败的曲目
        InitializePlaylist();
    }

    public override void OnShow()
    {
        // 只注册游戏结束事件（用于播放胜利/失败音乐）
        eventService.AddEventListening((EventID)WarBrokerEventID.OnGameEnd, OnGameEnd);

        // 自动开始播放
        if (currentPlaylist.Count > 0 && !isPlaying)
        {
            Play();
        }
    }

    public override void OnExit()
    {
        eventService.RemoveEventListeningByTarget(this);
        Stop();
    }

    public override void UnInit()
    {
        Stop();
    }

    #region 播放控制

    /// <summary>播放/恢复播放</summary>
    public void Play()
    {
        if (musicConfig == null || currentPlaylist.Count == 0) return;

        if (currentTrackIndex >= currentPlaylist.Count)
            currentTrackIndex = 0;

        var track = currentPlaylist[currentTrackIndex];
        if (track.Clip != null)
        {
            audioService.PlayBGM(track.Clip, audioService.BGMVolume);
            isPlaying = true;
            OnPlayStateChanged?.Invoke(true);
            OnTrackChanged?.Invoke(track);
        }
    }

    /// <summary>暂停播放</summary>
    public void Pause()
    {
        if (!isPlaying) return;

        audioService.PauseBGM();
        isPlaying = false;
        OnPlayStateChanged?.Invoke(false);
    }

    /// <summary>停止播放</summary>
    public void Stop()
    {
        audioService?.StopBGM();
        isPlaying = false;
        OnPlayStateChanged?.Invoke(false);
    }

    /// <summary>切换播放/暂停</summary>
    public void TogglePlayPause()
    {
        if (isPlaying)
            Pause();
        else
            Play();
    }

    /// <summary>下一首（智能推荐）</summary>
    public void Next()
    {
        if (currentPlaylist.Count == 0) return;

        // 使用智能推荐选择下一首
        int nextIndex = SelectNextTrackByGameState();
        currentTrackIndex = nextIndex;

        shuffleHistory.Add(currentTrackIndex);
        if (shuffleHistory.Count > 50)
            shuffleHistory.RemoveAt(0);

        if (isPlaying)
            Play();
        else
            OnTrackChanged?.Invoke(currentPlaylist[currentTrackIndex]);
    }

    /// <summary>上一首</summary>
    public void Previous()
    {
        if (currentPlaylist.Count == 0) return;

        if (isShuffleMode && shuffleHistory.Count > 1)
        {
            // 从历史中回退
            shuffleHistory.RemoveAt(shuffleHistory.Count - 1);
            currentTrackIndex = shuffleHistory[shuffleHistory.Count - 1];
        }
        else
        {
            currentTrackIndex = (currentTrackIndex - 1 + currentPlaylist.Count) % currentPlaylist.Count;
        }

        if (isPlaying)
            Play();
        else
            OnTrackChanged?.Invoke(currentPlaylist[currentTrackIndex]);
    }

    /// <summary>切换随机模式</summary>
    public void ToggleShuffle()
    {
        isShuffleMode = !isShuffleMode;
        shuffleHistory.Clear();
        if (isShuffleMode && currentPlaylist.Count > 0)
        {
            shuffleHistory.Add(currentTrackIndex);
        }
        OnShuffleModeChanged?.Invoke(isShuffleMode);
    }

    #endregion

    #region 氛围切换

    /// <summary>设置音乐氛围，切换到对应的播放列表</summary>
    public void SetMood(MusicMood mood)
    {
        if (musicConfig == null) return;

        // 如果氛围没有变化，不做处理
        if (mood == currentMood && currentPlaylist.Count > 0) return;

        currentMood = mood;
        var newPlaylist = musicConfig.GetTracksByMood(mood);

        if (newPlaylist.Count == 0)
        {
            // 如果该氛围没有曲目，保持当前播放列表
            Debug.LogWarning($"[MusicPlayerManager] No tracks found for mood: {mood}");
            return;
        }

        // 切换播放列表
        currentPlaylist = new List<MusicTrack>(newPlaylist);
        currentTrackIndex = 0;
        shuffleHistory.Clear();

        if (isShuffleMode)
        {
            // 随机模式下随机选择起始曲目
            currentTrackIndex = UnityEngine.Random.Range(0, currentPlaylist.Count);
            shuffleHistory.Add(currentTrackIndex);
        }

        // 如果正在播放，切换到新曲目
        if (isPlaying)
        {
            Play();
        }
        else
        {
            OnTrackChanged?.Invoke(currentPlaylist[currentTrackIndex]);
        }
    }

    #endregion

    #region 内部方法

    /// <summary>初始化播放列表（包含所有常规曲目，不含胜利/失败）</summary>
    private void InitializePlaylist()
    {
        currentPlaylist.Clear();

        foreach (var track in musicConfig.Tracks)
        {
            // 排除胜利和失败曲目，这些只在游戏结束时播放
            if (track.Mood != MusicMood.Victory && track.Mood != MusicMood.Defeat)
            {
                currentPlaylist.Add(track);
            }
        }

        if (currentPlaylist.Count == 0)
        {
            Debug.LogWarning("[MusicPlayerManager] No tracks found in playlist");
            return;
        }

        // 使用智能权重选择初始曲目，而不是总是从第一首开始
        currentTrackIndex = SelectNextTrackByGameState();
        shuffleHistory.Clear();
        shuffleHistory.Add(currentTrackIndex);
    }

    /// <summary>
    /// 根据游戏状态智能选择下一首曲目
    /// 核心逻辑：根据战场状态影响各氛围曲目的选择概率
    /// </summary>
    private int SelectNextTrackByGameState()
    {
        if (currentPlaylist.Count <= 1)
            return 0;

        // 计算各氛围的权重
        var moodWeights = new Dictionary<MusicMood, float>();
        foreach (MusicMood mood in Enum.GetValues(typeof(MusicMood)))
        {
            if (mood == MusicMood.Victory || mood == MusicMood.Defeat)
                continue;
            moodWeights[mood] = GetMoodWeight(mood);
        }

        // 计算每首曲目的权重
        var trackWeights = new List<float>();
        float totalWeight = 0f;

        for (int i = 0; i < currentPlaylist.Count; i++)
        {
            var track = currentPlaylist[i];
            float weight = moodWeights.TryGetValue(track.Mood, out float w) ? w : 1f;

            // 避免连续播放同一首：当前曲目权重降低
            if (i == currentTrackIndex)
                weight *= 0.1f;

            trackWeights.Add(weight);
            totalWeight += weight;
        }

        // 加权随机选择
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < trackWeights.Count; i++)
        {
            cumulative += trackWeights[i];
            if (randomValue <= cumulative)
            {
                return i;
            }
        }

        // 保险返回
        return (currentTrackIndex + 1) % currentPlaylist.Count;
    }

    /// <summary>
    /// 计算指定氛围的权重
    /// 基于后备役比例和将军状态调整
    /// </summary>
    private float GetMoodWeight(MusicMood mood)
    {
        float baseWeight = 1f;

        // 如果没有战役数据，返回基础权重
        if (campaignSystem?.Data?.Battle == null)
            return baseWeight;

        var battleData = campaignSystem.Data.Battle;

        // 后备役比例 (0~1，越低越紧张)
        // InitialReserves 默认为 60
        float maxReserves = campaignSystem.Data.Config?.InitialReserves ?? 60f;
        float reserveRatio = maxReserves > 0 ? battleData.CurrentReserves / maxReserves : 1f;
        reserveRatio = Mathf.Clamp01(reserveRatio);

        // 计算总伤亡（累计的兵力损失）
        float totalCasualties = 0f;
        foreach (var general in battleData.AllyGenerals)
        {
            // 假设每个将军满编为 10 兵力
            totalCasualties += Mathf.Max(0, 10 - general.Troops);
        }
        // 伤亡比例 (归一化到 0~1)
        float maxCasualties = battleData.AllyGenerals.Count * 10f;
        float casualtyRatio = maxCasualties > 0 ? totalCasualties / maxCasualties : 0f;
        casualtyRatio = Mathf.Clamp01(casualtyRatio);

        switch (mood)
        {
            case MusicMood.Calm:
                // 后备役充足时权重高
                return baseWeight + reserveRatio * 1.5f;

            case MusicMood.Tension:
                // 后备役紧张时权重高
                return baseWeight + (1f - reserveRatio) * 1.5f;

            case MusicMood.Battle:
                // 伤亡高或后备役紧张时权重高
                return baseWeight + casualtyRatio * 1.5f + (1f - reserveRatio) * 0.5f;

            default:
                return baseWeight;
        }
    }

    private void PlayRandomTrack()
    {
        if (currentPlaylist.Count <= 1)
        {
            currentTrackIndex = 0;
        }
        else
        {
            // 避免连续播放同一首
            int newIndex;
            do
            {
                newIndex = UnityEngine.Random.Range(0, currentPlaylist.Count);
            } while (newIndex == currentTrackIndex && currentPlaylist.Count > 1);

            currentTrackIndex = newIndex;
        }

        shuffleHistory.Add(currentTrackIndex);

        // 限制历史记录长度
        if (shuffleHistory.Count > 50)
            shuffleHistory.RemoveAt(0);

        if (isPlaying)
            Play();
        else
            OnTrackChanged?.Invoke(currentPlaylist[currentTrackIndex]);
    }

    #endregion

    #region 事件处理

    private void OnGameEnd(object param1, object param2)
    {
        if (musicConfig == null) return;

        bool isVictory = (bool)param1;
        SetMood(isVictory ? MusicMood.Victory : MusicMood.Defeat);
    }

    #endregion

    private void Update()
    {
        // 检测曲目是否播放完毕，自动切换下一首
        if (isPlaying && audioService != null && musicConfig != null)
        {
            // 由于 AudioService 使用循环播放，这里不需要手动检测曲目结束
            // 如果需要列表播放模式，可以在这里添加检测逻辑
        }
    }
}
