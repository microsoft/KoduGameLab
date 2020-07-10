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

using KoiX;

using Boku.Common;

namespace Boku.SimWorld.Terra
{
    /// <summary>
    /// Class to load & manipulate height maps.  Height maps are a 2d array of 
    /// 16 bit unsigned ints.  For this we'll assume that middle grey, ie 32767 
    /// is the water line and the public interfaces will return this as altitude 0.
    /// </summary>
    public class HeightMap
    {
        private string filename = null;
        private Point size;         // Size of 2d array.
        private Vector3 scale;      // Scale in world units.  The height map is initially assumed to 
                                    // extend in the range [0, 1] in the X and Y directions and [0, 1] along
                                    // the Z axis.  This scale factor fits it into world coordinates.
        private UInt16[,] grid;
        private AABB box;

        #region Accessors
        /// <summary>
        /// Pixel dimensions of this heightmap.
        /// </summary>
        public Point Size
        {
            get { return size; }
        }
        /// <summary>
        /// Horizontal size and vertical scale (x 1<<16) of box.
        /// </summary>
        public Vector3 Scale
        {
            get { return scale; }
        }
        /// <summary>
        /// 3D bounnding box for this heightmap, in world space.
        /// </summary>
        public AABB BoundingBox
        {
            get { return box; }
            set { box = value; }
        }
        #endregion

        /// <summary>
        /// Shift the box to a position in world space. New Min will be at input position.
        /// </summary>
        /// <param name="swCorner"></param>
        /// <returns></returns>
        public AABB PositionBox(Vector2 swCorner)
        {
            box.Min = new Vector3(
                swCorner.X,
                swCorner.Y,
                box.Min.Z);
            box.Max = new Vector3(
                swCorner.X + scale.X,
                swCorner.Y + scale.Y,
                box.Max.Z);

            return box;
        }

        public AABB UpdateBox(float minZ, float maxZ)
        {
            box.MinZ = minZ;
            box.MaxZ = maxZ;

            return box;
        }

        public void Init(Vector2 pos, Point size, Vector3 scale)
        {
            this.size = size;
            this.scale = scale;
            this.box = new AABB(
                new Vector3(pos.X, pos.Y, 0.0f),
                new Vector3(pos.X + scale.X, pos.Y + scale.Y, 0.0f));

            // Allocate the grid.
            grid = new UInt16[size.X, size.Y];
        }
        public HeightMap(Vector2 pos, Point size, Vector3 scale)
        {
            Init(pos, size, scale);
        }
        public HeightMap(string filename, Point size, Vector3 scale)
        {
            this.filename = filename;
            Init(Vector2.Zero, size, scale);
            // Load the heightmap.
            Stream fs = Storage4.OpenRead(filename, StorageSource.All);
            BinaryReader br = new BinaryReader(fs);

            Load(br);

#if NETFX_CORE
            br.Dispose();
#else
            br.Close();
#endif
            Storage4.Close(fs);

        }   // end of HeightMap c'tor

        /// <summary>
        /// Writes the current height map to a file.
        /// </summary>
        /// <param name="filename"></param>
        public void Save(string filename)
        {
            Stream fs = Storage4.OpenWrite(filename);
            BinaryWriter bw = new BinaryWriter(fs);

            Save(bw);

#if NETFX_CORE
            bw.Flush();
            bw.Dispose();
#else
            bw.Close();
#endif
            Storage4.Close(fs);
        }   // end of HeightMap Save()

        public void Save(BinaryWriter bw)
        {
            for (int j = 0; j < size.Y; j++)
            {
                for (int i = 0; i < size.X; i++)
                {
                    bw.Write(grid[i, j]);
                }
            }
        }

        public void Load(BinaryReader br)
        {
            for (int j = 0; j < size.Y; j++)
            {
                for (int i = 0; i < size.X; i++)
                {
                    grid[i, j] = br.ReadUInt16();
                }
            }
        }

