// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


// Uncomment this to build the UI as it will look for release.
//#define FINAL_UI

#define USE_UNDO_STACK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
#if !NETFX_CORE
    using Microsoft.Xna.Framework.Net;
#endif


using Boku.Audio;
using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Sharing;
using Boku.SimWorld;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Common.Xml;
using Boku.Common.Gesture;

namespace Boku
{
    /// <summary>
    /// A simple slide show of screens.
    /// </summary>
    public class HelpScreens : GameObject, INeedsDeviceReset
    {
        public static HelpScreens Instance = null;

        // Individual entry.  Making this a class just in case 
        // we later want to add meta-data.
        public class Screen
        {
            public string name = null;
            public string mouseName = null;

            // c'tor
            public Screen()
            {
            }
        }

        protected class Shared : INeedsDeviceReset
        {
            public Camera camera = new PerspectiveUICamera();
            public int curScreen = 0;
            public Texture2D curTexture = null;
            public Texture2D prevTexture = null;
            public bool dirty = true;               // Need to get a new texture;

            public float pushOffset = 0.0f;         // Offset used when switching from one page to another.
                                                    // When == 0 no push is happening.

            public List<Screen> screenList = null;

            public AABB2D leftArrowBox = new AABB2D();      // Mouse hit boxes for left/right arrows.
            public AABB2D rightArrowBox = new AABB2D();
            public AABB2D backBox = new AABB2D();

            public Texture2D leftArrowTexture = null;
            public Texture2D rightArrowTexture = null;
            public Texture2D backTexture = null;

            // c'tor
            public Shared(HelpScreens parent)
            {
                // Init list.
                screenList = new List<Screen>();

                // Read in overlay information.
                XmlHelpScreensData helpScreensData = new XmlHelpScreensData();
                helpScreensData.ReadFromXml(@"HelpScreens.Xml");

                screenList = helpScreensData.screen;

            }   // end of Shared c'tor

            public void LoadContent(bool immediate)
            {
            }   // end of HelpScreens Shared LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                if (leftArrowTexture == null)
                {
                    leftArrowTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\Arrow_Left");
                }
                if (rightArrowTexture == null)
                {
                    rightArrowTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\Arrow_Right");
                }
                if (backTexture == null)
                {
                    backTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\Back");
                }
            }

            public void UnloadContent()
            {
                BokuGame.Release(ref leftArrowTexture);
                BokuGame.Release(ref rightArrowTexture);
                BokuGame.Release(ref backTexture);
            }   // end of HelpScreens Shared UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            private HelpScreens parent = null;
            private Shared shared = null;

            private GamePadInput.InputMode inputMode = GamePadInput.InputMode.GamePad;

            public UpdateObj(HelpScreens parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public override void Update()
            {
                // Did we switch modes?
                if (inputMode != GamePadInput.ActiveMode)
                {
                    inputMode = GamePadInput.ActiveMode;
                    shared.dirty = true;
                }

                if (AuthUI.IsModalActive)
                {
                    return;
                }

                // Input focus and not pushing?
                if (parent.Active && CommandStack.Peek() == parent.commandMap && shared.pushOffset == 0.0f)
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if (Actions.Cancel.WasPressed)
                    {
                        Actions.Cancel.ClearAllWasPressedState();

                        parent.Deactivate();
                        Foley.PlayShuffle();
                        shared.dirty = true;
                    }

                    bool moveLeft = false;
                    bool moveRight = false;

                    // left
                    if (Actions.ComboLeft.WasPressedOrRepeat)
                    {
                        moveLeft = true;
                    }

                    // right
                    if (Actions.ComboRight.WasPressedOrRepeat)
                    {
                        moveRight = true;
                    }

                    //touch?
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        TouchContact touch = TouchInput.GetOldestTouch();
                        Vector2 touchHit = Vector2.Zero;

                        SwipeGestureRecognizer swipeGesture = TouchGestureManager.Get().SwipeGesture;
                        if (swipeGesture.WasSwiped() &&
                            swipeGesture.SwipeDirection == Boku.Programming.Directions.East)
                        {
                            moveLeft = true;
                        }
                        else if (swipeGesture.WasSwiped() &&
                                 swipeGesture.SwipeDirection == Boku.Programming.Directions.West)
                        {
                            moveRight = true;
                        }
                        else if (touch!= null)
                        {
                            touchHit = touch.position;

                            if (shared.leftArrowBox.Touched(touch, touchHit))
                            {
                                moveLeft = true;
                            }

                            if (shared.rightArrowBox.Touched(touch, touchHit))
                            {
                                moveRight = true;
                            }

                            if (shared.backBox.Touched(touch, touchHit))
                            {
                                Actions.Cancel.ClearAllWasPressedState();

                                parent.Deactivate();
                                Foley.PlayShuffle();
                                shared.dirty = true;
                            }
                        }
                    }

                    // Mouse hit?
                    else if(GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    {
                        Vector2 mouseHit = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);
                        
                        if(shared.leftArrowBox.LeftPressed(mouseHit))
                        {
                            moveLeft = true;
                        }

                        if(shared.rightArrowBox.LeftPressed(mouseHit))
                        {
                            moveRight = true;
                        }

                        if (shared.backBox.LeftPressed(mouseHit))
                        {
                            Actions.Cancel.ClearAllWasPressedState();

                            parent.Deactivate();
                            Foley.PlayShuffle();
                            shared.dirty = true;
                        }
                    }

                    if(moveLeft)
                    {
                        --shared.curScreen;
                        if (shared.curScreen < 0)
                        {
                            parent.Deactivate();
                        }
                        Foley.PlayShuffle();
                        shared.dirty = true;

                        shared.pushOffset = -BokuGame.ScreenSize.X;
                    }

                    if(moveRight)
                    {
                        ++shared.curScreen;
                        if (shared.curScreen >= shared.screenList.Count)
                        {
                            parent.Deactivate();
                        }
                        Foley.PlayShuffle();
                        shared.dirty = true;

                        shared.pushOffset = BokuGame.ScreenSize.X;
                    }
                }

