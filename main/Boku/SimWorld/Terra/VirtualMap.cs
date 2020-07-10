// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Common;
using Boku.Common.Xml;

namespace Boku.SimWorld.Terra
{
    /// <summary>
    /// Abstraction for infinite heightMap. Only non-empty tiles actually
    /// take up system resources. Includes height info (HeightMap), material
    /// info (ColorMap), and renderable info (Tile).
    /// </summary>
    public partial class VirtualMap
    {
        #region Members
        /// <summary>
        /// Grid of maps which this class virtualizes into a single infinite map
        /// </summary>
        private HeightMap[,] maps = null;

        /// <summary>
        /// Number of maps we actually have.
        /// </summary>
        private Point numMaps;
        /// <summary>
        /// Size of each map in world units
        /// </summary>
        private Vector3 mapSize;

        /// <summary>
        /// Min and Max extents in horizontal world space
        /// </summary>
        private Vector2 worldMin;
        private Vector2 worldMax;

        /// <summary>
        /// All tiles (and heightmaps) share the following:
        ///     Size in pixels
        ///     Size in world space
        ///     
        /// A tile is:
        ///     a heightmap
        ///     a mask of which materials are used
        ///     at a position (i.e. max and min corners)
        ///     max and min height
        ///     whether it contains any water.
        ///     a list of renderable geometrys - one for each material.
        /// </summary>
        private int mapCount = 0;
        private Tile[,] tiles = null;

        /// <summary>
        /// Material info grid matching the maps and tiles.
        /// </summary>
        private ColorMap[,] colorMaps = null;

        /// <summary>
        /// Water info grid matching maps and tiles.
        /// </summary>
        private WaterMap[,] waterMaps = null;

        /// <summary>
        /// Water renderables for each tile on the grid
        /// </summary>
        private WaterTile[,] waterTiles = null;

        /// <summary>
        /// Bounds for all current terrain.
        /// </summary>
        private AABB landBounds = new AABB();
        private float maxWaterHeight = 0.0f;

        /// <summary>
        /// Keep a queue of tiles that need updating, so we can spread the
        /// updates out over multiple frames as we can afford.
        /// </summary>
        private Queue<Tile> updates = new Queue<Tile>();
        private int deadMaps = 0;
        private int maxDeadMaps = 3; // ???, random value TODO
        private static bool suppressWaterUpdate = false;
        private static bool flushWaterUpdate = false;

        /// <summary>
        /// Remembers the last selection position passed into "MakeMaterialSelection".
        /// This is used to determine whether a new material selection should be computed. We don't want
        /// to change the selection if we haven't moved and we are shrinking the selection, because
        /// this causes us to lose our brush if it shrinks past the currently selected material.
        /// </summary>
        private Vector2 lastSelectionPos = Vector2.Zero;
        bool shrinkingSelection = false;

        private class WaterQueues
        {
            public Queue<Water> erases = new Queue<Water>();
            public Queue<Water> disposes = new Queue<Water>();
            public Queue<Water> fills = new Queue<Water>();
            public Queue<WaterTile> builds = new Queue<WaterTile>();

            public void Clear()
            {
                erases.Clear();
                disposes.Clear();
                fills.Clear();
                builds.Clear();
            }
        }
        private WaterQueues waterQueues = new WaterQueues();
        #endregion Members

        #region Accessors
        /// <summary>
        /// Return size of the virtual (aggregate) map. NOTE THAT THIS IS SUBJECT
        /// TO CHANGE. Especially, if during edit the terrain is extended (or shortened),
        /// the size of the virtual map increases (or decreases).
        /// </summary>
        public Point VirtualSize
        {
            get { return new Point(NumMaps.X * PixPerMap, NumMaps.Y * PixPerMap); }
        }
        /// <summary>
        /// Return the size of a horizontal edge of a cube.
        /// </summary>
        public float CubeSize
        {
            get { return mapSize.X / PixPerMap; }
        }
        /// <summary>
        /// The minimum height of terrain, below which it is undefined.
        /// </summary>
        public float MinHeight
        {
            get { return CubeSize * 0.5f; }
        }

        /// <summary>
        /// Minimum position in world space of grid. May not be populated.
        /// May change as terrain is added or removed.
        /// </summary>
        public Vector2 Min
        {
            get { return worldMin; }
            set { worldMin = value; }
        }
        /// <summary>
        /// Max position in world space of grid. May not be populated.
        /// May change as terrain is added or removed.
        /// </summary>
        public Vector2 Max
        {
            get { return worldMax; }
            set { worldMax = value; }
        }

        /// <summary>
        /// Bounds of actual terrain geometry.
        /// </summary>
        public AABB LandBounds
        {
            get { return landBounds; }
            private set { landBounds = value; }
        }

        /// <summary>
        /// The highest water height base in the game. Not updated while raising and lowering.
        /// </summary>
        public float MaxWaterHeight
        {
            get { return maxWaterHeight; }
            private set { maxWaterHeight = value; }
        }

        /// <summary>
        /// Min bounds of renderable terrain geometry.
        /// </summary>
        public Vector3 LandMin
        {
            get { return landBounds.Min; }
        }

        /// <summary>
        /// Max bounds of renderable terrain geometry.
        /// </summary>
        public Vector3 LandMax
        {
            get { return landBounds.Max; }
        }

        /// <summary>
        /// True if there is no terrain data.
        /// </summary>
        public bool Empty
        {
            get { return MapCount == 0; }
        }

        /// <summary>
        /// The southwest corner of a map or tile from its grid coords.
        /// Note that a tile's grid coords will change as tile are added and removed.
        /// It's world space position will not.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public Vector2 TileMin(int i, int j)
        {
            return new Vector2(Min.X + i * MapSize.X, Min.Y + j * MapSize.Y);
        }

        /// <summary>
        /// Find the bounding rectangle of maps for input corners.
        /// These indices may become invalid as soon as a tile is added or removed.
        /// </summary>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <returns></returns>
        public Rectangle MapsTouched(Vector2 lo, Vector2 hi)
        {
            Rectangle rect = new Rectangle(
                (int)Math.Floor((lo.X - Min.X) / MapSize.X),
                (int)Math.Floor((lo.Y - Min.Y) / MapSize.Y),

                ((int)Math.Ceiling((hi.X - Min.X) / MapSize.X)),
                ((int)Math.Ceiling((hi.Y - Min.Y) / MapSize.Y)));

            rect.Width -= rect.X;
            rect.Height -= rect.Y;
            return rect;
        }
        /// <summary>
        /// Used by the Edit tools to notify that there's no point in doing a full
        /// update yet, the terrain/water is still in flux.
        /// </summary>
        public static bool SuppressWaterUpdate
        {
            get { return suppressWaterUpdate; }
            set
            {
                if (ignoreSuppressWaterUpdateThisFrame)
                {
                    value = false;
                    ignoreSuppressWaterUpdateThisFrame = false;
                }
                suppressWaterUpdate = value;
            }
        }
        static bool ignoreSuppressWaterUpdateThisFrame = false;
        // When the water gets to min height, allow an update so that
        // we no longer see it on top of the terrain cubes even if the
        // user still has the button/trigger pressed.
        public static bool IgnoreSuppressWaterUpdateThisFrame
        {
            set { ignoreSuppressWaterUpdateThisFrame = value; }
        }
        internal static void FlushWaterUpdate()
        {
            flushWaterUpdate = true;
        }

        /// <summary>
        /// Return the number of terrain tiles waiting for processing.
        /// </summary>
        internal int TerraQueue
        {
            get { return updates.Count; }
        }

        #region PrivateAccessors
        private HeightMap[,] Maps
        {
            get { return maps; }
            set { maps = value; }
        }
        private Tile[,] Tiles
        {
            get { return tiles; }
            set { tiles = value; }
        }
        private ColorMap[,] ColorMaps
        {
            get { return colorMaps; }
            set { colorMaps = value; }
        }
        private WaterMap[,] WaterMaps
        {
            get { return waterMaps; }
            set { waterMaps = value; }
        }
        private WaterTile[,] WaterTiles
        {
            get { return waterTiles; }
            set { waterTiles = value; }
        }
        private int MapCount
        {
            get { return mapCount; }
            set { mapCount = value; }
        }
        private Point TileIndex(Vector2 pos)
        {
            Debug.Assert(!Empty);
            Point p = new Point((int)((pos.X - Min.X) / (Max.X - Min.X) * (float)NumMaps.X),
                             (int)((pos.Y - Min.Y) / (Max.Y - Min.Y) * (float)NumMaps.Y));
            if (p.X >= NumMaps.X)
            {
                p.X = NumMaps.X - 1;
            }
            if (p.Y >= NumMaps.Y)
            {
                p.Y = NumMaps.Y - 1;
            }
            return p;
        }
        private Point TileIndex(Vector3 pos)
        {
            return TileIndex(new Vector2(pos.X, pos.Y));
        }

        /// <summary>
        /// The size of the grid of maps
        /// </summary>
        private Point NumMaps
        {
            get { return numMaps; }
            set { numMaps = value; }
        }
        /// <summary>
        /// The size of each heightMap in world space units.
        /// </summary>
        private Vector3 MapSize
        {
            get { return mapSize; }
            set { mapSize = value; }
        }
        /// <summary>
        /// The 2D size of each heightMap in world space units.
        /// </summary>
        private Vector2 MapSize2D
        {
            get { return new Vector2(mapSize.X, mapSize.Y); }
        }
        /// <summary>
        /// The size of each heightMap in pixels (each is PixPerMap x PixPerMap)
        /// </summary>
        private const int PixPerMap = 32;

        /// <summary>
        /// Get the next tile to be refreshed, removing from queue.
        /// </summary>
        private Tile NextUpdate
        {
            get { return updates.Count > 0 ? updates.Dequeue() : null; }
        }
        #endregion PrivateAccessors
        #endregion Accessors

        #region Public
        /// <summary>
        /// Constructor. Doesn't do much.
        /// </summary>
        public VirtualMap()
        {
        }

        #region BACKWARD_COMPAT
        /// <summary>
        /// Backward compat converts old heightmap to virtual grid of maps.
        /// </summary>
        /// <param name="heightMap"></param>
        public void InitFromSingle(HeightMap heightMap)
        {
            Point downSamp = new Point(
                (int)Math.Ceiling(heightMap.Size.X / (float)PixPerMap),
                (int)Math.Ceiling(heightMap.Size.Y / (float)PixPerMap));
            MapSize = new Vector3(
                heightMap.Scale.X / (float)Math.Floor(heightMap.Size.X / (float)PixPerMap),
                heightMap.Scale.Y / (float)Math.Floor(heightMap.Size.Y / (float)PixPerMap),
                heightMap.Scale.Z);

            Init(
                Vector2.Zero,
                Vector2.Zero,
                MapSize);

            ExpandMaps(new Vector2(0.0f, 0.0f), new Vector2(heightMap.Scale.X, heightMap.Scale.Y));

            for (int j = 0; j < downSamp.Y; ++j)
            {
                for (int i = 0; i < downSamp.X; ++i)
                {
                    HeightMap h = Extract(heightMap, new Point(i, j));

                    AddMap(h, TileMin(i, j));
                }
            }

            Refresh(Min, Max);
            Update(true);
        }

        /// <summary>
        /// Extract a submap from the heightmap
        /// </summary>
        /// <param name="heightMap"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public HeightMap Extract(HeightMap heightMap, Point pt)
        {
            HeightMap h = NewMap(Vector2.Zero);

            pt.X *= PixPerMap;
            pt.Y *= PixPerMap;

            for (int j = 0; j < PixPerMap; ++j)
            {
                for (int i = 0; i < PixPerMap; ++i)
                {
                    if ((i + pt.X < heightMap.Size.X) && (j + pt.Y < heightMap.Size.Y))
                    {
                        h.SetHeight(i, j, heightMap.GetHeight(i + pt.X, j + pt.Y));
                    }
                }
            }
            return h;
        }
        #endregion BACKWARD_COMPAT

        /// <summary>
        /// Set initial size and scale.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="size"></param>
        public void Init(Vector2 min, Vector2 max, Vector3 size)
        {
            maps = null;
            Min = min;
            Max = max;
            MapSize = size;
            NumMaps = new Point(0, 0);
        }

        private const Int32 version = 3;
        /// <summary>
        /// Save to specified file.
        /// </summary>
        public void Save(string filename)
        {
            Update(true);

            Stream fs = Storage4.OpenWrite(filename);

            // Note:  If the file fails to open the problem may be that the user
            // is blocked from opeing .raw files.  From one email:
            //      We've got to the bottom of the issue we were having.  We use FSRM 
            //      here across the schools with the Microsoft file type list for exclusions, 
            //      and .raw is (was) on the blocked list, so Kodu couldn't write the 
            //      Terrain file in SavedGames.

            /*
            if (fs == null)
            {
                System.Windows.Forms.MessageBox.Show(
                    "User location : " + Storage4.UserLocation + "\nFilename : " + filename,
                    "Error opening terrain file for write.",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Asterisk);
            }
            */

            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(version);

            bw.Write(NumMaps.X);
            bw.Write(NumMaps.Y);
            bw.Write(MapSize.Z);
            bw.Write(Min.X);
            bw.Write(Min.Y);

            for (int i = 0; i < NumMaps.X; i++)
            {
                for (int j = 0; j < NumMaps.Y; j++)
                {
                    bw.Write(Maps[i, j] != null);
                    if (Maps[i, j] != null)
                    {
                        Debug.Assert(ColorMaps[i, j] != null); // map not null but color is?
                        Maps[i, j].Save(bw);
                        ColorMaps[i, j].Save(bw);
                    }
                }
            }

#if NETFX_CORE
            bw.Flush();
            bw.Dispose();
#else
            bw.Close();
#endif
            Storage4.Close(fs);
        }

        /// <summary>
        /// Load from specified file. Cubesize comes from xml.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="cubeSize"></param>
        public void Load(string filename, float cubeSize)
        {
            Stream fs = Storage4.OpenRead(filename, StorageSource.All);

            // Hack to handle missing terrain files.
            // Since we don't have a real terrain file, grab the small patch
            // from New World.
            if (fs == null)
            {
                filename = "Content\\Xml\\Levels\\Stuff\\TerrainHeightMaps\\30e0bd73-fa87-4849-9610-42dfb9ec5403.Raw";
                fs = Storage4.OpenRead(filename, StorageSource.All);
            }
            
            BinaryReader br = new BinaryReader(fs);

            int v = br.ReadInt32();
            Debug.Assert(v <= version);

            //For version 3, we switched to 16-bit material indices
            bool use16BitMatIdx = v < 3 ? false : true;

            numMaps.X = br.ReadInt32();
            numMaps.Y = br.ReadInt32();
            if (v < 1)
            {
                float trash = br.ReadSingle();
                trash = br.ReadSingle();
                cubeSize = trash / PixPerMap;
            }
            mapSize.X = cubeSize * PixPerMap;
            mapSize.Y = cubeSize * PixPerMap;
            mapSize.Z = br.ReadSingle();
            worldMin.X = br.ReadSingle();
            worldMin.Y = br.ReadSingle();
            worldMax.X = worldMin.X + NumMaps.X * MapSize.X;
            worldMax.Y = worldMin.Y + NumMaps.Y * MapSize.Y;

            const float kMaxHeight = 200.0f;

            Terrain.TotalCost = 0;

            Maps = new HeightMap[NumMaps.X, NumMaps.Y];
            Tiles = new Tile[NumMaps.X, NumMaps.Y];
            ColorMaps = new ColorMap[NumMaps.X, NumMaps.Y];
            WaterMaps = new WaterMap[NumMaps.X, NumMaps.Y];
            WaterTiles = new WaterTile[NumMaps.X, NumMaps.Y];

            for (int i = 0; i < NumMaps.X; i++)
            {
                for (int j = 0; j < NumMaps.Y; j++)
                {
                    bool nonNull = br.ReadBoolean();
                    if (nonNull)
                    {
                        Vector2 swCorner = new Vector2(worldMin.X + i * MapSize.X, worldMin.Y + j * MapSize.Y);
                        HeightMap map = new HeightMap(
                            swCorner,
                            new Point(PixPerMap, PixPerMap),
                            MapSize);

                        map.Load(br);

                        if (v < 2)
                        {
                            map.RescaleZ(kMaxHeight);
                        }

                        Maps[i, j] = map;

                        ColorMap color = new ColorMap(PixPerMap);
                        color.Load(br, use16BitMatIdx);

                        ColorMaps[i, j] = color;

                        WaterMaps[i, j] = new WaterMap(PixPerMap);

                        Vector2 neCorner = swCorner + new Vector2(mapSize.X, mapSize.Y);
                        WaterTiles[i, j] = new WaterTile(swCorner, neCorner);

                        Tiles[i, j] = new Tile(swCorner, neCorner);

                        AddCost(map);

                        // HACK -- We've seen a couple of instances where terrain gets deleted but
                        // it's not really deleted.  In particular the material value is set to 
                        // TerrainMaterial.EmptyMatIdx but the height doesn't get reset to 0.  So,
                        // here we look though the color map and for every square marked empty we
                        // ensure that the height is 0.
                        for (int jj = 0; jj < PixPerMap; jj++)
                        {
                            for (int ii = 0; ii < PixPerMap; ii++)
                            {
                                if (color[ii, jj] == TerrainMaterial.EmptyMatIdx)
                                {
                                    map.SetHeight(ii, jj, 0.0f);
                                }
                            }
                        }


                        ++MapCount;
                    }
                }
            }

#if NETFX_CORE
            br.Dispose();
#else
            br.Close();
#endif
            fs.Dispose();

            mapSize.Z = kMaxHeight;
            Refresh(Min, Max);
            Update(true);
        }   // end of Load()

