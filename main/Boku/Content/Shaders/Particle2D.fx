
//
// Particle2D -- A collection of shaders for tuextured, 2D particles.
//

//
// Shared Globals.
//

#include "Globals.fx"
#include "DOF.fx"
#include "ParticleSize.fx"

//
// Locals.
//

// The world view and projection matrices
float4x4 WorldMatrix;

float4 DiffuseColor;    // Base color to tint sprite.
float  TileOffset;

// Values for SimpleParticle stuff.
float  CurrentTime;
float  MaxAge;
float3 Gravity;

// Textures
texture DiffuseTexture;

// Distortion specifics
shared texture Bump;
shared float BumpStrength = 1.0f;
shared float BlurStrength = 1.0f;
shared float4 BumpScroll = float4(0.0f, 0.0f, 0.0f, 0.0f);
shared float4 BumpScale = float4(1.0f, 1.0f, 1.0f, 1.0f);

shared texture DepthTexture;     // R channel is amount of DOF needed. G is W / DOF_FarPlane

sampler2D DepthTextureSampler =
sampler_state
{
    Texture = <DepthTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};


//
// Texture samplers
//
sampler2D DiffuseTextureSampler =
sampler_state
{
    Texture = <DiffuseTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D DistortBumpSampler =
sampler_state
{
    Texture = <Bump>;
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
    float4 position     : POSITION;     // Vertex position
    float2 textureUV    : TEXCOORD0;    // Vertex texture coords
    float alpha         : TEXCOORD1;    // Overall alpha amount.
};

//
// Color Pass Vertex Shader
//
VS_OUTPUT ColorVS(
            float3 position : POSITION,
            float3 tex      : TEXCOORD0, 
            float3 params   : TEXCOORD1)    // rotation, radius, alpha
{
    VS_OUTPUT   Output;

    // Copy alpha and texture over, untouched.    
    Output.alpha = params.z;
    Output.textureUV.xy = tex.xy;
    Output.textureUV.x *= TileOffset;
    Output.textureUV.x += tex.z * TileOffset;
    
    float rotation = params.x;
    float radius = params.y;
    
    // Transform position in world coords.
    //float4 worldPosition = mul( position, WorldMatrix );
    //worldPosition /= worldPosition.w;
    
    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - position;
    eyeDir = normalize( eyeDir );
    
	Output.position = ParticleProject(position, radius, rotation, tex);
	
    return Output;
}   // end of ColorVS()

//
// Vertex shader output structure
//
struct VS_SIMPLE_OUTPUT
{
    float4 position     : POSITION;     // Vertex position
    float2 textureUV    : TEXCOORD0;    // Vertex texture coords
    float4 color        : TEXCOORD1;    // Color
};

//
// Color Pass Vertex Shader
//
VS_SIMPLE_OUTPUT SimpleParticleColorVS(
            float3 origin   : POSITION,
            float3 velocity : TEXCOORD0,
            float2 tex      : TEXCOORD1,
            float2 radii    : TEXCOORD2,
            float birthTime : TEXCOORD3,
            float4 color    : COLOR )
{
    VS_SIMPLE_OUTPUT    Output;

    float dt = CurrentTime - birthTime;
    float t = saturate( dt / MaxAge );

    // Copy texture over, untouched.    
    Output.textureUV.xy = tex;
    
    // Fade color.
    Output.color = color * DiffuseColor;
    Output.color.a *= 1 - t;
    
    // Calc position.
    float3 position = origin + dt * velocity;
    
    // Apply gravity.
    position += Gravity * dt * dt / 2.0f;
    
    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - position;
    eyeDir = normalize( eyeDir );
    
    float radius = radii.x + t * ( radii.y - radii.x );

	Output.position = ParticleProject(position, radius, tex);
    
    return Output;

}   // end of SimpleParticleColorVS()

//
// Color Pass Pixel Shader
//
float4 ColorPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = DiffuseColor * tex2D( DiffuseTextureSampler, In.textureUV );
    
    diffuseColor.a *= In.alpha;
    
    return diffuseColor;

}   // end of PS()

float4 SimpleParticleColorPS( VS_SIMPLE_OUTPUT In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = In.color * tex2D( DiffuseTextureSampler, In.textureUV );
    
    return diffuseColor;

}   // end of SimpleParticleColorPS()






//
// TexturedColorPassNormalAlpha technique
//
technique TexturedColorPassNormalAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 ColorPS();

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
        ZWriteEnable = false;
    }
}

//
// TexturedColorPassPremultipliedAlpha technique
//
technique TexturedColorPassPremultipliedAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 ColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// TexturedColorPassOneOneBlend technique
//
technique TexturedColorPassOneOneBlend
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS();
        PixelShader  = compile ps_2_0 ColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = One;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// TexturedColorPassSimpleParticleOneOneBlend technique
