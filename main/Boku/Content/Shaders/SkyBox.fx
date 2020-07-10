// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//
//  SkyBox -- Renders the environment map as a sky box.
//

#include "Globals.fx"
#include "StandardLight.fx" // for the environment map

//
// Variables
//
float3 ViewDir;
float3 UpDir;
float3 RightDir;
float3 Eye;
float DomeRadius;
float3 DomeCenter;

// Gradient colors for each tap.  The alpha channel
// contains a 0..1 value for their position in the gradient.
// These positions must be strictly ascending.
float4 Color0;
float4 Color1;
float4 Color2;
float4 Color3;
float4 Color4;

//
// Texture samplers
//

//
// Vertex shader output structure
//
struct VS_OUTPUT
{
    float4 position     : POSITION;     // vertex position
    float3 dir          : TEXCOORD0;    // view direction for this vertex
};

#include "QuadUvToPos.fx"


//
// Vertex shader
//
VS_OUTPUT
VS( float2 tex : TEXCOORD0 )
{
    VS_OUTPUT   Output;

    Output.position = QuadUvToPos(tex, 1.0f);
    Output.dir = normalize( ViewDir 
			+ UpDir * Output.position.y 
			+ RightDir * Output.position.x );

    return Output;
}   // end of VS()

//
// Pixel shaders
//
struct PS_OUTPUT
{
    float4 color    : COLOR0;
};

PS_OUTPUT
PS( VS_OUTPUT In ) : COLOR0
{
    PS_OUTPUT result;

    result.color = texCUBE( EnvMapSampler, In.dir );
    result.color.rgb = lerp(result.color.rgb, FogColor, FogVector.z);
    result.color.a = 1.0f;
    
    return result;
}   // end of PS()

// forward decl
float4 Gradient( float z );

float4
GradientPS( VS_OUTPUT In ) : COLOR0
{
	// Use Z value of input normal to determine color.
	float3 dir = normalize( In.dir );
	
	// Shift to range 0..1
	float z = dir.z * 0.5f + 0.5f;
    
    float4 result = Gradient( z );
    
    return result;
}	// end of GradientPS()

float4
SmallDomePS( VS_OUTPUT In ) : COLOR0
{
	float3 dir = normalize( In.dir );
	
	float3 v = DomeCenter - Eye;
	float b = dot( v, dir );
	float dotVV = dot( v, v );
	float disc = b * b - dotVV + DomeRadius * DomeRadius;

	float4 result = float4(0, 0, 0, 1);
	if( disc >= 0 )
	{
		disc = sqrt( disc );
		
		// This is simplified a bit since we know we're starting inside the 
		// sphere.  This guarantees that we hit and that the positive hit
		// is the one we want.
		float t = b + disc;
		
		float3 hitPoint = Eye + t * dir;
		
		float z = hitPoint.z / DomeRadius;
		z = z * 0.5f + 0.5f;
		
		result = Gradient( z );
	}	
	
	return result;

}	// end of SmallDomePS()

float4
Gradient( float z )
{
    float4 result;

	if( z < Color0.a )
	{
		result = Color0;
	}
	else if( z < Color1.a )
	{
		z = (z - Color0.a) / ( Color1.a - Color0.a);
		z = smoothstep( 0.0f, 1.0f, z );
		result = lerp( Color0, Color1, z );
	}
	else if( z < Color2.a )
	{
		z = (z - Color1.a) / ( Color2.a - Color1.a);
		z = smoothstep( 0.0f, 1.0f, z );
		result = lerp( Color1, Color2, z );
	}
	else if( z < Color3.a )
	{
		z = (z - Color2.a) / ( Color3.a - Color2.a);
		z = smoothstep( 0.0f, 1.0f, z );
		result = lerp( Color2, Color3, z );
	}
	else if( z < Color4.a )
	{
		z = (z - Color3.a) / ( Color4.a - Color3.a);
		z = smoothstep( 0.0f, 1.0f, z );
		result = lerp( Color3, Color4, z );
	}
	else
	{
		result = Color4;
	}
	
    result.a = 1.0f;
    
    return result;
}   // end of Gradient()

//
// Just copy the image
//
technique EnvironmentMappedSkyBox
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 PS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

technique SkyBox
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        //PixelShader  = compile ps_2_0 GradientPS();
        PixelShader  = compile ps_2_0 SmallDomePS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
//
// Effects pass.
//
//

PS_OUTPUT
PSEffects( VS_OUTPUT In ) : COLOR0
{
    PS_OUTPUT result;

	// Return no-blur .r (because the sky box texture can be pre-blurred),
	// but max distance .g.
	result.color = float4(0.f, 1.f, 0.f, 0.f);
    
    return result;
}   // end of PS()

technique SkyBoxEffects
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 PSEffects();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}
