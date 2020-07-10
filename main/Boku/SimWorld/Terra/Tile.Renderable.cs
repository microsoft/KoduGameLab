// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

using Boku.Common;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Input;

namespace Boku.SimWorld.Terra
{
    public partial class Tile
    {
        /// <summary>
        /// A tile's Renderable is the combination of the geometry
        /// extracted from the tile's heightmap, along with the material
        /// information for this section.
        /// 
        /// A Tile will have a collection of Renderables, one for each material.
        /// </summary>
        internal partial class Renderable
        {
            #region FewerDraws render method state

            private VertexBuffer vBuffTop_FD;
            private int vNumTop_FD;                     // Num vertices (before opt).
            private int vBuffTop_FDNumVertices = 0;     // Num vertices in vertex buffer.
            private int vBuffTop_FDSize = 0;            // Size of vertex buffer.

            private VertexBuffer vBuffSides_FD;
            private int vNumSides_FD;                   // Num vertices we want to fit in buffer.
            private int vBuffSides_FDNumVertices = 0;   // Num vertices in vertex buffer.
            private int vBuffSides_FDSize = 0;          // Size of vertex buffer.

            private Terrain.TerrainVertex_FD[] localVertsTop_FD = null;     // Local top verts before opt.  Needs to be at least vNumTop_FD in size.
            private Terrain.TerrainVertex_FD[] localVertsSides_FD = null;   // Local side verts before opt.  Needs to be at least vNumSides_FD in size.

            private static Terrain.TerrainVertex_FD[][] localVertsPool_FD = null;
            private static int localVertsPoolSize_FD = 0;       // Number of arrays in the pool.
            private static int localVertsPoolIndex_FD = 0;      // Next available index.  Array may or may not already be allocated.
            private static int localVertsArraySize_FD = 0;      // Number of vertices in each array.


            #region Accessors

            /// <summary>
            /// FewerDraws vertex buffer for this renderable's top face.
            /// </summary>
            public VertexBuffer VertexBufferTop_FD { get { return vBuffTop_FD; } }

            /// <summary>
            /// Number of vertices in the FewerDraws VertexBuffer for the top face.
            /// </summary>
            public int VertNumTop_FD { get { return vBuffTop_FDNumVertices; } }

            /// <summary>
            /// FewerDraws vertex buffer for this renderable's side faces.
            /// </summary>
            public VertexBuffer VertexBufferSides_FD { get { return vBuffSides_FD; } }

            /// <summary>
            /// Number of vertices in the FewerDraws VertexBuffer for the side faces.
            /// </summary>
            public int VertNumSides_FD { get { return vBuffSides_FDNumVertices; } }

            #endregion

            /// <summary>
            /// Get the local verts array for the top of the cubes.
            /// </summary>
            private Terrain.TerrainVertex_FD[] LocalVertsTop_FD
            {
                get
                {
                    Debug.Assert(Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws);

                    if (localVertsPoolIndex_FD > 100)
                    {
                    }

                    if (localVertsTop_FD == null)
                    {
                        // Allocate base pool array if needed.
                        if (localVertsPool_FD == null)
                        {
                            // The *2 is needed since this same pool is used for top and sides.
                            localVertsPool_FD = new Terrain.TerrainVertex_FD[2 * maxNumRenderables][];
                        }

                        if (localVertsPool_FD[localVertsPoolIndex_FD] == null)
                        {
                            localVertsPool_FD[localVertsPoolIndex_FD] = new Terrain.TerrainVertex_FD[localVertsArraySize_FD];
                            ++localVertsPoolSize_FD;    // Not used.  Useful for keeping track of max size while debugging.
                        }

                        localVertsTop_FD = localVertsPool_FD[localVertsPoolIndex_FD];
                        ++localVertsPoolIndex_FD;
                    }
                    return localVertsTop_FD;
                }
            }

            /// <summary>
            /// Get the local verts array for the sides of the cubes.
            /// </summary>
            private Terrain.TerrainVertex_FD[] LocalVertsSides_FD
            {
                get
                {
                    Debug.Assert(Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws);

                    if (localVertsSides_FD == null)
                    {
                        // Allocate base pool array if needed.
                        if (localVertsPool_FD == null)
                        {
                            // The *2 is needed since this same pool is used for top and sides.
                            localVertsPool_FD = new Terrain.TerrainVertex_FD[2 * maxNumRenderables][];
                        }

                        if (localVertsPool_FD[localVertsPoolIndex_FD] == null)
                        {
                            localVertsPool_FD[localVertsPoolIndex_FD] = new Terrain.TerrainVertex_FD[localVertsArraySize_FD];
                            ++localVertsPoolSize_FD;    // Not used.  Useful for keeping track of max size while debugging.
                        }

                        localVertsSides_FD = localVertsPool_FD[localVertsPoolIndex_FD];
                        ++localVertsPoolIndex_FD;
                    }
                    return localVertsSides_FD;
                }
            }


            #endregion

            /// <summary>
            /// Clears the local vertex and index counts paving the way 
            /// for creating new vertices using the same buffers.
            /// </summary>
            public void ClearLocalVertexCounts()
            {
                // Reset vertex and index counts.
                vNumTop_FD = 0;
                vNumSides_FD = 0;
                vNum_FA = 0;
                iNum_FA = 0;

                // Reset local verts references.
                localVertsTop_FD = null;
                localVertsSides_FD = null;

                localVerts_FA = null;
                localIndices_FA = null;

                gridToVertIdxDictsAvail_FA = 0;
                gridToVertIdxDict_FA = null;
            }   // end of ClearLocalVertexCounts()

            public static void ResetStaticVertexCounts()
            {
                gridToVertIdxDictsAvail_FA = 0;

                localVertsPoolIndex_FD = 0;
                localVertsPoolIndex_FA = 0;
                localIndicesPoolIndex_FA = 0;
            }   // end of ResetStaticVertexCounts()

            #region Fabric render method state

