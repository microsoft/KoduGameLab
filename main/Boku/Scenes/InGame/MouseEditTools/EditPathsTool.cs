// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

using Boku;
using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Common.Gesture;
using Boku.Fx;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;

namespace Boku.Scenes.InGame.MouseEditTools
{
    public class EditPathsTool : BaseMouseEditTool, INeedsDeviceReset
    {
        #region Members
        
        private static EditPathsTool instance = null;

        private Boku.InGame.WayPointEdit.MouseOver mouseOver = Boku.InGame.WayPointEdit.mouseOver;
        private Boku.InGame.WayPointEdit.TouchOver touchOver = Boku.InGame.WayPointEdit.touchOver;

        // Right-click menus.
        private MouseMenu groundMenu = new MouseMenu();
        private MouseMenu nodeMenu = new MouseMenu();
        private MouseMenu edgeMenu = new MouseMenu();

        // Position of mouse on terrain when menu activated.
        private Vector3 menuPosition;

        private int focusColorIndex = 2;                    // Color index of focus path's color in ColorPalette.

        private Texture2D closeSquareTexture = null;        // Current texture we're using.
        private Texture2D closeSquareLitTexture = null;     // Selected version.
        private Texture2D closeSquareUnlitTexture = null;   // Unselected version.
        private Vector2 closePosition;
        private Vector2 closeSize;

        private UIGridModularFloatSliderElement slider = null;
        private bool sliderActive = false;
        private float sliderPrevValue = 0.0f;
        private UIGridElement.ParamBlob blob = new UIGridElement.ParamBlob();

        #endregion Members

        #region Accessors

        public MouseMenu GroundMenu
        {
            get { return groundMenu; }
        }

        public MouseMenu NodeMenu
        {
            get { return nodeMenu; }
        }

        public MouseMenu EdgeMenu
        {
            get { return edgeMenu; }
        }

        /// <summary>
        /// Are any of the menus active?
        /// </summary>
        public bool MenusActive
        {
            get { return GroundMenu.Active || NodeMenu.Active || EdgeMenu.Active; }
        }

        /// <summary>
        /// Are one of the sliders for tweaking paths active?
        /// </summary>
        public bool SliderActive
        {
            get { return sliderActive; }
        }

        public bool ActOnPath
        {
            get { return touchOver.ActOnPath; }
            set { touchOver.ActOnPath = value; }
        }

        #endregion

        #region Public

        // c'tor
        public EditPathsTool()
        {
            HelpOverlayID = @"EditPaths";

            // We don't want to see any brush rendered for this tool.
            prevBrushIndex = -1;

            // Get references.
            inGame = Boku.InGame.inGame;
            shared = inGame.shared;

            SetUpMenus();

            // Set up blob for slider.
            blob.width = 512.0f / 96.0f;
            blob.height = blob.width / 5.0f;
            blob.edgeSize = 0.06f;
            blob.Font = UI2D.Shared.GetGameFont24Bold;
            blob.textColor = new Color(20, 20, 20);
            blob.normalMapName = @"Slant0Smoothed5NormalMap";
            blob.justify = UIGridElement.Justification.Center;

            slider = new UIGridModularFloatSliderElement(blob, "To Be Replaced");
            slider.OnChange = SliderOnChange;
            Matrix mat = Matrix.CreateTranslation(new Vector3(0.0f, -4.0f, 0.0f));
            slider.WorldMatrix = mat;

        }   // end of c'tor

