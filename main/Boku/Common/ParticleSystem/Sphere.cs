
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

namespace Boku.Common.ParticleSystem
{
    /// <summary>
    /// Simple sphere geometry for particles, glows, etc.  The
    /// sphere is located at the origin and has a radius of 1.
    /// </summary>
    public class Sphere : INeedsDeviceReset
    {
        #region Members

        private static Sphere instance = null;

        private VertexBuffer vbuf = null;
        private IndexBuffer ibuf = null;

        private const int stride = 12;

        private const int segs = 6;
        private int numVertices = (segs + 1) * (segs + 1) * 6;
        private int numTriangles = segs * segs * 2 * 6;

        public struct Vertex : IVertexType
        {
            public Vector3 position;

            public Vertex(Vector3 position)
            {
                this.position = position;
            }   // end of Vertex c'tor

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0)
                // size == 12
            };

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

        #endregion

        #region Accessors

        public BoundingBox BoundingBox
        {
            get { return new BoundingBox(new Vector3(-1.0f), new Vector3(1.0f)); }
        }
        public BoundingSphere BoundingSphere
        {
            get { return new BoundingSphere(new Vector3(), 1.0f); }
        }

        public IndexBuffer Ibuf
        {
            get { return ibuf; }
        }

        public VertexBuffer Vbuf
        {
            get { return vbuf; }
        }

        public int Stride
        {
            get { return stride; }
        }

        public int NumVertices
        {
            get { return numVertices; }
        }

        public int NumTriangles
        {
            get { return numTriangles; }
        }

        #endregion

        // c'tor
        private Sphere()
        {
            BokuGame.Load(this);
        }   // end of Sphere c'tor

        /// <summary>
        /// Returns a static, shareable instance of a Fruit sro.
        /// </summary>
        public static Sphere GetInstance()
        {
            if (instance == null)
            {
                instance = new Sphere();
            }

            return instance;
        }   // end of Sphere GetInstance()

        /// <summary>
        /// Set geometry to device.
        /// </summary>
        public void PreDraw(GraphicsDevice device)
        {
            device.Indices = ibuf;
            device.SetVertexBuffer(vbuf);
        }

