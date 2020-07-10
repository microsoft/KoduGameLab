// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// Pie Slice shaders, Reach
//
// The Reach shaders are right on the edge of the instruction limit.  So, the
// bevel versions must be done in 2 passes.  The first pass renders the diffuse
// color and the second pass adds the specular on top.
//

#define Reach 1

#include "Code\UILighting.fx"
#include "Code\PieSlice.fx"

technique OuterShadowOnly
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowOnlyPS();
    }
}

technique InnerShadowOnly
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowOnlyPS();
    }
}

//
// Non-textured
// 

technique NoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowPS();
    }
}

technique OutlineNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowPS();
    }
}

//
// Bevel, no texture, no outline.
//
technique NoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowSlantBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowSlantBevelPS();
    }
}

technique NoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundedSlantBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowRoundedSlantBevelPS();
    }
}

technique NoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowRoundBevelPS();
    }
}

//
// Textured, no bevel
//
technique NoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTexturePS();
    }
}

technique OutlineNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTexturePS();
    }
}

//
// Bevel with outline, no texture.
//
// Note that for the specular pass, we can use the shaders from the non-outline versions.
//
technique OutlineNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowSlantBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowSlantBevelPS();
    }
}

technique OutlineNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundedSlantBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowRoundBevelPS();
    }
}

//
// Bevel with texture, no outline
//
// Note that for the specular pass, we can use the shaders from the non-texture versions.
//
technique NoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureSlantBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowSlantBevelPS();
    }
}

technique NoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundedSlantBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowRoundedSlantBevelPS();
    }
}

technique NoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowRoundBevelPS();
    }
}


//
// Bevel with texture and outline
//
// Note that for the specular pass, we can use the shaders from the non-outline/texture versions.
//
technique OutlineNoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureSlantBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowSlantBevelPS();
    }
}

technique OutlineNoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundedSlantBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineNoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundBevelPS();
    }
    pass Pass2
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 SpecularOnlyNoShadowRoundBevelPS();
    }
}
