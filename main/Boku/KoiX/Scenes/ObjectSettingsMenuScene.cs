
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
using Boku.Base;

namespace KoiX.Scenes
{
    /// <summary>
    /// This is the scene activated when the user chooses Settings for a game object.
    /// Right now we just kind of skip forward to the first page of the options.
    /// Eventually, if we have alot more options pages then this page will be a
    /// menu allowing you to skip to the section you care about.
    /// </summary>
    public class ObjectSettingsMenuScene : BasePageScene
    {
        #region Members

        static ObjectSettingsMenuScene instance;

        GameActor actor;

        ObjectSettingsPage1Scene page1;
        ObjectSettingsPage2Scene page2;
        ObjectSettingsPage3Scene page3;

        #endregion

        #region Accessors

        static public GameActor Actor
        {
            get { return instance.actor; }
            set 
            { 
                instance.actor = value;
                instance.page1.Actor = value;
                instance.page2.Actor = value;
                instance.page3.Actor = value;
            }
        }

        #endregion

        #region Public

        public ObjectSettingsMenuScene()
            : base("ObjectSettingsMenuScene")
        {
            instance = this;

            string backTarget = "EditWorldScene";

            backButton.TargetScene = "EditWorldScene";
            NextTargetScene = "ObjectSettingsPage1Scene";

            page1 = new ObjectSettingsPage1Scene(nextLabelId: "editObjectParams.page2");
            page1.BackTargetScene = backTarget;
            page1.NextTargetScene = "ObjectSettingsPage2Scene";
            page1.PrevTargetScene = "";

            page2 = new ObjectSettingsPage2Scene(prevLabelId: "editObjectParams.page1", nextLabelId: "editObjectParams.page3");
            page2.BackTargetScene = backTarget;
            page2.NextTargetScene = "ObjectSettingsPage3Scene";
            page2.PrevTargetScene = "ObjectSettingsPage1Scene";

            page3 = new ObjectSettingsPage3Scene(prevLabelId: "editObjectParams.page2");
            page3.BackTargetScene = backTarget;
            page3.NextTargetScene = "";
            page3.PrevTargetScene = "ObjectSettingsPage2Scene";

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

            string message = "objet settings menu scene";
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

        public override void Activate(params object[] args)
        {
            Debug.Assert(Actor != null);

            base.Activate(args);
        }   // end of Activate()

        public override void Deactivate()
        {
            base.Deactivate();
        }   // end of Deactivate()

        #endregion

        #region Internal
        #endregion

    }   // endof class ObjectSettingsMenuScene
}   // end of namespace KoiX.Scenes
