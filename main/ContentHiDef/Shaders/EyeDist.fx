// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#ifndef EYE_DIST_FX
#define EYE_DIST_FX

/// Return the normalized vector from position to camera, with the
/// distance in the .w component.
float4 EyeDist(float3 worldPos)
{
    float3 eye = EyeLocation - worldPos.xyz;
    float eyeDist = dot(eye, eye);
    eyeDist = eyeDist > 0.0f ? rsqrt(dot(eye, eye)) : 0.00001f;
    eye *= eyeDist; // Normalize
    eyeDist = 1.0f / eyeDist;
    return float4(eye, eyeDist);
}


#endif // EYE_DIST_FX
