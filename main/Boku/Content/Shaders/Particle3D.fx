
//
// Particle3D -- A collection of shaders for non-textured, 3D particles.
//

#include "Globals.fx"

#include "Fog.fx"
#include "DOF.fx"
#include "StandardLight.fx"

//
// Locals.
//

// The world view and projection matrices
float4x4    WorldViewProjMatrix;
float4x4    WorldMatrix;

// Material info.
float       GlowFactor;     // Modulates alpha based on angle between eye ray and normal.
float       Alpha;    
float		Radius; 

//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float3 normal           : TEXCOORD0;    // normal in world space after transform
    float3 eye              : TEXCOORD1;    // vector to eye from point
    float  fog              : TEXCOORD2;    // fog strength in x
};

// Transform our coordinates into world space
COLOR_VS_OUTPUT ColorVS(float3 position : POSITION)
{
    COLOR_VS_OUTPUT   Output;

    // Transform our position.
    position *= Radius;
    Output.position = mul( float4(position, 1.0f), WorldViewProjMatrix );

    // Transform the normal into world coordinates.
    Output.normal = mul( position.xyz, WorldMatrix);

    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( float4(position, 1.0f), WorldMatrix );
    //Output.positionWorld = worldPosition;

    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - worldPosition.xyz / worldPosition.w;
    float eyeDist = length(eyeDir);
    Output.eye = eyeDir / eyeDist;  // Normalize

    Output.fog = CalcFog( eyeDist );

    return Output;
}

//
// Basic Pixel shader
//
float4 BasicPS( COLOR_VS_OUTPUT In ) : COLOR0
{
    // Normalize our vectors.
    float3 normal = normalize( In.normal );
    float3 eye = normalize( In.eye );

    // Create a reflection vector
    float EdotN = saturate( dot( eye, normal ) );
    float3 reflection = normalize( 2 * EdotN * normal - eye );
    
    float4 gloss = GlossEnv(Shininess, eye, normal, SpecularPower);
    
    float3 diffuseLight = DiffuseLight(normal, 1.0f, DiffuseColor, LightWrap) + EmissiveColor;
    
    

    float4 result;
    result.rgb = diffuseLight;
    result.rgb += SpecularColor * gloss.a;
    result.rgb += gloss.rgb;

    // Set alpha.
    result.a = Alpha;

    // Return the combined color
    return result;
}   // end of BasicPS()

//
// Additve Glow Pixel shader
//
float4 PremultAlphaGlowPS( COLOR_VS_OUTPUT In ) : COLOR0
{
    // Normalize our vectors.
    float3 normal = normalize( In.normal );
    float3 eye = normalize( In.eye );

    // Calc alpha. 
    float alpha = 1.0f - abs( dot( normal, eye ) );
    alpha = Alpha * pow( abs(alpha), GlowFactor );
    
    float4 result;
    result.rgb = (DiffuseColor.rgb + EmissiveColor.rgb) * alpha;
    result.a = alpha;

    // Return the combined color
    return result;
}   // end of AdditiveGlowPS()

//
// Technique - basic, full color
//
technique BasicColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 BasicPS();

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
        ZWriteEnable = false;
    }
}

//
// Technique - opaque, full color
//
technique OpaqueColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 BasicPS();

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

//
// Technique - transparent, full color, normal alpha
//
technique TransparentColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 BasicPS();

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
        ZWriteEnable = false;
    }
}

//
// Technique - transparent, full color, normal alpha
//
technique TransparentColorPassNoZ
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 BasicPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = CCW;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// Technique - additive glow 
//
technique PremultAlphaGlowColorPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 PremultAlphaGlowPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = CCW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}


//
//
//  Depth passes.
//
//

//
// Vertex shader output structure
//
struct DEPTH_VS_OUTPUT
{
    float4 position         : POSITION;     // vertex position
    float4 color            : TEXCOORD0;    // depth values
};

// Transform our coordinates into world space
DEPTH_VS_OUTPUT DepthVS(float3 position : POSITION)
{
    DEPTH_VS_OUTPUT   Output;

    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );

    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( pos, WorldMatrix );

    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - worldPosition.xyz / worldPosition.w;
    float eyeDist = length(eyeDir);

    Output.color = CalcDOF( eyeDist );

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




