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

namespace Boku.Scenes.InGame.Tools
{
    /// <summary>
    /// Base class for tools which fit into the terrain editing toolbox.
    /// </summary>
    public abstract class BaseTool
    {
        #region Members
        // This is the description string that will appear next to the tool icon when the toolbox is open.
        // Add an entry to Strings.cs in the tools struct and add the actual string to Strings.Xml.
        private string description = null;

        // This is the ID of the help overlay associated with the tool.  Add the text strings for the 
        // help overlay in HelpOverlay.Xml.
        private string helpOverlayID = null;
        private string helpOverlayMagicBrushID = null;
        private string helpOverlayStartID = null;
        private string helpOverlayGoingID = null;

        // This is the texture to be used as this tool's icon.
        private string iconTextureName = null;

        private bool active = false;
        private bool starting = true;   // Set to true when tool is activated.  Used to debounce buttons.

        protected int prevBrushIndex = 0;   // Let the tools that care about brushes remember what they were using.

        // Helpful references.
        protected Boku.InGame inGame = null;
        protected Boku.InGame.Shared shared = null;

        private float leftRate = 0.0f;
        private float rightRate = 0.0f;
        private bool leftWasPressed = false;
        private bool rightWasPressed = false;
        private float aButtonRate = 0.0f;
        private bool aButtonWasPressed = false;

        private float terrainSpeed = 1.0f;

        private bool inStretchMode = false;
        private Vector2 endPos = Vector2.Zero;
        private bool bButtonExits = false;

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
        /// The text string displayed next to the tool's icon.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
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
        /// Texture2D for icon in tool select menu.
        /// </summary>
        public string IconTextureName
        {
            get { return iconTextureName; }
            set { iconTextureName = value; }
        }

