
//
// Particle2D -- A collection of shaders for textured, 2D particles cotrolled by Perlin noise.
//

//
// Global variables
//

float3 EyeLocation;
float3 CameraUp;

//
// Locals.
//

// The world view and projection matrices
float4x4 WorldViewProjMatrix;
float4x4 WorldMatrix;

float4  WaterColor;     // RGB value to tint underwater terrain.  Alpha value is depth where
                        // maximum attenuation happens.

float4 DiffuseColor;    // Base color for tinting sprites.
float2 BaseUV;          // Base used for sampling noise.
float Amplitude;        // How much noise to add;
float Sync;             // How much to scale the value that goes into the UV coord.  Making this smaller
                        // causes neighboring particles to be more in sync.

// Textures
texture DiffuseTexture;
texture NoiseTexture;

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

sampler2D NoiseTextureSampler =
sampler_state
{
    Texture = <NoiseTexture>;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
    /*
    MipFilter = Linear;         // Most cards don't support VS texture filtering!
    MinFilter = Linear;
    MagFilter = Linear;
    */

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
    float4 color        : COLOR;        // Particle color.
};

// forward decl
VS_OUTPUT ColorVS(
            float3 position : POSITION,
            float2 tex      : TEXCOORD0, 
            float3 params   : TEXCOORD1 );  // rotation, radius, alpha


//
// MultiSample -- bilinear filtering
//

float Texel = 1.0f / 256.0f;
float HalfTexel = 0.5f / 256.0f;

VS_OUTPUT MultiSampleVS(
            float3 position : POSITION,
            float2 tex      : TEXCOORD0, 
            float3 params   : TEXCOORD1 )   // rotation, radius, alpha
{
    VS_OUTPUT   Output;
    
    // Add noise to position.
    float4 uv = float4( 0, 0, 0, 0 );
    uv.xy  = BaseUV;
    uv.xy += position.xy * Sync;
    float4 noise00 = tex2Dlod( NoiseTextureSampler, uv + float4( 0, 0, 0, 0 ) );
    float4 noise10 = tex2Dlod( NoiseTextureSampler, uv + float4( Texel, 0, 0, 0 ) );
    float4 noise01 = tex2Dlod( NoiseTextureSampler, uv + float4( 0, Texel, 0, 0 ) );
    float4 noise11 = tex2Dlod( NoiseTextureSampler, uv + float4( Texel, Texel, 0, 0 ) );
    
    float2 fraction = frac( uv.xy * 256.0f );
    
    float4 noise = lerp( lerp( noise00, noise01, fraction.y ), lerp( noise10, noise11, fraction.y ), fraction.x );
    
    position.xyz += 0.5f * Amplitude - Amplitude * noise.xyz;
    
    Output = ColorVS( position, tex, params );
    
    return Output;
}

//
// SingleSample
//
VS_OUTPUT SingleSampleVS(
            float3 position : POSITION,
            float2 tex      : TEXCOORD0, 
            float3 params   : TEXCOORD1 )   // rotation, radius, alpha
{
    VS_OUTPUT   Output;
    
    // Add noise to position.
    float4 uv = float4( 0, 0, 0, 0 );
    uv.xy  = BaseUV;
    uv.xy += position.xy * 0.1f;
    float4 noise = tex2Dlod( NoiseTextureSampler, uv );
    position.xyz += 10.0f * noise.xyz;

    Output = ColorVS( position, tex, params );
    
    return Output;
}

//
// Color Pass Vertex Shader
//
VS_OUTPUT ColorVS(
            float3 position : POSITION,
            float2 tex      : TEXCOORD0, 
            float3 params   : TEXCOORD1 )   // rotation, radius, alpha
{
    VS_OUTPUT   Output;
    
    // Copy texture over, untouched.    
    Output.textureUV = tex;
    
    float rotation = params.x;
    float radius = params.y;
    
    // Transform position in world coords.
    //float4 worldPosition = mul( position, WorldMatrix );
    //worldPosition /= worldPosition.w;
    
    // Calc the eye vector.  This is the direction from the point to the eye.
    float3 eyeDir = EyeLocation - position;
    eyeDir = normalize( eyeDir );
    
    // Calc right vector.
    float3 right = cross( CameraUp, eyeDir );
    right = normalize( right );
    
    // Calc screen space up vector.
    float3 up = cross( eyeDir, right );
    up = normalize( up );
    
    // Offset world position based on UV coords.
    float sine;
    float cosine;
    sincos( rotation, sine, cosine );
    
    // Move texture coords into -1, 1 range.
    tex = 2.0f * ( tex - 0.5f );
    
    /*
    // Rotate.
    float2 coords = float2( tex.x*cosine - tex.y*sine, tex.x*sine + tex.y*cosine );
    position += right * coords.x * radius;
    position -= up * coords.y * radius;
    */
    position += right * tex.x * radius;
    position -= up * tex.y * radius;
    
        
    // Transform our position.
    float4 pos;
    pos.xyz = position;
    pos.w = 1.0f;
    Output.position = mul( pos, WorldViewProjMatrix );

    // Add underwater tint.
    float amount = saturate( -position.z / WaterColor.a );
    Output.color.rgb = lerp(DiffuseColor.rgb, WaterColor.rgb, amount);
    Output.color.a = DiffuseColor.a * params.z;
    
    // Attenuate alpha by depth based on near clip plane.
    float alpha = 1.0f - Output.position.z / Output.position.w;
    // Fade at near clip rather than pop.
    if(alpha > 0.9)
    {
        alpha = ( 1.0f - alpha ) * 10.0f; 
        Output.color.a *= alpha;
    }
    
    return Output;
}


//
// Color Pass Pixel Shader
//
float4 ColorPS( VS_OUTPUT In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor = In.color * tex2D( DiffuseTextureSampler, In.textureUV );
    
    return diffuseColor;

}   // end of PS()


//
// NormalAlphaColorPass technique
//
technique TexturedColorPassNormalAlpha
{
    pass P0
    {
        VertexShader = compile vs_3_0 MultiSampleVS();
        PixelShader  = compile ps_3_0 ColorPS();

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

