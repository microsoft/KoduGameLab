// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#ifndef STANDARD_SM2_FX
#define STANDARD_SM2_FX

#define SM2_LUZ

//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT_SM2
{
    float4 position         : POSITION;     // vertex position
    float4 diffuse          : COLOR0;
    float3 textureUV        : TEXCOORD0;    // Fog in .z
};

/// Don't call this directly, it can't fold in the LocalToModel for you, because
/// you might have already folded in the palette or flex or something.
COLOR_VS_OUTPUT_SM2 ColorVS_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL)
{
    COLOR_VS_OUTPUT_SM2   Output;

    // Transform our position.
    Output.position = mul( float4(position, 1.0f), WorldViewProjMatrix );

    // Transform the normals into world coordinates and normalize.
    normal = normalize( mul( normal.xyz, WorldMatrix) );

    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( float4(position, 1.0f), WorldMatrix );

    // Calc the eye vector.  This is the direction from the point to the eye.
    float4 eyeDist = EyeDist(worldPosition.xyz);

    Output.textureUV = float3(0.0f, 0.0f, 0.0f);

    // Calc lighting.
	float4 gloss = GlossVtx(Shininess, eyeDist.xyz, normal, SpecularPower);

    float4 diffuseLight = DiffuseLight(normal, 1.0f, 1.0f.xxx, LightWrap);
    
    float3 specularLight = gloss.a * LightColor0 * SpecularColor; // do we want to shadow specular?

    float4 result;
    result =  diffuseLight;

    /// Can't currently afford these, we run out of vertex shader 
    /// instructions (max 256 for VS_2_0). The worst case is WithWind,
    /// but WithSkinning is also slightly over. %P
    //float3 luzPos;
    //float3 luzCol;
    //AverageLuz(worldPosition, normal, luzPos, luzCol);
    //result.rgb += ApplyLuz(worldPosition, normal, luzPos, luzCol, 1.0f.xxx);

#ifdef SM2_BRUTE
	result.rgb += PointLights(worldPosition, normal);
#endif // SM2_BRUTE

    result.rgba *= DiffuseColor;

    result.rgb += EmissiveColor;
    
    result.rgb += specularLight;

    result.rgb += gloss.rgb * 0.25f;

    Output.diffuse = result;

    Output.textureUV.z = CalcFog( eyeDist.w );

    return Output;
}

COLOR_VS_OUTPUT_SM2 ColorSimpVS_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    return ColorVS_SM2(position, normal);
}

COLOR_VS_OUTPUT_SM2 ColorTexVS_SM2(
							float3 position : POSITION,
							float3 normal	: NORMAL,
							float2 tex		: TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

	COLOR_VS_OUTPUT_SM2 Output = ColorVS_SM2(position, normal);
	Output.textureUV.xy = tex;
	
	return Output;
}

// Vertex shader for fish.  Uses Flex param to bend shape left/right around Z axis.
COLOR_VS_OUTPUT_SM2 ColorWithFlexVS_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL)
{
    // First, transform the fishbones.  Assumes transform is affine.
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    float3 pos = ApplyFlex(position, normal);
    
    return ColorVS_SM2(pos, normal);
}
COLOR_VS_OUTPUT_SM2 ColorTexWithFlexVS_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float2 tex      : TEXCOORD0)
{
    // First, transform the fishbones.  Assumes transform is affine.
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    float3 pos = ApplyFlex(position, normal);
    
    return ColorTexVS_SM2(pos, normal, tex);
}

COLOR_VS_OUTPUT_SM2 ColorTexWithSkinVS_SM2(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin4(input);
    
	return ColorTexVS_SM2(skin.position, skin.normal, input.texcoord);
}

COLOR_VS_OUTPUT_SM2 ColorWithSkinVS_SM2(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin4(input);
    
	return ColorVS_SM2(skin.position, skin.normal);
}

COLOR_VS_OUTPUT_SM2 ColorWithWindVS_SM2(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin8(input);
    
	return ColorTexVS_SM2(skin.position, skin.normal, input.texcoord);
}

