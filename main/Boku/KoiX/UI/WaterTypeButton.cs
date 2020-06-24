
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

using Boku;
using Boku.Common;
using Boku.SimWorld.Terra;


namespace KoiX.UI
{
    /// <summary>
    /// Custome button for displaying water samples.
    /// </summary>
    public class WaterTypeButton : BaseWidget
    {
        #region Members

        static VertexBuffer vbuf;
        static IndexBuffer ibuf;
        static int numVerts = 4;
        static int numTris = 2;

        static Vector3[] faceDirs = {
            Vector3.UnitZ,
            -Vector3.UnitY,
            Vector3.UnitY,
            -Vector3.UnitX,
            Vector3.UnitX };

        static Texture2D reticule;

        int waterType;
        string labelText;

        UIState prevCombinedState = UIState.Inactive;

        List<WaterTypeButton> siblings;     // List of all radio buttons in this set.  Used for clearing
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

                    // If we're setting this WaterTypeButton "on" then
                    // all its siblings need to be "off".
                    if (value == true)
                    {
                        foreach (WaterTypeButton rb in siblings)
                        {
                            if (rb != this)
                            {
                                rb.Selected = false;
                            }
                        }
                    }

                    // TODO (scoy) Should we only call OnChange when selected or
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

        public int WaterType
        {
            get { return waterType; }
        }

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        #endregion

        #region Public

        public WaterTypeButton(BaseDialog parentDialog, int waterType, List<WaterTypeButton> siblings, Vector2 displaySize, string labelText = null, Callback OnChange = null, ThemeSet theme = null, string id = null, object data = null)
            : base(parentDialog, OnChange: OnChange, theme: theme, id: id, data: data)
        {
            this.waterType = waterType;
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

                    // For WaterTypeButton selected == checked.
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

            Vector2 pos = Position + parentPosition;
            Vector2 center = pos + new Vector2(displaySize.X / 2.0f, displaySize.Y - curTheme.CornerRadius);

            if (InFocus)
            {
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                Rectangle rect = LocalRect.ToRectangle();
                rect.X += (int)parentPosition.X;
                rect.Y += (int)parentPosition.Y;
                rect.Inflate(8, 8);
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.ViewMatrix);
                {
                    batch.Draw(reticule, rect, Color.White);
                }
                batch.End();
            }

            // Render material sample.
            {
                // We need to push the near plane out more than is normal for UI 
                // so that the material cubes don't render behind the terrain.
                Camera cam = new SmoothCamera();
                cam.From = new Vector3(0, -100, 0);
                cam.NearClip = 30.0f;
                cam.FarClip = 10000.0f;

                float scale = 140.0f;
                world = Matrix.CreateRotationZ(rotation) * Matrix.CreateRotationX(0.7f) * Matrix.CreateScale(scale);

                // Shift the cubes to match up wth the buttons.
                pos = 1.1f * (LocalRect.Center + parentPosition) * camera.Zoom;
                Vector3 trans = (1.0f / camera.Zoom) * camera.ScreenSize.Y * cam.ScreenToWorldCoords(pos + camera.ScreenSize / 2.0f);
                world.Translation = trans;

                Matrix worldToCamera = world * cam.ViewMatrix;

                device.SetVertexBuffer(vbuf);

                device.Indices = ibuf;

                // Render back facing first.
                for (int face = 1; face < Tile.NumFaces; ++face)
                {
                    Vector3 camFaceDir = Vector3.TransformNormal(faceDirs[face], worldToCamera);
                    if (camFaceDir.Z < 0)
                    {
                        terrain.PreRenderWaterCube(device, 0.5f, waterType, face, world, cam);
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVerts, 0, numTris);
                        terrain.PostRenderWaterCube();
                    }
                }

                // Then front facing.
                for (int face = 1; face < Tile.NumFaces; ++face)
                {
                    Vector3 camFaceDir = Vector3.TransformNormal(faceDirs[face], worldToCamera);
                    if (camFaceDir.Z >= 0)
                    {
                        terrain.PreRenderWaterCube(device, 0.5f, waterType, face, world, cam);
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVerts, 0, numTris);
                        terrain.PostRenderWaterCube();
                    }
                }

                // Always do the top face last.
                terrain.PreRenderWaterCube(device, 0.5f, waterType, (int)Tile.Face.Top, world, cam);
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVerts, 0, numTris);
                terrain.PostRenderWaterCube();
            }

            // Disc should be centered at bottom.  This is the radio button itself.
            Disc.Render(camera, center, curTheme.CornerRadius, curTheme.BodyColor,
                    outlineWidth: curTheme.OutlineWidth, outlineColor: curTheme.OutlineColor,
                    bevelStyle: BevelStyle.Round, bevelWidth: curTheme.CornerRadius);

            // Label.
            {
                GetSpriteFont Font = SharedX.GetSegoeUI24;
                SpriteBatch batch = KoiLibrary.SpriteBatch;
                InGame.RestoreViewportToFull();

                batch.Begin();
                {
                    string str = (waterType + 1).ToString();
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

            if (DeviceResetX.NeedsLoad(reticule))
            {
                reticule = KoiLibrary.LoadTexture2D(@"Textures\UI2D\Tools\SelectionReticule2");
            }

            base.LoadContent();
        }   // end of LoadContent()

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref vbuf);
            DeviceResetX.Release(ref ibuf);

            DeviceResetX.Release(ref reticule);

            base.UnloadContent();
        }   // end of UnloadContent()

        #endregion

    }   // end of class WaterTypeButton

}   // end of namespace KoiX.UI
