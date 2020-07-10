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

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Scenes;
using Boku.Scenes.InGame;
using KoiX.Managers;

namespace Boku.Scenes.InGame.MouseEditTools
{
    /// <summary>
    /// Base class for tools which fit into the terrain editing toolbox.
    /// Used for keyboard/mouse and touch.  Not used for gamepad.
    /// </summary>
    public abstract class BaseMouseEditTool : InputEventHandler
    {
        #region Members

        // This is the ID of the help overlay associated with the tool.  Add the text strings for the 
        // help overlay in HelpOverlay.Xml.
        string helpOverlayID = null;
        string helpOverlayMagicBrushID = null;
        string helpOverlayStartID = null;
        string helpOverlayGoingID = null;

        bool active = false;

        // Helpful references.
        protected Boku.InGame inGame = null;
        protected Boku.InGame.Shared shared = null;

        float terrainSpeed = 1.0f;

        bool usingLinearBrush = false;
        Vector2 endPos = Vector2.Zero;

        protected bool isInAction = false;

        // Current edit modes as set by the current tool.
        protected Terrain.EditMode leftMode = Terrain.EditMode.Noop;
        protected Terrain.EditMode middleMode = Terrain.EditMode.Noop;
        protected Terrain.EditMode rightMode = Terrain.EditMode.Noop;
        Terrain.EditMode activeMode = Terrain.EditMode.Noop;

        bool linearBrushPainting = false;

        protected delegate void AudioFeedback();

        protected AudioFeedback LeftAudioStart;
        protected AudioFeedback MiddleAudioStart;
        protected AudioFeedback RightAudioStart;
        protected AudioFeedback LeftAudioEnd;
        protected AudioFeedback MiddleAudioEnd;
        protected AudioFeedback RightAudioEnd;

        #endregion Members

        #region Accessors

        protected bool UsingLinearBrush
        {
            get { return usingLinearBrush; }
            set { usingLinearBrush = value; }
        }

        public bool Active
        {
            get { return active; }
            set
            {
                if (active != value)
                {
                    active = value;
                    if (active)
                    {
                        // Start up.
                        OnActivate();
                    }
                    else
                    {
                        // Shut down.
                        OnDeactivate();
                    }
                }
            }
        }

        /// <summary>
        /// The string used to identify which help overlay 
        /// to activate while this tool is active.
        /// </summary>
        public string HelpOverlayID
        {
            get { return helpOverlayID; }
            set { helpOverlayID = value; }
        }

        /// <summary>
        /// The string used to identify which help overlay 
        /// to activate while this tool is active and the
        /// magic brush is in action.
        /// </summary>
        public string HelpOverlayMagicBrushID
        {
            get { return helpOverlayMagicBrushID; }
            set { helpOverlayMagicBrushID = value; }
        }

        /// <summary>
        /// Which overlay to use prompting user to begin a stretch cursor operation.
        /// </summary>
        public string HelpOverlayStartID
        {
            get { return helpOverlayStartID; }
            set { helpOverlayStartID = value; }
        }

        /// <summary>
        /// Which overlay to use during a stretch cursor operation.
        /// </summary>
        public string HelpOverlayGoingID
        {
            get { return helpOverlayGoingID; }
            set { helpOverlayGoingID = value; }
        }

