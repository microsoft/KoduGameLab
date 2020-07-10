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

using KoiX;
using KoiX.Managers;
using KoiX.Scenes;

using Boku.Base;
using Boku.Fx;
using Boku.Scenes.InGame.MouseEditTools;
using Boku.SimWorld;
using Boku.SimWorld.Path;
using Boku.SimWorld.Terra;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.Common.ParticleSystem;
using Boku.Common.Xml;
using Boku.Programming;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Audio;

#if NOTES

    TODO (mouse)  Tag things that need to be looked at with this.

    Try using the shim and pie menu from InGameEditObjectAddItem.  It should be (or could be) made publically accessible.

#endif



namespace Boku
{
    /// <summary>
    /// UpdateObject for InGame -> EditObject
    /// </summary>
    public partial class InGame : GameObject, INeedsDeviceReset
    {
        public partial class TouchEditUpdateObj : BaseEditUpdateObj
        {
            #region Members

            private ToolBar toolBar = new ToolBar();
            private TouchEditToolBox toolBox = new TouchEditToolBox();

            #endregion

            #region Accessors

            public ToolBar ToolBar
            {
                get { return toolBar; }
            }

            public TouchEditToolBox ToolBox
            {
                get { return toolBox; }
            }

            /// <summary>
            /// True if in an editing mode that is changing the height map or materials.
            /// </summary>
            public bool EditingTerrain
            {
                get { return toolBox.Active && toolBox.EditingTerrain; }
            }

            #endregion

            #region Public

            // c'tor
            public TouchEditUpdateObj(InGame parent, ref Shared shared)
                : base(parent, ref shared)
            {
                commandMap = new CommandMap("TouchEditBase");
            }   // end of TouchEditUpdateObj c'tor

            public override void Update()
            {
            }   // end of Update()

            public override void Activate()
            {
                if (!active)
                {
                    base.Activate();

                    CommandStack.Push(commandMap);
                    HelpOverlay.Push("MouseEditBase");  // This will get replaced by the active tool.

                    // Default start in Camera mode since it's interactive and non-destructive.
                    EditWorldScene.CurrentToolMode = EditWorldScene.ToolMode.CameraMove;
                    // Prime the pump for toolBar rendering.
                    //toolBar.Update();

                    // No need for a tool icon in the upper left since they're always 
                    // visible at the bottom of the screen.  Use this for something else?
                    HelpOverlay.ToolIcon = null;

                    /// Don't null out the cutPasteObject. That way, if we load up
                    /// a new level, we can still paste it in, allowing copy of objects
                    /// from one level to another. ***
                    parent.Cursor3D.Activate();
                    parent.Cursor3D.Hidden = true;
                    parent.Cursor3D.Rep = Cursor3D.Visual.Edit;
                    parent.Cursor3D.DiffuseColor = new Vector4(1, 1, 1, 1);

                    // TODO (mouse)
                    //RemoveFocusEffects();
                    shared.editWayPoint.Clear();
                }
            }   // end of Activate()

            public override void Deactivate()
            {
                if (active)
                {
                    CommandStack.Pop(commandMap);

                    base.Deactivate();

                    HelpOverlay.Pop();

                    ColorPalette.Active = false;

                    // TODO (mouse)
                    //RemoveFocusEffects();
                    //LastSelectedActor = null;

                    // Force the camera back to not having an offset.
                    shared.camera.SetDefaultHeightOffset(shared.CursorPosition, 0.5f);

                    shared.editWayPoint.Clear();

                    toolBox.Deactivate();

                    // Make the cursor visible so that the GamePad or RunSim
                    // side of things can choose what to do with it.
                    inGame.Cursor3D.Hidden = false;
                    inGame.Cursor3D.Rep = Cursor3D.Visual.Edit;
                }
            }   // end of Deactivate()


            #endregion

            #region Internal


            public override void LoadContent(bool immediate)
            {
                base.LoadContent(immediate);

                toolBar.LoadContent(immediate);
                toolBox.LoadContent(immediate);
            }   // end of LoadContent()

            public override void UnloadContent()
            {
                base.UnloadContent();

                toolBar.UnloadContent();
                toolBox.UnloadContent();
            }   // end of UnloadContent()

            #endregion

        }   // end of class MouseEditUpdateObj

    }   // end of class InGame

}   // end of namespace Boku
