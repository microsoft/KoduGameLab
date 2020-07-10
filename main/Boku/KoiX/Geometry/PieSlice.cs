// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX.UI;

namespace KoiX.Geometry
{
    /// <summary>
    /// Static class for rendering pie menu slices with optional shadows, bevels, textures, and outlines.
    /// This only renders a single slice at a time.  No batching.
    /// All shadows should be rendered before all slices so they layer correctly.
    /// </summary>
    public class PieSlice
    {
        static VertexElement[] elements = new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),              // Vertx postion in pixels.
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),     // Texture coords.
            // Total = 16 bytes
        };

        struct Vertex : IVertexType
        {
            public Vector2 pos;             // Expanded to a Vector4 in the vertex shader.
            public Vector2 uv;              // Only used for texturing.

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

        // Variables for whole menu.
        public Vector2 center;          // Center of whole menu.
        public float innerRadius;       
        public float outerRadius;

        // Variables for this slice.
        public Vector2 edge0;           // Edge normal for one side of the slice.
        public Vector2 edge1;           // Other edge normal.
        public Vector2 edgeIntersect;   // Point where edges intersect.

        #endregion

        #region Accessors
        #endregion

        #region Public

        public static void Render(SpriteCamera camera, 
                                    RectangleF rect,
                                    Vector2 center, float innerRadius, float outerRadius, 
                                    Vector2 edgeNormal0, Vector2 edgeNormal1, Vector2 edgeIntersect,
                                    Color bodyColor, 
                                    float outlineWidth = 0, Color outlineColor = default(Color),
                                    BevelStyle bevelStyle = BevelStyle.None, float bevelWidth = 0,
                                    //ShadowStyle shadowStyle = ShadowStyle.None, Vector2 shadowOffset = default(Vector2), float shadowSize = 0, float shadowAttenuation = 1,
                                    Texture2D texture = null, Padding texturePadding = default(Padding),
                                    float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            // Create vertices.
            SetVertices(camera, rect, texturePadding);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            // Build up the technique depending on the active options.
            string technique = "";

            if (outlineWidth > 0)
            {
                technique += "Outline";
                effect.Parameters["OutlineColor"].SetValue(outlineColor.ToVector4());
                effect.Parameters["OutlineWidth"].SetValue(new Vector3(outlineWidth, outlineWidth - edgeBlend, outlineWidth + edgeBlend));
            }

            technique += "NoShadow";

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
            effect.Parameters["InnerRadius"].SetValue(new Vector3(innerRadius, innerRadius - edgeBlend, innerRadius + edgeBlend));
            effect.Parameters["OuterRadius"].SetValue(new Vector3(outerRadius, outerRadius - edgeBlend, outerRadius + edgeBlend));
            effect.Parameters["EdgeNormal0"].SetValue(edgeNormal0);
            effect.Parameters["EdgeNormal1"].SetValue(edgeNormal1);
            effect.Parameters["EdgeIntersect"].SetValue(edgeIntersect);
            
            effect.Parameters["BodyColor"].SetValue(bodyColor.ToVector4());
            effect.Parameters["EdgeBlend"].SetValue(new Vector2(edgeBlend / camera.Zoom, 2.0f * edgeBlend / camera.Zoom));

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

        public static void RenderShadow(SpriteCamera camera,
                                    RectangleF rect,
                                    Vector2 center, float innerRadius, float outerRadius,
                                    Vector2 edgeNormal0, Vector2 edgeNormal1, Vector2 edgeIntersect,
                                    ShadowStyle shadowStyle = ShadowStyle.None, Vector2 shadowOffset = default(Vector2), float shadowSize = 0, float shadowAttenuation = 1,
                                    float edgeBlend = Geometry.DefaultEdgeBlend)
        {
            if (shadowStyle == ShadowStyle.None)
            {
                return;
            }

            // Create vertices.
            SetVertices(camera, rect, shadowOffset, shadowSize);

            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            // Build up the technique depending on the active options.
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
                    technique += "NoShadow";
                    break;
            }

            effect.CurrentTechnique = effect.Techniques[technique];

            // Note no world matrix.
            Matrix worldViewProjMatrix = camera.ViewProjMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);

            // Set common params.
            effect.Parameters["Center"].SetValue(center);
            effect.Parameters["InnerRadius"].SetValue(new Vector3(innerRadius, innerRadius - edgeBlend, innerRadius + edgeBlend));
            effect.Parameters["OuterRadius"].SetValue(new Vector3(outerRadius, outerRadius - edgeBlend, outerRadius + edgeBlend));
            effect.Parameters["EdgeNormal0"].SetValue(edgeNormal0);
            effect.Parameters["EdgeNormal1"].SetValue(edgeNormal1);
            effect.Parameters["EdgeIntersect"].SetValue(edgeIntersect);

            effect.Parameters["BodyColor"].SetValue(Vector4.One);
            effect.Parameters["EdgeBlend"].SetValue(new Vector2(edgeBlend / camera.Zoom, 2.0f * edgeBlend / camera.Zoom));

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

        static void SetVertices(SpriteCamera camera, RectangleF rect, Padding texturePadding)
        {
            SetVertices(camera, rect, Vector2.Zero, 0, texturePadding);
        }

        static void SetVertices(SpriteCamera camera, RectangleF rect, Vector2 shadowOffset, float shadowSize, Padding texturePadding = default(Padding))
        {
            Vector2 position = rect.Position;
            Vector2 size = rect.Size;

            // Calc extra cushion needed since outer shadow will exceed rect bounds.
            {
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
                // Magic half pixel offset.
                position -= new Vector2(left, top) + new Vector2(0.5f) / camera.Zoom;
                size += new Vector2(left + right, top + bottom);

                // TODO (****) Need to test with pixel-perfect texture...
                // Check other prims, also.

                // Adjust texture Padding for these changes.
                if (texturePadding != Padding.Empty)
                {
                    texturePadding.left += (int)left;
                    texturePadding.right += (int)right;
                    texturePadding.top += (int)top;
                    texturePadding.bottom += (int)bottom;
                }

                // TODO (****) Can this function be made common to all prims rather than duplicating code?
            }

            // Set the vertex positions.
            {
                localVerts[0].pos = new Vector2(position.X, position.Y);
                localVerts[1].pos = new Vector2(position.X + size.X, position.Y);
                localVerts[2].pos = new Vector2(position.X + size.X, position.Y + size.Y);
                localVerts[3].pos = new Vector2(position.X, position.Y + size.Y);
            }

            // Set UV coords.
            {
                if (texturePadding != Padding.Empty)
                {
                    // Start by extracting desired texture size.
                    float width = size.X - texturePadding.left - texturePadding.right;
                    float height = size.Y - texturePadding.top - texturePadding.bottom;

                    float left = -texturePadding.left / width;
                    float right = 1.0f + texturePadding.right / width;
                    float top = -texturePadding.top / height;
                    float bottom = 1.0f + texturePadding.bottom / height;
                    localVerts[0].uv = new Vector2(left, top);
                    localVerts[1].uv = new Vector2(right, top);
                    localVerts[2].uv = new Vector2(right, bottom);
                    localVerts[3].uv = new Vector2(left, bottom);
                }
                else
                {
                    // Set texture to stretch across full shape.
                    // May want other options in the future.
                    localVerts[0].uv = new Vector2(0, 0);
                    localVerts[1].uv = new Vector2(1, 0);
                    localVerts[2].uv = new Vector2(1, 1);
                    localVerts[3].uv = new Vector2(0, 1);
                }
            }

        }   // end of SetVertices()

        public static void LoadContent()
        {
            if (DeviceResetX.NeedsLoad(effect))
            {
                effect = KoiLibrary.LoadEffect(@"KoiXContent\Shaders\Geometry\PieSlice");
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

    }   // end of class PieSlice

}   // end of namespace KoiX.Geometry
