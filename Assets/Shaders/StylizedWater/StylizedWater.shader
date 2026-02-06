Shader "Custom/StylizedWater"
{
    Properties
    {
        [Header(Density)]
        _DepthDensity ("Depth Density", Range(0, 1)) = 0.5
        _DistanceDensity ("Distance Density", Range(0, 1)) = 0.1

        [Header(Waves Normal Map)]
        _WaveNormalMap ("Wave Normal Map", 2D) = "bump" {}
        _WaveNormalScale ("Wave Normal Scale", Float) = 1.0
        _WaveNormalSpeed ("Wave Normal Speed", Float) = 0.1

        [Header(Base Colors)]
        [HDR] _ShallowColor ("Shallow Color", Color) = (0.3, 0.8, 0.9, 1)
        [HDR] _DeepColor ("Deep Color", Color) = (0.1, 0.3, 0.5, 1)
        [HDR] _FarColor ("Far Color", Color) = (0.05, 0.2, 0.4, 1)

        [Header(Reflection)]
        _ReflectionCubemap ("Reflection Cubemap", Cube) = "" {}
        _ReflectionContribution ("Reflection Contribution", Range(0, 1)) = 0.5
        _ReflectionDistortion ("Reflection Distortion", Range(0, 1)) = 0.1

        [Header(Subsurface Scattering)]
        [HDR] _SSSColor ("SSS Color", Color) = (0.2, 0.8, 0.4, 1)
        _SSSStrength ("SSS Strength", Range(0, 1)) = 0.3
        _SSSPower ("SSS Power", Range(1, 16)) = 4

        [Header(Foam)]
        _FoamTexture ("Foam Texture", 2D) = "white" {}
        _FoamNoiseTexture ("Foam Noise Texture", 2D) = "white" {}
        [HDR] _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamScale ("Foam Scale", Float) = 10
        _FoamSpeed ("Foam Speed", Float) = 0.1
        _FoamNoiseScale ("Foam Noise Scale", Float) = 20
        _FoamContribution ("Foam Contribution", Range(0, 1)) = 0.5

        [Header(Edge Foam)]
        _EdgeFoamDepth ("Edge Foam Depth", Float) = 1.0
        _EdgeFoamStrength ("Edge Foam Strength", Range(0, 1)) = 0.5

        [Header(Sun Specular)]
        [HDR] _SunSpecularColor ("Sun Specular Color", Color) = (1, 1, 1, 1)
        _SunSpecularExponent ("Sun Specular Exponent", Range(1, 512)) = 256

        [Header(Sparkles)]
        _SparkleScale ("Sparkle Scale", Float) = 50
        _SparkleSpeed ("Sparkle Speed", Float) = 0.2
        _SparkleExponent ("Sparkle Exponent", Range(1, 512)) = 128
        _SparkleContribution ("Sparkle Contribution", Range(0, 1)) = 0.3

        [Header(Vertex Waves Set 1)]
        _Wave1Direction ("Wave 1 Direction", Vector) = (1, 0, 0, 0)
        _Wave1Wavelength ("Wave 1 Wavelength", Float) = 5
        _Wave1Amplitude ("Wave 1 Amplitude", Float) = 0.1
        _Wave1Speed ("Wave 1 Speed", Float) = 1

        [Header(Vertex Waves Set 2)]
        _Wave2Direction ("Wave 2 Direction", Vector) = (0, 0, 1, 0)
        _Wave2Wavelength ("Wave 2 Wavelength", Float) = 3
        _Wave2Amplitude ("Wave 2 Amplitude", Float) = 0.05
        _Wave2Speed ("Wave 2 Speed", Float) = 1.5

        [Header(Refraction)]
        _RefractionStrength ("Refraction Strength", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StylizedWater"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            #include "WaterUtilities.hlsl"

            TEXTURE2D(_WaveNormalMap);
            SAMPLER(sampler_WaveNormalMap);
            TEXTURE2D(_FoamTexture);
            SAMPLER(sampler_FoamTexture);
            TEXTURE2D(_FoamNoiseTexture);
            SAMPLER(sampler_FoamNoiseTexture);
            TEXTURECUBE(_ReflectionCubemap);
            SAMPLER(sampler_ReflectionCubemap);

            CBUFFER_START(UnityPerMaterial)
                // Density
                float _DepthDensity;
                float _DistanceDensity;

                // Wave Normal
                float4 _WaveNormalMap_ST;
                float _WaveNormalScale;
                float _WaveNormalSpeed;

                // Colors
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FarColor;

                // Reflection
                float _ReflectionContribution;
                float _ReflectionDistortion;

                // SSS
                float4 _SSSColor;
                float _SSSStrength;
                float _SSSPower;

                // Foam
                float4 _FoamTexture_ST;
                float4 _FoamNoiseTexture_ST;
                float4 _FoamColor;
                float _FoamScale;
                float _FoamSpeed;
                float _FoamNoiseScale;
                float _FoamContribution;

                // Edge Foam
                float _EdgeFoamDepth;
                float _EdgeFoamStrength;

                // Sun Specular
                float4 _SunSpecularColor;
                float _SunSpecularExponent;

                // Sparkles
                float _SparkleScale;
                float _SparkleSpeed;
                float _SparkleExponent;
                float _SparkleContribution;

                // Vertex Waves
                float4 _Wave1Direction;
                float _Wave1Wavelength;
                float _Wave1Amplitude;
                float _Wave1Speed;
                float4 _Wave2Direction;
                float _Wave2Wavelength;
                float _Wave2Amplitude;
                float _Wave2Speed;

                // Refraction
                float _RefractionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                float fogFactor : TEXCOORD6;
                float3 viewDirWS : TEXCOORD7;
            };

            float GetWaveHeight(float3 worldPos)
            {
                float wave1 = SimpleWave(worldPos.xz, _Wave1Direction.xz, _Wave1Wavelength, _Wave1Amplitude, _Wave1Speed);
                float wave2 = SimpleWave(worldPos.xz, _Wave2Direction.xz, _Wave2Wavelength, _Wave2Amplitude, _Wave2Speed);
                return wave1 + wave2;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Apply vertex wave displacement
                float3 positionOS = input.positionOS.xyz;
                float3 positionWS = TransformObjectToWorld(positionOS);
                positionWS.y += GetWaveHeight(positionWS);

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);

                // Normal, tangent, bitangent
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;

                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Screen UV
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // Depth
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceDepth = input.screenPos.w;
                float depthDifference = sceneDepth - surfaceDepth;
                depthDifference = max(0, depthDifference);

                // Distance from camera
                float distanceFromCamera = length(input.positionWS - _WorldSpaceCameraPos);

                // Transmittance (depth and distance based)
                float depthTransmittance = exp(-depthDifference * _DepthDensity);
                float distanceTransmittance = exp(-distanceFromCamera * _DistanceDensity);

                // Wave normal (four-way chaos sampling)
                float3 waveNormal = MotionFourWayChaos(
                    _WaveNormalMap, sampler_WaveNormalMap,
                    input.uv * _WaveNormalScale,
                    _WaveNormalSpeed,
                    true
                );

                // Transform normal to world space
                float3x3 TBN = float3x3(
                    normalize(input.tangentWS),
                    normalize(input.bitangentWS),
                    normalize(input.normalWS)
                );
                float3 normalWS = normalize(mul(waveNormal, TBN));

                // View direction
                float3 viewDirWS = normalize(input.viewDirWS);

                // Fresnel
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 5.0);

                // Base color (gradient based on depth and distance)
                float3 baseColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, 1.0 - depthTransmittance);
                baseColor = lerp(baseColor, _FarColor.rgb, 1.0 - distanceTransmittance);

                // Refraction
                float2 refractionOffset = normalWS.xz * _RefractionStrength * depthTransmittance;
                float2 refractedUV = screenUV + refractionOffset;
                float3 refractionColor = SampleSceneColor(refractedUV);

                // Blend refraction with base color based on depth
                baseColor = lerp(refractionColor, baseColor, 1.0 - depthTransmittance * 0.5);

                // Reflection
                float3 reflectDir = reflect(-viewDirWS, normalWS);
                reflectDir = normalize(reflectDir + normalWS * _ReflectionDistortion);
                float3 reflectionColor = SAMPLE_TEXTURECUBE(_ReflectionCubemap, sampler_ReflectionCubemap, reflectDir).rgb;
                baseColor = lerp(baseColor, reflectionColor, fresnel * _ReflectionContribution);

                // Main light
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float3 lightColor = mainLight.color;

                // Subsurface scattering
                float sss = pow(saturate(dot(viewDirWS, -lightDir)), _SSSPower);
                sss *= (1.0 - depthTransmittance) * _SSSStrength;
                baseColor += _SSSColor.rgb * sss * lightColor;

                // Sun specular
                float3 halfDir = normalize(lightDir + viewDirWS);
                float sunSpec = pow(saturate(dot(normalWS, halfDir)), _SunSpecularExponent);
                baseColor += _SunSpecularColor.rgb * sunSpec * lightColor;

                // Sparkles
                float3 sparkleNormal = MotionFourWaySparkle(
                    _WaveNormalMap, sampler_WaveNormalMap,
                    input.uv * _SparkleScale,
                    _SparkleSpeed
                );
                float3 sparkleNormalWS = normalize(mul(sparkleNormal, TBN));
                float sparkle = pow(saturate(dot(sparkleNormalWS, halfDir)), _SparkleExponent);
                baseColor += _SunSpecularColor.rgb * sparkle * _SparkleContribution * lightColor;

                // Foam
                float2 foamUV = input.uv * _FoamScale;
                float foam = MotionFourWayChaosScalar(
                    _FoamTexture, sampler_FoamTexture,
                    foamUV,
                    _FoamSpeed
                );
                float foamNoise = SAMPLE_TEXTURE2D(_FoamNoiseTexture, sampler_FoamNoiseTexture, input.uv * _FoamNoiseScale).r;
                foam *= foamNoise;
                foam *= _FoamContribution;

                // Edge foam (depth-based)
                float edgeFoam = 1.0 - saturate(depthDifference / _EdgeFoamDepth);
                edgeFoam = pow(edgeFoam, 2.0) * _EdgeFoamStrength;
                foam = max(foam, edgeFoam);

                baseColor = lerp(baseColor, _FoamColor.rgb, saturate(foam));

                // Alpha
                float alpha = lerp(0.9, 1.0, 1.0 - depthTransmittance);
                alpha = max(alpha, foam);

                // Apply fog
                baseColor = MixFog(baseColor, input.fogFactor);

                return half4(baseColor, alpha);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "WaterUtilities.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _DepthDensity;
                float _DistanceDensity;
                float4 _WaveNormalMap_ST;
                float _WaveNormalScale;
                float _WaveNormalSpeed;
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FarColor;
                float _ReflectionContribution;
                float _ReflectionDistortion;
                float4 _SSSColor;
                float _SSSStrength;
                float _SSSPower;
                float4 _FoamTexture_ST;
                float4 _FoamNoiseTexture_ST;
                float4 _FoamColor;
                float _FoamScale;
                float _FoamSpeed;
                float _FoamNoiseScale;
                float _FoamContribution;
                float _EdgeFoamDepth;
                float _EdgeFoamStrength;
                float4 _SunSpecularColor;
                float _SunSpecularExponent;
                float _SparkleScale;
                float _SparkleSpeed;
                float _SparkleExponent;
                float _SparkleContribution;
                float4 _Wave1Direction;
                float _Wave1Wavelength;
                float _Wave1Amplitude;
                float _Wave1Speed;
                float4 _Wave2Direction;
                float _Wave2Wavelength;
                float _Wave2Amplitude;
                float _Wave2Speed;
                float _RefractionStrength;
            CBUFFER_END

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float GetWaveHeight(float3 worldPos)
            {
                float wave1 = SimpleWave(worldPos.xz, _Wave1Direction.xz, _Wave1Wavelength, _Wave1Amplitude, _Wave1Speed);
                float wave2 = SimpleWave(worldPos.xz, _Wave2Direction.xz, _Wave2Wavelength, _Wave2Amplitude, _Wave2Speed);
                return wave1 + wave2;
            }

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS.y += GetWaveHeight(positionWS);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "WaterUtilities.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _DepthDensity;
                float _DistanceDensity;
                float4 _WaveNormalMap_ST;
                float _WaveNormalScale;
                float _WaveNormalSpeed;
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FarColor;
                float _ReflectionContribution;
                float _ReflectionDistortion;
                float4 _SSSColor;
                float _SSSStrength;
                float _SSSPower;
                float4 _FoamTexture_ST;
                float4 _FoamNoiseTexture_ST;
                float4 _FoamColor;
                float _FoamScale;
                float _FoamSpeed;
                float _FoamNoiseScale;
                float _FoamContribution;
                float _EdgeFoamDepth;
                float _EdgeFoamStrength;
                float4 _SunSpecularColor;
                float _SunSpecularExponent;
                float _SparkleScale;
                float _SparkleSpeed;
                float _SparkleExponent;
                float _SparkleContribution;
                float4 _Wave1Direction;
                float _Wave1Wavelength;
                float _Wave1Amplitude;
                float _Wave1Speed;
                float4 _Wave2Direction;
                float _Wave2Wavelength;
                float _Wave2Amplitude;
                float _Wave2Speed;
                float _RefractionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float GetWaveHeight(float3 worldPos)
            {
                float wave1 = SimpleWave(worldPos.xz, _Wave1Direction.xz, _Wave1Wavelength, _Wave1Amplitude, _Wave1Speed);
                float wave2 = SimpleWave(worldPos.xz, _Wave2Direction.xz, _Wave2Wavelength, _Wave2Amplitude, _Wave2Speed);
                return wave1 + wave2;
            }

            Varyings DepthVert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS.y += GetWaveHeight(positionWS);

                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
