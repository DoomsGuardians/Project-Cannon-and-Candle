// encoding: utf-8
// Lilliput volume settings - Depth-based DoF for miniature effect
// 基于深度的景深效果 - 让场景看起来像微缩模型/沙盘

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
[VolumeComponentMenu("Post-processing/Lilliput")]
public sealed class LilliputVolume : VolumeComponent, IPostProcessComponent
{
    [Tooltip("焦点距离 (世界单位)\n相机到焦点平面的距离，该距离处的物体最清晰\n建议根据场景调整，通常 10-50")]
    public ClampedFloatParameter focusDistance = new ClampedFloatParameter(20f, 0.1f, 200f);

    [Tooltip("焦点范围 (世界单位)\n焦点前后保持清晰的范围，值越小景深越浅\n建议值: 5-20")]
    public ClampedFloatParameter focusRange = new ClampedFloatParameter(10f, 0.1f, 100f);

    [Tooltip("模糊强度 (像素半径)\n值越大，失焦区域越模糊\n建议值: 3-10")]
    public ClampedFloatParameter blurStrength = new ClampedFloatParameter(6f, 0f, 30f);

    public bool IsActive()
    {
        return blurStrength.value > 0.1f;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
