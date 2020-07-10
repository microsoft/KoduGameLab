// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#include "Terrain_FD.fx"
#include "Terrain_FA_Color.fx"

#ifndef XBOX
VertexShader ColorVS[] =
{
	compile vs_2_0 ColorL0VS_SM2(),
	compile vs_2_0 ColorL2VS_SM2(),
	compile vs_2_0 ColorL4VS_SM2(),
	compile vs_2_0 ColorL6VS_SM2(),
	compile vs_2_0 ColorL10VS_SM2(),
	//compile vs_3_0 ColorVS_SM3(),
};
#endif

#ifndef XBOX
PixelShader ColorPS[] =
{
	compile ps_2_0 ColorPS_SM2(),
	compile ps_2_0 Color2PS_SM2(),
	//compile ps_3_0 ColorPS_SM3(),
	//compile ps_3_0 Color2PS_SM3(),
};
#endif

// -----------------------------------------------------
// Color-pass techniques
// -----------------------------------------------------
technique TerrainColorPass
{
    pass P0
    {
#ifndef XBOX
        VertexShader = (ColorVS[VSIndex]);
        PixelShader  = (ColorPS[PSIndex]);
#else
        VertexShader = compile vs_3_0 ColorVS_SM3();
        PixelShader  = compile ps_3_0 ColorPS_SM3();
#endif

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

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
#ifndef XBOX
        VertexShader = (ColorVS[VSIndex]);
        PixelShader  = (ColorPS[PSIndex + 1]);
#else
        VertexShader = compile vs_3_0 ColorVS_SM3();
        PixelShader  = compile ps_3_0 Color2PS_SM3();
#endif

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

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

