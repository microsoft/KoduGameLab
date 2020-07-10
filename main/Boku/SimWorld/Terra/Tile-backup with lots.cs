// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;

namespace Boku.SimWorld
{
    /// <summary>
    /// Contains a single chunk of terrain 65x65 vertices in size.  Note, if you go to
    /// a bigger tile size you need to change the index buffer to use 32 bit indices
    /// rather than 16 bit.
    /// Actually, 129x129 will also fit, but then small world (only 65x65 heightmap)
    /// will die with a truncation problem coming up with 0x0 tiles - maf
    /// 
    /// With the change to allowing for hidden tiles skirts and water have been integrated
    /// into the Tile class.
    /// </summary>
    public class Tile : INeedsDeviceReset
    {
        private const int tileSize = 65;
        private static Terrain.TerrainVertex[,] vertices = null;
        private static Vector2[,] kurvatures = null;

        private VertexBuffer vbuf = null;
        private int stride = 0;

        private Vector3 position = new Vector3();   // Offset from origin for this tile in world coordinates.
        private int lod = 0;                        // Current LOD level.
        private AABB box;

        private static int width;       // Size of this tile in meters.
        private static int height;      // ibid.
        private int indexX = -1;        // Indices of this tile into the terrain's tiles array.
        private int indexY = -1;

        private bool hidden = false;    // Hidden tiles exist but aren't rendered.

        private Skirt skirt = null;     // Each tile has it's own skirt geometry.
        private Water water = null;

        private static int downSample = -2;

        #region Accessors
        public AABB Box
        {
            get { return box; }
        }
        public Skirt Skirt
        {
            get { return skirt; }
        }
        public Water Water
        {
            get { return water; }
        }
        public VertexBuffer VBuf
        {
            get { return vbuf; }
        }
        static public int BuffSize
        {
            get { return tileSize; }
        }
        public int VertSize
        {
            get { return BuffSize; }
        }
        static public int MapSize
        {
            get { return VtxToMap(tileSize - 1) | 1; }
        }
        public int Stride
        {
            get { return stride; }
        }
        public int NumTriangles
        {
            get { return (BuffSize - 1) * (BuffSize - 1) * 2; }
        }
        public int NumVertices
        {
            get { return BuffSize * BuffSize; }
        }
        public bool Hidden
        {
            get { return hidden; }
            set { hidden = value; }
        }
        public Vector3 Position
        {
            get { return position; }
        }
        public int X
        {
            get { return indexX; }
        }
        public int Y
        {
            get { return indexY; }
        }
        static public int MapToVtx(int n)
        {
            return downSample < 0
            ? (n << -downSample)
            : (n >> downSample);
        }
        static public int VtxToMap(int n)
        {
            return downSample < 0
            ? (n >> -downSample)
            : (n << downSample);
        }
        #endregion

        private Tile()
        {
        }   // end of Tile c'tor

        static public Point HeightMapSize(HeightMap heightMap)
        {
            Point heightMapSize;

            heightMapSize.X = MapToVtx(heightMap.Size.X - 1) | 1;
            heightMapSize.Y = MapToVtx(heightMap.Size.Y - 1) | 1;

            return heightMapSize;
        }

        static public void MakeTiles(Terrain terrain, ref Tile[,] tiles)
        {
            HeightMap heightMap = terrain.HeightMap;
            Point heightMapSize = HeightMapSize(heightMap);

            width = heightMapSize.X / (BuffSize - 1);
            height = heightMapSize.Y / (BuffSize - 1);

            terrain.NumTiles = new Point(width, height);

            if (terrain.WaterParticleEmitter != null)
            {
                terrain.WaterParticleEmitter.Active = false;
            }

            // Allocate the tiles.
            tiles = new Tile[width, height];
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    tiles[i, j] = new Tile();
                    tiles[i, j].stride = System.Runtime.InteropServices.Marshal.SizeOf(new Terrain.TerrainVertex());
                    tiles[i, j].indexX = i;
                    tiles[i, j].indexY = j;
                    tiles[i, j].position = new Vector3(i * heightMap.Scale.X / width, j * heightMap.Scale.Y / height, 0.0f); 

                    Point offset = new Point(i * (BuffSize - 1), j * (BuffSize - 1));

                    // Create the skirt for this tile and update it to match the heightmap.
                    tiles[i, j].skirt = new Skirt(heightMap, offset);
                }
            }

