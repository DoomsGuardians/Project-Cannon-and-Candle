// encoding: utf-8
// Kuwahara Filter volume settings - Oil painting style effect
// 桑原滤镜 - 油画风格后处理效果

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
[VolumeComponentMenu("Post-processing/Kuwahara")]
public sealed class KuwaharaVolume : VolumeComponent, IPostProcessComponent
{
    [Tooltip("滤镜半径\n采样区域的大小，值越大油画效果越明显\n建议值: 2-8")]
    public ClampedIntParameter radius = new ClampedIntParameter(3, 1, 15);

    public bool IsActive()
    {
        return radius.value > 0;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