        private void AddCost(HeightMap map)
        {
            float cost = Terrain.CostPerVertex * 4.0f;
            for (int i = 0; i < map.Size.X; ++i)
            {
                for (int j = 0; j < map.Size.Y; ++j)
                {
                    if (map.GetHeight(i, j) > 0)
                    {
                        Terrain.TotalCost += cost;
                    }
                }
            }
        }

        /// <summary>
        /// Force a rebuild of all geometry.
        /// </summary>
        public void RebuildAll()
        {
            RebuildAllWater();
            Refresh(Min, Max);
            Update(true);
        }

        /// <summary>
        /// Refresh all tiles in the specified world space region.
        /// </summary>
        /// <param name="worldMin"></param>
        /// <param name="worldMax"></param>
        public void Refresh(Vector2 worldMin, Vector2 worldMax)
        {
            // Use the heightMap grid to update the renderable data.
            Rectangle tileRect = MapsTouched(
                worldMin,
                worldMax);

            Refresh(tileRect);
        }

        /// <summary>
        /// Refresh the tiles from the updated heightmaps within the input rect.
        /// </summary>
        /// <param name="tileRect"></param>
        public void Refresh(Rectangle tileRect)
        {
            for (int j = tileRect.Y; j < tileRect.Y + tileRect.Height; ++j)
            {
                for (int i = tileRect.X; i < tileRect.X + tileRect.Width; ++i)
                {
                    if (Tiles[i, j] != null)
                    {
                        AddUpdate(Tiles[i, j]);
                    }
                }
            }
        }

        /// <summary>
        /// Really do a single refresh on any tiles queued up for refresh.
        /// </summary>
        public void Update(bool doAll)
        {
            bool updated = false;
            const int MaxUpdates = 1;
            int maxUpdates = Tiles != null ? Tiles.Length : 0;
            maxUpdates = !doAll && (maxUpdates > MaxUpdates) ? MaxUpdates : maxUpdates;
            int cnt = 0;
            while (cnt < maxUpdates)
            {
                Tile tile = NextUpdate;
                if (tile == null)
                    break;

                updated = true;
                Point coord = WorldToMapIndex((tile.Min + tile.Max) * 0.5f);
                if (!tile.MakeGeometry(
                    GetHeightNeighborhood(coord.X, coord.Y),
                    GetColorNeighborhood(coord.X, coord.Y)))
                {
                    DeleteMap(coord.X, coord.Y);
                    ++deadMaps;
                }
                else
                {
                    UpdateMapBound(
                        coord.X,
                        coord.Y,
                        tile.Bounds.MinZ,
                        tile.Bounds.MaxZ);
                    tile.Queued = false;
                }
                ++cnt;
            }
            if (deadMaps > maxDeadMaps)
            {
                CleanDeadMaps();
            }
            if (doAll || updated)
            {
                UpdateMaterialUsage();
            }

            if (!SuppressWaterUpdate || flushWaterUpdate)
            {
                UpdateWater(doAll || flushWaterUpdate);
            }
        }

        public void CullCheck(Camera camera)
        {
            //nothing to cull if no terrain
            if (tiles == null) return;

            foreach (Tile tile in tiles)
            {
                if (tile != null)
                {
                    tile.CullCheck(camera);
                }
            }
        }


        /// <summary>
        /// Render all cubes with given material from given angle(face).
        /// </summary>
        public void Render_FD(GraphicsDevice device, Camera camera, ushort matIdx, bool doSides)
        {
            for (int i = 0; i < NumMaps.X; ++i)
                for (int j = 0; j < NumMaps.Y; ++j)
                {
                    Tile tile = tiles[i, j];
                    if ((tile != null) && !tile.Culled)
                        tile.Render_FD(device, camera, matIdx, doSides);
                }
        }

        /// <summary>
        /// Render all cubes with given material from given angle(face).
        /// </summary>
        public void Render_FA(GraphicsDevice device, Camera camera, ushort matIdx)
        {
            for (int i = 0; i < NumMaps.X; ++i)
                for (int j = 0; j < NumMaps.Y; ++j)
                {
                    Tile tile = tiles[i, j];
                    if ((tile != null) && !tile.Culled)
                        tile.Render_FA(device, camera, matIdx);
                }
        }


        /// <summary>
        /// Load up device dependent resources.
        /// </summary>
        /// <param name="graphics"></param>
        public void LoadContent(bool immediate)
        {
            LoadWaterEffect();

            WaterTile.LoadContent(immediate);
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            WaterTile.InitDeviceResources(device);
        }

        /// <summary>
        /// Let go of any device dependent resources.
        /// </summary>
        public void UnloadContent()
        {
            DisposeAll();
            UnloadWaterEffect();

            WaterTile.UnloadContent();
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            WaterTile.DeviceReset(device);
        }

        /// <summary>
        /// Expand the virtual grid to include the rectangle input.
        /// Doesn't fill in with heightMaps, just expands (nulled) grid.
        /// Returns true if anything changed.
        /// </summary>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        /// <returns></returns>
        public bool ExpandMaps(Vector2 lo, Vector2 hi)
        {
            /// Save off initial state to rebuild into new grid.
            HeightMap[,] oldMaps = Maps;
            if (oldMaps == null)
            {
                Min = lo;
                Max = hi;
            }
            Vector2 oldMin = Min;
            Point oldNumMaps = NumMaps;
            Tile[,] oldTiles = Tiles;
            ColorMap[,] oldColors = ColorMaps;
            WaterMap[,] oldWaterMaps = WaterMaps;
            WaterTile[,] oldWaterTiles = WaterTiles;

            // first off, round lo down and hi up to boundaries
            Min = Vector2.Min(Min, lo);
            Min = new Vector2(
                (float)(Math.Floor(Min.X / MapSize.X) * MapSize.X),
                (float)(Math.Floor(Min.Y / MapSize.Y) * MapSize.Y));

            Max = Vector2.Max(Max, hi);
            Max = new Vector2(
                (float)(Math.Ceiling(Max.X / MapSize.X) * MapSize.X),
                (float)(Math.Ceiling(Max.Y / MapSize.Y) * MapSize.Y));

            Point del = new Point(
                (int)Math.Round((oldMin.X - Min.X) / MapSize.X),
                (int)Math.Round((oldMin.Y - Min.Y) / MapSize.Y)
            );

            NumMaps = new Point(
                (int)Math.Round((Max.X - Min.X) / MapSize.X),
                (int)Math.Round((Max.Y - Min.Y) / MapSize.Y));

            if (NumMaps != oldNumMaps)
            {
                Tiles = new Tile[NumMaps.X, NumMaps.Y];

                Maps = new HeightMap[NumMaps.X, NumMaps.Y];

                ColorMaps = new ColorMap[NumMaps.X, NumMaps.Y];

                WaterMaps = new WaterMap[NumMaps.X, NumMaps.Y];

                WaterTiles = new WaterTile[NumMaps.X, NumMaps.Y];

                if (oldMaps != null)
                {
                    Debug.Assert(oldTiles != null); // maps not null but tiles is? 
                    Debug.Assert(oldColors != null); // maps not null but colors are?
                    Debug.Assert(oldWaterMaps != null); // maps not null but water maps are?
                    Debug.Assert(oldWaterTiles != null); // maps not null but water tiles are?
                    for (int j = 0; j < oldNumMaps.Y; ++j)
                    {
                        for (int i = 0; i < oldNumMaps.X; ++i)
                        {
                            Tiles[i + del.X, j + del.Y] = oldTiles[i, j];
                            Maps[i + del.X, j + del.Y] = oldMaps[i, j];
                            ColorMaps[i + del.X, j + del.Y] = oldColors[i, j];
                            WaterMaps[i + del.X, j + del.Y] = oldWaterMaps[i, j];
                            WaterTiles[i + del.X, j + del.Y] = oldWaterTiles[i, j];
                        }
                    }
                }
                return true; // Expanded.
            }
            return false; // Didn't do anything.
        }

        /// <summary>
        /// Make sure there's a tile/map at specified tile coordinate.
        /// </summary>
        /// <param name="mapIdx"></param>
        public void EnsureMap(Point mapIdx)
        {
            if ((Maps == null) || (Maps[mapIdx.X, mapIdx.Y] == null))
            {
                Vector2 swCorner = TileMin(mapIdx.X, mapIdx.Y);

                AddMap(NewMap(swCorner), swCorner);
            }
        }

        /// <summary>
        /// Returns an ordered list of maps touched by the segment from p0 to p1,
        /// with p0's map first and p1's map last.
        /// Return whether any non-empty map were found.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="heightMaps"></param>
        /// <returns></returns>
        public bool ListMaps(Vector2 p0, Vector2 p1, List<HeightMap> heightMaps)
        {
            Vector2 dir = new Vector2(p1.X - p0.X, p1.Y - p0.Y);
            if (Math.Abs(dir.X) >= Math.Abs(dir.Y))
            {
                if (dir.X < 0.0f)
                {
                    dir.Y /= dir.X;
                    dir.X = 1.0f;
                    ListMapsX(p1, p0, dir, heightMaps);
                    FlipOrder(heightMaps);
                }
                else
                {
                    dir.Y /= dir.X;
                    dir.X = 1.0f;
                    ListMapsX(p0, p1, dir, heightMaps);
                }
            }
            else
            {
                if (dir.Y < 0.0f)
                {
                    dir.X /= dir.Y;
                    dir.Y = 1.0f;
                    ListMapsY(p1, p0, dir, heightMaps);
                    FlipOrder(heightMaps);
                }
                else
                {
                    dir.X /= dir.Y;
                    dir.Y = 1.0f;
                    ListMapsY(p0, p1, dir, heightMaps);
                }
            }
            return heightMaps.Count > 0;
        }

        /// <summary>
        /// Perform LOS check from p0 to p1. Return true if ray hits terrain.
        /// If true, return distance from p0 to hit point in hitDist. 
        /// If not true, hitDist indeterminate.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="hitDist"></param>
        /// <returns></returns>
        public bool LOSCheck(Vector3 p0, Vector3 p1, ref Vector3 hitPoint)
        {
            losCheckHeightMaps.Clear();

            Ray ray = new Ray(p0, p1 - p0);
            // Get the distance between the points and use 
            // that to normalize the direction vector.
            float distance = ray.Direction.Length();

            // Handle degenerate case where we're testing a point against itself.
            if (distance == 0)
            {
                return false;
            }

            ray.Direction /= distance;

            ListMaps(new Vector2(p0.X, p0.Y), new Vector2(p1.X, p1.Y), losCheckHeightMaps);

            bool hit = false;
            for (int i = 0; i < losCheckHeightMaps.Count; ++i)
            {
                Ray subRay = ray;
                if (losCheckHeightMaps[i].Intersect(subRay, distance, ref hitPoint))
                {
                    hit = true;
                    break;
                }
            }

            return hit;
        }
        #region Heap locals
        List<HeightMap> losCheckHeightMaps = new List<HeightMap>();
        #endregion

        /// <summary>
        /// Checks to see if the actor can validly continue moving.
        /// </summary>
        /// <param name="hw">Terrain height and water height looking forward to the next position.</param>
        /// <param name="prevH">Terrain height and water height where the actor currently is.</param>
        /// <param name="minMaxH">Min/max allowable heights.  No clue why.</param>
        /// <param name="maxStep">Max positive step height actor can surmount.</param>
        /// <param name="curActorHeight">Actor's current height.</param>
        /// <returns></returns>
        private bool CheckHit(Vector2 hw, Vector2 prevH, Vector2 minMaxH, Vector4 maxStep, float curActorHeight)
        {
            if (hw.X <= minMaxH.X)
                return true;
            if (hw.X > minMaxH.Y)
                return true;
            
            // step is the height difference from the land height in hw (where we want to go)
            // compared to prevH.X which is the land height where we currently are.
            // The problem is that this doesn't account for roads which can raise the height
            // of a character.  Probably a better definition of step would be the difference
            // from where we currently are (actor's height) to hw.X.  This allows roads to be 
            // used as bridges across canyons.
            float step = hw.X - prevH.X;
            step = hw.X - curActorHeight;
            if (step > maxStep.X)
                return true; // too high to climb

            /*
            // No height to be too high to fall from.
            if (step < maxStep.Y)
                return true; // too far to fall
            */
                        
            /* 
            // TODO (****)
            // Should land/water transition stop actors?  Or should this
            // be based on actor's domain???
              
            if ((prevH.Y <= maxStep.Z) && (hw.Y > maxStep.Z))
                return true; // transited land to water
            if ((prevH.Y > maxStep.W) && (hw.Y <= maxStep.W))
                return true; // transited water to land
            */

            return false;
        }
        /// <summary>
        /// Test for end of world or cliff traversing from p0 to p1.
        /// Relevant info returned in hitBlock.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="minMaxH"></param>
        /// <param name="hitBlock"></param>
        /// <returns></returns>
        public bool Blocked(Vector3 p0, Vector3 p1, Vector2 minMaxH, Vector4 maxStep, ref Terrain.HitBlock hitBlock, float curActorHeight)
        {
            if ((p0 - p1) == Vector3.Zero)
            {
                // No movement, not blocked.
                return false;
            }

            Stepper stepper = Stepper.Select(this, p0, p1);

            Vector2 hw = GetHeightAndWater(stepper.X, stepper.Y);
            hitBlock.Min = hitBlock.Max = hw.X;
            hitBlock.MaxWater = hw.Y;
            hitBlock.LoStep = hitBlock.HiStep = 0.0f;
            hitBlock.LandStart = hw.Y <= 0.0f;
            if ((hw.X <= minMaxH.X) || (hw.X > minMaxH.Y))
            {
                /// We started right in satisfying the termination condition.
                hitBlock.Position = p0;
                hitBlock.BlockHeight = hw.X;

                // We hit the side of some terrain so need to return a reasonable normal?
                //hitBlock.Normal = new Vector3(0, -1, 0);

                return true;
            }
            if (stepper.Empty())
            {
                return false;
            }
            stepper.StepFirst();
            Vector2 prevH = hw;
            do
            {
                if (stepper.StepMinor())
                {
                    Point coord = stepper.MinorCoord;
                    hw = GetHeightAndWater(coord.X, coord.Y);
                    hitBlock.Absorb(hw, prevH, stepper.HitMinor);
                    if (CheckHit(hw, prevH, minMaxH, maxStep, curActorHeight))
                    {
                        hitBlock.Position = stepper.HitMinor;
                        hitBlock.Normal = stepper.NormMinor;
                        hitBlock.BlockHeight = hw.X;

                        return true;
                    }
                    if (coord == stepper.End)
                    {
                        return false;
                    }
                    prevH = hw;
                }
                hw = GetHeightAndWater(stepper.Coord.X, stepper.Coord.Y);
                hitBlock.Absorb(hw, prevH, stepper.HitMajor);
                if (CheckHit(hw, prevH, minMaxH, maxStep, curActorHeight))
                {
                    hitBlock.Position = stepper.HitMajor;
                    hitBlock.Normal = stepper.NormMajor;
                    hitBlock.BlockHeight = hw.X;

                    return true;
                }
                prevH = hw;
            }
            while (stepper.StepMajor());

            if (!stepper.DidEnd())
            {
                stepper.SetToEnd();
                hw = GetHeightAndWater(stepper.X, stepper.Y);
                hitBlock.Absorb(hw, prevH, stepper.HitMinor);
                if (CheckHit(hw, prevH, minMaxH, maxStep, curActorHeight))
                {
                    hitBlock.Position = stepper.HitMinor;
                    hitBlock.Normal = stepper.NormMinor;
                    hitBlock.BlockHeight = hw.X;

                    return true;
                }
                prevH = hw;
            }

            return false;
        }