                if (shared.dirty && shared.curScreen >= 0 && shared.curScreen < shared.screenList.Count)
                {
                    shared.prevTexture = shared.curTexture;

                    // Get the correct overlay image to use depending on input mode.
                    string name = GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad ? shared.screenList[shared.curScreen].name : shared.screenList[shared.curScreen].mouseName;
                    shared.curTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\HelpScreens\" + name);
                    shared.dirty = false;

                    // Create a twitch to do the push.
                    TwitchManager.Set<float> set = delegate(float val, Object param) { shared.pushOffset = val; };
                    TwitchManager.CreateTwitch<float>(shared.pushOffset, 0.0f, set, 0.3f, TwitchCurve.Shape.EaseOut);
                }

            }   // end of Update()

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

        }   // end of class HelpScreens UpdateObj  

        protected class RenderObj : RenderObject
        {
            private Shared shared;

            public RenderObj(Shared shared)
            {
                this.shared = shared;
            }

            public override void Render(Camera camera)
            {
                // Clear the screen.
                InGame.Clear(Color.Black);

                InGame.SetViewportToScreen();

                // Display current help screen.
                // Basically we're goin to shrink or stretch it to ensure that it is fully displayed without
                // having its aspect ratio changed.
                Vector2 imgSize = new Vector2(shared.curTexture.Width, shared.curTexture.Height);
                Vector2 ratios = imgSize / BokuGame.ScreenSize;
                float scale = 1.0f / Math.Max(ratios.X, ratios.Y);
                Vector2 size = imgSize * scale;
                Vector2 pos = (BokuGame.ScreenSize - imgSize * scale) / 2.0f;

                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                pos.X += shared.pushOffset;
                quad.Render(shared.curTexture, pos, imgSize * scale, "TexturedNoAlpha");

                // If pushing, also render the previous help screen.
                if (shared.prevTexture != null && shared.pushOffset != 0)
                {
                    float offset = -Math.Sign(shared.pushOffset) * BokuGame.ScreenSize.X;
                    pos.X += offset;
                    quad.Render(shared.prevTexture, pos, imgSize, "TexturedNoAlpha");
                }

                // Label with page number in lower right hand corner.
                if (shared.curScreen >= 0 && shared.curScreen < shared.screenList.Count)
                {
                    // Start with far right corner and subtract off what we don't need.
                    pos = BokuGame.ScreenSize;

                    string str = (shared.curScreen + 1).ToString() + @"/" + shared.screenList.Count.ToString();

                    // Add in arrow buttons for mouse navigation.
                    size = new Vector2(shared.rightArrowTexture.Width, shared.rightArrowTexture.Height);
                    size *= 0.5f;

                    // And add a bit of a margin.
                    pos -= new Vector2(30, 10 + UI2D.Shared.GameFont18Bold.LineSpacing);

                    // Right arrow.
                    pos.X -= size.X;
                    quad.Render(shared.rightArrowTexture, pos, size, "TexturedRegularAlpha");
                    shared.rightArrowBox.Set(pos, pos + size);

                    // Adjust for length of string.
                    pos.X -= UI2D.Shared.GameFont18Bold.MeasureString(str).X;

                    SpriteBatch batch = UI2D.Shared.SpriteBatch;
//                    Color blueText = new Color(12, 150, 209);
                    Color blueText = new Color(241, 221, 83);
                    Color shadow = new Color(4, 50, 70);
//                    Color highlight = new Color(200, 230, 240);
                    Color highlight = new Color(255, 255, 240);
                    batch.Begin();
                    // shadow
                    TextHelper.DrawString(UI2D.Shared.GetGameFont18Bold, str, pos + new Vector2(1, 1), shadow);
                    // other dirs.
                    TextHelper.DrawString(UI2D.Shared.GetGameFont18Bold, str, pos + new Vector2(1, -1), shadow);
                    TextHelper.DrawString(UI2D.Shared.GetGameFont18Bold, str, pos + new Vector2(-1, 1), shadow);
                    // highlight
                    TextHelper.DrawString(UI2D.Shared.GetGameFont18Bold, str, pos + new Vector2(-1, -1), highlight);
                    // Front face
                    TextHelper.DrawString(UI2D.Shared.GetGameFont18Bold, str, pos, blueText);
                    batch.End();

                    // Left arrow.
                    pos.X -= size.X;
                    quad.Render(shared.leftArrowTexture, pos, size, "TexturedRegularAlpha");
                    shared.leftArrowBox.Set(pos, pos + size);

                    //Back button
                    //some padding on left of screen
                    pos.X = 30;
                    quad.Render(shared.backTexture, pos, size, "TexturedRegularAlpha");
                    shared.backBox.Set(pos, pos + size);
                }
            }

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

        }   // end of class HelpScreens RenderObj     


        // List objects.
        protected Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        private enum States
        {
            Inactive,
            Active,
        }

        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private CommandMap commandMap = new CommandMap("App.HelpScreens");  // Placeholder for stack.

        #region Accessors
        public bool Active
        {
            get { return (state == States.Active); }
        }

        #endregion

        // c'tor
        public HelpScreens()
        {
            HelpScreens.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(shared);

        }   // end of HelpScreens c'tor

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();
                }
                else
                {
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                }

                state = pendingState;
            }

            return result;
        }   // end of HelpScreens Refresh()

        override public void Activate()
        {
            if (state != States.Active)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                // Start at the beginning?
                shared.curScreen = 0;
                shared.curTexture = null;
                shared.dirty = true;

                Foley.PlayMenuLoop();
            }
        }   // end of HelpScreens Activate()

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);

                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;

                GamePadInput.ClearAllWasPressedState();

                Foley.StopMenuLoop();

                // Exiting, restart the main menu.
                BokuGame.bokuGame.mainMenu.Activate();

            }
        }   // end of HelpScreens Deactivate()

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(shared, immediate);
        }   // end of HelpScreens LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared);
        }   // end of HelpScreens UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class HelpScreens


    //
    //
    // Xml file reading.
    //
    //

    public class XmlHelpScreensData
    {
        [XmlElement(Type = typeof(HelpScreens.Screen))]
        public List<HelpScreens.Screen> screen = null;

        public XmlHelpScreensData()
        {
            screen = new List<HelpScreens.Screen>();
        }

        /// <summary>
        /// Returns true on success, false if failed.
        /// </summary>
        public bool ReadFromXml(string filename)
        {
            bool success = true;

            // Read the Xml file into local data.
            XmlHelpScreensData data = Load(filename);
            if (data == null)
            {
                success = false;
            }
            else
            {
                this.screen = data.screen;
            }

            return success;
        }   // end of XmlHelpScreensData ReadFromXml()


        private static XmlHelpScreensData Load(string filename)
        {
            // Fix up the filename with the full path.
            filename = BokuGame.Settings.MediaPath + @"Xml\" + filename;

            XmlHelpScreensData data = null;
            try
            {
                Stream stream = Storage4.OpenRead(filename, StorageSource.All);

                XmlSerializer serializer = new XmlSerializer(typeof(XmlHelpScreensData));
                data = (XmlHelpScreensData)serializer.Deserialize(stream);
                Storage4.Close(stream);
            }
            catch (Exception)
            {
                data = null;
            }

            return data;
        }   // end of XmlHelpScreensData Load()


    }   // end of class XmlOverlayData

}   // end of namespace Boku


