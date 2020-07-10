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

namespace Boku.UI2D
{
    /// <summary>
    /// Your classic 9-grid based UI entity.  Combined with the
    /// appropriate textures and normal maps this will allow us 
    /// to have a nicely scalable tile-shaped 2D object.  This is
    /// just the geometry and a holder for the size params.
    /// 
    /// The geometry is created in the XY plane at Z==0 centered
    /// at the origin.  The extents are +- width/2 in the X direction
    /// and +- height/2 in the Y direction.
    /// </summary>
    public class Base9Grid : INeedsDeviceReset
    {
        private const int numTriangles = 18;
        private const int numVertices = 16;

        private VertexBuffer vbuf = null;
        private static IndexBuffer ibuf = null;     // We only need one shared among all the 9-grid objects.

        private float width = 0.0f;
        private float height = 0.0f;
        private float edgeSize = 0.0f;

        private float maxU = 1.0f;      // In the case that we have a card that is restricted to power of two 
        private float maxV = 1.0f;      // textures we want to be able to limit the UV coords for the 9-grid.

        #region Accessors
        public float Width
        {
            get { return width; }
        }
        public float Height
        {
            get { return height; }
        }
        public float EdgeSize
        {
            get { return edgeSize; }
        }
        public int NumTriangles
        {
            get { return numTriangles; }
        }
        public int NumVertices
        {
            get { return numVertices; }
        }
        public VertexBuffer VBuf
        {
            get { return vbuf; }
        }
        public IndexBuffer IBuf
        {
            get { return ibuf; }
        }
        #endregion

        private const int stride = 24;

        public struct Vertex : IVertexType
        {
            private Vector2 pos;    // Expanded to a Vector4 in the vertex shader.
            private Vector2 scaledTex;
            private Vector2 overallTex;

