
// Uncomment this to debug calls to BaseDialog.
//#define DEBUG_ACTIVITY

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
using KoiX.Managers;
using KoiX.Text;

using Boku.Audio;

namespace KoiX.UI
{
    using Keys = Microsoft.Xna.Framework.Input.Keys;

    /// <summary>
    /// Base class for all modal dialogs.  
    /// BaseDialogNonModal derives from this.
    /// </summary>
    public abstract class BaseDialog : InputEventHandler, IDeviceResetX, IDirty
    {
        public delegate void Callback(BaseDialog d);

        protected enum State
        {
            Inactive,       // No Update, no Render.
            Active,         // Update and Render.
        }

        const float secondGapFudgeFactor = 0.2f;    // Fraction of second gap we add to the first gap when computing the dPad links.
                                                    // The idea here is to favor mostly horizontal or vertical links rather than
                                                    // diagonal ones.  By adding a bit of the second gap we favor links where the
                                                    // second gap is very small even if the primary gap is slightly bigger.

        #region Members

#if DEBUG
        /// <summary>
        /// String that we only use in debug mode.  
        /// Put whatever you want here to make life
        /// easier.  Never use this for anything else.
        /// </summary>
        public string _name = "";
#endif 

        protected State state = State.Inactive;

        // The rect for this dialog.  If set externally this should trigger a Recalc().
        protected RectangleF rect;

        // All our children, well sort-of.  This is all of our top level children.
        // Widgets which are contained by a WidgetSet should not be added to this
        // list, only the top level WidgetSet should be added.
        // Note, don't make this visible to derived classes.  We want to force
        // going through AddWidget().  But, that also make's it inaccessible for 
        // reading. Arg.
        protected List<BaseWidget> widgets = new List<BaseWidget>();

        /// <summary>
        /// Is this dialog in focus?  Note this is just local state
        /// so we can catch transitions.  The final arbiter of which
        /// dialog is in focus is DialogManager.FocusDialog.
        /// </summary>
        bool inFocus = false;

        /// <summary>
        /// Can this dialog be in focus.  Will be true for most dialogs
        /// but prevents FullScreenDialog and CanvasDialog from being
        /// given focus.
        /// </summary>
        protected bool focusable = true;

        /// <summary>
        /// Widget that currently has focus.
        /// </summary>
        protected BaseWidget currentFocusWidget;
        protected BaseWidget prevFocusWidget;       // Used to restore focus when a modal dialog goes away.

        protected BaseWidget firstFocusWidget;
        protected BaseWidget lastFocusWidget;

        protected bool renderBaseTile = true;   // Should the underlying tile be rendered?
        protected bool hitTestable = false;     // Should the dialog respond to hit testing?  If false, we only test
                                                // the widgets and not the base region.  When set to true it's useful
                                                // for click-drag on the dialog.

        protected bool quiet = false;           // Used to turn off sound effects.

        protected bool ignoreGamepadBackButton = false; // By default, dialogs use GamePad.Back to exit.  Setting
                                                        // this to true overrides that behaviour and lets Back
                                                        // be used for other things.

        protected bool dirty = true;    // Need to force all child widgets dirty.

        protected int frameLastActivated = 0;   // What frame was this last activated on?

        /// <summary>
        /// Theme params for setting the visual style.
        /// </summary>

        protected ThemeSet theme = Theme.CurrentThemeSet;

        protected Twitchable<Color> bodyColor;
        protected Twitchable<float> cornerRadius;
        protected Twitchable<Padding> padding;
        protected Twitchable<Color> outlineColor;
        protected Twitchable<float> outlineWidth;

        protected BevelStyle bevelStyle = BevelStyle.None;
        protected Twitchable<float> bevelWidth;

        protected ShadowStyle shadowStyle = ShadowStyle.None;
        protected Twitchable<Color> shadowColor;
        protected Twitchable<float> shadowSize;
        protected Twitchable<Vector2> shadowOffset;

        protected Callback onDeactivate = null;

        Color backdropColor = Color.Transparent;        // For modal dialogs, this is the color we lay under the dialog.
                                                        // Should probably be semi-transparent.
        Color curBackdropColor = Color.Transparent;     // This is the current color used for rendering.  May be being twitched.

        #endregion

        #region Accessors

        /// <summary>
        /// Currently rendering and accepting input.
        /// </summary>
        public bool Active
        {
            get { return state == State.Active; }
        }

        /// <summary>
        /// Not rendering, not accepting input.
        /// </summary>
        public bool Inactive
        {
            get { return state == State.Inactive; }
        }

        public ThemeSet ThemeSet
        {
            get { return theme; }
        }

        /// <summary>
        /// The widget in the dialog that currently has focus.
        /// </summary>
        public BaseWidget CurrentFocusWidget
        {
            get { return currentFocusWidget; }
            set 
            {
                prevFocusWidget = currentFocusWidget;
                currentFocusWidget = value; 
            }
        }

        /// <summary>
        /// The widget in the dialog that previously had focus.
        /// 
        /// The setter is only there to clear this.  It should never
        /// be used with anything except null.
        /// (****) TODO Should I remove the setter and just make a 
        /// method to clear the value?
        /// </summary>
        public BaseWidget PrevFocusWidget
        {
            get { return prevFocusWidget; }
            set
            {
                Debug.Assert(value == null, "Should only be used for clearing...");
                prevFocusWidget = value;
            }
        }

        /// <summary>
        /// Should the underlying tile of the dialog be rendered?
        /// True by default.
        /// </summary>
        public bool RenderBaseTile
        {
            get { return renderBaseTile; }
            set
            {
                if (value != renderBaseTile)
                {
                    renderBaseTile = value;
                    dirty = true;
                }
            }
        }

        /// <summary>
        /// Forces all child widgets to recalc selves.
        /// </summary>
        public bool Dirty
        {
            get { return dirty; }
            set
            {
                if (dirty != value)
                {
                    dirty = value;
                }
            }
        }

        public bool Quiet
        {
            get { return quiet; }
            set 
            {
                if (quiet != value)
                {
                    quiet = value;
                }
            }
        }