        public void RescaleZ(float newScale)
        {
            if (newScale != scale.Z)
            {
                double rescale = scale.Z / newScale;
                Debug.Assert(rescale <= 1.0, "Warning, truncation may occur");
                for (int i = 0; i < size.X; ++i)
                {
                    for (int j = 0; j < size.Y; ++j)
                    {
                        double newHeight = grid[i, j] * rescale + 0.5;
                        grid[i, j] = (ushort)newHeight;
                    }
                }
                scale.Z = newScale;
            }
        }

        /// <summary>
        /// Get the scaled height at a particular vertex.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public float GetHeight(int i, int j)
        {
            // Clamp i and j to valid range.
            i = i < 0 ? 0 : i >= size.X ? size.X - 1 : i;
            j = j < 0 ? 0 : j >= size.Y ? size.Y - 1 : j;

            return (float)grid[i, j]* scale.Z / (float)0xffff;
        }   // end of HeightMap GetHeight()

        /// <summary>
        /// Get the scaled height at a particular vertex.
        /// Note this version does no range checking on i and j.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public float GetHeightUnsafe(int i, int j)
        {
            return (float)grid[i, j] * scale.Z / (float)0xffff;
        }   // end of HeightMap GetHeightUnsafe()

        public float GetHeight(Vector2 position)
        {
            // Remap to "texel" coordinates.
            position.X *= (size.X - 1) / scale.X;
            position.Y *= (size.Y - 1) / scale.Y;

            // Get the integral and fraction parts of the position.
            int i = (int)position.X;
            int j = (int)position.Y;
            float dx = position.X - i;
            float dy = position.Y - j;

            // Calc the bilinearly weighted sample.
            float result = MyMath.Lerp(MyMath.Lerp(GetHeight(i, j), GetHeight(i + 1, j), dx), MyMath.Lerp(GetHeight(i, j + 1), GetHeight(i + 1, j + 1), dx), dy);

            return result;
        }   // end of HeightMap GetHeight()


        public float GetHeight(Vector3 position)
        {
            Vector2 pos = new Vector2(position.X, position.Y);

            return GetHeight(pos);
        }   // end of HeightMap GetHeight()

        public Vector3 GetNormal(int i, int j)
        {
            float dHdX;

            if (i < Size.X - 1)
                dHdX = GetHeight(i + 1, j);
            else
                dHdX = GetHeight(i, j);
            if (i > 0)
                dHdX -= GetHeight(i - 1, j);
            else
                dHdX -= GetHeight(i, j);
            float stepX = 2.0f * Scale.X / (Size.X - 1);
            dHdX /= stepX;

            float dHdY;
            if (j < Size.Y - 1)
                dHdY = GetHeight(i, j + 1);
            else
                dHdY = GetHeight(i, j);
            if (j > 0)
                dHdY -= GetHeight(i, j - 1);
            else
                dHdY -= GetHeight(i, j);
            float stepY = 2.0f * Scale.Y / (Size.Y - 1);
            dHdY /= stepY;

            Vector3 ret = new Vector3(-dHdX, -dHdY, 1.0f);
            ret.Normalize();
            return ret;
        }

        /// <summary>
        /// Returns the surface normal at the position on the surface.
        /// </summary>
        public Vector3 GetNormal(Vector2 position)
        {
            float height = GetHeight(position);
            float heightDX = GetHeight(position + new Vector2(0.1f, 0.0f));
            float heightDY = GetHeight(position + new Vector2(0.0f, 0.1f));

            Vector3 dx = new Vector3(0.1f, 0.0f, heightDX - height);
            Vector3 dy = new Vector3(0.0f, 0.1f, heightDY - height);

            Vector3 result = Vector3.Cross(dx, dy);
            result.Normalize();

            return result;
        }   // end of HeightMap GetNormal()

        /// <summary>
        /// Returns the surface normal at the position on the surface.
        /// </summary>
        public Vector3 GetNormal(Vector3 position)
        {
            return GetNormal(new Vector2(position.X, position.Y));
        }   // end of HeightMap GetNormal()


        /// <summary>
        /// Set the height of a particular vertex in the heightmap.  If
        /// the i,j position is out of range the input is ignored.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="height"></param>
        public void SetHeight(int i, int j, float height)
        {
            if (i < 0 || j < 0 || i >= size.X || j > size.Y)
            {
                // Out of range indices, do nothing.
                return;
            }

            // Map height into 0,1 range and clamp.
            height = MathHelper.Clamp(height / scale.Z, 0.0f, 1.0f);

            grid[i, j] = (UInt16)((float)0xffff * height);
        }   // end of HeightMap SetHeight()


