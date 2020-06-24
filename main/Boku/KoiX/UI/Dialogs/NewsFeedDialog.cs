
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;

using Boku;

namespace KoiX.UI.Dialogs
{
    /// <summary>
    /// Dialog for display of NewsFeed on MainMenuScene.
    /// 
    /// TODO (scoy) Twitch some values?
    ///     -- scrolling when using mouse wheel.
    ///     -- alpha of right stick icon when going in/out of gamepad mode.
    /// </summary>
    public class NewsFeedDialog : BaseDialogNonModal
    {
        #region Members

        static float margin = 8;        // Margin around text so it's not right on the edge of the tile.
                                        // Also used as the gap between feed items.
        static float knobRadius = 32;


        NewsFeeds newsFeed;
        bool gettingFeed;               // Are we actively trying to get the feed?
        List<FeedItem> feedItems;

        Label title;
        SystemFont titleFont;

        int textWidth;                              // In pixel space, not camera.
        RectangleF textRect = new RectangleF();     // Region where text is displayed.  This is in cameraPixel coords, not dialog coords.
        bool draggingText;                          // Scrolling text via direct dragging?

        float slider = 0;   // 0 = top, 1 = bottom

        FeedItem clickedFeedItem = null;

        SpriteCamera camera;        // Just a local ref we can hang on to.
        float scaleFactor = 1.0f;   // Based on the above camera's zoom.
        SpriteCamera pixelCamera;   // Camera in pixel space.

        RenderTarget2D rt;

        #endregion

        #region Accessors
        #endregion

        #region Public

        public NewsFeedDialog()
            : base()
        {
#if DEBUG
            _name = "NewsFeedDialog";
#endif

            theme = Theme.CurrentThemeSet;
            focusable = false;
            RenderBaseTile = false;
            FeedItem.Init(theme, 1.0f);

            titleFont = SysFont.GetSystemFont(theme.TextFontFamily, 1.5f * theme.TextBaseFontSize, System.Drawing.FontStyle.Bold);
            title = new Label(this, titleFont, theme.LightTextColor, outlineColor: theme.DarkTextColor, outlineWidth: 1.2f, labelId: "mainMenu.news");
            title.Size = title.CalcMinSize();

            this.AddWidget(title);

            pixelCamera = new SpriteCamera();
            pixelCamera.Update();

            newsFeed = new NewsFeeds();
            newsFeed.BeginFetchNews();
            gettingFeed = true;

        }   // end of c'tor

        public override void Recalc()
        {
            // Based on the current rect size, calc sizes for everything else.
            if (Dirty || scaleFactor != camera.Zoom)
            {
                scaleFactor = camera.Zoom;
                FeedItem.Init(theme, scaleFactor);

                knobRadius = 32.0f;
                margin = 8.0f;

                // Position textRect in cameraPixel coords, not dialog coords.
                textRect.Position = scaleFactor * (new Vector2(0, titleFont.LineSpacing) + rect.Position);
                textRect.Size = scaleFactor * (new Vector2(rect.Width - 2 * knobRadius - margin, rect.Height - titleFont.LineSpacing));
                textRect.Truncate();

                knobRadius = 32.0f * scaleFactor;
                margin = 8.0f * scaleFactor;

                textWidth = (int)(textRect.Width - 2 * margin);

                if (feedItems != null)
                {
                    foreach (FeedItem item in feedItems)
                    {
                        item.Width = textWidth;
                    }
                }
            }

            base.Recalc();
        }   // end of Recalc()

        public override void Update(SpriteCamera camera)
        {
            this.camera = camera;
            pixelCamera.Update();

            Recalc();

            // Create/update the rendertarget if needed.
            RefreshRT(camera);

            if (gettingFeed)
            {
                newsFeed.Update();

                if (newsFeed.CurrentState == NewsFeeds.OpState.Failed)
                {
                    gettingFeed = false;
                }
                else if (newsFeed.CurrentState == NewsFeeds.OpState.Retrieving)
                {
                    // Nothing to see here, move along.
                }
                else if (newsFeed.CurrentState == NewsFeeds.OpState.Retrieved)
                {
                    try
                    {
                        // Done getting news feeds items.
                        feedItems = newsFeed.GetFeedList(textWidth);

                        gettingFeed = false;
                    }
                    catch
                    {
                        gettingFeed = false;
                    }
                }

            }   // end of if gettingFeed

            base.Update(camera);
        }   // end of Update()

