
#ifndef PREP_XFORM_FX
#define PREP_XFORM_FX

float4x4    LocalToModel;

float3 PrepPosition(float3 pos)
{
    return mul(float4(pos, 1.0f), LocalToModel);
}
float4 PrepPosition(float4 pos)
{
    return mul(float4(pos.xyz, 1.0f), LocalToModel);
}
float3 PrepNormal(float3 norm)
{
    return mul(norm, LocalToModel);
}



#endif // PREP_XFORM_FX
