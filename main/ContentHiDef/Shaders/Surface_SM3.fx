
#ifndef SURFACE_SM3_FX
#define SURFACE_SM3_FX

//#define DEBUG_VTX_COLOR

//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT_SURF_SM3
{
    float4 position         : POSITION;     // vertex position
    float3 luzCol			: COLOR1;		// average point light color
    float3 textureUV        : TEXCOORD0;    // vertex texture coords
    float4 positionWorld    : TEXCOORD1;    // position in world space after transform
    float3 normal           : TEXCOORD2;    // normal in world space after transform
    float4 eye              : TEXCOORD3;    // vector to eye from point, fog strength in w
    float3 luzPos           : TEXCOORD4;    // average point light position
#ifdef DEBUG_VTX_COLOR
    float3 dbgIndex         : TEXCOORD5;
#endif // DEBUG_VTX_COLOR
};

/// Don't call this directly, it can't fold in the LocalToModel for you, because
/// you might have already folded in the palette or flex or something.
COLOR_VS_OUTPUT_SURF_SM3 ColorVS_SURF_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float3 matSelect : COLOR0)
{
    COLOR_VS_OUTPUT_SURF_SM3   Output;

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

    Output.textureUV = float3(0.0f, 0.0f, MaterialIndex(matSelect.rgb));

#ifdef DEBUG_VTX_COLOR
    Output.dbgIndex = float3(0.0f, 0.0f, 0.0f);
#endif // DEBUG_VTX_COLOR

    Output.eye.w = CalcFog( eyeDist.w );

    return Output;
}

COLOR_VS_OUTPUT_SURF_SM3 ColorSimpVS_SURF_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float3 matSelect : COLOR0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    return ColorVS_SURF_SM3(position, normal, matSelect);
}

COLOR_VS_OUTPUT_SURF_SM3 ColorTexVS_SURF_SM3(
							float3 position : POSITION,
							float3 normal	: NORMAL,
                            float3 matSelect : COLOR0,
							float2 tex		: TEXCOORD0)
{
    COLOR_VS_OUTPUT_SURF_SM3 Output = ColorVS_SURF_SM3(position, normal, matSelect);
	Output.textureUV.xy = tex;
	
	return Output;
}
COLOR_VS_OUTPUT_SURF_SM3 ColorSimpTexVS_SURF_SM3(
							float3 position : POSITION,
							float3 normal	: NORMAL,
                            float3 matSelect : COLOR0,
							float2 tex		: TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    return ColorTexVS_SURF_SM3(position, normal, matSelect, tex);
}

// Vertex shader for fish.  Uses Flex param to bend shape left/right around Z axis.
COLOR_VS_OUTPUT_SURF_SM3 ColorWithFlexVS_SURF_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float3 matSelect : COLOR0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    // First, transform the fishbones.  Assumes transform is affine.
    float3 pos = ApplyFlex(position, normal);
    
    return ColorVS_SURF_SM3(pos, normal, matSelect);
}
COLOR_VS_OUTPUT_SURF_SM3 ColorTexWithFlexVS_SURF_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float3 matSelect : COLOR0,
                        float2 tex      : TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    // First, transform the fishbones.  Assumes transform is affine.
    float3 pos = ApplyFlex(position, normal);
    
    return ColorTexVS_SURF_SM3(pos, normal, matSelect, tex);
}

COLOR_VS_OUTPUT_SURF_SM3 ColorTexWithSkinVS_SURF_SM3(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin4(input);
    
	COLOR_VS_OUTPUT_SURF_SM3 Out = ColorTexVS_SURF_SM3(skin.position, skin.normal, input.color.rgb, input.texcoord);
#ifdef DEBUG_VTX_COLOR
    Out.dbgIndex = input.color.rgb;
#endif // DEBUG_VTX_COLOR
    
    return Out;
}

COLOR_VS_OUTPUT_SURF_SM3 ColorWithSkinVS_SURF_SM3(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin4(input);
    
	COLOR_VS_OUTPUT_SURF_SM3 Out = ColorVS_SURF_SM3(skin.position, skin.normal, input.color.rgb);
#ifdef DEBUG_VTX_COLOR
    Out.dbgIndex = input.color.rgb;
#endif // DEBUG_VTX_COLOR
    
    return Out;
}

