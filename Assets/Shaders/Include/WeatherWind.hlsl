#ifndef TRAIN_WEATHER_WIND_INCLUDED
#define TRAIN_WEATHER_WIND_INCLUDED

#include "Assets/Shaders/Include/PixelNoise.hlsl"

float Wind_Gust(float time)
{
    float wave = 0.5 + 0.5 * sin(time * _GustSpeed);
    float stepped = round(wave * 2.0) / 2.0;
    return lerp(_GustMin, 1.0, stepped);
}

float Wind_GustFront(float2 pixel, float time)
{
    float n = PN_ValueNoise(float2((pixel.x + time * _GustTravel) / max(_GustSize, 1.0), pixel.y / max(_GustSize * 2.0, 1.0)));
    float front = saturate((n - _GustThreshold) / 0.15);
    return round(front * 2.0) / 2.0;
}

float Wind_Travel(float row, float time, float speed)
{
    float phase = PN_Hash12(float2(row, 11.7)) * 6.2832;
    float frequency = max(_SurgeFrequency, 0.01);
    return speed * (time - _SurgeAmount / frequency * cos(time * frequency + phase));
}

float Wind_StreakAt(float row, float x, float time, out float waveOffset)
{
    waveOffset = 0.0;
    if (PN_Hash12(float2(row, 7.13)) > _RowDensity)
        return 0.0;

    float speed = _Speed * lerp(1.0 - _SpeedVariance, 1.0 + _SpeedVariance, PN_Hash12(float2(row, 3.3)));
    float u = x + Wind_Travel(row, time, speed);

    float spacing = max(_Spacing, _MaxLength + 2.0);
    float s = floor(u / spacing);
    float streakSeed = PN_Hash12(float2(s, row));
    if (streakSeed > _StreakChance)
        return 0.0;

    float life = floor(time * _Flicker + streakSeed * 7.0);
    if (PN_Hash12(float2(s + 1.7, row + life * 0.37)) < _VanishChance)
        return 0.0;

    float streakLength = floor(lerp(_MinLength, _MaxLength, PN_Hash12(float2(row, s + 5.1))));
    float start = floor(PN_Hash12(float2(s + 9.2, row)) * (spacing - streakLength));
    float local = floor(u - s * spacing) - start;
    if (local < 0.0 || local >= streakLength)
        return 0.0;

    float phase = PN_Hash12(float2(s, row + 2.2)) * 6.2832;
    waveOffset = round(sin(local * _WaveFrequency + phase - time * _FlutterSpeed) * _WaveAmplitude);

    float t = local / max(streakLength, 1.0);
    return t < 0.34 ? 1.0 : (t < 0.67 ? 0.6 : 0.3);
}

half4 Wind_Debris(float2 pixel, float time)
{
    float cellWidth = max(_DebrisCell, 8.0);
    float cellHeight = max(round(_DebrisCell * 0.6), 6.0);

    float rowCell = floor(pixel.y / cellHeight);
    float speed = _DebrisSpeed * lerp(0.7, 1.3, PN_Hash12(float2(rowCell, 1.1)));
    float u = pixel.x + Wind_Travel(rowCell + 0.5, time, speed);

    float columnCell = floor(u / cellWidth);
    float seed = PN_Hash12(float2(columnCell, rowCell));
    if (seed > _DebrisChance)
        return half4(0.0, 0.0, 0.0, 0.0);

    float baseX = floor(PN_Hash12(float2(columnCell + 3.1, rowCell)) * (cellWidth - 3.0));
    float bobRange = min(2.0, floor((cellHeight - 4.0) * 0.5));
    float baseY = rowCell * cellHeight + 2.0 + bobRange + floor(PN_Hash12(float2(columnCell, rowCell + 5.3)) * max(cellHeight - 4.0 - bobRange * 2.0, 1.0));
    float y = baseY + round(sin(time * _DebrisBob + seed * 6.2832) * bobRange);

    float dx = floor(u - columnCell * cellWidth) - baseX;
    float dy = pixel.y - y;
    bool horizontal = fmod(floor(time * _DebrisSpin + seed * 4.0), 2.0) < 0.5;

    bool hit = horizontal
        ? (abs(dy) < 0.5 && dx > -0.5 && dx < 1.5)
        : (abs(dx) < 0.5 && dy > -0.5 && dy < 1.5);
    if (!hit)
        return half4(0.0, 0.0, 0.0, 0.0);

    return PN_Hash12(float2(seed, 8.8)) < 0.5 ? _DebrisColor : _DebrisColor2;
}

half4 Wind_Apply(float2 pixel, float2 uv, float3 positionWS)
{
    float time = Weather_Time();
    float strength = Wind_Gust(time) * lerp(_CalmBetweenFronts, 1.0, Wind_GustFront(pixel, time));

    half4 debris = Wind_Debris(pixel, time);
    if (debris.a > 0.0)
        return half4(debris.rgb, debris.a * lerp(0.4, 1.0, strength));

    float level = 0.0;
    [unroll]
    for (int k = -2; k <= 2; k++)
    {
        float waveOffset;
        float streak = Wind_StreakAt(pixel.y - k, pixel.x, time, waveOffset);
        if (streak > 0.0 && abs(waveOffset - k) < 0.5)
            level = max(level, streak);
    }

    return half4(_StreakColor.rgb, _StreakColor.a * level * strength);
}

#endif
