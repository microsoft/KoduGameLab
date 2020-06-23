
#ifndef WATERHEIGHT_H
#define WATERHEIGHT_H

float    WaveCycle;      // Where in the wave cycle we are.
float    WaveHeight;		// Max wave amplitude
float2	 WaveCenter;		// Epicenter of our single wave source
float	 InverseWaveLength; // 2 * PI / WaveLength;
float	 BaseHeight;		// DC height of water surface


void HeightAndNormal(float2 waveCenter, float invWaveLength, float2 center, out float height, out float3 normal)
{
	float2 dir = center.xy - waveCenter;
	float waveOffset = length(dir);
	dir *= 1.0f / waveOffset;
	waveOffset *= invWaveLength;

    float2 heightNorm;
    sincos(waveOffset - WaveCycle, heightNorm.x, heightNorm.y);
    heightNorm.y *= WaveHeight;
    heightNorm.x -= 1.0f;

	height = heightNorm.x * WaveHeight + BaseHeight;

    normal = float3(-dir * heightNorm.y, 1.0f);
	normal = normalize(normal);	
}

float Height(float baseHeight, float2 waveCenter, float invWaveLength, float2 center)
{
	float2 dir = center.xy - waveCenter;
	float waveOffset = length(dir);
	waveOffset *= invWaveLength;

    float height = sin(waveOffset - WaveCycle) - 1.0f;
    height = height * WaveHeight;

	height += baseHeight;
	
	return height;
}

float Height(float2 waveCenter, float invWaveLength, float2 center)
{
	return Height(BaseHeight, waveCenter, invWaveLength, center);
}


#endif // WATERHEIGHT_H
