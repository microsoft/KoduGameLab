
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;


using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Programming;
using Boku.SimWorld;
using Boku.Web;
using Boku.Fx;

using Boku.Audio;
using BokuShared;

namespace Boku
{
    public class MicrobitPatternEditor : INeedsDeviceReset
    {
        #region Members

        bool active = false;

        UIGridModularIntegerSliderElement brightnessSlider;
        UIGridModularFloatSliderElement durationSlider;
        UIGrid2DLEDArrayElement ledGrid;
        UIGrid2DButtonBarElement bar;

        Texture2D leftStickTexture;
        Texture2D rightStickTexture;
        Vector2 leftStickPosition;
        Vector2 rightStickPosition;
        Vector2 rightStickPositionBrightness;   // Where it should be when brightness slider is selected.
        Vector2 rightStickPositionDuration;     // Where it should be when duration slider is selected.
        float stickAlpha = 1;
        float _stickAlpha = 1;

        public Button cancelButton = null;
        public Button saveButton = null;
        public Button toggleLEDButton = null;

        PerspectiveUICamera camera;

        Texture2D thumbnail;
        bool touchedThisFrame = false;

        ReflexData reflex;
        Modifier modifier;
        int modifierIndex;  // Index indicating which pattern modifier this is in the current reflex.  0=first patter, 1=second etc.
                            // This is used to fit put the data into the list of values on the reflex.

        MicroBitPattern pattern;    // The pattern we're currently editing.

        // Colors for button labels
        Color labelColor = Color.White;

        #endregion

        #region Accessors

        public bool Active
        {
            get { return active; }
        }

        public bool WasTouchedThisFrame 
        { 
            get { return touchedThisFrame; } 
        }

        #endregion

        #region Public

        public MicrobitPatternEditor()
        {
            // Create a blob of common parameters.
            UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();
            blob.width = 512.0f / 96.0f;        // 5.33333
            blob.height = blob.width / 5.0f;    // 1.06667
            blob.edgeSize = 0.06f;
            blob.Font = UI2D.Shared.GetGameFont24Bold;
            blob.textColor = Color.White;
            blob.dropShadowColor = Color.Black;
            blob.useDropShadow = true;
            blob.invertDropShadow = false;
            blob.unselectedColor = new Color(new Vector3(4, 100, 90) / 255.0f);
            blob.selectedColor = new Color(new Vector3(5, 180, 160) / 255.0f);
            blob.normalMapName = @"Slant0Smoothed5NormalMap";
            blob.justify = UIGridModularCheckboxElement.Justification.Left;

            brightnessSlider = new UIGridModularIntegerSliderElement(blob, Strings.Localize("microbitPatternEditor.brightness"));
            brightnessSlider.Position = new Vector3(1.06667f, 1.01f, 0);
            brightnessSlider.MinValue = 0;
            brightnessSlider.MaxValue = 255;
            brightnessSlider.IncrementByAmount = 1;
            brightnessSlider.FastScrollScalar = 10;
            brightnessSlider.UseRightStick = true;
            brightnessSlider.OnChange = delegate(int brightness) { pattern.Brightness = brightness; };
            brightnessSlider.SetHelpOverlay = false;

            durationSlider = new UIGridModularFloatSliderElement(blob, Strings.Localize("microbitPatternEditor.duration"));
            durationSlider.Position = new Vector3(1.06667f, 0.0f, 0);
            durationSlider.MinValue = 0.0f;
            durationSlider.MaxValue = 5.0f;
            durationSlider.IncrementByAmount = 0.1f;
            durationSlider.CurrentValueImmediate = 0.1f;
            durationSlider.UseRightStick = true;
            durationSlider.OnChange = delegate(float duration) { pattern.Duration = duration; };
            durationSlider.SetHelpOverlay = false;

            blob.width = 2.06777f;
            blob.height = blob.width;
            ledGrid = new UIGrid2DLEDArrayElement(blob);
            ledGrid.Position = new Vector3(-2.58f, 0.51f, 0);
            ledGrid.SetHelpOverlay = false;

            blob.width = brightnessSlider.Width + ledGrid.Width - 0.04f;
            blob.height = brightnessSlider.Height * 0.75f;
            bar = new UIGrid2DButtonBarElement(blob);
            bar.Position = new Vector3(0.055f, -0.875f, 0);
            bar.SetHelpOverlay = false;

            camera = new PerspectiveUICamera();

            leftStickPosition = new Vector2(-3.9f, 0.5f);
            rightStickPositionBrightness = new Vector2(4.0f, 1.0f);
            rightStickPositionDuration = new Vector2(4.0f, 0.0f);
            rightStickPosition = rightStickPositionBrightness;

            // Buttons
            {
                GetTexture getTexture = delegate() { return ButtonTextures.BButton; };
                cancelButton = new Button(Strings.Localize("saveLevelDialog.cancel"), labelColor, getTexture, UI2D.Shared.GetGameFont20);
            }
            {
                GetTexture getTexture = delegate() { return ButtonTextures.AButton; };
                saveButton = new Button(Strings.Localize("saveLevelDialog.save"), labelColor, getTexture, UI2D.Shared.GetGameFont20);
            }
            {
                GetTexture getTexture = delegate() { return ButtonTextures.YButton; };
                toggleLEDButton = new Button(Strings.Localize("microbitPatternEditor.toggleLED"), labelColor, getTexture, UI2D.Shared.GetGameFont20);
            }

        }   // end of c'tor

