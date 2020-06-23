
#ifndef GHOST_SM2_FX
#define GHOST_SM2_FX

COLOR_VS_OUTPUT_SM2 GhostColorVS_SM2(
							float3 position : POSITION,
							float3 normal	: NORMAL)
{
    COLOR_VS_OUTPUT_SM2   Output = ColorVS_SM2(position, normal);
    
    Output.diffuse = GhostColor(Output.diffuse);
    
    return Output;
}


COLOR_VS_OUTPUT_SM2 GhostColorTexVS_SM2(
							float3 position : POSITION,
							float3 normal	: NORMAL,
							float2 tex		: TEXCOORD0)
{
    COLOR_VS_OUTPUT_SM2   Output = ColorTexVS_SM2(position, normal, tex);
    
    Output.diffuse = GhostColor(Output.diffuse);
    
    return Output;
}

COLOR_VS_OUTPUT_SM2 GhostCloudColorWithSkinVS_SM2(in SKIN_VS_INPUT input)
{
    COLOR_VS_OUTPUT_SM2   Output = CloudColorWithSkinVS_SM2(input);
    
    Output.diffuse = GhostColor(Output.diffuse);
    
    return Output;
}

COLOR_VS_OUTPUT_SM2 GhostColorTexWithFlexVS_SM2(
							float3 position : POSITION,
							float3 normal	: NORMAL,
							float2 tex		: TEXCOORD0)
{
    COLOR_VS_OUTPUT_SM2 Output = ColorTexWithFlexVS_SM2(position, normal, tex);
    
    Output.diffuse = GhostColor(Output.diffuse);
    
    return Output;
}

COLOR_VS_OUTPUT_SM2 GhostColorWithSkinVS_SM2(in SKIN_VS_INPUT input)
{
    COLOR_VS_OUTPUT_SM2 Output = ColorWithSkinVS_SM2(input);
    
    Output.diffuse = GhostColor(Output.diffuse);
    
    return Output;
}

COLOR_VS_OUTPUT_SM2 GhostColorWithWindVS_SM2(in SKIN_VS_INPUT input)
{
    COLOR_VS_OUTPUT_SM2 Output = ColorWithWindVS_SM2(input);
    
    Output.diffuse = GhostColor(Output.diffuse);
    
    return Output;
}


//
// Techniques
//
technique GhostPass_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 GhostColorTexVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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

technique GhostPassNonTextured_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 GhostColorVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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

technique GhostPassCloud_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 GhostCloudColorWithSkinVS_SM2();
        PixelShader  = compile ps_2_0 CloudColorPS_SM2();

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

technique GhostPassWithFlex_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 GhostColorTexWithFlexVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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

technique GhostPassWithSkinning_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 GhostColorWithSkinVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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


technique GhostPassWithWind_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 GhostColorWithWindVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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


#endif // GHOST_SM2_FX
