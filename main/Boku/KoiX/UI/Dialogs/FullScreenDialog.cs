
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
using KoiX.Text;
using KoiX.UI;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Dialog owned by each BaseScene designed to contain
    /// all the widgets which normally wouldn't be in a dialog.
    /// For instance, the back button.
    /// Coordinate system uses default camera. Zoom = 1.0 and 
    /// postion is 0, 0 (origin at center of screen).
    /// Note this is non-modal.  We don't want it locking out
    /// other UI interactions.
    /// 
    /// TODO (scoy) Would it be useful to have another dialog type
    /// that doesn't have a base plate like this one but also
    /// isn't always fullscreen?
    /// </summary>
    public class FullScreenDialog : BaseDialogNonModal
    {
        #region Members
        #endregion

        #region Accessors
        #endregion

        #region Public

        /// <summary>
        /// c'tor
        /// 
        /// By default, FullScreenDialog is not focusable since it is designed to sit
        /// in the background. But, for some cases we may want to override that.
        /// </summary>
        /// <param name="focusable"></param>
        /// <param name="theme"></param>
        public FullScreenDialog(bool focusable = false, ThemeSet theme = null)
            : base(theme: theme)
        {
#if DEBUG
            _name = "FullScreenDialog";
#endif

            this.focusable = focusable;
        }

        public override void Update(SpriteCamera camera)
        {
            // Fit self to full screen.
            // Need to make sure the size takes the zoom 
            // factor of the camera into account.

            Debug.Assert(camera != null, "Do we ever have a null camera?  Shouldn't we always at least have the default one?");

            float zoom = camera != null ? 1.0f / camera.Zoom : 1.0f;

            float width = camera.ScreenSize.X * zoom;
            float height = camera.ScreenSize.Y * zoom;

            // Has size or scaling changed?
            if (rect.Width != width || rect.Height != height)
            {
                rect.Size = new Vector2(width, height);
                // Center around origin since default camera is looking at origin.
                rect.Position = -rect.Size / 2.0f;

                Dirty = true;
            }

            // Update all our children.
            base.Update(camera);
        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            // For this dialog we don't want the base plate to be rendered since
            // the widgets are "free floating" so just render the widgets.
            if (state != State.Inactive)
            {
                RenderWidgets(camera);
            }

        }   // end of Render()

        #endregion

        #region InputEventHandler

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitLocation">Hit location in camera coordinates.</param>
        /// <returns></returns>
        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            InputEventHandler result = base.HitTest(hitLocation);

            // Since FullScreenDialog is meant as a container for loose, free-floating
            // widgets we don't want to claim focus on behalf of the dialog.  Otherwise
            // no input would get to anything underneath.
            if (result == this)
            {
                result = null;
            }

            return result;
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class FullScreenDialog

}   // end of namespace KoiX.UI.Dialogs
