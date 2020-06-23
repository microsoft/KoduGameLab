
//#define DISPLAY_IMAGE_HACK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Microsoft.Xna.Framework.Media;

using Boku.Base;
using Boku.Input;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.UI2D;
using Boku.Web;

using BokuShared;

namespace Boku
{
    public class TitleScreenMode : GameObject, INeedsDeviceReset
    {
        protected class Shared
        {
            public Camera camera = null;

#if !NETFX_CORE
            public Video video = null;
            public VideoPlayer player = null;
#endif
        }

        protected class UpdateObj : UpdateObject
        {
            private TitleScreenMode parent = null;
            private Shared shared = null;
            public List<UpdateObject> updateList = null; // Children's update list.
            private CommandMap commandMap;
            public bool done = false;

            public UpdateObj(TitleScreenMode parent, ref Shared shared)
            {
                this.parent = parent;
                this.shared = shared;

                this.commandMap = new CommandMap(@"TitleScreen");

                updateList = new List<UpdateObject>();
            }   // end of UpdateObj c'tor

            public override void Update()
            {
                foreach (UpdateObject obj in updateList)
                {
                    obj.Update();
                }

                if (done && !parent.logonDialog.Active)
                {
                    // Done loading, should we show the intro video?
                    if (XmlOptionsData.ShowIntroVideo)
                    {
                        try
                        {
#if NETFX_CORE
                            // Switch to MainMenu.
                            parent.DismissAndShowMain(null, null);
#else
                            if (shared.video == null)
                            {
                                // Start video.
                                shared.video = BokuGame.Load<Video>(BokuGame.Settings.MediaPath + @"Video\Intro");
                                shared.player = new VideoPlayer();
                                shared.player.IsLooped = false;
                                shared.player.Play(shared.video);
                            }

                            // Check if we're done with the video or the user hit escape to skip.
                            if (shared.player.State != MediaState.Playing || 
                                Actions.Cancel.WasPressed ||
                                TouchInput.WasLastReleased)
                            {
                                Actions.Cancel.ClearAllWasPressedState();
                                shared.player.Stop();

                                shared.player.Dispose();
                                shared.video = null;

                                XmlOptionsData.ShowIntroVideo = false;

                                // Switch to MainMenu.
                                parent.DismissAndShowMain(null, null);
                            }
#endif
                        }
                        catch (Exception e)
                        {
                            if (e != null)
                            {
                            }

                            // Something failed with the video so just pretend
                            // we never intended to go there anyway.
                            XmlOptionsData.ShowIntroVideo = false;

                            // Switch to MainMenu.
                            parent.DismissAndShowMain(null, null);
                        }
                    }
                    else
                    {
                        // Switch to MainMenu.
                        parent.DismissAndShowMain(null, null);
                    }
                }

                if (parent.progress != null && parent.progressMessage != null)
                {
                    lock (parent.progressMessage)
                    {
                        parent.progress.Message = parent.progressMessage;
                    }
                }
            }   // end of UpdateObj Update()

            public override void Activate()
            {
                CommandStack.Push(commandMap);
            }
            public override void Deactivate()
            {
                CommandStack.Pop(commandMap);
            }
        }   // end of class UpdateObj
        
        protected class RenderObj : RenderObject
        {
            private TitleScreenMode parent = null;
            private Shared shared = null;
            public List<RenderObject> renderList = null; // Children's render list.

            public RenderObj(TitleScreenMode parent, ref Shared shared)
            {
                this.parent = parent;
                this.shared = shared;

                renderList = new List<RenderObject>();
            }   // end of RenderObj c'tor

            public override void Render(Camera camera)
            {
                // Render the parent's list of objects using our local camera.
#if !NETFX_CORE
                if (shared.player == null)
#endif
                {
                foreach (RenderObject obj in renderList)
                    {
                        obj.Render(shared.camera);
                    }
                }

#if !NETFX_CORE
                if (shared.player != null && !shared.player.IsDisposed && shared.player.State == MediaState.Playing)
                {
                    GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                    device.Clear(Color.Black);
                    ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                    Texture2D vid = shared.player.GetTexture();
                    int w = device.Viewport.Width;
                    int h = device.Viewport.Height;
                    float scale = (float)w / vid.Width;

                    Vector2 size = new Vector2(w, vid.Height * scale);
                    Vector2 pos = new Vector2(0, (h - size.Y) / 2.0f);

                    ssquad.Render(vid, pos, size, "TexturedNoAlpha");
                }
#endif

            }   // end of RenderObj Render()
            public override void Activate()
            {

            }
            public override void Deactivate()
            {

            }
        }   // end of class RenderObj



        // Children.
        private TitleScreen titleScreen = null;

        // List objects.
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;
        protected Shared shared = null;

        private bool state = false;
        private bool pendingState = false;

        private ProgressOperation progress;
        private string progressMessage = null;