        public override void Render(SpriteCamera camera)
        {
            Vector2 pos = Rectangle.Position;
            pos.Y += titleFont.LineSpacing;

            // If there's anything to display.
            if (rt != null)
            {
                // Tile with text as texture.
                // Calc padding based on slider.  This is how we scroll the rt with the text.
                int yOffset = (int)(rt.Height - textRect.Height);
                Padding padding = new Padding((int)margin, -(int)(slider * yOffset), (int)margin, -(int)((1 - slider) * yOffset));
                padding = new Padding(0, -(int)(slider * yOffset), 0, -(int)((1 - slider) * yOffset));

                RoundedRect.Render(pixelCamera, textRect, scaleFactor * theme.BaseCornerRadius, Color.White,
                                texture: rt, texturePadding: padding,
                                outlineColor: Color.Black, outlineWidth: scaleFactor * 2.0f,
                                shadowStyle: ShadowStyle.Inner, shadowSize: scaleFactor * 8.0f, shadowOffset: scaleFactor * new Vector2(6, 8), shadowAttenuation: 1.0f);

                Vector2 sliderTop = new Vector2(textRect.Right + margin + knobRadius, textRect.Top + knobRadius);
                Vector2 sliderBottom = sliderTop;
                sliderBottom.Y = textRect.Bottom - knobRadius;

                // If gamepad input, show right stick icon.
                if (KoiLibrary.LastTouchedDeviceIsGamepad)
                {
                    Disc.Render(pixelCamera, sliderTop + new Vector2(-knobRadius, knobRadius), knobRadius, theme.BaseColor,
                                texture: Textures.Get("GamePad RightStick"), texturePadding: new Padding(-2),   // The padding hides the underlying disc.
                                shadowStyle: ShadowStyle.Outer, shadowSize: 4.0f, shadowOffset: new Vector2(2, 3), shadowAttenuation: 0.8f);
                }

                // Note that we're only calling base.Render if we are rendering anything at all.  Otherwise
                // we'd display the "NEWS" title without anything below it.
                base.Render(camera);

                // Debug rect for the whole dialog.
                //RoundedRect.Render(camera, rect, 2.0f, Color.Red * 0.2f);

            }

        }   // end of Render()

