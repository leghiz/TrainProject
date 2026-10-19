#ifndef TRAIN_WEATHER_RAIN_INCLUDED
#define TRAIN_WEATHER_RAIN_INCLUDED

#include "Assets/Shaders/Include/PixelNoise.hlsl"

float Rain_Layer(float2 pixel, float time, float speed, float cellWidth, float cellHeight, float dropLength, float density, float seed)
{
    float column = floor(pixel.x - _Slope * pixel.y);
    float v = pixel.y + time * speed;

    cellWidth = max(cellWidth, 2.0);
    cellHeight = max(cellHeight, dropLength + 2.0);

    float c = floor(column / cellWidth);
    float offsetX = floor(PN_Hash12(float2(c, seed)) * cellWidth);
    if (abs(column - c * cellWidth - offsetX) > 0.5)
        return 0.0;

    float r = floor(v / cellHeight);
    if (PN_Hash12(float2(c + seed * 2.0, r * 1.3)) > density)
        return 0.0;

    float offsetY = floor(PN_Hash12(float2(c + 31.7, r + seed)) * (cellHeight - dropLength));
    float local = floor(v - r * cellHeight) - offsetY;
    if (local < 0.0 || local >= dropLength)
        return 0.0;

    return local < 0.5 ? 1.0 : 0.6;
}

half4 Rain_Apply(float2 pixel, float2 uv, float3 positionWS)
{
    float time = Weather_Time();

    float nearDrop = Rain_Layer(pixel, time, _NearSpeed, _NearCellWidth, _NearCellHeight, _NearLength, _NearDensity, 0.0);
    if (nearDrop > 0.0)
        return half4(_NearColor.rgb, _NearColor.a * nearDrop);

    float farDrop = Rain_Layer(pixel, time, _FarSpeed, _FarCellWidth, _FarCellHeight, _FarLength, _FarDensity, 17.0);
    if (farDrop > 0.0)
        return half4(_FarColor.rgb, _FarColor.a * farDrop);

    return _DimColor;
}

#endif
