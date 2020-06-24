
//
// Disc shaders
//

//
//
// Techniques
//
//

#include "Code\UILighting.fx"
#include "Code\Disc.fx"

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

technique InnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowPS();
    }
}

technique OuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowPS();
    }
}

technique OutlineInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowPS();
    }
}

technique OutlineOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowPS();
    }
}

//
// Non-textured with bevel
// 
technique NoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowSlantBevelPS();
    }
}
technique NoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundedSlantBevelPS();
    }
}
technique NoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowRoundBevelPS();
    }
}

technique OutlineNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowSlantBevelPS();
    }
}
technique OutlineNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundedSlantBevelPS();
    }
}
technique OutlineNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowRoundBevelPS();
    }
}

technique InnerShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowSlantBevelPS();
    }
}
technique InnerShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowRoundedSlantBevelPS();
    }
}
technique InnerShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowRoundBevelPS();
    }
}

technique OuterShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowSlantBevelPS();
    }
}
technique OuterShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowRoundedSlantBevelPS();
    }
}
technique OuterShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowRoundBevelPS();
    }
}

technique OutlineInnerShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowSlantBevelPS();
    }
}
technique OutlineInnerShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowRoundedSlantBevelPS();
    }
}
technique OutlineInnerShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowRoundBevelPS();
    }
}

technique OutlineOuterShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowSlantBevelPS();
    }
}
technique OutlineOuterShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowRoundedSlantBevelPS();
    }
}
technique OutlineOuterShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowRoundBevelPS();
    }
}


//
// Textured
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

technique InnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowTexturePS();
    }
}

technique OuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowTexturePS();
    }
}

technique OutlineInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowTexturePS();
    }
}

technique OutlineOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowTexturePS();
    }
}

//
// Textured with bevel
//
technique NoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureSlantBevelPS();
    }
}
technique NoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundedSlantBevelPS();
    }
}
technique NoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 NoShadowTextureRoundBevelPS();
    }
}

technique OutlineNoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureSlantBevelPS();
    }
}
technique OutlineNoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundedSlantBevelPS();
    }
}
technique OutlineNoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineNoShadowTextureRoundBevelPS();
    }
}

technique InnerShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowTextureSlantBevelPS();
    }
}
technique InnerShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowTextureRoundedSlantBevelPS();
    }
}
technique InnerShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 InnerShadowTextureRoundBevelPS();
    }
}

technique OuterShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowTextureSlantBevelPS();
    }
}
technique OuterShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowTextureRoundedSlantBevelPS();
    }
}
technique OuterShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OuterShadowTextureRoundBevelPS();
    }
}

technique OutlineInnerShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowTextureSlantBevelPS();
    }
}
technique OutlineInnerShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowTextureRoundedSlantBevelPS();
    }
}
technique OutlineInnerShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineInnerShadowTextureRoundBevelPS();
    }
}

technique OutlineOuterShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowTextureSlantBevelPS();
    }
}
technique OutlineOuterShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowTextureRoundedSlantBevelPS();
    }
}
technique OutlineOuterShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 OutlineOuterShadowTextureRoundBevelPS();
    }
}
