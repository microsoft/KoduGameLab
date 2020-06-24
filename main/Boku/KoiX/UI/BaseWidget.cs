
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
using KoiX.Geometry;

namespace KoiX.UI
{
    public abstract class BaseWidget : InputEventHandler, IDirty, IDeviceResetX
    {
        public delegate void Callback(BaseWidget w);

        #region Members

#if DEBUG
        /// <summary>
        /// String that we only use in debug mode.  
        /// Put whatever you want here to make life
        /// easier.  Never use this for anything else.
        /// </summary>
        public string _name = "";
#endif 
        protected string id;    // Optional string id for widget.  The idea here is to be able
                                // to use this for things like switch statements when you have a 
                                // collection of related widgets.  In a lot of the older code we
                                // used the labelText or labelId but with things like GraphicButtons
                                // we don't have labels.  If possible, use an enum based identifier
                                // instead since that gives you type checking and helps prevent typos.

        protected Callback onChange = null; // When value (not state) of widget changes.

        protected Twitchable<float> alpha;  // Alpha for overall rendering.  Also used to control
                                            // rendering.  If alpha == 0, don't render.

        // State should not be externally visible.  Instead accessors should set/clear values as needed.
        UIState state = UIState.Inactive;

        bool selected = false;      // Is this widget currently selected?
        bool hover = false;         // Is the mouse currently hovering over this widget?
                                    // Disabled since we determine focus by asking the DialogManager which widget has focus.
                                    //bool focused = false;       // Is this widget currently in focus?

        protected bool focusable = true;        // Everything except labels and textboxes?
        //protected bool selectable = false;      // For latchable buttons, checkboxes, radio buttons, etc.
        protected bool hoverable = true;        // Do we set the hover flag?

        protected RectangleF localRect;     // Bounding rect in local (parent dialog) space.  Note that padding is inside rect, margin is outside.
        protected Vector2 parentPosition;   // Position of parent.  Local rect is an offset from this.  Top level widgets will have 0,0 for this.
                                            // This is relative to the parent dialog's position.

        // Given the size of the widget (ie the rect), margin is added to the outside
        // when aligning inside a WidgetSet.
        // Padding is added inside the rect around the content.
        // Web comments seem to suggest that favoring margin works out best.
        // Web comments also suggest that margins of neighboring elements are allowed to overlap.

        protected Padding margin = new Padding();
        protected Padding padding = new Padding();

        // TODO Should we have a dirty flag for all widgets.  This way when a widget
        // changes size it can inform its parent that a recalc is needed.
        // Note: Should we also treat WidgetSet as a parent.  If so, we not have
        // BaseDialog as a possible parent and BaseWidget.  What's the best way to
        // resolve this?  IParentable???
        // OR All widgets parent to other widgets.  WidgetSet _may_ also parent to 
        // a dialog.  Implies dialogs host all widgets in a top level set.
        // OR Parent is always the dialog and SetParent is the optional WidgetSet that
        // this widget belongs to.

        // *** Forget having ref to parent WidgetSet.  Just have ref to parent dialog.
        // Yes, this may cuase slightly more refreshing than needed but not enough to notice.

        // **** Don't worry about dirty and just recalc everything all the time?


        protected BaseDialog parentDialog;      // May be null at init time but should be non-null in use.

        protected bool dirty = true;            // Need to recalc self (and children)

        protected bool marginDebug = false;     // Render overlays to help debug margin and padding.  Requires
                                                // that base.Render() get called by derived classes.

        // Navigation for focus. 
        public BaseWidget TabListNext;          // Next widget in tab list.  Will be null at end of parent dialog's list.
        public BaseWidget TabListPrev;          // Previous widget in tab list.  Will be null for first widget in parent dialog's list.
        public BaseWidget DPadListUp;           // Widget above this one, navigated via gamepad.  May be null.
        public BaseWidget DPadListDown;         // Widget below this one, navigated via gamepad.  May be null.
        public BaseWidget DPadListLeft;         // Widget to the left of this one, navigated via gamepad.  May be null.
        public BaseWidget DPadListRight;        // Widget to the right of this one, navigated via gamepad.  May be null.

        protected ThemeSet theme = Theme.CurrentThemeSet;

        protected object data;                  // ref for any random data we want to associate with this widget.

        #endregion

        #region Accessors

        public ThemeSet ThemeSet
        {
            get { return theme; }
        }

