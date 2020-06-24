
// Set this to visually debug DPad links between widgets.
//#define DEBUG_NAVIGATION_LINKS

// Set this to output current stack state.
//#define STATE_DEBUG

// Set this to debug which dialogs are being shown or killed
//#define DEBUG_SHOW_KILL

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Input;
using KoiX.UI;

#if DEBUG_NAVIGATION_LINKS
    using KoiX.Geometry;
#endif

namespace KoiX.Managers
{
    /// <summary>
    /// Singleton class which managers Dialogs that are currently active.
    /// 
    /// NOTE Still not sure whether singleton of static approach is best...
    /// Argh, I do hate having to publically say .Instance or .Instance() all the time
    /// though so that should be hidden behind static methods.
    /// 
    /// This can't be fully static since it derives from InputEventHandler which
    /// requires an instance.  While the class isn't static, everything in it is
    /// which means we don't have to type instance.foo everywhere.
    /// 
    /// How to handle multiple active dialogs:
    /// Dialogs are store in list in order added.  This implies that the last one entered is active.
    /// If any are modal, only the last modal dialog is active.
    /// If only non-modal exist then all are active and one at end of list has focus.
    /// -- need a way to set focus.
    /// 
    /// If we have some sense of 'focus' that can be traversed via tab key we need to have that
    /// owned by this manager since if we have multiple dialogs, the focus may move among them.
    /// Input should be handled by individual dialogs and widgets but they will call into here
    /// when they need to navigate to next dialog.
    /// </summary>
    public class DialogManagerX : InputEventHandler
    {
        protected class DialogCameraSet
        {
            public BaseDialog Dialog;
            public SpriteCamera Camera;

            public DialogCameraSet(BaseDialog dialog, SpriteCamera camera = null)
            {
                Dialog = dialog;
                Camera = camera;
            }
        }

        #region Members

        static DialogManagerX instance = new DialogManagerX();  // Not used externally.  Wrap any needed access in a 
                                                                // static API.  Should only be visible internally.

        static List<DialogCameraSet> dialogCameraSets;  // This is our list of current dialog sets.  It acts like a
                                                        // stack for most use so the 0th element is the one most pushed
                                                        // back and the Count-1 element is on top.

        static SpriteCamera defaultCamera;  // Default camera used by dialogs which are shown without being
                                            // given a specific camera.  This camera always looks at the 
                                            // origin and should only change to match the window resolution
                                            // and any zooming done for the whole UI.

        static BaseDialog currentFocusDialog;      // Which dialog has focus.

        #endregion

        #region Accessors

        /// <summary>
        /// Are there any dialogs at all that are active?
        /// </summary>
        public static bool DialogIsActive
        {
            get { return dialogCameraSets.Count > 0; }
        }