COLOR_VS_OUTPUT_SURF_SM3 ColorWithWindVS_SURF_SM3(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
	// NOTE Changed from Skin8().  Skin8 was causes serious problems
	// when run on Surface3, SurfaceBook and Lenovo Yoga devices.
	// It looks like the vertices where jumping upward about 1 unit
	// randomy each frame.
    SKIN_OUTPUT skin = Skin4(input);
    
	COLOR_VS_OUTPUT_SURF_SM3 Out = ColorTexVS_SURF_SM3(skin.position, skin.normal, input.color.rgb, input.texcoord);
#ifdef DEBUG_VTX_COLOR
    Out.dbgIndex = input.color.rgb;
#endif // DEBUG_VTX_COLOR
    
    return Out;
}

COLOR_VS_OUTPUT_SURF_SM3 FoliageColorVS_SURF_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float2 tex      : TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    COLOR_VS_OUTPUT_SURF_SM3   Output;

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

    Output.textureUV.xy = tex;
    Output.textureUV.z = 0.0f;

    Output.eye.w = CalcFog( eyeDist.w );

#ifdef DEBUG_VTX_COLOR
    Output.dbgIndex = float3(0.f, 0.f, 0.f);
#endif // DEBUG_VTX_COLOR

    return Output;
}

//
// Pixel shader that does all of the work for both
// textured and untextured cases.
//
float4 ColorPS_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In, float4 diffuseColor : COLOR0 ) : COLOR0
{

    // Normalize our vectors.
    In.normal = normalize( In.normal );
    In.normal += BumpDetailLU(In.textureUV.z, In.textureUV.xy, In.normal);
    In.normal = normalize( In.normal );
    float3 eye = normalize( In.eye.xyz );

    // HACK HACK
    // Calc effect of shadow.
    // float attenuation = Shadow(In.position.xy);
    // Ignore shadows for now.
    float attenuation = 1.0f;

    // Calc lighting.
    float4 specularColor = SpecularColorLU(In.textureUV.z);
    float4 gloss = GlossEnvAniso(EnvIntensityLU(In.textureUV.z), eye, In.normal, AnisoLU(In.textureUV.z), specularColor.w);
    
    float4 diffuseLight = DiffuseLight(In.normal, attenuation, diffuseColor, WrapLU(In.textureUV.z));
    float4 darkMap = tex2D(DirtMapSampler, In.textureUV.xy);
    darkMap.rgb = 1.0f - (1.0f - darkMap.rgb) * darkMap.a;
    diffuseLight.rgb *= darkMap.rgb;
    
    float3 specularLight = gloss.a * specularColor.rgb * LightColor0.rgb;

    float4 result = diffuseLight;
    
    result.rgb += ApplyLuz(In.positionWorld, In.normal, In.luzPos, In.luzCol, diffuseColor);
    
    result.rgb += specularLight.rgb;
    result.rgb += gloss.rgb;

    result.rgb += EmissiveColorLU(In.textureUV.z);

    // Get alpha from diffuse texture.
    result.a = diffuseColor.a;

//result.rgb = BumpDetailLU(In.textureUV.z, In.textureUV.xy, In.normal) * 0.5 + 0.5;
    
#ifdef DEBUG_VTX_COLOR
    result.rgb = In.dbgIndex;
#endif // DEBUG_VTX_COLOR
    // Return the combined color
    return result;
}   // end of ColorPS()

// Pixel shader for textured subsets.
//
float4 TexturedColorPS_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = DiffuseColorLU(In.textureUV.z) * tex2D( DiffuseTextureSampler, In.textureUV.xy );

    float4 result = ColorPS_SURF_SM3( In, diffuseColor );

    // Add in fog.
    result.rgb = lerp(result, FogColor, In.eye.w);

    return result;
}

//
// Pixel shader for non-textured subsets.
//
float4 NonTexturedColorPS_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    float4 result = ColorPS_SURF_SM3( In, DiffuseColorLU(In.textureUV.z) );

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
float4 TexturedFoliageColorPS_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV.xy );
    
    // Brighten the whole thing.
    diffuseColor *= 2.0f;

    // Use the V coord as an ambient occlusion factor.
    diffuseColor *= 1.0f - In.textureUV.y * 0.8f;

    diffuseColor *= DiffuseColorLU(In.textureUV.z);
    
    float4 result = ColorPS_SURF_SM3( In, diffuseColor );

    // Add in fog.
    result.rgb = lerp(result, FogColor, In.eye.w);

    return result;
}


