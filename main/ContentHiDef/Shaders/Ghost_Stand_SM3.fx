
#ifndef GHOST_SM3_FX
#define GHOST_SM3_FX

float4 GhostNonTexturedColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    float4 result = NonTexturedColorPS_SM3( In );

    result = GhostColor(result);

    return result;
}

float4 GhostCloudColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
	float4 result = CloudColorPS_SM3(In);
	
    result = GhostColor(result);
	
	return result;
}

//
// Techniques
//
technique GhostPass_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorSimpTexVS_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SM3();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique GhostPassNonTextured_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorSimpVS_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SM3();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}


technique GhostPassCloud_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorWithSkinVS_SM3();
        PixelShader  = compile ps_3_0 GhostCloudColorPS_SM3();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
	}
}

technique GhostPassWithFlex_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithFlexVS_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SM3();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}


technique GhostPassWithSkinning_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorWithSkinVS_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SM3();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique GhostPassWithWind_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorWithWindVS_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SM3();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
   	}
}


technique GhostPassFoliage_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 FoliageColorVS_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SM3();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}


#endif // GHOST_SM3_FX