//
technique TexturedColorPassSimpleParticleOneOneBlend
{
    pass P0
    {
        VertexShader = compile vs_2_0 SimpleParticleColorVS();
        PixelShader  = compile ps_2_0 SimpleParticleColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = One;

        CullMode = None;

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
    float4 position     : POSITION;     // Vertex position
    float2 textureUV    : TEXCOORD0;    // Vertex texture coords
    float3 alphaDOF     : TEXCOORD1;    // Overall alpha amount, and DOF
};

// Transform our coordinates into world space
DEPTH_VS_OUTPUT DepthVS(
            float3 position : POSITION,
            float3 tex      : TEXCOORD0, 
            float3 params   : TEXCOORD1)    // rotation, radius, alpha
{
	VS_OUTPUT colorOutput = ColorVS(position, tex, params);
	
    DEPTH_VS_OUTPUT   Output;

	Output.position = colorOutput.position;
	Output.textureUV = colorOutput.textureUV;
	Output.alphaDOF.x = colorOutput.alpha;
	
    Output.alphaDOF.yz  = CalcDOF( colorOutput.position.w ).rg;

    return Output;
}

//
// Pixel shader
//
float4 DepthPS( DEPTH_VS_OUTPUT In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = 0;
    
    diffuseColor.a = DiffuseColor.a * tex2D( DiffuseTextureSampler, In.textureUV ).a * In.alphaDOF.x;
    
    diffuseColor.rg = In.alphaDOF.yz;
     
    return diffuseColor;
}

//
// Depth technique
//
technique DepthPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 DepthVS();
        PixelShader  = compile ps_2_0 DepthPS();

        /* // Alpha test
        AlphaRef = 10;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// Distortion
//

//
// Vertex shader output structure
//
struct DISTORT_VS_OUTPUT
{
    float4 position     : POSITION;     // Vertex position
    float3 uvAlpha		: TEXCOORD0;    // Vertex texture coords and alpha
    float4 screenPos	: TEXCOORD1;
};

// Transform our coordinates into world space
DISTORT_VS_OUTPUT DistortVS(
            float3 position : POSITION,
            float3 tex      : TEXCOORD0, 
            float3 params   : TEXCOORD1)    // rotation, radius, alpha
{
	params.y *= 2.0f;
	VS_OUTPUT colorOutput = ColorVS(position, tex, params);
	
    DISTORT_VS_OUTPUT   Output;

	Output.position = colorOutput.position;
	Output.uvAlpha.xy = colorOutput.textureUV;
	Output.uvAlpha.z = colorOutput.alpha;
	
    // Record it in a useful form
    Output.screenPos = Output.position;
    Output.screenPos.xy /= Output.position.w;
    Output.screenPos = Output.screenPos * float4(0.5f, -0.5f, 1.f, 1.f / DOF_FarPlane) 
										+ float4(0.5f, 0.5f, 0.f, 0.f);
	
    return Output;
}

struct DISTORT_PS_OUTPUT
{
	float4		rt0 : COLOR0;
	float4		rt1 : COLOR1;
};

//
// Pixel shader
//
DISTORT_PS_OUTPUT DistortPS( DISTORT_VS_OUTPUT In ) : COLOR0
{
	DISTORT_PS_OUTPUT Output;
	
    float alpha = DiffuseColor.a * tex2D( DiffuseTextureSampler, In.uvAlpha.xy ).a * In.uvAlpha.z;
    
    float4 depth = tex2D( DepthTextureSampler, In.screenPos.xy );
    
    alpha *= In.screenPos.w > depth.g ? 0.0f : 1.0f;

	float3 norm = tex2D( DistortBumpSampler, In.uvAlpha.xy * BumpScale.xy + BumpScroll.xy ) 
					* 2.0f - 1.0f;
	norm += tex2D( DistortBumpSampler, In.uvAlpha.xy * BumpScale.xy + BumpScroll.zw )
					* 2.0f - 1.0f;
	norm = normalize(norm);
	float4 offset = saturate(float4(-norm.x, -norm.y, norm.x, norm.y));
	offset *= alpha;

	// Glint.rgb and blurriness.w into rt0
	Output.rt0.rgb = offset.w * offset.w;
Output.rt0.rgb = 0.0f;
	Output.rt0.a = alpha * BlurStrength;

	// Distortion offset into rt1.	
	Output.rt1 = offset * BumpStrength;
	
//Output.rt0.r = depth.g;
//Output.rt0.g = In.screenPos.w;
//Output.rt0.b = saturate(depth.g - In.screenPos.w);
	
	return Output;
}

//
// Distortion technique
//
technique DistortionPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 DistortVS();
        PixelShader  = compile ps_2_0 DistortPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        BlendOp = Max;
        SrcBlend = One;
        DestBlend = One;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

