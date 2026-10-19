Shader "Train/Weather/Rain"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}

        _Intensity("Intensity", Range(0, 1)) = 1
        _Slope("Slope (x per y)", Range(0, 1)) = 0.35

        [Header(Near drops)]
        _NearColor("Color", Color) = (0.75, 0.85, 1, 0.75)
        _NearSpeed("Speed (px per sec)", Range(10, 600)) = 170
        _NearCellWidth("Column Width (px)", Range(2, 32)) = 9
        _NearCellHeight("Cell Height (px)", Range(4, 128)) = 40
        _NearLength("Drop Length (px)", Range(1, 16)) = 5
        _NearDensity("Density", Range(0, 1)) = 0.55

        [Header(Far drops)]
        _FarColor("Color", Color) = (0.65, 0.75, 0.95, 0.35)
        _FarSpeed("Speed (px per sec)", Range(10, 600)) = 100
        _FarCellWidth("Column Width (px)", Range(2, 32)) = 6
        _FarCellHeight("Cell Height (px)", Range(4, 128)) = 30
        _FarLength("Drop Length (px)", Range(1, 16)) = 3
        _FarDensity("Density", Range(0, 1)) = 0.45

        [Header(Screen dim)]
        _DimColor("Dim Color", Color) = (0.05, 0.08, 0.16, 0.18)

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
            float _Slope;
            half4 _NearColor;
            float _NearSpeed;
            float _NearCellWidth;
            float _NearCellHeight;
            float _NearLength;
            float _NearDensity;
            half4 _FarColor;
            float _FarSpeed;
            float _FarCellWidth;
            float _FarCellHeight;
            float _FarLength;
            float _FarDensity;
            half4 _DimColor;
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
            #include "Assets/Shaders/Include/WeatherRain.hlsl"

            half4 WeatherEffect(float2 pixel, float2 uv, float3 positionWS) { return Rain_Apply(pixel, uv, positionWS); }
            ENDHLSL
        }
    }
}
