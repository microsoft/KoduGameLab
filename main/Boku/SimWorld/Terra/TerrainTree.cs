// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;

namespace Boku.SimWorld
{
    class TerrainTree
    {
        #region Geometry Structures
        /// <summary>
        /// The vertices we build.
        /// </summary>
        public class Vertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector3 debug;
            public int index;

            public Vertex(Vector3 p, Vector3 n, int i)
            {
                position = p;
                normal = n;
                debug = new Vector3(0.0f, 0.5f, 0.0f);
                index = i;
            }
            public static Vertex operator +(Vertex v0, Vertex v1)
            {
                Vertex ret = TerrainTree.NewVertex(v0);
                ret.position += v1.position;
                ret.normal += v1.normal;

                return ret;
            }
            public static Vertex operator *(Vertex v, float s)
            {
                v.position *= s;
                v.normal *= s;
                return v;
            }

        }
        public struct SkirtVertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 uv;
        }
        /// <summary>
        /// The vertices we render
        /// </summary>
        public struct DrawVertex
        {
            public Vector3 position;
            public Vector3 normal;
        }
        public class SkirtDraw
        {
            public IndexBuffer ibuf;
            public int primCount;
        }

        /// <summary>
        /// Geometry data for the terrain
        /// </summary>
        static protected VertexBuffer vbuf = null;
        static protected int numQuadVerts = 0;
        static protected IndexBuffer[,] ibuf = null;
        static protected int[,] primCount = null;

        /// <summary>
        /// And for the skirts
        /// </summary>
        static protected VertexBuffer skirtVBuf = null;
        static protected int numSkirtVerts = 0;
        static protected List<SkirtDraw>[,] skirtIBuf = null;

        /// <summary>
        /// The list of verts we're building
        /// </summary>
        static protected List<Vertex> vertexList = null;

        /// <summary>
        /// Bounding box for each tile, null if tile hidden
        /// </summary>
        static protected AABB[,] tileBounds = null;

        /// <summary>
        /// Whether to remap verts to increase cache efficiency on the GPU.
        /// See notes at RemapVerts<>()
        /// </summary>
        static bool doRemap = true;

        #endregion Geometry Structures

        /// <summary>
        /// The heightmap we're building from
        /// </summary>
        static protected HeightMap heightMap = null;
        static protected float invisiHeight = 0.0f;

        #region State
        /// <summary>
        /// Our state as we build the tree.
        /// </summary>
        static protected int numProcessed = 0;
        static protected List<Quad> procList = new List<Quad>();
        static protected Quad[,] rootList = null;
        static protected List<float[,]>[,] kurvatures = null;
        static protected float[,] kurvBase = null;
        static protected Point numTiles;
        static protected bool enabled = false;
        static protected bool pending = false;
        #endregion State

        #region Accessors
        static int NumQuads
        {
            get { return procList.Count; }
        }
        static public int NumQuadVerts
        {
            get { return vertexList == null ? numQuadVerts : vertexList.Count; }
        }
        static public int NumSkirtVerts
        {
            get { return numSkirtVerts; }
        }
        static public Point NumTiles
        {
            get { return numTiles; }
        }
        static public int Stride
        {
            get { return 24; }
        }
        static public int SkirtStride
        {
            get { return 32; }
        }
        static public VertexBuffer VertexBuffer
        {
            get { return vbuf; }
        }
        static public IndexBuffer IndexBuffer(int i, int j)
        {
            return ibuf[i, j];
        }
        static public AABB BoundingBox(int i, int j)
        {
            return tileBounds[i, j];
        }
        static public int PrimCount(int i, int j)
        {
            return primCount[i, j];
        }
        static public bool Enabled
        {
            get { return enabled; }
        }
        static public bool Pending
        {
            get { return pending; }
        }
        #endregion Accessors

        /// <summary>
        /// Enable and disable the tree. Ideally disable during intensive
        /// updates, and then re-enable (and rebuild) at end.
        /// </summary>
        static public void Enable()
        {
            enabled = false;
            pending = false;
        }
        static public void Disable()
        {
            Dispose();
            enabled = false;
            pending = true;
        }

        #region Renderable Versions



        /// <summary>
        /// Make vertex buffers
        /// </summary>
        static protected void MakeVerts()
        {
            if (vbuf != null)
            {
                DisposeVbuf();
            }
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
            numQuadVerts = vertexList.Count;
            Debug.Assert(numQuadVerts < (1 << 16)); // may need to move up to 32 bit indices.
            vbuf = new VertexBuffer(device, typeof(DrawVertex), numQuadVerts, BufferUsage.WriteOnly);

            DrawVertex[] drawVerts = new DrawVertex[numQuadVerts];

            for (int i = 0; i < numQuadVerts; ++i)
            {
                drawVerts[i].position = vertexList[i].position;
                drawVerts[i].normal = vertexList[i].normal;

                //drawVerts[i].normal = vertexList[i].debug;

                //float k = Kurvature(1000, drawVerts[i].position.X, drawVerts[i].position.Y);
                //drawVerts[i].normal = new Vector3(k, k, k);
            }

            // Copy to vertex buffer.
            vbuf.SetData<DrawVertex>(drawVerts);
        }
        /// <summary>
        /// Dispose vertex buffer resources
        /// </summary>
        static protected void DisposeVbuf()
        {
            if (vbuf != null)
            {
                vbuf.Dispose();
                vbuf = null;
            }
        }
        /// <summary>
        /// Dispose index buffer resources
        /// </summary>
        static protected void DisposeIbuf()
        {
            if (ibuf != null)
            {
                for (int j = 0; j < numTiles.Y; ++j)
                {
                    for (int i = 0; i < numTiles.X; ++i)
                    {
                        if (ibuf[i, j] != null)
                        {
                            ibuf[i, j].Dispose();
                        }
                    }
                }
                ibuf = null;
                primCount = null;
            }
        }
        static protected void DisposeBounds()
        {
            tileBounds = null;
        }
        /// <summary>
        /// Dispose rendering resources
        /// </summary>
        static protected void Dispose()
        {
            DisposeVbuf();
            DisposeIbuf();
            DisposeSkirts();
            DisposeBounds();
            heightMap = null;
        }
        /// <summary>
        /// Index buffer generation
        /// </summary>
        static protected void MakeIndices()
        {
            if (ibuf != null)
            {
                DisposeIbuf();
            }
            ibuf = new IndexBuffer[numTiles.X, numTiles.Y];
            primCount = new int[numTiles.X, numTiles.Y];

            List<Vertex> remappedVerts = null;
            ushort[] remapVerts = null;
            if (doRemap)
            {
                remapVerts = new ushort[vertexList.Count];
                remappedVerts = new List<Vertex>(vertexList.Count);
            }

            for (int j = 0; j < numTiles.Y; ++j)
            {
                for (int i = 0; i < numTiles.X; ++i)
                {
                    if (rootList[i, j] != null)
                    {
                        List<ushort> indices = new List<ushort>();
                        rootList[i, j].CollectIndices(indices);

                        if (indices.Count > 0)
                        {
                            if (doRemap)
                            {
                                RemapVerts(vertexList, remappedVerts, indices, remapVerts);
                            }

                            ibuf[i, j] = new IndexBuffer(BokuGame.bokuGame.GraphicsDevice,
                                indices.Count * sizeof(ushort),
                                BufferUsage.WriteOnly, IndexElementSize.SixteenBits);

                            // Is there a way around this superfluous copy? We just
                            // want to go directly from indices into the index buffer.
                            ushort[] idxArray = new ushort[indices.Count];
                            indices.CopyTo(idxArray);

                            ibuf[i, j].SetData<ushort>(idxArray);

                            primCount[i, j] = indices.Count / 3;
                        }
                        else
                        {
                            rootList[i, j] = null;
                        }
                    }
                }
            }
            if (doRemap)
            {
                vertexList = remappedVerts;
            }
        }
        /// <summary>
        /// Skirt vertex buffer generation
        /// </summary>
        /// <param name="skirtVerts"></param>
        static protected void MakeSkirtVBuf(List<SkirtVertex> skirtVerts)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
            numSkirtVerts = skirtVerts.Count;
            Debug.Assert(numSkirtVerts < (1 << 16)); // may need to move up to 32 bit indices.
            skirtVBuf = new VertexBuffer(device, typeof(SkirtVertex), numSkirtVerts, BufferUsage.WriteOnly);

            SkirtVertex[] drawVerts = new SkirtVertex[numSkirtVerts];

            for (int i = 0; i < numSkirtVerts; ++i)
            {
                drawVerts[i].position = skirtVerts[i].position;
                drawVerts[i].normal = skirtVerts[i].normal;
                drawVerts[i].uv = skirtVerts[i].uv;
            }

            // Copy to vertex buffer.
            skirtVBuf.SetData<SkirtVertex>(drawVerts);
        }
        /// <summary>
        /// Skirt index buffer generation
        /// </summary>
        /// <param name="iTile"></param>
        /// <param name="jTile"></param>
        /// <param name="firstVert"></param>
        /// <param name="numVerts"></param>
        static protected void MakeSkirtIndices(int iTile, int jTile, int firstVert, 
            int numVerts, List<SkirtVertex> skirtVerts)
        {
            int numQuads = (numVerts / 2) - 1;
            int numTris = numQuads * 2;
            int numIndices = numTris * 3;
            ushort[] indices = new ushort[numIndices];
            int next = 0;
            for (int i = 0; i < numQuads; ++i)
            {
                if( !SkipVertex(skirtVerts[firstVert].position)
                    && !SkipVertex(skirtVerts[firstVert].position))
                {
                    indices[next++] = (ushort)(firstVert);
                    indices[next++] = (ushort)(firstVert + 1);
                    indices[next++] = (ushort)(firstVert + 2);

                    indices[next++] = (ushort)(firstVert + 2);
                    indices[next++] = (ushort)(firstVert + 1);
                    indices[next++] = (ushort)(firstVert + 3);
                }

                firstVert += 2;
            }
            numIndices = next;
            if (numIndices > 0)
            {
                numTris = numIndices / 3;
                numQuads = numTris / 2;
                SkirtDraw skirtDraw = new SkirtDraw();
                skirtDraw.ibuf = new IndexBuffer(BokuGame.bokuGame.GraphicsDevice,
                    numIndices * sizeof(ushort),
                    BufferUsage.WriteOnly, IndexElementSize.SixteenBits);

                // Is there a way around this superfluous copy? We just
                // want to go directly from indices into the index buffer.
                skirtDraw.ibuf.SetData<ushort>(indices, 0, numIndices);

                skirtDraw.primCount = numTris;

                if (skirtIBuf[iTile, jTile] == null)
                    skirtIBuf[iTile, jTile] = new List<SkirtDraw>();
                skirtIBuf[iTile, jTile].Add(skirtDraw);
            }
        }
        /// <summary>
        /// Dispose of skirt resources
        /// </summary>
        static protected void DisposeSkirts()
        {
            if (skirtVBuf != null)
            {
                skirtVBuf.Dispose();
                skirtVBuf = null;
                numSkirtVerts = 0;
            }
            if (skirtIBuf != null)
            {
                for (int j = 0; j < numTiles.Y; ++j)
                {
                    for (int i = 0; i < numTiles.X; ++i)
                    {
                        if (skirtIBuf[i, j] != null)
                        {
                            foreach (SkirtDraw skirtDraw in skirtIBuf[i, j])
                            {
                                skirtDraw.ibuf.Dispose();
                                skirtDraw.ibuf = null;
                                skirtDraw.primCount = 0;
                            }
                            skirtIBuf[i, j].Clear();
                        }
                    }
                }
            }
            skirtIBuf = null;
        }
        /// <summary>
        /// Collect skirt vertices from a root tile, and generate connectivity
        /// </summary>
        /// <param name="skirtVerts"></param>
        /// <param name="iTile"></param>
        /// <param name="jTile"></param>
        /// <param name="norm"></param>
        /// <param name="dir"></param>
        static protected void CollectSkirt(List<SkirtVertex> skirtVerts, int iTile, int jTile, Vector3 norm, Quad.Card dir)
        {
            int firstVert = skirtVerts.Count;
            rootList[iTile, jTile].CollectSkirt(skirtVerts, norm, dir);
            int numVerts = skirtVerts.Count - firstVert;

            if (numVerts > 2)
            {
                MakeSkirtIndices(iTile, jTile, firstVert, numVerts, skirtVerts);
            }
        }
        /// <summary>
        /// Make the renderable skirts
        /// </summary>
        static protected void MakeSkirts()
        {
            DisposeSkirts();

            List<SkirtVertex> skirtVerts = new List<SkirtVertex>();

            skirtIBuf = new List<SkirtDraw>[numTiles.X, numTiles.Y];

            for (int j = 0; j < numTiles.Y; ++j)
            {
                for (int i = 0; i < numTiles.X; ++i)
                {
                    if (rootList[i, j] != null)
                    {
                        CollectSkirt(skirtVerts, i, j, Vector3.Up, Quad.Card.N);
                        CollectSkirt(skirtVerts, i, j, Vector3.Right, Quad.Card.W);
                        CollectSkirt(skirtVerts, i, j, Vector3.Down, Quad.Card.S);
                        CollectSkirt(skirtVerts, i, j, Vector3.Left, Quad.Card.E);
                    }
                }
            }
            MakeSkirtVBuf(skirtVerts);
        }
        /// <summary>
        /// Theoretically, this should provide a speedup by increasing vertex cache reuse
        /// on the GPU, but vertex processing just isn't currently a bottle neck.
        /// Need to enable it on the 360 to see if it matters there. In no case should
        /// it slow things down, other than the extra time during the terrain build to
        /// do the O(N) remapping.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="verts"></param>
        /// <param name="newVerts"></param>
        /// <param name="indices"></param>
        /// <param name="remap"></param>
        static protected void RemapVerts(List<Vertex> verts, List<Vertex> newVerts, List<ushort> indices, ushort[] remap)
        {
            for (int i = 0; i < indices.Count; ++i)
            {
                if (remap[indices[i]] == 0)
                {
                    remap[indices[i]] = (ushort)(newVerts.Count + 1);
                    newVerts.Add(verts[indices[i]]);
                }
                indices[i] = (ushort)(remap[indices[i]] - 1);
            }
        }
        /// <summary>
        /// Set up bounds for each non-null tile.
        /// </summary>
        static protected void MakeBounds()
        {
            for (int j = 0; j < numTiles.Y; ++j)
            {
                for (int i = 0; i < numTiles.X; ++i)
                {
                    if (rootList[i, j] != null)
                    {
                        tileBounds[i, j] = rootList[i, j].BoundingBox;
                    }
                }
            }
        }
        /// <summary>
        /// Make the renderable geometries
        /// </summary>
        /// <returns></returns>
        static public bool MakeGeometry()
        {
            MakeIndices();

            MakeVerts();

            MakeSkirts();

            MakeBounds();

            return vbuf != null;
        }
        /// <summary>
        ///  Debug tool for setting to wireframe
        /// </summary>
        static private bool RenderWire
        {
            get { return false; }
        }
        /// <summary>
        /// Render the main body of the terrain (but not skirts). Assumes effect parameters already properly set.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="effect"></param>
        static public void Render(Camera camera, Effect effect)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            if (RenderWire)
                device.RenderState.FillMode = FillMode.WireFrame;

            device.Vertices[0].SetSource(VertexBuffer, 0, Stride);

            effect.Begin();

            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Begin();
                // For each tile.
                for (int jTile = 0; jTile < NumTiles.Y; ++jTile)
                {
                    for (int iTile = 0; iTile < NumTiles.X; ++iTile)
                    {
                        if (tileBounds[iTile, jTile] == null)
                        {
                            continue;
                        }

                        // First, cull tile against camera frustum.  Only render if it's not offscreen.
                        Frustum.CullResult cullResult = camera.Frustum.CullTest(BoundingBox(iTile, jTile));

                        if (cullResult != Frustum.CullResult.TotallyOutside)
                        {

                            device.Indices = IndexBuffer(iTile, jTile);

                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                0, // base vertex
                                0, // min vert index (unused on 360)
                                NumQuadVerts,
                                0, // start index, idx in index array to start at
                                PrimCount(iTile, jTile) // numprims
                                );
                        }
                    }
                }
                pass.End();
            }
            effect.End();

            if (RenderWire)
                device.RenderState.FillMode = FillMode.Solid;
        }
        /// <summary>
        /// Render the terrain skirts. Assumes effect parameters already properly set.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="effect"></param>
        static public void RenderSkirts(Camera camera, Effect effect)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            if (RenderWire)
                device.RenderState.FillMode = FillMode.WireFrame;

            device.Vertices[0].SetSource(skirtVBuf, 0, SkirtStride);

            effect.Begin();

            for (int i = 0; i < effect.CurrentTechnique.Passes.Count; i++)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[i];
                pass.Begin();
                // For each tile.
                for (int jTile = 0; jTile < NumTiles.Y; ++jTile)
                {
                    for (int iTile = 0; iTile < NumTiles.X; ++iTile)
                    {
                        List<SkirtDraw> skirtDraws = skirtIBuf[iTile, jTile];
                        if (skirtDraws == null)
                        {
                            continue;
                        }

                        AABB box = BoundingBox(iTile, jTile);
                        box.MinZ = -heightMap.Scale.Z;
                        // First, cull tile against camera frustum.  Only render if it's not offscreen.
                        Frustum.CullResult cullResult = camera.Frustum.CullTest(box);

                        if (cullResult != Frustum.CullResult.TotallyOutside)
                        {

                            foreach (SkirtDraw skirtDraw in skirtDraws)
                            {

                                device.Indices = skirtDraw.ibuf;

                                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                    0, // base vertex
                                    0, // min vert index (unused on 360)
                                    NumSkirtVerts,
                                    0, // start index, idx in index array to start at
                                    skirtDraw.primCount // numprims
                                    );
                            }
                        }
                    }
                }
                pass.End();
            }
            effect.End();

            if (RenderWire)
                device.RenderState.FillMode = FillMode.Solid;
        }

        #endregion Renderable Versions


        /// <summary>
        /// Dispose of old tree and rebuild from scratch
        /// </summary>
        /// <param name="hMap"></param>
        /// <param name="nTiles"></param>
        static public void Init(HeightMap hMap, Point nTiles)
        {
            Dispose();
            ///
            /// TODO - We currently save around a lot of resources that should be
            /// pitched once we've generated the renderable geometry.
            /// So, really, Done() should dispose of everything but the renderable
            /// geometry and be called at the end of Init(), and then Dispose()
            /// should be called here at the beginning instead of Done().
            if (!Enabled)
                return;

            heightMap = hMap;
            invisiHeight = -heightMap.Scale.Z * 255.0f / 256.0f;

            vertexList = new List<Vertex>();

            numTiles = nTiles;
            rootList = new Quad[numTiles.X, numTiles.Y];
            tileBounds = new AABB[numTiles.X, numTiles.Y];
            MakeKurvBase();
            Terrain terrain = InGame.inGame.Terrain;
            // Create our list of quad roots
            for (int j = 0; j < numTiles.Y; ++j)
            {
                for (int i = 0; i < numTiles.X; ++i)
                {
                    // New quads are automatically added for processing
                    if (!Tile.TileHidden(i, j))
                    {
                        rootList[i, j] = NewQuad(Quad.Diag.X);
                    }
                }
            }
            // Fill them in with essential data.
            for (int j = 0; j < numTiles.Y; ++j)
            {
                for (int i = 0; i < numTiles.X; ++i)
                {
                    if (rootList[i, j] != null)
                    {
                        if (i > 0)
                        {
                            rootList[i, j].SetNeighbor(Quad.Card.W, rootList[i - 1, j]);
                        }
                        if (i < numTiles.X - 1)
                        {
                            rootList[i, j].SetNeighbor(Quad.Card.E, rootList[i + 1, j]);
                        }
                        if (j > 0)
                        {
                            rootList[i, j].SetNeighbor(Quad.Card.S, rootList[i, j - 1]);
                        }
                        if (j < numTiles.Y - 1)
                        {
                            rootList[i, j].SetNeighbor(Quad.Card.N, rootList[i, j + 1]);
                        }
                        rootList[i, j].MakeRootVerts(i, j);

                    }
                    // This isn't ideal. We make an entire kurv map just
                    // so we can look up the edge verts from it. Might be
                    // smarter to make the lookup smarter, so a lookup of
                    // a kurvature on the boundary between an existing map
                    // and a missing map will use the existing map.
                    MakeKurvMap(i, j);
                }
            }

            SubDivide();

            MakeGeometry();

            Done();
        }
        /// <summary>
        /// Make the highest resolution (base) version of kurvature map.
        /// </summary>
        static protected void MakeKurvBase()
        {
            float hiK = 0.0f;
            kurvBase = new float[heightMap.Size.X, heightMap.Size.Y];
            for (int j = 0; j < heightMap.Size.Y - 1; ++j)
            {
                for (int i = 0; i <= heightMap.Size.X - 1; ++i)
                {
                    float hxb = heightMap.GetHeight(i - 1, j);
                    float hx0 = heightMap.GetHeight(i, j);
                    float hxf = heightMap.GetHeight(i + 1, j);

                    float kurvX = Math.Abs(
                                        hxb
                                        - 2.0f * hx0
                                        + hxf);

                    float hyb = heightMap.GetHeight(i, j - 1);
                    float hy0 = hx0;
                    float hyf = heightMap.GetHeight(i, j + 1);

                    float kurvY = Math.Abs(
                                        hyb
                                        - 2.0f * hy0
                                        + hyf);

                    kurvBase[i, j] = Math.Max(kurvX, kurvY);
                    double kPower = 0.65f;
                    kurvBase[i, j] = (float)Math.Pow(kurvBase[i, j], kPower);
                    hiK = Math.Max(kurvBase[i, j], hiK);
                }
            }
            if (hiK > 0.0f)
            {
                float norm = 1.0f / hiK;
                for (int j = 0; j < heightMap.Size.Y - 1; ++j)
                {
                    for (int i = 0; i < heightMap.Size.X - 1; ++i)
                    {
                        kurvBase[i, j] *= norm;
                    }
                }
            }
            kurvatures = new List<float[,]>[numTiles.X, numTiles.Y];
        }
        /// <summary>
        /// Make the mip levels for the kurvature map.
        /// The mip levels are based on max, so that we don't miss out on a high
        /// resolution feature surrounded by a flat plane.
        /// </summary>
        /// <param name="iTile"></param>
        /// <param name="jTile"></param>
        static protected void MakeKurvMap(int iTile, int jTile)
        {
            if (kurvatures[iTile, jTile] != null)
                return;

            List<float[,]> kurvs = new List<float[,]>();
            Point size = new Point((heightMap.Size.X - 1) / numTiles.X, (heightMap.Size.Y - 1) / numTiles.Y);
            Debug.Assert(size.X == size.Y); // non square tiles all of a sudden?

            Point cornerIdx = new Point(iTile * size.X, jTile * size.Y);

            Vector2 corner;
            corner.X = iTile * heightMap.Scale.X / numTiles.X;
            corner.Y = jTile * heightMap.Scale.Y / numTiles.Y;

            int sz = size.X + 1;

            float[,] kurv = new float[sz, sz];
            kurvs.Add(kurv);

            for (int j = 0; j < sz; ++j)
            {
                for (int i = 0; i < sz; ++i)
                {
                    kurv[i, j] = kurvBase[cornerIdx.X + i, cornerIdx.Y + j];
                }
            }

            int szPrev = sz;
            sz >>= 1;
            sz |= 1;

            while (sz > 0)
            {
                float[,] kurvPrev = kurvs[kurvs.Count - 1];
                float[,] kurvNext = new float[sz, sz];
                kurvs.Add(kurvNext);

                for (int j = 0; j < sz; ++j)
                {
                    int jPrev = j << 1;
                    for (int i = 0; i < sz; ++i)
                    {
                        kurvNext[i, j] = 0.0f;

                        int iPrev = i << 1;
                        for (int jj = -1; jj <= 1; ++jj)
                        {
                            int jjPrev = jPrev + jj;
                            for (int ii = -1; ii <= 1; ++ii)
                            {
                                int iiPrev = iPrev + ii;
                                if ((iiPrev >= 0) && (iiPrev < szPrev) && (jjPrev >= 0) && (jjPrev < szPrev))
                                {
                                    kurvNext[i, j] = Math.Max(kurvNext[i, j],
                                        kurvPrev[iiPrev, jjPrev]);
                                }
                            }
                        }
                    }
                }

                szPrev = sz;
                sz >>= 1;
                if (sz > 1)
                    sz += 1;
            }
            kurvatures[iTile, jTile] = kurvs;
        }
        /// <summary>
        /// Look up the kurvature for a given point at a given resolution (quadLevel)
        /// quadLevel == 0 is coarsest resolution.
        /// </summary>
        /// <param name="quadLevel"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static public float Kurvature(int quadLevel, float x, float y)
        {
            Vector2 tileScale;
            tileScale.X = numTiles.X / heightMap.Scale.X;
            tileScale.Y = numTiles.Y / heightMap.Scale.Y;
            float xTile = x * tileScale.X;
            float yTile = y * tileScale.Y;
            int iTile = (int)xTile;
            if (iTile >= numTiles.X)
            {
                iTile = numTiles.X - 1;
            }

            int jTile = (int)yTile;
            if (jTile >= numTiles.Y)
            {
                jTile = numTiles.Y - 1;
            }

            List<float[,]> kurvs = kurvatures[iTile, jTile];
            int level = kurvs.Count - quadLevel - 1;
            if (level < 0)
            {
                quadLevel = kurvs.Count;
                level = 0;
            }

            float[,] kurv = kurvs[level];
            int sz = (heightMap.Size.X - 1) / numTiles.X;
            sz >>= level;
            if (sz <= 1)
                return kurv[0, 0];

            sz += 1;

            Vector2 corner = new Vector2(iTile / tileScale.X, jTile / tileScale.Y);
            Vector2 fPos = new Vector2((x - corner.X) * tileScale.X * (sz - 1),
                                    (y - corner.Y) * tileScale.Y * (sz - 1));
            Point iPos = new Point((int)fPos.X, (int)fPos.Y);
            fPos.X -= iPos.X;
            fPos.Y -= iPos.Y;
            if (iPos.X < 0)
            {
                iPos.X = 0;
                fPos.X = 0.0f;
            }
            if (iPos.X >= sz - 1)
            {
                iPos.X = sz - 2;
                fPos.X = 1.0f;
            }
            if (iPos.Y < 0)
            {
                iPos.Y = 0;
                fPos.Y = 0.0f;
            }
            if (iPos.Y >= sz - 1)
            {
                iPos.Y = sz - 2;
                fPos.Y = 1.0f;
            }

            return kurv[iPos.X + 0, iPos.Y + 0] * (1.0f - fPos.X) * (1.0f - fPos.Y)
                + kurv[iPos.X + 1, iPos.Y + 0] * (0.0f + fPos.X) * (1.0f - fPos.Y)
                + kurv[iPos.X + 0, iPos.Y + 1] * (1.0f - fPos.X) * (0.0f + fPos.Y)
                + kurv[iPos.X + 1, iPos.Y + 1] * (0.0f + fPos.X) * (0.0f + fPos.Y);
        }

        /// <summary>
        /// Keep subdividing until there's nothing left to subdivide.
        /// Note that a quad subdividing will append to the procList,
        /// so it will grow as it is processed (up to a point).
        /// </summary>
        static public void SubDivide()
        {
            while (numProcessed < procList.Count)
            {
                procList[numProcessed++].SubDivide();
            }
        }

        /// <summary>
        /// Clear out temp data. See TODO note at top of Init().
        /// </summary>
        static public void Done()
        {
            numQuadVerts = vertexList.Count;
            vertexList = null;
            rootList = null;
            kurvBase = null;
            kurvatures = null;
            procList.Clear();
            numProcessed = 0;
        }

        #region Helpers
        /// <summary>
        /// Generate a new vertex with appropriate index stored.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static public Vertex NewVertex(Vertex v)
        {
            return NewVertex(v.position, v.normal);
        }
        static public Vertex NewVertex(Vector3 p, Vector3 n)
        {
            Vertex ret = new Vertex(p, n, vertexList.Count);
            vertexList.Add(ret);
            return ret;
        }
        static public Quad NewQuad(Quad.Diag dir)
        {
            Quad quad = new Quad(dir);
            procList.Add(quad);
            return quad;
        }
        static public bool SkipVertex(Vector3 pos)
        {
            //return pos.Z < invisiHeight;
            return false;
        }
        #endregion Helpers

        /// <summary>
        /// Quads are nodes in the tree.
        /// </summary>
        public class Quad
        {
            #region Direction enums
            /// <summary>
            /// Actual numeric values of these are arbitrary. No
            /// fancy tricks used like (k+1)%n is the ccw neighbor.
            /// </summary>
            public enum Diag
            {
                NE,
                NW,
                SW,
                SE,
                X
            }
            public enum Card
            {
                N,
                S,
                E,
                W,
                C
            }

            #endregion Direction enums

            #region Hierarchy and vertex data
            Vertex[] diag = new Vertex[4];
            Vertex[] card = new Vertex[5];

            Quad[] neighbor = new Quad[4];
            Quad[] children = new Quad[4];
            Quad parent = null;

            int level = 0;

            AABB boundingBox = new AABB();

            Diag direction = Diag.X; // which child am i, init val X is invalid
            bool haveChildren = false;
            #endregion Hierarchy and vertex data

            #region Accessors
            protected bool HaveChildren
            {
                get { return haveChildren; }
            }
            public AABB BoundingBox
            {
                get { return boundingBox; }
            }
            #endregion Accessors

            /// <summary>
            /// Thin constructor. Most setup done in MakeRootVerts (for root quads)
            /// or SetParent for level > 0.
            /// </summary>
            /// <param name="dir"></param>
            public Quad(Diag dir)
            {
                direction = dir;
            }

            /// <summary>
            /// Initialize a root quad from the heightmap
            /// </summary>
            /// <param name="iTile"></param>
            /// <param name="jTile"></param>
            public void MakeRootVerts(int iTile, int jTile)
            {
                Vector2 scale = new Vector2(
                            heightMap.Scale.X / numTiles.X,
                            heightMap.Scale.Y / numTiles.Y);
                Vector3 pos = new Vector3(
                                iTile * scale.X,
                                jTile * scale.Y,
                                0.0f);

                if (iTile > 0)
                {
                    if (GetNeighbor(Card.W) != null)
                    {
                        SetDiag(Diag.SW, GetNeighbor(Card.W).GetDiag(Diag.SE));
                        SetDiag(Diag.NW, GetNeighbor(Card.W).GetDiag(Diag.NE));
                    }
                }
                if (jTile > 0)
                {
                    if (GetNeighbor(Card.S) != null)
                    {
                        SetDiag(Diag.SW, GetNeighbor(Card.S).GetDiag(Diag.NW));
                        SetDiag(Diag.SE, GetNeighbor(Card.S).GetDiag(Diag.NE));
                    }
                }

                if (GetDiag(Diag.SW) == null)
                {
                    SetDiag(Diag.SW, LookUpVertex(pos));
                }

                if (GetDiag(Diag.NW) == null)
                {
                    SetDiag(Diag.NW, LookUpVertex(new Vector3(pos.X, pos.Y + scale.Y, 0.0f)));
                }

                if (GetDiag(Diag.SE) == null)
                {
                    SetDiag(Diag.SE, LookUpVertex(new Vector3(pos.X + scale.X, pos.Y, 0.0f)));
                }

                if (GetDiag(Diag.NE) == null)
                {
                    SetDiag(Diag.NE, LookUpVertex(new Vector3(pos.X + scale.X, pos.Y + scale.Y, 0.0f)));
                }

                Debug.Assert(GetDiag(Diag.SW) != null);
                Debug.Assert(GetDiag(Diag.NW) != null);
                Debug.Assert(GetDiag(Diag.SE) != null);
                Debug.Assert(GetDiag(Diag.NE) != null);

                SetBoundsFromDiags();
            }
            /// <summary>
            /// Initialize bounds from the corners.
            /// </summary>
            protected void SetBoundsFromDiags()
            {
                boundingBox = new AABB(GetDiag(Diag.SW).position, GetDiag(Diag.NE).position);

                float minZ = Math.Min(GetDiag(Diag.NW).position.Z, GetDiag(Diag.SE).position.Z);
                boundingBox.MinZ = Math.Min(boundingBox.Min.Z, minZ);

                float maxZ = Math.Max(GetDiag(Diag.NW).position.Z, GetDiag(Diag.SE).position.Z);
                boundingBox.MaxZ = Math.Min(boundingBox.Max.Z, maxZ);
            }
            /// <summary>
            /// Update quad bounds from a height (no xy check).
            /// </summary>
            /// <param name="z"></param>
            protected void UpdateBoundsZ(float z)
            {
                boundingBox.MinZ = Math.Min(boundingBox.Min.Z, z);
                boundingBox.MaxZ = Math.Max(boundingBox.Max.Z, z);
            }
            /// <summary>
            /// Update quad bounds height from a child. (no xy check).
            /// </summary>
            /// <param name="child"></param>
            protected void UpdateBoundsZ(Quad child)
            {
                boundingBox.MinZ = Math.Min(boundingBox.Min.Z, child.boundingBox.Min.Z);
                boundingBox.MaxZ = Math.Max(boundingBox.Max.Z, child.boundingBox.Max.Z);
            }
            /// <summary>
            /// If a position matches a sample point on the height map
            /// return a lookup, else return null.
            /// </summary>
            /// <param name="p0"></param>
            /// <param name="p1"></param>
            /// <returns></returns>
            protected Vertex LookUpVertex(Vector3 p0, Vector3 p1)
            {
                Vector3 pC = (p0 + p1) * 0.5f;

                return LookUpVertex(pC);
            }
            /// <summary>
            /// If a position matches a sample point on the height map
            /// return a lookup, else return null.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            protected bool LookUpVertex(double x, double y)
            {
                x = x * (heightMap.Size.X - 1) / heightMap.Scale.X;
                y = y * (heightMap.Size.Y - 1) / heightMap.Scale.Y;
                double xErr = x - Math.Round(x);
                double yErr = y - Math.Round(y);

                double kSmall = 0.0001;
                if ((Math.Abs(xErr) > kSmall) || (Math.Abs(yErr) > kSmall))
                {
                    return false;
                }
                return true;
            }
            /// <summary>
            /// If a position matches a sample point on the height map
            /// return a lookup, else return null.
            /// </summary>
            /// <param name="pC"></param>
            /// <returns></returns>
            protected Vertex LookUpVertex(Vector3 pC)
            {
                double x = pC.X * (heightMap.Size.X - 1) / heightMap.Scale.X;
                double y = pC.Y * (heightMap.Size.Y - 1) / heightMap.Scale.Y;
                double xErr = x - Math.Round(x);
                double yErr = y - Math.Round(y);

                double kSmall = 0.0001;
                if ((Math.Abs(xErr) > kSmall) || (Math.Abs(yErr) > kSmall))
                {
                    return null;
                }
                int idxX = (int)x;
                int idxY = (int)y;


                pC.Z = heightMap.GetHeight(idxX, idxY);
                Vector3 nC = heightMap.GetNormal(idxX, idxY);

                return TerrainTree.NewVertex(pC, nC);
            }

            #region ToCleanUp Vertex Generation
            /// <summary>
            /// But first I want to get this checked in for a backup.
            /// </summary>
            /// <param name="v0"></param>
            /// <param name="v1"></param>
            /// <returns></returns>
            protected Vertex MakeVertexOld(Vertex v0, Vertex v1)
            {
                Vector3 p0 = v0.position;
                Vector3 p1 = v1.position;

                Vertex lu = LookUpVertex(p0, p1);
                if (lu != null)
                {
                    return lu;
                }

                Vector3 n0 = v0.normal;
                Vector3 n1 = v1.normal;

                Vector3 nC = n0 + n1;
                nC = Vector3.Cross(Vector3.Cross(p1 - p0, nC), p1 - p0);
                nC.Normalize();

                float Tension = 0.7f;

                //Vector3 pC = (p0 + p1) * 0.5f
                //    + (p0 - p1).Length() * (n0 - n1).LengthSquared() * PosTension * nC;
                Vector3 p0t = p0 + n1 * Vector3.Dot(p1 - p0, n1) * Tension;
                Vector3 p1t = p1 + n0 * Vector3.Dot(p0 - p1, n0) * Tension;
                Vector3 pC = (p0t + p1t) * 0.5f;

                Vertex vC = TerrainTree.NewVertex(pC, nC);
                return vC;
            }
            protected Vector3 ProjectOnto(Vector3 p, Vector3 n, Vertex plane)
            {
                float t = Vector3.Dot(plane.position - p, plane.normal)
                            / Vector3.Dot(n, plane.normal);
                return p + t * n;
            }
            protected Vertex MakeVertex(Vertex v0, Vertex v1)
            {
                Vector3 p0 = v0.position;
                Vector3 p1 = v1.position;

                Vertex lu = LookUpVertex(p0, p1);
                if (lu != null)
                {
                    return lu;
                }

                Vector3 n0 = v0.normal;
                Vector3 n1 = v1.normal;

                Vector3 pAvg = (p0 + p1) * 0.5f;

                Vector3 nC = n0 + n1;
                nC.Normalize();

                Vector3 proj0 = ProjectOnto(pAvg, nC, v0);
                Vector3 proj1 = ProjectOnto(pAvg, nC, v1);
                Vector3 pProj = (proj0 + proj1) * 0.5f;

                float Tension = 0.5f;

                //Vector3 pC = (p0 + p1) * 0.5f
                //    + (p0 - p1).Length() * (n0 - n1).LengthSquared() * PosTension * nC;
                Vector3 pC = pProj + Tension * (pAvg - pProj);

                Vertex vC = TerrainTree.NewVertex(pC, nC);
                vC.debug = Vector3.Right;
                return vC;
            }
            protected Vertex MakeCenter()
            {
                Vector3 pAvg = (GetDiag(Diag.NW).position
                                    + GetDiag(Diag.SW).position
                                    + GetDiag(Diag.SE).position
                                    + GetDiag(Diag.NE).position) * 0.25f;
                Vertex lu = LookUpVertex(pAvg);
                if (lu != null)
                {
                    return lu;
                }

                Vector3 nC = GetDiag(Diag.NE).normal + GetDiag(Diag.SW).normal
                    + GetDiag(Diag.NW).normal + GetDiag(Diag.SE).normal;
                nC.Normalize();

                Vector3 pNW = ProjectOnto(pAvg, nC, GetDiag(Diag.NW));
                Vector3 pSW = ProjectOnto(pAvg, nC, GetDiag(Diag.SW));
                Vector3 pSE = ProjectOnto(pAvg, nC, GetDiag(Diag.SE));
                Vector3 pNE = ProjectOnto(pAvg, nC, GetDiag(Diag.NE));
                Vector3 pProj = (pNW + pSW + pSE + pNE) * 0.25f;

                float Tension = 1.0f;
                Vector3 pC = pProj + Tension * (pAvg - pProj);

                Vertex vC = TerrainTree.NewVertex(pC, nC);
                vC.debug = Vector3.Backward;

                return vC;
            }
            protected Vertex MakeCardinal(Card which)
            {
                Debug.Assert(card[(int)which] == null);
                switch (which)
                {
                    case Card.N:
                        card[(int)Card.N] = MakeVertex(GetDiag(Diag.NE), GetDiag(Diag.NW));
                        break;
                    case Card.W:
                        card[(int)Card.W] = MakeVertex(GetDiag(Diag.NW), GetDiag(Diag.SW));
                        break;
                    case Card.S:
                        card[(int)Card.S] = MakeVertex(GetDiag(Diag.SW), GetDiag(Diag.SE));
                        break;
                    case Card.E:
                        card[(int)Card.E] = MakeVertex(GetDiag(Diag.SE), GetDiag(Diag.NE));
                        break;
                    case Card.C:
                        card[(int)Card.C] = MakeCenter();
                        break;
                }
                return GetCard(which);
            }
            protected bool HaveExtendedNeighbors(Card dir)
            {
                switch (dir)
                {
                    case Card.N:
                        return (GetNeighbor(Card.N) != null)
                        && (GetNeighbor(Card.W) != null)
                        && (GetNeighbor(Card.E) != null);
                    case Card.W:
                        return (GetNeighbor(Card.W) != null)
                        && (GetNeighbor(Card.S) != null)
                        && (GetNeighbor(Card.N) != null);
                    case Card.S:
                        return (GetNeighbor(Card.S) != null)
                        && (GetNeighbor(Card.E) != null)
                        && (GetNeighbor(Card.W) != null);
                    case Card.E:
                        return (GetNeighbor(Card.E) != null)
                        && (GetNeighbor(Card.N) != null)
                        && (GetNeighbor(Card.S) != null);
                    case Card.C:
                        return true;
                    default:
                        Debug.Assert(false);
                        break;
                }
                return false;
            }
            /// <summary>
            ///         vu0     vu1
            ///         |       |
            ///         |       |
            /// vo0-----vc0-----vc1------vo1
            ///         |       |
            ///         |       |
            ///         vl0     vl1
            /// </summary>
            protected Vertex MakeCardinalExp(Card which)
            {
                float k0 = 0.495f;
                float k1 = 1.0f / 60.0f;
                float k2 = (1.0f - 2.0f * k0 - 4.0f * k1) * 0.5f;
                Vector3 position;
                Vector3 normal;
                switch (which)
                {
                    case Card.N:
                        position = GetDiag(Diag.NW).position
                              + GetDiag(Diag.NE).position;
                        if (HaveExtendedNeighbors(Card.N))
                        {
                            position *= k0;
                            position += GetDiag(Diag.SE).position * k1
                                    + GetDiag(Diag.SW).position * k1
                                    + GetNeighborDiag(Card.N, Diag.NW).position * k1
                                    + GetNeighborDiag(Card.N, Diag.NE).position * k1
                                    + GetNeighbor(Card.E).GetDiag(Diag.NE).position * k2
                                    + GetNeighbor(Card.W).GetDiag(Diag.NW).position * k2;
                        }
                        else
                        {
                            position *= 0.5f;
                        }
                        normal = Vector3.Cross(GetNeighborDiag(Card.N, Diag.NE).position - GetDiag(Diag.SW).position,
                                             GetNeighborDiag(Card.N, Diag.NW).position - GetDiag(Diag.SE).position);
                        break;
                    case Card.W:
                        position = GetDiag(Diag.SW).position
                              + GetDiag(Diag.NW).position;
                        if (HaveExtendedNeighbors(Card.W))
                        {
                            position *= k0;
                            position += GetDiag(Diag.NE).position * k1
                            + GetDiag(Diag.SE).position * k1
                            + GetNeighborDiag(Card.W, Diag.SW).position * k1
                            + GetNeighborDiag(Card.W, Diag.NW).position * k1
                            + GetNeighbor(Card.N).GetDiag(Diag.NW).position * k2
                            + GetNeighbor(Card.S).GetDiag(Diag.SW).position * k2;
                        }
                        else
                        {
                            position *= 0.5f;
                        }
                        normal = Vector3.Cross(GetNeighborDiag(Card.W, Diag.NW).position - GetDiag(Diag.SE).position,
                                               GetNeighborDiag(Card.W, Diag.SW).position - GetDiag(Diag.NE).position);
                        break;
                    case Card.S:
                        position = GetDiag(Diag.SE).position
                              + GetDiag(Diag.SW).position;
                        if (HaveExtendedNeighbors(Card.S))
                        {
                            position *= k0;
                            position += GetDiag(Diag.NW).position * k1
                            + GetDiag(Diag.NE).position * k1
                            + GetNeighborDiag(Card.S, Diag.SE).position * k1
                            + GetNeighborDiag(Card.S, Diag.SW).position * k1
                            + GetNeighbor(Card.W).GetDiag(Diag.SW).position * k2
                            + GetNeighbor(Card.E).GetDiag(Diag.SE).position * k2;
                        }
                        else
                        {
                            position *= 0.5f;
                        }
                        normal = Vector3.Cross(GetNeighborDiag(Card.S, Diag.SW).position - GetDiag(Diag.NE).position,
                                               GetNeighborDiag(Card.S, Diag.SE).position - GetDiag(Diag.NW).position);
                        break;
                    case Card.E:
                        position = GetDiag(Diag.NE).position
                              + GetDiag(Diag.SE).position;
                        if (HaveExtendedNeighbors(Card.E))
                        {
                            position *= k0;
                            position += GetDiag(Diag.SW).position * k1
                            + GetDiag(Diag.NW).position * k1
                            + GetNeighborDiag(Card.E, Diag.NE).position * k1
                            + GetNeighborDiag(Card.E, Diag.SE).position * k1
                            + GetNeighbor(Card.S).GetDiag(Diag.SE).position * k2
                            + GetNeighbor(Card.N).GetDiag(Diag.NE).position * k2;
                        }
                        else
                        {
                            position *= 0.5f;
                        }
                        normal = Vector3.Cross(GetNeighborDiag(Card.E, Diag.SE).position - GetDiag(Diag.NW).position,
                                               GetNeighborDiag(Card.E, Diag.NE).position - GetDiag(Diag.SW).position);
                        break;
                    case Card.C:
                        position = (GetDiag(Diag.NW).position
                                    + GetDiag(Diag.NE).position
                                    + GetDiag(Diag.SE).position
                                    + GetDiag(Diag.SW).position) * 0.25f;
                        normal = Vector3.Cross(GetDiag(Diag.NE).position - GetDiag(Diag.SW).position,
                                               GetDiag(Diag.NW).position - GetDiag(Diag.SE).position);
                        break;
                    default:
                        Debug.Assert(false);
                        return null;
                }
                Vertex vert = LookUpVertex(position);
                if (vert == null)
                {
                    //normal = Vector3.Backward;
                    normal.Normalize();
                    vert = TerrainTree.NewVertex(position, normal);
                }
                card[(int)which] = vert;
                return vert;
            }
            /// 
            /// <summary>
            ///         vu0     vu1
            ///         |       |
            ///         |       |
            ///         vc0--V--vc1
            ///         |       |
            ///         |       |
            ///         vl0     vl1
            /// </summary>
            protected Vertex MakeCardinalCC(Card which)
            {
                Vector3 position;
                Vector3 normal;
                switch (which)
                {
                    case Card.N:
                        position = GetDiag(Diag.NW).position
                              + GetDiag(Diag.NE).position;
                        if (GetNeighbor(Card.N) != null)
                        {
                            position *= 3.0f / 8.0f;
                            position += GetDiag(Diag.SE).position * 1.0f / 16.0f
                                    + GetDiag(Diag.SW).position * 1.0f / 16.0f
                                    + GetNeighborDiag(Card.N, Diag.NW).position * 1.0f / 16.0f
                                    + GetNeighborDiag(Card.N, Diag.NE).position * 1.0f / 16.0f;
                        }
                        else
                        {
                            position *= 0.5f;
                        }
                        normal = Vector3.Cross(GetNeighborDiag(Card.N, Diag.NE).position - GetDiag(Diag.SW).position,
                                             GetNeighborDiag(Card.N, Diag.NW).position - GetDiag(Diag.SE).position);
                        break;
                    case Card.W:
                        position = GetDiag(Diag.SW).position
                              + GetDiag(Diag.NW).position;
                        if (GetNeighbor(Card.W) != null)
                        {
                            position *= 3.0f / 8.0f;
                            position += GetDiag(Diag.NE).position * 1.0f / 16.0f
                            + GetDiag(Diag.SE).position * 1.0f / 16.0f
                            + GetNeighborDiag(Card.W, Diag.SW).position * 1.0f / 16.0f
                            + GetNeighborDiag(Card.W, Diag.NW).position * 1.0f / 16.0f;
                        }
                        else
                        {
                            position *= 0.5f;
                        }
                        normal = Vector3.Cross(GetNeighborDiag(Card.W, Diag.NW).position - GetDiag(Diag.SE).position,
                                               GetNeighborDiag(Card.W, Diag.SW).position - GetDiag(Diag.NE).position);
                        break;
                    case Card.S:
                        position = GetDiag(Diag.SE).position
                              + GetDiag(Diag.SW).position;
                        if (GetNeighbor(Card.S) != null)
                        {
                            position *= 3.0f / 8.0f;
                            position += GetDiag(Diag.NW).position * 1.0f / 16.0f
                              + GetDiag(Diag.NE).position * 1.0f / 16.0f
                              + GetNeighborDiag(Card.S, Diag.SE).position * 1.0f / 16.0f
                              + GetNeighborDiag(Card.S, Diag.SW).position * 1.0f / 16.0f;
                        }
                        else
                        {
                            position *= 0.5f;
                        }
                        normal = Vector3.Cross(GetNeighborDiag(Card.S, Diag.SW).position - GetDiag(Diag.NE).position,
                                               GetNeighborDiag(Card.S, Diag.SE).position - GetDiag(Diag.NW).position);
                        break;
                    case Card.E:
                        position = GetDiag(Diag.NE).position
                              + GetDiag(Diag.SE).position;
                        if (GetNeighbor(Card.E) != null)
                        {
                            position *= 3.0f / 8.0f;
                            position += GetDiag(Diag.SW).position * 1.0f / 16.0f
                              + GetDiag(Diag.NW).position * 1.0f / 16.0f
                              + GetNeighborDiag(Card.E, Diag.NE).position * 1.0f / 16.0f
                              + GetNeighborDiag(Card.E, Diag.SE).position * 1.0f / 16.0f;
                        }
                        else
                        {
                            position *= 0.5f;
                        }
                        normal = Vector3.Cross(GetNeighborDiag(Card.E, Diag.SE).position - GetDiag(Diag.NW).position,
                                               GetNeighborDiag(Card.E, Diag.NE).position - GetDiag(Diag.SW).position);
                        break;
                    case Card.C:
                        position = (GetDiag(Diag.NW).position
                                    + GetDiag(Diag.NE).position
                                    + GetDiag(Diag.SE).position
                                    + GetDiag(Diag.SW).position) * 0.25f;
                        normal = Vector3.Cross(GetDiag(Diag.NE).position - GetDiag(Diag.SW).position,
                                               GetDiag(Diag.NW).position - GetDiag(Diag.SE).position);
                        break;
                    default:
                        Debug.Assert(false);
                        return null;
                }
                Vertex vert = LookUpVertex(position);
                if (vert == null)
                {
                    normal = Vector3.Backward;
                    normal.Normalize();
                    vert = TerrainTree.NewVertex(position, normal);
                }
                card[(int)which] = vert;
                return vert;
            }

            protected Vertex NewDiag(Diag diag)
            {
                //                return TerrainTree.NewVertex(GetDiag(diag));

                Quad diagNeighbor = null;
                float wgt = 1.0f;
                Vector3 centers = GetCard(Card.C).position;
                if ((GetNeighbor(DiagToCard(diag, true)) != null)
                    && (GetNeighbor(DiagToCard(diag, true)).GetCard(Card.C) != null))
                {
                    centers += GetNeighbor(DiagToCard(diag, true)).GetCard(Card.C).position;
                    wgt += 1.0f;

                    diagNeighbor = GetNeighbor(DiagToCard(diag, true)).GetNeighbor(DiagToCard(diag, false));
                }
                if ((GetNeighbor(DiagToCard(diag, false)) != null)
                    && (GetNeighbor(DiagToCard(diag, false)).GetCard(Card.C) != null))
                {
                    centers += GetNeighbor(DiagToCard(diag, false)).GetCard(Card.C).position;
                    wgt += 1.0f;

                    if ((diagNeighbor == null) || (diagNeighbor.GetCard(Card.C) == null))
                    {
                        diagNeighbor = GetNeighbor(DiagToCard(diag, false)).GetNeighbor(DiagToCard(diag, true));
                    }
                }
                if ((diagNeighbor != null) && (diagNeighbor.GetCard(Card.C) != null))
                {
                    centers += diagNeighbor.GetCard(Card.C).position;
                    wgt += 1.0f;
                }
                centers *= 1.0f / wgt;

                //if ((wgt > 3.0f) && (new Vector2(centers.position.X - GetDiag(diag).position.X,
                //                                centers.position.Y - GetDiag(diag).position.Y).Length() > 0.1f))
                //{
                //    centers.normal.Normalize();
                //}

                centers += GetDiag(diag).position;

                centers *= 0.5f;

                Vector3 normal = GetDiag(diag).normal;

                return TerrainTree.NewVertex(centers, normal); ;
            }
            /// <summary>
            /// Unfinished part of Catmull-Clark (approximating) subdivision.
            /// To ensure the verts are still shared, the parent needs to generate
            /// the even verts (corners) at MakeChildren, and share them with the 
            /// appropriate neighbors.
            /// </summary>
            protected void ShareParentVertsCC()
            {
                switch (direction)
                {
                    case Diag.NW:
                        diag[(int)Diag.NW] = parent.NewDiag(Diag.NW);
                        diag[(int)Diag.SW] = parent.card[(int)Card.W];
                        diag[(int)Diag.SE] = parent.card[(int)Card.C];
                        diag[(int)Diag.NE] = parent.card[(int)Card.N];

                        break;
                    case Diag.SW:
                        diag[(int)Diag.NW] = parent.card[(int)Card.W];
                        diag[(int)Diag.SW] = parent.NewDiag(Diag.SW);
                        diag[(int)Diag.SE] = parent.card[(int)Card.S];
                        diag[(int)Diag.NE] = parent.card[(int)Card.C];
                        break;
                    case Diag.SE:
                        diag[(int)Diag.NW] = parent.card[(int)Card.C];
                        diag[(int)Diag.SW] = parent.card[(int)Card.S];
                        diag[(int)Diag.SE] = parent.NewDiag(Diag.SE);
                        diag[(int)Diag.NE] = parent.card[(int)Card.E];
                        break;
                    case Diag.NE:
                        diag[(int)Diag.NW] = parent.card[(int)Card.N];
                        diag[(int)Diag.SW] = parent.card[(int)Card.C];
                        diag[(int)Diag.SE] = parent.card[(int)Card.E];
                        diag[(int)Diag.NE] = parent.NewDiag(Diag.NE);
                        break;
                }
            }

            #endregion ToCleanUp Vertex Generation

            /// <summary>
            /// Debug function to check integrity of a given quad in a given direction.
            /// </summary>
            /// <param name="dir"></param>
            /// <returns></returns>
            protected bool CheckSharing(Card dir)
            {
                Quad n = GetNeighbor(dir);
                Card opp = Opposite(dir);
                if (GetCard(dir) != n.GetCard(opp))
                    return false;

                if (GetDiag(CardToDiag(dir, true)) != n.GetDiag(CardToDiag(opp, false)))
                    return false;

                if (GetDiag(CardToDiag(dir, false)) != n.GetDiag(CardToDiag(opp, true)))
                    return false;

                if (level > 0)
                {
                    if (parent == null)
                    {
                        return false;
                    }

                    if (parent.GetChild(direction) != this)
                    {
                        return false;
                    }

                    Quad p = parent;
                    Quad np = n.parent;
                    while ((p != null) && (p != np))
                    {
                        p = p.parent;
                        np = np.parent;
                    }
                    if ((p == null) != (np == null))
                        return false;
                }

                return true;
            }
            /// <summary>
            /// Debug function to check the integrity of the tree.
            /// </summary>
            protected void CheckSharing()
            {
                //for (int dir = 0; dir < 4; ++dir)
                //{
                //    if (neighbor[dir] != null)
                //    {
                //        CheckSharing((Card)dir);
                //    }
                //}
            }
            /// <summary>
            /// Create the four cardinal children.
            /// </summary>
            public void MakeChildren()
            {
                if (HaveChildren)
                {
                    // Nothing to do, we have them already.
                    return;
                }

                // Make sure we have a center vertex.
                if (GetCard(Card.C) == null)
                {
                    MakeCardinal(Card.C);
                }
                for (int d = 0; d < 4; ++d)
                {
                    Card dir = (Card)d;

                    // If we don't have a neighbor in that direction
                    // make one
                    if ((GetNeighbor(dir) == null) && (parent != null))
                    {
                        Quad neighParent = parent.GetNeighbor(dir);
                        // The neighbor parent will be null at the edge of the world.
                        if (neighParent != null)
                        {
                            neighParent.MakeChildren();
                        }
                    }

                    Card opp = Opposite(dir);

                    // if any of the 4 cardinal direction verts doesn't exist,
                    //  create it.
                    Vertex vtx = GetCard(dir);
                    if (vtx == null)
                    {
                        // We don't have it, check our neighbor
                        if (GetNeighbor(dir) != null)
                        {
                            // This should always also be null, because
                            // if the neighbor had created the matching vert,
                            // it should have already shared it with us.
                            vtx = GetNeighbor(dir).GetCard(opp);
                        }
                        if (vtx == null)
                        {
                            //neither of us had it, make it and share it

                            // The neighbor will need a center vert.
                            if (GetNeighbor(dir) != null)
                            {
                                if (GetNeighbor(dir).GetCard(Card.C) == null)
                                {
                                    GetNeighbor(dir).MakeCardinal(Card.C);
                                }
                            }

                            // Make the shared vert
                            vtx = MakeCardinal(dir);

                            // Share it
                            if (GetNeighbor(dir) != null)
                            {
                                GetNeighbor(dir).SetCard(opp, vtx);
                            }
                        }
                    }
                    else if (GetNeighbor(dir) != null)
                    {
                        // Make sure neighbor has it. This may be redundant,
                        // but doesn't matter.
                        Debug.Assert((GetNeighbor(dir).GetCard(opp) == null)
                                || (GetNeighbor(dir).GetCard(opp) == vtx));
                        if (GetNeighbor(dir).GetCard(opp) == null)
                        {
                            GetNeighbor(dir).SetCard(opp, vtx);
                        }
                    }
                }
                // Get all children set before setting parents,
                // so that they can find each other in SetParent.
                SetChild(Diag.NW, TerrainTree.NewQuad(Diag.NW));
                SetChild(Diag.SW, TerrainTree.NewQuad(Diag.SW));
                SetChild(Diag.SE, TerrainTree.NewQuad(Diag.SE));
                SetChild(Diag.NE, TerrainTree.NewQuad(Diag.NE));

                // Set parent will let them hook up to all essentials,
                // like verts and neighbors.
                GetChild(Diag.NW).SetParent(this);
                GetChild(Diag.SW).SetParent(this);
                GetChild(Diag.SE).SetParent(this);
                GetChild(Diag.NE).SetParent(this);

                CheckSharing();

                haveChildren = true;
            }
            /// <summary>
            /// Decide whether to subdivide, and if yes then do it.
            /// </summary>
            public void SubDivide()
            {
                if (!ShouldDivide())
                    return;

                // If we're subdividing, we need children
                MakeChildren();

                // Let the children decide whether or not to further subdivide.
                // They'll be processed in turn now that they are in procList.
            }
            /// <summary>
            /// Grab pointers to parent verts into appropriate slots
            /// </summary>
            protected void ShareParentVerts()
            {
                switch (direction)
                {
                    case Diag.NW:
                        diag[(int)Diag.NW] = parent.diag[(int)Diag.NW];
                        diag[(int)Diag.SW] = parent.card[(int)Card.W];
                        diag[(int)Diag.SE] = parent.card[(int)Card.C];
                        diag[(int)Diag.NE] = parent.card[(int)Card.N];
                        break;
                    case Diag.SW:
                        diag[(int)Diag.NW] = parent.card[(int)Card.W];
                        diag[(int)Diag.SW] = parent.diag[(int)Diag.SW];
                        diag[(int)Diag.SE] = parent.card[(int)Card.S];
                        diag[(int)Diag.NE] = parent.card[(int)Card.C];
                        break;
                    case Diag.SE:
                        diag[(int)Diag.NW] = parent.card[(int)Card.C];
                        diag[(int)Diag.SW] = parent.card[(int)Card.S];
                        diag[(int)Diag.SE] = parent.diag[(int)Diag.SE];
                        diag[(int)Diag.NE] = parent.card[(int)Card.E];
                        break;
                    case Diag.NE:
                        diag[(int)Diag.NW] = parent.card[(int)Card.N];
                        diag[(int)Diag.SW] = parent.card[(int)Card.C];
                        diag[(int)Diag.SE] = parent.card[(int)Card.E];
                        diag[(int)Diag.NE] = parent.diag[(int)Diag.NE];
                        break;
                }
            }
            /// <summary>
            /// Return the opposite direction from which
            /// </summary>
            /// <param name="which"></param>
            /// <returns></returns>
            public Card Opposite(Card which)
            {
                switch (which)
                {
                    case Card.N:
                        return Card.S;
                    case Card.W:
                        return Card.E;
                    case Card.S:
                        return Card.N;
                    case Card.E:
                        return Card.W;
                }
                Debug.Assert(false);
                return Card.C;
            }
            /// <summary>
            /// Install c as the specified child.
            /// </summary>
            /// <param name="dir"></param>
            /// <param name="c"></param>
            protected void SetChild(Diag dir, Quad c)
            {
                children[(int)dir] = c;
            }
            /// <summary>
            /// Return the requested (possibly null) child.
            /// </summary>
            /// <param name="dir"></param>
            /// <returns></returns>
            protected Quad GetChild(Diag dir)
            {
                return children[(int)dir];
            }
            /// <summary>
            /// Get the specified child from the specified neighbor.
            /// Will be null if neighbor or child is null.
            /// </summary>
            /// <param name="neiDir"></param>
            /// <param name="childDir"></param>
            /// <returns></returns>
            protected Quad GetChildNeighbor(Card neiDir, Diag childDir)
            {
                Quad n = GetNeighbor(neiDir);
                if (n != null)
                {
                    return n.GetChild(childDir);
                }
                return null;
            }
            /// <summary>
            /// Get specified (possibly null) neighbor.
            /// </summary>
            /// <param name="dir"></param>
            /// <returns></returns>
            protected Quad GetNeighbor(Card dir)
            {
                return neighbor[(int)dir];
            }
            /// <summary>
            /// Set the neighbor and make sure neighbor knows about us.
            /// Neighbor can be null.
            /// </summary>
            /// <param name="dir"></param>
            /// <param name="n"></param>
            public void SetNeighbor(Card dir, Quad n)
            {
                if (neighbor[(int)dir] != n)
                {
                    neighbor[(int)dir] = n;
                    if (n != null)
                    {
                        Card opp = Opposite(dir);
                        if (n.GetNeighbor(opp) != this)
                        {
                            n.SetNeighbor(opp, this);
                        }
                    }
                }
            }
            /// <summary>
            /// Get the specified diagonal vertex. Should never be null.
            /// </summary>
            /// <param name="d"></param>
            /// <returns></returns>
            protected Vertex GetDiag(Diag d)
            {
                return diag[(int)d];
            }
            /// <summary>
            /// Set the specified diagonal vertex.
            /// </summary>
            /// <param name="d"></param>
            /// <param name="v"></param>
            protected void SetDiag(Diag d, Vertex v)
            {
                diag[(int)d] = v;
            }
            /// <summary>
            /// Get the specified (possibly null) cardinal vertex.
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            protected Vertex GetCard(Card c)
            {
                return card[(int)c];
            }
            /// <summary>
            /// Set the specified cardinal vertex. Should not be called
            /// redundantly. Will update bounds.
            /// </summary>
            /// <param name="c"></param>
            /// <param name="v"></param>
            protected void SetCard(Card c, Vertex v)
            {
                Debug.Assert(card[(int)c] == null);
                card[(int)c] = v;
                UpdateBoundsZ(v.position.Z);

                // if we don't have a center vertex, go ahead and make one, we'll be needing it.
                if (GetCard(Card.C) == null)
                {
                    MakeCardinal(Card.C);
                    UpdateBoundsZ(GetCard(Card.C).position.Z);
                }
            }
            /// <summary>
            /// Get (possibly null)corner vertex from neighbor.
            /// </summary>
            /// <param name="neighbor"></param>
            /// <param name="diag"></param>
            /// <returns></returns>
            protected Vertex GetNeighborDiag(Card neighbor, Diag diag)
            {
                return GetNeighbor(neighbor) != null
                    ? GetNeighbor(neighbor).GetDiag(diag)
                    : GetDiag(diag);
            }
            /// <summary>
            /// Get (possibly null) side vertex from neighbor
            /// </summary>
            /// <param name="neighbor"></param>
            /// <param name="card"></param>
            /// <returns></returns>
            protected Vertex GetNeighborCard(Card neighbor, Card card)
            {
                return GetNeighbor(neighbor) != null
                    ? GetNeighbor(neighbor).GetCard(card)
                    : GetCard(card);
            }
            /// <summary>
            /// Get (possibly null) center vertex from neighbor
            /// </summary>
            /// <param name="neighbor"></param>
            /// <returns></returns>
            protected Vertex GetNeighborCenter(Card neighbor)
            {
                return GetNeighbor(neighbor) != null
                    ? GetNeighbor(neighbor).GetCard(Card.C)
                    : null;
            }

            /// <summary>
            /// Find neighbors from parent.
            /// </summary>
            protected void GetNeighbors()
            {
                switch (direction)
                {
                    case Diag.NW:
                        SetNeighbor(Card.N, parent.GetChildNeighbor(Card.N, Diag.SW));
                        SetNeighbor(Card.W, parent.GetChildNeighbor(Card.W, Diag.NE));
                        SetNeighbor(Card.S, parent.GetChild(Diag.SW));
                        SetNeighbor(Card.E, parent.GetChild(Diag.NE));
                        break;
                    case Diag.SW:
                        SetNeighbor(Card.S, parent.GetChildNeighbor(Card.S, Diag.NW));
                        SetNeighbor(Card.W, parent.GetChildNeighbor(Card.W, Diag.SE));
                        SetNeighbor(Card.N, parent.GetChild(Diag.NW));
                        SetNeighbor(Card.E, parent.GetChild(Diag.SE));
                        break;
                    case Diag.SE:
                        SetNeighbor(Card.S, parent.GetChildNeighbor(Card.S, Diag.NE));
                        SetNeighbor(Card.E, parent.GetChildNeighbor(Card.E, Diag.SW));
                        SetNeighbor(Card.N, parent.GetChild(Diag.NE));
                        SetNeighbor(Card.W, parent.GetChild(Diag.SW));
                        break;
                    case Diag.NE:
                        SetNeighbor(Card.N, parent.GetChildNeighbor(Card.N, Diag.SE));
                        SetNeighbor(Card.E, parent.GetChildNeighbor(Card.E, Diag.NW));
                        SetNeighbor(Card.S, parent.GetChild(Diag.SE));
                        SetNeighbor(Card.W, parent.GetChild(Diag.NW));
                        break;
                }
            }
            /// <summary>
            /// Set par as parent, gathering verts and neighbors from it.
            /// </summary>
            /// <param name="par"></param>
            protected void SetParent(Quad par)
            {
                parent = par;
                if (parent != null)
                    level = parent.level + 1;

                ShareParentVerts();

                GetNeighbors();
            }
            /// <summary>
            /// Return the cardinal direction counterclockwise from the input direction.
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            protected Card CCWCard(Card c)
            {
                switch (c)
                {
                    case Card.N:
                        return Card.W;
                    case Card.W:
                        return Card.S;
                    case Card.S:
                        return Card.E;
                    case Card.E:
                        return Card.N;
                    default:
                        break;
                }
                Debug.Assert(false); // Center doesn't have a ccw relative direction.
                return Card.C;
            }
            /// <summary>
            /// Return the diagonal direction clockwise or ccw (as specified)
            /// from the cardinal direction.
            /// </summary>
            /// <param name="c"></param>
            /// <param name="cw"></param>
            /// <returns></returns>
            protected Diag CardToDiag(Card c, bool cw)
            {
                switch (c)
                {
                    case Card.N:
                        return cw ? Diag.NE : Diag.NW;
                    case Card.W:
                        return cw ? Diag.NW : Diag.SW;
                    case Card.S:
                        return cw ? Diag.SW : Diag.SE;
                    case Card.E:
                        return cw ? Diag.SE : Diag.NE;
                    default:
                        break;
                }
                Debug.Assert(false); // should never get here.
                return Diag.X; // Invalid return.
            }
            protected Card DiagToCard(Diag d, bool cw)
            {
                switch (d)
                {
                    case Diag.NW:
                        return cw ? Card.N : Card.W;
                    case Diag.SW:
                        return cw ? Card.W : Card.S;
                    case Diag.SE:
                        return cw ? Card.S : Card.E;
                    case Diag.NE:
                        return cw ? Card.E : Card.N;
                    default:
                        break;
                }
                Debug.Assert(false); // should never get here.
                return Card.C; // Invalid return
            }
            /// <summary>
            /// return the index of the specified vertex
            /// </summary>
            /// <param name="d"></param>
            /// <returns></returns>
            protected ushort GetIndex(Diag d)
            {
                return (ushort)GetDiag(d).index;
            }
            /// <summary>
            /// return the index of the specified vertex
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            protected ushort GetIndex(Card c)
            {
                return (ushort)GetCard(c).index;
            }

            /// <summary>
            /// Test for whether this quad should subdivide.
            /// </summary>
            /// <returns></returns>
            public bool ShouldDivide()
            {
                int kMaxQuads = 20000;
                if (TerrainTree.NumQuads > kMaxQuads)
                {
                    return false;
                }
                // kMaxVerts should be something like the most verts
                // we want to allow minus 9, because one more division can
                // force 9 more verts.
                int kMaxVerts = 60000;
                if (TerrainTree.NumQuadVerts >= kMaxVerts)
                {
                    return false;
                }
                //if (TerrainTree.NumQuads < (heightMap.Size.X * heightMap.Size.Y))
                //{
                //    return true;
                //}
                Vector3 pSW = GetDiag(Diag.SW).position;
                Vector3 pNE = GetDiag(Diag.NE).position;
                //Vector2 kurv = Tile.KurvatureLUT(heightMap,
                //    (pSW.X + pNE.X) * 0.5f,
                //    (pSW.Y + pNE.Y) * 0.5f);

                //kurv.X *= pNE.X - pSW.X;
                //kurv.Y *= pNE.Y - pSW.Y;

                //float kurvMax = Math.Max(kurv.X, kurv.Y);

                //if (!LookUpVertex((pSW.X + pNE.X) * 0.5f, (pSW.Y + pNE.Y) * 0.5f))
                //{
                //    return false;
                //}

                float kurvMax = Kurvature(level,
                    (pSW.X + pNE.X) * 0.5f,
                    (pSW.Y + pNE.Y) * 0.5f);

                float kMinKurv = 0.1f; // *(1 << level);
                if (kurvMax <= kMinKurv)
                {
                    return false;
                }

                //// Test hack first version
                //Vector3 p0 = GetDiag(Diag.NW).position;
                //Vector3 p1 = GetDiag(Diag.SE).position;
                //Vector3 pt = p0 + 1.0f / 2.0f * (p1 - p0);
                //return LookUpVertex(pt) != null;
                return true;
            }

            #region Terrain Holes
            bool SkipVertex(Vector3 pos)
            {
                return TerrainTree.SkipVertex(pos);
            }
            bool SkipVertex(Diag diag)
            {
                return SkipVertex(GetDiag(diag).position);
            }
            bool SkipVertex(Card card)
            {
                return SkipVertex(GetCard(card).position);
            }
            #endregion TerrainHoles

            /// <summary>
            /// Collect indices for a simple (4 vert) quad.
            /// </summary>
            /// <param name="indices"></param>
            protected void CollectIndicesSimple(List<ushort> indices)
            {
                UpdateBoundsZ(GetDiag(Diag.NW).position.Z);
                UpdateBoundsZ(GetDiag(Diag.SW).position.Z);
                UpdateBoundsZ(GetDiag(Diag.SE).position.Z);
                UpdateBoundsZ(GetDiag(Diag.NE).position.Z);

                if (!SkipVertex(Diag.NW)
                    || !SkipVertex(Diag.SW)
                    || !SkipVertex(Diag.SE)
                    || !SkipVertex(Diag.NE))
                {
                    // No holes here

                    // Could alternate diagonals here
                    indices.Add(GetIndex(Diag.SW));
                    indices.Add(GetIndex(Diag.NW));
                    indices.Add(GetIndex(Diag.SE));

                    indices.Add(GetIndex(Diag.SE));
                    indices.Add(GetIndex(Diag.NW));
                    indices.Add(GetIndex(Diag.NE));
                }
            }
            /// <summary>
            /// Collect indices from a quad with a center point (>= 5 verts)
            /// </summary>
            /// <param name="indices"></param>
            protected void CollectIndicesFan(List<ushort> indices)
            {
                if (SkipVertex(Diag.NW)
                    && SkipVertex(Diag.SW)
                    && SkipVertex(Diag.SE)
                    && SkipVertex(Diag.NE)
                    && SkipVertex(Card.C))
                {
                    return;
                }

                UpdateBoundsZ(GetCard(Card.C).position.Z);

                // Start northwest, and work our way around CCW

                indices.Add(GetIndex(Diag.NW));
                UpdateBoundsZ(GetDiag(Diag.NW).position.Z);
                if (GetCard(Card.W) != null)
                {
                    UpdateBoundsZ(GetCard(Card.W).position.Z);

                    indices.Add(GetIndex(Card.C));
                    indices.Add(GetIndex(Card.W));
                    indices.Add(GetIndex(Card.W));
                }
                indices.Add(GetIndex(Card.C));
                indices.Add(GetIndex(Diag.SW));

                indices.Add(GetIndex(Diag.SW));
                UpdateBoundsZ(GetDiag(Diag.SW).position.Z);
                if (GetCard(Card.S) != null)
                {
                    UpdateBoundsZ(GetCard(Card.S).position.Z);

                    indices.Add(GetIndex(Card.C));
                    indices.Add(GetIndex(Card.S));
                    indices.Add(GetIndex(Card.S));
                }
                indices.Add(GetIndex(Card.C));
                indices.Add(GetIndex(Diag.SE));

                indices.Add(GetIndex(Diag.SE));
                UpdateBoundsZ(GetDiag(Diag.SE).position.Z);
                if (GetCard(Card.E) != null)
                {
                    UpdateBoundsZ(GetCard(Card.E).position.Z);

                    indices.Add(GetIndex(Card.C));
                    indices.Add(GetIndex(Card.E));
                    indices.Add(GetIndex(Card.E));
                }
                indices.Add(GetIndex(Card.C));
                indices.Add(GetIndex(Diag.NE));

                indices.Add(GetIndex(Diag.NE));
                UpdateBoundsZ(GetDiag(Diag.NE).position.Z);
                if (GetCard(Card.N) != null)
                {
                    UpdateBoundsZ(GetCard(Card.N).position.Z);

                    indices.Add(GetIndex(Card.C));
                    indices.Add(GetIndex(Card.N));
                    indices.Add(GetIndex(Card.N));
                }
                indices.Add(GetIndex(Card.C));
                indices.Add(GetIndex(Diag.NW));
            }
            /// <summary>
            /// Recursively collect indices into the list.
            /// </summary>
            /// <param name="indices"></param>
            public void CollectIndices(List<ushort> indices)
            {
                CheckSharing();
                if (HaveChildren)
                {
                    GetChild(Diag.NW).CollectIndices(indices);
                    GetChild(Diag.SW).CollectIndices(indices);
                    GetChild(Diag.SE).CollectIndices(indices);
                    GetChild(Diag.NE).CollectIndices(indices);

                    UpdateBoundsZ(GetChild(Diag.NW));
                    UpdateBoundsZ(GetChild(Diag.SW));
                    UpdateBoundsZ(GetChild(Diag.SE));
                    UpdateBoundsZ(GetChild(Diag.NE));

                    return;
                }

                if (GetCard(Card.C) == null)
                {
                    CollectIndicesSimple(indices);
                    return;
                }

                CollectIndicesFan(indices);
            }
            /// <summary>
            /// Collect edge verts for a skirt.
            /// </summary>
            /// <param name="skirtVerts"></param>
            /// <param name="src"></param>
            /// <param name="norm"></param>
            protected void CollectSkirtVert(List<SkirtVertex> skirtVerts, Vertex src, Vector3 norm)
            {
                if (src != null)
                {
                    float tile = 0.01f; // from legacy code.
                    Vector3 uvAxis = Vector3.Cross(norm, Vector3.Backward) * tile;
                    SkirtVertex vert = new SkirtVertex();
                    vert.position = src.position;
                    vert.normal = norm;
                    vert.uv.X = Vector3.Dot(vert.position, uvAxis);
                    vert.uv.Y = 0.0f;

                    skirtVerts.Add(vert);

                    vert.uv.Y = (vert.position.Z / heightMap.Scale.Z + 3.0f) * 0.25f; // from legacy code
                    vert.position.Z = -heightMap.Scale.Z;

                    skirtVerts.Add(vert);
                }
            }
            /// <summary>
            /// recursively collect edge verts for a terrain skirt.
            /// </summary>
            /// <param name="skirtVerts"></param>
            /// <param name="norm"></param>
            /// <param name="dir"></param>
            public void CollectSkirt(List<SkirtVertex> skirtVerts, Vector3 norm, Card dir)
            {
                if (GetNeighbor(dir) == null)
                {
                    // If we're the root, prime the pump with the first set of verts.
                    // Children don't need to do this, because the vert on the CCW side
                    // was either the CW of the previous vert or the primed vert.
                    if (level == 0)
                    {
                        CollectSkirtVert(skirtVerts, GetDiag(CardToDiag(dir, false)), norm);
                    }
                    if (HaveChildren)
                    {
                        GetChild(CardToDiag(dir, false)).CollectSkirt(skirtVerts, norm, dir);
                        GetChild(CardToDiag(dir, true)).CollectSkirt(skirtVerts, norm, dir);
                    }
                    else
                    {
                        CollectSkirtVert(skirtVerts, GetCard(dir), norm);
                        CollectSkirtVert(skirtVerts, GetDiag(CardToDiag(dir, true)), norm);
                    }
                }
            }
        }

    }
}
