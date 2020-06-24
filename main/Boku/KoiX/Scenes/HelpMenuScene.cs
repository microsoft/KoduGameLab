
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
    /// This is the scene activated when the user chooses help from the main menu.
    /// Right now we just kind of skip forward to the first page of the help.
    /// Eventually, we should have alot more help pages and this page will be a
    /// menu allowing you to skip to the section you care about.
    /// </summary>
    public class HelpMenuScene : BasePageScene
    {
        #region Members

        HelpPageScene page1;
        HelpPageScene page2;
        HelpPageScene page3;
        HelpPageScene page4;
        HelpPageScene page5;
        HelpPageScene page6;
        HelpPageScene page7;
        HelpPageScene page8;
        HelpPageScene page9;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public HelpMenuScene()
            : base("HelpMenuScene")
        {
            // TODO (scoy) When the full menu is implemented, this should point
            // to this scene instead of going all the way back to the main menu.
            string backTarget = "MainMenuScene";

            backButton.TargetScene = "MainMenuScene";
            NextTargetScene = "HelpPage1Scene";

            page1 = new HelpPageScene("HelpPage1Scene", @"Textures\HelpScreens\Intro-Help");
            page1.BackTargetScene = backTarget;
            page1.NextTargetScene = "HelpPage2Scene";
            page1.PrevTargetScene = "";

            page2 = new HelpPageScene("HelpPage2Scene", @"Textures\HelpScreens\Load-Screen-Help");
            page2.BackTargetScene = backTarget;
            page2.NextTargetScene = "HelpPage3Scene";
            page2.PrevTargetScene = "HelpPage1Scene";

            page3 = new HelpPageScene("HelpPage3Scene", @"Textures\HelpScreens\Built-Ins-Help");
            page3.BackTargetScene = backTarget;
            page3.NextTargetScene = "HelpPage4Scene";
            page3.PrevTargetScene = "HelpPage2Scene";

            page4 = new HelpPageScene("HelpPage4Scene", @"Textures\HelpScreens\Tool-Palette-Help");
            page4.BackTargetScene = backTarget;
            page4.NextTargetScene = "HelpPage5Scene";
            page4.PrevTargetScene = "HelpPage3Scene";

            page5 = new HelpPageScene("HelpPage5Scene", @"Textures\HelpScreens\Object-Select-Help");
            page5.BackTargetScene = backTarget;
            page5.NextTargetScene = "HelpPage6Scene";
            page5.PrevTargetScene = "HelpPage4Scene";

            page6 = new HelpPageScene("HelpPage6Scene", @"Textures\HelpScreens\Tweak-Screen-Help");
            page6.BackTargetScene = backTarget;
            page6.NextTargetScene = "HelpPage7Scene";
            page6.PrevTargetScene = "HelpPage5Scene";

            page7 = new HelpPageScene("HelpPage7Scene", @"Textures\HelpScreens\Blog-Help");
            page7.BackTargetScene = backTarget;
            page7.NextTargetScene = "HelpPage8Scene";
            page7.PrevTargetScene = "HelpPage6Scene";

            page8 = new HelpPageScene("HelpPage8Scene", @"Textures\HelpScreens\Credits");
            page8.BackTargetScene = backTarget;
            page8.NextTargetScene = "HelpPage9Scene";
            page8.PrevTargetScene = "HelpPage7Scene";

            page9 = new HelpPageScene("HelpPage9Scene", @"Textures\HelpScreens\Credits2");
            page9.BackTargetScene = backTarget;
            page9.NextTargetScene = "";
            page9.PrevTargetScene = "HelpPage8Scene";

        }   // end of c'tor

        public override void Update()
        {
            // TODO (scoy) Hack to skip right to first help page.
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

            string message = "help menu scene";
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

    }   // endof class HelpMenuScene
}   // end of namespace KoiX.Scenes
