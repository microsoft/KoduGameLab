
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

namespace Boku.Scenes.InGame.MouseEditTools
{
    /// <summary>
    /// Base class for tools which fit into the terrain editing toolbox.
    /// </summary>
    public abstract class BaseMouseEditTool
    {
        #region Members

        // This is the ID of the help overlay associated with the tool.  Add the text strings for the 
        // help overlay in HelpOverlay.Xml.
        private string helpOverlayID = null;
        private string helpOverlayMagicBrushID = null;
        private string helpOverlayStartID = null;
        private string helpOverlayGoingID = null;

        private bool active = false;
        private bool starting = true;   // Set to true when tool is activated.  Used to debounce buttons.

        protected int prevBrushIndex = 0;   // Let the tools that care about brushes remember what they were using.

        protected BasePicker pickerX = null;    // Picker associated with X key.
        protected BasePicker pickerY = null;    // Picker associated with Y key.

        // Helpful references.
        protected Boku.InGame inGame = null;
        protected Boku.InGame.Shared shared = null;
        protected MaterialPicker materialPicker = null;
        protected WaterPicker waterPicker = null;
        protected BrushPicker brushPicker = null;

        private float terrainSpeed = 1.0f;

        private bool inStretchMode = false;
        private Vector2 endPos = Vector2.Zero;

        private bool isInAction = false;

        protected enum Phase
        {
            Open,
            Ready,
            Going
        };
        private Phase stretchPhase = Phase.Open;

        protected delegate void AudioFeedback();

        protected AudioFeedback LeftAudioStart;
        protected AudioFeedback MiddleAudioStart;
        protected AudioFeedback RightAudioStart;
        protected AudioFeedback LeftAudioEnd;
        protected AudioFeedback MiddleAudioEnd;
        protected AudioFeedback RightAudioEnd;

        #endregion Members

        #region Accessors

