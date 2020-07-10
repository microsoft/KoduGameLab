// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

using Boku.Audio;
using Boku.Base;
using Boku.Common;
using Boku.Fx;
using Boku.Input;

namespace Boku.UI2D
{
    /// <summary>
    /// Popup element for the LoadLevelMenu.  Includes thumbnail of level
    /// and a list of options...
    /// </summary>
    public class LoadLevelPopup : INeedsDeviceReset
    {
        public static Color unselectedTextColor = new Color(200, 200, 200);
        public static Color selectedTextColor = new Color(20, 20, 20);

        public static UI2D.Shared.GetFont ItemFont = UI2D.Shared.GetGameFont24;
        public static int additionalLineSpacing = 8;

        public class PopupItem
        {
            #region Members

            private LoadLevelPopup parent = null;

            public delegate void OnSelectEvent();   // Delegate to call when user chooses this item.
            private OnSelectEvent onSelect = null;

            private string text;
            private Vector4 textColor = unselectedTextColor.ToVector4();    // Displayed color.
            private Vector4 _textColor = unselectedTextColor.ToVector4();   // Twitch target color.
            private float barAlpha = 0.0f;                                  // Display alpha for highlight bar.
            private float _barAlpha = 0.0f;                                 // Twitch target alpha.

            private bool selected = false;

            public AABB2D hitBox = new AABB2D();   // Pixel based bounding box for mouse hit testing.

            #endregion

            #region Accessors

            public bool Selected
            {
                get { return selected; }
                set
                {
                    if (selected != value)
                    {
                        selected = value;
                        if (selected)
                        {
                            // Twitch to selected colors.
                            float time = 0.2f;
                            {
                                _textColor = LoadLevelPopup.selectedTextColor.ToVector4();
                                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = val; };
                                TwitchManager.CreateTwitch<Vector4>(textColor, _textColor, set, time, TwitchCurve.Shape.EaseOut);
                            }
                            {
                                _barAlpha = 1.0f;
                                TwitchManager.Set<float> set = delegate(float val, Object param) { barAlpha = val; };
                                TwitchManager.CreateTwitch<float>(barAlpha, _barAlpha, set, time, TwitchCurve.Shape.EaseOut);
                            }
                        }
                        else
                        {
                            // Twitch to unselected colors.
                            float time = 0.2f;
                            {
                                _textColor = LoadLevelPopup.unselectedTextColor.ToVector4();
                                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = val; };
                                TwitchManager.CreateTwitch<Vector4>(textColor, _textColor, set, time, TwitchCurve.Shape.EaseIn);
                            }
                            {
                                _barAlpha = 0.0f;
                                TwitchManager.Set<float> set = delegate(float val, Object param) { barAlpha = val; };
                                TwitchManager.CreateTwitch<float>(barAlpha, _barAlpha, set, time, TwitchCurve.Shape.EaseIn);
                            }
                        }
                    }
                }
            }   // end of Selected accessor

            public Color TextColor
            {
                get { return new Color(textColor); }
            }

            public float BarAlpha
            {
                get { return barAlpha; }
            }

            public string Text
            {
                get { return text; }
            }

            public OnSelectEvent OnSelect
            {
                get { return onSelect; }
            }

            #endregion


            #region Public

            public PopupItem(string text, OnSelectEvent onSelect, LoadLevelPopup parent)
            {
                this.text = text;
                this.onSelect = onSelect;
                this.parent = parent;
            }

