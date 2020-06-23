
#ifndef GHOST_SURF_SM3_FX
#define GHOST_SURF_SM3_FX

float4 GhostNonTexturedColorPS_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    float4 result = NonTexturedColorPS_SURF_SM3( In );

    result = GhostColor(result);

    return result;
}

float4 GhostCloudColorPS_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
	float4 result = CloudColorPS_SURF_SM3(In);
	
    result = GhostColor(result);
	
	return result;
}

float4 GhostNonTexturedColorPSBokuFace_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
	float4 result = NonTexturedColorPSBokuFace_SURF_SM3(In);
	
    result = GhostColor(result);
	
	return result;
}

float4 GhostNonTexturedColorPSWideFace_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
	float4 result = NonTexturedColorPSWideFace_SURF_SM3(In);
	
    result = GhostColor(result);
	
	return result;
}

float4 GhostNonTexturedColorPSTwoFace_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
	float4 result = NonTexturedColorPSTwoFace_SURF_SM3(In);
	
    result = GhostColor(result);
	
	return result;
}

//
// Techniques
//
technique GhostPass_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorSimpTexVS_SURF_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SURF_SM3();

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


technique GhostPassCloud_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 GhostCloudColorPS_SURF_SM3();

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

technique GhostPassWithFlex_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithFlexVS_SURF_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SURF_SM3();

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


technique GhostPassWithSkinning_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SURF_SM3();

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

technique GhostPassBokuFaceWithSkinning_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPSBokuFace_SURF_SM3();

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

technique GhostPassWideFaceWithSkinning_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPSWideFace_SURF_SM3();

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

technique GhostPassTwoFaceWithSkinning_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPSTwoFace_SURF_SM3();

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

technique GhostPassWithWind_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorWithWindVS_SURF_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SURF_SM3();

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


technique GhostPassFoliage_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 FoliageColorVS_SURF_SM3();
        PixelShader  = compile ps_3_0 GhostNonTexturedColorPS_SURF_SM3();

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


#endif // GHOST_SURF_SM3_FX
