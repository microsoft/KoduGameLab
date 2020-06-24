
using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using System.Diagnostics;

using KoiX;

namespace KoiX.Input
{

    /// <summary>
    /// An abstract class which is used as a base class for any objects which
    /// expect to handle user input events (mouse & keyboard).  Classes which 
    /// derive from this one only need to implement/override the methods they 
    /// care about.
    /// </summary>
    public abstract class InputEventHandler : ArbitraryComparable
    {
        /// <summary>
        /// Causes all processing to stop when this InputEventHandler is found
        /// even if it's not consuming input.  Only used by DialogManager to
        /// prevent any active UI underneath a modal dialog from getting input.
        /// </summary>
        public bool IsModalDialog = false;

        public virtual void RegisterForInputEvents()
        {
        }   // end of RegisterForInputEvents()

        public virtual void UnregisterForInputEvents()
        {
            // Unregister.
            KoiLibrary.InputEventManager.UnregisterForAllEvents(this);
        }   // end of UnregisterForInputEvents()

        /// <summary>
        /// Process a mouse event.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>
        /// true indicates that the object has "consumed" the event
        /// false indicates that the event has been ignored.
        /// </returns>
        public virtual bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            return false;
        }

        public virtual bool ProcessMouseMiddleDownEvent(MouseInput input)
        {
            return false;
        }

        public virtual bool ProcessMouseRightDownEvent(MouseInput input)
        {
            return false;
        }

        public virtual bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            return false;
        }

        public virtual bool ProcessMouseMiddleUpEvent(MouseInput input)
        {
            return false;
        }

        public virtual bool ProcessMouseRightUpEvent(MouseInput input)
        {
            return false;
        }

        public virtual bool ProcessMouseMoveEvent(MouseInput input)
        {
            return false;
        }

        public virtual bool ProcessMousePositionEvent(MouseInput input)
        {
            return false;
        }

        public virtual bool ProcessMouseHoverEvent(MouseInput input)
        {
            return false;
        }

        public virtual bool ProcessMouseWheelEvent(MouseInput input)
        {
            return false;
        }

        /// <summary>
        /// Process a keyboard event.  This works at a higher level
        /// in that it only triggers on key-down.  For button press
        /// style handling use LowLevelKeyboardInput.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>
        /// true indicates that the object has "consumed" the event
        /// false indicates that the event has been ignored.
        /// </returns>
        public virtual bool ProcessKeyboardEvent(KeyInput input)
        {
            return false;
        }

        /// <summary>
        /// Keyboard handler which gets its input from the underlying Windows Form.
        /// This should be used to text input and is the only way to get the input
        /// to be processed properly for international keyboards.
        /// 
        /// Note that it does not handle arrow keys, back, delete, home or end
        /// correctly so those need to be handled with ProcessKeyboardEvent.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual bool ProcessWinFormsKeyboardEvent(KeyInput input)
        {
            return false;
        }

        /// <summary>
        /// Touch handler which gets raw touch events.  Any consumed events should be 
        /// removed from the collection.
        /// </summary>
        /// <param name="touchSampleList"></param>
        /// <returns>True if any of the touches have been consumed.</returns>
        public virtual bool ProcessTouchEvent(List<TouchSample> touchSampleList)
        {
            return false;
        }

        public virtual bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            return false;
        }

        public virtual bool ProcessTouchDoubleTapEvent(TapGestureEventArgs gesture)
        {
            return false;
        }

        public virtual bool ProcessTouchHoldEvent(TapGestureEventArgs gesture)
        {
            return false;
        }

        public virtual bool ProcessTouchOnePointDragEvent(OnePointDragGestureEventArgs gesture)
        {
            return false;
        }

        public virtual bool ProcessTouchTwoPointDragEvent(TwoPointDragGestureEventArgs gesture)
        {
            return false;
        }


        public virtual bool ProcessGamePadEvent(GamePadInput pad)
        {
            return false;
        }


        public virtual InputEventHandler HitTest(Vector2 hitLocation)
        {
            return null;
        }

    }   // end of class InputEventHandler

}   // end of namespace KoiX.Input
