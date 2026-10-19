Shader "Train/UI/Radial Fill"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}

        _Progress("Progress", Range(0, 1)) = 0.5
        _Size("Size (px)", Float) = 9
        _FillColor("Fill", Color) = (1, 1, 1, 1)
        _BackColor("Back", Color) = (0.08, 0.08, 0.1, 0.65)
        _RimColor("Rim", Color) = (0, 0, 0, 0.9)
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

            CBUFFER_START(UnityPerMaterial)
                float _Progress;
                float _Size;
                half4 _FillColor;
                half4 _BackColor;
                half4 _RimColor;
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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float size = max(_Size, 1.0);
                float2 p = floor(input.uv * size) + 0.5 - size * 0.5;
                float radius = size * 0.5;
                float distance = length(p);

                if (distance > radius)
                    return half4(0.0, 0.0, 0.0, 0.0);

                half4 color;
                if (distance > radius - 1.0)
                {
                    color = _RimColor;
                }
                else
                {
                    float turn = atan2(p.x, p.y) / (2.0 * PI);
                    turn = turn < 0.0 ? turn + 1.0 : turn;
                    color = turn <= _Progress ? _FillColor : _BackColor;
                }

                color.a *= input.color.a;
                return color;
            }
            ENDHLSL
        }
    }
}
