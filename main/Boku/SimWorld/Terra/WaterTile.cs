// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


#region USING
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;
#endregion USING

namespace Boku.SimWorld.Terra
{
    public partial class VirtualMap
    {
        /// <summary>
        /// A renderable vertex.
        /// </summary>
        public struct WaterVertex : IVertexType
        {
            private Vector3 center;
            private Color neighbors;
            private Vector2 uv;

            static VertexDeclaration decl = null;
            static VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                // Total == 24 bytes
            };

            public WaterVertex(Vector3 center, Vector2 uv, Color neighbors)
            {
                this.center = center;
                this.uv = uv;
                this.neighbors = neighbors;
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
        private class WaterTile
        {
            #region CHILD_CLASSES
            /// <summary>
            /// The Renderable corresponds to the part of a specific water body
            /// overlapping this tile.
            /// </summary>
            public class Renderable
            {
                #region MEMBERS
                private byte label = (byte)Water.InvalidLabel;

                private bool dirty = true;

                private bool culled = true;

                private static List<CoreVertex> coreVerts = new List<CoreVertex>();
                private static WaterVertex[] localVerts = null;

                private VertexBuffer vbuf = null;

                private int numTris = 0;
                private int numVerts = 0;

                private AABB box = new AABB();

                #endregion MEMBERS

                #region ACCESSORS
                /// <summary>
                /// Which water body are we a part of.
                /// </summary>
                public byte Label
                {
                    get { return label; }
                }

                /// <summary>
                /// Do we need rebuilding?
                /// </summary>
                public bool Dirty
                {
                    get { return dirty; }
                    set { dirty = value; }
                }

                /// <summary>
                ///  The number of triangles.
                /// </summary>
                public int NumTris
                {
                    get { return numTris; }
                    private set { numTris = value; }
                }

                /// <summary>
                /// The number of vertices.
                /// </summary>
                public int NumVerts
                {
                    get { return numVerts; }
                    private set { numVerts = value; }
                }

                /// <summary>
                /// Bounds for this collection of renderables
                /// </summary>
                public AABB Bounds
                {
                    get { return box; }
                    private set { box = value; }
                }

                /// <summary>
                /// Cached result of cull test for this water tile, including all renderables.
                /// </summary>
                /// <param name="label"></param>
                public bool Culled
                {
                    get { return culled; }
                    private set { culled = value; }
                }
                #endregion ACCESSORS

                #region PUBLIC
                public Renderable(byte label)
                {
                    this.label = label;
                }

                /// <summary>
                /// Test for visibility, return true if VISIBLE and cache result.
                /// </summary>
                /// <param name="camera"></param>
                /// <returns></returns>
                public bool CullTest(Camera camera)
                {
                    Water water = Water.FromLabel(Label);
                    if (water != null)
                    {
                        Bounds.MaxZ = water.BaseHeight;
                        Frustum.CullResult cull = camera.Frustum.CullTest(Bounds);
                        Culled = (cull == Frustum.CullResult.TotallyOutside);

                        return true;
                    }

                    return false;
                }

                /// <summary>
                /// Render this section of a water body.
                /// </summary>
                /// <param name="device"></param>
                public void Render(GraphicsDevice device)
                {
                    Debug.Assert(!Culled);
                    if (NumVerts > 0)
                    {
                        device.SetVertexBuffer(vbuf);

                        device.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            0, // base vertex
                            0, // min vertex index
                            NumVerts, // number of vertices
                            0, // start index
                            NumTris); // prim count
                    }
                }

                /// <summary>
                /// Release unmanaged resources for this renderable.
                /// </summary>
                public void Dispose()
                {
                    Terrain.TotalCost -= NumVerts * Terrain.CostPerVertex;
                    BokuGame.Release(ref vbuf);
                    NumVerts = 0;
                }

                /// <summary>
                /// Prepare to start receiving vertices.
                /// </summary>
                public void BeginBuild()
                {
                    Bounds.Set(Vector3.Zero, Vector3.Zero);

                    Terrain.TotalCost -= NumVerts * Terrain.CostPerVertex;
                    NumTris = 0;
                    NumVerts = 0;
                    coreVerts.Clear();
                }
                /// <summary>
                /// Add a sample point as part of this water body at this tile.
                /// </summary>
                /// <param name="center"></param>
                /// <param name="h"></param>
                /// <param name="neighbors"></param>
                public void AddCoreVertex(Vector2 center, float h, Color neighbors)
                {
                    if (Bounds.Max.Z <= 0.0f)
                    {
                        /// Empty bounds, just take the new point as a primer.
                        Bounds.Min = Bounds.Max = new Vector3(center, h);
                        Bounds.MinZ = 0.0f;
                    }
                    else
                    {
                        Bounds.Union(new Vector3(center, h));
                    }
                    coreVerts.Add(new CoreVertex(center, h, neighbors));
                }
                /// <summary>
                /// All sample points are added, convert them to something that can be rendered.
                /// Return whether there is anything to be rendered.
                /// </summary>
                /// <returns></returns>
                public bool EndBuild(float halfSize)
                {
                    /// If water is null here, it means there was no water here and the renderable
                    /// is about to quietly die. If there is water here, the renderable needs to know
                    /// the base hight for setting up its bounds.
                    Water water = Water.FromLabel(Label);
                    if (water != null)
                    {
                        float waterLevel = water.BaseHeight;
                        if ((vbuf != null) && (vbuf.VertexCount < coreVerts.Count * 4))
                        {
                            BokuGame.Release(ref vbuf);
                        }
                        if (coreVerts.Count > 0)
                        {
                            if ((localVerts == null) || (localVerts.Length < coreVerts.Count * 4))
                            {
                                localVerts = new WaterVertex[coreVerts.Count * 4];
                            }

                            foreach (CoreVertex cv in coreVerts)
                            {
                                AddVertex(cv.Center, cv.Neighbors);
                            }
                            Debug.Assert(NumVerts == coreVerts.Count * 4);

                            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                            if (vbuf == null)
                            {
                                vbuf = new VertexBuffer(device, typeof(WaterVertex), NumVerts, BufferUsage.WriteOnly);
                            }

                            vbuf.SetData<WaterVertex>(localVerts, 0, NumVerts);

                            NumTris = NumVerts / 4 * 2;

                            Bounds.Min += new Vector3(-halfSize, -halfSize, 0.0f);
                            Bounds.Max += new Vector3(halfSize, halfSize, waterLevel);

                            Terrain.TotalCost += NumVerts * Terrain.CostPerVertex;
                        }
                    }
                    else
                    {
                        Debug.Assert(NumTris == 0, "How did we get water geometry if there's no water?");
                    }

                    return NumTris > 0;
                }

                /// <summary>
                /// Load content.
                /// </summary>
                public static void LoadContent(bool immediate)
                {
                }

                /// <summary>
                /// Init device dependent stuff.
                /// </summary>
                /// <param name="mgr"></param>
                public static void InitDeviceResources(GraphicsDevice device)
                {
                }

                /// <summary>
                /// Unload graphics device dependent static stuff.
                /// </summary>
                public static void UnloadContent()
                {
                }

                /// <summary>
                /// Recreate render targets.
                /// </summary>
                public static void DeviceReset(GraphicsDevice device)
                {
                }

                #endregion PUBLIC

                #region INTERNAL
                /// <summary>
                /// Add the 4 vertices corresponding to a sample point.
                /// </summary>
                /// <param name="center"></param>
                /// <param name="neighbors"></param>
                private void AddVertex(Vector3 center, Color neighbors)
                {
                    localVerts[numVerts++] = new WaterVertex(center, new Vector2(-1.0f, -1.0f), neighbors);
                    localVerts[numVerts++] = new WaterVertex(center, new Vector2(1.0f, -1.0f), neighbors);
                    localVerts[numVerts++] = new WaterVertex(center, new Vector2(1.0f, 1.0f), neighbors);
                    localVerts[numVerts++] = new WaterVertex(center, new Vector2(-1.0f, 1.0f), neighbors);
                }
                #endregion INTERNAL
            }

            /// <summary>
            /// Info for a single sample point in the heightmap field.
            /// </summary>
            public struct CoreVertex
            {
                private Vector3 center;
                private Color neighbors;

                public CoreVertex(Vector2 center, float h, Color neighbors)
                {
                    const float Epsilon = 0.01f;
                    this.center = new Vector3(center.X, center.Y, h + Epsilon);
                    this.neighbors = neighbors;
                }
                public Vector3 Center
                {
                    get { return center; }
                }
                public Color Neighbors
                {
                    get { return neighbors; }
                }
            }

            #endregion CHILD_CLASSES

            #region MEMBERS
            // renderables may be empty
            private Dictionary<byte, Renderable> renderables = new Dictionary<byte, Renderable>();

            private Vector3 min;
            private Vector3 max;

            private AABB box = new AABB();

            private bool culled = true;

            private bool dirty = false;

            private bool queuedForBuild = false;

            private static IndexBuffer ibuff = null;

            #endregion MEMBERS

            #region ACCESSORS
            /// <summary>
            /// Min corner of this tile's place in the world grid.
            /// </summary>
            public Vector3 Min
            {
                get { return min; }
                private set { min = value; }
            }
            /// <summary>
            /// Max corner of this tile's place in the world grid.
            /// </summary>
            public Vector3 Max
            {
                get { return max; }
                private set { max = value; }
            }
            /// <summary>
            /// Center corner of this tile's place in the world grid.
            /// </summary>
            public Vector2 Center
            {
                get { return new Vector2((Min.X + Max.X) * 0.5f, (Min.Y + Max.Y) * 0.5f); }
            }
            /// <summary>
            /// Bounds of this water collection.
            /// </summary>
            public AABB Bounds
            {
                get { return box; }
                private set { box = value; }
            }
            /// <summary>
            /// Are we waiting to build our renderable geometry?
            /// </summary>
            public bool QueuedForBuild
            {
                get { return queuedForBuild; }
                set { queuedForBuild = value; }
            }
            /// <summary>
            /// Have we no renderable data?
            /// </summary>
            public bool Empty
            {
                get { return renderables.Count == 0; }
            }
            /// <summary>
            /// Cached result of last cull test.
            /// </summary>
            public bool Culled
            {
                get { return culled; }
                private set { culled = value; }
            }

            #region INTERNAL_ACCESSORS
            private bool Dirty
            {
                get { return dirty; }
            }

            private Dictionary<byte, Renderable> Renderables
            {
                get { return renderables; }
            }

            #endregion INTERNAL_ACCESSORS

            #endregion ACCESSORS

            #region PUBLIC
            /// <summary>
            /// Constructor, the corners of this tile will never change.
            /// </summary>
            /// <param name="seCorner"></param>
            /// <param name="neCorner"></param>
            public WaterTile(Vector2 seCorner, Vector2 neCorner)
            {
                Min = new Vector3(seCorner.X, seCorner.Y, 0.0f);
                Max = new Vector3(neCorner.X, neCorner.Y, 0.0f);
            }

            /// <summary>
            /// Draw this chunk of water.
            /// </summary>
            /// <param name="device"></param>
            /// <param name="label"></param>
            public void Render(GraphicsDevice device, int label)
            {
                Debug.Assert(!Culled);
                Renderable renderable = GetRenderable(label);
                if ((renderable != null) && !renderable.Culled)
                {
                    device.Indices = ibuff;

                    renderable.Render(device);
                }
            }

            /// <summary>
            /// Test for visibility, return true if VISIBLE and cache result.
            /// </summary>
            /// <param name="camera"></param>
            /// <returns></returns>
            public bool CullTest(Camera camera)
            {
                Culled = true;
                if (renderables.Count > 0)
                {
                    Frustum.CullResult cull = camera.Frustum.CullTest(Bounds);
                    if (cull != Frustum.CullResult.TotallyOutside)
                    {
                        Culled = false;

                        Debug.Assert(_disposes.Count == 0);
                        Dictionary<byte, Renderable>.ValueCollection values = renderables.Values;
                        foreach (Renderable renderable in values)
                        {
                            if (!renderable.CullTest(camera))
                            {
                                _disposes.Add(renderable.Label);
                            }
                        }

                        foreach (byte label in _disposes)
                        {
                            Dispose(label);
                        }
                        _disposes.Clear();
                    }
                }
                return !Culled;
            }

            /// <summary>
            /// Mark the renderable corresponding to label as needing rebuild.
            /// </summary>
            /// <param name="label"></param>
            public void SetDirty(int label)
            {
                Renderable renderable = GetRenderable(label);
                if (renderable != null)
                {
                    renderable.Dirty = true;
                }
                else
                {
                    AddRenderable(label);
                }
                dirty = true;
            }

            /// <summary>
            /// Lose all renderable resources for this label.
            /// </summary>
            public void Dispose(int label)
            {
                Renderable renderable = GetRenderable(label);
                if (renderable != null)
                {
                    renderable.Dispose();
                    RemoveRenderable(label);
                }
            }

            /// <summary>
            /// Discard device dependent resources.
            /// </summary>
            public void Dispose()
            {
                Dictionary<byte, Renderable>.ValueCollection values = renderables.Values;
                foreach (Renderable renderable in values)
                {
                    renderable.Dispose();
                }
            }

            /// <summary>
            /// Rebuild all renderable geometry for this tile.
            /// </summary>
            /// <param name="virtualMap"></param>
            public void Refresh(VirtualMap virtualMap)
            {
                Debug.Assert(_disposes.Count == 0);

                Dictionary<byte, Renderable>.ValueCollection values = renderables.Values;
                bool boundsFirst = true;
                foreach (Renderable renderable in values)
                {
                    if (renderable.Dirty)
                    {
                        renderable.Dirty = false;
                        if (!virtualMap.RebuildWater(this, renderable))
                        {
                            _disposes.Add(renderable.Label);
                        }
                    }
                    if (boundsFirst)
                    {
                        Bounds = new AABB(renderable.Bounds);
                        boundsFirst = false;
                    }
                    else
                    {
                        Bounds.Union(renderable.Bounds);
                    }
                }
                Bounds.MaxZ = float.MaxValue;
                foreach(byte label in _disposes)
                {
                    Dispose(label);
                }
                _disposes.Clear();
                dirty = false;
            }
            private static List<byte> _disposes = new List<byte>();

            /// <summary>
            /// Load graphics device dependent resources.
            /// </summary>
            /// <param name="graphics"></param>
            public static void LoadContent(bool immediate)
            {
                Renderable.LoadContent(immediate);
            }

            public static void InitDeviceResources(GraphicsDevice device)
            {
                Renderable.InitDeviceResources(device);
                CheckIndices();
            }

            /// <summary>
            /// Relinquish device dependent resources.
            /// </summary>
            public static void UnloadContent()
            {
                Renderable.UnloadContent();

                BokuGame.Release(ref ibuff);
            }

            /// <summary>
            /// Recreate render targets.
            /// </summary>
            public static void DeviceReset(GraphicsDevice device)
            {
                Renderable.DeviceReset(device);
            }

            #endregion PUBLIC

            #region INTERNAL

            /// <summary>
            /// Build index buffer if needed. Note that all renderables share common index buffer.
            /// </summary>
            private static void CheckIndices()
            {
                if (ibuff == null)
                {
                    int numQuads = VirtualMap.PixPerMap * VirtualMap.PixPerMap;
                    int numTris = numQuads * 2;
                    int numIndices = numTris * 3;
                    GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                    ibuff = new IndexBuffer(device, typeof(UInt16), numIndices, BufferUsage.WriteOnly);

                    UInt16[] localIdx = new UInt16[numIndices];
                    int idx = 0;
                    int iVert = 0;

                    for (int i = 0; i < numQuads; ++i)
                    {
                        localIdx[idx++] = (UInt16)(iVert + 0);
                        localIdx[idx++] = (UInt16)(iVert + 1);
                        localIdx[idx++] = (UInt16)(iVert + 3);

                        localIdx[idx++] = (UInt16)(iVert + 3);
                        localIdx[idx++] = (UInt16)(iVert + 1);
                        localIdx[idx++] = (UInt16)(iVert + 2);

                        iVert += 4;
                    }
                    ibuff.SetData<UInt16>(localIdx);
                }
            }

            #region BOOKKEEPING
            private void AddRenderable(byte label)
            {
                Renderable renderable = new Renderable(label);
                renderables.Add(label, renderable);
            }
            private void AddRenderable(int label)
            {
                AddRenderable((byte)label);
            }
            private Renderable GetRenderable(byte label)
            {
                return renderables.ContainsKey(label)
                    ? renderables[label]
                    : null;
            }
            private Renderable GetRenderable(int label)
            {
                return GetRenderable((byte)label);
            }
            public void RemoveRenderable(byte label)
            {
                if (renderables.ContainsKey(label))
                    renderables.Remove(label);
            }
            public void RemoveRenderable(int label)
            {
                RemoveRenderable((byte)label);
            }
            #endregion BOOKKEEPING

            #endregion INTERNAL
        }
    }
}
