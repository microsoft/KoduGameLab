// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;

namespace Boku.SimWorld
{
    /// <summary>
    /// So we know which way we're going...
    /// This is a 2d quad which sits in the corner of the screen and
    /// rotates to always point north.  It also stays perpendicular
    /// to the Up vector so it gives an additional indication of the
    /// orientation of the camera.
    /// </summary>
    public class Compass : INeedsDeviceReset
    {
        private Effect effect = null;
        private VertexBuffer vbuf = null;
        private Texture2D texture = null;

        private Matrix worldMatrix = Matrix.Identity;
        private Transform localTransform = new Transform();

        #region Accessors
        #endregion



        public struct Vertex : IVertexType
        {
            private Vector3 position;
            private Vector2 texCoord;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // size == 20
            };

            public Vertex(Vector3 pos, Vector2 tex)
            {
                position = pos;
                texCoord = tex;
            }   // end of Vertex c'tor

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

        }   // end of Vertex

        // c'tors
        public Compass()
        {
        }   // end of Compass c'tor

        private void Update(Camera camera)
        {
            // Based on the camera, set the position and orientation of the compass.

            worldMatrix = Matrix.Identity;

            // Calc position of compass as an offset from the center line of the camera and assume 96 dpi.
            Vector3 offset = Vector3.Zero;

            float fd = camera.GetFocalDistance();
            float cos = (float)Math.Cos(camera.Fov / 2.0f);
            float sin = (float)Math.Sin(camera.Fov / 2.0f);
            float r = fd / cos;
            offset.Y = r * sin;
            offset.X = offset.Y * camera.AspectRatio;
            offset.Z = fd;

            offset *= 1.8f / fd;  // Adjust so Z is at 1.8 to prevent clipping and size correctly.

            // Shift to keep all of compass on screen.
            float shift = 0.15f;
            offset.X -= shift;
            offset.Y -= shift;

            Vector3 up = camera.ViewUp;
            Vector3 right = Vector3.Cross(camera.ViewDir, up);

            worldMatrix.Translation = camera.ActualFrom
                                        + camera.ViewDir * offset.Z
                                        - up * offset.Y
                                        + right * offset.X;

        }   // end of Compass Update()


        public void Render(Camera camera)
        {
            // Call the compass update from within the render to ensure that 
            // we're really using the right camera to set the position.
            Update(camera);

            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);

            // Render all passes.
            effect.Parameters["DiffuseTexture"].SetValue(texture);
            effect.CurrentTechnique = effect.Techniques["NormalAlphaColorPassNoZ"];

            device.SetVertexBuffer(vbuf);
            device.Indices = UI2D.Shared.QuadIndexBuff;

            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            }
        }   // end of Compass Render()


        public void LoadContent(bool immediate)
        {
            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\Billboard");
                ShaderGlobals.RegisterEffect("Billboard", effect);
            }

            // Load the texture.
            if (texture == null)
            {
                texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Compass");
            }

        }   // end of Compass LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            // Init the vertex buffer.
            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(Vertex), 4, BufferUsage.WriteOnly);

                // Create local vertices.
                Vertex[] localVerts = new Vertex[4];

                localVerts[0] = new Vertex(new Vector3(-0.2f, 0.2f, 0.0f), new Vector2(0, 0));
                localVerts[1] = new Vertex(new Vector3(0.2f, 0.2f, 0.0f), new Vector2(1, 0));
                localVerts[2] = new Vertex(new Vector3(0.2f, -0.2f, 0.0f), new Vector2(1, 1));
                localVerts[3] = new Vertex(new Vector3(-0.2f, -0.2f, 0.0f), new Vector2(0, 1));

                // Copy to vertex buffer.
                vbuf.SetData<Vertex>(localVerts);
            }
        }

        public void UnloadContent()
        {
            BokuGame.Release(ref vbuf);
            BokuGame.Release(ref effect);
            BokuGame.Release(ref texture);
        }   // end of Compass UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class Compass

}   // end of namespace Boku.SimWorld
