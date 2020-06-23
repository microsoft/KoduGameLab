
#ifndef SURFACE_SM2_FX
#define SURFACE_SM2_FX

#define SM2_BRUTE

//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT_SURF_SM2
{
    float4 position         : POSITION;     // vertex position
    float4 diffuse          : COLOR0;
    float4 specular         : COLOR1;       // matidx in specular.a
    float3 textureUV        : TEXCOORD0;    // Fog in .z
};

/// Don't call this directly, it can't fold in the LocalToModel for you, because
/// you might have already folded in the palette or flex or something.
COLOR_VS_OUTPUT_SURF_SM2 ColorVS_SURF_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float matIdx)
{
    COLOR_VS_OUTPUT_SURF_SM2   Output;

    // Transform our position.
    Output.position = mul( float4(position, 1.0f), WorldViewProjMatrix );

    // Transform the normals into world coordinates and normalize.
    normal = normalize( mul( normal.xyz, WorldMatrix) );

    // Transform the position into world coordinates for calculating the eye vector.
    float3 worldPosition = mul( float4(position, 1.0f), WorldMatrix ).xyz;

    // Calc the eye vector.  This is the direction from the point to the eye.
    float4 eyeDist = EyeDist(worldPosition.xyz);

    Output.textureUV = float3(0.0f, 0.0f, 0.0f);

    // Calc lighting.
    float4 specularColor = SpecularColorLU(matIdx);
	float4 gloss = GlossVtx(EnvIntensityLU(matIdx), eyeDist.xyz, normal, specularColor.w);

    float4 diffuseLight = DiffuseLight(normal);
    
    float3 specularLight = gloss.a * specularColor; // do we want to shadow specular?

    float4 result;
    result =  diffuseLight;

//    float3 luzPos;
//    float3 luzCol;

    /// Can't currently afford these, we run out of vertex shader 
    /// instructions (max 256 for VS_2_0). The worst case is WithWind,
    /// but WithSkinning is also slightly over. %P
//    AverageLuz(worldPosition, normal, luzPos, luzCol);
//    result.rgb += ApplyLuz(worldPosition, normal, luzPos, luzCol, 1.0f.xxx);

#ifdef SM2_BRUTE
	result.rgb += PointLights(worldPosition, normal);
#endif // SM2_BRUTE

    result.rgb *= DiffuseColorLU(matIdx);

    result.rgb += EmissiveColorLU(matIdx);
    
    Output.diffuse = result;
    Output.specular.rgb = (specularLight + gloss.rgb * 0.25f) * LightColor0;
    Output.specular.a = matIdx;

    Output.textureUV.z = CalcFog( eyeDist.w );

    return Output;
}

COLOR_VS_OUTPUT_SURF_SM2 ColorSimpVS_SURF_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float3 matSelect : COLOR0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    return ColorVS_SURF_SM2(position, normal, MaterialIndex(matSelect));
}

COLOR_VS_OUTPUT_SURF_SM2 ColorTexVS_SURF_SM2(
							float3 position : POSITION,
							float3 normal	: NORMAL,
                            float3 matSelect : COLOR0,
							float2 tex		: TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

	COLOR_VS_OUTPUT_SURF_SM2 Output = ColorVS_SURF_SM2(position, normal, MaterialIndex(matSelect));
	Output.textureUV.xy = tex;
	
	return Output;
}

// Vertex shader for fish.  Uses Flex param to bend shape left/right around Z axis.
COLOR_VS_OUTPUT_SURF_SM2 ColorWithFlexVS_SURF_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float3 matSelect : COLOR0)
{
    // First, transform the fishbones.  Assumes transform is affine.
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    float3 pos = ApplyFlex(position, normal);
    
    return ColorVS_SURF_SM2(pos, normal, MaterialIndex(matSelect));
}
COLOR_VS_OUTPUT_SURF_SM2 ColorTexWithFlexVS_SURF_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float3 matSelect : COLOR0,
                        float2 tex      : TEXCOORD0)
{
    // First, transform the fishbones.  Assumes transform is affine.
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    float3 pos = ApplyFlex(position, normal);
    
    COLOR_VS_OUTPUT_SURF_SM2 Out = ColorVS_SURF_SM2(pos, normal, MaterialIndex(matSelect));
    Out.textureUV.xy = tex;
    return Out;
}

