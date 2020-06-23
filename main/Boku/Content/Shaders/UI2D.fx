//
// UI2D -- Shaders for 2d UI elements
//

#include "Globals.fx"

//
// Locals.
//

// The world view and projection matrices
float4x4    WorldViewProjMatrix;
float4x4    WorldMatrix;

// Material info.
float4      DiffuseColor;		// For no texture, this is the color of the element.  With a texture, 
								// this is the color that shows through where the texture alpha < 1.0
float4      SpecularColor;
float       SpecularPower;
float       Shininess;

float       Alpha;				// Overall alpha applied to complete element.
float		Grey;				// Used to transition to grey scale.

texture     DiffuseTexture;
texture		OverlayTexture;
texture     NormalMap;

//
// Texture samplers
//
sampler2D DiffuseTextureSampler =
sampler_state
{
    Texture = <DiffuseTexture>;
    MipFilter = None;   // This texture is generated on the fly and doesn't have any mip-map levels.
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D OverlayTextureSampler =
sampler_state
{
    Texture = <OverlayTexture>;
    MipFilter = None;   // This texture is generated on the fly and doesn't have any mip-map levels.
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D NormalMapSampler =
sampler_state
{
    Texture = <NormalMap>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = Clamp;
    AddressV = Clamp;
};

shared texture  EnvironmentMap;

samplerCUBE EnvMapSampler =
sampler_state
{
    Texture = <EnvironmentMap>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = WRAP;
    AddressV = WRAP;
};


//
// Vertex shader output structure
//
struct VS_OUTPUT
{
    float4 position     : POSITION;     // Vertex position.
    float2 normalMapUV  : TEXCOORD0;    // Texture coords for normal map.
    float2 diffuseUV    : TEXCOORD1;    // Texture coords for diffuse texture.
    float3 eye          : TEXCOORD2;    // Vector to eye from point.
};

//
// Vertex shader
//
VS_OUTPUT
VS( float2 pos          : POSITION0,
    float2 normalMapTex : TEXCOORD0,
    float2 diffuseTex   : TEXCOORD1 )
{
    VS_OUTPUT   Output;

    // Expand the position to homogenious coords.
    float4 position = float4( pos.x, pos.y, 0.0f, 1.0f );

    // Transform.
    Output.position = mul( position, WorldViewProjMatrix );
    
    // Transform the position into world coordinates for calculating the eye vector.
    float3 worldPosition = mul( position, WorldMatrix );
    
    // Calc the eye vector.  This is the direction from the point to the eye.
    Output.eye = normalize( EyeLocation - worldPosition );

    // Pass through texture coords.
    Output.normalMapUV = normalMapTex;
    Output.diffuseUV = diffuseTex;

    return Output;
}   // end of VS()

//
// Pixel shaders
//

// forward decl
float4 BaseNormalMappedPS( VS_OUTPUT In, float4 diffuse ) : COLOR0;

float4
NormalMappedPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the diffuse texture.
    float4 diffuse = tex2D( DiffuseTextureSampler, In.diffuseUV );
	
    // Blend with base color.
    diffuse = lerp( DiffuseColor, diffuse, diffuse.a );

    return BaseNormalMappedPS( In, diffuse );
}

float4
AltNormalMappedPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the decal texture.
    float4 decalColor = tex2D( DiffuseTextureSampler, In.diffuseUV );

    // Get the normal and transform it back into world space.
    float4 normalSample = tex2D( NormalMapSampler, In.normalMapUV );
    
    float3 normal = normalSample.xyz;

    // Shift from 0..1 to -1..1.
    normal = normal * 2.0f - 1.0f;
    
    // Transform into world coords.
    normal = mul( normal, WorldMatrix );

    // This shouldn't be required if we don't mind 
    // living with the 8 bit quantizing of the normal.
    normal = normalize( normal );

    // Normalize our eye vector.
    float3 eye = normalize( In.eye );

    // Create a reflection vector    
    float EdotN = saturate( dot( eye, normal ) );
    float3 reflection = normalize( 2 * EdotN * normal - eye );

    // Calc lighting.
    // Light0
    float LdotN = saturate( -dot( UILightDirection0.xyz, normal ) );
    float specular = pow( saturate( -dot( UILightDirection0.xyz, reflection ) ), SpecularPower );
    float3 diffuseLight = LdotN * UILightColor0;
    float3 specularLight = specular * UILightColor0;

    // Ignore diffuse on second and third lights.

    // Light1
    LdotN = saturate( -dot( UILightDirection1.xyz, normal ) );
    specular = pow( saturate( -dot( UILightDirection1.xyz, reflection ) ), SpecularPower );
    //diffuseLight += LdotN * UILightColor1;
    specularLight += specular * UILightColor1;
    
    // Light2
    LdotN = saturate( -dot( UILightDirection2.xyz, normal ) );
    specular = pow( saturate( -dot( UILightDirection2.xyz, reflection ) ), SpecularPower );
    //diffuseLight += LdotN * UILightColor2;
    specularLight += specular * UILightColor2;
    
    float4 result;
    // Light the underlying geometry.
    float4 geomColor;
    geomColor.rgb = DiffuseColor.rgb * diffuseLight.rgb + SpecularColor.rgb * specularLight.rgb;
    geomColor.a = DiffuseColor.a * Alpha;
    
    result.rgb = lerp( geomColor, decalColor, decalColor.a );
    
    // Mask corners.
    result.rgb *= normalSample.a;
    result.a = normalSample.a * ( decalColor.a + geomColor.a * ( 1 - decalColor.a ) );
    
    return result;
}	// end of AltNormalMappedPS()

