// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// Common place for UI lighting to reside.
//

float3 LightDir = float3(-0.577, -0.577, 1);            // Hard code to cut down a bit of # instructions.
float SpecularPower = 16;
float3 HalfWayVector = float3(-0.577f, -0.577f, 0.6f);  // Match Light Dir

//
// Note that bodyColor is already premultiplied by alpha.
//
float4 CalcDiffuse(float4 bodyColor, float3 normal)
{
    float lDotN = dot(LightDir, normal);
    // Wrap shading.
    lDotN = lDotN * 0.5 + 0.5;

    float3 litColor = lDotN *  bodyColor;
    float4 result = float4(litColor, bodyColor.a);

    return result;
}   // end of CalcDiffuse()

float4 CalcSpecular(float alpha, float3 normal)
{
    float spec = dot(HalfWayVector, normal);
    spec = pow(abs(spec), SpecularPower);

    float4 result = spec * alpha;

    return result;
}   // end of CalcSpecular()
