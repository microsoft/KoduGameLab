
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Audio;
using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.Input;

namespace Boku.UI2D
{
    public delegate void UIGridEvent(UIGrid grid);

    /// <summary>
    /// A grid layout UI menu.  This can be set up as a 2d grid or as
    /// an either vertical or horizontal 1d list.  The c'tor takes a 
    /// Point type which defines the initial dimensions for the grid.  
    /// If an element is added outside of this initial size the grid
    /// is grown to match.
    /// 
    /// While the UIGrid doesn't itself need to worry about device reset we
    /// we put the interface here so that the owning object need only make
    /// a single call rather than a call for every element in the grid.
    /// </summary>
    public class UIGrid : INeedsDeviceReset
    {
        public event UIGridEvent Select;
        public event UIGridEvent Cancel;

        /// <summary>
        /// If set, these callbacks override default handling for these input events and instead
        /// give control to the caller when these events occur. i.e., we detect them here, but
        /// expect them to be acted on elsewhere.
        /// </summary>
        protected event UIGridEvent movedLeft;
        protected event UIGridEvent movedRight;
        protected event UIGridEvent movedUp;
        protected event UIGridEvent movedDown;

        protected Point maxDimensions;          // The size the grid that has been allocated.
        protected Point actualDimensions;       // The size of the grid that has been filled in with elements.
        protected UIGridElement[,] grid = null;

        // Local transform for this grid.
        protected Matrix localMatrix = Matrix.Identity;
        // Local combined with parent.
        protected Matrix worldMatrix = Matrix.Identity;

        protected bool active = false;
        protected bool renderWhenInactive = false;    // Forces rendering even if inactive.

        protected Point focusIndex;   // Which element currently has focus.

        protected bool dirty = true;  // Has something been added, moved, etc?

        protected bool renderEndsIn = false;    // Render the grid elements starting at the ends
                                                // and go inward toward the selected element.
        protected bool scrolling = false;       // If this is false, the elements have a static position
                                                // on the screen.  If this is true then the current focus
                                                // element is centered and the whole grid is scrolled as
                                                // the selection changes.
        protected Vector3 scrollOffset = new Vector3(); // Offset caused by scrolling.
        private Vector3 twitchScrollOffset = new Vector3();

        protected bool slopOffset = false;  // If scrolling, don't force the focus object to always be
                                            // at the center.

        protected bool wrap = false;        // Allow selection to wrap when it gets to the end.

        protected bool ignoreFocusChanged = false;  // Set to true when externally keeping track of focus.

        protected bool ignoreInput = false; // Acts as if the grid never has input focus.  Useful 
                                            // for grids that need to be driven from outside.

        protected bool alwaysReadInput = false; // Acts as if the grid always has input focus.

        protected bool useLeftStick = true;     // Use left stick to navigate menu.  True by default.
        protected bool useDPad = true;          // Use DPad to navigate menu.  True by default.
        protected bool useTriggers = false;     // Use trigger buttons for left/right input.  False by default.
        protected bool useShoulders = false;    // Use shoulder buttons for left/right input.  False by default.
        protected bool useRightStick = false;   // Use the right stick to navigate menu.  False by default.
        protected bool useKeyboard = true;      // Use the keyboard to navigate menu, select, and back out. True by default.
        protected bool useTab = false;          // Use the tab key to cycle through the list.  Obviously only works with 1-d grids.
        protected bool useMouseScrollWheel = false; // Use the mouse scroll wheel to go through the elements.  Only works with 1-d grids.

        protected Vector2 spacing = new Vector2(0.1f);  // Spacing between grid elements.  Allows for non-uniform element sizes.

        protected bool isDragging = false;

        // Remaining residual velocity from a drag.
        protected float residualVelocity = 0.0f;
        protected bool hasResidualVelocity = false;

        public CommandMap commandMap = new CommandMap("App.UiGrid");    // Used just to reserve our place in the stack.

        public static float MIN_DRAG_START = 10.0f;

        #region Accessors