        /// <summary>
        /// Callback called by slider whenever current value changes.
        /// </summary>
        /// <param name="value"></param>
        public void SliderOnChange(float value)
        {
            if (sliderActive)
            {
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    // Figure out which value to update.
                    if (slider.Label == Strings.Localize("mouseEdit.heightValue"))
                    {
                        if (touchOver.ActOnPath)
                        {
                            // Adjust height of whole path.
                            Vector3 delta = new Vector3(0, 0, slider.CurrentValue - sliderPrevValue);
                            touchOver.Path.Translate(delta);
                        }
                        else if (touchOver.node != null)
                        {
                            // Adjust height of node.
                            touchOver.node.Height = slider.CurrentValue;
                        }
                        else if (touchOver.edge != null)
                        {
                            // Adjust height of edge.
                            Vector3 delta = new Vector3(0, 0, slider.CurrentValue - sliderPrevValue);
                            touchOver.edge.Translate(delta);
                        }
                    }
                    else if (slider.Label == Strings.Localize("mouseEdit.rotationValue"))
                    {
                        if (touchOver.ActOnPath)
                        {
                            // Adjust rotation of whole path.
                            // Orbit around selected node.
                            float delta = slider.CurrentValue - sliderPrevValue;
                            Matrix mat = Matrix.CreateTranslation(-touchOver.node.Position)
                                        * Matrix.CreateRotationZ(MathHelper.ToRadians(delta))
                                        * Matrix.CreateTranslation(touchOver.node.Position);
                            touchOver.Path.Rotate(mat);
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "Should never get here.");
                    }
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    // Figure out which value to update.
                    if (slider.Label == Strings.Localize("mouseEdit.heightValue"))
                    {
                        if (mouseOver.ActOnPath)
                        {
                            // Adjust height of whole path.
                            Vector3 delta = new Vector3(0, 0, slider.CurrentValue - sliderPrevValue);
                            mouseOver.Path.Translate(delta);
                        }
                        else if (mouseOver.node != null)
                        {
                            // Adjust height of node.
                            mouseOver.node.Height = slider.CurrentValue;
                        }
                        else if (mouseOver.edge != null)
                        {
                            // Adjust height of edge.
                            Vector3 delta = new Vector3(0, 0, slider.CurrentValue - sliderPrevValue);
                            mouseOver.edge.Translate(delta);
                        }
                    }
                    else if (slider.Label == Strings.Localize("mouseEdit.rotationValue"))
                    {
                        if (mouseOver.ActOnPath)
                        {
                            // Adjust rotation of whole path.
                            // Orbit around selected node.
                            float delta = slider.CurrentValue - sliderPrevValue;
                            Matrix mat = Matrix.CreateTranslation(-mouseOver.node.Position)
                                        * Matrix.CreateRotationZ(MathHelper.ToRadians(delta))
                                        * Matrix.CreateTranslation(mouseOver.node.Position);
                            mouseOver.Path.Rotate(mat);
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "Should never get here.");
                    }
                }

                sliderPrevValue = slider.CurrentValue;
                Boku.InGame.IsLevelDirty = true;
            }
        }   // end of SliderOnChange()

        public static BaseMouseEditTool GetInstance()
        {
            if (instance == null)
            {
                instance = new EditPathsTool();
            }
            return instance;
        }   // end of GetInstance()

        public override void Update(Camera uicamera)
        {
            if (Active)
            {
                // We need to update our child objects first so they have first shot at grabbing any input.
                inGame.shared.addItemHelpCard.Update();

                if (sliderActive)
                {                    
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                        UpdateTouchSliders(uicamera);
                    else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                        UpdateMouseSliders(uicamera);                    
                }

                SelectOverlay();

                // If the slider is still active, we're done.  
                // We don't want any other input acted upon.
                if (sliderActive)
                {
                    return;
                }



                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    UpdateTouch();
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    UpdateKeyboardMouse();

                }
            }   // end if active.

