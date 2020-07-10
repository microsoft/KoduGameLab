// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.UI;

using Boku;
using Boku.Common;

namespace KoiX.Geometry
{
    /// <summary>
    /// Static class for rendering rounded rectangles with optional shadows, bevels, textures, and outlines.
    /// </summary>
    public static class RoundedRect
    {
        static VertexElement[] elements = new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),              // Position in pixels in world coords.
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),     // Texture coords.
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),    // 0,0 pixel based coords.
            // Total = 24 bytes
        };

        struct Vertex : IVertexType
        {
            public Vector2 pos;     // Expanded to a Vector4 in the vertex shader.
            public Vector2 uv;
            public Vector2 coords;  // Pixel coords with 0,0 in upper left corner of rect.

            // c'tor
            public Vertex(Vector2 pos, Vector2 uv, Vector2 coords)
            {
                this.pos = pos;
                this.uv = uv;
                this.coords = coords;
            }   // end of Vertex c'tor

            public Vertex(Vertex v)
            {
                this.pos = v.pos;
                this.uv = v.uv;
                this.coords = v.coords;
            }

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        DeviceResetX.Release(ref decl);
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }
        }   // end of Vertex

        #region Members

        static Effect effect;

        static VertexDeclaration decl;
        static short[] quadIndices = { 0, 1, 2, 0, 2, 3 };
        static Vertex[] localVerts = new Vertex[4];

        #endregion

        #region Accessors
        #endregion

        #region Public

        public static void Render(SpriteCamera camera, RectangleF rect, float radius, Color color,
                                    float outlineWidth = 0, Color outlineColor = default(Color),
                                    Color twoToneSecondColor = default(Color), float twoToneSplitPosition = float.MinValue, bool twoToneHorizontalSplit = true,
                                    BevelStyle bevelStyle = BevelStyle.None, float bevelWidth = 0,
                                    ShadowStyle shadowStyle = ShadowStyle.None, Vector2 shadowOffset = default(Vector2), float shadowSize = 0, float shadowAttenuation = 1,
                                    Texture2D texture = null, Padding texturePadding = default(Padding),
                                    float edgeBlend = Geometry.DefaultEdgeBlend,
                                    Matrix worldMatrix = default(Matrix))
        {
            Render(camera, rect.Position, rect.Size, radius, color,
                    outlineWidth, outlineColor,
                    twoToneSecondColor, twoToneSplitPosition, twoToneHorizontalSplit,
                    bevelStyle, bevelWidth,
                    shadowStyle, shadowOffset, shadowSize, shadowAttenuation,
                    texture, texturePadding,
                    edgeBlend,
                    worldMatrix);
        }

        public static void Render(SpriteCamera camera, Vector2 position, Vector2 size, float radius, Color color, 
                                    float outlineWidth = 0, Color outlineColor = default(Color),
                                    Color twoToneSecondColor = default(Color), float twoToneSplitPosition = float.MinValue, bool twoToneHorizontalSplit = true,
                                    BevelStyle bevelStyle = BevelStyle.None, float bevelWidth = 0,
                                    ShadowStyle shadowStyle = ShadowStyle.None, Vector2 shadowOffset = default(Vector2), float shadowSize = 0, float shadowAttenuation = 1, 
                                    Texture2D texture = null, Padding texturePadding = default(Padding),
                                    float edgeBlend = Geometry.DefaultEdgeBlend,
                                    Matrix worldMatrix = default(Matrix))
        {

            // TODO (****) for bevel plus shadow breaks SM2 so if we're
            // in Reach mode and want a bevelled shape with a shadow we
            // need to render the shadow by itself and then render the
            // shape as a second call.  This be done using 2 passes
            // in the technique itself.

            // Ensure the effect is valid and not Disposed.  No, I'm 
            // not sure why it's getting disposed to begin with.
            LoadContent();

            // Create vertices.
            if (shadowStyle != ShadowStyle.Outer)
            {
                // For Inner or None we don't need to increase the quad size for the shadow.
                SetVertices(localVerts, camera, position, size, texture: texture, texturePadding: texturePadding);
            }
            else
            {
                SetVertices(localVerts, camera, position, size, shadowOffset: shadowOffset, shadowSize: shadowSize, texture: texture, texturePadding: texturePadding);
            }

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            // Build up the technique depending on the active options.
            string technique = "";

            if (outlineWidth > 0)
            {
                technique += "Outline";
                if (outlineWidth >= radius)
                {
                    technique += "Alt";
                }
                effect.Parameters["OutlineColor"].SetValue(outlineColor.ToVector4());
                effect.Parameters["OutlineWidth"].SetValue(outlineWidth);
            }

            if (twoToneSplitPosition != float.MinValue)
            {
                technique += "TwoTone";
                effect.Parameters["TwoToneHorizontalSplit"].SetValue(twoToneHorizontalSplit);
                effect.Parameters["TwoToneSecondColor"].SetValue(twoToneSecondColor.ToVector4());
                effect.Parameters["TwoToneSplitPosition"].SetValue(twoToneSplitPosition);
            }

            switch (shadowStyle)
            {
                case ShadowStyle.Inner:
                    technique += "InnerShadow";
                    effect.Parameters["ShadowOffset"].SetValue(shadowOffset);
                    effect.Parameters["ShadowSize"].SetValue(shadowSize);
                    effect.Parameters["ShadowAttenuation"].SetValue(shadowAttenuation);
                    effect.Parameters["ShadowRadius"].SetValue(Math.Max(radius, shadowSize));
                    break;
                case ShadowStyle.Outer:
                    technique += "OuterShadow";
                    effect.Parameters["ShadowOffset"].SetValue(shadowOffset);
                    effect.Parameters["ShadowSize"].SetValue(shadowSize);
                    effect.Parameters["ShadowAttenuation"].SetValue(shadowAttenuation);
                    effect.Parameters["ShadowRadius"].SetValue(Math.Max(radius, shadowSize));
                    break;
                default:
                    technique += "NoShadow";
                    break;
            }

            if (texture != null)
            {
                technique += "Texture";
                effect.Parameters["DiffuseTexture"].SetValue(texture);
            }

            if (bevelWidth <= 0)
            {
                bevelStyle = BevelStyle.None;
                effect.Parameters["BevelWidth"].SetValue(Vector3.Zero);
            }
            else
            {
                effect.Parameters["BevelWidth"].SetValue(new Vector3(bevelWidth, bevelWidth - edgeBlend, bevelWidth + edgeBlend));
            }
            switch (bevelStyle)
            {
                case BevelStyle.None:
                    break;
                case BevelStyle.Slant:
                    technique += "SlantBevel";
                    break;
                case BevelStyle.RoundedSlant:
                    technique += "RoundedSlantBevel";
                    break;
                case BevelStyle.Round:
                    technique += "RoundBevel";
                    break;
            }

            effect.CurrentTechnique = effect.Techniques[technique];

            Matrix worldViewProjMatrix = camera.ViewProjMatrix;
            if(worldMatrix != default(Matrix))
            {
                worldViewProjMatrix = worldMatrix * camera.ViewProjMatrix;
            }
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);

            // Set common params.
            effect.Parameters["Size"].SetValue(size);
            effect.Parameters["CornerRadius"].SetValue(radius);
            effect.Parameters["BodyColor"].SetValue(color.ToVector4());
            effect.Parameters["EdgeBlend"].SetValue(edgeBlend / camera.Zoom);

            // Set renderstate we care about.  Pre-mult alpha blending and no Z.
            device.DepthStencilState = DepthStencilState.None;
            device.BlendState = BlendState.AlphaBlend;

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserIndexedPrimitives<Vertex>(PrimitiveType.TriangleList, localVerts, 0, 4, quadIndices, 0, 2);
            }
        }   // end of Render()

        public static void RenderShadow(SpriteCamera camera, RectangleF rect, float radius,
                            ShadowStyle shadowStyle = ShadowStyle.None, Vector2 shadowOffset = default(Vector2), float shadowSize = 0, float shadowAttenuation = 1,
                            float edgeBlend = Geometry.DefaultEdgeBlend,
                            Matrix worldMatrix = default(Matrix))
        {
            RenderShadow(camera, rect.Position, rect.Size, radius,
                                    shadowStyle, shadowOffset, shadowSize, shadowAttenuation,
                                    edgeBlend,
                                    worldMatrix);
        }

        public static void RenderShadow(SpriteCamera camera, Vector2 position, Vector2 size, float radius,
                                    ShadowStyle shadowStyle = ShadowStyle.None, Vector2 shadowOffset = default(Vector2), float shadowSize = 0, float shadowAttenuation = 1,
                                    float edgeBlend = Geometry.DefaultEdgeBlend,
                                    Matrix worldMatrix = default(Matrix))
        {
            Debug.Assert(shadowStyle != ShadowStyle.None);

            if (shadowStyle == ShadowStyle.None)
            {
                return;
            }

            // Create vertices.
            SetVertices(localVerts, camera, position, size, shadowOffset: shadowOffset, shadowSize: shadowSize);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            string technique = null;

            switch (shadowStyle)
            {
                case ShadowStyle.Inner:
                    technique = "InnerShadowOnly";
                    effect.Parameters["ShadowOffset"].SetValue(shadowOffset);
                    effect.Parameters["ShadowSize"].SetValue(shadowSize);
                    effect.Parameters["ShadowAttenuation"].SetValue(shadowAttenuation);
                    effect.Parameters["ShadowRadius"].SetValue(Math.Max(radius, shadowSize));
                    break;
                case ShadowStyle.Outer:
                    technique = "OuterShadowOnly";
                    effect.Parameters["ShadowOffset"].SetValue(shadowOffset);
                    effect.Parameters["ShadowSize"].SetValue(shadowSize);
                    effect.Parameters["ShadowAttenuation"].SetValue(shadowAttenuation);
                    effect.Parameters["ShadowRadius"].SetValue(Math.Max(radius, shadowSize));
                    break;
                default:
                    break;
            }

            effect.Parameters["ShadowOffset"].SetValue(shadowOffset);
            effect.Parameters["ShadowSize"].SetValue(shadowSize);
            effect.Parameters["ShadowAttenuation"].SetValue(shadowAttenuation);
            effect.Parameters["ShadowRadius"].SetValue(Math.Max(shadowSize, radius));

            effect.CurrentTechnique = effect.Techniques[technique];

            Matrix worldViewProjMatrix = camera.ViewProjMatrix;
            if (worldMatrix != default(Matrix))
            {
                worldViewProjMatrix = worldMatrix * camera.ViewProjMatrix;
            }
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);

            // Set common params.
            effect.Parameters["Size"].SetValue(size);
            effect.Parameters["CornerRadius"].SetValue(radius);
            effect.Parameters["EdgeBlend"].SetValue(edgeBlend / camera.Zoom);

            // Set renderstate we care about.  Pre-mult alpha blending and no Z.
            device.DepthStencilState = DepthStencilState.None;
            device.BlendState = BlendState.AlphaBlend;

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserIndexedPrimitives<Vertex>(PrimitiveType.TriangleList, localVerts, 0, 4, quadIndices, 0, 2);
            }
        }   // end of RenderShadow()

        #endregion

        #region Internal

        /// <summary>
        /// Converts position in pixel coordinates into homogeneous coords.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size">Size of the quad in pixels that will be rendered.  May be bigger than actually rounded-rect due to outer shadow.</param>
        //

        /// <summary>
        /// Adjusts for antialiased edges.
        /// Calculates texture UV coords.
        /// Converts position in pixel coordinates into homogeneous coords.
        /// </summary>
        /// <param name="localVerts"></param>
        /// <param name="camera"></param>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <param name="shadowOffset"></param>
        /// <param name="shadowSize"></param>
        /// <param name="texture"></param>
        /// <param name="texturePadding"></param>
        static void SetVertices(Vertex[] localVerts, SpriteCamera camera, Vector2 position, Vector2 size, Vector2 shadowOffset = default(Vector2), float shadowSize = 0, Texture2D texture = null, Padding texturePadding = default(Padding))
        {
            // Calc extra cushion needed since outer shadow will exceed rect bounds.
            float left = 0;
            float right = 0;
            float top = 0;
            float bottom = 0;

            left = MathHelper.Max(shadowSize - shadowOffset.X, 0);
            right = MathHelper.Max(shadowSize + shadowOffset.X, 0);
            top = MathHelper.Max(shadowSize - shadowOffset.Y, 0);
            bottom = MathHelper.Max(shadowSize + shadowOffset.Y, 0);

            // This extra single pixel all around helps antialiasing when rotated.
            // So, we end up doing it always just to be more consistant.
            ++left;
            ++right;
            ++top;
            ++bottom;
            
            // Adjust position and size to cover both rect and extra space needed by shadow.
            // Magic half pixel offset.  Not sure why this is in X only.
            // This is needed to get outlines to align pixel-perfect.
            Vector2 paddedPosition = position - new Vector2(left, top) + new Vector2(0.0f, 0.0f) / camera.Zoom;
            Vector2 paddedSize = size + new Vector2(left + right, top + bottom);

            // Set pixel coords for vertices.  0,0 is the upper left hand corner of the shape.
            {
                localVerts[0].coords = new Vector2(-left, -top);
                localVerts[1].coords = new Vector2(paddedSize.X - left, -top);
                localVerts[2].coords = new Vector2(paddedSize.X - left, paddedSize.Y - top);
                localVerts[3].coords = new Vector2(-left, paddedSize.Y - top);
            }

            // Set the vertex positions.  This is in camera coordinates.
            {
                localVerts[0].pos = new Vector2(paddedPosition.X, paddedPosition.Y);
                localVerts[1].pos = new Vector2(paddedPosition.X + paddedSize.X, paddedPosition.Y);
                localVerts[2].pos = new Vector2(paddedPosition.X + paddedSize.X, paddedPosition.Y + paddedSize.Y);
                localVerts[3].pos = new Vector2(paddedPosition.X, paddedPosition.Y + paddedSize.Y);
            }

            // Set UV coords.
            {
                // Calc size, in pixels, that texture will cover.  This is the size that the 
                // texture would be if fully displayed.  If the RoundedRect is smaller then
                // we will only se a part of the texture.
                // For perfect 1:1 display of texels, this should match the size of the texture.
                // In UV space this is 1, 1.  So, this gives us the ratio of pixels to UV coords.
                Vector2 textureDisplaySize = size - new Vector2(texturePadding.Horizontal, texturePadding.Vertical);

                // If you're here looking for a fix to textures which don't stay pixel perfectly aligned
                // you should make sure that you camera position has a half-pixel offset in either 
                // dimension if the size in that dimension is even.  Look at RoundedRect.cs for example.
                /*
                // We need a half pixel offset depending on the screen resolution.
                float dx = (((int)camera.ScreenSize.X) & 0x01) == 1 ? 0.0f: 0.5f;
                float dy = (((int)camera.ScreenSize.Y) & 0x01) == 1 ? 0.0f: 0.5f;
                */

                float leftCoord = -(texturePadding.Left + left) / textureDisplaySize.X;
                float rightCoord = 1.0f + (texturePadding.Right + right) / textureDisplaySize.X;
                float topCoord = -(texturePadding.Top + top) / textureDisplaySize.Y;
                float bottomCoord = 1.0f + (texturePadding.Bottom + bottom) / textureDisplaySize.Y; 

                localVerts[0].uv = new Vector2(leftCoord, topCoord);
                localVerts[1].uv = new Vector2(rightCoord, topCoord);
                localVerts[2].uv = new Vector2(rightCoord, bottomCoord);
                localVerts[3].uv = new Vector2(leftCoord, bottomCoord);
            }

        }   // end of SetVertices()

        #endregion


        public static void LoadContent()
        {
            if (DeviceResetX.NeedsLoad(effect))
            {
                effect = KoiLibrary.LoadEffect(@"KoiXContent\Shaders\Geometry\RoundedRect");
            }
        }

        public static void UnloadContent()
        {
            throw new NotImplementedException();
        }

        public static void DeviceResetHandler(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }   // end of class RoundedRect

}   // end of namespace KoiX.Geometry

