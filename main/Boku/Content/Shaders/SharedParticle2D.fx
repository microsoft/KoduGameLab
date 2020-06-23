
//
// SharedParticle2D -- A collection of shaders for textured, 2D particles which use the shared emitters.
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

float  CurrentTime;
float3 Gravity;
float4 BleepRadius; // nominal ndc size in x, in y, then min ndc size, max ndc size.
float2  Drag; // drag constant.x, minspeed.y
float4 DiffuseColor; // tint for the whole system

// Distortion specifics
shared texture Bump;
shared float BumpStrength = 1.0f;
shared float BlurStrength = 1.0f;
shared float4 BumpScroll = float4(0.0f, 0.0f, 0.0f, 0.0f);
shared float4 BumpScale = float4(1.0f, 1.0f, 1.0f, 1.0f);

// Textures
texture DiffuseTexture;
shared texture DepthTexture;     // R channel is amount of DOF needed. G is W / DOF_FarPlane

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
// Vertex shader output structure
//
struct VS_OUTPUT
{
    float4 position     : POSITION;     // Vertex position
    float2 textureUV    : TEXCOORD0;    // Vertex texture coords
    float4 color		: COLOR;		// Vertex color to be attenuated by texture.
};

float4 CurrentTint(float life, float3 flash)
{
	float2 t;
	t.x = saturate(CurrentTime * flash.x + flash.y);
	t.y = flash.z > 0 ? life * flash.z : 1.0f;
	t = smoothstep(0.0f, 1.0f, t);
	t.x = min(t.x, t.y);
	return lerp(float4(1.0f, 1.0f, 1.0f, 1.0f), DiffuseColor, t.x);
}

//
// Color Pass Vertex Shader
//
VS_OUTPUT ColorVS(
            float3 position		: POSITION,
            float3 velocity		: TEXCOORD0,
            float3 acceleration : TEXCOORD1,
            float2 tex			: TEXCOORD2,
            float2 radii		: TEXCOORD3,	// start radius, end radius
            float2 times		: TEXCOORD4,	// birth time, death time, flash scale, flash offset 
            float2 rotation		: TEXCOORD5,	// rotation start, rotation rate
            float3 flash		: TEXCOORD6,	// scale,offset,scale for muzzle flash
            float4 color		: COLOR)		// alpha is start alpha, assume final alpha = 0
{
    VS_OUTPUT   Output;

	// How long has this particle been alive?
	float life = CurrentTime - times.x;
	    
    // Calc interpolant for life, 0==birth, 1==death
    // If t>1 then this particle is dead, just not removed yet so 
    // "render" it as 0 size and totally transparent.
    float t = life / (times.y - times.x);
    
    // Copy texture over, untouched.    
    Output.textureUV.xy = tex.xy;
    
    // Color.
    color.a = saturate( color.a * ( 1 - t ) );
    
    Output.color = color * CurrentTint(life, flash.xyz);

	float rot = rotation.x + life * rotation.y;    
    float rad = lerp(radii.x, radii.y, t);
    if(t > 1.0f)
		rad = 0.0f;

	// Calc current position.  Assumes constant acceleration.
	float3 pos;
	pos.xyz = position + life * velocity + 0.5f * life * life * acceleration;
    
    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - pos.xyz;
    eyeDir = normalize( eyeDir );

	Output.position = ParticleProject(pos, rad, rot, tex);    

    return Output;
}   // end of ColorVS()

//
// Color Pass Vertex Shader
//
VS_OUTPUT ColorDragVS(
            float3 position		: POSITION,
            float3 velocity		: TEXCOORD0,
            float3 acceleration : TEXCOORD1,
            float2 tex			: TEXCOORD2,
            float2 radii		: TEXCOORD3,	// start radius, end radius
            float2 times		: TEXCOORD4,	// birth time, death time, flash scale, flash offset 
            float2 rotation		: TEXCOORD5,	// rotation start, rotation rate
            float3 flash		: TEXCOORD6,	// scale,offset,scale for muzzle flash
            float4 color		: COLOR)		// alpha is start alpha, assume final alpha = 0
{
    VS_OUTPUT   Output;

	// How long has this particle been alive?
	float life = CurrentTime - times.x;
	    
    // Calc interpolant for life, 0==birth, 1==death
    // If t>1 then this particle is dead, just not removed yet so 
    // "render" it as 0 size and totally transparent.
    float t = life / (times.y - times.x);
    
    // Copy texture over, untouched.    
    Output.textureUV.xy = tex.xy;
    
    // Color.
    color.a = saturate( color.a * ( 1 - t ) );
    
    Output.color = color * CurrentTint(life, flash.xyz);

	float rot = rotation.x + life * rotation.y;    
    float rad = lerp(radii.x, radii.y, t);
    if(t > 1.0f)
		rad = 0.0f;
		
	velocity += 0.5f * life * acceleration;
		
	float invSpeed = rsqrt(dot(velocity, velocity));
	float speed = 1.0f / invSpeed;
	velocity *= invSpeed;
	float decay = exp(life * Drag.x);
	speed = Drag.y + decay * (speed - Drag.y);
//speed = Drag.y;
	velocity *= speed;

	// Calc current position.  Assumes constant acceleration.
	float3 pos;
	pos.xyz = position + life * velocity;
    
    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - pos.xyz;
    eyeDir = normalize( eyeDir );

	Output.position = ParticleProject(pos, rad, rot, tex);    

    return Output;
}   // end of ColorVS()