            // Mark hidden tiles as such.
            if (terrain.XmlTerrainData.hiddenTiles != null)
            {
                for (int i = 0; i < terrain.XmlTerrainData.hiddenTiles.Length; i++)
                {
                    Point tile = (Point)(terrain.XmlTerrainData.hiddenTiles[i]);
                    tiles[tile.X, tile.Y].Hidden = true;
                }
            }

            // Now create the water for each tile.
            if (terrain.XmlTerrainData.hasWater)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        Point offset = new Point(i * (BuffSize - 1), j * (BuffSize - 1));
                        tiles[i, j].water = new Water(terrain, offset);
                    }
                }
            }

            // Allocate the big array of vertices covering all of the terrain.
            vertices = new Terrain.TerrainVertex[heightMapSize.X, heightMapSize.Y];
            // Kurvatures map registered with heightMap, interpolated for vertices.
            kurvatures = new Vector2[heightMap.Size.X, heightMap.Size.Y];

            UpdateTileData(heightMap);

        }   // end of Tile MakeTiles()

        static public void UpdateTileData(HeightMap heightMap)
        {
            UpdateTileData(heightMap, new Point(0, 0), new Point(heightMap.Size.X - 1, heightMap.Size.Y - 1));
        }   // end of Tile UpdateTileData

        protected delegate bool Relaxation(HeightMap heightMap, Point point);

        private static Point currRelax = new Point(0, 0);
        private static bool doneAny = false;
        //static public void RelaxTileData(HeightMap heightMap)
        //{
        //    int numToDo = 1000;
            
        //    int numDone = 0;
        //    while (numDone < numToDo)
        //    {
        //        // Sum the forces.
        //        // Forces are:
        //        //  if there's a left neighbor
        //        //      spring force to/from left neighbor
        //        //  else
        //        //      don't allow left/right motion
        //        // etc. for each of the 4 cardinal neighbors
        //        //  Gravity times slope of gradient of kurvature
        //        Vector2 force = new Vector2(0.0f, 0.0f);

        //        int i = currRelax.X;
        //        if (i > 0)
        //        {
        //            Vector2 
        //        }
        //        int j = currRelax.Y;

        //        if (++currRelax.X >= heightMap.Size.X)
        //        {
        //            currRelax.X = 0;
        //            if (++currRelax.Y >= heightMap.Size.Y)
        //            {
        //                currRelax.Y = 0;
        //            }
        //        }
        //    }
        //}
        static protected void AccumForceK(Vector3 del, ref Vector2 force)
        {
            float kSpring = 0.1f;
            //float len = del.Length();
            //if (len > 0)
            //{
            //    Vector2 del2 = new Vector2(del.X, del.Y);
            //    del2.Normalize();

            //    force.X += del2.X * len * kSpring;
            //    force.Y += del2.Y * len * kSpring;
            //}
            force.X += del.X * kSpring;
            force.Y += del.Y * kSpring;
        }
        static protected void RangeIndices(int i, int di,
            out int ib, out int i0, out int i1, out int i2)
        {
            ib = i - di;
            i0 = i;
            i1 = i + di;
            i2 = i1 + di;
        }
        static bool OutOfRange(int i, int di, int sz)
        {
            if (i - di < 0)
                return true;
            if (i + 2 * di >= sz)
                return true;
            return false;
        }
        static protected float Kurvature(Point sz, int i, int j, int di, int dj)
        {
            if (OutOfRange(i, di, sz.X) || OutOfRange(j, dj, sz.Y))
            {
                return 0.0f;
            }
            int ib, i0, i1, i2;
            RangeIndices(i, di, out ib, out i0, out i1, out i2);

            int jb, j0, j1, j2;
            RangeIndices(j, dj, out jb, out j0, out j1, out j2);

            Vector3 hb = vertices[ib, jb].position;
            Vector3 h0 = vertices[i0, j0].position;
            Vector3 h1 = vertices[i1, j1].position;
            Vector3 h2 = vertices[i2, j2].position;

            float sb, s1, s2;
            if (di > 0)
            {
                sb = h0.X - hb.X;
                s1 = h1.X - h0.X;
                s2 = h2.X - h1.X;
            }
            else
            {
                sb = h0.Y - hb.Y;
                s1 = h1.Y - h0.Y;
                s2 = h2.Y - h1.Y;
            }

            float kurv =
                h2.Z / (s1 * s1 * s2)
                - h1.Z * (1.0f / (s1 * s1 * s2) + 1.0f / (s1 * s1 * s1) + 1.0f / (s1 * s1 * sb))
                + h0.Z * (1.0f / (s1 * s1 * s1) + 1.0f / (s1 * s1 * sb) + 1.0f / (s1 * sb * sb))
                - hb.Z / (s1 * sb * sb);


            float kEnergy = 0.1f;

            float kLowK = 0.25f;
            return (kurv > kLowK 
                ? s1 
                : kurv < -kLowK
                    ? -sb
                    : 0.0f) * kEnergy;
        }
        static protected float K2(Terrain.TerrainVertex v0, Terrain.TerrainVertex v1)
        {
            Vector3 delPos = v0.position - v1.position;

            float cosTwoTheta = Vector3.Dot(v0.normal, v1.normal);
            float twoSinTheta = MathHelper.Clamp((2.0f * (1.0f - cosTwoTheta)), 0.0f, 1.0f);
            float distSq = delPos.LengthSquared();
            Debug.Assert(distSq > 0.0f);
            return (float)Math.Sqrt(twoSinTheta / distSq);
        }
        static protected float K2(Point sz, int i, int j, int di, int dj)
        {
            if (OutOfRange(i, di, sz.X) || OutOfRange(j, dj, sz.Y))
            {
                return 0.0f;
            }
            Terrain.TerrainVertex vback = vertices[i - di, j - dj];
            Terrain.TerrainVertex vhere = vertices[i, j];
            Terrain.TerrainVertex vfore = vertices[i + di, j + dj];
            float kBack = K2(vback, vhere);
            float kFore = K2(vhere, vfore);
            
            if (kBack * kFore <= 0.0f)
                return 0.0f;

            float step = 0.0f;
            if (kBack > kFore)
            {
                step = di > 0 
                    ? vback.position.X - vhere.position.X 
                    : vback.position.Y - vhere.position.Y;
                step *= (kBack - kFore) / kBack;
            }
            else
            {
                step = di > 0
                    ? vfore.position.X - vhere.position.X
                    : vfore.position.Y - vhere.position.Y;
                step *= (kFore - kBack) / kFore;
            }
            float kEnergy = 0.1f;
            float kMinStep = 0.01f;
            step *= kEnergy;

            Debug.Assert(!float.IsNaN(step));
            Debug.Assert(!float.IsInfinity(step));
            if (Math.Abs(step) < kMinStep)
                return 0.0f;
            return step;
        }
        static protected Vector2 KurvEnergy(HeightMap heightMap, int i, int j)
        {
            Point heightMapSize = HeightMapSize(heightMap);

            float kurvX = K2(heightMapSize, i, j, 1, 0);

            float kMaxStepX = heightMap.Scale.X / (heightMapSize.X - 1);
            kurvX = MathHelper.Clamp(kurvX, -kMaxStepX, kMaxStepX);

            float kurvY = K2(heightMapSize, i, j, 0, 1);

            float kMaxStepY = heightMap.Scale.Y / (heightMapSize.Y - 1);
            kurvY = MathHelper.Clamp(kurvY, -kMaxStepY, kMaxStepY);

            return new Vector2(kurvX, kurvY);
        }
        static protected void SetKurvMap(HeightMap heightMap, Point inMin, Point inMax)
        {
            Vector2 hi = new Vector2(0.0f, 0.0f);

            Point min;
            Point max;
            min.X = Math.Max(1, VtxToMap(inMin.X));
            min.Y = Math.Max(1, VtxToMap(inMin.Y));
            max.X = Math.Min(heightMap.Size.X-1, VtxToMap(inMax.X));
            max.Y = Math.Min(heightMap.Size.Y-1, VtxToMap(inMax.Y));
            for (int j = min.Y; j < max.Y; ++j)
            {
                //// First along X
                //for (int i = min.X; i <= max.X; ++i)
                //{
                //    float hxbb = heightMap.GetHeight(i - 2, j);
                //    float hxb = heightMap.GetHeight(i - 1, j);
                //    float hxf = heightMap.GetHeight(i + 1, j);
                //    float hxff = heightMap.GetHeight(i + 2, j);

                //    kurvatures[i, j].X = -hxbb
                //                        + 2.0f * hxb
                //                        - 2.0f * hxf
                //                        + hxff;
                //    hi.X = Math.Max(Math.Abs(kurvatures[i, j].X), hi.X);

                //    float hybb = heightMap.GetHeight(i, j - 2);
                //    float hyb = heightMap.GetHeight(i, j - 1);
                //    float hyf = heightMap.GetHeight(i, j + 1);
                //    float hyff = heightMap.GetHeight(i, j + 2);

                //    kurvatures[i, j].Y = -hybb
                //                        + 2.0f * hyb
                //                        - 2.0f * hyf
                //                        + hyff;

                //    hi.Y = Math.Max(Math.Abs(kurvatures[i, j].Y), hi.Y);
                //}
                // First along X
                for (int i = min.X; i <= max.X; ++i)
                {
                    float hxb = heightMap.GetHeight(i - 1, j);
                    float hx0 = heightMap.GetHeight(i, j);
                    float hxf = heightMap.GetHeight(i + 1, j);

                    kurvatures[i, j].X = hxb
                                        - 2.0f * hx0
                                        + hxf;
                    hi.X = Math.Max(Math.Abs(kurvatures[i, j].X), hi.X);

                    float hyb = heightMap.GetHeight(i, j - 1);
                    float hy0 = hx0;
                    float hyf = heightMap.GetHeight(i, j + 1);

                    kurvatures[i, j].Y = hyb
                                        - 2.0f * hy0
                                        + hyf;

                    hi.Y = Math.Max(Math.Abs(kurvatures[i, j].Y), hi.Y);
                }
            }
        }
        static protected bool RelaxKurv(HeightMap heightMap, Point point)
        {
            // Next version, we'll fake kurvature potential energy with height

            Point heightMapSize = HeightMapSize(heightMap);

            Vector2 force = Vector2.Zero;

            bool stepped = false;
            int i = point.X;
            int j = point.Y;
            bool inX = false;
            bool inY = false;
            if ((i > 0) && (i < heightMapSize.X - 1))
            {
                AccumForceK((vertices[i - 1, j].position - vertices[i, j].position), ref force);

                AccumForceK((vertices[i + 1, j].position - vertices[i, j].position), ref force);

                inX = true;
            }
            if ((j > 0) && (j < heightMapSize.Y - 1))
            {
                AccumForceK((vertices[i, j - 1].position - vertices[i, j].position), ref force);

                AccumForceK((vertices[i, j + 1].position - vertices[i, j].position), ref force);

                inY = true;
            }
            if (inX && inY)
            {
                if (((i + j) & 1) == 0)
                {
                    AccumForceK(vertices[i - 1, j - 1].position - vertices[i, j].position, ref force);
                    AccumForceK(vertices[i + 1, j + 1].position - vertices[i, j].position, ref force);
                    AccumForceK(vertices[i - 1, j + 1].position - vertices[i, j].position, ref force);
                    AccumForceK(vertices[i + 1, j - 1].position - vertices[i, j].position, ref force);
                }

                force += KurvEnergy(heightMap, i, j);
            }

            float kMinLenSq = 0.00001f;
            float lenSq = force.LengthSquared();
            if (lenSq > kMinLenSq)
            {
//                force.X = force.Y = 0.0f;

                vertices[i, j].position.X += force.X;
                vertices[i, j].position.Y += force.Y;

                vertices[i, j].position = heightMap.GetVertex(vertices[i, j].position, ref vertices[i, j].normal);

                Vector2 kurv = KurvEnergy(heightMap, i, j);

                kurv *= 10.0f;
                vertices[i, j].morphTargetNormal.X = kurv.X;
                vertices[i, j].morphTargetNormal.Y = kurv.Y;
                vertices[i, j].morphTargetNormal.Z = 0.0f;

                stepped = true;
            }

            return stepped;
        }
        static protected Vector3 ReCenter(ref Vector3 posBack, ref Vector3 pos0, ref Vector3 posFore)
        {
            float drag = 0.25f;
            Vector3 delBack = posBack - pos0;
            Vector3 delFore = posFore - pos0;

            float lenSqBack = delBack.LengthSquared();
            float lenSqFore = delFore.LengthSquared();

            if (lenSqBack > lenSqFore)
            {
                float t = (lenSqBack - lenSqFore)
                    / (2.0f * (lenSqBack - Vector3.Dot(delBack, delFore)))
                    * drag;
                pos0 += t * delBack;
            }
            else
            {
                float t = (lenSqFore - lenSqBack)
                    / (2.0f * (lenSqFore - Vector3.Dot(delBack, delFore)))
                    * drag;
                pos0 += t * delFore;
            }
            return pos0;
        }
        static protected void ReCenter(HeightMap heightMap, int i, int j)
        {
            Point heightMapSize = HeightMapSize(heightMap);

            bool inX = false;
            if ((i > 0) && (i < heightMapSize.X - 1))
            {
                ReCenter(ref vertices[i - 1, j].position, 
                    ref vertices[i, j].position, 
                    ref vertices[i + 1, j].position);

                inX = true;
            }
            bool inY = false;
            if ((j > 0) && (j < heightMapSize.Y - 1))
            {
                ReCenter(ref vertices[i, j - 1].position,
                    ref vertices[i, j].position,
                    ref vertices[i, j + 1].position);

                inY = true;
            }
            if (inX && inY)
            {
                if (((i + j) & 1) == 0)
                {
                    ReCenter(ref vertices[i - 1, j - 1].position,
                        ref vertices[i, j].position,
                        ref vertices[i + 1, j + 1].position);

                    ReCenter(ref vertices[i - 1, j + 1].position,
                        ref vertices[i, j].position,
                        ref vertices[i + 1, j - 1].position);
                }
            }

            if (inX || inY)
            {
                vertices[i, j].position = heightMap.GetVertex(vertices[i, j].position, ref vertices[i, j].normal);
            }
        }
        static protected Vector2 KurvatureLUT(HeightMap heightMap, Vector3 pos)
        {
            return KurvatureLUT(heightMap, pos.X, pos.Y);
        }
        static Vector2 TempLerp(Vector2 a, Vector2 b, float f)
        {
            return a + f * (b - a);
        }
        static protected Vector2 KurvatureLUT(HeightMap heightMap, float x, float y)
        {
            // Remap to "texel" coordinates.
            x *= (heightMap.Size.X - 1) / heightMap.Scale.X;
            y *= (heightMap.Size.Y - 1) / heightMap.Scale.Y;

            // Get the integral and fraction parts of the position.
            int i = (int)x;
            if (i > heightMap.Size.X - 2)
                i = heightMap.Size.X - 2;
            int j = (int)y;
            if (j > heightMap.Size.Y - 2)
                j = heightMap.Size.Y - 2;
            float dx = x - i;
            float dy = y - j;

            // Calc the bilinearly weighted sample.
            Vector2 result = TempLerp(
                TempLerp(kurvatures[i, j],
                            kurvatures[i + 1, j], dx),
                TempLerp(kurvatures[i, j + 1], 
                            kurvatures[i + 1, j + 1], dx), 
                       dy);

            return result;
        }
        static protected bool RelaxBrute(HeightMap heightMap, Point point)
        {
            int i = point.X;
            int j = point.Y;
//            Vector2 kurv = KurvEnergy(heightMap, i, j);
            Vector2 kurv = KurvatureLUT(heightMap, vertices[i, j].position);

            if (false && kurv.Length() > 0.0f)
            {
                vertices[i, j].position.X += kurv.X;
                vertices[i, j].position.Y += kurv.Y;
                vertices[i, j].position = heightMap.GetVertex(vertices[i, j].position, ref vertices[i, j].normal);
            }

            kurv *= 10.0f;

            vertices[i, j].morphTargetNormal.X = Math.Abs(kurv.X);
            vertices[i, j].morphTargetNormal.Y = Math.Abs(kurv.Y);
            vertices[i, j].morphTargetNormal.Z = 0.0f;

            ReCenter(heightMap, i, j);

            return true;
        }
        static protected void AccumForce(Vector3 del, ref Vector2 force)
        {
            float kEnergy = 0.01f;
            float kSpring = 0.01f;
            float len = del.Length();
            if (len > 0)
            {
                Vector2 del2 = new Vector2(del.X, del.Y);
                del2.Normalize();

                force.X += del2.X * len * kSpring;
                force.Y += del2.Y * len * kSpring;

                //if (del.Z > 0.0f)
                //{
                //    del2 *= del.Z * kEnergy;
                //    force += del2;
                //}
            }
        }
        static protected bool RelaxHeight(HeightMap heightMap, Point point)
        {
            // Next version, we'll fake kurvature potential energy with height

            Point heightMapSize = HeightMapSize(heightMap);

            Vector2 force = Vector2.Zero;

            bool stepped = false;
            int i = point.X;
            int j = point.Y;
            bool inX = false;
            bool inY = false;
            if ((i > 0) && (i < heightMapSize.X - 1))
            {
                AccumForce((vertices[i - 1, j].position - vertices[i, j].position), ref force);

                AccumForce((vertices[i + 1, j].position - vertices[i, j].position), ref force);

                inX = true;
            }
            if ((j > 0) && (j < heightMapSize.Y - 1))
            {
                AccumForce((vertices[i, j - 1].position - vertices[i, j].position), ref force);
                
                AccumForce((vertices[i, j + 1].position - vertices[i, j].position), ref force);
                
                inY = true;
            }
            if (inX && inY)
            {
                if (((i + j) & 1) == 0)
                {
                    AccumForce(vertices[i - 1, j - 1].position - vertices[i, j].position, ref force);
                    AccumForce(vertices[i + 1, j + 1].position - vertices[i, j].position, ref force);
                    AccumForce(vertices[i - 1, j + 1].position - vertices[i, j].position, ref force);
                    AccumForce(vertices[i + 1, j - 1].position - vertices[i, j].position, ref force);
                }
            }

            float kMinLenSq = 0.00001f;
            float lenSq = force.LengthSquared();
            if (lenSq > kMinLenSq)
            {
                vertices[i, j].position.X += force.X;
                vertices[i, j].position.Y += force.Y;

                vertices[i, j].position = heightMap.GetVertex(vertices[i, j].position, ref vertices[i, j].normal);

                stepped = true;
            }

            return stepped;
        }

        static protected bool RelaxPoint(HeightMap heightMap, Point point)
        {
            // For first version, we'll just try even spacing.
            Point heightMapSize = HeightMapSize(heightMap);

            bool stepped = false;
            float kStep = 0.1f;
            float kLerp = 0.1f;
            int i = point.X;
            int j = point.Y;
            if (i > 0)
            {

                if (i < heightMapSize.X - 1)
                {
                    Vector3 delLeft = (vertices[i - 1, j].position - vertices[i, j].position);
                    float lenLeft = delLeft.Length();

                    Vector3 delRight = (vertices[i + 1, j].position - vertices[i, j].position);
                    float lenRight = delRight.Length();

                    float diff = lenRight - lenLeft;
                    if (diff > kStep)
                    {
                        vertices[i, j].position.X += (vertices[i + 1, j].position.X - vertices[i, j].position.X) * kLerp;
                        stepped = true;
                    }
                    else if (diff < -kStep)
                    {
                        vertices[i, j].position.X += (vertices[i - 1, j].position.X - vertices[i, j].position.X) * kLerp;
                        stepped = true;
                    }
                }
            }
            if (j > 0)
            {
                if (j < heightMapSize.Y - 1)
                {
                    Vector3 delDown = (vertices[i, j - 1].position - vertices[i, j].position);
                    float lenDown = delDown.Length();

                    Vector3 delUp = (vertices[i, j + 1].position - vertices[i, j].position);
                    float lenUp = delUp.Length();

                    float diff = lenUp - lenDown;
                    if (diff > kStep)
                    {
                        vertices[i, j].position.Y += (vertices[i, j + 1].position.Y - vertices[i, j].position.Y) * kLerp;
                        stepped = true;
                    }
                    else if (diff < -kStep)
                    {
                        vertices[i, j].position.Y += (vertices[i, j - 1].position.Y - vertices[i, j].position.Y) * kLerp;
                        stepped = true;
                    }
                }
            }

            if (stepped)
                vertices[i, j].position = heightMap.GetVertex(vertices[i, j].position, ref vertices[i, j].normal);

            return stepped;
        }
        static protected bool AdvancePoint(Point heightMapSize, ref Point point)
        {
            if ((point.X += 2) >= heightMapSize.X)
            {
                point.X = point.X % heightMapSize.X;
                if ((point.Y += 2) >= heightMapSize.Y)
                {
                    point.Y = point.Y % heightMapSize.Y;
                }
            }
            if ((point.X == 0) && (point.Y == 0))
            {
                if (!doneAny)
                {
                    return true;
                }
                doneAny = false;
            }
            return false;
        }
        static protected bool RelaxNull(HeightMap heightMap, Point point)
        {
            return true;
        }
        static public bool RelaxTileData(HeightMap heightMap)
        {
            int numToDo = 10000;
            Point heightMapSize = HeightMapSize(heightMap);

            Relaxation relax = RelaxNull;
            int relaxType = 4;
            switch (relaxType)
            {
                case 0:
                    relax = RelaxNull;
                    break;
                case 1:
                    relax = RelaxPoint;
                    break;
                case 2:
                    relax = RelaxHeight;
                    break;
                case 3:
                    relax = RelaxKurv;
                    break;
                case 4:
                    relax = RelaxBrute;
                    break;
            }
            int numDone = 0;
            bool allDone = false;
            while (numDone < numToDo)
            {
                // Sum the forces.
                // Forces are:
                //  if there's a left neighbor
                //      spring force to/from left neighbor
                //  else
                //      don't allow left/right motion
                // etc. for each of the 4 cardinal neighbors
                //  Gravity times slope of gradient of kurvature

                if (relax(heightMap, currRelax))
                    doneAny = true;

                allDone = AdvancePoint(heightMapSize, ref currRelax);
                if (allDone)
                    break;

                ++numDone;
            }
            return allDone;
        }

        /// <summary>
        /// Updates the tile data from new heightmap info.
        /// </summary>
        /// <param name="heightMap"></param>
        /// <param name="min">min point that needs updating.</param>
        /// <param name="max">max point (+1) that needs updating.</param>
        static public void UpdateTileData(HeightMap heightMap, Point min, Point max)
        {
            min.X = MapToVtx(min.X);
            min.Y = MapToVtx(min.Y);
            max.X = MapToVtx(max.X);
            max.Y = MapToVtx(max.Y);
            Point heightMapSize = HeightMapSize(heightMap);

            // Set the vertex postions.
            for (int j = min.Y; j <= max.Y; j++)
            {
                for (int i = min.X; i <= max.X; i++)
                {
                    float x = i / (float)(heightMapSize.X - 1) * heightMap.Scale.X;
                    float y = j / (float)(heightMapSize.Y - 1) * heightMap.Scale.Y;
                    vertices[i, j].position = heightMap.GetVertex(x, y, ref vertices[i, j].normal);
                    vertices[i, j].morphTargetZ = vertices[i, j].position.Z;
                }
            }
            SetKurvMap(heightMap, min, max);

//            float[,] wgts = { {0.25f, 0.5f, 0.25f},
//                                {0.5f, 1.0f, 0.5f},
//                                {0.25f, 0.5f, 0.25f} };
//            // Calc vertex normals.
//            for (int j = min.Y; j < max.Y; j++)
//            {
//                for (int i = min.X; i < max.X; i++)
//                {
//                    // The delta in world coordinates between vertices.
//                    float baseDX = heightMap.Scale.X / (heightMapSize.X - 1);
//                    float baseDY = heightMap.Scale.Y / (heightMapSize.Y - 1);

//                    float dx = 0;
//                    float dy = 0;
//                    float dzX = 0;  // dz in the x direction.
//                    float dzY = 0;  // dz in the y direction.

//                    if (i < heightMapSize.X - 1)
//                    {
//                        for (int k = Math.Max(0, j - 1); k < Math.Min(j + 1, heightMapSize.Y); ++k)
//                        {
//                            dx += baseDX;
//                            dzX += vertices[i + 1, k].position.Z - vertices[i, k].position.Z;
//                        }
//                    }
//                    if (i > 0)
//                    {
//                        for (int k = Math.Max(0, j - 1); k < Math.Min(j + 1, heightMapSize.Y); ++k)
//                        {
//                            dx += baseDX;
//                            dzX += vertices[i, k].position.Z - vertices[i - 1, k].position.Z;
//                        }
//                    }

//                    if (j < heightMapSize.Y - 1)
//                    {
//                        for (int k = Math.Max(0, i - 1); k < Math.Min(i + 1, heightMapSize.X); ++k)
//                        {
//                            dy += baseDY;
//                            dzY += vertices[k, j + 1].position.Z - vertices[k, j].position.Z;
//                        }
//                    }
//                    if (j > 0)
//                    {
//                        for (int k = Math.Max(0, i - 1); k < Math.Min(i + 1, heightMapSize.X); ++k)
//                        {
//                            dy += baseDY;
//                            dzY += vertices[k, j].position.Z - vertices[k, j - 1].position.Z;
//                        }
//                    }

//                    // Now use the deltas to calc the actual vertex normal.
//                    // First, the X component.
//                    Vector2 normal = new Vector2(-dzX, dx);
////                    normal.Normalize();
//                    vertices[i, j].normal.X = normal.X;
//                    vertices[i, j].normal.Z = normal.Y;
//                    // Then the Y.
//                    normal = new Vector2(-dzY, dy);
////                    normal.Normalize();
//                    vertices[i, j].normal.Y = normal.X;
//                    vertices[i, j].normal.Z += normal.Y;
//                    // And finally normalize.
//                    vertices[i, j].normal.Normalize();
//                    vertices[i, j].morphTargetNormal = vertices[i, j].normal;
//                }
//            }
            //for (int j = min.Y + 1; j < max.Y - 1; ++j)
            //{
            //    for (int i = min.X + 1; i < max.X - 1; ++i)
            //    {
            //        Vector3 normal = Vector3.Zero;
            //        for (int jj = j - 1; jj <= j + 1; ++jj)
            //        {
            //            for (int ii = i - 1; ii <= i + 1; ++ii)
            //            {
            //                normal += vertices[ii, jj].normal * wgts[ii - i + 1, jj - j + 1];
            //            }
            //        }
            //        normal.Normalize();
            //        vertices[i, j].normal = normal;
            //        vertices[i, j].morphTargetNormal = normal;
            //    }
            //}

            // TODO (scoy) : Calc the proper morphTargetZ and morphTargetNormal values.

        }   // end of Tile UpdateTileData()

        public void LoadGraphicsContent(GraphicsDeviceManager graphics)
        {
            GraphicsDevice device = graphics.GraphicsDevice;

            // For each tile create the tile's vertex buffer.
            Terrain.TerrainVertex[] localVerts = new Terrain.TerrainVertex[NumVertices];

            // Init the vertex buffer.
            if (vbuf != null)
            {
                vbuf.Dispose();
                vbuf = null;
            }
            vbuf = new VertexBuffer(device, typeof(Terrain.TerrainVertex), NumVertices, ResourceUsage.WriteOnly, ResourceManagementMode.Automatic);

            // Copy the right vertices into the local buffer.
            int offsetX = indexX * (BuffSize - 1);
            int offsetY = indexY * (BuffSize - 1);

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int j = 0; j < BuffSize; j++)
            {
                for (int i = 0; i < BuffSize; i++)
                {
                    localVerts[j * BuffSize + i] = vertices[i + offsetX, j + offsetY];

                    min = Vector3.Min(min, localVerts[j * BuffSize + i].position);
                    max = Vector3.Max(max, localVerts[j * BuffSize + i].position);
                }
            }
            box = new AABB(min, max);

            // Copy to vertex buffer.
            vbuf.SetData<Terrain.TerrainVertex>(localVerts);

            BokuGame.Load(skirt);
            BokuGame.Load(water);
        }   // end of Tile LoadGraphicsContent()

        public void UnloadGraphicsContent()
        {
            BokuGame.Release(ref vbuf);

            BokuGame.Unload(skirt);
            BokuGame.Unload(water);
        }   // end of Tile UnloadGraphicsContent()

    }   // end of class Tile

}   // end of namespace Boku.SimWorld