        /// <summary>
        /// Bounding rect for this dialog.
        /// Dialogs should be explicitly positioned and sized.
        /// </summary>
        public RectangleF Rectangle
        {
            get { return rect; }
            set
            {
                if (rect != value)
                {
                    rect = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Is this dialog focusable?  Note that if the dialog has no widgets
        /// it can't be focusable otherwise the DialogManager gets mad.
        /// </summary>
        public bool Focusable
        {
            get { return focusable && widgets.Count > 0; }
            set { focusable = value; }
        }

        /// <summary>
        /// For modal dialogs, this is the color that is overlayed
        /// undernerath the dialog.  Should be transparent to show
        /// the underlying content.
        /// </summary>
        public Color BackdropColor
        {
            get { return backdropColor; }
            set { backdropColor = value; }
        }   

#if DEBUG
        
        /// <summary>
        /// All our children, well sort-of.  This is all of our top level children.
        /// Widgets which are contained by a WidgetSet should not be added to this
        /// list, only the top level WidgetSet should be added.
        /// Note, don't make this visible to derived classes.  We want to force
        /// going through AddWidget().
        /// 
        /// Only available in debug mode since it's convenient.
        /// </summary>
        public List<BaseWidget> WidgetsDEBUGONLY
        {
            get { return widgets; }
        }

#endif

        public Color BodyColor 
        {
            get { return bodyColor.Value; }
        }
        public float CornerRadius
        {
            get { return cornerRadius.Value; }
        }
        public Color OutlineColor
        {
            get { return outlineColor.Value; }
        }
        public float OutlineWidth
        {
            get { return outlineWidth.Value; }
        }
        public BevelStyle BevelStyle
        {
            get { return bevelStyle; }
        }
        public float BevelWidth
        {
            get { return bevelWidth.Value; }
        }
        public ShadowStyle ShadowStyle
        {
            get { return shadowStyle; }
        }
        public Color ShadowColor
        {
            get { return shadowColor.Value; }
        }
        public float ShadowSize
        {
            get { return shadowSize.Value; }
        }
        public Vector2 ShadowOffset
        {
            get { return shadowOffset.Value; }
        }

        /// <summary>
        /// Allows outside code to set a trigger on this dialog when deactivating.
        /// </summary>
        public Callback OnDeactivate
        {
            get { return onDeactivate; }
            set 
            {
                onDeactivate = value; 
            }
        }

        /// <summary>
        /// By default, dialogs will exit on Gamepad.Back.  This allows that
        /// to be overridden so other widgets can handle Back.
        /// </summary>
        public bool IgnoreGamepadBackButton
        {
            get { return ignoreGamepadBackButton; }
            set { ignoreGamepadBackButton = value; }
        }

        #endregion

        #region Public

        public BaseDialog(ThemeSet theme = null)
        {
            if (theme != null)
            {
                this.theme = theme;
            }
            else
            {
                this.theme = Theme.CurrentThemeSet;
            }

            // Causes all processing to stop when this InputEventHandler is found
            // even if it's not consuming input.  Only used by DialogManager to
            // prevent any active UI underneath a modal dialog from getting input.
            IsModalDialog = true;

            backdropColor = curBackdropColor = this.theme.BackdropColor;

            // Init Twitchables.
            bodyColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this);
            cornerRadius = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this);
            padding = new Twitchable<Padding>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this);
            outlineColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this);
            outlineWidth = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this);
            bevelWidth = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this);
            shadowColor = new Twitchable<Color>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this);
            shadowSize = new Twitchable<float>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this);
            shadowOffset = new Twitchable<Vector2>(Theme.QuickTwitchTime, TwitchCurve.Shape.EaseInOut, this);
        }   // end of c'tor

        /// <summary>
        /// Add a widget to this dialog.  
        /// 
        /// Note that widgets contained in a WidgetSet should not be added.  
        /// In that case you only need to add the owning WidgetSet.
        /// </summary>
        /// <param name="widget"></param>
        public void AddWidget(BaseWidget widget)
        {
            Debug.Assert(widget.ParentDialog == this, "Why is this not in sync?");

            if (!focusable)
            {
                widget.Focusable = false;
            }
            widgets.Add(widget);
            Dirty = true;
        }   // end of AddWidget()

        /// <summary>
        /// Get the [index] widget for this dialog.
        /// Returns null if index is out of range for value widgets.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public BaseWidget GetWidget(int index)
        {
            BaseWidget result = null;

            if (index >= 0 && index < widgets.Count)
            {
                result = widgets[index];
            }

            return result;
        }   // end of GetWidget()

        /// <summary>
        /// Recalcualtes the layout of the dialog.  Broken out from Update()
        /// so we can call this even when not active.
        /// </summary>
        public virtual void Recalc()
        {
            foreach (BaseWidget widget in widgets)
            {
                // All widgets are in parent dialog's coordinate space.
                widget.Recalc(Vector2.Zero);
            }

        }   // end of Recalc()

        /// <summary>
        /// Update should focus on things that are changing frame to frame.  Things
        /// that change once and then stay the same for most or all of the life of
        /// the dialog should go into Recalc().
        /// </summary>
        /// <param name="camera"></param>
        public virtual void Update(SpriteCamera camera)
        {
            // Give a few frame to settle positions then calc dPad links.
            // This feels very hackish.  Basically, at c'tor time we don't
            // have all the layout done so we can't do the right thing
            // wrt dPad links.  So, 5 frames after activation, we assume
            // that positioning will have settled down and we then create
            // the links.
            if (frameLastActivated + 5 == Time.FrameCounter)
            {
                CreateDPadLinks();
            }

            // Update Theme elements.
            if (RenderBaseTile)
            {
                // Are we focused?
                if (DialogManagerX.CurrentFocusDialog == this)
                {
                    bodyColor.Value = theme.DialogBodyTileFocused.TileColor;
                    cornerRadius.Value = theme.DialogBodyTileFocused.CornerRadius;
                    padding.Value = theme.DialogBodyTileFocused.Padding;
                    outlineColor.Value = theme.DialogBodyTileFocused.OutlineColor;
                    outlineWidth.Value = theme.DialogBodyTileFocused.OutlineWidth;

                    bevelStyle = theme.DialogBodyTileFocused.BevelStyle;
                    bevelWidth.Value = theme.DialogBodyTileFocused.BevelWidth;

                    shadowStyle = theme.DialogBodyTileFocused.ShadowStyle;
                    shadowColor.Value = Color.Black * theme.DialogBodyTileFocused.ShadowAlpha;
                    shadowSize.Value = theme.DialogBodyTileFocused.ShadowSize;
                    shadowOffset.Value = theme.DialogBodyTileFocused.ShadowOffset;
                }
                else
                {
                    bodyColor.Value = theme.DialogBodyTileNormal.TileColor;
                    cornerRadius.Value = theme.DialogBodyTileNormal.CornerRadius;
                    padding.Value = theme.DialogBodyTileNormal.Padding;
                    outlineColor.Value = theme.DialogBodyTileNormal.OutlineColor;
                    outlineWidth.Value = theme.DialogBodyTileNormal.OutlineWidth;

                    bevelStyle = theme.DialogBodyTileNormal.BevelStyle;
                    bevelWidth.Value = theme.DialogBodyTileNormal.BevelWidth;

                    shadowStyle = theme.DialogBodyTileNormal.ShadowStyle;
                    shadowColor.Value = Color.Black * theme.DialogBodyTileNormal.ShadowAlpha;
                    shadowSize.Value = theme.DialogBodyTileNormal.ShadowSize;
                    shadowOffset.Value = theme.DialogBodyTileNormal.ShadowOffset;
                }
            }

            // If dirty, pass that on to all our children.  We 
            // clear the flag after we've used it down below.
            if (Dirty)
            {
                foreach (BaseWidget widget in widgets)
                {
                    widget.Dirty = true;
                }

                Recalc();
            }
                
            if (state == State.Active)
            {
                // Hit testing.
                // Mouse hit needs to be adjusted for camera.
                //Vector2 hitCamera = mouse;
                //mouseCamera.point = camera.ScreenToCamera(mouse.point);

                // Asserts to make sure focus tracking is working correctly.
                if (DialogManagerX.CurrentFocusDialog == this)
                {
                    Debug.Assert(inFocus, "DialogManager thinks we're in focus, why don't we?");
                }
                else
                {
                    Debug.Assert(!inFocus, "DialogManager doesn't think we're in focus, why do we?");
                }

                // Update widgets.
                foreach (BaseWidget widget in widgets)
                {
                    Debug.Assert(widget.ParentDialog != null, "This should never be null.  If it is figure out what went wrong.");
                    widget.Update(camera, parentPosition: Vector2.Zero);
                }
            }   // end if Active

            // Done with Dirty flag, reset.
            Dirty = false;

        }   // end of Update()

        public virtual void Render(SpriteCamera camera)
        {
            if (state != State.Inactive)
            {
                // Cull if possible.
                if (camera.CullTest(rect) == Boku.Common.Frustum.CullResult.TotallyOutside)
                {
                    return;
                }

                SpriteBatch batch = KoiLibrary.SpriteBatch;

                RenderBackdrop();

                if (RenderBaseTile)
                {
                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState: null, depthStencilState: null, rasterizerState: null, effect: null, transformMatrix: camera.ViewMatrix);
                    {
                        // Render base.
                        RoundedRect.Render(camera, rect, cornerRadius.Value, bodyColor.Value,
                                            outlineColor: outlineColor.Value, outlineWidth: outlineWidth.Value,
                                            bevelStyle: bevelStyle, bevelWidth: bevelWidth.Value,
                                            shadowStyle: shadowStyle, shadowOffset: shadowOffset.Value, shadowSize: shadowSize.Value, shadowAttenuation: 0.85f);
                    }
                    batch.End();
                }

                RenderWidgets(camera);
            }
        }   // end of Render()

        protected void RenderWidgets(SpriteCamera camera)
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.Render(camera, rect.Position);
            }
        }   // end of RenderWidgets.

        /// <summary>
        /// Version of RenderWidgets that takes a parentPosition 
        /// as input instead of using the dialog's rect position.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="basePosition"></param>
        protected void RenderWidgets(SpriteCamera camera, Vector2 basePosition)
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.Render(camera, basePosition);
            }
        }   // end of RenderWidgets.

        /// <summary>
        /// Renders a backdrop under the current dialog.  For now
        /// just tints the background to show that the dialog is modal
        /// and to focus the user on it.
        /// </summary>
        protected void RenderBackdrop()
        {
            if (IsModalDialog && curBackdropColor.A > 0)
            {
                SpriteBatch batch = KoiLibrary.SpriteBatch;

                batch.Begin();
                batch.Draw(SharedX.WhiteTexture, new Rectangle(0, 0, KoiLibrary.ViewportSize.X, KoiLibrary.ViewportSize.Y), curBackdropColor);
                batch.End();
            }

        }   // end of RenderBackdrop()

        /// <summary>
        /// Should always be called by derived classes.  If overridden, this base version
        /// must be called _after_ all other registration is done.
        /// </summary>
        public override void RegisterForInputEvents()
        {
            if (Focusable)
            {
                // Register with keyboard and gamepad so allow navigation between widgets.
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
            }

            base.RegisterForInputEvents();

            /*
            // Don't register widgets here.  They register themselves when Activated.
            // In ToolBarDialog it would be nice to register these.  Instead we have to do it manually.
            // There we are unregisatering and re-registering to get the correct stack placement since
            // we want the buttons to be higher in the stack than the tools themselves.
             
            // Register widgets.  We register these last so they have higher priority than dialog.
            foreach (BaseWidget widget in widgets)
            {
                widget.RegisterForInputEvents();
            }
            */

        }   // end of RegisterForInputEvents()

        public override void UnregisterForInputEvents()
        {
            // Unregister widgets.
            // TODO (****)  Do we need to do this here or do the widgets
            // take care of themselves when deactivated?
            foreach (BaseWidget widget in widgets)
            {
                widget.UnregisterForInputEvents();
            }

            // We do this before calling PopSet.  This ensures that this dialog
            // has unregistered so the internal debug asserts don't fire.  In
            // reality it doesn't matter but the asserts can help catch any 
            // issues with the Input Set handling.
            base.UnregisterForInputEvents();

        }   // end of UnregisterForInputEvents()

        /// <summary>
        /// Activate this dialog.
        /// This should be overridden.
        /// </summary>
        /// <param name="args">optional argument list.  Most Scenes will not use one but for those cases where it's needed this is here.</param>
        public virtual void Activate(params object[] args)
        {
            if (!Active)
            {
                // Activate.

                // Process args.
                foreach (object arg in args)
                {
                    // Do something with each arg...
                }

                state = State.Active;

                // If modal, push a new set before registering ourself.
                if (IsModalDialog)
                {
                    KoiLibrary.InputEventManager.PushSet();
                }

                // Register self first so widgets get higher priority.
#if DEBUG_ACTIVITY
                Debug.Print("BaseDialog RegisterForInputEvents");
#endif
                RegisterForInputEvents();

#if DEBUG_ACTIVITY
                Debug.Print("BaseDialog Activate widgets");
#endif
                foreach (BaseWidget widget in widgets)
                {
                    widget.Activate();
                }

                // Twitch in backdrop color.
                // TODO (****) Can we also have this fade out on deactivate?
                TwitchManager.Set<Color> set = delegate(Color val, object param) { curBackdropColor = val; };
                TwitchManager.CreateTwitch<Color>(Color.Transparent, backdropColor, set, 0.4f, TwitchCurve.Shape.EaseOut, this);

                frameLastActivated = Time.FrameCounter;
            }
        }   // end of default Activate()

        public virtual void Deactivate()
        {
            if (!Inactive)
            {
                state = State.Inactive;
                UnregisterForInputEvents();

                foreach (BaseWidget widget in widgets)
                {
                    widget.Deactivate();
                }

                if (onDeactivate != null)
                {
                    onDeactivate(this);
                }

                // If modal, pop set after everything is unregistered.
                if (IsModalDialog)
                {
                    KoiLibrary.InputEventManager.PopSet();
                }
            }
        }   // end of default Deactivate()

        /// <summary>
        /// Sets this dialog as having focus.  This also sets focus on the
        /// first widget in this dialog.
        /// 
        /// Note: if this dialog is not focusable then after calling
        /// SetFocus() the dialog's foucsWidget will still be null.
        /// </summary>
        /// <param name="topOfList">Set focus on the first widget?  If false, we set focus on the last.</param>
        public virtual void SetFocus(bool topOfList = true)
        {
            //Debug.Assert(currentFocusWidget == null, "Why wasn't this previously cleared?");
            Debug.Assert(!inFocus, "Why are we trying to set focus on a dialog that's already in focus?");
            Debug.Assert(focusable, "This dialog is not focusable, why are we trying to set focus on it?");

            if (focusable)
            {
                inFocus = true;

                ValidateFocus();

                if (CurrentFocusWidget == null && prevFocusWidget != null)
                {
                    CurrentFocusWidget = prevFocusWidget;
                    prevFocusWidget = null;
                }

                // If we don't already have a focus widget, find one.
                // The reason we may already have one is that another dialog popped up over this
                // one.  In that case we want to return focus to whichever widget previously 
                // had focus.
                if (CurrentFocusWidget == null)
                {
                    // Pick which widget to focus on.
                    CurrentFocusWidget = topOfList ? firstFocusWidget : lastFocusWidget;

                    if (CurrentFocusWidget == null)
                        return;

                    // TODO (****) Consider changing this. Overall it doesn't make sense 
                    // but it does help during development to be able to set up a scene
                    // as a placeholder and have it work.
                    Debug.Assert(CurrentFocusWidget != null, "All focusable dialogs must have at least 1 focusable widget.");

                    // CurrentFocusWiget may be disabled, so find next one.
                    if (!CurrentFocusWidget.Focusable)
                    {
                        if (topOfList)
                        {
                            FocusNextWidget();
                        }
                        else
                        {
                            FocusPrevWidget();
                        }
                    }
                }
                CurrentFocusWidget.SetFocus();
            }
        }   // end of SetFocus()

        /// <summary>
        /// Ensures that CurrentFocusWidget and preFocusWidget are
        /// actually valid.  The case where they may not be is something like
        /// in the ObjectSettings pages.  If we first click on a Kodu and then
        /// have the focus on MovementSpeed, exit, click on a rock and navigate
        /// to the Movement page.  It will try and set focus on the MovementSpeed
        /// widget, but that widget is not in the list since rocks don't move.
        /// This code, instead of traversing the widget hierarchy, just looks at
        /// the Active statis of the widgets.
        /// </summary>
        void ValidateFocus()
        {
            if (CurrentFocusWidget != null && !CurrentFocusWidget.Active)
            {
                CurrentFocusWidget = null;
            }
            if (prevFocusWidget != null && !prevFocusWidget.Active)
            {
                prevFocusWidget = null;
            }

        }   // end of ValidateFocus()

        /// <summary>
        /// Set this dialog so it does not have focus.
        /// Clears the focus on all it's widgets.
        /// </summary>
        public virtual void ClearFocus()
        {
            if (inFocus)
            {
                inFocus = false;
                if (CurrentFocusWidget != null)
                {
                    CurrentFocusWidget.ClearFocus();
                }
            }
        }   // end of ClearFocus();

        /// <summary>
        /// Creates a tab list.  The tab ordering is determined by the ordering of the arguments.
        /// Also sets first and last focusWidget to first and last objects in list.
        /// 
        /// If args list is 0-length then uses all the widgets in the dialog.
        /// </summary>
        /// <param name="args"></param>
        public void CreateTabList(params object[] args)
        {
            // If no args are passed in get full list of focussable widgets and use that.
            if (args.Length == 0)
            {
                List<BaseWidget> focusWidgets = new List<BaseWidget>();
                GetFocusableWidgets(focusWidgets);
                args = focusWidgets.ToArray();
            }

            for (int i = 0; i < args.Length; i++)
            {
                BaseWidget cur = args[i] as BaseWidget;
                Debug.Assert(cur != null, "All args must be BaseWidgets.");
                Debug.Assert(cur.Focusable, "All args must be focusable.");

                BaseWidget prev = (i > 0 ? args[i - 1] : null) as BaseWidget;
                BaseWidget next = (i < args.Length - 1 ? args[i + 1] : null) as BaseWidget;

                cur.SetTabList(next, prev);

                if (i == 0)
                {
                    firstFocusWidget = cur;
                }
                else if (i == args.Length - 1)
                {
                    lastFocusWidget = cur;
                }
            }
        }   // end of CreateTabList()

        /// <summary>
        /// Creates the DPad links for the given widgets. If args
        /// list is 0-length then uses all the widgets in the dialog.
        /// 
        /// Remember that SpriteCamera uses -y as 'up' on the screen.
        /// The up, down, left, and right directions here are the directions
        /// relative to the user that the user is trying to move the focus.
        /// </summary>
        /// <param name="args"></param>
        public void CreateDPadLinks(params object[] args)
        {
            List<BaseWidget> widgetList = new List<BaseWidget>();
            
            // If we have args, create the list from them.
            if (args.Length > 0)
            {
                foreach (object obj in args)
                {
                    BaseWidget cur = obj as BaseWidget;
                    Debug.Assert(cur != null, "All args must be BaseWidgets.");
                    widgetList.Add(cur);
                }
            }
            else
            {
                // No args, so collect all the focusable widgets.
                GetFocusableWidgets(widgetList, dPadNav: true);
            }

            // Calc up, down, left, and right neighbors.
            for (int i = 0; i < widgetList.Count; i++)
            {
                BaseWidget cur = widgetList[i];

                // Start by nulling out any exisitng values.
                cur.DPadListUp = null;
                cur.DPadListDown = null;
                cur.DPadListLeft = null;
                cur.DPadListRight = null;

                CalcUpLink(widgetList, cur);
                CalcDownLink(widgetList, cur);
                CalcLeftLink(widgetList, cur);
                CalcRightLink(widgetList, cur);
            }

        }   // end of CreateDPadLinks()

        public void FocusNextWidget()
        {
            Debug.Assert(CurrentFocusWidget != null, "If the dialog is focused then there should always be a widget in focus.");
            Debug.Assert(CurrentFocusWidget != CurrentFocusWidget.TabListNext, "Shouldn't have any widgets point to themselves.");

            // Follow chain to next focusable widget (or end of chain).
            BaseWidget nextWidget = CurrentFocusWidget.TabListNext;
            while (nextWidget != null && !nextWidget.Focusable)
            {
                nextWidget = nextWidget.TabListNext;
            }

            if (nextWidget != null)
            {
                nextWidget.SetFocus();
            }
            else
            {
                // At end of list?
                DialogManagerX.FocusNextDialog();

                // If we've tabbed through all the widgets and there isn't
                // another dialog to tab to (so we stay focussed) then we
                // should have tab wrap around to the beginning of the list.
                if (this == DialogManagerX.CurrentFocusDialog)
                {
                    firstFocusWidget.SetFocus();
                }
            }

            if (!Quiet)
            {
                Foley.PlayClickDown();
            }

        }   // end of FocusNextWidget()

        public void FocusPrevWidget()
        {
            Debug.Assert(CurrentFocusWidget != null, "If the dialog is focused then there should always be a widget in focus.");
            Debug.Assert(CurrentFocusWidget != CurrentFocusWidget.TabListPrev, "Shouldn't have any widgets point to themselves.");

            // Follow chain to next focusable widget (or end of chain).
            BaseWidget nextWidget = CurrentFocusWidget.TabListPrev;
            while (nextWidget != null && !nextWidget.Focusable)
            {
                nextWidget = nextWidget.TabListPrev;
            }

            if (nextWidget != null)
            {
                nextWidget.SetFocus();
            }
            else
            {
                // At end of list?
                DialogManagerX.FocusPrevDialog();

                // If we've shift-tabbed through all the widgets and there isn't
                // another dialog to tab to (so we stay focusssed) then we
                // should have tab wrap around to the end of the list.
                if (this == DialogManagerX.CurrentFocusDialog)
                {
                    lastFocusWidget.SetFocus();
                }

            }

            if (!Quiet)
            {
                Foley.PlayClickUp();
            }

        }   // end of FocusPrevWidget()

        /// <summary>
        /// Move the focus to the widget above this one.
        /// </summary>
        public void FocusUpWidget()
        {
            BaseWidget curFocus = CurrentFocusWidget;
            if (curFocus != null)
            {
                if (curFocus.DPadListUp != null)
                {
                    curFocus.DPadListUp.SetFocus();
                }
                else
                {
                    DialogManagerX.FocusUpDialog();
                }

                if (!Quiet)
                {
                    Foley.PlayClickUp();
                }
            }

        }   // end of FocusUpWidget()

        /// <summary>
        /// Move the focus to the widget below this one.
        /// </summary>
        public void FocusDownWidget()
        {
            BaseWidget curFocus = CurrentFocusWidget;
            if (curFocus != null)
            {
                if (curFocus.DPadListDown != null)
                {
                    curFocus.DPadListDown.SetFocus();
                }
                else
                {
                    DialogManagerX.FocusDownDialog();
                }

                if (!Quiet)
                {
                    Foley.PlayClickDown();
                }
            }

        }   // end of FocusDownWidget()

        /// <summary>
        /// Move the focus to the widget to the left of this one.
        /// </summary>
        public void FocusLeftWidget()
        {
            BaseWidget curFocus = CurrentFocusWidget;
            if (curFocus != null)
            {
                if (curFocus.DPadListLeft != null)
                {
                    curFocus.DPadListLeft.SetFocus();
                }
                else
                {
                    DialogManagerX.FocusLeftDialog();
                }

                if (!Quiet)
                {
                    Foley.PlayClickDown();
                }
            }

        }   // end of FocusLeftWidget()

        /// <summary>
        /// Move the focus to the widget to the right of this one.
        /// </summary>
        public void FocusRightWidget()
        {
            BaseWidget curFocus = CurrentFocusWidget;
            if (curFocus != null)
            {
                if (curFocus.DPadListRight != null)
                {
                    curFocus.DPadListRight.SetFocus();
                }
                else
                {
                    DialogManagerX.FocusRightDialog();
                }

                if (!Quiet)
                {
                    Foley.PlayClickUp();
                }
            }

        }   // end of FocusRightWidget()


        #endregion

        #region InputEventHandler

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitLocation">Hit location in camera coordinates.</param>
        /// <returns></returns>
        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            InputEventHandler result = null;

            // Test self (if we have a valid rect).  Also acts as a quick out for the whole dialog.
            // CanvasDialog will not have a valid rect.
            if (rect.IsEmpty || rect.Contains(hitLocation))
            {
                // May override with children.
                // Need to offset mouse position for children since they are relative to parent.
                if (!rect.IsEmpty)
                {
                    hitLocation.X -= (int)rect.Position.X;
                    hitLocation.Y -= (int)rect.Position.Y;
                }
                foreach (BaseWidget widget in widgets)
                {
                    result = widget.HitTest(hitLocation);
                    if (result != null)
                    {
                        break;
                    }
                }

                // If none of the children claim the hit, claim it for the dialog.
                if (result == null && hitTestable)
                {
                    result = this;
                }
            }
            else
            {
                // If hit is not over dialog, be sure to clear MouseOver for widgets.
                foreach (BaseWidget widget in widgets)
                {
                    WidgetSet w = widget as WidgetSet;
                    if (w != null)
                    {
                        w.MouseOver = false;
                    }
                }
            }

            return result;
        }   // end of HitTest()

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            Debug.Assert(Active, "Shouldn't be getting input unless active.");

            if (DialogManagerX.CurrentFocusDialog == this)
            {
                switch (input.Key)
                {
                    // Navigate tab linked list.
                    case Keys.Tab:
                        // If firstFocusWidget is null, then we don't have a 
                        // focus list, or the dialog is handling it manually.
                        // In either case, skip this and let the Tab pass through.
                        if (firstFocusWidget != null)
                        {
                            if (input.Shift)
                            {
                                FocusPrevWidget();
                            }
                            else
                            {
                                FocusNextWidget();
                            }
                            return true;
                        }
                        break;

                    // Navigate via dPad or arrows.
                    case Keys.Up:
                        // Note, if the tab list is null then we also
                        // assume that dpad/arrow nav is not being used.
                        // TODO (****) Is this valid?  Should we have a
                        // seperate way to check this?  bool set by the 
                        // CalcDpadLinks funtion?
                        if (firstFocusWidget != null)
                        {
                            FocusUpWidget();
                            return true;
                        }
                        break;
                    case Keys.Down:
                        if (firstFocusWidget != null)
                        {
                            FocusDownWidget();
                            return true;
                        }
                        break;
                    case Keys.Left:
                        if (firstFocusWidget != null)
                        {
                            FocusLeftWidget();
                            return true;
                        }
                        break;
                    case Keys.Right:
                        if (firstFocusWidget != null)
                        {
                            FocusRightWidget();
                            return true;
                        }
                        break;

                    default:
                        // Do nothing here, just let fall through to base.
                        break;
                }
            }

            return base.ProcessKeyboardEvent(input);
        }   // end of ProcessKeyboardEvent()

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active, "Shouldn't be getting input unless active.");

            // If this dialog is not focusable don't try and move focus.
            if (Focusable)
            {
                // Note: Button A press is handled by Button class.

                if (!ignoreGamepadBackButton && pad.Back.WasPressed)
                {
                    /*
                    if (onCancel != null)
                    {
                        onCancel(null);
                        return true;
                    }
                    */
                }

                // Allow widget navigation via either dPad or left stick.  Note that
                // this means for widgets like sliders the left stick may be used to
                // adjust the value.  In that case the slider's ProcessGamePadEvent()
                // should return true when value is non-zero.

                if (pad.DPadUp.WasPressedOrRepeat || pad.LeftStickUp.WasPressedOrRepeat)
                {
                    FocusUpWidget();
                    return true;
                }
                if (pad.DPadDown.WasPressedOrRepeat || pad.LeftStickDown.WasPressedOrRepeat)
                {
                    FocusDownWidget();
                    return true;
                }
                if (pad.DPadLeft.WasPressedOrRepeat || pad.LeftStickLeft.WasPressedOrRepeat)
                {
                    FocusLeftWidget();
                    return true;
                }
                if (pad.DPadRight.WasPressedOrRepeat || pad.LeftStickRight.WasPressedOrRepeat)
                {
                    FocusRightWidget();
                    return true;
                }
            }

            return base.ProcessGamePadEvent(pad);
        }   // end of ProcessGamePadEvent()

        #endregion

        #region Internal

        /// <summary>
        /// Gathers a list of all the focusable widgets
        /// </summary>
        /// <param name="resultList"></param>
        /// <param name="queryList">Null to start list at dialog's top level list.</param>
        /// <param name="dPadNav">Are we gathering this for calculating dPad navigation?</param>
        public void GetFocusableWidgets(List<BaseWidget> resultList, List<BaseWidget> queryList = null, bool dPadNav = false)
        {
            // Will be null when first called.
            // Will be non-null when called recursively for WidgetSets.
            if (queryList == null)
            {
                queryList = widgets;
            }

            foreach (BaseWidget widget in queryList)
            {
                // If the current one is a set, gather its children.
                WidgetSet set = widget as WidgetSet;

                // If we're gathering this list for doing navigation and this
                // set is a combo, then don't treat it as a set.  Just treat
                // it as a single widget.
                if (dPadNav && set != null && set.TreatAsSingleWidgetForNavigation)
                {
                    set = null;
                }
                if (set != null)
                {
                    GetFocusableWidgets(resultList, set.Widgets, dPadNav);
                }
                else
                {
                    if (widget.Focusable)
                    {
                        resultList.Add(widget);
                    }
                }
            }

        }   // end of GetFocusableWidgets()


        void CalcUpLink(List<BaseWidget> widgetList, BaseWidget cur)
        {
            int bestNumSplits = 3;  // Number of splitting axes between cur and other.
                                    // 1 is best, 2 is ok, 0 means they overlap so no links.
            BaseWidget bestOther = null;
            float bestGap = float.MaxValue;
            float bestSecondGap = float.MaxValue;   // Used to break ties in 2-split case when rightGaps are equal.

            for (int i = 0; i < widgetList.Count; i++)
            {
                BaseWidget other = widgetList[i];
                if (cur == other)
                    continue;

                float gapUp = (cur.ParentPosition.Y + cur.LocalRect.Top) - (other.ParentPosition.Y + other.LocalRect.Bottom);
                float gapDown = (other.ParentPosition.Y + other.LocalRect.Top) - (cur.ParentPosition.Y + cur.LocalRect.Bottom);
                float gapLeft = (cur.ParentPosition.X + cur.LocalRect.Left) - (other.ParentPosition.X + other.LocalRect.Right);
                float gapRight = (other.ParentPosition.X + other.LocalRect.Left) - (cur.ParentPosition.X + cur.LocalRect.Right);

                // Calc the number of axis aligned splitting planes that divide cur from other.  
                // This is just the count of non-negative gaps.  Will be 0, 1, or 2.
                int numSplits = 0;
                numSplits += gapUp >= 0 ? 1 : 0;
                numSplits += gapDown >= 0 ? 1 : 0;
                numSplits += gapLeft >= 0 ? 1 : 0;
                numSplits += gapRight >= 0 ? 1 : 0;

                if (gapUp < 0 || numSplits <= 0 || numSplits >= 3)
                {
                    continue;
                }

                // Calc second gap if we need it.
                float secondGap = float.MaxValue;
                if (numSplits == 2)
                {
                    if (gapLeft >= 0)
                    {
                        secondGap = Math.Min(secondGap, gapLeft);
                    }
                    if (gapRight >= 0)
                    {
                        secondGap = Math.Min(secondGap, gapRight);
                    }
                    gapUp += secondGapFudgeFactor * secondGap;
                }
                // In the case of a single split, use secondGap to determine
                // which widget is best aligned with the current one based
                // on the centers of their rects.
                if (numSplits == 1)
                {
                    secondGap = Math.Abs((cur.ParentPosition.X + cur.LocalRect.Center.X) - (other.ParentPosition.X + other.LocalRect.Center.X));
                }

                // If numSplits is less than best
                // Or if numSplits = bestNumSplits == 2 and gap is smaller
                // then we have a new better choice.
                if (numSplits == 1 && bestNumSplits > 1
                    || (numSplits <= bestNumSplits && numSplits <= 2 && gapUp < bestGap)
                    || (numSplits <= bestNumSplits && numSplits <= 2 && gapUp == bestGap && secondGap < bestSecondGap))
                {
                    bestGap = gapUp;
                    bestNumSplits = numSplits;
                    bestOther = other;
                    if (secondGap < bestSecondGap)
                    {
                        bestSecondGap = secondGap;
                    }
                }

            }   // end of loop over others.

            // Handle WidgetSet combos.  Note that both cur and bestOther
            // may be sets so we need to dig into both.
            cur = GetFocusableWidgetFromCombo(cur);
            bestOther = GetFocusableWidgetFromCombo(bestOther);
            cur.DPadListUp = bestOther;

        }   // end of CalcUpLink()

        void CalcDownLink(List<BaseWidget> widgetList, BaseWidget cur)
        {
            int bestNumSplits = 3;  // Number of splitting axes between cur and other.
                                    // 1 is best, 2 is ok, 0 means they overlap so no links.
            BaseWidget bestOther = null;
            float bestGap = float.MaxValue;
            float bestSecondGap = float.MaxValue;   // Used to break ties in 2-split case when rightGaps are equal.

            for (int i = 0; i < widgetList.Count; i++)
            {
                BaseWidget other = widgetList[i];
                if (cur == other)
                    continue;

                float gapUp = (cur.ParentPosition.Y + cur.LocalRect.Top) - (other.ParentPosition.Y + other.LocalRect.Bottom);
                float gapDown = (other.ParentPosition.Y + other.LocalRect.Top) - (cur.ParentPosition.Y + cur.LocalRect.Bottom);
                float gapLeft = (cur.ParentPosition.X + cur.LocalRect.Left) - (other.ParentPosition.X + other.LocalRect.Right);
                float gapRight = (other.ParentPosition.X + other.LocalRect.Left) - (cur.ParentPosition.X + cur.LocalRect.Right);

                // Calc the number of axis aligned splitting planes that divide cur from other.  
                // This is just the count of non-negative gaps.  Will be 0, 1, or 2.
                int numSplits = 0;
                numSplits += gapUp >= 0 ? 1 : 0;
                numSplits += gapDown >= 0 ? 1 : 0;
                numSplits += gapLeft >= 0 ? 1 : 0;
                numSplits += gapRight >= 0 ? 1 : 0;

                if (gapDown < 0 || numSplits <= 0 || numSplits >= 3)
                {
                    continue;
                }

                // Calc second gap if we need it.
                float secondGap = float.MaxValue;
                if (numSplits == 2)
                {
                    if (gapLeft >= 0)
                    {
                        secondGap = Math.Min(secondGap, gapLeft);
                    }
                    if (gapRight >= 0)
                    {
                        secondGap = Math.Min(secondGap, gapRight);
                    }
                    gapDown += secondGapFudgeFactor * secondGap;
                }
                // In the case of a single split, use secondGap to determine
                // which widget is best aligned with the current one based
                // on the centers of their rects.
                if (numSplits == 1)
                {
                    secondGap = Math.Abs((cur.ParentPosition.X + cur.LocalRect.Center.X) - (other.ParentPosition.X + other.LocalRect.Center.X));
                }

                // If numSplits is less than best
                // Or if numSplits = bestNumSplits == 2 and gap is smaller
                // then we have a new better choice.
                if (numSplits == 1 && bestNumSplits > 1
                    || (numSplits <= bestNumSplits && numSplits <= 2 && gapDown < bestGap)
                    || (numSplits <= bestNumSplits && numSplits <= 2 && gapDown == bestGap && secondGap < bestSecondGap))
                {
                    bestGap = gapDown;
                    bestNumSplits = numSplits;
                    bestOther = other;
                    if (secondGap < bestSecondGap)
                    {
                        bestSecondGap = secondGap;
                    }
                }

            }   // end of loop over others.

            // Handle WidgetSet combos.  Note that both cur and bestOther
            // may be sets so we need to dig into both.
            cur = GetFocusableWidgetFromCombo(cur);
            bestOther = GetFocusableWidgetFromCombo(bestOther);
            cur.DPadListDown = bestOther;

        }   // end of CalcDownLink()

        void CalcLeftLink(List<BaseWidget> widgetList, BaseWidget cur)
        {
            int bestNumSplits = 3;  // Number of splitting axes between cur and other.
                                    // 1 is best, 2 is ok, 0 means they overlap so no links.
            BaseWidget bestOther = null;
            float bestGap = float.MaxValue;
            float bestSecondGap = float.MaxValue;   // Used to break ties in 2-split case when rightGaps are equal.

            for (int i = 0; i < widgetList.Count; i++)
            {
                BaseWidget other = widgetList[i];
                if (cur == other)
                    continue;

                float gapUp = (cur.ParentPosition.Y + cur.LocalRect.Top) - (other.ParentPosition.Y + other.LocalRect.Bottom);
                float gapDown = (other.ParentPosition.Y + other.LocalRect.Top) - (cur.ParentPosition.Y + cur.LocalRect.Bottom);
                float gapLeft = (cur.ParentPosition.X + cur.LocalRect.Left) - (other.ParentPosition.X + other.LocalRect.Right);
                float gapRight = (other.ParentPosition.X + other.LocalRect.Left) - (cur.ParentPosition.X + cur.LocalRect.Right);

                // Calc the number of axis aligned splitting planes that divide cur from other.  
                // This is just the count of non-negative gaps.  Will be 0, 1, or 2.
                int numSplits = 0;
                numSplits += gapUp >= 0 ? 1 : 0;
                numSplits += gapDown >= 0 ? 1 : 0;
                numSplits += gapLeft >= 0 ? 1 : 0;
                numSplits += gapRight >= 0 ? 1 : 0;

                if (gapLeft < 0 || numSplits <= 0 || numSplits >= 3)
                {
                    continue;
                }

                // Calc second gap if we need it.
                float secondGap = float.MaxValue;
                if (numSplits == 2)
                {
                    if (gapUp >= 0)
                    {
                        secondGap = Math.Min(secondGap, gapUp);
                    }
                    if (gapDown >= 0)
                    {
                        secondGap = Math.Min(secondGap, gapDown);
                    }
                    gapLeft += secondGapFudgeFactor * secondGap;
                }
                // In the case of a single split, use secondGap to determine
                // which widget is best aligned with the current one based
                // on the centers of their rects.
                if (numSplits == 1)
                {
                    secondGap = Math.Abs((cur.ParentPosition.Y + cur.LocalRect.Center.Y) - (other.ParentPosition.Y + other.LocalRect.Center.Y));
                }

                // If numSplits is less than best
                // Or if numSplits = bestNumSplits == 2 and gap is smaller
                // then we have a new better choice.
                if (numSplits == 1 && bestNumSplits > 1
                    || (numSplits <= bestNumSplits && numSplits <= 2 && gapLeft < bestGap)
                    || (numSplits <= bestNumSplits && numSplits <= 2 && gapLeft == bestGap && secondGap < bestSecondGap))
                {
                    bestGap = gapLeft;
                    bestNumSplits = numSplits;
                    bestOther = other;
                    if (secondGap < bestSecondGap)
                    {
                        bestSecondGap = secondGap;
                    }
                }

            }   // end of loop over others.

            // Handle WidgetSet combos.  Note that both cur and bestOther
            // may be sets so we need to dig into both.
            cur = GetFocusableWidgetFromCombo(cur);
            bestOther = GetFocusableWidgetFromCombo(bestOther);
            cur.DPadListLeft = bestOther;

        }   // end of CalcLeftLink()

        void CalcRightLink(List<BaseWidget> widgetList, BaseWidget cur)
        {
            int bestNumSplits = 3;  // Number of splitting axes between cur and other.
                                    // 1 is best, 2 is ok, 0 means they overlap so no links.
            BaseWidget bestOther = null;
            float bestGap = float.MaxValue;
            float bestSecondGap = float.MaxValue;   // Used to break ties in 2-split case when rightGaps are equal.

            for (int i = 0; i < widgetList.Count; i++)
            {
                BaseWidget other = widgetList[i];
                if (cur == other)
                    continue;

                float gapUp = (cur.ParentPosition.Y + cur.LocalRect.Top) - (other.ParentPosition.Y + other.LocalRect.Bottom);
                float gapDown = (other.ParentPosition.Y + other.LocalRect.Top) - (cur.ParentPosition.Y + cur.LocalRect.Bottom);
                float gapLeft = (cur.ParentPosition.X + cur.LocalRect.Left) - (other.ParentPosition.X + other.LocalRect.Right);
                float gapRight = (other.ParentPosition.X + other.LocalRect.Left) - (cur.ParentPosition.X + cur.LocalRect.Right);

                // Calc the number of axis aligned splitting planes that divide cur from other.  
                // This is just the count of non-negative gaps.  Will be 0, 1, or 2.
                int numSplits = 0;
                numSplits += gapUp >= 0 ? 1 : 0;
                numSplits += gapDown >= 0 ? 1 : 0;
                numSplits += gapLeft >= 0 ? 1 : 0;
                numSplits += gapRight >= 0 ? 1 : 0;

                if (gapRight < 0 || numSplits <= 0 || numSplits >= 3)
                {
                    continue;
                }

                // Calc second gap if we need it.
                float secondGap = float.MaxValue;
                if (numSplits == 2)
                {
                    if (gapUp >= 0)
                    {
                        secondGap = Math.Min(secondGap, gapUp);
                    }
                    if (gapDown >= 0)
                    {
                        secondGap = Math.Min(secondGap, gapDown);
                    }
                    gapRight += secondGapFudgeFactor * secondGap;
                }
                // In the case of a single split, use secondGap to determine
                // which widget is best aligned with the current one based
                // on the centers of their rects.
                if (numSplits == 1)
                {
                    secondGap = Math.Abs((cur.ParentPosition.Y + cur.LocalRect.Center.Y) - (other.ParentPosition.Y + other.LocalRect.Center.Y));
                }

                // If numSplits is less than best
                // Or if numSplits = bestNumSplits == 2 and gap is smaller
                // then we have a new better choice.
                if (numSplits == 1 && bestNumSplits > 1 
                    || (numSplits <= bestNumSplits && numSplits <= 2 && gapRight < bestGap)
                    || (numSplits <= bestNumSplits && numSplits <= 2 && gapRight == bestGap && secondGap < bestSecondGap))
                {
                    bestGap = gapRight;
                    bestNumSplits = numSplits;
                    bestOther = other;
                    if (secondGap < bestSecondGap)
                    {
                        bestSecondGap = secondGap;
                    }
                }

            }   // end of loop over others.

            // Handle WidgetSet combos.  Note that both cur and bestOther
            // may be sets so we need to dig into both.
            cur = GetFocusableWidgetFromCombo(cur);
            bestOther = GetFocusableWidgetFromCombo(bestOther);
            cur.DPadListRight = bestOther;
        
        }   // end of CalcRightLink()

        /// <summary>
        /// This looks at the passed in widget.  If this is a combo widgetset it
        /// finds the focusable widget in the set to return.  
        /// If not, it just returns the passed in widget.
        /// 
        /// The reason we do this has to do with calculating the dPad links.  The function
        /// that gathers all the focusable widgets is allowed to treat some WidgetSets as
        /// single widgets.  Their position (compared to the position of thier contained widget)
        /// works better for calculating nav.  But, once we use the WidgetSet's position to 
        /// calulate the nav link, we want to make the link itself actually connect to the 
        /// focusable widget in the set.
        /// For example, a RadioButtonLableHelp looks like this:
        ///     [ (0) This is my label            (?) ]
        /// For calculating the links, it works better when the position we use is in the center
        /// of this entire thing.  But, any links connecting to this need to go direclty to the
        /// Checkbox.
        /// </summary>
        /// <param name="widget"></param>
        /// <returns></returns>
        BaseWidget GetFocusableWidgetFromCombo(BaseWidget widget)
        {
            BaseWidget result = widget; // Default to just returning the passed in widget.

            WidgetSet set = widget as WidgetSet;
            if (set != null)
            {
                result = set.GetFirstFocusableWidget();
            }

            return result;
        }   // end of GetFocusableWidgetFromCombo()

        public void LoadContent()
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.LoadContent();
            }
        }

        public void UnloadContent()
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.UnloadContent();
            }
        }

        public void DeviceResetHandler(object sender, EventArgs e)
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.DeviceResetHandler(sender, e);
            }
        }

        #endregion

    }   // end of class BaseDialog
}   // end of namespace KoiX.UI
