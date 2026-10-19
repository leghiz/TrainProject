Shader "Train/UI/Text Shimmer"
{
    Properties
    {
        [HideInInspector] _MainTex("Font Atlas", 2D) = "white" {}
        [Enum(Plain,0,Glint,1,Rust,2,Ghost,3)] _Mode("Mode", Float) = 0
        _ColorA("Color A", Color) = (0.55, 0.55, 0.55, 1)
        _ColorB("Color B", Color) = (0.8, 0.8, 0.8, 1)
        _Speed("Speed", Float) = 1
        _Span("Span", Float) = 40
        _Width("Width", Float) = 3
        _AlphaMin("Alpha Min", Range(0, 1)) = 1
        _FaceColor("Face Color", Color) = (1, 1, 1, 1)
        _GradientScale("Gradient Scale", Float) = 5
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
                float _Mode;
                half4 _ColorA;
                half4 _ColorB;
                float _Speed;
                float _Span;
                float _Width;
                half _AlphaMin;
                half4 _FaceColor;
                float _GradientScale;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 pixel : TEXCOORD1;
                half4 color : COLOR;
            };

            float Shimmer_Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv.xy;
                output.pixel = input.positionOS.xy;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half distance = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                half edge = max(fwidth(distance) * 0.75, 0.0001);
                half alpha = smoothstep(0.5 - edge, 0.5 + edge, distance) * input.color.a;

                float2 p = floor(input.pixel);
                float t = _Time.y * _Speed;
                half mixAmount = 0.0;
                half fade = 1.0;

                if (_Mode > 0.5 && _Mode < 1.5)
                {
                    float sweep = frac(t * 0.35) * (_Span + 30.0) - 15.0 - _Span * 0.5;
                    float slant = p.x + p.y * 0.5;
                    mixAmount = abs(slant - sweep) < _Width ? 1.0 : 0.0;
                }
                else if (_Mode > 1.5 && _Mode < 2.5)
                {
                    float crust = Shimmer_Hash(p);
                    float flicker = Shimmer_Hash(p + floor(t) * 17.0);
                    mixAmount = max(step(0.78, crust) * 0.5, step(0.72, flicker));
                }
                else if (_Mode > 2.5)
                {
                    float wave = 0.5 + 0.5 * sin(p.x * 0.45 + p.y * 0.3 - t * 2.0);
                    mixAmount = floor(wave * 3.0 + 0.5) / 3.0;
                    float breath = 0.5 + 0.5 * sin(t * 1.5 + p.x * 0.2);
                    fade = lerp(_AlphaMin, 1.0, floor(breath * 3.0 + 0.5) / 3.0);
                }

                half3 rgb = lerp(_ColorA.rgb, _ColorB.rgb, mixAmount);
                return half4(rgb, alpha * fade * _ColorA.a);
            }
            ENDHLSL
        }
    }
}
