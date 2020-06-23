
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.SimWorld;
using Boku.Common;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;

namespace Boku
{
    /// <summary>
    /// UpdateObject for InGame -> EditTexture
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        protected class TexturePickerUpdateObj : BaseEditUpdateObj
        {

            // c'tor
            public TexturePickerUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of TexturePickerUpdateObj c'tor


            /// <summary>
            /// TexturePickerUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                // No need to check for input focus or anything.  If
                // we're active then the TexturePicker object has focus.

                shared.texturePicker.Update();

                // If the TexturePicker is no longer active we're done.
                if (!shared.texturePicker.Active)
                {
                    parent.CurrentUpdateMode = UpdateMode.EditTexture;
                    return;
                }

               // Do the common bits of the Update().
                UpdateCamera();
                UpdateWorld();

            }   // end of TexturePickerUpdateObj Update()

            public override void Activate()
            {
                base.Activate();

                HelpOverlay.Push("TexturePicker");
                parent.cursor3D.Deactivate();
                shared.texturePicker.Activate();
            }   // end of TexturePickerUpdateObj Activate()

            public override void Deactivate()
            {
                base.Deactivate();

                // Probably overkill.
                shared.texturePicker.Deactivate();
            }

        }   // end of class TexturePickerUpdateObj

    }   // end of class InGame

}   // end of namespace Boku


