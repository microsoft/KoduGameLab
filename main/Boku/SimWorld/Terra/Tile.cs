
#if DEBUG
#define Debug_CountTerrainVerts
#define Debug_DrawNormalsWithF8
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Input;

using Boku.Common;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Input;

namespace Boku.SimWorld.Terra
{
    public partial class Tile
    {
        /// <summary>
        /// The different angles we draw the terrain cubes from, notably missing Bottom.
        /// </summary>
        public enum Face
        {
            Top, // facing positive Z
            Front, // facing negative Y
            Back, // facing positive Y
            Left, // facing negative X
            Right // facing positive X
        };

        #region CONSTANTS

        /// <summary>
        /// Convenience for iteration.
        /// </summary>
        public const int NumFaces = 5;
        /// <summary>
        /// Array of Normal directions for each face direction.
        /// </summary>
        public static Vector3[] FaceNormals = new Vector3[] { 
            Vector3.UnitZ,
            -Vector3.UnitY,
            Vector3.UnitY,
            -Vector3.UnitX,
            Vector3.UnitX 
        };
        private const int maxNumRenderables = TerrainMaterial.MaxNum * 2; //Double the max number of terrain materials because they can come with or without the TerrainMaterial.Flags.Selection flag
       
        #endregion

        #region FewerDraws render method state

        private static IndexBuffer indexBuffer_FD;

        /// <summary>
        /// Return the index buffer.
        /// </summary>
        public static IndexBuffer IndexBuffer_FD()
        {
            return indexBuffer_FD;
        }

        #endregion

        #region Members

        private static bool indexBuffersInitialized = false;

        private Vector2 min;
        private Vector2 max;

        private AABB box = new AABB();

        private Dictionary<ushort, Renderable> renderableDict = new Dictionary<ushort, Renderable>(TerrainMaterial.MaxNum);

        private bool queued = false;

        private bool culled = false;

        #endregion Members

        #region Accessors

        /// <summary>
        /// Min corner this tile can potentially cover.
        /// </summary>
        public Vector2 Min
        {
            get { return min; }
            set { min = value; }
        }
        /// <summary>
        /// Max corner this tile can potentially cover.
        /// </summary>
        public Vector2 Max
        {
            get { return max; }
            set { max = value; }
        }
        /// <summary>
        /// Bounds of actual renderable geometry.
        /// </summary>
        public AABB Bounds
        {
            get { return box; }
            private set { box = value; }
        }
        /// <summary>
        /// An assured renderable for each possible material.
        /// Will create if it doesn't exist.
        /// </summary>
        /// <param name="matIdx"></param>
        /// <returns></returns>
        private Renderable GetOrMakeRenderable(ushort matIdx)
        {
            Debug.Assert(TerrainMaterial.IsValid(matIdx, false, true)); // There is no reason to fetch a renderable
                                                                        // for an empty or invalid material.

            if (!renderableDict.ContainsKey(matIdx))
            {
                renderableDict[matIdx] = new Renderable(matIdx);
            }
            return renderableDict[matIdx];
        }

        /// <summary>
        /// A (possibly null) renderable for each possible material.
        /// </summary>
        /// <param name="matIdx"></param>
        /// <returns></returns>
        private Renderable GetRenderable(ushort matIdx)
        {
            Renderable r;

            if (renderableDict.TryGetValue(matIdx, out r))
                return r;
            else
                return null;
        }

        /// <summary>
        /// We're about to rebuild all the Renderables so
        /// we need to clear all their vertex counts.
        /// </summary>
        private void ResetAllVertexCounts()
        {
            foreach (KeyValuePair<ushort, Renderable> kv in renderableDict)
            {
                kv.Value.ClearLocalVertexCounts();
            }

            Renderable.ResetStaticVertexCounts();
        }

        /// <summary>
        /// Whether this tile is queued up for update.
        /// </summary>
        public bool Queued
        {
            get { return queued; }
            set { queued = value; }
        }
        public bool Culled
        {
            get { return culled; }
            set { culled = value; }
        }
        
        #endregion Accessors

        #region Public

        /// <summary>
        /// Constructor, takes world location for initialization, which never changes for this tile.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public Tile(Vector2 min, Vector2 max)
        {
            this.Min = min;
            this.Max = max;
        }

        /// <summary>
        /// Add ourself to the user count of the materials we use.
        /// </summary>
        public void UpdateMaterialUsage()
        {
            var keys = renderableDict.Keys;
            foreach (ushort matIdx in keys)
            {
                if (!TerrainMaterial.Users.ContainsKey(matIdx))
                    TerrainMaterial.Users[matIdx] = 0;

                TerrainMaterial.Users[matIdx]++;
            }
        }


