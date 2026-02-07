#ifndef SHADOW_SAMPLING_DISK_INCLUDED
#define SHADOW_SAMPLING_DISK_INCLUDED

#define DISK_SAMPLE_COUNT 64

// Fibonacci螺旋采样方向（预计算）
// 使用黄金角分布，提供均匀的圆盘采样
static const float2 fibonacciSpiralDirection[DISK_SAMPLE_COUNT] = {
    float2(0.000000, 1.000000),
    float2(0.850651, 0.525731),
    float2(0.934172, -0.356822),
    float2(0.233445, -0.972370),
    float2(-0.657801, -0.753198),
    float2(-0.996917, 0.078459),
    float2(-0.500000, 0.866025),
    float2(0.374607, 0.927184),
    float2(0.951057, 0.309017),
    float2(0.809017, -0.587785),
    float2(0.104528, -0.994522),
    float2(-0.669131, -0.743145),
    float2(-0.994522, -0.104528),
    float2(-0.587785, 0.809017),
    float2(0.309017, 0.951057),
    float2(0.927184, 0.374607),
    float2(0.866025, -0.500000),
    float2(0.078459, -0.996917),
    float2(-0.753198, -0.657801),
    float2(-0.972370, -0.233445),
    float2(-0.356822, 0.934172),
    float2(0.525731, 0.850651),
    float2(0.992709, 0.120537),
    float2(0.707107, -0.707107),
    float2(-0.120537, -0.992709),
    float2(-0.850651, -0.525731),
    float2(-0.934172, 0.356822),
    float2(-0.233445, 0.972370),
    float2(0.657801, 0.753198),
    float2(0.996917, -0.078459),
    float2(0.500000, -0.866025),
    float2(-0.374607, -0.927184),
    float2(-0.951057, -0.309017),
    float2(-0.809017, 0.587785),
    float2(-0.104528, 0.994522),
    float2(0.669131, 0.743145),
    float2(0.994522, 0.104528),
    float2(0.587785, -0.809017),
    float2(-0.309017, -0.951057),
    float2(-0.927184, -0.374607),
    float2(-0.866025, 0.500000),
    float2(-0.078459, 0.996917),
    float2(0.753198, 0.657801),
    float2(0.972370, 0.233445),
    float2(0.356822, -0.934172),
    float2(-0.525731, -0.850651),
    float2(-0.992709, -0.120537),
    float2(-0.707107, 0.707107),
    float2(0.120537, 0.992709),
    float2(0.850651, 0.525731),
    float2(0.934172, -0.356822),
    float2(0.233445, -0.972370),
    float2(-0.657801, -0.753198),
    float2(-0.996917, 0.078459),
    float2(-0.500000, 0.866025),
    float2(0.374607, 0.927184),
    float2(0.951057, 0.309017),
    float2(0.809017, -0.587785),
    float2(0.104528, -0.994522),
    float2(-0.669131, -0.743145),
    float2(-0.994522, -0.104528),
    float2(-0.587785, 0.809017),
    float2(0.309017, 0.951057),
    float2(0.927184, 0.374607)
};

// 计算均匀分布的采样偏移（用于PCF滤波）
// 采样点均匀分布在整个圆盘上
float2 ComputeFibonacciSpiralDiskSampleUniform(int sampleIndex, float sampleCountInverse,
    float sampleCountBias, out float sampleDistNorm)
{
    sampleDistNorm = sqrt((float)sampleIndex * sampleCountInverse + sampleCountBias);
    return fibonacciSpiralDirection[sampleIndex] * sampleDistNorm;
}

// 计算集中分布的采样偏移（用于BlockerSearch）
// 采样点集中在圆盘中心附近，通过clumpExponent控制集中程度
float2 ComputeFibonacciSpiralDiskSampleClumped(int sampleIndex, float sampleCountInverse,
    float clumpExponent, out float sampleDistNorm)
{
    sampleDistNorm = (float)sampleIndex * sampleCountInverse;
    sampleDistNorm = pow(sampleDistNorm, clumpExponent);
    return fibonacciSpiralDirection[sampleIndex] * sampleDistNorm;
}

// 获取随机旋转向量（基于屏幕位置的伪随机）
float2 GetRotationVector(float2 screenPos)
{
    // 使用交错梯度噪声生成伪随机角度
    float noise = frac(52.9829189 * frac(dot(screenPos, float2(0.06711056, 0.00583715))));
    float angle = noise * 6.28318530718; // 2 * PI
    float s, c;
    sincos(angle, s, c);
    return float2(c, s);
}

// 使用时域抖动的旋转向量（减少时域闪烁）
float2 GetRotationVectorTemporal(float2 screenPos, float frameIndex)
{
    float noise = frac(52.9829189 * frac(dot(screenPos, float2(0.06711056, 0.00583715))) + frameIndex * 0.618033988749);
    float angle = noise * 6.28318530718;
    float s, c;
    sincos(angle, s, c);
    return float2(c, s);
}

#endif
