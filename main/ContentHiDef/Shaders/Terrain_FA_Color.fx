// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#include "Terrain_FA.fx"

#ifndef XBOX
VertexShader ColorVS_FA[] =
{
	compile vs_2_0 ColorL0VS_FA_SM2(),
	compile vs_2_0 ColorL2VS_FA_SM2(),
	compile vs_2_0 ColorL4VS_FA_SM2(),
	compile vs_2_0 ColorL6VS_FA_SM2(),
	compile vs_2_0 ColorL10VS_FA_SM2(),
	compile vs_3_0 ColorVS_FA_SM3(),
};

PixelShader ColorPS_FA[] =
{
	compile ps_2_0 ColorPS_FA_SM2(),
	compile ps_2_0 Color2PS_FA_SM2(),
	compile ps_3_0 ColorPS_FA_SM3(),
	compile ps_3_0 Color2PS_FA_SM3(),
};
#endif

// -----------------------------------------------------
// Color-pass techniques
// -----------------------------------------------------
technique TerrainColorPass_FA
{
    pass P0
    {
#ifndef XBOX
        VertexShader = (ColorVS_FA[VSIndex]);
        PixelShader  = (ColorPS_FA[PSIndex]);
#else
		VertexShader = compile vs_3_0 ColorVS_FA_SM3();
		PixelShader = compile ps_3_0 ColorPS_FA_SM3();
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
technique TerrainColorPass_FAMasked
{
    pass P0
    {
#ifndef XBOX
        VertexShader = (ColorVS_FA[VSIndex]);
        PixelShader  = (ColorPS_FA[PSIndex + 1]);
#else
		VertexShader = compile vs_3_0 ColorVS_FA_SM3();
		PixelShader = compile ps_3_0 Color2PS_FA_SM3();
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
