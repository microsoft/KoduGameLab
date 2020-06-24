
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

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
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Common.Gesture;
using Boku.Input;
using Boku.UI2D;

namespace Boku.UI2D
{
  //  public delegate Texture2D GetTexture();

    /// <summary>
    /// A text element scrollbox.
    /// Contains containers which can hold text blobs and other info needed for rendering.
    /// </summary>
    public class ItemScroller
    {
        #region Members
        private const int MIN_WIDTH =32;
        private const int MIN_HEIGHT =24;
        private float scrollOffset = 0.0f;
        private float startDragOffset = 0.0f;
        private float startScrollOffset = 0.0f;
        private float seperatorHeight = 33.0f;

        private bool active = false;
        private int indexFirstVisibleItem = 0;
        private int indexLastVisibleItem = 0;
        private int indexFocusedItem = 0;
        private UiCamera uiCamera = new UiCamera();
        private Vector2 relativePosition;   // where am i to be drawn from?
        private AABB2D scrollBoxHit = null;
        private Viewport originalDeviceViewport;

        private CommandMap commandMap = new CommandMap("ModularScrollboxList");


        GetTexture getTexture = null;       // Texture2D for button icon.
        string label;                       // Label to be rendered next to icon.
        AABB2D box = null;                  // Hit box for mouse testing.
        private float smallBarWidth = 8.0f;
        private float normalBoxWidth= 16.0f;

