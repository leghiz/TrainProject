Shader "Train/Wagon/New"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [Header(Fresh paint)]
        _Saturation("Saturation Boost", Range(0, 1)) = 0.2
        _Brightness("Brightness Boost", Range(0, 0.5)) = 0.08
        _KeepOutline("Keep Outline Below Tone", Range(0, 0.5)) = 0.12

        [Header(Shine sweep)]
        _ShineColor("Shine Color", Color) = (1, 0.97, 0.88, 1)
        _ShineStrength("Shine Strength", Range(0, 1)) = 0.65
        _ShineWidth("Shine Width (texels)", Range(1, 12)) = 4
        _ShineSlope("Shine Slope", Range(0, 2)) = 0.7
        _ShineSecondary("Second Stripe", Range(0, 1)) = 0.5
        _ShineSpacing("Spacing Between Sweeps (texels)", Range(32, 1024)) = 240
        _ShinePeriod("Sweep Period (sec)", Range(0.5, 10)) = 3.5

        [Header(Sparkles)]
        _SparkleColor("Sparkle Color", Color) = (1, 1, 1, 1)
        _SparkleCell("Cell Size (texels)", Range(6, 32)) = 14
        _SparkleChance("Chance Per Cycle", Range(0, 1)) = 0.35
        _SparklePeriod("Period (sec)", Range(0.2, 8)) = 2.2
        _SparkleLife("Life (sec)", Range(0.05, 2)) = 0.45

        [Header(Misc)]
        _NewSeed("Seed", Float) = 0
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
            float _Saturation;
            float _Brightness;
            float _KeepOutline;
            half4 _ShineColor;
            float _ShineStrength;
            float _ShineWidth;
            float _ShineSlope;
            float _ShineSecondary;
            float _ShineSpacing;
            float _ShinePeriod;
            half4 _SparkleColor;
            float _SparkleCell;
            float _SparkleChance;
            float _SparklePeriod;
            float _SparkleLife;
            float _NewSeed;
            float _PreviewTime;
        CBUFFER_END
        ENDHLSL

        // Основной проход 2D Renderer: эффект «Новый», затем обычное 2D-освещение
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
            #include "Assets/Shaders/Include/WagonNew.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return New_Apply(uv, positionWS); }
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
            #include "Assets/Shaders/Include/WagonNew.hlsl"

            half4 SpriteEffect(float2 uv, float3 positionWS) { return New_Apply(uv, positionWS); }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
