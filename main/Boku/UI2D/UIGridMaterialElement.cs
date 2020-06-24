
using System;
using System.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;
using KoiX.UI.Dialogs;

using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using System.Diagnostics;

namespace Boku.UI2D
{
    /// <summary>
    /// A grid element which displays a cube using one of the standard cube materials.
    /// </summary>
    public class UIGridMaterialElement : UIGridElement
    {
        protected int uiSlot = 0;
        protected bool selected = false;

        protected float selectedScale = 1.0f;
        protected float unselectedScale = 0.75f;

        private static VertexBuffer vBuff = null;
        private static VertexBuffer vBuff_FA = null;
        private static IndexBuffer iBuff_FA = null;

        // A local version of the world matrix used for hit testing.
        // This is needed since we tweak the object position in the render call.
        protected Matrix hitWorld = Matrix.Identity;

        #region Accessors
        public override bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    selected = value;
                    if (selected)
                    {
                        // scale = selectedScale;
                        TwitchManager.Set<float> set = delegate(float val, Object param) { scale = val; };
                        TwitchManager.CreateTwitch<float>(scale, selectedScale, set, 0.2f, TwitchCurve.Shape.OvershootInOut);
                    }
                    else
                    {
                        // scale = unselectedScale;
                        TwitchManager.Set<float> set = delegate(float val, Object param) { scale = val; };
                        TwitchManager.CreateTwitch<float>(scale, unselectedScale, set, 0.2f, TwitchCurve.Shape.OvershootInOut);
                    }
                }
            }
        }
        public override Vector2 Size
        {
            get { return Vector2.One; }
            set { /* do nothing, should be removed from base class */ }
        }
        /// <summary>
        /// Scale to use when this material is selected.
        /// </summary>
        public float SelectedScale
        {
            get { return selectedScale; }
            set
            {
                selectedScale = value;
                if (selected)
                {
                    scale = selectedScale;
                }
            }
        }
        /// <summary>
        /// Scale to use when this material is unselected.
        /// </summary>
        public float UnselectedScale
        {
            get { return unselectedScale; }
            set
            {
                unselectedScale = value;
                if (!selected)
                {
                    scale = unselectedScale;
                }
            }
        }

        /// <summary>
        /// Local version of the world matrix used for hit testing.
        /// </summary>
        public Matrix HitWorldMatrix
        {
            get { return hitWorld; }
        }

        #endregion

        // c'tor
        public UIGridMaterialElement(int uiSlot)
        {
            this.uiSlot = uiSlot;

            rotation.X = MathHelper.ToRadians(-70.0f);
            scale = unselectedScale;
        }

        public override void HandleMouseInput(Vector2 hitUV)
        {
        }   // end of HandleMouseInput()

        public override void HandleTouchInput(TouchContact touch, Vector2 hitUV)
        {
        }   // end of HandleTouchInput()

        public override void Render(Camera camera)
        {
            // Don't bother if offscreen.
            Vector3 position = worldMatrix.Translation;
            float radius = 2.0f;
            Frustum.CullResult cullResult = camera.Frustum.CullTest(position, radius);
            if (cullResult == Frustum.CullResult.TotallyOutside)
                return;

            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            Terrain terrain = BokuGame.bokuGame.inGame.Terrain;

            int materialIndex = Terrain.UISlotToMatIndex(uiSlot);

            Effect effect = terrain.EffectColor;
            EffectTechnique technique = TerrainMaterial.Get(materialIndex).TechniqueColor(TerrainMaterial.EffectTechs.TerrainColorPass);

            effect.CurrentTechnique = technique;

            // We need to push the near plane out more than is normal for UI 
            // so that the material cubes don't render behind the terrain.
            Camera cam = camera;
            cam.NearClip = 6.0f;

            Matrix twistMatrix = Matrix.CreateRotationZ(worldMatrix.Translation.X + 0.5f);
            float scale = MathHelper.Clamp(2.0f / ((float)Math.Abs(worldMatrix.Translation.X) + 1.0f), 0.1f, 1.0f);
            Matrix scaleMatrix = Matrix.CreateScale(scale);

            Matrix world = twistMatrix * scaleMatrix * worldMatrix;

            Vector3 trans = world.Translation;
            trans.X *= scale + 0.5f;
            world.Translation = trans;

            hitWorld = world;

            // Compensate for verts being [0..1] instead of [-0.5..0.5] as we'd prefer.
            // They must be in [0..1] because UV mapping is implicit in local position.
            Matrix preTrans = Matrix.Identity;
            preTrans.Translation = new Vector3(-0.5f, -0.5f, -0.5f);
            world = preTrans * world;

            Matrix worldViewProjMatrix = world * cam.ViewProjectionMatrix;

            terrain.ParameterColor(Terrain.EffectParams.WorldMatrix).SetValue(worldMatrix);
            terrain.ParameterColor(Terrain.EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);
            terrain.ParameterColor(Terrain.EffectParams.WarpCenter).SetValue(Vector4.Zero);
            terrain.ParameterEdit(Terrain.EffectParams.WorldMatrix).SetValue(worldMatrix);
            terrain.ParameterEdit(Terrain.EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);
            terrain.ParameterEdit(Terrain.EffectParams.WarpCenter).SetValue(Vector4.Zero);

#if NETFX_CORE
                // Note: Indexing into shaders doesn't work with MG.  Apparently it
                // was some hack done in XNA related to the Effect code they used.
                // Anyway, instead of using this indexing we need to pick and set 
                // the right technique which we do further down from here.
#else
            if (BokuSettings.Settings.PreferReach)
            {
                //Select the VS based on the number of point-lights
                var lightNum = Boku.Fx.Luz.Count;
                if (lightNum > 6)
                {
                    terrain.ParameterColor(Terrain.EffectParams.VSIndex).SetValue(4);
                    terrain.ParameterEdit(Terrain.EffectParams.VSIndex).SetValue(4);
                }
                else if (lightNum > 4)
                {
                    terrain.ParameterColor(Terrain.EffectParams.VSIndex).SetValue(3);
                    terrain.ParameterEdit(Terrain.EffectParams.VSIndex).SetValue(3);
                }
                else if (lightNum > 2)
                {
                    terrain.ParameterColor(Terrain.EffectParams.VSIndex).SetValue(2);
                    terrain.ParameterEdit(Terrain.EffectParams.VSIndex).SetValue(2);
                }
                else if (lightNum > 0)
                {
                    terrain.ParameterColor(Terrain.EffectParams.VSIndex).SetValue(1);
                    terrain.ParameterEdit(Terrain.EffectParams.VSIndex).SetValue(1);
                }
                else
                {
                    terrain.ParameterColor(Terrain.EffectParams.VSIndex).SetValue(0);
                    terrain.ParameterEdit(Terrain.EffectParams.VSIndex).SetValue(0);
                }

                //Select the PS
                terrain.ParameterColor(Terrain.EffectParams.PSIndex).SetValue(0);
                terrain.ParameterEdit(Terrain.EffectParams.PSIndex).SetValue(0);
            }
            else // Shader Model v3
            {
                //SM3 only uses one VS
                terrain.ParameterColor(Terrain.EffectParams.VSIndex).SetValue(5);
                terrain.ParameterEdit(Terrain.EffectParams.VSIndex).SetValue(5);

                //Select the PS
                terrain.ParameterColor(Terrain.EffectParams.PSIndex).SetValue(2);
                terrain.ParameterEdit(Terrain.EffectParams.PSIndex).SetValue(2);
            }
#endif

            if (TerrainMaterialDialog.FabricMode)
            {
                var cubeSize = 1f;
                terrain.ParameterColor(Terrain.EffectParams.InvCubeSize).SetValue(new Vector3(cubeSize, 1.0f / cubeSize, cubeSize * 0.5f));
                terrain.ParameterEdit(Terrain.EffectParams.InvCubeSize).SetValue(new Vector3(cubeSize, 1.0f / cubeSize, cubeSize * 0.5f));

                #region Fabric
                device.SetVertexBuffer(vBuff_FA);
                device.Indices = iBuff_FA;

                terrain.SetGlobalParams_FA();
                terrain.SetMaterialParams_FA((ushort)materialIndex, true);

                TerrainMaterial mat = TerrainMaterial.Get(materialIndex);

#if NETFX_CORE
                int lightNum = Boku.Fx.Luz.Count;
                if (lightNum > 6)
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L10_FA_SM2"];
                }
                else if (lightNum > 4)
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L6_FA_SM2"];
                }
                else if (lightNum > 2)
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L4_FA_SM2"];
                }
                else if (lightNum > 0)
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L2_FA_SM2"];
                }
                else
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L0_FA_SM2"];
                }