        /// <summary>
        /// Intersects a ray with the height map.
        /// </summary>
        /// <param name="ray">Input ray to test against.</param>
        /// <param name="maxDistance">The max distance we care about the ray travelling.  Note that the code may still return a hit beyond this distance so you still have to check the return distance param if hit is true.</param>
        /// <param name="distance">If hit, returns the distance to the nearest hit.  If no hit, this value may still be changed.</param>
        /// <returns>Returns true is hit, false otherwise.</returns>
        public bool Intersect(Ray ray, float maxDistance, ref Vector3 hitPoint)
        {
            float boxDistance = 0.0f;   // This distance from the ray origin to
                                        // the bounding box of the height map.

            // First, test against the bounding box.
            bool hit = box.Intersect(ray, float.MaxValue, ref boxDistance);

            if (hit && boxDistance < maxDistance)
            {
                // We hit the box surrounding the height map,  
                // now we have to test agianst the data itself.

                // Handle the degenerate case where the ray is straight up or down.
                if (Math.Abs(ray.Direction.X) < float.Epsilon && Math.Abs(ray.Direction.Y) < float.Epsilon)
                {
                    Vector2 localRayPos = new Vector2(ray.Position.X - box.Min.X, ray.Position.Y - box.Min.Y);
                    float height = GetHeight(localRayPos);
                    float distance = (height - ray.Position.Z) * ray.Direction.Z;
                    hitPoint = new Vector3(
                        ray.Position.X + box.Min.X, 
                        ray.Position.Y + box.Min.Y, 
                        height);
                    return distance > 0.0f;
                }

                // Project the ray to the edge of the box if we're not already in it.
                if (boxDistance > 0.0f)
                {
                    ray.Position += ray.Direction * boxDistance;
                }
                else
                {
                    boxDistance = 0.0f;
                }

                // Convert the ray to a coordinate system such that
                // the height map samples are exactly 1 unit apart.  This 
                // gives us a fixed interger grid to iterate across. 
                Vector3 rayEnd = ray.Position + ray.Direction * (maxDistance - boxDistance);
                ray.Position = WorldToMap(ray.Position);
                ray.Direction = WorldToMapDir(ray.Direction);
                ray.Direction.Normalize();
                rayEnd = WorldToMap(rayEnd);
                float mapMaxDist = Vector3.Distance(rayEnd, ray.Position);

                // Calc integer coords of starting point.
                int i = (int)Math.Floor(ray.Position.X);
                int j = (int)Math.Floor(ray.Position.Y);

                float errorX = ray.Position.X - i;
                float errorY = ray.Position.Y - j;
                float z = ray.Position.Z;

                float frac = 0.0f;  // Fractional step amount.

                bool dominantX = Math.Abs(ray.Direction.X) > Math.Abs(ray.Direction.Y);

                // Keep track of how far the ray has travelled so we can 
                // bail early if we haven't hit anything by the time we've
                // gone maxDistance units.
                float distancePerStep = 0.0f;   // This is the distance that the ray travels for each step in the dominant direction.
                                                // Set below once the dominant direction is determined and then used to scale the ray dir.

                if (dominantX)
                {
                    // Since X is dominant, we need to scale the direction vector so
                    // that dx == 1.0f.
                    distancePerStep = 1.0f / (float)Math.Abs(ray.Direction.X);
                    ray.Direction *= distancePerStep;
                    mapMaxDist /= distancePerStep;

                    if (ray.Direction.X >= 0.0f)
                    {
                        int end = Math.Min(size.X, (int)rayEnd.X + 1);
                        while (i < end)
                        {
                            frac = 1.0f - errorX;

                            // Test triangles in current cell.
                            hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                            if (hit)
                            {
                                hitPoint = MapToWorld(hitPoint);
                                return true;
                            }

                            // We need to determine whether the next step is in the 
                            // X direction or Y.  See if Y will over/under flow next step.
                            // If so, step in Y direction first.
                            float tmpY = errorY + ray.Direction.Y * frac;
                            if (tmpY < 0.0f)
                            {
                                frac = -errorY / ray.Direction.Y;
                                --j;
                                errorY = 1.0f;
                                errorX += ray.Direction.X * frac;
                                z += ray.Direction.Z * frac;

                                // Test triangles in current cell.
                                hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                                if (hit)
                                {
                                    hitPoint = MapToWorld(hitPoint);
                                    return true;
                                }

                            }
                            else if (tmpY >= 1.0f)
                            {
                                frac = (1.0f - errorY) / ray.Direction.Y;
                                ++j;
                                errorY = 0.0f;
                                errorX += ray.Direction.X * frac;
                                z += ray.Direction.Z * frac;

                                // Test triangles in current cell.
                                hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                                if (hit)
                                {
                                    hitPoint = MapToWorld(hitPoint);
                                    return true;
                                }

                            }

                            frac = 1.0f - errorX;
                            ++i;
                            errorX = 0.0f;
                            errorY += ray.Direction.Y * frac;
                            z += ray.Direction.Z * frac;

                        }
                    }
                    else // if dir.X < 0
                    {
                        int end = Math.Max(0, (int)rayEnd.X);
                        while (i >= end)
                        {
                            frac = errorX;

                            // Test triangles in current cell.
                            hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                            if (hit)
                            {
                                hitPoint = MapToWorld(hitPoint);
                                return true;
                            }

                            // We need to determine whether the next step is in the 
                            // X direction or Y.  See if Y will over/under flow next step.
                            // If so, step in Y direction first.
                            float tmpY = errorY + ray.Direction.Y * frac;
                            if (tmpY < 0.0f)
                            {
                                frac = -errorY / ray.Direction.Y;
                                --j;
                                errorY = 1.0f;
                                errorX += ray.Direction.X * frac;
                                z += ray.Direction.Z * frac;

                                // Test triangles in current cell.
                                hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                                if (hit)
                                {
                                    hitPoint = MapToWorld(hitPoint);
                                    return true;
                                }

                            }
                            else if (tmpY >= 1.0f)
                            {
                                frac = (1.0f - errorY) / ray.Direction.Y;
                                ++j;
                                errorY = 0.0f;
                                errorX += ray.Direction.X * frac;
                                z += ray.Direction.Z * frac;

                                // Test triangles in current cell.
                                hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                                if (hit)
                                {
                                    hitPoint = MapToWorld(hitPoint);
                                    return true;
                                }

                            }

                            frac = errorX == 0.0f ? 1.0f : errorX;
                            --i;
                            errorX = 1.0f;
                            errorY += ray.Direction.Y * frac;
                            z += ray.Direction.Z * frac;

                        }
                    }   // end if dir.X < 0

                }   // end if X dominant
                else
                {
                    // Since Y is dominant, we need to scale the direction vector so
                    // that dy == 1.0f.
                    distancePerStep = 1.0f / (float)Math.Abs(ray.Direction.Y);
                    ray.Direction *= distancePerStep;
                    mapMaxDist /= distancePerStep;

                    if (ray.Direction.Y >= 0.0f)
                    {
                        int end = Math.Min(size.Y, (int)rayEnd.Y + 1);
                        while (j < end)
                        {
                            frac = 1.0f - errorY;

                            // Test triangles in current cell.
                            hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                            if (hit)
                            {
                                hitPoint = MapToWorld(hitPoint);
                                return true;
                            }

                            // We need to determine whether the next step is in the 
                            // X direction or Y.  See if X will over/under flow next step.
                            // If so, step in X direction first.
                            float tmpX = errorX + ray.Direction.X * frac;
                            if (tmpX < 0.0f)
                            {
                                frac = -errorX / ray.Direction.X;
                                --i;
                                errorX = 1.0f;
                                errorY += ray.Direction.Y * frac;
                                z += ray.Direction.Z * frac;

                                // Test triangles in current cell.
                                hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                                if (hit)
                                {
                                    hitPoint = MapToWorld(hitPoint);
                                    return true;
                                }

                            }
                            else if (tmpX >= 1.0f)
                            {
                                frac = (1.0f - errorX) / ray.Direction.X;
                                ++i;
                                errorX = 0.0f;
                                errorY += ray.Direction.Y * frac;
                                z += ray.Direction.Z * frac;

                                // Test triangles in current cell.
                                hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                                if (hit)
                                {
                                    hitPoint = MapToWorld(hitPoint);
                                    return true;
                                }

                            }

                            frac = 1.0f - errorY;
                            ++j;
                            errorY = 0.0f;
                            errorX += ray.Direction.X * frac;
                            z += ray.Direction.Z * frac;

                        }
                    }
                    else // if dir.Y < 0
                    {
                        int end = Math.Max(0, (int)rayEnd.Y);
                        while (j >= 0)
                        {
                            frac = errorY;

                            // Test triangles in current cell.
                            hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                            if (hit)
                            {
                                hitPoint = MapToWorld(hitPoint);
                                return true;
                            }

                            // We need to determine whether the next step is in the 
                            // X direction or Y.  See if X will over/under flow next step.
                            // If so, step in X direction first.
                            float tmpX = errorX + ray.Direction.X * frac;
                            if (tmpX < 0.0f)
                            {
                                frac = -errorX / ray.Direction.X;
                                --i;
                                errorX = 1.0f;
                                errorY += ray.Direction.Y * frac;
                                z += ray.Direction.Z * frac;

                                // Test triangles in current cell.
                                hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                                if (hit)
                                {
                                    hitPoint = MapToWorld(hitPoint);
                                    return true;
                                }

                            }
                            else if (tmpX >= 1.0f)
                            {
                                frac = (1.0f - errorX) / ray.Direction.X;
                                ++i;
                                errorX = 0.0f;
                                errorY += ray.Direction.Y * frac;
                                z += ray.Direction.Z * frac;

                                // Test triangles in current cell.
                                hit = HitTestCell(ray, i, j, mapMaxDist, ref hitPoint);
                                if (hit)
                                {
                                    hitPoint = MapToWorld(hitPoint);
                                    return true;
                                }

                            }

                            frac = errorY == 0.0f ? 1.0f : errorY;
                            --j;
                            errorY = 1.0f;
                            errorX += ray.Direction.X * frac;
                            z += ray.Direction.Z * frac;
                        }
                    }   // end if dir.X < 0

                }   // end of Y dominant

                return false;
            }   // end of box is hit

            return false;
        }   // end of HeightMap Intersect()


