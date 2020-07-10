// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#include "Terrain_FA.fx"

// -----------------------------------------------------
// Edit mode vertex shaders
// -----------------------------------------------------
COLOR_VS_EDIT_OUTPUT_FA_SM2 EditColorL10VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
    COLOR_VS_EDIT_OUTPUT_FA_SM2 Output;
    
    float3 position = positionAndNormalZ.xyz;
    
    Output.base = ColorL10VS_FA_SM2(positionAndNormalZ, normalXY);
	Output.worldPos.xy = position.xy;

    return Output;
}
COLOR_VS_EDIT_OUTPUT_FA_SM2 EditColorL6VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
    COLOR_VS_EDIT_OUTPUT_FA_SM2 Output;
    
    float3 position = positionAndNormalZ.xyz;
    
    Output.base = ColorL6VS_FA_SM2(positionAndNormalZ, normalXY);
	Output.worldPos.xy = position.xy;

    return Output;
}
COLOR_VS_EDIT_OUTPUT_FA_SM2 EditColorL4VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
    COLOR_VS_EDIT_OUTPUT_FA_SM2 Output;
    
    float3 position = positionAndNormalZ.xyz;
    
    Output.base = ColorL4VS_FA_SM2(positionAndNormalZ, normalXY);
	Output.worldPos.xy = position.xy;

    return Output;
}
COLOR_VS_EDIT_OUTPUT_FA_SM2 EditColorL2VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
    COLOR_VS_EDIT_OUTPUT_FA_SM2 Output;
    
    float3 position = positionAndNormalZ.xyz;
    
    Output.base = ColorL2VS_FA_SM2(positionAndNormalZ, normalXY);
	Output.worldPos.xy = position.xy;

    return Output;
}
COLOR_VS_EDIT_OUTPUT_FA_SM2 EditColorL0VS_FA_SM2( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
    COLOR_VS_EDIT_OUTPUT_FA_SM2 Output;
    
    float3 position = positionAndNormalZ.xyz;
    
    Output.base = ColorL0VS_FA_SM2(positionAndNormalZ, normalXY);
	Output.worldPos.xy = position.xy;

    return Output;
}

COLOR_VS_EDIT_OUTPUT_FA_SM3 EditColorVS_FA_SM3( float4 positionAndNormalZ : POSITION, float2 normalXY : NORMAL )
{
    COLOR_VS_EDIT_OUTPUT_FA_SM3 Output;
    
    Output.base = ColorVS_FA_SM3(positionAndNormalZ, normalXY);

    return Output;
}

// -----------------------------------------------------
// Edit mode pixel shaders
// -----------------------------------------------------
float4 EditColorPS_FA_SM2( COLOR_VS_EDIT_OUTPUT_FA_SM2 In ) : COLOR0
{
    float4 result = ColorPS_FA_SM2(In.base);
    
    float2 center = CubeCenter_FA(In.worldPos.xy);
    result.rgb = EditInvert_FA(center, result.rgb);
    
    return result;

}   // end of EditColorPS_SM2()
float4 EditColor2PS_FA_SM2( COLOR_VS_EDIT_OUTPUT_FA_SM2 In ) : COLOR0
{
    float4 result = Color2PS_FA_SM2(In.base);
    
    float2 center = CubeCenter_FA(In.worldPos.xy);
    result.rgb = EditInvert_FA(center, result.rgb);
    
    return result;

}   // end of EditColor2PS_SM2()
float4 EditColorPS_FA_SM3( COLOR_VS_EDIT_OUTPUT_FA_SM3 In ) : COLOR0
{
    float4 result = ColorPS_FA_SM3(In.base);
    
    float2 center = CubeCenter_FA(In.base.worldPos.xy);
    result.rgb = EditInvert_FA(center, result.rgb);
    
    return result;

}   // end of EditColorPS_SM3()
float4 EditColor2PS_FA_SM3( COLOR_VS_EDIT_OUTPUT_FA_SM3 In ) : COLOR0
{
    float4 result = Color2PS_FA_SM3(In.base);
    
    float2 center = CubeCenter_FA(In.base.worldPos.xy);
    result.rgb = EditInvert_FA(center, result.rgb);
    
    return result;

}   // end of EditColor2PS_SM3()

#ifndef XBOX
VertexShader EditColorVS_FA[] =
{
	compile vs_2_0 EditColorL0VS_FA_SM2(),
	compile vs_2_0 EditColorL2VS_FA_SM2(),
	compile vs_2_0 EditColorL4VS_FA_SM2(),
	compile vs_2_0 EditColorL6VS_FA_SM2(),
	compile vs_2_0 EditColorL10VS_FA_SM2(),
	//compile vs_3_0 EditColorVS_FA_SM3(),
};

PixelShader EditColorPS_FA[] = 
{
	compile ps_2_0 EditColorPS_FA_SM2(),
	compile ps_2_0 EditColor2PS_FA_SM2(),
	//compile ps_3_0 EditColorPS_FA_SM3(),
	//compile ps_3_0 EditColor2PS_FA_SM3(),	
};
#endif

// -----------------------------------------------------
// Edit mode techniques
// -----------------------------------------------------
technique TerrainEditMode_FA
{
    pass P0
    {
#ifndef XBOX
        VertexShader = (EditColorVS_FA[VSIndex]);
        PixelShader  = (EditColorPS_FA[PSIndex]);
#else
		VertexShader = compile vs_3_0 EditColorVS_FA_SM3();
		PixelShader = compile ps_3_0 EditColorPS_FA_SM3();
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
// that select special effects by specifying a
// technique extension (e.g. "Masked"). It should
// be done by manipulation of the PSIndex and 
// VSIndex instead.
technique TerrainEditMode_FAMasked
{
    pass P0
    {
#ifndef XBOX
        VertexShader = (EditColorVS_FA[VSIndex]);
        PixelShader  = (EditColorPS_FA[PSIndex + 1]);
#else
		VertexShader = compile vs_3_0 EditColorVS_FA_SM3();
		PixelShader = compile ps_3_0 EditColor2PS_FA_SM3();
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



