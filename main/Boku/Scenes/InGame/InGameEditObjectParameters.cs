
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

using KoiX;
using KoiX.Input;

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
    /// UpdateObject for InGame -> EditObjectParameters
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        protected class EditObjectParametersUpdateObj : BaseEditUpdateObj
        {

            // c'tor
            public EditObjectParametersUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of EditObjectParametersUpdateObj c'tor


            /// <summary>
            /// EditObjectParametersUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                base.Update();

                if (AuthUI.IsModalActive)
                {
                    return;
                }

                // No need to check for input focus or anything.  If
                // we're active then the EditObjectParameters object has focus.

                GamePadInput pad = GamePadInput.GetGamePad0();

                // Jump straight to Tool Menu.
                if (pad.Back.WasPressed)
                {
                    parent.CurrentUpdateMode = UpdateMode.ToolMenu;
                    return;
                }

                shared.editObjectParameters.Update();

                // If the Object param editor is no longer active we're done.
                if (!shared.editObjectParameters.Active)
                {
                    return;
                }

                // TODO (****) Needed?
                // Do the common bits of the Update().
                UpdateCamera();
                UpdateWorld();

            }   // end of EditObjectParametersUpdateObj Update()

            private object timerInstrument = null;

            public override void Activate()
            {
                if (!active)
                {
                    base.Activate();

                    parent.cursor3D.Deactivate();
                    shared.editObjectParameters.Activate();

                    timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameEditObjectParameters);
                }
            }   // end of EditObjectParametersUpdateObj Activate()

            public override void Deactivate()
            {
                if (active)
                {
                    shared.editObjectParameters.Deactivate();

                    Instrumentation.StopTimer(timerInstrument);

                    base.Deactivate();
                }
            }

        }   // end of class EditObjectParametersUpdateObj

    }   // end of class InGame

}