        /// <summary>
        /// True if the tool is currently changing the world.  Not just active
        /// but actively being used.
        /// </summary>
        public bool IsInAction
        {
            get { return isInAction; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Common update code, expected to be overridden but base Update called as well.
        /// </summary>
        public virtual void Update()
        {
            // Only update if active and the toolbox isn't changing.
            if (Active)
            {
                NotifyTerrainActivity();

                CheckLinearBrush();

                if (!linearBrushPainting)
                {
                    shared.editBrushStart = shared.editBrushPosition;
                }

                // Force end of action if modal dialog is activated.
                if (DialogManagerX.ModalDialogIsActive)
                {
                    inGame.Terrain.EndSelection();
                }
            }

        }   // end of BaseMouseEditTool Update()

        /// <summary>
        /// Override this to provide a place to initialize 
        /// anything that needs to be done on a per-use basis.
        /// </summary>
        protected virtual void OnActivate()
        {
            // Get references.  We can't do this in the
            // c'tor since not all of these exist yet.
            inGame = Boku.InGame.inGame;
            shared = inGame.shared;

            linearBrushPainting = false;

            shared.editBrushStart = shared.editBrushPosition;

            // Allow brush sizing.
            shared.editBrushSizeActive = true;

            //HelpOverlay.Push(helpOverlayID);
            inGame.Cursor3D.Rep = Cursor3D.Visual.Pointy;
            inGame.Cursor3D.DiffuseColor = Vector4.One;

            terrainSpeed = Common.Xml.XmlOptionsData.TerrainSpeed; // just cache this.

            isInAction = false;

            RegisterForInputEvents();
        }

        /// <summary>
        /// Override this to provide a place to clean up
        /// anything that needs it on a per-use basis.
        /// </summary>
        protected virtual void OnDeactivate()
        {
            inGame.ShowCursor();
            inGame.Cursor3D.Rep = Cursor3D.Visual.Edit;
            inGame.Cursor3D.DiffuseColor = new Vector4(0.5f, 0.9f, 0.8f, 0.3f);

            inGame.Terrain.EndSelection();
            VirtualMap.SuppressWaterUpdate = false;

            shared.editBrushSizeActive = false;

            //HelpOverlay.Pop();

            // Shut down all the terrain editing sounds just to be sure.
            Foley.StopPaint();
            Foley.StopEarthDown();
            Foley.StopEarthUp();
            Foley.StopEraseLand();
            Foley.StopLowerWater();
            Foley.StopRaiseWater();

            isInAction = false;

            UnregisterForInputEvents();
        }

        #endregion Public

        #region InputEventHandler

        public override void RegisterForInputEvents()
        {
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseMiddleDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseRightDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.OnePointDrag);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.TwoPointDrag);

            base.RegisterForInputEvents();
        }   // end of RegisterForInputEvents()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                // Claim mouse focus as ours.
                KoiLibrary.InputEventManager.MouseFocusObject = this;

                // Register to get position and left up events.
                // Note, we are using MousePosition rather than MouseMove ehre because we want this to 
                // work properly with the terrain raise and lower type tools.
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MousePosition);
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                activeMode = leftMode;
                if (activeMode != Terrain.EditMode.Noop)
                {
                    LeftAudioStart();
                }
                isInAction = true;

                Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
                bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