#else
                effect.CurrentTechnique = mat.TechniqueColor(TerrainMaterial.EffectTechs_FA.TerrainColorPass_FA);
#endif
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 37, 0, 56);
                }

                #endregion
            }
            else
            {
                #region Cube
                var cubeSize = 1.0f;
                terrain.ParameterColor(Terrain.EffectParams.InvCubeSize).SetValue(new Vector3(cubeSize, 1.0f / cubeSize, cubeSize * 0.5f));
                terrain.ParameterEdit(Terrain.EffectParams.InvCubeSize).SetValue(new Vector3(cubeSize, 1.0f / cubeSize, cubeSize * 0.5f));

                Tile.CheckIndices(new Point(32, 32));

                if (Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws)
                {
                    device.SetVertexBuffer(vBuff);

                    device.Indices = Tile.IndexBuffer_FD();

                    terrain.SetGlobalParams_FD();
                    terrain.SetMaterialParams_FD((ushort)materialIndex, true);
                }

                TerrainMaterial mat = TerrainMaterial.Get(materialIndex);
                
#if NETFX_CORE
                int lightNum = Boku.Fx.Luz.Count;
                if (lightNum > 6)
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L10_FD_SM2"];
                }
                else if (lightNum > 4)
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L6_FD_SM2"];
                }
                else if (lightNum > 2)
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L4_FD_SM2"];
                }
                else if (lightNum > 0)
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L2_FD_SM2"];
                }
                else
                {
                    effect.CurrentTechnique = effect.Techniques["TerrainColorPass_L0_FD_SM2"];
                }
