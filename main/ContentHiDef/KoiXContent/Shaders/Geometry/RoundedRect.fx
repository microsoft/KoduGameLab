
//
// RoundedRect shaders
//
// Basically all these variants including simple, outline and shadow are just
// variations on the same algorithm for rendering a rounded rect.  We take the
// input pixel position and "collapse" the rect down to a disk.  Then we calc
// the radius and find the right colors and edges.  Each edge has an inner and
// outer radius.  In between these values we blend to produce smooth edges.
// If the radii are close (1.5 pixels) then this produces a nice antialiased edge.
// If the radii are further apart we can use this for soft shadows.
//
// Note "Alt" shaders are used for the the following cases:
// -- shadows are rendered and ShadowSize > CornerRadius.
// -- outlining is used and OutlineWidth > CornerRadius
//
// For SM2 shaders where we have both a SlantBevel and a shadow we have to do the 
// shadow as a seperate pass.
//

#include "..\..\..\..\Boku\Content\KoiXContent\Shaders\Geometry\Code\UILighting.fx"
#include "..\..\..\..\Boku\Content\KoiXContent\Shaders\Geometry\Code\RoundedRect.fx"

//
//
// Techniques
//
//

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
// No shadow, no Bevel versions.
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

technique OutlineAltNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltNoShadowPS();
    }
}

technique OuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OuterShadowPS();
    }
}

technique InnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 InnerShadowPS();
    }
}

technique OutlineOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineOuterShadowPS();
    }
}

technique OutlineAltOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltOuterShadowPS();
    }
}

technique OutlineInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineInnerShadowPS();
    }
}

technique OutlineAltInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltInnerShadowPS();
    }
}

//
// Textured versions.
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

technique OutlineAltNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltNoShadowTexturePS();
    }
}

technique OuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OuterShadowTexturePS();
    }
}

technique InnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 InnerShadowTexturePS();
    }
}

technique OutlineOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineOuterShadowTexturePS();
    }
}

technique OutlineAltOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltOuterShadowTexturePS();
    }
}

technique OutlineInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineInnerShadowTexturePS();
    }
}

technique OutlineAltInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltInnerShadowTexturePS();
    }
}


//
// Non-textured, with SlantBevel.
//

technique NoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowSlantBevelPS();
    }
}

technique OutlineNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowSlantBevelPS();
    }
}

technique OutlineAltNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltNoShadowSlantBevelPS();
    }
}


technique OuterShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OuterShadowSlantBevelPS();
    }
}

technique InnerShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 InnerShadowSlantBevelPS();
    }
}

technique OutlineOuterShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineOuterShadowSlantBevelPS();
    }
}

technique OutlineAltOuterShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltOuterShadowSlantBevelPS();
    }
}

technique OutlineInnerShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineInnerShadowSlantBevelPS();
    }
}

technique OutlineAltInnerShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltInnerShadowSlantBevelPS();
    }
}


//
// Textured with SlantBevel.
//

technique NoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowTextureSlantBevelPS();
    }
}

technique OutlineNoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineNoShadowTextureSlantBevelPS();
    }
}

technique OutlineAltNoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltNoShadowTextureSlantBevelPS();
    }
}


technique OuterShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OuterShadowTextureSlantBevelPS();
    }
}

technique InnerShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 InnerShadowTextureSlantBevelPS();
    }
}

technique OutlineOuterShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineOuterShadowTextureSlantBevelPS();
    }
}

technique OutlineAltOuterShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltOuterShadowTextureSlantBevelPS();
    }
}

technique OutlineInnerShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineInnerShadowTextureSlantBevelPS();
    }
}

technique OutlineAltInnerShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltInnerShadowTextureSlantBevelPS();
    }
}

//
// Non-textured, with RoundedSlantBevel.
//

technique NoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowRoundedSlantBevelPS();
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

technique OutlineAltNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltNoShadowRoundedSlantBevelPS();
    }
}


