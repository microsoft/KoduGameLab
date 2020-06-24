
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

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;

namespace Boku.Scenes.InGame.Tools
{
    /// <summary>
    /// Tool which deletes objects under the brush from the world.
    /// </summary>
    public class DelObjTool : BaseTool
    {
        #region Members
        private static DelObjTool instance = null;

        private List<GameActor> selected = new List<GameActor>();
        private List<GameActor> unselected = new List<GameActor>();
        #endregion Members

        #region Public
        // c'tor
        public DelObjTool()
        {
            Description = Strings.Localize("tools.delObjTool");
            HelpOverlayID = @"DelObjTool";
            IconTextureName = @"\UI2D\Tools\DeleteObject";

            RightAudioStart = delegate() { };
            MiddleAudioStart = delegate() { };
            LeftAudioStart = delegate() { };
            RightAudioEnd = delegate() { };
            MiddleAudioEnd = delegate() { };
            LeftAudioEnd = delegate() { };


        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new DelObjTool();
            }
            return instance;
        }   // end of DelObjTool GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(false);

                if (DebouncePending)
                    return;

                UpdateRates();

                ProcessTriggers(
                    Terrain.EditMode.Noop,
                    Terrain.EditMode.Noop,
                    Terrain.EditMode.Noop);

                BuildSelected();

                if (LeftTriggerOn || RightTriggerOn)
                {
                    DeleteSelected();
                }

                SelectOverlay();
            }

            base.Update();
        }   // end of DelObjTool Update()
        #endregion Public

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
                    else if (InStretchMode)
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
        public override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGameDeleteTool);
            base.OnActivate();
        }   // end of HeightMapTool OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            UnSelectAll();

            Instrumentation.StopTimer(timerInstrument);
        }

        #endregion Internal

    }   // class DelObjTool

}   // end of namespace Boku.Scenes.InGame.Tools


