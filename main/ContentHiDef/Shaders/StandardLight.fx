// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#ifndef STANDARD_LIGHT_FX
#define STANDARD_LIGHT_FX

shared float4      DiffuseColor;
shared float4      EmissiveColor;
shared float4      SpecularColor;
shared float       SpecularPower;
shared float       Shininess;
shared float2      Aniso = float2(1.0f, 1.0f);

#include "Light.fx"

#endif // STANDARD_LIGHT_FX