            public bool IsFabric()
            {
                return TerrainMaterial.IsFabric(matIdx);
            }

            //Vertex array
            private Terrain.TerrainVertex_FA[] LocalVerts_FA
            {
                get
                {
                    if (localVerts_FA == null)
                    {
                        // Allocate base pool array if needed.
                        if (localVertsPool_FA == null)
                        {
                            localVertsPool_FA = new Terrain.TerrainVertex_FA[maxNumRenderables][];
                        }

                        if (localVertsPool_FA[localVertsPoolIndex_FA] == null)
                        {
                            localVertsPool_FA[localVertsPoolIndex_FA] = new Terrain.TerrainVertex_FA[localVertsArraySize_FA];
                            ++localVertsPoolSize_FA;    // Not used.  Useful for keeping track of max size while debugging.
                        }

                        localVerts_FA = localVertsPool_FA[localVertsPoolIndex_FA];
                        ++localVertsPoolIndex_FA;
                    }
                    return localVerts_FA;
                }
            }
            
            private Terrain.TerrainVertex_FA[] localVerts_FA = null;        // Local verts array used for this tile/material.

            private static Terrain.TerrainVertex_FA[][] localVertsPool_FA;  // Static pool of local verts arrays shared by all Renderables.

            private static int localVertsPoolSize_FA = 0;       // Number of arrays in the pool.
            private static int localVertsPoolIndex_FA = 0;      // Next available index.  Array may or may not already be allocated.
            private static int localVertsArraySize_FA = 0;      // Number of vertices in each local verts array.
            
            //Vertex buffer
            private VertexBuffer vBuff_FA;
            public VertexBuffer VertexBuffer_FA { get { return vBuff_FA; } }
            private int vBuff_FANumVertices = 0;    // Num vertices in buffer.
            private int vBuff_FASize = 0;           // Size of buffer.

            //Vertex number
            private UInt16 vNum_FA = 0;
            public int VertNum_FA { get { return vNum_FA; } }

            //Index array
            private UInt16[] LocalIndices_FA
            {
                get
                {
                    if (localIndices_FA == null)
                    {
                        // Allocate base pool array if needed.
                        if (localIndicesPool_FA == null)
                        {
                            localIndicesPool_FA = new UInt16[maxNumRenderables][];
                        }

                        if (localIndicesPool_FA[localIndicesPoolIndex_FA] == null)
                        {
                            localIndicesPool_FA[localIndicesPoolIndex_FA] = new UInt16[localIndicesArraySize_FA];
                            ++localIndicesPoolSize_FA;    // Not used.  Useful for keeping track of max size while debugging.
                        }

                        localIndices_FA = localIndicesPool_FA[localIndicesPoolIndex_FA];
                        ++localIndicesPoolIndex_FA;
                    }
                    return localIndices_FA;
                }
            }
            
            private UInt16[] localIndices_FA = null;

            private static UInt16[][] localIndicesPool_FA;

            private static int localIndicesPoolSize_FA = 0;     // Number of arrays in the pool.
            private static int localIndicesPoolIndex_FA = 0;    // Next available index.  Array may or may not already be allocated.
            private static int localIndicesArraySize_FA = 0;    // Number of vertices in each local verts array.

            //Index buffer
            private IndexBuffer iBuff_FA;
            public IndexBuffer IndexBuffer_FA { get { return iBuff_FA; } }
            private int iBuff_FANumIndices = 0;     // Num indices in buffer.
            private int iBuff_FASize = 0;           // Size of buffer.

            // Index number
            private int iNum_FA;
            public int IndexNum_FA { get { return iNum_FA; } }

            // Grid index to vertex index dictionaries
            private IDictionary<UInt16, UInt16> GridToVertIdxDict_FA
            {
                get
                {
                    if (gridToVertIdxDict_FA == null)
                    {
                        if (gridToVertIdxDictsPool_FA == null)
                        {
                            gridToVertIdxDictsPool_FA = new IDictionary<UInt16, UInt16>[maxNumRenderables];
                        }

                        if (gridToVertIdxDictsPool_FA[gridToVertIdxDictsAvail_FA] == null)
                        {
                            // None available, add a new one.
                            gridToVertIdxDict_FA = new Dictionary<UInt16, UInt16>();
                            gridToVertIdxDictsPool_FA[gridToVertIdxDictsAvail_FA++] = gridToVertIdxDict_FA;
                        }
                        else
                        {
                            // Reusing an old one, clear it first.
                            gridToVertIdxDict_FA = gridToVertIdxDictsPool_FA[gridToVertIdxDictsAvail_FA++];
                            gridToVertIdxDict_FA.Clear();
                        }
                    }
                    return gridToVertIdxDict_FA;
                }
            }
            private IDictionary<UInt16, UInt16> gridToVertIdxDict_FA = null;

            private static IDictionary<UInt16, UInt16>[] gridToVertIdxDictsPool_FA;
            private static int gridToVertIdxDictsAvail_FA = 0;

            private static byte gridLengthX_FA = 0;
            private static byte gridLengthY_FA = 0;

            private UInt16 GetGridIndex_FA(int i, int j) { return GetGridIndex_FA(new Point(i, j)); }
            private UInt16 GetGridIndex_FA(Point coord) { return GetGridIndex_FA(coord, new Point(0, 0)); }
            private UInt16 GetGridIndex_FA(Point coord, Point corner)
            {
                var gridX = (coord.X * 2 + 1) + corner.X;
                var gridY = (coord.Y * 2 + 1) + corner.Y;
                UInt16 gridIdx = (ushort)(gridX * gridLengthY_FA + gridY);

                return gridIdx;
            }

            #endregion

            #region MEMBERS
            private ushort matIdx;

            private AABB box = new AABB();

            private bool selected = false;

            private bool culled = false;

            /// <summary>
            /// The following are just maintained during build, and are unique
            /// to this renderable chunk.
            /// </summary>
            private int minX;
            private int minY;
            private int maxX;
            private int maxY;
            private float minZ;
            private float maxZ;