        public override void RegisterForInputEvents()
        {
            // Call base register first.  By putting the child widgets on the input stacks
            // first we can then put oursleves on and have priority.
            base.RegisterForInputEvents();

            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftDown);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseWheel);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.Tap);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.OnePointDrag);
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.GamePad);
        }

        public override void UnregisterForInputEvents()
        {
            base.UnregisterForInputEvents();
        }

        public override bool ProcessMouseLeftDownEvent(MouseInput input)
        {
            Debug.Assert(Active);

            Vector2 hit = pixelCamera.ScreenToCamera(input.Position);

            // Feed item URL links.
            // Note we test these before the overall textRect.
            {
                // Offset the hit to account for textRect and current slider position.
                Vector2 feedHit = hit - textRect.Position;
                float yOffset = rt.Height - textRect.Height;
                feedHit.Y += slider * yOffset;

                foreach (FeedItem item in feedItems)
                {
                    if (item.LinkHitBox.Contains(feedHit))
                    {
                        clickedFeedItem = item;
                        KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                        return true;
                    }
                }
            }

            // Text box.
            if (textRect.Contains(hit))
            {
                draggingText = true;
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);
                KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseMove);

                return true;
            }

            return base.ProcessMouseLeftDownEvent(input);
        }

        public override bool ProcessMouseMoveEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (draggingText)
            {
                float yOffset = rt.Height - textRect.Height;
                slider -= input.DeltaPosition.Y / yOffset / pixelCamera.Zoom;
                slider = MathHelper.Clamp(slider, 0, 1);

                return true;
            }

            return base.ProcessMouseMoveEvent(input);
        }

        public override bool ProcessMouseWheelEvent(MouseInput input)
        {
            Debug.Assert(Active);

            // Scroll news.
            slider -= input.E.Delta / 2400.0f;
            slider = MathHelper.Clamp(slider, 0, 1);

            return true;
        }

        public override bool ProcessMouseLeftUpEvent(MouseInput input)
        {
            Debug.Assert(Active);

            if (draggingText)
            {
                draggingText = false;

                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseLeftUp);
                KoiLibrary.InputEventManager.UnregisterForEvent(this, InputEventManager.Event.MouseMove);
            }

            if (clickedFeedItem != null)
            {
                // Hit test once again to ensure we're still over the link.
                Vector2 hit = pixelCamera.ScreenToCamera(input.Position);

                // Offset the hit to account for textRect and current slider position.
                Vector2 feedHit = hit - textRect.Position;
                float yOffset = rt.Height - textRect.Height;
                feedHit.Y += slider * yOffset;

                if (clickedFeedItem.LinkHitBox.Contains(feedHit))
                {
                    Process.Start(clickedFeedItem.URL);
                        
                    KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseLeftUp);

                    return true;
                }

                clickedFeedItem = null;
            }

            return base.ProcessMouseLeftUpEvent(input);
        }

        public override bool ProcessTouchTapEvent(TapGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            Vector2 hit = pixelCamera.ScreenToCamera(gesture.Position);

            // Offset the hit to account for textRect and current slider position.
            Vector2 feedHit = hit - textRect.Position;
            float yOffset = rt.Height - textRect.Height;
            feedHit.Y += slider * yOffset;

            foreach (FeedItem item in feedItems)
            {
                if (item.LinkHitBox.Contains(feedHit))
                {
                    Process.Start(item.URL);

                    return true;
                }
            }

            return base.ProcessTouchTapEvent(gesture);
        }

        public override bool ProcessTouchOnePointDragEvent(OnePointDragGestureEventArgs gesture)
        {
            Debug.Assert(Active);

            if (!draggingText && gesture.Gesture == GestureType.OnePointDragBegin)
            {
                Vector2 hit = pixelCamera.ScreenToCamera(gesture.StartPosition);
                if (textRect.Contains(hit))
                {
                    draggingText = true;
                }
            }
            if (draggingText)
            {
                float yOffset = rt.Height - textRect.Height;
                slider -= gesture.DeltaPosition.Y / yOffset / pixelCamera.Zoom;
                slider = MathHelper.Clamp(slider, 0, 1);

                if (draggingText)
                {
                    if (gesture.Gesture == GestureType.OnePointDragEnd)
                    {
                        draggingText = false;
                    }
                }

                return true;
            }

            return base.ProcessTouchOnePointDragEvent(gesture);
        }

        public override bool ProcessGamePadEvent(GamePadInput pad)
        {
            Debug.Assert(Active);

            if (pad.RightStick.Y != 0)
            {
                float tics = Time.WallClockFrameSeconds;
                slider = MathHelper.Clamp(slider - tics * pad.RightStick.Y, 0, 1);
                return true;
            }

            return base.ProcessGamePadEvent(pad);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Updates the RT with the news feed text in it.  This happens if the RT loses
        /// the content or if the screen size changes.
        /// </summary>
        /// <param name="camera"></param>
        void RefreshRT(SpriteCamera camera)
        {
            if (feedItems == null || feedItems.Count == 0)
            {
                // Nothing to do here.
                return;
            }

            bool refreshNeeded = false;

            int totalFeedHeight = (int)GetTotalHeight();

            Point rtSize = new Point((int)textRect.Width, totalFeedHeight);
            if (rt == null || rt.IsDisposed || rt.GraphicsDevice.IsDisposed || rt.Width != rtSize.X || rt.Height != rtSize.Y)
            {
                DeviceResetX.Release(ref rt);
                rt = new RenderTarget2D(KoiLibrary.GraphicsDevice, rtSize.X, rtSize.Y);
                refreshNeeded = true;
            }

            if (rt.IsContentLost)
            {
                refreshNeeded = true;
            }

            if (refreshNeeded)
            {
                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                InGame.SetRenderTarget(rt);
                InGame.Clear(Color.Transparent);

                // Create a temp camera with 0,0 in the upper left hand corner.
                SpriteCamera cam = new SpriteCamera();
                cam.Position = new Vector2(rt.Width, rt.Height) / 2.0f;
                cam.Update(new Vector2(rt.Width, rt.Height));

                Vector2 pos = new Vector2(margin, 0);

                foreach (FeedItem item in feedItems)
                {
                    item.Width = textWidth;
                    item.Render(cam, pos);

                    pos.Y += item.Height + 2 * margin;
                }

                InGame.SetRenderTarget(null);
            }

        }   // end of RefreshRT()

        /// <summary>
        /// Gets the total height of all news items.
        /// </summary>
        /// <returns></returns>
        float GetTotalHeight()
        {
            float height = 0;
            
            foreach (FeedItem item in feedItems)
            {
                height += item.Height + 2 * margin;
            }

            return height;
        }   // end of GetTotalHeight()

        #endregion


    }   // end of class NewsFeedDialog

}   // end of namespace KoiX.UI.Dialogs
