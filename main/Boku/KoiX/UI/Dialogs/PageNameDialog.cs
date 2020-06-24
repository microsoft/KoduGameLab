
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
using KoiX.UI;

namespace KoiX.UI.Dialogs
{
    public class PageNameDialog : BaseDialog
    {
        #region Members
        #endregion

        #region Accessors
        #endregion

        #region Public

        public PageNameDialog()
            : base()
        {
        }

        public override void Update(SpriteCamera camera)
        {
            throw new NotImplementedException();
        }

        public override void Render(SpriteCamera camera)
        {
            throw new NotImplementedException();
        }

        public override void RegisterForInputEvents()
        {
            // Register to get all mouse events.  
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);
            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseRightDown);
            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseRightUp);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseWheel);
            // TODO (****) Is this the right keyboard???
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Keyboard);
            //KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.WinFormsKeyboard);
        }   // end of RegisterForInputEvents()

        public override void UnregisterForInputEvents()
        {
            // Unregister for events.
            KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);
            KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseRightDown);
            KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseRightUp);
            KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseMove);
            KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MousePosition);
            KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseWheel);
            KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.Keyboard);
            KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.WinFormsKeyboard);
        }   // end of UnregisterForInputEvents()

        #endregion

        #region Internal
        #endregion

    }   // end of class PageNameDialog

}   // end of namespace KoiX.UI.Dialogs