float4
AltNormalMappedPreMultAlphaPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the decal texture.
    float4 decalColor = tex2D( DiffuseTextureSampler, In.diffuseUV );

    // Get the normal and transform it back into world space.
    float4 normalSample = tex2D( NormalMapSampler, In.normalMapUV );
    
    float3 normal = normalSample.xyz;

    // Shift from 0..1 to -1..1.
    normal = normal * 2.0f - 1.0f;
    
    // Transform into world coords.
    normal = mul( normal, WorldMatrix );

    // This shouldn't be required if we don't mind 
    // living with the 8 bit quantizing of the normal.
    normal = normalize( normal );

    // Normalize our eye vector.
    float3 eye = normalize( In.eye );

    // Create a reflection vector    
    float EdotN = saturate( dot( eye, normal ) );
    float3 reflection = normalize( 2 * EdotN * normal - eye );

    // Calc lighting.
    // Light0
    float LdotN = saturate( -dot( UILightDirection0.xyz, normal ) );
    float specular = pow( saturate( -dot( UILightDirection0.xyz, reflection ) ), SpecularPower );
    float3 diffuseLight = LdotN * UILightColor0;
    float3 specularLight = specular * UILightColor0;

    // Light1
    LdotN = saturate( -dot( UILightDirection1.xyz, normal ) );
    specular = pow( saturate( -dot( UILightDirection1.xyz, reflection ) ), SpecularPower );
    diffuseLight += LdotN * UILightColor1;
    specularLight += specular * UILightColor1;
    
    // Light2
    LdotN = saturate( -dot( UILightDirection2.xyz, normal ) );
    specular = pow( saturate( -dot( UILightDirection2.xyz, reflection ) ), SpecularPower );
    diffuseLight += LdotN * UILightColor2;
    specularLight += specular * UILightColor2;
    
    float4 result;
    // Light the underlying geometry.
    float4 geomColor;
    geomColor.rgb = DiffuseColor.rgb * diffuseLight.rgb + SpecularColor.rgb * specularLight.rgb;
    geomColor.a = DiffuseColor.a * Alpha;
    
	result.rgb = decalColor + ( 1 - decalColor.a ) * geomColor;
    
    // Mask corners.
    result.rgb *= normalSample.a;
    result.a = normalSample.a * ( decalColor.a + geomColor.a * ( 1 - decalColor.a ) );
    
    return result;
}	// end of AltNormalMappedPreMultAlphaPS()

float4
NormalMappedNoAlphaInTexturePS( VS_OUTPUT In ) : COLOR0
{
    // Sample the diffuse texture.
    float4 diffuse = tex2D( DiffuseTextureSampler, In.diffuseUV );
	diffuse.a = 1.0f;

    return BaseNormalMappedPS( In, diffuse );
}