            private static Vector2[,] minLut = null;
            private static Vector2 min;

            private static Point size;

            #endregion MEMBERS

            #region ACCESSORS
            /// <summary>
            /// Material index for this renderable.
            /// </summary>
            public ushort MatIndex
            {
                get { return matIdx; }
                set { matIdx = value; }
            }
            /// <summary>
            /// Bounds for culling.
            /// </summary>
            public AABB Bounds
            {
                get { return box; }
                private set { box = value; }
            }
            /// <summary>
            /// Whether this is part of current terrain selection set.
            /// </summary>
            public bool Selected
            {
                get { return selected; }
                set { selected = value; }
            }

            public bool Culled
            {
                get { return culled; }
                set { culled = value; }
            }

            public bool AnythingToRender
            {
                get
                {
                    return vNumSides_FD > 0 || vNumTop_FD > 0 || vNum_FA > 0;

                    // TODO (****) Rethink this so that it actually works.
                    // Once optimization is done, should be the following.  But this also gets called before then so we can't change it.
                    //return vBuffTop_FDNumVertices > 0 || vBuffSides_FDNumVertices > 0 || vBuff_FANumVertices > 0;
                }
            }

            #region PRIVATE_ACCESSORS
            /// <summary>
            /// width and height of source heightmap, only valid during build.
            /// </summary>
            private static Point Size
            {
                get { return size; }
                set { size = value; }
            }
            /// <summary>
            /// Position in world space for southwest corner of tile. Only
            /// valid during build.
            /// </summary>
            private static Vector2 Min
            {
                get { return min; }
                set { min = value; }
            }

            /// <summary>
            /// Temp stash for minLut, passed in by owning tile for use during
            /// build process.
            /// </summary>
            public static Vector2[,] MinLut
            {
                get { return minLut; }
                set { minLut = value; }
            }
            /// <summary>
            /// Step is really just cached so we aren't looking it up
            /// through terrain.materials all the time. If you want the
            /// value for step, go to terrain.materials[i].Step.
            /// </summary>
            private float Step
            {
                get { return TerrainMaterial.Get(matIdx).Step; }
            }
            #endregion PRIVATE_ACCESSORS

            #endregion ACCESSORS

            #region PUBLIC
            /// <summary>
            /// Constructor takes material index, which never changes again.
            /// </summary>
            public Renderable(ushort matIdx)
            {
                Debug.Assert(TerrainMaterial.IsValid(matIdx, false, true)); // There is no reason to create a renderable
                                                                            // to render an empty or invalid material.

                this.matIdx = matIdx;

                localVertsTop_FD = null;
                vNumTop_FD = 0;

                localVertsSides_FD = null;
                vNumSides_FD = 0;
                localVerts_FA = null;
                vNum_FA = 0;

                gridToVertIdxDict_FA = null;
                localIndices_FA = null;
                iNum_FA = 0;

                minX = Size.X;
                minY = Size.Y;
                maxX = 0;
                maxY = 0;
                minZ = Single.MaxValue;
                maxZ = Single.MinValue;
            }

            /// <summary>
            /// Prepare to start generating vertices.
            /// </summary>
            /// <param name="size"></param>
            /// <param name="min"></param>
            /// <param name="minLut"></param>
            /// <param name="maxNumVerts"></param>
            public static void PrepareToBuild(Point size, Vector2 min, Vector2[,] minLut)
            {
                MinLut = minLut;
                Min = min;
                Size = size;

                const int vertsPerFace = 4;
                localVertsArraySize_FD = size.X * size.Y * vertsPerFace * Tile.NumFaces;
                const int maxVertsPerGrid = 5;
                const int maxTrianglesPerGrid = 4;
                const int indicesPerTriangle = 3;

                localVertsArraySize_FA = size.X * size.Y * maxVertsPerGrid;
                localIndicesArraySize_FA = size.X * size.Y * maxTrianglesPerGrid * indicesPerTriangle;

                Debug.Assert(localIndicesArraySize_FA < UInt16.MaxValue);

                gridLengthX_FA = (byte)(size.X * 2 + 1);
                gridLengthY_FA = (byte)(size.Y * 2 + 1);
            }

