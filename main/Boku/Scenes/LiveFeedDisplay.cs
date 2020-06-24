
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
using KoiX.Text;

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
    /// 
    /// TODO This needs a clearer definition of Active, Focus, and Expanded.  When
    /// is each valid?  How can they combine?  What do they mean?  Why does the
    /// NewScroller have a different Active state?
    /// </summary>
    public class LiveFeedDisplay : INeedsDeviceReset
    {
        #region Members

        private GetFont expandFlagFont = SharedX.GetGameFont13_5;
        private Color flagMoreLessColor = Color.Blue;
        private TextBlob flagMoreBlob = null;
        private TextBlob flagLessBlob = null;

        public AABB2D moreLessHitBox = new AABB2D();

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;

        private NewsFeeds msNewsFeed = null;
        private ItemScroller newsScroller = null;
        private Vector2 scrollBoxPos = new Vector2(24.0f, 116.0f);

        private CommandMap commandMap = new CommandMap("LiveFeedDisplay");

        private bool useBackgroundThumbnail = true;
        private bool expanded = false; 
       
      //  private Texture2D Header_bg  = null;

        private Texture2D cloudTR = null;
        private Texture2D cloudBR = null;
        private Texture2D cloudTL = null;
        private Texture2D cloudBL = null;
        private Texture2D cloudLC = null;
        private Texture2D cloudRC = null;
        private Texture2D cloudTC = null;
        private Texture2D cloudBC = null;
        private Texture2D cloudCenter = null;

        private bool useOverscanForHitTesting = false;

        private TextBlob smallblob = null;
        private AABB2D hitBox = new AABB2D();           // Mouse hit region for <A> Continue.

        // Text color for button label.
        private Color labelAColor = new Color(191, 191, 191);
        // Color targetted by the twitch.  Used for comparisons so
        // we know whether or not to start a new twitch.
        private Color labelATargetColor = Color.Gray;

        // Color constants.
        private Color lightTextColor = new Color(191, 191, 191);
        private Color hoverTextColor = new Color(50, 255, 50);

        private bool gettingFeeds = false;


        // Bounds
        AABB2D aBox = new AABB2D();

        private Vector2 textPosition = new Vector2(5, 2);
        private Vector2 iconPosition = new Vector2(5, 2);

        #endregion

        #region Accessors

        private GetFont TitleFont
        {
            get
            {
                if (true)//Always use small size font. //BokuGame.ScreenSize.Y < 700)
                {
                    return SharedX.GetGameFont13_5;
                }
                else
                {
                    //return Shared.GetGameFont20;
                }
            }
        }

        private GetFont DateFont
        {
            get
            {
                if (true)//Always use small size font. //if (BokuGame.ScreenSize.Y < 700)
                {
                    return SharedX.GetGameFont10;
                }
                //else if (BokuGame.ScreenSize.Y > 1000)
                {
                    //return Shared.GetGameFont15_75;
                }
                //else
                {
                    //return Shared.GetGameFont13_5;
                }
            }
        }

        private GetFont BodyFont
        {
            get {
                if (true)//Always use small size font. //if ( BokuGame.ScreenSize.Y < 700)
                {
                    return SharedX.GetGameFont10;
                }
                else
                {
                    //return Shared.GetGameFont13_5;
                }
            }
        }        

        private GetFont Font
        {
            get { return SharedX.GetGameFont20; }
        }

        public bool Active
        {
            get { return (state == States.Active); }
        }
                
        public bool UseBackgroundThumbnail
        {
            get { return useBackgroundThumbnail; }
        }

        public Vector2 ResetScrollBoxSize
        {
            get {
                Vector2 baseSize = BokuGame.ScreenSize;

                Vector2 defaultScreenSize = new Vector2(1280, 1024);
                Vector2 extraSize = Vector2.Zero;
                Vector2 scaledBy = new Vector2(baseSize.X / defaultScreenSize.X, baseSize.Y / defaultScreenSize.Y);
                float xCrushScale = 1.0f;

                if (scaledBy.X < 0.9f)
                {
                    xCrushScale = scaledBy.X;
                }

                if (expanded)
                {
                    extraSize.Y += 600.0f * scaledBy.Y;
                }
                extraSize.X = 80;//moves over the word "News"

                baseSize.Y = (cloudTL.Height + ((cloudLC.Height + extraSize.Y) * scaledBy.Y) + cloudBL.Height);
                baseSize.X = ((cloudTL.Width + (cloudTC.Width + extraSize.X) + cloudTR.Width) * xCrushScale);
                baseSize.X -= (scrollBoxPos.X * 2.0f);
                baseSize.Y -= (scrollBoxPos.Y * 1.0f);
                return baseSize;
            }
        }


        public Vector2 FeedSize
        {
            get
            {
                if (newsScroller == null)
                    return new Vector2(80.0f, 20.0f);
                else
                    return newsScroller.FixedSize; 
            }
            set {
                Vector2 defaultItemSize = ResetScrollBoxSize;
                if (value != defaultItemSize)
                    defaultItemSize = value;
                newsScroller.FixedSize = defaultItemSize;
                hitBox = new AABB2D(scrollBoxPos, scrollBoxPos + defaultItemSize);
            }
        }

        public bool Dirty
        {
            set
            {
                newsScroller.Dirty = true;
            }
        }

        #endregion

        #region Public

        // c'tor
        public LiveFeedDisplay()
        {
            this.msNewsFeed = new NewsFeeds();
            this.smallblob = new TextBlob(SharedX.GetGameFont18Bold, Strings.Localize("mainMenu.news"), 300);
            this.smallblob.Justification = TextHelper.Justification.Left;
            this.flagMoreBlob = new TextBlob(expandFlagFont, Strings.Localize("mainMenu.more"), 80);
            this.flagLessBlob = new TextBlob(expandFlagFont, Strings.Localize("mainMenu.less"), 80);


            this.newsScroller = new ItemScroller(scrollBoxPos, FeedSize, new Color(0.0f, 0.0f, 0.0f, 0.0f), null, null);
            this.hitBox = new AABB2D(new Vector2(0, 0), FeedSize);
           // this.Header_bg = KoiLibrary.LoadTexture2D(@"Textures\twitter_Icon");
            this.cloudTR = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudTR");
            this.cloudBR = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudBR");

            this.cloudTL = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudTL");
            this.cloudBL = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudBL");

            this.cloudTC = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudTC");
            this.cloudBC = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudBC");

            this.cloudLC = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudLC");
            this.cloudRC = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudRC");
            this.cloudCenter = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudCenter");

            newsScroller.Clear();
            FeedMs item = new FeedMs(FeedSize, "Getting News feed...", TitleFont, DateFont, BodyFont);
            newsScroller.AddItem(item);

            //Twitter.GetTweets();
            msNewsFeed.BeginFetchNews();
            
            gettingFeeds = true;

        }   // end of c'tor

        public void Update(Camera camera)
        {
            if (gettingFeeds)
            {
                msNewsFeed.Update();
                if (msNewsFeed.CurrentState == NewsFeeds.OpState.Failed)
                {
                    newsScroller.Clear();
                    FeedMs item = new FeedMs(FeedSize, "Failed to get news feed...", TitleFont, DateFont, BodyFont);
                    newsScroller.AddItem(item);
                    gettingFeeds = false;
                    newsScroller.Dirty = true;
                }
                else if (msNewsFeed.CurrentState == NewsFeeds.OpState.Retrieving)
                {
                    newsScroller.Clear();
                    FeedMs item = new FeedMs(FeedSize, "Updating news...", TitleFont, DateFont, BodyFont);
                    newsScroller.AddItem(item);
                    //gettingFeeds = false;
                    newsScroller.Dirty = true;
                }
                else if (msNewsFeed.CurrentState == NewsFeeds.OpState.Retrieved)
                {
                    try
                    {
                        /*
                        // Done getting Tweets
                        newsScroller.Clear();
                        List<FeedMs> feedList = msNewsFeed.GetFeedList(
                            (int)(FeedSize.X - newsScroller.ScrollBoxSize.X), TitleFont, DateFont, BodyFont);
                        if (feedList != null)
                        {
                            foreach (FeedMs newsItem in feedList)
                            {
                                newsScroller.AddItem(newsItem);
                            }
                        }
                        newsScroller.ResizeItemWidths();
                        gettingFeeds = false;
                        newsScroller.Dirty = true;
                        */
                    }
                    catch
                    {
                        newsScroller.Clear();
                        FeedMs item = new FeedMs(FeedSize, "Error getting news feed...", TitleFont, DateFont, BodyFont);
                        newsScroller.AddItem(item);
                        gettingFeeds = false;
                        newsScroller.Dirty = true;
                    }
                    
                }
            }

            Vector2 pureMouseHit = new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y);
            if (moreLessHitBox.Contains(pureMouseHit) && LowLevelMouseInput.Left.WasReleased)
            {
                expanded = !expanded;
            }

            //check touch hit
            TouchContact touch = TouchInput.GetOldestTouch();
            if (TouchInput.WasLastReleased && moreLessHitBox.Contains(touch.position))
            {
                expanded = !expanded;
            }

            // Even if not active we need to refresh the rendertarget.
            newsScroller.RefreshRT();

            if (Active)
            {
                newsScroller.Update(camera);

                if (camera == null)
                {
                    camera = new PerspectiveUICamera();
                }
                
                if (KoiLibrary.LastTouchedDeviceIsKeyboardMouse)
                {
                    Vector2 hit = LowLevelMouseInput.GetAspectRatioAdjustedPosition(camera, useOverscanForHitTesting);
                    pureMouseHit = new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y);
                    if (!IsInScrollwindow(pureMouseHit))
                    {
                        // Don't Deactivate here just because the mouse cursor has left the bounds of the scroll window.
                        // We should only deactivate if ComboRight is pressed.
                        //Deactivate();

                        // But if we're out of bounds and the used clicks, then Deactivate()
                        if (LowLevelMouseInput.Left.WasPressed)
                        {
                            Deactivate();
                        }
                    }
                    else if (LowLevelMouseInput.DeltaPosition != Point.Zero)
                    {
                        newsScroller.Activate();
                    }

                    if (true) //newsScroller.IsFocused)
                    {
                        
                        if (Actions.ComboUp.WasPressedOrRepeat )
                        {
                            newsScroller.FocusPrev();
                        }
                        else if (Actions.ComboDown.WasPressedOrRepeat)
                        {
                            newsScroller.FocusNext();
                        }
                        else if (Actions.ComboRight.WasPressed )
                        {
                            Deactivate();
                        }
                        if (Actions.ZoomIn.WasPressedOrRepeat)
                        {
                            newsScroller.FocusPrevItem();
                        }
                        else if (Actions.ZoomOut.WasPressedOrRepeat)
                        {
                            newsScroller.FocusNextItem();
                        }
                    }

                    if (Actions.ComboLeft.WasPressed)
                    {
                        newsScroller.Activate();
                    }
                    
                    
                }
                else if (KoiLibrary.LastTouchedDeviceIsGamepad)
                {
                    HandleGamepadInput();
                }
            }   // end if active.

        }   // end of Update()

        public void UpdateFeed()
        {
            Vector2 defaultItemSize = FeedSize;
            defaultItemSize.X -= newsScroller.ScrollBoxSize.X;
            defaultItemSize.Y = 20.0f;
        }

        public bool IsInScrollwindow(Vector2 pos)
        {

            return hitBox.Contains(pos);// - scrollBoxPos);
        }

        private void HandleGamepadInput()
        {
            if (!KoiLibrary.LastTouchedDeviceIsGamepad) { return; }
            
            GamePadInput pad = GamePadInput.GetGamePad0();
            if ((Actions.Raise.WasPressedOrRepeat) || (Actions.Up.WasPressedOrRepeat))
            {
                if (newsScroller.Count >0)
                    newsScroller.FocusPrev();
            }
            else if ((Actions.Lower.WasPressedOrRepeat) || (Actions.Down.WasPressedOrRepeat))
            {
                if (newsScroller.Count > 0)
                    newsScroller.FocusNext();
            }
            else if (Actions.ComboRight.WasPressed)
            {
                Deactivate();
            }
            else if (Actions.Select.WasPressed)
            {

                if (newsScroller.Count > 0)
                    newsScroller.PressFocus();
            }
        }

        public void Render()
        {
            if ( true ) //Active)
            {
                RenderFeedBasePlate();
                newsScroller.Render();  
            }
        }   // end of LiveFeedDisplay Render()

        #endregion

        #region Internal

        private void RenderFeedBasePlate()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            // default width = 300 px
            // default height = 190 px
            // Adjustment Scale X
            // Adjustment Scale Y
            Vector2 defaultScreenSize = new Vector2(1280,1024);
            Vector2 extraSize = Vector2.Zero;
            Vector2 cOffset = Vector2.Zero; //center Offset of cloud background;
            float xCrushScale = 1.0f;
            float xCrushPixel = 0.0f;

            extraSize.X = 80;//moves over the word "News"

            ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
            Color darkTextColor = new Color(20, 20, 20);
            Color greyTextColor = new Color(127, 127, 127);
            Color greenTextColor = new Color(0, 255, 45);
            Vector2 screenSize = BokuGame.ScreenSize;

            Vector2 scaledBy = new Vector2(screenSize.X / defaultScreenSize.X, screenSize.Y / defaultScreenSize.Y);
            if (expanded)
            {
                extraSize.Y += 600.0f * scaledBy.Y;
            }
            if (scaledBy.X < 0.9f)
            {
                xCrushScale = scaledBy.X;
                xCrushPixel = cloudTL.Width-(cloudTL.Width * scaledBy.X);
            }
            Vector2 pos = new Vector2(2.0f, 80.0f);
            cOffset = pos;
            cOffset.X += cloudTL.Width;
            cOffset.Y += cloudTL.Height-25.0f; 

            Vector2 baseSize = FeedSize;
            Vector2 iconSize = new Vector2(42.0f, 42.0f);
            Vector4 baseColor = new Vector4(0.0f, 0.0f, 0.0f, 0.7f);
            Vector2 headerSize = baseSize;
            headerSize.X *= 0.8f;
            headerSize.Y *= 0.055f;
            Vector2 headerPos = pos;
            Vector2 baseOffset;
            baseOffset.Y = (float)cloudTL.Height + cloudLC.Height * scaledBy.Y; 

            headerPos.Y -= headerSize.Y;
            
            iconSize = new Vector2(headerSize.Y * 0.95f, headerSize.Y * 0.95f);

            Vector2 cornerSize = new Vector2(headerSize.Y * 0.25f, headerSize.Y * 0.25f);

            Vector2 titlePos = headerPos + textPosition;
            titlePos.X += iconSize.X;

            Vector2 iconPos = iconPosition;

            iconPos.Y += headerPos.Y;
            if (screenSize.X > 1200)
            {
                iconPos.Y += 5.0f;
            }
             
          //  ssquad.Render(baseColor, headerPos, headerSize);
          //  ssquad.Render(baseColor, pos, baseSize);
          //  ssquad.Render(baseColor, headerPos + new Vector2(headerSize.X, cornerSize.Y), new Vector2(cornerSize.X, headerSize.Y - cornerSize.Y));

            //Top left cloud
            ssquad.Render(
                cloudTL, cOffset - new Vector2(cloudTL.Width, cloudTL.Height / 1.25f), 
                new Vector2(cloudTL.Width * xCrushScale,cloudTL.Height/1.25f) , "TexturedRegularAlpha");
            //Bottom left cloud
            ssquad.Render(
                cloudBL, cOffset - new Vector2(cloudBL.Width, -((cloudLC.Height + extraSize.Y) * scaledBy.Y)), //baseOffset.Y + ((cloudLC.Height + extraSize.Y) * scaledBy.Y)),
                new Vector2(cloudBL.Width * xCrushScale, cloudBL.Height), "TexturedRegularAlpha");
            // left center Fill
            ssquad.Render(
                cloudLC, cOffset - new Vector2(cloudLC.Width, 0.0f),
                new Vector2(cloudLC.Width * xCrushScale, (cloudLC.Height + extraSize.Y) * scaledBy.Y), "TexturedRegularAlpha");

            //Top right cloud
            ssquad.Render(
                cloudTR, cOffset + new Vector2(((cloudTC.Width + extraSize.X) * scaledBy.X) - xCrushPixel, -(cloudTR.Height / 1.25f)),
                new Vector2(cloudTR.Width * xCrushScale, cloudTR.Height / 1.25f), "TexturedRegularAlpha");
            //Bottom right cloud
            ssquad.Render(
                cloudBR, cOffset + new Vector2(((cloudTC.Width + extraSize.X) * scaledBy.X) - xCrushPixel, (cloudLC.Height + extraSize.Y) * scaledBy.Y),
                new Vector2(cloudBR.Width * xCrushScale, cloudBR.Height), "TexturedRegularAlpha");
            // right center Fill
            ssquad.Render(
                cloudRC, cOffset + new Vector2(((cloudTC.Width + extraSize.X) * scaledBy.X) - xCrushPixel, 0.0f),
                new Vector2(cloudRC.Width * xCrushScale, (cloudRC.Height + extraSize.Y) * scaledBy.Y), "TexturedRegularAlpha");
            // Top center Fill
            ssquad.Render(
                cloudTC, cOffset - new Vector2(xCrushPixel, cloudTC.Height / 1.25f),
                new Vector2((cloudTC.Width + extraSize.X) * scaledBy.X, cloudTC.Height / 1.25f), "TexturedRegularAlpha");
            // Top bottom Fill
            ssquad.Render(
                cloudBC, cOffset + new Vector2(-xCrushPixel, (cloudLC.Height + extraSize.Y) * scaledBy.Y),
                new Vector2((cloudBC.Width + extraSize.X) * scaledBy.X, cloudBC.Height), "TexturedRegularAlpha");

            // Center Fill
            ssquad.Render(
                cloudCenter, cOffset + new Vector2(-xCrushPixel,0.0f),
                new Vector2((cloudTC.Width + extraSize.X) * scaledBy.X,
                    (cloudCenter.Height + extraSize.Y) * scaledBy.Y), "TexturedRegularAlpha");

            Vector2 moreLessBoxLT =
                new Vector2(
                    (cOffset.X - ((cloudBL.Width * 0.70f) * xCrushScale) - xCrushPixel),
                    (cOffset.Y + ((cloudLC.Height + extraSize.Y) * scaledBy.Y) + cloudBL.Height * 0.40f));
            Vector2 moreLessBoxBR = moreLessBoxLT;

            float moreLessPos = (cloudBL.Width * xCrushScale * 0.52f);
            if (newsScroller.Count > 1)
            {
                if (!expanded)
                {
                    moreLessPos -= (float)(flagMoreBlob.GetLineWidth(0) / 3);
                    flagMoreBlob.Justification = TextHelper.Justification.Center;
                    pos = new Vector2(moreLessPos, moreLessBoxLT.Y + 8);
                    // Clamp to pixel coords so text doesn't look like #$%.
                    pos.X = (int)pos.X;
                    pos.Y = (int)pos.Y;
                    flagMoreBlob.RenderText(null, pos, flagMoreLessColor, maxLines: 1);
                    moreLessBoxBR.X += flagMoreBlob.Width*2.5f; //2.5 makes click area larger.
                    moreLessBoxBR.Y += flagMoreBlob.TotalSpacing * 1.85f; //1.85 makes click area larger.
                }
                else
                {
                    moreLessPos -= (float)(flagLessBlob.GetLineWidth(0) / 3);
                    flagLessBlob.Justification = TextHelper.Justification.Center;
                    pos = new Vector2(moreLessPos, moreLessBoxLT.Y + 8);
                    // Clamp to pixel coords so text doesn't look like #$%.
                    pos.X = (int)pos.X;
                    pos.Y = (int)pos.Y;
                    flagLessBlob.RenderText(null, pos, flagMoreLessColor, maxLines: 1);
                    moreLessBoxBR.X += (int)flagMoreBlob.Width * 2.5f; //2.5 makes click area larger.
                    moreLessBoxBR.Y += (int)flagMoreBlob.TotalSpacing * 1.85f; //1.85 makes click area larger.
                }

                moreLessHitBox.Set(moreLessBoxLT, moreLessBoxBR);
            }
            else
            {
                moreLessHitBox.Set(new Vector2(0, 0), new Vector2(0, 0));
            }


            pos = new Vector2(cOffset.X - ((cloudTL.Width / 1.8f) * xCrushScale) - xCrushPixel, cOffset.Y - cloudTL.Height * 0.65f);
            // Clamp to pixel coords so text doesn't look like #$%.
            pos.X = (int)pos.X + 3;
            pos.Y = (int)pos.Y;

            smallblob.RenderText(null, pos, Active ? greenTextColor : darkTextColor, maxLines: 1);

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

        public void InitDeviceResources(GraphicsDevice device)
        {
           // if (Header_bg == null)
           // {
           //     Header_bg = KoiLibrary.LoadTexture2D(@"Textures\twitter_Icon");                
           // }
            
            if (cloudTR == null)
            {
                cloudTR = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudTR");                
            }
            
            if (cloudBR == null)
            {
                cloudBR = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudBR");                
            }

            if (cloudTL == null)
            {
                cloudTL = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudTL");
            }

            if (cloudBL == null)
            {
                cloudBL = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudBL");
            }

            if (cloudTC == null)
            {
                cloudTC = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudTC");
            }

            if (cloudBC == null)
            {
                cloudBC = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudBC");
            }

            if (cloudLC == null)
            {
                cloudLC = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudLC");
            }

            if (cloudRC == null)
            {
                cloudRC = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudRC");
            }

            
            if (cloudCenter == null)
            {
                cloudCenter = KoiLibrary.LoadTexture2D(@"Textures\MainMenu\cloudCenter");
            }

            newsScroller.InitDeviceResources();
            
        }   // end of LiveFeedDisplay InitDeviceResources()

        public void UnloadContent()
        {
          //  DeviceResetX.Release(ref Header_bg);
            DeviceResetX.Release(ref cloudTR);
            DeviceResetX.Release(ref cloudBR);
            DeviceResetX.Release(ref cloudTL);
            DeviceResetX.Release(ref cloudBL);
            DeviceResetX.Release(ref cloudTC);
            DeviceResetX.Release(ref cloudBC);
            DeviceResetX.Release(ref cloudLC);
            DeviceResetX.Release(ref cloudRC);
            DeviceResetX.Release(ref cloudCenter);
            
        }   // end of LiveFeedDisplay UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        public void Activate()
        {
           
            if (state != States.Active)
            {
                this.useBackgroundThumbnail = true;// useBackgroundThumbnail;
              //  this.useOverscanForHitTesting = useOverscanForHitTesting;
                if (newsScroller == null)
                {
                    newsScroller = new ItemScroller(scrollBoxPos, FeedSize, new Color(0.0f, 0.0f, 0.0f, 0.0f), null, null);
                    hitBox = new AABB2D(scrollBoxPos, FeedSize);
                }
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                 CommandStack.Push(commandMap);
                // FeedSize = ResetScrollBoxSize;
                newsScroller.Activate();
                state = States.Active;

                expanded = true;
            }
        }   // end of Activate

        public void Deactivate()
        {
            if (state != States.Inactive)
            {
                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Pop(commandMap);
                newsScroller.Deactivate();

                state = States.Inactive;

                expanded = false;
            }
        }

        #endregion

    }   // end of class LiveFeedDisplay

}   // end of namespace Boku
