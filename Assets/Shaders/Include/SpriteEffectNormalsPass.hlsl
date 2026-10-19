#ifndef TRAIN_SPRITE_EFFECT_NORMALS_PASS_INCLUDED
#define TRAIN_SPRITE_EFFECT_NORMALS_PASS_INCLUDED


struct Attributes
{
    COMMON_2D_NORMALS_INPUTS
    float4 color : COLOR;
    UNITY_SKINNED_VERTEX_INPUTS
};

struct Varyings
{
    COMMON_2D_NORMALS_OUTPUTS
    half4 color : COLOR;
};

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

#if defined(SPRITE_EFFECT_VERTEX_OFFSET)
float3 SpriteEffectVertexOffset(float3 positionOS, float2 uv);
#endif

Varyings SpriteEffectNormalsVertex(Attributes input)
{
    UNITY_SKINNED_VERTEX_COMPUTE(input);
    SetUpSpriteInstanceProperties();
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
#if defined(SPRITE_EFFECT_VERTEX_OFFSET)
    input.positionOS += SpriteEffectVertexOffset(input.positionOS, input.uv);
#endif

    Varyings o = CommonNormalsVertex(input);
    o.color = input.color * _Color * unity_SpriteColor;
    return o;
}

half4 SpriteEffectNormalsFragment(Varyings input) : SV_Target
{
    return CommonNormalsFragment(input, input.color);
}

#endif 
