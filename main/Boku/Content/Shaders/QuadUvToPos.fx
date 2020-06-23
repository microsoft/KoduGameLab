
#ifndef QUAD_UV_TO_POS_FX
#define QUAD_UV_TO_POS_FX

float4 UvToPos;
float4 UvToSource;

float4 QuadUvToPos(float2 uv, float z)
{
	float4 uv4 = float4(uv.x, uv.y, z, 1.0f);
	uv4.xy = uv.xy * UvToPos.xy + UvToPos.zw;
	return uv4;
}

float2 QuadUvToSource(float2 uv)
{
	return uv * UvToSource.xy + UvToSource.zw;
}

#endif // QUAD_UV_TO_POS_FX