        /// <summary>
        /// Activate the pattern editor.
        /// </summary>
        /// <param name="reflex">The current reflex containing the Pattern tile.</param>
        /// <param name="modifier">Modifier we're changing the pattern on.  If null this indicates we want the last one.</param>
        public void Activate(ReflexData reflex, Modifier modifier)
        {
            Debug.Assert(active == false, "Already active, why?");

            if (!active)
            {
                this.reflex = reflex;

                // If we passed in null, that indicates we want the last one.
                if (modifier == null)
                {
                    for (int i = reflex.Modifiers.Count - 1; i >= 0; i--)
                    {
                        if (reflex.Modifiers[i].upid == "modifier.microbit.pattern")
                        {
                            modifier = reflex.Modifiers[i];
                            break;
                        }
                    }
                    Debug.Assert(modifier != null);
                }

                this.modifier = modifier;

                modifierIndex = -1;
                for (int i = 0; i < reflex.Modifiers.Count; i++)
                {
                    if (reflex.Modifiers[i].upid == "modifier.microbit.pattern")
                    {
                        ++modifierIndex;
                    }

                    if (reflex.Modifiers[i] == modifier)
                    {
                        // Ensure blank settings exist.
                        if (reflex.microbitPatterns == null)
                        {
                            reflex.microbitPatterns = new List<MicroBitPattern>();
                            reflex.microbitPatterns.Add(new MicroBitPattern());
                        }
                        if (reflex.microbitPatterns.Count <= i)
                        {
                            reflex.microbitPatterns.Add(new MicroBitPattern());
                        }

                        break;
                    }
                }

                // Valid index?
                Debug.Assert(modifierIndex > -1);
                Debug.Assert(modifierIndex < reflex.microbitPatterns.Count);

                // Get the current pattern.
                pattern = (MicroBitPattern)reflex.microbitPatterns[modifierIndex].Clone();
                // Copy values to UI.
                ledGrid.LEDs = pattern.LEDs;
                ledGrid.FocusLEDIndex = 0;
                brightnessSlider.CurrentValue = pattern.Brightness;
                durationSlider.CurrentValue = pattern.Duration;

                brightnessSlider.Selected = true;
                durationSlider.Selected = false;
                ledGrid.Selected = true;
                bar.Selected = true;

                thumbnail = InGame.inGame.SmallThumbNail;
                HelpOverlay.Push(@"MicrobitPatternEditor");
                active = true;
            }

        }   // end of Activate()

        /// <summary>
        /// 
        /// </summary>
        /// <param name="saveChanges">Should the updated values be saved out to the reflex?</param>
        public void Deactivate(bool saveChanges)
        {
            if (active)
            {
                brightnessSlider.Selected = false;
                durationSlider.Selected = true;
                ledGrid.Selected = false;
                bar.Selected = false;

                rightStickPosition = rightStickPositionBrightness;

                // Copy changes to reflex?
                if (saveChanges)
                {
                    // Push the current pattern back to the reflex.
                    pattern.LEDs = ledGrid.LEDs;
                    reflex.microbitPatterns[modifierIndex] = (MicroBitPattern)pattern.Clone();

                    // TODO Copy the duration to all the other pattern modifiers?
                }

                HelpOverlay.Pop();
                active = false;
            }
        }   // end of Deactivate()