        #region Queries

        /// <summary>
        /// Get height of input virtual pixel. i and j in Virtual pixel units.
        /// You probably don't want to use this, you probably want to use the
        /// Vector2 (worldSpace) variants.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public float GetHeight(int i, int j)
        {
            Point coord;
            HeightMap heightMap = VirtualToMap(i, j, out coord);
            if (heightMap == null)
            {
                return 0.0f;
            }

            return heightMap.GetHeight(coord.X, coord.Y);
        }
        /// <summary>
        /// Return height of input vitual pixel, with i and j in global Virtual pixel units.
        /// Also returns terrain and water material info for that spot.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="matInfo"></param>
        /// <returns></returns>
        public float GetHeightAndMaterial(int i, int j, out Terrain.MaterialInfo matInfo)
        {
            float height = 0.0f;
            matInfo = Terrain.MaterialInfo.InvalidInfo;
            Point coord;
            Point mapIdx;
            if (VirtualToCoord(i, j, out mapIdx, out coord))
            {
                HeightMap heightMap = Maps[mapIdx.X, mapIdx.Y];
                if (heightMap != null)
                {
                    height = heightMap.GetHeight(coord.X, coord.Y);

                    if (height > 0.0f)
                    {

                        Debug.Assert(WaterMaps[mapIdx.X, mapIdx.Y] != null, "Missing water map for valid terrain location");
                        Debug.Assert(ColorMaps[mapIdx.X, mapIdx.Y] != null, "Missing material map for valid terrain location");

                        WaterMap waterMap = WaterMaps[mapIdx.X, mapIdx.Y];

                        if (waterMap[coord.X, coord.Y] != Water.InvalidLabel)
                        {
                            Water water = Water.FromLabel(waterMap[coord.X, coord.Y]);
                            matInfo.WaterType = water.Type;
                        }

                        ColorMap colorMap = ColorMaps[mapIdx.X, mapIdx.Y];
                        ushort terrainMat = colorMap[coord.X, coord.Y];
                        matInfo.IsFabric = (terrainMat & (int)TerrainMaterial.Flags.Fabric) != 0;
                        terrainMat = TerrainMaterial.GetNonFabric(terrainMat);
                        terrainMat = TerrainMaterial.RemoveFlags(terrainMat, TerrainMaterial.Flags.Selection);

                        //Debug.Assert(TerrainMaterial.IsValid(terrainMat));

                        matInfo.TerrainType = terrainMat;
                    }
                }
            }
            return height;
        }

