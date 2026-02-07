// encoding: utf-8
// Kuwahara Filter URP post-process shader - Oil painting style effect
// 桑原滤镜 - 通过计算四个象限的方差，选择方差最小的象限平均色，产生油画效果

Shader "Hidden/PostProcessing/Kuwahara"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        float4 _MainTex_TexelSize;
        int _Radius;

        // 参考分辨率高度，用于保持不同屏幕尺寸下效果一致
        static const float REFERENCE_HEIGHT = 1080.0;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            return output;
        }

        // 计算一个象限的平均色和方差
        void CalculateQuadrant(float2 uv, float2 direction, out half3 mean, out half variance)
        {
            half3 colorSum = 0;
            half3 colorSqSum = 0;
            int sampleCount = _Radius * _Radius;

            // 根据当前分辨率与参考分辨率的比例缩放采样步长
            // 保证在不同屏幕尺寸下采样的屏幕空间区域一致
            float scale = _MainTex_TexelSize.w / REFERENCE_HEIGHT;

            for (int x = 0; x < _Radius; x++)
            {
                for (int y = 0; y < _Radius; y++)
                {
                    float2 offset = float2(x + 1, y + 1) * direction * _MainTex_TexelSize.xy * scale;
                    half3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset).rgb;
                    colorSum += color;
                    colorSqSum += color * color;
                }
            }

            mean = colorSum / sampleCount;
            // 方差 = E[X^2] - E[X]^2
            half3 var = colorSqSum / sampleCount - mean * mean;
            variance = var.r + var.g + var.b;
        }
        ENDHLSL

        // Pass 0: Kuwahara Filter
        Pass
        {
            Name "Kuwahara"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                half3 mean[4];
                half variance[4];

                // 右上象限 (direction: +x, +y)
                CalculateQuadrant(input.uv, float2(1, 1), mean[0], variance[0]);

                // 左上象限 (direction: -x, +y)
                CalculateQuadrant(input.uv, float2(-1, 1), mean[1], variance[1]);

                // 右下象限 (direction: +x, -y)
                CalculateQuadrant(input.uv, float2(1, -1), mean[2], variance[2]);

                // 左下象限 (direction: -x, -y)
                CalculateQuadrant(input.uv, float2(-1, -1), mean[3], variance[3]);

                // 找到方差最小的象限
                half minVariance = variance[0];
                half3 finalColor = mean[0];

                [unroll]
                for (int i = 1; i < 4; i++)
                {
                    if (variance[i] < minVariance)
                    {
                        minVariance = variance[i];
                        finalColor = mean[i];
                    }
                }

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