#else
                effect.CurrentTechnique = mat.TechniqueColor(TerrainMaterial.EffectTechs.TerrainColorPass);
#endif

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    if (Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws)
                    {
                        terrain.SetTopParams_FD((ushort)materialIndex, true);
                        pass.Apply();

                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2); //20 verts, 10 triangles

                        terrain.SetSideParams_FD((ushort)materialIndex, true);
                        pass.Apply();

                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 4, 0, 16, 0, 8); //20 verts, 10 triangles
                    }

                }
                #endregion
            }

            RenderLabel(camera, world);

        }   // end of UIGridMaterialElement Render()

        /// <summary>
        /// Draw a numeric label under each cube, identifying it by its UI slot.
        /// Might be cool to have the letters spin as the cube spins. Might not.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="world"></param>
        private void RenderLabel(Camera camera, Matrix world)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            GetFont Font = SharedX.GetGameFont18Bold;
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            // Handle small screens.
            if (BokuGame.ScreenSize.Y < 720)
            {
                Font = SharedX.GetGameFont15_75;
            }

            /// Find a placement sort of lower cube
            Matrix center = Matrix.Identity;
            center.Translation = new Vector3(0.5f, 0.5f, 0.0f);
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

            int label = Terrain.MaterialIndexToLabel(Terrain.UISlotToMatIndex(uiSlot));
            string uiSlotString = (label).ToString();
            Vector2 strSize = Font().MeasureString(uiSlotString);
            x = (int)(x - strSize.X * scale * 0.5f + 0.5f);

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

        public override void LoadContent(bool immediate)
        {
        }   // end of UIGridMaterialElement LoadContent()

        public override void InitDeviceResources(GraphicsDevice device)
        {
            #region Cubes
            if (vBuff == null)
            {
                if (Terrain.RenderMethod == Terrain.RenderMethods.FewerDraws)
                {
                    Terrain.TerrainVertex_FD[] verts =
                    {
                        //Top
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 0.0f, 1.0f), 1.0f, 0, 0),
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 1.0f, 1.0f), 1.0f, 0, 2),
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 1.0f, 1.0f), 1.0f, 0, 3),
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 0.0f, 1.0f), 1.0f, 0, 1),
                        //Front
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 0.0f, 1.0f), 1.0f, 1, 0),
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 0.0f, 1.0f), 1.0f, 1, 1),
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 0.0f, 0.0f), 1.0f, 1, 1),
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 0.0f, 0.0f), 1.0f, 1, 0),
                        //Back
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 1.0f, 1.0f), 1.0f, 2, 2),
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 1.0f, 1.0f), 1.0f, 2, 2),
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 1.0f, 0.0f), 1.0f, 2, 2),
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 1.0f, 0.0f), 1.0f, 2, 3),
                        //Left
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 1.0f, 1.0f), 1.0f, 3, 2),
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 0.0f, 1.0f), 1.0f, 3, 0),
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 0.0f, 0.0f), 1.0f, 3, 0),
                        new Terrain.TerrainVertex_FD(new Vector3(0.0f, 1.0f, 0.0f), 1.0f, 3, 2),
                        //Right
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 0.0f, 1.0f), 1.0f, 4, 1),
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 1.0f, 1.0f), 1.0f, 4, 2),
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 1.0f, 0.0f), 1.0f, 4, 2),
                        new Terrain.TerrainVertex_FD(new Vector3(1.0f, 0.0f, 0.0f), 1.0f, 4, 1),
                    };

                    vBuff = new VertexBuffer(device, typeof(Terrain.TerrainVertex_FD), verts.Length, BufferUsage.None);
                    vBuff.SetData(verts);
                }

                Debug.Assert(vBuff != null, "Unknown render method!");
            }
            #endregion

            #region Fabric
            if (vBuff_FA == null || iBuff_FA == null)
            {
                const float h0 = 0.5f;
                const float h1 = h0 + 1.5f;
                const float h2 = h0 + 4.0f;
                const float h3 = h0 + 4.5f;
                const float h4 = h0 + 5.0f;
                const float s = 0.25f;

                Terrain.TerrainVertex_FA[] fabricVerts =
                {
                    new Terrain.TerrainVertex_FA(new Vector3(0f, 2f, h0) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(0f, 3f, h0) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(0f, 4f, h0) * s, Vector3.UnitZ),

                    new Terrain.TerrainVertex_FA(new Vector3(1f, 1f, h0) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(1f, 2f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(1f, 3f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(1f, 4f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(1f, 5f, h0) * s, Vector3.UnitZ),

                    new Terrain.TerrainVertex_FA(new Vector3(2f, 0f, h0) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(2f, 1f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(2f, 2f, h2) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(2f, 3f, h3) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(2f, 4f, h2) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(2f, 5f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(2f, 6f, h0) * s, Vector3.UnitZ),

                    new Terrain.TerrainVertex_FA(new Vector3(3f, 0f, h0) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(3f, 1f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(3f, 2f, h3) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(3f, 3f, h4) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(3f, 4f, h3) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(3f, 5f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(3f, 6f, h0) * s, Vector3.UnitZ),

                    new Terrain.TerrainVertex_FA(new Vector3(4f, 0f, h0) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(4f, 1f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(4f, 2f, h2) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(4f, 3f, h3) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(4f, 4f, h2) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(4f, 5f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(4f, 6f, h0) * s, Vector3.UnitZ),

                    new Terrain.TerrainVertex_FA(new Vector3(5f, 1f, h0) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(5f, 2f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(5f, 3f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(5f, 4f, h1) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(5f, 5f, h0) * s, Vector3.UnitZ),

                    new Terrain.TerrainVertex_FA(new Vector3(6f, 2f, h0) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(6f, 3f, h0) * s, Vector3.UnitZ),
                    new Terrain.TerrainVertex_FA(new Vector3(6f, 4f, h0) * s, Vector3.UnitZ),
                };

                short[] fabricIndices =
                {
                    1,5,4,
                    1,2,5,
                    5,2,6,
                    2,7,6,
                    2,3,7,
                    3,8,7,
                    9,4,10,
                    4,5,10,
                    10,5,11,
                    11,5,6,
                    11,6,12,
                    12,6,13,
                    6,7,13,
                    13,7,14,
                    7,8,14,
                    14,8,15,
                    16,9,10,
                    16,10,17,
                    17,10,11,
                    17,11,18,
                    18,11,12,
                    18,12,19,
                    19,12,20,
                    12,13,20,
                    20,13,21,
                    13,14,21,
                    21,14,22,
                    22,14,15,
                    23,16,24,
                    16,17,24,
                    24,17,25,
                    17,18,25,
                    25,18,26,
                    18,19,26,
                    26,19,20,
                    26,20,27,
                    27,20,21,
                    27,21,28,
                    28,21,22,
                    28,22,29,
                    23,24,30,
                    30,24,31,
                    31,24,25,
                    31,25,32,
                    32,25,26,
                    32,26,27,
                    33,32,27,
                    33,27,28,
                    33,28,34,
                    34,28,29,
                    35,30,31,
                    35,31,36,
                    36,31,32,
                    36,32,33,
                    36,33,37,
                    37,33,34,
                };

                // WTF? If we have 37 vertices the indices should be
                // in the range 0..36 inclusive.  Instead they're 1..37.
                // How did this ever work?
                // Ok, in the call to DrawPrimitives -1 was passed as the vertexOffset.  
                // Also, the number of vertices was passed as 36 instead of the actual 37.
                // The end result is that this more or less worked on nVidia and Intel chipsets
                // but totally failed to render on ATI chipsets.  DON"T DO THIS!
                for(int i=0; i<fabricIndices.Length; i++)
                {
                    --fabricIndices[i];
                }

                vBuff_FA = new VertexBuffer(device, typeof(Terrain.TerrainVertex_FA), fabricVerts.Length, BufferUsage.None);
                vBuff_FA.SetData(fabricVerts);

                iBuff_FA = new IndexBuffer(device, typeof(UInt16), fabricIndices.Length, BufferUsage.WriteOnly);
                iBuff_FA.SetData(fabricIndices);
            }
            #endregion
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            DeviceResetX.Release(ref vBuff);
        }   // end of UIGridMaterialElement UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public override void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class UIGridMaterialElement

}   // end of namespace Boku.UI2D
