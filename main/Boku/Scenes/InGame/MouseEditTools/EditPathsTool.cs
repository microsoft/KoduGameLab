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

using KoiX;
using KoiX.Input;
using KoiX.Text;

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

            // Get references.
            inGame = Boku.InGame.inGame;
            shared = inGame.shared;

            SetUpMenus();

        }   // end of c'tor

        public static BaseMouseEditTool GetInstance()
        {
            if (instance == null)
            {
                instance = new EditPathsTool();
            }
            return instance;
        }   // end of GetInstance()

        public override void Update()
        {
            if (Active)
            {
                // We need to update our child objects first so they have first shot at grabbing any input.
                inGame.shared.addItemHelpCard.Update();

                SelectOverlay();

                if (KoiLibrary.LastTouchedDeviceIsTouch)
                {
                    UpdateTouch();
                }
                else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
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

            HitInfo MouseTouchHitInfo = TouchEdit.MouseTouchHitInfo;

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
                touchOver.NewPath(MouseTouchHitInfo.TerrainPosition, focusColorIndex);
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
                    menuPosition = MouseTouchHitInfo.TerrainPosition;
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

            HitInfo MouseTouchHitInfo = MouseEdit.MouseTouchHitInfo;

            mouseOver.ActOnPath = KeyboardInput.ShiftIsPressed;

            mouseOver.Update(inGame, camera);

            // If the user presses the left button while not over anything
            // start adding a plain path.
            if (!mouseOver.Adding && !mouseOver.Adjusting && LowLevelMouseInput.Left.WasPressed)
            {
                Road.GenIndex = 0;
                mouseOver.NewPath(MouseTouchHitInfo.TerrainPosition, focusColorIndex);
            }

            // Check for Escape to cancel adding.
            // Also allow clicking the right button to cancel.
            if (mouseOver.Adding && (Actions.Cancel.WasPressed || LowLevelMouseInput.Right.WasPressed))
            {
                Actions.Cancel.ClearAllWasPressedState();
                LowLevelMouseInput.Right.ClearAllWasPressedState();

                if (mouseOver.Adding)
                {
                    mouseOver.StopAdding();
                }
            }

            // Check to see if any of the menus need activating.
            if (LowLevelMouseInput.Right.WasPressed)
            {
                // The menus may change depending on whether or not the full path is selected.
                SetUpMenus();

                if (mouseOver.Over)
                {
                    if (mouseOver.node != null)
                    {
                        nodeMenu.Activate(new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y));
                    }
                    if (mouseOver.edge != null)
                    {
                        edgeMenu.Activate(new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y));
                    }
                }
                else
                {
                    groundMenu.Activate(new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y));
                    menuPosition = MouseTouchHitInfo.TerrainPosition;
                }
            }

            groundMenu.Update();
            nodeMenu.Update();
            edgeMenu.Update();

            // See if we've tried to change path types.
            if (mouseOver.Over && mouseOver.Path != null && mouseOver.node != null)
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
            if (mouseOver.Over && mouseOver.Path != null)
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

        public void Render(Camera camera)
        {
            if (Active)
            {
                if (KoiLibrary.LastTouchedDeviceIsTouch)
                {
                    touchOver.Render(Boku.InGame.inGame.shared.camera);
                }
                else if (KoiLibrary.LastTouchedDeviceIsTouch)
                {
                    mouseOver.Render(Boku.InGame.inGame.shared.camera);
                }

                Boku.InGame.RenderColorMenu(focusColorIndex);

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
            if (!KoiLibrary.LastTouchedDeviceIsTouch)
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
                if (KoiLibrary.LastTouchedDeviceIsTouch)
                    touchOver.NewPath(position, focusColorIndex);
                else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                    mouseOver.NewPath(position, focusColorIndex);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.addNodeRoad"))
            {
                Road.GenIndex = Road.LastRoadCreated;
                if (KoiLibrary.LastTouchedDeviceIsTouch)
                    touchOver.NewPath(position, focusColorIndex);
                else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                    mouseOver.NewPath(position, focusColorIndex);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.addNodeWall"))
            {
                Road.GenIndex = Road.LastWallCreated;
                if (KoiLibrary.LastTouchedDeviceIsTouch)
                    touchOver.NewPath(position, focusColorIndex);
                else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                    mouseOver.NewPath(position, focusColorIndex);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.addNodeFlora"))
            {
                Road.GenIndex = Road.LastVegCreated;
                if (KoiLibrary.LastTouchedDeviceIsTouch)
                    touchOver.NewPath(position, focusColorIndex);
                else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
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
                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    mouseOver.StartAdding(mouseOver.node);
                }
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.height"))
            {
                // TODO (****) Launch dialog for setting this.
                Debug.Assert(false);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.rotate"))
            {
                // TODO (****) Launch dialog for setting this.
                Debug.Assert(false);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.type"))
            {
                if (KoiLibrary.LastTouchedDeviceIsTouch)
                    touchOver.Path.Road.AdvanceGen(1);
                else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                    mouseOver.Path.Road.AdvanceGen(1);
            }
            else if (menu.CurString == Strings.Localize("mouseEdit.delete"))
            {
                if (KoiLibrary.LastTouchedDeviceIsTouch)
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
                else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
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
            if (KoiLibrary.LastTouchedDeviceIsTouch)
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
            else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
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

        protected override void OnActivate()
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

        protected override void OnDeactivate()
        {
            //base.OnDeactivate();

            if (KoiLibrary.LastTouchedDeviceIsTouch)
            {   // Ensure that the selection highlight is off.
                TouchEdit touchEdit = Boku.InGame.inGame.TouchEdit;
                touchEdit.KillSelectionHighlight();
            }
            else if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
            {
                // Ensure that the selection highlight is off.
                MouseEdit mouseEdit = Boku.InGame.inGame.MouseEdit;
                mouseEdit.KillSelectionHighlight();
            }

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
            if (closeSquareLitTexture == null)
            {
                closeSquareLitTexture = KoiLibrary.LoadTexture2D(@"Textures\GridElements\CloseSquare");
            }
            if (closeSquareUnlitTexture == null)
            {
                closeSquareUnlitTexture = KoiLibrary.LoadTexture2D(@"Textures\GridElements\CloseSquareDesat");
            }

        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            DeviceResetX.Release(ref closeSquareLitTexture);
            DeviceResetX.Release(ref closeSquareUnlitTexture);
        }

        public void DeviceReset(GraphicsDevice device)
        {
        }

        #endregion

    }   // class EditPathsTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


