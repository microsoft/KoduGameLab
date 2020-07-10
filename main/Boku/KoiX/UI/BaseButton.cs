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
using KoiX.Input;
using KoiX.Managers;


namespace KoiX.UI
{
    /// <summary>
    /// Base class for Buttons.  Adds button specific code.
    /// 
    /// TODO (****) Need to rethink this and make it (or another base class)
    /// work for various button types:
    ///     latchable vs non-latchable
    ///     one-shot vs continuous (handled via WasPressed vs IsPressed?)
    /// </summary>
    public abstract class BaseButton : BaseWidget
    {
        #region Members

        string targetScene;
        SceneManager.Transition transition = SceneManager.Transition.Cut;

        bool latchable = false;                 // If latchable, the pressed state sticks until it is forced out.
        bool killParentDialogOnSelect = false;  // If true, then the owning parent dialog is killed when this
                                                // button is selected even if the OnSelect delgate is null.

        #endregion

        #region Accessors

        /// <summary>
        /// If this button's OnSelect callback is null then this button
        /// will instead switch scenes to this scene.
        /// </summary>
        public string TargetScene
        {
            get { return targetScene; }
            set { targetScene = value; }
        }

        /// <summary>
        /// If using TargetScene, this is the transition to use.
        /// Defaults to Cut.
        /// </summary>
        public SceneManager.Transition Transition
        {
            get { return transition; }
            set { transition = value; }
        }

        /// <summary>
        /// Does this button have a valid target?  A valid target means either having 
        /// an OnChange delegate or a scene name to switch to on triggering.
        /// </summary>
        public bool HasValidTarget
        {
            get { return onChange != null || !string.IsNullOrEmpty(targetScene); }
        }

        /// <summary>
        /// Changes the button behaviour so that the Pressed state
        /// is sticky and persists until it is overridden.
        /// </summary>
        public bool Latchable
        {
            get { return latchable; }
            set { latchable = value; }
        }

        public bool KillParentDialogOnSelect
        {
            get { return killParentDialogOnSelect; }
            set { killParentDialogOnSelect = value; }
        }

        #endregion

        #region Public

        public BaseButton(BaseDialog parentDialog, Callback OnChange, ThemeSet theme = null, string id = null, object data = null)
            : base(parentDialog, OnChange: OnChange, theme: theme, id: id, data: data)
        {
        }

        public override void RegisterForInputEvents()
        {
            // Register to get left down mouse event.  
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            // Also register for Keyboard.  If this button has focus and enter is pressed that's the same as a mouse click.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);

            // We use tap do decide if the action should be taken.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
            // We use raw touch for pressed, moved and unpressed events to show selected state.
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Touch);

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
        }

        public override void UnregisterForInputEvents()
        {
            // Unregister for events
            KoiLibrary.InputEventManager.UnregisterForAllEvents(this);
        }

        /// <summary>
        /// Call this button's OnChange handler if not null.  If 
        /// null, try to transition to the target scene.
        /// </summary>
        public void OnButtonSelect()
        {
            if (KillParentDialogOnSelect)
            {
                DialogManagerX.KillDialog(ParentDialog);
            }

            if (onChange != null)
            {
                onChange(this);
            }
            else if (!string.IsNullOrEmpty(targetScene))
            {
                SceneManager.SwitchToScene(targetScene, transition: transition, transitionTime: Theme.TwitchTime);
            }
            else if(!KillParentDialogOnSelect)
            {
                Debug.Assert(false, "This button should do something.");
            }
        }   // end of OnButtonSelect()

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

                    // Change state.
                    Selected = true;

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

                // Change state back to normal.
                if (!latchable)
                {
                    Selected = false;
                }

                // Only call onPress if mouse is still over button.
                if (KoiLibrary.InputEventManager.MouseHitObject == this)
                {
                    OnButtonSelect();
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
                if (latchable)
                {
                    Selected = true;
                }
                OnButtonSelect();
                return true;
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            // Did this gesture hit us?
            if (gesture.HitObject == this)
            {
                if (latchable)
                {
                    Selected = true;
                }
                OnButtonSelect();
                return true;
            }

            return base.ProcessTouchTapEvent(gesture);
        }   // end of ProcessTouchTapEvent()

        /// <summary>
        /// Process raw touch events.  Just use this to show selected state.
        /// </summary>
        /// <param name="touchSampleList"></param>
        /// <returns></returns>
        public override bool ProcessTouchEvent(List<TouchSample> touchSampleList)
        {
            Debug.Assert(Active);

            for (int i = 0; i < touchSampleList.Count; i++)
            {
                TouchSample ts = touchSampleList[i];

                /*
                if (this.UniqueNum == 163)
                {
                    Debug.Print("frame : " + Time.FrameCounter.ToString());
                    Debug.Print("button hit object : " + (ts.HitObject == null ? "null" : ts.HitObject.UniqueNum.ToString()));
                }
                */

                if (KoiLibrary.InputEventManager.TouchHitObject == this && ts.State == Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Pressed)
                {
                    KoiLibrary.InputEventManager.TouchFocusObject = this;
                    if (ts.HitObject == this)
                    {
                        Selected = true;
                    }
                    else
                    {
                        if (!latchable)
                        {
                            Selected = false;
                        }
                    }
                }

                // Release always clears selected state and nothing else since we're
                // using Tap to determine if the button's action should be taken.
                if (ts.State == Microsoft.Xna.Framework.Input.Touch.TouchLocationState.Released)
                {
                    if (KoiLibrary.InputEventManager.TouchFocusObject == this)
                    {
                        KoiLibrary.InputEventManager.TouchFocusObject = null;
                    }

                    if (!latchable)
                    {
                        Selected = false;
                    }
                }


            }   // end of loop over samples.

            return base.ProcessTouchEvent(touchSampleList);
        }   // end of ProcessTouchEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (InFocus)
            {
                if (pad.ButtonA.WasPressed)
                {
                    if (latchable)
                    {
                        Selected = true;
                    }
                    OnButtonSelect();
                    pad.ButtonA.ClearAllWasPressedState();
                    return true;
                }
            }

            return base.ProcessGamePadEvent(pad);
        }
        #endregion

        #region Internal
        #endregion

    }   // end of class BaseButton

}   // end of namespace KoiX.UI