        /// <summary>
        /// Render the geometry for this tile which uses the input material
        /// and facing the requested direction. Might not have any of said material.
        /// </summary>
        public void Render_FD(GraphicsDevice device, Camera camera, ushort matIdx, bool doSides)
        {
            Debug.Assert(Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws);

            Renderable r = GetRenderable(matIdx);
            if (r != null)
            {
                /// We should cull test on the tile bounds as well, and not even
                /// get here if the whole tile is culled.
                if (!r.Culled && r.AnythingToRender)
                {
                    int numTris = 0;
                    int numVerts = 0;

                    if (doSides)
                    {
                        device.SetVertexBuffer(r.VertexBufferSides_FD);
                        numVerts = r.VertNumSides_FD;
                    }
                    else //doTop
                    {
                        device.SetVertexBuffer(r.VertexBufferTop_FD);
                        numVerts = r.VertNumTop_FD;
                    }

                    numTris = (numVerts / 4) * 2;

                    try
                    {
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVerts, 0, numTris);
                    }
                    catch(Exception)
                    {
                    }

#if Debug_CountTerrainVerts
                    Terrain.VertCounter_Debug += numVerts;
                    Terrain.TriCounter_Debug += numTris;
#endif
                }
            }
        }

        /// <summary>
        /// Render the geometry for this tile which uses the input material
        /// and facing the requested direction. Might not have any of said material.
        /// </summary>
        public void Render_FA(GraphicsDevice device, Camera camera, ushort matIdx)
        {
            Renderable r = GetRenderable(matIdx);
            if (r != null)
            {
                /// We should cull test on the tile bounds as well, and not even
                /// get here if the whole tile is culled.
                if (!r.Culled && r.AnythingToRender)
                {
                    device.Indices = r.IndexBuffer_FA;
                    device.SetVertexBuffer(r.VertexBuffer_FA);

                    var numVerts = r.VertNum_FA;
                    var numTris = r.IndexNum_FA / 3;

                    #region Debug_DrawNormalsWithF8: Draw normals
#if Debug_DrawNormalsWithF8
                    if (LowLevelKeyboardInput.IsPressed(Keys.F8))
                    {
                        device.Indices = normalsIBuff;
                        device.SetVertexBuffer(r.normalsVBuff);
                        device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, numVerts * 2, 0, numVerts);
                    }
                    else
#endif
                    #endregion
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVerts, 0, numTris);

#if Debug_CountTerrainVerts
                    Terrain.VertCounter_Debug += numVerts;
                    Terrain.TriCounter_Debug += numTris;
#endif
                }
            }
        }

        public void CullCheck(Camera camera)
        {
            if (Bounds == null)
            {
                Culled = true;
            }
            else
            {
                Frustum.CullResult cull = camera.Frustum.CullTest(Bounds);
                Culled = cull == Frustum.CullResult.TotallyOutside;
                if (!Culled)
                {
                    Dictionary<ushort, Renderable>.ValueCollection values = renderableDict.Values;
                    if (cull == Frustum.CullResult.TotallyInside)
                    {
                        foreach (Renderable r in values)
                        {
                            r.Culled = false;
                        }
                    }
                    else
                    {
                        foreach (Renderable r in values)
                        {
                            cull = camera.Frustum.CullTest(r.Bounds);
                            r.Culled = cull == Frustum.CullResult.TotallyOutside;
                        }
                    }
                }
            }
            //Culled = false; // Debug hack to disable all culling, to separate fill from drawprim overhead
        }

        /// <summary>
        /// Build up renderable geometry from input height neighborhood and material map.
        /// </summary>
        /// <param name="neighbors"></param>
        /// <param name="colorMap"></param>
        /// <returns></returns>
        public bool MakeGeometry(VirtualMap.HeightMapNeighbors heightNeighbors, VirtualMap.ColorMapNeighbors colorNeighbors)
        {
            MakeMinLut(heightNeighbors);

            bool notEmpty = MakeRenderables(heightNeighbors, colorNeighbors);

            CheckIndices(heightNeighbors[(int)VirtualMap.HeightMapNeighbors.Dir.Center].Size);

            return notEmpty;
        }

        /// <summary>
        /// Select or deselect renderable with given material index.
        /// </summary>
        /// <param name="matIdx"></param>
        /// <param name="value"></param>
        public void Select(ushort matIdx, bool value)
        {
            Renderable r = GetRenderable(matIdx);
            if (r != null)
            {
                r.Selected = value;
            }
        }

        public bool Selected(ushort matIdx)
        {
            Renderable r = GetRenderable(matIdx);
            if (r != null)
            {
                return r.Selected;
            }
            return false;
        }

        /// <summary>
        /// Discard device resources.
        /// </summary>
        public void Dispose()
        {
            foreach (Renderable r in renderableDict.Values)
            {
                r.Dispose();
            }

            renderableDict.Clear();
        }

        #endregion Public

        #region INTERNAL

        /// <summary>
        /// Build up renderable geometry for each possible material. Discard empties.
        /// </summary>
        /// <param name="neighbors"></param>
        /// <param name="colorMap"></param>
        /// <returns></returns>
        private bool MakeRenderables(
            VirtualMap.HeightMapNeighbors heightNeighbors,
            VirtualMap.ColorMapNeighbors colorNeighbors)
        {
            var heightMap = heightNeighbors[VirtualMap.HeightMapNeighbors.Dir.Center];
            VirtualMap.ColorMap colorMap = colorNeighbors[VirtualMap.ColorMapNeighbors.Dir.Center];

            Renderable.PrepareToBuild(heightMap.Size, Min, minLut);

            //Dispose();
            //renderableDict.Clear();

            // Reset number of local vertices in each Renderable
            // since we're about to remake the list.
            ResetAllVertexCounts();
            

            Vector2 cubeSize = new Vector2(
                heightMap.Scale.X / (float)heightMap.Size.X,
                heightMap.Scale.Y / (float)heightMap.Size.Y);
            Vector2 halfSize = cubeSize * 0.5f;

            Point vert = new Point(0, 0);
            for (vert.X = 0; vert.X < heightMap.Size.X; vert.X += 1)
            {
                for (vert.Y = 0; vert.Y < heightMap.Size.Y; vert.Y += 1)
                {
                    float h = heightMap.GetHeightUnsafe(vert.X, vert.Y);
                    if (h != 0)
                    {
                        ushort color = colorMap[vert.X, vert.Y];

                        //Debug.Assert(TerrainMaterial.IsValid(color, false, true));

                        if (TerrainMaterial.IsValid(color, false, true))
                        {
                            Vector3 pos = new Vector3(
                                (float)vert.X * cubeSize.X + halfSize.X,
                                (float)vert.Y * cubeSize.Y + halfSize.Y,
                                h);

                            Renderable r = GetOrMakeRenderable(colorMap[vert.X, vert.Y]);

                            r.AddVertices(pos, halfSize, vert, heightNeighbors, colorNeighbors);
                        }
                    }
                }
            }

            return FinishGeometry(cubeSize, halfSize);
        }

        /// <summary>
        /// Convert filled out Renderables local data to vertex buffers.
        /// </summary>
        private bool FinishGeometry(Vector2 cubeSize, Vector2 halfSize)
        {
            bool empty = true;

            if (renderableDict.Count > 0)
            {
                Renderable[] renderableList = new Renderable[renderableDict.Count];
                renderableDict.Values.CopyTo(renderableList, 0);

                for (int i = 0; i < renderableList.Length; i++)
                {
                    Renderable r = renderableList[i];

                    if (!r.FinishGeometry(cubeSize, halfSize))
                    {
                        renderableDict.Remove(r.MatIndex);
                    }
                    else
                    {
                        if (empty)
                        {
                            Bounds.Set(r.Bounds);
                        }
                        else
                        {
                            Bounds.Union(r.Bounds.Min);
                            Bounds.Union(r.Bounds.Max);
                        }
                        empty = false;
                    }

                }
            }

            Renderable.MinLut = null;

            return !empty;
        }

        /// <summary>
        /// Compute maximum size of index buffer based on grid size.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private static int IndexSize(Point size)
        {
            if (Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws)
            {
                return size.X * size.Y * 2 * 3 * 5;
            }

            Debug.Assert(false, "Unknown render method!");
            throw new NotImplementedException("Unknown render method!");
        }

        /// <summary>
        /// Check that existing index buffers are sufficient, rebuild if necessary.
        /// </summary>
        /// <param name="size"></param>
        public static void CheckIndices(Point size)
        {
            if (!indexBuffersInitialized)
            {
                MakeIndexBuffers(size);
                indexBuffersInitialized = true;
            }
        }

        /// <summary>
        /// LUT of minimum neighborhood values. See notes in Renderable.MinNeighbor()
        /// </summary>
        private Vector2[,] minLut = null;
        /// <summary>
        /// Size of minLut. Must match backing heightMap size + 1.
        /// </summary>
        private Point minLutSize;
        /// <summary>
        /// Generate the minumum lookup table from the height neighborhood.
        /// </summary>
        /// <param name="neighbors"></param>
        private void MakeMinLut(VirtualMap.HeightMapNeighbors neighbors)
        {
            HeightMap heightMap = neighbors[VirtualMap.HeightMapNeighbors.Dir.Center];

            if ((minLut == null) || (minLutSize.X != heightMap.Size.X + 1) || (minLutSize.Y != heightMap.Size.Y + 1))
            {
                minLutSize = new Point(heightMap.Size.X + 1, heightMap.Size.Y + 1);
                minLut = new Vector2[minLutSize.X, minLutSize.Y];
            }

            float absMax = heightMap.Scale.Z;
            float absMin = 0.0f;

            for (int j = 0; j < minLutSize.Y; ++j)
            {
                for (int i = 0; i < minLutSize.X; ++i)
                {
                    minLut[i, j].X = absMax;
                    minLut[i, j].Y = absMin;
                }
            }

            Debug.Assert(heightMap.Size.X == heightMap.Size.Y);

            int lastPix = heightMap.Size.X - 1;
            int lastLut = lastPix + 1;

            /// Prime the sides from the neighbors. 
            HeightMap north = neighbors[VirtualMap.HeightMapNeighbors.Dir.North];
            if (north != null)
            {
                for (int i = 0; i <= lastPix; ++i)
                {
                    float height = north.GetHeight(i, 0);

                    minLut[i, lastLut].X = Math.Min(minLut[i, lastLut].X, height);
                    minLut[i + 1, lastLut].X = Math.Min(minLut[i + 1, lastLut].X, height);

                    minLut[i, lastLut].Y = Math.Max(minLut[i, lastLut].Y, height);
                    minLut[i + 1, lastLut].Y = Math.Max(minLut[i + 1, lastLut].Y, height);
                }
            }
            else
            {
                for (int i = 0; i < minLutSize.X; ++i)
                {
                    minLut[i, lastLut].X = absMin;
                    minLut[i, lastLut].Y = absMin;
                }
            }
            HeightMap east = neighbors[VirtualMap.HeightMapNeighbors.Dir.East];
            if (east != null)
            {
                for (int j = 0; j <= lastPix; ++j)
                {
                    float height = east.GetHeight(0, j);

                    minLut[lastLut, j].X = Math.Min(minLut[lastLut, j].X, height);
                    minLut[lastLut, j + 1].X = Math.Min(minLut[lastLut, j + 1].X, height);

                    minLut[lastLut, j].Y = Math.Max(minLut[lastLut, j].Y, height);
                    minLut[lastLut, j + 1].Y = Math.Max(minLut[lastLut, j + 1].Y, height);
                }
            }
            else
            {
                for (int j = 0; j < minLutSize.Y; ++j)
                {
                    minLut[lastLut, j].X = absMin;
                    minLut[lastLut, j].Y = absMin;
                }
            }
            HeightMap south = neighbors[VirtualMap.HeightMapNeighbors.Dir.South];
            if (south != null)
            {
                for (int i = 0; i <= lastPix; ++i)
                {
                    float height = south.GetHeight(i, lastPix);

                    minLut[i, 0].X = Math.Min(minLut[i, 0].X, height);
                    minLut[i + 1, 0].X = Math.Min(minLut[i + 1, 0].X, height);

                    minLut[i, 0].Y = Math.Max(minLut[i, 0].Y, height);
                    minLut[i + 1, 0].Y = Math.Max(minLut[i + 1, 0].Y, height);
                }
            }
            else
            {
                for (int i = 0; i < minLutSize.X; ++i)
                {
                    minLut[i, 0].X = absMin;
                    minLut[i, 0].Y = absMin;
                }
            }
            HeightMap west = neighbors[VirtualMap.HeightMapNeighbors.Dir.West];
            if (west != null)
            {
                for (int j = 0; j <= lastPix; ++j)
                {
                    float height = west.GetHeight(lastPix, j);

                    minLut[0, j].X = Math.Min(minLut[0, j].X, height);
                    minLut[0, j + 1].X = Math.Min(minLut[0, j + 1].X, height);

                    minLut[0, j].Y = Math.Max(minLut[0, j].Y, height);
                    minLut[0, j + 1].Y = Math.Max(minLut[0, j + 1].Y, height);
                }
            }
            else
            {
                for (int j = 0; j < minLutSize.Y; ++j)
                {
                    minLut[0, j].X = absMin;
                    minLut[0, j].Y = absMin;
                }
            }

            for (int i = 0; i < heightMap.Size.X; ++i)
            {
                for (int j = 0; j < heightMap.Size.Y; ++j)
                {
                    float height = heightMap.GetHeight(i, j);

                    minLut[i, j].X = Math.Min(minLut[i, j].X, height);
                    minLut[i + 1, j].X = Math.Min(minLut[i + 1, j].X, height);
                    minLut[i, j + 1].X = Math.Min(minLut[i, j + 1].X, height);
                    minLut[i + 1, j + 1].X = Math.Min(minLut[i + 1, j + 1].X, height);

                    minLut[i, j].Y = Math.Max(minLut[i, j].Y, height);
                    minLut[i + 1, j].Y = Math.Max(minLut[i + 1, j].Y, height);
                    minLut[i, j + 1].Y = Math.Max(minLut[i, j + 1].Y, height);
                    minLut[i + 1, j + 1].Y = Math.Max(minLut[i + 1, j + 1].Y, height);
                }
            }
        }

        /// <summary>
        /// Generate and record index data for a single quad.
        /// </summary>
        /// <param name="local"></param>
        /// <param name="face"></param>
        /// <param name="idxBase"></param>
        /// <param name="vertBase"></param>
        /// <param name="idx0"></param>
        /// <param name="idx1"></param>
        /// <param name="idx2"></param>
        /// <param name="idx3"></param>
        private static void StuffIdx(UInt16[] local, int idxBase, int vertBase,
            int idx0, int idx1, int idx2, int idx3)
        {
            local[idxBase + 0] = (UInt16)(vertBase + idx1);
            local[idxBase + 1] = (UInt16)(vertBase + idx0);
            local[idxBase + 2] = (UInt16)(vertBase + idx2);

            local[idxBase + 3] = (UInt16)(vertBase + idx2);
            local[idxBase + 4] = (UInt16)(vertBase + idx0);
            local[idxBase + 5] = (UInt16)(vertBase + idx3);
        }

        /// <summary>
        /// Generate and store index buffer from local indices.
        /// </summary>
        private static void SetIndexBuffer_FD(UInt16[] local, int numIndices)
        {
            if (indexBuffer_FD != null)
            {
                DeviceResetX.Release(ref indexBuffer_FD);
            }

            var device = KoiLibrary.GraphicsDevice;
            var ibuffer = new IndexBuffer(device, typeof(UInt16), numIndices, BufferUsage.WriteOnly);

            ibuffer.SetData<UInt16>(local, 0, numIndices);
            indexBuffer_FD = ibuffer;
        }

        /// <summary>
        /// Make index buffers. (The number depends on the Terrain.RenderMethod.) These are shared among all tiles.
        /// </summary>
        private static bool MakeIndexBuffers(Point size)
        {
            #region Debug_DrawNormalsWithF8: Setup index buffer
#if Debug_DrawNormalsWithF8
            {
                var maxNormalIndices = size.X * size.Y * 4 * 2;
                var localIdx = new UInt16[maxNormalIndices];
                for (ushort i = 0; i < maxNormalIndices; i++)
                    localIdx[i] = i;
                normalsIBuff = new IndexBuffer(KoiLibrary.GraphicsDevice, typeof(UInt16), maxNormalIndices, BufferUsage.WriteOnly);
                normalsIBuff.SetData(localIdx);
            }
#endif
            #endregion

            if (Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws)
            {
                UInt16[] localIdx = new UInt16[IndexSize(size)];

                int idx = 0;
                int iVert = 0;
                for (int x = 0; x < size.X; x++)
                {
                    for (int y = 0; y < size.Y; y++)
                    {
                        for (int i = 0; i < Tile.NumFaces; i++)
                        {
                            StuffIdx(localIdx, idx, iVert, 3, 2, 1, 0); // Must be kept in-sync with index patterns in CacheGeometry!
                            // Also, if this pattern changes, we'll need to rethink the
                            // TopFaceOptimizer_FD.
                            idx += 6;
                            iVert += 4;
                        }
                    }
                }
                int numIndices = idx;

                SetIndexBuffer_FD(localIdx, numIndices);

                return true;
            }

            return false;
        }

        public static void LoadContent(bool immediate)
        {
        }   // end of LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }   // end of LoadContent()

        public static void UnloadContent()
        {
            DeviceResetX.Release(ref indexBuffer_FD);

            indexBuffersInitialized = false;
        }   // end of UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

        #region Debug_DrawNormalsWithF8: IndexBuffer
#if Debug_DrawNormalsWithF8
        private static IndexBuffer normalsIBuff;
#endif
        #endregion

        #endregion INTERNAL

    }   // end of class Tile

}   // end of namespace Boku.SimWorld



