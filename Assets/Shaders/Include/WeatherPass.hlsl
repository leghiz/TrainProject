#ifndef TRAIN_WEATHER_PASS_INCLUDED
#define TRAIN_WEATHER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half4 color : COLOR;
    UNITY_VERTEX_OUTPUT_STEREO
};

half4 WeatherEffect(float2 pixel, float2 uv, float3 positionWS);

float Weather_Time()
{
    return _PreviewTime >= 0.0 ? _PreviewTime : _Time.y;
}

Varyings WeatherVertex(Attributes input)
{
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionWS = TransformObjectToWorld(input.positionOS);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.uv = input.uv;
    o.color = input.color;
    return o;
}

half4 WeatherFragment(Varyings input) : SV_Target
{
    float2 pixel = floor(input.positionWS.xy * max(_PixelsPerUnit, 0.0001));
    half4 color = WeatherEffect(pixel, input.uv, input.positionWS);
    color.a *= input.color.a * _Intensity;
    return color;
}

#endif
