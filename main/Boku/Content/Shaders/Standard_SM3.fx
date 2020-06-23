
#ifndef STANDARD_SM3_FX
#define STANDARD_SM3_FX

//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT_SM3
{
    float4 position         : POSITION;     // vertex position
    float3 luzCol			: COLOR1;		// average point light color
    float2 textureUV        : TEXCOORD0;    // vertex texture coords
    float4 positionWorld    : TEXCOORD1;    // position in world space after transform
    float3 normal           : TEXCOORD2;    // normal in world space after transform
    float4 eye              : TEXCOORD3;    // vector to eye from point, fog strength in w
    float3 luzPos           : TEXCOORD4;    // average point light position
};

/// Don't call this directly, it can't fold in the LocalToModel for you, because
/// you might have already folded in the palette or flex or something.
COLOR_VS_OUTPUT_SM3 ColorVS_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL)
{
    COLOR_VS_OUTPUT_SM3   Output;

    // Transform our position.
    Output.position = mul( float4(position, 1.0f), WorldViewProjMatrix );

    // Transform the normals into world coordinates and normalize.
    Output.normal = normalize( mul( normal.xyz, WorldMatrixInverseTranspose ) );

    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( float4(position, 1.0f), WorldMatrix );
    Output.positionWorld = worldPosition;

    AverageLuz(worldPosition, Output.normal, Output.luzPos, Output.luzCol);

    // Calc the eye vector.  This is the direction from the point to the eye.
    float4 eyeDist = EyeDist(worldPosition.xyz);
	Output.eye.xyz = eyeDist.xyz;

    Output.textureUV = float2(0.0f, 0.0f);

    Output.eye.w = CalcFog( eyeDist.w );

    return Output;
}

COLOR_VS_OUTPUT_SM3 ColorSimpVS_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    return ColorVS_SM3(position, normal);
}

COLOR_VS_OUTPUT_SM3 ColorTexVS_SM3(
							float3 position : POSITION,
							float3 normal	: NORMAL,
							float2 tex		: TEXCOORD0)
{
    COLOR_VS_OUTPUT_SM3 Output = ColorVS_SM3(position, normal);
	Output.textureUV = tex;
	
	return Output;
}
COLOR_VS_OUTPUT_SM3 ColorSimpTexVS_SM3(
							float3 position : POSITION,
							float3 normal	: NORMAL,
							float2 tex		: TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    return ColorTexVS_SM3(position, normal, tex);
}

// Vertex shader for fish.  Uses Flex param to bend shape left/right around Z axis.
COLOR_VS_OUTPUT_SM3 ColorWithFlexVS_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    // First, transform the fishbones.  Assumes transform is affine.
    float3 pos = ApplyFlex(position, normal);
    
    return ColorVS_SM3(pos, normal);
}
COLOR_VS_OUTPUT_SM3 ColorTexWithFlexVS_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float2 tex      : TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    // First, transform the fishbones.  Assumes transform is affine.
    float3 pos = ApplyFlex(position, normal);
    
    return ColorTexVS_SM3(pos, normal, tex);
}

COLOR_VS_OUTPUT_SM3 ColorTexWithSkinVS_SM3(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin4(input);
    
	return ColorTexVS_SM3(skin.position, skin.normal, input.texcoord);
}

COLOR_VS_OUTPUT_SM3 ColorWithSkinVS_SM3(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin4(input);
    
	return ColorVS_SM3(skin.position, skin.normal);
}

COLOR_VS_OUTPUT_SM3 ColorWithWindVS_SM3(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin8(input);
//SKIN_OUTPUT skin = Skin4(input);
    
	return ColorTexVS_SM3(skin.position, skin.normal, input.texcoord);
}

COLOR_VS_OUTPUT_SM3 FoliageColorVS_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float2 tex      : TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    COLOR_VS_OUTPUT_SM3   Output;

    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );

    // Transform the normals into world coordinates and normalize.
    Output.normal = normalize( mul( normal.xyz, WorldMatrixInverseTranspose ) );
    if( Output.normal.z < 0.0f)
    {
        Output.normal = -Output.normal;
    } 

    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( pos, WorldMatrix );
    Output.positionWorld = worldPosition;

    AverageLuz(worldPosition, Output.normal, Output.luzPos, Output.luzCol);

    // Calc the eye vector.  This is the direction from the point to the eye.
    float4 eyeDist = EyeDist(worldPosition.xyz);
    Output.eye.xyz = eyeDist.xyz;

    Output.textureUV = tex;

    Output.eye.w = CalcFog( eyeDist.w );

    return Output;
}

