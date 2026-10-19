#ifndef TRAIN_PIXEL_NOISE_INCLUDED
#define TRAIN_PIXEL_NOISE_INCLUDED


float PN_Hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}


float2 PN_Hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}


float PN_ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = PN_Hash12(i);
    float b = PN_Hash12(i + float2(1.0, 0.0));
    float c = PN_Hash12(i + float2(0.0, 1.0));
    float d = PN_Hash12(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}


float PN_Fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    [unroll]
    for (int i = 0; i < 3; i++)
    {
        value += amplitude * PN_ValueNoise(p);
        p = p * 2.03 + 17.1;
        amplitude *= 0.5;
    }
    return value / 0.875;
}


float PN_Bayer4(float2 texel)
{
    static const float bayer[16] =
    {
         0.0,  8.0,  2.0, 10.0,
        12.0,  4.0, 14.0,  6.0,
         3.0, 11.0,  1.0,  9.0,
        15.0,  7.0, 13.0,  5.0
    };
    uint2 q = uint2(max(texel, 0.0)) & 3u;
    return (bayer[q.y * 4u + q.x] + 0.5) / 16.0;
}

half PN_Luma(half3 color)
{
    return dot(color, half3(0.299h, 0.587h, 0.114h));
}

#endif 
