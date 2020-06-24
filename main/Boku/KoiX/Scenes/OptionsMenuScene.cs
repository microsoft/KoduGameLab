
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;

namespace KoiX.Scenes
{
    /// <summary>
    /// This is the scene activated when the user chooses OPTIONS from the main menu.
    /// Right now we just kind of skip forward to the first page of the options.
    /// Eventually, if we have alot more options pages then this page will be a
    /// menu allowing you to skip to the section you care about.
    /// </summary>
    public class OptionsMenuScene : BasePageScene
    {
        #region Members

        OptionsPage1Scene page1;
        OptionsPage2Scene page2;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public OptionsMenuScene()
            : base("OptionsMenuScene")
        {
            // TODO (****) When the full menu is implemented, this should point
            // to this scene instead of going all the way back to the main menu.
            string backTarget = "MainMenuScene";

            backButton.TargetScene = "MainMenuScene";
            NextTargetScene = "OptionsPage1Scene";

            page1 = new OptionsPage1Scene(nextLabelId: "optionsParams.Language");
            page1.BackTargetScene = backTarget;
            page1.NextTargetScene = "OptionsPage2Scene";
            page1.PrevTargetScene = "";

            page2 = new OptionsPage2Scene(prevLabelId: "optionsParams.Options");
            page2.BackTargetScene = backTarget;
            page2.NextTargetScene = "";
            page2.PrevTargetScene = "OptionsPage1Scene";

        }   // end of c'tor

        public override void Update()
        {
            // TODO (****) Hack to skip right to first options page.
            SceneManager.SwitchToScene(page1);

            // Keep the nav buttons happy.
            base.Update();

            if (Active)
            {
            }
        }   // end of Update()

        public override void Render(RenderTarget2D rt)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;

            if (rt != null)
            {
                device.SetRenderTarget(rt);
            }

            SpriteBatch batch = KoiLibrary.SpriteBatch;

            device.Clear(Color.Khaki);

            string message = "options menu scene";
            Vector2 size = SharedX.GetGameFont30Bold().MeasureString(message);
            Vector2 pos = (new Vector2(KoiLibrary.ViewportSize.X, KoiLibrary.ViewportSize.Y) - size) / 2.0f;
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
            KoiX.Text.TextHelper.DrawStringNoBatch(SharedX.GetGameFont30Bold, message, pos, Color.LightGray, outlineColor: Color.Black, outlineWidth: 1.2f);

            if (rt != null)
            {
                device.SetRenderTarget(null);
            }
        }   // end of Render()

        #endregion

        #region Internal
        #endregion

    }   // endof class OptionsMenuScene
}   // end of namespace KoiX.Scenes
