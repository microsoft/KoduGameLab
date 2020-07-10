// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// A grid element which displays a cube using one of the standard cube materials.
    /// </summary>
    public class UIGridWaterElement : UIGridMaterialElement
    {
        private static VertexBuffer vbuf = null;
        private static IndexBuffer ibuf = null;
        private static int numVerts = 4;
        private static int numTris = 2;
        private static Texture2D glowTexture = null;

        private static Vector3[] faceDirs = {
            Vector3.UnitZ,
            -Vector3.UnitY,
            Vector3.UnitY,
            -Vector3.UnitX,
            Vector3.UnitX };

        #region Accessors
        #endregion

        // c'tor
        public UIGridWaterElement(int materialIndex)
            : base(materialIndex)
        {
        }

        public override void Render(Camera camera)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
            Terrain terrain = BokuGame.bokuGame.inGame.Terrain;

            int materialIndex = uiSlot;

            // We need to push the near plane out more than is normal for UI 
            // so that the material cubes don't render behind the terrain.
            Camera cam = camera;
            cam.NearClip = 3.0f;

            Matrix twistMatrix = Matrix.CreateRotationZ(worldMatrix.Translation.X + 0.5f);
            float scale = 2.0f * MathHelper.Clamp(2.0f / ((float)Math.Abs(worldMatrix.Translation.X) + 1.0f), 0.1f, 1.0f);
            Matrix scaleMatrix = Matrix.CreateScale(scale);

            Matrix world = twistMatrix * scaleMatrix * worldMatrix;

            Vector3 trans = world.Translation;
            trans.X *= scale * 0.5f + 0.5f;

            // Shift the water cubes down a bit so they better fit into the reticule.
            trans.Y -= 0.4f;
            world.Translation = trans;

            // Magic numbers to make transform work out right.
            hitWorld = world;
            trans.Y += 0.7f - 0.7f * (1.0f - world.M33);
            hitWorld.Translation = trans;

            Matrix worldToCamera = world * cam.ViewMatrix;

            device.SetVertexBuffer(vbuf);

            device.Indices = ibuf;

            /// Render back facing first.
            for (int face = 1; face < Tile.NumFaces; ++face)
            {
                Vector3 camFaceDir = Vector3.TransformNormal(faceDirs[face], worldToCamera);
                if (camFaceDir.Z < 0)
                {

                    terrain.PreRenderWaterCube(
                        device,
                        0.5f,
                        materialIndex,
                        face,
                        world,
                        camera);


                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVerts, 0, numTris);

                    terrain.PostRenderWaterCube();
                }
            }

            /// Then front facing.
            for (int face = 1; face < Tile.NumFaces; ++face)
            {
                Vector3 camFaceDir = Vector3.TransformNormal(faceDirs[face], worldToCamera);
                if (camFaceDir.Z >= 0)
                {

                    terrain.PreRenderWaterCube(
                        device,
                        0.5f,
                        materialIndex,
                        face,
                        world,
                        camera);


                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVerts, 0, numTris);

                    terrain.PostRenderWaterCube();
                }
            }

            /// Always do the top face last.
            terrain.PreRenderWaterCube(
                device,
                0.5f,
                materialIndex,
                (int)Tile.Face.Top,
                world,
                camera);


            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVerts, 0, numTris);

            terrain.PostRenderWaterCube();

            RenderGlow(camera, worldToCamera, materialIndex);

            RenderLabel(camera, world);
        }   // end of UIGridWaterElement Render()

        public override void LoadContent(bool immediate)
        {
        }   // end of UIGridWaterElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            if (vbuf == null)
            {
                vbuf = new VertexBuffer(device, typeof(VirtualMap.WaterVertex), 4, BufferUsage.WriteOnly);

                VirtualMap.WaterVertex[] verts = new VirtualMap.WaterVertex[4];

                verts[0] = new VirtualMap.WaterVertex(
                    new Vector3(0.0f, 0.0f, 0.0f),
                    new Vector2(-1, -1),
                    Color.Transparent);
                verts[1] = new VirtualMap.WaterVertex(
                    new Vector3(0.0f, 0.0f, 0.0f),
                    new Vector2(1, -1),
                    Color.Transparent);
                verts[2] = new VirtualMap.WaterVertex(
                    new Vector3(0.0f, 0.0f, 0.0f),
                    new Vector2(1, 1),
                    Color.Transparent);
                verts[3] = new VirtualMap.WaterVertex(
                    new Vector3(0.0f, 0.0f, 0.0f),
                    new Vector2(-1, 1),
                    Color.Transparent);

                vbuf.SetData<VirtualMap.WaterVertex>(verts);
            }

            if (ibuf == null)
            {
                UInt16[] localIdx = new UInt16[6];
                int idx = 0;
                localIdx[idx++] = (UInt16)(0);
                localIdx[idx++] = (UInt16)(1);
                localIdx[idx++] = (UInt16)(3);

                localIdx[idx++] = (UInt16)(3);
                localIdx[idx++] = (UInt16)(1);
                localIdx[idx++] = (UInt16)(2);

                ibuf = new IndexBuffer(device, typeof(UInt16), 6, BufferUsage.WriteOnly);
                ibuf.SetData<UInt16>(localIdx);
            }

            if (glowTexture == null)
            {
                glowTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\Tools\WaterGlow");
            }
        }

        public override void UnloadContent()
        {
            BokuGame.Release(ref vbuf);
            BokuGame.Release(ref ibuf);
            BokuGame.Release(ref glowTexture);
        }   // end of UIGridWaterElement UnloadContent()

        #region Internal
        /// <summary>
        /// Fake the explicit bloom some water materials have, since the real bloom effect doesn't
        /// work in the UI (it's postprocess).
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="worldToCamera"></param>
        /// <param name="materialIndex"></param>
        private void RenderGlow(Camera camera, Matrix worldToCamera, int materialIndex)
        {
            if (Water.Types[materialIndex].ExplicitBloom > 0.0f)
            {
                /// Figure out how tall the water cube is.
                Vector3 normal = Vector3.Zero;
                float offset = Terrain.GetCycleOffset(Vector2.Zero, ref normal);
                offset -= (float)Time.GameTimeTotalSeconds;
                float waveHeight = Terrain.WaveHeight * (float)(Math.Sin(offset) - 1.0);

                /// Lots of hacky constants here, just trying to get the quad
                /// to match in size with this undulating blobby squishy cube.
                /// Sorry, but not really.
                CameraSpaceQuad quad = CameraSpaceQuad.GetInstance();
                Vector2 position = new Vector2(Position.X, Position.Y);
                position.X = worldToCamera.Translation.X;
                position.Y = worldToCamera.Translation.Y + (waveHeight + 0.4f) * 0.5f * worldToCamera.Up.Length();
                Vector2 size = new Vector2(
                    Size.X * worldToCamera.Right.Length() * 0.8f,
                    Size.Y * worldToCamera.Up.Length() * (1.0f + waveHeight));
                quad.Render(camera,
                    glowTexture,
                    2.0f * Water.Types[materialIndex].ExplicitBloom,
                    position,
                    size,
                    @"AdditiveBlend");
            }
        }

        /// <summary>
        /// Draw a numeric label under each cube, identifying it by its UI slot.
        /// Might be cool to have the letters spin as the cube spins. Might not.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="world"></param>
        private void RenderLabel(Camera camera, Matrix world)
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont18Bold;
            SpriteBatch batch = UI2D.Shared.SpriteBatch;

            // Handle small screens.
            if (device.Viewport.Height < 720)
            {
                Font = UI2D.Shared.GetGameFont15_75;
            }

            /// Find a placement sort of lower cube
            Matrix center = Matrix.Identity;
            center.Translation = new Vector3(0.0f, 0.0f, 0.1f);
            Matrix worldViewProj = center * world * camera.ViewProjectionMatrix;

            /// Find the screen position
            Vector4 screenPos = new Vector4(0, 0, 0, 1.0f);
            screenPos = Vector4.Transform(screenPos, worldViewProj);
            screenPos.X /= screenPos.W;
            screenPos.Y /= screenPos.W;

            /// Translate to pixel coords. Note the flip Y.
            int x = (int)((screenPos.X * 0.5f + 0.5f) * BokuGame.ScreenSize.X + 0.5f);
            int y = (int)((screenPos.Y * -0.5f + 0.5f) * BokuGame.ScreenSize.Y + 0.5f);

            /// Center the string
            string uiSlotString = (uiSlot + 1).ToString();
            Vector2 strSize = Font().MeasureString(uiSlotString);
            x = (int)(x - strSize.X * 0.5f + 0.5f);

            /// Compute how centered the cube is as a var [0..1].
            float centerness = Math.Abs(x - BokuGame.ScreenSize.X * 0.5f);
            centerness /= BokuGame.ScreenSize.X * 0.5f;
            centerness = (0.75f - centerness) / 0.75f;
            centerness = MathHelper.Clamp(centerness, 0.0f, 1.0f);
            centerness = MyMath.SmoothStep(0.0f, 1.0f, centerness);

            /// Translate centeredness into opacity, fading out in either extreme.
            float kMinAlpha = 64.0f;
            byte byteAlpha = (byte)(kMinAlpha + centerness * (255.9f - kMinAlpha));
            byteAlpha = 255;

            /// Centeredness into scale. Note that this keeps the numbers from
            /// overlapping when the cubes are very close together.
            float kMinScale = 0.5f;
            float kMaxScale = 1.0f;
            float scale = kMinScale + centerness * (kMaxScale - kMinScale);

            /// Shadow and text colors, with appropriate opacity.
            Color darkGrey = new Color(10, 10, 10, byteAlpha);
            Color transWhite = new Color(255, 255, 255, byteAlpha);

            // Finally, just do it. Note that we can't put the batching any
            // higher up the food chain, because the generic grid doesn't know
            // whether it's going to be rendering text. Although if perf is an
            // issue, it might be worth looking at moving the batch.begin()/end()
            // up to the BasePicker.Render().
            // Note this no longer uses 'scale' to shrink the text to match the cubes.
            // Do we really care?
            // Note this uses SpriteFont rendering since there are so many labels (numbers).
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            {
                batch.DrawString(Font().spriteFont, uiSlotString, new Vector2(x + 1, y + 1), darkGrey);
                batch.DrawString(Font().spriteFont, uiSlotString, new Vector2(x, y), transWhite);
            }
            batch.End();

        }


        #endregion Internal

    }   // end of class UIGridWaterElement

}   // end of namespace Boku.UI2D
