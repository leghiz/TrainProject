Shader "Train/Wagon/Ghost"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [Header(Ghost color)]
        _GhostDeep("Deep", Color) = (0.02, 0.16, 0.18, 1)
        _GhostMid("Mid", Color) = (0.10, 0.55, 0.52, 1)
        _GhostLight("Light", Color) = (0.70, 1, 0.92, 1)
        _ToneMin("Tone Min", Range(0, 1)) = 0.06
        _ToneMax("Tone Max", Range(0, 1)) = 0.45
        _Invert("Invert Tone (lines glow)", Range(0, 1)) = 0
        _Contrast("Contrast", Range(0.5, 3)) = 1.4
        _Steps("Color Steps (0 = smooth)", Range(0, 8)) = 4

        [Header(Transparency)]
        _AlphaDark("Alpha Of Dark Parts", Range(0, 1)) = 0.2
        _AlphaLight("Alpha Of Light Parts", Range(0, 1)) = 0.95
        _FadeHeight("Fade Height (0..1 from bottom)", Range(0.05, 1)) = 0.55
        _FadeMin("Alpha At Bottom", Range(0, 1)) = 0.1
        _FadeSteps("Fade Steps (0 = smooth)", Range(0, 8)) = 4

        [Header(Rim)]
        _RimColor("Rim Color", Color) = (0.80, 1, 0.95, 1)
        _RimStrength("Rim Strength", Range(0, 1)) = 0.9
        _RimAlpha("Rim Alpha", Range(0, 1)) = 0.95

        [Header(Breathing)]
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.15
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 1.5

        [Header(Misc)]
        _PreviewTime("Preview Time (-1 = live)", Float) = -1

        // Как у стандартного Sprite-Lit-Default, чтобы материал вёл себя так же
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

        // Все свойства — в UnityPerMaterial, как у Sprite-Lit-Default: так работает SRP Batcher
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half4 _GhostDeep;
            half4 _GhostMid;
            half4 _GhostLight;
            float _ToneMin;
            float _ToneMax;
            float _Invert;
            float _Contrast;
            float _Steps;
            float _AlphaDark;
            float _AlphaLight;
            float _FadeHeight;
            float _FadeMin;
            float _FadeSteps;
            half4 _RimColor;
            float _RimStrength;
            float _RimAlpha;
            float _PulseAmount;
            float _PulseSpeed;
            float _PreviewTime;
        CBUFFER_END
        ENDHLSL

        // Основной проход 2D Renderer: эффект «Призрачный», затем обычное 2D-освещение
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
            #include "Assets/Shaders/Include/WagonGhost.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return Ghost_Apply(uv, positionWS); }
            ENDHLSL
        }

        // Нормали для 2D-света с картами нормалей
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

        // Запасной проход для обычного (не 2D) рендерера URP
        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue" = "Transparent" "RenderType" = "Transparent" }

            HLSLPROGRAM
            #pragma vertex SpriteEffectUnlitVertex
            #pragma fragment SpriteEffectUnlitFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #include "Assets/Shaders/Include/SpriteEffectUnlitPass.hlsl"
            #include "Assets/Shaders/Include/WagonGhost.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return Ghost_Apply(uv, positionWS); }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
