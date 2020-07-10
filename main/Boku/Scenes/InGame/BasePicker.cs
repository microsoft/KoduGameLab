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

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Common.Gesture;

namespace Boku
{
    /// <summary>
    /// Base class for pickers which are used by the edit tools.
    ///
    /// Tools which want to use a picker should set the tool's Active to true
    /// when they start up and back to false when they exit.  Note that
    /// setting Active to true just causes the picker to listen for being made
    /// visible.  The picker only displays when Visible is set to true.
    /// </summary>
    public abstract class BasePicker : INeedsDeviceReset
    {
        public delegate void OnSetMaterial(int index);
        public delegate void OnPickMaterial(int index);
        public delegate int OnGetMaterial();
        public delegate int OnSampleMaterial();

        protected Matrix worldMatrix = Matrix.Identity;

        public static Texture2D reticuleTexture = null;
        protected UIGrid grid = null;

        private Point previousChoice = Point.Zero;

        protected bool active = false;
        protected bool hidden = true;               // If hidden then don't render and ignore input. 

        protected float alpha = 0.0f;               // Used for pickers that use transparency to fade.
        protected double startFadeTime = 0.0;       // Time when picker was hidden.
        protected double fadeTime = 0.0;            // Seconds to fade when going hidden.

        protected string helpOverlay = @"BasePicker";
        protected string altHelpOverlay = @"BasePicker";

        protected bool useAltOverlay = false;       // Use the alt help overlay.  This gets reset to false when
                                                    // this picker is deactivated so must be reset on each activation.
        private OnSetMaterial onSetType = delegate(int idx) { };
        private OnPickMaterial onPickType = delegate(int idx) { };
        private OnGetMaterial onGetType = delegate() { return -1; };
        private OnSampleMaterial onSampleType = delegate() { return -1; };

        protected double kPreFadeTime = 2.0;        // # seconds to wait on inaction before fading in trigger button icons.
        protected double kFadeTime = 0.5;           // # second to fade up.
        protected double lastChangedTime = 0;       // Time when user last changed something.

        #region Accessors

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
                        // Starting up.
                        grid.Active = true;
                        grid.Dirty = true;