        private TextDialog logonDialog = new TextDialog(Color.DarkSeaGreen, TextDialog.TextDialogButtons.Accept);

        // c'tor
        public TitleScreenMode()
        {
            // Create the RenderObject and UpdateObject parts of this mode.
            shared = new Shared();
            updateObj = new UpdateObj(this, ref shared);
            renderObj = new RenderObj(this, ref shared);

            logonDialog.OnButtonPressed += OnTextDialogButton;
            logonDialog.UserText = Auth.CreatorName;
            logonDialog.Prompt = Strings.Localize("textDialog.logonPrompt");
            logonDialog.SetButtonText(TextDialog.TextDialogButtons.Accept, Strings.Localize("textDialog.continue"));

            Init();

        }   // end of TitleScreenMode c'tor

        private void Init()
        {
            shared.camera = new UiCamera();
            titleScreen = new TitleScreen();
        }   // end of TitleScreenMode Init()

        public void DoneLoadingContent()
        {
            if (progress != null)
            {
                progress.Complete();
                progress = null;
            }

            updateObj.done = true;

            if (Program2.bShowVersionWarning)
            {
                Program2.bShowVersionWarning = false;
                GamePadInput.CreateNewerVersionDialog();
            }

            if (BokuGame.Logon)
            {
                logonDialog.Activate();
            }

            // Did we save the previous user?  If so, restore.
            if (XmlOptionsData.KeepSignedInOnExit)
            {
                Auth.SetCreator(XmlOptionsData.CreatorName, XmlOptionsData.CreatorIdHash);
            }
            else
            {
                // Else, show the SignIn dialog.
                AuthUI.ShowSignInDialog();
            }

        }   // end of DoneLoadingContent()


        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();
                }
                else
                {
                    BokuGame.gameListManager.RemoveObject(this);
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);

                    titleScreen.Deactivate();
                    logonDialog.Deactivate();
                    
                    result = true;
                }

                state = pendingState;
            }

            // call refresh on child list.
            titleScreen.Refresh(updateObj.updateList, renderObj.renderList);
            logonDialog.Refresh(updateObj.updateList, renderObj.renderList);

            return result;
        }   // end of TitleScreenMode Refresh()

        override public void Activate()
        {
            if (!state)
            {
                if (progress == null)
                {
                    progress = ProgressScreen.RegisterOperation();
                    progress.AlwaysShow = true;
                }
                pendingState = true;
            }
        }   // end of TitleScreenMode Activate()

        /// <summary>
        /// Note, exiting TitleScreenMode always goes to SimWorld mode.
        /// </summary>
        override public void Deactivate()
        {
            if (state)
            {
                // Remove this object.
                pendingState = false;
                BokuGame.objectListDirty = true;
            }
        }   // end of TitleScreenMode Deactivate()

        public void OnLoadingItem(INeedsDeviceReset item)
        {
            /*
            if (item.GetType().Name == "UIGrid2DBrushElement")
            {
                titleScreen.WaitMode = true;
            }
            */
            /*
            lock (progressMessage)
            {
                progressMessage = "Loading " + item.GetType().Name + "...";
            }
            */
        }

        public void DismissAndShowMain(Object sender, EventArgs args)
        {
            //before we enter the main menu for the first time, do a check to see if:
            // 1) touch input is available, and 
            // 2) we have less than 5 max touch points
            //if these conditions are both true, then we know the touch hardware isn't windows 8 compliant. this means 
            //we may see hardware like the infrared monitors that can't handle rotate gestures reliably.  Display a 
            //warning to the user that touch gestures may not perform in an ideal manner.
#if false
            if (TouchInput.TouchAvailable && TouchInput.MaxTouchCount < 5)
            {
#if !NETFX_CORE
                System.Windows.Forms.MessageBox.Show(
                    Strings.Localize("warning.noncomplianttouch"),
                    Strings.Localize("warning.noncomplianttouch_title"),
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
#endif
            }
#endif

            BokuGame.bokuGame.mainMenu.Activate();

            Deactivate();
        }   // end of TitleScreenMode DismissAndShowMain()

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(titleScreen, immediate);
            BokuGame.Load(logonDialog, immediate);
        }   // end of TitleScreenMode LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            titleScreen.Activate();
        }

        public void UnloadContent()
        {
            BokuGame.Unload(titleScreen);
            BokuGame.Unload(logonDialog);
        }   // end of TitleScreenMode UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(titleScreen, device);
            BokuGame.DeviceReset(logonDialog, device);
        }

        private void OnTextDialogButton(TextDialog dialog)
        {
            Debug.Assert(false, "Need to remove this login path.  Not sure if anyone ever used it anyway.");
#if NETFX_CORE
            Storage4.Username = dialog.UserText;
#else
            //GamerServices.CreatorName = dialog.UserText;
#endif
            XmlOptionsData.Username = dialog.UserText;
        }

    }   // end of class TitleScreenMode

}   // end of namespace Boku
