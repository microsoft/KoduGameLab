
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
    /// Based on the camera that owns it, this class creates the six planes
    /// which define the current view frustum.  This class also contains 
    /// functions which test the frustum against bounding objects.
    /// </summary>
    public class Frustum
    {
        public enum CullResult
        {
            TotallyInside,
            PartiallyInside,
            TotallyOutside
        };

        private Vector4[] planes = null;
        
        // c'tor
        public Frustum()
        {
            planes = new Vector4[6];

        }   // end of Frustum c'tor

        public Frustum Clone()
        {
            Frustum clone = new Frustum();

            clone.planes = new Vector4[6];
            
            for (int i = 0; i < 6; ++i)
            {
                clone.planes[i] = this.planes[i];
            }
            return clone;
        }

        public void Update(ref Matrix viewProjectionMatrix)
        {
            ExtractPlanes(ref viewProjectionMatrix, true);
        }   // end of Frustum Update()

        public CullResult CullTest(BoundingSphere sphere)
        {
            return CullTest(sphere.Center, sphere.Radius);
        }   // end of Frustum CullTest()

        /// <summary>
        /// Test a sphere against the frustum.
        /// </summary>
        public CullResult CullTest(Vector3 center, float radius)
        {
            CullResult result = CullResult.TotallyInside;

            float dist;
            for (int i = 0; i < 6; ++i)
            {
                // Calc the distance to the plane.
                dist = planes[i].X * center.X + planes[i].Y * center.Y + planes[i].Z * center.Z + planes[i].W;

                if (dist < -radius)         // If sphere is outside and we can exit.
                {
                    result = CullResult.TotallyOutside;
                    break;
                }
                else if (dist < radius)     // If sphere intersects plane, change result to
                {                           // partial but keep looking for full exclusion.
                    result = CullResult.PartiallyInside;
                }
            }

            return result;
        }   // end of Frustum CullTest()

        /// <summary>
        /// Test an axis aligned bounding box against the frustum.
        /// </summary>
        public CullResult CullTest(AABB box)
        {
            return CullTest(box.Min, box.Max);
        }   // end of Frustum CullTest()

        public CullResult CullTest(BoundingBox box)
        {
            return CullTest(box.Min, box.Max);
        }   // end of Frustum CullTest()

        /// <summary>
        /// Test an axis aligned bounding box against the frustum.
        /// </summary>
        public CullResult CullTest(Vector3 min, Vector3 max)
        {
            CullResult result = CullResult.TotallyInside;

            for (int i = 0; i < 6; i++)
            {
                // Calc the two vertices we need to test against.
                Vector3 pVertex;
                Vector3 nVertex;
                if (planes[i].X < 0)
                {
                    pVertex.X = min.X;
                    nVertex.X = max.X;
                }
                else
                {
                    pVertex.X = max.X;
                    nVertex.X = min.X;
                }
                if (planes[i].Y < 0)
                {
                    pVertex.Y = min.Y;
                    nVertex.Y = max.Y;
                }
                else
                {
                    pVertex.Y = max.Y;
                    nVertex.Y = min.Y;
                }
                if (planes[i].Z < 0)
                {
                    pVertex.Z = min.Z;
                    nVertex.Z = max.Z;
                }
                else
                {
                    pVertex.Z = max.Z;
                    nVertex.Z = min.Z;
                }

                // Check for totally outside case.
                float dist = pVertex.X * planes[i].X + pVertex.Y * planes[i].Y + pVertex.Z * planes[i].Z + planes[i].W;
                if (dist < 0)
                {
                    result = CullResult.TotallyOutside;
                    break;
                }

                // Check for box intersecting plane case.
                dist = nVertex.X * planes[i].X + nVertex.Y * planes[i].Y + nVertex.Z * planes[i].Z + planes[i].W;
                if (dist < 0 )
                {
                    result = CullResult.PartiallyInside;
                }

            }   // end of loop over frustum planes.

            return result;
        }   // end of Frustum CullTest()

        private void NormalizePlane(ref Vector4 plane)
        {
            float invMag = 1.0f / (float)Math.Sqrt(plane.X * plane.X + plane.Y * plane.Y + plane.Z * plane.Z);

            plane.X = plane.X * invMag;
            plane.Y = plane.Y * invMag;
            plane.Z = plane.Z * invMag;
            plane.W = plane.W * invMag;
        }   // end of Frustum NormalizePlane()

        private void ExtractPlanes(ref Matrix viewProjectionMatrix, bool normalize)
        {
            // Left clipping plane
            planes[0].X = viewProjectionMatrix.M14 + viewProjectionMatrix.M11;
            planes[0].Y = viewProjectionMatrix.M24 + viewProjectionMatrix.M21;
            planes[0].Z = viewProjectionMatrix.M34 + viewProjectionMatrix.M31;
            planes[0].W = viewProjectionMatrix.M44 + viewProjectionMatrix.M41;

            // Right clipping plane
            planes[1].X = viewProjectionMatrix.M14 - viewProjectionMatrix.M11;
            planes[1].Y = viewProjectionMatrix.M24 - viewProjectionMatrix.M21;
            planes[1].Z = viewProjectionMatrix.M34 - viewProjectionMatrix.M31;
            planes[1].W = viewProjectionMatrix.M44 - viewProjectionMatrix.M41;

            // Top clipping plane
            planes[2].X = viewProjectionMatrix.M14 - viewProjectionMatrix.M12;
            planes[2].Y = viewProjectionMatrix.M24 - viewProjectionMatrix.M22;
            planes[2].Z = viewProjectionMatrix.M34 - viewProjectionMatrix.M32;
            planes[2].W = viewProjectionMatrix.M44 - viewProjectionMatrix.M42;

            // Bottom clipping plane
            planes[3].X = viewProjectionMatrix.M14 + viewProjectionMatrix.M12;
            planes[3].Y = viewProjectionMatrix.M24 + viewProjectionMatrix.M22;
            planes[3].Z = viewProjectionMatrix.M34 + viewProjectionMatrix.M32;
            planes[3].W = viewProjectionMatrix.M44 + viewProjectionMatrix.M42;

            // Near clipping plane
            planes[4].X = viewProjectionMatrix.M13;
            planes[4].Y = viewProjectionMatrix.M23;
            planes[4].Z = viewProjectionMatrix.M33;
            planes[4].W = viewProjectionMatrix.M43;

            // Far clipping plane
            planes[5].X = viewProjectionMatrix.M14 - viewProjectionMatrix.M13;
            planes[5].Y = viewProjectionMatrix.M24 - viewProjectionMatrix.M23;
            planes[5].Z = viewProjectionMatrix.M34 - viewProjectionMatrix.M33;
            planes[5].W = viewProjectionMatrix.M44 - viewProjectionMatrix.M43;

            // Normalize the plane equations, if requested.
            if (normalize == true)
            {
                NormalizePlane(ref planes[0]);
                NormalizePlane(ref planes[1]);
                NormalizePlane(ref planes[2]);
                NormalizePlane(ref planes[3]);
                NormalizePlane(ref planes[4]);
                NormalizePlane(ref planes[5]);
            }
        }   // end of Frustum ExtractPlanes()


    }   // end of class Frustum

}   // end of namespace Boku.Common