technique OuterShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OuterShadowRoundedSlantBevelPS();
    }
}

technique InnerShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 InnerShadowRoundedSlantBevelPS();
    }
}

technique OutlineOuterShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineOuterShadowRoundedSlantBevelPS();
    }
}

technique OutlineAltOuterShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltOuterShadowRoundedSlantBevelPS();
    }
}

technique OutlineInnerShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineInnerShadowRoundedSlantBevelPS();
    }
}

technique OutlineAltInnerShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltInnerShadowRoundedSlantBevelPS();
    }
}


//
// Textured with RoundedSlantBevel.
//

technique NoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowTextureRoundedSlantBevelPS();
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

technique OutlineAltNoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltNoShadowTextureRoundedSlantBevelPS();
    }
}


technique OuterShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OuterShadowTextureRoundedSlantBevelPS();
    }
}

technique InnerShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 InnerShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineOuterShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineOuterShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineAltOuterShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltOuterShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineInnerShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineInnerShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineAltInnerShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltInnerShadowTextureRoundedSlantBevelPS();
    }
}


//
// Non-textured, with RoundBevel.
//

technique NoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowRoundBevelPS();
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

technique OutlineAltNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltNoShadowRoundBevelPS();
    }
}


technique OuterShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OuterShadowRoundBevelPS();
    }
}

technique InnerShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 InnerShadowRoundBevelPS();
    }
}

technique OutlineOuterShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineOuterShadowRoundBevelPS();
    }
}

technique OutlineAltOuterShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltOuterShadowRoundBevelPS();
    }
}

technique OutlineInnerShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineInnerShadowRoundBevelPS();
    }
}

technique OutlineAltInnerShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltInnerShadowRoundBevelPS();
    }
}


//
// Textured with RoundBevel.
//

technique NoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 NoShadowTextureRoundBevelPS();
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

technique OutlineAltNoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltNoShadowTextureRoundBevelPS();
    }
}


technique OuterShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OuterShadowTextureRoundBevelPS();
    }
}

technique InnerShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 InnerShadowTextureRoundBevelPS();
    }
}

technique OutlineOuterShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineOuterShadowTextureRoundBevelPS();
    }
}

technique OutlineAltOuterShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltOuterShadowTextureRoundBevelPS();
    }
}

technique OutlineInnerShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineInnerShadowTextureRoundBevelPS();
    }
}

technique OutlineAltInnerShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineAltInnerShadowTextureRoundBevelPS();
    }
}


//
//
//	Same as all of the above but with two-tone
//
//

//
// No shadow, no Bevel versions.
//

technique TwoToneNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneNoShadowPS();
    }
}

technique OutlineTwoToneNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneNoShadowPS();
    }
}

technique OutlineTwoToneAltNoShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltNoShadowPS();
    }
}

technique TwoToneOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneOuterShadowPS();
    }
}

technique TwoToneInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneInnerShadowPS();
    }
}

technique OutlineTwoToneOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneOuterShadowPS();
    }
}

technique OutlineTwoToneAltOuterShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltOuterShadowPS();
    }
}

technique OutlineTwoToneInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneInnerShadowPS();
    }
}

technique OutlineTwoToneAltInnerShadow
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltInnerShadowPS();
    }
}

//
// Textured versions.
//

technique TwoToneNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneNoShadowTexturePS();
    }
}

technique OutlineTwoToneNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneNoShadowTexturePS();
    }
}

technique OutlineTwoToneAltNoShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltNoShadowTexturePS();
    }
}

technique TwoToneOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneOuterShadowTexturePS();
    }
}

technique TwoToneInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneInnerShadowTexturePS();
    }
}

technique OutlineTwoToneOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneOuterShadowTexturePS();
    }
}

technique OutlineTwoToneAltOuterShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltOuterShadowTexturePS();
    }
}

technique OutlineTwoToneInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneInnerShadowTexturePS();
    }
}

