// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#ifndef SURFACE_LIGHT_FX
#define SURFACE_LIGHT_FX

float4 Diffuse3_Bloom1[8];
float4 Emissive3_Wrap1[8];
float4 SpecCol3_Pow1[8];
float4 Aniso2_EnvInt1_Unused1[8];
float4 Bump_Tile1_Int1_Unused2[8];

texture BumpDetail;
texture DirtMap;

sampler2D BumpDetailSampler =
sampler_state
{
    Texture = <BumpDetail>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = WRAP;
    AddressV = WRAP;
};

sampler2D DirtMapSampler =
sampler_state
{
    Texture = <DirtMap>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = Clamp;
    AddressV = Clamp;
};


float MaterialIndex(float3 rgbSelector)
{
    const float3 SelectorMult = float3(4.1f, 2.1f, 1.1f);
    const float3 SelectorDot = float3(1.0f, 1.0f, 1.0f);

    return dot(rgbSelector * SelectorMult, SelectorDot);
}

float4 DiffuseColorLU(float idx)
{
    return Diffuse3_Bloom1[idx];
//    return float4(Diffuse3_Bloom1[idx].xyz, 1.0f);
}
float BloomLU(float idx)
{
    return Diffuse3_Bloom1[idx].w;
}
float3 EmissiveColorLU(float idx)
{
    return Emissive3_Wrap1[idx].xyz;
}
float3 WrapLU(float idx)
{
    float3 r;
    r.x = Emissive3_Wrap1[idx].w * LightWrap.x;
    r.y = 1.0f / (1.f + r.x);
    r.z = r.x * r.y;
    return r;
}
float4 SpecularColorLU(float idx)
{
    return SpecCol3_Pow1[idx];
}
float2 AnisoLU(float idx)
{
    return Aniso2_EnvInt1_Unused1[idx].xy;
}
float EnvIntensityLU(float idx)
{
    return Aniso2_EnvInt1_Unused1[idx].z;
}
float3 BumpDetailLU(float idx, float2 uv, float3 normal)
{
    float3 bump = tex2D(BumpDetailSampler, uv * Bump_Tile1_Int1_Unused2[idx].xx) * 2.0f - 1.0f;
    bump.xy *= Bump_Tile1_Int1_Unused2[idx].y;
//    bump = float3(0.0f, 0.0f, 1.0f);

    bump = mul(bump, float3x3(-normal.y, normal.x, 0.0f,
                        -normal.x * normal.z, -normal.y * normal.z, dot(normal.xy, normal.xy),
                        normal.x, normal.y, normal.z));

    return normalize(bump);
}

#endif // SURFACE_LIGHT_FX