                        // Start with the selection index set to the first element.  
                        SetDefaultSelection();
                    }
                    else
                    {
                        // Shutting down.
                        grid.Active = false;
                        Hidden = true;
                        useAltOverlay = false;
                    }
                }
            }
        }

        /// <summary>
        /// When the picker is hidden, nothing is rendered and it ignores input.
        /// </summary>
        public virtual bool Hidden
        {
            get { return hidden; }
            set
            {
                if (hidden != value)
                {
                    hidden = value;
                    if (!hidden)
                    {
                        Alpha = 1.0f;   // Pop to fully visible.
                        previousChoice = grid.SelectionIndex;
                        lastChangedTime = Time.WallClockTotalSeconds;
                    }
                    else
                    {
                        startFadeTime = Time.WallClockTotalSeconds;

                        // Restart current tool to debounce.
                        InGame.inGame.shared.ToolBox.RestartCurrentTool();
                    }
                }
            }
        }

        private float Alpha
        {
            get { return alpha; }
            set
            {
                if (alpha != value)
                {
                    alpha = value;
                    UpdateWithNewAlpha();
                }
            }
        }

        /// <summary>
        /// Use the alt help overlay set.
        /// </summary>
        public bool UseAltOverlay
        {
            get { return useAltOverlay; }
            set { useAltOverlay = value; }
        }

        public OnSetMaterial OnSetType
        {
            get { return onSetType; }
            set { onSetType = value; }
        }
        public OnPickMaterial OnPickType
        {
            get { return onPickType; }
            set { onPickType = value; }
        }
        public OnGetMaterial OnGetType
        {
            get { return onGetType; }
            set { onGetType = value; }
        }
        public OnSampleMaterial OnSampleType
        {
            get { return onSampleType; }
            set { onSampleType = value; }
        }
        #endregion Accessors

        // c'tor
        public BasePicker(OnSetMaterial onSet, OnGetMaterial onGet)
        {
            this.onSetType = onSet;
            this.onGetType = onGet;
        }   // end of BasePicker c'tor

        /// <summary>
        /// The alpha value has changed, make any needed updates.
        /// </summary>
        protected virtual void UpdateWithNewAlpha()
        {
        }   // end of BasePicker UpdateWithNewAlpha()

        /// <summary>
        /// Set the default selection in the grid when picker starts up.  
        /// </summary>
        protected virtual void SetDefaultSelection()
        {
            grid.SelectionIndex = new Point(0, 0);
        }   // end of BasePicker SetDefaultSelection()

        /// <summary>
        /// Change which element is in focus by incrementing the selection index.
        /// </summary>
        protected virtual void IncrementFocus()
        {
            Point selection = grid.SelectionIndex;
            if (selection.X < grid.ActualDimensions.X - 1)
            {
                selection.X = (selection.X + 1) % grid.ActualDimensions.X;
                grid.SelectionIndex = selection;

                UpdateIndex();
            }
        }   // end of BasePicker IncrementFocus()

        /// <summary>
        /// Change which element is in focus by decrementing the selection index.
        /// </summary>
        protected virtual void DecrementFocus()
        {
            Point selection = grid.SelectionIndex;
            if (selection.X > 0)
            {
                selection.X = (selection.X - 1 + grid.ActualDimensions.X) % grid.ActualDimensions.X;
                grid.SelectionIndex = selection;

                UpdateIndex();
            }
        }   // end of BasePicker DecrementFocus()

        /// <summary>
        /// The focus has changed, make any needed updates to external consumers.
        /// </summary>
        protected virtual void UpdateIndex()
        {
            lastChangedTime = Time.WallClockTotalSeconds;
        }   // end of BasePicker UpdateIndex()

        public void SelectCurrentChoice()
        {
            Hidden = true;
            OnPickType(grid.SelectionIndex.X);
        }   // end of BasePicker SelectCurrentChoice()

        public void RestorePreviousChoice()
        {
            Hidden = true;

            // Cycle up or down until we line back up the the choice we started with.
            while (grid.SelectionIndex.X > previousChoice.X)
            {
                DecrementFocus();
            }

            while (grid.SelectionIndex.X < previousChoice.X)
            {
                IncrementFocus();
            }


        }   // end of BasePicker RestorePreviousChoice()

        public virtual void Update(PerspectiveUICamera camera)
        {
            if (active)
            {
                if (hidden)
                {
                    // Note, even though we're hidden we may still be rendering our fade out.
                    double elapsedTime = Time.WallClockTotalSeconds - startFadeTime;
                    if (elapsedTime >= fadeTime)
                    {
                        Alpha = 0.0f;
                    }
                    else
                    {
                        Alpha = 1.0f - (float)(elapsedTime / fadeTime);
                    }
                }
                else
                {
                    // Not hidden, so respond to user input.

                    int scroll = MouseInput.ScrollWheel - MouseInput.PrevScrollWheel;

                    if (Actions.PickerRight.WasPressedOrRepeat || scroll > 0)
                    {
                        Actions.PickerRight.ClearAllWasPressedState();
                        DecrementFocus();
                    }
                    if (Actions.PickerLeft.WasPressedOrRepeat || scroll < 0)
                    {
                        Actions.PickerLeft.ClearAllWasPressedState();
                        IncrementFocus();
                    }

                    if (Actions.Select.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState();

                        SelectCurrentChoice();
                        Foley.PlayPressA();
                    }

                    if (Actions.Cancel.WasPressedOrRepeat)
                    {
                        Actions.Cancel.ClearAllWasPressedState();

                        RestorePreviousChoice();
                        Foley.PlayBack();
                    }
                    bool handled = false;
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        handled = HandleTouchInput(camera);
                    }
                    else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    {
                        handled = HandleMouseInput(camera);
                    }

                    if (!handled)
                    {
                        // If the user clicked but didn't hit any of the picker elements, close the picker.
                        // If alt is pressed, they must be in eyedropper mode.
                        if ( (MouseInput.Left.WasPressed && !KeyboardInput.AltIsPressed) ||
                            (TouchGestureManager.Get().TapGesture.WasRecognized))
                        {
                            SelectCurrentChoice();
                            Foley.PlayPressA();
                        }
                        else if (Actions.Sample.WasPressed ||
                            MouseEdit.TriggerSample() || TouchEdit.TriggerSample())
                        {
                            Actions.Sample.ClearAllWasPressedState();

                            int t = OnSampleType();
                            if (t >= 0)
                            {
                                OnSetType(t);
                                Point selection = grid.SelectionIndex;
                                selection.X = t;
                                grid.SelectionIndex = selection;
                                Foley.PlayCut();
                            }
                            else
                            {
                                Foley.PlayNoBudget();
                            }
                        }
                    }

                    grid.Update(ref worldMatrix);
                }
            }   // end if active

        }   // end of BasePicker Update()

        public abstract bool HandleTouchInput(Camera camera);
        public abstract bool HandleMouseInput(Camera camera);

        public virtual void Render(Camera camera)
        {
            // TODO (****) What to do here???
            //Fx.Luz.SetToEffect(true); // disable point lights
            if (active && alpha > 0.0f)
            {
                //string oldRig = BokuGame.bokuGame.shaderGlobals.PushLightRig(Fx.ShaderGlobals.UIRigName);
                grid.Render(camera);
                //BokuGame.bokuGame.shaderGlobals.PopLightRig(oldRig);
            }

            // TODO (****) What to do here???
            //Fx.Luz.SetToEffect(false); // re-enable point lights
        }   // end of BasePicker Render()

        //
        // Not used since we're controlling the grid externally
        // and have set the grid to ignore input.
        //
        public void OnSelect(UIGrid grid)
        {
        }   // end of OnSelect

        public void OnCancel(UIGrid grid)
        {
        }   // end of OnCancel()


        public virtual void LoadContent(bool immediate)
        {
            if (reticuleTexture == null)
            {
                reticuleTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\Tools\SelectionReticule");
            }

            BokuGame.Load(grid, immediate);
        }   // end of BasePicker LoadContent()

        public virtual void InitDeviceResources(GraphicsDevice device)
        {
        }

        public virtual void UnloadContent()
        {
            BokuGame.Release(ref reticuleTexture);

            BokuGame.Unload(grid);
        }   // end of BasePicker UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class BasePicker

}   // end of namespace Boku


