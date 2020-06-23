//
//  Screen Space Quad shaders
//

//
// Variables
//

float4 DiffuseColor;
float DiffuseAlpha;

texture DiffuseTexture;
texture ShadowMaskTexture;
texture MaskTexture;
float4 YLimits;

// Split texture specific
texture LeftTexture;
texture RightTexture;
float T;

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
sampler2D LeftTextureSampler =
sampler_state
{
    Texture = <LeftTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};
sampler2D RightTextureSampler =
sampler_state
{
    Texture = <RightTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};
sampler2D ShadowMaskTextureSampler =
sampler_state
{
    Texture = <ShadowMaskTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;

    AddressU = Clamp;
    AddressV = Clamp;
};
sampler2D MaskTextureSampler =
sampler_state
{
    Texture = <MaskTexture>;
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
    float4 position     : POSITION;     // vertex position
    float2 textureUV    : TEXCOORD0;    // vertex texture coords
};

//
// Vertex shader
//
VS_OUTPUT
VS( float2 pos : POSITION0,
    float2 tex : TEXCOORD0 )
{
    VS_OUTPUT   Output;

    Output.position = float4( pos.x, pos.y, 0.0f, 1.0f );
    Output.textureUV = tex;

    return Output;
}   // end of VS()

//
// Pixel shaders
//
float4
TexturedPS( VS_OUTPUT In ) : COLOR0
{
    float4 result = DiffuseColor * tex2D( DiffuseTextureSampler, In.textureUV );

    return result;
}   // end of TexturedPS()

float4
TexturedAlphaPS( VS_OUTPUT In ) : COLOR0
{
    float4 result = DiffuseColor * tex2D( DiffuseTextureSampler, In.textureUV );
    result.a *= DiffuseAlpha;

    return result;
}   // end of TexturedAlphaPS()

float4
MaskTexturedPS( VS_OUTPUT In ) : COLOR0
{
	float2 uv = In.textureUV;
	//uv.y *= 0.75f;
	float4 mask = tex2D( MaskTextureSampler, uv );
    float4 result = mask.r * DiffuseColor * tex2D( DiffuseTextureSampler, In.textureUV );

    return result;
}   // end of MaskTexturedPS()

// Version of masking that just uses limits based of on V tex coord.
float4
YLimitTexturedPS( VS_OUTPUT In ) : COLOR0
{
    float4 result = 0;
	float2 uv = In.textureUV;
    float4 text = tex2D( DiffuseTextureSampler, uv );

    result = DiffuseColor * text;

    float mask = smoothstep( YLimits.x, YLimits.y, uv.y ) * smoothstep( YLimits.w, YLimits.z, uv.y );

    result *= mask;

    return result;
}   // end of MaskTexturedPS()

float4
TexturedNoAlphaPS( VS_OUTPUT In ) : COLOR0
{
	float4 result = DiffuseColor;
	result.rgb *= tex2D( DiffuseTextureSampler, In.textureUV );
	return result;
}

float4
SplitTexturedPS( VS_OUTPUT In ) : COLOR0
{
    float4 left = tex2D( LeftTextureSampler, In.textureUV );
    float4 right = tex2D( RightTextureSampler, In.textureUV );

	float4 result = T > In.textureUV.x ? left : right;
	
    return result;
}   // end of TexturedPS()

//
// Drop Shadow -- This requires a shadow mask texture.  This texture should
//                have the "shape" of the object in white in the RGB channels
//                and a blurred version of this in the alpha channel for the 
//                shadow.
//
float4
DropShadowPS( VS_OUTPUT In ) : COLOR0
{
    float4 diffuse = tex2D( DiffuseTextureSampler, In.textureUV );
    float4 mask = tex2D( ShadowMaskTextureSampler, In.textureUV );
    
    float4 result;
    result.rgb = diffuse.rgb * mask.rgb;
    result.a = mask.a;

    return result;
}   // end of DropShadowPS()

//
// SolidColorWithDrop Shadow -- This requires a shadow mask texture.  This texture should
//                              have the "shape" of the object in white in the RGB channels
//                              and a blurred version of this in the alpha channel for the 
//                              shadow.  The RGB channels are attenuated by the DiffuseColor
//
float4
SolidColorWithDropShadowPS( VS_OUTPUT In ) : COLOR0
{
    float4 mask = tex2D( ShadowMaskTextureSampler, In.textureUV );
    
    float4 result;
    result.rgb = DiffuseColor.rgb * mask.rgb;
    result.a = max(mask.r, mask.a);

    return result;
}   // end of SolidColorWithDropShadowPS()

//
// SolidColor -- Just a solid fill in the DiffuseColor.
//
float4
SolidColorPS( VS_OUTPUT In ) : COLOR0
{
    float4 result = DiffuseColor;

    return result;
}   // end of SolidColorPS()

//
// Gradient -- Fill in the sky gradient.
//
float4
GradientPS( VS_OUTPUT In ) : COLOR0
{
    float4 result = float4(0, 0, 0, 0);
    
    // Use V value of input to determine color.

	float z = 1.0f - In.textureUV.y;
	
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
}   // end of SolidColorPS()


//
// Stencil
//
float4
StencilPS( VS_OUTPUT In ) : COLOR0
{
    float4 result;
    result.rgba = DiffuseColor.rgba;

    return result;
}   // end of StencilPS()

//
// TexturedNoAlpha
//
technique TexturedNoAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedNoAlphaPS();

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
        ZWriteEnable = false;
    }
}

//
// TexturedRegularAlpha
//
technique TexturedRegularAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedPS();

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
// TexturedDiffuseAlpha
//
technique TexturedDiffuseAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedAlphaPS();

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
// TexturedRegularAlphaNoZ
//
technique TexturedRegularAlphaNoZ
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// TexturedPreMultAlpha
//
technique TexturedPreMultAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedPS();

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
// MaskTexturedRegularAlpha
//
technique MaskTexturedRegularAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 MaskTexturedPS();

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
// MaskTexturedPreMultAlpha
//
technique MaskTexturedPreMultAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 MaskTexturedPS();

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
// YLimitTexturedRegularAlpha
//
technique YLimitTexturedRegularAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 YLimitTexturedPS();

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
// YLimitTexturedPreMultAlpha
//
technique YLimitTexturedPreMultAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 YLimitTexturedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
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
// AdditiveBlend
//
technique AdditiveBlend
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = One;
        DestBlend = One;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// AdditiveBlendWithAlpha
//
technique AdditiveBlendWithAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 TexturedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
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
// DropShadow
//
technique DropShadow
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 DropShadowPS();

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
// SolidColor
//
technique SolidColor
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 SolidColorPS();

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
// SolidColorNoAlpha
//
technique SolidColorNoAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 SolidColorPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = true;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = false;

        CullMode = None;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// SolidColorWithDropShadow
//
technique SolidColorWithDropShadow
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 SolidColorWithDropShadowPS();

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
// Stencil
//
technique Stencil
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 StencilPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = Zero;
        DestBlend = One;

        CullMode = None;
        
        StencilEnable = true;
        StencilFunc = Always;
        StencilPass = Replace;
        StencilRef = 1;


        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}

//
// Gradient
//
technique Gradient
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 GradientPS();

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
// SplitTexture
//
technique SplitTexturedRegularAlpha
{
    pass P0
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader  = compile ps_2_0 SplitTexturedPS();

        /* // Alpha test
        AlphaRef = 1;
        AlphaTestEnable = false;
        AlphaFunc = GreaterEqual; */

        // Alpha blending
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;

        CullMode = None;

        ZEnable = false;
        ZFunc = LessEqual;
        ZWriteEnable = false;
    }
}