        /// <summary>
        /// Setup transforms, then call PreDraw and DrawPrim.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="worldMatrix"></param>
        /// <param name="effect"></param>
        public void Render(Camera camera, ref Matrix worldMatrix, Effect effect)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            Matrix worldViewProjMatrix = worldMatrix * camera.ViewProjectionMatrix;
            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);

            PreDraw(device);

            DrawPrim(effect);
        }   // end of Sphere Render()

        /// <summary>
        /// Call drawprimitive for each pass on the effect. Doesn't set any effect parameters
        /// or technique.
        /// </summary>
        /// <param name="effect"></param>
        public void DrawPrim(Effect effect)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            for (int indexEffectPass = 0; indexEffectPass < effect.CurrentTechnique.Passes.Count; indexEffectPass++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[indexEffectPass];
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, 0, numTriangles);
            }   // end loop over each pass.
        }   // end of DrawPrim()

        public void LoadContent(bool immediate)
        {
        }

        /// <summary>
        /// Create the sphere generating the geometry from a cube which is
        /// "puffed" out to be a sphere.  Segs controls how finely each side
        /// of the cube is diced up before being turned into a sphere.
        /// </summary>
        public void InitDeviceResources(GraphicsDevice device)
        {
            // Init the vertex buffer.
            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(Vertex), numVertices, BufferUsage.WriteOnly);

                // Create local vertices.
                Vertex[] localVerts = new Vertex[numVertices];

                // Generate vertices for first face.
                for (int i = 0; i <= segs; i++)
                {
                    for (int j = 0; j <= segs; j++)
                    {
                        float x = (float)i / (float)segs - 0.5f;
                        float y = (float)j / (float)segs - 0.5f;

                        localVerts[j * (segs + 1) + i] = new Vertex(new Vector3(x, y, 0.5f));
                    }
                }

                // Copy to other faces.
                int numVertsPerFace = numVertices / 6;

                for (int i = 0; i < numVertsPerFace; i++)
                {
                    // opposite face
                    localVerts[i + numVertsPerFace * 1].position.X = localVerts[i].position.X;
                    localVerts[i + numVertsPerFace * 1].position.Y = localVerts[i].position.Y;
                    localVerts[i + numVertsPerFace * 1].position.Z = -localVerts[i].position.Z;

                    // swap x and z
                    localVerts[i + numVertsPerFace * 2].position.X = -localVerts[i].position.Z;
                    localVerts[i + numVertsPerFace * 2].position.Y = localVerts[i].position.Y;
                    localVerts[i + numVertsPerFace * 2].position.Z = localVerts[i].position.X;

                    localVerts[i + numVertsPerFace * 3].position.X = localVerts[i].position.Z;
                    localVerts[i + numVertsPerFace * 3].position.Y = localVerts[i].position.Y;
                    localVerts[i + numVertsPerFace * 3].position.Z = localVerts[i].position.X;

                    // swap y and z
                    localVerts[i + numVertsPerFace * 4].position.X = localVerts[i].position.X;
                    localVerts[i + numVertsPerFace * 4].position.Y = -localVerts[i].position.Z;
                    localVerts[i + numVertsPerFace * 4].position.Z = localVerts[i].position.Y;

                    localVerts[i + numVertsPerFace * 5].position.X = localVerts[i].position.X;
                    localVerts[i + numVertsPerFace * 5].position.Y = localVerts[i].position.Z;
                    localVerts[i + numVertsPerFace * 5].position.Z = localVerts[i].position.Y;

                }

                // Normalize the vertices to make the cube into a sphere.
                for (int i = 0; i < numVertices; i++)
                {
                    localVerts[i].position.Normalize();
                }

                // Copy to vertex buffer.
                vbuf.SetData<Vertex>(localVerts);
            }

            // Create index buffer.
            if (ibuf == null)
            {
                ibuf = new IndexBuffer(device, IndexElementSize.SixteenBits, numTriangles * 3, BufferUsage.WriteOnly);

                // Generate the local copy of the index data.
                ushort[] localIBuf = new ushort[numTriangles * 3];

                int numVertsPerFace = numVertices / 6;

                // For each axis, generate the triangle indices for each face.
                int t = 0;
                for (int k = 0; k < 3; k++)
                {
                    int baseVertex = k * numVertsPerFace * 2;

                    for (int i = 0; i < segs; i++)
                    {
                        for (int j = 0; j < segs; j++)
                        {
                            localIBuf[t + 0] = (ushort)(baseVertex + j * (segs + 1) + i);
                            localIBuf[t + 2] = (ushort)(baseVertex + j * (segs + 1) + i + 1);
                            localIBuf[t + 1] = (ushort)(baseVertex + j * (segs + 1) + i + (segs + 1) + 1);
                            t += 3;
                            localIBuf[t + 0] = (ushort)(baseVertex + j * (segs + 1) + i);
                            localIBuf[t + 2] = (ushort)(baseVertex + j * (segs + 1) + i + (segs + 1) + 1);
                            localIBuf[t + 1] = (ushort)(baseVertex + j * (segs + 1) + i + (segs + 1));
                            t += 3;
                        }
                    }
                    baseVertex += numVertsPerFace;
                    for (int i = 0; i < segs; i++)
                    {
                        for (int j = 0; j < segs; j++)
                        {
                            localIBuf[t + 0] = (ushort)(baseVertex + j * (segs + 1) + i);
                            localIBuf[t + 1] = (ushort)(baseVertex + j * (segs + 1) + i + 1);
                            localIBuf[t + 2] = (ushort)(baseVertex + j * (segs + 1) + i + (segs + 1) + 1);
                            t += 3;
                            localIBuf[t + 0] = (ushort)(baseVertex + j * (segs + 1) + i);
                            localIBuf[t + 1] = (ushort)(baseVertex + j * (segs + 1) + i + (segs + 1) + 1);
                            localIBuf[t + 2] = (ushort)(baseVertex + j * (segs + 1) + i + (segs + 1));
                            t += 3;
                        }
                    }
                }

                // Copy it to the index buffer.
                ibuf.SetData<ushort>(localIBuf);
            }
        }

        public void UnloadContent()
        {
            DeviceResetX.Release(ref ibuf);
            DeviceResetX.Release(ref vbuf);
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class Sphere

}   // end of namespace Boku.SimWorld
