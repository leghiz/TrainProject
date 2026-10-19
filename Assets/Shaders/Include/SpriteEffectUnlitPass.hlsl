#ifndef TRAIN_SPRITE_EFFECT_UNLIT_PASS_INCLUDED
#define TRAIN_SPRITE_EFFECT_UNLIT_PASS_INCLUDED


struct Attributes
{
    COMMON_2D_INPUTS
    half4 color : COLOR;
    UNITY_SKINNED_VERTEX_INPUTS
};

struct Varyings
{
    COMMON_2D_OUTPUTS
    half4 color : COLOR;
    float3 effectPositionWS : TEXCOORD6;
};

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

half4 SpriteEffect(float2 uv, float3 positionWS);

#if defined(SPRITE_EFFECT_VERTEX_OFFSET)
float3 SpriteEffectVertexOffset(float3 positionOS, float2 uv);
#endif

Varyings SpriteEffectUnlitVertex(Attributes input)
{
    UNITY_SKINNED_VERTEX_COMPUTE(input);
    SetUpSpriteInstanceProperties();
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
#if defined(SPRITE_EFFECT_VERTEX_OFFSET)
    input.positionOS += SpriteEffectVertexOffset(input.positionOS, input.uv);
#endif

    Varyings o = CommonUnlitVertex(input);
    o.color = input.color * _Color * unity_SpriteColor;
    o.effectPositionWS = TransformObjectToWorld(input.positionOS);
    return o;
}

half4 SpriteEffectUnlitFragment(Varyings input) : SV_Target
{
    return SpriteEffect(input.uv, input.effectPositionWS) * input.color;
}

#endif 