        ButtonState state = ButtonState.Released;
        Vector4 color_bg = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);        // transparent background
        Vector4 color_bgCurr = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);    // current background, can be focused
        Vector4 color_seperator = new Vector4(0.1f,0.1f,0.1f,0.0f); // Container seperator 
        Vector4 color_Bar = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);    // scroll bar seperator 
        Vector4 color_BarBase = new Vector4(0.75f, 0.75f, 0.75f, 1.0f);    // scroll bar seperator 
        Vector4 color_border = new Vector4(0.0f, 0.0f, 0.0f, 0.5f); // Container seperator 
        Vector4 color_FocusItemBase = new Vector4(1.0f, 1.0f, 1.0f, 0.25f); // Item Focus color 
        Vector2 size = new Vector2(MIN_WIDTH, MIN_HEIGHT);

        Color defaultColor = Color.White;           // Color when not hovered over.
        Color hoverColor = new Color(50, 255, 50);  // Color when hovered over.

        Vector4 renderColor = Color.White.ToVector4();    // What is currently rendered.
        Vector4 targetColor = Color.White.ToVector4();    // Where the twitch is going.

        GetFont Font = null;
        private Vector2 fixedSize;

        // JW - SO not good
        public List<ScrollContainer> scrollItems = null; // holds scrollable items

        bool dirty = true;

        #endregion

        #region Accessors

        /// <summary>
        /// Allows direct manipulation of scroll box size
        /// </summary>
        public Vector2 FixedSize
        {
            get
            {
                return box.Size;
            }

            set 
            {
                if (size != value)
                {
                    indexFirstVisibleItem = 0;
                    indexLastVisibleItem = 0;
                    scrollOffset = 0.0f;
                    size = value;
                    fixedSize = size;
                    ResizeItemWidths();

                    box = new AABB2D(new Vector2(0.0f, 0.0f), size);
                }
            }
        }

        // verticle item separation distance
        public int SeperatorHeight
        {
            get { return (int)seperatorHeight; }            
            set { seperatorHeight = value; }
        }

        // verticle item separator color
        public Vector4 SeperatorColor
        {
            get { return color_seperator; }
            set { color_seperator = value; }
        }

        public int Count
        {
            get { return scrollItems.Count; }
        }

        public bool IsFocused
        {
            get { return state == ButtonState.Pressed; }
        }

        public AABB2D Box
        {
            get { return box; }
        }

        /// <summary>
        /// Set the normal (not hovered over) color for the button.
        /// </summary>
        public Vector4 BackgroundColor
        {
            set { color_bg = value; }
            get { return color_bg; }
        }

        public float TotalContainerHeight
        {
            get {
                float height = 0.0f; 
                for (int i = 0; i < scrollItems.Count; i++)
                {
                    height += scrollItems[i].Height;
                    if (i > 0)
                        height += seperatorHeight;
                }
                // HACK  Prevent last item from being too close to 
                // the edge when scrolled all the way down.
                height += 50;
                return height;
            }
        }

        public float ScrollableHeight
        {
            get
            {
                return TotalContainerHeight - FixedSize.Y;
            }
        }

        public float MaxBoxTravelDistance
        {
            get
            {
                return FixedSize.Y - ScrollBoxSize.Y;
            }
        }

        public Vector2 ScrollBoxSize
        {
            get 
            {
                float boxHeight = CalculateScrollBoxHeight();

                if (boxHeight < normalBoxWidth)
                {
                    boxHeight = normalBoxWidth;
                }

                if (size.X < MIN_WIDTH * 2)
                    return new Vector2(smallBarWidth, boxHeight);
                else
                    return new Vector2(normalBoxWidth, boxHeight);
            }
        }

        public ScrollContainer FocusedItem
        {
            get
            {
                if (indexFocusedItem > -1 && scrollItems != null)
                    return scrollItems[indexFocusedItem];
                else
                    return null;
            }
        }

        public bool Dirty
        {
            get { return dirty; }
            set { dirty = value; }
        }

        #endregion

        #region Public
        public void Activate()
        {
            if (!active)
            {
                active = true;
                scrollBoxHit = new AABB2D(GetBoxPos() + FixedSize, ScrollBoxSize);
                CommandStack.Push(commandMap);
                OnEnterFocus();

                dirty = true;
            }
        }
        
        public void Deactivate()
        {
            active = false;
            CommandStack.Pop(commandMap);
            OnExitFocus();
        }

        public void ActivateItem(int index)
        {
            if (index < scrollItems.Count )
            {
               // scrollItems[index].Click(new Vector2(0.0,0.0));

            }
        }

        public ItemScroller(Vector2 pos, Vector2 size, Color baseColor, GetTexture getTexture, 
            GetFont Font)
        {
            this.scrollItems = new List<ScrollContainer>();
            this.color_bg = baseColor.ToVector4();
            this.renderColor = baseColor.ToVector4();
            this.getTexture = getTexture;
            this.Font = Font;
            this.state = ButtonState.Released;
            this.FixedSize = size;
            this.relativePosition = pos;

            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            this.originalDeviceViewport =  device.Viewport;

            // Since we're rendering to a 1280*720 rendertarget.
            this.uiCamera.Resolution = new Point(1280, 720);

        }

        public void Clear()
        {
            // remove all items
            scrollItems.Clear();
        }
        public void InitDeviceResources()
        {
        }

        public void UnloadContent()
        {
        }   // end of UnloadContent()

        public void InsertItem(int pos, ScrollContainer item)
        {
            scrollItems.Insert(pos, item);
            dirty = true;
        }
        public void AddItem(ScrollContainer item)
        {
            scrollItems.Add(item);
            if (indexFocusedItem < 0)
            {
                indexFocusedItem = 0;
            }
            dirty = true;
        }

        public void RemoveItem(ScrollContainer item)
        {
            scrollItems.Remove(item);
            if (scrollItems.Count <= indexFocusedItem)
            {
                indexFocusedItem = scrollItems.Count - 1;
            }
            dirty = true;
        }

        public void RemoveAt(int no)
        {
            scrollItems.RemoveAt(no);
            dirty = true;
        }

        public string Label
        {
            set
            {
                label = value;
            }
        }

        public Vector2 GetSize()
        {
            return fixedSize;
        }   // end of GetSize()
       
        public void Update(Camera camera)
        {
            if (!active) 
            {
                return;
            }

            if (!KoiLibrary.LastTouchedDeviceIsGamepad)
            {
                if (indexFocusedItem < (scrollItems.Count))
                {
                    scrollItems[indexFocusedItem].DisableFocus();
                }
            }

            uiCamera.Update();
            //HandleGamepadInput();
            Vector2 boxPos = GetBoxPos()+ relativePosition;
            scrollBoxHit.Set(new AABB2D(boxPos, boxPos + ScrollBoxSize));
            ConstrainScrollBox(scrollOffset);

            //handle input *after* the scroll box has been updated
            //without this, the bounding box won't be properly updated (as done in the preceding few lines)
            //it works for mouse since this code is called every frame the mouse is over the feed
            //it *doesnt* work for touch, since this code is only called when a touch occurs (we don't have a hover equivalent)
            //this way, it works for both
            HandleMouseInput(camera);
            HandleTouchInput();
        }   // end of Update

        /// <summary>
        /// Set the viewport to match the current rendertarget size.
        /// </summary>
        /// <param name="useRelativePosition"></param>
        /// <param name="rt"></param>
        private void SetViewPort(bool useRelativePosition, RenderTarget2D rt)
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            Viewport vp = device.Viewport;

            // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<< FULL SCREEN WINDOWED MODE FIX
            // Save current viewport.
            originalDeviceViewport = vp;
            // FULL SCREEN WINDOWED MODE FIX >>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            if (useRelativePosition)
            {
                vp.X = (int)relativePosition.X;
                vp.Y = (int)relativePosition.Y + 4;
            }
            vp.Width = Math.Min((int)FixedSize.X, rt.Width - vp.X);
            vp.Height = Math.Min((int)FixedSize.Y - 6, rt.Height - vp.Y);
            vp.MinDepth = 0;
            vp.MaxDepth = 1;
            device.Viewport = vp;
        }

        private void RestoreViewport()
        {
            GraphicsDevice device = KoiLibrary.GraphicsDevice;
            device.Viewport = originalDeviceViewport;
        }

        public void Render()
        {
            RenderTarget2D rt = SharedX.RenderTarget1024_768;
            SpriteBatch batch = KoiLibrary.SpriteBatch;
            
            SetViewPort(true, rt);

            batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            {
                Rectangle srcRect = new Rectangle(0, 0, (int)FixedSize.X, (int)FixedSize.Y);
                Rectangle dstRect = new Rectangle(0, 0, (int)FixedSize.X, (int)FixedSize.Y);
                batch.Draw(rt, dstRect, srcRect, Color.White);
            }
            batch.End();
            
            RestoreViewport();
        }   // end Render()

        int hackFrame = -1;
        public void RefreshRT()
        {
            RenderTarget2D rt = SharedX.RenderTarget1024_768;

            if (dirty || hackFrame == Time.FrameCounter || rt.IsContentLost)
            {
                // Force another refresh 4 frames after dirty is set.
                // Yes, this is an utterly absurd hack.  But for some reason at startup
                // the rt isn't fully rendered.  By waiting 4 or more frames and then
                // refreshing again, it works.  No clue why.
                if (dirty)
                {
                    hackFrame = Time.FrameCounter + 4;
                }

                GraphicsDevice device = KoiLibrary.GraphicsDevice;

                device.SetRenderTarget(rt);

                SetViewPort(false, rt);

                device.Clear(Color.White);

                Vector2 pos = new Vector2(0.0f, 0.0f);
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                SpriteBatch batch = KoiLibrary.SpriteBatch;
                Vector2 baseSize = FixedSize;



                Vector4 drawColor = GetDrawColor();
                Texture2D buttonTexture = SharedX.BlackButtonTexture;

                // Render rectangular baseplate of scroller (maybe a transparent texture...).
                ssquad.Render(color_bgCurr, pos, baseSize - new Vector2(ScrollBoxSize.X, 0.0f));
                // Render scroll bar 
                if (TotalContainerHeight > FixedSize.Y)
                {
                    Vector2 barpos = GetBoxPos();
                    barpos.Y += pos.Y;
                    ssquad.Render(color_BarBase,
                        new Vector2(barpos.X + (ScrollBoxSize.X / 2.2f), 0.0f),
                        new Vector2(ScrollBoxSize.X / 9.0f, baseSize.Y));
                    ssquad.Render(color_Bar, barpos, ScrollBoxSize);
                }

                Vector2 seperatorSize = fixedSize;
                seperatorSize.X -= ScrollBoxSize.X;
                seperatorSize.Y = seperatorHeight;

                float pastScrollDist = 0.0f;
                indexFirstVisibleItem = -1;
                indexLastVisibleItem = -1;
                float prevContainerHeight = 0.0f;
                for (int i = 0; i < scrollItems.Count; i++)
                {
                    ScrollContainer sContainer = scrollItems[i];
                    Vector2 itemPos = pos;
                    itemPos.Y -= scrollOffset - pastScrollDist - seperatorHeight * i;

                    if (sContainer.Height >= ((scrollOffset - pastScrollDist) - sContainer.Height - prevContainerHeight))
                    {
                        if (indexFocusedItem == i)
                        {
                            ssquad.Render(color_FocusItemBase, itemPos, new Vector2(sContainer.Width, sContainer.Height));
                        }
                        sContainer.ResetWidth();

                        sContainer.Render(itemPos);
                        if (indexFirstVisibleItem < 0)
                            indexFirstVisibleItem = i;
                        if (itemPos.Y + 60 < (FixedSize.Y + relativePosition.Y))
                            indexLastVisibleItem = i;
                    }

                    scrollItems[i].UpdatePosition(itemPos);

                    prevContainerHeight = sContainer.Height;
                    pastScrollDist += sContainer.Height;

                    Vector2 seperatorOffset = pos;
                    seperatorOffset.Y += pastScrollDist - scrollOffset;

                    ssquad.Render(color_seperator, seperatorOffset, seperatorSize);
                }

                RestoreViewport();

                device.SetRenderTarget(null);

                dirty = false;
            }   // end if dirty
        }   // end of RefreshRT()

        public void ClearState()
        {
            state = ButtonState.Released;
            Vector4 curColor = new Vector4(renderColor.X, renderColor.Y, renderColor.Z, renderColor.W);
            Vector4 destColor = new Vector4(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, BackgroundColor.W);
            /*
            TwitchManager.Set<Vector4> set = delegate(Vector4 value, Object param)
            {
                renderColor.X = (byte)(value.X * 255.0f + 0.5f);
                renderColor.Y = (byte)(value.Y * 255.0f + 0.5f);
                renderColor.Z = (byte)(value.Z * 255.0f + 0.5f);
                renderColor.W = (byte)(value.W * 255.0f + 0.5f);
            };
            TwitchManager.CreateTwitch<Vector4>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
             * */
        }

        public void SetHoverState(Vector2 mouseHit)
        {
            Color newColor = box.Contains(mouseHit) ? hoverColor : defaultColor;
            /*
                 Vector3 curColor = new Vector3(renderColor.R / 255.0f, renderColor.G / 255.0f, renderColor.B / 255.0f);
                 Vector3 destColor = new Vector3(newColor.R / 255.0f, newColor.G / 255.0f, newColor.B / 255.0f);

                 TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                 {
                     renderColor.R = (byte)(value.X * 255.0f + 0.5f);
                     renderColor.G = (byte)(value.Y * 255.0f + 0.5f);
                     renderColor.B = (byte)(value.Z * 255.0f + 0.5f);
                 };
                 TwitchManager.CreateTwitch<Vector3>(curColor, destColor, set, 0.1f, TwitchCurve.Shape.EaseOut);
             }
             */

        }   // end of SetHoverState()

        public void OnEnterFocus()
        {
            color_bgCurr = color_bg;
            if (color_bgCurr.W < 0.5f)
                color_bgCurr.W += 0.05f;
            else
                color_bgCurr.W -= 0.05f;

            if (indexFocusedItem < (scrollItems.Count))
            {
                if (KoiLibrary.LastTouchedDeviceIsGamepad)
                {
                    scrollItems[indexFocusedItem].EnableFocus();
                }
            }

        } // end on EnterFocus

        public void OnExitFocus()
        {
            color_bgCurr = color_bg;
            if (indexFocusedItem < (scrollItems.Count))
            {
                scrollItems[indexFocusedItem].DisableFocus();
            }
        }// end on ExitFocus
        #endregion

        #region Private

        public float CalculateScrollBoxHeight()
        {
            float boxHeight = TotalContainerHeight;
            if (TotalContainerHeight > FixedSize.Y)
            {
                boxHeight = (FixedSize.Y / ScrollableHeight) * FixedSize.Y;
            }
            if (boxHeight > FixedSize.Y)  // HACK, the abaove calculation does not work perfectly.
            {
                boxHeight = ScrollableHeight;
            }
            return boxHeight;
        }

        private void HandleMouseInput(Camera camera)
        {
            if (!KoiLibrary.LastTouchedDeviceIsKeyboardMouse) { return; }
            if (scrollItems.Count == 0) { return; }

            Vector2 mouseHit = new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y);
            Vector2 adjHitPos = mouseHit;

            if (LowLevelMouseInput.DeltaScrollWheel < 0)
            {
                FocusNext();
            } 
            else if (LowLevelMouseInput.DeltaScrollWheel > 0)
            {
                FocusPrev();
            }
            else if (scrollBoxHit.Contains(adjHitPos) || startDragOffset != 0.0f)
            {
                if (LowLevelMouseInput.Left.WasPressed)
                {
                    startDragOffset = mouseHit.Y;
                    startScrollOffset = scrollOffset;
                }
                else if (LowLevelMouseInput.Left.IsPressed)
                {
                    float dragDistance = mouseHit.Y - startDragOffset;

                    float desiredScrollOffset =
                        (ScrollableHeight * (dragDistance / MaxBoxTravelDistance)) + startScrollOffset;
                    ConstrainScrollBox(desiredScrollOffset);
                }
                else
                {
                    startDragOffset = 0.0f;
                }
            }
            else
            {
                adjHitPos -= relativePosition;

                for (int i = 0; i < scrollItems.Count; i++)
                {
                    if (scrollItems[i].IsInFocus(adjHitPos) || true)
                    {
                        Object obj;
                        if (LowLevelMouseInput.Left.WasReleased)
                        {
                            scrollItems[i].Click(adjHitPos, out obj, ClickType.WasReleased);
                            if (indexFocusedItem != i)
                            {
                                indexFocusedItem = i;
                                scrollItems[indexFocusedItem].ResetFocus();
                            }

                        }
                        else if (LowLevelMouseInput.Left.WasPressed)
                        {
                            scrollItems[i].Click(adjHitPos, out obj, ClickType.WasPressed);
                        }
                        else if (LowLevelMouseInput.Left.IsPressed)
                        {
                            scrollItems[i].Click(adjHitPos, out obj, ClickType.IsPressed);
                        }
                        else
                        {
                            scrollItems[i].Hover(adjHitPos);
                        }
                    }
                }
            }
        }

        private void HandleTouchInput()
        {
            if (!KoiLibrary.LastTouchedDeviceIsTouch) { return; }
            if (scrollItems.Count == 0) { return; }

            if (TouchInput.TouchCount!=1) { return; }

            Vector2 touchHit = TouchInput.GetOldestTouch().position;
            Vector2 adjHitPos = touchHit;

            if (scrollBoxHit.Contains(adjHitPos) || startDragOffset != 0.0f)
            {
                if (TouchInput.WasTouched || (TouchInput.IsTouched && startDragOffset==0.0f))
                {
                    startDragOffset = touchHit.Y;
                    startScrollOffset = scrollOffset;
                }
                else if (TouchInput.IsTouched)
                {
                    float dragDistance = touchHit.Y - startDragOffset;

                    float desiredScrollOffset =
                        (ScrollableHeight * (dragDistance / MaxBoxTravelDistance)) + startScrollOffset;
                    ConstrainScrollBox(desiredScrollOffset);
                }
                else
                {
                    startDragOffset = 0.0f;
                }
            }
            else
            {
                adjHitPos -= relativePosition;

                for (int i = 0; i < scrollItems.Count; i++)
                {
                    if (scrollItems[i].IsInFocus(adjHitPos))
                    {
                        if (TouchGestureManager.Get().TapGesture.WasRecognized)
                        {
                            Object obj;
                            scrollItems[i].Click(adjHitPos, out obj, ClickType.WasReleased);
                            if (indexFocusedItem != i)
                            {
                                indexFocusedItem = i;
                                scrollItems[indexFocusedItem].ResetFocus();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Looks at the current focused item and asks it if there is a previous index to focuse within its control
        /// if so update the index to the prev. If not, call FocusPrevItem
        /// </summary>
        public void FocusPrev()
        {
            if (scrollItems.Count == 0)
                return;
            if (!scrollItems[indexFocusedItem].SetPrevFocus())
            {
                float spacerHeight = 0.0f;
                if (indexFocusedItem > 0)
                {
                    if ((scrollItems[indexFocusedItem].Top < 0) && (scrollItems[indexFocusedItem].Bottom > (fixedSize.Y - 8)))
                    {
                        // Scroll box is off the top of the screen and is so large it exceeds the length of the scroller
                        if (scrollItems[indexFocusedItem].Top > -10)
                        {
                            spacerHeight = scrollItems[indexFocusedItem].Top;
                        }
                        else
                        {
                            spacerHeight = -10.0f;
                        }
                        ConstrainScrollBox(scrollOffset + spacerHeight);
                    }
                    else if ((scrollItems[indexFocusedItem].Top == 0.0f) ||
                             (scrollItems[indexFocusedItem].Top > FixedSize.Y) ||
                             (scrollItems[indexFocusedItem - 1].Top < 0.0f))
                    {
                        FocusPrevItem();
                    }
                    else
                    {
                        indexFocusedItem--;
                    }
                    scrollItems[indexFocusedItem].SetFocusLast();
                }
            }
        }

        /// <summary>
        /// Looks at the current focused item and tells it to press.
        /// </summary>
        public void PressFocus()
        {
            if (indexFocusedItem < scrollItems.Count)
            {
                scrollItems[indexFocusedItem].Press();
            }
        }

        /// <summary>
        /// Looks at the current focused item and asks it if there is a next index to focuse within its control
        /// if so update the index to the next. If not, call FocusNextItem
        /// </summary>
        public void FocusNext()
        {
            if (scrollItems.Count == 0)
                return;
            if (!scrollItems[indexFocusedItem].SetNextFocus())
            {
                if (indexFocusedItem == (scrollItems.Count - 1))
                {
                    scrollItems[indexFocusedItem].SetFocusLast();
                    scrollItems[indexFocusedItem].EnableFocus();
                }

                float spacerHeight = 0.0f;
                // determin potential partial scroll amount
                if ( (scrollItems[indexFocusedItem].Top <= 0.0) &&
                     (scrollItems[indexFocusedItem].Bottom > fixedSize.Y))
                {
                    spacerHeight = 10.0f;
                    if (scrollItems[indexFocusedItem].Bottom - fixedSize.Y < 10)
                    {
                        spacerHeight = scrollItems[indexFocusedItem].Bottom - fixedSize.Y + spacerHeight;
                    }
                }
                if (indexFocusedItem < scrollItems.Count - 1)
                {
                    if ((scrollItems[indexFocusedItem + 1].Height > fixedSize.Y) &&
                        (scrollItems[indexFocusedItem].Bottom > 0.0f) &&
                        (scrollItems[indexFocusedItem + 1].Bottom > fixedSize.Y))
                    {
                        if (!((scrollItems[indexFocusedItem].Top <= 0.0) &&
                             (scrollItems[indexFocusedItem].Bottom > fixedSize.Y)))
                        {
                            spacerHeight = scrollItems[indexFocusedItem].Bottom +seperatorHeight;
                            indexFocusedItem++;
                        }
                        ConstrainScrollBox(scrollOffset + spacerHeight);
                    }
                    else if ((scrollItems[indexFocusedItem + 1].Bottom > fixedSize.Y) ||
                             (scrollItems[indexFocusedItem + 1].Bottom < 0))
                    {
                        FocusNextItem();
                    }
                    else
                    {
                        if (indexFocusedItem < scrollItems.Count)
                            indexFocusedItem++;
                        else
                            indexFocusedItem = scrollItems.Count - 1;
                    }
                    scrollItems[indexFocusedItem].ResetFocus();
                    scrollItems[indexFocusedItem].EnableFocus();
                }
                else if (spacerHeight > 0.0f)
                {
                    ConstrainScrollBox(scrollOffset + spacerHeight);
                }
            }
        }

        public void FocusPrevItem()
        {
            if ( indexFocusedItem == 0)
                return;
            if (indexFocusedItem > 0)
            {
                indexFocusedItem--;
            }
            if (TotalContainerHeight < FixedSize.Y)
                return; // no container scrolling needed
            if (indexFirstVisibleItem < 0 || indexFirstVisibleItem >= scrollItems.Count)
                return; // something whent very wrong
            if (scrollItems[indexFocusedItem].Top > 0 &&(scrollItems[indexFocusedItem].Top < FixedSize.Y))
                return;
           
            float desiredScrollOffset = scrollItems[indexFocusedItem].Top;

            desiredScrollOffset = scrollOffset + desiredScrollOffset;

            ConstrainScrollBox(desiredScrollOffset);
        }

        /// <summary>
        ///  move to the next index and refocuse to the Top of that item
        ///  and show as much of the item as possible without cutting the top off
        /// </summary>
        public void FocusNextItem()
        {
            if (indexFocusedItem == scrollItems.Count - 1)
                return;

            if (indexFocusedItem < (scrollItems.Count - 1))
            {
                indexFocusedItem++;
            }

            if (TotalContainerHeight < FixedSize.Y)
                return; // no container scrolling needed

            if (indexLastVisibleItem < 0 || indexLastVisibleItem >= scrollItems.Count)
                return; // something bad happened
            if ((indexFocusedItem < indexLastVisibleItem) &&
                 (scrollItems[indexFocusedItem].Bottom < FixedSize.Y) &&
                (scrollItems[indexFocusedItem].Bottom>0.0f))// + relativePosition.Y))
            {
                return; // still a HOLE visible item below us.
            }

            float itemHeight = scrollItems[indexFocusedItem].Height;
            float itemTop = scrollItems[indexFocusedItem].Top;
            float maxUpDist = itemHeight - (FixedSize.Y- itemTop);
            float minUpDist = itemTop;
            float desiredScrollOffset = maxUpDist;
            if ((itemTop - maxUpDist) < 0.0)
                desiredScrollOffset = minUpDist;

            ConstrainScrollBox(scrollOffset + desiredScrollOffset + seperatorHeight);
        }

        private void ConstrainScrollBox(float desiredScrollOffset)
        {
            float prevScrollOffset = scrollOffset;

            //(FixedSize.Y * desiredScrollOffset / TotalContainerHeight) + scrollBoxHit.Size.Y;
            float baseOfSB = GetBoxPos().Y + ScrollBoxSize.Y;
            if (desiredScrollOffset < 0.0f)
            {
                scrollOffset = 0.0f;
            }
            else if (baseOfSB > (MaxBoxTravelDistance + ScrollBoxSize.Y) + 2) // desiredScrollOffset > (TotalContainerHeight - ScrollBoxSize.Y - FixedSize.Y)) // (MaxBoxTravelDistance + ScrollBoxSize.Y)) //    baseOfSB > (MaxBoxTravelDistance + ScrollBoxSize.Y))
            {
                scrollOffset = ScrollableHeight - ScrollBoxSize.Y + ScrollBoxSize.Y + 6; // ScrollableHeight - 2;
            }
            else
            {
                scrollOffset = desiredScrollOffset;
            }

            // If the scroll offset changed, refresh the texture.
            if (prevScrollOffset != scrollOffset)
            {
                dirty = true;
            }
        }

        private Vector4 GetDrawColor()
        {
            switch (state)
            {
                case ButtonState.Pressed:
                {
                    return new Vector4(0.7f, 0.7f, 0.7f, 0.4f);
                }
                case ButtonState.Released:
                {
                    return new Vector4(1.0f, 1.0f, 1.0f, 0.95f);
                }
                default:
                    return new Vector4(1.0f, 1.0f, 1.0f, 0.95f);
            }
        }

        private Vector2 GetBoxPos()
        {
            Vector2 pos = new Vector2(
                FixedSize.X - ScrollBoxSize.X,
                (MaxBoxTravelDistance * (scrollOffset / ScrollableHeight))
            );
            return pos;
        }

        public void ResizeItemWidths()
        {
            for (int i = 0; i < scrollItems.Count; i++)
            {
                ScrollContainer sContainer = scrollItems[i];
                sContainer.Width = fixedSize.X - ScrollBoxSize.X;
                sContainer.ResetWidth();
            }            
        }

        #endregion
       
    }   // end of class Button

}   // end of namespace Boku.UI2D