float4
NormalMappedWithOverlayPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the diffuse texture.
    float4 diffuse = tex2D( DiffuseTextureSampler, In.diffuseUV );
    float4 overlay = tex2D( OverlayTextureSampler, In.diffuseUV );
	
	// Blend overlay onto diffuse texture.
	diffuse.rgb = lerp( diffuse.rgb, overlay.rgb, overlay.a );

    // Blend with base color.
    diffuse = lerp( DiffuseColor, diffuse, diffuse.a );

    return BaseNormalMappedPS( In, diffuse );
}

float4
NormalMappedNoTexturePS( VS_OUTPUT In ) : COLOR0
{
    return BaseNormalMappedPS( In, DiffuseColor );
}

float4
BaseNormalMappedPS( VS_OUTPUT In, float4 diffuse ) : COLOR0
{
    // Get the normal and transform it back into world space.
    float4 normalSample = tex2D( NormalMapSampler, In.normalMapUV );
    
    float3 normal = normalSample.xyz;

    // Shift from 0..1 to -1..1.
    normal = normal * 2.0f - 1.0f;
    
    // Transform into world coords.
    normal = mul( normal, WorldMatrix );

    // This shouldn't be required if we don't mind 
    // living with the 8 bit quantizing of the normal.
    normal = normalize( normal );

    // Normalize our eye vector.
    float3 eye = normalize( In.eye );

    // Create a reflection vector    
    float EdotN = saturate( dot( eye, normal ) );
    float3 reflection = normalize( 2 * EdotN * normal - eye );

    // Calc lighting.
    // Light0
    float LdotN = saturate( -dot( UILightDirection0.xyz, normal ) );
    float specular = pow( saturate( -dot( UILightDirection0.xyz, reflection ) ), SpecularPower );
    float3 diffuseLight = LdotN * UILightColor0;
    float3 specularLight = specular * UILightColor0;

    // Light1
    LdotN = saturate( -dot( UILightDirection1.xyz, normal ) );
    specular = pow( saturate( -dot( UILightDirection1.xyz, reflection ) ), SpecularPower );
    diffuseLight += LdotN * UILightColor1;
    specularLight += specular * UILightColor1;
    
    // Light2
    LdotN = saturate( -dot( UILightDirection2.xyz, normal ) );
    specular = pow( saturate( -dot( UILightDirection2.xyz, reflection ) ), SpecularPower );
    diffuseLight += LdotN * UILightColor2;
    specularLight += specular * UILightColor2;
    
    // Sample env map
    //float3 env = texCUBE( EnvMapSampler, reflection );
    
    // Fake a fresnel-like term.
    //float fresnel = 1.0f - abs( dot( normal, eye ) );
    
    float4 result;
    result.rgb = diffuse.a * (diffuse.rgb * diffuseLight.rgb) + SpecularColor.rgb * specularLight.rgb;
    
    // Use the alpha channel of the normal map as a mask
    // and attenuate by overall alpha.
    result.rgb *= normalSample.a * Alpha;
    
    result.a = normalSample.a * diffuse.a * Alpha;

    return result;
}   // end of BaseNormalMappedPS()