COLOR_VS_OUTPUT_SURF_SM2 ColorTexWithSkinVS_SURF_SM2(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin4(input);
    
    float matIdx = MaterialIndex(input.color.rgb);

	COLOR_VS_OUTPUT_SURF_SM2 Out = ColorVS_SURF_SM2(skin.position, skin.normal, matIdx);
    
    Out.textureUV.xy = input.texcoord;
    return Out;
}

COLOR_VS_OUTPUT_SURF_SM2 ColorWithSkinVS_SURF_SM2(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin4(input);
    
    float matIdx = MaterialIndex(input.color.rgb);

	return ColorVS_SURF_SM2(skin.position, skin.normal, matIdx);
}

COLOR_VS_OUTPUT_SURF_SM2 ColorWithWindVS_SURF_SM2(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    // Calculate the skinned normal and position
    SKIN_OUTPUT skin = Skin8(input);
    
    float matIdx = MaterialIndex(input.color.rgb);

	COLOR_VS_OUTPUT_SURF_SM2 Out = ColorVS_SURF_SM2(skin.position, skin.normal, matIdx);
    Out.textureUV.xy = input.texcoord;

    return Out;
}

COLOR_VS_OUTPUT_SURF_SM2 FoliageColorVS_SURF_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float3 matSelect : COLOR0,
                        float2 tex      : TEXCOORD0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    normal = normalize( mul( normal.xyz, WorldMatrix) );
    if (normal.z < 0.0f)
        normal = -normal;

    return ColorTexVS_SURF_SM2(position, normal, matSelect, tex);
}

COLOR_VS_OUTPUT_SURF_SM2 CloudColorVS_SURF_SM2( 
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float matIdx)
{
    COLOR_VS_OUTPUT_SURF_SM2 Output = ColorVS_SURF_SM2(position, normal, matIdx);

    normal = normalize( mul( normal.xyz, WorldMatrix) );

    // normals pointing down get diffuse color; up get white color
    float glowAmount = normal.z * 0.5f + 0.5f;		
    float3 glow = glowAmount.xxx + (1.0f - glowAmount) * DiffuseColorLU(matIdx);
//glow = glowAmount;

    Output.diffuse.rgb = Output.diffuse.rgb *.2f + glow * .8f;
//Output.diffuse.rgb = glow;

    Output.diffuse.a = 0.9f;

    Output.specular = float4(0.0f, 0.0f, 0.0f, matIdx);

    return Output;
}

COLOR_VS_OUTPUT_SURF_SM2 CloudSimpColorVS_SURF_SM2( 
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float3 matSelect : COLOR0)
{
    position = PrepPosition(position);
    normal = PrepNormal(normal);

    return CloudColorVS_SURF_SM2(position, normal, MaterialIndex(matSelect));
}

COLOR_VS_OUTPUT_SURF_SM2 CloudColorWithSkinVS_SURF_SM2(in SKIN_VS_INPUT input)
{
    input.position = PrepPosition(input.position);
    input.normal = PrepNormal(input.normal);

    SKIN_OUTPUT skin = Skin4(input);

    float matIdx = MaterialIndex(input.color.rgb);

    return CloudColorVS_SURF_SM2(skin.position, skin.normal, matIdx);
}

//
// Pixel shader that does all of the work for both
// textured and untextured cases.
//
float4 ColorPS_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In) : COLOR0
{
    float4 result = In.diffuse;
    result.rgb += In.specular.rgb;

    result.rgb = lerp(result.rgb, FogColor, In.textureUV.z);

    return result;
}   // end of ColorPS()


