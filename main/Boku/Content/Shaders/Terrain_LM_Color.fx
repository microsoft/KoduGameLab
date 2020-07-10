// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#include "Terrain_LM.fx"
#include "Terrain_FA_Color.fx"

// -----------------------------------------------------
// Color-pass techniques
// -----------------------------------------------------
technique TerrainColorPass
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorVS_SM3();
        PixelShader  = compile ps_3_0 ColorPS_SM3();

        // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual;

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = CCW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}
//ToDo (DZ): We need to rethink the mechanisms
// that selects special effects by specifying a
// technique extension (e.g. "Masked"). It should
// be done by manipulation of the PSIndex and 
// VSIndex instead.
technique TerrainColorPassMasked
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorVS_SM3();
        PixelShader  = compile ps_3_0 Color2PS_SM3();

        // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual;

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = CCW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

