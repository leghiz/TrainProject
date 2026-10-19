Shader "Train/Machine/Waterproof"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [Header(Blue sheen)]
        _BlueDeep("Deep", Color) = (0.04, 0.12, 0.30, 1)
        _BlueMid("Mid", Color) = (0.15, 0.40, 0.85, 1)
        _BlueLight("Light", Color) = (0.60, 0.85, 1, 1)
        _TintStrength("Tint Strength", Range(0, 1)) = 0.35
        _ToneMin("Tone Min", Range(0, 1)) = 0.06
        _ToneMax("Tone Max", Range(0, 1)) = 0.5
        _KeepOutline("Keep Outline Below Tone", Range(0, 0.5)) = 0.12

        [Header(Top coating)]
        _CoatColor("Coat Color", Color) = (0.70, 0.90, 1, 1)
        _CoatStrength("Coat Strength", Range(0, 1)) = 0.75

        [Header(Drops)]
        _DropColor("Drop Color", Color) = (0.80, 0.95, 1, 1)
        _DropStrength("Drop Strength", Range(0, 1)) = 0.9
        _DropCell("Column Width (texels)", Range(3, 16)) = 6
        _DropPeriod("Slide Time (sec)", Range(0.2, 5)) = 1.4
        _DropChance("Chance Per Cycle", Range(0, 1)) = 0.4

        [Header(Misc)]
        _PreviewTime("Preview Time (-1 = live)", Float) = -1

        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half4 _BlueDeep;
            half4 _BlueMid;
            half4 _BlueLight;
            float _TintStrength;
            float _ToneMin;
            float _ToneMax;
            float _KeepOutline;
            half4 _CoatColor;
            float _CoatStrength;
            half4 _DropColor;
            float _DropStrength;
            float _DropCell;
            float _DropPeriod;
            float _DropChance;
            float _PreviewTime;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex SpriteEffectLitVertex
            #pragma fragment SpriteEffectLitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            #include "Assets/Shaders/Include/SpriteEffectLitPass.hlsl"
            #include "Assets/Shaders/Include/MachineWaterproof.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return Water_Apply(uv, positionWS); }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #pragma vertex SpriteEffectNormalsVertex
            #pragma fragment SpriteEffectNormalsFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            #include "Assets/Shaders/Include/SpriteEffectNormalsPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue" = "Transparent" "RenderType" = "Transparent" }

            HLSLPROGRAM
            #pragma vertex SpriteEffectUnlitVertex
            #pragma fragment SpriteEffectUnlitFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #include "Assets/Shaders/Include/SpriteEffectUnlitPass.hlsl"
            #include "Assets/Shaders/Include/MachineWaterproof.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return Water_Apply(uv, positionWS); }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