        protected bool InStretchMode
        {
            get { return inStretchMode; }
            private set { inStretchMode = value; }
        }
        protected Phase StretchPhase
        {
            get { return stretchPhase; }
            set { stretchPhase = value; }
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
        /// Are we still waiting for the buttons/triggers to be released?
        /// </summary>
        public bool DebouncePending
        {
            get
            {
                if (starting)
                {
                    if (!Actions.PickerX.IsPressed
                        && !Actions.PickerY.IsPressed
                        && !Actions.Select.IsPressed
                        && !Actions.PickerLeft.IsPressed
                        && !Actions.PickerRight.IsPressed)
                    {
                        starting = false;
                    }
                }

                return starting;
            }
        }
        /// <summary>
        /// Put the tool into a state which forces debouncing.
        /// </summary>
        public bool Starting
        {
            get { return starting; }
            set { starting = value; }
        }

        /// <summary>
        /// Set and activate the picker associated with key X.
        /// </summary>
        protected BasePicker PickerX
        {
            get { return pickerX; }
            set
            {
                pickerX = value;
                if (pickerX != null)
                {
                    pickerX.Active = true;
                }
            }
        }
        /// <summary>
        /// Set and activate the picker associated with key Y.
        /// </summary>
        protected BasePicker PickerY
        {
            get { return pickerY; }
            set
            {
                pickerY = value;
                if (pickerY != null)
                {
                    pickerY.Active = true;
                }
            }
        }

        /// <summary>
        /// Is the picker assigned to the X key in use.
        /// </summary>
        protected bool PickerXInUse
        {
            get { return pickerX != null && pickerX.Active && !pickerX.Hidden; }
        }
        /// <summary>
        /// Is the picker assigned to the Y key in use.
        /// </summary>
        protected bool PickerYInUse
        {
            get { return pickerY != null && pickerY.Active && !pickerY.Hidden; }
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
        public virtual void Update(Camera camera)
        {
            // Only update if active and the toolbox isn't changing.
            if (Active)
            {
                NotifyTerrainActivity();

                CheckStretchMode();

                // Switch to Mini-Hub?
                if (!PickerXInUse && !PickerYInUse && Actions.MiniHub.WasPressed)
                {
                    /// The InGameEditBase will handle backing us out. Just don't do anything.
                    return;
                }

                // When a picker is being used currentTouchAction gets set to the invalid state NUMBER_OF_Buttons but
                // when the picker stops being used currentTouchAction doesn't get restored to its previous value.  The
                // result is that the user must tap the paint button again before any input works.  So, this hack tries
                // to keep track of the good state in previousGoodTouchAction and restores that if the pickers are both
                // closed and the state is bad.
                // If neither picker is in use, either restore currentTouchAction to a useful state or save its current state.
                if ((pickerX == null || pickerX.Hidden) && (pickerY == null || pickerY.Hidden))
                {
                    // Do we have an invalid state?
                    if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.NUMBER_OF_Buttons)
                    {
                        // Invalid state, restore previous good state.
                        shared.currentTouchAction = shared.previousGoodTouchAction;
                    }
                    else
                    {
                        // Valid state, save it to restore later.
                        shared.previousGoodTouchAction = shared.currentTouchAction;
                    }
                }

                // Use X and Y to bring up associated pickers.  If another is already
                // being used, accept the current state and switch to the new one.
                if ((Actions.PickerX.WasPressed) && pickerX != null)
                {
                    Actions.PickerX.ClearAllWasPressedState();

                    // First check if the other picker is already in use.
                    if (PickerYInUse)
                    {
                        PickerY.SelectCurrentChoice();
                        PickerY.Hidden = true;
                    }

                    PickerX.Hidden = false;
                }
                if ((Actions.PickerY.WasPressed) && pickerY != null && pickerY.Hidden)
                {
                    Actions.PickerY.ClearAllWasPressedState();

                    // First check if the other picker is already in use.
                    if (PickerXInUse)
                    {
                        PickerX.SelectCurrentChoice();
                        PickerX.Hidden = true;
                    }

                    PickerY.Hidden = false;
                }
            }

            if (Active)
            {
                if (shared.ToolBox.PickersActive)
                {
                    inGame.Terrain.EndSelection();
                }
            }
            shared.editBrushSizeActive = !inGame.mouseEditUpdateObj.ToolBox.PickersActive;

        }   // end of BaseMouseEditTool Update()

        /// <summary>
        /// Override this to provide a place to initialize 
        /// anything that needs to be done on a per-use basis.
        /// </summary>
        public virtual void OnActivate()
        {
            // Get references.  We can't do this in the
            // c'tor since not all of these exist yet.
            inGame = Boku.InGame.inGame;
            shared = inGame.shared;

            materialPicker = inGame.mouseEditUpdateObj.ToolBox.MaterialPicker;
            waterPicker = inGame.mouseEditUpdateObj.ToolBox.WaterPicker;
            brushPicker = inGame.mouseEditUpdateObj.ToolBox.BrushPicker;

            starting = true;
            StretchPhase = Phase.Open;

            shared.editBrushIndex = prevBrushIndex;
            shared.editBrushStart = shared.editBrushPosition;

            //HelpOverlay.Push(helpOverlayID);
            inGame.Cursor3D.Rep = Cursor3D.Visual.Pointy;
            inGame.Cursor3D.DiffuseColor = Vector4.One;

            terrainSpeed = Common.Xml.XmlOptionsData.TerrainSpeed; // just cache this.

            isInAction = false;
        }

        /// <summary>
        /// Override this to provide a place to clean up
        /// anything that needs it on a per-use basis.
        /// </summary>
        public virtual void OnDeactivate()
        {
            prevBrushIndex = shared.editBrushIndex;

            // Deactivate any pickers.
            if (PickerX != null)
            {
                PickerX.Active = false;
                PickerX = null;
            }
            if (PickerY != null)
            {
                PickerY.Active = false;
                PickerY = null;
            }

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
        }

        #endregion Public

        #region Internal

        private void NotifyTerrainActivity()
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

        private void CheckStretchMode()
        {
            Brush2DManager.Brush2D brush = Brush2DManager.GetBrush(shared.editBrushIndex);
            if ((brush != null)
                &&
                ((brush.Type & Brush2DManager.BrushType.StretchedAll) != 0))
            {
                /// We're in stretch now. Is that new?
                if (!InStretchMode)
                {
                    StretchPhase = Phase.Open;
                    InStretchMode = true;
                }
                //if (PickerXInUse || PickerYInUse)
                //{
                //    shared.editBrushStart = shared.editBrushPosition;
                //    StretchPhase = Phase.Open;
                //}
            }
            else
            {
                InStretchMode = false;
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

            Brush2DManager.Brush2D brush
                = Brush2DManager.GetBrush(shared.editBrushIndex);

            if (HelpOverlayMagicBrushID != null && brush.Type == Brush2DManager.BrushType.Selection)
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
            /// The new pointy cursor seems always helpful, even if slightly
            /// redundant with the brush.
            inGame.ShowCursor();

            Brush2DManager.Brush2D brush = Brush2DManager.GetBrush(shared.editBrushIndex);
            if (brush == null)
            {
                //inGame.ShowCursor();
                return;
            }

            bool isSelection = (brush.Type & Brush2DManager.BrushType.Selection) != 0;
            bool isStretch = (brush.Type & Brush2DManager.BrushType.StretchedAll) != 0;

            //in touch mode, make sure we don't have the selection on unless the touch is active
            if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
            {
                if (TouchInput.TouchCount != 1 || TouchInput.WasMultiTouch || !Boku.InGame.inGame.TouchEdit.HasNonUITouch())
                {
                    isSelection = false;
                }
            }

            if (isSelection)
            {
                //inGame.ShowCursor();
                inGame.Terrain.MakeSelection(shared.editBrushPosition);
            }
            else
            {
                inGame.Terrain.EndSelection();
                //if (!alwaysShowCursor)
                //    inGame.HideCursor();
            }
        }

        /// <summary>
        /// Pass off commands as appropriate to the right handlers, depending on
        /// the current game pad and brush states.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected void ProcessTriggers(
            Terrain.EditMode left,
            Terrain.EditMode middle,
            Terrain.EditMode right)
        {
            /// If any of the toolbox pickers are active, disable our selves.
            if (shared.ToolBox.PickersActive || Boku.InGame.inGame.mouseEditUpdateObj.ToolBox.PickersActive)
            {
                return;
            }

            Brush2DManager.Brush2D brush = Brush2DManager.GetBrush(shared.editBrushIndex);
            bool isSelection = (brush != null) && ((brush.Type & Brush2DManager.BrushType.Selection) != 0);

            // Size timer is used for changing selection brush size. Required for both mouse and touch
            sizeTimer += Time.WallClockFrameSeconds;

            if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
            {
                if (isSelection)
                {
                    ProcessTouchSelection(left, middle, right);
                }
                else
                {
                    ProcessTouch(left, middle, right);
                }
            }

            else if(GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
            {
                if (isSelection)
                {
                    ProcessSelection(left,middle,right);
                }
                else if (InStretchMode)
                {
                    ProcessStretched(left,middle,right);
                }
                else
                {
                    ProcessPoint(left,middle,right);
                }
            }
        }

        public const float kSmallRate = 0.1f;
        public const float kSmallMove = 0.1f;
        public const float kSmallMoveSq = kSmallMove * kSmallMove;

        protected virtual void ProcessStretchedOpen()
        {
            if (StretchPhase == Phase.Open)
            {
                shared.editBrushStart = shared.editBrushPosition;
                StretchPhase = Phase.Ready;
            }
        }
        protected virtual void ProcessStretchedReady()
        {
            if (StretchPhase == Phase.Ready)
            {
                endPos = shared.editBrushPosition;

                if (MouseInput.Left.IsPressed || MouseInput.Right.IsPressed)
                {
                    BeginStretchGoing();
                    StretchPhase = Phase.Going;
                }
                ProcessBButton();
            }
        }
        protected virtual void ProcessStretchedGoing(
            Terrain.EditMode left,
            Terrain.EditMode middle,
            Terrain.EditMode right)
        {
            if (StretchPhase == Phase.Going)
            {
                float distSq = Vector2.DistanceSquared(endPos, shared.editBrushPosition);
                bool gamePadMoved = distSq > kSmallMoveSq;
                bool mouseMoved = MouseInput.PrevPosition != MouseInput.Position;
                bool moved = GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse
                    ? mouseMoved
                    : gamePadMoved;
                if (moved
                    && !MouseInput.Left.IsPressed
                    && !MouseInput.Right.IsPressed)
                {
                    StretchPhase = Phase.Ready;
                    shared.editBrushStart = shared.editBrushPosition;
                    RightAudioEnd();
                    MiddleAudioEnd();
                    LeftAudioEnd();
                }
                else
                {
                    if (MouseInput.Left.WasReleased)
                    {
                        float editSpeed = 1.0f;
                        inGame.Terrain.RenderToHeightMap(
                            shared.editBrushIndex,
                            shared.editBrushStart,
                            shared.editBrushPosition,
                            shared.editBrushRadius,
                            left,
                            editSpeed * editSpeed * terrainSpeed);

                        LeftAudioStart();
                    }
                    else
                    {
                        LeftAudioEnd();
                    }

                    if (MouseInput.Middle.WasReleased)
                    {
                        float editSpeed = 1.0f;
                        inGame.Terrain.RenderToHeightMap(
                            shared.editBrushIndex,
                            shared.editBrushStart,
                            shared.editBrushPosition,
                            shared.editBrushRadius,
                            middle,
                            editSpeed * editSpeed * terrainSpeed);

                        MiddleAudioStart();
                    }
                    else
                    {
                        MiddleAudioEnd();
                    }


                    if (MouseInput.Right.WasReleased)
                    {
                        float editSpeed = 1.0f;
                        inGame.Terrain.RenderToHeightMap(
                            shared.editBrushIndex,
                            shared.editBrushStart,
                            shared.editBrushPosition,
                            shared.editBrushRadius,
                            right,
                            editSpeed * editSpeed * terrainSpeed);

                        RightAudioStart();
                    }
                    else
                    {
                        RightAudioEnd();
                    }
                    shared.heightMapModified = true;
                    Boku.InGame.IsLevelDirty = true;
                }
                ProcessBButton();
            }
        }
        protected virtual void ProcessBButton()
        {
            float distSq = Vector2.DistanceSquared(shared.editBrushStart, shared.editBrushPosition);

            GamePadInput pad = GamePadInput.GetGamePad0();
            if (Actions.Cancel.IsPressed)
            {
                StretchPhase = Phase.Open;
                Actions.Cancel.IgnoreUntilReleased();
            }
            if (MouseInput.Left.WasPressed
                && !MouseInput.Middle.IsPressed
                && !MouseInput.Right.IsPressed)
            {
                StretchPhase = Phase.Open;
            }
        }
        protected virtual void BeginStretchGoing()
        {
            inGame.Terrain.LevelStart = Terrain.GetTerrainHeightFlat(shared.editBrushStart);
            inGame.Terrain.LevelHeight = Terrain.GetTerrainHeightFlat(shared.editBrushPosition);
        }
        /// <summary>
        /// Apply commands in stretched brush mode.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected virtual void ProcessStretched(
            Terrain.EditMode left,
            Terrain.EditMode middle,
            Terrain.EditMode right)
        {
            ProcessStretchedOpen();
            ProcessStretchedReady();
            ProcessStretchedGoing(left, middle, right);
        }

        private float sizeTimer = 0.0f;
        private void CheckResizeSelection()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();
            const float kMinResizeTime = 0.25f;
            if (sizeTimer >= kMinResizeTime)
            {
                if (Actions.BrushLarger.WasPressedOrRepeat)
                {
                    inGame.Terrain.ExpandSelection();
                    sizeTimer = 0.0f;
                }

                if (Actions.BrushSmaller.WasPressedOrRepeat)
                {
                    inGame.Terrain.ShrinkSelection();
                    sizeTimer = 0.0f;
                }
            }
        }
        /// <summary>
        /// Apply commands in material select mode.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected virtual void ProcessSelection(
            Terrain.EditMode left,
            Terrain.EditMode middle,
            Terrain.EditMode right)
        {
            isInAction = false;

            CheckResizeSelection();
            if (MouseInput.Left.IsPressed)
            {
                float rate = 1.0f;
                inGame.Terrain.RenderToSelection(left, rate * rate * terrainSpeed);
                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;

                LeftAudioStart();
                isInAction = true;
            }
            else
            {
                LeftAudioEnd();
            }

            if (MouseInput.Middle.IsPressed)
            {
                float rate = 1.0f;
                inGame.Terrain.RenderToSelection(middle, rate * rate * terrainSpeed);
                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;

                MiddleAudioStart();
                isInAction = true;
            }
            else
            {
                MiddleAudioEnd();
            }

            if (MouseInput.Right.IsPressed)
            {
                float rate = 1.0f;
                inGame.Terrain.RenderToSelection(right, rate * rate * terrainSpeed);
                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;

                RightAudioStart();
                isInAction = true;
            }
            else
            {
                RightAudioEnd();
            }

            if (Actions.MaterialFabric.WasPressed)
            {
                Actions.MaterialFabric.ClearAllWasPressedState();

                inGame.Terrain.SetSelectionToFabric();

                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;
            }
            if (Actions.MaterialCubic.WasPressed)
            {
                Actions.MaterialCubic.ClearAllWasPressedState();

                inGame.Terrain.SetSelectionToCubic();

                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;
            }
        }
        
