#ifndef TRAIN_SPRITE_TEXEL_INCLUDED
#define TRAIN_SPRITE_TEXEL_INCLUDED


float2 ST_Size()
{
    uint width, height;
    _MainTex.GetDimensions(width, height);
    return max(float2(width, height), 1.0);
}

float2 ST_TexelOf(float2 uv, float2 size)
{
    return floor(uv * size);
}


float2 ST_TexelWorldCoord(float2 uv, float2 texel, float2 size, float3 positionWS)
{
    float2 t = uv * size;
    float2 dtdx = ddx(t);
    float2 dtdy = ddy(t);
    float2 dwdx = ddx(positionWS.xy);
    float2 dwdy = ddy(positionWS.xy);

    float det = dtdx.x * dtdy.y - dtdy.x * dtdx.y;
    if (abs(det) < 1e-8)
        return positionWS.xy;

    float2 worldPerTexelX = (dwdx * dtdy.y - dwdy * dtdx.y) / det;
    float2 worldPerTexelY = (dwdy * dtdx.x - dwdx * dtdy.x) / det;

    float2 d = (texel + 0.5) - t;
    float2 centerWS = positionWS.xy + worldPerTexelX * d.x + worldPerTexelY * d.y;

    return centerWS / max(length(worldPerTexelX), 1e-5);
}


half4 ST_SampleTexel(float2 texel, float2 size)
{
    if (texel.x < 0.0 || texel.y < 0.0 || texel.x >= size.x || texel.y >= size.y)
        return half4(0.0, 0.0, 0.0, 0.0);
    return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, (texel + 0.5) / size, 0);
}

#endif 
