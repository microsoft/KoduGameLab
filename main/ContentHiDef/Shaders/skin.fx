
#ifndef SKIN_H
#define SKIN_H


float4x4 MatrixPalette[15];
float4x4 RestPalette[15];

shared float WindStrength = 1.0f;

#if 0 /// see comments below in WindStrength()
shared float2 WindRemap;
shared float4	Wind0;
shared float4	Wind1;
shared float4	Wind2;
#endif

// This is passed into our vertex shader from Xna
struct SKIN_VS_INPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
	float3 normal : NORMAL0;
	float4 indices : BLENDINDICES0;
	float4 weights : BLENDWEIGHT0;
};

// This is passed out from our vertex shader once we have processed the input
//struct VS_OUTPUT
//{
//	float4 position : POSITION;
//    float4 normal : NORMAL0;
//	float2 texcoord : TEXCOORD0;
//};

// This is the output from our skinning method
struct SKIN_OUTPUT
{
    float4 position;
    float3 normal;
};

// This calculates the skinned vertex and normal position based on the blend indices
// and weights.
// For four indices and four weights, which is what this shader uses,
// the formula for position vertex Vi, weight array W, index array I, and matrix array M is:
// Vf = Vi*W[0]*M[I[0]] + Vi*W[1]*M[I[1]] + Vi*W[2]*M[I[2]] + Vi*W[3]*M[I[3]]
// In fact, the weights may not always add up to 1,
// so we replace the last weight with:
// W[3] = (1 - W[2] - W[1] - W[0])
// The formula is the same for calculating the skinned normal position.
SKIN_OUTPUT Skin4( const SKIN_VS_INPUT input)
{
    SKIN_OUTPUT output = (SKIN_OUTPUT)0;

    float lastWeight = 1.0;
    for (int i = 0; i < 3; ++i)
    {
		
        float weight = input.weights[i];
        lastWeight -= weight;        
        output.position     += mul( input.position, MatrixPalette[input.indices[i]]) * weight;
        output.normal       += mul( input.normal  , MatrixPalette[input.indices[i]]) * weight;
    }
    
    output.position     += mul( input.position, MatrixPalette[input.indices[3]])*lastWeight;
    output.normal       += mul( input.normal  , MatrixPalette[input.indices[3]])*lastWeight;
    return output;
};

#if 0
/// Moving this to the CPU, it's constant over the entire model anyway.
/// The preshader should be picking it up, but isn't, so we'll do it
/// manually.
float WindStrength(float2 pos)
{
	float2 p = pos - Wind0.xy;
	p *= Wind0.zw;
	p = cos(p);
	
	float strength = saturate(p.x + p.y);

	p = pos - Wind1.xy;
	p *= Wind1.zw;
	p = cos(p);
	
	strength += saturate(p.x + p.y);

	p = pos - Wind2.xy;
	p *= Wind2.zw;
	p = cos(p);
	
	strength += saturate(p.x * p.y);
	
	return saturate(strength * WindRemap.x + WindRemap.y);
}
#endif

SKIN_OUTPUT Skin8( const SKIN_VS_INPUT input)
{
    SKIN_OUTPUT output = (SKIN_OUTPUT)0;
    
    float activeStrength = WindStrength; // 1.0f - WindStrength(WorldMatrix[3].xy);

    float lastWeight = 1.0;
    for (int i = 0; i < 3; ++i)
    {
		
        float weight = input.weights[i];
        lastWeight -= weight;
        
        float activeWgt = activeStrength * weight;
        
        output.position     += mul( input.position, MatrixPalette[input.indices[i]]) * activeWgt;
        output.normal       += mul( input.normal  , MatrixPalette[input.indices[i]]) * activeWgt;

        float restWgt = (1.0f - activeStrength) * weight;

        output.position     += mul( input.position, RestPalette[input.indices[i]]) * restWgt;
        output.normal       += mul( input.normal  , RestPalette[input.indices[i]]) * restWgt;
    }
    
    float lastActiveWgt = activeStrength * lastWeight;

    output.position     += mul(input.position, MatrixPalette[input.indices[3]]) * lastActiveWgt;
    output.normal       += mul(input.normal  , MatrixPalette[input.indices[3]]) * lastActiveWgt;

	float lastRestWgt = (1.0f - activeStrength) * lastWeight;
	
    output.position     += mul(input.position, RestPalette[input.indices[3]]) * lastRestWgt;
    output.normal       += mul(input.normal  , RestPalette[input.indices[3]]) * lastRestWgt;

    return output;
};

#endif // FLEX_H