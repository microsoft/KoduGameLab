// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Boku.Common
{
    /// <summary>
    /// Collection of static intersection methods for doing collision testing.
    /// </summary>
    static public class SweptPrims
    {
        /// <summary>
        /// Publicly accessible ray/sphere test. Determines how far along the ray segment the ray hits
        /// the sphere, if at all.
        /// Note, a ray that starts inside it the sphere is assumed to immediately hit.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="rayStart">start of ray</param>
        /// <param name="rayEnd">end of ray</param>
        /// <param name="t"></param>
        /// <returns>True is ray hits.</returns>
        public static bool RaySphere(Vector3 center, float radius, Vector3 rayStart, Vector3 rayEnd, ref float t)
        {
            Vector3 dir = rayEnd - rayStart;

            // If rayStart is already inside of sphere then we have an immediate hit.
            if (Vector3.DistanceSquared(center, rayStart) <= radius * radius)
            {
                // Inside the sphere, immediate hit.
                t = 0;
                return true;
            }

            // If ray start and end are the same, no hit.
            double a = dir.LengthSquared();
            if (a <= 0.000001)
            {
                return false;
            }

            double b = 2.0 * Vector3.Dot(dir, rayStart - center);
            double c = Vector3.DistanceSquared(rayStart, center) - radius * radius;

            Debug.Assert(a > 0.0, "degenerate ray should have been filtered out already");

            double det = b * b - 4.0 * a * c;
            if (det < 0)
            {
                return false;
            }
            t = (float)((-b - Math.Sqrt(det)) / (2.0 * a));

            bool hit = (t >= 0.0f) && (t <= 1.0f);

            return hit;

        }   // end of RaySphere()

        public static bool RaySweptSphere(Vector3 centerStart, Vector3 centerEnd, float radius, Vector3 rayStart, Vector3 rayEnd, ref float t)
        {
            bool hit = false;

            // Need to shift coordinate systems to put us in one where the sphere is not moving.
            // So, adjust end of ray segment.
            rayEnd -= centerEnd - centerStart;

            hit = RaySphere(centerStart, radius, rayStart, rayEnd, ref t);

            if (hit)
            {
            }

            return hit;
        }   // end of RaySweptSphere()

        public static bool SweptSphereSweptSphere(Vector3 centerStart0, Vector3 centerEnd0, float radius0, Vector3 centerStart1, Vector3 centerEnd1, float radius1, ref float t)
        {
            bool hit = false;

            // Add ss1's radius to ss0 so we can treat ss1 as a ray and test.
            hit = RaySweptSphere(centerStart0, centerEnd0, radius0 + radius1, centerStart1, centerEnd1, ref t);

            return hit;
        }

        /// <summary>
        /// Test a ray segment against an axis-aligned ellipsoid
        /// </summary>
        /// <param name="center">Center of ellipsoid.</param>
        /// <param name="radii">Axis aligned radii of ellipsoid.</param>
        /// <param name="rayStart">Start of ray segment.</param>
        /// <param name="rayEnd">End of ray segment.</param>
        /// <param name="hitPosition">Position of hit, if any.</param>
        /// <param name="hitNormal">Normal at hit point, if any.</param>
        /// <param name="t">Destance along ray segment if hit.  Will be in 0..1 range.</param>
        /// <returns></returns>
        public static bool RayEllipsoid(Vector3 center, Vector3 radii, Vector3 rayStart, Vector3 rayEnd, ref Vector3 hitPosition, ref Vector3 hitNormal, ref float t)
        {
            bool hit = false;

            // Adjust ray inputs so that ellipsoid is at origin.
            rayStart -= center;
            rayEnd -= center;

            // Adjust ray inputs to make ellipse into unit sphere.
            rayStart /= radii;
            rayEnd /= radii;

            // We now test the ray against the unit sphere centered at the origin.
            hit = RaySphere(Vector3.Zero, 1.0f, rayStart, rayEnd, ref t);
            if (hit)
            {
                // Calc position and normal.
                hitPosition = rayStart + t * (rayEnd - rayStart);
                hitNormal = hitPosition;    // Works since this is a unit sphere.

                // Undo scaling and offset.
                hitPosition = hitPosition * radii + center;
                hitNormal = hitNormal / radii;
                hitNormal.Normalize();
            }

            return hit;
        }   // end of RayEllipsoid()

        /// <summary>
        /// Test a ray segment against an axis-aligned, swept ellipsoid.
        /// </summary>
        /// <param name="centerStart">Center of ellipsoid at start of path.</param>
        /// <param name="centerEnd">Center of ellipsoid at end of path.</param>
        /// <param name="radii">Axis aligned radii of ellipsoid.</param>
        /// <param name="rayStart">Start of ray segment.</param>
        /// <param name="rayEnd">End of ray segment.</param>
        /// <param name="hitPosition">Position of hit, if any.</param>
        /// <param name="hitNormal">Normal at hit point, if any.</param>
        /// <param name="t">Destance along ray segment if hit.  Will be in 0..1 range.</param>
        /// <returns></returns>
        public static bool RaySweptEllipsoid(Vector3 centerStart, Vector3 centerEnd, Vector3 radii, Vector3 rayStart, Vector3 rayEnd, ref Vector3 hitPosition, ref Vector3 hitNormal, ref float t)
        {
            bool hit = false;

            // Adjust ray motion so that ellipse is fixed.
            Vector3 dir = centerEnd - centerStart;
            rayEnd -= dir;

            hit = RayEllipsoid(centerStart, radii, rayStart, rayEnd, ref hitPosition, ref hitNormal, ref t);
            if (hit)
            {
                // Put hit position back in place.
                hitPosition += t * dir;
            }


            return hit;
        }   // end of RaySweptEllipsoid()

        public static bool SweptEllipsoidSweptEllipsoid(Vector3 centerStart0, Vector3 centerEnd0, Vector3 radii0, Vector3 centerStart1, Vector3 centerEnd1, Vector3 radii1, ref Vector3 hitPosition, ref Vector3 hitNormal, ref float t)
        {
            bool hit = false;

            // Inflate ellipsoid0 by Ellipsoid1's radii.
            radii0 += radii1;

            hit = RaySweptEllipsoid(centerStart0, centerEnd0, radii0, centerStart1, centerEnd1, ref hitPosition, ref hitNormal, ref t);
            if (hit)
            {
                // TODO (****)  This gives us an approximation of the hit, not the exact hit.  Good enough for now...
                // Also, normal is also off a bit.

                // Remove inflation amount from hitPoint.
                hitPosition -= hitNormal * radii1;
            }

            return hit;
        }   // end of SweptEllipsoidSweptEllipsoid()

    }   // end of class SweptPrims

}   // end of namespace Boku.Common
