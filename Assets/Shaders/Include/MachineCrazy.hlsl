#ifndef TRAIN_MACHINE_CRAZY_INCLUDED
#define TRAIN_MACHINE_CRAZY_INCLUDED

#include "Assets/Shaders/Include/PixelNoise.hlsl"
#include "Assets/Shaders/Include/SpriteTexel.hlsl"

float Crazy_Time()
{
    return _PreviewTime >= 0.0 ? _PreviewTime : _Time.y;
}

float3 Crazy_VertexOffset(float3 positionOS, float2 uv)
{
    float2 origin = ST_SpriteOriginWS(positionOS, uv, _PixelsPerUnit);
    float phase = origin.x * 0.0137 + origin.y * 0.0251;

    float frameRate = max(_FrameRate, 1.0);
    float frame = floor((Crazy_Time() + phase * 7.3) * frameRate);
    float t = frame / frameRate;

    float hop = round(abs(sin(t * _HopSpeed * PI)) * _HopHeight);

    float shake = 0.0;
    if (PN_Hash12(float2(frame, phase * 101.0)) < _ShakeChance)
        shake = PN_Hash12(float2(phase * 57.0, frame)) < 0.5 ? -1.0 : 1.0;

    return float3(shake, hop, 0.0) / max(_PixelsPerUnit, 0.0001);
}

half4 Crazy_Apply(float2 uv, float3 positionWS)
{
    float2 size = ST_Size();
    float2 texel = ST_TexelOf(uv, size);
    float2 worldTexel = floor(ST_TexelWorldCoord(uv, texel, size, positionWS));

    half4 baseColor = ST_SampleTexel(texel, size);
    if (baseColor.a < 0.01)
        return baseColor;

    if (ST_IsRim(texel, size))
    {
        float2 spriteSeed = worldTexel - texel;
        float frame = floor(Crazy_Time() * max(_FlickerRate, 1.0));
        float flicker = PN_Hash12(float2(frame, spriteSeed.x + spriteSeed.y * 3.1)) < 0.5 ? 1.0 : 0.45;
        return half4(lerp(baseColor.rgb, _RimColor.rgb, _RimStrength * flicker), baseColor.a);
    }

    half tone = sqrt(PN_Luma(baseColor.rgb));
    if (tone <= _KeepOutline)
        return baseColor;

    float t = saturate((tone - _ToneMin) / max(_ToneMax - _ToneMin, 0.01));
    half3 red = PN_Gradient3(t, _RedDeep.rgb, _RedMid.rgb, _RedLight.rgb);
    return half4(lerp(baseColor.rgb, red, _TintStrength), baseColor.a);
}

#endif