        /// <summary>
        /// Is there an active modal dialog?
        /// </summary>
        public static bool ModalDialogIsActive
        {
            get
            {
                if(currentFocusDialog != null && currentFocusDialog.IsModalDialog)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Which dialog currently has focus.
        /// Null if none.
        /// Active dialogs look at this and set thier focus state accordingly.
        /// </summary>
        public static BaseDialog CurrentFocusDialog
        {
            get { return currentFocusDialog; }
        }

        /// <summary>
        /// Returns the camera associated with the CurrentFocusDialog.
        /// May return null.
        /// </summary>
        public static SpriteCamera CurrentFocusDialogCamera
        {
            get
            {
                foreach (DialogCameraSet set in dialogCameraSets)
                {
                    if (set.Dialog == CurrentFocusDialog)
                    {
                        return set.Camera;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// We don't expose the full camera since it shouldn't be touched
        /// but we do allow the zoom value to be tweaked.  Note that this
        /// only affects the dialogs that were activated without a camera
        /// of their own.
        /// </summary>
        public static float Zoom
        {
            get { return defaultCamera.Zoom; }
            set 
            {
                if (defaultCamera.Zoom != value)
                {
                    defaultCamera.Zoom = value;
                    foreach (DialogCameraSet set in dialogCameraSets)
                    {
                        if (set.Camera == null)
                        {
                            set.Dialog.Dirty = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// private c'tor
        /// </summary>
        DialogManagerX()
        {
            instance = this;

            dialogCameraSets = new List<DialogCameraSet>();

            // Make camera look at center of screen.
            defaultCamera = new SpriteCamera();
            defaultCamera.Position = Vector2.Zero;
        }

        /// <summary>
        /// Updates active dialogs.
        /// </summary>
        public static void Update()
        {
            if (dialogCameraSets.Count > 0)
            {
                // Make camera look at center of screen.
                defaultCamera.Position = Vector2.Zero;
                defaultCamera.Update();

                // Update all dialogs.  Only Active dialogs will 
                // do anything but this give suspeneded dialogs a 
                // change to update timers/ progress bars, etc.
                foreach (DialogCameraSet set in dialogCameraSets)
                {
                    SpriteCamera camera = set.Camera != null ? set.Camera : defaultCamera;
                    set.Dialog.Update(camera);
                }
            }

        }   // end of Update()

        /// <summary>
        /// Renders any active dialogs.
        /// </summary>
        public static void Render()
        {
            SpriteCamera camera;

            // Call Render on all dialogs.  Inactive dialogs should ignore this.
            // Since foreach just iterates through the list, we end up rendering
            // back to front which is what we want.
            foreach (DialogCameraSet set in dialogCameraSets)
            {
                camera = set.Camera != null ? set.Camera : defaultCamera;
                set.Dialog.Render(camera);
            }

#if DEBUG_NAVIGATION_LINKS
            {
                float bigRadius = 20.0f;
                float smallRadius = 5.0f;
                float offset = 7.0f;
                float lineWidth = 1.5f;

                // Links are only valid if dialog is focusable.  So find the topmost
                // focusable dialog.  If none found, just skips debug rendering.
                DialogCameraSet dcs = null;
                for (int i = dialogCameraSets.Count - 1; i >= 0; i--)
                {
                    dcs = dialogCameraSets[i];
                    if (dcs.Dialog.Focusable)
                    {
                        break;
                    }
                    else
                    {
                        dcs = null;
                    }
                }

                // Do we have a focusable dialog?
                if (dcs != null)
                {
                    BaseDialog dialog = dcs.Dialog;
                    // Make sure we've got a valid camera.
                    camera = dcs.Camera;
                    if (camera == null)
                    {
                        camera = defaultCamera;
                    }

                    // Get all the focusable widgets.  We don't care about the others.
                    // Note that there is a little oddness to the link rendering.  For WidgetSets
                    // that have a single, focusable widget in them (combos) we use the center of
                    // the combo for calculating the link BUT the debug rendering shows the links
                    // to the center of the focusable widget.
                    // TODO (****) fix this is really bored.
                    List<BaseWidget> widgets = new List<BaseWidget>();
                    dialog.GetFocusableWidgets(widgets, dPadNav: false);

                    // First draw big dots at cetner of widgets.
                    foreach (BaseWidget widget in widgets)
                    {
                        Vector2 pos = dialog.Rectangle.Position + widget.ParentPosition + widget.LocalRect.Center;

                        Disc.Render(camera, pos, bigRadius, Color.Black, outlineColor: Color.White, outlineWidth: 2.0f);

                        // Small dot at each connection point.
                        Disc.Render(camera, pos + new Vector2(offset, -offset), smallRadius, Color.Green);  // Right
                        Disc.Render(camera, pos + new Vector2(-offset, offset), smallRadius, Color.Yellow); // Left
                        Disc.Render(camera, pos + new Vector2(offset, offset), smallRadius, Color.Blue);    // Down
                        Disc.Render(camera, pos + new Vector2(-offset, -offset), smallRadius, Color.Red);   // Up
                    }

                    // Then draw connecting lines.
                    foreach (BaseWidget widget in widgets)
                    {
                        Vector2 pos = dialog.Rectangle.Position + widget.ParentPosition + widget.LocalRect.Center;

                        // Lines to target widgets.  Lines go from source pads to center of target widget.
                        if (widget.DPadListRight != null)
                        {
                            Vector2 targetPos = dialog.Rectangle.Position + widget.DPadListRight.ParentPosition + widget.DPadListRight.LocalRect.Center;
                            Line.DrawLine(camera, pos + new Vector2(offset, -offset), targetPos, Color.Green, Color.Green, lineWidth, lineWidth);
                        }
                        if (widget.DPadListLeft != null)
                        {
                            Vector2 targetPos = dialog.Rectangle.Position + widget.DPadListLeft.ParentPosition + widget.DPadListLeft.LocalRect.Center;
                            Line.DrawLine(camera, pos + new Vector2(-offset, offset), targetPos, Color.White, Color.Yellow, lineWidth, lineWidth);
                        }
                        if (widget.DPadListDown != null)
                        {
                            Vector2 targetPos = dialog.Rectangle.Position + widget.DPadListDown.ParentPosition + widget.DPadListDown.LocalRect.Center;
                            Line.DrawLine(camera, pos + new Vector2(offset, offset), targetPos, Color.Blue, Color.Blue, lineWidth, lineWidth);
                        }
                        if (widget.DPadListUp != null)
                        {
                            Vector2 targetPos = dialog.Rectangle.Position + widget.DPadListUp.ParentPosition + widget.DPadListUp.LocalRect.Center;
                            Line.DrawLine(camera, pos + new Vector2(-offset, -offset), targetPos, Color.Red, Color.Red, lineWidth, lineWidth);
                        }
                    }
                }   // end if we have a focusable dialog.
            }
#endif

        }   // end of Render()

        /// <summary>
        /// Activates and shows the current dialog.  Note that this may be
        /// delayed if there's a higher priority dialog already in the manager.
        /// 
        /// TODO (****) args need to be filtered in to where they are used.  Do we save
        /// them as part of the set and reuse them if a dialog is reactivated?
        /// </summary>
        /// <param name="dialog"></param>
        public static void ShowDialog(BaseDialog dialog, SpriteCamera camera = null, object[] args = null)
        {
#if DEBUG_SHOW_KILL
            Debug.Print("showing dialog : " + dialog._name);
#endif
            Debug.Assert(!(dialog is KoiX.UI.Dialogs.CanvasDialog && camera == null), "CanvasDialogs must have their own camera.");

            // If this is first dialog, register manager for input events.
            if (dialogCameraSets.Count == 0)
            {
                instance.RegisterForInputEvents();
            }

            // Figure out where to insert new dialog.  If non-modal and
            // we currently have any modal on top we need to insert under
            // all the modal ones.  If it's modal, just push on top.
            int index = dialogCameraSets.Count;
            if (!dialog.IsModalDialog)
            {
                while (index - 1 >= 0 && dialogCameraSets[index - 1].Dialog.IsModalDialog)
                {
                    --index;
                }
            }
            dialogCameraSets.Insert(index, new DialogCameraSet(dialog, camera));

            // Give the dialog an update call.  This ensures that it has a chance to
            // allocate any resources needed to render before Render() is called.
            if (camera == null)
            {
                camera = defaultCamera;
            }
            dialog.Update(camera);

            // TODO (****) Note 100% certain about setting restoreFocusWidget to true here.
            // Originally this was false.  Changed it to true so that when returning to the
            // MainMenu, after being in Options, the same MainMenu button would be in focus.
            // This just feels better.  No clue if it might cause issues later.  Probably
            // the right thing to do is to have this be a per-dialog option that is set on
            // creation and maybe even changed dynamically.
            SetDialogState(restoreFocusWidget: true);
        }   // end of ShowDialog()

        /// <summary>
        /// Brute force kills the given dialog.  Note that this is a bit overkill in some
        /// ways.  Normally, a dialog only needs to be killed if it is active and in the
        /// current list.  This code doesn't make those assumptions so it is save to call
        /// on a dialog that is not active.
        /// </summary>
        /// <param name="dialog"></param>
        /// <returns>true if found</returns>
        public static bool KillDialog(BaseDialog dialog)
        {
#if DEBUG_SHOW_KILL
            Debug.Print("killing dialog : " + dialog._name);
#endif

            bool found = false;

            for (int i = 0; i < dialogCameraSets.Count; i++)
            {
                if (dialogCameraSets[i].Dialog == dialog)
                {
                    if (currentFocusDialog == dialog)
                    {
                        dialog.ClearFocus();
                        currentFocusDialog = null;
                    }
                    dialogCameraSets.RemoveAt(i);
                    dialog.Deactivate();
                    found = true;
                    break;
                }
            }

            // Only reset state if a dialog was found to kill.  If not, 
            // nothing has changed so why bother to recalc anything.
            if (found)
            {
                // Update state of all remaining dialogs.  Since this focus change
                // is the result of killing the current in-focus dialog, we want 
                // to try and restore with focus widget state of the new dialog 
                // coming into focus.
                SetDialogState(restoreFocusWidget: true);

                // If no dialogs then unregister for input events.
                if (dialogCameraSets.Count == 0)
                {
                    instance.UnregisterForInputEvents();
                }
            }

            return found;
        }   // end of KillDialog()

        /// <summary>
        /// Looks at the current dialog stack and sets their state.
        /// 
        /// Hack?  Basically, instead of changing state and assuming that
        /// we always keep things perfectly in sync, this just says that
        /// we can look at the stack and determine the state for each dialog.
        /// This way the dialogs always have a valid state.
        /// </summary>
        /// <param name="restoreFocusWidget">If the dialog was previously in focus, restore focus to the widget that was in focus (if possible).</param>
        static void SetDialogState(bool restoreFocusWidget)
        {
#if STATE_DEBUG
            Debug.Print("\nDialog Stack\npre SetDialogState()");
            for (int i = 0; i < dialogCameraSets.Count; i++)
            {
                Debug.Print("    " + dialogCameraSets[i].Dialog._name + " active: " + dialogCameraSets[i].Dialog.Active.ToString());
            }
#endif

            // Clear focus state first.
            if (currentFocusDialog != null)
            {
                currentFocusDialog.ClearFocus();
            }

            bool modalFound = false;
            for (int i = dialogCameraSets.Count - 1; i >= 0; i--)
            {
                if (!modalFound)
                {
                    if (!dialogCameraSets[i].Dialog.Active)
                    {
                        dialogCameraSets[i].Dialog.Activate();
                    }
                }

                if (!(dialogCameraSets[i].Dialog is BaseDialogNonModal))
                {
                    modalFound = true;
                }
            }

            // Give focus to last one on list that is focusable, if any.
            currentFocusDialog = null;
            for (int i = dialogCameraSets.Count - 1; i >= 0 ; i--)
            {
                if (dialogCameraSets[i].Dialog.Focusable)
                {
                    currentFocusDialog = dialogCameraSets[i].Dialog;

                    if (!restoreFocusWidget)
                    {
                        currentFocusDialog.PrevFocusWidget = null;
                    }

                    currentFocusDialog.SetFocus();

                    // Found our focus, break;
                    break;
                }
            }

#if STATE_DEBUG
            Debug.Print("\npost SetDialogState()");
            for (int i = 0; i < dialogCameraSets.Count; i++)
            {
                Debug.Print("    " + dialogCameraSets[i].Dialog._name + " active: " + dialogCameraSets[i].Dialog.Active.ToString());
            }
            Debug.Print("\n");
#endif
        }   // end of SetDialogState()

        /// <summary>
        /// Steps the dialog focus to the next focusable dialog.
        /// </summary>
        public static void FocusNextDialog()
        {
            // Find the next active dialog and set focus on it.
            for (int i = 0; i < dialogCameraSets.Count; i++)
            {
                DialogCameraSet set = dialogCameraSets[i];
                if (set.Dialog == currentFocusDialog)
                {
                    // Ok, we've found the current one.  Now loop forward looking 
                    // the the next dialog that is both Active and Focusable.
                    while (true)
                    {
                        i = (i + 1) % dialogCameraSets.Count;
                        set = dialogCameraSets[i];
                        if (set.Dialog.Focusable && set.Dialog.Active)
                        {
                            // Clear focus on old one and set focus on new one.
                            // Note this needs to work even if they are the same.  At the 
                            // least it will move to the top focus widget for the dialog.
                            currentFocusDialog.ClearFocus();
                            set.Dialog.SetFocus(true);
                            
                            return;
                        }
                    }
                }
            }

        }   // end of FocusNextDialog()

        public static void FocusPrevDialog()
        {
            // Find the previous active dialog and set focus on it.
            for (int i = 0; i < dialogCameraSets.Count; i++)
            {
                DialogCameraSet set = dialogCameraSets[i];
                if (set.Dialog == currentFocusDialog)
                {
                    // Ok, we've found the current one.  Now loop backward looking 
                    // the the next dialog that is both Active and Focusable.
                    while (true)
                    {
                        i = (i + dialogCameraSets.Count - 1) % dialogCameraSets.Count;
                        set = dialogCameraSets[i];
                        if (set.Dialog.Focusable && set.Dialog.Active)
                        {
                            // Clear focus on old one and set focus on new one.
                            // Note this needs to work even if they are the same.  At the 
                            // least it will move to the top focus widget for the dialog.
                            currentFocusDialog.ClearFocus();
                            set.Dialog.SetFocus(false);

                            return;
                        }
                    }
                }
            }
        }   // end of FocusPrevDialog()

        /// <summary>
        /// Called when the user is navigating the UI using the DPad or arrow keys.
        /// If there is not widget on the current dialog, then try and find the
        /// next active dialog in the correct direction.
        /// </summary>
        public static void FocusUpDialog()
        {
            List<BaseDialog> dialogs = GetActiveDialogs();
            // Don't even bother to do anything unless we have other dialogs.
            if (dialogs.Count <= 1)
            {
                return;
            }

            // Find the nearest dialog in the correct direction.
            BaseDialog best = null;
            float bestDist = float.MaxValue;
            foreach (BaseDialog dialog in dialogs)
            {
                if (currentFocusDialog == dialog) continue; // Ignore self.
                if (!dialog.Focusable) continue;            // Ignore non-focusable dialogs.

                Vector2 delta = dialog.Rectangle.Center - currentFocusDialog.Rectangle.Center;
                float dist = delta.LengthSquared();

                if (delta.Y < 0 && delta.Y >= Math.Abs(delta.X) && dist < bestDist)
                {
                    best = dialog;
                    bestDist = dist;
                }
            }

            // Found one?  Set focus to it.
            if (best != null)
            {
                currentFocusDialog.ClearFocus();
                best.SetFocus(true);
            }

        }   // end of FocusUpDialog()

        /// <summary>
        /// Called when the user is navigating the UI using the DPad or arrow keys.
        /// If there is not widget on the current dialog, then try and find the
        /// next active dialog in the correct direction.
        /// </summary>
        public static void FocusDownDialog()
        {
            List<BaseDialog> dialogs = GetActiveDialogs();
            // Don't even bother to do anything unless we have other dialogs.
            if (dialogs.Count <= 1)
            {
                return;
            }

            // Find the nearest dialog in the correct direction.
            BaseDialog best = null;
            float bestDist = float.MaxValue;
            foreach (BaseDialog dialog in dialogs)
            {
                if (currentFocusDialog == dialog) continue; // Ignore self.
                if (!dialog.Focusable) continue;            // Ignore non-focusable dialogs.

                Vector2 delta = dialog.Rectangle.Center - currentFocusDialog.Rectangle.Center;
                float dist = delta.LengthSquared();

                if (delta.Y > 0 && delta.Y >= Math.Abs(delta.X) && dist < bestDist)
                {
                    best = dialog;
                    bestDist = dist;
                }
            }

            // Found one?  Set focus to it.
            if (best != null)
            {
                currentFocusDialog.ClearFocus();
                best.SetFocus(true);
            }

        }   // end of FocusDownDialog()

        /// <summary>
        /// Called when the user is navigating the UI using the DPad or arrow keys.
        /// If there is not widget on the current dialog, then try and find the
        /// next active dialog in the correct direction.
        /// </summary>
        public static void FocusLeftDialog()
        {
            List<BaseDialog> dialogs = GetActiveDialogs();
            // Don't even bother to do anything unless we have other dialogs.
            if (dialogs.Count <= 1)
            {
                return;
            }

            // Find the nearest dialog in the correct direction.
            BaseDialog best = null;
            float bestDist = float.MaxValue;
            foreach (BaseDialog dialog in dialogs)
            {
                if (currentFocusDialog == dialog) continue; // Ignore self.
                if (!dialog.Focusable) continue;            // Ignore non-focusable dialogs.

                Vector2 delta = dialog.Rectangle.Center - currentFocusDialog.Rectangle.Center;
                float dist = delta.LengthSquared();

                if (delta.X < 0 && delta.X >= Math.Abs(delta.Y) && dist < bestDist)
                {
                    best = dialog;
                    bestDist = dist;
                }
            }

            // Found one?  Set focus to it.
            if (best != null)
            {
                currentFocusDialog.ClearFocus();
                best.SetFocus(true);
            }

        }   // end of FocusLeftDialog()

        /// <summary>
        /// Called when the user is navigating the UI using the DPad or arrow keys.
        /// If there is not widget on the current dialog, then try and find the
        /// next active dialog in the correct direction.
        /// </summary>
        public static void FocusRightDialog()
        {
            List<BaseDialog> dialogs = GetActiveDialogs();
            // Don't even bother to do anything unless we have other dialogs.
            if (dialogs.Count <= 1)
            {
                return;
            }

            // Find the nearest dialog in the correct direction.
            BaseDialog best = null;
            float bestDist = float.MaxValue;
            foreach (BaseDialog dialog in dialogs)
            {
                if (currentFocusDialog == dialog) continue; // Ignore self.
                if (!dialog.Focusable) continue;            // Ignore non-focusable dialogs.

                Vector2 delta = dialog.Rectangle.Center - currentFocusDialog.Rectangle.Center;
                float dist = delta.LengthSquared();

                if (delta.X > 0 && delta.X >= Math.Abs(delta.Y) && dist < bestDist)
                {
                    best = dialog;
                    bestDist = dist;
                }
            }

            // Found one?  Set focus to it.
            if (best != null)
            {
                currentFocusDialog.ClearFocus();
                best.SetFocus(true);
            }

        }   // end of FocusRightDialog()


        public override void RegisterForInputEvents()
        {
            // Register to get keyboard input for tab focus.
            KoiLibrary.InputEventManager.RegisterForEvent(instance, InputEventManager.Event.Keyboard);
        }

        public override void UnregisterForInputEvents()
        {
            // Unregister.
            KoiLibrary.InputEventManager.UnregisterForEvent(instance, InputEventManager.Event.Keyboard);
        }

        static public void ProcessMouseHits()
        {
            if (DialogIsActive)
            {
                // Find the object, if any, under the mouse.
                // Only override the current value if we hit something.
                Vector2 mouseHit = LowLevelMouseInput.Position.ToVector2();

                InputEventHandler hitObject = DialogManagerX.instance.HitTest(mouseHit);
                KoiLibrary.InputEventManager.MouseHitObject = hitObject;
            }
        }   // end of ProcessMouseHits()

        static public void ProcessTouchHits(List<TouchSample> sampleList)
        {
            if (DialogIsActive)
            {
                // Associate touches with widgets.
                KoiLibrary.InputEventManager.TouchHitObject = null;
                if (sampleList.Count > 0)
                {
                    // Do hit testing with first touch only since they'll
                    // all be at the same position.
                    KoiLibrary.InputEventManager.TouchHitObject = DialogManagerX.instance.HitTest(sampleList[0].Position);
                    // If widget is found, assign it as the hit object for all the touches.
                    foreach (TouchSample touchSample in sampleList)
                    {
                        touchSample.HitObject = KoiLibrary.InputEventManager.TouchHitObject;
                    }
                }   // end of if we have touches.
            }
        }   // end of ProcessTouchHits()

        #endregion

        #region InputEventHandler

        public override bool ProcessKeyboardEvent(KeyInput input)
        {
            if (DialogIsActive && currentFocusDialog != null)
            {
                if (input.Key == Microsoft.Xna.Framework.Input.Keys.Tab)
                {
                    if (input.Shift)
                    {
                        currentFocusDialog.FocusPrevWidget();
                    }
                    else
                    {
                        currentFocusDialog.FocusNextWidget();
                    }

                    return true;
                }
            }

            return base.ProcessKeyboardEvent(input);
        }

        /// <summary>
        /// Do mouse hit testing.  The input hitLocation is in pixels
        /// but gets transformed into camera space before doing the 
        /// actual hit testing.
        /// </summary>
        /// <param name="hitLocation"></param>
        /// <returns></returns>
        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            InputEventHandler result = null;

            if (DialogManagerX.DialogIsActive)
            {
                // Transform hit by default camera.
                Vector2 defaultCameraHit = defaultCamera.ScreenToCamera(hitLocation);

                // Hit test all active dialogs, this allows clicking on them
                // to being them into focus.  Loop backwards to give top ones priority.
                for (int i = dialogCameraSets.Count - 1; i >= 0; i--)
                {
                    DialogCameraSet set = dialogCameraSets[i];
                    if (set.Dialog.Active)
                    {
                        // If dialog has its own camera, use it to adjust the hit location.
                        Vector2 hit = defaultCameraHit;
                        if (set.Camera != null)
                        {
                            hit = set.Camera.ScreenToCamera(hitLocation);
                        }

                        result = set.Dialog.HitTest(hit);
                        if (result != null)
                        {
                            break;
                        }
                    }

                    // If we've got down to a modal dialog, stop testing 
                    // since we don't want to hit anything beneath it.
                    if (set.Dialog.IsModalDialog)
                    {
                        break;
                    }
                }

                // If mouse is not over a dialog, and we have a focusDialog and it's modal 
                // have the manager claim mouse hit.  This prevents any underlying UI from claiming.
                if (result == null && currentFocusDialog != null && !(currentFocusDialog is BaseDialogNonModal))
                {
                    result = this;
                }
            }

            return result;
        }   // end of HitTest()

        #endregion

        #region Internal

        /// <summary>
        /// Returns a list of all the currently active dialogs.
        /// </summary>
        /// <returns></returns>
        static List<BaseDialog> GetActiveDialogs()
        {
            List<BaseDialog> dialogs = new List<BaseDialog>();

            foreach (DialogCameraSet set in dialogCameraSets)
            {
                if (set.Dialog.Active)
                {
                    dialogs.Add(set.Dialog);
                }
            }

            return dialogs;
        }   // end of GetActiveDialogs()

        #endregion

    }   // end of class DialogManager
}
