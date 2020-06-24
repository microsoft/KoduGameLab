
// Uncomment to debug pushing and popping of input sets.
//#define DEBUG_INPUT_SETS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using KoiX.Managers;
using KoiX.UI;


namespace KoiX.Input
{
    /// <summary>
    /// Event manager for handling keyboard and mouse events.
    /// GamePad is here but we don't break out individual buttons.
    /// </summary>
    public partial class InputEventManager : InputEventHandler
    {
        public enum Event
        {
            MouseLeftDown,
            MouseMiddleDown,
            MouseRightDown,
            MouseLeftUp,
            MouseMiddleUp,
            MouseRightUp,
            MouseMove,          // Only when mouse changes position.
            MousePosition,      // Position of mouse every frame even if not changing.
            MouseHover,
            MouseWheel,

            Keyboard,           // XNA based
            WinFormsKeyboard,   // Windows Forms based

            Touch,              // Raw touch.

            Tap,                // Gestures
            DoubleTap,
            Hold,
            OnePointDrag,
            TwoPointDrag,

            GamePad,
        }

        #region Members

        InputEventHandlerListSet curSet;

        InputEventHandler mouseFocusObject;     // Object that owns mouse focus.
        InputEventHandler keyboardFocusObject;  // Object that owns keyboard focus.
        InputEventHandler mouseHitObject;       // Object "hit" by mouse.

        InputEventHandler touchFocusObject;     // Object that owns touch focus.
        InputEventHandler touchHitObject;       // Object "hit" by first touch location.

        double touchFocusObjectTime = 0;        // When was focus taken.  Will be 0 if focus is null.

        #endregion

        #region Accessors

        /// <summary>
        /// The InputEventHandler that currently owns the mouse focus.
        /// </summary>
        public InputEventHandler MouseFocusObject
        {
            get { return mouseFocusObject; }
            set { mouseFocusObject = value; }
        }

        /// <summary>
        /// The InputEventHandler that currently owns the Keyboard focus.
        /// Not sure if this is really useful.  For now, on a dialog we
        /// know that there can only be one Focused object.  So, for text
        /// boxes, etc, we should only look at input if Focused.
        /// </summary>
        public InputEventHandler KeyboardFocusObject
        {
            get { return keyboardFocusObject; }
            set { keyboardFocusObject = value; }
        }

        /// <summary>
        /// The InputEventHandler currently under the mouse.
        /// </summary>
        public InputEventHandler MouseHitObject
        {
            get { return mouseHitObject; }
            set { mouseHitObject = value; }
        }

        /// <summary>
        /// The InputEventHandler that currently has claimed the touch focus.
        /// 
        /// TODO (****)  This doesn't really seem to be used right now.  It's not
        /// set and cleared as different handlers take focus.  I think the problem
        /// is that it's difficult to know when to release focus when there are 
        /// multiple possible touches.  (with one touch we could (?) just look at
        /// the up/down events to claim and release this.
        /// So far, everything works without this but it should probably be kept
        /// since it can be used to claim touch events first.
        /// </summary>
        public InputEventHandler TouchFocusObject
        {
            get { return touchFocusObject; }
            set 
            { 
                if(touchFocusObject != value)
                {
                    touchFocusObject = value;
                    touchFocusObjectTime = value == null ? 0 : Time.WallClockTotalSeconds;
                }
            }
        }

        /// <summary>
        /// This is the wall clock time when the TouchFocusObject was
        /// most recently set.
        /// If TouchFocusObject is null, this will be 0.
        /// </summary>
        public double TouchFocusObjectTime
        {
            get { return touchFocusObjectTime; }
        }

        /// <summary>
        /// The InputEventHandler currently under the mouse.
        /// </summary>
        public InputEventHandler TouchHitObject
        {
            get { return touchHitObject; }
            set { touchHitObject = value; }
        }

        #endregion

        // c'tor
        public InputEventManager()
        {
            // Start with an empty set.
            PushSet();
        }   // end of InputEventManager c'tor

        public void PushSet()
        {
#if DEBUG_INPUT_SETS
            Debug.Print("--push set");
#endif
            curSet = InputEventHandlerListSet.PushSet();
        }

