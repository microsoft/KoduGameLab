
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using System.IO;

namespace TileProcessor
{
    #region MyRegion
	//[ContentProcessor]
    //class MyMaterialProcessor : MaterialProcessor
    //{
    //    protected override ExternalReference<TextureContent> BuildTexture(string textureName,
    //                                                                        ExternalReference<TextureContent> texture,
    //                                                                        ContentProcessorContext context)
    //    {
    //        //            texture.Name;
    //        System.Diagnostics.Debugger.Launch();
    //        return context.BuildAsset<TextureContent, TextureContent>(texture, "MyTextureProcessor");
    //    }
    //} 
	#endregion

    [ContentProcessor]
    class UIModelProcessor : ModelProcessor
    {
        //ModelContent modelContent = null;
        //static bool m_First = true;

        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            bool collapsed = BokuPipeline.MaterialsGroup.PreProcess(input);

            // stash our model content so that we can use it to look up meshes
            // 
            ModelContent model = base.Process(input, context);

            FindBBoxesRecurse(input, model);

            BokuPipeline.MaterialsGroup.TagMeshParts(model, collapsed);

            /*
            // Just debug foo, nothing to see here.
            foreach (ModelMeshContent mesh in model.Meshes)
            {
                foreach (ModelMeshPartContent part in mesh.MeshParts)
                {
                    MaterialContent mat = part.Material;
                    float alpha = mat.OpaqueData.GetValue<float>("Alpha", -1.0f);
                    Vector3 diffuse = mat.OpaqueData.GetValue<Vector3>("DiffuseColor", -Vector3.One);
                    if(alpha != 1.0f)
                    {
                    }
                }
            }
            */

            return model;



            //NodeContentCollection ncc = input.Children;

            //parseChildren(ncc);
            //ModelContent mc2 = base.Process(input, context);


            //mc2.Meshes[0].MeshParts[0];
            //int nMeshes = mc2.Meshes.Count;
            //BoundingBox[] boxes = new BoundingBox[nMeshes];
            //for(int i = 0; i < nMeshes; i++)
            //{
            //    boxes[i] = GetBoundingBox(mc2.Meshes[i]);
            //}
            //return m_ModelContent;
        }

        public void FindBBoxesRecurse(NodeContent input, ModelContent owner)
        {
            MeshContent mesh = input as MeshContent;

            if(mesh != null)
            {
                Vector3 minBox = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);
                Vector3 maxBox = new Vector3(-Single.MaxValue, -Single.MaxValue, -Single.MaxValue);
                                foreach (Vector3 x in mesh.Positions)
                {
                    //bool transform = false;
                    //if (transform)
                    //{
                    //    Vector3 xt = Vector3.Transform(x, mesh.AbsoluteTransform);
                    //    pointData[i++] = xt.X;
                    //    pointData[i++] = xt.Y;
                    //    pointData[i++] = xt.Z;
                    //}
                    //else
                    //{
                    //    pointData[i++] = x.X;
                    //    pointData[i++] = x.Y;
                    //    pointData[i++] = x.Z;
                    //}

                    minBox.X = Math.Min(minBox.X, x.X);
                    minBox.Y = Math.Min(minBox.Y, x.Y);
                    minBox.Z = Math.Min(minBox.Z, x.Z);

                    maxBox.X = Math.Max(maxBox.X, x.X);
                    maxBox.Y = Math.Max(maxBox.Y, x.Y);
                    maxBox.Z = Math.Max(maxBox.Z, x.Z);
                }


                // find the output mesh corresponding to the meshcontent object we're processing
                // (conversion has already happened; we're just looking at them after the fact
                foreach (ModelMeshContent m in owner.Meshes)
                {
                    if (m.Name == mesh.Name)
                    {
                        UIMeshData data = new UIMeshData();
                        data.bBox.Min = minBox;
                        data.bBox.Max = maxBox;

                        m.Tag = data;
                    }
                }
            }

            foreach (NodeContent child in input.Children)
            {
                FindBBoxesRecurse(child, owner);
            }
        }
    }
}