        public void Update()
        {
            touchedThisFrame = false;
            if (active)
            {
                InGame.SetViewportToScreen();
                
                camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                camera.Recalc();
                camera.Update();

                Matrix matrix = Matrix.Identity;
                brightnessSlider.Update(ref matrix);
                durationSlider.Update(ref matrix);
                ledGrid.Update(ref matrix);
                bar.Update(ref matrix);

                switch(GamePadInput.ActiveMode)
                {
                    case GamePadInput.InputMode.GamePad:
                        HandleGamePad();
                        break;
                    case GamePadInput.InputMode.Touch:
                        HandleTouch();
                        break;
                    case GamePadInput.InputMode.KeyboardMouse:
                        HandleMouse();
                        break;
                }

                // Adjust alpha value for stick icons if needed.
                float targetAlpha = GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad ? 1.0f : 0.0f;
                if (targetAlpha != _stickAlpha)
                {
                    _stickAlpha = targetAlpha;
                    TwitchManager.Set<float> set = delegate(float value, Object param) { stickAlpha = value; };
                    TwitchManager.CreateTwitch<float>(stickAlpha, _stickAlpha, set, 0.2f, TwitchCurve.Shape.EaseOut);
                }

                // touchedThisFrame = true;

            }

        }   // end of Update()

        private void HandleGamePad()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();

            // Accept changes.
            if (pad.ButtonA.WasPressed)
            {
                pad.ButtonA.ClearAllWasPressedState();
                Deactivate(saveChanges:true);
            }

            // Cancel changes.
            if (pad.ButtonB.WasPressed)
            {
                pad.ButtonB.ClearAllWasPressedState();
                Deactivate(saveChanges:false);
            }

            // Toggle LED.
            if (pad.ButtonY.WasPressed)
            {
                pad.ButtonY.ClearAllWasPressedState();
                ledGrid.LEDs[ledGrid.FocusLEDIndex] = !ledGrid.LEDs[ledGrid.FocusLEDIndex];
            }

            // LED Grid
            {
                int i = ledGrid.FocusLEDIndex % 5;
                int j = ledGrid.FocusLEDIndex / 5;
                if (pad.LeftStickLeft.WasPressedOrRepeat)
                {
                    if (i > 0)
                        --i;
                }
                if (pad.LeftStickRight.WasPressedOrRepeat)
                {
                    if (i < 4)
                        ++i;
                }
                if (pad.LeftStickUp.WasPressedOrRepeat)
                {
                    if (j > 0)
                        --j;
                }
                if (pad.LeftStickDown.WasPressedOrRepeat)
                {
                    if (j < 4)
                        ++j;
                }

                ledGrid.FocusLEDIndex = j * 5 + i;
            }

            // Sliders
            if (pad.RightStickDown.WasPressedOrRepeat)
            {
                if (brightnessSlider.Selected == true)
                {
                    brightnessSlider.Selected = false;
                    durationSlider.Selected = true;

                    {
                        TwitchManager.Set<Vector2> set = delegate(Vector2 value, Object param) { rightStickPosition = value; };
                        TwitchManager.CreateTwitch<Vector2>(rightStickPosition, rightStickPositionDuration, set, 0.2f, TwitchCurve.Shape.EaseOut);
                    }
                }
            }
            if (pad.RightStickUp.WasPressedOrRepeat)
            {
                if (brightnessSlider.Selected == false)
                {
                    brightnessSlider.Selected = true;
                    durationSlider.Selected = false;

                    {
                        TwitchManager.Set<Vector2> set = delegate(Vector2 value, Object param) { rightStickPosition = value; };
                        TwitchManager.CreateTwitch<Vector2>(rightStickPosition, rightStickPositionBrightness, set, 0.2f, TwitchCurve.Shape.EaseOut);
                    }
                }
            }

        }   // end of HandleGamePad()