        public void PopSet()
        {
#if DEBUG_INPUT_SETS
            Debug.Print("--pop set");
#endif
            InputEventHandlerListSet.PopSet();
            curSet = InputEventHandlerListSet.CurSet;
        }

        /// <summary>
        /// Allow an object to register itself for an event.
        /// Note that we insert objects are the beginning of the list.  This results
        /// in later entries getting first access.  We need this so when a dialog
        /// registers it is at the beginning of the list.
        /// </summary>
        public void RegisterForEvent(InputEventHandler obj, InputEventManager.Event input)
        {
            switch (input)
            {
                case Event.MouseLeftDown:
                    if (!curSet.MouseLeftDownList.Contains(obj))
                    {
                        curSet.MouseLeftDownList.Insert(0, obj);
                    }
                    break;
                case Event.MouseMiddleDown:
                    if (!curSet.MouseMiddleDownList.Contains(obj))
                    {
                        curSet.MouseMiddleDownList.Insert(0, obj);
                    }
                    break;
                case Event.MouseRightDown:
                    if (!curSet.MouseRightDownList.Contains(obj))
                    {
                        curSet.MouseRightDownList.Insert(0, obj);
                    }
                    break;
                case Event.MouseLeftUp:
                    if (!curSet.MouseLeftUpList.Contains(obj))
                    {
                        curSet.MouseLeftUpList.Insert(0, obj);
                    }
                    break;
                case Event.MouseMiddleUp:
                    if (!curSet.MouseMiddleUpList.Contains(obj))
                    {
                        curSet.MouseMiddleUpList.Insert(0, obj);
                    }
                    break;
                case Event.MouseRightUp:
                    if (!curSet.MouseRightUpList.Contains(obj))
                    {
                        curSet.MouseRightUpList.Insert(0, obj);
                    }
                    break;
                case Event.MouseMove:
                    if (!curSet.MouseMoveList.Contains(obj))
                    {
                        curSet.MouseMoveList.Insert(0, obj);
                    }
                    break;
                case Event.MousePosition:
                    if (!curSet.MousePositionList.Contains(obj))
                    {
                        curSet.MousePositionList.Insert(0, obj);
                    }
                    break;
                case Event.MouseHover:
                    if (!curSet.MouseHoverList.Contains(obj))
                    {
                        curSet.MouseHoverList.Insert(0, obj);
                    }
                    break;
                case Event.MouseWheel:
                    if (!curSet.MouseWheelList.Contains(obj))
                    {
                        curSet.MouseWheelList.Insert(0, obj);
                    }
                    break;
                case Event.Keyboard:
                    if (!curSet.KeyboardList.Contains(obj))
                    {
                        curSet.KeyboardList.Insert(0, obj);
                    }
                    break;
                case Event.WinFormsKeyboard:
                    if (!curSet.WinFormsKeyboardList.Contains(obj))
                    {
                        curSet.WinFormsKeyboardList.Insert(0, obj);
                    }
                    break;
                case Event.Touch:
                    if (!curSet.TouchList.Contains(obj))
                    {
                        curSet.TouchList.Insert(0, obj);
                    }
                    break;
                case Event.Tap:
                    if (!curSet.TapList.Contains(obj))
                    {
                        curSet.TapList.Insert(0, obj);
                    }
                    break;
                case Event.DoubleTap:
                    if (!curSet.DoubleTapList.Contains(obj))
                    {
                        curSet.DoubleTapList.Insert(0, obj);
                    }
                    break;
                case Event.Hold:
                    if (!curSet.HoldList.Contains(obj))
                    {
                        curSet.HoldList.Insert(0, obj);
                    }
                    break;
                case Event.OnePointDrag:
                    if (!curSet.OnePointDragList.Contains(obj))
                    {
                        curSet.OnePointDragList.Insert(0, obj);
                    }
                    break;
                case Event.TwoPointDrag:
                    if (!curSet.TwoPointDragList.Contains(obj))
                    {
                        curSet.TwoPointDragList.Insert(0, obj);
                    }
                    break;
                case Event.GamePad:
                    if (!curSet.GamePadList.Contains(obj))
                    {
                        curSet.GamePadList.Insert(0, obj);
                    }
                    break;
            }   // end of switch on event
        }   // end of InputEventManager RegisterForEvent()