float4 CloudColorPS_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    // Normalize our vectors.
	// mix in a little lighting by shading white as the diffuse color
    float3 shading = ColorPS_SURF_SM3(In, DiffuseColorLU(In.textureUV.z));
    
    // normals pointing down get diffuse color; up get white color
    float glowAmount = In.normal.z * 0.5f + 0.5f;		
    float3 glow = glowAmount.xxx + (1.0f - glowAmount) * DiffuseColorLU(In.textureUV.z);

    float4 result;
    // we mostly use the glow with a little bit of the shading to show volume.
    result.rgb = shading *.2f + glow * .8f;
    
    // and some fog
    result.rgb = lerp(result.rgb, FogColor, In.eye.w);
    result.a = 0.9f;
    return result;
}

float4 NonTexturedColorPSBokuFace_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    float4 diffuse = DiffuseColorLU(In.textureUV.z);
    diffuse = diffuse * float4(BokuFace(In.textureUV.xy).rgb, 1.0f);
    
    diffuse = ColorPS_SURF_SM3(In, diffuse);

	return diffuse;
}


float4 TexturedColorPSBokuFace_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    return NonTexturedColorPSBokuFace_SURF_SM3(In);
}

float4 NonTexturedColorPSWideFace_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    float4 diffuse = DiffuseColorLU(In.textureUV.z);
    diffuse = float4(WideFace(In.textureUV.xy, diffuse).rgb, 1.0f);
    
    diffuse = ColorPS_SURF_SM3(In, diffuse);

	return diffuse;
}


float4 TexturedColorPSWideFace_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    return NonTexturedColorPSWideFace_SURF_SM3(In);
}

float4 NonTexturedColorPSTwoFace_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    float4 diffuse = DiffuseColorLU(In.textureUV.z);
    diffuse = float4(TwoFace(In.textureUV.xy, diffuse, In.textureUV.z).rgb, 1.0f);
    
    diffuse = ColorPS_SURF_SM3(In, diffuse);

	return diffuse;
}


float4 TexturedColorPSTwoFace_SURF_SM3( COLOR_VS_OUTPUT_SURF_SM3 In ) : COLOR0
{
    return NonTexturedColorPSTwoFace_SURF_SM3(In);
}


//
// Techniques
//
technique TexturedColorPass_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorSimpTexVS_SURF_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPS_SURF_SM3();

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


technique NonTexturedColorPass_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorSimpTexVS_SURF_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SURF_SM3();

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

technique NonTexturedColorPassCloud_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 CloudColorPS_SURF_SM3();

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

technique NonTexturedColorPassWithFlex_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithFlexVS_SURF_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SURF_SM3();

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

technique TexturedColorPassFace_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorSimpTexVS_SURF_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPSBokuFace_SURF_SM3();

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


technique NonTexturedColorPassWithSkinning_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SURF_SM3();

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

technique TexturedColorPassWithSkinning_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPS_SURF_SM3();

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

technique NonTexturedColorPassBokuFaceWithSkinning_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPSBokuFace_SURF_SM3();

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

technique NonTexturedColorPassWideFaceWithSkinning_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPSWideFace_SURF_SM3();

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

technique NonTexturedColorPassTwoFaceWithSkinning_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPSTwoFace_SURF_SM3();

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

technique TexturedColorPassBokuFaceWithSkinning_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPSBokuFace_SURF_SM3();

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

technique TexturedColorPassWideFaceWithSkinning_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPSWideFace_SURF_SM3();

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

technique TexturedColorPassTwoFaceWithSkinning_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithSkinVS_SURF_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPSTwoFace_SURF_SM3();

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

technique NonTexturedColorPassWithWind_SURF_SM3
{
	pass P0
	{
        VertexShader = compile vs_3_0 ColorWithWindVS_SURF_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SURF_SM3();

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

technique TexturedColorPassWithFlex_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorTexWithFlexVS_SURF_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPS_SURF_SM3();

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


technique TexturedColorPassFoliage_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 FoliageColorVS_SURF_SM3();
        PixelShader  = compile ps_3_0 TexturedFoliageColorPS_SURF_SM3();

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


technique NonTexturedColorPassFoliage_SURF_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 FoliageColorVS_SURF_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SURF_SM3();

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


#include "Ghost_SURF_SM3.fx"

#endif // SURFACE_SM3_FX