        /// <summary>
        /// Position, instantaneous, not twitched.
        /// Local space.
        /// </summary>
        public Vector2 Position
        {
            get { return localRect.Position; }
            set
            {
                if (localRect.Position != value)
                {
                    localRect.Position = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Instantaneous size.
        /// </summary>
        public Vector2 Size
        {
            get { return localRect.Size; }
            set
            {
                if (localRect.Size != value)
                {
                    localRect.Size = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Local space (Relative to parent dialog or set) bounding rectangle.  
        /// </summary>
        public RectangleF LocalRect
        {
            get { return localRect; }
            set
            {
                if (localRect != value)
                {
                    localRect = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Parent position.  Read only.
        /// </summary>
        public Vector2 ParentPosition
        {
            get { return parentPosition; }
        }

        /// <summary>
        /// Dialog that owns this widget.  Used for propagation of dirty flag.
        /// 
        /// Note that this does not necessarily mean that this widget is in the
        /// parent dialog's widget list.  This widget may be contained by a 
        /// WidgetSet which is, in turn, contained by dialog's widget list.
        /// </summary>
        public virtual BaseDialog ParentDialog
        {
            get { return parentDialog; }
        }

        //
        //
        // State related accessors
        //
        //

        /// <summary>
        /// Deliberately marked as protected so it's only used internally.
        /// </summary>
        protected UIState State
        {
            get { return state; }
            private set
            {
                if (state != value)
                {
                    state = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Gets the widget state combined with flags for selected, focused, and hover.
        /// </summary>
        protected UIState CombinedState
        {
            get
            {
                UIState combinedState = state;
                if (InFocus)
                {
                    combinedState |= UIState.Focused;
                }
                if (Selected)
                {
                    combinedState |= UIState.Selected;
                }
                if (Hover)
                {
                    combinedState |= UIState.Hover;
                }
                return combinedState;
            }
        }

        /// <summary>
        /// Active == Rendering and accepting input.
        /// Looking for input?  Set via Activate(), Deactivate(), or Disable().
        /// </summary>
        public bool Active
        {
            get { return (state & UIState.Active) != 0; }
        }

        /// <summary>
        /// Inactive == Neither rendering nor accepting input.
        /// Looking for input?  Set via Activate(), Deactivate(), or Disable().
        /// </summary>
        public bool Inactive
        {
            get { return (state & UIState.Inactive) != 0; }
        }

        /// <summary>
        /// Disabled == Rendering in grey'd out look, not accepting input.
        /// Looking for input?  Set via Activate(), Deactivate(), or Disable().
        /// </summary>
        public bool Disabled
        {
            get { return (state & UIState.Disabled) != 0; }
        }

        /// <summary>
        /// Is this widget the current mouseHitObject?  Set in Update().
        /// </summary>
        public bool Hover
        {
            get { return hover; }
            set
            {
                if (hoverable)
                {
                    hover = value;
                }
                else
                {
                    hover = false;
                }
            }
        }

        /// <summary>
        /// Shold this widget respond to the mouse hovering over it.
        /// Defaults to true.
        /// </summary>
        public bool Hoverable
        {
            get { return hoverable; }
            set
            {
                if (value != hoverable)
                {
                    hoverable = value;
                    if (!hoverable)
                    {
                       hover = false;
                    }
                }
            }
        }

        /// <summary>
        /// Does the widget currently have focus?  This can happen via either
        /// tab or gamepad dpad selection.  Also, if direct action is taken
        /// on a widget then the focus should also move to it.
        /// To change the focus state use SetFocus() and ClearFocus().
        /// </summary>
        public bool InFocus
        {
            get { return parentDialog.CurrentFocusWidget == this; }
        }

        /// <summary>
        /// Only applies to some widgets ie checkboxes and 
        /// buttons.  For buttons this is the "pressed" state.
        /// </summary>
        public virtual bool Selected
        {
            get { return selected; }
            set 
            {
                if (value != selected)
                {
                    selected = value;
                }
            }
        }

        public bool Focusable
        {
            get { return focusable && !Disabled; }
            set { focusable = value; }
        }

        /// <summary>
        /// Given the size of the widget (ie the rect), Margin is added to the outside
        /// when aligning inside a WidgetSet.  The Margins of neighboring widgets are 
        /// allowed to overlap.
        /// Padding is added inside the rect around the content.  No overlap is possible.
        /// Best practices tend to favor using Margin over Padding where reasonable.
        /// </summary>
        public Padding Padding
        {
            get { return padding; }
            set
            {
                if (padding != value)
                {
                    padding = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Given the size of the widget (ie the rect), Margin is added to the outside
        /// when aligning inside a WidgetSet.  The Margins of neighboring widgets are 
        /// allowed to overlap.
        /// Padding is added inside the rect around the content.  No overlap is possible.
        /// Best practices tend to favor using Margin over Padding where reasonable.
        /// </summary>
        public Padding Margin
        {
            get { return margin; }
            set 
            {
                if (margin != value)
                {
                    margin = value;
                    Dirty = true;
                }
            }
        }

        public bool Dirty
        {
            get { return dirty; }
            set
            {
                if (dirty != value)
                {
                    dirty = value;
                }
                // Pass dirty state up to parent.  This is needed mostly
                // for widgets owning Twitchable members.  When the value
                // of a member is changed, this dirty flag is set which then
                // gets percolated up to the parent dialog.
                if (value && parentDialog != null)
                {
                    parentDialog.Dirty = true;
                }
            }
        }

        /// <summary>
        /// Alpha used for rendering overall widget.  If alpha == 0
        /// widget is not rendered at all.
        /// </summary>
        public virtual float Alpha
        {
            get { return alpha.Value; }
            set 
            {
                if (alpha.TargetValue != value)
                {
                    alpha.Value = value;
                }
            }
        }

        /// <summary>
        /// Render overlays to help debug margin and padding.  Requires
        /// that base.Render() get called by derived classes.
        /// </summary>
        public bool RenderMarginDebug
        {
            get { return marginDebug; }
            set { marginDebug = value; }
        }

        /// <summary>
        /// Optional string id for widget.  The idea here is to be able
        /// to use this for things like switch statements when you have a 
        /// collection of related widgets.  In a lot of the older code we
        /// used the labelText or labelId but with things like GraphicButtons
        /// we don't have labels. If possible, use an enum based identifier
        /// instead since that gives you type checking and helps prevent typos.
        /// </summary>
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Generic ref for any data we want to associate with this widget.
        /// </summary>
        public object Data
        {
            get { return data; }
            set { data = value; }
        }

        #endregion

        #region Public

        public BaseWidget(BaseDialog parentDialog, Callback OnChange = null, ThemeSet theme = null, string id = null, object data = null)
        {
            this.parentDialog = parentDialog;
            this.onChange = OnChange;
            this.id = id;
            this.data = data;

            if (theme != null)
            {
                this.theme = theme;
            }
            else
            {
                this.theme = Theme.CurrentThemeSet;
            }

            alpha = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this, startingValue: 1.0f);

        }   // c'tor

        /// <summary>
        /// Recalcs all the sizing and positioning for the widget.  
        /// Should only do anything if dirty.  Should clear dirty.
        /// </summary>
        /// <param name="parentPosition"></param>
        public virtual void Recalc(Vector2 parentPosition)
        {
            this.parentPosition = parentPosition;

            if (dirty)
            {
                dirty = false;
            }
        }   // end of Recalc()

        /// <summary>
        /// Goes through whole tree of children and ensures parentPosition
        /// is correct.  This should only need to be called after a dialog
        /// calls Recalc.  (so no need for user calling...)
        /// </summary>
        /// <param name="parentPosition">This position is relative to position of dialog.</param>
        public virtual void RecalcParentPosition(Vector2 parentPosition)
        {
            this.parentPosition = parentPosition;
        }   // end of RecalcParentPosition()

        public virtual void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            Debug.Assert(parentDialog != null, "We should always have a valid parent dialog.");

            this.parentPosition = parentPosition;

            // Is the mouse over us?
            Hover = false;
            if (KoiLibrary.LastTouchedDeviceIsMouse)
            {
                if (State != UIState.Disabled)
                {
                    // Only set hover if no buttons are pressed.
                    if (!LowLevelMouseInput.Left.IsPressed && !LowLevelMouseInput.Right.IsPressed && !LowLevelMouseInput.Middle.IsPressed)
                    {
                        Hover = KoiLibrary.InputEventManager.MouseHitObject == this;
                    }
                }
            }

            if (Dirty)
            {
                Recalc(parentPosition);
            }

        }   // end of Update()

        public virtual void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            if (alpha.Value > 0)
            {
                if (marginDebug)
                {
                    // Debug overlay, full rect.  This could actually go on BaseWidget Render() if we called it regularly...
                    RectangleF marginRect = new RectangleF(LocalRect);
                    marginRect.Position += parentPosition;
                    marginRect.Position -= new Vector2(margin.Left, margin.Top);
                    marginRect.Size += new Vector2(margin.Horizontal, margin.Vertical);
                    RoundedRect.Render(camera, marginRect, 0, Color.White * 0.25f);

                    RectangleF fullRect = new RectangleF(LocalRect);
                    fullRect.Position += parentPosition;
                    RoundedRect.Render(camera, fullRect, 0, Color.Blue * 0.25f);

                    RectangleF paddingRect = new RectangleF(LocalRect);
                    paddingRect.Position += parentPosition;
                    paddingRect.Position += new Vector2(padding.Left, padding.Top);
                    paddingRect.Size -= new Vector2(padding.Horizontal, padding.Vertical);
                    RoundedRect.Render(camera, paddingRect, 0, Color.Red * 0.25f);
                }
            }   // end if alpha > 0
        }   // end of Render()

        /// <summary>
        /// Activate.
        /// </summary>
        /// <param name="args">optional argument list.  Most Scenes will not use one but for those cases where it's needed this is here.</param>
        public virtual void Activate(params object[] args)
        {
            if (!Active)
            {
                if (args != null)
                {
                    foreach (object arg in args)
                    {
                        // Do something with each arg...
                    }
                }

                RegisterForInputEvents();

                state = UIState.Active;
            }
        }   // end of Activate()

        public virtual void Deactivate()
        {
            if (!Inactive)
            {
                UnregisterForInputEvents();

                State = UIState.Inactive;
            }
        }   // end of Deactivate()

        public virtual void Disable()
        {
            if (!Disabled)
            {
                UnregisterForInputEvents();
                // If we happen to be in focus when getting disabled,
                // tell our parent to skip to the next focusable widget.
                if (parentDialog.CurrentFocusWidget == this)
                {
                    parentDialog.FocusNextWidget();
                }

                Hover = false;

                State = UIState.Disabled;
            }
        }   // end of Disable()


        /// <summary>
        /// Returns the minimum size of this widget if no
        /// compression is done.  For widgets that can't be
        /// compressed, just returns the actual size.
        /// </summary>
        /// <returns></returns>
        public virtual Vector2 CalcMinSize()
        {
            return LocalRect.Size;
        }   // end of CalcMinSize()

        /// <summary>
        /// Test if this widget can have focus.
        /// 
        /// We do this via a method call so compound widgets (WidgetSet) can work.
        /// That's also why we return the widget rather than just a bool since
        /// the returned widget may be a widget that is part of a set.
        /// </summary>
        /// <param name="overrideInactive">Normally a widget can't have focus set on it if inactive.  But if this is true we do it anyway.  This can be used in Dialog.Activate() since the dialog's widgets will imediately get activated.</param>
        /// <returns>True if widget can be in focus.  False if not.</returns>
        protected virtual bool IsFocusable(bool overrideInactive = false)
        {
            if (focusable && (overrideInactive || Active))
            {
                return true;
            }

            return false;
        }   // end of IsFocusable()

        /// <summary>
        /// Set focus on this widget.  If another widget in this dialog
        /// is already in focus then it will be unfocused first.
        /// Note that focus is controlled by the dialog.  Widgets
        /// don't keep track of weather or not they are in focus.
        /// </summary>
        /// <returns>True if focus is set, false if not.</returns>
        public virtual bool SetFocus(bool overrideInactive = false)
        {
            if (IsFocusable(overrideInactive))
            {
                // If not already in focus.
                if (ParentDialog.CurrentFocusWidget != this)
                {
                    ParentDialog.CurrentFocusWidget = this;
                }
                dirty = true;

                return true;
            }
            else
            {
                Debug.Assert(false, "Can't focus on something that's not focusable.");
            }

            return false;
        }   // end of SetFocus()

        public virtual void ClearFocus()
        {
            if (IsFocusable())
            {
                if (parentDialog.CurrentFocusWidget == this)
                {
                    parentDialog.CurrentFocusWidget = null;
                }
                dirty = true;
            }
        }   // end of ClearFocus()

        /// <summary>
        /// Helper function for setting nav links.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="prev"></param>
        public void SetTabList(BaseWidget next, BaseWidget prev)
        {
            TabListNext = next;
            TabListPrev = prev;
        }   // end of SetTabList()

        /// <summary>
        /// Helper function for setting nav links.
        /// </summary>
        /// <param name="up"></param>
        /// <param name="down"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void SetPadList(BaseWidget up, BaseWidget down, BaseWidget left, BaseWidget right)
        {
            DPadListUp = up;
            DPadListDown = down;
            DPadListLeft = left;
            DPadListRight = right;
        }   // end of SetPadList()

        public void OnChange()
        {
            if (onChange != null)
            {
                onChange(this);
            }
        }

        /// <summary>
        /// Sets the OnSelect callback for this button.  Usually buttons are created
        /// with this set and never change it.  This is for the shared dialogs in DialogCenter 
        /// where we want to be able to update the behaviour of the dialog's buttons.
        /// </summary>
        /// <param name="onChange"></param>
        public virtual void SetOnChange(Callback onChange)
        {
            this.onChange = onChange;
        }

        #endregion

        #region InputEventHandler

        /// <summary>
        /// Default version of widget HitTest.  Just looks at position and size.
        /// Note this requires mouse position to be offset by parent position.
        /// </summary>
        /// <param name="mouse"></param>
        /// <returns></returns>
        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            InputEventHandler result = null;

            // Test self.
            if (localRect.Contains(hitLocation))
            {
                result = this;
            }

            return result;
        }   // end of HitTest()

        #endregion

        #region Internal

        public virtual void LoadContent()
        {
        }

        public virtual void UnloadContent()
        {
        }

        public virtual void DeviceResetHandler(object sender, EventArgs e)
        {
        }

        #endregion

    }   // end of class BaseWidget

}   // end of namespace KoiX.UI
