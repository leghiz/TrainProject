#ifndef TRAIN_SPRITE_EFFECT_LIT_PASS_INCLUDED
#define TRAIN_SPRITE_EFFECT_LIT_PASS_INCLUDED


struct Attributes
{
    COMMON_2D_INPUTS
    half4 color : COLOR;
    UNITY_SKINNED_VERTEX_INPUTS
};

struct Varyings
{
    COMMON_2D_LIT_OUTPUTS
    half4 color : COLOR;
    float3 effectPositionWS : TEXCOORD6;
};

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

half4 SpriteEffect(float2 uv, float3 positionWS);

Varyings SpriteEffectLitVertex(Attributes input)
{
    UNITY_SKINNED_VERTEX_COMPUTE(input);
    SetUpSpriteInstanceProperties();
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

    Varyings o = CommonLitVertex(input);
    o.color = input.color * _Color * unity_SpriteColor;
    o.effectPositionWS = TransformObjectToWorld(input.positionOS);
    return o;
}

// Эффект свойства, затем обычное 2D-освещение URP
half4 SpriteEffectLitFragment(Varyings input) : SV_Target
{
    const half4 main = SpriteEffect(input.uv, input.effectPositionWS) * input.color;
    const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
    const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

    SurfaceData2D surfaceData;
    InputData2D inputData;
    InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
    InitializeInputData(input.uv, input.lightingUV, inputData);

#if defined(DEBUG_DISPLAY)
    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
    surfaceData.normalWS = input.normalWS;
#endif

    return CombinedShapeLightShared(surfaceData, inputData);
}

#endif 
