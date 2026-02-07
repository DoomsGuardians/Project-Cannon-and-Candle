Shader "Hidden/PCSS/ScreenSpaceShadows"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // 深度纹理
        TEXTURE2D_X(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);

        // _MainLightShadowmapTexture 已在 Shadows.hlsl 中定义
        // 使用内联采样器 sampler_PointClamp 进行点采样

        // 逆VP矩阵用于深度重建世界坐标
        float4x4 _InverseViewProjectionMatrix;

        // PCSS参数
        // x: softness, y: blockerSearchRadius, z: maxFilterRadius, w: unused
        float4 _PCSSParams;
        // x: blockerSamples, y: filterSamples, z: frameIndex, w: unused
        float4 _PCSSParams2;

        // Poisson采样盘
        #define SAMPLE_COUNT 64
        static const float2 poissonDisk[SAMPLE_COUNT] = {
            float2(-0.5119625, -0.4827938),
            float2(-0.2171264, -0.4768726),
            float2(-0.7552931, -0.2426507),
            float2(-0.7136765, -0.4496614),
            float2(-0.5938849, -0.6895654),
            float2(-0.3148003, -0.7047654),
            float2(-0.4234620, -0.8710970),
            float2(-0.1259156, -0.8755529),
            float2(0.0821210, -0.9314062),
            float2(-0.0587183, -0.6512714),
            float2(0.3284700, -0.8890885),
            float2(0.1738460, -0.6835566),
            float2(0.4383390, -0.7233660),
            float2(0.5303614, -0.5765756),
            float2(0.5016390, -0.3929630),
            float2(0.6416712, -0.2465550),
            float2(0.3474689, -0.5253052),
            float2(0.1605300, -0.4814568),
            float2(0.0899292, -0.2615823),
            float2(-0.0819515, -0.3659318),
            float2(-0.2321420, -0.2203520),
            float2(-0.4048794, -0.2506730),
            float2(-0.5699694, -0.0545610),
            float2(-0.4423550, 0.1039290),
            float2(-0.3242220, 0.0410230),
            float2(-0.1604230, 0.0711420),
            float2(-0.1909928, 0.2451467),
            float2(-0.3516650, 0.3050720),
            float2(-0.5152266, 0.3055824),
            float2(-0.6542195, 0.1654920),
            float2(-0.7831282, 0.0276960),
            float2(-0.8241160, 0.2369280),
            float2(-0.9136420, 0.4081540),
            float2(-0.6837622, 0.4451142),
            float2(-0.5381640, 0.4766150),
            float2(-0.3793800, 0.4878460),
            float2(-0.2163670, 0.4334370),
            float2(-0.0494920, 0.4461340),
            float2(0.1113590, 0.4414500),
            float2(-0.0595850, 0.6093640),
            float2(0.0989470, 0.6008340),
            float2(0.2471830, 0.5450610),
            float2(0.3804040, 0.4816210),
            float2(0.5175390, 0.4389930),
            float2(0.6518940, 0.3761460),
            float2(0.7652550, 0.2693860),
            float2(0.8655200, 0.1390000),
            float2(0.7771780, 0.4166410),
            float2(0.8899010, 0.3003110),
            float2(0.9602430, 0.1549320),
            float2(0.9331750, 0.0151670),
            float2(0.8051280, -0.0971560),
            float2(0.6879430, 0.0174730),
            float2(0.5562340, 0.1451450),
            float2(0.4127150, 0.2471330),
            float2(0.2688950, 0.3415980),
            float2(0.1229210, 0.2519880),
            float2(-0.0197070, 0.2606630),
            float2(-0.0665460, 0.0802280),
            float2(0.0966530, 0.0772540),
            float2(0.2595950, 0.0573700),
            float2(0.4032390, 0.0681340),
            float2(0.5419610, 0.0177330),
            float2(0.6786430, -0.1182830)
        };

        // 随机旋转向量（用于采样抖动）
        float2 GetRotation(float2 screenPos, float frameIndex)
        {
            float noise = frac(52.9829189 * frac(dot(screenPos, float2(0.06711056, 0.00583715))) + frameIndex * 0.618033988749);
            float angle = noise * 6.28318530718;
            float s, c;
            sincos(angle, s, c);
            return float2(c, s);
        }

        // 从深度重建世界坐标
        float3 ReconstructWorldPos(float2 uv, float depth)
        {
            float4 posCS = float4(uv * 2.0 - 1.0, depth, 1.0);
            #if UNITY_UV_STARTS_AT_TOP
            posCS.y = -posCS.y;
            #endif
            float4 posWS = mul(_InverseViewProjectionMatrix, posCS);
            return posWS.xyz / posWS.w;
        }

        // 手动计算阴影坐标（支持级联阴影）
        float4 GetShadowCoord(float3 positionWS)
        {
            float3 fromCenter0 = positionWS - _CascadeShadowSplitSpheres0.xyz;
            float3 fromCenter1 = positionWS - _CascadeShadowSplitSpheres1.xyz;
            float3 fromCenter2 = positionWS - _CascadeShadowSplitSpheres2.xyz;
            float3 fromCenter3 = positionWS - _CascadeShadowSplitSpheres3.xyz;

            float4 distances2 = float4(
                dot(fromCenter0, fromCenter0),
                dot(fromCenter1, fromCenter1),
                dot(fromCenter2, fromCenter2),
                dot(fromCenter3, fromCenter3)
            );

            half4 weights = half4(distances2 < _CascadeShadowSplitSphereRadii);
            weights.yzw = saturate(weights.yzw - weights.xyz);

            half cascadeIndex = half(4.0) - dot(weights, half4(4, 3, 2, 1));

            return float4(mul(_MainLightWorldToShadow[(int)cascadeIndex], float4(positionWS, 1.0)).xyz, cascadeIndex);
        }

        // Blocker Search - 搜索遮挡物平均深度
        float2 FindBlocker(float3 shadowCoord, float searchRadius, float2 rotation)
        {
            float blockerSum = 0;
            float numBlockers = 0;

            int samples = (int)_PCSSParams2.x;
            samples = min(samples, SAMPLE_COUNT);

            UNITY_LOOP
            for (int i = 0; i < samples; i++)
            {
                float2 offset = poissonDisk[i];
                // 应用旋转
                offset = float2(
                    offset.x * rotation.x - offset.y * rotation.y,
                    offset.x * rotation.y + offset.y * rotation.x
                );
                offset *= searchRadius;

                float2 sampleUV = shadowCoord.xy + offset;

                // 边界检查
                if (sampleUV.x < 0 || sampleUV.x > 1 || sampleUV.y < 0 || sampleUV.y > 1)
                    continue;

                // 点采样阴影贴图深度
                float shadowMapDepth = SAMPLE_TEXTURE2D_LOD(_MainLightShadowmapTexture, sampler_PointClamp, sampleUV, 0).r;

                // 检测遮挡物
                #if UNITY_REVERSED_Z
                if (shadowMapDepth > shadowCoord.z)
                #else
                if (shadowMapDepth < shadowCoord.z)
                #endif
                {
                    blockerSum += shadowMapDepth;
                    numBlockers += 1.0;
                }
            }

            return float2(blockerSum / max(numBlockers, 0.0001), numBlockers);
        }

        // PCF滤波
        float PCF_Filter(float3 shadowCoord, float filterRadius, float2 rotation)
        {
            float shadow = 0;
            int samples = (int)_PCSSParams2.y;
            samples = min(samples, SAMPLE_COUNT);

            UNITY_LOOP
            for (int i = 0; i < samples; i++)
            {
                float2 offset = poissonDisk[i];
                // 应用旋转
                offset = float2(
                    offset.x * rotation.x - offset.y * rotation.y,
                    offset.x * rotation.y + offset.y * rotation.x
                );
                offset *= filterRadius;

                float3 sampleCoord = float3(shadowCoord.xy + offset, shadowCoord.z);

                // 边界检查
                if (sampleCoord.x < 0 || sampleCoord.x > 1 || sampleCoord.y < 0 || sampleCoord.y > 1)
                {
                    shadow += 1.0;
                    continue;
                }

                // 硬件比较采样
                shadow += SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_LinearClampCompare, sampleCoord);
            }

            return shadow / samples;
        }

        // PCSS主函数
        float PCSS(float3 shadowCoord, float2 screenPos)
        {
            float2 rotation = GetRotation(screenPos, _PCSSParams2.z);

            float softness = _PCSSParams.x;
            float searchRadius = _PCSSParams.y;
            float maxFilterRadius = _PCSSParams.z;

            // Step 1: Blocker Search - 搜索遮挡物
            float2 blocker = FindBlocker(shadowCoord, searchRadius, rotation);

            // 无遮挡物，返回完全光照
            if (blocker.y < 1.0)
                return 1.0;

            // Step 2: Penumbra Estimation - 估计半影大小
            float avgBlockerDepth = blocker.x;

            #if UNITY_REVERSED_Z
            // Reversed Z: 近处=1, 远处=0
            // blockerDepth > receiverDepth 表示遮挡物更近
            float blockerDistance = shadowCoord.z - avgBlockerDepth;
            #else
            float blockerDistance = avgBlockerDepth - shadowCoord.z;
            #endif

            // 确保距离为正
            blockerDistance = max(blockerDistance, 0.00001);

            // 半影宽度计算 - 使用改进的公式
            // softness 控制基础软度
            // 距离越远，半影越大（通过 blockerDistance 控制）
            // 将深度差值放大（因为深度在0-1范围内，差值很小）
            float penumbraScale = blockerDistance * softness;
            float filterRadius = clamp(penumbraScale, 0.002, maxFilterRadius);

            // Step 3: PCF Filter - 软阴影滤波
            return PCF_Filter(shadowCoord, filterRadius, rotation);
        }

        // 片段着色器 - 生成屏幕空间阴影纹理
        // 输出: R通道 = 阴影值 (0=阴影, 1=无阴影)
        half4 FragScreenSpaceShadow(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;

            // 采样深度
            float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;

            // 调试：输出深度值
            // return half4(depth, depth, depth, 1);

            // 天空盒检查 - 天空区域返回无阴影
            #if UNITY_REVERSED_Z
            if (depth < 0.0001)
                return half4(1, 0, 0, 1);
            #else
            if (depth > 0.9999)
                return half4(1, 0, 0, 1);
            #endif

            // 重建世界坐标
            float3 positionWS = ReconstructWorldPos(uv, depth);

            // 调试：输出世界坐标
            // return half4(frac(positionWS * 0.1), 1);

            // 获取阴影坐标
            float4 shadowCoord = GetShadowCoord(positionWS);
            float cascadeIndex = shadowCoord.w;

            // 级联索引检查 - 超出级联范围返回无阴影
            if (cascadeIndex >= 4.0)
                return half4(1, 0, 0, 1);

            // 计算级联边界渐变（避免硬边）
            float3 fromCenter = positionWS - _CascadeShadowSplitSpheres0.xyz;
            float distSq0 = dot(fromCenter, fromCenter);
            fromCenter = positionWS - _CascadeShadowSplitSpheres1.xyz;
            float distSq1 = dot(fromCenter, fromCenter);
            fromCenter = positionWS - _CascadeShadowSplitSpheres2.xyz;
            float distSq2 = dot(fromCenter, fromCenter);
            fromCenter = positionWS - _CascadeShadowSplitSpheres3.xyz;
            float distSq3 = dot(fromCenter, fromCenter);

            // 计算到当前级联边界的距离
            float4 distToEdge = float4(
                _CascadeShadowSplitSphereRadii.x - distSq0,
                _CascadeShadowSplitSphereRadii.y - distSq1,
                _CascadeShadowSplitSphereRadii.z - distSq2,
                _CascadeShadowSplitSphereRadii.w - distSq3
            );

            // 边界检查
            if (shadowCoord.x < 0.0 || shadowCoord.x > 1.0 ||
                shadowCoord.y < 0.0 || shadowCoord.y > 1.0)
                return half4(1, 0, 0, 1);

            // 调试：直接采样阴影贴图（硬阴影）
            float hardShadow = SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_LinearClampCompare, shadowCoord.xyz);
            // return half4(hardShadow, 0, 0, 1);

            // 计算PCSS阴影
            float shadow = PCSS(shadowCoord.xyz, input.positionCS.xy);

            // 应用阴影强度
            shadow = lerp(1.0, shadow, _MainLightShadowParams.x);

            // 输出到R通道（URP屏幕空间阴影格式）
            return half4(shadow, 0, 0, 1);
        }

        ENDHLSL

        // Pass 0: 生成屏幕空间阴影纹理
        Pass
        {
            Name "PCSS Screen Space Shadow"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragScreenSpaceShadow

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            ENDHLSL
        }
    }

    FallBack Off
}
