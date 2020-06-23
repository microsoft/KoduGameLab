//
// UI -- Shaders to handle the standard materials for UI elements from 3DS Max via Xbf Files
//

#include "Globals.fx"
#include "StandardLight.fx"

//
// Locals.
//

// The world view and projection matrices
float4x4    WorldViewProjMatrix;
float4x4    WorldMatrix;

// Material info.

texture     DiffuseTexture;
texture     OverlayTexture;

//
// Texture samplers
//
sampler2D DiffuseTextureSampler =
sampler_state
{
    Texture = <DiffuseTexture>;
    MipFilter = None;
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D OverlayTextureSampler =
sampler_state
{
    Texture = <OverlayTexture>;
    MipFilter = None;
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = Clamp;
    AddressV = Clamp;
};

//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float2 textureUV        : TEXCOORD0;    // vertex texture coords
    float4 positionWorld    : TEXCOORD1;    // position in world space after transform
    float3 normal           : TEXCOORD2;    // normal in world space after transform
    float3 eye              : TEXCOORD4;    // vector to eye from point
};

// Transform our coordinates into world space
COLOR_VS_OUTPUT ColorVS(
                        float3 position : POSITION,
                        float3 normal   : NORMAL)
{
    COLOR_VS_OUTPUT   Output;

    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );

    // Transform the normals into world coordinates and normalize.
    Output.normal = normalize( mul( normal.xyz, WorldMatrix) );

    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( pos, WorldMatrix );
    Output.positionWorld = worldPosition;

    // Calc the eye vector.  This is the direction from the point to the eye.
    float4 eyeDist = EyeDist(worldPosition.xyz);

    Output.eye = eyeDist.xyz;  // Normalized already

    Output.textureUV = float2(0.0f, 0.0f);

    return Output;
}

COLOR_VS_OUTPUT ColorTexVS(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float2 tex      : TEXCOORD0)
{
	COLOR_VS_OUTPUT Output = ColorVS(position, normal);
	Output.textureUV = tex;
	
	return Output;
}

// forward decls
float4 ColorPS( COLOR_VS_OUTPUT In, float4 diffuseColor );

//
// Pixel shader for textured subsets.
//
float4 TwoTextureColorPS( COLOR_VS_OUTPUT In ) : COLOR0
{
    // Sample the textures.
    float4 diffuseColor = tex2D( DiffuseTextureSampler, In.textureUV );
    float4 overlayColor = tex2D( OverlayTextureSampler, In.textureUV );
    
    // Blend textures
    float4 result = DiffuseColor;
    
    // Blends the background texture.
    result.rgb = diffuseColor.rgb * diffuseColor.a + ( 1.0f - diffuseColor.a ) * DiffuseColor.rgb;
    
    // Add the overlay.
	result.rgb = overlayColor.rgb + ( 1.0f - overlayColor.a ) * result.rgb;
	
    result = ColorPS( In, result );
	result.a = 1.0;

    return result;
}

//
// Pixel shader for textured subsets with no background texture.
//
float4 OneTextureColorPS( COLOR_VS_OUTPUT In ) : COLOR0
{
    // Sample the textures.
    float4 overlayColor = tex2D( OverlayTextureSampler, In.textureUV );
    
    // Add the overlay.
    float4 result = DiffuseColor;
    //result.rgb = overlayColor.rgb * overlayColor.a + ( 1.0f - overlayColor.a ) * DiffuseColor.rgb;
	if(overlayColor.a > 0)
	{
		result.rgb = overlayColor.rgb + ( 1.0f - overlayColor.a ) * DiffuseColor.rgb;
	}

    result.a = 1.0f;
    
    result = ColorPS( In, result );
    
    return result;
}

//
// Pixel shader for non-textured subsets.
//
float4 NoTextureColorPS( COLOR_VS_OUTPUT In ) : COLOR0
{
    float4 result = ColorPS( In, DiffuseColor );

    return result;
}

//
// Pixel shader that does all of the work for both
// textured and untextured cases.
//
float4 ColorPS( COLOR_VS_OUTPUT In, float4 diffuseColor : COLOR0 ) : COLOR0
{
    // Normalize our vectors.
    float3 normal = normalize( In.normal );
    //float3 eye = normalize( In.eye );
    // HACK to get around lighting blowing out on long reflexes.
    float3 eye = normalize(float3(-0.1, 0.05, 0.5));

    // Create a reflection vector
    float EdotN = saturate( dot( eye, normal ) );
    float3 reflection = normalize( 2 * EdotN * normal - eye );

    // HACK HACK
    // Calc effect of shadow.
    // float attenuation = CalcShadow4x4( In.shadowPosition );
    // Ignore shadows for now.
    float attenuation = 1.0f;

    // Calc lighting.
    // Light0
    float LdotN = -dot( LightDirection0.xyz, normal );
    LdotN = saturate( LdotN * LightWrap.y + LightWrap.z );
    float specular = pow( saturate( -dot( LightDirection0.xyz, reflection ) ), SpecularPower );
    float3 diffuseLight = attenuation * LdotN * LightColor0;
    float3 specularLight = attenuation * specular * LightColor0;

    // Sample env map
    //float3 env = texCUBE( EnvMapSampler, reflection );
    float fresnel = 1.0f - abs( dot( normal, eye ) );

    float4 result;
    result.rgb =  EmissiveColor + diffuseColor * diffuseLight + SpecularColor * specularLight;
    result.rgb += fresnel * Shininess; // * env;

    // Get alpha from diffuse texture.
    result.a = diffuseColor.a;
    
    // Return the combined color
    return result;
}   // end of ColorPS()

//
// Techniques
//
technique TwoTextureColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexVS();
        PixelShader  = compile ps_2_0 TwoTextureColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}


technique OneTextureColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorTexVS();
        PixelShader  = compile ps_2_0 OneTextureColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}


technique NoTextureColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 NoTextureColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}



//
// Vertex shader output structure
//
struct DEPTH_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float4 color            : TEXCOORD0;    // depth values
};

// Transform our coordinates into world space
DEPTH_VS_OUTPUT DepthVS(
                            float3 position : POSITION,
                            float3 normal   : NORMAL,
                            float2 tex      : TEXCOORD0)
{
    DEPTH_VS_OUTPUT   Output;

    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );

    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( pos, WorldMatrix );

    Output.color = 0.0f;

    return Output;
}

//
// Pixel shader
//
float4 DepthPS( DEPTH_VS_OUTPUT In ) : COLOR0
{
    float4 result = In.color;
    
    return result;
}

//
// Technique
//
technique DepthPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 DepthVS();
        PixelShader  = compile ps_2_0 DepthPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}