        /// <summary>
        /// Allow an object to unregister itself for an event.
        /// Call UnregisterForAllEvents() if you just want to unregister all.
        /// This version is primarily used when an InputEventHandler is dynamically
        /// registering and unregistering during a scene.
        /// </summary>
        public void UnregisterForEvent(InputEventHandler obj, InputEventManager.Event input)
        {
            switch (input)
            {
                case Event.MouseLeftDown:
                    curSet.MouseLeftDownList.Remove(obj);
                    break;
                case Event.MouseMiddleDown:
                    curSet.MouseMiddleDownList.Remove(obj);
                    break;
                case Event.MouseRightDown:
                    curSet.MouseRightDownList.Remove(obj);
                    break;
                case Event.MouseLeftUp:
                    curSet.MouseLeftUpList.Remove(obj);
                    break;
                case Event.MouseMiddleUp:
                    curSet.MouseMiddleUpList.Remove(obj);
                    break;
                case Event.MouseRightUp:
                    curSet.MouseRightUpList.Remove(obj);
                    break;
                case Event.MouseMove:
                    curSet.MouseMoveList.Remove(obj);
                    break;
                case Event.MousePosition:
                    curSet.MousePositionList.Remove(obj);
                    break;
                case Event.MouseHover:
                    curSet.MouseHoverList.Remove(obj);
                    break;
                case Event.MouseWheel:
                    curSet.MouseWheelList.Remove(obj);
                    break;
                case Event.Keyboard:
                    curSet.KeyboardList.Remove(obj);
                    break;
                case Event.WinFormsKeyboard:
                    curSet.WinFormsKeyboardList.Remove(obj);
                    break;
                case Event.Touch:
                    curSet.TouchList.Remove(obj);
                    break;
                case Event.Tap:
                    curSet.TapList.Remove(obj);
                    break;
                case Event.DoubleTap:
                    curSet.DoubleTapList.Remove(obj);
                    break;
                case Event.Hold:
                    curSet.HoldList.Remove(obj);
                    break;
                case Event.OnePointDrag:
                    curSet.OnePointDragList.Remove(obj);
                    break;
                case Event.TwoPointDrag:
                    curSet.TwoPointDragList.Remove(obj);
                    break;
                case Event.GamePad:
                    curSet.GamePadList.Remove(obj);
                    break;
            }   // end of switch on event
        }   // end of InputEventManager UnregisterForEvent()   

        /// <summary>
        /// Unregisters an object from all events it has previsously 
        /// registerd for.
        /// </summary>
        /// <param name="obj"></param>
        public void UnregisterForAllEvents(InputEventHandler obj)
        {
            curSet.MouseLeftDownList.Remove(obj);
            curSet.MouseMiddleDownList.Remove(obj);
            curSet.MouseRightDownList.Remove(obj);
            curSet.MouseLeftUpList.Remove(obj);
            curSet.MouseMiddleUpList.Remove(obj);
            curSet.MouseRightUpList.Remove(obj);
            curSet.MouseMoveList.Remove(obj);
            curSet.MousePositionList.Remove(obj);
            curSet.MouseHoverList.Remove(obj);
            curSet.MouseWheelList.Remove(obj);

            curSet.KeyboardList.Remove(obj);
            curSet.WinFormsKeyboardList.Remove(obj);

            curSet.TouchList.Remove(obj);
            curSet.TapList.Remove(obj);
            curSet.DoubleTapList.Remove(obj);
            curSet.HoldList.Remove(obj);
            curSet.OnePointDragList.Remove(obj);
            curSet.TwoPointDragList.Remove(obj);

            curSet.GamePadList.Remove(obj);

            // If current focus object, also clear.
            if (MouseFocusObject == obj)
            {
                MouseFocusObject = null;
            }
            if (KeyboardFocusObject == obj)
            {
                KeyboardFocusObject = null;
            }
            if (TouchFocusObject == obj)
            {
                TouchFocusObject = null;
            }

        }   // end of UnregisterForAllEvents()

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            // If the MouseFocusObject isn't null on LeftDown, that probably indicates
            // a bug where the focus is being grabbed and not properly released.
            // This seems to be happening with the SortMenu in the LoadLevelScene.
            // Alternate possibility is that we just ran the program and have yet
            // to touch the mouse at all.  In this case, it seems like the system 
            // is still translating touch inputs into mouse inputs even though
            // we've told it not to.  Argh.
            // Right now the fix involves telling the mouse system to ignore inputs on
            // frames where the touch system has input.  This mostly works.   Maybe 
            // part of the issue is that the concept of "frame" is different for mouse
            // and touch compared to the game.  Maybe, only touch clears the ignore flag,
            // the mouse should wait until after the next state update to start looking
            // at input again?
            //Debug.Assert(MouseFocusObject == null);