float4
NormalMappedWithEnvPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the diffuse texture.
    float4 diffuse = DiffuseColor * tex2D( DiffuseTextureSampler, In.diffuseUV );

    // Get the normal and transform it back into world space.
    float4 normalSample = tex2D( NormalMapSampler, In.normalMapUV );
    
    float3 normal = normalSample.xyz;

    // Shift from 0..1 to -1..1.
    normal = normal * 2.0f - 1.0f;
    
    // Transform into world coords.
    normal = mul( normal, WorldMatrix );

    // This shouldn't be required if we don't mind 
    // living with the 8 bit quantizing of the normal.
    normal = normalize( normal );

    // Normalize our eye vector.
    float3 eye = normalize( In.eye );

    // Create a reflection vector    
    float EdotN = saturate( dot( eye, normal ) );
    float3 reflection = normalize( 2 * EdotN * normal - eye );

    // Calc lighting.
    // Light0
    float LdotN = saturate( -dot( UILightDirection0.xyz, normal ) );
    float specular = pow( saturate( -dot( UILightDirection0.xyz, reflection ) ), SpecularPower );
    float3 diffuseLight = LdotN * UILightColor0;
    float3 specularLight = specular * UILightColor0;

    // Ignore diffuse on second and third lights

    // Light1
    LdotN = saturate( -dot( UILightDirection1.xyz, normal ) );
    specular = pow( saturate( -dot( UILightDirection1.xyz, reflection ) ), SpecularPower );
    //diffuseLight += LdotN * UILightColor1;
    specularLight += specular * UILightColor1;
    
    // Light2
    LdotN = saturate( -dot( UILightDirection2.xyz, normal ) );
    specular = pow( saturate( -dot( UILightDirection2.xyz, reflection ) ), SpecularPower );
    //diffuseLight += LdotN * UILightColor2;
    specularLight += specular * UILightColor2;
    
    // Sample env map
    float3 env = texCUBE( EnvMapSampler, reflection );
    
    // Fake a fresnel-like term.
    float fresnel = 1.0f - abs( dot( normal, eye ) );
    
    // Multiplying the specular by the fresnel term is designed to kill off any 
    // specular reflections on the front face of the tile to help improve readability.
    float4 result;
    result.rgb = diffuse.a * (diffuse.rgb * diffuseLight.rgb) + fresnel * SpecularColor.rgb * specularLight.rgb;
    result.rgb = lerp(result.rgb, env, fresnel);
    
    // Use the alpha channel of the normal map as a mask
    // and attenuate by overall alpha.
    result.rgb *= normalSample.a * Alpha;
    
    result.a = normalSample.a * diffuse.a * Alpha;

    return result;
}   // end of NormalMappedWithEnvPS()

float4
TexturedRegularAlphaPS( VS_OUTPUT In ) : COLOR0
{
	// We're using the normal mapp UVs here because we want the diffuse
	// textured stretched to fit the 9grid.
	float4 result = DiffuseColor * tex2D( DiffuseTextureSampler, In.normalMapUV );
	result.a *= Alpha;
	
	return result;
	
}	// end of TexturedRegularAlphaPS()

float4
GreyFlatPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the decal texture.
    float4 decalColor = tex2D( DiffuseTextureSampler, In.diffuseUV );
    float4 normalSample = tex2D( NormalMapSampler, In.normalMapUV );
    
    float4 result = decalColor + ( 1 - decalColor.a ) * DiffuseColor;
    
    // Calc greyscale image.
    // TODO (scoy) Should this be NTSC colors or does even weighting look better?
    float3 greyMapping = float3( 0.33f, 0.33f, 0.33f );
    float3 grey = dot( greyMapping, result.rgb );
    
    // Blend between color and greyscale.
    result.rgb = lerp( result.rgb, grey.rgb, Grey );
    
    // Apply mask form normal map.
    result.rgb *= normalSample.a;
	result.a = normalSample.a;
	
	return result;
	
}	// end of GreyFlat()

//
// Techniques
//

//
// Textured, no normal map.
//

technique TexturedRegularAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedRegularAlphaPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
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
// Normal Mapped
//

technique NormalMapped
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 NormalMappedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique NormalMappedWithEnv
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 NormalMappedWithEnvPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique NormalMappedNoAlphaInTextureNoZ
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 NormalMappedNoAlphaInTexturePS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique NormalMappedWithOverlay
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 NormalMappedWithOverlayPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique NormalMappedNoTexture
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 NormalMappedNoTexturePS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

technique NormalMappedNoZ
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 NormalMappedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique NormalMappedNoZWithOverlay
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 NormalMappedWithOverlayPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique NormalMappedNoTextureNoZ
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 NormalMappedNoTexturePS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// Normal mapped where the texture is treated as a decal which is rendered fully lit.
//
technique AltNormalMapped
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 AltNormalMappedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

//
// Normal mapped where the texture is treated as a decal which is rendered fully lit.
//
technique AltNormalMappedPreMultAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 AltNormalMappedPreMultAlphaPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}

//
// Blends between fully lit and grey scale result.  Normal map is only used for alpha mask.
//
technique GreyFlat
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 GreyFlatPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending (assumes pre-mult)
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}
