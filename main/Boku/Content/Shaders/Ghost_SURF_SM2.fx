
#ifndef GHOST_SURF_SM2_FX
#define GHOST_SURF_SM2_FX

COLOR_VS_OUTPUT_SURF_SM2 GhostColorTexVS_SURF_SM2(
							float3 position : POSITION,
							float3 normal	: NORMAL,
                            float3 matSelect : COLOR0,
							float2 tex		: TEXCOORD0)
{
    COLOR_VS_OUTPUT_SURF_SM2   Output = ColorTexVS_SURF_SM2(position, normal, matSelect, tex);
    
    Output.diffuse = GhostColor(Output.diffuse);
    Output.specular = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    return Output;
}

COLOR_VS_OUTPUT_SURF_SM2 GhostCloudColorWithSkinVS_SURF_SM2(in SKIN_VS_INPUT input)
{
    COLOR_VS_OUTPUT_SURF_SM2   Output = CloudColorWithSkinVS_SURF_SM2(input);
    
    Output.diffuse = GhostColor(Output.diffuse);
    Output.specular = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    return Output;
}

COLOR_VS_OUTPUT_SURF_SM2 GhostColorTexWithFlexVS_SURF_SM2(
							float3 position : POSITION,
							float3 normal	: NORMAL,
                            float3 matSelect : COLOR0,
							float2 tex		: TEXCOORD0)
{
    COLOR_VS_OUTPUT_SURF_SM2 Output = ColorTexWithFlexVS_SURF_SM2(position, normal, matSelect, tex);
    
    Output.diffuse = GhostColor(Output.diffuse);
    Output.specular = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    return Output;
}

COLOR_VS_OUTPUT_SURF_SM2 GhostColorTexWithSkinVS_SURF_SM2(in SKIN_VS_INPUT input)
{
    COLOR_VS_OUTPUT_SURF_SM2 Output = ColorTexWithSkinVS_SURF_SM2(input);
    
    Output.diffuse = GhostColor(Output.diffuse);
    Output.specular = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    return Output;
}

COLOR_VS_OUTPUT_SURF_SM2 GhostColorWithWindVS_SURF_SM2(in SKIN_VS_INPUT input)
{
    COLOR_VS_OUTPUT_SURF_SM2 Output = ColorWithWindVS_SURF_SM2(input);
    
    Output.diffuse = GhostColor(Output.diffuse);
    Output.specular = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    return Output;
}


//
// Techniques
//
technique GhostPass_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 GhostColorTexVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SURF_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
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

technique GhostPassCloud_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 GhostCloudColorWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 CloudColorPS_SURF_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
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

technique GhostPassWithFlex_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 GhostColorTexWithFlexVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SURF_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
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

technique GhostPassFace_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 GhostColorTexVS_SURF_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPSBokuFace_SURF_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
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

technique GhostPassWithSkinning_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 GhostColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SURF_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
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

technique GhostPassBokuFaceWithSkinning_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 GhostColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPSBokuFace_SURF_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
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

technique GhostPassWideFaceWithSkinning_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 GhostColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPSWideFace_SURF_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
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

technique GhostPassTwoFaceWithSkinning_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 GhostColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPSTwoFace_SURF_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
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


technique GhostPassWithWind_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 GhostColorWithWindVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SURF_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
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


#endif // GHOST_SURF_SM2_FX