technique OutlineTwoToneAltInnerShadowTexture
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltInnerShadowTexturePS();
    }
}


//
// Non-textured, with SlantBevel.
//

technique TwoToneNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneNoShadowSlantBevelPS();
    }
}

technique OutlineTwoToneNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneNoShadowSlantBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltNoShadowSlantBevelPS();
    }
}


technique TwoToneOuterShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneOuterShadowSlantBevelPS();
    }
}

technique TwoToneInnerShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneInnerShadowSlantBevelPS();
    }
}

technique OutlineTwoToneOuterShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneOuterShadowSlantBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltOuterShadowSlantBevelPS();
    }
}

technique OutlineTwoToneInnerShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneInnerShadowSlantBevelPS();
    }
}

technique OutlineTwoToneAltInnerShadowSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltInnerShadowSlantBevelPS();
    }
}


//
// Textured with SlantBevel.
//

technique TwoToneNoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneNoShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneNoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneNoShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltNoShadowTextureSlantBevelPS();
    }
}


technique TwoToneOuterShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneOuterShadowTextureSlantBevelPS();
    }
}

technique TwoToneInnerShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneInnerShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneOuterShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneOuterShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltOuterShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneInnerShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneInnerShadowTextureSlantBevelPS();
    }
}

technique OutlineTwoToneAltInnerShadowTextureSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltInnerShadowTextureSlantBevelPS();
    }
}

//
// Non-textured, with RoundedSlantBevel.
//

technique TwoToneNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneNoShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltNoShadowRoundedSlantBevelPS();
    }
}


technique TwoToneOuterShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneOuterShadowRoundedSlantBevelPS();
    }
}

technique TwoToneInnerShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneInnerShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneOuterShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneOuterShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltOuterShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneInnerShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneInnerShadowRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltInnerShadowRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltInnerShadowRoundedSlantBevelPS();
    }
}


//
// Textured with RoundedSlantBevel.
//

technique TwoToneNoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneNoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneNoShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltNoShadowTextureRoundedSlantBevelPS();
    }
}


technique TwoToneOuterShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneOuterShadowTextureRoundedSlantBevelPS();
    }
}

technique TwoToneInnerShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneInnerShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneOuterShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneOuterShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltOuterShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneInnerShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneInnerShadowTextureRoundedSlantBevelPS();
    }
}

technique OutlineTwoToneAltInnerShadowTextureRoundedSlantBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltInnerShadowTextureRoundedSlantBevelPS();
    }
}


//
// Non-textured, with RoundBevel.
//

technique TwoToneNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneNoShadowRoundBevelPS();
    }
}

technique OutlineTwoToneNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneNoShadowRoundBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltNoShadowRoundBevelPS();
    }
}


technique TwoToneOuterShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneOuterShadowRoundBevelPS();
    }
}

technique TwoToneInnerShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneInnerShadowRoundBevelPS();
    }
}

technique OutlineTwoToneOuterShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneOuterShadowRoundBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltOuterShadowRoundBevelPS();
    }
}

technique OutlineTwoToneInnerShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneInnerShadowRoundBevelPS();
    }
}

technique OutlineTwoToneAltInnerShadowRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltInnerShadowRoundBevelPS();
    }
}


//
// Textured with RoundBevel.
//

technique TwoToneNoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneNoShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneNoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneNoShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneAltNoShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltNoShadowTextureRoundBevelPS();
    }
}


technique TwoToneOuterShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneOuterShadowTextureRoundBevelPS();
    }
}

technique TwoToneInnerShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 TwoToneInnerShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneOuterShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneOuterShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneAltOuterShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltOuterShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneInnerShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneInnerShadowTextureRoundBevelPS();
    }
}

technique OutlineTwoToneAltInnerShadowTextureRoundBevel
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 OutlineTwoToneAltInnerShadowTextureRoundBevelPS();
    }
}
