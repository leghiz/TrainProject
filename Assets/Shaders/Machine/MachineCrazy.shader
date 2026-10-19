Shader "Train/Machine/Crazy"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [Header(Red sheen)]
        _RedDeep("Deep", Color) = (0.28, 0.03, 0.03, 1)
        _RedMid("Mid", Color) = (0.78, 0.14, 0.08, 1)
        _RedLight("Light", Color) = (1, 0.55, 0.38, 1)
        _TintStrength("Tint Strength", Range(0, 1)) = 0.45
        _ToneMin("Tone Min", Range(0, 1)) = 0.06
        _ToneMax("Tone Max", Range(0, 1)) = 0.5
        _KeepOutline("Keep Outline Below Tone", Range(0, 0.5)) = 0.12

        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1, 0.25, 0.12, 1)
        _RimStrength("Rim Strength", Range(0, 1)) = 0.85
        _FlickerRate("Flicker Rate (per sec)", Range(1, 30)) = 12

        [Header(Hop)]
        _HopHeight("Hop Height (texels)", Range(0, 6)) = 2
        _HopSpeed("Hops Per Second", Range(0, 10)) = 3.5
        _FrameRate("Frame Rate", Range(1, 30)) = 12
        _ShakeChance("Shake Chance", Range(0, 1)) = 0.25
        _PixelsPerUnit("Pixels Per Unit", Float) = 1

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
            half4 _RedDeep;
            half4 _RedMid;
            half4 _RedLight;
            float _TintStrength;
            float _ToneMin;
            float _ToneMax;
            float _KeepOutline;
            half4 _RimColor;
            float _RimStrength;
            float _FlickerRate;
            float _HopHeight;
            float _HopSpeed;
            float _FrameRate;
            float _ShakeChance;
            float _PixelsPerUnit;
            float _PreviewTime;
        CBUFFER_END

        #define SPRITE_EFFECT_VERTEX_OFFSET
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
            #include "Assets/Shaders/Include/MachineCrazy.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return Crazy_Apply(uv, positionWS); }
            float3 SpriteEffectVertexOffset(float3 positionOS, float2 uv) { return Crazy_VertexOffset(positionOS, uv); }
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
            #include "Assets/Shaders/Include/MachineCrazy.hlsl"

            float3 SpriteEffectVertexOffset(float3 positionOS, float2 uv) { return Crazy_VertexOffset(positionOS, uv); }
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
            #include "Assets/Shaders/Include/MachineCrazy.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return Crazy_Apply(uv, positionWS); }
            float3 SpriteEffectVertexOffset(float3 positionOS, float2 uv) { return Crazy_VertexOffset(positionOS, uv); }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
