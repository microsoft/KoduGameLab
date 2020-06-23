
using System;
using System.Collections;
using System.Diagnostics;

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
    /// Base class for SROs used by GameProp and GamePropMenuItem
    /// </summary>
    public abstract class GamePropSRO : FBXModel
    {
        public GamePropSRO(String resourceName)
            : base(resourceName)
        {
        }

        public override void Render(Camera camera, ref Matrix worldMatrix, ArrayList listPartsReplacement)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            ArrayList meshInfoList = listPartsReplacement == null ? infoList : listPartsReplacement;

            Matrix viewProjMatrix = camera.ViewMatrix * camera.ProjectionMatrix;

            effect.Parameters["Shininess"].SetValue(1.0f);

            for (int i = 0; i < model.Meshes.Count; i++)
            {
                ModelMesh mesh = (ModelMesh)model.Meshes[i];
                ArrayList list = (ArrayList)meshInfoList[i];

                Matrix boneWorldMatrix = mesh.ParentBone.Transform * worldMatrix;
                Matrix worldViewProjMatrix = boneWorldMatrix * viewProjMatrix;

                effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
                effect.Parameters["WorldMatrix"].SetValue(boneWorldMatrix);

                device.Indices = mesh.IndexBuffer;

                for (int j = 0; j < mesh.MeshParts.Count; j++)
                {
                    ModelMeshPart part = (ModelMeshPart)mesh.MeshParts[j];

                    device.VertexDeclaration = part.VertexDeclaration;
                    device.Vertices[0].SetSource(mesh.VertexBuffer, part.StreamOffset, part.VertexStride);

                    if (InGame.inGame.renderEffects != null)
                    {
                        effect.CurrentTechnique = effect.Techniques[InGame.inGame.renderEffects];
                    }
                    else
                    {

                        // Apply part info params.
                        PartInfo partInfo = (PartInfo)list[j];
                        effect.Parameters["SpecularColor"].SetValue(partInfo.SpecularColor);
                        effect.Parameters["EmissiveColor"].SetValue(partInfo.EmissiveColor);
                        effect.Parameters["SpecularPower"].SetValue(partInfo.SpecularPower);

                        if (partInfo.DiffuseTexture == null)
                        {
                            // Only tint white parts...
                            if (partInfo.DiffuseColor == new Vector4(1.0f))
                            {
                                effect.Parameters["DiffuseColor"].SetValue(RenderColor);
                            }
                            else
                            {
                                effect.Parameters["DiffuseColor"].SetValue(partInfo.DiffuseColor);
                            }
                            effect.CurrentTechnique = effect.Techniques["NonTexturedColorPass"];
                        }
                        else
                        {
                            effect.Parameters["DiffuseColor"].SetValue(partInfo.DiffuseColor);
                            effect.CurrentTechnique = effect.Techniques["TexturedColorPass"];
                            effect.Parameters["DiffuseTexture"].SetValue(partInfo.DiffuseTexture);
                        }

                        effect.CommitChanges();
                    }

                    effect.Begin();
                    for (int indexEffectPass = 0; indexEffectPass < effect.CurrentTechnique.Passes.Count; indexEffectPass++)
                    {
                        EffectPass pass = (EffectPass)effect.CurrentTechnique.Passes[indexEffectPass];
                        pass.Begin();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                        part.BaseVertex,
                                                        0,
                                                        part.NumVertices,
                                                        part.StartIndex,
                                                        part.PrimitiveCount);

                        pass.End();
                    }
                    effect.End();
                }
            }
        }
    }  
}  