        private void HandleTouch()
        {
            // Touch input
            TouchContact touch = TouchInput.GetOldestTouch();

            if (touch != null)
            {
                //Vector2 hit = TouchInput.GetAspectRatioAdjustedPosition(touch.position, shared.camera, true);
                Vector2 hit = touch.position;

                // Cancel
                if (cancelButton.Box.Touched(touch, hit))
                {
                    Deactivate(saveChanges:false);
                    return;
                }

                // Save
                if (saveButton.Box.Touched(touch, hit))
                {
                    Deactivate(saveChanges: true);
                    return;
                }

                // Toggle LED
                if (toggleLEDButton.Box.Touched(touch, hit))
                {
                    ledGrid.LEDs[ledGrid.FocusLEDIndex] = !ledGrid.LEDs[ledGrid.FocusLEDIndex];
                    return;
                }

                // Get hit point in camera coords.
                Vector2 cameraHit = -camera.PixelsToCameraSpaceScreenCoords(hit);

                // Handle clicks on ledGrid.  Hit boxes are in camera coords.
                if(touch.phase == TouchPhase.Began)
                {
                    for (int i = 0; i < ledGrid.ledHitBoxes.Length; i++)
                    {
                        if (ledGrid.ledHitBoxes[i].Contains(cameraHit))
                        {
                            ledGrid.FocusLEDIndex = i;
                            // Also toggle when clicked.
                            ledGrid.LEDs[ledGrid.FocusLEDIndex] = !ledGrid.LEDs[ledGrid.FocusLEDIndex];
                        }
                    }
                }

                // Handle touch on sliders.
                {
                    // Brightness Slider
                    Vector2 position = new Vector2(brightnessSlider.Position.X, brightnessSlider.Position.Y);
                    Vector2 min = position - brightnessSlider.Size / 2.0f;
                    Vector2 max = position + brightnessSlider.Size / 2.0f;
                    AABB2D box = new AABB2D(min, max);
                    if (box.Contains(cameraHit))
                    {
                        brightnessSlider.Selected = true;
                        durationSlider.Selected = false;

                        Vector2 uv = (cameraHit - min) / (max - min);
                        uv.Y = 1.0f - uv.Y;
                        brightnessSlider.HandleTouchInput(touch, uv);
                    }

                    // Duration Slider
                    position = new Vector2(durationSlider.Position.X, durationSlider.Position.Y);
                    min = position - durationSlider.Size / 2.0f;
                    max = position + durationSlider.Size / 2.0f;
                    box = new AABB2D(min, max);
                    if (box.Contains(cameraHit))
                    {
                        durationSlider.Selected = true;
                        brightnessSlider.Selected = false;

                        Vector2 uv = (cameraHit - min) / (max - min);
                        uv.Y = 1.0f - uv.Y;
                        durationSlider.HandleTouchInput(touch, uv);
                    }
                }

            }   // end if we have a touch.
        }   // end of HandleTouch()

        private void HandleMouse()
        {
            // Mouse input
            Vector2 hit = MouseInput.PositionVec;

            // Cancel
            if (cancelButton.Box.LeftPressed(hit))
            {
                Deactivate(saveChanges: false);
                return;
            }

            // Save
            if (saveButton.Box.LeftPressed(hit))
            {
                Deactivate(saveChanges: true);
                return;
            }

            // Toggle LED
            if (toggleLEDButton.Box.LeftPressed(hit))
            {
                ledGrid.LEDs[ledGrid.FocusLEDIndex] = !ledGrid.LEDs[ledGrid.FocusLEDIndex];
                return;
            }

            // Change button label colors based on mouse hover.
            cancelButton.SetHoverState(hit);
            saveButton.SetHoverState(hit);
            toggleLEDButton.SetHoverState(hit);

            // Get hit point in camera coords.
            Vector2 cameraHit = -camera.PixelsToCameraSpaceScreenCoords(hit);

            // Handle clicks on ledGrid.  Hit boxes are in camera coords.
            if (MouseInput.Left.WasPressed)
            {
                for (int i = 0; i < ledGrid.ledHitBoxes.Length; i++)
                {
                    if (ledGrid.ledHitBoxes[i].Contains(cameraHit))
                    {
                        ledGrid.FocusLEDIndex = i;
                        // Also toggle when clicked.
                        ledGrid.LEDs[ledGrid.FocusLEDIndex] = !ledGrid.LEDs[ledGrid.FocusLEDIndex];
                    }
                }
            }

            // Handle clicks on sliders.
            if (MouseInput.Left.IsPressed)
            {
                // Brightness Slider
                Vector2 position = new Vector2(brightnessSlider.Position.X, brightnessSlider.Position.Y);
                Vector2 min = position - brightnessSlider.Size / 2.0f;
                Vector2 max = position + brightnessSlider.Size / 2.0f;
                AABB2D box = new AABB2D(min, max);
                if (box.Contains(cameraHit))
                {
                    brightnessSlider.Selected = true;
                    durationSlider.Selected = false;

                    Vector2 uv = (cameraHit - min) / (max - min);
                    uv.Y = 1.0f - uv.Y;
                    brightnessSlider.HandleMouseInput(uv);
                }

                // Duration Slider
                position = new Vector2(durationSlider.Position.X, durationSlider.Position.Y);
                min = position - durationSlider.Size / 2.0f;
                max = position + durationSlider.Size / 2.0f;
                box = new AABB2D(min, max);
                if (box.Contains(cameraHit))
                {
                    durationSlider.Selected = true;
                    brightnessSlider.Selected = false;

                    Vector2 uv = (cameraHit - min) / (max - min);
                    uv.Y = 1.0f - uv.Y;
                    durationSlider.HandleMouseInput(uv);
                }
            }

        }   // end of HandleKeyboardMouse()

