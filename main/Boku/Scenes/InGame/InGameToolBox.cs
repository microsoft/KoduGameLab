
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
    /// UpdateObject for InGame -> ToolBox
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {

        /// <summary>
        /// This is the update object which is active when the ToolBox has
        /// been selected from the TopLevelPalette.  It's role is to handle
        /// all the standard camera commands while passing on the other input
        /// to the ToolBox which, in turn, will pass control on down to the
        /// selected tool.
        /// </summary>
        protected class ToolBoxUpdateObj : BaseEditUpdateObj
        {
            #region Accessors
            #endregion

            // c'tor
            public ToolBoxUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
            }   // end of ToolBoxUpdateObj c'tor


            /// <summary>
            /// ToolBoxUpdateObj Update()
            /// </summary>
            /// <param name="camera"></param>
            public override void Update()
            {
                base.Update();

                // If the ToolBox is no longer active we're done.
                /*
                if (!shared.ToolBox.Active)
                {
                    return;
                }
                */

                // Do the common bits of the Update().
                // The actual input focus is in the currently active tool so
                // temporarily push ourselves onto the command stack so we
                // can still steal camera control input.
                // TODO (****) Move this up above the tool update to remove a frame of lag?  Will this cause other problems?
                CommandStack.Push(commandMap);

                // No need to lock the zoom any more since we're using the trigger buttons for grid selection.
                bool lockZoom = false;
                UpdateCamera(lockZoom);
                UpdateWorld();
                UpdateEditBrush();
                CommandStack.Pop(commandMap);

            }   // end of ToolBoxUpdateObj Update()

           // private object timerInstrument = null;

            public override void Activate()
            {
                base.Activate();

                parent.cursor3D.Deactivate();

            }   // end of ToolBoxUpdateObj Activate()

            public override void Deactivate()
            {
                base.Deactivate();

            }

        }   // end of class ToolBoxUpdateObj

    }   // end of class InGameToolBox

}   // end of namespace Boku


