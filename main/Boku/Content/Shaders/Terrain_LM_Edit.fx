
#include "Terrain_LM.fx"
#include "Terrain_FA_Edit.fx"

// -----------------------------------------------------
// Edit mode pixel shaders
// -----------------------------------------------------
float4 EditColorPS_SM2( COLOR_VS_EDIT_OUTPUT_SM2 In ) : COLOR0
{
    float4 result = ColorPS_SM2(In.base);
    
    result.rgb = EditInvert(In.center, result.rgb);
    
    return result;

}   // end of EditColorPS_SM2()
float4 EditColor2PS_SM2( COLOR_VS_EDIT_OUTPUT_SM2 In ) : COLOR0
{
    float4 result = Color2PS_SM2(In.base);
    
    result.rgb = EditInvert(In.center, result.rgb);
    
    return result;

}   // end of EditColor2PS_SM2()
float4 EditColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    float4 result = ColorPS_SM3(In);
    
    result.rgb = EditInvert(In.center, result.rgb);
    
    return result;

}   // end of EditColorPS_SM3()
// Edit-mode pixel shader (PS)
float4 EditColor2PS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    float4 result = Color2PS_SM3(In);
    
    result.rgb = EditInvert(In.center, result.rgb);
    
    return result;

}   // end of EditColor2PS_SM3()

// -----------------------------------------------------
// Edit mode techniques
// -----------------------------------------------------
technique TerrainEditMode
{
    pass P0
    {
        VertexShader = compile vs_3_0 EditColorVS_SM3();
        PixelShader  = compile ps_3_0 EditColorPS_SM3();

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
// that select special effects by specifying a
// technique extension (e.g. "Masked"). It should
// be done by manipulation of the PSIndex and 
// VSIndex instead.
technique TerrainEditModeMasked
{
    pass P0
    {
        VertexShader = compile vs_3_0 EditColorVS_SM3();
        PixelShader  = compile ps_3_0 EditColor2PS_SM3();

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

