// encoding: utf-8
// Lilliput URP post-process shader - Depth-based DoF for miniature effect

Shader "Hidden/LilliputURP"
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
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_CoCTex);
        SAMPLER(sampler_CoCTex);
        TEXTURE2D(_DoFTex);
        SAMPLER(sampler_DoFTex);

        float4 _MainTex_TexelSize;
        float _FocusDistance;   // 焦点距离 (世界单位)
        float _FocusRange;      // 焦点范围 (过渡区域)
        float _BokehRadius;     // 模糊半径
        float _GaussianStrength; // 高斯模糊强度

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

        // Calculate Circle of Confusion based on depth
        half CalculateCoC(float2 uv)
        {
            // Sample depth buffer
            float rawDepth = SampleSceneDepth(uv);
            float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

            // Calculate CoC based on distance from focus plane
            float coc = (linearDepth - _FocusDistance) / (_FocusRange + 0.001);

            // Absolute value for both near and far blur, clamped
            coc = clamp(coc, -1.0, 1.0);
            coc = abs(coc) * _BokehRadius;

            return coc;
        }

        // Signed CoC for near/far distinction (used in bokeh pass)
        half CalculateSignedCoC(float2 uv)
        {
            float rawDepth = SampleSceneDepth(uv);
            float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
            float coc = (linearDepth - _FocusDistance) / (_FocusRange + 0.001);
            coc = clamp(coc, -1.0, 1.0) * _BokehRadius;
            return coc;
        }
        ENDHLSL

        // Pass 0: Circle of Confusion
        Pass
        {
            Name "COC"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                half coc = CalculateSignedCoC(input.uv);
                return half4(coc, coc, coc, 1);
            }
            ENDHLSL
        }

        // Pass 1: PreFilter - Downsample with CoC
        Pass
        {
            Name "PreFilter"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                float4 offset = texelSize.xyxy * float2(-0.5, 0.5).xxyy;

                half3 c0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset.xy).rgb;
                half3 c1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset.zy).rgb;
                half3 c2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset.xw).rgb;
                half3 c3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset.zw).rgb;

                half coc0 = SAMPLE_TEXTURE2D(_CoCTex, sampler_CoCTex, input.uv + offset.xy).r;
                half coc1 = SAMPLE_TEXTURE2D(_CoCTex, sampler_CoCTex, input.uv + offset.zy).r;
                half coc2 = SAMPLE_TEXTURE2D(_CoCTex, sampler_CoCTex, input.uv + offset.xw).r;
                half coc3 = SAMPLE_TEXTURE2D(_CoCTex, sampler_CoCTex, input.uv + offset.zw).r;

                // Use max absolute CoC for downsampling
                half cocMin = min(min(coc0, coc1), min(coc2, coc3));
                half cocMax = max(max(coc0, coc1), max(coc2, coc3));
                half coc = abs(cocMax) > abs(cocMin) ? cocMax : cocMin;

                half3 color = (c0 + c1 + c2 + c3) * 0.25;

                return half4(color, coc);
            }
            ENDHLSL
        }

        // Pass 2: Bokeh blur (Enhanced with 48 samples)
        Pass
        {
            Name "Bokeh"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // Enhanced Bokeh kernel - 48 samples for smoother blur (4 rings)
            #define KERNEL_SIZE 48

            // Get kernel sample offset
            float2 GetKernelSample(int index)
            {
                // Ring 0 - center (1 sample)
                if (index == 0) return float2(0, 0);
                // Ring 1 - 6 samples (radius 0.2)
                if (index == 1) return float2(0.2, 0);
                if (index == 2) return float2(0.1, 0.1732);
                if (index == 3) return float2(-0.1, 0.1732);
                if (index == 4) return float2(-0.2, 0);
                if (index == 5) return float2(-0.1, -0.1732);
                if (index == 6) return float2(0.1, -0.1732);
                // Ring 2 - 12 samples (radius 0.4)
                if (index == 7) return float2(0.4, 0);
                if (index == 8) return float2(0.3464, 0.2);
                if (index == 9) return float2(0.2, 0.3464);
                if (index == 10) return float2(0, 0.4);
                if (index == 11) return float2(-0.2, 0.3464);
                if (index == 12) return float2(-0.3464, 0.2);
                if (index == 13) return float2(-0.4, 0);
                if (index == 14) return float2(-0.3464, -0.2);
                if (index == 15) return float2(-0.2, -0.3464);
                if (index == 16) return float2(0, -0.4);
                if (index == 17) return float2(0.2, -0.3464);
                if (index == 18) return float2(0.3464, -0.2);
                // Ring 3 - 12 samples (radius 0.6)
                if (index == 19) return float2(0.6, 0);
                if (index == 20) return float2(0.5196, 0.3);
                if (index == 21) return float2(0.3, 0.5196);
                if (index == 22) return float2(0, 0.6);
                if (index == 23) return float2(-0.3, 0.5196);
                if (index == 24) return float2(-0.5196, 0.3);
                if (index == 25) return float2(-0.6, 0);
                if (index == 26) return float2(-0.5196, -0.3);
                if (index == 27) return float2(-0.3, -0.5196);
                if (index == 28) return float2(0, -0.6);
                if (index == 29) return float2(0.3, -0.5196);
                if (index == 30) return float2(0.5196, -0.3);
                // Ring 4 - 16 samples (radius 1.0)
                if (index == 31) return float2(1.0, 0);
                if (index == 32) return float2(0.9239, 0.3827);
                if (index == 33) return float2(0.7071, 0.7071);
                if (index == 34) return float2(0.3827, 0.9239);
                if (index == 35) return float2(0, 1.0);
                if (index == 36) return float2(-0.3827, 0.9239);
                if (index == 37) return float2(-0.7071, 0.7071);
                if (index == 38) return float2(-0.9239, 0.3827);
                if (index == 39) return float2(-1.0, 0);
                if (index == 40) return float2(-0.9239, -0.3827);
                if (index == 41) return float2(-0.7071, -0.7071);
                if (index == 42) return float2(-0.3827, -0.9239);
                if (index == 43) return float2(0, -1.0);
                if (index == 44) return float2(0.3827, -0.9239);
                if (index == 45) return float2(0.7071, -0.7071);
                if (index == 46) return float2(0.9239, -0.3827);
                if (index == 47) return float2(0.8, 0.15);
                return float2(0, 0);
            }

            half Weigh(half coc, half radius)
            {
                return saturate((abs(coc) - radius + 2) / 2);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                // 根据当前分辨率与参考分辨率的比例缩放采样步长
                float scale = _MainTex_TexelSize.w / REFERENCE_HEIGHT;

                half4 centerSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half centerCoC = centerSample.a;
                half absCenterCoC = abs(centerCoC);

                // Early out if no blur needed
                if (absCenterCoC < 0.1)
                {
                    return centerSample;
                }

                half3 bgColor = 0, fgColor = 0;
                half bgWeight = 0, fgWeight = 0;

                for (int i = 0; i < KERNEL_SIZE; i++)
                {
                    float2 kernelSample = GetKernelSample(i);
                    float2 offset = kernelSample * absCenterCoC * texelSize * scale;
                    half4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset);
                    half radius = length(kernelSample) * absCenterCoC;

                    // Background (far) blur
                    half bgw = Weigh(max(0, s.a), radius);
                    bgColor += s.rgb * bgw;
                    bgWeight += bgw;

                    // Foreground (near) blur
                    half fgw = Weigh(-s.a, radius);
                    fgColor += s.rgb * fgw;
                    fgWeight += fgw;
                }

                bgColor *= 1.0 / (bgWeight + (bgWeight == 0));
                fgColor *= 1.0 / (fgWeight + (fgWeight == 0));

                // Blend foreground and background
                half bgfg = min(1, fgWeight * 3.14159265359 / KERNEL_SIZE);
                half3 color = lerp(bgColor, fgColor, bgfg);

                return half4(color, centerCoC);
            }
            ENDHLSL
        }

        // Pass 3: PostFilter - Tent filter for smoother result
        Pass
        {
            Name "PostFilter"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                float4 offset = texelSize.xyxy * float2(-0.5, 0.5).xxyy;

                half4 s0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset.xy);
                half4 s1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset.zy);
                half4 s2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset.xw);
                half4 s3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset.zw);

                return (s0 + s1 + s2 + s3) * 0.25;
            }
            ENDHLSL
        }

        // Pass 4: Combine - Blend blurred and sharp based on CoC
        Pass
        {
            Name "Combine"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half coc = SAMPLE_TEXTURE2D(_CoCTex, sampler_CoCTex, input.uv).r;
                half4 dof = SAMPLE_TEXTURE2D(_DoFTex, sampler_DoFTex, input.uv);

                // Smooth blend based on absolute CoC
                half absCoc = abs(coc);
                half blendFactor = smoothstep(0.1, _BokehRadius * 0.5, absCoc);
                half3 color = lerp(source.rgb, dof.rgb, blendFactor);

                return half4(color, source.a);
            }
            ENDHLSL
        }

        // Pass 5: Gaussian Blur Horizontal
        Pass
        {
            Name "GaussianH"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // 9-tap Gaussian weights
            static const float gaussWeights[9] = {
                0.0162162162, 0.0540540541, 0.1216216216, 0.1945945946, 0.2270270270,
                0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162
            };
            static const float gaussOffsets[9] = {
                -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
            };

            half4 Frag(Varyings input) : SV_Target
            {
                half coc = SAMPLE_TEXTURE2D(_CoCTex, sampler_CoCTex, input.uv).r;
                half absCoc = abs(coc);

                // Scale gaussian blur by CoC and gaussian strength
                // 根据当前分辨率与参考分辨率的比例缩放
                float scale = _MainTex_TexelSize.w / REFERENCE_HEIGHT;
                float blurScale = absCoc * _GaussianStrength * _MainTex_TexelSize.x * scale;

                half3 color = 0;
                for (int i = 0; i < 9; i++)
                {
                    float2 offset = float2(gaussOffsets[i] * blurScale, 0);
                    color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset).rgb * gaussWeights[i];
                }

                return half4(color, 1);
            }
            ENDHLSL
        }

        // Pass 6: Gaussian Blur Vertical
        Pass
        {
            Name "GaussianV"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // 9-tap Gaussian weights
            static const float gaussWeights[9] = {
                0.0162162162, 0.0540540541, 0.1216216216, 0.1945945946, 0.2270270270,
                0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162
            };
            static const float gaussOffsets[9] = {
                -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
            };

            half4 Frag(Varyings input) : SV_Target
            {
                half coc = SAMPLE_TEXTURE2D(_CoCTex, sampler_CoCTex, input.uv).r;
                half absCoc = abs(coc);

                // Scale gaussian blur by CoC and gaussian strength
                // 根据当前分辨率与参考分辨率的比例缩放
                float scale = _MainTex_TexelSize.w / REFERENCE_HEIGHT;
                float blurScale = absCoc * _GaussianStrength * _MainTex_TexelSize.y * scale;

                half3 color = 0;
                for (int i = 0; i < 9; i++)
                {
                    float2 offset = float2(0, gaussOffsets[i] * blurScale);
                    color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset).rgb * gaussWeights[i];
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
