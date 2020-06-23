
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
using Microsoft.Xna.Framework.GamerServices;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Programming;
using Boku.SimWorld;
using Boku.Web;
using Boku.Fx;

using Boku.Audio;
using BokuShared;

namespace Boku
{
    /// <summary>
    /// Modal text display.  Used for the 'say' verb when 'fullscreen' is checked.
    /// </summary>
    public class LiveFeedDisplay : INeedsDeviceReset
    {
        #region Members

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;

        private ItemScroller newsScroller = null;

        private CommandMap commandMap = new CommandMap("LiveFeedDisplay");

        private bool useBackgroundThumbnail = true;
       
        private Texture2D Header_bg  = null;
       // private Texture2D Feed_bg    = null;
        private Texture2D cornerTR = null;
        private Texture2D cornerBR = null;

        private bool useOverscanForHitTesting = false;

        private TextBlob blob = null;
        //private Vector2 displayPosition;
        private AABB2D hitBox = new AABB2D();           // Mouse hit region for <A> Continue.

        // Text color for button label.
        private Color labelAColor = new Color(191, 191, 191);
        // Color targetted by the twitch.  Used for comparisons so
        // we know whether or not to start a new twitch.
        private Color labelATargetColor = Color.Gray;

        // Color constants.
        private Color lightTextColor = new Color(191, 191, 191);
        private Color hoverTextColor = new Color(50, 255, 50);


        // Bounds
        AABB2D aBox = new AABB2D();

        private Vector2 textPosition = new Vector2(10, 2);
        private Vector2 iconPosition = new Vector2(5, 2);

        private int maxLines = 7;           // Max lines we can display.

        #endregion

        #region Accessors

        private UI2D.Shared.GetFont Font
        {
            get { return UI2D.Shared.GetGameFont20; }
        }

        public bool Active
        {
            get { return (state == States.Active); }
        }

        public bool Overflow
        {
            get { return blob.NumLines > maxLines; }
        }

        public bool UseBackgroundThumbnail
        {
            get { return useBackgroundThumbnail; }
        }

        public Vector2 GetScrollBoxSize
        {
            get {
                Vector2 baseSize = new Vector2(
                    BokuGame.Graphics.GraphicsDevice.Viewport.Width,
                    BokuGame.Graphics.GraphicsDevice.Viewport.Height);
                baseSize.X /= 5;
                baseSize.Y *= 0.80f;
                return baseSize;
            }
        }

        #endregion

        #region Public

        // c'tor
        public LiveFeedDisplay()
        {
            blob = new TextBlob(UI2D.Shared.GetGameFont24, "NEWSFEED", 150);

            newsScroller = new ItemScroller(GetScrollBoxSize, new Color(0.0f, 0.0f, 0.0f, 0.0f), null, null);
        }   // end of c'tor