        public void Render()
        {
            if (active)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                Vector2 screenSize = BokuGame.ScreenSize;

                InGame.SetViewportToScreen();

                // Render dialog using local camera.
                Fx.ShaderGlobals.SetCamera(camera);

                device.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, new Color(20, 20, 20), 1.0f, 0);

                // Start by using the blurred version of the scene as a backdrop.
                // If no thumbnail use the black we just cleared to.
                if (thumbnail != null && !thumbnail.IsDisposed && !thumbnail.GraphicsDevice.IsDisposed)
                {
                    batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                    {
                        Rectangle rect = new Rectangle(0, 0, (int)screenSize.X, (int)screenSize.Y);
                        batch.Draw(thumbnail, rect, Color.White);
                    }
                    batch.End();
                }

                brightnessSlider.Render(camera);
                durationSlider.Render(camera);
                ledGrid.Render(camera);
                bar.Render(camera);

                // If in GamePad input mode, render left and right stick icons.
                // Actual rendering is done based on alpha.
                if (stickAlpha > 0)
                {
                    CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
                    Vector2 size = new Vector2(0.6f, 1.0f);

                    csquad.Render(camera, leftStickTexture, stickAlpha, leftStickPosition, size, "TexturedRegularAlpha");
                    csquad.Render(camera, rightStickTexture, stickAlpha, rightStickPosition, size, "TexturedRegularAlpha");
                }

                // Buttons
                {
                    float margin = 32;
                    // Start with lower bar.
                    Vector3 screen = new Vector3(bar.Position.X, bar.Position.Y, 0);
                    // Convert to pixels.
                    Vector2 barPos = camera.WorldToScreenCoordsVector2(screen);
                    // Find right edge.
                    Vector2 pos = barPos;
                    pos.X += bar.Width / 2.0f * camera.DPI;
                    float barHeight = bar.Height * camera.DPI;
                    // Center on bar.
                    Vector2 size = cancelButton.GetSize();
                    pos.Y -= size.Y / 2.0f;

                    pos.X -= size.X + 2 * margin;
                    
                    cancelButton.Render(pos, useBatch: false);
                    size = saveButton.GetSize();
                    pos.X -= size.X + margin;
                    saveButton.Render(pos, useBatch: false);

                    // Find left edge for Toggle button.
                    pos.X = barPos.X - bar.Width / 2.0f * camera.DPI + margin;
                    toggleLEDButton.Render(pos, useBatch: false);
                }
            }   // end if active

        }   // end of Render()

        #endregion

        #region Internal

        public void LoadContent(bool immediate)
        {
            brightnessSlider.LoadContent(immediate);
            durationSlider.LoadContent(immediate);
            ledGrid.LoadContent(immediate);
            bar.LoadContent(immediate);

            if (leftStickTexture == null)
            {
                leftStickTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\LeftStick");
            }

            if (rightStickTexture == null)
            {
                rightStickTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpCard\RightStick");
            }


        }   // end of LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            brightnessSlider.InitDeviceResources(device);
            durationSlider.InitDeviceResources(device);
            ledGrid.InitDeviceResources(device);
            bar.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            brightnessSlider.UnloadContent();
            durationSlider.UnloadContent();
            ledGrid.UnloadContent();
            bar.UnloadContent();

            BokuGame.Release(ref leftStickTexture);
            BokuGame.Release(ref rightStickTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // end of class MicrobitPatternEditor
}   // end of namespace Boku