COLOR_VS_OUTPUT_SM2 FoliageColorVS_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float2 tex      : TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    normal = normalize( mul( normal.xyz, WorldMatrix) );
    if (normal.z < 0.0f)
        normal = -normal;

    return ColorTexVS_SM2(position, normal, tex);
}

COLOR_VS_OUTPUT_SM2 CloudColorVS_SM2( 
                        float3 position : POSITION,
                        float3 normal   : NORMAL)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    COLOR_VS_OUTPUT_SM2 Output = ColorVS_SM2(position, normal);

    normal = normalize( mul( normal.xyz, WorldMatrix) );

    // normals pointing down get diffuse color; up get white color
    float glowAmount = normal.z * 0.5f + 0.5f;		
    float3 glow = glowAmount.xxx + (1.0f - glowAmount) * DiffuseColor;
//glow = glowAmount;

    Output.diffuse.rgb = Output.diffuse.rgb *.2f + glow * .8f;
//Output.diffuse.rgb = glow;

    Output.diffuse.a = 0.9f;

    return Output;
}

COLOR_VS_OUTPUT_SM2 CloudColorWithSkinVS_SM2(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    SKIN_OUTPUT skin = Skin4(input);

    return CloudColorVS_SM2(skin.position, skin.normal);
}

//
// Pixel shader that does all of the work for both
// textured and untextured cases.
//
float4 ColorPS_SM2( COLOR_VS_OUTPUT_SM2 In) : COLOR0
{
    float4 result = In.diffuse;

    result.rgb = lerp(result.rgb, FogColor, In.textureUV.z);

    return result;
}   // end of ColorPS()


//
// Pixel shader for textured subsets.
//
float4 TexturedColorPS_SM2( COLOR_VS_OUTPUT_SM2 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV );

    In.diffuse *= diffuseColor;

    float4 result = ColorPS_SM2( In );

    return result;
}

//
// Pixel shader for textured foliage subsets.
//
//  Warning, this is very hacked...
//
float4 TexturedFoliageColorPS_SM2( COLOR_VS_OUTPUT_SM2 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV );
    
    // Brighten the whole thing.
    diffuseColor *= 2.0f;

    // Use the V coord as an ambient occlusion factor.
    diffuseColor *= 1.0f - In.textureUV.y * 0.8f;
    
    In.diffuse *= diffuseColor;
    float4 result = ColorPS_SM2( In );

    return result;
}

//
// Pixel shader for non-textured subsets.
//
float4 NonTexturedColorPS_SM2( COLOR_VS_OUTPUT_SM2 In ) : COLOR0
{
    float4 result = ColorPS_SM2( In );

    return result;
}

float4 CloudColorPS_SM2( COLOR_VS_OUTPUT_SM2 In ) : COLOR0
{
    return ColorPS_SM2(In);
}

float4 TexturedColorPSFace_SM2( COLOR_VS_OUTPUT_SM2 In ) : COLOR0
{
    float4 diffuse = ColorPS_SM2(In);

    diffuse *= BokuFace(In.textureUV);

//return shapeLeft;
//return float4(pupilLeft.aaa, 1.0f);
	return diffuse;
}


//
// Techniques
//
technique TexturedColorPass_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexVS_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPS_SM2();

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


technique NonTexturedColorPass_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorSimpVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = CCW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique NonTexturedColorPassCloud_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 CloudColorWithSkinVS_SM2();
        PixelShader  = compile ps_2_0 CloudColorPS_SM2();

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

technique NonTexturedColorPassWithFlex_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorWithFlexVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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

technique TexturedColorPassFace_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexVS_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPSFace_SM2();

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

technique NonTexturedColorPassWithSkinning_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 ColorWithSkinVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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

technique TexturedColorPassWithSkinning_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexWithSkinVS_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPSFace_SM2();

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

technique NonTexturedColorPassWithWind_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 ColorWithWindVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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

technique TexturedColorPassWithFlex_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexWithFlexVS_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPS_SM2();

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


technique TexturedColorPassFoliage_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 FoliageColorVS_SM2();
        PixelShader  = compile ps_2_0 TexturedFoliageColorPS_SM2();

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


technique NonTexturedColorPassFoliage_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 FoliageColorVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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

#include "Ghost_Stand_SM2.fx"

#endif // STANDARD_SM2_FX

