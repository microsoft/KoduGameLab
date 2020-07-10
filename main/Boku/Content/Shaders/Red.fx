// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.



float4x4    WorldViewProjMatrix = float4x4(float4(1.0f, 0.0f, 0.0f, 0.0f),
											float4(0.0f, 1.0f, 0.0f, 0.0f),
											float4(0.0f, 0.0f, 1.0f, 0.0f),
											float4(0.0f, 0.0f, 0.0f, 1.0f));

// TODO: add effect parameters here.

struct VertexShaderInput
{
    float4 Position : POSITION0;

    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;

    // TODO: add vertex shader outputs such as colors and texture
    // coordinates here. These values will automatically be interpolated
    // over the triangle, and provided as input to your pixel shader.
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(input.Position, WorldViewProjMatrix);

    // TODO: add your vertex shader code here.

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // TODO: add your pixel shader code here.

    return float4(1, 0, 0, 1);
}

technique Technique1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();

        // Alpha blending
        AlphaBlendEnable = false;

        CullMode = CW;

        ZEnable = true;
        ZFunc = LessEqual;
        ZWriteEnable = true;
    }
}
