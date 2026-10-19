#ifndef TRAIN_MACHINE_WATERPROOF_INCLUDED
#define TRAIN_MACHINE_WATERPROOF_INCLUDED

#include "Assets/Shaders/Include/PixelNoise.hlsl"
#include "Assets/Shaders/Include/SpriteTexel.hlsl"

float Water_Time()
{
    return _PreviewTime >= 0.0 ? _PreviewTime : _Time.y;
}

float Water_DropLevel(float2 texel, float2 worldTexel, float2 size, float time)
{
    float2 spriteSeed = worldTexel - texel;
    float cellSize = max(_DropCell, 3.0);
    float cell = floor(worldTexel.x / cellSize);
    float seed = spriteSeed.y * 0.137;

    float dropX = cell * cellSize + 1.0 + floor(PN_Hash12(float2(cell, 4.1 + seed)) * (cellSize - 2.0));
    if (abs(worldTexel.x - dropX) > 0.5)
        return 0.0;

    float t = time / max(_DropPeriod, 0.1) + PN_Hash12(float2(cell, 9.7 + seed));
    float cycle = floor(t);
    if (PN_Hash12(float2(cell + seed, cycle)) > _DropChance)
        return 0.0;

    float head = floor(lerp(size.y + 1.0, -3.0, t - cycle));
    float d = texel.y - head;
    if (abs(d) < 0.5)
        return 1.0;
    if (d > 0.5 && d < 1.5)
        return 0.55;
    if (d > 1.5 && d < 2.5)
        return 0.25;
    return 0.0;
}

half4 Water_Apply(float2 uv, float3 positionWS)
{
    float2 size = ST_Size();
    float2 texel = ST_TexelOf(uv, size);
    float2 worldTexel = floor(ST_TexelWorldCoord(uv, texel, size, positionWS));

    half4 baseColor = ST_SampleTexel(texel, size);
    if (baseColor.a < 0.01)
        return baseColor;

    half tone = sqrt(PN_Luma(baseColor.rgb));
    half3 color = baseColor.rgb;

    if (ST_IsTopEdge(texel, size))
    {
        color = lerp(color, _CoatColor.rgb, _CoatStrength);
    }
    else if (tone > _KeepOutline)
    {
        float t = saturate((tone - _ToneMin) / max(_ToneMax - _ToneMin, 0.01));
        color = lerp(color, PN_Gradient3(t, _BlueDeep.rgb, _BlueMid.rgb, _BlueLight.rgb), _TintStrength);
    }

    float drop = Water_DropLevel(texel, worldTexel, size, Water_Time());
    color = lerp(color, _DropColor.rgb, drop * _DropStrength);

    return half4(color, baseColor.a);
}

#endif
