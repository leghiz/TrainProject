Shader "Train/UI/Outline"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            static const float Pad = 1.0;

            float2 Outline_Size()
            {
                uint width, height;
                _MainTex.GetDimensions(width, height);
                return float2(max(width, 1u), max(height, 1u));
            }

            half Outline_Alpha(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0))
                    return 0.0;
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).a;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 direction = sign(input.uv - 0.5);
                float3 positionOS = input.positionOS + float3(direction * Pad, 0.0);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = input.uv + direction * Pad / Outline_Size();
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texel = 1.0 / Outline_Size();
                float2 uv = input.uv;

                if (Outline_Alpha(uv) > 0.5)
                    return half4(0.0, 0.0, 0.0, 0.0);

                half near = max(max(Outline_Alpha(uv + float2(texel.x, 0.0)), Outline_Alpha(uv - float2(texel.x, 0.0))),
                                max(Outline_Alpha(uv + float2(0.0, texel.y)), Outline_Alpha(uv - float2(0.0, texel.y))));

                return near > 0.5 ? input.color : half4(0.0, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
}
