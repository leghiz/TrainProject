#ifndef TRAIN_WAGON_NEW_INCLUDED
#define TRAIN_WAGON_NEW_INCLUDED

#include "Assets/Shaders/Include/PixelNoise.hlsl"
#include "Assets/Shaders/Include/SpriteTexel.hlsl"



float New_Time()
{
    return _PreviewTime >= 0.0 ? _PreviewTime : _Time.y;
}

float New_ShineLevel(float2 worldTexel, float time)
{

    float s = worldTexel.x - worldTexel.y * _ShineSlope;
    float spacing = max(_ShineSpacing, _ShineWidth * 4.0);

    float q = s / spacing - time / max(_ShinePeriod, 0.1);
    float f = frac(q);
    float d = min(f, 1.0 - f) * spacing;

    float level = 0.0;
    if (d < _ShineWidth * 0.5)
        level = 1.0;
    else if (d < _ShineWidth)
        level = 0.5;

    float f2 = frac(q + _ShineWidth * 1.8 / spacing);
    if (min(f2, 1.0 - f2) * spacing < 0.75)
        level = max(level, _ShineSecondary);

    return level;
}


float New_SparkleLevel(float2 worldTexel, float time)
{
    float cellSize = max(_SparkleCell, 6.0);
    float2 cell = floor(worldTexel / cellSize);
    float2 seed = cell + _NewSeed * 7.31;

    float period = max(_SparklePeriod, 0.1);
    float t = time + PN_Hash12(seed + 3.7) * period;
    float cycle = floor(t / period);
    float local = t - cycle * period;
    float life = max(_SparkleLife, 0.05);

    if (local > life)
        return 0.0;
    if (PN_Hash12(seed + cycle * 1.618) > _SparkleChance)
        return 0.0;


    float2 center = cell * cellSize + 2.0 + floor(PN_Hash22(seed + cycle * 2.399) * (cellSize - 4.0));

    float k = local / life;
    float pulse = 1.0 - abs(k * 2.0 - 1.0); // 0 -> 1 -> 0
    float arm = round(pulse * 2.0);

    float2 d = abs(worldTexel - center);
    if (d.x < 0.5 && d.y < 0.5)
        return pulse > 0.15 ? 1.0 : 0.0;
    if ((d.x < 0.5 && d.y <= arm) || (d.y < 0.5 && d.x <= arm))
        return 0.6;
    return 0.0;
}


half4 New_Apply(float2 uv, float3 positionWS)
{
    float2 size = ST_Size();
    float2 texel = ST_TexelOf(uv, size);

    float2 worldTexel = floor(ST_TexelWorldCoord(uv, texel, size, positionWS));

    half4 baseColor = ST_SampleTexel(texel, size);
    if (baseColor.a < 0.01)
        return baseColor;

    half luma = PN_Luma(baseColor.rgb);
    if (sqrt(luma) <= _KeepOutline)
        return baseColor; 


    half3 color = lerp(luma.xxx, baseColor.rgb, 1.0 + _Saturation) * (1.0 + _Brightness);

    float time = New_Time();

    float shine = New_ShineLevel(worldTexel, time);
    color = lerp(color, _ShineColor.rgb, shine * _ShineStrength);

    float sparkle = New_SparkleLevel(worldTexel, time);
    color = lerp(color, _SparkleColor.rgb, sparkle);

    return half4(max(color, 0.0), baseColor.a);
}

#endif 
