// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Text;
using KoiX.UI.Dialogs;

using Boku;
using Boku.Common;
using Boku.SimWorld.Terra;


namespace KoiX.UI
{
    /// <summary>
    /// Custome button for displaying water samples.
    /// </summary>
    public class TerrainMaterialButton : BaseWidget
    {
        #region Members

        static VertexBuffer vBuff = null;
        static VertexBuffer vBuff_FA = null;
        static IndexBuffer iBuff_FA = null;

        static Texture2D reticule;

        int materialIndex;
        string labelText;

        UIState prevCombinedState = UIState.Inactive;

        List<TerrainMaterialButton> siblings;   // List of all radio buttons in this set.  Used for clearing
                                                // selection of others when this one is set.

        Vector2 displaySize;

        float rotation;
        Matrix world = Matrix.Identity;

        RadioButtonTheme curTheme;          // Colors and sizes for current state.

        Twitchable<Color> bodyColor;
        Twitchable<Color> outlineColor;
        Twitchable<float> outlineWidth;
        Twitchable<float> cornerRadius;

        #endregion

        #region Accessors

        public new bool Selected
        {
            get { return base.Selected; }
            set
            {
                if (base.Selected != value)
                {
                    base.Selected = value;

                    // If we're setting this TerrainMaterialButton "on" then
                    // all its siblings need to be "off".
                    if (value == true)
                    {
                        foreach (TerrainMaterialButton rb in siblings)
                        {
                            if (rb != this)
                            {
                                rb.Selected = false;
                            }
                        }
                    }

                    // TODO (****) Should we only call OnChange when selected or
                    // should be call any time the state changes?
                    if (Selected)
                    {
                        SetFocus(overrideInactive: true);
                        OnChange();
                    }
                }   // end if value changes.

            }
        }

        public Color BodyColor
        {
            get { return bodyColor.Value; }
            set { bodyColor.Value = value; }
        }

        public Color _BodyColor
        {
            get { return bodyColor.TargetValue; }
            set { bodyColor.TargetValue = value; }
        }

        public Color OutlineColor
        {
            get { return outlineColor.Value; }
            set { outlineColor.Value = value; }
        }

        public Color _OutlineColor
        {
            get { return outlineColor.TargetValue; }
            set { outlineColor.TargetValue = value; }
        }

        public float OutlineWidth
        {
            get { return outlineWidth.Value; }
            set { outlineWidth.Value = value; }
        }

        public float _OutlineWidth
        {
            get { return outlineWidth.TargetValue; }
            set { outlineWidth.TargetValue = value; }
        }

        public float CornerRadius
        {
            get { return cornerRadius.Value; }
            set { cornerRadius.Value = value; }
        }

        public float _CornerRadius
        {
            get { return cornerRadius.TargetValue; }
            set { cornerRadius.TargetValue = value; }
        }

        /// <summary>
        /// Optional label string.  This will generally just be a number.
        /// </summary>
        public string LabelText
        {
            get { return labelText; }
            set { labelText = value; }
        }

        public int MaterialIndex
        {
            get { return materialIndex; }
        }

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        #endregion

        #region Public

        public TerrainMaterialButton(BaseDialog parentDialog, int materialIndex, List<TerrainMaterialButton> siblings, Vector2 displaySize, string labelText = null, Callback OnChange = null, ThemeSet theme = null, string id = null, object data = null)
            : base(parentDialog, OnChange: OnChange, theme: theme, id: id, data: data)
        {
            this.materialIndex = materialIndex;
            this.siblings = siblings;
            this.labelText = labelText;
            this.displaySize = displaySize;

            // Add self.
            siblings.Add(this);

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet;
            }

            // Default to Inactive state.
            prevCombinedState = UIState.Inactive;
            curTheme = theme.RadioButtonNormal.Clone() as RadioButtonTheme;