//
// Pixel shader that does all of the work for both
// textured and untextured cases.
//
float4 ColorPS_SM3( COLOR_VS_OUTPUT_SM3 In, float4 diffuseColor : COLOR0 ) : COLOR0
{

    // Normalize our vectors.
    In.normal = normalize( In.normal );
    float3 eye = normalize( In.eye.xyz );

    // HACK HACK
    // Calc effect of shadow.
    // float attenuation = Shadow(In.position.xy);
    // Ignore shadows for now.
    float attenuation = 1.0f;

    // Calc lighting.
    float4 gloss = GlossEnv(Shininess, eye, In.normal, SpecularPower);
    
    float4 diffuseLight = DiffuseLight(In.normal, attenuation, diffuseColor, LightWrap);
    
    
    float3 specularLight = gloss.a * SpecularColor.rgb * LightColor0.rgb;

    float4 result = diffuseLight;
    
    result.rgb += ApplyLuz(In.positionWorld, In.normal, In.luzPos, In.luzCol, diffuseColor);
    
    result.rgb += specularLight.rgb;
    result.rgb += gloss.rgb;

    result.rgb += EmissiveColor;

    // Get alpha from diffuse texture.
    result.a = diffuseColor.a;
    
    // Return the combined color
    return result;
}   // end of ColorPS()

// Pixel shader for textured subsets.
//
float4 TexturedColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = DiffuseColor * tex2D( DiffuseTextureSampler, In.textureUV );

    float4 result = ColorPS_SM3( In, diffuseColor );

    // Add in fog.
    result.rgb = lerp(result, FogColor, In.eye.w);

    return result;
}

//
// Pixel shader for non-textured subsets.
//
float4 NonTexturedColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    float4 result = ColorPS_SM3( In, DiffuseColor );

    // Add in fog.
    result.rgb = lerp(result, FogColor, In.eye.w);

    return result;
}


//
//
// Pixel shader for textured foliage subsets.
//
//  Warning, this is very hacked...
//
float4 TexturedFoliageColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV );
    
    // Brighten the whole thing.
    diffuseColor *= 2.0f;

    // Use the V coord as an ambient occlusion factor.
    diffuseColor *= 1.0f - In.textureUV.y * 0.8f;

    diffuseColor *= DiffuseColor;
    
    float4 result = ColorPS_SM3( In, diffuseColor );

    // Add in fog.
    result.rgb = lerp(result, FogColor, In.eye.w);

    return result;
}


float4 CloudColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    // Normalize our vectors.
	// mix in a little lighting by shading white as the diffuse color
    float3 shading = ColorPS_SM3(In, DiffuseColor);
    
    // normals pointing down get diffuse color; up get white color
    float glowAmount = In.normal.z * 0.5f + 0.5f;		
    float3 glow = glowAmount.xxx + (1.0f - glowAmount) * DiffuseColor;

    float4 result;
    // we mostly use the glow with a little bit of the shading to show volume.
    result.rgb = shading *.2f + glow * .8f;
    
    // and some fog
    result.rgb = lerp(result.rgb, FogColor, In.eye.w);
    result.a = 0.9f;
    return result;
}

float4 TexturedColorPSFace_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    float4 diffuse = DiffuseColor * BokuFace(In.textureUV);
    
    diffuse = ColorPS_SM3(In, diffuse);

	return diffuse;
}


//
// Techniques
//
technique TexturedColorPass_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorSimpTexVS_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPS_SM3();

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


technique NonTexturedColorPass_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorSimpVS_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SM3();

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

technique NonTexturedColorPassCloud_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorWithSkinVS_SM3();
        PixelShader  = compile ps_3_0 CloudColorPS_SM3();

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

technique NonTexturedColorPassWithFlex_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorWithFlexVS_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SM3();

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

technique TexturedColorPassFace_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorSimpTexVS_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPSFace_SM3();

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


technique NonTexturedColorPassWithSkinning_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorWithSkinVS_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SM3();

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

technique TexturedColorPassWithSkinning_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPSFace_SM3();

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

technique NonTexturedColorPassWithWind_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorWithWindVS_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SM3();

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

technique TexturedColorPassWithFlex_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithFlexVS_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPS_SM3();

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


technique TexturedColorPassFoliage_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 FoliageColorVS_SM3();
        PixelShader  = compile ps_3_0 TexturedFoliageColorPS_SM3();

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


technique NonTexturedColorPassFoliage_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 FoliageColorVS_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SM3();

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

#include "Ghost_Stand_SM3.fx"

#endif // STANDARD_SM3_FX
