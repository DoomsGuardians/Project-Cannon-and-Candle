#ifndef WATER_UTILITIES_INCLUDED
#define WATER_UTILITIES_INCLUDED

#define PI 3.1415926536

// Simple sine wave for vertex displacement
// Returns height offset based on position, direction, wavelength, amplitude, and speed
float SimpleWave(float2 position, float2 direction, float wavelength, float amplitude, float speed)
{
    direction = normalize(direction);
    float phase = dot(position, direction) * (2.0 * PI / wavelength);
    float time = _Time.y * speed * (2.0 * PI / wavelength);
    return amplitude * sin(phase + time);
}

// UV panning utility
float2 Panner(float2 uv, float2 direction, float speed)
{
    return uv + direction * speed * _Time.y;
}

// Four-way chaos sampling for animated normal maps
// Samples texture at 4 offset positions with different panning directions
float3 MotionFourWayChaos(
    TEXTURE2D_PARAM(tex, samplerTex),
    float2 uv,
    float speed,
    bool unpackNormal
)
{
    // Four directions for sampling
    float2 dir1 = float2(1.0, 0.0);
    float2 dir2 = float2(-1.0, 0.0);
    float2 dir3 = float2(0.0, 1.0);
    float2 dir4 = float2(0.0, -1.0);

    // Offsets for each sample
    float2 offset1 = float2(0.0, 0.0);
    float2 offset2 = float2(0.418, 0.355);
    float2 offset3 = float2(0.865, 0.148);
    float2 offset4 = float2(0.651, 0.752);

    // Sample with panning
    float2 uv1 = Panner(uv + offset1, dir1, speed);
    float2 uv2 = Panner(uv + offset2, dir2, speed * 0.8);
    float2 uv3 = Panner(uv + offset3, dir3, speed * 1.2);
    float2 uv4 = Panner(uv + offset4, dir4, speed * 0.9);

    float4 sample1 = SAMPLE_TEXTURE2D(tex, samplerTex, uv1);
    float4 sample2 = SAMPLE_TEXTURE2D(tex, samplerTex, uv2);
    float4 sample3 = SAMPLE_TEXTURE2D(tex, samplerTex, uv3);
    float4 sample4 = SAMPLE_TEXTURE2D(tex, samplerTex, uv4);

    if (unpackNormal)
    {
        // Unpack normal maps
        float3 n1 = UnpackNormal(sample1);
        float3 n2 = UnpackNormal(sample2);
        float3 n3 = UnpackNormal(sample3);
        float3 n4 = UnpackNormal(sample4);

        // Blend normals using Reoriented Normal Mapping
        float3 result = normalize(float3(
            n1.xy + n2.xy + n3.xy + n4.xy,
            n1.z * n2.z * n3.z * n4.z
        ));

        return result;
    }
    else
    {
        // Average color samples
        return (sample1.rgb + sample2.rgb + sample3.rgb + sample4.rgb) * 0.25;
    }
}

// Scalar version for foam/mask textures
float MotionFourWayChaosScalar(
    TEXTURE2D_PARAM(tex, samplerTex),
    float2 uv,
    float speed
)
{
    float2 dir1 = float2(1.0, 0.0);
    float2 dir2 = float2(-1.0, 0.0);
    float2 dir3 = float2(0.0, 1.0);
    float2 dir4 = float2(0.0, -1.0);

    float2 offset1 = float2(0.0, 0.0);
    float2 offset2 = float2(0.418, 0.355);
    float2 offset3 = float2(0.865, 0.148);
    float2 offset4 = float2(0.651, 0.752);

    float2 uv1 = Panner(uv + offset1, dir1, speed);
    float2 uv2 = Panner(uv + offset2, dir2, speed * 0.8);
    float2 uv3 = Panner(uv + offset3, dir3, speed * 1.2);
    float2 uv4 = Panner(uv + offset4, dir4, speed * 0.9);

    float sample1 = SAMPLE_TEXTURE2D(tex, samplerTex, uv1).r;
    float sample2 = SAMPLE_TEXTURE2D(tex, samplerTex, uv2).r;
    float sample3 = SAMPLE_TEXTURE2D(tex, samplerTex, uv3).r;
    float sample4 = SAMPLE_TEXTURE2D(tex, samplerTex, uv4).r;

    return (sample1 + sample2 + sample3 + sample4) * 0.25;
}

// Four-way sparkle sampling for high-frequency specular highlights
float3 MotionFourWaySparkle(
    TEXTURE2D_PARAM(tex, samplerTex),
    float2 uv,
    float speed
)
{
    // Different scale factors for each sample to create sparkle effect
    float scale1 = 1.0;
    float scale2 = 1.3;
    float scale3 = 0.7;
    float scale4 = 1.5;

    float2 dir1 = float2(0.7, 0.7);
    float2 dir2 = float2(-0.7, 0.7);
    float2 dir3 = float2(0.7, -0.7);
    float2 dir4 = float2(-0.7, -0.7);

    float2 uv1 = Panner(uv * scale1, dir1, speed);
    float2 uv2 = Panner(uv * scale2, dir2, speed * 1.1);
    float2 uv3 = Panner(uv * scale3, dir3, speed * 0.9);
    float2 uv4 = Panner(uv * scale4, dir4, speed * 1.2);

    float4 sample1 = SAMPLE_TEXTURE2D(tex, samplerTex, uv1);
    float4 sample2 = SAMPLE_TEXTURE2D(tex, samplerTex, uv2);
    float4 sample3 = SAMPLE_TEXTURE2D(tex, samplerTex, uv3);
    float4 sample4 = SAMPLE_TEXTURE2D(tex, samplerTex, uv4);

    float3 n1 = UnpackNormal(sample1);
    float3 n2 = UnpackNormal(sample2);
    float3 n3 = UnpackNormal(sample3);
    float3 n4 = UnpackNormal(sample4);

    // Multiply normals for sharper sparkle effect
    float3 result = normalize(float3(
        n1.x * n2.x * n3.x * n4.x * 4.0,
        n1.y * n2.y * n3.y * n4.y * 4.0,
        n1.z * n2.z
    ));

    return result;
}

// Fresnel calculation
float Fresnel(float3 normal, float3 viewDir, float power)
{
    return pow(1.0 - saturate(dot(normal, viewDir)), power);
}

// Depth-based color interpolation
float3 DepthGradient(float3 shallow, float3 deep, float3 far, float depthFactor, float distanceFactor)
{
    float3 color = lerp(shallow, deep, depthFactor);
    color = lerp(color, far, distanceFactor);
    return color;
}

#endif // WATER_UTILITIES_INCLUDED
