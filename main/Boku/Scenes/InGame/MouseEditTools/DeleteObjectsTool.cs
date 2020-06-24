
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.Scenes.InGame.MouseEditTools
{
    /// <summary>
    /// Tool which deletes objects under the brush from the world.
    /// </summary>
    public class DeleteObjectsTool : BaseMouseEditTool
    {
        #region Members
        private static DeleteObjectsTool instance = null;

        private List<GameActor> selected = new List<GameActor>();
        private List<GameActor> unselected = new List<GameActor>();
        #endregion Members

        #region Public
        // c'tor
        public DeleteObjectsTool()
        {
            HelpOverlayID = null;

            RightAudioStart = delegate() { };
            MiddleAudioStart = delegate() { };
            LeftAudioStart = delegate() { };
            RightAudioEnd = delegate() { };
            MiddleAudioEnd = delegate() { };
            LeftAudioEnd = delegate() { };

        }   // end of c'tor

        public static BaseMouseEditTool GetInstance()
        {
            if (instance == null)
            {
                instance = new DeleteObjectsTool();
            }
            return instance;
        }   // end of DeleteObjectsTool GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(false);

                // Don't want any changes to terrain.
                SetEditModes(Terrain.EditMode.Noop, Terrain.EditMode.Noop, Terrain.EditMode.Noop);

                SelectOverlay();
            }

            base.Update();
        }   // end of DeleteObjectsTool Update()
        #endregion Public

        protected override void ProcessTouch()
        {
            if (TouchInput.TouchCount > 0 && Boku.InGame.inGame.TouchEdit.HasNonUITouch())
            {
                if (shared.currentTouchAction == ToolBar.TouchControls.BrushActionIDs.baDelete)
                {
                    BuildSelected();
                    DeleteSelected();
                }
            }

            base.ProcessTouch();
        }

        protected override void ProcessPoint()
        {
            BuildSelected();
            DeleteSelected();

            base.ProcessPoint();
        }

        protected override void ProcessSelection()
        {
            BuildSelected();
            DeleteSelected();

            base.ProcessSelection();
        }

        #region Internal
        private void BuildSelected()
        {
            List<GameThing> gameThings = inGame.gameThingList;

            selected.Clear();
            unselected.Clear();

            Brush2DManager.Brush2D brush = Brush2DManager.GetActiveBrush();
            bool isMagicBrush = brush.Shape == Brush2DManager.BrushShape.Magic;

            for (int i = 0; i < gameThings.Count; ++i)
            {
                GameActor actor = gameThings[i] as GameActor;
                if (actor != null)
                {
                    Vector3 pos = actor.Movement.Position;

                    bool actorSelected = false;
                    if (isMagicBrush)
                    {
                        actorSelected = Terrain.Current.PositionSelected(pos);
                    }
                    else if (UsingLinearBrush)
                    {
                        actorSelected = Terrain.Current.PositionSelected(pos,
                            shared.editBrushStart,
                            shared.editBrushPosition,
                            shared.editBrushRadius);
                    }
                    else
                    {
                        actorSelected = Terrain.Current.PositionSelected(pos,
                            shared.editBrushPosition,
                            shared.editBrushRadius);
                    }
                    if (actorSelected)
                    {
                        actor.MakeSelected(true, Vector4.UnitX);
                        selected.Add(actor);
                    }
                    else
                    {
                        actor.MakeSelected(false, Vector4.Zero);
                        unselected.Add(actor);
                    }
                }
            }
        }

        private void DeleteSelected()
        {
            for (int i = 0; i < selected.Count; ++i)
            {
                selected[i].MakeSelected(false, Vector4.Zero);
                Boku.InGame.DeleteThingFromScene(selected[i]);
            }
        }

        private void UnSelectAll()
        {
            for (int i = 0; i < selected.Count; ++i)
            {
                selected[i].MakeSelected(false, Vector4.Zero);
            }
        }
        private object timerInstrument = null;
        protected override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameDeleteTool);
            base.OnActivate();


            inGame.ShowCursor();

        }   // end of HeightMapTool OnActivate()

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            UnSelectAll();

            inGame.HideCursor();
            Instrumentation.StopTimer(timerInstrument);
        }

        #endregion Internal

    }   // class DeleteObjectsTool

}   // end of namespace Boku.Scenes.InGame.MouseEditTools


