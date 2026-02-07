#ifndef PCSS_COMMON_INCLUDED
#define PCSS_COMMON_INCLUDED

#include "ShadowSamplingDisk.hlsl"

// PCSS参数
// x: lightAngularRadius (radians)
// y: blockerSearchRadius (world units)
// z: maxSoftness (shadowmap texels)
// w: unused
float4 _PCSSParams;

// x: blockerSampleCount
// y: filterSampleCount
// z: frameIndex (for temporal)
// w: unused
float4 _PCSSParams2;

// 遮挡物搜索
// 返回: x = 平均遮挡物深度, y = 遮挡物数量
float2 PCSS_BlockerSearch(
    TEXTURE2D_PARAM(ShadowMap, sampler_ShadowMap),
    float3 shadowCoord,
    float searchRadius,
    float2 rotationVec,
    float4 shadowmapSize)
{
    float avgBlockerDepth = 0.0;
    float numBlockers = 0.0;
    int sampleCount = (int)_PCSSParams2.x;
    float invSampleCount = rcp((float)sampleCount);

    UNITY_LOOP
    for (int i = 0; i < sampleCount && i < DISK_SAMPLE_COUNT; i++)
    {
        float sampleDistNorm;
        float2 offset = ComputeFibonacciSpiralDiskSampleClumped(i, invSampleCount, 0.5, sampleDistNorm);

        // 应用随机旋转避免规则条纹
        offset = float2(
            offset.x * rotationVec.x - offset.y * rotationVec.y,
            offset.x * rotationVec.y + offset.y * rotationVec.x
        );
        offset *= searchRadius;

        float2 sampleCoord = shadowCoord.xy + offset * shadowmapSize.xy;

        // 边界检查
        if (any(sampleCoord < 0.0) || any(sampleCoord > 1.0))
            continue;

        float shadowMapDepth = SAMPLE_TEXTURE2D_LOD(ShadowMap, sampler_ShadowMap, sampleCoord, 0).r;

        // 检测遮挡物（考虑Z方向）
        #if UNITY_REVERSED_Z
        if (shadowMapDepth > shadowCoord.z)
        #else
        if (shadowMapDepth < shadowCoord.z)
        #endif
        {
            avgBlockerDepth += shadowMapDepth;
            numBlockers += 1.0;
        }
    }

    if (numBlockers > 0.0)
        avgBlockerDepth /= numBlockers;

    return float2(avgBlockerDepth, numBlockers);
}

// PCF滤波
float PCSS_Filter(
    TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap),
    float3 shadowCoord,
    float filterRadius,
    float2 rotationVec,
    float4 shadowmapSize)
{
    float shadow = 0.0;
    int sampleCount = (int)_PCSSParams2.y;
    float invSampleCount = rcp((float)sampleCount);

    UNITY_LOOP
    for (int i = 0; i < sampleCount && i < DISK_SAMPLE_COUNT; i++)
    {
        float sampleDistNorm;
        float2 offset = ComputeFibonacciSpiralDiskSampleUniform(i, invSampleCount, 0.5 * invSampleCount, sampleDistNorm);

        // 应用随机旋转
        offset = float2(
            offset.x * rotationVec.x - offset.y * rotationVec.y,
            offset.x * rotationVec.y + offset.y * rotationVec.x
        );
        offset *= filterRadius;

        float3 sampleCoord = float3(shadowCoord.xy + offset * shadowmapSize.xy, shadowCoord.z);

        // 边界检查
        if (any(sampleCoord.xy < 0.0) || any(sampleCoord.xy > 1.0))
        {
            shadow += 1.0; // 边界外视为无阴影
            continue;
        }

        shadow += SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, sampleCoord);
    }

    return shadow * invSampleCount;
}

// 主PCSS函数
// ShadowMap: 用于点采样读取深度
// ShadowMapShadow: 用于硬件比较采样
float PCSS_MainLight(
    TEXTURE2D_PARAM(ShadowMap, sampler_point),
    TEXTURE2D_SHADOW_PARAM(ShadowMapShadow, sampler_shadow),
    float3 shadowCoord,
    float2 screenPos,
    float4 shadowmapSize,
    float receiverDepth)
{
    float lightAngularRadius = _PCSSParams.x;
    float blockerSearchRadius = _PCSSParams.y;
    float maxSoftness = _PCSSParams.z;

    // 获取随机旋转向量
    float2 rotationVec = GetRotationVectorTemporal(screenPos, _PCSSParams2.z);

    // Step 1: Blocker Search
    // 搜索半径基于blockerSearchRadius参数
    float searchRadiusTexels = blockerSearchRadius * shadowmapSize.x;
    float2 blockerResult = PCSS_BlockerSearch(
        TEXTURE2D_ARGS(ShadowMap, sampler_point),
        shadowCoord,
        searchRadiusTexels,
        rotationVec,
        shadowmapSize
    );

    // 无遮挡物，完全lit
    if (blockerResult.y < 1.0)
        return 1.0;

    // Step 2: Penumbra Estimation
    float avgBlockerDepth = blockerResult.x;

    #if UNITY_REVERSED_Z
    float blockerDistance = shadowCoord.z - avgBlockerDepth;
    #else
    float blockerDistance = avgBlockerDepth - shadowCoord.z;
    #endif

    // 防止除零
    blockerDistance = max(blockerDistance, 0.0001);

    // 基于相似三角形计算半影宽度
    // penumbraWidth = lightRadius * blockerDistance / blockerDepth
    float penumbraRatio = blockerDistance / max(avgBlockerDepth, 0.0001);

    // 将角半径转换为阴影图上的尺寸
    // 简化模型：假设光源在远处，使用角半径直接缩放
    float filterRadiusTexels = tan(lightAngularRadius) * penumbraRatio * shadowmapSize.x * receiverDepth;

    // 限制最大软度
    filterRadiusTexels = clamp(filterRadiusTexels, 1.0, maxSoftness);

    // Step 3: PCF Filter
    return PCSS_Filter(
        TEXTURE2D_SHADOW_ARGS(ShadowMapShadow, sampler_shadow),
        shadowCoord,
        filterRadiusTexels,
        rotationVec,
        shadowmapSize
    );
}

// 简化版PCSS（固定软度，仅做PCF）
float PCSS_Simple(
    TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap),
    float3 shadowCoord,
    float2 screenPos,
    float filterRadius,
    float4 shadowmapSize)
{
    float2 rotationVec = GetRotationVectorTemporal(screenPos, _PCSSParams2.z);

    return PCSS_Filter(
        TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap),
        shadowCoord,
        filterRadius,
        rotationVec,
        shadowmapSize
    );
}

#endif
