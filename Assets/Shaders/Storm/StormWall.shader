Shader "Train/Storm/Wall"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}

        _Intensity("Intensity", Range(0, 1)) = 1

        [Header(Front)]
        _EdgeWobble("Edge Wobble (px)", Range(0, 40)) = 10
        _EdgeScale("Edge Wave Size (px)", Range(4, 200)) = 40
        _EdgeSpeed("Edge Wave Speed", Range(0, 10)) = 1.5
        _LineColor("Line Color", Color) = (1, 0.9, 0.7, 1)
        _LineStrength("Line Strength", Range(0, 1)) = 0

        [Header(Dust)]
        [ToggleUI] _Smooth("Smooth (no pixels)", Float) = 0
        _DustDark("Dark", Color) = (0.11, 0.08, 0.07, 1)
        _DustLight("Light", Color) = (0.30, 0.23, 0.17, 1)
        _DustAlpha("Max Opacity", Range(0, 1)) = 1
        _WallFade("Fade In Depth (px)", Range(1, 300)) = 18
        _DustScale("Cloud Size (px)", Range(4, 200)) = 40
        _DustRise("Rise Speed (px per sec)", Range(0, 200)) = 25
        _DustSwirl("Swirl Speed", Range(0, 3)) = 0.4
        _DustVariation("Edge Raggedness", Range(0, 1)) = 0.5
        _DustOffset("Shift Toward Line (px)", Range(-100, 100)) = 0
        _Block("Block Size (px)", Range(1, 8)) = 2
        _Steps("Steps (0 = smooth)", Range(0, 8)) = 4

        [Header(Debris)]
        _DebrisColorA("Color A", Color) = (0.62, 0.40, 0.22, 1)
        _DebrisColorB("Color B", Color) = (0.52, 0.52, 0.56, 1)
        _DebrisColorC("Color C", Color) = (0.82, 0.72, 0.52, 1)
        _DebrisAmount("Amount", Range(0, 1)) = 0.75
        _DeepDensity("Amount Deep Inside", Range(0, 1)) = 0.45
        _WallBand("Dense Wall Width (px)", Range(0, 300)) = 50
        _Spill("Fly Ahead (px)", Range(0, 100)) = 24
        _BackDim("Back Visibility", Range(0, 1)) = 0.55

        [Header(Vortices)]
        _FrontEddy("Front Vortex Size (px)", Range(24, 256)) = 88
        _BackEddy("Back Vortex Size (px)", Range(24, 256)) = 60
        _RingWidth("Ring Spacing (px)", Range(4, 24)) = 7
        _SlotLength("Spacing Along Ring (px)", Range(4, 40)) = 9
        _VortexSpeed("Spin Speed (rad per sec, sign = direction)", Range(-6, 6)) = 1.6
        _Tumble("Tumble (flips per sec)", Range(0, 20)) = 1.5
        _Drift("Drift Up (px per sec)", Range(-100, 100)) = 6

        [Header(Misc)]
        _FrontMargin("Quad Past Line (px)", Float) = 64
        _PixelsPerUnit("Pixels Per Unit", Float) = 1
        _PreviewTime("Preview Time (-1 = live)", Float) = -1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _Intensity;
            float _EdgeWobble;
            float _EdgeScale;
            float _EdgeSpeed;
            half4 _LineColor;
            float _LineStrength;
            float _Smooth;
            half4 _DustDark;
            half4 _DustLight;
            float _DustAlpha;
            float _WallFade;
            float _DustScale;
            float _DustRise;
            float _DustSwirl;
            float _DustVariation;
            float _DustOffset;
            float _Block;
            float _Steps;
            half4 _DebrisColorA;
            half4 _DebrisColorB;
            half4 _DebrisColorC;
            float _DebrisAmount;
            float _DeepDensity;
            float _WallBand;
            float _Spill;
            float _BackDim;
            float _FrontEddy;
            float _BackEddy;
            float _RingWidth;
            float _SlotLength;
            float _VortexSpeed;
            float _Tumble;
            float _Drift;
            float _FrontMargin;
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
            #include "Assets/Shaders/Include/StormWall.hlsl"

            half4 WeatherEffect(float2 pixel, float2 uv, float3 positionWS) { return Storm_Apply(pixel, uv, positionWS); }
            ENDHLSL
        }
    }
}