            // Create all the Twitchables and set initial values.
            bodyColor = new Twitchable<Color>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.BodyColor);
            outlineColor = new Twitchable<Color>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.OutlineColor);
            outlineWidth = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: curTheme.OutlineWidth);
            cornerRadius = new Twitchable<float>(Theme.TwitchTime, TwitchCurve.Shape.EaseInOut, parent: this, startingValue: 4.0f);

            localRect.Size = displaySize;
            Margin = new Padding(32, 32, 32, 32);

            rotation = MathHelper.Pi * (float)KoiLibrary.Random.NextDouble();
            rotation = 0;

        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            // Needed to handle focus changes.
            base.Update(camera, parentPosition);

            if (Hover)
            {
                SetFocus();
            }

            UIState combinedState = CombinedState;
            if (combinedState != prevCombinedState)
            {
                // Set new state params.  Note that dirty flag gets
                // set internally by setting individual values so
                // we don't need to worry about it here.
                switch (combinedState)
                {
                    case UIState.Disabled:
                    case UIState.DisabledSelected:
                        curTheme = theme.RadioButtonDisabled;
                        break;

                    case UIState.Active:
                    case UIState.ActiveHover:
                        curTheme = theme.RadioButtonNormal;
                        break;

                    case UIState.ActiveFocused:
                    case UIState.ActiveFocusedHover:
                        curTheme = theme.RadioButtonNormalFocused;
                        break;

                    case UIState.ActiveSelected:
                    case UIState.ActiveSelectedHover:
                        curTheme = theme.RadioButtonSelected;
                        break;

                    // For TerrainMaterialButton selected == checked.
                    case UIState.ActiveSelectedFocused:
                    case UIState.ActiveSelectedFocusedHover:
                        curTheme = theme.RadioButtonSelectedFocused;
                        break;

                    default:
                        // Should only happen on state.None
                        break;

                }   // end of switch

                prevCombinedState = combinedState;

            }   // end if state changed.

        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            Terrain terrain = BokuGame.bokuGame.inGame.Terrain;

            this.parentPosition = parentPosition;

            Vector2 pos = Position + parentPosition;
            Vector2 center = pos + new Vector2(displaySize.X / 2.0f, displaySize.Y - curTheme.CornerRadius);

            // Render material sample.
            {
                // We need to push the near plane out more than is normal for UI 
                // so that the material cubes don't render behind the terrain.
                Camera cam = new SmoothCamera();
                cam.From = new Vector3(0, -100, 0);
                cam.NearClip = 30.0f;
                cam.FarClip = 10000.0f;

                float scale = 60.0f;
                Matrix world = Matrix.CreateRotationZ(rotation) * Matrix.CreateRotationX(0.7f) * Matrix.CreateScale(scale);

                // Shift the cubes to match up wth the buttons.
                pos = 1.1f * (LocalRect.Center + parentPosition) * camera.Zoom;
                Vector3 trans = (1.0f / camera.Zoom) * camera.ScreenSize.Y * cam.ScreenToWorldCoords(pos + camera.ScreenSize / 2.0f);
                trans += new Vector3(0, -32, 0);
                world.Translation = trans;


                Effect effect = terrain.EffectColor;
                EffectTechnique technique = TerrainMaterial.Get(materialIndex).TechniqueColor(TerrainMaterial.EffectTechs.TerrainColorPass);

                effect.CurrentTechnique = technique;

                // Compensate for verts being [0..1] instead of [-0.5..0.5] as we'd prefer.
                // They must be in [0..1] because UV mapping is implicit in local position.
                Matrix preTrans = Matrix.Identity;
                preTrans.Translation = TerrainMaterialDialog.FabricMode ? new Vector3(-0.75f, -0.75f, -0.5f) : new Vector3(-0.5f, -0.5f, -0.5f);
                world = preTrans * world;

                Matrix worldViewProjMatrix = world * cam.ViewProjectionMatrix;

                terrain.ParameterColor(Terrain.EffectParams.WorldMatrix).SetValue(world);
                terrain.ParameterColor(Terrain.EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);
                terrain.ParameterColor(Terrain.EffectParams.WarpCenter).SetValue(Vector4.Zero);
                terrain.ParameterEdit(Terrain.EffectParams.WorldMatrix).SetValue(world);
                terrain.ParameterEdit(Terrain.EffectParams.WorldViewProjMatrix).SetValue(worldViewProjMatrix);
                terrain.ParameterEdit(Terrain.EffectParams.WarpCenter).SetValue(Vector4.Zero);

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

                    effect.CurrentTechnique = mat.TechniqueColor(TerrainMaterial.EffectTechs_FA.TerrainColorPass_FA);

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

                    effect.CurrentTechnique = mat.TechniqueColor(TerrainMaterial.EffectTechs.TerrainColorPass);

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
                }   // end else of if fabric.  (ie end of cubic section)
            }   // end of material sample rendering.

            // Disc should be centered at bottom.  This is the radio button itself.
            Disc.Render(camera, center, curTheme.CornerRadius, curTheme.BodyColor,
                    outlineWidth: curTheme.OutlineWidth, outlineColor: curTheme.OutlineColor,
                    bevelStyle: BevelStyle.Round, bevelWidth: curTheme.CornerRadius);

            // Label.
            {
                GetSpriteFont Font = SharedX.GetSegoeUI20;
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                InGame.RestoreViewportToFull();

                batch.Begin();
                {
                    string str = (materialIndex + 1).ToString();
                    Vector2 offset = -Font().MeasureString(str) * new Vector2(0.5f, 0.5f);
                    Vector2 position = camera.Zoom * (LocalRect.Center + parentPosition) + offset + camera.ScreenSize / 2.0f;
                    batch.DrawString(Font(), str, position + new Vector2(1, 1), Color.Black);
                    batch.DrawString(Font(), str, position + new Vector2(1, -1), Color.Black);
                    batch.DrawString(Font(), str, position + new Vector2(-1, 1), Color.Black);
                    batch.DrawString(Font(), str, position + new Vector2(-1, -1), Color.Black);
                    batch.DrawString(Font(), str, position, Color.White);
                }
                batch.End();
            }

            // Needed for debug rendering.
            base.Render(camera, parentPosition);

        }   // end of Render()

        /// <summary>
        /// Render the focus reticule (if this button is in focus).
        /// We want to call this _after_ all the material samples
        /// have been rendered to ensure that the reticule stays
        /// on top of everything.
        /// </summary>
        public void RenderReticule(SpriteCamera camera)
        {
            if (InFocus)
            {
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                Rectangle rect = LocalRect.ToRectangle();
                rect.X += (int)parentPosition.X;
                rect.Y += (int)parentPosition.Y;
                rect.Inflate(32, 32);
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.ViewMatrix);
                {
                    batch.Draw(reticule, rect, Color.White);
                }
                batch.End();
            }
        }   // end of RenderReticule()

        public override void RegisterForInputEvents()
        {
            // Register to get left down mouse event.  
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);

            // Also register for Keyboard.  If this button has focus and enter is pressed that's the same as a mouse click.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);

            // Tap also toggles checked.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);

            // If we have focus, gamepad A should toggle state.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);

        }   // end of RegisterForInputEvents()

        #endregion

        #region InputEventHandler

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                if (KoiLibrary.InputEventManager.MouseHitObject == this)
                {
                    // Claim mouse focus as ours.
                    KoiLibrary.InputEventManager.MouseFocusObject = this;

                    // Register to get left up events.
                    KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                    return true;
                }
            }

            return false;
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                // Release mouse focus.
                if (KoiLibrary.InputEventManager.MouseFocusObject == this)
                {
                    KoiLibrary.InputEventManager.MouseFocusObject = null;
                }

                // Stop getting move and up events.
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseMove);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                // If mouse up happens over box, fine.  If not, ignore.
                if (KoiLibrary.InputEventManager.MouseHitObject == this)
                {
                    // Set to "on".
                    Selected = true;
                }

                return true;
            }
            return false;
        }   // end of ProcessMouseLeftUpEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active);

            if (InFocus && input.Key == Microsoft.Xna.Framework.Input.Keys.Enter && !input.Modifier)
            {
                // If inFocus, toggle state.
                if (InFocus)
                {
                    // Set to "on".
                    Selected = true;

                    return true;
                }
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            // Did this gesture hit us?
            if (gesture.HitObject == this)
            {
                // Set to "on".
                Selected = true;

                return true;
            }

            return base.ProcessTouchTapEvent(gesture);
        }   // end of ProcessTouchTapEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (InFocus)
            {
                if (pad.ButtonA.WasPressed && InFocus)
                {
                    // Set to "on".
                    Selected = true;

                    return true;
                }
            }

            return base.ProcessGamePadEvent(pad);
        }
        #endregion

        #region Internal

        public override void LoadContent()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

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
                for (int i = 0; i < fabricIndices.Length; i++)
                {
                    --fabricIndices[i];
                }

                vBuff_FA = new VertexBuffer(device, typeof(Terrain.TerrainVertex_FA), fabricVerts.Length, BufferUsage.None);
                vBuff_FA.SetData(fabricVerts);

                iBuff_FA = new IndexBuffer(device, typeof(UInt16), fabricIndices.Length, BufferUsage.WriteOnly);
                iBuff_FA.SetData(fabricIndices);
            }
            #endregion

            if (DeviceResetX.NeedsLoad(reticule))
            {
                reticule = KoiLibrary.LoadTexture2D(@"Textures\UI2D\Tools\SelectionReticule2");
            }

            base.LoadContent();
        }   // end of LoadContent()

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref vBuff);

            DeviceResetX.Release(ref reticule);

            base.UnloadContent();
        }   // end of UnloadContent()

        #endregion

    }   // end of class TerrainMaterialButton

}   // end of namespace KoiX.UI
