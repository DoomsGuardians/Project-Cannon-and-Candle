// encoding: utf-8
// GT (Gran Turismo) Tonemapping URP post-process shader
// GT 色调映射 - 来自 Gran Turismo 的色调映射曲线
// 参考: https://www.desmos.com/calculator/gslcdxvipg

Shader "Hidden/PostProcessing/GTTonemapping"
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
        float _Intensity;
        float _MaxBrightness;   // P - 最大亮度
        float _Contrast;        // a - 对比度/线性段斜率
        float _LinearStart;     // m - 线性段起点
        float _LinearLength;    // l - 线性段长度比例
        float _ToeStrength;     // c - 暗部对比度
        float _BlackTighten;    // b - 黑色偏移

        static const float E = 2.71828182845904523536;

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

        // Smoothstep 权重函数 W
        float W_func(float x, float e0, float e1)
        {
            if (x <= e0) return 0.0;
            if (x >= e1) return 1.0;
            float t = (x - e0) / (e1 - e0);
            return t * t * (3.0 - 2.0 * t);
        }

        // 线性权重函数 H
        float H_func(float x, float e0, float e1)
        {
            if (x <= e0) return 0.0;
            if (x >= e1) return 1.0;
            return (x - e0) / (e1 - e0);
        }

        // Gran Turismo Tonemapper
        // 曲线由三段组成: Toe (暗部), Linear (线性段), Shoulder (高光压缩)
        float GTTonemapper(float x)
        {
            float P = _MaxBrightness;   // 最大亮度
            float a = _Contrast;        // 线性段斜率
            float m = _LinearStart;     // 线性段起点
            float l = _LinearLength;    // 线性段长度
            float c = _ToeStrength;     // 暗部 gamma
            float b = _BlackTighten;    // 黑色偏移

            // 计算线性段参数
            float l0 = (P - m) * l / a;
            float L0 = m - m / a;
            float L1 = m + (1.0 - m) / a;

            // 线性段: L(x) = m + a * (x - m)
            float L_x = m + a * (x - m);

            // 暗部 Toe 段: T(x) = m * pow(x/m, c) + b
            float T_x = m * pow(x / m, c) + b;

            // 高光 Shoulder 段参数
            float S0 = m + l0;
            float S1 = m + a * l0;
            float C2 = a * P / (P - S1);

            // Shoulder 段: S(x) = P - (P - S1) * exp(-(C2 * (x - S0) / P))
            float S_x = P - (P - S1) * pow(E, -(C2 * (x - S0) / P));

            // 混合权重
            float w0_x = 1.0 - W_func(x, 0.0, m);           // Toe 权重
            float w2_x = H_func(x, m + l0, m + l0);         // Shoulder 权重
            float w1_x = 1.0 - w0_x - w2_x;                 // Linear 权重

            // 最终混合
            float f_x = T_x * w0_x + L_x * w1_x + S_x * w2_x;
            return f_x;
        }
        ENDHLSL

        // Pass 0: GT Tonemapping
        Pass
        {
            Name "GTTonemapping"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // 应用 GT Tonemapping 到每个通道
                half3 tonemapped;
                tonemapped.r = GTTonemapper(color.r);
                tonemapped.g = GTTonemapper(color.g);
                tonemapped.b = GTTonemapper(color.b);

                // 根据强度混合
                half3 finalColor = lerp(color.rgb, tonemapped, _Intensity);

                return half4(finalColor, color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
