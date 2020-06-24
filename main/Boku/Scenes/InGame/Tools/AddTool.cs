
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

using Boku.Audio;
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
    /// Tool which adds terrain to a world using 2 modes.  The basic mode is to just add
    /// new terrain at the minimum altitude.  The second mode adds terrain at the max
    /// altitude of it's neighbors.  This allows areas to be easily extended at the same
    /// altitude.  In both cases existing terrain is not touched.  This tool also supports
    /// the material picker for choosing which material to use when adding terrain.
    /// </summary>
    public class AddTool : BaseTool
    {
        #region Members
        private static AddTool instance = null;

        #endregion Members

        #region Public
        // c'tor
        public AddTool()
        {
            Description = Strings.Localize("tools.addTool");
            HelpOverlayID = @"AddTool";
            HelpOverlayMagicBrushID = @"AddToolMagicBrush";
            HelpOverlayStartID = @"AddToolStart";
            HelpOverlayGoingID = @"AddToolGoing";
            IconTextureName = @"\UI2D\Tools\AddTool";

            RightAudioStart = delegate() { Foley.PlayEarthUp(); };
            MiddleAudioStart = delegate() { Foley.PlayPaint(); };
            LeftAudioStart = delegate() { Foley.PlayEraseLand(); };
            RightAudioEnd = delegate() { Foley.StopEarthUp(); };
            MiddleAudioEnd = delegate() { Foley.StopPaint(); };
            LeftAudioEnd = delegate() { Foley.StopEraseLand(); };

        }   // end of c'tor

        public static BaseTool GetInstance()
        {
            if (instance == null)
            {
                instance = new AddTool();
            }
            return instance;
        }   // end of AddTool GetInstance()

        public override void Update()
        {
            if (Active)
            {
                CheckSelectCursor(false);

                if (DebouncePending)
                    return;

                UpdateRates();

                ProcessTriggers(
                    Terrain.EditMode.AddAtCenter,
                    Terrain.EditMode.PaintMaterial,
                    Terrain.EditMode.Delete);

                SelectOverlay();
            }

            base.Update();
        }   // end of AddTool Update()
        #endregion Public

        #region Internal
        private object timerInstrument = null;

        public override void OnActivate()
        {
            timerInstrument = Instrumentation.StartTimer(Instrumentation.TimerId.InGamePaintTool);

            base.OnActivate();

            Boku.InGame.inGame.Cursor3D.Hidden = true;

            // If the location of the cursor is not over any terrain then
            // don't allow the magic brush as the default.
            if (Terrain.GetTerrainHeightFlat(Boku.InGame.inGame.Cursor3D.Position) == 0.0f)
            {
                // By not including the magic brush in the brush set we
                // force the picker to change the current brush to one
                // of the standard brushes if not already.
            }

        }   // end of AddTool OnActivate()

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            Instrumentation.StopTimer(timerInstrument);
        }   // end of AddTool OnDeactivate()

        #endregion Internal

    }   // class AddTool

}   // end of namespace Boku.Scenes.InGame.Tools


