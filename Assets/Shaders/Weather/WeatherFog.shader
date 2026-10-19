Shader "Train/Weather/Fog"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}

        _Intensity("Intensity", Range(0, 1)) = 1

        [Header(Fog)]
        [ToggleUI] _Smooth("Smooth (no pixels)", Float) = 0
        _FogColor("Color", Color) = (0.78, 0.84, 0.88, 1)
        _Density("Density", Range(0, 1)) = 0.72
        _Variation("Variation", Range(0, 2)) = 0.7
        _Block("Block Size (px)", Range(1, 16)) = 4
        _NoiseScale("Cloud Size (px)", Range(4, 256)) = 48
        _DriftSpeed("Drift Speed (px per sec)", Range(0, 100)) = 6
        _Steps("Steps (0 = smooth)", Range(0, 8)) = 4

        [Header(Clear window)]
        _WindowSize("Window Radius (px)", Vector) = (60, 45, 0, 0)
        _WindowOffset("Window Offset (0..1 of layer)", Vector) = (0, 0, 0, 0)
        _WindowSoftness("Window Softness", Range(0.01, 2)) = 0.6
        _WindowNoise("Window Edge Raggedness", Range(0, 2)) = 0.8
        _WindowFog("Fog Inside Window", Range(0, 1)) = 0.1

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
            float _Smooth;
            half4 _FogColor;
            float _Density;
            float _Variation;
            float _Block;
            float _NoiseScale;
            float _DriftSpeed;
            float _Steps;
            float4 _WindowSize;
            float4 _WindowOffset;
            float _WindowSoftness;
            float _WindowNoise;
            float _WindowFog;
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
            #include "Assets/Shaders/Include/WeatherFog.hlsl"

            half4 WeatherEffect(float2 pixel, float2 uv, float3 positionWS) { return Fog_Apply(pixel, uv, positionWS); }
            ENDHLSL
        }
    }
}
