
#ifndef ROAD_SM3_FX
#define ROAD_SM3_FX

sampler2D NormalTexture0Sampler =
sampler_state
{
    Texture = <NormalTexture0>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = WRAP;
    AddressV = WRAP;
};
sampler2D NormalTexture1Sampler =
sampler_state
{
    Texture = <NormalTexture1>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
    
    AddressU = WRAP;
    AddressV = WRAP;
};


//
// Vertex shader output structure
//
struct COLOR_VS_OUTPUT_SM3
{
    float4 position         : POSITION;     // vertex position
    float3 luzCol			: COLOR1;
    float3 normal           : TEXCOORD0;		// normal in world space after transform
    float3 binorm			: TEXCOORD1;		// binormal in world space
    float3 tangent			: TEXCOORD2;		// tangent in world space
    float4 textureUV        : TEXCOORD3;    // vertex texture coords, tex0=>xy, tex1=>zw
    float4 positionWorld    : TEXCOORD4;    // position in world space after transform
    float3 eye              : TEXCOORD5;    // vector to eye from point
    float2 fogTexWgt		: TEXCOORD6;	// fog in x, strength of tex0 in y
    float3 luzPos			: TEXCOORD7;
};

// Transform our coordinates into world space
COLOR_VS_OUTPUT_SM3 ColorVS_SM3(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float4 texSelect : COLOR0,
                        float2 tex      : TEXCOORD0)
{
    COLOR_VS_OUTPUT_SM3   Output;

    // Transform our position.
    Output.position = mul( float4(position, 1.0f), WorldViewProjMatrix );

    // Transform the normals into world coordinates and normalize.
    Output.normal = normalize( mul( normal.xyz, WorldMatrix) );
    
    // 1rst case is flat downward projection. So
    // binormal = (0, 1, 0) x normal
    // tangent = normal x (1, 0, 0)
    float3 binorm0 = float3(normal.z, 0.0f, -normal.x);
    float3 tangen0 = float3(0.0f, normal.z, -normal.y);

    // 2nd case is binormal along direction of travel. So
    // binormal = up x norm
    float3 binorm1 = float3(-normal.y, normal.x, 0.0f);
    float3 tangen1 = cross(normal, binorm0);
    
    Output.binorm = lerp(binorm0, binorm1, texSelect.a);
    Output.tangent = lerp(tangen0, tangen1, texSelect.a);

    // Transform the position into world coordinates for calculating the eye vector.
    float4 worldPosition = mul( float4(position, 1.0f), WorldMatrix );
    Output.positionWorld = worldPosition;

    // Calc the eye vector.  This is the direction from the point to the eye.
    float4 eyeDist = EyeDist(worldPosition.xyz);
    Output.eye = eyeDist.xyz;

	float2 uvVertical = float2(tex.x, worldPosition.z);
	float2 uvHorizontal = HorizontalUV(worldPosition);
	float uvSelect = texSelect.a;
	Output.textureUV = lerp(uvHorizontal.xyxy, uvVertical.xyxy, uvSelect);

    AverageLuz(position, Output.normal, Output.luzPos, Output.luzCol);

    Output.fogTexWgt.x = CalcFog( eyeDist.w );
    Output.fogTexWgt.y = texSelect.r;

    return Output;
}

//
// Pixel shader that does all of the work for both
// textured and untextured cases.
//
float4 ColorPS_SM3( COLOR_VS_OUTPUT_SM3 In, float shine, float4 diffuseColor : COLOR0, float3 normal ) : COLOR0
{
    // Normalize our vectors.
    float3 eye = normalize( In.eye.xyz );

    // Calc effect of shadow.
    float2 shadow = Shadow(In.positionWorld.xy);

    // Calc lighting.
	float4 gloss = GlossEnvAniso(Shininess * shine, eye, normal, Aniso, SpecularPower);

    float4 diffuseLight = DiffuseLight(normal, shadow.r, diffuseColor, LightWrap);
    
    float3 specularLight = gloss.a * LightColor0 * SpecularColor * shine; // do we want to shadow specular?

    float4 result;
    result =  EmissiveColor + diffuseLight;
    
    result.rgb += ApplyLuz(In.positionWorld, normal, In.luzPos, In.luzCol, diffuseColor);
    
    result.rgb += specularLight;

    result.rgb += gloss.rgb;

    // Get alpha from diffuse texture.
    result.a = diffuseColor.a;
    
    // Return the combined color
    return result;
}   // end of ColorPS()


//
// Pixel shader for textured subsets.
//
float4 TexturedColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor0 = tex2D( DiffuseTexture0Sampler, In.textureUV.xy * UVXfm.xx );
    float4 diffuseColor1 = tex2D( DiffuseTexture1Sampler, In.textureUV.zw * UVXfm.yy );
    float4 diffuseColor = DiffuseColor * lerp(diffuseColor0,
											  diffuseColor1,
											  In.fogTexWgt.yyyy);

//    float3 normal = normalize( In.normal );
    
    float4 normalMap0 = tex2D( NormalTexture0Sampler, In.textureUV.xy * UVXfm.zz );
    float4 normalMap1 = tex2D( NormalTexture1Sampler, In.textureUV.xy * UVXfm.ww );
    float4 normalMap = lerp(normalMap0, normalMap1, In.fogTexWgt.yyyy);
    normalMap = normalMap * 2.0f - 1.0f;

//return normalMap0;
//return float4(In.tangent, 1.0f);
  
	float3 normal = mul( normalMap.xyz, float3x3(In.binorm, In.tangent, In.normal) );  
	normal = normalize(normal);
//return float4(normal, 1.0f);

    float4 result = ColorPS_SM3( In, normalMap.a, diffuseColor, normal );

    // Add in fog.
    result.rgb = lerp(result, FogColor, In.fogTexWgt.x);

//result = diffuseColor0;
//result.xy = fmod(In.textureUV.xy, 1.0f);
//result.zw = float2(0.f, 1.f);
//return In.fogTexWgt.yyyy;

    return result;
}

//
// Pixel shader for non-textured subsets.
//
float4 NonTexturedColorPS_SM3( COLOR_VS_OUTPUT_SM3 In ) : COLOR0
{
    float3 normal = normalize( In.normal );
    float4 result = ColorPS_SM3( In, 1.0f, DiffuseColor, normal );

    // Add in fog.
    result.rgb = lerp(result, FogColor, In.fogTexWgt.x);

    return result;
}

//
// Techniques
//
technique TexturedColorPass_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorVS_SM3();
        PixelShader  = compile ps_3_0 TexturedColorPS_SM3();

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


technique NonTexturedColorPass_SM3
{
    pass P0
    {
        VertexShader = compile vs_3_0 ColorVS_SM3();
        PixelShader  = compile ps_3_0 NonTexturedColorPS_SM3();

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
        ZWriteEnable = true;
    }
}

#endif // ROAD_SM3_FX