        public bool oldInAction=false;

        /// <summary>
        /// Apply commands to single brush region.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected virtual void ProcessPoint(
            Terrain.EditMode left,
            Terrain.EditMode middle,
            Terrain.EditMode right)
        {
            float rate = 0.5f;

            isInAction = false;

            if (MouseInput.Left.IsPressed)
            {
                inGame.Terrain.RenderToHeightMap(
                    shared.editBrushIndex,
                    shared.editBrushPosition,
                    shared.editBrushRadius,
                    left,
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

            // Smooth
            if (MouseInput.Middle.IsPressed)
            {
                inGame.Terrain.RenderToHeightMap(
                    shared.editBrushIndex,
                    shared.editBrushPosition,
                    shared.editBrushRadius,
                    middle,
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
            if (MouseInput.Right.IsPressed)
            {
                inGame.Terrain.RenderToHeightMap(
                    shared.editBrushIndex,
                    shared.editBrushPosition,
                    shared.editBrushRadius,
                    right,
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

            if(oldInAction !=isInAction)
            {
#if !NETFX_CORE
                Console.WriteLine(this.ToString() + ":" + 
                    ":" + MouseInput.Left.IsPressed.ToString() +
                    ":" + MouseInput.Middle.IsPressed.ToString() +
                    ":" + MouseInput.Right.IsPressed.ToString()
                    );
#endif
                oldInAction=isInAction;
            }
        }
		
        /// <summary>
        /// Apply commands in material select mode.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected virtual void ProcessTouchSelection(
            Terrain.EditMode left,
            Terrain.EditMode middle,
            Terrain.EditMode right)
        {
            isInAction = false;

            if (TouchInput.TouchCount==1 && TouchInput.Touches[0].phase != TouchPhase.Ended && 
                shared.currentTouchAction != ToolBar.TouchControls.BrushActionIDs.NUMBER_OF_Buttons && shared.editBrushAllowedForTouch)
            {
                if ((shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baBrushMore) ||
                     (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baBrushLess))
                {
                    const float kMinResizeTime = 0.3f;
                    if (sizeTimer >= kMinResizeTime)
                    {
                        sizeTimer = 0.0f;
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
                    inGame.Terrain.RenderToSelection(left, rate * rate * terrainSpeed);
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
                    inGame.Terrain.RenderToSelection(middle, rate * rate * terrainSpeed);
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
                    inGame.Terrain.RenderToSelection(right, rate * rate * terrainSpeed);
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
        protected virtual void ProcessTouch(
            Terrain.EditMode left,
            Terrain.EditMode middle,
            Terrain.EditMode right)
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
                        shared.editBrushIndex,
                        shared.editBrushPosition,
                        shared.editBrushRadius,
                        left,
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
                        shared.editBrushIndex,
                        shared.editBrushPosition,
                        shared.editBrushRadius,
                        right,
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
                        shared.editBrushIndex,
                        shared.editBrushPosition,
                        shared.editBrushRadius,
                        middle,
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
                        shared.editBrushIndex,
                        shared.editBrushPosition,
                        shared.editBrushRadius,
                        right,
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
