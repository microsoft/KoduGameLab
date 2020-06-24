
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
using KoiX.Geometry;
using KoiX.Input;

namespace KoiX.UI
{
    /// <summary>
    /// A container class for a set of widgets.  Manages
    /// their size and position.
    /// </summary>
    public class WidgetSet : BaseWidget
    {
        #region Members

        // Orientation of stack.
        // Default is None which implies that widgets just stay where they are placed
        // relative to the origin of the WidgetSet.
        Orientation orientation = Orientation.None;

        Justification horizontalJustification = Justification.Center;
        Justification verticalJustification = Justification.Center;

        // TODO Do we need this?  Would it be useful to do the check anyway and assert if not beig enough?
        // Allow size to grow if widgets are too big or numerous.
        //bool elastic = false;

        List<BaseWidget> widgets = new List<BaseWidget>();

        bool fitToParentDialog = false;         // Force size to match parent dialog.
        bool ignoreRectWhenHitTesting = false;  // Normally hit testing will not be done for hits outside of the LocalRect.
                                                // If this is set to 'true' the rect is ignored and all hits are passed
                                                // down to the widgets.  This allows widgets to exceed the rect bounds and
                                                // still get input.  (think worlds in LoadLevelMenu)
        
        bool renderOutline = false;             // Renders a light outline around the set and it's contents.  The size of the
                                                // outline is the LocalRect of the set +- the adjustment from outlineAdjust.
        Padding outlinePadding = Padding.Empty;  

        bool debugOutline = false;              // Render an outline to help debugging.

        bool mouseOver = false;

        bool treatAsSingleWidgetForNavigation = false;  // When calculating nav links, it is sometimes better to treat combos
                                                        // as single widgets.  This puts their position in the center of the combo 
                                                        // rather than the center of the focusable widget within the combo.

        #endregion 

        #region Accessors

        public Orientation Orientation
        {
            get { return orientation; }
            set
            {
                if (orientation != value)
                {
                    orientation = value;
                    dirty = true;
                }
            }
        }

        public Justification HorizontalJustification
        {
            get { return horizontalJustification; }
            set
            {
                if (horizontalJustification != value)
                {
                    horizontalJustification = value;
                    dirty = true;
                }
            }
        }

        public Justification VerticalJustification
        {
            get { return verticalJustification; }
            set
            {
                if (verticalJustification != value)
                {
                    verticalJustification = value;
                    dirty = true;
                }
            }
        }

        /// <summary>
        /// Should not be used for adding elements.  Do that via AddWidget().
        /// </summary>
        public List<BaseWidget> Widgets
        {
            get { return widgets; }
        }

        /// <summary>
        /// Forces size and position to match parent dialog.
        /// </summary>
        public bool FitToParentDialog
        {
            get { return fitToParentDialog; }
            set { fitToParentDialog = value; }
        }

        public bool RenderOutline
        {
            get { return renderOutline; }
            set { renderOutline = value; }
        }

        public Padding OutlinePadding
        {
            get { return outlinePadding; }
            set { outlinePadding = value; }
        }

        public bool RenderDebugOutline
        {
            get { return debugOutline; }
            set { debugOutline = value; }
        }