            static VertexDeclaration decl = null;   // We only need one shared among all the 9-grid objects.   
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),              // Expanded to vector3 in the shader...
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),     // For edges & normal map.
                new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1)     // Overall coords.
                // Total = 24 bytes
            };

            // c'tor
            public Vertex(Vector2 pos, Vector2 scaledTex, Vector2 overallTex)
            {
                this.pos = pos;
                this.scaledTex = scaledTex;
                this.overallTex = overallTex;
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
        public Base9Grid(float width, float height, float edgeSize)
        {
            this.width = width;
            this.height = height;
            this.edgeSize = edgeSize;

            // Sanity checks...
            Debug.Assert(width >= 0.0f);
            Debug.Assert(height >= 0.0f);
            Debug.Assert(edgeSize >= 0.0f);
            Debug.Assert(width >= 2.0f * edgeSize);
            Debug.Assert(height >= 2.0f * edgeSize);

        }

        public Base9Grid(float width, float height, float edgeSize, float maxU, float maxV)
        {
            this.width = width;
            this.height = height;
            this.edgeSize = edgeSize;
            this.maxU = maxU;
            this.maxV = maxV;

            // Sanity checks...
            Debug.Assert(width >= 0.0f);
            Debug.Assert(height >= 0.0f);
            Debug.Assert(edgeSize >= 0.0f);
            Debug.Assert(width >= 2.0f * edgeSize);
            Debug.Assert(height >= 2.0f * edgeSize);

        }   // end of Base9Grid c'tor


        /// <summary>
        /// This is just a thin render shell.  It expects that the effect 
        /// and all the variables have been set up before calling this.
        /// </summary>
        public void Render(Effect effect)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            device.Indices = ibuf;
            device.SetVertexBuffer(vbuf);

            // Render all passes.
            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, 0, numTriangles);
            }
            device.Indices = null;
            device.SetVertexBuffer(null);
        }   // end of Base9Grid Render()


        public void LoadContent(bool immediate)
        {
        }   // end of Base9Grid LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            //
            // Init the static elements if needed.
            //

            // Init the index buffer.
            if (ibuf == null)
            {
                // 18 triangles, 3 vertices per triangle.
                ibuf = new IndexBuffer(device, IndexElementSize.SixteenBits, numTriangles * 3, BufferUsage.WriteOnly);

                // Generate the local copy of the data.
                ushort[] localIBuf = new ushort[numTriangles * 3];

                int index = 0;
                localIBuf[index++] = (ushort)0;
                localIBuf[index++] = (ushort)1;
                localIBuf[index++] = (ushort)5;

                localIBuf[index++] = (ushort)0;
                localIBuf[index++] = (ushort)5;
                localIBuf[index++] = (ushort)4;

                localIBuf[index++] = (ushort)1;
                localIBuf[index++] = (ushort)2;
                localIBuf[index++] = (ushort)6;

                localIBuf[index++] = (ushort)1;
                localIBuf[index++] = (ushort)6;
                localIBuf[index++] = (ushort)5;

                localIBuf[index++] = (ushort)2;
                localIBuf[index++] = (ushort)3;
                localIBuf[index++] = (ushort)7;

                localIBuf[index++] = (ushort)2;
                localIBuf[index++] = (ushort)7;
                localIBuf[index++] = (ushort)6;

                localIBuf[index++] = (ushort)4;
                localIBuf[index++] = (ushort)5;
                localIBuf[index++] = (ushort)9;

                localIBuf[index++] = (ushort)4;
                localIBuf[index++] = (ushort)9;
                localIBuf[index++] = (ushort)8;

                localIBuf[index++] = (ushort)5;
                localIBuf[index++] = (ushort)6;
                localIBuf[index++] = (ushort)10;

                localIBuf[index++] = (ushort)5;
                localIBuf[index++] = (ushort)10;
                localIBuf[index++] = (ushort)9;

                localIBuf[index++] = (ushort)6;
                localIBuf[index++] = (ushort)7;
                localIBuf[index++] = (ushort)11;

                localIBuf[index++] = (ushort)6;
                localIBuf[index++] = (ushort)11;
                localIBuf[index++] = (ushort)10;

                localIBuf[index++] = (ushort)8;
                localIBuf[index++] = (ushort)9;
                localIBuf[index++] = (ushort)13;

                localIBuf[index++] = (ushort)8;
                localIBuf[index++] = (ushort)13;
                localIBuf[index++] = (ushort)12;

                localIBuf[index++] = (ushort)9;
                localIBuf[index++] = (ushort)10;
                localIBuf[index++] = (ushort)14;

                localIBuf[index++] = (ushort)9;
                localIBuf[index++] = (ushort)14;
                localIBuf[index++] = (ushort)13;

                localIBuf[index++] = (ushort)10;
                localIBuf[index++] = (ushort)11;
                localIBuf[index++] = (ushort)15;

                localIBuf[index++] = (ushort)10;
                localIBuf[index++] = (ushort)15;
                localIBuf[index++] = (ushort)14;

                // Copy it to the index buffer.
                ibuf.SetData<ushort>(localIBuf);

            }

            //
            // Init the instance specific items.
            //

            // Init the vertex buffer.
            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(Vertex), numVertices, BufferUsage.WriteOnly);

                Vertex[] localVerts = new Vertex[numVertices];

                // Fill in the local vertex data.
                float w2 = width / 2.0f;
                float h2 = height / 2.0f;
                localVerts[0] = new Vertex(new Vector2(-w2, h2), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f));
                localVerts[1] = new Vertex(new Vector2(-w2 + edgeSize, h2), new Vector2(0.5f, 0.0f), new Vector2(maxU * edgeSize / width, 0.0f));
                localVerts[2] = new Vertex(new Vector2(w2 - edgeSize, h2), new Vector2(0.5f, 0.0f), new Vector2(maxU * (1.0f - edgeSize / width), 0.0f));
                localVerts[3] = new Vertex(new Vector2(w2, h2), new Vector2(1.0f, 0.0f), new Vector2(maxU, 0.0f));

                localVerts[4] = new Vertex(new Vector2(-w2, h2 - edgeSize), new Vector2(0.0f, 0.5f), new Vector2(0.0f, maxV * edgeSize / height));
                localVerts[5] = new Vertex(new Vector2(-w2 + edgeSize, h2 - edgeSize), new Vector2(0.5f, 0.5f), new Vector2(maxU * (edgeSize / width), maxV * edgeSize / height));
                localVerts[6] = new Vertex(new Vector2(w2 - edgeSize, h2 - edgeSize), new Vector2(0.5f, 0.5f), new Vector2(maxU * (1.0f - edgeSize / width), maxV * edgeSize / height));
                localVerts[7] = new Vertex(new Vector2(w2, h2 - edgeSize), new Vector2(1.0f, 0.5f), new Vector2(maxU, maxV * edgeSize / height));

                localVerts[8] = new Vertex(new Vector2(-w2, -h2 + edgeSize), new Vector2(0.0f, 0.5f), new Vector2(0.0f, maxV * (1.0f - edgeSize / height)));
                localVerts[9] = new Vertex(new Vector2(-w2 + edgeSize, -h2 + edgeSize), new Vector2(0.5f, 0.5f), new Vector2(maxU * (edgeSize / width), maxV * (1.0f - edgeSize / height)));
                localVerts[10] = new Vertex(new Vector2(w2 - edgeSize, -h2 + edgeSize), new Vector2(0.5f, 0.5f), new Vector2(maxU * (1.0f - edgeSize / width), maxV * (1.0f - edgeSize / height)));
                localVerts[11] = new Vertex(new Vector2(w2, -h2 + edgeSize), new Vector2(1.0f, 0.5f), new Vector2(maxU, maxV * (1.0f - edgeSize / height)));

                localVerts[12] = new Vertex(new Vector2(-w2, -h2), new Vector2(0.0f, 1.0f), new Vector2(0.0f, maxV));
                localVerts[13] = new Vertex(new Vector2(-w2 + edgeSize, -h2), new Vector2(0.5f, 1.0f), new Vector2(maxU * (edgeSize / width), maxV));
                localVerts[14] = new Vertex(new Vector2(w2 - edgeSize, -h2), new Vector2(0.5f, 1.0f), new Vector2(maxU * (1.0f - edgeSize / width), maxV));
                localVerts[15] = new Vertex(new Vector2(w2, -h2), new Vector2(1.0f, 1.0f), new Vector2(maxU, maxV));

                // Copy to vertex buffer.
                vbuf.SetData<Vertex>(localVerts);
            }

        }   // end of Base9Grid Init()

        public void UnloadContent()
        {
            BokuGame.Release(ref ibuf);
            BokuGame.Release(ref vbuf);
        }   // end of Base9Grid UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class Base9Grid

}   // end of namespace Boku.UI2D
