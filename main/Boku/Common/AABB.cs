
using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace Boku.Common
{
    /// <summary>
    /// Axis Aligned Bounding Box.
    /// </summary>
    public class AABB
    {
        private Vector3 min;
        private Vector3 max;

        #region Accessors
        public Vector3 Min
        {
            get { return min; }
            set { min = value; }
        }
        public Vector3 Max
        {
            get { return max; }
            set { max = value; }
        }
        public float MinZ
        {
            get { return min.Z; }
            set { min.Z = value; }
        }
        public float MaxZ
        {
            get { return max.Z; }
            set { max.Z = value; }
        }
        #endregion

        // c'tor
        public AABB(Vector3 min, Vector3 max)
        {
            this.min = Vector3.Min(min, max);
            this.max = Vector3.Max(min, max);
        }   // end of AABB c'tor

        public AABB(AABB src)
        {
            this.min = src.min;
            this.max = src.max;
        }

        public AABB()
        {
        }

        /// <summary>
        /// Create an empty box
        /// </summary>
        /// <returns></returns>
        public static AABB EmptyBox()
        {
            AABB box = new AABB();
            box.Min = new Vector3(Single.MaxValue);
            box.Max = new Vector3(Single.MinValue);
            return box;
        }

        /// <summary>
        /// Empty box signified by min being greater than max.
        /// </summary>
        /// <returns></returns>
        public bool Empty()
        {
            return (Min.X > Max.X)
                || (Min.Y > Max.Y)
                || (Min.Z > Max.Z);
        }

        /// <summary>
        /// Set the AABBs size.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void Set(Vector3 min, Vector3 max)
        {
            this.min = Vector3.Min(min, max);
            this.max = Vector3.Max(min, max);
        }   // end of Set

        /// <summary>
        /// Initialize from another bounding box.
        /// </summary>
        /// <param name="src"></param>
        public void Set(AABB src)
        {
            this.min = src.Min;
            this.max = src.Max;
        }

        /// <summary>
        /// Test an AABB against a point.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>True if AABB contains pos.  False otherwise.</returns>
        public bool Contains(Vector3 pos)
        {
            return (pos.X >= min.X) && (pos.X <= max.X)
                && (pos.Y >= min.Y) && (pos.Y <= max.Y)
                && (pos.Z >= min.Z) && (pos.Z <= max.Z);
        }

        /// <summary>
        /// Union self with a point in space.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public AABB Union(Vector3 pos)
        {
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
            return this;
        }

        /// <summary>
        /// Union self with other box.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public AABB Union(AABB other)
        {
            min = Vector3.Min(min, other.Min);
            max = Vector3.Max(max, other.Max);
            return this;
        }

        /// <summary>
        /// Union in a bounding sphere.
        /// </summary>
        /// <param name="sphere"></param>
        /// <returns></returns>
        public AABB Union(BoundingSphere sphere)
        {
            min = Vector3.Min(min, sphere.Center - new Vector3(sphere.Radius));
            max = Vector3.Max(max, sphere.Center + new Vector3(sphere.Radius));
            return this;
        }

        /// <summary>
        /// Create a bounding sphere around self.
        /// </summary>
        /// <returns></returns>
        public BoundingSphere MakeSphere()
        {
            return new BoundingSphere(
                (min + max) * 0.5f,
                Vector3.Distance(min, max) * 0.5f);
        }

        /// <summary>
        /// Test two AABBs against each other.
        /// </summary>
        /// <param name="box"></param>
        /// <returns>True if they intersect.  False otherwise.</returns>
        public bool Intersect(AABB box)
        {
            bool result = true;

            if ( (min.X > box.max.X || max.X < box.min.X) ||
                 (min.Y > box.max.Y || max.Y < box.min.Y) ||
                 (min.Z > box.max.Z || max.Z < box.min.Z))
            {
                result = false;
            }

            return result;
        }   // end of AABB Intersect()

        /// <summary>
        /// Tests a ray against an AABB.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="maxDist">Max distance that ray extends.</param>
        /// <param name="dist">Distance to the nearest hit along the ray.  
        /// Only valid if the function returns true.</param>
        /// <returns>Boolean whether or not the ray hist the box.</returns>
        public bool Intersect(Ray ray, float maxDist, ref float dist)
        {
            float tmin;
            float tmax;
            float dmin = float.MinValue;
            float dmax = maxDist;

            const float rayeps = 1e-6f;

            Vector3 invDir; // same as den in Vivid code...
            invDir.X = ray.Direction.X == 0.0f ? 0.0f : 1.0f / ray.Direction.X;
            invDir.Y = ray.Direction.Y == 0.0f ? 0.0f : 1.0f / ray.Direction.Y;
            invDir.Z = ray.Direction.Z == 0.0f ? 0.0f : 1.0f / ray.Direction.Z;

            // First check sides with normal along X axis.
            if (invDir.X != 0.0) 
            {
                // enters the slab here...
                tmin = (min.X - ray.Position.X) * invDir.X;
                // and exits here...
                tmax = (max.X - ray.Position.X) * invDir.X;

                // but we may have to swap...
                if (tmin < tmax) 
                {
                    // if exited closer than we thought, update
                    if (tmax < dmax) 
                    {
                        dmax = tmax;
                        if (dmax < rayeps)
                        {
                            return false;
                        }
                    }
                    // if entered farther than we thought, update
                    if (tmin > dmin)
                    {
                        dmin = tmin;
                    }
                } 
                else 
                {
                    // if exited closer than we thought, update
                    if (tmin < dmax) 
                    {
                        dmax = tmin;
                        if (dmax < rayeps) 
                        {
                            return false;
                        }
                    }
                    // if entered farther than we thought, update
                    if (tmax > dmin)
                    {
                        dmin = tmax;
                    }
                }

                if (dmin>dmax)
                {
                    return false;
                }
            }
            else
            {
                if (ray.Position.X < min.X || ray.Position.X > max.X)
                {
                    return false;
                }
            }


            // Check sides with normal along Y axis.
            if (invDir.Y != 0.0) 
            {
                // enters the slab here...
                tmin = (min.Y - ray.Position.Y) * invDir.Y;
                // and exits here...
                tmax = (max.Y - ray.Position.Y) * invDir.Y;

                // but we may have to swap...
                if (tmin < tmax) 
                {
                    // if exited closer than we thought, update
                    if (tmax < dmax) 
                    {
                        dmax = tmax;
                        if (dmax < rayeps) 
                        {
                            return false;
                        }
                    }
                    // if entered farther than we thought, update
                    if (tmin > dmin)
                    {
                        dmin = tmin;
                    }
                } else {
                    // if exited closer than we thought, update
                    if (tmin < dmax) 
                    {
                        dmax = tmin;
                        if (dmax < rayeps) 
                        {
                            return false;
                        }
                    }
                    // if entered farther than we thought, update
                    if (tmax > dmin)
                    {
                        dmin = tmax;
                    }
                }

                if (dmin>dmax)
                {
                    return false;
                }
            }
            else
            {
                if (ray.Position.Y < min.Y || ray.Position.Y > max.Y)
                {
                    return false;
                }
            }

            // Check sides with normal along Z axis.            
            if (invDir.Z != 0.0)
            {
                // enters the slab here...
                tmin = (min.Z - ray.Position.Z) * invDir.Z;
                // and exits here...
                tmax = (max.Z - ray.Position.Z) * invDir.Z;

                // but we may have to swap...
                if (tmin < tmax)
                {
                    // if exited closer than we thought, update
                    if (tmax < dmax)
                    {
                        dmax = tmax;
                        if (dmax < rayeps)
                        {
                            return false;
                        }
                    }
                    // if entered farther than we thought, update
                    if (tmin > dmin)
                    {
                        dmin = tmin;
                    }
                }
                else
                {
                    // if exited closer than we thought, update
                    if (tmin < dmax)
                    {
                        dmax = tmin;
                        if (dmax < rayeps)
                        {
                            return false;
                        }
                    }
                    // if entered farther than we thought, update
                    if (tmax > dmin)
                    {
                        dmin = tmax;
                    }
                }

                if (dmin > dmax)
                {
                    return false;
                }
            }
            else
            {
                if (ray.Position.Z < min.Z || ray.Position.Z > max.Z)
                {
                    return false;
                }
            }

            dist = dmin;
            return true;

        }   // end of AABB Intersect()

    }   // end of class AABB

}   // end of namespace Boku.Common


