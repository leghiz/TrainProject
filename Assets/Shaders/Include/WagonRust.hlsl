#ifndef TRAIN_WAGON_RUST_INCLUDED
#define TRAIN_WAGON_RUST_INCLUDED

#include "Assets/Shaders/Include/PixelNoise.hlsl"
#include "Assets/Shaders/Include/SpriteTexel.hlsl"

// Ржавчина для свойства вагона «Ржавый».
// Подключать после прохода SpriteEffect*Pass.hlsl (там объявлена _MainTex) и CBUFFER шейдера Train/Wagon/Rust.
// Каждый тексель спрайта получает одно решение «ржавый / пятно / чистый»,
// поэтому ржавчина всегда совпадает с пиксельной сеткой арта.

half4 Rust_SampleTexel(float2 texel)
{
    return ST_SampleTexel(texel, ST_Size());
}

// 1 — тексель у самого края силуэта, 0 — дальше чем _EdgeWidth текселей от края
float Rust_EdgeFactor(float2 texel)
{
    static const float2 dirs[8] =
    {
        float2(1.0, 0.0), float2(-1.0, 0.0), float2(0.0, 1.0), float2(0.0, -1.0),
        float2(1.0, 1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(-1.0, -1.0)
    };

    float nearest = 4.0;
    [unroll]
    for (int r = 3; r >= 1; r--)
    {
        [unroll]
        for (int d = 0; d < 8; d++)
        {
            if (Rust_SampleTexel(texel + dirs[d] * r).a < 0.5)
                nearest = r;
        }
    }
    return saturate((_EdgeWidth + 1.0 - nearest) / max(_EdgeWidth, 1.0));
}

// Насколько тексель светлее соседей: ржавчина любит швы, стыки и заклёпки
float Rust_SeamFactor(float2 texel, half tone)
{
    static const float2 dirs[4] = { float2(1.0, 0.0), float2(-1.0, 0.0), float2(0.0, 1.0), float2(0.0, -1.0) };

    half darkest = tone;
    [unroll]
    for (int d = 0; d < 4; d++)
    {
        half4 neighbour = Rust_SampleTexel(texel + dirs[d]);
        if (neighbour.a >= 0.5)
            darkest = min(darkest, sqrt(PN_Luma(neighbour.rgb)));
    }
    // Слабые перепады (полосы досок) не считаем, только заметные швы и заклёпки
    return saturate((tone - darkest - 0.06) * 6.0);
}

half3 Rust_PaletteColor(half tone, float2 p)
{
    float t = saturate((tone - _ToneMin) / max(_ToneMax - _ToneMin, 0.01));
    t = saturate(t + (PN_Hash12(p + 91.7) - 0.5) * _ToneJitter);

    if (t < 0.25) return _RustDark.rgb;
    if (t < 0.5)  return _RustMid.rgb;
    if (t < 0.75) return _RustLight.rgb;
    return _RustHighlight.rgb;
}

// Цвет текселя спрайта с ржавчиной (без тинта SpriteRenderer)
half4 Rust_Apply(float2 uv)
{
    float2 size = ST_Size();
    float2 texel = ST_TexelOf(uv, size);
    half4 baseColor = ST_SampleTexel(texel, size);
    if (baseColor.a < 0.01)
        return baseColor;

    // Тон в перцептивной шкале: в линейном цветовом пространстве так проще подбирать пороги
    half tone = sqrt(PN_Luma(baseColor.rgb));
    if (tone <= _KeepOutline)
        return baseColor; // контур не трогаем, силуэт остаётся чётким

    float2 p = texel + _RustSeed * float2(17.13, 31.71);

    // Пятна: контрастная маска, чтобы между ними оставался чистый металл
    float blob   = smoothstep(0.42, 0.78, PN_Fbm(p / max(_BlobSize, 1.0)));
    float edge   = Rust_EdgeFactor(texel);
    float seam   = Rust_SeamFactor(texel, tone);
    float bottom = 1.0 - saturate((texel.y + 0.5) / size.y);
    bottom *= bottom;
    float streak = smoothstep(0.6, 0.9, PN_ValueNoise(float2(p.x, p.y * 0.18)));

    float rust = blob * 0.8
               + edge * _EdgeRust * 0.5
               + seam * _SeamRust * 0.5
               + bottom * _BottomRust * 0.5
               + streak * _StreakRust * 0.5;
    rust += (PN_Bayer4(texel) - 0.5) * _Dither * 0.3;

    float heavyThreshold = lerp(1.3, 0.1, _RustAmount);
    float stainThreshold = heavyThreshold - _StainBand;

    if (rust < stainThreshold)
        return baseColor;

    half3 rustColor = Rust_PaletteColor(tone, p);

    if (rust >= heavyThreshold)
    {
        if (PN_Hash12(p * 1.37 + 5.0) < _PitChance)
            rustColor = _RustDark.rgb * 0.7h; // раковины в металле
        return half4(rustColor, baseColor.a);
    }

    return half4(lerp(baseColor.rgb, rustColor, _StainMix), baseColor.a);
}

#endif // TRAIN_WAGON_RUST_INCLUDED
