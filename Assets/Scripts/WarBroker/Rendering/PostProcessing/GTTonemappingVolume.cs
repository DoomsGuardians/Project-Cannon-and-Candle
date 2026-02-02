// encoding: utf-8
// GT (Gran Turismo) Tonemapping volume settings
// GT 色调映射 - 来自 Gran Turismo 的色调映射曲线

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
[VolumeComponentMenu("Post-processing/GT Tonemapping")]
public sealed class GTTonemappingVolume : VolumeComponent, IPostProcessComponent
{
    [Tooltip("效果强度\n0 = 原始颜色, 1 = 完全应用 GT 曲线")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 1f);

    [Tooltip("最大亮度 (P)\n控制输出的最大亮度值")]
    public ClampedFloatParameter maxBrightness = new ClampedFloatParameter(1f, 0.5f, 2f);

    [Tooltip("对比度 (a)\n线性段的斜率")]
    public ClampedFloatParameter contrast = new ClampedFloatParameter(1f, 0.5f, 2f);

    [Tooltip("线性段起点 (m)\n从暗部过渡到线性段的位置")]
    public ClampedFloatParameter linearStart = new ClampedFloatParameter(0.22f, 0.01f, 0.5f);

    [Tooltip("线性段长度 (l)\n线性段占总范围的比例")]
    public ClampedFloatParameter linearLength = new ClampedFloatParameter(0.4f, 0.01f, 0.99f);

    [Tooltip("暗部对比度 (c)\n暗部区域的 gamma 曲线指数")]
    public ClampedFloatParameter toeStrength = new ClampedFloatParameter(1.33f, 1f, 3f);

    [Tooltip("黑色偏移 (b)\n提升暗部的最低亮度")]
    public ClampedFloatParameter blackTighten = new ClampedFloatParameter(0f, 0f, 0.1f);

    public bool IsActive()
    {
        return intensity.value > 0f;
    }

    public bool IsTileCompatible()
    {
        return true;
    }
}
