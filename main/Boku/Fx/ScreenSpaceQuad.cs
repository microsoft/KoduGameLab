// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


/// Relocated from Boku.Common namespace

using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Base;

namespace Boku.Fx
{
    /// <summary>
    /// 2D quad which lives in screen space.  Uses pixel coordinates.
    /// Only one is ever created and is instanced as needed.
    /// </summary>
    public class ScreenSpaceQuad : INeedsDeviceReset
    {
        private static ScreenSpaceQuad instance = null;

        public static Effect effect = null;

        public struct Vertex : IVertexType
        {
            public Vector2 pos;     // Expanded to a Vector4 in the vertex shader.
            public Vector2 tex;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // Total = 16 bytes
            };

            public Vertex(Vector2 position, Vector2 tex)
            {
                this.pos = position;
                this.tex = tex;
            }

            public Vertex(Vertex v)
            {
                this.pos = v.pos;
                this.tex = v.tex;
            }

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    if (decl == null || decl.IsDisposed)
                    {
                        decl = new VertexDeclaration(elements);
                    }
                    return decl;
                }
            }

        }

        private Vertex[] localVerts = new Vertex[4];

        public static ScreenSpaceQuad GetInstance()
        {
            if (instance == null)
            {
                instance = new ScreenSpaceQuad();
            }

            return instance;
        }   // end of ScreenSpaceQuad GetInstance()

        // c'tor
        private ScreenSpaceQuad()
        {
        }   // end of ScreenSpaceQuad c'tor

        private void UpdateVertices(Vector2 position, Vector2 size)
        {
            // Magical half pixel offset.
            position += new Vector2(-0.5f, -0.5f);

            // Transform position and size to homogeneous coordinates.
            Vector2 viewport = new Vector2((float)KoiLibrary.GraphicsDevice.Viewport.Width, (float)KoiLibrary.GraphicsDevice.Viewport.Height);

            position = 2.0f * position / viewport - new Vector2(1.0f, 1.0f);
            size = size * 2.0f / viewport;

            // Fill in the local vertex data.
            localVerts[0] = new Vertex(new Vector2(position.X + size.X, -position.Y - size.Y),   new Vector2(1.0f, 1.0f));
            localVerts[1] = new Vertex(new Vector2(position.X + size.X, -position.Y),            new Vector2(1.0f, 0.0f));
            localVerts[2] = new Vertex(new Vector2(position.X, -position.Y),                     new Vector2(0.0f, 0.0f));
            localVerts[3] = new Vertex(new Vector2(position.X, -position.Y - size.Y),            new Vector2(0.0f, 1.0f));
        }   // end of ScreenSpaceQuad UpdateVertices()

        /// <summary>
        /// Renders a screen space quad using both a texture and a shadow mask texture.
        /// </summary>
        /// <param name="diffuse"></param>
        /// <param name="shadowMask"></param>
        /// <param name="position">Position in pixel coords.  0,0 is upper left hand corner.</param>
        /// <param name="size">Size in pixels for the quad.</param>
        public void RenderWithShadowMask(Texture2D diffuse, Texture2D shadowMask, Vector2 position, Vector2 size)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            UpdateVertices(position, size);

            effect.Parameters["DiffuseColor"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["DiffuseTexture"].SetValue(diffuse);
            effect.Parameters["ShadowMaskTexture"].SetValue(shadowMask);

            effect.CurrentTechnique = effect.Techniques["DropShadow"];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, SharedX.QuadIndices, 0, 2);
            }
        }   // end of ScreenSpaceQuad RenderWithShadowMask()

        /// <summary>
        /// Renders a screen space quad using a solid color and a shadow mask texture.
        /// </summary>
        /// <param name="diffuseColor"></param>
        /// <param name="shadowMask"></param>
        /// <param name="position">Position in pixel coords.  0,0 is upper left hand corner.</param>
        /// <param name="size">Size in pixels for the quad.</param>
        public void RenderWithShadowMask(Vector4 diffuseColor, Texture2D shadowMask, Vector2 position, Vector2 size)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            UpdateVertices(position, size);

            effect.Parameters["DiffuseColor"].SetValue(diffuseColor);
            effect.Parameters["ShadowMaskTexture"].SetValue(shadowMask);

            effect.CurrentTechnique = effect.Techniques["SolidColorWithDropShadow"];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, SharedX.QuadIndices, 0, 2);
            }
        }   // end of ScreenSpaceQuad RenderWithShadowMask()

        /// <summary>
        /// Renders a quad to the screen.
        /// </summary>
        /// <param name="texture">Texture2D to render with.</param>
        /// <param name="position">Position in pixel coords.  0,0 is upper left hand corner.</param>
        /// <param name="size">Size in pixels for the quad.</param>
        /// <param name="technique">Technique to render with.</param>
        public void Render(Texture2D texture, Vector2 position, Vector2 size, string technique)
        {
            Render(texture, new Vector4(1.0f, 1.0f, 1.0f, 1.0f), position, size, technique);
        }

        /// <summary>
        /// Renders a quad to the screen.
        /// </summary>
        /// <param name="texture">Texture2D to render with.</param>
        /// <param name="diffuseColor">Color with alpha to render with.</param>
        /// <param name="position">Position in pixel coords.  0,0 is upper left hand corner.</param>
        /// <param name="size">Size in pixels for the quad.</param>
        /// <param name="technique">Technique to render with.</param>
        public void Render(Texture2D texture, Vector4 diffuseColor, Vector2 position, Vector2 size, string technique)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            UpdateVertices(position, size);

            if (texture == null || texture.IsDisposed || texture.GraphicsDevice.IsDisposed)
            {
                return;
            }

            effect.Parameters["DiffuseColor"].SetValue(diffuseColor);
            effect.Parameters["DiffuseTexture"].SetValue(texture);

            effect.CurrentTechnique = effect.Techniques[technique];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                try
                {
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, SharedX.QuadIndices, 0, 2);
                }
                catch
                {
                }
            }
        }   // end of ScreenSpaceQuad Render()

        /// <summary>
        /// Renders a quad to the screen.
        /// </summary>
        /// <param name="texture">Texture2D to render with.</param>
        /// <param name="position">Position in pixel coords.  0,0 is upper left hand corner.</param>
        /// <param name="size">Size in pixels for the quad.</param>
        /// <param name="technique">Technique to render with.</param>
        public void RenderWithMask(Texture2D texture, Texture2D mask, Vector2 position, Vector2 size, string technique)
        {
            RenderWithMask(texture, mask, new Vector4(1.0f, 1.0f, 1.0f, 1.0f), position, size, technique);
        }

        /// <summary>
        /// Renders a quad to the screen.
        /// </summary>
        /// <param name="texture">Texture2D to render with.</param>
        /// <param name="mask">Texture2D to use as mask.</param>
        /// <param name="diffuseColor">Color with alpha to render with.</param>
        /// <param name="position">Position in pixel coords.  0,0 is upper left hand corner.</param>
        /// <param name="size">Size in pixels for the quad.</param>
        /// <param name="technique">Technique to render with.</param>
        public void RenderWithMask(Texture2D texture, Texture2D mask,  Vector4 diffuseColor, Vector2 position, Vector2 size, string technique)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            UpdateVertices(position, size);

            effect.Parameters["DiffuseColor"].SetValue(diffuseColor);
            effect.Parameters["DiffuseTexture"].SetValue(texture);
            effect.Parameters["MaskTexture"].SetValue(mask);

            effect.CurrentTechnique = effect.Techniques[@"Mask" + technique];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                try
                {
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, SharedX.QuadIndices, 0, 2);
                }
                catch
                {
                }
            }
        }   // end of ScreenSpaceQuad RenderWithMask()

        /// <summary>
        /// Renders a quad to the screen.
        /// </summary>
        /// <param name="texture">Texture2D to render with.</param>
        /// <param name="limits">min, max, min, max limits for y range in uv coords.  First pair defines where texture fades in, second pair where it fades out.</param>
        /// <param name="position">Position in pixel coords.  0,0 is upper left hand corner.</param>
        /// <param name="size">Size in pixels for the quad.</param>
        /// <param name="technique">Technique to render with.</param>
        public void RenderWithYLimits(Texture2D texture, Vector4 limits, Vector2 position, Vector2 size, string technique)
        {
            RenderWithYLimits(texture, limits, new Vector4(1.0f, 1.0f, 1.0f, 1.0f), position, size, technique);
        }

        /// <summary>
        /// Renders a quad to the screen.
        /// </summary>
        /// <param name="texture">Texture2D to render with.</param>
        /// <param name="mask">Texture2D to use as mask.</param>
        /// <param name="diffuseColor">Color with alpha to render with.</param>
        /// <param name="position">Position in pixel coords.  0,0 is upper left hand corner.</param>
        /// <param name="size">Size in pixels for the quad.</param>
        /// <param name="technique">Technique to render with.</param>
        public void RenderWithYLimits(Texture2D texture, Vector4 limits, Vector4 diffuseColor, Vector2 position, Vector2 size, string technique)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            UpdateVertices(position, size);

            effect.Parameters["DiffuseColor"].SetValue(diffuseColor);
            effect.Parameters["DiffuseTexture"].SetValue(texture);
            effect.Parameters["YLimits"].SetValue(limits);

            effect.CurrentTechnique = effect.Techniques[@"YLimit" + technique];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                try
                {
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, SharedX.QuadIndices, 0, 2);
                }
                catch
                {
                }
            }
        }   // end of ScreenSpaceQuad RenderWithMask()

        /// <summary>
        /// Renders a quad in a solid color w/o any texturing.
        /// </summary>
        /// <param name="diffuseColor">Color (with alpha) to render the quad.</param>
        /// <param name="position">Upper left position in pixels.</param>
        /// <param name="size">Size in pixels.</param>
        public void Render(Vector4 diffuseColor, Vector2 position, Vector2 size)
        {
            Render(diffuseColor, position, size, "SolidColor");
        }

        /// <summary>
        /// Renders a quad in a solid color w/o any texturing.
        /// </summary>
        /// <param name="diffuseColor">Color (with alpha) to render the quad.</param>
        /// <param name="position">Upper left position in pixels.</param>
        /// <param name="size">Size in pixels.</param>
        public void Render(Vector4 diffuseColor, Vector2 position, Vector2 size, string technique)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            UpdateVertices(position, size);

            if (effect == null)
            {
                LoadContent(true);
            }

            effect.Parameters["DiffuseColor"].SetValue(diffuseColor);

            effect.CurrentTechnique = effect.Techniques[technique];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                try
                {
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, SharedX.QuadIndices, 0, 2);
                }
                catch
                {
                }
            }
        }   // end of ScreenSpaceQuad Render()

        /// <summary>
        /// Specialty render call to render quad using the sky gradient.
        /// </summary>
        /// <param name="gradient"></param>
        public void RenderGradient(Vector4[] gradient)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            Vector2 position = Vector2.Zero;
            Vector2 size = new Vector2(device.Viewport.Width, device.Viewport.Height);
            UpdateVertices(position, size);

            effect.Parameters["Color0"].SetValue(gradient[0]);
            effect.Parameters["Color1"].SetValue(gradient[1]);
            effect.Parameters["Color2"].SetValue(gradient[2]);
            effect.Parameters["Color3"].SetValue(gradient[3]);
            effect.Parameters["Color4"].SetValue(gradient[4]);

            effect.CurrentTechnique = effect.Techniques["Gradient"];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                try
                {
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, SharedX.QuadIndices, 0, 2);
                }
                catch
                {
                }
            }
        }   // end of ScreenSpaceQuad RenderGradient()

        /// <summary>
        /// Renders into the stencil buffer.
        /// </summary>
        /// <param name="diffuseColor"></param>
        /// <param name="position"></param>
        /// <param name="size"></param>
        public void RenderStencil(Vector4 diffuseColor, Vector2 position, Vector2 size)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            UpdateVertices(position, size);

            effect.Parameters["DiffuseColor"].SetValue(diffuseColor);

            effect.CurrentTechnique = effect.Techniques["Stencil"];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                try
                {
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, SharedX.QuadIndices, 0, 2);
                }
                catch
                {
                }
            }
        }   // end of ScreenSpaceQuad RenderStencil()

        /// <summary>
        /// A specialized render call for rendering things like ratings where 
        /// you want part of the quad rendered with one texture and the rest 
        /// of the quad rendered with another.
        /// </summary>
        /// <param name="leftTexture"></param>
        /// <param name="rightTexture"></param>
        /// <param name="t">0..1 value where the dividing line between the textures is.</param>
        /// <param name="position"></param>
        /// <param name="size"></param>
        public void RenderSplitTexture(Texture2D leftTexture, Texture2D rightTexture, float t, Vector2 position, Vector2 size)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            UpdateVertices(position, size);

            effect.CurrentTechnique = effect.Techniques["SplitTexturedRegularAlpha"];
            effect.Parameters["LeftTexture"].SetValue(leftTexture);
            effect.Parameters["RightTexture"].SetValue(rightTexture);
            effect.Parameters["T"].SetValue(t);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                try
                {
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, localVerts, 0, 4, SharedX.QuadIndices, 0, 2);
                }
                catch
                {
                }
            }
        }   // end of RenderSplitTexture()


        public void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\ScreenSpaceQuad");
            }
        }   // end of ScreenSpaceQuad LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            DeviceResetX.Release(ref effect);
        }   // end of ScreenSpaceQuad UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class ScreenSpaceQuad

}   // end of namespace Boku.Common
