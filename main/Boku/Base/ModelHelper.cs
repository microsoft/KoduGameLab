// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//#define DEBUG_OUTPUT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace Boku.Base
{
    public class ModelHelper
    {
        static public ModelMesh FindMatchingMeshForBone(Model model, ModelBone bone)
        {
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                if (modelMesh.ParentBone == bone)
                {
                    return modelMesh;
                }
            }
            return null;
        }
/*
        static public BoundingBox CalculateBoundingBox( ModelMesh mesh)
        {
            int cVertexBuffer = mesh.VertexBuffer.SizeInBytes / mesh.MeshParts[0].VertexStride;

            VertexPositionNormalTexture[] listVerticies = new VertexPositionNormalTexture[cVertexBuffer];
            mesh.VertexBuffer.GetData<VertexPositionNormalTexture>( listVerticies );

            Vector3[] listPoints = new Vector3[listVerticies.Length];
            for (int iVertex = 0; iVertex < listVerticies.Length; iVertex++)
            {
                listPoints[iVertex] = listVerticies[iVertex].Position;
            }
            BoundingBox box = BoundingBox.CreateFromPoints( listPoints );
            return box;
        }
*/
        static public void DebugOutTransform( string indent, Matrix matrix)
        {
#if DEBUG_OUTPUT
            Debug.Print("{4} {0,8:F}  {1,8:F}  {2,8:F}  {3,8:F}", matrix.M11, matrix.M12, matrix.M13, matrix.M14, indent);
            Debug.Print("{4} {0,8:F}  {1,8:F}  {2,8:F}  {3,8:F}", matrix.M21, matrix.M22, matrix.M23, matrix.M24, indent);
            Debug.Print("{4} {0,8:F}  {1,8:F}  {2,8:F}  {3,8:F}", matrix.M31, matrix.M32, matrix.M33, matrix.M34, indent);
            Debug.Print("{4} {0,8:F}  {1,8:F}  {2,8:F}  {3,8:F}", matrix.M41, matrix.M42, matrix.M43, matrix.M44, indent);
#endif
        }

        static public void DumpBoneTree(Model model, ModelBone bone, bool showMatrix)
        {
#if DEBUG_OUTPUT
            RecurseDumpBoneTree(model, bone, "", showMatrix);
#endif
        }
#if DEBUG_OUTPUT
        static protected void RecurseDumpBoneTree(Model model, ModelBone bone, string indent, bool showMatrix)
        {
            ModelMesh mesh = ModelHelper.FindMatchingMeshForBone(model, bone);
            if (mesh != null)
            {
                Debug.Print("{0}{1} ({2})", indent, bone.Name, mesh.Name);
            }
            else
            {
                Debug.Print("{0}{1}", indent, bone.Name);
            }
            DebugOutTransform(indent, bone.Transform);

            indent += "    ";
            foreach(ModelBone boneChild in bone.Children)
            {
                RecurseDumpBoneTree(model, boneChild, indent, showMatrix);
            }
        }
#endif
    }
}