            // This tool is odd enough compared to the standard 
            // tools that we don't want to call the base update.
            //base.Update();

        }   // end of Update()

        private void UpdateTouch()
        {
            Camera camera = Boku.InGame.inGame.shared.camera;
            TouchEdit touchEdit = Boku.InGame.inGame.TouchEdit;

            TouchEdit.TouchHitInfo hitInfo = TouchEdit.HitInfo;

            // Don't update touchOver if the menus are active.  This way the focus state will be preserved.
            if (!MenusActive && !SliderActive)
            {
                //handle color palette interactions
                if (touchOver.Over && touchOver.Path != null)
                {
                    //set target color
                    focusColorIndex = ColorPalette.GetIndexFromColor(touchOver.Path.Color);
                    Boku.InGame.ColorPalette.Active = true;

                    //check for touch on a new color
                    Classification.Colors touchColor = Boku.InGame.ColorPalette.GetColorFromTouch();
                    if ((touchColor != Classification.Colors.None) && (touchOver.Path.Color != touchColor))
                    {
                        touchOver.Path.Color = touchColor;
                        focusColorIndex = ColorPalette.GetIndexFromColor(touchColor);
                        Foley.PlayColorChange();
                        Boku.InGame.IsLevelDirty = true;
                    }

                    // For the duration of the color palette handling touch, all touch inputs are deferred.
                    if (Boku.InGame.ColorPalette.HandlingTouch)
                    {
                        return;
                    }
                }
                else
                {
                    //no path selected, hide the palette
                    Boku.InGame.ColorPalette.Active = false;
                }

                //clear selections if menu options change
                if (TouchInput.WasTouched && !Boku.InGame.inGame.TouchEdit.HasNonUITouch())
                {
                    touchOver.Clear();
                }

                //adding/adjusting flags may change with update - but we don't want to allow a new path if we entered the update in
                //one of these modes
                bool allowNewPath = !touchOver.Adding && !touchOver.Adjusting;

                touchOver.Update(inGame, camera);

                // If the user presses the left button while not over anything
                // start adding a plain path.
                if (allowNewPath && 
                    !touchOver.Adding && !touchOver.Adjusting && 
                    TouchGestureManager.Get().TapGesture.WasRecognized &&
                    Boku.InGame.inGame.touchEditUpdateObj.ToolBar.IsButtonActionToggledOn(ToolBar.TouchControls.BrushActionIDs.baNode) &&
                    Boku.InGame.inGame.TouchEdit.HasNonUITouch())
                {
                    Road.GenIndex = 0;
                    touchOver.NewPath(hitInfo.TerrainPosition, focusColorIndex);
                }
            }

            //We stop adding in touch mode when the add node button is toggled off
            if (touchOver.Adding && !Boku.InGame.inGame.touchEditUpdateObj.ToolBar.IsButtonActionToggledOn(ToolBar.TouchControls.BrushActionIDs.baNode))
            {
                touchOver.StopAdding();
            }

            // Check to see if any of the menus need activating.
            if (TouchGestureManager.Get().TouchHoldGesture.WasRecognized && Boku.InGame.inGame.TouchEdit.HasNonUITouch())
            {
                // The menus may change depending on whether or not the full path is selected.
                SetUpMenus();

                if (touchOver.Over)
                {
                    if (touchOver.node != null)
                    {
                        nodeMenu.Activate(TouchInput.GetOldestTouch().position);
                    }
                    if (touchOver.edge != null)
                    {
                        edgeMenu.Activate(TouchInput.GetOldestTouch().position);
                    }
                }
                else
                {
                    groundMenu.Activate(TouchInput.GetOldestTouch().position);
                    menuPosition = hitInfo.TerrainPosition;
                }
            }

            if (TouchGestureManager.Get().RotateGesture.IsValidated ||
                TouchGestureManager.Get().PinchGesture.IsValidated ||
                TouchGestureManager.Get().DoubleDragGesture.IsValidated)
            {
                //turn off menu if rotating, pinching or double dragging (i.e. terrain manipulation)
                groundMenu.Deactivate();
                nodeMenu.Deactivate();
                edgeMenu.Deactivate();
            }

            groundMenu.Update();
            nodeMenu.Update();
            edgeMenu.Update();

            // Change edge direction?
            if (touchOver.Over && touchOver.Path != null && touchOver.edge != null)
            {
                //direction change via double tap
                if (TouchGestureManager.Get().DoubleTapGesture.WasRecognized)
                {
                    if (touchOver.ActOnPath)
                    {
                        touchOver.Path.IncDir();
                    }
                    else
                    {
                        touchOver.edge.IncDir();
                    }
                }
            }

            //
            // Set up correct HelpOverlay
            //
            if (touchOver.Over)
            {
                if (touchOver.ActOnPath)
                {
                    HelpOverlay.ReplaceTop("MouseEditPathsFocusPath");
                }
                else if (touchOver.node != null)
                {
                    HelpOverlay.ReplaceTop("MouseEditPathsFocusNode");
                }
                else if (touchOver.edge != null)
                {
                    HelpOverlay.ReplaceTop("MouseEditPathsFocusEdge");
                }
            }
        }

        private void UpdateKeyboardMouse()
        {
            Camera camera = Boku.InGame.inGame.shared.camera;
            MouseEdit mouseEdit = Boku.InGame.inGame.MouseEdit;

            MouseEdit.MouseHitInfo hitInfo = MouseEdit.HitInfo;

            // Don't update mouseOver if the menus are active.  This way the focus state will be preserved.
            if (!MenusActive && !SliderActive)
            {
                mouseOver.ActOnPath = KeyboardInput.ShiftIsPressed;

                mouseOver.Update(inGame, camera);

                // If the user presses the left button while not over anything
                // start adding a plain path.
                if (!mouseOver.Adding && !mouseOver.Adjusting && MouseInput.Left.WasPressed)
                {
                    Road.GenIndex = 0;
                    mouseOver.NewPath(hitInfo.TerrainPosition, focusColorIndex);
                }
            }

            // Check for Escape to cancel adding.
            // Also allow clicking the right button to cancel.
            if (mouseOver.Adding && (Actions.Cancel.WasPressed || MouseInput.Right.WasPressed))
            {
                Actions.Cancel.ClearAllWasPressedState();
                MouseInput.Right.ClearAllWasPressedState();

                if (mouseOver.Adding)
                {
                    mouseOver.StopAdding();
                }
            }

            // Check to see if any of the menus need activating.
            if (MouseInput.Right.WasPressed)
            {
                // The menus may change depending on whether or not the full path is selected.
                SetUpMenus();

                if (mouseOver.Over)
                {
                    if (mouseOver.node != null)
                    {
                        nodeMenu.Activate(new Vector2(MouseInput.Position.X, MouseInput.Position.Y));
                    }
                    if (mouseOver.edge != null)
                    {
                        edgeMenu.Activate(new Vector2(MouseInput.Position.X, MouseInput.Position.Y));
                    }
                }
                else
                {
                    groundMenu.Activate(new Vector2(MouseInput.Position.X, MouseInput.Position.Y));
                    menuPosition = hitInfo.TerrainPosition;
                }
            }

            groundMenu.Update();
            nodeMenu.Update();
            edgeMenu.Update();

            // See if we've tried to change path types.
            if (mouseOver.Over && mouseOver.Path != null && mouseOver.node != null && !MenusActive && !sliderActive)
            {
                if (Actions.Up.WasPressedOrRepeat)
                {
                    mouseOver.Path.Road.AdvanceGen(1);
                }
                if (Actions.Down.WasPressedOrRepeat)
                {
                    mouseOver.Path.Road.AdvanceGen(-1);
                }
            }

            // Change edge direction?
            if (mouseOver.Over && mouseOver.Path != null && mouseOver.edge != null)
            {
                if (Actions.Up.WasPressedOrRepeat)
                {
                    if (mouseOver.ActOnPath)
                    {
                        mouseOver.Path.IncDir();
                    }
                    else
                    {
                        mouseOver.edge.IncDir();
                    }
                }
                if (Actions.Down.WasPressedOrRepeat)
                {
                    if (mouseOver.ActOnPath)
                    {
                        mouseOver.Path.DecDir();
                    }
                    else
                    {
                        mouseOver.edge.DecDir();
                    }
                }
            }

            // Color palette support.
            if (mouseOver.Over && mouseOver.Path != null && !MenusActive && !sliderActive)
            {
                focusColorIndex = ColorPalette.GetIndexFromColor(mouseOver.Path.Color);
                Boku.InGame.ColorPalette.Active = true;

                int numColors = Boku.InGame.ColorPalette.NumEntries;
                if (Actions.Left.WasPressedOrRepeat)
                {
                    focusColorIndex = (focusColorIndex + numColors - 1) % numColors;
                    mouseOver.Path.Color = ColorPalette.GetColorFromIndex(focusColorIndex);
                    Foley.PlayColorChange();
                    Boku.InGame.IsLevelDirty = true;
                }
                if (Actions.Right.WasPressedOrRepeat)
                {
                    focusColorIndex = (focusColorIndex + 1) % numColors;
                    mouseOver.Path.Color = ColorPalette.GetColorFromIndex(focusColorIndex);
                    Foley.PlayColorChange();
                    Boku.InGame.IsLevelDirty = true;
                }
            }
            else
            {
                Boku.InGame.ColorPalette.Active = false;
            }

            //
            // Set up correct HelpOverlay
            //
            if (mouseOver.Over)
            {
                if (mouseOver.ActOnPath)
                {
                    HelpOverlay.ReplaceTop("MouseEditPathsFocusPath");
                }
                else if (mouseOver.node != null)
                {
                    HelpOverlay.ReplaceTop("MouseEditPathsFocusNode");
                }
                else if (mouseOver.edge != null)
                {
                    HelpOverlay.ReplaceTop("MouseEditPathsFocusEdge");
                }
            }

        }

        private void UpdateMouseSliders(Camera uicamera)
        {
            Vector2 hitPos = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
            
            Matrix mat = Matrix.Invert(slider.WorldMatrix);

            // Convert mouse hit into UV coords.
            Vector2 hitUV = TouchInput.GetHitUV(hitPos, uicamera, ref mat, slider.Size.X, slider.Size.Y, useRtCoords: false);


            bool outside = true;
            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
            {
                slider.HandleMouseInput(hitUV);
                outside = false;
            }

            Matrix identity = Matrix.Identity;
            slider.Update(ref identity);

            //Get Close position matrix.
            mat = slider.WorldMatrix;
            mat.Translation = new Vector3(closePosition.X, closePosition.Y, mat.Translation.Z);
            mat = Matrix.Invert(mat);

            hitUV = TouchInput.GetHitUV(hitPos, uicamera, ref mat, closeSize.X, closeSize.Y, useRtCoords: false);

            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
            {
                if (MouseInput.Left.WasPressed)
                {
                    MouseInput.ClickedOnObject = this;
                }
                if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == this)
                {
                    sliderActive = false;
                }

                if (MouseInput.ClickedOnObject == this)
                {
                    closeSquareTexture = closeSquareLitTexture;
                }
            }
            else
            {
                closeSquareTexture = closeSquareUnlitTexture;

                // If user clicked outside of slide and close box, close slider.
                if (MouseInput.Left.WasPressed && outside)
                {
                    MouseInput.Left.ClearAllWasPressedState();
                    sliderActive = false;
                }
            }
            if (Actions.Cancel.WasPressed)
            {
                Actions.Cancel.ClearAllWasPressedState();
                sliderActive = false;
            }
        }

        private void UpdateTouchSliders(Camera uicamera)
        {
            TouchContact touch = TouchInput.GetOldestTouch();
            Vector2 hitPos = Vector2.Zero;
            if (touch != null)
            {
                hitPos = touch.position;

                Matrix mat = slider.InvWorldMatrix;

                // Convert mouse hit into UV coords.
                Vector2 hitUV = TouchInput.GetHitUV(hitPos, uicamera, ref mat, slider.Size.X, slider.Size.Y, useRtCoords: false);

                bool outside = true;
                if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                {
                    slider.HandleTouchInput(touch, hitUV);
                    outside = false;
                }

                mat = Matrix.Identity;
                slider.Update(ref mat);

                //Get Close position matrix.
                mat = slider.WorldMatrix;
                mat.Translation = new Vector3( closePosition.X, closePosition.Y, mat.Translation.Z );
                mat = Matrix.Invert(mat);

                hitUV = TouchInput.GetHitUV(hitPos, uicamera, ref mat, closeSize.X, closeSize.Y, useRtCoords: false);

                if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                {
                    //   if (MouseInput.Left.WasPressed)
                    if (touch.phase == TouchPhase.Began)
                    {
                        touch.TouchedObject = this;
                    }
                    if (touch.phase == TouchPhase.Ended && touch.TouchedObject == this)
                    {
                        sliderActive = false;
                    }

                    if (touch.TouchedObject == this)
                    {
                        closeSquareTexture = closeSquareLitTexture;
                    }
                }
                else
                {
                    closeSquareTexture = closeSquareUnlitTexture;

                    // If user clicked outside of slide and close box, close slider.
                    if ((TouchInput.GetOldestTouch() != null) &&
                        (TouchInput.GetOldestTouch().phase == TouchPhase.Began) &&
                        outside)
                    {
                        sliderActive = false;
                    }
                }
                if (Actions.Cancel.WasPressed)
                {
                    Actions.Cancel.ClearAllWasPressedState();
                    sliderActive = false;
                }
            }
        }

        public void Render(Camera camera)
        {
            if (Active)
            {
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    touchOver.Render(Boku.InGame.inGame.shared.camera);
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    mouseOver.Render(Boku.InGame.inGame.shared.camera);
                }

                Boku.InGame.RenderColorMenu(focusColorIndex);

                if (sliderActive)
                {
                    // Render menu using local camera.
                    Fx.ShaderGlobals.SetCamera(camera);



                    float tutScale = 1.0f;
                    if (BokuGame.ScreenSize.X > BokuGame.ScreenSize.Y)
                    {
                        float smallestRes = Math.Min(camera.Resolution.X, camera.Resolution.Y);
                        tutScale = (smallestRes > 0) ? BokuGame.ScreenSize.Y / smallestRes : 1.0f;
                    }
                    else
                    {
                        float biggestRes = Math.Max(camera.Resolution.X, camera.Resolution.Y);
                        tutScale = (biggestRes > 0) ? BokuGame.ScreenSize.X / biggestRes : 1.0f;
                    }

                    // Note 7.5 is the default vertical height for the UI camera.
                    float y = -7.5f / 2.0f + tutScale * slider.Size.Y;
                    slider.WorldMatrix = Matrix.CreateScale(tutScale) * Matrix.CreateTranslation(new Vector3(0.0f, y, 0.0f));
                    slider.position = slider.WorldMatrix.Translation;
                    slider.Render(camera);

                    // Render the CloseBox.
                    CameraSpaceQuad csquad = CameraSpaceQuad.GetInstance();
                    float scale = 0.33f;
                    closeSize = tutScale * scale * new Vector2(slider.Height, slider.Height);
                    // Calc position to have top even with top of slider.
                    closePosition = tutScale * new Vector2(slider.WorldMatrix.Translation.X + slider.Width / 2.0f + closeSize.X / 2.0f, slider.WorldMatrix.Translation.Y + (1.0f - scale) * slider.Height / 2.0f);
                    closePosition = new Vector2(slider.WorldMatrix.Translation.X, slider.WorldMatrix.Translation.Y);
                    closePosition.X += tutScale * slider.Size.X / 2.0f + closeSize.X / 2.0f;
                    closePosition.Y += tutScale * slider.Size.Y / 2.0f - closeSize.Y / 2.0f;
                    // Hack adjust for issues with art.
                    closePosition += tutScale * new Vector2(-0.04f, -0.03f);
                    csquad.Render(camera, closeSquareTexture, closePosition, closeSize, "TexturedRegularAlpha");
                }
            }
        }   // end of Render()


        #endregion Public

        #region Internal

        private void SetUpMenus()
        {
            //
            // GroundMenu
            //

            groundMenu.DeleteAll();
            groundMenu.AddText(Strings.Localize("mouseEdit.addNodePlain"));
            groundMenu.AddText(Strings.Localize("mouseEdit.addNodeWall"));
            groundMenu.AddText(Strings.Localize("mouseEdit.addNodeRoad"));
            groundMenu.AddText(Strings.Localize("mouseEdit.addNodeFlora"));
            groundMenu.OnSelect = GroundOnSelect;
            groundMenu.OnCancel = OnCancel;

            //
            // NodeMenu
            //

            nodeMenu.DeleteAll();

            //add more is not supported in this fashion when in touch mode - instead, just use add mode, select the starting point, and then start 
            //adding
            if (GamePadInput.ActiveMode != GamePadInput.InputMode.Touch)
            {
                nodeMenu.AddText(Strings.Localize("mouseEdit.addMore"));
            }

            nodeMenu.AddText(Strings.Localize("mouseEdit.height"));
            if (mouseOver.ActOnPath)
            {
                nodeMenu.AddText(Strings.Localize("mouseEdit.rotate"));
            }
            nodeMenu.AddText(Strings.Localize("mouseEdit.type"));
            nodeMenu.AddText(Strings.Localize("mouseEdit.delete"));
            nodeMenu.OnSelect = NodeOnSelect;
            nodeMenu.OnCancel = OnCancel;

            //
            // EdgeMenu
            //

            edgeMenu.DeleteAll();
            edgeMenu.AddText(Strings.Localize("mouseEdit.directions"));
            edgeMenu.AddText(Strings.Localize("mouseEdit.split"));
            edgeMenu.AddText(Strings.Localize("mouseEdit.delete"));
            edgeMenu.OnSelect = EdgeOnSelect;
            edgeMenu.OnCancel = OnCancel;

        }   // end of SetUpMenus();

        public void GroundOnSelect(MouseMenu menu)
        {
            Camera camera = Boku.InGame.inGame.shared.camera;

            // Calc position for new node adjusted for perspective.
            Vector3 terrainToCameraDir = menuPosition - camera.From;
            terrainToCameraDir.Normalize();
            float nodeRadius = 1.0f;
            Vector3 position = menuPosition + terrainToCameraDir * (nodeRadius / terrainToCameraDir.Z);

            if (menu.CurString == Strings.Localize("mouseEdit.addNodePlain"))
            {
                Road.GenIndex = 0;
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    touchOver.NewPath(position, focusColorIndex);
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    mouseOver.NewPath(position, focusColorIndex);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.addNodeRoad"))
            {
                Road.GenIndex = Road.LastRoadCreated;
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    touchOver.NewPath(position, focusColorIndex);
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    mouseOver.NewPath(position, focusColorIndex);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.addNodeWall"))
            {
                Road.GenIndex = Road.LastWallCreated;
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    touchOver.NewPath(position, focusColorIndex);
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    mouseOver.NewPath(position, focusColorIndex);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.addNodeFlora"))
            {
                Road.GenIndex = Road.LastVegCreated;
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    touchOver.NewPath(position, focusColorIndex);
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    mouseOver.NewPath(position, focusColorIndex);
            }
            else
            {
                Debug.Assert(false, "Should never get here.");
            }
        }   // end of GroundOnSelect()

        public void NodeOnSelect(MouseMenu menu)
        {
            if (menu.CurString == Strings.Localize("mouseEdit.addMore"))
            {
                //add more is not supported in this fashion when in touch mode - instead, just use add mode, select the starting point, and then start 
                //adding
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    mouseOver.StartAdding(mouseOver.node);
                }
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.height"))
            {
                slider.Label = Strings.Localize("mouseEdit.heightValue");
                slider.MinValue = 0.0f;
                slider.MaxValue = 30.0f;
                slider.NumberOfDecimalPlaces = 2;
                slider.IncrementByAmount = 0.01f;

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    slider.CurrentValue = touchOver.node.Height;
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    slider.CurrentValue = mouseOver.node.Height;
                sliderPrevValue = slider.CurrentValue;

                slider.Selected = true;

                sliderActive = true;
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.rotate"))
            {
                slider.Label = Strings.Localize("mouseEdit.rotationValue");
                slider.MinValue = -180.0f;
                slider.MaxValue = 180.0f;
                slider.NumberOfDecimalPlaces = 1;
                slider.IncrementByAmount = 0.1f;

                slider.CurrentValue = 0.0f;
                sliderPrevValue = slider.CurrentValue;

                slider.Selected = true;

                sliderActive = true;
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.type"))
            {
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    touchOver.Path.Road.AdvanceGen(1);
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    mouseOver.Path.Road.AdvanceGen(1);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.delete"))
            {
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    if (touchOver.ActOnPath)
                    {
                        touchOver.Path.Delete();
                    }
                    else
                    {
                        touchOver.node.Delete();
                    }
                    //clear things out in touch mode
                    touchOver.Clear();
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    if (mouseOver.ActOnPath)
                    {
                        mouseOver.Path.Delete();
                    }
                    else
                    {
                        mouseOver.node.Delete();
                    }
                }
            }
            else
            {
                Debug.Assert(false, "Should never get here.");
            }
        }   // end of NodeOnSelect()

        public void EdgeOnSelect(MouseMenu menu)
        {
            if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
            {
                if (menu.CurString == Strings.Localize("mouseEdit.directions"))
                {
                    if (touchOver.ActOnPath)
                    {
                        touchOver.Path.IncDir();
                    }
                    else
                    {
                        touchOver.edge.IncDir();
                    }
                }
                else if (menu.CurString == Strings.Localize("mouseEdit.split"))
                {
                    touchOver.SplitCurrentEdge();
                }
                else if (menu.CurString == Strings.Localize("mouseEdit.delete"))
                {
                    if (touchOver.ActOnPath)
                    {
                        touchOver.Path.Delete();
                    }
                    else
                    {
                        touchOver.edge.Delete();
                    }
                    touchOver.Clear();
                }
                else
                {
                    Debug.Assert(false, "Should never EVER get here.");
                }
            }
            else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
            {
                if (menu.CurString == Strings.Localize("mouseEdit.directions"))
                {
                    if (mouseOver.ActOnPath)
                    {
                        mouseOver.Path.IncDir();
                    }
                    else
                    {
                        mouseOver.edge.IncDir();
                    }
                }
                else if (menu.CurString == Strings.Localize("mouseEdit.split"))
                {
                    mouseOver.SplitCurrentEdge();
                }
                else if (menu.CurString == Strings.Localize("mouseEdit.delete"))
                {
                    if (mouseOver.ActOnPath)
                    {
                        mouseOver.Path.Delete();
                    }
                    else
                    {
                        mouseOver.edge.Delete();
                    }
                }
                else
                {
                    Debug.Assert(false, "Should never get here.");
                }
            }
        }   // end of EdgeOnSelect()

        public void OnCancel(MouseMenu menu)
        {

        }   // end of OnCancel()

        public override void OnActivate()
        {
            //base.OnActivate();

            // Get references.  We can't do this in the
            // c'tor since not all of these exist yet.
            inGame = Boku.InGame.inGame;
            shared = inGame.shared;

            inGame.HideCursor();

            // May have changed since last time.
            SetUpMenus();

            closeSquareTexture = closeSquareUnlitTexture;

        }   // end of OnActivate()

        public override void OnDeactivate()
        {
            //base.OnDeactivate();

            if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
            {   // Ensure that the selection highlight is off.
                TouchEdit touchEdit = Boku.InGame.inGame.TouchEdit;
                touchEdit.KillSelectionHighlight();
            }
            else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
            {
                // Ensure that the selection highlight is off.
                MouseEdit mouseEdit = Boku.InGame.inGame.MouseEdit;
                mouseEdit.KillSelectionHighlight();
            }
            sliderActive = false;

            groundMenu.Deactivate();
            nodeMenu.Deactivate();
            edgeMenu.Deactivate();

            //make sure the color palette doesn't stay up
            Boku.InGame.ColorPalette.Active = false;

        }   // end of OnDeactivate()
        #endregion Internal


        #region INeedsDeviceReset Members

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(slider);

            if (closeSquareLitTexture == null)
            {
                closeSquareLitTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CloseSquare");
            }
            if (closeSquareUnlitTexture == null)
            {
                closeSquareUnlitTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\CloseSquareDesat");
            }

        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            slider.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(slider);
            BokuGame.Release(ref closeSquareLitTexture);
            BokuGame.Release(ref closeSquareUnlitTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
            slider.DeviceReset(device);
        }

        #endregion

    }   // class EditPathsTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


