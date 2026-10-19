#ifndef TRAIN_STORM_WALL_INCLUDED
#define TRAIN_STORM_WALL_INCLUDED

#include "Assets/Shaders/Include/PixelNoise.hlsl"

float Storm_Quantize(float value, float steps)
{
    return steps >= 1.0 ? round(value * steps) / steps : value;
}

float Storm_Depth(float2 local, float time)
{
    float edge = PN_ValueNoise(float2(local.y / max(_EdgeScale, 1.0), time * _EdgeSpeed));
    return -local.x - (edge - 0.5) * 2.0 * _EdgeWobble;
}

float Storm_DebrisChance(float depth)
{
    if (depth < -_Spill)
        return 0.0;

    float chance = _DebrisAmount;
    if (depth < 0.0)
        chance *= saturate(1.0 + depth / max(_Spill, 1.0)) * 0.5;
    else if (depth > _WallBand)
        chance *= _DeepDensity;
    return chance;
}

bool Storm_ShapeHit(float2 d, float shape, float orientation)
{
    if (shape < 0.5)
        return abs(d.x) < 0.5 && abs(d.y) < 0.5;
    if (shape < 1.5)
        return d.x > -0.5 && d.x < 1.5 && d.y > -0.5 && d.y < 1.5;
    if (shape < 2.5)
        return abs(d.x) < 1.5 && abs(d.y) < 1.5;

    float halfLength = shape < 3.5 ? 1.0 : (shape < 4.5 ? 2.0 : 3.0);
    if (orientation < 0.5)
        return abs(d.y) < 0.5 && abs(d.x) <= halfLength;
    if (orientation < 1.5)
        return abs(d.x - d.y) < 0.5 && abs(d.x) <= max(halfLength - 1.0, 1.0);
    if (orientation < 2.5)
        return abs(d.x) < 0.5 && abs(d.y) <= halfLength;
    return abs(d.x + d.y) < 0.5 && abs(d.x) <= max(halfLength - 1.0, 1.0);
}

half4 Storm_DebrisLayer(float2 local, float time, float eddySize, float seed, float speedScale)
{
    eddySize = max(eddySize, 24.0);

    float2 shift = float2(seed, seed - floor(time * _Drift * speedScale));
    float2 p = local + shift;
    float2 cell = floor(p / eddySize);
    float2 jitter = (PN_Hash22(cell + seed) - 0.5) * eddySize * 0.2;
    float2 center = round(cell * eddySize + eddySize * 0.5 + jitter);

    float chance = Storm_DebrisChance(shift.x - center.x) * lerp(0.4, 1.3, PN_Hash12(cell + seed + 9.3));
    if (chance <= 0.0)
        return half4(0.0, 0.0, 0.0, 0.0);

    float2 d = p - center;
    float ringWidth = max(_RingWidth, 4.0);
    float maxRadius = eddySize * lerp(0.28, 0.4, PN_Hash12(cell + seed + 3.7)) - 4.0;
    float ring = floor(length(d) / ringWidth);
    float angle = atan2(d.y, d.x);
    float slotLength = max(_SlotLength, 4.0);

    for (int i = -1; i <= 1; i++)
    {
        float k = ring + i;
        float radius = (k + 0.5) * ringWidth;
        if (k < 1.0 || radius > maxRadius)
            continue;

        float slots = max(3.0, floor(6.2832 * radius / slotLength));
        float slotAngle = 6.2832 / slots;
        float ringHash = PN_Hash12(cell * 1.31 + float2(k, seed));
        float speed = _VortexSpeed * speedScale * lerp(1.4, 0.7, radius / max(maxRadius, 1.0)) * lerp(0.85, 1.15, ringHash);
        float turn = time * speed + ringHash * 6.2832;
        float firstSlot = floor((angle - turn) / slotAngle);

        for (int j = -1; j <= 1; j++)
        {
            float s = firstSlot + j;
            float slot = s - slots * floor(s / slots);
            float2 key = cell * 1.7 + float2(k * 13.1 + seed, slot * 7.3);
            float h = PN_Hash12(key);
            if (h > chance)
                continue;

            float2 rnd = PN_Hash22(key + 5.9);
            float theta = (s + 0.5 + (rnd.x - 0.5) * 0.4) * slotAngle + turn;
            float r = radius + floor(rnd.y * 3.0) - 1.0;
            float2 position = center + round(float2(cos(theta), sin(theta)) * r);

            float shape = floor(PN_Hash12(key + 2.9) * 6.0);
            float tumble = floor(time * _Tumble * rnd.y * 1.5 + h * 4.0);
            float stick = round((theta + 1.5708) / 0.7854) + tumble;
            float orientation = stick - 4.0 * floor(stick / 4.0);
            if (!Storm_ShapeHit(p - position, shape, orientation))
                continue;

            float tint = PN_Hash12(key + 6.1);
            return tint < 0.4 ? _DebrisColorA : (tint < 0.75 ? _DebrisColorB : _DebrisColorC);
        }
    }

    return half4(0.0, 0.0, 0.0, 0.0);
}

half4 Storm_Apply(float2 pixel, float2 uv, float3 positionWS)
{
    float sizeX = ddx(positionWS.x) / ddx(uv.x);
    float pixelsPerUnit = max(_PixelsPerUnit, 0.0001);
    float frontPixel = round((positionWS.x + (1.0 - uv.x) * sizeX) * pixelsPerUnit) - round(_FrontMargin);

    float time = Weather_Time();
    float2 local = float2(pixel.x - frontPixel, pixel.y);
    float depth = Storm_Depth(local, time);

    half4 debris = Storm_DebrisLayer(local, time, _FrontEddy, 0.0, 1.0);
    if (debris.a > 0.0)
        return debris;

    if (_LineStrength > 0.0 && abs(depth) < 0.5)
        return half4(_LineColor.rgb, _LineColor.a * _LineStrength);

    bool smooth = _Smooth > 0.5;
    float block = max(_Block, 1.0);
    float2 samplePoint = smooth ? local : floor(local / block) * block;
    float steps = smooth ? 0.0 : _Steps;

    float scale = max(_DustScale, 1.0);
    float2 flow = float2(time * _DustSwirl * scale, -time * _DustRise);
    float n = PN_Fbm((samplePoint + flow) / scale);
    float n2 = PN_Fbm((samplePoint - flow * 0.7 + 57.0) / (scale * 0.5));
    float turbulence = saturate(n * 0.65 + n2 * 0.35);

    float fade = max(_WallFade, 1.0);
    float ragged = (0.5 - turbulence) * 2.0 * _DustVariation * fade;
    float wall = saturate((depth + _DustOffset - ragged) / fade);
    float alpha = _DustAlpha * Storm_Quantize(wall, steps);
    if (alpha <= 0.0)
        return half4(0.0, 0.0, 0.0, 0.0);

    half3 color = lerp(_DustDark.rgb, _DustLight.rgb, Storm_Quantize(turbulence, steps));

    half4 back = Storm_DebrisLayer(local, time, _BackEddy, 31.0, 0.8);
    if (back.a > 0.0)
        color = lerp(color, back.rgb, _BackDim);

    return half4(color, alpha);
}

#endif