        public Vector2 GetHeightAndWater(int i, int j)
        {
            Vector2 ret = Vector2.Zero;
            Point coord;
            Point mapIdx;
            if (VirtualToCoord(i, j, out mapIdx, out coord))
            {
                HeightMap heightMap = Maps[mapIdx.X, mapIdx.Y];
                if (heightMap != null)
                {
                    ret.X = heightMap.GetHeight(coord.X, coord.Y);

                    WaterMap waterMap = WaterMaps[mapIdx.X, mapIdx.Y];

                    if (waterMap[coord.X, coord.Y] != Water.InvalidLabel)
                    {
                        Water water = Water.FromLabel(waterMap[coord.X, coord.Y]);
                        ret.Y = water.BaseHeight;
                    }
                    else
                    {
                        ret.Y = 0.0f;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Get the normal at the given virtual coordinate.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public Vector3 GetNormal(int i, int j)
        {
            float center = GetHeight(i, j);
            if (center <= 0.0f)
            {
                return Vector3.UnitZ;
            }
            float north = GetHeight(i, j + 1);
            north = north > 0.0f ? north : center;
            float south = GetHeight(i, j - 1);
            south = south > 0.0f ? south : center;
            float east = GetHeight(i + 1, j);
            east = east > 0.0f ? east : center;
            float west = GetHeight(i - 1, j);
            west = west > 0.0f ? west : center;

            Vector3 normal = new Vector3(
                west - east,
                south - north,
                2.0f * CubeSize);

            return Vector3.Normalize(normal);
        }

        /// <summary>
        /// Get the maximum sample point from the 4 surrounding this position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public float GetMaxHeight(Vector2 pos)
        {
            Point coord = WorldToVirtualIndex(pos);
            float h00 = GetHeight(coord.X, coord.Y);
            float h10 = GetHeight(coord.X + 1, coord.Y);
            float h01 = GetHeight(coord.X, coord.Y + 1);
            float h11 = GetHeight(coord.X + 1, coord.Y + 1);

            return Math.Max(h00, Math.Max(h10, Math.Max(h01, h11)));
        }
        /// <summary>
        /// Return the unsmoothed uninterpolated height (cube height).
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public float GetHeightFlat(Vector2 pos)
        {
            Point coord = WorldToVirtualIndex(pos);
            return GetHeight(coord.X, coord.Y);
        }
        /// <summary>
        /// Return uninterpolated height and terrain and water material info for that point.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="matInfo"></param>
        /// <returns></returns>
        public float GetHeightAndMaterial(Vector2 pos, out Terrain.MaterialInfo matInfo)
        {
            Point coord = WorldToVirtualIndex(pos);
            return GetHeightAndMaterial(coord.X, coord.Y, out matInfo);
        }

        /// <summary>
        /// Return the height for a given position in space (smoothed).
        /// Undefined space has height of 0.0f.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public float GetHeight(Vector2 pos)
        {
            ///*** - this is clean and works, but it returns the flattened
            ///cubeworld height. That may be what we want, but for now we're going
            ///with the interpolated smoothed height.
            //Point ipos = WorldToVirtualIndex(pos);
            //return GetHeight(ipos.X, ipos.Y);

            Vector2 frac;
            Point coord = WorldToVirtualCoord(
                pos - new Vector2(CubeSize * 0.5f, CubeSize * 0.5f),
                out frac);
            float h00 = GetHeight(coord.X, coord.Y);
            float h10 = GetHeight(coord.X + 1, coord.Y);
            float h01 = GetHeight(coord.X, coord.Y + 1);
            float h11 = GetHeight(coord.X + 1, coord.Y + 1);

            return h00 * (1.0f - frac.X) * (1.0f - frac.Y)
                + h10 * (0.0f + frac.X) * (1.0f - frac.Y)
                + h01 * (1.0f - frac.X) * (0.0f + frac.Y)
                + h11 * (0.0f + frac.X) * (0.0f + frac.Y);
        }

        /// <summary>
        /// Return height at position in space. Z of position ignored.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public float GetHeight(Vector3 pos)
        {
            return GetHeight(new Vector2(pos.X, pos.Y));
        }

        /// <summary>
        /// Use the xy position in worldPos to fill out the height
        /// and normal.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="worldNormal"></param>
        /// <returns>Whether there be land there.</returns>
        public bool GetHeightAndNormal(Vector2 inPos, out Vector3 worldPos, out Vector3 worldNormal)
        {
            Point coord = WorldToVirtualIndex(inPos);

            worldPos = new Vector3(inPos.X, inPos.Y, GetHeight(coord.X, coord.Y));
            worldNormal = GetNormal(coord.X, coord.Y);

            return worldPos.Z > 0.0f;
        }

        /// <summary>
        /// Return the smoothed normal for a position in space. Undefined space has normal 
        /// pointed straight up.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vector3 GetNormal(Vector2 pos)
        {
            Vector2 frac;
            Point coord = WorldToVirtualCoord(
                pos - new Vector2(CubeSize * 0.5f, CubeSize * 0.5f),
                out frac);
            Vector3 n00 = GetNormal(coord.X, coord.Y);
            Vector3 n10 = GetNormal(coord.X + 1, coord.Y);
            Vector3 n01 = GetNormal(coord.X, coord.Y + 1);
            Vector3 n11 = GetNormal(coord.X + 1, coord.Y + 1);

            return n00 * (1.0f - frac.X) * (1.0f - frac.Y)
                + n10 * (0.0f + frac.X) * (1.0f - frac.Y)
                + n01 * (1.0f - frac.X) * (0.0f + frac.Y)
                + n11 * (0.0f + frac.X) * (0.0f + frac.Y);
        }

        /// <summary>
        /// Return smoothed normal at position. Z of position ignored.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vector3 GetNormal(Vector3 pos)
        {
            return GetNormal(new Vector2(pos.X, pos.Y));
        }

        /// <summary>
        /// Set the height of the given virtual pixel. Out of range indices ignored.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="h"></param>
        public void SetHeight(int i, int j, float h)
        {
            Point mapIdx;
            Point coord;
            if (VirtualToCoord(i, j, out mapIdx, out coord))
            {
                HeightMap heightMap = Maps[mapIdx.X, mapIdx.Y];
                if (heightMap != null)
                {
                    float oldHeight = heightMap.GetHeight(coord.X, coord.Y);
                    if (oldHeight != h)
                    {
                        if ((oldHeight <= 0) && (h > 0))
                        {
                            /// Adding terrain
                            Terrain.TotalCost += Terrain.CostPerVertex * 4.0f;
                        }
                        else if ((oldHeight > 0) && (h <= 0))
                        {
                            /// Removing terrain
                            Terrain.TotalCost -= Terrain.CostPerVertex * 4.0f;
                        }
                        heightMap.SetHeight(coord.X, coord.Y, h);

                        AddUpdate(mapIdx, coord);
                        AddWaterUpdate(mapIdx, coord, oldHeight, h);
                    }
                }
            }
        }

        /// <summary>
        /// Get the material index at the specified virtual coordinate.
        /// If the virtual pixel doesn't exist, defaultIdx is returned.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="defaultIdx"></param>
        /// <returns></returns>
        public ushort GetColor(int i, int j, ushort defaultIdx)
        {
            Point coord;
            ColorMap colorMap = VirtualToColorMap(i, j, out coord);
            if (colorMap != null)
            {
                return colorMap[coord.X, coord.Y];
            }
            return defaultIdx;
        }

        /// <summary>
        /// Set the material index at the specified virtual coordinate.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="matIdx"></param>
        public void SetColor(int i, int j, ushort matIdx)
        {
            Point mapIdx;
            Point coord;
            if (VirtualToCoord(i, j, out mapIdx, out coord))
            {
                ColorMap colorMap = ColorMaps[mapIdx.X, mapIdx.Y];
                if (colorMap != null)
                {
                    if (colorMap[coord.X, coord.Y] != (ushort)matIdx)
                    {
                        colorMap[coord.X, coord.Y] = (ushort)matIdx;
                        AddUpdate(Tiles[mapIdx.X, mapIdx.Y]);
                    }
                }
            }
        }

        #endregion Queries

        #region ConvenienceStruct

        /// <summary>
        /// Helper struct that encapulates both which map and where in the map.
        /// </summary>
        public struct MapCoord
        {
            /// <summary>
            /// Which map
            /// </summary>
            public Point mapIdx;
            /// <summary>
            /// Where in the map
            /// </summary>
            public Point coord;

            public MapCoord(MapCoord mc)
            {
                mapIdx = mc.mapIdx;
                coord = mc.coord;
            }

            public MapCoord(Point mapIdx, Point coord)
            {
                this.mapIdx = mapIdx;
                this.coord = coord;
            }
        }

        #endregion ConvenienceStruct

        #endregion Public

        #region Water

        #region WaterIteration
        /// <summary>
        /// Helper class for iterating through the "wet" cubes in the world.
        /// </summary>
        public class WaterIterator
        {
            #region MEMBERS
            private VirtualMap vMap = null;
            private MapCoord currCoord = new MapCoord();
            #endregion MEMBERS

            #region PUBLIC
            public WaterIterator(VirtualMap vMap)
            {
                this.vMap = vMap;
                Begin();
            }

            /// <summary>
            /// Prepare to iterate. Can be used to reset for multiple iterations.
            /// </summary>
            /// <returns></returns>
            public bool Begin()
            {
                currCoord = new MapCoord(new Point(0, 0), new Point(0, 0));
                if (NotWater())
                {
                    Next();
                }
                return More();
            }

            /// <summary>
            /// Return info on the current sample.
            /// </summary>
            /// <param name="pos"></param>
            /// <param name="waterHeight"></param>
            /// <param name="terrainHeight"></param>
            public void Current(ref Vector2 pos, ref float waterHeight, ref float terrainHeight)
            {
                pos = vMap.MapCoordToWorld(currCoord);
                waterHeight = Water.FromLabel(
                    vMap.WaterMaps[currCoord.mapIdx.X, currCoord.mapIdx.Y]
                                    [currCoord.coord.X, currCoord.coord.Y]).BaseHeight;
                terrainHeight = vMap.Maps[currCoord.mapIdx.X, currCoord.mapIdx.Y].GetHeight(
                                            currCoord.coord.X, currCoord.coord.Y);
            }

            /// <summary>
            /// True if there is still a sample to be processed.
            /// </summary>
            /// <returns></returns>
            public bool More()
            {
                return !NotWater();
            }

            /// <summary>
            /// Advance to next "wet" cube (if any).
            /// </summary>
            /// <returns></returns>
            public bool Next()
            {
                do
                {
                    if (++currCoord.coord.X >= PixPerMap)
                    {
                        currCoord.coord.X = 0;
                        if (++currCoord.mapIdx.X >= vMap.NumMaps.X)
                        {
                            currCoord.mapIdx.X = 0;

                            if (++currCoord.coord.Y >= PixPerMap)
                            {
                                currCoord.coord.Y = 0;
                                ++currCoord.mapIdx.Y;
                            }
                        }
                        if (currCoord.mapIdx.Y >= vMap.NumMaps.Y)
                        {
                            return false;
                        }
                    }
                }
                while (NotWater());

                return true;
            }

            #endregion PUBLIC

            #region INTERNAL
            private bool NotWater()
            {
                if (!vMap.ValidMapIndex(currCoord.mapIdx))
                {
                    return true;
                }
                WaterMap waterMap = vMap.WaterMaps[currCoord.mapIdx.X, currCoord.mapIdx.Y];
                return (waterMap == null)
                    || (waterMap[currCoord.coord.X, currCoord.coord.Y] == Water.InvalidLabel);
            }
            #endregion INTERNAL
        }
        #endregion WaterIteration

        #region Public
        /// <summary>
        ///  Init all the water bodies in this xml data array.
        /// </summary>
        /// <param name="waters"></param>
        public void InitWater(XmlWaterData[] waters)
        {
            Water.Flush();
            if (waters != null)
            {
                foreach (XmlWaterData waterData in waters)
                {
                    Vector3 seedPos = waterData.SeedPosition;
                    CreateWater(new Vector2(seedPos.X, seedPos.Y), seedPos.Z, waterData);
                }
            }
            UpdateWater(true);
        }

        /// <summary>
        /// Resets the height and material for each water source.
        /// Used when coming back from run mode into edit mode.
        /// </summary>
        public static void ResetWater()
        {
            foreach (Water water in Water.AllWaters)
            {
                water.Reset();
            }

        }   // end of ResetWater()

        /// <summary>
        /// Setup to render a face of a water cube. Assumes technique is single pass.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="baseHeight"></param>
        /// <param name="type"></param>
        /// <param name="face"></param>
        /// <param name="localToWorld"></param>
        /// <param name="camera"></param>
        public void PreRenderWaterCube(
            GraphicsDevice device,
            float baseHeight,
            int type,
            int face,
            Matrix localToWorld,
            Camera camera)
        {

            SetupWaterEffect(camera, localToWorld, false);
            SetupWaterEffect(Water.Types[type], baseHeight);
            SetupWaterFaceEffect(face);

            EffectPass pass = effect.CurrentTechnique.Passes[0];
            pass.Apply();

        }

        /// <summary>
        /// Finish up render of water cube face.
        /// </summary>
        public void PostRenderWaterCube()
        {
            EffectPass pass = effect.CurrentTechnique.Passes[0];

            EndWaterEffect();
        }

        /// <summary>
        /// Render the water. Effects true means it's the depth pass.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="camera"></param>
        /// <param name="effects"></param>
        public void RenderWater(GraphicsDevice device, Camera camera, bool effects)
        {
            /// Cull test the water upfront, just once, since it will be the same
            /// result for each face set.
            if (!CullTestWater(camera))
                return;

            /// Set common state
            SetupWaterEffect(camera, Matrix.Identity, effects);

            /// Enable depth buffering. We disable it when rendering water cubes
            /// for the UI.
            BokuGame.bokuGame.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            foreach (Water water in Water.AllWaters)
            {
                if (water.TileCenters != null)
                {
                    SetupWaterEffect(water.Define, water.BaseHeight);

                    int[] faces = OrderWaterFaces(camera, water);

                    /// For each facing direction
                    for (int faceIdx = 0; faceIdx < Tile.NumFaces; ++faceIdx)
                    {
                        int face = faces[faceIdx];

                        /// Set state for this direction
                        SetupWaterFaceEffect(face);

                        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();

                            foreach (Vector2 center in water.TileCenters)
                            {
                                RenderWater(device, water.Label, center);
                            }
                        }
                    }

                    EndWaterEffect();
                }
            }
        }

        /// <summary>
        /// Render the water (if any) at given tile location with given label.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="center"></param>
        public void RenderWater(GraphicsDevice device, int label, Vector2 center)
        {
            Point mapIdx = WorldToMapIndex(center);

            /// We need this check for valid map index, because some of the tiles
            /// under this body of water may have been deleted, but the water hasn't
            /// been reprocessed yet because the action is ongoing. Specifically,
            /// someone might be holding the delete terrain trigger down even though
            /// the terrain has been deleted.
            if (ValidMapIndex(mapIdx))
            {
                WaterTile waterTile = WaterTiles[mapIdx.X, mapIdx.Y];
                if ((waterTile != null) && !waterTile.Culled)
                {
                    waterTile.Render(device, label);
                }
            }
        }

        /// <summary>
        /// Return water body at given world position. May be null.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public Water GetWater(Vector2 pos)
        {
            Point virtCoord = WorldToVirtualIndex(pos);

            return GetWater(virtCoord);
        }

        /// <summary>
        /// Return water body at given virtual coordinate. May be null.
        /// </summary>
        /// <param name="virtCoord"></param>
        /// <returns></returns>
        public Water GetWater(Point virtCoord)
        {
            Point coord;
            Point mapIdx;
            if (VirtualToCoord(virtCoord.X, virtCoord.Y, out mapIdx, out coord))
            {
                WaterMap waterMap = WaterMaps[mapIdx.X, mapIdx.Y];

                if ((waterMap != null)
                    && (waterMap[coord.X, coord.Y] != Water.InvalidLabel))
                {
                    Water water = Water.FromLabel(waterMap[coord.X, coord.Y]);
                    Debug.Assert(water != null);
                    return water;
                }
            }
            return null;
        }

        /// <summary>
        /// Return whether there's water covering this spot.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsWater(Vector2 pos)
        {
            Point virtCoord = WorldToVirtualIndex(pos);

            Point coord;
            Point mapIdx;
            if (VirtualToCoord(virtCoord.X, virtCoord.Y, out mapIdx, out coord))
            {
                WaterMap waterMap = WaterMaps[mapIdx.X, mapIdx.Y];

                if ((waterMap != null)
                    && (waterMap[coord.X, coord.Y] != Water.InvalidLabel))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Erase any body of water covering position pos.
        /// </summary>
        /// <param name="pos"></param>
        public void EraseWater(Vector2 pos)
        {
            Point virtCoord = WorldToVirtualIndex(pos);

            Point coord;
            Point mapIdx;
            if (VirtualToCoord(virtCoord.X, virtCoord.Y, out mapIdx, out coord))
            {
                WaterMap waterMap = WaterMaps[mapIdx.X, mapIdx.Y];
                if ((waterMap != null)
                    && (waterMap[coord.X, coord.Y] != Water.InvalidLabel))
                {
                    AddForDelete(Water.FromLabel(waterMap[coord.X, coord.Y]));
                }
            }
        }

        /// <summary>
        /// Create a body of water containing pos with water level h.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="h"></param>
        public Water CreateWater(Vector2 pos, float h)
        {
            /// We'll get around to it later.
            Water water = Water.Create(pos, h);
            AddForCreate(water);

            return water;
        }

        /// <summary>
        /// Create a body of water with a specific tint.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="h"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public Water CreateWater(Vector2 pos, float h, XmlWaterData waterData)
        {
            Water water = CreateWater(pos, h);
            water.Init(waterData);

            return water;
        }

        /// <summary>
        /// Has all the water been processed, or is there a backlog?
        /// </summary>
        public bool WaterBacklog
        {
            get
            {
                return waterQueues.builds.Count
                      + waterQueues.disposes.Count
                      + waterQueues.erases.Count
                      + waterQueues.fills.Count > 0;
            }
        }
        #endregion Public

        #region Internal
        /// <summary>
        /// Find the lowest point in the water body, and use that as the new
        /// seed base. This is done when the terrain under the seed base
        /// gets raised or deleted.
        /// </summary>
        /// <param name="water"></param>
        private void Reseed(Water water, float stopHeight)
        {
            List<Vector2> tileCenters = water.TileCenters;
            if (tileCenters != null)
            {
                float seedHeight = water.SeedPosition.Z;
                MapCoord minMap = new MapCoord();
                foreach (Vector2 center in tileCenters)
                {
                    Point mapIdx = WorldToMapIndex(center);
                    if (ValidMapIndex(mapIdx))
                    {
                        WaterMap waterMap = WaterMaps[mapIdx.X, mapIdx.Y];
                        HeightMap heightMap = Maps[mapIdx.X, mapIdx.Y];
                        byte label = (byte)water.Label;
                        if (waterMap != null)
                        {
                            for (int i = 0; i < PixPerMap; ++i)
                            {
                                for (int j = 0; j < PixPerMap; ++j)
                                {
                                    if (waterMap[i, j] == label)
                                    {
                                        float height = heightMap.GetHeight(i, j);
                                        if ((height > 0.0f) && (height < seedHeight))
                                        {
                                            if (height <= stopHeight)
                                            {
                                                minMap = new MapCoord(
                                                    mapIdx,
                                                    new Point(i, j));
                                                /// Good enough.
                                                water.SeedPosition = MapCoordToWorld(minMap, water.BaseHeight);
                                                return;
                                            }
                                            seedHeight = height;
                                            minMap = new MapCoord(
                                                mapIdx,
                                                new Point(i, j));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (seedHeight <= water.SeedPosition.Z)
                {
                    water.SeedPosition = MapCoordToWorld(minMap, water.BaseHeight);
                }
            }
        }

        /// <summary>
        /// We need a new seed position if:
        ///     a) The new input position has a lower spot to offer than our current seed
        ///     b) Our current seed position has been lowered, just update it.
        ///     c) Our current seed position has been raised, we need to find a new
        ///         spot in this water at least as low.
        /// We need to regenerate everything if:
        ///     a) The spot has been lowered from above the water to below the water (expand).
        ///     b) The spot has been raised from below the water to above (shrink).
        /// </summary>
        /// <param name="water"></param>
        /// <param name="mapCoord"></param>
        private void Reseed(Water water, MapCoord mapCoord, float newTerrainHeight)
        {
            if (water != null)
            {
                Vector3 seedPos = water.SeedPosition;
                MapCoord seedCoord = WorldToMapCoord(new Vector2(seedPos.X, seedPos.Y));
                if ((seedCoord.coord.X == mapCoord.coord.X)
                    && (seedCoord.coord.Y == mapCoord.coord.Y)
                    && (seedCoord.mapIdx.X == mapCoord.mapIdx.X)
                    && (seedCoord.mapIdx.Y == mapCoord.mapIdx.Y))
                {
                    /// We're at the same spot.
                    /// If we're lower now, we just have a deeper seed position.
                    // If we're raised above the water surface, then we need to find a new minspot
                    if ((newTerrainHeight <= 0) || (newTerrainHeight > water.BaseHeight))
                    {
                        Reseed(water, water.BaseHeight - CubeSize);
                    }
                }
                else
                {
                    /// We're somewhere else. If the ground is now lower than our
                    /// seed position, take this as the new min spot, otherwise we
                    /// don't really care.
                    float seedHeight = GetHeight(seedCoord.mapIdx.X, seedCoord.mapIdx.Y, seedCoord.coord.X, seedCoord.coord.Y);
                    if ((newTerrainHeight > 0) && (seedHeight > newTerrainHeight))
                    {
                        water.SeedPosition = MapCoordToWorld(mapCoord, water.BaseHeight);
                    }
                }
            }
        }

        /// <summary>
        /// Erase the water's label from all watermaps.
        /// </summary>
        /// <param name="water"></param>
        private void Erase(Water water)
        {
            if (Terrain.Current != null)
            {
                Terrain.Current.WaterDirty = true;
            }
            List<Vector2> tileCenters = water.TileCenters;
            if (tileCenters != null)
            {
                foreach (Vector2 center in tileCenters)
                {
                    Point mapIdx = WorldToMapIndex(center);
                    if (ValidMapIndex(mapIdx))
                    {
                        if (WaterMaps[mapIdx.X, mapIdx.Y] != null)
                        {
                            WaterMaps[mapIdx.X, mapIdx.Y].Erase(water.Label);
                        }
                    }
                }

                water.TileCenters = null;
            }
        }

        /// <summary>
        /// Fill from given position with given water.
        /// Will overwrite (destroy) any other water encountered.
        /// </summary>
        /// <param name="water"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private bool Fill(Water water, Vector3 pos)
        {
            if (Terrain.Current != null)
            {
                Terrain.Current.WaterDirty = true;
            }

            bool[,] touchedTiles = new bool[NumMaps.X, NumMaps.Y];

            Point virtCoord = WorldToVirtualIndex(new Vector2(pos.X, pos.Y));

            Point coord;
            Point mapIdx;
            if (VirtualToCoord(virtCoord.X, virtCoord.Y, out mapIdx, out coord))
            {
                water.EdgeOfWorld = false; // will get set during fill if hit edge
                if (FillLoop(water, new MapCoord(mapIdx, coord), touchedTiles))
                {
                    ProcessTouched(water, touchedTiles);
                    return true;
                }
            }

            /// Failed to create, dispose and return false.
            water.TileCenters = null;
            AddForDelete(water);

            return false;
        }

        /// <summary>
        /// Fixup all tiles touched by the preceeding fill operation.
        /// </summary>
        /// <param name="water"></param>
        /// <param name="touchedTiles"></param>
        /// <returns></returns>
        private bool ProcessTouched(Water water, bool[,] touchedTiles)
        {
            List<Vector2> centers = new List<Vector2>(touchedTiles.Length);

            for (int i = 0; i < NumMaps.X; ++i)
            {
                for (int j = 0; j < NumMaps.Y; ++j)
                {
                    if (touchedTiles[i, j])
                    {
                        Vector2 center = Min
                            + new Vector2(
                                (i + 0.5f) * MapSize.X,
                                (j + 0.5f) * MapSize.Y);

                        centers.Add(center);
                        AddForBuild(WaterTiles[i, j], water.Label);
                    }
                }
            }

            water.TileCenters = centers;

            return water.TileCenters.Count > 0;
        }

        /// <summary>
        /// Advance the mapCoord, crossing to adjacent tile as necessary.
        /// No clamping. Coord will be valid, but mapIdx may go off edge of world.
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        private MapCoord IncMapCoord(MapCoord mc, int dx, int dy)
        {
            mc.coord.X += dx;
            if (mc.coord.X < 0)
            {
                mc.mapIdx.X += mc.coord.X / PixPerMap - 1;
                mc.coord.X = mc.coord.X % PixPerMap;
                if (mc.coord.X < 0)
                    mc.coord.X += PixPerMap;

                //mc.coord.X = PixPerMap - 1;
                //--mc.mapIdx.X;
            }
            else if (mc.coord.X >= PixPerMap)
            {
                mc.mapIdx.X += mc.coord.X / PixPerMap;
                mc.coord.X = mc.coord.X % PixPerMap;

                //mc.coord.X = 0;
                //++mc.mapIdx.X;
            }

            mc.coord.Y += dy;
            if (mc.coord.Y < 0)
            {
                mc.mapIdx.Y += mc.coord.Y / PixPerMap - 1;
                mc.coord.Y = mc.coord.Y % PixPerMap;
                if (mc.coord.Y < 0)
                    mc.coord.Y += PixPerMap;

                //mc.coord.Y = PixPerMap - 1;
                //--mc.mapIdx.Y;
            }
            else if (mc.coord.Y >= PixPerMap)
            {
                mc.mapIdx.Y += mc.coord.Y / PixPerMap;
                mc.coord.Y = mc.coord.Y % PixPerMap;

                //mc.coord.Y = 0;
                //++mc.mapIdx.Y;
            }

            return mc;
        }

        /// <summary>
        /// Test for whether the input position should be added in a water fill operation.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="height"></param>
        /// <param name="mapCoord"></param>
        /// <returns></returns>
        private bool NeedsAdding(Water water, float height, MapCoord mapCoord)
        {
            if (!ValidMapIndex(mapCoord.mapIdx))
            {
                /// Off the grid.
                return false;
            }
            WaterMap waterMap = WaterMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
            if (waterMap[mapCoord.coord.X, mapCoord.coord.Y] == (byte)water.Label)
            {
                /// Already labeled.
                return false;
            }
            HeightMap heightMap = Maps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
            float h = heightMap.GetHeight(mapCoord.coord.X, mapCoord.coord.Y);
            if (h > height)
            {
                /// Above the water line.
                return false;
            }

            if (h <= 0.0f)
            {
                water.EdgeOfWorld = true;
                /// No terrain here.
                return false;
            }

            /// Passed all tests.
            return true;
        }

        /// <summary>
        /// Add the given position in a fill operation.
        /// </summary>
        /// <param name="water"></param>
        /// <param name="height"></param>
        /// <param name="mapCoord"></param>
        /// <param name="touchedTiles"></param>
        /// <returns></returns>
        private bool AddMapCoord(Water water, float height, MapCoord mapCoord, bool[,] touchedTiles)
        {
            if (NeedsAdding(water, height, mapCoord))
            {
                if (nextCoord > 0)
                {
                    /// If there's room at the beginning, add it there...
                    mapCoordStack[--nextCoord] = mapCoord;
                }
                else
                {
                    /// otherwise append it.
                    mapCoordStack.Add(mapCoord);
                }

                /// Now set the label, so we don't hit this one again.
                WaterMap waterMap = WaterMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                byte oldLabel = waterMap[mapCoord.coord.X, mapCoord.coord.Y];
                if (oldLabel != Water.InvalidLabel)
                {
                    Water oldWater = Water.FromLabel(oldLabel);
                    AddForDelete(oldWater);
                }
                waterMap[mapCoord.coord.X, mapCoord.coord.Y] = (byte)water.Label;
                touchedTiles[mapCoord.mapIdx.X, mapCoord.mapIdx.Y] = true;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Internals for use in making the fill(s) non-recursive.
        /// </summary>
        private List<MapCoord> mapCoordStack = new List<MapCoord>();
        private int nextCoord = 0;

        /// <summary>
        /// Do the fill iteratively.
        /// </summary>
        /// <param name="water"></param>
        /// <param name="mapCoord"></param>
        /// <param name="touchedTiles"></param>
        /// <returns></returns>
        private bool FillLoop(Water water, MapCoord mapCoord, bool[,] touchedTiles)
        {
            float height = water.BaseHeight;

            if (!AddMapCoord(water, height, mapCoord, touchedTiles))
            {
                return false;
            }

            while (nextCoord < mapCoordStack.Count)
            {
                mapCoord = mapCoordStack[nextCoord];
                ++nextCoord;

                /// Now check the neighbors.
                MapCoord north = IncMapCoord(mapCoord, 0, 1);
                AddMapCoord(water, height, north, touchedTiles);

                MapCoord east = IncMapCoord(mapCoord, 1, 0);
                AddMapCoord(water, height, east, touchedTiles);

                MapCoord south = IncMapCoord(mapCoord, 0, -1);
                AddMapCoord(water, height, south, touchedTiles);

                MapCoord west = IncMapCoord(mapCoord, -1, 0);
                AddMapCoord(water, height, west, touchedTiles);
            }
            mapCoordStack.Clear();
            nextCoord = 0;
            /// Should we trim excess here? Or wait until we're out of
            /// edit mode?

            return true;
        }

        /// <summary>
        /// Queue up for erasure.
        /// </summary>
        /// <param name="water"></param>
        private void AddForErase(Water water)
        {
            if ((water != null) && !water.QueuedForErase)
            {
                water.QueuedForErase = true;
                waterQueues.erases.Enqueue(water);
            }
        }

        /// <summary>
        /// Queue up for disposal of renderables.
        /// </summary>
        /// <param name="water"></param>
        private void AddForDispose(Water water)
        {
            if ((water != null) && !water.QueuedForDispose)
            {
                water.QueuedForDispose = true;
                waterQueues.disposes.Enqueue(water);
            }
        }

        /// <summary>
        /// Queue up for a fill operation.
        /// </summary>
        /// <param name="water"></param>
        private void AddForFill(Water water)
        {
            if ((water != null) && !water.QueuedForFill)
            {
                water.QueuedForFill = true;
                waterQueues.fills.Enqueue(water);
            }
        }

        /// <summary>
        /// Queue up for building renderable geometry.
        /// </summary>
        /// <param name="waterTile"></param>
        /// <param name="label"></param>
        private void AddForBuild(WaterTile waterTile, int label)
        {
            if (waterTile != null)
            {
                waterTile.SetDirty(label);
                if (!waterTile.QueuedForBuild)
                {
                    waterTile.QueuedForBuild = true;
                    waterQueues.builds.Enqueue(waterTile);
                }
            }
        }

        /// <summary>
        /// Queue up for being created.
        /// </summary>
        /// <param name="water"></param>
        private void AddForCreate(Water water)
        {
            AddForFill(water);
        }

        /// <summary>
        /// Queue up for being refreshed, like if the terrain underneath changed.
        /// </summary>
        /// <param name="water"></param>
        public void AddForRefresh(Water water, MapCoord mapCoord, float newHeight)
        {
            Reseed(water, mapCoord, newHeight);
            AddForErase(water);
            AddForFill(water);
            ///AddForBuild
        }

        public void AddForRefresh(Water water, Vector2 position, float newHeight)
        {
            MapCoord mapCoord = WorldToMapCoord(position);
            AddForRefresh(water, mapCoord, newHeight);
        }

        /// <summary>
        /// Queue up for being deleted.
        /// </summary>
        /// <param name="water"></param>
        private void AddForDelete(Water water)
        {
            AddForErase(water);
            AddForDispose(water);
            /// Destroy Built stuff handled in Dispose update
        }

        /// <summary>
        /// Queue for refresh if given point rising to new height necessitates it.
        /// </summary>
        /// <param name="mapCoord"></param>
        /// <param name="newHeight"></param>
        private void AddWaterLandRise(MapCoord mapCoord, float newHeight)
        {
            WaterMap waterMap = GetWaterMap(mapCoord.mapIdx);
            if (waterMap != null)
            {
                byte label = waterMap[mapCoord.coord.X, mapCoord.coord.Y];
                Water water = Water.FromLabel(label);
                if (water != null)
                {
                    if (water.EdgeOfWorld || (water.BaseHeight <= newHeight))
                    {
                        AddForRefresh(water, mapCoord, newHeight);
                    }
                }
            }
        }
        /// <summary>
        /// Queue for refresh if given point lowering to new height necessitates it.
        /// </summary>
        /// <param name="mapCoord"></param>
        /// <param name="newHeight"></param>
        private void AddWaterLandLower(MapCoord mapCoord, float newHeight)
        {
            WaterMap waterMap = GetWaterMap(mapCoord.mapIdx);
            if (waterMap != null)
            {
                byte label = waterMap[mapCoord.coord.X, mapCoord.coord.Y];
                Water water = Water.FromLabel(label);
                if (water != null)
                {
                    if ((newHeight <= 0) || (water.BaseHeight > newHeight) || water.EdgeOfWorld)
                    {
                        AddForRefresh(water, mapCoord, newHeight);
                    }
                }
            }
        }

        /// <summary>
        /// Given a point changing from oldHeight to newHeight, refresh any affected water bodies.
        /// </summary>
        /// <param name="mapIdx"></param>
        /// <param name="coord"></param>
        /// <param name="oldHeight"></param>
        /// <param name="newHeight"></param>
        private void AddWaterUpdate(Point mapIdx, Point coord, float oldHeight, float newHeight)
        {
            MapCoord mapCoord = new MapCoord(mapIdx, coord);

            if ((oldHeight > 0) && (newHeight > oldHeight))
            {
                AddWaterLandRise(mapCoord, newHeight);
            }
            else if ((oldHeight <= 0) || (newHeight < oldHeight))
            {
                WaterMap waterMap = GetWaterMap(mapCoord.mapIdx);
                if (waterMap != null)
                {
                    byte label = waterMap[mapCoord.coord.X, mapCoord.coord.Y];
                    Water water = Water.FromLabel(label);
                    if ((newHeight <= 0) || (water == null) || water.EdgeOfWorld)
                    {
                        AddWaterLandLower(IncMapCoord(mapCoord, 0, 1), newHeight);
                        AddWaterLandLower(IncMapCoord(mapCoord, 0, -1), newHeight);
                        AddWaterLandLower(IncMapCoord(mapCoord, 1, 0), newHeight);
                        AddWaterLandLower(IncMapCoord(mapCoord, -1, 0), newHeight);
                    }
                }
            }
        }

        /// <summary>
        /// Update queued up water related tasks.
        /// </summary>
        /// <param name="doAll"></param>
        private void UpdateWater(bool doAll)
        {
            UpdateWaterErase();
            UpdateWaterDispose();
            UpdateWaterFill(doAll);
            UpdateWaterErase();
            UpdateWaterDispose();
            UpdateWaterBuild(doAll);
            flushWaterUpdate = false;
        }

        /// <summary>
        /// Do all queued erase updates.
        /// </summary>
        private void UpdateWaterErase()
        {
            Queue<Water> erases = waterQueues.erases;
            while (erases.Count > 0)
            {
                Water water = erases.Dequeue();
                water.QueuedForErase = false;
                Erase(water);
            }
        }

        /// <summary>
        /// Do all queued disposal tasks.
        /// </summary>
        private void UpdateWaterDispose()
        {
            Queue<Water> disposes = waterQueues.disposes;
            while (disposes.Count > 0)
            {
                Water water = disposes.Dequeue();
                water.QueuedForDispose = false;
                if (!water.QueuedForFill)
                {
                    water.Dispose();
                }
            }
        }

        /// <summary>
        /// Chip away at the fill queue. Empty it if doAll.
        /// </summary>
        /// <param name="doAll"></param>
        private void UpdateWaterFill(bool doAll)
        {
            const int MaxUpdates = 5;
            int numToDo = waterQueues.fills.Count;
            numToDo = !doAll && (numToDo > MaxUpdates) ? MaxUpdates : numToDo;

            Queue<Water> fills = waterQueues.fills;
            while (numToDo-- > 0)
            {
                Water water = fills.Dequeue();

                water.QueuedForFill = false;

                /// Don't bother filling it if we're later just going to throw it away.
                if (!water.QueuedForDispose && !water.QueuedForErase)
                {
                    Fill(water, water.SeedPosition);
                }
            }
        }

        /// <summary>
        /// Chip away at the build renderable queue. Empty it if doAll.
        /// </summary>
        /// <param name="doAll"></param>
        private void UpdateWaterBuild(bool doAll)
        {
            const int MaxUpdates = 5;
            int numToDo = waterQueues.builds.Count;
            numToDo = !doAll && (numToDo > MaxUpdates) ? MaxUpdates : numToDo;

            bool doAny = numToDo > 0;

            Queue<WaterTile> builds = waterQueues.builds;
            while (numToDo-- > 0)
            {
                WaterTile waterTile = builds.Dequeue();
                waterTile.QueuedForBuild = false;
                waterTile.Refresh(this);
            }

            maxWaterHeight = 0;
            for (int i = 0; i < Water.AllWaters.Count; ++i)
            {
                Water water = Water.AllWaters[i];
                if (water.BaseHeight > maxWaterHeight)
                    maxWaterHeight = water.BaseHeight;
            }
        }

        /// <summary>
        /// Test whether the given coordinate is off the end of the defined world.
        /// </summary>
        /// <param name="mapCoord"></param>
        /// <returns></returns>
        private bool OffEndOfWorld(MapCoord mapCoord)
        {
            /// Off the grid
            if (!ValidMapIndex(mapCoord.mapIdx))
                return true;
            HeightMap heightMap = Maps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
            /// There's a hole in the world here.
            if (heightMap == null)
                return true;
            /// Height == zero means no terrain here.
            if (heightMap.GetHeight(mapCoord.coord.X, mapCoord.coord.Y) <= 0.0f)
                return true;

            return false;
        }

        /// <summary>
        /// Test whether the sample is labeled with no water or a different water body. 
        /// true if either.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="mapCoord"></param>
        /// <returns></returns>
        private bool DifferentWater(byte label, MapCoord mapCoord)
        {
            Debug.Assert(ValidMapIndex(mapCoord.mapIdx));
            Debug.Assert(WaterMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y] != null);

            byte labelHere = WaterMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y][mapCoord.coord.X, mapCoord.coord.Y];
            return (labelHere != label);
        }

        /// <summary>
        /// 0.0 is off end of world, 0.5f is wet but a different water body,
        /// 1.0 is dry land or same water body.
        /// 
        /// </summary>
        /// <param name="label"></param>
        /// <param name="mapCoord"></param>
        /// <returns></returns>
        private float TestNeighbor(byte label, MapCoord mapCoord)
        {
            if (OffEndOfWorld(mapCoord))
            {
                return 0.0f;
            }
            else if (DifferentWater(label, mapCoord))
            {
                return 0.5f;
            }
            return 1.0f;
        }

        /// <summary>
        /// Test all 4 cardinal neighbors.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="mapCoord"></param>
        /// <returns></returns>
        private Color GetNeighbors(byte label, MapCoord mapCoord)
        {
            Vector4 neighbors = Vector4.Zero;

            // North
            neighbors.X = TestNeighbor(label, IncMapCoord(mapCoord, 0, 1));

            // East
            neighbors.Y = TestNeighbor(label, IncMapCoord(mapCoord, 1, 0));

            // South
            neighbors.Z = TestNeighbor(label, IncMapCoord(mapCoord, 0, -1));

            // North
            neighbors.W = TestNeighbor(label, IncMapCoord(mapCoord, -1, 0));

            return new Color(neighbors);
        }

        /// <summary>
        /// Rebuild renderable geometry for this water tile.
        /// </summary>
        /// <param name="waterTile"></param>
        /// <param name="renderable"></param>
        /// <returns></returns>
        private bool RebuildWater(WaterTile waterTile, WaterTile.Renderable renderable)
        {
            byte label = renderable.Label;
            MapCoord mapCoord = new MapCoord(
                WorldToMapIndex(waterTile.Center),
                new Point(0, 0));

            if (ValidMapIndex(mapCoord.mapIdx))
            {
                WaterMap waterMap = WaterMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                if (waterMap != null)
                {
                    renderable.BeginBuild();

                    HeightMap heightMap = Maps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];

                    Vector2 swCorner = new Vector2(
                        waterTile.Min.X + CubeSize * 0.5f,
                        waterTile.Min.Y + CubeSize * 0.5f);
                    for (mapCoord.coord.X = 0; mapCoord.coord.X < PixPerMap; ++mapCoord.coord.X)
                    {
                        for (mapCoord.coord.Y = 0; mapCoord.coord.Y < PixPerMap; ++mapCoord.coord.Y)
                        {
                            if (waterMap[mapCoord.coord.X, mapCoord.coord.Y] == label)
                            {
                                Vector2 center = swCorner
                                    + new Vector2(mapCoord.coord.X * CubeSize, mapCoord.coord.Y * CubeSize);

                                float h = heightMap.GetHeight(mapCoord.coord.X, mapCoord.coord.Y);

                                /// The neighborhood creation can be optimized to a single pass
                                /// over the whole tile. That would be a big win when the tile
                                /// is completely covered (cutting the number of GetHeight() calls
                                /// to 1/4), but obviously less of a win when the tile is sparsely
                                /// covered. Leaving for an optimization later.

                                Color neighbors = GetNeighbors(label, mapCoord);
                                renderable.AddCoreVertex(center, h, neighbors);
                            }
                        }
                    }

                    return renderable.EndBuild(CubeSize * 0.5f);
                }
            }
            return false;
        }

        /// <summary>
        /// Test whether and which water is visible, and cache the results.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        private bool CullTestWater(Camera camera)
        {
            if (!Water.JustEmptied && (Water.AllWaters.Count == 0))
            {
                /// No water in the world.
                return false;
            }
            Water.JustEmptied = false;
            bool allCulled = true;
            for (int i = 0; i < NumMaps.X; ++i)
            {
                for (int j = 0; j < NumMaps.Y; ++j)
                {
                    WaterTile waterTile = WaterTiles[i, j];
                    if (waterTile != null)
                    {
                        if (waterTile.CullTest(camera))
                        {
                            allCulled = false;
                        }
                    }
                }
            }
            return !allCulled;
        }
        #endregion Internal

        #endregion Water

        #region MaterialSelect
        #region Public
        public delegate bool WantedCoord(MapCoord mapCoord);
        public delegate void OnAdded(MapCoord mapCoord);

        /// <summary>
        /// Sort function, sorts x major, then y, ascending.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        static public int CompareCoords(MapCoord lhs, MapCoord rhs)
        {
            /// First key, map grid X.
            if (lhs.mapIdx.X < rhs.mapIdx.X)
                return -1;
            if (lhs.mapIdx.X > rhs.mapIdx.X)
                return 1;
            /// Map grid X is equal.
            /// 
            /// Second key, map grid Y.
            if (lhs.mapIdx.Y < rhs.mapIdx.Y)
                return -1;
            if (lhs.mapIdx.Y > rhs.mapIdx.Y)
                return 1;
            /// Map grid Y is equal.
            /// 
            /// Third key, coord X
            if (lhs.coord.X < rhs.coord.X)
                return -1;
            if (lhs.coord.X > rhs.coord.X)
                return 1;
            /// Coord X is equal.
            /// 
            /// Last key, coord Y.
            if (lhs.coord.Y < rhs.coord.Y)
                return -1;
            if (lhs.coord.Y > rhs.coord.Y)
                return 1;

            /// All equal
            return 0;
        }

        /// <summary>
        /// Class to feed into FillGeneric to select and cache all 
        /// contiguous sample points of given material.
        /// </summary>
        public class FloodSelect
        {
            #region Members
            private VirtualMap vMap = null;
            private List<MapCoord> selection = new List<MapCoord>();
            private List<Point> coords = new List<Point>();
            private List<Tile> tiles = new List<Tile>();
            private ushort selectionIdx = TerrainMaterial.EmptyMatIdx;
            #endregion Members

            #region Accessors
            /// <summary>
            /// Material index for this selection set.
            /// </summary>
            public ushort SelectionIndex
            {
                get { return selectionIdx; }
                set
                {
                    selectionIdx = value;
                }
            }

            /// <summary>
            /// ALl of the virtual coordinates of selected samples.
            /// No duplicates.
            /// </summary>
            public List<Point> Coords
            {
                get { return coords; }
            }

            /// <summary>
            /// List of tiles touched by the sample set. No duplicates.
            /// </summary>
            public List<Tile> Tiles
            {
                get { return tiles; }
            }

            /// <summary>
            /// Return whether anything is currently selected.
            /// </summary>
            public bool Empty
            {
                get { return Coords.Count == 0; }
            }
            #endregion Accessors

            #region Public

            public FloodSelect(VirtualMap vMap)
            {
                this.vMap = vMap;
            }

            /// <summary>
            /// We want to add the point if it is a valid defined point and hasn't
            /// been added already and is the right material value.
            /// </summary>
            /// <param name="mapCoord"></param>
            /// <returns></returns>
            public bool WantTest(MapCoord mapCoord)
            {
                if (!vMap.ValidMapIndex(mapCoord.mapIdx))
                    return false;

                HeightMap heightMap = vMap.Maps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                ColorMap colorMap = vMap.ColorMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];

                if (colorMap == null || heightMap == null)
                    return false;

                if (heightMap.GetHeight(mapCoord.coord.X, mapCoord.coord.Y) <= 0.0f) //Todo(DZ): We should change this "0.0" const to a named const
                    return false;

                ushort matIdx = colorMap[mapCoord.coord.X, mapCoord.coord.Y];

                if (matIdx != selectionIdx)
                    return false;

                return true;
            }

            /// <summary>
            /// Add the point to our selection set, and set the material to a marker value
            /// so we don't add it again.
            /// </summary>
            /// <param name="mapCoord"></param>
            public void DoAdd(MapCoord mapCoord)
            {
                Debug.Assert(vMap.ValidMapIndex(mapCoord.mapIdx));
                ColorMap colorMap = vMap.ColorMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                Debug.Assert(colorMap != null);
                colorMap[mapCoord.coord.X, mapCoord.coord.Y] |= (ushort)TerrainMaterial.Flags.Selection;
                Selection.Add(mapCoord);
            }

            /// <summary>
            /// Change the material back to original value. Translate our internal map
            /// coords to externally useful virtual coords.
            /// </summary>
            public void Finished()
            {
                Tiles.Clear();
                Coords.Clear();
                Coords.Capacity = Math.Max(Selection.Count, coords.Capacity);
                Selection.Sort(CompareCoords);
                Tile lastTile = null;
                foreach (MapCoord mapCoord in Selection)
                {
                    Debug.Assert(vMap.ValidMapIndex(mapCoord.mapIdx));
                    Tile tile = vMap.Tiles[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                    Debug.Assert(tile != null);
                    if (tile != lastTile)
                    {
                        tile.Select(SelectionIndex, true);
                        Tiles.Add(tile);
                        lastTile = tile;
                        vMap.AddUpdate(tile);
                    }

                    Point virtCoord = new Point(
                        mapCoord.mapIdx.X * VirtualMap.PixPerMap + mapCoord.coord.X,
                        mapCoord.mapIdx.Y * VirtualMap.PixPerMap + mapCoord.coord.Y);
                    coords.Add(virtCoord);
                }

            }

            /// <summary>
            /// Remap all contained coords down by offset amount, because
            /// the grid has been trimmed and hence shifted up. A negative offset
            /// would mean the virtual grid has expanded on the negative side.
            /// </summary>
            /// <param name="offset"></param>
            public void Remap(Point offset)
            {
                int selCnt = Selection.Count;
                for (int i = 0; i < selCnt; ++i)
                {
                    MapCoord mapCoord = selection[i];
                    mapCoord.mapIdx.X -= offset.X;
                    mapCoord.mapIdx.Y -= offset.Y;
                    selection[i] = mapCoord;
                }

                offset.X *= VirtualMap.PixPerMap;
                offset.Y *= VirtualMap.PixPerMap;
                int coordCnt = Coords.Count;
                for (int i = 0; i < coordCnt; ++i)
                {
                    Point coord = coords[i];
                    coord.X -= offset.X;
                    coord.Y -= offset.Y;
                    coords[i] = coord;
                }

            }

            /// <summary>
            /// Deselect everything we've selected.
            /// </summary>
            public void UnSelect()
            {
                foreach (MapCoord mapCoord in Selection)
                {
                    if (vMap.ValidMapIndex(mapCoord.mapIdx))
                    {
                        ColorMap colorMap = vMap.ColorMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                        if ((colorMap != null)
                            && (TerrainMaterial.HasFlags(colorMap[mapCoord.coord.X, mapCoord.coord.Y], TerrainMaterial.Flags.Selection)))
                        {
                            colorMap[mapCoord.coord.X, mapCoord.coord.Y] = SelectionIndex;
                        }
                    }
                }
                foreach (Tile tile in Tiles)
                {
                    vMap.AddUpdate(tile);
                    tile.Select(SelectionIndex, false);
                }
                Coords.Clear();
                Tiles.Clear();
                Selection.Clear();
                SelectionIndex = TerrainMaterial.EmptyMatIdx;
            }

            /// <summary>
            /// Expand material selection by one sample. If ContainSelection is true, this is
            /// clamped at the current material border.
            /// </summary>
            public void ExpandSelection()
            {
                UpdateTiles();
                List<MapCoord> selOrig = Selection;
                Selection = new List<MapCoord>(selOrig.Capacity);
                foreach (MapCoord mapCoord in selOrig)
                {
                    Selection.Add(mapCoord);
                    TestExpand(vMap.IncMapCoord(mapCoord, 0, 1));
                    TestExpand(vMap.IncMapCoord(mapCoord, 0, -1));
                    TestExpand(vMap.IncMapCoord(mapCoord, 1, 0));
                    TestExpand(vMap.IncMapCoord(mapCoord, -1, 0));
                }
                Finished();
            }

            /// <summary>
            /// Shrink the current material selection region by one sample border.
            /// Don't allow it to shrink to nothing.
            /// </summary>
            public void ShrinkSelection()
            {
                UpdateTiles();
                List<MapCoord> selOrig = Selection;
                List<MapCoord> border = new List<MapCoord>();
                Selection = new List<MapCoord>(selOrig.Capacity);
                foreach (MapCoord mapCoord in selOrig)
                {
                    if (!IsBorder(mapCoord))
                    {
                        Selection.Add(mapCoord);
                    }
                    else
                    {
                        border.Add(mapCoord);
                    }
                }
                if (Selection.Count > 0)
                {
                    foreach (MapCoord mapCoord in border)
                    {
                        ColorMap colorMap = vMap.ColorMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                        colorMap[mapCoord.coord.X, mapCoord.coord.Y] = SelectionIndex;
                    }
                }
                else
                {
                    /// If we've shrunk down to nothing, disallow the operation.
                    Selection = selOrig;
                }
                Finished();
            }

            #endregion Public

            #region Internal
            private List<MapCoord> Selection
            {
                get { return selection; }
                set { selection = value; }
            }

            private void Reset()
            {
                selection.Clear();
            }

            private void TestExpand(MapCoord mapCoord)
            {
                if (vMap.ValidMapIndex(mapCoord.mapIdx))
                {
                    HeightMap heightMap = vMap.Maps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                    if (heightMap.GetHeight(mapCoord.coord.X, mapCoord.coord.Y) > 0.0f)
                    {
                        ColorMap colorMap = vMap.ColorMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                        //if (colorMap[mapCoord.coord.X, mapCoord.coord.Y] == SelectionIndex)
                        if (vMap.Contained(colorMap[mapCoord.coord.X, mapCoord.coord.Y]))
                        {
                            colorMap[mapCoord.coord.X, mapCoord.coord.Y] |= (ushort)TerrainMaterial.Flags.Selection;
                            Selection.Add(mapCoord);
                        }
                    }
                }
            }

            private void UpdateTiles()
            {
                foreach (Tile tile in Tiles)
                {
                    vMap.AddUpdate(tile);
                }
            }

            private bool Outside(MapCoord srcCoord, int dx, int dy)
            {
                MapCoord mapCoord = vMap.IncMapCoord(srcCoord, dx, dy);
                if (vMap.ValidMapIndex(mapCoord.mapIdx))
                {
                    HeightMap heightMap = vMap.Maps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                    if (heightMap.GetHeight(mapCoord.coord.X, mapCoord.coord.Y) > 0.0f)
                    {
                        ColorMap colorMap = vMap.ColorMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                        if (TerrainMaterial.HasFlags(colorMap[mapCoord.coord.X, mapCoord.coord.Y], TerrainMaterial.Flags.Selection))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            private bool IsBorder(MapCoord mapCoord)
            {
                return Outside(mapCoord, 0, 1)
                    || Outside(mapCoord, 0, -1)
                    || Outside(mapCoord, 1, 0)
                    || Outside(mapCoord, -1, 0);
            }

            #endregion Internal
        }

        /// <summary>
        /// Determine whether expansion of the selected material region beyond original border is allowed.
        /// </summary>
        public bool ContainSelection
        {
            get { return containSelection; }
            set { containSelection = value; }

        }
        /// <summary>
        /// Generate and cache a flood fill from pos.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool MakeMaterialSelection(Vector2 pos)
        {
            if (floodSelect == null)
            {
                floodSelect = new FloodSelect(this);
            }

            MapCoord mapCoord = WorldToMapCoord(pos);
            float height = GetHeight(mapCoord.mapIdx.X, mapCoord.mapIdx.Y, mapCoord.coord.X, mapCoord.coord.Y);
            ColorMap colorMap = GetColorMap(mapCoord.mapIdx);

            if (colorMap == null || height <= 0)//ToDo(DZ): We need to get rid of the "0" constant. It may seem obvious that the minHeight and/or invalidHeight will
            {                                   // always be "0", but it may not always be so. For one, we may want to differantiate between an "invalid" height and
                // and the lowest possible height. This lesson was learned the hard way with the material indices.

                //The user has clicked outside the terrain area thus we should unselect the magic brush's selection (if it has one)
                ClearMaterialSelection();
                return false;
            }

            ushort matIdx = colorMap[mapCoord.coord.X, mapCoord.coord.Y];

            float maxDelta = 0.0001f;
            if (shrinkingSelection &&
                (lastSelectionPos.X - pos.X) < maxDelta &&
                (lastSelectionPos.Y - pos.Y) < maxDelta
            )
            {
                return false;
            }

            shrinkingSelection = false;
            lastSelectionPos = pos;

            if (!TerrainMaterial.IsValid(matIdx, false, false))
            {
                //We got a bad material or the material we got already has the "selection" flag thus we don't
                //need to reselect it.
                return false;
            }

            lastSelectionPos = pos;

            Tile tile = Tiles[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];

            if (tile == null)
            {
                //If we don't have a tile here, there is no point in selecting anything.
                return false;
            }

            ClearMaterialSelection();
            floodSelect.SelectionIndex = matIdx;

            bool[,] touchedTiles = new bool[NumMaps.X, NumMaps.Y];

            if (FillGeneric(
                    new MapCoord(mapCoord.mapIdx, mapCoord.coord),
                    touchedTiles,
                    floodSelect.WantTest,
                    floodSelect.DoAdd))
            {
                floodSelect.Finished();
                return true;
            }

            return false;
        }
        /// <summary>
        /// Expand the current selection by one sample.
        /// </summary>
        public void ExpandSelection()
        {
            if (floodSelect != null)
            {
                floodSelect.ExpandSelection();
            }
        }
        /// <summary>
        /// Shrink the current selection by one sample.
        /// </summary>
        public void ShrinkSelection()
        {
            if (floodSelect != null)
            {
                floodSelect.ShrinkSelection();
                shrinkingSelection = true;
            }
        }
        /// <summary>
        /// Set the type of the selected material (if any).
        /// </summary>
        /// <param name="matIdx"></param>
        public void ChangeSelectedMaterial(ushort matIdx)
        {
            if (floodSelect == null || floodSelect.SelectionIndex == matIdx)
                return;

            if (!TerrainMaterial.IsValid(matIdx, false, false))
                return;

            floodSelect.SelectionIndex = matIdx;
            floodSelect.UnSelect();
        }
        /// <summary>
        /// List of virtual coordinates of selected samples (no duplicates).
        /// </summary>
        public List<Point> SelectList
        {
            get { return floodSelect != null ? floodSelect.Coords : null; }
        }
        /// <summary>
        /// List of tiles touched by selected samples (no duplicates).
        /// </summary>
        public List<Tile> SelectTiles
        {
            get { return floodSelect != null ? floodSelect.Tiles : null; }
        }
        /// <summary>
        /// Is input material index the one currently selected?
        /// </summary>
        /// <param name="matIdx"></param>
        /// <returns></returns>
        public bool IsSelected(ushort matIdx)
        {
            return matIdx == SelectedMatIdx;
        }
        /// <summary>
        /// Return currently selected material index.
        /// </summary>
        public ushort SelectedMatIdx
        {
            get
            {
                if (floodSelect == null)
                    return TerrainMaterial.EmptyMatIdx;

                Debug.Assert(!TerrainMaterial.HasFlags(floodSelect.SelectionIndex, TerrainMaterial.Flags.Selection));
                return floodSelect.SelectionIndex;
            }
        }
        /// <summary>
        /// Clear any current material selection.
        /// </summary>
        public void ClearMaterialSelection()
        {
            if (floodSelect != null)
            {
                floodSelect.UnSelect();
            }
        }

        /// <summary>
        /// Flood fill from input position to change material there to terrain current material.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool ChangeMaterial(Vector2 pos)
        {
            bool[,] touchedTiles = new bool[NumMaps.X, NumMaps.Y];

            MapCoord mapCoord = WorldToMapCoord(pos);
            ColorMap colorMap = GetColorMap(mapCoord.mapIdx);
            if (colorMap != null)
            {
                ushort fromIdx = colorMap[mapCoord.coord.X, mapCoord.coord.Y];
                ushort toIdx = Terrain.CurrentMaterialIndex;
                if (fromIdx != toIdx)
                {
                    MaterialChanger changeMat = new MaterialChanger(this, fromIdx, toIdx);
                    if (FillGeneric(
                        new MapCoord(mapCoord.mapIdx, mapCoord.coord),
                        touchedTiles,
                        changeMat.WantTest,
                        changeMat.DoAdd))
                    {
                        for (int i = 0; i < NumMaps.X; ++i)
                        {
                            for (int j = 0; j < NumMaps.Y; ++j)
                            {
                                if (touchedTiles[i, j])
                                {
                                    AddUpdate(Tiles[i, j]);
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        #endregion Public

        #region Internal
        private bool containSelection = true;
        /// <summary>
        /// Check whether this material index is allowed to be absorbed by a selection expansion operation.
        /// </summary>
        /// <param name="matIdx"></param>
        /// <returns></returns>
        private bool Contained(ushort matIdx)
        {
            return containSelection
                ? matIdx == SelectedMatIdx
                : !TerrainMaterial.HasFlags(matIdx, TerrainMaterial.Flags.Selection);
        }

        private bool AddGeneric(MapCoord mapCoord, bool[,] touchedTiles, WantedCoord wantTest, OnAdded doAdd)
        {
            if (wantTest(mapCoord))
            {
                if (nextCoord > 0)
                {
                    /// If there's room at the beginning, add it there...
                    mapCoordStack[--nextCoord] = mapCoord;
                }
                else
                {
                    /// otherwise append it.
                    mapCoordStack.Add(mapCoord);
                }

                doAdd(mapCoord);
                touchedTiles[mapCoord.mapIdx.X, mapCoord.mapIdx.Y] = true;

                return true;
            }
            return false;
        }
        private bool FillGeneric(MapCoord mapCoord, bool[,] touchedTiles, WantedCoord wantTest, OnAdded doAdd)
        {
            if (!AddGeneric(mapCoord, touchedTiles, wantTest, doAdd))
            {
                return false;
            }
            while (nextCoord < mapCoordStack.Count)
            {
                mapCoord = mapCoordStack[nextCoord];
                ++nextCoord;

                MapCoord north = IncMapCoord(mapCoord, 0, 1);
                AddGeneric(north, touchedTiles, wantTest, doAdd);

                MapCoord east = IncMapCoord(mapCoord, 1, 0);
                AddGeneric(east, touchedTiles, wantTest, doAdd);

                MapCoord south = IncMapCoord(mapCoord, 0, -1);
                AddGeneric(south, touchedTiles, wantTest, doAdd);

                MapCoord west = IncMapCoord(mapCoord, -1, 0);
                AddGeneric(west, touchedTiles, wantTest, doAdd);
            }
            mapCoordStack.Clear();
            nextCoord = 0;
            /// Should we trim excess here? Or wait until we're out of
            /// edit mode?

            return true;
        }
        private FloodSelect floodSelect = null;

        private class MaterialChanger
        {
            private VirtualMap vMap = null;
            private ushort fromIdx = TerrainMaterial.EmptyMatIdx;
            private ushort toIdx = TerrainMaterial.EmptyMatIdx;
            public MaterialChanger(VirtualMap vMap, ushort fromIdx, ushort toIdx)
            {
                this.vMap = vMap;
                this.toIdx = toIdx;
                this.fromIdx = fromIdx;
                Debug.Assert(fromIdx != toIdx
                    && TerrainMaterial.IsValid(fromIdx, false, false)
                    && TerrainMaterial.IsValid(toIdx, false, false), "The selection flag shouldn't be allowed here! (DZ)");
            }

            public bool WantTest(MapCoord mapCoord)
            {
                if (vMap.ValidMapIndex(mapCoord.mapIdx))
                {
                    HeightMap heightMap = vMap.Maps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                    if (heightMap.GetHeight(mapCoord.coord.X, mapCoord.coord.Y) > 0.0f)
                    {
                        ColorMap colorMap = vMap.ColorMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                        if (colorMap != null)
                        {
                            ushort matIdx = colorMap[mapCoord.coord.X, mapCoord.coord.Y];
                            return matIdx == fromIdx;
                        }
                    }
                }
                return false;
            }
            public void DoAdd(MapCoord mapCoord)
            {
                Debug.Assert(vMap.ValidMapIndex(mapCoord.mapIdx));
                ColorMap colorMap = vMap.ColorMaps[mapCoord.mapIdx.X, mapCoord.mapIdx.Y];
                Debug.Assert(colorMap != null);
                colorMap[mapCoord.coord.X, mapCoord.coord.Y] = (ushort)toIdx;
            }
        }

        #endregion Internal

        #endregion MaterialSelect


        #region Internal
        #region GenerateList
        static private void FlipOrder(List<HeightMap> heightMaps)
        {
            int last = heightMaps.Count - 1;
            int half = (last + 1) / 2;
            for (int i = 0; i < half; ++i)
            {
                HeightMap t = heightMaps[i];
                heightMaps[i] = heightMaps[last - i];
                heightMaps[last - i] = t;
            }
        }
        private void AddMapToList(List<HeightMap> heightMaps, Point p)
        {
            if ((p.X >= 0)
                && (p.X < NumMaps.X)
                && (p.Y >= 0)
                && (p.Y < NumMaps.Y)
                && (Maps[p.X, p.Y] != null))
            {
                heightMaps.Add(Maps[p.X, p.Y]);
            }
        }
        private void ListMapsX(
            Vector2 p0, Vector2 p1, Vector2 dir,
            List<HeightMap> heightMaps)
        {
            p0 -= Min;
            p1 -= Min;
            p0 /= MapSize2D;
            p1 /= MapSize2D;

            Point lastAdded = new Point((int)p0.X, (int)p0.Y);
            AddMapToList(heightMaps, lastAdded);

            float firstStepX = (float)(Math.Ceiling(p0.X) - p0.X);
            if (firstStepX > 0.0f)
            {
                float firstStepY = dir.Y * firstStepX;
                Point nextAdded = new Point((int)(p0.X + firstStepX), (int)(p0.Y + firstStepY));
                if (nextAdded.Y != lastAdded.Y)
                {
                    AddMapToList(heightMaps, new Point(lastAdded.X, nextAdded.Y));
                }
                AddMapToList(heightMaps, nextAdded);
                lastAdded = nextAdded;
                p0 += new Vector2(firstStepX, firstStepY);
            }
            p0 += dir;

            while (p0.X < p1.X)
            {
                Point nextAdded = new Point((int)p0.X, (int)p0.Y);
                if (nextAdded.Y != lastAdded.Y)
                {
                    AddMapToList(heightMaps, new Point(lastAdded.X, nextAdded.Y));
                }
                AddMapToList(heightMaps, nextAdded);
                lastAdded = nextAdded;
                p0 += dir;
            }

            Point p = new Point((int)p0.X, (int)p0.Y);
            if (p != lastAdded)
            {
                AddMapToList(heightMaps, p);
            }
        }
        private void ListMapsY(
            Vector2 p0, Vector2 p1, Vector2 dir,
            List<HeightMap> heightMaps)
        {
            p0 -= Min;
            p1 -= Min;
            p0 /= MapSize2D;
            p1 /= MapSize2D;

            Point lastAdded = new Point((int)p0.X, (int)p0.Y);
            AddMapToList(heightMaps, lastAdded);

            float firstStepY = (float)(Math.Ceiling(p0.Y) - p0.Y);
            if (firstStepY > 0.0f)
            {
                float firstStepX = dir.X * firstStepY;
                Point nextAdded = new Point((int)(p0.X + firstStepX), (int)(p0.Y + firstStepY));
                if (nextAdded.X != lastAdded.X)
                {
                    AddMapToList(heightMaps, new Point(nextAdded.X, lastAdded.Y));
                }
                AddMapToList(heightMaps, nextAdded);
                lastAdded = nextAdded;
                p0 += new Vector2(firstStepX, firstStepY);
            }
            p0 += dir;

            while (p0.Y < p1.Y)
            {
                Point nextAdded = new Point((int)p0.X, (int)p0.Y);
                if (nextAdded.X != lastAdded.X)
                {
                    AddMapToList(heightMaps, new Point(nextAdded.X, lastAdded.Y));
                }
                AddMapToList(heightMaps, nextAdded);
                lastAdded = nextAdded;
                p0 += dir;
            }

            Point p = new Point((int)p0.X, (int)p0.Y);
            if (p != lastAdded)
            {
                AddMapToList(heightMaps, p);
            }
        }
        #endregion GenerateList

        #region RemappingIndices
        /// <summary>
        /// Determine whether these coordinate point into a non-null map.
        /// </summary>
        /// <param name="iMap"></param>
        /// <param name="jMap"></param>
        /// <param name="iCoord"></param>
        /// <param name="jCoord"></param>
        /// <returns></returns>
        private bool ValidMapCoord(int iMap, int jMap, int iCoord, int jCoord)
        {
            return (iMap >= 0)
                && (iMap < NumMaps.X)
                && (jMap >= 0)
                && (jMap < NumMaps.Y)
                && (maps[iMap, jMap] != null)
                && (iCoord >= 0)
                && (iCoord < PixPerMap)
                && (jCoord >= 0)
                && (jCoord < PixPerMap);
        }
        /// <summary>
        /// Check that the given index is in bounds and to an existing map.
        /// </summary>
        /// <param name="mapIdx"></param>
        /// <returns></returns>
        private bool ValidMapIndex(Point mapIdx)
        {
            return
                (mapIdx.X >= 0)
                && (mapIdx.Y >= 0)
                && (mapIdx.X < NumMaps.X)
                && (mapIdx.Y < NumMaps.Y)
                && (Maps[mapIdx.X, mapIdx.Y] != null);
        }

        /// <summary>
        /// Get the non-interpolated height at (iCoord,jCoord) from map(iMap, jMap).
        /// </summary>
        /// <param name="iMap"></param>
        /// <param name="jMap"></param>
        /// <param name="iCoord"></param>
        /// <param name="jCoord"></param>
        /// <returns>Height in world units</returns>
        private float GetHeight(int iMap, int jMap, int iCoord, int jCoord)
        {
            if (ValidMapCoord(iMap, jMap, iCoord, jCoord))
            {
                return maps[iMap, jMap].GetHeight(iCoord, jCoord);
            }
            return 0.0f;
        }

        /// <summary>
        /// Translate from virtual coordinate to map coord and coord within map.
        /// </summary>
        /// <param name="iVirt"></param>
        /// <param name="jVirt"></param>
        /// <param name="mapIdx"></param>
        /// <param name="coord"></param>
        /// <returns>True if in bounds</returns>
        private bool VirtualToCoord(int iVirt, int jVirt, out Point mapIdx, out Point coord)
        {
            Point virtSize = VirtualSize;
            if (Empty || (iVirt < 0) || (iVirt >= virtSize.X) || (jVirt < 0) || (jVirt >= virtSize.Y))
            {
                mapIdx = coord = new Point(0, 0);
                return false;
            }

            mapIdx = new Point(
                iVirt / PixPerMap,
                jVirt / PixPerMap);

            coord = new Point(
                iVirt - mapIdx.X * PixPerMap,
                jVirt - mapIdx.Y * PixPerMap);

            return true;
        }

        /// <summary>
        /// Return color map of input virtual coord and coordinate within that colorMap.
        /// </summary>
        /// <param name="iVirt"></param>
        /// <param name="jVirt"></param>
        /// <param name="coord"></param>
        /// <returns></returns>
        private ColorMap VirtualToColorMap(int iVirt, int jVirt, out Point coord)
        {
            Point mapIdx;
            if (!VirtualToCoord(iVirt, jVirt, out mapIdx, out coord))
            {
                return null;
            }

            return ColorMaps[mapIdx.X, mapIdx.Y];
        }
        /// <summary>
        /// Return height map of input virtual coord and coordinate within that heightMap.
        /// </summary>
        /// <param name="iVirt"></param>
        /// <param name="jVirt"></param>
        /// <param name="coord"></param>
        /// <returns></returns>
        private HeightMap VirtualToMap(int iVirt, int jVirt, out Point coord)
        {
            Point mapIdx;
            if (!VirtualToCoord(iVirt, jVirt, out mapIdx, out coord))
            {
                return null;
            }

            return Maps[mapIdx.X, mapIdx.Y];
        }

        /// <summary>
        /// Map world space to map coord and coord within that map and fractional distance for bilerp.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="coord"></param>
        /// <param name="frac"></param>
        /// <returns>Coordinates of map</returns>
        private Point WorldToMapCoords(Vector2 pos, out Point coord, out Vector2 frac)
        {
            if (Empty)
            {
                coord = new Point(0, 0);
                frac = Vector2.Zero;
                return new Point(0, 0);
            }
            Vector2 norm = new Vector2(
                (pos.X - Min.X) / MapSize.X,
                (pos.Y - Min.Y) / MapSize.Y);

            Point p = new Point(
                (int)norm.X,
                (int)norm.Y);

            Vector2 mapPos = new Vector2(
                (norm.X - p.X) * PixPerMap,
                (norm.Y - p.Y) * PixPerMap);

            coord = new Point(
                (int)mapPos.X,
                (int)mapPos.Y);

            frac = new Vector2(
                mapPos.X - coord.X,
                mapPos.Y - coord.Y);

            return p;
        }

        /// <summary>
        /// Translate world space to index of map.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Point WorldToMapIndex(Vector2 pos)
        {
            return new Point(
                (int)((pos.X - Min.X) / MapSize.X),
                (int)((pos.Y - Min.Y) / MapSize.Y));
        }

        /// <summary>
        /// Translate world space to unbounded virtual coordinates.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Point WorldToVirtualIndex(Vector2 pos)
        {
            if (Empty)
            {
                return new Point(0, 0);
            }
            Point p = new Point(
                (int)((pos.X - Min.X) / (Max.X - Min.X) * VirtualSize.X),
                (int)((pos.Y - Min.Y) / (Max.Y - Min.Y) * VirtualSize.Y));


            return p;
        }
        public Vector2 VirtualIndexToWorld(int iVirt, int jVirt)
        {
            if (Empty)
            {
                return new Vector2(0.0f, 0.0f);
            }
            Vector2 v2wScale = new Vector2(CubeSize);
            Vector2 v2wOffset = Min + new Vector2(CubeSize * 0.5f);

            return new Vector2(
                iVirt * v2wScale.X + v2wOffset.X,
                jVirt * v2wScale.Y + v2wOffset.Y);
        }

        /// <summary>
        /// Translate world space to virtual coordinates with fraction for bilerp.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="frac"></param>
        /// <returns></returns>
        private Point WorldToVirtualCoord(Vector2 pos, out Vector2 frac)
        {
            if (Empty)
            {
                frac = Vector2.Zero;
                return new Point(0, 0);
            }
            Vector2 remap = new Vector2(
                (pos.X - Min.X) / (Max.X - Min.X) * VirtualSize.X,
                (pos.Y - Min.Y) / (Max.Y - Min.Y) * VirtualSize.Y);

            Point p = new Point(
                (int)remap.X,
                (int)remap.Y);

            frac = new Vector2(
                remap.X - p.X,
                remap.Y - p.Y);

            return p;
        }

        /// <summary>
        /// Convert world space position to a (possibly off the grid) set
        /// of coordinates. NO CLAMPING HERE! DIY.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private MapCoord WorldToMapCoord(Vector2 pos)
        {
            if (Empty)
            {
                return new MapCoord();
            }
            Point virtCoord = new Point(
                (int)((pos.X - Min.X) / (Max.X - Min.X) * VirtualSize.X),
                (int)((pos.Y - Min.Y) / (Max.Y - Min.Y) * VirtualSize.Y));

            Point mapIdx = new Point(virtCoord.X / PixPerMap, virtCoord.Y / PixPerMap);
            Point coord = new Point(
                virtCoord.X - mapIdx.X * PixPerMap,
                virtCoord.Y - mapIdx.Y * PixPerMap);
            if (coord.X < 0)
            {
                Debug.Assert(coord.X < PixPerMap);
                --mapIdx.X;
                virtCoord.X += PixPerMap;
            }
            if (coord.Y < 0)
            {
                Debug.Assert(coord.Y < PixPerMap);
                --mapIdx.Y;
                virtCoord.Y += PixPerMap;
            }
            return new MapCoord(mapIdx, coord);
        }

        /// <summary>
        /// Convert a map coordinate to a world space position.
        /// </summary>
        /// <param name="mapCoord"></param>
        /// <returns></returns>
        private Vector2 MapCoordToWorld(MapCoord mapCoord)
        {
            return new Vector2(
                Min.X + mapCoord.mapIdx.X * MapSize.X + (mapCoord.coord.X + 0.5f) * CubeSize,
                Min.Y + mapCoord.mapIdx.Y * MapSize.Y + (mapCoord.coord.Y + 0.5f) * CubeSize);
        }
        /// <summary>
        /// Convert map coordinate plus height to 3D world space position.
        /// </summary>
        /// <param name="mapCoord"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private Vector3 MapCoordToWorld(MapCoord mapCoord, float h)
        {
            return new Vector3(MapCoordToWorld(mapCoord), h);
        }
        #endregion RemappingIndices


        #region Sorting
        private int[] waterFaceOrder = new int[Tile.NumFaces];
        private int[] OrderWaterFaces(Camera camera, Water water)
        {
            Vector3 viewPos = camera.ActualFrom;
            Vector3 viewDir = camera.ViewDir;

            int lo = 0;
            int hi = Tile.NumFaces - 1;

            if (viewPos.Z > water.BaseHeight)
            {
                waterFaceOrder[hi--] = (int)Tile.Face.Top;
            }
            else
            {
                waterFaceOrder[lo++] = (int)Tile.Face.Top;
            }
            for (int i = 0; i < Tile.NumFaces - 1; ++i)
            {
                waterFaceOrder[lo + i] = i + 1;
            }
            /// This is a good example of where it's better to be consistently
            /// wrong than right most of the time. The following gives the "best"
            /// order, but pops when the best order changes. The pops are worse
            /// than the artifacts the sorting tries to cover.
            //if (Math.Abs(viewDir.X) > Math.Abs(viewDir.Y))
            //{
            //    if (viewDir.X > 0.0f)
            //    {
            //        waterFaceOrder[lo++] = (int)Tile.Face.Right;
            //        waterFaceOrder[hi--] = (int)Tile.Face.Left;
            //    }
            //    else
            //    {
            //        waterFaceOrder[lo++] = (int)Tile.Face.Left;
            //        waterFaceOrder[hi--] = (int)Tile.Face.Right;
            //    }

            //    if (viewDir.Y > 0.0f)
            //    {
            //        waterFaceOrder[lo++] = (int)Tile.Face.Back;
            //        waterFaceOrder[hi--] = (int)Tile.Face.Front;
            //    }
            //    else
            //    {
            //        waterFaceOrder[lo++] = (int)Tile.Face.Front;
            //        waterFaceOrder[hi--] = (int)Tile.Face.Back;
            //    }
            //}
            //else
            //{
            //    if (viewDir.Y > 0.0f)
            //    {
            //        waterFaceOrder[lo++] = (int)Tile.Face.Back;
            //        waterFaceOrder[hi--] = (int)Tile.Face.Front;
            //    }
            //    else
            //    {
            //        waterFaceOrder[lo++] = (int)Tile.Face.Front;
            //        waterFaceOrder[hi--] = (int)Tile.Face.Back;
            //    }

            //    if (viewDir.X > 0.0f)
            //    {
            //        waterFaceOrder[lo++] = (int)Tile.Face.Right;
            //        waterFaceOrder[hi--] = (int)Tile.Face.Left;
            //    }
            //    else
            //    {
            //        waterFaceOrder[lo++] = (int)Tile.Face.Left;
            //        waterFaceOrder[hi--] = (int)Tile.Face.Right;
            //    }
            //}
            return waterFaceOrder;
        }
        #endregion Sorting

        #region LookupMaps
        /// <summary>
        /// Return the tile height neighborhood around the specified tile coordinate.
        /// </summary>
        private HeightMapNeighbors GetHeightNeighborhood(int i, int j)
        {
            HeightMapNeighbors hood = new HeightMapNeighbors();

            hood[HeightMapNeighbors.Dir.Center] = GetHeightMap(i, j);
            hood[HeightMapNeighbors.Dir.North] = GetHeightMap(i, j + 1);
            hood[HeightMapNeighbors.Dir.NorthEast] = GetHeightMap(i + 1, j + 1);
            hood[HeightMapNeighbors.Dir.East] = GetHeightMap(i + 1, j);
            hood[HeightMapNeighbors.Dir.SouthEast] = GetHeightMap(i + 1, j - 1);
            hood[HeightMapNeighbors.Dir.South] = GetHeightMap(i, j - 1);
            hood[HeightMapNeighbors.Dir.SouthWest] = GetHeightMap(i - 1, j - 1);
            hood[HeightMapNeighbors.Dir.West] = GetHeightMap(i - 1, j);
            hood[HeightMapNeighbors.Dir.NorthWest] = GetHeightMap(i - 1, j + 1);

            return hood;
        }
        /// <summary>
        /// Return the tile color neighborhood around the specified tile coordinate.
        /// </summary>
        private ColorMapNeighbors GetColorNeighborhood(int i, int j)
        {
            var hood = new ColorMapNeighbors();

            hood[ColorMapNeighbors.Dir.Center] = GetColorMap(i, j);
            hood[ColorMapNeighbors.Dir.North] = GetColorMap(i, j + 1);
            hood[ColorMapNeighbors.Dir.NorthEast] = GetColorMap(i + 1, j + 1);
            hood[ColorMapNeighbors.Dir.East] = GetColorMap(i + 1, j);
            hood[ColorMapNeighbors.Dir.SouthEast] = GetColorMap(i + 1, j - 1);
            hood[ColorMapNeighbors.Dir.South] = GetColorMap(i, j - 1);
            hood[ColorMapNeighbors.Dir.SouthWest] = GetColorMap(i - 1, j - 1);
            hood[ColorMapNeighbors.Dir.West] = GetColorMap(i - 1, j);
            hood[ColorMapNeighbors.Dir.NorthWest] = GetColorMap(i - 1, j + 1);

            return hood;
        }



        /// <summary>
        /// Return (possibly null) HeightMap at given coordinates, or NULL if 
        /// coordinates out of bounds.
        /// </summary>
        private HeightMap GetHeightMap(int i, int j)
        {
            return GetHeightMap(new Point(i, j));
        }
        /// <summary>
        /// Return (possibly null) heightmap at coord, or null if out of bounds.
        /// </summary>
        private HeightMap GetHeightMap(Point mapIdx)
        {
            if (ValidMapIndex(mapIdx))
            {
                return maps[mapIdx.X, mapIdx.Y];
            }
            return null;
        }

        /// <summary>
        /// Return (possibly null) colormap at coord, or null if out of bounds.
        /// </summary>
        private ColorMap GetColorMap(int i, int j)
        {
            return GetColorMap(new Point(i, j));
        }
        /// <summary>
        /// Return (possibly null) colormap at coord, or null if out of bounds.
        /// </summary>
        private ColorMap GetColorMap(Point mapIdx)
        {
            if (ValidMapIndex(mapIdx))
            {
                return ColorMaps[mapIdx.X, mapIdx.Y];
            }
            return null;
        }

        /// <summary>
        /// Return (possibly null) watermap at coord, or null if out of bounds.
        /// </summary>
        /// <param name="mapIdx"></param>
        /// <returns></returns>
        private WaterMap GetWaterMap(Point mapIdx)
        {
            if (ValidMapIndex(mapIdx))
            {
                return WaterMaps[mapIdx.X, mapIdx.Y];
            }
            return null;
        }
        #endregion LookupMaps


        #region Bookkeeping
        /// <summary>
        /// Compact down to given lo and hi rectangle.
        /// </summary>
        /// <param name="lo"></param>
        /// <param name="hi"></param>
        private void TrimGrid(Point lo, Point hi)
        {
            if ((lo.X > hi.X) || (lo.Y > hi.Y))
            {
                Maps = null;
                Tiles = null;
                ColorMaps = null;
                WaterMaps = null;
                WaterTiles = null;
                NumMaps = new Point(0, 0);
                Min = Vector2.Zero;
                Max = Vector2.Zero;
            }
            else if ((lo != new Point(0, 0)) || (hi != NumMaps))
            {
                Vector2 oldMin = Min;
                Vector2 oldMax = Max;
                HeightMap[,] oldMaps = Maps;
                Tile[,] oldTiles = Tiles;
                ColorMap[,] oldColors = ColorMaps;
                WaterMap[,] oldWaterMaps = WaterMaps;
                WaterTile[,] oldWaterTiles = WaterTiles;

                Min = oldMin + new Vector2(lo.X * MapSize.X, lo.Y * MapSize.Y);
                Max = oldMin + new Vector2((hi.X + 1) * MapSize.X, (hi.Y + 1) * MapSize.Y);

                NumMaps = new Point(
                    (int)Math.Round((Max.X - Min.X) / MapSize.X),
                    (int)Math.Round((Max.Y - Min.Y) / MapSize.Y));

                Maps = new HeightMap[NumMaps.X, NumMaps.Y];
                Tiles = new Tile[NumMaps.X, NumMaps.Y];
                ColorMaps = new ColorMap[NumMaps.X, NumMaps.Y];
                WaterMaps = new WaterMap[NumMaps.X, NumMaps.Y];
                WaterTiles = new WaterTile[NumMaps.X, NumMaps.Y];

                for (int j = 0; j < NumMaps.Y; ++j)
                {
                    for (int i = 0; i < NumMaps.X; ++i)
                    {
                        Maps[i, j] = oldMaps[i + lo.X, j + lo.Y];
                        Tiles[i, j] = oldTiles[i + lo.X, j + lo.Y];
                        ColorMaps[i, j] = oldColors[i + lo.X, j + lo.Y];
                        WaterMaps[i, j] = oldWaterMaps[i + lo.X, j + lo.Y];
                        WaterTiles[i, j] = oldWaterTiles[i + lo.X, j + lo.Y];
                    }
                }

                if (floodSelect != null)
                {
                    floodSelect.Remap(lo);
                }
            }
        }

        /// <summary>
        /// Update the z component of the specified maps bounds.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="minZ"></param>
        /// <param name="maxZ"></param>
        private void UpdateMapBound(int i, int j, float minZ, float maxZ)
        {
            Debug.Assert(maps[i, j] != null);
            maps[i, j].UpdateBox(minZ, maxZ);
        }

        /// <summary>
        /// Delete the map at that position.
        /// </summary>
        /// <param name="pos"></param>
        private void DeleteMap(Vector2 pos)
        {
            Point mapIdx = WorldToMapIndex(pos + 0.5f * MapSize2D);

            DeleteMap(mapIdx.X, mapIdx.Y);
        }

        /// <summary>
        /// Delete the map at given coordinate.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        private void DeleteMap(int i, int j)
        {
            Maps[i, j] = null;
            Tiles[i, j] = null;
            ColorMaps[i, j] = null;
            WaterMaps[i, j] = null;
            WaterTiles[i, j] = null;

            --MapCount;
        }

        /// <summary>
        /// Add given map with min corner at input swCorner.
        /// </summary>
        /// <param name="h"></param>
        /// <param name="swCorner"></param>
        private void AddMap(HeightMap h, Vector2 swCorner)
        {
            Vector2 neCorner = swCorner + MapSize2D;
            ExpandMaps(swCorner, neCorner);

            Point mapIdx = WorldToMapIndex(swCorner + 0.5f * MapSize2D);

            h.PositionBox(swCorner);

            Maps[mapIdx.X, mapIdx.Y] = h;

            Tile tile = new Tile(swCorner, neCorner);

            Tiles[mapIdx.X, mapIdx.Y] = tile;
            AddUpdate(tile);

            ColorMap color = new ColorMap(PixPerMap);
            ColorMaps[mapIdx.X, mapIdx.Y] = color;

            WaterMap waterMap = new WaterMap(PixPerMap);
            WaterMaps[mapIdx.X, mapIdx.Y] = waterMap;

            WaterTile waterTile = new WaterTile(swCorner, neCorner);
            WaterTiles[mapIdx.X, mapIdx.Y] = waterTile;

            ++MapCount;
        }

        /// <summary>
        /// Trim out outlying null map/tiles.
        /// </summary>
        private void CleanDeadMaps()
        {
            if (Maps != null)
            {
                // Find min and max corners of bounding rectangle of non-null tiles
                Point lo = NumMaps;
                Point hi = new Point(0, 0);

                for (int j = 0; j < NumMaps.Y; ++j)
                {
                    for (int i = 0; i < NumMaps.X; ++i)
                    {
                        if (Maps[i, j] != null)
                        {
                            lo.X = Math.Min(lo.X, i);
                            lo.Y = Math.Min(lo.Y, j);

                            hi.X = Math.Max(hi.X, i);
                            hi.Y = Math.Max(hi.Y, j);
                        }
                    }
                }

                TrimGrid(lo, hi);

                deadMaps = 0;
            }
        }

        /// <summary>
        /// Allocate a new heightmap at the specified world location.
        /// </summary>
        /// <param name="swCorner"></param>
        /// <returns></returns>
        private HeightMap NewMap(Vector2 swCorner)
        {
            return new HeightMap(
                swCorner,
                new Point(PixPerMap, PixPerMap),
                MapSize);
        }

        /// <summary>
        /// Queue up a tile to be refreshed.
        /// </summary>
        /// <param name="tile"></param>
        private void AddUpdate(Tile tile)
        {
            if ((tile != null) && !tile.Queued)
            {
                tile.Queued = true;
                updates.Enqueue(tile);
            }
        }

        /// <summary>
        /// Mark all tiles affected by this operation for update.
        /// </summary>
        /// <param name="mapIdx"></param>
        /// <param name="coord"></param>
        private void AddUpdate(Point mapIdx, Point coord)
        {
            AddUpdate(Tiles[mapIdx.X, mapIdx.Y]);

            if (coord.X <= 0)
            {
                if (mapIdx.X > 0)
                {
                    AddUpdate(Tiles[mapIdx.X - 1, mapIdx.Y]);
                }
            }
            if (coord.X >= (PixPerMap - 1))
            {
                if (mapIdx.X < NumMaps.X - 1)
                {
                    AddUpdate(Tiles[mapIdx.X + 1, mapIdx.Y]);
                }
            }
            if (coord.Y <= 0)
            {
                if (mapIdx.Y > 0)
                {
                    AddUpdate(Tiles[mapIdx.X, mapIdx.Y - 1]);
                }
            }
            if (coord.Y >= (PixPerMap - 1))
            {
                if (mapIdx.Y < NumMaps.Y - 1)
                {
                    AddUpdate(Tiles[mapIdx.X, mapIdx.Y + 1]);
                }
            }
        }

        /// <summary>
        /// Optimization to find which materials are used and which we can skip.
        /// Piggybacking in to get total land bounds.
        /// </summary>
        private void UpdateMaterialUsage()
        {
            landBounds.Min = new Vector3(float.MaxValue);
            landBounds.Max = new Vector3(float.MinValue);

            TerrainMaterial.ResetUsers();
            if (Tiles != null)
            {
                foreach (Tile tile in Tiles)
                {
                    if (tile != null)
                    {
                        tile.UpdateMaterialUsage();
                        if (tile.Bounds != null)
                        {
                            landBounds.Union(tile.Bounds);
                        }
                    }
                }
            }

            if (landBounds.Min.X >= landBounds.Max.X)
            {
                landBounds.Set(Vector3.Zero, Vector3.Zero);
            }
        }

        /// <summary>
        /// Queue up all waters to be rebuilt
        /// </summary>
        private void RebuildAllWater()
        {
            foreach (Water water in Water.AllWaters)
            {
                AddForErase(water);
                AddForCreate(water);
            }
        }
        /// <summary>
        /// Dispose all terrain dependent device resources
        /// </summary>
        private void DisposeAll()
        {
            if (Tiles != null)
            {
                Debug.Assert(WaterTiles != null, "WaterTiles null when Tiles isn't");
                for (int j = 0; j < NumMaps.Y; ++j)
                {
                    for (int i = 0; i < NumMaps.X; ++i)
                    {
                        WaterTile waterTile = WaterTiles[i, j];
                        if (waterTile != null)
                        {
                            waterTile.Dispose();
                            waterTile = null;
                        }
                    }
                }
                foreach (Tile tile in Tiles)
                {
                    if (tile != null)
                    {
                        tile.Dispose();
                    }
                }
            }
        }


        #endregion Bookkeeping

        #endregion INTERNAL
    }
}
