
#ifndef ROAD_SM2_FX
#define ROAD_SM2_FX

#define SM2_BRUTE

struct COLOR_VS_OUTPUT_SM2
{
    float4 position         : POSITION;
    float4 diffuse          : COLOR0;
    float4 textureUV        : TEXCOORD0;
    float4 shadowUV         : TEXCOORD1;
    float2 fogTexWgt        : TEXCOORD2; // fog, strength of tex0
};

COLOR_VS_OUTPUT_SM2 ColorVS_SM2(
                        float3 position : POSITION,
                        float3 normal   : NORMAL,
                        float4 texSelect : COLOR0,
                        float2 tex      : TEXCOORD0)
{
    COLOR_VS_OUTPUT_SM2   Output;

    // Transform our position.
    Output.position = mul( float4(position, 1.0f), WorldViewProjMatrix );

    // Transform the normals into world coordinates and normalize.
    normal = normalize( mul( normal.xyz, WorldMatrix) );
    
    // Transform our coordinates into world space
    float4 worldPosition = mul( float4(position, 1.0f), WorldMatrix );

    // Calc the eye vector.  This is the direction from the point to the eye.
    float4 eyeDist = EyeDist(worldPosition.xyz);

	float2 uvVertical = float2(tex.x, worldPosition.z);
	float2 uvHorizontal = HorizontalUV(worldPosition);
	float uvSelect = texSelect.a;
	Output.textureUV = lerp(uvHorizontal.xyxy, uvVertical.xyxy, uvSelect);

#ifdef SM2_LUZ
    float3 luzPos;
    float3 luzCol;
    AverageLuz(worldPosition, normal, luzPos, luzCol);
#endif // SM2_LUZ

    // Calc lighting.
	float4 gloss = GlossVtx(Shininess, eyeDist.xyz, normal, SpecularPower);

    float4 diffuseLight = DiffuseLight(normal, 1.0f, DiffuseColor, LightWrap);
    
    float3 specularLight = gloss.a * LightColor0 * SpecularColor; // do we want to shadow specular?

    float4 result;
    result =  EmissiveColor + diffuseLight;
    
#ifdef SM2_LUZ
    result.rgb += ApplyLuz(worldPosition, normal, luzPos, luzCol, DiffuseColor);
#endif SM2_LUZ

#ifdef SM2_BRUTE
	result.rgb += PointLights(worldPosition, normal) * DiffuseColor;
#endif // SM2_BRUTE
    
    result.rgb += specularLight;

    result.rgb += gloss.rgb;

    Output.diffuse = result;

    Output.shadowUV = ShadowCoord(worldPosition.xy);

    Output.fogTexWgt.x = CalcFog( eyeDist.w );
    Output.fogTexWgt.y = texSelect.r;

    return Output;
}

//
// Pixel shader that does all of the work for both
// textured and untextured cases.
//
float4 ColorPS_SM2( COLOR_VS_OUTPUT_SM2 In, float4 diffuseColor ) : COLOR0
{
    // Calc effect of shadow.
    float2 shadow = ShadowAtten(In.shadowUV);

    // Return the shadowed
    float4 result = diffuseColor;
    result.rgb *= shadow.r;
    return result;
}   // end of ColorPS()


//
// Pixel shader for textured subsets.
//
float4 TexturedColorPS_SM2( COLOR_VS_OUTPUT_SM2 In ) : COLOR0
{
    // Sample the texture.
    float4 diffuseColor0 = tex2D( DiffuseTexture0Sampler, In.textureUV.xy * UVXfm.xx );
    float4 diffuseColor1 = tex2D( DiffuseTexture1Sampler, In.textureUV.zw * UVXfm.yy );
    float4 diffuseColor = In.diffuse * lerp(diffuseColor0,
											  diffuseColor1,
											  In.fogTexWgt.yyyy);

    float4 result = ColorPS_SM2( In, diffuseColor );

    // Add in fog.
    result.rgb = lerp(result, FogColor, In.fogTexWgt.x);

    return result;
}

//
// Pixel shader for non-textured subsets.
//
float4 NonTexturedColorPS_SM2( COLOR_VS_OUTPUT_SM2 In ) : COLOR0
{
    float4 result = ColorPS_SM2( In, In.diffuse );

    // Add in fog.
    result.rgb = lerp(result, FogColor, In.fogTexWgt.x);

    return result;
}

//
// Techniques
//
technique TexturedColorPass_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS_SM2();
        PixelShader  = compile ps_2_0 TexturedColorPS_SM2();

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


technique NonTexturedColorPass_SM2
{
    pass P0
    {
        VertexShader = compile vs_2_0 ColorVS_SM2();
        PixelShader  = compile ps_2_0 NonTexturedColorPS_SM2();

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


#endif // ROAD_SM2_FX

