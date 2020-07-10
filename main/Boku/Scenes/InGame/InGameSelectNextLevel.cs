// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
    /// UpdateObject for InGame -> EditWorldParameters
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        protected class SelectNextLevelUpdateObj : BaseEditUpdateObj
        {

            // c'tor
            public SelectNextLevelUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of EditWorldParametersUpdateObj c'tor


            /// <summary>
            /// EditWorldParametersUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                base.Update();

            }   // end of EditWorldParametersUpdateObj Update()

            public override void Activate()
            {
                if (!active)
                {
                    base.Activate();

                    HelpOverlay.Push("SelectNextLevel");
                    parent.cursor3D.Deactivate();
                }
            }   // end of EditWorldParametersUpdateObj Activate()

            public override void Deactivate()
            {
                if (active)
                {
                    HelpOverlay.Pop();

                    parent.CurrentUpdateMode = UpdateMode.EditWorldParameters;

                    base.Deactivate();
                }
            }

        }   // end of class EditWorldParametersUpdateObj

    }   // end of class InGame

}