        private Vector3 MapToWorld(Vector3 pos)
        {
            return new Vector3(
                pos.X * scale.X / size.X + box.Min.X,
                pos.Y * scale.Y / size.Y + box.Min.Y,
                pos.Z);
        }
        private Vector3 WorldToMap(Vector3 pos)
        {
            return new Vector3(
                (pos.X - box.Min.X) * size.X / scale.X,
                (pos.Y - box.Min.Y) * size.Y / scale.Y,
                pos.Z);
        }
        private Vector3 WorldToMapDir(Vector3 pos)
        {
            return new Vector3(
                pos.X * size.X / scale.X,
                pos.Y * size.Y / scale.Y,
                pos.Z);
        }
        // Hit tests the current cell the ray is in.
        #region HitTestCell local objects
        AABB cell = new AABB();
        #endregion
        private bool HitTestCell(Ray ray, int i, int j, float maxDist, ref Vector3 hitPoint)
        {
            if ((i >= 0) && (i < size.X) && (j >= 0) && (j < size.Y))
            {
                float h = GetHeight(i, j);
                Vector3 pmin = new Vector3(i, j, 0.0f);
                Vector3 pmax = new Vector3(i + 1, j + 1, h);
                cell.Set(pmin, pmax);
                float dist = 0.0f;
                if (cell.Intersect(ray, maxDist, ref dist))
                {
                    hitPoint = ray.Position + ray.Direction * dist;
                    return true;
                }
            }
            return false;

        }   // end of HeightMap HitTestCell()

    }   // end of class HeightMap

}   // end of namespace Boku.SimWorld