        /// <summary>
        /// Grids are by default set up inactive.  Setting active to true causes the
        /// grid to push itself onto the input focus stack and start rendering itself.
        /// If a second Grid is activated while the first is still active then both
        /// will render but only the second will have focus until it is deactivated.
        /// At that point focus will return to the first grid.  This allows grids to 
        /// be "layered".
        /// </summary>
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
                        CommandStack.Push(commandMap);
                    }
                    else
                    {
                        CommandStack.Pop(commandMap);
                    }
                }
            }
        }
        /// <summary>
        /// Forces rendering even when inactive.  
        /// Defaults to false.
        /// </summary>
        public bool RenderWhenInactive
        {
            get { return renderWhenInactive; }
            set { renderWhenInactive = value; }
        }
        public Matrix LocalMatrix
        {
            get { return localMatrix; }
            set { localMatrix = value; }
        }
        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
            set { worldMatrix = value; }
        }
        /// <summary>
        /// Spacing between grid elements.
        /// </summary>
        public Vector2 Spacing
        {
            get { return spacing; }
            set { spacing = value; }
        }
        /// <summary>
        /// Tells the grid to render its elements from either end of the 
        /// array inward toward the selected object, rendering the selected
        /// object last.  Only works correctly for 1d grids.
        /// </summary>
        public bool RenderEndsIn
        {
            get { return renderEndsIn; }
            set { renderEndsIn = value; }
        }

        /// <summary>
        /// If this is false, the elements have a static position
        /// on the screen.  If this is true then the current focus
        /// element is centered and the whole grid is scrolled as
        /// the selection changes.
        /// </summary>
        public bool Scrolling
        {
            get { return scrolling; }
            set { scrolling = value; scrollOffset = Vector3.Zero; dirty = true; }
        }

        public float ScrollSpeed { get; set; }

        /// <summary>
        /// Yes, this needs a better name.  What this does is that for scrolling lists
        /// it allows the focus object to not always be at the center of the display
        /// when it's at the beginning or end of the list.
        /// </summary>
        public bool SlopOffset
        {
            get { return slopOffset; }
            set { slopOffset = value; }
        }
        public Point SelectionIndex
        {
            get { return focusIndex; }
            set
            {
                if (focusIndex != value)
                {
                    focusIndex = value;
                    focusChanged = true;
                }
            }
        }
        public UIGridElement SelectionElement
        {
            get { return grid[focusIndex.X, focusIndex.Y]; }
        }
        /// <summary>
        /// This is the size of the grid as determined by the elements that have been added.  
        /// This will be less than or equal to the allocated size of the grid.  Setting this
        /// value should be done carefully if at all.
        /// </summary>
        public Point ActualDimensions
        {
            get { return actualDimensions; }
            set { actualDimensions = value; }
        }

        /// <summary>
        /// This is the allocated dimensions.  ActualDimensions may be smaller.
        /// </summary>
        public Point MaxDimensions
        {
            get { return maxDimensions; }
        }

        /// <summary>
        /// Wrap allows focus to wrap top to bottom or left to 
        /// right when scrolling off the end of the list.
        /// </summary>
        public bool Wrap
        {
            get { return wrap; }
            set { wrap = value; }
        }

        /// <summary>
        /// Tells the grid to ignore when the focus changes.  Mostly
        /// this just stops the click sound being played.
        /// </summary>
        public bool IgnoreFocusChanged
        {
            get { return ignoreFocusChanged; }
            set { ignoreFocusChanged = true; }
        }

        /// <summary>
        /// Acts as if the grid never has input focus.  Useful 
        /// for grids that need to be driven from outside.
        /// </summary>
        public bool IgnoreInput
        {
            get { return ignoreInput; }
            set { ignoreInput = value; }
        }

        /// <summary>
        /// Acts as if the grid always has input focus.
        /// </summary>
        public bool AlwaysReadInput
        {
            get { return alwaysReadInput; }
            set { alwaysReadInput = value; }
        }

        /// <summary>
        /// Use the trigger buttons for left/right input.
        /// False by default.
        /// </summary>
        public bool UseTriggers
        {
            get { return useTriggers; }
            set { useTriggers = value; }
        }

        /// <summary>
        /// Use the shoulder buttons for left/right input.
        /// False by default.
        /// </summary>
        public bool UseShoulders
        {
            get { return useShoulders; }
            set { useShoulders = value; }
        }

        /// <summary>
        /// Use the left stick for input.  
        /// True by default.
        /// </summary>
        public bool UseLeftStick
        {
            get { return useLeftStick; }
            set { useLeftStick = value; }
        }

        /// <summary>
        /// Use the DPad for input.  
        /// True by default.
        /// </summary>
        public bool UseDPad
        {
            get { return useDPad; }
            set { useDPad = value; }
        }

        /// <summary>
        /// Use the right stick for input.
        /// False by default.
        /// </summary>
        public bool UseRightStick
        {
            get { return useRightStick; }
            set { useRightStick = value; }
        }

        /// <summary>
        /// Use the keyboard for input.
        /// True by default.
        /// </summary>
        public bool UseKeyboard
        {
            get { return useKeyboard; }
            set { useKeyboard = value; }
        }

        /// <summary>
        /// Use the tab key for input.
        /// Only works for 1-d grids.
        /// Acts likes 'down' or 'right'.
        /// Wrap should be set to true.
        /// False by default.
        /// </summary>
        public bool UseTab
        {
            get { return useTab; }
            set { useTab = value; }
        }

        /// <summary>
        /// Use the mouse scroll wheel to go through elements.
        /// Only works with 1-d grids.
        /// Doesn't work on XBox.
        /// False by default.
        /// </summary>
        public bool UseMouseScrollWheel
        {
            get { return useMouseScrollWheel; }
            set
            {
                useMouseScrollWheel = value;
            }
        }

        /// <summary>
        /// Does the grid need a refresh?
        /// </summary>
        public bool Dirty
        {
            get { return dirty; }
            set { dirty = value; }
        }

        /// <summary>
        /// Does this grid have input focus?
        /// </summary>
        public bool HasFocus
        {
            get { return CommandStack.Peek() == commandMap; }
        }

        public CommandMap CommandMap
        {
            get { return commandMap; }
        }

        /// <summary>
        /// Event triggered when user moves focus.
        /// </summary>
        public UIGridEvent MovedLeft
        {
            get { return movedLeft; }
            set { movedLeft = value; }
        }

        /// <summary>
        /// Event triggered when user moves focus.
        /// </summary>
        public UIGridEvent MovedRight
        {
            get { return movedRight; }
            set { movedRight = value; }
        }

        /// <summary>
        /// Event triggered when user moves focus.
        /// </summary>
        public UIGridEvent MovedUp
        {
            get { return movedUp; }
            set { movedUp = value; }
        }

        /// <summary>
        /// Event triggered when user moves focus.
        /// </summary>
        public UIGridEvent MovedDown
        {
            get { return movedDown; }
            set { movedDown = value; }
        }

        public Vector3 ScrollOffset
        {
            get { return scrollOffset; }
        }

        #endregion


        public UIGrid(
            UIGridEvent onSelect,
            UIGridEvent onCancel,
            Point initialDimensions,
            string uiMode
        )
        {
            ScrollSpeed = 0.35f;

            this.Select = onSelect;
            this.Cancel = onCancel;
            this.maxDimensions = initialDimensions;
            this.commandMap.name = uiMode;

            grid = new UIGridElement[maxDimensions.X, maxDimensions.Y];

            actualDimensions = new Point(0, 0);
            focusIndex = new Point(0, 0);   // TODO need to decide how to set this if not already set.  Should never point
                                            // to a null element?  Does this mean we can't have a row of diagonal elements?
                                            // Why bother about supporting something that's stupid, just limit it.

        }   // end of UIGrid c'tor

        private bool UserMovedLeft(GamePadInput gamePad)
        {
            bool moveLeft = false;

            moveLeft |= UseDPad && gamePad.DPadLeft.WasPressedOrRepeat;
            moveLeft |= UseLeftStick && gamePad.LeftStickLeft.WasPressedOrRepeat;
            moveLeft |= UseRightStick && gamePad.RightStickLeft.WasPressedOrRepeat;
            moveLeft |= UseShoulders && gamePad.LeftShoulder.WasPressedOrRepeat;
            moveLeft |= UseTriggers && gamePad.LeftTriggerButton.WasPressedOrRepeat;
            moveLeft |= UseKeyboard && KeyboardInput.WasPressedOrRepeat(Keys.Left);
            moveLeft |= UseMouseScrollWheel && (MouseInput.PrevScrollWheel < MouseInput.ScrollWheel);

            return moveLeft;
        }

        private bool UserMovedRight(GamePadInput gamePad)
        {
            bool moveRight = false;

            moveRight |= UseDPad && gamePad.DPadRight.WasPressedOrRepeat;
            moveRight |= UseLeftStick && gamePad.LeftStickRight.WasPressedOrRepeat;
            moveRight |= UseRightStick && gamePad.RightStickRight.WasPressedOrRepeat;
            moveRight |= UseShoulders && gamePad.RightShoulder.WasPressedOrRepeat;
            moveRight |= UseTriggers && gamePad.RightTriggerButton.WasPressedOrRepeat;
            moveRight |= UseKeyboard && KeyboardInput.WasPressedOrRepeat(Keys.Right);
            moveRight |= UseTab && KeyboardInput.WasPressedOrRepeat(Keys.Tab);
            moveRight |= UseMouseScrollWheel && (MouseInput.PrevScrollWheel > MouseInput.ScrollWheel);

            return moveRight;
        }

        private bool UserMovedUp(GamePadInput gamePad)
        {
            bool moveUp = false;

            moveUp |= UseDPad && gamePad.DPadUp.WasPressedOrRepeat;
            moveUp |= UseLeftStick && gamePad.LeftStickUp.WasPressedOrRepeat;
            moveUp |= UseRightStick && gamePad.RightStickUp.WasPressedOrRepeat;
            moveUp |= UseKeyboard && KeyboardInput.WasPressedOrRepeat(Keys.Up);
            moveUp |= UseMouseScrollWheel && (MouseInput.PrevScrollWheel < MouseInput.ScrollWheel);

            return moveUp;
        }

        public bool UserMovedDown(GamePadInput gamePad)
        {
            bool moveDown = false;

            moveDown |= UseDPad && gamePad.DPadDown.WasPressedOrRepeat;
            moveDown |= UseLeftStick && gamePad.LeftStickDown.WasPressedOrRepeat;
            moveDown |= UseRightStick && gamePad.RightStickDown.WasPressedOrRepeat;
            moveDown |= UseKeyboard && KeyboardInput.WasPressedOrRepeat(Keys.Down);
            moveDown |= UseTab && KeyboardInput.WasPressedOrRepeat(Keys.Tab);
            moveDown |= UseMouseScrollWheel && (MouseInput.PrevScrollWheel > MouseInput.ScrollWheel);

            return moveDown;
        }

        protected bool focusChanged = false;

        public virtual void Update(ref Matrix parentMatrix)
        {
            // Update the grid elements first so that they have a chance to take any input.
            if (Active)
            {
                // Call update on each child object.
                for (int j = 0; j < maxDimensions.Y; j++)
                {
                    for (int i = 0; i < maxDimensions.X; i++)
                    {
                        if (grid[i, j] != null && grid[i, j].Visible)
                        {
                            grid[i, j].Update(ref worldMatrix);
                        }
                    }
                }
            }

            // See if we have input focus, if so check for any input, unless the guide is up.
            // TODO (****) This is truly horrific.  The "CommandStack" doesn't actually act like a proper stack
            // so we can get into cases where the current commandMap is actually down one from the top.
            if ((alwaysReadInput || ((CommandStack.Peek() == commandMap || CommandStack.Peek(1) == commandMap) && !ignoreInput)))
            {
                GamePadInput gamePad = GamePadInput.GetGamePad0();

                // TODO Right now this allows the focus to be on a location that is null.  Do
                // we want to allow this?  Should we skip to the next valid location?

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();

                    Active = false;
                    Foley.PlayPressA();

                    if (Select != null)
                    {
                        Select(this);
                    }
                }

                // 'B' to back out.
                if (Actions.Cancel.WasPressed)
                {
                    Actions.Cancel.ClearAllWasPressedState();

                    Active = false;
                    Foley.PlayBack();

                    if (Cancel != null)
                    {
                        Cancel(this);
                    }
                }

                // Move left.
                if (UserMovedLeft(gamePad))
                {
                    MoveLeft();
                }

                // Move right.
                if (UserMovedRight(gamePad))
                {
                    MoveRight();
                }

                // Move up.
                if (UserMovedUp(gamePad))
                {
                    MoveUp();
                }

                // Move down.
                if (UserMovedDown(gamePad))
                {
                    MoveDown();
                }
            }   // end if we have input focus.

            if (dirty)
            {
                Refresh();
                dirty = false;
            }

            // We do this outside of having input focus in case someone external changed the focus.
            if (focusChanged && !ignoreFocusChanged)
            {
                Foley.PlayShuffle();
                if (scrolling)
                {
                    if (Time.FrameRate < 19.0f)
                    {
                        // Without a twitch...
                        scrollOffset = grid[focusIndex.X, focusIndex.Y].Position;
                    }
                    else
                    {
                        // With a twitch...
                        Vector3 desiredScrollOffset = CalcScrollOffset();
                        TwitchScrollOffset(desiredScrollOffset, ScrollSpeed);
                    }
                }
            }
            focusChanged = false;

            if (scrolling)
            {
                if (!isDragging)
                {
                    if (hasResidualVelocity)
                    {
                        // Apply any residual vertical velocity still affecting the grid, and end it
                        // if the move becomes invalid or the velocity becomes too small.
                        float minDelta = 0.0001f;

                        if ((Math.Abs(residualVelocity) < minDelta) || !SlideByY(residualVelocity))
                        {
                            //Vector3 desiredScrollOffset = CalcScrollOffset();
                            //TwitchScrollOffset(desiredScrollOffset, ScrollSpeed);
                            residualVelocity = 0.0f;
                            hasResidualVelocity = false;
                        }
                    }
                }
            }

            // Update grid state
            if (parentMatrix != null)
            {
                worldMatrix = localMatrix * parentMatrix;
                worldMatrix.Translation -= scrollOffset;
            }
        }   // end of UIGrid Update()

        /// <summary>
        /// Smoothly changes the scroll offset from the current value to the new.
        /// </summary>
        /// <param name="o"></param>
        private void TwitchScrollOffset(Vector3 offset, float speed)
        {
            if (twitchScrollOffset != offset)
            {
                twitchScrollOffset = offset;
                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                { scrollOffset = value; };
                TwitchManager.CreateTwitch<Vector3>(scrollOffset, offset, set,
                    speed, TwitchCurve.Shape.OvershootOut);
            }
        }

        /// <summary>
        /// Performs input handling for the UI grid. Currently, vertical drag only.
        /// </summary>
        /// <param name="camera"></param>
        public virtual void HandleTouchInput(Camera camera)
        {
            TouchContact touch = TouchInput.GetOldestTouch();
            if (touch == null) { return; }

            if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
                return;
            }
            else if (!isDragging)
            {
                // This touch hasn't yet been classified as a drag yet.. so we
                // will see if it meets the criteria.
                float minDiff = MIN_DRAG_START;
                float diff = (touch.position - touch.startPosition).Length();
                if (diff < minDiff)
                {
                    return;
                }
                else
                {
                    isDragging = true;
                }
            }

            // Get local coordinates difference between this touch position and the previous
            Matrix localInvMatrix = Matrix.Invert(LocalMatrix);
            Vector2 currentLocalPos = TouchInput.GetLocalXYFromScreenCoords(
                touch.position,
                camera,
                ref localInvMatrix);
            Vector2 previousLocalPos = TouchInput.GetLocalXYFromScreenCoords(
                touch.previousPosition,
                camera,
                ref localInvMatrix);

            // We want the grid to move in the opposite direction to the movement of the finger,
            // so we invert the number.
            float localYMove = -((currentLocalPos - previousLocalPos).Y);

            if (SlideByY(localYMove))
            {
                // We did a successful move, so capture the momentum of the touch in pixels/frame
                float duration = 1.0f;

                if (Math.Abs(localYMove) > 0.025f)
                {
                    residualVelocity = localYMove / 4.0f;
                    hasResidualVelocity = true;
                    TwitchManager.Set<float> set = velocityDelegate;
                    TwitchManager.CreateTwitch<float>(residualVelocity, 0, set, duration,
                        TwitchCurve.Shape.EaseOut);
                }
                else if (touch.phase == TouchPhase.Stationary)
                {
                    // If the user has stopped moving but hasn't ended his touch, we will put
                    // the brakes on any residual velocity, as the user probably wants to settle
                    // here.
                    residualVelocity = 0.0f;
                    hasResidualVelocity = false;
                }
            }
        }

        public UIGridElement GetNextVisibleElementY(int xCol, int yStartRow, bool bBackwards)
        {
            UIGridElement retElement = null;

            if (xCol >= 0 && xCol < actualDimensions.X &&
                yStartRow >= 0 && yStartRow < actualDimensions.Y)
            {
                if (bBackwards)
                {
                    for (int i = yStartRow; i >= 0; --i)
                    {
                        if (null != grid[xCol, i] && grid[xCol, i].Visible)
                        {
                            retElement = grid[xCol, i];
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = yStartRow; i < actualDimensions.Y; ++i)
                    {
                        if (null != grid[xCol, i] && grid[xCol, i].Visible)
                        {
                            retElement = grid[xCol, i];
                            break;
                        }
                    }
                }
            }

            return retElement;
        }

        protected bool SlideByY(float yMove)
        {
            float newScrollYOffset = scrollOffset.Y + yMove;

            //Assure the focusIndex is within bounds 
            focusIndex.X = Math.Max(0, Math.Min(focusIndex.X, (actualDimensions.X - 1)));
            focusIndex.Y = Math.Max(0, Math.Min(focusIndex.Y, (actualDimensions.Y - 1)));

            UIGridElement focusElement = grid[focusIndex.X, focusIndex.Y];
            if (null == focusElement)
            {
                //and return false if the focus element is null to stop the drag.  Should not happen.
                return false;
            }
            float newToFocusDiff = focusElement.position.Y - newScrollYOffset;

            if (newToFocusDiff < 0)
            {
                // Approaching previous element
                if ((focusIndex.Y == 0) || (grid[focusIndex.X, focusIndex.Y - 1] == null))
                {
                    if (Math.Abs(newToFocusDiff) > (focusElement.Size.Y / 2.0f))
                    {
                        // If top, disallow dragging any further than half the height of the element.
                        return false;
                    }
                }
                else
                {
                    UIGridElement aboveElement = GetNextVisibleElementY(focusIndex.X, focusIndex.Y - 1, true);
                    if (null != aboveElement)
                    {
                        float newToAboveDiff = aboveElement.position.Y - newScrollYOffset;

                        if (Math.Abs(newToAboveDiff) < Math.Abs(newToFocusDiff))
                        {
                            // The drag move should bring focus up.
                            focusIndex.Y = aboveElement.gridCoords.Y;
                        }
                    }
                }
            }
            else if (newToFocusDiff > 0)
            {
                // Approaching next element
                if ((focusIndex.Y == (ActualDimensions.Y - 1)) ||
                    (grid[focusIndex.X, focusIndex.Y + 1] == null))
                {
                    if (Math.Abs(newToFocusDiff) > (focusElement.Size.Y / 2.0f))
                    {
                        // If bottom, disallow dragging any further than half the height of the element.
                        return false;
                    }
                }
                else
                {
                    UIGridElement belowElement = GetNextVisibleElementY(focusIndex.X, focusIndex.Y + 1, false);

                    if (null != belowElement)
                    {
                        float newToBelowDiff = belowElement.position.Y - newScrollYOffset;
                        if (Math.Abs(newToBelowDiff) < Math.Abs(newToFocusDiff))
                        {
                            // The drag move should bring focus down.
                            focusIndex.Y = belowElement.gridCoords.Y;
                        }
                    }
                }
            }


            scrollOffset.Y += yMove;
            return true;
        }

        protected void velocityDelegate(float val, Object param)
        {
            residualVelocity = val;
        }

        /// <summary>
        /// Moves the selection index up if possible.
        /// </summary>
        public void MoveUp()
        {
            if (movedUp != null)
            {
                movedUp(this);
            }

            int orig = focusIndex.Y;
            int curr = orig;
            int count = 0;

            while (true)
            {
                curr -= 1;

                if (curr < 0)
                {
                    if (Wrap)
                    {
                        curr = Math.Max(0, actualDimensions.Y - 1);
                    }
                    else
                    {
                        curr = 0;
                        break;
                    }
                }

                if (grid[focusIndex.X, curr] != null && grid[focusIndex.X, curr].Visible)
                    break;

                count += 1;
                if (count >= actualDimensions.Y)
                    break;
            }

            if (curr != orig && grid[focusIndex.X, curr] != null && grid[focusIndex.X, curr].Visible)
            {
                focusIndex.Y = curr;
                focusChanged = true;
            }
        }   // end of MoveUp()

        /// <summary>
        /// Moves the selection index down if possible.
        /// </summary>
        public void MoveDown()
        {
            if (movedDown != null)
            {
                movedDown(this);
            }

            int orig = focusIndex.Y;
            int curr = orig;
            int count = 0;

            while (true)
            {
                curr += 1;

                if (curr >= actualDimensions.Y)
                {
                    if (Wrap)
                    {
                        curr %= actualDimensions.Y;
                    }
                    else
                    {
                        curr = Math.Max(0, actualDimensions.Y - 1);
                        break;
                    }
                }

                if (grid[focusIndex.X, curr] != null && grid[focusIndex.X, curr].Visible)
                    break;

                count += 1;
                if (count >= actualDimensions.Y)
                    break;
            }

            if (curr != orig && grid[focusIndex.X, curr] != null && grid[focusIndex.X, curr].Visible)
            {
                focusIndex.Y = curr;
                focusChanged = true;
            }
        }   // end of MoveDown()

        /// <summary>
        /// Moves the selection index left if possible.
        /// </summary>
        public void MoveLeft()
        {
            if (movedLeft != null)
            {
                movedLeft(this);
            }

            int orig = focusIndex.X;
            int curr = orig;
            int count = 0;

            while (true)
            {
                curr -= 1;

                if (curr < 0)
                {
                    if (Wrap)
                    {
                        curr = Math.Max(0, actualDimensions.X - 1);
                    }
                    else
                    {
                        curr = 0;
                        break;
                    }
                }

                if (grid[curr, focusIndex.Y] != null && grid[curr, focusIndex.Y].Visible)
                    break;

                count += 1;
                if (count >= actualDimensions.X)
                    break;
            }

            if (curr != orig && grid[curr, focusIndex.Y] != null && grid[curr, focusIndex.Y].Visible)
            {
                focusIndex.X = curr;
                focusChanged = true;
            }
        }   // end of MoveLeft()

        /// <summary>
        /// Moves the selection index right if possible.
        /// </summary>
        public void MoveRight()
        {
            if (movedRight != null)
            {
                movedRight(this);
            }

            int orig = focusIndex.X;
            int curr = orig;
            int count = 0;

            while (true)
            {
                curr += 1;

                if (curr >= actualDimensions.X)
                {
                    if (Wrap)
                    {
                        curr %= actualDimensions.X;
                    }
                    else
                    {
                        curr = Math.Max(0, actualDimensions.X - 1);
                        break;
                    }
                }

                if (grid[curr, focusIndex.Y] != null && grid[curr, focusIndex.Y].Visible)
                    break;

                count += 1;
                if (count >= actualDimensions.X)
                    break;
            }

            if (curr != orig && grid[curr, focusIndex.Y] != null && grid[curr, focusIndex.Y].Visible)
            {
                focusIndex.X = curr;
                focusChanged = true;
            }
        }   // end of MoveRight()

        protected Point prevFocus = new Point(-1, -1);
        /// <summary>
        /// Checks if the infocus element has changed.  If so, changes 
        /// the Selected state on the new and previous infocus object.
        /// </summary>
        public virtual void UpdateSelectionFocus()
        {
            bool focusChanged = prevFocus != focusIndex;

            if (focusIndex.X < actualDimensions.X && focusIndex.Y < actualDimensions.Y && grid[focusIndex.X, focusIndex.Y] != null && (focusChanged || grid[focusIndex.X, focusIndex.Y].Selected == false))
            {
                // Unselect the previously infocus element before selecting
                // the new one.  This helps keep the help overlay stack coherent.
                if (prevFocus.X != -1 && prevFocus.Y != -1)
                {
                    if (grid[prevFocus.X, prevFocus.Y] != null)
                    {
                        grid[prevFocus.X, prevFocus.Y].Selected = false;
                    }
                }
                grid[focusIndex.X, focusIndex.Y].Selected = true;
            }
            prevFocus = focusIndex;
        }   // end of UpdateSelectionFocus()

        public virtual void Render(Camera camera)
        {
            if (active || renderWhenInactive)
            {
                // Ensure that camera poisiton/orientation is properly 
                // set so we get correct lighting on the UI.
                ShaderGlobals.SetCamera(camera);

                UpdateSelectionFocus();

                if (renderEndsIn)
                {
                    if (actualDimensions.X > 1)
                    {
                        // Left half.
                        for (int i = 0; i < SelectionIndex.X; i++)
                        {
                            if (grid[i, 0] != null && grid[i, 0].Visible)
                            {
                                grid[i, 0].Render(camera);
                            }
                        }
                        // Right half plus selected element.
                        for (int i = actualDimensions.X - 1; i >= SelectionIndex.X; i--)
                        {
                            if (grid[i, 0] != null && grid[i, 0].Visible)
                            {
                                grid[i, 0].Render(camera);
                            }
                        }
                    }
                    else
                    {
                        // Top half.
                        for (int j = 0; j < SelectionIndex.Y; j++)
                        {
                            if (grid[0, j] != null && grid[0, j].Visible)
                            {
                                grid[0, j].Render(camera);
                            }
                        }
                        // Bottom half plus selected element.
                        for (int j = actualDimensions.Y - 1; j >= SelectionIndex.Y; j--)
                        {
                            if (grid[0, j] != null && grid[0, j].Visible)
                            {
                                grid[0, j].Render(camera);
                            }
                        }
                    }
                }
                else
                {
                    // Render each child object.
                    for (int j = 0; j < actualDimensions.Y; j++)
                    {
                        for (int i = 0; i < actualDimensions.X; i++)
                        {
                            if (grid[i, j] != null && grid[i, j].Visible)
                            {
                                grid[i, j].Render(camera);
                            }
                        }
                    }
                }
            }   // end of if active.

        }   // end of UIGrid Render()

        /// <summary>
        /// Needs to be called whenever something has changed in the grid
        /// to recalculate the sizes/positions, etc.
        /// </summary>
        public virtual void Refresh()
        {
            if (actualDimensions.X < 1 || actualDimensions.Y < 1)
            {
                return;
            }

            // Calc total size of grid elements.
            Vector2 totalSize = Vector2.Zero;
            int totalCountY = 0;
            for (int j = 0; j < actualDimensions.Y; j++)
            {
                for (int i = 0; i < actualDimensions.X; i++)
                {
                    if (grid[i, j] != null && grid[i, j].Visible)
                    {
                        totalSize += grid[i, j].Scale * grid[i, j].Size;
                        totalCountY += 1;
                    }
                }
            }

            // Add in spacing to get total size of layout.
            totalSize += spacing * new Vector2(actualDimensions.X - 1, totalCountY - 1);

            // For each row and each column we need to know the maximum height and width of the elements.
            float[] rowHeights = new float[actualDimensions.Y];
            float[] columnWidths = new float[actualDimensions.X];
            float totalHeight = 0.0f;
            float totalWidth = 0.0f;

            // Initialize.
            for (int j = 0; j < actualDimensions.Y; j++)
            {
                rowHeights[j] = 0.0f;

                for (int i = 0; i < actualDimensions.X; i++)
                {
                    if (grid[i, j] != null)
                    {
                        if (grid[i, j].Visible)
                        {
                            rowHeights[j] = MathHelper.Max(rowHeights[j], grid[i, j].Scale * grid[i, j].Size.Y);
                        }
                    }
                }
                totalHeight += rowHeights[j];
            }

            for (int i = 0; i < actualDimensions.X; i++)
            {
                columnWidths[i] = 0.0f;
                for (int j = 0; j < actualDimensions.Y; j++)
                {
                    if (grid[i, j] != null && grid[i, j].Visible)
                    {
                        columnWidths[i] = MathHelper.Max(columnWidths[i], grid[i, j].Scale * grid[i, j].Size.X);
                    }
                }
                totalWidth += columnWidths[i];
            }

            // Add in spacing to totals.
            totalHeight += spacing.Y * (totalCountY - 1);
            totalWidth += spacing.X * (actualDimensions.X - 1);

            // We now have the row and column widths and we want to translate these into positions for each row and column.
            float[] rowPositions = new float[actualDimensions.Y];
            float[] columnPositions = new float[actualDimensions.X];
            rowPositions[0] = totalHeight / 2.0f - rowHeights[0] / 2.0f;
            for (int j = 1, prevJ = 0; j < actualDimensions.Y; j++)
            {
                //                if (grid[0, j].Visible)
                {
                    rowPositions[j] = rowPositions[prevJ] - (rowHeights[prevJ] + rowHeights[j]) / 2.0f - spacing.Y;
                    prevJ = j;
                }
            }
            columnPositions[0] = -totalWidth / 2.0f + columnWidths[0] / 2.0f;
            for (int i = 1; i < actualDimensions.X; i++)
            {
                columnPositions[i] = columnPositions[i - 1] + (columnWidths[i - 1] + columnWidths[i]) / 2.0f + spacing.X;
            }

            // Set position of each element.
            for (int j = 0; j < actualDimensions.Y; j++)
            {
                for (int i = 0; i < actualDimensions.X; i++)
                {
                    if (grid[i, j] != null && grid[i, j].Visible)
                    {
                        grid[i, j].Position = new Vector3(columnPositions[i], rowPositions[j], 0.0f);
                    }
                }
            }

            if (scrolling)
            {
                scrollOffset = CalcScrollOffset();
            }

        }   // end of UIGrid Refresh()

        /// <summary>
        /// Based on the current focus object, determines where the center of the grid should be.
        /// </summary>
        /// <returns></returns>
        private Vector3 CalcScrollOffset()
        {
            if (ActualDimensions.X == 0 || ActualDimensions.Y == 0)
                return Vector3.Zero;

            // Default to putting focus object at center.
            Vector3 offset = grid[focusIndex.X, focusIndex.Y].Position;

            if (slopOffset)
            {
                // NOTE This is currently not very flexible.  Only really works for 1d veritcal lists.
                int numElements = actualDimensions.Y;
                if (numElements < 3)
                {
                    // Stay with default.
                }
                else
                {
                    // If focus is first or last object in the list, center on the one before/after to it.
                    if (focusIndex.Y == 0)
                    {
                        offset = grid[0, 1].Position;
                    }
                    else if (focusIndex.Y == numElements - 1)
                    {
                        offset = grid[0, numElements - 2].Position;
                    }
                }
            }   // end if slopOffset

            return offset;
        }   // end of CalcScrollOffset()

        /// <summary>
        /// Adds a new element to the grid.
        /// </summary>
        /// <param name="element">the new element to add</param>
        /// <param name="i">column for new element</param>
        /// <param name="j">row for new element</param>
        public void Add(UIGridElement element, int i, int j)
        {
            CheckSize(i, j);
            grid[i, j] = element;

            if (element != null)
            {
                actualDimensions.X = Math.Max(actualDimensions.X, i + 1);
                actualDimensions.Y = Math.Max(actualDimensions.Y, j + 1);
                dirty = true;
                element.gridCoords = new Point(i, j);
            }
        }   // end of UIGrid Add()

        /// <summary>
        /// Sets the visibility of an element in the grid by element name
        /// </summary>
        /// <param name="name">string name for the element sought</param>
        /// <param name="isVisible">visible setting</param>
        public bool SetVisible(string name, bool isVisible)
        {
            for (int j = 0; j < actualDimensions.Y; j++)
            {
                for (int i = 0; i < actualDimensions.X; i++)
                {
                    if (grid[i, j].ElementName == name)
                    {
                        grid[i, j].Visible = isVisible;
                        grid[i, j].Dirty = true;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the visibility all elements in the grid to a specific value
        /// </summary>
        public void SetAllVisible(bool isVisible)
        {
            for (int i = 0; i < actualDimensions.X; i++)
            {
                for (int j = 0; j < actualDimensions.Y; j++)
                {
                    grid[i, j].Visible = isVisible;
                    grid[i, j].Dirty = true;
                }
            }
            dirty = true;
        }

        /// <summary>
        /// Checks the underlying grid to see if it's big enough
        /// to hold an element at i,j.  If not, it expands the grid.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        private void CheckSize(int i, int j)
        {
            if (i >= MaxDimensions.X || j >= MaxDimensions.Y)
            {
                int sizeIncrement = 8;

                // Need to expand grid.
                Point newSize = MaxDimensions;
                if (i >= MaxDimensions.X)
                {
                    newSize.X += sizeIncrement;
                }
                if (j >= MaxDimensions.Y)
                {
                    newSize.Y += sizeIncrement;
                }
                // Allocate new grid.
                UIGridElement[,] newGrid = new UIGridElement[newSize.X, newSize.Y];
                // Copy over all references.
                for (int ii = 0; ii < MaxDimensions.X; ii++)
                {
                    for (int jj = 0; jj < MaxDimensions.Y; jj++)
                    {
                        newGrid[ii, jj] = grid[ii, jj];
                    }
                }

                grid = newGrid;
                maxDimensions = newSize;
            }
        }   // end of CheckSize()

        /// <summary>
        /// Removes all grid entries and reset size to 0, 0.
        /// Calls Unload for each element.
        /// </summary>
        public virtual void Clear()
        {
            for (int j = 0; j < actualDimensions.Y; j++)
            {
                for (int i = 0; i < actualDimensions.X; i++)
                {
                    UIGridElement e = grid[i, j] as UIGridElement;
                    BokuGame.Unload(e);
                    grid[i, j] = null;
                }
            }
            actualDimensions = Point.Zero;
            focusIndex = Point.Zero;
            prevFocus = new Point(-1, -1);
            focusChanged = true;
            isDragging = false;
        }   // end of UIGrid Clear()

        /// <summary>
        /// Removes all grid entries and reset size to 0, 0.
        /// Does not call Unload for each element.  This assumes
        /// that something else is holding on to the elements and 
        /// still wants them to be usable.  Currently used with
        /// brush sets.
        /// </summary>
        public void ClearNoUnload()
        {
            for (int j = 0; j < actualDimensions.Y; j++)
            {
                for (int i = 0; i < actualDimensions.X; i++)
                {
                    grid[i, j] = null;
                }
            }
            actualDimensions = Point.Zero;
            focusIndex = Point.Zero;
            prevFocus = new Point(-1, -1);
            focusChanged = true;
        }   // end of UIGrid ClearNoUnload()

        /// <summary>
        /// Removes a tile from the grid and shuffles all the other tiles
        /// up to fill in the empty place.  Note that this can only work
        /// with 1d grids.  The current implementation only works with
        /// 1d vertical grids.
        /// 
        /// Returns true if the grid is empty and should be deleted.
        /// </summary>
        /// <param name="index"></param>
        public bool RemoveAndCollapse(Point index)
        {
            if (ActualDimensions.X > 1)
            {
                throw new System.Exception("Sorry, this only works with 1d vertical grids.");
            }

            for (int i = index.Y + 1; i < ActualDimensions.Y; i++)
            {
                grid[0, i - 1] = grid[0, i];
            }
            grid[0, ActualDimensions.Y - 1] = null;
            --actualDimensions.Y;

            prevFocus = new Point(-1, -1);
            if (focusIndex.Y == actualDimensions.Y)
            {
                --focusIndex.Y;
            }

            if (actualDimensions.Y == 0)
            {
                return true;
            }

            Refresh();

            return false;
        }   // end of UIGrid RemoveAndCollapse()

        /// <summary>
        /// Removes all empty tile from the grid and shuffles all the other tiles
        /// up to fill in the empty place.  Note that this can only work
        /// with 1d grids.  The current implementation only works with
        /// 1d vertical grids.
        /// 
        /// Returns true if the grid is empty and should be deleted.
        /// </summary>
        /// <param name="index"></param>
        public bool RemoveAllEmptyAndCollapse()
        {
            if (ActualDimensions.X > 1)
            {
                throw new System.Exception("Sorry, this only works with 1d vertical grids.");
            }

            int newDimensionY = ActualDimensions.Y;
            int firstEmpty = -1;
            for (int i = 1; i < ActualDimensions.Y; i++)
            {
                if (grid[0, i - 1] == null)
                {
                    if (firstEmpty < 0)
                    {
                        firstEmpty = i - 1;
                    }
                    newDimensionY--;
                }

                if (grid[0, i] != null && firstEmpty >= 0)
                {
                    grid[0, firstEmpty] = grid[0, i];
                    grid[0, i] = null;
                }
            }
            actualDimensions.Y = newDimensionY;

            prevFocus = new Point(-1, -1);
            if (focusIndex.Y >= actualDimensions.Y)
            {
                focusIndex.Y = 0;
            }

            if (actualDimensions.Y == 0)
            {
                return true;
            }

            Refresh();

            return false;
        }   // end of UIGrid RemoveAndCollapse()

        /// <summary>
        /// Inserts a new element into the grid at the specified index
        /// and shifts the other elements out to make room.  
        /// Note this only works for 1-d grids.
        /// </summary>
        /// <param name="e">The new element</param>
        /// <param name="index">Where to put it.</param>
        public void InsertAndExpand(UIGridElement e, Point index)
        {
            Debug.Assert(index.X == 0 || index.Y == 0, "Sorry, this only works with 1d vertical grids.");
            Debug.Assert(ActualDimensions.X == 0 || ActualDimensions.Y == 0);

            if (ActualDimensions.X > 0)
            {
                // Expand along X.

                // Ensure that the grid is big enough to hold this element plus any shifted ones.
                CheckSize(index.X + 1, index.Y);

                for (int i = actualDimensions.X; i > index.X; i--)
                {
                    grid[i, 0] = grid[i - 1, 0];
                }
                grid[index.X, 0] = e;

                actualDimensions.X = ActualDimensions.X + 1;
            }
            else
            {
                // Expand along Y.

                // Ensure that the grid is big enough to hold this element plus any shifted ones.
                CheckSize(index.X - 1, index.Y);

                for (int i = actualDimensions.Y; i > index.Y; i--)
                {
                    grid[0, i] = grid[0, i - 1];
                }
                grid[0, index.Y] = e;

                actualDimensions.Y = ActualDimensions.Y + 1;
            }

        }   // end of UIGrid InsertAndExpand()

        /// <summary>
        /// Returns the element at index i, j.
        /// </summary>
        public UIGridElement Get(int i, int j)
        {
            if (i < 0 || j < 0 || i >= maxDimensions.X || j >= maxDimensions.Y)
            {
                return null;
            }
            return grid[i, j];
        }   // end of UIGrid Get()

        /// <summary>
        /// Sets the element at index i, j.
        /// NOTE:  This is kind of a dangerous call since any elements that are not held
        /// by the grid will not be getting the correct LoadContent and UnloadContent
        /// calls.  Only use if you understand this.  Seriously.
        /// </summary>
        public void Set(UIGridElement e, int i, int j)
        {
            if (i >= 0 && j >= 0 && i < maxDimensions.X && j < maxDimensions.Y)
            {
                grid[i, j] = e;
                Matrix worldMatrix = Matrix.Identity;
                Refresh();
            }
        }   // end of UIGrid Get()


        public enum ShiftDirection
        {
            decrementIndex,
            incrementIndex,
        }

        /// <summary>
        /// Shifts the grid elements by one position in the specified direction.  The element
        /// that is shifted off the end is wrapped around.
        /// Note that this only works for 1d grids.
        /// </summary>
        /// <param name="dir">The direction to shift the elements.</param>
        public void ShiftElements(ShiftDirection dir)
        {
            // Only apply to 1d grids.
            if (ActualDimensions.X != 1 && ActualDimensions.Y != 1)
                return;

            // Recalc the positions of all the elements.  This need to be done in case the user is
            // hitting the buttons quickly and the positions haven't settled from the previous set
            // of twitches.
            Refresh();

            if (dir == ShiftDirection.decrementIndex)
            {
                UIGridElement tmp = grid[0, 0];     // The element which will be wrapped around.
                Vector3 finalPos = Vector3.Zero;

                if (ActualDimensions.X > 1)
                {
                    // Horizontal orientation.

                    // Change the elements in the grid.
                    for (int i = 0; i < ActualDimensions.X - 1; i++)
                    {
                        grid[i, 0] = grid[i + 1, 0];
                    }
                    grid[ActualDimensions.X - 1, 0] = tmp;
                }
                else
                {
                    // Vertical orientation.

                    // Change the elements in the grid.
                    for (int i = 0; i < ActualDimensions.Y - 1; i++)
                    {
                        grid[0, i] = grid[0, i + 1];
                    }
                    grid[0, ActualDimensions.Y - 1] = tmp;
                }
            }
            else
            {
                // dir == ShiftDirection.incrementIndex

                UIGridElement tmp = null;           // The element which will be wrapped around.
                Vector3 finalPos = Vector3.Zero;

                if (ActualDimensions.X > 1)
                {
                    // Horizontal orientation.

                    // Change the elements in the grid.
                    tmp = grid[ActualDimensions.X - 1, 0];
                    for (int i = ActualDimensions.X - 1; i > 0; i--)
                    {
                        grid[i, 0] = grid[i - 1, 0];
                    }
                    grid[0, 0] = tmp;
                }
                else
                {
                    // Vertical orientation.

                    // Change the elements in the grid.
                    tmp = grid[0, ActualDimensions.Y - 1];
                    for (int i = ActualDimensions.Y - 1; i > 0; i--)
                    {
                        grid[0, i] = grid[0, i - 1];
                    }
                    grid[0, 0] = tmp;
                }

            }

        }   // end of UIGrid ShiftElements()

        /// <summary>
        /// Sorts the list.  This is only expected to work with 1d vertical grids.  
        /// The paramter is a GetKey function which takes a UIGridElement and returns 
        /// a string key on which the array is sorted.  This way the same list can 
        /// be sorted various ways.
        /// </summary>
        public delegate string GetKey(UIGridElement e);
        protected class GridEntry
        {
            public string key;
            public UIGridElement e;
        }

        public void Sort(GetKey key, bool invert)
        {
            int numElements = ActualDimensions.Y;

            if (numElements == 0)
                return;

            GridEntry[] list = new GridEntry[numElements];

            UIGridElement focus = SelectionElement;

            // Fill the array.
            for (int i = 0; i < numElements; i++)
            {
                list[i] = new GridEntry();
                list[i].e = grid[0, i];
                list[i].key = key(grid[0, i]);
            }

            // Sort it based on the key.
            if (invert)
            {
                Array.Sort(list, InverseKeyComp);
            }
            else
            {
                Array.Sort(list, KeyComp);
            }

            // Now reorder the list to match the sorted version.
            for (int i = 0; i < numElements; i++)
            {
                grid[0, i] = list[i].e;

                // Restore focus
                if (focus == grid[0, i])
                {
                    focusIndex.Y = i;
                }
            }

            // Now that the grid has a new sorted order
            // recalc the positions for each tile.
            Refresh();

            if (scrolling)
            {
                Vector3 desiredScrollOffset = CalcScrollOffset();

                // And then fire off a twitch to re-center on the new focus object.
                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param) { scrollOffset = value; };
                TwitchManager.CreateTwitch<Vector3>(scrollOffset, desiredScrollOffset, set, ScrollSpeed, TwitchCurve.Shape.OvershootOut);
            }
        }   // end if UIGrid Sort()

        protected int InverseKeyComp(GridEntry e0, GridEntry e1)
        {
            return String.Compare(e1.key, e0.key);
        }   // end of UIGrid StringComp()

        protected int KeyComp(GridEntry e0, GridEntry e1)
        {
            return String.Compare(e0.key, e1.key);
        }   // end of UIGrid StringComp()


        public virtual void LoadContent(bool immediate)
        {
            for (int j = 0; j < maxDimensions.Y; j++)
            {
                for (int i = 0; i < maxDimensions.X; i++)
                {
                    UIGridElement e = grid[i, j];
                    BokuGame.Load(e, immediate);
                }
            }

        }   // end of UIGrid LoadContent()

        public virtual void InitDeviceResources(GraphicsDevice device)
        {
        }

        public virtual void UnloadContent()
        {
            for (int j = 0; j < maxDimensions.Y; j++)
            {
                for (int i = 0; i < maxDimensions.X; i++)
                {
                    UIGridElement e = grid[i, j];
                    BokuGame.Unload(e);
                }
            }
        }   // end of UIGrid UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public virtual void DeviceReset(GraphicsDevice device)
        {
            dirty = true;

            for (int j = 0; j < maxDimensions.Y; j++)
            {
                for (int i = 0; i < maxDimensions.X; i++)
                {
                    UIGridElement e = grid[i, j];
                    BokuGame.DeviceReset(e, device);
                }
            }
        }

    }   // end of class UIGrid

}   // end of namespace Boku.UI2D