            // On left down we shouldn't already have a MouseFocusObject so force this behaviour.
            MouseFocusObject = null; 

            /*
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MouseLeftDownList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseLeftDownEvent(input))
                    {
                        //MouseFocusObject = obj;
                        return true;
                    }
                }
            }
            */


            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MouseLeftDownList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseLeftDownEvent(input))
                    {
                        //MouseFocusObject = obj;
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MouseLeftDownList.Count; i++)
            {
                InputEventHandler obj = curSet.MouseLeftDownList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseLeftDownEvent(input))
                    {
                        //MouseFocusObject = obj;
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMouseLeftDownEvent()

        public override bool ProcessMouseMiddleDownEvent(MouseInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MouseMiddleDownList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseMiddleDownEvent(input))
                    {
                        //MouseFocusObject = obj;
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MouseMiddleDownList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseMiddleDownEvent(input))
                    {
                        //MouseFocusObject = obj;
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MouseMiddleDownList.Count; i++)
            {
                InputEventHandler obj = curSet.MouseMiddleDownList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseMiddleDownEvent(input))
                    {
                        //MouseFocusObject = obj;
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMouseMiddleDownEvent()

        public override bool ProcessMouseRightDownEvent(MouseInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MouseRightDownList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseRightDownEvent(input))
                    {
                        //MouseFocusObject = obj;
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MouseRightDownList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseRightDownEvent(input))
                    {
                        //MouseFocusObject = obj;
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MouseRightDownList.Count; i++)
            {
                InputEventHandler obj = curSet.MouseRightDownList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseRightDownEvent(input))
                    {
                        //MouseFocusObject = obj;
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMouseRightDownEvent()

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MouseLeftUpList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseLeftUpEvent(input))
                    {
                        //MouseFocusObject = null;
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MouseLeftUpList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseLeftUpEvent(input))
                    {
                        //MouseFocusObject = null;
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MouseLeftUpList.Count; i++)
            {
                InputEventHandler obj = curSet.MouseLeftUpList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseLeftUpEvent(input))
                    {
                        //MouseFocusObject = null;
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMouseLeftUpEvent()

        public override bool ProcessMouseMiddleUpEvent(MouseInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MouseMiddleUpList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseMiddleUpEvent(input))
                    {
                        //MouseFocusObject = null;
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MouseMiddleUpList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseMiddleUpEvent(input))
                    {
                        //MouseFocusObject = null;
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MouseMiddleUpList.Count; i++)
            {
                InputEventHandler obj = curSet.MouseMiddleUpList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseMiddleUpEvent(input))
                    {
                        //MouseFocusObject = null;
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMouseMiddleUpEvent()

        public override bool ProcessMouseRightUpEvent(MouseInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MouseRightUpList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseRightUpEvent(input))
                    {
                        //MouseFocusObject = null;
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MouseRightUpList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseRightUpEvent(input))
                    {
                        //MouseFocusObject = null;
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MouseRightUpList.Count; i++)
            {
                InputEventHandler obj = curSet.MouseRightUpList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseRightUpEvent(input))
                    {
                        //MouseFocusObject = null;
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMouseRightUpEvent()

        /// <summary>
        /// Mouse Move events fire only when the mouse changes position.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override bool ProcessMouseMoveEvent(MouseInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MouseMoveList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseMoveEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MouseMoveList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseMoveEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MouseMoveList.Count; i++)
            {
                InputEventHandler obj = curSet.MouseMoveList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseMoveEvent(input))
                    {
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMouseMoveEvent()

        /// <summary>
        /// Mouse Position events fire every frame.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override bool ProcessMousePositionEvent(MouseInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MousePositionList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMousePositionEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MousePositionList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMousePositionEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MousePositionList.Count; i++)
            {
                InputEventHandler obj = curSet.MousePositionList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMousePositionEvent(input))
                    {
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMousePositionEvent()

        public override bool ProcessMouseHoverEvent(MouseInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MouseHoverList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseHoverEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MouseHoverList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseHoverEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MouseHoverList.Count; i++)
            {
                InputEventHandler obj = curSet.MouseHoverList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseHoverEvent(input))
                    {
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMouseHoverEvent()

        public override bool ProcessMouseWheelEvent(MouseInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (MouseFocusObject != null && curSet.MouseWheelList.Contains(MouseFocusObject))
            {
                InputEventHandler obj = MouseFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseWheelEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.MouseWheelList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseWheelEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.MouseWheelList.Count; i++)
            {
                InputEventHandler obj = curSet.MouseWheelList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessMouseWheelEvent(input))
                    {
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessMouseWheelEvent()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            // Steal Alt-Enter for full screen toggle.
            if (input.Key == Keys.Enter && input.Alt)
            {
            }

            // Give first shot at the event to the focus object if it's interested.
            if (KeyboardFocusObject != null && curSet.KeyboardList.Contains(KeyboardFocusObject))
            {
                InputEventHandler obj = KeyboardFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessKeyboardEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.KeyboardList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessKeyboardEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.KeyboardList.Count; i++)
            {
                InputEventHandler obj = curSet.KeyboardList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessKeyboardEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Give the DialogManager a last chance.  Note we're assuming that the
            // DialogManager is the last InputEventHandler in the list.  This is used
            // for tabbing among multiple, non-modal dialogs.
            if (curSet.KeyboardList.Count > 0)
            {
                InputEventHandler dm = curSet.KeyboardList[curSet.KeyboardList.Count - 1] as DialogManagerX;
                if (dm != null)
                {
                    if (dm.ProcessKeyboardEvent(input))
                    {
                        return true;
                    }
                }
            }

            return false;
        }   // end of InputEventManager ProcessKeyboardEvent()

        public override bool ProcessWinFormsKeyboardEvent(KeyInput input)
        {
            // Give first shot at the event to the focus object if it's interested.
            if (KeyboardFocusObject != null && curSet.WinFormsKeyboardList.Contains(KeyboardFocusObject))
            {
                InputEventHandler obj = KeyboardFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessWinFormsKeyboardEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (mouseHitObject != null && curSet.WinFormsKeyboardList.Contains(mouseHitObject))
            {
                InputEventHandler obj = mouseHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessWinFormsKeyboardEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            for (int i = 0; i < curSet.WinFormsKeyboardList.Count; i++)
            {
                InputEventHandler obj = curSet.WinFormsKeyboardList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessWinFormsKeyboardEvent(input))
                    {
                        return true;
                    }
                }
            }

            // Give the DialogManager a last chance.  Note we're assuming that the
            // DialogManager is the last InputEventHandler in the list.
            if (curSet.KeyboardList.Count > 0)
            {
                InputEventHandler dm = curSet.KeyboardList[curSet.KeyboardList.Count - 1] as DialogManagerX;
                if (dm != null)
                {
                    if (dm.ProcessWinFormsKeyboardEvent(input))
                    {
                        return true;
                    }
                }
            }
            else
            {
            }

            return false;
        }   // end of InputEventManager ProcessWinFormsKeyboardEvent()

        public override bool ProcessTouchEvent(List<TouchSample> touchSampleList)
        {
            bool result = false;

            // If we've got no samples, just leave.
            if (touchSampleList.Count == 0)
            {
                return result;
            }

            // Give first shot at the event to the focus object if it's interested.
            if (TouchFocusObject != null && curSet.TouchList.Contains(TouchFocusObject))
            {
                InputEventHandler obj = TouchFocusObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessTouchEvent(touchSampleList))
                    {
                        result = true;
                    }
                }
            }

            // Next, offer it up to the hit object if it's interested.
            if (!result && TouchHitObject != null && curSet.TouchList.Contains(TouchHitObject))
            {
                InputEventHandler obj = TouchHitObject as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessTouchEvent(touchSampleList))
                    {
                        result = true;
                    }
                }
            }

            // Finally, see if anyone else cares.
            if (!result)
            {
                for (int i = 0; i < curSet.TouchList.Count; i++)
                {
                    InputEventHandler obj = curSet.TouchList[i] as InputEventHandler;
                    if (obj != null)
                    {
                        // Figure out if obj is also a widget.  If so, also check
                        // that it's active.  The reason we do this is that when a
                        // dialog is closed by a tap, the next frame, when we've
                        // returned to the previous input set, the items may get
                        // a 'Released' event before they have been actived.  We
                        // don't want to force all widgets to check themselves so
                        // we do this here.
                        // We only do this for touch samples.
                        BaseWidget widget = obj as BaseWidget;
                        if (widget == null || widget.Active)
                        {
                            if (obj.ProcessTouchEvent(touchSampleList))
                            {
                                result = true;
                            }
                        }
                    }
                }
            }

            return result;
        }   // end of ProcessTouchEvent()

        //
        //  Gesture Events.
        //

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            bool result = false;

            for (int i = 0; i < curSet.TapList.Count; i++)
            {
                InputEventHandler obj = curSet.TapList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessTouchTapEvent(gesture))
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        public override bool ProcessTouchDoubleTapEvent(TapGestureEventArgs gesture)
        {
            bool result = false;

            for (int i = 0; i < curSet.DoubleTapList.Count; i++)
            {
                InputEventHandler obj = curSet.DoubleTapList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessTouchDoubleTapEvent(gesture))
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        public override bool ProcessTouchHoldEvent(TapGestureEventArgs gesture)
        {
            bool result = false;

            for (int i = 0; i < curSet.HoldList.Count; i++)
            {
                InputEventHandler obj = curSet.HoldList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessTouchHoldEvent(gesture))
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        public override bool ProcessTouchOnePointDragEvent(OnePointDragGestureEventArgs gesture)
        {
            bool result = false;

            for (int i = 0; i < curSet.OnePointDragList.Count; i++)
            {
                InputEventHandler obj = curSet.OnePointDragList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessTouchOnePointDragEvent(gesture))
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        public override bool ProcessTouchTwoPointDragEvent(TwoPointDragGestureEventArgs gesture)
        {
            bool result = false;

            for (int i = 0; i < curSet.TwoPointDragList.Count; i++)
            {
                InputEventHandler obj = curSet.TwoPointDragList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessTouchTwoPointDragEvent(gesture))
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            bool result = false;

            // Give the input to whichever object is currently in focus.
            InputEventHandler obj = null;
            if(DialogManagerX.CurrentFocusDialog != null)
            {
                obj = DialogManagerX.CurrentFocusDialog.CurrentFocusWidget;
            }
            if (curSet.GamePadList.Contains(obj))
            {
                if (obj.ProcessGamePadEvent(pad))
                {
                    return true;
                }
            }

            // If input was not taken, show to rest of the list.
            for (int i = 0; i < curSet.GamePadList.Count; i++)
            {
                obj = curSet.GamePadList[i] as InputEventHandler;
                if (obj != null)
                {
                    if (obj.ProcessGamePadEvent(pad))
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        public struct MouseLocation
        {
            public Point point;
            public Ray ray;
        }

        /// <summary>
        /// Fill in a MouseLocation struct for pick testing.
        /// </summary>
        /// <param name="camera">Camera used for calculation of 3d ray.  May be null if you only care about 2d hit testing.</param>
        /// <returns></returns>
        public MouseLocation GetMouseLocation(Boku.Common.Camera camera)
        {
            MouseLocation result;
            result.point = LowLevelMouseInput.Position;
            if (camera != null)
            {
                Vector3 position = new Vector3(result.point.X, result.point.Y, 0);
                position = KoiLibrary.GraphicsDevice.Viewport.Unproject(position, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

                Vector3 dir = position - camera.From;
                dir.Normalize();
                result.ray = new Ray(camera.From, dir);
            }
            else
            {
                result.ray = new Ray();
            }

            return result;
        }   // end of InputEventManager GetMouseLocation()


    }   // end of class InputEventManager

}   // end of namespace KoiX.Input
