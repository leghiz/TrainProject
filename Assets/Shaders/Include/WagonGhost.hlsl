#ifndef TRAIN_WAGON_GHOST_INCLUDED
#define TRAIN_WAGON_GHOST_INCLUDED

#include "Assets/Shaders/Include/PixelNoise.hlsl"
#include "Assets/Shaders/Include/SpriteTexel.hlsl"

// Свойство вагона «Призрачный»:
//  - тон вагона можно инвертировать: линии и детали светятся, панели тусклые (как Negative в Balatro);
//  - цвет и прозрачность разложены на несколько чистых ступеней — никакого шума;
//  - вагон растворяется книзу ступенями, как привидение;
//  - чистый светящийся край в 1 пиксель и лёгкое «дыхание» прозрачности.
// Подключать после прохода SpriteEffect*Pass.hlsl и CBUFFER шейдера Train/Wagon/Ghost.

float Ghost_Time()
{
    return _PreviewTime >= 0.0 ? _PreviewTime : _Time.y;
}

// Тексель на краю силуэта: прозрачный сосед по горизонтали или вертикали
bool Ghost_IsRim(float2 texel, float2 size)
{
    return ST_SampleTexel(texel + float2(1.0, 0.0), size).a < 0.5
        || ST_SampleTexel(texel - float2(1.0, 0.0), size).a < 0.5
        || ST_SampleTexel(texel + float2(0.0, 1.0), size).a < 0.5
        || ST_SampleTexel(texel - float2(0.0, 1.0), size).a < 0.5;
}

float Ghost_Quantize(float value, float steps)
{
    return steps >= 1.0 ? round(value * steps) / steps : value;
}

half3 Ghost_Gradient(float t)
{
    return t < 0.5
        ? lerp(_GhostDeep.rgb, _GhostMid.rgb, t * 2.0)
        : lerp(_GhostMid.rgb, _GhostLight.rgb, (t - 0.5) * 2.0);
}

// Цвет текселя спрайта со свойством «Призрачный» (без тинта SpriteRenderer)
half4 Ghost_Apply(float2 uv, float3 positionWS)
{
    float2 size = ST_Size();
    float2 texel = ST_TexelOf(uv, size);
    // Производные считаются до любых ветвлений
    float2 worldTexel = floor(ST_TexelWorldCoord(uv, texel, size, positionWS));

    half4 baseColor = ST_SampleTexel(texel, size);
    if (baseColor.a < 0.01)
        return baseColor;

    // Тон исходного пикселя -> 0..1, при инверсии тёмные линии становятся самыми светлыми
    half tone = sqrt(PN_Luma(baseColor.rgb));
    float t = saturate((tone - _ToneMin) / max(_ToneMax - _ToneMin, 0.01));
    t = lerp(t, 1.0 - t, _Invert);
    t = saturate((t - 0.5) * _Contrast + 0.5);
    t = Ghost_Quantize(t, _Steps);

    half3 color = Ghost_Gradient(t);
    // Светлое плотное, тёмное прозрачное: панели просвечивают, рисунок читается
    float alpha = lerp(_AlphaDark, _AlphaLight, t);

    // Растворение книзу ступенями
    float height = (texel.y + 0.5) / size.y;
    float fade = Ghost_Quantize(saturate(height / max(_FadeHeight, 0.01)), _FadeSteps);
    alpha *= lerp(_FadeMin, 1.0, fade);

    // Чистый светящийся край в 1 пиксель, тоже гаснет книзу
    if (Ghost_IsRim(texel, size))
    {
        color = lerp(color, _RimColor.rgb, _RimStrength);
        alpha = max(alpha, _RimAlpha * lerp(_FadeMin, 1.0, fade));
    }

    // Лёгкое «дыхание» прозрачности, волной вдоль поезда
    float pulse = 0.5 + 0.5 * sin(Ghost_Time() * _PulseSpeed - worldTexel.x * 0.03);
    alpha *= 1.0 - _PulseAmount * pulse;

    return half4(color, saturate(alpha) * baseColor.a);
}

#endif // TRAIN_WAGON_GHOST_INCLUDED