            /// <summary>
            /// Add the vertices corresponding to a heightmap element.
            /// </summary>
            public void AddVertices(
                Vector3 tilePos,
                Vector2 halfSize,
                Point coord,
                VirtualMap.HeightMapNeighbors heightNeighbors,
                VirtualMap.ColorMapNeighbors colorNeighbors)
            {
                //Left: -X direction
                //Right: +X direction
                //Front: -Y direction
                //Back: +Y direction
                //Up: +Z direction
                //Down: -Z direction
                //The notation "minHLF" means "Minimum Height Left Front"
                var minHLF = MinNeighbor(coord, 0, 0); //Compares: this, left, front, left-front
                var minHLB = MinNeighbor(coord, 0, 1); //Compares: this, left, back, left-back
                var minHRF = MinNeighbor(coord, 1, 0); //Compares: this, right, front, right-front
                var minHRB = MinNeighbor(coord, 1, 1); //Compares: this, right, back, right-back

                minZ = minZ <= minHLF ? minZ : minHLF;
                minZ = minZ <= minHLB ? minZ : minHLB;
                minZ = minZ <= minHRF ? minZ : minHRF;
                minZ = minZ <= minHRB ? minZ : minHRB;

                var maxHLF = MaxNeighbor(coord, 0, 0); //Compares: this, left, front, left-front
                var maxHLB = MaxNeighbor(coord, 0, 1); //Compares: this, left, back, left-back
                var maxHRF = MaxNeighbor(coord, 1, 0); //Compares: this, right, front, right-front
                var maxHRB = MaxNeighbor(coord, 1, 1); //Compares: this, right, back, right-back

                maxZ = maxZ >= maxHLF ? maxZ : maxHLF;
                maxZ = maxZ >= maxHLB ? maxZ : maxHLB;
                maxZ = maxZ >= maxHRF ? maxZ : maxHRF;
                maxZ = maxZ >= maxHRB ? maxZ : maxHRB;

                minX = minX <= coord.X ? minX : coord.X;
                minY = minY <= coord.Y ? minY : coord.Y;
                maxX = maxX >= coord.X ? maxX : coord.X;
                maxY = maxY >= coord.Y ? maxY : coord.Y;

                var worldPos = new Vector3(tilePos.X + Min.X, tilePos.Y + Min.Y, tilePos.Z);

                var cMap = colorNeighbors[VirtualMap.ColorMapNeighbors.Dir.Center];
                var hMap = heightNeighbors[VirtualMap.HeightMapNeighbors.Dir.Center];

                //Height of the front neighbor-block
                var neighborHF = heightNeighbors.GetHeight(coord.X, coord.Y - 1);

                //Height of the back neighbor-block
                var neighborHB = heightNeighbors.GetHeight(coord.X, coord.Y + 1);

                //Height of the left neighbor-block
                var neighborHL = heightNeighbors.GetHeight(coord.X - 1, coord.Y);

                //Height of the right neighbor-block
                var neighborHR = heightNeighbors.GetHeight(coord.X + 1, coord.Y);

                //The empty material index
                var emptyIdx = TerrainMaterial.EmptyMatIdx;

                //Material of the front neighbor-block
                var neighborCF = neighborHF > 0
                    ? colorNeighbors.GetColor(coord.X, coord.Y - 1)
                    : emptyIdx;

                //Material of the back neighbor-block
                var neighborCB = neighborHB > 0
                    ? colorNeighbors.GetColor(coord.X, coord.Y + 1)
                    : emptyIdx;

                //Material of the left neighbor-block
                var neighborCL = neighborHL > 0
                    ? colorNeighbors.GetColor(coord.X - 1, coord.Y)
                    : emptyIdx;

                //Material of the right neighbor-block
                var neighborCR = neighborHR > 0
                    ? colorNeighbors.GetColor(coord.X + 1, coord.Y)
                    : emptyIdx;

                //Is the front neighbor-block the same material as us and within our tile?
                var isSameCF = (coord.Y - 1 < 0)
                    ? false
                    : neighborCF == matIdx;

                //Is the back neighbor-block the same material as us and within our tile?
                var isSameCB = (coord.Y + 1 >= cMap.Size)
                    ? false
                    : neighborCB == matIdx;

                //Is the left neighbor-block the same material as us and within our tile?
                var isSameCL = (coord.X - 1 < 0)
                    ? false
                    : neighborCL == matIdx;

                //Is the right neighbor-block the same material as us and within our tile?
                var isSameCR = (coord.X + 1 >= cMap.Size)
                    ? false
                    : neighborCR == matIdx;

                if (IsFabric())
                {
                    #region Fabric
                    //Get the height of the left-front neighbor
                    var neighborHLF = heightNeighbors.GetHeight(coord.X - 1, coord.Y - 1);

                    //Get the height of the left-back neighbor
                    var neighborHLB = heightNeighbors.GetHeight(coord.X - 1, coord.Y + 1);

                    //Get the height of the right-front neighbor
                    var neighborHRF = heightNeighbors.GetHeight(coord.X + 1, coord.Y - 1);

                    //Get the height of the right-back neighbor
                    var neighborHRB = heightNeighbors.GetHeight(coord.X + 1, coord.Y + 1);

                    //Get the color of the left-front neighbor
                    var neighborCLF = neighborHLF > 0
                        ? colorNeighbors.GetColor(coord.X - 1, coord.Y - 1)
                        : emptyIdx;

                    //Get the color of the left-back neighbor
                    var neighborCLB = neighborHLB > 0
                        ? colorNeighbors.GetColor(coord.X - 1, coord.Y + 1)
                        : emptyIdx;

                    //Get the color of the right-front neighbor
                    var neighborCRF = neighborHRF > 0
                        ? colorNeighbors.GetColor(coord.X + 1, coord.Y - 1)
                        : emptyIdx;

                    //Get the color of the right-back neighbor
                    var neighborCRB = neighborHRB > 0
                        ? colorNeighbors.GetColor(coord.X + 1, coord.Y + 1)
                        : emptyIdx;

                    bool isFabricF = TerrainMaterial.IsFabric(neighborCF);
                    bool isFabricB = TerrainMaterial.IsFabric(neighborCB);
                    bool isFabricL = TerrainMaterial.IsFabric(neighborCL);
                    bool isFabricR = TerrainMaterial.IsFabric(neighborCR);
                    bool isFabricLF = TerrainMaterial.IsFabric(neighborCLF);
                    bool isFabricLB = TerrainMaterial.IsFabric(neighborCLB);
                    bool isFabricRF = TerrainMaterial.IsFabric(neighborCRF);
                    bool isFabricRB = TerrainMaterial.IsFabric(neighborCRB);

                    //Is the left-front neighbor-block the same material as us and within our tile?
                    var isSameCLF = (coord.X - 1 < 0) || (coord.Y - 1 < 0)
                        ? false
                        : neighborCLF == matIdx;

                    //Is the left-back neighbor-block the same material as us and within our tile?
                    var isSameCLB = (coord.X - 1 < 0) || (coord.Y + 1 >= cMap.Size)
                        ? false
                        : neighborCLB == matIdx;

                    //Is the right-front neighbor-block the same material as us and within our tile?
                    var isSameCRF = (coord.X + 1 >= cMap.Size) || (coord.Y - 1 < 0)
                        ? false
                        : neighborCRF == matIdx;

                    //Is the right-back neighbor-block the same material as us and within our tile?
                    var isSameCRB = (coord.X + 1 >= cMap.Size) || (coord.Y + 1 >= cMap.Size)
                        ? false
                        : neighborCRB == matIdx;

                    //Center vertex
                    var gridIdx = GetGridIndex_FA(coord);
                    var vertIdx = vNum_FA++;

                    var pos = worldPos;
                    var normal = VertexNormal(coord, heightNeighbors);

                    LocalVerts_FA[vertIdx] = new Terrain.TerrainVertex_FA(worldPos, normal);
                    GridToVertIdxDict_FA[gridIdx] = vertIdx;

                    //Setup left vertex (if needed)
                    UInt16 vertIdxL = 0;
                    if (isSameCL)
                    {
                        var gridIdxL = GetGridIndex_FA(coord.X - 1, coord.Y);
                        vertIdxL = GridToVertIdxDict_FA[gridIdxL];
                    }

                    //Setup front vertex (if needed)
                    UInt16 vertIdxF = 0;
                    if (isSameCF)
                    {
                        var gridIdxF = GetGridIndex_FA(coord.X, coord.Y - 1);
                        vertIdxF = GridToVertIdxDict_FA[gridIdxF];
                    }

                    if (isSameCLF && isSameCL && isSameCF)
                    {
                        var gridIdxLF = GetGridIndex_FA(coord.X - 1, coord.Y - 1);
                        var vertIdxLF2 = GridToVertIdxDict_FA[gridIdxLF];

                        LocalIndices_FA[iNum_FA++] = vertIdx;
                        LocalIndices_FA[iNum_FA++] = vertIdxF;
                        LocalIndices_FA[iNum_FA++] = vertIdxLF2;

                        LocalIndices_FA[iNum_FA++] = vertIdxLF2;
                        LocalIndices_FA[iNum_FA++] = vertIdxL;
                        LocalIndices_FA[iNum_FA++] = vertIdx;
                    }

                    //Get the left-front corner height
                    float hLF;
                    if (isFabricLF)
                        hLF = (neighborHLF + worldPos.Z) / 2f;
                    else
                        hLF = minHLF;
                    if (!isFabricL || !isFabricF)
                        hLF = Math.Min(hLF, minHLF);

                    //Get the left-back corner height
                    var hLB = float.MaxValue;
                    if (isFabricB && isFabricL)
                        hLB = (neighborHL + neighborHB) / 2f;
                    if (!isFabricLB || !isFabricL || !isFabricB)
                        hLB = Math.Min(hLB, minHLB);

                    //Get the right-front corner height
                    var hRF = float.MaxValue;
                    if (isFabricF && isFabricR)
                        hRF = (neighborHF + neighborHR) / 2f;
                    if (!isFabricRF || !isFabricR || !isFabricF)
                        hRF = Math.Min(hRF, minHRF);

                    //Get the left-front corner height
                    float hRB;
                    if (isFabricRB)
                        hRB = (neighborHRB + worldPos.Z) / 2f;
                    else
                        hRB = minHRB;
                    if (!isFabricR || !isFabricB)
                        hRB = Math.Min(hRB, minHRB);

                    //Setup left-front vertex (if needed)
                    UInt16 vertIdxLF = 0;
                    if (!isSameCF || !isSameCL || !isSameCLF)
                    {
                        var gridIdxLF = GetGridIndex_FA(coord, new Point(-1, -1));

                        if (!GridToVertIdxDict_FA.TryGetValue(gridIdxLF, out vertIdxLF))
                        {
                            vertIdxLF = vNum_FA++;

                            var posLF = new Vector3(worldPos.X - halfSize.X, worldPos.Y - halfSize.Y, hLF);
                            var normalLF = normal;// FaceNormals[0]; //ToDo: Get proper normal

                            LocalVerts_FA[vertIdxLF] = new Terrain.TerrainVertex_FA(posLF, normalLF);

                            GridToVertIdxDict_FA[gridIdxLF] = vertIdxLF;
                        }
                    }

                    //Setup left-back vertex (if needed)
                    UInt16 vertIdxLB = 0;
                    if (!isSameCB || !isSameCL || !isSameCLB)
                    {
                        var gridIdxLB = GetGridIndex_FA(coord, new Point(-1, 1));

                        if (!GridToVertIdxDict_FA.TryGetValue(gridIdxLB, out vertIdxLB))
                        {
                            vertIdxLB = vNum_FA++;

                            var posLB = new Vector3(worldPos.X - halfSize.X, worldPos.Y + halfSize.Y, hLB);
                            var normalLB = normal;// FaceNormals[0]; //ToDo: Get proper normal

                            LocalVerts_FA[vertIdxLB] = new Terrain.TerrainVertex_FA(posLB, normalLB);

                            GridToVertIdxDict_FA[gridIdxLB] = vertIdxLB;
                        }
                    }

                    //Setup right-front vertex (if needed)
                    UInt16 vertIdxRB = 0;
                    if (!isSameCB || !isSameCR || !isSameCRB)
                    {
                        var gridIdxRB = GetGridIndex_FA(coord, new Point(1, 1));

                        if (!GridToVertIdxDict_FA.TryGetValue(gridIdxRB, out vertIdxRB))
                        {
                            vertIdxRB = vNum_FA++;

                            var posRB = new Vector3(worldPos.X + halfSize.X, worldPos.Y + halfSize.Y, hRB);
                            var normalRB = normal;// FaceNormals[0]; //ToDo: Get proper normal

                            LocalVerts_FA[vertIdxRB] = new Terrain.TerrainVertex_FA(posRB, normalRB);

                            GridToVertIdxDict_FA[gridIdxRB] = vertIdxRB;
                        }
                    }

                    //Setup right-front vertex (if needed)
                    UInt16 vertIdxRF = 0;
                    if (!isSameCF || !isSameCR || !isSameCRF)
                    {
                        var gridIdxRF = GetGridIndex_FA(coord, new Point(1, -1));

                        if (!GridToVertIdxDict_FA.TryGetValue(gridIdxRF, out vertIdxRF))
                        {
                            vertIdxRF = vNum_FA++;

                            var posRF = new Vector3(worldPos.X + halfSize.X, worldPos.Y - halfSize.Y, hRF);
                            var normalRF = normal;// FaceNormals[0]; //ToDo: Get proper normal

                            LocalVerts_FA[vertIdxRF] = new Terrain.TerrainVertex_FA(posRF, normalRF);

                            GridToVertIdxDict_FA[gridIdxRF] = vertIdxRF;
                        }
                    }

                    if (!isSameCF)
                    {
                        LocalIndices_FA[iNum_FA++] = vertIdx;
                        LocalIndices_FA[iNum_FA++] = vertIdxRF;
                        LocalIndices_FA[iNum_FA++] = vertIdxLF;
                    }
                    if (!isSameCB)
                    {
                        LocalIndices_FA[iNum_FA++] = vertIdx;
                        LocalIndices_FA[iNum_FA++] = vertIdxLB;
                        LocalIndices_FA[iNum_FA++] = vertIdxRB;
                    }
                    if (!isSameCL)
                    {
                        LocalIndices_FA[iNum_FA++] = vertIdx;
                        LocalIndices_FA[iNum_FA++] = vertIdxLF;
                        LocalIndices_FA[iNum_FA++] = vertIdxLB;
                    }
                    if (!isSameCR)
                    {
                        LocalIndices_FA[iNum_FA++] = vertIdx;
                        LocalIndices_FA[iNum_FA++] = vertIdxRB;
                        LocalIndices_FA[iNum_FA++] = vertIdxRF;
                    }

                    if (isSameCL && (!isSameCF || !isSameCLF))
                    {
                        LocalIndices_FA[iNum_FA++] = vertIdx;
                        LocalIndices_FA[iNum_FA++] = vertIdxLF;
                        LocalIndices_FA[iNum_FA++] = vertIdxL;
                    }
                    if (isSameCL && (!isSameCB || !isSameCLB))
                    {
                        LocalIndices_FA[iNum_FA++] = vertIdx;
                        LocalIndices_FA[iNum_FA++] = vertIdxL;
                        LocalIndices_FA[iNum_FA++] = vertIdxLB;
                    }
                    if (isSameCF && (!isSameCL || !isSameCLF))
                    {
                        LocalIndices_FA[iNum_FA++] = vertIdx;
                        LocalIndices_FA[iNum_FA++] = vertIdxF;
                        LocalIndices_FA[iNum_FA++] = vertIdxLF;
                    }
                    if (isSameCF && (!isSameCR || !isSameCRF))
                    {
                        LocalIndices_FA[iNum_FA++] = vertIdx;
                        LocalIndices_FA[iNum_FA++] = vertIdxRF;
                        LocalIndices_FA[iNum_FA++] = vertIdxF;
                    }

                    #endregion
                }
                else
                {
                    #region Cube

                    var topHLFSmoothed = (maxHLF - minHLF < Step);
                    var topHLF = topHLFSmoothed ? ((maxHLF + minHLF) * .5f) : tilePos.Z;

                    var topHLBSmoothed = (maxHLB - minHLB < Step);
                    var topHLB = topHLBSmoothed ? ((maxHLB + minHLB) * .5f) : tilePos.Z;

                    var topHRFSmoothed = (maxHRF - minHRF < Step);
                    var topHRF = topHRFSmoothed ? ((maxHRF + minHRF) * .5f) : tilePos.Z;

                    var topHRBSmoothed = (maxHRB - minHRB < Step);
                    var topHRB = topHRBSmoothed ? ((maxHRB + minHRB) * .5f) : tilePos.Z;

                    if (Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws)
                    {
                        Vector3 vertPos;

                        //Top
                        vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y - halfSize.Y, topHLF);
                        LocalVertsTop_FD[vNumTop_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLF, 0, 0);

                        vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y + halfSize.Y, topHLB);
                        LocalVertsTop_FD[vNumTop_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLB, 0, 2);

                        vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y + halfSize.Y, topHRB);
                        LocalVertsTop_FD[vNumTop_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRB, 0, 3);

