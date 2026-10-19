Shader "Train/Weather/Wind"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}

        _Intensity("Intensity", Range(0, 1)) = 1

        [Header(Streaks)]
        _StreakColor("Color", Color) = (0.92, 0.96, 1, 0.6)
        _Speed("Speed (px per sec)", Range(10, 800)) = 260
        _SpeedVariance("Speed Variance", Range(0, 0.9)) = 0.45
        _RowDensity("Rows With Streaks", Range(0, 1)) = 0.22
        _StreakChance("Streak Chance", Range(0, 1)) = 0.6
        _Spacing("Spacing (px)", Range(16, 512)) = 110
        _MinLength("Min Length (px)", Range(2, 64)) = 6
        _MaxLength("Max Length (px)", Range(2, 128)) = 30

        [Header(Surges)]
        _SurgeAmount("Surge Amount", Range(0, 0.9)) = 0.6
        _SurgeFrequency("Surge Frequency", Range(0, 5)) = 1.3

        [Header(Flutter)]
        _WaveAmplitude("Flutter (px)", Range(0, 2)) = 1
        _WaveFrequency("Flutter Frequency", Range(0, 1)) = 0.3
        _FlutterSpeed("Flutter Speed", Range(0, 20)) = 6

        [Header(Appear and vanish)]
        _Flicker("Changes Per Sec", Range(0, 10)) = 2
        _VanishChance("Vanish Chance", Range(0, 1)) = 0.3

        [Header(Debris)]
        _DebrisColor("Color A", Color) = (0.85, 0.80, 0.62, 0.95)
        _DebrisColor2("Color B", Color) = (0.55, 0.62, 0.35, 0.95)
        _DebrisSpeed("Speed (px per sec)", Range(10, 800)) = 320
        _DebrisCell("Spacing (px)", Range(8, 128)) = 36
        _DebrisChance("Amount", Range(0, 1)) = 0.3
        _DebrisBob("Bob Speed", Range(0, 30)) = 9
        _DebrisSpin("Spin (flips per sec)", Range(0, 30)) = 8

        [Header(Gust fronts)]
        _GustSize("Front Size (px)", Range(16, 512)) = 140
        _GustTravel("Front Speed (px per sec)", Range(0, 800)) = 320
        _GustThreshold("Front Threshold", Range(0, 1)) = 0.45
        _CalmBetweenFronts("Calm Between Fronts", Range(0, 1)) = 0.15

        [Header(Global gusts)]
        _GustSpeed("Gust Speed", Range(0, 5)) = 0.8
        _GustMin("Calm Strength", Range(0, 1)) = 0.5

        [Header(Misc)]
        _PixelsPerUnit("Pixels Per Unit", Float) = 1
        _PreviewTime("Preview Time (-1 = live)", Float) = -1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _Intensity;
            half4 _StreakColor;
            float _Speed;
            float _SpeedVariance;
            float _RowDensity;
            float _StreakChance;
            float _Spacing;
            float _MinLength;
            float _MaxLength;
            float _SurgeAmount;
            float _SurgeFrequency;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _FlutterSpeed;
            float _Flicker;
            float _VanishChance;
            half4 _DebrisColor;
            half4 _DebrisColor2;
            float _DebrisSpeed;
            float _DebrisCell;
            float _DebrisChance;
            float _DebrisBob;
            float _DebrisSpin;
            float _GustSize;
            float _GustTravel;
            float _GustThreshold;
            float _CalmBetweenFronts;
            float _GustSpeed;
            float _GustMin;
            float _PixelsPerUnit;
            float _PreviewTime;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex WeatherVertex
            #pragma fragment WeatherFragment
            #pragma multi_compile_instancing

            #include "Assets/Shaders/Include/WeatherPass.hlsl"
            #include "Assets/Shaders/Include/WeatherWind.hlsl"

            half4 WeatherEffect(float2 pixel, float2 uv, float3 positionWS) { return Wind_Apply(pixel, uv, positionWS); }
            ENDHLSL
        }
    }
}