                if (isMagicBrush)
                {
                    ProcessSelection();
                }
                else if (UsingLinearBrush)
                {
                    ProcessLinearBrushStart();
                }
                else
                {
                    ProcessPoint();
                }

                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }   // end of ProcessMouseLeftDownEvent()

        public override bool ProcessMousePositionEvent(MouseInput input)
        {
            Debug.Assert(Active);
            
            if (KoiLibrary.InputEventManager.MouseFocusObject == this)
            {
                Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
                bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

                if (isMagicBrush)
                {
                    ProcessSelection();
                }
                else if (UsingLinearBrush)
                {
                    ProcessLinearBrushMove();
                }
                else
                {
                    ProcessPoint();
                }

                return true;
            }
            

            return base.ProcessMousePositionEvent(input);
        }   // end of ProcessMousePositionEvent()

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
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MousePosition);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                // Clear these even if the action fails.
                if (LeftAudioEnd != null)
                {
                    LeftAudioEnd();
                }
                isInAction = false;

                Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
                bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

                if (isMagicBrush)
                {
                    ProcessSelection();
                }
                else if (UsingLinearBrush)
                {
                    ProcessLinearBrushRelease();
                }
                else
                {
                    ProcessPoint();
                }

                return true;
            }
            return false;
        }   // end of ProcessMouseLeftUpEvent()

        public override bool ProcessMouseMiddleDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                // Claim mouse focus as ours.
                KoiLibrary.InputEventManager.MouseFocusObject = this;

                // Register to get move and Middle up events.
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MousePosition);
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseMiddleUp);

                activeMode = middleMode;
                MiddleAudioStart();
                isInAction = true;

                Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
                bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

                if (isMagicBrush)
                {
                    ProcessSelection();
                }
                else if (UsingLinearBrush)
                {
                    ProcessLinearBrushStart();
                }
                else
                {
                    ProcessPoint();
                }

                return true;
            }

            return base.ProcessMouseMiddleDownEvent(input);
        }   // end of ProcessMouseMiddleDownEvent()

        public override bool ProcessMouseMiddleUpEvent(MouseInput input)
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
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MousePosition);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseMiddleUp);

                // Clear these even if the action fails.
                MiddleAudioEnd();
                isInAction = false;

                Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
                bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

                if (isMagicBrush)
                {
                    ProcessSelection();
                }
                else if (UsingLinearBrush)
                {
                    ProcessLinearBrushRelease();
                }
                else
                {
                    ProcessPoint();
                }

                return true;
            }
            return false;
        }   // end of ProcessMouseMiddleUpEvent()

        public override bool ProcessMouseRightDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (KoiLibrary.InputEventManager.MouseFocusObject == null)
            {
                // Claim mouse focus as ours.
                KoiLibrary.InputEventManager.MouseFocusObject = this;

                // Register to get move and Right up events.
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MousePosition);
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseRightUp);

                activeMode = rightMode;
                RightAudioStart();
                isInAction = true;

                Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
                bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

                if (isMagicBrush)
                {
                    ProcessSelection();
                }
                else if (UsingLinearBrush)
                {
                    ProcessLinearBrushStart();
                }
                else
                {
                    ProcessPoint();
                }

                return true;
            }

            return base.ProcessMouseRightDownEvent(input);
        }   // end of ProcessMouseRightDownEvent()

        public override bool ProcessMouseRightUpEvent(MouseInput input)
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
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MousePosition);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseRightUp);

                // Clear these even if the action fails.
                RightAudioEnd();
                isInAction = false;

                Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
                bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

                if (isMagicBrush)
                {
                    ProcessSelection();
                }
                else if (UsingLinearBrush)
                {
                    ProcessLinearBrushRelease();
                }
                else
                {
                    ProcessPoint();
                }

                return true;
            }
            return false;
        }   // end of ProcessMouseRightUpEvent()

        #endregion InputEventHandler

        #region Internal

        /// <summary>
        /// TODO (socy) Currently does nothing.  Could be useful if
        /// we don't want the perf hit for recalculating water while
        /// editing, but we need to see if it's needed first.  So,
        /// leave this here as a reminder.  Remove when we're sure
        /// we don't need it.
        /// </summary>
        void NotifyTerrainActivity()
        {
            VirtualMap.SuppressWaterUpdate = false;

            // TODO (mouse) Problems?
            /*
            VirtualMap.SuppressWaterUpdate
                RightTriggerOn
                || LeftTriggerOn
                || AButtonOn;
            */
        }

        void CheckLinearBrush()
        {
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            if (brush != null && brush.IsLinear)
            {
                /// We're in stretch now. Is that new?
                if (!UsingLinearBrush)
                {
                    linearBrushPainting = false;
                    UsingLinearBrush = true;
                }
            }
            else
            {
                UsingLinearBrush = false;
            }
        }
        /// <summary>
        /// Select which help overlay to be currently displayed.
        /// </summary>
        protected virtual void SelectOverlay()
        {
            //Note: Most mouse edit tools leave their
            // "HelpOverlayID" and "HelpOverlayMagicBrushID"
            // strings null. This is because the ToolBar
            // class sets help overlays when you hover over
            // a tool and in most cases, those help overlays
            // never need to change (for the duration of that
            // tool.) One notable exception is the PaintTool
            // which needs slightly different help for magic
            // brush mode.

            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            if (HelpOverlayMagicBrushID != null && brush.Shape == Brush2DManager.BrushShape.Magic)
            {
                SetOverlay(HelpOverlayMagicBrushID);
            }
            else if (helpOverlayID != null)
            {
                SetOverlay(HelpOverlayID);
            }
        }

        protected void SetOverlay(string overlay)
        {
            HelpOverlay.ReplaceTop(overlay);
        }

        /// <summary>
        /// Check whether cursor should be displayed.
        /// </summary>
        /// <returns></returns>
        protected virtual void CheckSelectCursor(bool alwaysShowCursor)
        {
            // The new pointy cursor seems always helpful, even if slightly
            // redundant with the brush.
            inGame.ShowCursor();

            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            if (brush == null)
            {
                return;
            }

            bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;
            bool isLinearBrush = brush.IsLinear;

            // In touch mode, make sure we don't have the selection on unless the touch is active.
            if (KoiLibrary.LastTouchedDeviceIsTouch)
            {
                if (TouchInput.TouchCount != 1 || TouchInput.WasMultiTouch || !Boku.InGame.inGame.TouchEdit.HasNonUITouch())
                {
                    isMagicBrush = false;
                }
            }

            if (isMagicBrush)
            {
                inGame.Terrain.MakeSelection(shared.editBrushPosition);
            }
            else
            {
                inGame.Terrain.EndSelection();
            }
        }

        /// <summary>
        /// Called by the individual tools to set the current edit modes.
        /// Still need to figure out how to get the inputs from ToolBarDialog
        /// into here.  Or should this become part of ToolBarDialog?
        /// </summary>
        /// <param name="left"></param>
        /// <param name="middle"></param>
        /// <param name="right"></param>
        protected void SetEditModes(Terrain.EditMode left,
                                    Terrain.EditMode middle,
                                    Terrain.EditMode right)
        {
            leftMode = left;
            middleMode = middle;
            rightMode = right;
        }   // end of SetEditModes()


        void ProcessLinearBrushStart()
        {
            Debug.Assert(linearBrushPainting == false);

            // Capture starting position.
            shared.editBrushStart = shared.editBrushPosition;

            linearBrushPainting = true;

        }   // end of ProcessLinearBrushStart()
        
        void ProcessLinearBrushMove()
        {
            Debug.Assert(linearBrushPainting == true);

            // Nothing to see here.  Move aloing.

        }   // end of ProcessLinearBrushMove()

        void ProcessLinearBrushRelease()
        {
            Debug.Assert(linearBrushPainting == true);

            linearBrushPainting = false;

            // Apply the brush to the terrain.
            ProcessLinearBrush();
        }   // end of ProcessLinearBrushRelease()

        public const float kSmallRate = 0.1f;
        public const float kSmallMove = 0.1f;
        public const float kSmallMoveSq = kSmallMove * kSmallMove;

        /// <summary>
        /// We've moved the brush and released the button.  Now apply the result.
        /// </summary>
        void ProcessLinearBrush()
        {
            float distSq = Vector2.DistanceSquared(endPos, shared.editBrushPosition);
            bool gamePadMoved = distSq > kSmallMoveSq;
            bool mouseMoved = LowLevelMouseInput.DeltaPosition != Point.Zero;

            float editSpeed = 1.0f;
            inGame.Terrain.RenderToHeightMap(
                shared.editBrushStart,
                shared.editBrushPosition,
                shared.editBrushRadius,
                activeMode,
                editSpeed * editSpeed * terrainSpeed);

            shared.heightMapModified = true;
            Boku.InGame.IsLevelDirty = true;

        }   // end of ProcessLinearBrush()

        double sizeTime = 0;
        void CheckResizeSelection()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();
            const float kMinResizeTime = 0.25f;

            if (sizeTime >= Time.WallClockTotalSeconds)
            {
                if (Actions.BrushLarger.WasPressedOrRepeat)
                {
                    inGame.Terrain.ExpandSelection();
                }

                if (Actions.BrushSmaller.WasPressedOrRepeat)
                {
                    inGame.Terrain.ShrinkSelection();
                }

                sizeTime = Time.WallClockTotalSeconds + kMinResizeTime;
            }
        }   // end of CheckResizeSelection()


        /// <summary>
        /// Pass off commands as appropriate to the right handlers, depending on
        /// the current game pad and brush states.
        /// 
        /// This is called by the individual tool's Update function after it has done
        /// any tool specific processing.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected void ProcessTriggers(
            Terrain.EditMode leftMode,
            Terrain.EditMode middleMode,
            Terrain.EditMode right)
        {
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

            // Size timer is used for changing selection brush size. Required for both mouse and touch.
            sizeTime += Time.WallClockFrameSeconds;

            if (KoiLibrary.LastTouchedDeviceIsTouch)
            {
                if (isMagicBrush)
                {
                    ProcessTouchSelection();
                }
                else
                {
                    ProcessTouch();
                }
            }

            else if(KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
            {
                if (isMagicBrush)
                {
                    ProcessSelection();
                }
                else if (UsingLinearBrush)
                {
                    //ProcessStretched();
                }
                else
                {
                    ProcessPoint();
                }
            }
        }   // end of ProcessTriggers()


        /// <summary>
        /// Apply commands in material select mode.
        /// </summary>
        protected virtual void ProcessSelection()
        {
            isInAction = false;

            CheckResizeSelection();
            inGame.Terrain.RenderToSelection(activeMode, terrainSpeed);
            shared.heightMapModified = true;
            Boku.InGame.IsLevelDirty = true;
        }   // end of PorcessSelection()
        
        public bool oldInAction=false;

        /// <summary>
        /// Apply commands to single brush region.
        /// </summary>
        protected virtual void ProcessPoint()
        {
            float rate = 0.5f;
            isInAction = false;

            inGame.Terrain.RenderToHeightMap(
                shared.editBrushPosition,
                shared.editBrushRadius,
                activeMode,
                rate * rate * terrainSpeed);
            shared.heightMapModified = true;
            Boku.InGame.IsLevelDirty = true;
        }   // end of ProcessPoint()
		
        /// <summary>
        /// Apply commands in material select mode.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected virtual void ProcessTouchSelection()
        {
            isInAction = false;

            if (TouchInput.TouchCount==1 && TouchInput.Touches[0].phase != TouchPhase.Ended && 
                shared.currentTouchAction != ToolBar.TouchControls.BrushActionIDs.NUMBER_OF_Buttons && shared.editBrushAllowedForTouch)
            {
                if ((shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baBrushMore) ||
                     (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baBrushLess))
                {
                    const float kMinResizeTime = 0.3f;
                    if (sizeTime >= kMinResizeTime)
                    {
                        sizeTime = 0.0f;
                        if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baBrushMore)
                        {
                            inGame.Terrain.ExpandSelection();
                        }
                        else if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baBrushLess)
                        {
                            inGame.Terrain.ShrinkSelection();
                        }
                    }
                }

                // Left
                if ((shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baPaintMaterial) ||
                     (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baTerrainRaise) ||
                     (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baFlatten) ||
                     (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baSpikey))
                {
#if !NETFX_CORE
                    Debug.Print("Painting with selection...");
#endif

                    float rate = 1.0f;
                    inGame.Terrain.RenderToSelection(leftMode, rate * rate * terrainSpeed);
                    shared.heightMapModified = true;
                    Boku.InGame.IsLevelDirty = true;

                    LeftAudioStart();
                    isInAction = true;
                }
                else
                {
                    LeftAudioEnd();
                }

                // Middle
                if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baSmooth)
                {
                    float rate = 1.0f;
                    inGame.Terrain.RenderToSelection(middleMode, rate * rate * terrainSpeed);
                    shared.heightMapModified = true;
                    Boku.InGame.IsLevelDirty = true;

                    MiddleAudioStart();
                    isInAction = true;
                }
                else
                {
                    MiddleAudioEnd();
                }

                // Right
                if ((shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baDelete) ||
                    (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baTerrainLower) ||
                    (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baHilly))
                {
                    float rate = 1.0f;
                    inGame.Terrain.RenderToSelection(rightMode, rate * rate * terrainSpeed);
                    shared.heightMapModified = true;
                    Boku.InGame.IsLevelDirty = true;

                    RightAudioStart();
                    isInAction = true;
                }
                else
                {
                    RightAudioEnd();
                }
            }
            else if (!shared.editBrushAllowedForTouch)
            {
                LeftAudioEnd();
                MiddleAudioEnd();
                RightAudioEnd();
            }
		}

        /// <summary>
        /// Apply commands to single brush region.
        /// </summary>
        protected virtual void ProcessTouch()
        {
            float rate = 0.5f;

            isInAction = false;

            if ((shared.currentTouchAction != ToolBar.TouchControls.BrushActionIDs.NUMBER_OF_Buttons) &&
                shared.editBrushAllowedForTouch)
            {
                if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baTerrainRaise ||
                    shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baPaintMaterial ||
                    shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baSpikey ||
                    shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baWaterRaise )
                {
                    inGame.Terrain.RenderToHeightMap(
                        shared.editBrushPosition,
                        shared.editBrushRadius,
                        leftMode,
                        rate * rate * terrainSpeed);
                    shared.heightMapModified = true;
                    Boku.InGame.IsLevelDirty = true;

                    LeftAudioStart();
                    isInAction = true;
                }
                else
                {
                    LeftAudioEnd();
                }

                if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baFlatten ||
                    shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baDelete)
                {
                    inGame.Terrain.RenderToHeightMap(
                        shared.editBrushPosition,
                        shared.editBrushRadius,
                        rightMode,
                        rate * rate * terrainSpeed);
                    shared.heightMapModified = true;
                    Boku.InGame.IsLevelDirty = true;

                    LeftAudioStart();
                    isInAction = true;
                }
                    //This if check is to prevent the left audio from always ending when we're raising water.
                else if (shared.currentTouchAction != ToolBar.TouchControls.BrushActionIDs.baWaterRaise)
                {
                    LeftAudioEnd();
                }

                // Smooth
                if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baSmooth )
                {
                    inGame.Terrain.RenderToHeightMap(
                        shared.editBrushPosition,
                        shared.editBrushRadius,
                        middleMode,
                        rate * rate * terrainSpeed);
                    shared.heightMapModified = true;
                    Boku.InGame.IsLevelDirty = true;

                    MiddleAudioStart();
                    isInAction = true;
                }
                else
                {
                    MiddleAudioEnd();
                }

                // Down
                if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baTerrainLower ||
                    shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baWaterLower ||
                    shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baHilly )
                {
                    inGame.Terrain.RenderToHeightMap(
                        shared.editBrushPosition,
                        shared.editBrushRadius,
                        rightMode,
                        rate * rate * terrainSpeed);
                    shared.heightMapModified = true;
                    Boku.InGame.IsLevelDirty = true;

                    RightAudioStart();
                    isInAction = true;
                }
                else
                {
                    RightAudioEnd();
                }

                if ( TouchInput.WasReleased )
                {
                    LeftAudioEnd();
                    MiddleAudioEnd();
                    RightAudioEnd();
                }
            }
            else if (!shared.editBrushAllowedForTouch)
            {
                LeftAudioEnd();
                MiddleAudioEnd();
                RightAudioEnd();
            }
        }
        #endregion Internal

    }   // end of class BaseMouseEditTool

}   // end of namespace Boku.Scenes.InGame.Tools
