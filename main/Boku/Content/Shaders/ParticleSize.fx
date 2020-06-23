
#ifndef PARTICLE_SIZE_FX

float4x4 WorldViewProjMatrix;

float4 ParticleRadius;

float2 ParticleSize(float baseRadius, float distance)
{
	float2 radius = baseRadius * ParticleRadius.xx / distance.xx;
	radius = clamp(radius, ParticleRadius.zz, ParticleRadius.ww);
	radius *= distance.xx;
	
	return radius;
}

float4 ParticleProject(float3 pos, float radius, float2 tex)
{
    // Move texture coords into -1, 1 range.
    tex.xy = 2.0f * ( tex.xy - 0.5f );

    // Transform our position.
    float4 position = mul( float4(pos, 1.0f), WorldViewProjMatrix );
    
    float2 particleSize = tex * ParticleSize(radius, position.w);
	particleSize.y *= -ParticleRadius.y;
    
    position.xy += particleSize;

	return position;
}

float4 ParticleProject(float3 pos, float radius, float rotation, float2 tex)
{
    // Move texture coords into -1, 1 range.
    tex.xy = 2.0f * ( tex.xy - 0.5f );
    
    // Transform our position.
    float4 position = mul( float4(pos, 1.0f), WorldViewProjMatrix );

    // Offset world position based on UV coords.
    float sine;
    float cosine;
    sincos( rotation, sine, cosine );
    
	// Compute ndc space offset from position.
    tex.xy = tex.xy * ParticleSize(radius, position.w);
    
    // Rotate.
    float2 coords = float2( tex.x*cosine - tex.y*sine, tex.x*sine + tex.y*cosine );

	coords.y *= -ParticleRadius.y;

	position.xy += coords;
	
	return position;
}
#endif // PARTICLE_SIZE_FX