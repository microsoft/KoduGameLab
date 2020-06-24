
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

using Boku;
using Boku.Scenes.InGame.MouseEditTools;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// GamePad version ot ToolBarDialog
    /// </summary>
    public class GamePadToolBarDialog : BaseDialogNonModal
    {
        #region Members

        static GamePadToolBarDialog instance;   // Used for static calls.

        SpriteCamera camera;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public GamePadToolBarDialog(ThemeSet theme = null)
            : base(theme: theme)
        {
            instance = this;
#if DEBUG
            _name = "GamePadToolBarDialog";
#endif
            // Change ref to what base class has set.
            // Allows us to use theme rather than this.Theme.
            theme = this.ThemeSet;

        }   // end of c'tor

        public override void Update(SpriteCamera camera)
        {
            // Hold ref to current camera.
            this.camera = camera;

        }   // end of Update()



        #endregion

        #region Internal
        #endregion

    }   // end of class GamePadToolBarDialog
}   // end of namespace KoiX.UI.Dialogs
