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

        // Pass 2: Bokeh blur
        Pass
        {
            Name "Bokeh"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // Bokeh kernel - disc sampling pattern (golden angle spiral)
            static const int KERNEL_SIZE = 22;
            static const float2 kernel[KERNEL_SIZE] = {
                float2(0, 0),
                float2(0.53333336, 0),
                float2(0.3325279, 0.4169768),
                float2(-0.11867785, 0.5199616),
                float2(-0.48051673, 0.2314047),
                float2(-0.48051673, -0.23140468),
                float2(-0.11867763, -0.51996166),
                float2(0.33252785, -0.4169769),
                float2(1, 0),
                float2(0.90096885, 0.43388376),
                float2(0.6234898, 0.7818315),
                float2(0.22252098, 0.9749279),
                float2(-0.22252095, 0.9749279),
                float2(-0.62349, 0.7818314),
                float2(-0.90096885, 0.43388382),
                float2(-1, 0),
                float2(-0.90096885, -0.43388376),
                float2(-0.6234896, -0.7818316),
                float2(-0.22252055, -0.974928),
                float2(0.2225215, -0.9749278),
                float2(0.6234897, -0.7818316),
                float2(0.90096885, -0.43388376)
            };

            half Weigh(half coc, half radius)
            {
                return saturate((abs(coc) - radius + 2) / 2);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
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
                    float2 offset = kernel[i] * absCenterCoC * texelSize;
                    half4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset);
                    half radius = length(kernel[i]) * absCenterCoC;

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
    }

    Fallback Off
}