        /// <summary>
        /// Are we still waiting for the buttons/triggers to be released?
        /// </summary>
        public bool DebouncePending
        {
            get
            {
                UpdateRates();
                if (starting)
                {
                    if (!Actions.Select.IsPressed)
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
        /// Current strength of the left trigger.
        /// </summary>
        protected float LeftRate
        {
            get { return leftRate; }
        }

        /// <summary>
        /// Current strength of the right trigger.
        /// </summary>
        protected float RightRate
        {
            get { return rightRate; }
        }

        /// <summary>
        /// Return true if left trigger rate just went from zero to non-zero.
        /// Note pad.LeftTriggerButton.WasPressed is true if left trigger goes < 0.5 => > 0.5.
        /// </summary>
        protected bool LeftWasPressed
        {
            get { return leftWasPressed; }
        }
        /// <summary>
        /// Return true if right trigger rate just went from zero to non-zero.
        /// Note pad.RightTriggerButton.WasPressed is true if right trigger goes < 0.5 => > 0.5.
        /// </summary>
        protected bool RightWasPressed
        {
            get { return rightWasPressed; }
        }
        /// <summary>
        /// Is the left trigger at a non-zero value?
        /// </summary>
        protected bool LeftTriggerOn
        {
            get { return LeftRate > kSmallRate; }
        }
        /// <summary>
        /// Is the right trigger at a non-zero value?
        /// </summary>
        protected bool RightTriggerOn
        {
            get { return RightRate > kSmallRate; }
        }
        /// <summary>
        /// Magnitude of A Button action.
        /// </summary>
        protected float AButtonRate
        {
            get { return aButtonRate;  }
        }
        /// <summary>
        /// Is the middle button (A Button) activated?
        /// </summary>
        protected bool AButtonOn
        {
            get { return aButtonRate > 0; }
        }
        protected bool AButtonWasPressed
        {
            get { return aButtonWasPressed; }
        }
        #endregion 

        #region Public
        /// <summary>
        /// Common update code, expected to be overriden but base Update called as well.
        /// </summary>
        public virtual void Update()
        {
            // Only update if active and the toolbox isn't changing.
            if (Active)
            {
                NotifyTerrainActivity();

                CheckStretchMode();
            }

            shared.editBrushSizeActive = true;

        }   // end of BaseTool Update()

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
            starting = true;
            StretchPhase = Phase.Open;

            shared.editBrushStart = shared.editBrushPosition;

            HelpOverlay.Push(helpOverlayID);
            inGame.Cursor3D.Rep = Cursor3D.Visual.Pointy;
            inGame.Cursor3D.Hidden = false;
            inGame.Cursor3D.DiffuseColor = Vector4.One;

            terrainSpeed = Common.Xml.XmlOptionsData.TerrainSpeed; // just cache this.
        }

        /// <summary>
        /// Override this to provide a place to clean up
        /// anything that needs it on a per-use basis.
        /// </summary>
        public virtual void OnDeactivate()
        {
            inGame.ShowCursor();
            inGame.Cursor3D.Rep = Cursor3D.Visual.Edit;
            inGame.Cursor3D.DiffuseColor = new Vector4(0.5f, 0.9f, 0.8f, 0.3f);

            inGame.Terrain.EndSelection();
            VirtualMap.SuppressWaterUpdate = false;

            shared.editBrushSizeActive = false;

            HelpOverlay.Pop();

            // Shut down all the terrain editing sounds just to be sure.
            Foley.StopPaint();
            Foley.StopEarthDown();
            Foley.StopEarthUp();
            Foley.StopEraseLand();
            Foley.StopLowerWater();
            Foley.StopRaiseWater();
        }
        #endregion Public

        #region Internal
        private void NotifyTerrainActivity()
        {
            VirtualMap.SuppressWaterUpdate =
                RightTriggerOn
                || LeftTriggerOn
                || AButtonOn;
        }
        private void CheckStretchMode()
        {
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            if (brush != null && brush.IsLinear)
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
            Brush2DManager.Brush2D brush
                = Brush2DManager.GetActiveBrush();

            if (InStretchMode)
            {
                string helpStart = HelpOverlayStartID == null ? HelpOverlayID : HelpOverlayStartID;
                string helpGoing = HelpOverlayGoingID == null ? HelpOverlayID : HelpOverlayGoingID;
                if (!bButtonExits)
                {
                    SetOverlay(helpStart);
                }
                else
                {
                    SetOverlay(helpGoing);
                }
            }
            else
            {
                if (HelpOverlayMagicBrushID != null && brush.Shape == Brush2DManager.BrushShape.Magic)
                {
                    SetOverlay(HelpOverlayMagicBrushID);
                }
                else
                {
                    SetOverlay(HelpOverlayID);
                }
            }
        }

        protected void SetOverlay(string overlay)
        {
            if (HelpOverlay.Peek() != overlay)
            {
                HelpOverlay.Pop();
                HelpOverlay.Push(overlay);
            }
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

            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            if (brush == null)
            {
                //inGame.ShowCursor();
                return;
            }

            bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;
            bool isLinearBrush = brush.IsLinear;

            if (isMagicBrush)
            {
                inGame.Cursor3D.Hidden = false;
                inGame.Terrain.MakeSelection(shared.editBrushPosition);
            }
            else
            {
                inGame.Cursor3D.Hidden = !alwaysShowCursor;
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
            Terrain.EditMode rightMode,
            Terrain.EditMode aButton,
            Terrain.EditMode leftMode)
        {
            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

            if (isMagicBrush)
            {
                ProcessSelection(
                    rightMode,
                    aButton,
                    leftMode);
            }
            else if (InStretchMode)
            {
                ProcessStretched(
                    rightMode,
                    aButton,
                    leftMode);
            }
            else
            {
                ProcessPoint(
                    rightMode,
                    aButton,
                    leftMode);
            }
        }

        public const float kSmallRate = 0.1f;
        public const float kSmallMove = 0.1f;
        public const float kSmallMoveSq = kSmallMove * kSmallMove;

        /// <summary>
        /// Update the strengths of the left and right trigger effects.
        /// </summary>
        protected void UpdateRates()
        {
            if (!starting)
            {
                bool rightOn = RightTriggerOn;
                GamePadInput pad = GamePadInput.GetGamePad0();
                rightRate = pad.RightTrigger;
                rightRate += inGame.MouseEdit.RightTrigger;
                if (rightRate < kSmallRate)
                {
                    rightRate = 0.0f;
                }
                rightWasPressed = RightTriggerOn && !rightOn;

                bool leftOn = LeftTriggerOn;
                leftRate = pad.LeftTrigger;
                leftRate += inGame.MouseEdit.LeftTrigger;
                if (leftRate < kSmallRate)
                {
                    leftRate = 0.0f;
                }
                leftWasPressed = LeftTriggerOn && !leftOn;

                bool aButtonOn = AButtonOn;
                aButtonRate = pad.ButtonA.IsPressed || KeyboardInputX.IsPressed(Keys.A) ? 1.0f : 0.0f;
                float mouseA = inGame.MouseEdit.MiddleTrigger;
                aButtonRate = Math.Max(aButtonRate, mouseA);
                if (aButtonRate < kSmallRate)
                {
                    aButtonRate = 0.0f;
                }
                aButtonWasPressed = AButtonOn && !aButtonOn;
            }
            else
            {
                rightRate = leftRate = aButtonRate = 0.0f;
                rightWasPressed = leftWasPressed = aButtonWasPressed = false;
            }
        }

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

                float mouseA = inGame.MouseEdit.MiddleTrigger;
                if (RightTriggerOn || LeftTriggerOn || AButtonOn)
                {
                    BeginStretchGoing();
                    StretchPhase = Phase.Going;
                }
                ProcessBButton();
            }
        }
        protected virtual void ProcessStretchedGoing(
            Terrain.EditMode rightMode,
            Terrain.EditMode aButton,
            Terrain.EditMode leftMode)
        {
            if (StretchPhase == Phase.Going)
            {
                float distSq = Vector2.DistanceSquared(endPos, shared.editBrushPosition);
                bool gamePadMoved = distSq > kSmallMoveSq;
                bool mouseMoved = LowLevelMouseInput.DeltaPosition != Point.Zero;
                bool moved = KoiLibrary.LastTouchedDeviceIsKeyboardMouse
                    ? mouseMoved
                    : gamePadMoved;
                if (moved
                    && !RightTriggerOn
                    && !AButtonOn
                    && !LeftTriggerOn)
                {
                    StretchPhase = Phase.Ready;
                    shared.editBrushStart = shared.editBrushPosition;
                    RightAudioEnd();
                    MiddleAudioEnd();
                    LeftAudioEnd();
                }
                else
                {
                    if (RightTriggerOn)
                    {
                        float editSpeed = RightRate;
                        inGame.Terrain.RenderToHeightMap(
                            shared.editBrushStart,
                            shared.editBrushPosition,
                            shared.editBrushRadius,
                            rightMode,
                            editSpeed * editSpeed * terrainSpeed);

                        RightAudioStart();
                    }
                    else
                    {
                        RightAudioEnd();
                    }
                    if (AButtonOn)
                    {
                        float editSpeed = aButtonRate;
                        inGame.Terrain.RenderToHeightMap(
                            shared.editBrushStart,
                            shared.editBrushPosition,
                            shared.editBrushRadius,
                            aButton,
                            editSpeed * editSpeed * terrainSpeed);

                        MiddleAudioStart();
                    }
                    else
                    {
                        MiddleAudioEnd();
                    }
                    if (LeftTriggerOn)
                    {
                        float editSpeed = LeftRate;
                        inGame.Terrain.RenderToHeightMap(
                            shared.editBrushStart,
                            shared.editBrushPosition,
                            shared.editBrushRadius,
                            leftMode,
                            editSpeed * editSpeed * terrainSpeed);

                        LeftAudioStart();
                    }
                    else
                    {
                        LeftAudioEnd();
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
            bButtonExits = distSq < kSmallMoveSq;

            GamePadInput pad = GamePadInput.GetGamePad0();
            if(Actions.Cancel.IsPressed)
            {
                if (!bButtonExits)
                {
                    StretchPhase = Phase.Open;
                    Actions.Cancel.IgnoreUntilReleased();
                }
            }
            if (LowLevelMouseInput.Left.WasPressed
                && !LeftTriggerOn
                && !RightTriggerOn
                && !AButtonOn)
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
            Terrain.EditMode rightMode,
            Terrain.EditMode aButton,
            Terrain.EditMode leftMode)
        {
            ProcessStretchedOpen();
            ProcessStretchedReady();
            ProcessStretchedGoing(rightMode, aButton, leftMode);
        }

        private float sizeTimer = 0.0f;
        private void CheckResizeSelection()
        {
            GamePadInput pad = GamePadInput.GetGamePad0();
            sizeTimer += Time.WallClockFrameSeconds;
            const float kMinResizeTime = 0.25f;
            if (sizeTimer >= kMinResizeTime)
            {
                sizeTimer = kMinResizeTime;

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
            Terrain.EditMode rightMode,
            Terrain.EditMode aButton,
            Terrain.EditMode leftMode)
        {
            CheckResizeSelection();
            if (RightRate > kSmallRate)
            {
                float rate = RightRate;
                inGame.Terrain.RenderToSelection(rightMode, rate * rate * terrainSpeed);
                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;

                RightAudioStart();
            }
            else
            {
                RightAudioEnd();
            }
            if (AButtonOn)
            {
                float rate = aButtonRate;
                inGame.Terrain.RenderToSelection(aButton, rate * rate * terrainSpeed);
                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;

                MiddleAudioStart();
            }
            else
            {
                MiddleAudioEnd();
            }
            if (LeftRate > kSmallRate)
            {
                float rate = LeftRate;
                inGame.Terrain.RenderToSelection(leftMode, rate * rate * terrainSpeed);
                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;

                LeftAudioStart();
            }
            else
            {
                LeftAudioEnd();
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

        /// <summary>
        /// Apply commands to single brush region.
        /// </summary>
        /// <param name="rightMode"></param>
        /// <param name="aButton"></param>
        /// <param name="leftMode"></param>
        protected virtual void ProcessPoint(
            Terrain.EditMode rightMode,
            Terrain.EditMode aButton,
            Terrain.EditMode leftMode)
        {
            // Up
            if (RightRate > kSmallRate)
            {
                float rate = RightRate;
                inGame.Terrain.RenderToHeightMap(
                    shared.editBrushPosition,
                    shared.editBrushRadius,
                    rightMode,
                    rate * rate * terrainSpeed);
                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;

                RightAudioStart();
            }
            else
            {
                RightAudioEnd();
            }

            // Smooth
            if (AButtonOn)
            {
                float rate = aButtonRate;
                inGame.Terrain.RenderToHeightMap(
                    shared.editBrushPosition,
                    shared.editBrushRadius,
                    aButton,
                    rate * rate * terrainSpeed);
                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;

                MiddleAudioStart();
            }
            else
            {
                MiddleAudioEnd();
            }

            // Down
            if (LeftRate > kSmallRate)
            {
                float rate = LeftRate;
                inGame.Terrain.RenderToHeightMap(
                    shared.editBrushPosition,
                    shared.editBrushRadius,
                    leftMode,
                    rate * rate * terrainSpeed);
                shared.heightMapModified = true;
                Boku.InGame.IsLevelDirty = true;

                LeftAudioStart();
            }
            else
            {
                LeftAudioEnd();
            }

        }
        #endregion Internal

    }   // end of class BaseTool

}   // end of namespace Boku.Scenes.InGame.Tools
