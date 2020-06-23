
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
using Boku.Common.Xml;
using Boku.Common.Gesture;
using Boku.Input;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// Menu designed to work as a popup menu for MouseEdit mode.
    /// </summary>
    public class MouseMenu
    {
        public delegate void MouseMenuEvent(MouseMenu menu);

        public static Color unselectedTextColor = new Color(20, 20, 20);
        public static Color selectedTextColor = new Color(20, 20, 20);
        public static Color unselectedBackgroundColor = new Color(255, 255, 255, 220);
        public static Color selectedBackgroundColor = new Color(245, 255, 100, 240);

        public static UI2D.Shared.GetFont ItemFont = UI2D.Shared.GetGameFont24;
        private int chosenIndex = -1;

        public class MenuItem
        {
            #region Members

            private static Vector2 margin = new Vector2(8, 4);      // Margin used to offset text relative to panel position.

            private MouseMenu parent = null;

            private Vector2 position;           // Position relative to parent's position.
            private Vector2 absolutePosition;   // Absolute position on screen.

            private Vector2 size;               // Size in pixels.

            private string text;
            private Vector4 textColor = unselectedTextColor.ToVector4();                // Displayed color.
            private Vector4 _textColor = unselectedTextColor.ToVector4();               // Twitch target color.
            private Vector4 backgroundColor = unselectedBackgroundColor.ToVector4();    // Displayed color.
            private Vector4 _backgroundColor = unselectedBackgroundColor.ToVector4();   // Twitch target color.

            private AABB2D hitBox = new AABB2D();   // Needs to be filled in when size is calced.

            private bool selected = false;

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
                            float time = 0.1f;
                            {
                                _textColor = MouseMenu.selectedTextColor.ToVector4();
                                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = val; parent.dirty = true; };
                                TwitchManager.CreateTwitch<Vector4>(textColor, _textColor, set, time, TwitchCurve.Shape.EaseOut);
                            }
                            {
                                _backgroundColor = MouseMenu.selectedBackgroundColor.ToVector4();
                                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { backgroundColor = val; parent.dirty = true; };
                                TwitchManager.CreateTwitch<Vector4>(backgroundColor, _backgroundColor, set, time, TwitchCurve.Shape.EaseOut);
                            }
                        }
                        else
                        {
                            // Twitch to unselected colors.
                            float time = 0.1f;
                            {
                                _textColor = MouseMenu.unselectedTextColor.ToVector4();
                                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = val; parent.dirty = true; };
                                TwitchManager.CreateTwitch<Vector4>(textColor, _textColor, set, time, TwitchCurve.Shape.EaseIn);
                            }
                            {
                                _backgroundColor = MouseMenu.unselectedBackgroundColor.ToVector4();
                                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { backgroundColor = val; parent.dirty = true; };
                                TwitchManager.CreateTwitch<Vector4>(backgroundColor, _backgroundColor, set, time, TwitchCurve.Shape.EaseIn);
                            }
                        }
                    }
                }
            }   // end of Selected accessor

            public Color TextColor
            {
                get { return new Color(textColor); }
            }

            public AABB2D HitBox
            {
                get { return hitBox; }
            }

            public string Text
            {
                get { return text; }
            }

            /// <summary>
            /// Sets the menu item's position relative to the menu's position.
            /// </summary>
            public Vector2 Position
            {
                set 
                { 
                    position = value; 
                    absolutePosition = parent.Position + position;
                    // Force to pixel align to render cleaner.
                    absolutePosition.X = (float)Math.Round(absolutePosition.X);
                    absolutePosition.Y = (float)Math.Round(absolutePosition.Y);

                    hitBox.Set(absolutePosition, absolutePosition + size);
                }
                get { return position; }
            }

            public Vector2 Size
            {
                get { return size; }
                set 
                { 
                    size = value;
                    size.Y += 10.0f;
                    hitBox.Set(absolutePosition, absolutePosition + size);
                }
            }

            #endregion

            #region Public

            public MenuItem(string text, MouseMenu parent)
            {
                this.text = TextHelper.FilterInvalidCharacters(text);
                this.parent = parent;
            }

            public void CalcSize()
            {
                size.X = 2.0f * margin.X + ItemFont().MeasureString(text).X;
                size.Y = 2.75f * margin.Y + ItemFont().LineSpacing;

                hitBox.Set(absolutePosition, absolutePosition + size);

            }   // end of CalcSize()

            public void Render(SpriteBatch batch)
            {
                ScreenSpace3PanelQuad ssquad = ScreenSpace3PanelQuad.GetInstance();

                ssquad.Render(MouseMenu.roundedSquare, backgroundColor, absolutePosition, size, "TexturedRegularAlpha");
                TextHelper.DrawString(ItemFont, text, absolutePosition + margin, new Color(textColor));

            }   // end of Render()

            #endregion
        }   // end of class MenuItem

        public delegate void UIMouseMenuEvent(MouseMenu menu);

        private MouseMenuEvent onCancel = null;
        private MouseMenuEvent onChange = null;
        private MouseMenuEvent onSelect = null;

        public static Texture2D roundedSquare = null;

        private int curIndex = -1;   // The currently selected option.

        private Vector2 position;   // Location of menu in pixels.  (rendered here)
        private Vector2 _position;  // Position we're twitching toward.

        private Matrix worldMatrix = Matrix.Identity;
        private Matrix invWorldMatrix = Matrix.Identity;

        private bool active = false;

        private List<MenuItem> itemList = null; // The list of options.

        private CommandMap commandMap = new CommandMap("MouseMenu");  // Looking forward to the day when this is gone.
        private string helpOverlay = "MouseMenu";

        private float originRadius = 10.0f;     // How much the user is allowed to move the mouse to stay in 2-click mode.

        private bool dirty = true;  // Do we need to recalc the layout?

        private object obj = null;  // Generic ref used as needed.

        #region Accessors

        /// <summary>
        ///  Returns the last chosen index within the menu. 
        ///  -1 if the menu was closed without choosing anything.
        /// </summary>
        public int ChosenIndex
        {
            get { return chosenIndex; }
        }

        public bool Active
        {
            get { return active; }
        }

        public int CurIndex
        {
            get { return curIndex; }
            set
            {
                if (curIndex != value)
                {
                    if (curIndex < value)
                        Foley.PlayClickDown();
                    else
                        Foley.PlayClickUp();
                    curIndex = value;
                }
            }
        }

        public string CurString
        {
            get { return itemList[curIndex].Text; }
        }

        public int Count
        {
            get { return itemList.Count; }
        }

        /// <summary>
        /// Delegate to be called when the selection is changed.
        /// </summary>
        public MouseMenuEvent OnChange
        {
            set { onChange = value; }
        }

        /// <summary>
        /// Delegate to be called when user backs out.
        /// </summary>
        public MouseMenuEvent OnCancel
        {
            set { onCancel = value; }
        }

        /// <summary>
        /// Delegate to be called when the selection is chosen.
        /// </summary>
        public MouseMenuEvent OnSelect
        {
            set { onSelect = value; }
        }

        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
            set
            {
                worldMatrix = value;
                invWorldMatrix = Matrix.Invert(worldMatrix);
            }
        }

        /// <summary>
        /// Return the string for the given indexed item.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public string Item(int idx)
        {
            return idx < itemList.Count ? itemList[idx].Text : null;
        }

        /// <summary>
        /// Return the index for the given item text, or -1 if not found.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public int Index(string str)
        {
            for (int i = 0; i < itemList.Count; ++i)
            {
                if (itemList[i].Text == str)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Used to override the helpOverlay used by this menu.
        /// </summary>
        public string HelpOverlay
        {
            get { return helpOverlay; }
            set { helpOverlay = value; }
        }

        /// <summary>
        /// Position of menu in screen coordinate pixels.
        /// </summary>
        public Vector2 Position
        {
            get { return position; }
            set 
            {
                value.X = (float)Math.Round(value.X);
                value.Y = (float)Math.Round(value.Y);
                if (_position != value)
                {
                    _position = value;
                    // position = value; dirty = true;
                    TwitchManager.Set<Vector2> set = delegate(Vector2 val, Object param) { position = val; dirty = true; };
                    TwitchManager.CreateTwitch<Vector2>(position, _position, set, 0.2f, TwitchCurve.Shape.EaseOut);
                }
            }
        }

        /// <summary>
        /// Generic object ref.
        /// </summary>
        public object Object
        {
            get { return obj; }
            set { obj = value; }
        }

        public CommandMap CommandMap
        {
            get { return commandMap; }
        }
        

        #endregion

        // c'tor
        public MouseMenu()
        {
            itemList = new List<MenuItem>();
        }

        /// <summary>
        /// Adds a new text string entry to the list.
        /// </summary>
        /// <param name="text"></param>
        public void AddText(string text)
        {
            MenuItem item = new MenuItem(text, this);
            itemList.Add(item);
            if (itemList.Count == 1)
            {
                item.Selected = true;
            }
            dirty = true;
        }   // end of MouseMenu AddText()

        /// <summary>
        /// Inserts a new text entry at the given index.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        public void InsertText(string text, int index)
        {
            MenuItem item = new MenuItem(text, this);

            // Move everything below the new entry down one space.
            itemList.Add(itemList[itemList.Count - 1]);
            for (int i = itemList.Count - 1; i > index; i--)
            {
                itemList[i] = itemList[i - 1];
            }
            itemList[index] = item;

            dirty = true;
        }   // end of InsertText()

        /// <summary>
        /// Removes the given entry.
        /// </summary>
        /// <param name="text"></param>
        public void DeleteText(string text)
        {
            bool changed = false;
            // We filter the incoming text so we should also filter when looking for matches.
            string searchText = TextHelper.FilterInvalidCharacters(text);
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].Text == searchText)
                {
                    itemList.RemoveAt(i);
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                dirty = true;
            }
        }   // end of DeleteText()

        /// <summary>
        /// Deletes all the existing items.
        /// </summary>
        public void DeleteAll()
        {
            itemList.Clear();
            dirty = true;
        }   // end of DeleteAll()

        public void Update()
        {
            if (Active)
            {
                // Ensure everything is in the right place before trying any hit testing.
                if (dirty)
                {
                    CalcLayout();
                    dirty = false;
                }


                // Check for input.
                if (Active && itemList.Count > 0 && CommandStack.Peek() == commandMap)
                {
                    // Allow user to hit esc to exit.
                    if (Actions.Cancel.WasPressed)
                    {
                        Actions.Cancel.ClearAllWasPressedState();

                        Deactivate();
                        return;
                    }

                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        UpdateTouchInput();
                    }
                    else if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    {
                        UpdateMouseInput();                   
                    }
                }
            
            }   // end if active

        }   // end of MouseMenu Update()

        private void UpdateTouchInput()
        {
            for (int j = 0; j < TouchInput.TouchCount; j++)
            {
                TouchContact touch = TouchInput.GetTouchContactByIndex(j);
                Vector2 touchHit = touch.position;

                // HitTest mouse vs elements.  Set selected state.
                curIndex = -1;
                for (int i = 0; i < itemList.Count; i++)
                {
                    if (itemList[i].HitBox.Contains(touchHit))
                    {
                        itemList[i].Selected = true;
                        curIndex = i;
                    }
                    else
                    {
                        itemList[i].Selected = false;
                    }
                }

                if (TouchInput.WasReleased )
                {
                    // If released while over an item, then select that item.
                    if (curIndex != -1)
                    {
                        if (onSelect != null)
                        {
                            Foley.PlayPressA();
                            onSelect(this);
                            chosenIndex = curIndex;
                        }
                        itemList[CurIndex].Selected = false;
                        if (CurIndex != -1)
                        {
                            CurIndex = -1;
                        }
                        Deactivate();
                        return;
                    }

                    // If released near origin, then we assume that the user wants to click on items seperately so do nothing.
                    // If released elsewhere, treat this as a cancel, but only if its a tap release.
                    float dist = (touchHit - position).Length();
                    if (dist > originRadius)
                    {
                        if (TouchGestureManager.Get().TapGesture.WasTapped())
                        {
                            // Cancel.
                            if (onCancel != null)
                            {
                                onCancel(this);
                            }
                            if (CurIndex != -1)
                            {
                                itemList[CurIndex].Selected = false;
                            }
                            CurIndex = -1;
                            chosenIndex = -1;
                            Deactivate();
                        }
                    }
                }
            }
        }

        private void UpdateMouseInput()
        {
            Vector2 mouseHit = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);

            // HitTest mouse vs elements.  Set selected state.
            curIndex = -1;
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].HitBox.Contains(mouseHit))
                {
                    itemList[i].Selected = true;
                    curIndex = i;
                }
                else
                {
                    itemList[i].Selected = false;
                }
            }

            if (MouseInput.Left.WasReleased || MouseInput.Right.WasReleased)
            {
                // If released while over an item, then select that item.
                if (curIndex != -1)
                {
                    if (onSelect != null)
                    {
                        Foley.PlayPressA();
                        onSelect(this);
                        chosenIndex = curIndex;
                    }
                    itemList[CurIndex].Selected = false;
                    if (CurIndex != -1)
                    {
                        CurIndex = -1;
                    }
                    Deactivate();
                    return;
                }

                // If released near origin, then we assume that the user wants to click on items seperately so do nothing.
                // If released elsewhere, treat this as a cancel.
                float dist = (mouseHit - position).Length();
                if (dist > originRadius)
                {
                    // Cancel.
                    if (onCancel != null)
                    {
                        onCancel(this);
                    }
                    if (CurIndex != -1)
                    {
                        itemList[CurIndex].Selected = false;
                    }
                    CurIndex = -1;
                    chosenIndex = -1;
                    Deactivate();
                    return;
                }
            }

        }

        private void CalcLayout()
        {
            // For all of the items in the list, let them calc their size.
            float maxWidth = 0.0f;
            for (int i = 0; i < itemList.Count; i++)
            {
                itemList[i].CalcSize();
                maxWidth = Math.Max(maxWidth, itemList[i].Size.X);
            }

            // Now set all the widths equal
            for (int i = 0; i < itemList.Count; i++)
            {
                Vector2 size = itemList[i].Size;
                size.X = maxWidth;
                itemList[i].Size = size;
            }

            // Layout menu.
            float height = itemList.Count / 2.0f * itemList[0].Size.Y;
            Vector2 pos = new Vector2(50.0f, 20.0f - height);
            for (int i = 0; i < itemList.Count; i++)
            {
                itemList[i].Position = pos;
                pos.Y += itemList[i].Size.Y;
            }

            /*
            // Radial curve version.
            float radius = 120.0f;
            Vector2 centerOffset = new Vector2(-50.0f, 0.0f);
            float dtheta = (float)Math.Asin(itemList[0].Size.Y / radius);
            float theta = -itemList.Count * 0.7f * dtheta;

            for (int i = 0; i < itemList.Count; i++)
            {
                itemList[i].Position = centerOffset + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
                theta = (float)Math.Asin((itemList[i].Position.Y + itemList[i].Size.Y) / radius);
            }
            */



            // Calc extent of full menu.
            AABB2D menuBox = new AABB2D(itemList[0].HitBox);
            for (int i = 1; i < itemList.Count; i++)
            {
                menuBox.Union(itemList[i].HitBox);
            }

            // Check if this fits within overscan area.  If not, move the menu.
            Vector2 safeMin = Vector2.Zero;
            Vector2 safeMax = BokuGame.ScreenSize;
            Vector2 curPos = Position;
            if (safeMin.X > menuBox.Min.X)
            {
                curPos.X += safeMin.X - menuBox.Min.X + 1;
            }
            else if (safeMax.X < menuBox.Max.X)
            {
                curPos.X -= menuBox.Max.X - safeMax.X + 1;
            }
            if (safeMin.Y > menuBox.Min.Y)
            {
                curPos.Y += safeMin.Y - menuBox.Min.Y + 1;
            }
            else if (safeMax.Y < menuBox.Max.Y)
            {
                curPos.Y -= menuBox.Max.Y - safeMax.Y + 1;
            }
            // Update position.
            Position = curPos;
            
        }   // end of LayoutMenu()


        public void Render()
        {
            if (Active)
            {
                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                batch.Begin();

                for (int i = 0; i < itemList.Count; i++)
                {
                    itemList[i].Render(batch);
                }

                batch.End();
            }

        }   // end of MouseMenu Render()

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
                    dirty = true;
                    return true;
                }
            }
            return false;
        }   // end of MouseMenu SetValue()

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

        public void Activate(Vector2 position)
        {
            if(!active)
            {
                // clear last chosen index
                chosenIndex = -1;

                this.position = position;
                this._position = position;

                active = true;
                CommandStack.Push(commandMap);
                Boku.Common.HelpOverlay.Push(helpOverlay);

                dirty = true;
                Update();
            }
        }

        public void Deactivate()
        {
            if(active)
            {

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                {
                    if (TouchInput.TouchCount == 1 && TouchInput.GetOldestTouch().TouchedObject == null)
                    {
                        TouchInput.GetOldestTouch().TouchedObject = this;
                    }

                }
                active = false;
                CommandStack.Pop(commandMap);
                Boku.Common.HelpOverlay.Pop();
            }
        }

        public static void LoadContent(bool immediate)
        {
            // Load the textures.
            if (roundedSquare == null)
            {
                roundedSquare = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\RoundedSquare");
            }
        }   // end of LoadContent()

        public static void InitSharedDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            BokuGame.Release(ref roundedSquare);
        }   // end of MouseMenu UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class MouseMenu

}   // end of namespace Boku.UI2D






