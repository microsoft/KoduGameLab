// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KoiX.Geometry
{
    using Padding = KoiX.UI.Padding;

    /// <summary>
    /// Static class for rendering discs with optional shadows, bevels, textures, and outlines.
    /// </summary>
    public static class Disc
    {
        static VertexElement[] elements = new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),              // Vertx postion in pixels.
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),     // Texture coords.
            // Total = 16 bytes
        };

        struct Vertex : IVertexType
        {
            public Vector2 pos;     // Expanded to a Vector4 in the vertex shader.
            public Vector2 uv;      // Only used for texturing.

            // c'tor
            public Vertex(Vector2 pos, Vector2 uv)
            {
                this.pos = pos;
                this.uv = uv;
            }   // end of Vertex c'tor

            public Vertex(Vertex v)
            {
                this.pos = v.pos;
                this.uv = v.uv;
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

        /// <summary>
        /// Make me one with everything...
        /// Single static draw call for all disc variations.
        /// All optional params have default values so can be ignored although in order to do this you
        /// must used named arguments when calling this function except for the first 4 arguments
        /// which are always required.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="center">Center of disc.</param>
        /// <param name="radius">Radius of disc.</param>
        /// <param name="bodyColor">Color for main body of disc.</param>
        /// <param name="outlineWidth">Width in pixels of outline.  If 0, no outline is rendered.</param>
        /// <param name="outlineColor">Color for outline.</param>
        /// <param name="shadowStyle">Type of shadow.</param>
        /// <param name="shadowOffset">Offset from shape.  The "direction" the shadow is cast.</param>
        /// <param name="shadowSize">How soft the shadow edges are in pixels.</param>
        /// <param name="shadowAttenuation">Multiplier applied to shadow.  default = 1.  1 = dark shadow, .3 = dim shadow, 0 = no shadow</param>
        /// <param name="texture">Texture applied to disc.  Any transparent areas show up in the body/outline color.  This texture should be in pre-multiplied alpha format.</param>
        /// <param name="textureRotation">Texture rotation.</param>
        /// <param name="texturePadding">Padding to adjust texture size and location on disc.</param>
        /// <param name="edgeBlend">Blending radius in pixels at edge of shape and at outline boundary.  default 0.5</param>
        public static void Render(SpriteCamera camera, Vector2 center, float radius, Color bodyColor, 
                                    float outlineWidth = 0, Color outlineColor = default(Color), 
                                    BevelStyle bevelStyle = BevelStyle.None, float bevelWidth = 0,
                                    ShadowStyle shadowStyle = ShadowStyle.None, Vector2 shadowOffset = default(Vector2), float shadowSize = 0, float shadowAttenuation = 1, 
                                    Texture2D texture = null, float textureRotation = 0, Padding texturePadding = default(Padding), 
                                    float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            if (shadowStyle == ShadowStyle.Outer)
            {
                SetVertices(camera, center, radius, edgeBlend, textureRotation, texturePadding: texturePadding, shadowOffset: shadowOffset, shadowSize: shadowSize);
            }
            else
            {
                SetVertices(camera, center, radius, edgeBlend, textureRotation, texturePadding: texturePadding);
            }

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            // Build up the technique depending on the active options.
            string technique = "";

            if (outlineWidth > 0)
            {
                technique += "Outline";
                effect.Parameters["OutlineColor"].SetValue(outlineColor.ToVector4());
                effect.Parameters["OutlineWidth"].SetValue(outlineWidth);
            }

            switch (shadowStyle)
            {
                case ShadowStyle.Inner:
                    technique += "InnerShadow";
                    effect.Parameters["ShadowOffset"].SetValue(shadowOffset);
                    effect.Parameters["ShadowSize"].SetValue(shadowSize);
                    effect.Parameters["ShadowAttenuation"].SetValue(shadowAttenuation);
                    break;
                case ShadowStyle.Outer:
                    technique += "OuterShadow";
                    effect.Parameters["ShadowOffset"].SetValue(shadowOffset);
                    effect.Parameters["ShadowSize"].SetValue(shadowSize);
                    effect.Parameters["ShadowAttenuation"].SetValue(shadowAttenuation);
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

            // Note no world matrix.
            Matrix worldViewProjMatrix = camera.ViewProjMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);

            // Set common params.
            effect.Parameters["Center"].SetValue(center);
            effect.Parameters["Radius"].SetValue(radius);
            effect.Parameters["BodyColor"].SetValue(bodyColor.ToVector4());
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

        /// <summary>
        /// Just renders the shadow.  Assumes outer shadow.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="shadowOffset"></param>
        /// <param name="shadowSize"></param>
        /// <param name="shadowAttenuation"></param>
        /// <param name="edgeBlend"></param>
        public static void RenderShadow(SpriteCamera camera, Vector2 center, float radius,
                                        ShadowStyle shadowStyle = ShadowStyle.None, Vector2 shadowOffset = default(Vector2), float shadowSize = 0, float shadowAttenuation = 1,
                                        float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            Debug.Assert(shadowStyle != ShadowStyle.None);

            if (shadowStyle == ShadowStyle.None)
            {
                return;
            }

            SetVertices(camera, center, radius, edgeBlend, shadowOffset: shadowOffset, shadowSize: shadowSize);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            string technique = "";
            switch (shadowStyle)
            {
                case ShadowStyle.Inner:
                    technique += "InnerShadowOnly";
                    effect.Parameters["ShadowOffset"].SetValue(shadowOffset);
                    effect.Parameters["ShadowSize"].SetValue(shadowSize);
                    effect.Parameters["ShadowAttenuation"].SetValue(shadowAttenuation);
                    break;
                case ShadowStyle.Outer:
                    technique += "OuterShadowOnly";
                    effect.Parameters["ShadowOffset"].SetValue(shadowOffset);
                    effect.Parameters["ShadowSize"].SetValue(shadowSize);
                    effect.Parameters["ShadowAttenuation"].SetValue(shadowAttenuation);
                    break;
                default:
                    break;
            }

            effect.CurrentTechnique = effect.Techniques[technique];

            // Note no world matrix.
            Matrix worldViewProjMatrix = camera.ViewProjMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);

            effect.Parameters["Center"].SetValue(center);
            effect.Parameters["Radius"].SetValue(radius);
            effect.Parameters["EdgeBlend"].SetValue(edgeBlend / camera.Zoom);

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

        static void SetVertices(SpriteCamera camera, Vector2 center, float radius, float edgeBlend, float textureRotation = 0, Padding texturePadding = default(Padding), Vector2 shadowOffset = default(Vector2), float shadowSize = 0)
        {
            float diameter = 2.0f * radius;
            // Increase overall radius to handle outer shadow.  This forces the quad we 
            // are drawing to be big enough to also include the shadow.
            // Note that this must affect the UV coords.
            float shadowFrac = 1;
            if (shadowOffset != default(Vector2) || shadowSize != 0)
            {
                shadowOffset = MyMath.Abs(shadowOffset);
                float shadowIncrease = Math.Max(shadowOffset.X, shadowOffset.Y);
                shadowIncrease += shadowSize;
                shadowFrac = (radius + 0.5f * shadowIncrease) / radius;
                radius += shadowIncrease;
            }

            edgeBlend /= camera.Zoom;
            radius += edgeBlend;
            Matrix mat = Matrix.CreateRotationZ(textureRotation);
            localVerts[0].pos = center + Vector2.TransformNormal(new Vector2(-radius, -radius), mat);
            localVerts[1].pos = center + Vector2.TransformNormal(new Vector2( radius, -radius), mat);
            localVerts[2].pos = center + Vector2.TransformNormal(new Vector2( radius,  radius), mat);
            localVerts[3].pos = center + Vector2.TransformNormal(new Vector2(-radius,  radius), mat);

            // Set texture to stretch across full shape.
            localVerts[0].uv = new Vector2(1 - shadowFrac, 1 - shadowFrac);
            localVerts[1].uv = new Vector2(shadowFrac, 1 - shadowFrac);
            localVerts[2].uv = new Vector2(shadowFrac, shadowFrac);
            localVerts[3].uv = new Vector2(1 - shadowFrac, shadowFrac);

            // If we have texturePadding, need to adjust the UVs for this.
            if (texturePadding != Padding.Empty)
            {
                // Note that left and top are negative.
                float left = -texturePadding.Left / diameter;
                float right = texturePadding.Right / diameter;
                float top = -texturePadding.Top / diameter;
                float bottom = texturePadding.Bottom / diameter;

                localVerts[0].uv += new Vector2(left, top);
                localVerts[1].uv += new Vector2(right, top);
                localVerts[2].uv += new Vector2(right, bottom);
                localVerts[3].uv += new Vector2(left, bottom);
            }

        }   // end of SetVertices()

        public static void LoadContent()
        {
            if (DeviceResetX.NeedsLoad(effect))
            {
                effect = KoiLibrary.LoadEffect(@"KoiXContent\Shaders\Geometry\Disc");
            }
        }

        public static void UnloadContent()
        {
            DeviceResetX.Release(ref effect);
        }

        public static void DeviceResetHandler(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

    }   // end of class Disc

}   // end of namespace KoiX.Geometry


