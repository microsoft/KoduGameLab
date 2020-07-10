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
using Boku.Common;

namespace Boku.Fx
{
    /// <summary>
    /// 2D quad which lives in the XY plane.  Primarily used for UI.
    /// Only one is ever created and is instanced as needed.
    /// </summary>
    public class CameraSpaceQuad : INeedsDeviceReset
    {
        private static CameraSpaceQuad instance = null;

        private Effect effect = null;

        struct Vertex : IVertexType
        {
            Vector2 uv;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // Total = 8 bytes
            };

            public Vertex(Vector2 uv)
            {
                this.uv = uv;
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

        static private Vertex[] localVerts = new Vertex[4]
            {
                new Vertex(new Vector2(0.0f, 0.0f)),
                new Vertex(new Vector2(1.0f, 0.0f)),
                new Vertex(new Vector2(0.0f, 1.0f)),
                new Vertex(new Vector2(1.0f, 1.0f))
            };


        public static CameraSpaceQuad GetInstance()
        {
            if (instance == null)
            {
                instance = new CameraSpaceQuad();
            }

            return instance;
        }   // end of CameraSpaceQuad GetInstance()

        // c'tor
        private CameraSpaceQuad()
        {
        }   // end of CameraSpaceQuad c'tor


        private void SetUvToPos(Vector2 position, Vector2 size)
        {
            // Invert Y to make Y-up positive.
            position.Y = -position.Y;

            float Y = position.Y;
            Vector4 uvToPos = new Vector4(
                size.X, // x scale
                -size.Y, // y scale
                position.X - size.X * 0.5f, // x offset
                -position.Y + size.Y * 0.5f); // y offset

            effect.Parameters["UvToPos"].SetValue(uvToPos);

        }   // end of CameraSpaceQuad SetUvToPos()

        /// <summary>
        /// Renders a quad to the screen.
        /// </summary>
        /// <param name="texture">Texture2D to render with.</param>
        /// <param name="position">Position of center of quad in world coords in Z==0 plane.</param>
        /// <param name="size">Size in world units for the quad.</param>
        /// <param name="technique">Technique to render with.</param>
        public void Render(Camera camera, Texture2D texture, Vector2 position, Vector2 size, string technique)
        {
            Render(camera, texture, 1.0f, position, size, technique);
        }   // end of CameraSpaceQuad Render()

        /// <summary>
        /// Renders a quad to the screen.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="texture">Texture2D to render with.</param>
        /// <param name="alpha">Alpha value to attenuate whole quad.</param>
        /// <param name="position">Position in world coords in Z==0 plane.</param>
        /// <param name="size">Size in world units for the quad.</param>
        /// <param name="technique">Technique to render with.</param>
        public void Render(Camera camera, Texture2D texture, float alpha, Vector2 position, Vector2 size, string technique)
        {
            Render(camera, texture, Vector4.One, alpha, position, size, technique);
        }   // end of Render()

        /// <summary>
        /// Renders a quad to the screen.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="texture">Texture2D to render with.</param>
        /// <param name="diffuseColor">COlor to attenuate whole texture by.</param>
        /// <param name="alpha">Alpha value to attenuate whole quad.</param>
        /// <param name="position">Position in world coords in Z==0 plane.</param>
        /// <param name="size">Size in world units for the quad.</param>
        /// <param name="technique">Technique to render with.</param>
        public void Render(Camera camera, Texture2D texture, Vector4 diffuseColor, float alpha, Vector2 position, Vector2 size, string technique)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            SetUvToPos(position, size);

            Matrix worldMatrix = Matrix.Identity;
            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldMatrix * camera.ViewProjectionMatrix);

            effect.Parameters["DiffuseColor"].SetValue(diffuseColor);
            effect.Parameters["DiffuseTexture"].SetValue(texture);
            effect.Parameters["Alpha"].SetValue(alpha);

            effect.CurrentTechnique = effect.Techniques[technique];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, localVerts, 0, 2);
            }
        }   // end of CameraSpaceQuad Render()

        public void RenderStencil(Camera camera, Vector4 color, Vector2 position, Vector2 size)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            SetUvToPos(position, size);

            Matrix worldMatrix = Matrix.Identity;
            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldMatrix * camera.ViewProjectionMatrix);

            effect.Parameters["DiffuseColor"].SetValue(color);

            effect.CurrentTechnique = effect.Techniques[@"Stencil"];

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, localVerts, 0, 2);
            }
        }   // end of RenderStencil()

        public void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = KoiLibrary.LoadEffect(@"Shaders\CameraSpaceQuad");
            }
        }   // end of CameraSpaceQuad LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            DeviceResetX.Release(ref effect);
        }   // end of CameraSpaceQuad UnloadContent()

        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class CameraSpaceQuad

}   // end of namespace Boku.Common