        /// <summary>
        /// Is the mouse currently over one of our widgets?
        /// </summary>
        public bool MouseOver
        {
            get { return mouseOver; }
            set 
            {
                if (mouseOver != value)
                {
                    mouseOver = value;

                    // Recurse through children clearing all their MouseOver flags, too.
                    if (!mouseOver)
                    {
                        foreach (BaseWidget w in widgets)
                        {
                            if (w is WidgetSet)
                            {
                                (w as WidgetSet).MouseOver = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Normally hit testing will not be done for hits outside of the LocalRect.
        /// If this is set to 'true' the rect is ignored and all hits are passed
        /// down to the widgets.  This allows widgets to exceed the rect bounds and
        /// still get input.  (think worlds in LoadLevelMenu)
        /// Defaults to false;
        /// </summary>
        public bool IgnoreRectWhenHitTesting
        {
            get { return ignoreRectWhenHitTesting; }
            set { ignoreRectWhenHitTesting = value; }
        }

        /// <summary>
        /// When calculating nav links, it is sometimes better to treat combos
        /// as single widgets.  This puts their position in the center of the combo 
        /// rather than the center of the focusable widget within the combo.
        /// </summary>
        public bool TreatAsSingleWidgetForNavigation
        {
            get { return treatAsSingleWidgetForNavigation; }
            set { treatAsSingleWidgetForNavigation = value; }
        }

        public override float Alpha
        {
            set
            {
                if (alpha.TargetValue != value)
                {
                    alpha.Value = value;
                    foreach (BaseWidget w in widgets)
                    {
                        w.Alpha = value;
                    }
                }
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentDialog"></param>
        /// <param name="rect"></param>
        /// <param name="orientation">How widgets are arranged within this set.</param>
        /// <param name="horizontalJustification"></param>
        /// <param name="verticalJustification"></param>
        /// <param name="data"></param>
        public WidgetSet(BaseDialog parentDialog, RectangleF rect, Orientation orientation, Justification horizontalJustification = Justification.Center, Justification verticalJustification = Justification.Center, string id = null, object data = null)
            : base(parentDialog, id: id, data: data)
        {
            Debug.Assert(!(orientation == Orientation.Horizontal && verticalJustification == Justification.Full), "Setting vertical justification to Full is not compatible with Horizontal orientation.  Try Center instead.");
            Debug.Assert(!(orientation == Orientation.Vertical && horizontalJustification == Justification.Full), "Setting horizontal justification to Full is not compatible with Vertical orientation.  Try Center instead.");

            this.orientation = orientation;
            this.horizontalJustification = horizontalJustification;
            this.verticalJustification = verticalJustification;
            this.localRect = rect;
        }

        public void AddWidget(BaseWidget widget)
        {
            Debug.Assert(widget.ParentDialog == ParentDialog, "We should have the same parent dialog.");

            widgets.Add(widget);
            dirty = true;
        }   // end of AddWidget()

        public override void Recalc(Vector2 parentPosition)
        {
            this.parentPosition = parentPosition;

            if (fitToParentDialog)
            {
                localRect.Position = Vector2.Zero;
                localRect.Size = ParentDialog.Rectangle.Size;
            }

            // Recalc children.  Mark them as dirty if self is dirty.
            foreach (BaseWidget widget in widgets)
            {
                if (dirty)
                {
                    widget.Dirty = true;
                }
                widget.Recalc(localRect.Position + parentPosition);
            }

            if (Dirty && widgets.Count > 0)
            {
                // Massage the position and size of the child widgets.

                // TODO (****) Do we really want to allow WidgetSet to change size of stuff?  I guess it does
                // make sense if we want to have a set of buttons that are all the same width.  Need to make
                // sure this doesn't screw things up.  Seriously.

                if (orientation == Orientation.Horizontal)
                {
                    // Start by setting positions for left justification and then adjust as needed.
                    // Widget position is relative to set position so start with 0,0.

                    // TODO (****) This used to start at 0,0 but now takes padding into account.  Need to do the same vertically.
                    {
                        Vector2 pos = new Vector2(Padding.Left, Padding.Top);
                        for (int i = 0; i < widgets.Count; i++)
                        {
                            if (i == 0)
                            {
                                // Add left margins in.
                                pos.X += widgets[i].Margin.Left;
                            }
                            else
                            {
                                // Add max of left/right margins from this and prev widget.
                                pos.X += Math.Max(widgets[i - 1].Margin.Right, widgets[i].Margin.Left);
                            }
                            widgets[i].Position = pos + new Vector2(0, widgets[i].Margin.Top);
                            pos.X += widgets[i].LocalRect.Width;                     // Add width of widget.
                        }
                    }

                    // Grow set's LocalRect to fit children, if needed.
                    foreach (BaseWidget widget in widgets)
                    {
                        RectangleF r = widget.LocalRect;
                        r.Position += localRect.Position;
                        localRect = RectangleF.Union(localRect, r);
                    }


                    switch (horizontalJustification)
                    {
                        case Justification.Left:
                            {
                                // No adjusting needed.
                            }
                            break;
                        case Justification.Right:
                            {
                                // Figure out the right edge and move everything over.
                                BaseWidget rightmost = widgets[widgets.Count - 1];
                                float totalWidth = rightmost.Position.X + rightmost.LocalRect.Width + rightmost.Margin.Right + Padding.Right;
                                float offset = LocalRect.Width - totalWidth;

                                foreach (BaseWidget widget in widgets)
                                {
                                    Vector2 pos = widget.Position;
                                    pos.X += offset;
                                    widget.Position = pos;
                                }
                            }
                            break;
                        case Justification.Center:
                            {
                                // Same as Right, just half the offset.
                                BaseWidget rightmost = widgets[widgets.Count - 1];
                                float totalWidth = rightmost.Position.X + rightmost.LocalRect.Width + rightmost.Margin.Right + Padding.Right;
                                float offset = (int)((LocalRect.Width - totalWidth) / 2.0f);

                                foreach (BaseWidget widget in widgets)
                                {
                                    Vector2 pos = widget.Position;
                                    pos.X += offset;
                                    widget.Position = pos;
                                }

                            }
                            break;
                        case Justification.Full:
                            {
                                // Get total width of all widgets.
                                float totalWidth = 0;
                                for (int i = 0; i < widgets.Count; i++)
                                {
                                    totalWidth += widgets[i].LocalRect.Width + widgets[i].Margin.Horizontal;
                                }
                                // Calc gap size.
                                float gapSize = (LocalRect.Width - totalWidth - Padding.Horizontal) / (widgets.Count - 1);
                                // Start at Padding and position buttons.
                                Vector2 pos = new Vector2(Padding.Left, Padding.Top);
                                for (int i = 0; i < widgets.Count; i++)
                                {
                                    // Honor margin for first widget.
                                    if (i == 0)
                                    {
                                        pos.X += widgets[i].Margin.Left;
                                    }
                                    widgets[i].Position = pos.Truncate();                           // Set widget position.
                                    pos.X += widgets[i].LocalRect.Width + widgets[i].Margin.Horizontal;  // Add width of widget.
                                    pos.X += gapSize;                                               // Add gap.
                                }

                            }
                            break;
                    }

                    // Now apply vertical justification to widgets.
                    foreach (BaseWidget widget in widgets)
                    {
                        Vector2 pos = widget.LocalRect.Position;
                        switch (verticalJustification)
                        {
                            case Justification.Top:
                                pos.Y = padding.Top + widget.Margin.Top;
                                break;
                            case Justification.Center:
                                // Truncate to maintain pixel alignment.
                                // Note we're ignoring vertical margin here.  Basically assuming Top == Bottom.  Problem?
                                pos.Y = (int)((LocalRect.Height - Padding.Bottom - widget.LocalRect.Height) / 2.0f);
                                break;
                            case Justification.Bottom:
                                pos.Y = LocalRect.Height - Padding.Bottom - widget.LocalRect.Height - widget.Margin.Bottom;
                                break;
                        }
                        widget.Position = pos;
                    }
                }
                else if (orientation == Orientation.Vertical)
                {
                    // Start by setting positions for top justification and then adjust as needed.
                    // Widget position is relative to set position so start with 0,0 plus padding.
                    {
                        Vector2 pos = new Vector2(Padding.Left, Padding.Top);
                        for (int i = 0; i < widgets.Count; i++)
                        {
                            if (i == 0)
                            {
                                // Add top margins in.
                                pos.Y += widgets[i].Margin.Top;
                            }
                            else
                            {
                                // Add max of top/bottom margins from this and prev widget.
                                //pos.Y += Math.Max(widgets[i - 1].Margin.Bottom, widgets[i].Margin.Top);

                                // Instead of adding in max should we add in whichever has the largest absolute value?
                                if (Math.Abs(widgets[i - 1].Margin.Bottom) > Math.Abs(widgets[i].Margin.Top))
                                {
                                    pos.Y += widgets[i - 1].Margin.Bottom;
                                }
                                else
                                {
                                    pos.Y += widgets[i].Margin.Top;
                                }

                            }
                            widgets[i].Position = pos + new Vector2(widgets[i].Margin.Left, 0);
                            pos.Y += widgets[i].LocalRect.Height;    // Add height of widget.
                        }
                    }

                    // Grow set's LocalRect to fit children, if needed.
                    // Note that child coords are relative to this set so we need
                    // move this set's position to origin while doing this.
                    Vector2 curPos = localRect.Position;
                    localRect.Position = Vector2.Zero;
                    foreach (BaseWidget widget in widgets)
                    {
                        localRect = RectangleF.Union(localRect, widget.LocalRect);
                    }
                    // Restore position of set.
                    localRect.Position = curPos;

                    switch (verticalJustification)
                    {
                        case Justification.Top:
                            {
                                // No adjusting needed.
                            }
                            break;
                        case Justification.Bottom:
                            {
                                // Figure out the bottom edge and move everything up.
                                // TODO (****) Need to fix this to work properly with padding of containing object.
                                BaseWidget bottommost = widgets[widgets.Count - 1];
                                float totalHeight = bottommost.Position.Y + bottommost.LocalRect.Height + bottommost.Margin.Bottom;
                                float offset = LocalRect.Height - totalHeight;

                                foreach (BaseWidget widget in widgets)
                                {
                                    Vector2 pos = widget.Position;
                                    pos.Y += offset;
                                    widget.Position = pos;
                                }
                            }
                            break;
                        case Justification.Center:
                            {
                                // Same as Bottom, just half the offset.
                                // TODO (****) Need to fix this to work properly with padding of containing object.
                                BaseWidget bottommost = widgets[widgets.Count - 1];
                                float totalHeight = bottommost.Position.Y + bottommost.LocalRect.Height + bottommost.Margin.Bottom;
                                float offset = (int)((LocalRect.Height - totalHeight) / 2.0f);

                                foreach (BaseWidget widget in widgets)
                                {
                                    Vector2 pos = widget.Position;
                                    pos.Y += offset;
                                    widget.Position = pos;
                                }
                            }
                            break;
                        case Justification.Full:
                            {
                                // Get total height of all widgets.
                                float totalHeight = 0;
                                for (int i = 0; i < widgets.Count; i++)
                                {
                                    totalHeight += widgets[i].LocalRect.Height + widgets[i].Margin.Vertical;
                                }
                                // Calc gap size.
                                float gapSize = (LocalRect.Height - totalHeight) / (widgets.Count - 1);
                                // Start at Padding and position buttons.
                                Vector2 pos = new Vector2(Padding.Left, Padding.Top);
                                for (int i = 0; i < widgets.Count; i++)
                                {
                                    // Honor margin for first widget.
                                    if (i == 0)
                                    {
                                        pos.Y += widgets[i].Margin.Top;
                                    }
                                    widgets[i].Position = pos.Truncate();                           // Set widget position.
                                    pos.Y += widgets[i].LocalRect.Height + widgets[i].Margin.Vertical;   // Add width of widget.
                                    pos.Y += gapSize;                                               // Add gap.
                                }

                            }
                            break;
                    }

                    // Now apply horizontal justification to widgets.
                    foreach (BaseWidget widget in widgets)
                    {
                        Vector2 pos = widget.LocalRect.Position;
                        switch (horizontalJustification)
                        {
                            case Justification.Left:
                                pos.X = Padding.Left + widget.Margin.Left;
                                break;
                            case Justification.Center:
                                // Truncate to maintain pixel alignment.
                                // Note we're ignoring horizontal margin here.  Basically assuming Left == Right.  Problem?
                                pos.X = (int)((LocalRect.Width - Padding.Right - widget.LocalRect.Width) / 2.0f);
                                break;
                            case Justification.Right:
                                pos.X = LocalRect.Width - Padding.Right - widget.LocalRect.Width - widget.Margin.Right;
                                break;
                        }
                        widget.Position = pos;
                    }

                }
                else
                {
                    // Orientation.None -- For this we should be able to just expand the localRect
                    // to cover all the widgets which have been manually placed.  But, when I try
                    // this, a lot of UI ends up looking like crap.  Need to figure out what's 
                    // going on and be sure this is actualy the right thing to do.
                    // TODO (****) Fix this.
                    /*
                    foreach (BaseWidget w in widgets)
                    {
                        LocalRect = RectangleF.Union(LocalRect, w.LocalRect);
                    }
                     */
                }

            }   // end of if Dirty.
            
            base.Recalc(parentPosition);

            RecalcParentPosition(parentPosition);

        }   // end of Recalc()

        public override void RecalcParentPosition(Vector2 parentPosition)
        {
            base.RecalcParentPosition(parentPosition);

            foreach (BaseWidget widget in widgets)
            {
                widget.RecalcParentPosition(parentPosition + LocalRect.Position);
            }
        }   // end of RecalcParentPosition()

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            base.Update(camera, parentPosition);

            bool anyHover = false;     

            // Update children.  Mark them as dirty if self is dirty.
            foreach (BaseWidget widget in widgets)
            {
                if (dirty)
                {
                    widget.Dirty = true;
                }
                widget.Update(camera, localRect.Position + parentPosition);

                anyHover |= widget.Hover;
            }

            // Set hover for whole set if any of the children have hover set.
            Hover = anyHover;

            if (Dirty)
            {
                Recalc(parentPosition);
            }   // end of if Dirty.

        }   // end of Update()

        public override void Render(SpriteCamera camera, Vector2 parentPosition)
        {
            // Cull if possible.
            RectangleF cullRect = LocalRect;
            cullRect.Position += parentPosition;
            Boku.Common.Frustum.CullResult cullResult = camera.CullTest(cullRect);
            if (cullResult == Boku.Common.Frustum.CullResult.TotallyOutside)
            {
                return;
            }

#if DEBUG
            // Render an outline?
            if (debugOutline)
            {
                RoundedRect.Render(camera, cullRect, 5, Color.White * 0.1f, outlineColor: Color.Black * 0.1f, outlineWidth: 2.5f);
            }
#endif

            if (RenderOutline)
            {
                cullRect.AddPadding(outlinePadding);
                RoundedRect.Render(camera, cullRect, 8, theme.BaseColor * 0.2f, outlineColor: Color.Black * 0.2f, outlineWidth: 0.5f);
            }

            foreach (BaseWidget widget in widgets)
            {
                widget.Render(camera, Position + parentPosition);
            }

            base.Render(camera, parentPosition);

        }   // end of Render()

        /// <summary>
        /// Set all children to be the same size.  The size
        /// is picked as the max in each dimension of all the
        /// children.
        /// </summary>
        public void MatchChildSizes()
        {
            Vector2 max = Vector2.Zero;

            foreach (BaseWidget widget in widgets)
            {
                max = Vector2.Max(max, widget.Size);
            }

            foreach (BaseWidget widget in widgets)
            {
                widget.Size = max;
            }

        }   // end of MatchChildSizes()

        public void SetMarginDebugOnChildren(bool recursive = false)
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.RenderMarginDebug = true;
                if(recursive)
                {
                WidgetSet set = widget as WidgetSet;
                if (set != null)
                {
                    set.SetMarginDebugOnChildren(recursive);
                }
                }
            }
        }   // end of SetMarginDebugOnChildren()

        public override void Activate(params object[] args)
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.Activate(args);
            }

            base.Activate(args);
        }   // end of Activate()

        public override void Deactivate()
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.Deactivate();
            }

            base.Deactivate();
        }   // end of Deactivate()

        public override void RegisterForInputEvents()
        {
            // Nothing to do here.  Child widgets register themselves during activation.
        }

        public override void UnregisterForInputEvents()
        {
            foreach (BaseWidget w in widgets)
            {
                w.UnregisterForInputEvents();
            }

            base.UnregisterForInputEvents();
        }

        protected override bool IsFocusable(bool overrrideInactive = false)
        {
            Debug.Assert(false, "Should this ever be called?");
            return true;
        }

        public override bool SetFocus(bool overrrideInactive = false)
        {
            // If we or one of our children already has focus, 
            // then we don't need to do anything.
            BaseWidget w = GetFocusWidget();
            if (w != null)
            {
                Debug.Assert(ParentDialog.CurrentFocusWidget == w);
                return true;
            }

            // No widget is currently in focus, so just
            // find the first one that is focusable.
            foreach (BaseWidget widget in widgets)
            {
                if (widget.SetFocus(overrrideInactive))
                {
                    return true;
                }
            }

            return false;
        }   // end of SetFocus()

        /// <summary>
        /// Deep traversal of this WidgetSet and its children
        /// which returns the first InFocus widget found.
        /// </summary>
        /// <returns></returns>
        public BaseWidget GetFocusWidget()
        {
            if (InFocus)
            {
                return this;
            }

            foreach (BaseWidget widget in widgets)
            {
                if (widget is WidgetSet)
                {
                    BaseWidget inFocusWidget = (widget as WidgetSet).GetFocusWidget();
                    if (inFocusWidget != null)
                    {
                        return inFocusWidget;
                    }
                }
                else
                {
                    if (widget.InFocus)
                    {
                        return widget;
                    }
                }
            }

            return null;
        }   // end of GetFocusWidget()

        /// <summary>
        /// Deep traversal of this WidgetSet and its children
        /// which returns the first focusable widget found.
        /// Note, this will NOT return WidgetSets even if they
        /// are marked focusable.
        /// </summary>
        /// <returns></returns>
        public BaseWidget GetFirstFocusableWidget()
        {
            foreach (BaseWidget widget in widgets)
            {
                if (widget is WidgetSet)
                {
                    BaseWidget focusableWidget = (widget as WidgetSet).GetFirstFocusableWidget();
                    if (focusableWidget != null)
                    {
                        return focusableWidget;
                    }
                }
                else
                {
                    if (widget.Focusable)
                    {
                        return widget;
                    }
                }
            }

            return null;
        }   // end of GetFirstFocusableWidget()



        #endregion

        #region InputEventHandler

        /// <summary>
        /// Passes along hit test to child widgets.
        /// </summary>
        /// <param name="hitLocation">Hit in parent dialog coordinates.</param>
        /// <returns></returns>
        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            InputEventHandler result = null;

            // Test self.
            if (IgnoreRectWhenHitTesting || localRect.Contains(hitLocation))
            {
                // Offset mouse point by parent position.
                Vector2 hit = hitLocation - LocalRect.Position;

                foreach (BaseWidget widget in widgets)
                {
                    // Only capture the first result but still test the rest.
                    // This keeps the MouseOver values correct for sets that
                    // wouldn't otherwise get tested.
                    if (result == null)
                    {
                        result = widget.HitTest(hit);
                    }
                    else
                    {
                        widget.HitTest(hit);
                    }
                }
                
                MouseOver = true;
            }
            else
            {
                MouseOver = false;
            }

            return result;
        }   // end of HitTest()

        #endregion

        #region Internal

        public override void LoadContent()
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.LoadContent();
            }
        }

        public override void UnloadContent()
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.UnloadContent();
            }
        }

        public override void DeviceResetHandler(object sender, EventArgs e)
        {
            foreach (BaseWidget widget in widgets)
            {
                widget.DeviceResetHandler(sender, e);
            }
        }

        #endregion

    }   // end of class WidgetSet

}   // end of namespace KoiX.UI
