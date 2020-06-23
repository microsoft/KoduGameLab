/*
 * Util.cs
 * Copyright (c) 2006 David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */




using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Xclna.Xna.Animation
{
    /// <summary>
    /// Info on how a model is skinned.
    /// </summary>
    public enum SkinningType
    {
        /// <summary>
        /// No skinning.
        /// </summary>
        None,
        /// <summary>
        /// A max of four influences per vertex.
        /// </summary>
        FourBonesPerVertex,
        /// <summary>
        /// A max of eight influences per vertex.
        /// </summary>
        EightBonesPerVertex,
        /// <summary>
        /// A max of twelve influences per vertex.
        /// </summary>
        TwelveBonesPerVertex
    }
    /// <summary>
    /// Provides various animation utilities.
    /// </summary>
    public sealed class Util
    {
        /// <summary>
        /// Ticks per frame at 60 frames per second.
        /// </summary>
        public const long TICKS_PER_60FPS = TimeSpan.TicksPerSecond / 60;

        /// <summary>
        /// Gets info on what skinning info a vertex element array contains.
        /// </summary>
        /// <param name="elements">The vertex elements.</param>
        /// <returns>Info on what type of skinning the elements contain.</returns>
        public static SkinningType GetSkinningType(VertexElement[] elements)
        {
            int numIndexChannels = 0;
            int numWeightChannels = 0;
            foreach (VertexElement e in elements)
            {
                if (e.VertexElementUsage == VertexElementUsage.BlendIndices)
                    numIndexChannels++;
                else if (e.VertexElementUsage == VertexElementUsage.BlendWeight)
                    numWeightChannels++;
            }
            if (numIndexChannels == 3 || numWeightChannels == 3)
                return SkinningType.TwelveBonesPerVertex;
            else if (numIndexChannels == 2 || numWeightChannels == 2)
                return SkinningType.EightBonesPerVertex;
            else if (numIndexChannels == 1 || numWeightChannels == 1)
                return SkinningType.FourBonesPerVertex;
            return SkinningType.None;

        }





        /// <summary>
        /// Reflects a matrix across the Z axis by multiplying both the Z
        /// column and the Z row by -1 such that the Z,Z element stays intact.
        /// </summary>
        /// <param name="m">The matrix to be reflected across the Z axis</param>
        public static  void ReflectMatrix(ref Matrix m)
        {
            m.M13 *= -1;
            m.M23 *= -1;
            m.M33 *= -1;
            m.M43 *= -1;
            m.M31 *= -1;
            m.M32 *= -1;
            m.M33 *= -1;
            m.M34 *= -1;
        }



        private static T Max<T>(params T[] items) where T : IComparable
        {
            IComparable max = null;
            foreach (IComparable c in items)
            {
                if (max == null)
                    max = c;
                else
                {
                    if (c.CompareTo(max) > 0)
                        max = c;
                }
            }
            return (T)max;
        }

        
        /// <summary>
        /// Converts from an array of bytes to any vertex type.
        /// </summary>
        /// <typeparam name="T">The type of vertex to which we are converting the bytes</typeparam>
        /// <param name="data">The bytes that will be converted to the vertices</param>
        /// <param name="vertexSize">The size of one vertex</param>
        /// <param name="device">Any working device; required to use our conversion hack</param>
        /// <returns>An array of the converted vertices</returns>
        public static T[] Convert<T>(byte[] data, int vertexSize,
            GraphicsDevice device) where T : struct
        {
            T[] verts = new T[data.Length / vertexSize];
            using (VertexBuffer vb = new VertexBuffer(device, data.Length, ResourceUsage.None))
            {
                vb.SetData<byte>(data);
                vb.GetData<T>(verts);
            }
            return verts;
        }


        private static Quaternion qStart, qEnd, qResult;
        private static Vector3 curTrans, nextTrans, lerpedTrans;
        private static Vector3 curScale, nextScale, lerpedScale;
        private static Matrix startRotation, endRotation;
        private static Matrix returnMatrix;

        /// <summary>
        /// Roughly decomposes two matrices and performs spherical linear interpolation
        /// </summary>
        /// <param name="start">Source matrix for interpolation</param>
        /// <param name="end">Destination matrix for interpolation</param>
        /// <param name="slerpAmount">Ratio of interpolation</param>
        /// <returns>The interpolated matrix</returns>
        public static Matrix SlerpMatrix(Matrix start, Matrix end,
            float slerpAmount)
        {
            if (start == end)
                return start;
            // Get rotation components and interpolate (not completely accurate but I don't want 
            // to get into polar decomposition and this seems smooth enough)
            Quaternion.CreateFromRotationMatrix(ref start, out qStart);
            Quaternion.CreateFromRotationMatrix(ref end, out qEnd);
            Quaternion.Lerp(ref qStart, ref qEnd, slerpAmount, out qResult);

            // Get final translation components
            curTrans.X = start.M41;
            curTrans.Y = start.M42;
            curTrans.Z = start.M43;
            nextTrans.X = end.M41;
            nextTrans.Y = end.M42;
            nextTrans.Z = end.M43;
            Vector3.Lerp(ref curTrans, ref nextTrans, slerpAmount, out lerpedTrans);

            // Get final scale component
            Matrix.CreateFromQuaternion(ref qStart, out startRotation);
            Matrix.CreateFromQuaternion(ref qEnd, out endRotation);
            curScale.X = start.M11 - startRotation.M11;
            curScale.Y = start.M22 - startRotation.M22;
            curScale.Z = start.M33 - startRotation.M33;
            nextScale.X = end.M11 - endRotation.M11;
            nextScale.Y = end.M22 - endRotation.M22;
            nextScale.Z = end.M33 - endRotation.M33;
            Vector3.Lerp(ref curScale, ref nextScale, slerpAmount, out lerpedScale);

            // Create the rotation matrix from the slerped quaternions
            Matrix.CreateFromQuaternion(ref qResult, out returnMatrix);

            // Set the translation
            returnMatrix.M41 = lerpedTrans.X;
            returnMatrix.M42 = lerpedTrans.Y;
            returnMatrix.M43 = lerpedTrans.Z;

            // And the lerped scale component
            returnMatrix.M11 += lerpedScale.X;
            returnMatrix.M22 += lerpedScale.Y;
            returnMatrix.M33 += lerpedScale.Z;
            return returnMatrix;
        }

        /// <summary>
        /// Roughly decomposes two matrices and performs spherical linear interpolation
        /// </summary>
        /// <param name="start">Source matrix for interpolation</param>
        /// <param name="end">Destination matrix for interpolation</param>
        /// <param name="slerpAmount">Ratio of interpolation</param>
        /// <param name="result">Stores the result of hte interpolation.</param>
        public static void SlerpMatrix(
            ref Matrix start, 
            ref Matrix end,
            float slerpAmount,
            out Matrix result)
        {
            if (start == end)
            {
                result = start;
                return;
            }
            // Get rotation components and interpolate (not completely accurate but I don't want 
            // to get into polar decomposition and this seems smooth enough)
            Quaternion.CreateFromRotationMatrix(ref start, out qStart);
            Quaternion.CreateFromRotationMatrix(ref end, out qEnd);
            Quaternion.Lerp(ref qStart, ref qEnd, slerpAmount, out qResult);

            // Get final translation components
            curTrans.X = start.M41;
            curTrans.Y = start.M42;
            curTrans.Z = start.M43;
            nextTrans.X = end.M41;
            nextTrans.Y = end.M42;
            nextTrans.Z = end.M43;
            Vector3.Lerp(ref curTrans, ref nextTrans, slerpAmount, out lerpedTrans);

            // Get final scale component
            Matrix.CreateFromQuaternion(ref qStart, out startRotation);
            Matrix.CreateFromQuaternion(ref qEnd, out endRotation);
            curScale.X = start.M11 - startRotation.M11;
            curScale.Y = start.M22 - startRotation.M22;
            curScale.Z = start.M33 - startRotation.M33;
            nextScale.X = end.M11 - endRotation.M11;
            nextScale.Y = end.M22 - endRotation.M22;
            nextScale.Z = end.M33 - endRotation.M33;
            Vector3.Lerp(ref curScale, ref nextScale, slerpAmount, out lerpedScale);

            // Create the rotation matrix from the slerped quaternions
            Matrix.CreateFromQuaternion(ref qResult, out result);

            // Set the translation
            result.M41 = lerpedTrans.X;
            result.M42 = lerpedTrans.Y;
            result.M43 = lerpedTrans.Z;

            // Add the lerped scale component
            result.M11 += lerpedScale.X;
            result.M22 += lerpedScale.Y;
            result.M33 += lerpedScale.Z;
        }


        /// <summary>
        /// Determines whether or not a ModelMeshPart is skinned.
        /// </summary>
        /// <param name="meshPart">The part to check.</param>
        /// <returns>True if the part is skinned.</returns>
        public static bool IsSkinned(ModelMeshPart meshPart)
        {
            VertexElement[] ves = meshPart.VertexDeclaration.GetVertexElements();
            foreach (VertexElement ve in ves)
            {
                //(BlendIndices with UsageIndex = 0) specifies matrix indices for fixed-function vertex 
                // processing using indexed paletted skinning.
                if (ve.VertexElementUsage == VertexElementUsage.BlendIndices
                    && ve.UsageIndex == 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether or not a ModelMesh is skinned.
        /// </summary>
        /// <param name="mesh">The mesh to check.</param>
        /// <returns>True if the mesh is skinned.</returns>
        public static bool IsSkinned(ModelMesh mesh)
        {
            foreach (ModelMeshPart mmp in mesh.MeshParts)
            {
                if (IsSkinned(mmp))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether or not a Model is skinned.
        /// </summary>
        /// <param name="model">The model to check.</param>
        /// <returns>True if the model is skinned.</returns>
        public static bool IsSkinned(Model model)
        {
            foreach (ModelMesh mm in model.Meshes)
            {
                if (IsSkinned(mm))
                    return true;
            }
            return false;
        }


         
    }








}
