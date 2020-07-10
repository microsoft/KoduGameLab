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
using KoiX.Text;
using KoiX.UI;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Infinite canvas dialog.
    /// Just acts as a container for widgets without imposing any limits.
    /// Needs a camera passed in to be scrolled, zoomed, and rotated.
    /// The ultimate "do nothing" dialog.
    /// </summary>
    public class CanvasDialog : BaseDialogNonModal
    {
        #region Members
        #endregion

        #region Accessors
        #endregion

        #region Public

        /// <summary>
        /// c'tor
        /// </summary>
        public CanvasDialog()
        {
#if DEBUG
            _name = "CanvasDialog";
#endif

            focusable = false;
            hitTestable = true;     // Want to be able to click and drag this.
        }

        public override void Update(SpriteCamera camera)
        {
            // NOTE: CanvasDialog _must_ leave it's Rectangle invalid
            // otherise the hit testing tries to clip agains the rect
            // bounds and things just don't line up.  This should be
            // unique to the CanvasDialog but you never know.

            // Update all our children.
            base.Update(camera);
        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            // For this dialog we don't want the base plate to be rendered since
            // the widgets are "free floating" so just render the widgets.
            RenderWidgets(camera, Vector2.Zero);
        }   // end of Render()

        #endregion

        #region InputEventHandler

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitLocation">Assumed to be transformed into canvas space by DialogManager.</param>
        /// <returns></returns>
        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            InputEventHandler result = base.HitTest(hitLocation);

            // Since CanvasDialog is meant as a container for loose, free-floating
            // widgets we don't want to claim focus on behalf of the dialog.  Otherwise
            // no input would get to anything underneath.
            if (result == this)
            {
                //result = null;
            }

            return result;
        }

        #endregion

        #region Internal
        #endregion

    }   // end of class CanvasDialog

}   // end of namespace KoiX.UI.Dialogs