        public void Update(Camera camera)
        {
            if (Active)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                if (InGame.inGame.State == InGame.States.Active && InGame.inGame.CurrentUpdateMode == InGame.UpdateMode.RunSim)
                {
                    // We need to be able to slip out to the mini-hub here since
                    // continuous, repeated calls to LiveFeedDisplay can lock the 
                    // user out of control.
                    if (Actions.MiniHub.WasPressed)
                    {
                        Actions.MiniHub.ClearAllWasPressedState();

                        Deactivate();
                        InGame.inGame.SwitchToMiniHub();
                    }
/*
                    // We need to be able to slip out to the tool menu here since
                    // continuous, repeated calls to LiveFeedDisplay can lock the 
                    // user out of control.
                    if (Actions.ToolMenu.WasPressed)
                    {
                        Actions.ToolMenu.ClearAllWasPressedState();

                        Deactivate();
                        InGame.inGame.CurrentUpdateMode = InGame.UpdateMode.ToolMenu;
                    }
*/
                }

                if (Actions.Select.WasPressed)
                {
                    Actions.Select.ClearAllWasPressedState();

                    Deactivate();
                }

                // If we're rendering this into a 1280x720 rt we need a matching camera to calc mouse hits.
                if (useBackgroundThumbnail)
                {
                    camera = new PerspectiveUICamera();
                    camera.Resolution = new Point(1280, 720);
                }

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    for (int i = 0; i < TouchInput.TouchCount; i++)
                    {
                        TouchContact touch = TouchInput.GetTouchContactByIndex(i);

                        // Touch input on grid.
                        // Hit the in-focus tile, then open popup.
                        // Hit another tile, then bring that one to focus.  Note because of overlap of
                        // the tiles we should do this center-out.

                        Vector2 touchHit = TouchInput.GetAspectRatioAdjustedPosition(
                            touch.position,
                            camera,
                            useOverscanForHitTesting
                        );
                        HandleTouchInput(touch,touchHit);
                    }
                }
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                {
                    Vector2 hit = MouseInput.GetAspectRatioAdjustedPosition(camera, useOverscanForHitTesting);
                    HandleMouseInput(hit);
                }
            }   // end if active.

        }   // end of Update()

        public void UpdateFeed()
        {
            Vector2 defaultItemSize = GetScrollBoxSize;
            defaultItemSize.Y = 120.0f;

            // get new content
            if (true) // GetNewContent...
            {
                newsScroller.Clear();
                // replace existing content
                ScrollContainer itemA = new ScrollContainer(defaultItemSize);
                ScrollContainer itemB = new ScrollContainer(defaultItemSize);
                ScrollContainer itemC = new ScrollContainer(defaultItemSize);
                newsScroller.AddItem(itemA);
                newsScroller.AddItem(itemB);
                newsScroller.AddItem(itemC);
            }
        }

        private bool FillRandomBlobData(out ScrollContainer scItem)
        {
           scItem = null;

           return false;

        }




        private void HandleTouchInput(TouchContact touch, Vector2 hit)
        {
            if (hitBox.Touched(touch, hit))
            {
                Deactivate();
            }

            // Check for hover and adjust text color to match.
            Color newColor;

            newColor = hitBox.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelATargetColor)
            {
                labelATargetColor = newColor;
                Vector3 curColor = new Vector3(labelAColor.R / 255.0f, labelAColor.G / 255.0f, labelAColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelAColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelAColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelAColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }

        }   // end of HandleTouchInput()

        private void HandleMouseInput(Vector2 hit)
        {
            if (hitBox.LeftPressed(hit))
            {
                Deactivate();
            }

            // Check for hover and adjust text color to match.
            Color newColor;

            newColor = hitBox.Contains(hit) ? hoverTextColor : lightTextColor;
            if (newColor != labelATargetColor)
            {
                labelATargetColor = newColor;
                Vector3 curColor = new Vector3(labelAColor.R / 255.0f, labelAColor.G / 255.0f, labelAColor.B / 255.0f);
                Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                {
                    labelAColor.R = (byte)(value.X * 255.0f + 0.5f);
                    labelAColor.G = (byte)(value.Y * 255.0f + 0.5f);
                    labelAColor.B = (byte)(value.Z * 255.0f + 0.5f);
                };
                TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
            }

        }   // end of HandleMouseInput()

        public void Render()
        {
            if (Active)
            {
                RenderFeedBasePlate();
                
            }
        }   // end of LiveFeedDisplay Render()

        #endregion

        #region Internal

        private void RenderFeedBasePlate()
        {
            GraphicsDevice device = BokuGame.Graphics.GraphicsDevice;

            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            Color darkTextColor = new Color(20, 20, 20);
            Color greyTextColor = new Color(127, 127, 127);
            Color greenTextColor = new Color(8, 123, 110);
            Vector2 screenSize = new Vector2(
                BokuGame.Graphics.GraphicsDevice.Viewport.Width,
                BokuGame.Graphics.GraphicsDevice.Viewport.Height);
            Vector2 pos = new Vector2(0.0f, 40.0f);
            Vector2 baseSize = GetScrollBoxSize;
            Vector2 iconSize = new Vector2(42.0f, 42.0f);
            Vector4 baseColor = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);
           // baseSize.X /= 5;
           // baseSize.Y *= 0.80f;
            Vector2 headerSize = baseSize;
            headerSize.X *= 0.8f;
            headerSize.Y *= 0.055f;
            Vector2 headerPos = pos;
            headerPos.Y -= headerSize.Y;

            Vector2 cornerSize = new Vector2(headerSize.Y * 0.25f, headerSize.Y * 0.25f);

            Vector2 titlePos = headerPos + textPosition;
            titlePos.X += iconSize.X;

            Vector2 iconPos = iconPosition;
            iconPos.Y += headerPos.Y;

            ssquad.Render(baseColor, headerPos, headerSize);
            ssquad.Render(baseColor, pos, baseSize);
            ssquad.Render(baseColor, headerPos + new Vector2(headerSize.X, cornerSize.Y), new Vector2(cornerSize.X, headerSize.Y - cornerSize.Y));

            ssquad.Render(cornerTR, baseColor,headerPos + new Vector2(headerSize.X, 0.0f), cornerSize, "TexturedRegularAlpha");
            ssquad.Render(cornerTR, baseColor, pos + new Vector2(baseSize.X, 0.0f), cornerSize, "TexturedRegularAlpha");
            ssquad.Render(cornerBR, baseColor, pos + baseSize - new Vector2(0.0f, cornerSize.Y), cornerSize, "TexturedRegularAlpha");
            ssquad.Render(baseColor, pos + new Vector2(baseSize.X, cornerSize.Y), new Vector2(cornerSize.X, baseSize.Y - (cornerSize.Y * 2)));

            blob.RenderWithButtons(titlePos, greyTextColor, false, UIGridElement.Justification.Center);
            ssquad.Render(Header_bg, iconPos, iconSize, "TexturedRegularAlpha");

        }

        public void OnSelect(UIGrid grid)
        {
            // We should never actually get here.  The LiveFeedDisplay Update
            // should consume all 'A' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnSelect()

        public void OnCancel(UIGrid grid)
        {
            // We should never actually get here.  The LiveFeedDisplay Update
            // should consume all 'B' presses before the grids get them...

            Debug.Assert(false);

        }   // end of OnCancel()

        public void LoadContent(bool immediate)
        {
        }   // end of LiveFeedDisplay LoadContent()

        public void InitDeviceResources(GraphicsDeviceManager graphics)
        {
            if (Header_bg == null)
            {
                Header_bg = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\twitter_Icon");                
            }
            
            if (cornerTR == null)
            {
                cornerTR = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\cornerTR");                
            }
            
            if (cornerBR == null)
            {
                cornerBR = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\cornerBR");                
            }
        }   // end of LiveFeedDisplay InitDeviceResources()

        public void UnloadContent()
        {
            BokuGame.Release(ref Header_bg);
            BokuGame.Release(ref cornerTR);
            BokuGame.Release(ref cornerBR);
            
        }   // end of LiveFeedDisplay UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDeviceManager graphics)
        {
        }

        public void Activate()
        {
           
            if (state != States.Active)
            {
                this.useBackgroundThumbnail = true;// useBackgroundThumbnail;
              //  this.useOverscanForHitTesting = useOverscanForHitTesting;

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                state = States.Active;

                Header_bg = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\twitter_Icon");
                cornerTR = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\cornerTR");
                cornerBR = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\cornerBR");

            }
        }   // end of Activate

        public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);

                state = States.Inactive;
            }
        }

        #endregion

    }   // end of class LiveFeedDisplay

}   // end of namespace Boku
