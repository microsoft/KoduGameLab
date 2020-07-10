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
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku
{
    public class LightColumn : GameObject
    {
        public class Shared
        {
            private Vector3 position;
            private float altitude = 0.0f;      // Height above terrain.
            private float radius = 1.0f;
            private float height = 1.0f;
            private float textureOffset = 0.0f;

            public Matrix worldMatrix;

            #region Accessors
            public Vector3 Position
            {
                get { return position; }
                set
                {
                    position = value;
                    position.Z = altitude + Terrain.GetHeight(position);
                }
            }
            /// <summary>
            /// Distance above/below ground column starts.
            /// </summary>
            public float Altitude
            {
                get { return altitude; }
                set { altitude = value; }
            }
            public float Radius
            {
                get { return radius; }
                set { radius = value; }
            }
            public float Height
            {
                get { return height; }
                set { height = value; }
            }
            public float TextureOffset
            {
                get { return textureOffset; }
                set { textureOffset = value; }
            }
            #endregion

            public Shared()
            {
                position = new Vector3();
                altitude = 0.2f;
                radius = 0.8f;
                height = 80.0f;

                worldMatrix = Matrix.Identity;
            }
        }

        protected class UpdateObj : UpdateObject
        {
            private Shared shared = null;

            public UpdateObj(ref Shared shared)
            {
                this.shared = shared;
            }   // end of UpdateObj c'tor

            public override void Update()
            {
                shared.worldMatrix = Matrix.Identity;

                // Apply translation.
                shared.worldMatrix.Translation = shared.Position;

                // Apply the scaling.
                shared.worldMatrix.M11 *= shared.Radius;
                shared.worldMatrix.M22 *= shared.Radius;
                shared.worldMatrix.M33 *= shared.Height;

                // Move texture.
                shared.TextureOffset += Time.WallClockFrameSeconds * 0.1f;
                shared.TextureOffset = (float)Math.IEEERemainder(shared.TextureOffset, 1.0);

            }   // end of UpdateObj Update()
            public override void Activate()
            {

            }
            public override void Deactivate()
            {

            }
        }   // end of class UpdateObj

        protected class RenderObj : RenderObject
        {
            private Shared shared = null;
            private GraphicsDevice device = null;

            private VertexBuffer vbuf = null;
            private IndexBuffer ibuf = null;
            private Texture texture = null;
            private Effect effect = null;

            private Vector4 diffuse;            // Local override for diffuse color.
            private int numTriangles = 0;
            private int numVertices = 0;

            // Declare the vertex structure we'll use for the cursor.
            static private VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
                new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
                // size == 32
            };

            private const int stride = 32;
            private static VertexDeclaration decl = null;

            public struct Vertex
            {
                private Vector3 position;
                private Vector3 normal;
                private Vector2 tex;

                public Vertex(Vector3 position, Vector3 normal, Vector2 tex)
                {
                    this.position = position;
                    this.normal = normal;
                    this.tex = tex;
                }   // end of Vertex c'tor

            }   // end of Vertex


            public RenderObj(GraphicsDevice device, ref Shared shared, Vector4 diffuse)
            {
                this.device = device;
                this.shared = shared;

                this.diffuse = diffuse;

                Init();
            }   // end of RenderObj c'tor

            public Vector4 DiffuseColor
            {
                set { diffuse = value; }
                get { return diffuse; }
            }
            public void Init()
            {
                // Init the vertex decl.
                if (decl == null)
                {
                    decl = new VertexDeclaration(device, elements);
                }

                // Init the effect.
                if (effect == null)
                {
                    effect = BokuGame.ContentManager.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\LightColumn");
                }

                // Load the texture.
                if (texture == null)
                {
                    texture = BokuGame.ContentManager.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LightColumn");
                }

                //
                // Generate the geometry.
                //

                // Ring
                const int numSegments = 32;
                numTriangles = numSegments * 2 + 2;
                numVertices = numSegments * 2 + 2;

                const float radius = 1.0f;

                // Init the vertex buffer.
                if (vbuf == null)
                {
                    vbuf = new VertexBuffer(device, typeof(Vertex), numVertices, BufferUsage.WriteOnly);
                }

                // Create local vertices.
                Vertex[] localVerts = new Vertex[numVertices];

                int index = 0;
                for (int i = 0; i < numSegments + 1; i++)
                {
                    float t = i * (2.0f * (float)Math.PI) / numSegments;
                    float s = (float)Math.Sin(t);
                    float c = (float)Math.Cos(t);
                    Vector3 normal = new Vector3(c, s, 0.0f);

                    localVerts[index++] = new Vertex(new Vector3(c * radius, s * radius, 0.0f), normal, new Vector2(t/(2.0f * (float)Math.PI), 1.0f));
                    localVerts[index++] = new Vertex(new Vector3(c * radius, s * radius, 1.0f), normal, new Vector2(t/(2.0f * (float)Math.PI), 0.0f));
                }

                // Copy to vertex buffer.
                vbuf.SetData<Vertex>(localVerts);


                // Create index buffer.
                if (ibuf == null)
                {
                    ibuf = new IndexBuffer(device, numTriangles * 3 * sizeof(ushort), BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                }

                // Generate the local copy of the data.
                ushort[] localIBuf = new ushort[numTriangles * 3];

                index = 0;
                for (int i = 0; i < numSegments; i++)
                {
                    localIBuf[index++] = (ushort)(0 + i * 2);
                    localIBuf[index++] = (ushort)(1 + i * 2);
                    localIBuf[index++] = (ushort)(2 + i * 2);
                    localIBuf[index++] = (ushort)(2 + i * 2);
                    localIBuf[index++] = (ushort)(1 + i * 2);
                    localIBuf[index++] = (ushort)(3 + i * 2);
                }

                // Copy it to the index buffer.
                ibuf.SetData<ushort>(localIBuf);

            }   // end of RenderObj Init()


            public override void Render(Camera camera)
            {
                Matrix viewMatrix = camera.ViewMatrix;
                Matrix projMatrix = camera.ProjectionMatrix;

                Matrix worldViewProjMatrix = shared.worldMatrix * viewMatrix * projMatrix;
                effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
                effect.Parameters["WorldMatrix"].SetValue(shared.worldMatrix);

                effect.Parameters["LightTexture"].SetValue(texture);

                effect.Parameters["Color"].SetValue(diffuse);
                effect.Parameters["TextureOffset"].SetValue(shared.TextureOffset);

                device.VertexDeclaration = decl;
                device.Vertices[0].SetSource(vbuf, 0, stride);
                device.Indices = ibuf;

                // Render all passes.
                effect.CurrentTechnique = effect.Techniques["AdditiveTexturedColorPass"];

                effect.Begin();
                for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[i];
                    pass.Begin();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, 0, numTriangles);
                    pass.End();
                }
                effect.End();

            }   // end of RenderObj Render()

            public override void Activate()
            {

            }

            public override void Deactivate()
            {

            }
        }   // end of class RenderObj


        //
        //  LightColumn
        //

        private RenderObj renderObj = null;
        private UpdateObj updateObj = null;
        public Shared shared = null;

        private bool state = false;
        private bool pendingState = false;

        public LightColumn(GraphicsDevice device,
                Vector2 position,
                Vector4 color)
        {
            shared = new Shared();
            shared.Position = new Vector3(position.X, position.Y, 0.0f);

            // Z is set during update to match terrain.

            renderObj = new RenderObj(device, ref shared, color);
            updateObj = new UpdateObj(ref shared);
        }   // end of LightColumn c'tor

        private LightColumn()
        {
        }

        public Vector4 DiffuseColor
        {
            set { renderObj.DiffuseColor = value; }
            get { return renderObj.DiffuseColor; }
        }

        public override bool Refresh(ArrayList updateList, ArrayList renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();
                }
                else
                {
                    BokuGame.gameListManager.RemoveObject(this);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);

                    result = true;
                }

                state = pendingState;
            }

            // call refresh on child list

            return result;
        }   // end of LightColumn Refresh()

        override public void Activate()
        {
            if (!state)
            {
                pendingState = true;
                BokuGame.objectListDirty = true;
            }
        }
        override public void Deactivate()
        {
            if (state)
            {
                pendingState = false;
                BokuGame.objectListDirty = true;
            }
        }
    }   // end of class LightColumn

}   // end of namespace Boku



