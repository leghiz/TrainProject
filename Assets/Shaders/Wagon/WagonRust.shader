Shader "Train/Wagon/Rust"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [Header(Amount)]
        _RustAmount("Rust Amount", Range(0, 1)) = 0.55
        _RustSeed("Seed", Float) = 0

        [Header(Where rust grows)]
        _BlobSize("Blob Size (texels)", Range(1, 16)) = 6
        _EdgeRust("Silhouette Edges", Range(0, 1)) = 0.55
        _EdgeWidth("Edge Width (texels)", Range(1, 3)) = 2
        _SeamRust("Seams And Rivets", Range(0, 1)) = 0.35
        _BottomRust("Bottom", Range(0, 1)) = 0.35
        _StreakRust("Drips", Range(0, 1)) = 0.3

        [Header(Pixel look)]
        _Dither("Dither", Range(0, 1)) = 0.6
        _StainBand("Stain Band", Range(0, 0.6)) = 0.25
        _StainMix("Stain Strength", Range(0, 1)) = 0.55
        _PitChance("Pits", Range(0, 0.3)) = 0.06
        _KeepOutline("Keep Outline Below Tone", Range(0, 0.5)) = 0.12

        [Header(Palette)]
        _RustDark("Dark", Color) = (0.25, 0.12, 0.07, 1)
        _RustMid("Mid", Color) = (0.47, 0.22, 0.10, 1)
        _RustLight("Light", Color) = (0.70, 0.36, 0.15, 1)
        _RustHighlight("Highlight", Color) = (0.86, 0.55, 0.27, 1)
        _ToneMin("Tone Min", Range(0, 1)) = 0.06
        _ToneMax("Tone Max", Range(0, 1)) = 0.45
        _ToneJitter("Tone Jitter", Range(0, 0.5)) = 0.15

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
            float _RustAmount;
            float _RustSeed;
            float _BlobSize;
            float _EdgeRust;
            float _EdgeWidth;
            float _SeamRust;
            float _BottomRust;
            float _StreakRust;
            float _Dither;
            float _StainBand;
            float _StainMix;
            float _PitChance;
            float _KeepOutline;
            half4 _RustDark;
            half4 _RustMid;
            half4 _RustLight;
            half4 _RustHighlight;
            float _ToneMin;
            float _ToneMax;
            float _ToneJitter;
        CBUFFER_END
        ENDHLSL

        // Основной проход 2D Renderer: ржавчина, затем обычное 2D-освещение
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
            #include "Assets/Shaders/Include/WagonRust.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return Rust_Apply(uv); }
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
            #include "Assets/Shaders/Include/WagonRust.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return Rust_Apply(uv); }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
