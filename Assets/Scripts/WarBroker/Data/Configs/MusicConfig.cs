using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音乐氛围类型
/// 用于标记曲目的氛围分类，智能推荐根据游戏状态调整各氛围的选择概率
/// </summary>
public enum MusicMood
{
    Calm,       // 平静
    Tension,    // 紧张
    Battle,     // 战斗
    Victory,    // 胜利
    Defeat      // 失败
}

/// <summary>
/// 单曲配置
/// </summary>
[Serializable]
public class MusicTrack
{
    [Tooltip("音频文件")]
    public AudioClip Clip;

    [Tooltip("曲目氛围（影响智能推荐的选择概率）")]
    public MusicMood Mood;

    /// <summary>曲目名称（自动从音频文件名获取）</summary>
    public string TrackName => Clip != null ? Clip.name : "未知曲目";
}

/// <summary>
/// 音乐配置 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "MusicConfig", menuName = "WarBroker/Config/MusicConfig")]
public class MusicConfig : ScriptableObject
{
    [Header("主题曲")]
    [Tooltip("主界面播放的主题曲")]
    public AudioClip TitleTheme;

    [Header("曲目列表")]
    [Tooltip("所有可用的音乐曲目")]
    public List<MusicTrack> Tracks = new List<MusicTrack>();

    [Header("默认设置")]
    [Tooltip("游戏启动时的默认氛围")]
    public MusicMood DefaultMood = MusicMood.Calm;

    [Tooltip("切换曲目时的淡入淡出时间（秒）")]
    public float CrossfadeDuration = 1.0f;

    /// <summary>
    /// 获取指定氛围的所有曲目
    /// </summary>
    public List<MusicTrack> GetTracksByMood(MusicMood mood)
    {
        return Tracks.FindAll(t => t.Mood == mood);
    }
}
