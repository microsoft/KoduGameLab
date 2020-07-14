// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Managers;

namespace Boku
{
    /// <summary>
    /// Example XNA control for use w/ WinForms
    /// Inherits from GraphicsDeviceControl, which allows it to
    /// render using a GraphicsDevice. 
    /// This control shows how to draw animating 3D graphics inside a WinForms application. 
    /// It hooks the Application.Idle event, using this to invalidate the control, which will 
    /// cause the animation to constantly redraw.
    /// It loads a SpriteFont object through the ContentManager, then uses a SpriteBatch to draw text.
    /// </summary>
    public class XNAControl : GraphicsDeviceControl
    {
        static public XNAControl Instance = null;
        static public GraphicsDevice Device;
        static public ContentManager ContentManager;

        SpriteFont font;

        Main main = null;

        /// <summary>
        /// Initializes the control.
        /// </summary>
        protected override void Initialize()
        {
            Instance = this;
            Device = GraphicsDevice;

            // Create app's content manager.
            ContentManager = new ContentManager(Services, "");

            // Init KoiLibrary before everyting else.
            KoiLibrary.Init(ContentManager, MainForm.Instance, this.Handle);
            KoiLibrary.LoadContent(GraphicsDevice);

            // Grab Loading texture so we can not have a blank screen.
            Texture2D loadingTexture = ContentManager.Load<Texture2D>(@"Content\Textures\Loading");
            Device.Clear(Color.Black);

            Vector2 screenSize = new Vector2(Device.Viewport.Width, Device.Viewport.Height);
            Vector2 backgroundSize = new Vector2(loadingTexture.Width, loadingTexture.Height);
            SpriteBatch batch = KoiLibrary.SpriteBatch;
            batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            {
                Vector2 position = (screenSize - backgroundSize) / 2.0f;
                // Clamp to pixels.
                position.X = (int)position.X;
                position.Y = (int)position.Y;
                batch.Draw(loadingTexture, position, Color.White);
            }
            batch.End();

            // Initialize BokuGame.
            BokuGame game = new BokuGame();
            BokuGame.bokuGame.Initialize();
            BokuGame.bokuGame.LoadContent();
            BokuGame.bokuGame.BeginRun();

            // Create main.  We want to do this before calling Init on SceneManager since Init
            // will load the content for the scenes.
            main = new Main();

            SceneManager.Init();

            KoiX.Geometry.Geometry.LoadContent();
            KoiX.Geometry.RoundedRect.LoadContent();
            KoiX.Geometry.Line.LoadContent();
            KoiX.Geometry.Line2D.LoadContent();
            KoiX.Geometry.Disc.LoadContent();
            KoiX.Geometry.PieSlice.LoadContent();


            font = ContentManager.Load<SpriteFont>(@"Content\Fonts\SegoeUI20");

            // Hook the idle event to constantly redraw our animation.
            Application.Idle += delegate { Invalidate(); };

            // Be sure to kill off worker threads when leaving.
            //Application.ApplicationExit += delegate { TerrainMap.KillWorkerThread(); };
            Application.ApplicationExit += delegate
                                            {
                                                if (BokuGame.bokuGame != null)
                                                {
                                                    BokuGame.bokuGame.EndRun();
                                                }
                                                //Boku.Common.MouseInput.StopMouseWorkerThread();
                                            };

            // TODO (****) ??? Add drag and drop support.
            // this.AllowDrop = true;

            // this.DragEnter += new DragEventHandler(XNAControl_DragEnter);
            // this.DragDrop += new DragEventHandler(XNAControl_DragDrop);

            bokuGameInitialized = true;

        }   // end of Initialize()

        bool bokuGameInitialized = false;

        public void XNAControl_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;    
        }

        public void XNAControl_DragDrop(object sender, DragEventArgs e)
        {
            // Where in client coords object was dropped.
            // DragEventArgs coords are screen coords.
            System.Drawing.Point clientPoint = PointToClient(new System.Drawing.Point(e.X, e.Y));

            // We've had a drop event.  Assume they are image file names and add them to the animation.
            DataObject d = e.Data as DataObject;

            /*
            // Read image(s) dropped on this app.
            if (d.ContainsFileDropList())
            {
                var files = d.GetFileDropList();
                foreach (string filename in files)
                {
                    Img img = new Img(filename);
                    if (img.Texture != null)
                    {
                        img.InitDst(new Vector2(clientPoint.X, clientPoint.Y));
                        img.InitSrc();
                        Main.Images.Add(img);
                    }
                }
            }
            */

            /*
            string[] filenames = (string[])d.GetData("FileName");

            foreach(string f in filenames)
            {
                Img img = new Img(f);
                if (img.Texture != null)
                {
                    img.InitDst(new Vector2(clientPoint.X, clientPoint.Y));
                    img.InitSrc();
                    Main.Images.Add(img);
                }
            }
            */
        }

        /// <summary>
        /// Draws the control.
        /// </summary>
        protected override void Draw()
        {
            //Update();
            //Render();

            // Wraps Update and Draw calls.
            if (bokuGameInitialized)
            {
                BokuGame.bokuGame.DoFrame();
            }

        }   // end of Draw()

        protected new void Update()
        {
            //Point clientSize = new Point(ClientSize.Width, ClientSize.Height);
            //KoiLibrary.Update(clientSize);

            //Point mouse = LowLevelMouseInput.Position;

            main.Update();

        }   // end of Update()

#if DEBUG
        static public string debugString = null;
#endif

        protected void Render()
        {
            main.Render();

            //
            // Draw frame rate string over the top of what's been rendered.
            //

#if DEBUG
            /*
            // Grab SpriteBatch ref.
            SpriteBatch batch = KoiLibrary.SpriteBatch;

            string str = Time.FrameRate.ToString("F2") + "fps " + (1000.0 / Time.FrameRate).ToString("F2") + "ms";
            Vector2 size = font.MeasureString(str);
            int margin = 4;
            Vector2 origin = new Vector2(ClientSize.Width - size.X - margin, ClientSize.Height - font.LineSpacing - margin);

            Color textColor = SceneManager.CurrentScene.Theme.DebugText;

            batch.Begin();

            TextHelper.DrawString(font, str, origin, textColor);

            if (debugString != null)
            {
                origin = new Vector2(10, 0);
                TextHelper.DrawString(font, debugString, origin, textColor);
            }

            batch.End();
            */
#endif

        }   // end of Render()

        /*
        // TODO (****) do we want to support drag and drop for Kodu???
        public DragDropEffects DoDragDrop(Object data, DragDropEffects allowedEffects)
        {
            return DragDropEffects.Copy;
        }
        */

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);

        }

    }   // end of class XNAControl
}   // end of namespace EmptyWin2