                        vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y - halfSize.Y, topHRF);
                        LocalVertsTop_FD[vNumTop_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRF, 0, 1);

                        //Front
                        if ((neighborHF < topHLF || neighborHF < topHRF) || !isSameCF)
                        {
                            vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y - halfSize.Y, topHLF);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLF, 1, 0);

                            vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y - halfSize.Y, topHRF);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRF, 1, 1);

                            vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y - halfSize.Y, minHRF);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRF, 1, 1);

                            vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y - halfSize.Y, minHLF);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLF, 1, 0);
                        }

                        //Back
                        if ((neighborHB < topHLB || neighborHB < topHRB) || !isSameCB)
                        {
                            vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y + halfSize.Y, topHRB);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRB, 2, 3);

                            vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y + halfSize.Y, topHLB);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLB, 2, 2);

                            vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y + halfSize.Y, minHLB);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLB, 2, 2);

                            vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y + halfSize.Y, minHRB);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRB, 2, 3);
                        }

                        //Left
                        if ((neighborHL < topHLF || neighborHL < topHLB) || !isSameCL)
                        {
                            vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y + halfSize.Y, topHLB);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLB, 3, 2);

                            vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y - halfSize.Y, topHLF);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLF, 3, 0);

                            vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y - halfSize.Y, minHLF);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLF, 3, 0);

                            vertPos = new Vector3(worldPos.X - halfSize.X, worldPos.Y + halfSize.Y, minHLB);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHLB, 3, 2);
                        }

                        //Right
                        if ((neighborHR < topHRF || neighborHR < topHRB) || !isSameCR)
                        {
                            vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y - halfSize.Y, topHRF);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRF, 4, 1);

                            vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y + halfSize.Y, topHRB);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRB, 4, 3);

                            vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y + halfSize.Y, minHRB);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRB, 4, 3);

                            vertPos = new Vector3(worldPos.X + halfSize.X, worldPos.Y - halfSize.Y, minHRF);
                            LocalVertsSides_FD[vNumSides_FD++] = new Terrain.TerrainVertex_FD(vertPos, topHRF, 4, 1);
                        }
                    } // End FewerDraws render method branch
                    #endregion
                }
            }

            /// <summary>
            /// Finish up build process, generating bounds and device geometry.
            /// Called after all vertices have been generated.
            /// </summary>
            /// <param name="scale"></param>
            /// <param name="step"></param>
            /// <returns>True if there's anything to render.</returns>
            public bool FinishGeometry(Vector2 cubeSize, Vector2 halfSize)
            {
                if (AnythingToRender)
                {
                    /// Set up the bounds before we forget.
                    Bounds.Set(
                        new Vector3(
                            Min.X + minX * cubeSize.X,
                            Min.Y + minY * cubeSize.Y,
                            minZ),
                        new Vector3(
                            Min.X + maxX * cubeSize.X + cubeSize.X,
                            Min.Y + maxY * cubeSize.Y + cubeSize.Y,
                            maxZ)
                    );

                    SetAndOptimizeBuffers();

                }
                return AnythingToRender;
            }

            /// <summary>
            /// Reset the vertex buffers and possibly perform optimizations
            /// </summary>
            public void SetAndOptimizeBuffers()
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                // When allocating a buffer, allocate it the size needed plus the cushion size.
                // This will help minimize churning by preventing lots of small changes.
                const int kCushion = 300;   

                if (IsFabric())
                {
                    #region Fabric

                    //
                    // Vertex buffer.
                    //
                    vBuff_FANumVertices = vNum_FA;

                    // Allocate buffer if null or too small.
                    if (vBuff_FA == null || vBuff_FASize < vBuff_FANumVertices)
                    {
                        // Release old buffer.
                        BokuGame.Release(ref vBuff_FA);

                        // Calc new size and allocate.
                        vBuff_FASize = vBuff_FANumVertices + kCushion;
                        vBuff_FA = new VertexBuffer(device, typeof(Terrain.TerrainVertex_FA), vBuff_FASize, BufferUsage.WriteOnly);
                    }

                    // Set data into buffer.
                    vBuff_FA.SetData<Terrain.TerrainVertex_FA>(LocalVerts_FA, 0, vBuff_FANumVertices);

                    //
                    // Index buffer.
                    //
                    iBuff_FANumIndices = iNum_FA;

                    // Allocate buffer if null or too small.
                    if (iBuff_FA == null || iBuff_FASize < iBuff_FANumIndices)
                    {
                        // Release old buffer.
                        BokuGame.Release(ref iBuff_FA);

                        // Calc new size and allocate.
                        iBuff_FASize = iBuff_FANumIndices + kCushion;
                        iBuff_FA = new IndexBuffer(device, typeof(UInt16), iBuff_FASize, BufferUsage.WriteOnly);
                    }

                    // Set data into buffer.
                    iBuff_FA.SetData<UInt16>(LocalIndices_FA, 0, iBuff_FANumIndices);

                    #endregion
                    #region Debug_DrawNormalsWithF8: Setup vertex buffer
#if Debug_DrawNormalsWithF8
                    {
                        var normVerts = new Terrain.TerrainVertex_FA[vNum_FA * 2];
                        for (int i = 0; i < vNum_FA; i++)
                        {
                            var vert = LocalVerts_FA[i];

                            //Unpack our vertex
                            var p4 = vert.positionAndNormalZ;
                            var n2 = vert.normalXY;
                            var pos = new Vector3(p4.X, p4.Y, p4.Z);
                            var norm = new Vector3(n2.X, n2.Y, p4.W);

                            //Extend our second position
                            const float s = 0.75f;
                            var pos2 = pos + norm * s;

                            //Build our second vert
                            var vert2 = vert;
                            vert2.positionAndNormalZ = new Vector4(pos2.X, pos2.Y, pos2.Z, norm.Z);

                            //Set our local verts
                            normVerts[i * 2] = vert;
                            normVerts[i * 2 + 1] = vert2;
                        }

                        //
                        // Normals vbuff
                        //

                        normalsVBuffNumVertices = vNum_FA * 2;

                        // Allocate buffer if null or too small.
                        if (normalsVBuff == null || normalsVBuffSize < normalsVBuffNumVertices)
                        {
                            // Release old buffer.
                            BokuGame.Release(ref normalsVBuff);

                            // Calc new size and allocate.
                            normalsVBuffSize = normalsVBuffNumVertices + kCushion;
                            normalsVBuff = new VertexBuffer(device, typeof(Terrain.TerrainVertex_FA), normalsVBuffSize, BufferUsage.WriteOnly);
                        }

                        // Set data into buffer.
                        normalsVBuff.SetData<Terrain.TerrainVertex_FA>(normVerts, 0, normalsVBuffNumVertices);
                    }
#endif
                    #endregion
                }
                else
                {
                    #region Cube
                    if (Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws)
                    {
                        //Optimize for flat terrain
                        FlatTerrainOptimizer_FD optimizer = new FlatTerrainOptimizer_FD(LocalVertsTop_FD, vNumTop_FD);
                        Terrain.TerrainVertex_FD[] optimizedVertsTop = optimizer.Optimize();

                        vBuffTop_FDNumVertices = optimizedVertsTop.Length;
                        vBuffSides_FDNumVertices = vNumSides_FD;

                        // Allocate vertex buffers if null or too small.
                        if (vBuffTop_FD == null || vBuffTop_FDSize < optimizedVertsTop.Length)
                        {
                            // Free any existing vertex buffer.
                            BokuGame.Release(ref vBuffTop_FD);

                            // Get new length and allocate new buffer.
                            vBuffTop_FDSize = vBuffTop_FDNumVertices + kCushion;
                            vBuffTop_FD = new VertexBuffer(device, typeof(Terrain.TerrainVertex_FD), vBuffTop_FDSize, BufferUsage.WriteOnly);
                        }

                        if (vBuffSides_FD == null || vBuffSides_FDSize < vNumSides_FD)
                        {
                            // Free any existing vertex buffer.
                            BokuGame.Release(ref vBuffSides_FD);

                            // Get new length and allocate new buffer.
                            vBuffSides_FDSize = vBuffSides_FDNumVertices + kCushion;
                            vBuffSides_FD = new VertexBuffer(device, typeof(Terrain.TerrainVertex_FD), vBuffSides_FDSize, BufferUsage.WriteOnly);
                        }

                        //Set the top face buffer
                        vBuffTop_FD.SetData<Terrain.TerrainVertex_FD>(optimizedVertsTop, 0, vBuffTop_FDNumVertices);

                        //Set the side faces buffer
                        vBuffSides_FD.SetData<Terrain.TerrainVertex_FD>(LocalVertsSides_FD, 0, vBuffSides_FDNumVertices);
                    }
                    #endregion
                }

            }

            /// <summary>
            /// Discard device resources.
            /// </summary>
            public void Dispose()
            {
                BokuGame.Release(ref vBuffTop_FD);
                BokuGame.Release(ref vBuffSides_FD);
                vBuffTop_FDNumVertices = 0;
                vBuffSides_FDNumVertices = 0;
                vBuffTop_FDSize = 0;
                vBuffSides_FDSize = 0;

                BokuGame.Release(ref vBuff_FA);
                BokuGame.Release(ref iBuff_FA);
                vBuff_FANumVertices = 0;
                iBuff_FANumIndices = 0;
                vBuff_FASize = 0;
                iBuff_FASize = 0;

                #region Debug_DrawNormalsWithF8: Dispose VertexBuffer
#if Debug_DrawNormalsWithF8
                BokuGame.Release(ref normalsVBuff);
                normalsVBuffNumVertices = 0;
                normalsVBuffSize = 0;
#endif
                #endregion
            }

            /// <summary>
            /// Give up our vertex buffers because someone else (the batching system)
            /// is taking them over.
            /// </summary>
            public void PreDispose()
            {
                vBuffTop_FD = null;
                vBuffSides_FD = null;
                vBuff_FA = null;
                iBuff_FA = null;

                #region Debug_DrawNormalsWithF8: PreDispose VertexBuffer
#if Debug_DrawNormalsWithF8
                normalsVBuff = null;
#endif
                #endregion
            }
            #endregion PUBLIC

            #region INTERNAL
            private float MaxNeighbor(Point pos, int dirX, int dirY)
            {
                return minLut[pos.X + dirX, pos.Y + dirY].Y;
            }
            /// <summary>
            /// Get height of shortest adjacent neighbor in height neighborhood.
            /// </summary>
            /// <param name="minH"></param>
            /// <param name="pos"></param>
            /// <param name="dir"></param>
            /// <returns></returns>
            private float MinNeighbor(Point pos, int dirX, int dirY)
            {
                return minLut[pos.X + dirX, pos.Y + dirY].X;

                /// The LUT is a more efficient way, with a single call of get height per
                /// terrain grid point, of calculating the same thing as the following code.
                //int nx = pos.X + dir.X;
                //int ny = pos.Y + dir.Y;
                //if ((nx < 0) || (ny < 0) || (nx >= heightMap.Size.X) || (ny >= heightMap.Size.Y))
                //{
                //    return -heightMap.Scale.Z;
                //}
                //float h = heightMap.GetHeight(nx, ny);
                //minH = Math.Min(h, minH);
                //h = heightMap.GetHeight(nx, pos.Y);
                //minH = Math.Min(h, minH);
                //h = heightMap.GetHeight(pos.X, ny);
                //minH = Math.Min(h, minH);
                //return minH;
            }

            private Vector3 VertexNormal(Point coord, VirtualMap.HeightMapNeighbors heightNeighbors)
            {
                var hMap = heightNeighbors[VirtualMap.HeightMapNeighbors.Dir.Center];
                var hMapF = heightNeighbors[VirtualMap.HeightMapNeighbors.Dir.South];
                var hMapB = heightNeighbors[VirtualMap.HeightMapNeighbors.Dir.North];
                var hMapL = heightNeighbors[VirtualMap.HeightMapNeighbors.Dir.West];
                var hMapR = heightNeighbors[VirtualMap.HeightMapNeighbors.Dir.East];

                var h = hMap.GetHeight(coord.X, coord.Y);

                var hF = (coord.Y - 1 < 0)
                    ? (hMapF == null ? h : hMapF.GetHeight(coord.X, hMapF.Size.Y - 1))
                    : (hMap.GetHeight(coord.X, coord.Y - 1));

                var hB = (coord.Y + 1 >= hMap.Size.Y)
                    ? (hMapB == null ? h : hMapB.GetHeight(coord.X, 0))
                    : (hMap.GetHeight(coord.X, coord.Y + 1));

                var hL = (coord.X - 1 < 0)
                    ? (hMapL == null ? h : hMapL.GetHeight(hMapL.Size.X - 1, coord.Y))
                    : (hMap.GetHeight(coord.X - 1, coord.Y));

                var hR = (coord.X + 1 >= hMap.Size.X)
                    ? (hMapR == null ? h : hMapR.GetHeight(0, coord.Y))
                    : (hMap.GetHeight(coord.X + 1, coord.Y));

                var vF = new Vector3(coord.X, coord.Y - 1, hF);
                var vB = new Vector3(coord.X, coord.Y + 1, hB);
                var vL = new Vector3(coord.X - 1, coord.Y, hL);
                var vR = new Vector3(coord.X + 1, coord.Y, hR);

                return Vector3.Normalize(Vector3.Cross(vL - vR, vF - vB));
            }

            #region Debug_DrawNormalsWithF8: VertexBuffer
#if Debug_DrawNormalsWithF8
            internal VertexBuffer normalsVBuff;
            int normalsVBuffNumVertices = 0;    // Num vertices in the buffer.
            int normalsVBuffSize = 0;           // Size of buffer.
#endif
            #endregion

            #endregion INTERNAL
    
        }   // end of class Renderable

    }   // end of class Tile

}   // end of namespace Boku.SimWorld.Terra
