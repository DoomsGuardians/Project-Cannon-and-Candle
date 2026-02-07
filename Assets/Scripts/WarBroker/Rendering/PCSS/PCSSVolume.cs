using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WarBroker.Rendering
{
    /// <summary>
    /// PCSS (Percentage Closer Soft Shadows) Volume组件
    /// 控制软阴影的参数
    /// </summary>
    [Serializable]
    [VolumeComponentMenu("Post-processing/PCSS Shadows")]
    public sealed class PCSSVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("Light Settings")]
        [Tooltip("光源角半径(度)\n控制软阴影的最大柔和度\n太阳约0.5度，阴天可设置更大值")]
        public ClampedFloatParameter lightAngularRadius = new ClampedFloatParameter(0.5f, 0.1f, 5.0f);

        [Header("Search Settings")]
        [Tooltip("遮挡物搜索半径(世界单位)\n在此范围内搜索遮挡物\n较大值可检测更远的遮挡物，但可能引入噪点")]
        public ClampedFloatParameter blockerSearchRadius = new ClampedFloatParameter(5.0f, 1.0f, 20.0f);

        [Tooltip("遮挡物搜索采样数\n更多采样=更精确的遮挡物检测\n推荐16-32")]
        public ClampedIntParameter blockerSampleCount = new ClampedIntParameter(32, 8, 64);

        [Header("Filter Settings")]
        [Tooltip("PCF滤波采样数\n更多采样=更平滑的阴影边缘\n推荐32-64")]
        public ClampedIntParameter filterSampleCount = new ClampedIntParameter(64, 16, 64);

        [Tooltip("最大软阴影半径(阴影图像素)\n限制阴影的最大模糊范围\n过大可能导致采样不足")]
        public ClampedFloatParameter maxSoftness = new ClampedFloatParameter(32.0f, 4.0f, 64.0f);

        [Header("Quality")]
        [Tooltip("启用时域稳定\n使用帧间抖动减少闪烁，但可能在快速移动时产生拖影")]
        public BoolParameter temporalStability = new BoolParameter(true);

        /// <summary>
        /// 检查效果是否激活
        /// </summary>
        public bool IsActive() => lightAngularRadius.value > 0.0f;

        /// <summary>
        /// 是否兼容Tile-based渲染
        /// </summary>
        public bool IsTileCompatible() => false;
    }
}