//
// Color Pass Pixel Shader
//
float4 ColorPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the texture.
    float4 result = tex2D( DiffuseTextureSampler, In.textureUV );
    result *= In.color;
    
    return result;

}   // end of PS()


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
// TexturedColorPassNormalAlpha technique
//
technique TexturedColorPassNormalAlphaDrag
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorDragVS();
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
//
// Distortion
//
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

DISTORT_VS_OUTPUT DistortVS(
							float3 position		: POSITION,
							float3 velocity		: TEXCOORD0,
							float3 acceleration : TEXCOORD1,
							float2 tex			: TEXCOORD2,
							float2 radii		: TEXCOORD3,	// start radius, end radius
							float4 times		: TEXCOORD4,	// birth time, death time 
							float2 rotation		: TEXCOORD5,	// rotation start, rotation rate
				            float3 flash		: TEXCOORD6,	// scale,offset,scale for muzzle flash
							float4 color		: COLOR)		// alpha is start alpha, assume final alpha = 0
{
	VS_OUTPUT colorOutput = ColorVS(position, velocity, acceleration, tex, radii, times, rotation, flash, color);

    DISTORT_VS_OUTPUT   Output;

	Output.position = colorOutput.position;
	Output.uvAlpha.xy = colorOutput.textureUV;
	Output.uvAlpha.z = colorOutput.color.a;
	
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
	
    float alpha = tex2D( DiffuseTextureSampler, In.uvAlpha.xy ).a * In.uvAlpha.z;
    
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

struct BLEEP_VS_OUT
{
	float4 position : POSITION;
	float4 color : COLOR0;
	float2 texcoord : TEXCOORD0;
};

BLEEP_VS_OUT BleepVS(float3 position : POSITION,
					 float4 color : COLOR0,
					 float4 texcoord : TEXCOORD0)
{
	BLEEP_VS_OUT Out;
	
	float hparm = 1.0f - saturate(texcoord.z / 0.05f);
	hparm *= hparm * hparm;
	position.z = lerp(texcoord.w, position.z, 1.0f - hparm);
	
	Out.position = ParticleProject(position, 1.0f, texcoord.xy);
	
	Out.color = color;
	
	Out.texcoord = texcoord.xy;
	
	return Out;
}			

float4 BleepPS(BLEEP_VS_OUT In) : COLOR0
{
    float4 result = In.color;
    result *= tex2D( DiffuseTextureSampler, In.texcoord ).xxxx;
	return result;
}		 

//
// Distortion technique
//
technique BleepPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 BleepVS();
        PixelShader  = compile ps_2_0 BleepPS();

        /* // Alpha test
        AlphaRef = 10;
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



struct BEAM_VS_OUT
{
	float4 position : POSITION;
	float4 color : COLOR0;
	float2 texcoord : TEXCOORD0;
};

BEAM_VS_OUT BeamVS(float3 position : POSITION,
					 float4 color : COLOR0,
					 float4 texcoord : TEXCOORD0)
{
	BEAM_VS_OUT Out;
	
	Out.position = ParticleProject(position, 1.0f, texcoord.xy);
	
	Out.color = color;
	
	Out.texcoord = texcoord.xy;
	
	return Out;
}			


float4 BeamPS(BEAM_VS_OUT In) : COLOR0
{
    float4 result = In.color;
    result *= tex2D( DiffuseTextureSampler, In.texcoord ).xyzw;
    result *= tex2D( DiffuseTextureSampler, In.texcoord ).xxxx;
	return result;
}		 

//
// Distortion technique
//
technique BeamPass
{
    pass P0
    {
        VertexShader = compile vs_2_0 BeamVS();
        PixelShader  = compile ps_2_0 BeamPS();

        /* // Alpha test
        AlphaRef = 10;
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

