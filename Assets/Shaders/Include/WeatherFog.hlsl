#ifndef TRAIN_WEATHER_FOG_INCLUDED
#define TRAIN_WEATHER_FOG_INCLUDED

#include "Assets/Shaders/Include/PixelNoise.hlsl"

float Fog_Quantize(float value, float steps)
{
    return steps >= 1.0 ? round(value * steps) / steps : value;
}

half4 Fog_Apply(float2 pixel, float2 uv, float3 positionWS)
{
    float2 sizeWS = float2(ddx(positionWS.x) / ddx(uv.x), ddy(positionWS.y) / ddy(uv.y));

    bool smooth = _Smooth > 0.5;
    float pixelsPerUnit = max(_PixelsPerUnit, 0.0001);
    float block = max(_Block, 1.0);
    float2 samplePoint = smooth ? positionWS.xy * pixelsPerUnit : floor(pixel / block) * block;
    float steps = smooth ? 0.0 : _Steps;

    float time = Weather_Time();
    float2 drift = float2(time * _DriftSpeed, 0.0);

    float scale = max(_NoiseScale, 1.0);
    float n1 = PN_Fbm((samplePoint + drift) / scale);
    float n2 = PN_Fbm((samplePoint - drift * 0.6 + 113.0) / (scale * 0.6));
    float density = saturate(_Density + (n1 * 0.6 + n2 * 0.4 - 0.5) * _Variation);

    float2 centerWS = positionWS.xy - (uv - 0.5 - _WindowOffset.xy) * sizeWS;
    float2 centerPoint = centerWS * pixelsPerUnit;
    if (!smooth)
        centerPoint = round(centerPoint);

    float2 radius = max(_WindowSize.xy, 1.0);
    float distance = length((samplePoint - centerPoint) / radius) + (n1 - 0.5) * _WindowNoise;
    float open = saturate((distance - 1.0) / max(_WindowSoftness, 0.01));
    open = smooth ? smoothstep(0.0, 1.0, open) : Fog_Quantize(open, steps);
    density *= lerp(_WindowFog, 1.0, open);

    density = Fog_Quantize(density, steps);
    return half4(_FogColor.rgb, _FogColor.a * density);
}

#endif