            #endregion
        }

        public static Texture2D frame = null;
        public static Texture2D greenBar = null;

        private int curIndex = 0;   // The currently selected option.

        private Vector2 position;
        private Vector2 size;
        private Vector2 _size;      // Size used in rendering.  (twitched)

        private Matrix worldMatrix = Matrix.Identity;

        private bool active = false;

        private List<PopupItem> itemList = null;    // The list of options.

        private TextBlob blob = null;       // Allows icon in menu items.

        // Params for pop in/out.
        private double startTime = 0.0;     // Time when transition started.
        private float duration = 0.2f;      // Duration of transition.

        private CommandMap commandMap = new CommandMap("LoadLevelPopup");

        #region Accessors

        public bool Active
        {
            get { return active || Time.WallClockTotalSeconds - startTime < duration; }
            set
            {
                if (active != value)
                {
                    if (value)
                    {
                        //clear out any previous mouse input - this prevents false positive double clicks caused by the second click coming shortly after 
                        //entering the level browser
                        MouseInput.Left.ClearAllWasPressedState();

                        curIndex = 0;   // Always start at the top.

                        startTime = Time.WallClockTotalSeconds;
                        // twitch _size
                        _size.X = 0.0f;
                        TwitchManager.Set<float> set = delegate(float val, Object param) { _size.X = val; };
                        TwitchManager.CreateTwitch<float>(_size.X, size.X, set, duration, TwitchCurve.Shape.OvershootOut);

                        CommandStack.Push(commandMap);
                        HelpOverlay.Push("LoadLevelPopup");

                        active = true;
                    }
                    else
                    {
                        startTime = Time.WallClockTotalSeconds;
                        // twitch _size
                        TwitchManager.Set<float> set = delegate(float val, Object param) { _size.X = val; };
                        TwitchManager.CreateTwitch<float>(_size.X, 0.0f, set, duration, TwitchCurve.Shape.OvershootIn);

                        CommandStack.Pop(commandMap);
                        HelpOverlay.Pop();

                        active = false;
                    }
                }
            }
        }

        public int CurIndex
        {
            get { return curIndex; }
            set
            {
                if (curIndex != value)
                {
                    curIndex = value;
                }
            }
        }

        public string CurString
        {
            get { return itemList[curIndex].Text; }
        }

        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
            set { worldMatrix = value; }
        }

        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Vector2 Size
        {
            get { return size; }
            set { size = value; _size = value; }
        }

        /// <summary>
        /// Returns the number of items in the list.
        /// </summary>
        public int NumItems
        {
            get { return itemList.Count; }
        }

        #endregion

        // c'tor
        public LoadLevelPopup()
        {
            itemList = new List<PopupItem>();

            blob = new TextBlob(ItemFont, "foo", 500);
            blob.Justification = UIGridElement.Justification.Right;
        }

        /// <summary>
        /// Adds a new item entry to the list.
        /// </summary>
        /// <param name="text"></param>
        public void AddItem(string text, PopupItem.OnSelectEvent onSelect )
        {
            PopupItem item = new PopupItem(text, onSelect, this);
            itemList.Add(item);
            if (itemList.Count == 1)
            {
                item.Selected = true;
            }
        }   // end of LoadLevelPopup AddItem()

        public void ClearAllItems()
        {
            itemList.Clear();
        }   // end of ClearAllItems()

        public void Update(Camera camera, ref Matrix parentMatrix)
        {
            CommandMap map = CommandStack.Peek();

            if (map != commandMap)
                return;

            // Check for input.
            if (active && itemList.Count > 0)
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                bool mouseDown = false;
                bool mouseUp = false;
                bool mouseSelect = false;
                bool touchSelect = false;
                if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                // MouseInput
                {
                    // Did user double click?  If so, treat as a shortcut to play.
                    if (MouseInput.Left.WasDoubleClicked)
                    {
                        // This works because we _know_ Play is the first one in the list.
                        // Not exactly a great solution.
                        curIndex = 0;
                        mouseSelect = true;
                    }

                    Vector2 hit = MouseInput.GetMouseInRtCoords();
                    if (!mouseSelect)
                    {
                        if (itemList[CurIndex].hitBox.LeftPressed(hit))
                        {
                            mouseSelect = true;
                        }
                    }

                    // If mouse is over menu and moving, choose item under mouse as selection.
                    if (!mouseSelect && MouseInput.Position != MouseInput.PrevPosition)
                    {
                        for (int i = 0; i < itemList.Count; i++)
                        {
                            if (itemList[i].hitBox.Contains(hit))
                            {
                                CurIndex = i;
                                break;
                            }
                        }
                    }

                    int scroll = MouseInput.ScrollWheel - MouseInput.PrevScrollWheel;
                    if (scroll > 0)
                    {
                        mouseUp = true;
                    }
                    else if (scroll < 0)
                    {
                        mouseDown = true;
                    }

                    // If user clicks off of the popup, treat as Back.
                    if (MouseInput.Left.WasPressed && MouseInput.ClickedOnObject == null)
                    {
                        Active = false;
                        return;
                    }

                }   // end of mouse input.
                else if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                // TouchInput
                {
                    TouchContact touch = TouchInput.GetOldestTouch();
                    if (touch != null)
                    {
                        Vector2 hit = TouchInput.GetAspectRatioAdjustedPosition(touch.position, camera, true);
                        bool hitSomething = false;

                        // Check for a hit on any of the items.
                        for (int i = 0; i < itemList.Count; i++)
                        {
                            if (itemList[i].hitBox.Contains(hit))
                            {
                                CurIndex = i;
                                hitSomething = true;
                            }
                        }

                        if (touch.phase == TouchPhase.Ended)
                        {
                            if (hitSomething)
                            {
                                // We've touched off on an item so choose it.
                                touchSelect = true;
                            }
                            else
                            {
                                // We touched off and didn't hit anything.
                                if (touch.TouchedObject == this)
                                {
                                    touch.TouchedObject = null;
                                }
                                else
                                {
                                    Active = false;
                                }
                            }
                        }
                    }
                }   // end of Touch input.


                if (Actions.Select.WasPressed || mouseSelect || touchSelect)
                {
                    Actions.Select.ClearAllWasPressedState();

                    if (itemList[curIndex].OnSelect != null)
                        itemList[curIndex].OnSelect();
                    Foley.PlayPressA();

                    return;
                }

                if (Actions.Cancel.WasPressed)
                {
                    Actions.Cancel.ClearAllWasPressedState();

                    Active = false;
                    Foley.PlayBack();

                    return;
                }

                int prevIndex = curIndex;

                // Handle input changes here.
                if (Actions.ComboDown.WasPressedOrRepeat || mouseDown)
                {
                    ++curIndex;
                    if (curIndex >= itemList.Count)
                    {
                        curIndex = 0;
                    }

                    Foley.PlayShuffle();
                }

                if (Actions.ComboUp.WasPressedOrRepeat || mouseUp)
                {
                    --curIndex;
                    if (curIndex < 0)
                    {
                        curIndex = itemList.Count - 1;
                    }

                    Foley.PlayShuffle();
                }

                // Ensure that the selected state of all items is correct.
                for (int i = 0; i < itemList.Count; i++)
                {
                    itemList[i].Selected = i == CurIndex;
                }
            }

        }   // end of LoadLevelPopup Update()

        public void Render(Camera camera)
        {
            if (Active)
            {
                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                // Frame.
                quad.Render(frame, position, _size, "TexturedRegularAlpha");

                // Calc alpha value to use for elements based on expanded size of frame.
                float alpha = _size.X / size.X;
                alpha *= alpha;

                // Items.

                float additionalSpacing = (_size.Y - itemList.Count * ItemFont().LineSpacing) / (itemList.Count + 1);
                Vector2 barSize = new Vector2(_size.X, ItemFont().LineSpacing);
                float margin = 6;
                Vector2 pos = Vector2.Zero;
                pos.X = position.X + _size.X - margin;
                pos.Y = position.Y + _size.Y / 2.0f;    // Center.
                pos.Y -= itemList.Count / 2.0f * ItemFont().LineSpacing + (itemList.Count - 1) / 2.0f * additionalSpacing;
                for (int i = 0; i < itemList.Count; i++)
                {
                    // Render bar if needed.
                    if (itemList[i].BarAlpha > 0.0f)
                    {
                        quad.Render(greenBar, new Vector4(1.0f, 1.0f, 1.0f, itemList[i].BarAlpha * alpha), pos - new Vector2(_size.X, -2), barSize, "TexturedRegularAlpha");
                    }

                    // Set up mouse hit box.
                    itemList[i].hitBox.Set(pos - new Vector2(_size.X, -2), pos - new Vector2(_size.X, -2) + barSize);

                    // Render text
                    Color textColor = itemList[i].TextColor;
                    if (alpha < 1.0f)
                    {
                        textColor.A = (byte)(textColor.A * alpha);
                    }
                    blob.RawText = itemList[i].Text;
                    Vector2 textPos = pos - new Vector2(505, 0);
                    int lineWidth = blob.GetLineWidth(0);
                    int spacing = blob.TotalSpacing;
                    if (lineWidth > 180)
                    {
                        blob.Font = UI2D.Shared.GetGameFont20;
                        lineWidth = blob.GetLineWidth(0);
                        if (lineWidth > 180)
                        {
                            blob.Font = UI2D.Shared.GetGameFont18Bold;
                            lineWidth = blob.GetLineWidth(0);
                            if (lineWidth > 180)
                            {
                                blob.Font = UI2D.Shared.GetGameFont15_75;
                            }
                        }
                    }
                    int down = (int)((spacing - blob.TotalSpacing) / 2.0f);
                    blob.RenderWithButtons(textPos + new Vector2(0, down), textColor);
                    
                    // Restore larger font.
                    blob.Font = UI2D.Shared.GetGameFont24;

                    pos.Y += ItemFont().LineSpacing + additionalSpacing;
                }

            }

        }   // end of LoadLevelPopup Render()

        /// <summary>
        /// This sets the current index on the matching text line.  If no
        /// matching line is found, the current index is not changed.
        /// </summary>
        /// <param name="text"></param>
        public bool SetValue(string text)
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].Text == text)
                {
                    curIndex = i;
                    return true;
                }
            }
            return false;
        }   // end of LoadLevelPopup SetValue()

        /// <summary>
        /// Returns the index associated with the text. 
        /// Returns -1 if not found.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public int GetIndex(string text)
        {
            int result = -1;
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].Text == text)
                {
                    result = i;
                    break;
                }
            }

            return result;
        }   // end of GetIndex()

        public void LoadContent(bool immediate)
        {
            // Load the textures.
            if (frame == null)
            {
                frame = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\PopupFrame");
            }
            if (greenBar == null)
            {
                greenBar = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\GreenBar");
            }
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Release(ref frame);
            BokuGame.Release(ref greenBar);
        }   // end of LoadLevelPopup UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class LoadLevelPopup

}   // end of namespace Boku.UI2D






