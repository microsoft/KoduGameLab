// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


//
// Pie Slice shaders, HiDef
//

#define HiDef 1

#include "..\..\..\..\Boku\Content\KoiXContent\Shaders\Geometry\Code\UILighting.fx"
#include "..\..\..\..\Boku\Content\KoiXContent\Shaders\Geometry\Code\PieSlice.fx"

technique OuterShadowOnly
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OuterShadowOnlyPS();
    }
}

technique InnerShadowOnly
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 InnerShadowOnlyPS();
    }
}

//
// Non-textured
// 

technique NoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowPS();
    }
}

technique OutlineNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowPS();
    }
}

//
// Bevel, no texture, no outline
//
technique NoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowSlantBevelPS();
    }
}

technique NoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowRoundedSlantBevelPS();
    }
}

technique NoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowRoundBevelPS();
    }
}

//
// Textured, no bevel
//
technique NoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowTexturePS();
    }
}

technique OutlineNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowTexturePS();
    }
}

//
// Bevel with outline, no texture
//
technique OutlineNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowSlantBevelPS();
    }
}

technique OutlineNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowRoundBevelPS();
    }
}

//
// Bevel with texture, no outline
//
technique NoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowTextureSlantBevelPS();
    }
}

technique NoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowTextureRoundedSlantBevelPS();
    }
}

technique NoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowTextureRoundBevelPS();
    }
}

//
// Bevel with texture and outline
//
technique OutlineNoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowTextureSlantBevelPS();
    }
}

technique OutlineNoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineNoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowTextureRoundBevelPS();
    }
}