//
// Pixel shader for textured subsets.
//
float4 TexturedColorPS_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV );

    In.diffuse *= diffuseColor;

    float4 result = ColorPS_SURF_SM2( In );

    return result;
}

//
// Pixel shader for textured foliage subsets.
//
//  Warning, this is very hacked...
//
float4 TexturedFoliageColorPS_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV );
    
    // Brighten the whole thing.
    diffuseColor *= 2.0f;

    // Use the V coord as an ambient occlusion factor.
    diffuseColor *= 1.0f - In.textureUV.y * 0.8f;
    
    In.diffuse *= diffuseColor;
    float4 result = ColorPS_SURF_SM2( In );

    return result;
}

//
// Pixel shader for non-textured subsets.
//
float4 NonTexturedColorPS_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    float4 result = ColorPS_SURF_SM2( In );

    return result;
}

float4 CloudColorPS_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    return ColorPS_SURF_SM2(In);
}

float4 NonTexturedColorPSBokuFace_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    In.diffuse *= BokuFace(In.textureUV.xy);

    return ColorPS_SURF_SM2(In);
}

float4 TexturedColorPSBokuFace_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    return NonTexturedColorPSBokuFace_SURF_SM2(In);
}

float4 NonTexturedColorPSWideFace_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    In.diffuse = WideFace(In.textureUV.xy, In.diffuse);

    return ColorPS_SURF_SM2(In);
}

float4 TexturedColorPSWideFace_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    return NonTexturedColorPSWideFace_SURF_SM2(In);
}

float4 NonTexturedColorPSTwoFace_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    In.diffuse = TwoFace(In.textureUV.xy, In.diffuse, In.specular.a);

    return ColorPS_SURF_SM2(In);
}

float4 TexturedColorPSTwoFace_SURF_SM2( COLOR_VS_OUTPUT_SURF_SM2 In ) : COLOR0
{
    return NonTexturedColorPSTwoFace_SURF_SM2(In);
}

//
// Techniques
//
technique TexturedColorPass_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexVS_SURF_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPS_SURF_SM2();

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


technique NonTexturedColorPass_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorSimpVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SURF_SM2();

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

technique NonTexturedColorPassCloud_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 CloudColorWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 CloudColorPS_SURF_SM2();

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

technique NonTexturedColorPassWithFlex_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorWithFlexVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SURF_SM2();

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

technique TexturedColorPassFace_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexVS_SURF_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPSBokuFace_SURF_SM2();

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

technique NonTexturedColorPassWithSkinning_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 ColorWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SURF_SM2();

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

technique TexturedColorPassWithSkinning_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPS_SURF_SM2();

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

technique NonTexturedColorPassBokuFaceWithSkinning_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 ColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPSBokuFace_SURF_SM2();

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

technique NonTexturedColorPassWideFaceWithSkinning_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 ColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPSWideFace_SURF_SM2();

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

technique NonTexturedColorPassTwoFaceWithSkinning_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 ColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPSTwoFace_SURF_SM2();

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

technique TexturedColorPassBokuFaceWithSkinning_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPSBokuFace_SURF_SM2();

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

technique TexturedColorPassWideFaceWithSkinning_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPSWideFace_SURF_SM2();

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

technique TexturedColorPassTwoFaceWithSkinning_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexWithSkinVS_SURF_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPSTwoFace_SURF_SM2();

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

technique NonTexturedColorPassWithWind_SURF_SM2
{
	pass P0
	{
        VertexShader = compile vs_2_0 ColorWithWindVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SURF_SM2();

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

technique TexturedColorPassWithFlex_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexWithFlexVS_SURF_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPS_SURF_SM2();

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


technique TexturedColorPassFoliage_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 FoliageColorVS_SURF_SM2();
        PixelShader  = compile ps_2_0 TexturedFoliageColorPS_SURF_SM2();

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


technique NonTexturedColorPassFoliage_SURF_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 FoliageColorVS_SURF_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SURF_SM2();

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

#include "Ghost_SURF_SM2.fx"

#endif // SURFACE_SM2_FX

