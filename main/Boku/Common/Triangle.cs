
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
    public class Triangle
    {
        private Vector3 v0;
        private Vector3 v1;
        private Vector3 v2;

        // c'tor
        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
        }   // end of Triangle c'tor

        /// <summary>
        /// Test a ray against the triangle.  Based on Tomas Moller's algorithm.
        /// </summary>
        /// <param name="ray">The ray to test against.</param>
        /// <param name="dist">If hit, this is the distance to the hit.</param>
        /// <returns>Returns true if a valid hit, false otherwise.</returns>
        public bool Intersect(Ray ray, ref float dist)
        {
            float u = 0.0f;
            float v = 0.0f;

            return Intersect(ray, ref dist, ref u, ref v);

        }   // end of Triangle Intersect()

        /// <summary>
        /// Test a ray against the triangle.  Based on Tomas Moller's algorithm.
        /// </summary>
        /// <param name="ray">The ray to test against.</param>
        /// <param name="u">U coord of hit.</param>
        /// <param name="v">V coord of hit.</param>
        /// <param name="dist">If hit, this is the distance to the hit.</param>
        /// <returns>Returns true if a valid hit, false otherwise.</returns>
        public bool Intersect(Ray ray, ref float dist, ref float u, ref float v)
        {
            // Find vectors for two edges sharing v0.
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
 
            // Begin calculating determinant - also used to calculate U parameter.
            Vector3 pvec = Vector3.Cross(ray.Direction, edge2);

            // If determinant is near zero, ray lies in plane of triangle.
            float det = Vector3.Dot(edge1, pvec);

            // Calculate distance from v0 to ray origin.
            Vector3 tvec = ray.Position - v0;
            float inv_det = 1.0f / det;
           
            Vector3 qvec = Vector3.Cross(tvec, edge1);
              
            if (det > float.Epsilon)
            {
                u = Vector3.Dot(tvec, pvec);
                if (u < 0.0f || u > det)
                {
                    return false;
                }

                // Calculate V parameter and test bounds.
                v = Vector3.Dot(ray.Direction, qvec);
                if (v < 0.0f || u + v > det)
                {
                    return false;
                }

            }
            else if(det < -float.Epsilon)
            {
                // Calculate U parameter and test bounds.
                u = Vector3.Dot(tvec, pvec);
                if (u > 0.0f || u < det)
                {
                    return false;
                }

                // Calculate V parameter and test bounds.
                v = Vector3.Dot(ray.Direction, qvec) ;
                if (v > 0.0f || u + v < det)
                {
                    return false;
                }
            }
            else 
            {
                // Ray is parallel to the plane of the triangle.
                return false;
            }

            dist = Vector3.Dot(edge2, qvec) * inv_det;
            u *= inv_det;
            v *= inv_det;

            return true;

        }   // end of Triangle Intersect()


}   // end of class Triangle

}   // end of namespace Boku.Common
