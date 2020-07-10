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
using Boku.Common.Gesture;
using Boku.Input;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// Menu designed to match the new modular tweak screen UI elements.
    /// </summary>
    public class ModularMenu : INeedsDeviceReset
    {
        public static Color titleTextColor = new Color(127, 127, 127);      // For titles on white background.
        public static Color valueTextColor = new Color(140, 200, 63);       // For slider values.
        public static Color shadowColor = new Color(0, 0, 0, 20);
        public static Vector2 shadowOffset = new Vector2(0, 6);
        public static Color unselectedTextColor = new Color(200, 200, 200);
        public static Color selectedTextColor = new Color(20, 20, 20);

        public static UI2D.Shared.GetFont TitleFont = UI2D.Shared.GetGameFont24Bold;
        public static UI2D.Shared.GetFont ItemFont = UI2D.Shared.GetGameFont18Bold;
        public static int additionalLineSpacing = 8;

        public class MenuItem
        {
            #region Members

            private ModularMenu parent = null;

            private string text;
            private Vector4 textColor = unselectedTextColor.ToVector4();    // Displayed color.
            private Vector4 _textColor = unselectedTextColor.ToVector4();   // Twitch target color.
            private float barAlpha = 0.0f;                                  // Display alpha for highlight bar.
            private float _barAlpha = 0.0f;                                 // Twitch target alpha.

            private AABB2D uvBoundingBox = new AABB2D();                    // Needs to be filled in when rt is refreshed.

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
                            float time = 0.2f;
                            {
                                _textColor = ModularMenu.selectedTextColor.ToVector4();
                                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = val; parent.dirty = true; };
                                TwitchManager.CreateTwitch<Vector4>(textColor, _textColor, set, time, TwitchCurve.Shape.EaseOut);
                            }
                            {
                                _barAlpha = 1.0f;
                                TwitchManager.Set<float> set = delegate(float val, Object param) { barAlpha = val; parent.dirty = true; };
                                TwitchManager.CreateTwitch<float>(barAlpha, _barAlpha, set, time, TwitchCurve.Shape.EaseOut);
                            }
                        }
                        else
                        {
                            // Twitch to unselected colors.
                            float time = 0.2f;
                            {
                                _textColor = ModularMenu.unselectedTextColor.ToVector4();
                                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { textColor = val; parent.dirty = true; };
                                TwitchManager.CreateTwitch<Vector4>(textColor, _textColor, set, time, TwitchCurve.Shape.EaseIn);
                            }
                            {
                                _barAlpha = 0.0f;
                                TwitchManager.Set<float> set = delegate(float val, Object param) { barAlpha = val; parent.dirty = true; };
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

            public AABB2D UVBoundingBox
            {
                get { return uvBoundingBox; }
                set { uvBoundingBox = value; }
            }

            public string Text
            {
                get { return text; }
            }

            #endregion

            #region Public

            public MenuItem(string text, ModularMenu parent)
            {
                this.text = TextHelper.FilterInvalidCharacters(text);
                this.parent = parent;
            }

            #endregion
        }

        public delegate void UIModularMenuEvent(ModularMenu menu);

        private UIModularMenuEvent onCancel = null;
        private UIModularMenuEvent onChange = null;
        private UIModularMenuEvent onSelect = null;

        private static Effect effect = null;

        private RenderTarget2D diffuse = null;
        private Texture2D normalMap = null;
        private string normalMapName = null;

        private Texture2D whiteTop = null;
        private Texture2D whiteHighlight = null;
        private Texture2D blackHighlight = null;
        private Texture2D greenBar = null;

        private int curIndex = 0;   // The currently selected option.

        // Properties for the underlying 9-grid geometry.
        private float width;
        private float height;
        private float edgeSize;

        private Base9Grid geometry = null;
        private Matrix worldMatrix = Matrix.Identity;
        private Matrix invWorldMatrix = Matrix.Identity;

        private bool active = false;

        private Vector4 specularColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private float specularPower = 8.0f;

        private static Point margin = new Point(32, 12);    // Margin used by individual TextLines.
        private UIGridElement.Justification justify = UIGridElement.Justification.Center;

        private string title = null;
        private List<MenuItem> itemList = null; // The list of options.

        private bool dirty = true;              // Texture2D needs to be refreshed.

        private CommandMap commandMap = new CommandMap("ModularMenu");  // Looking forward to the day when this is gone.
        private string helpOverlay = "ModularMenu";

        private bool acceptStartForCancel = false;      // Special case code used for mini-hub allows the Start button to toggle the mini-hub.
        private bool useRtCoords = false;        // If the menu is being rendered into a rendertarget which is then
                                                        // scaled to compensate for overscan this should be set to true.
                                                        // Currently for MainMenu this is true, for MiniHub this is false.

        #region Accessors

        public bool Active
        {
            get { return active; }
            set
            {
                if (active != value)
                {
                    active = value;
                    if (active)
                    {
                        CommandStack.Push(commandMap);
                        Boku.Common.HelpOverlay.Push(helpOverlay);
                    }
                    else
                    {
                        CommandStack.Pop(commandMap);
                        Boku.Common.HelpOverlay.Pop();
                    }
                    dirty = true;
                }
            }
        }

        /// <summary>
        /// If true, no input is processed but rendering is not affected.
        /// Current item may still be changed programmatically.
        /// </summary>
        public bool IgnoreInput { get; set; }

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
                    dirty = true;
                }
            }
        }

        public string CurString
        {
            get { return itemList[CurIndex].Text; }
        }

        public int Count
        {
            get { return itemList.Count; }
        }

        /// <summary>
        /// Delegate to be called when the selection is changed.
        /// </summary>
        public UIModularMenuEvent OnChange
        {
            set { onChange = value; }
        }

        /// <summary>
        /// Delegate to be called when user backs out.
        /// </summary>
        public UIModularMenuEvent OnCancel
        {
            set { onCancel = value; }
        }

        /// <summary>
        /// Delegate to be called when the selection is chosen.
        /// </summary>
        public UIModularMenuEvent OnSelect
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
        /// Special case flag which tells the menu to also accept the 
        /// start button for cancelling as well as the B button.
        /// Only used to allow toggling of the mini-hub.
        /// </summary>
        public bool AcceptStartForCancel
        {
            get { return acceptStartForCancel; }
            set { acceptStartForCancel = value; }
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
        /// If the menu is being rendered into a rendertarget which is then
        /// scaled to fit the screen this should be set to true.
        /// Currently for MainMenu this is true, for MiniHub this is false.
        /// Defaults to false.
        /// </summary>
        public bool UseRtCoords
        {
            get { return useRtCoords; }
            set { useRtCoords = value; }
        }

        /// <summary>
        /// Width of menu in world units.
        /// </summary>
        public float Width
        {
            get { return width; }
        }

        /// <summary>
        /// Height of menu in world units.
        /// </summary>
        public float Height
        {
            get { return height; }
        }

        #endregion

        // c'tor
        /// <summary>
        /// Simple c'tor using a blob to hold the common data.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="label"></param>
        public ModularMenu(UIGridElement.ParamBlob blob, string title)
        {
            this.title = TextHelper.FilterInvalidCharacters(title);
            if (this.title == "")
            {
                this.title = null;
            }

            // blob
            this.width = blob.width;
            this.height = blob.height;
            this.edgeSize = blob.edgeSize;

            this.normalMapName = blob.normalMapName;

            this.justify = blob.justify;

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
        }   // end of UIGridModularMenu AddText()

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

            InitDeviceResources(BokuGame.bokuGame.GraphicsDevice);

            // Ensure that the selected state of all items is correct.
            for (int i = 0; i < itemList.Count; i++)
            {
                itemList[i].Selected = i == CurIndex;
            }

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
                InitDeviceResources(BokuGame.bokuGame.GraphicsDevice);

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

        public void Update(Camera camera, ref Matrix parentMatrix)
        {
            // Check for input.
            if (active && itemList.Count > 1 && !IgnoreInput && CommandStack.Peek() == commandMap)
            {
                bool selectionChanged = false;
                bool moveUp = false;
                bool moveDown = false;

                {
                    // Mouse input
                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
                    {
                        // If the mouse is over the menu, move the selection index to the item under the mouse.
                        // On mouse down, make the item (if any) under the mouse the ClickedOnItem.
                        // On mouse up, if the mouse is still over the ClickedOnItem, activate it.  If not, just clear ClickedOnItem. 

                        Vector2 hitUV = MouseInput.GetHitUV(camera, ref invWorldMatrix, width, height, useRtCoords);

                        // See if we're over anything.  If so, set that item to being selected but only if we've moved the mouse.
                        // This prevents the menu from feeling 'broken' if the mouse is over it and the user tries to use
                        // the gamepad or keyboard.
                        int mouseOverItem = -1;
                        for (int i = 0; i < itemList.Count; i++)
                        {
                            if (itemList[i].UVBoundingBox != null && itemList[i].UVBoundingBox.Contains(hitUV))
                            {
                                // Only update the current in-focus element when the mouse moves.
                                if (MouseInput.Position != MouseInput.PrevPosition)
                                {
                                    CurIndex = i;
                                }
                                mouseOverItem = i;
                            }
                        }

                        if (MouseInput.Left.WasPressed && mouseOverItem != -1)
                        {
                            MouseInput.ClickedOnObject = itemList[mouseOverItem];
                        }

                        if (MouseInput.Left.WasReleased && mouseOverItem != -1 && MouseInput.ClickedOnObject == itemList[mouseOverItem])
                        {
                            // Normally this is already set except in the case where the app didn't have focus and a menu item is clicked.
                            // In that case, the CurIndex value stays stuck at its previous position.  In particular this was causing Kodu's
                            // Main Menu to think RESUME was clicked even when it was one of the other menu items.
                            CurIndex = mouseOverItem;
                            if (onSelect != null)
                                onSelect(this);
                            Foley.PlayPressA();
                            return;
                        }

                        // Allow scroll wheel to cycle through elements.
                        int wheel = MouseInput.ScrollWheel - MouseInput.PrevScrollWheel;

                        if (wheel > 0)
                        {
                            moveUp = true;
                        }
                        else if (wheel < 0)
                        {
                            moveDown = true;
                        }

                    }   // end if KeyboardMouse mode.

                    if (GamePadInput.ActiveMode == GamePadInput.InputMode.Touch)
                    {
                        // Look for Tap gesture first.  No need to care about touch response if we've got a tap.
                        bool goodTapGesture = false;
                        if (TouchGestureManager.Get().TapGesture.WasTapped())
                        {
                            // Check if tap position hit any of the menu elements.
                            Vector2 tapPosition = TouchGestureManager.Get().TapGesture.Position;
                            Vector2 touchHitUV = TouchInput.GetHitUV(tapPosition, camera, ref invWorldMatrix, width, height, useRtCoords);
                            for (int index = 0; index < itemList.Count; index++)
                            {
                                if (itemList[index].UVBoundingBox != null && itemList[index].UVBoundingBox.Contains(touchHitUV))
                                {
                                    CurIndex = index;
                                    if (onSelect != null)
                                    {
                                        onSelect(this);
                                        Foley.PlayPressA();
                                        selectionChanged = true;
                                        goodTapGesture = true;
                                    }
                                }
                            }
                        }

                        // Look for touch events to change focus.  Ignore this if we had a good tap gesture.
                        if (!goodTapGesture)
                        {
                            for (int i = 0; i < TouchInput.TouchCount; i++)
                            {
                                TouchContact touch = TouchInput.GetTouchContactByIndex(i);

                                // Touch input
                                // If the user touched the menu, move the selection index to the item under the touch.
                                // On touch down, make the item (if any) under the contact the touchedItem.
                                // On touch up, if the touch is still over the touchedItem, activate it.  If not, just clear touchedItem. 

                                Vector2 touchHitUV = TouchInput.GetHitUV(touch.position, camera, ref invWorldMatrix, width, height, useRtCoords);

                                // See what UI element we are over, if anything.  If it hits, set that item to being selected but only if the touch has moved.
                                // This emulates how the mouse is being handled by allowing the input to change only if the touch has moved. If a user moves 
                                // the gamepad or keyboard but doesn't move their touch contact, then the selection won't flicker back and forth and feel 'broken'
                                int touchOverIndex = -1;
                                for (int index = 0; index < itemList.Count; index++)
                                {
                                    if (itemList[index].UVBoundingBox != null && itemList[index].UVBoundingBox.Contains(touchHitUV))
                                    {
                                        touchOverIndex = index;
                                    }
                                }

                                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
                                {
                                    // Touch is down!
                                    if (touchOverIndex != -1)
                                    {
                                        touch.TouchedObject = itemList[touchOverIndex];
                                        if (CurIndex != touchOverIndex)
                                            selectionChanged = true;
                                        CurIndex = touchOverIndex;
                                    }
                                }
                                else if (touch.phase == TouchPhase.Moved)
                                {
                                    // Touch moved!!! Update Something!!
                                    if (CurIndex != touchOverIndex)
                                    {
                                        CurIndex = touchOverIndex;
                                    }
                                }
                            }
                        }   // end if not good tap gesture.

                        if (selectionChanged)
                        {
                            if (onChange != null)
                                onChange(this);
                            dirty = true;
                            //RecalcPositions();
                            // Ensure that the selected state of all items is correct.
                            for (int i = 0; i < itemList.Count; i++)
                            {
                                itemList[i].Selected = i == CurIndex;
                            }
                        }
                    }   // end of touch mode processing.

                }

                if (GamePadInput.ActiveMode == GamePadInput.InputMode.GamePad ||
                    GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse )
                {
                    // Gamepad / Keyboard input.
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    if ((AcceptStartForCancel && Actions.Start.WasPressed) || Actions.Cancel.WasPressed)
                    {
                        Actions.Cancel.ClearAllWasPressedState();
                        if (onCancel != null)
                            onCancel(this);
                        Foley.PlayBack();
                        return;
                    }

                    if (Actions.Select.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState();
                        if (onSelect != null)
                            onSelect(this);
                        Foley.PlayPressA();
                        return;
                    }

                    int prevIndex = CurIndex;

                    // Handle input changes here.
                    if (Actions.ComboDown.WasPressedOrRepeat || moveDown)
                    {
                        ++CurIndex;
                        if (CurIndex >= itemList.Count)
                        {
                            CurIndex = 0;
                        }

                        selectionChanged = true;
                    }

                    if (Actions.ComboUp.WasPressedOrRepeat || moveUp)
                    {
                        --CurIndex;
                        if (CurIndex < 0)
                        {
                            CurIndex = itemList.Count - 1;
                        }

                        selectionChanged = true;
                    }

                    if (selectionChanged)
                    {
                        if (onChange != null)
                            onChange(this);
                        dirty = true;
                        //RecalcPositions();
                    }

                    // Ensure that the selected state of all items is correct.
                    for (int i = 0; i < itemList.Count; i++)
                    {
                        itemList[i].Selected = i == CurIndex;
                    }
                }
            }

            RefreshTexture();

        }   // end of UIGridModularMenu Update()

        public void RefreshTexture()
        {
            if (dirty || diffuse.IsContentLost)
            {
                int w, h;
                GetWH(out w, out h);

                // If the number of elements has changed, we need a new rendertarget.
                InitDeviceResources(BokuGame.bokuGame.GraphicsDevice);

                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                InGame.SetRenderTarget(diffuse);
                InGame.Clear(Color.Transparent);

                // The thin margin around the highlight where the normal color shows through.
                int highlightMargin = 5;

                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                // Render the white background.  Alpha on normal map is used to round corners.
                Vector2 position = Vector2.Zero;
                Vector2 size = new Vector2(w, h);

                if (title != null)
                {
                    quad.Render(Vector4.One, position, size);
                    position.Y = 70;
                }

                // And the black parts.
                size.Y = whiteTop.Height;
                quad.Render(whiteTop, new Vector4(0, 0, 0, 1), position, size, "TexturedRegularAlpha");

                position.Y += 16;
                size.Y = h - position.Y;
                quad.Render(new Vector4(0, 0, 0, 1), position, size);

                // Disable writing to alpha channel.
                // This prevents transparent fringing around the text.
                device.BlendState = UI2D.Shared.BlendStateColorWriteRGB;

                // Add the highlight/shadow onto the white region.
                if (title != null)
                {
                    position.Y = 25;
                    size.Y = 48;
                    quad.Render(whiteHighlight, new Vector4(0.6f, 1.0f, 0.8f, 0.2f), position + new Vector2(highlightMargin, 0), size + new Vector2(-2 * highlightMargin, -highlightMargin), "TexturedRegularAlpha");
                }

                // Render the label and value text into the texture.

                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                batch.Begin();

                // Title.
                UI2D.Shared.GetFont Font = UI2D.Shared.GetGameFont24Bold;
                if (title != null)
                {
                    position.X = TextHelper.CalcJustificationOffset(margin.X, w, (int)Font().MeasureString(title).X, justify);
                    position.Y = (int)((64 - Font().LineSpacing) / 2.0f) + 4;
                    TextHelper.DrawString(Font, title, position + shadowOffset, shadowColor);
                    TextHelper.DrawString(Font, title, position, titleTextColor);
                }

                // Entries.
                Font = UI2D.Shared.GetGameFont18Bold;
                position.Y = 8 + (title != null ? 70 : 0);
                Vector2 min = Vector2.Zero;
                Vector2 max = Vector2.One;
                for (int i = 0; i < itemList.Count; i++)
                {
                    // Render bar.
                    Vector4 barColor = active ? new Vector4(1, 1, 1, itemList[i].BarAlpha) : new Vector4(1, 0.5f, 1, itemList[i].BarAlpha);
                    quad.Render(greenBar, barColor, new Vector2(8, position.Y - additionalLineSpacing / 3), new Vector2(w - 16, Font().LineSpacing + additionalLineSpacing), "TexturedRegularAlpha");

                    // Render text.
                    position.X = TextHelper.CalcJustificationOffset(margin.X, w, (int)Font().MeasureString(itemList[i].Text).X, justify);
                    TextHelper.DrawString(Font, itemList[i].Text, position, itemList[i].TextColor);

                    min.Y = position.Y / h;

                    position.Y += Font().LineSpacing + additionalLineSpacing;

                    max.Y = position.Y / h;
                    itemList[i].UVBoundingBox.Set(min, max);
                }

                batch.End();

                // Add the highlight to the black region.
                position = new Vector2(highlightMargin, 1 + (title != null ? 70 : 0));
                size.X = w - 2 * highlightMargin;
                size.Y = 60;
                quad.Render(blackHighlight, new Vector4(1, 1, 1, 0.2f), position, size, "AdditiveBlendWithAlpha");

                // Restore write channels.
                device.BlendState = BlendState.NonPremultiplied;
 
                // Restore backbuffer.
                InGame.RestoreRenderTarget();

                dirty = false;
            }

        }   // end of UIGridModularMenu RefreshTexture()

        public void Render(Camera camera)
        {
            try
            {
                ShaderGlobals.SetValues(effect);
                ShaderGlobals.SetCamera(effect, camera);

                effect.CurrentTechnique = effect.Techniques["NormalMappedWithEnv"];
                effect.Parameters["DiffuseTexture"].SetValue(diffuse);

                effect.Parameters["WorldMatrix"].SetValue(worldMatrix);
                effect.Parameters["WorldViewProjMatrix"].SetValue(worldMatrix * camera.ViewProjectionMatrix);

                effect.Parameters["Alpha"].SetValue(1.0f);
                effect.Parameters["DiffuseColor"].SetValue(new Vector4(1, 1, 1, 1));
                effect.Parameters["SpecularColor"].SetValue(specularColor);
                effect.Parameters["SpecularPower"].SetValue(specularPower);

                effect.Parameters["NormalMap"].SetValue(normalMap);

                geometry.Render(effect);
            }
            catch
            {
                // This Try/Catch is here since the first frame back from a device reset
                // the call to diffuse fails saying that the rendertarget is
                // still set on the device.  As far as I can tell it's not but I still
                // can't win the argument.
            }

        }   // end of UIGridModularMenu Render()

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
                    CurIndex = i;
                    dirty = true;
                    return true;
                }
            }
            return false;
        }   // end of UIGridModularMenu SetValue()

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


        bool contentLoaded;
        public void LoadContent(bool immediate)
        {
            contentLoaded = true;

            // Init the effect.
            if (effect == null)
            {
                effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\UI2D");
                ShaderGlobals.RegisterEffect("UI2D", effect);
            }

            // Load the textures.
            if (whiteTop == null)
            {
                whiteTop = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\WhiteTop");
            }
            if (whiteHighlight == null)
            {
                whiteHighlight = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\WhiteHighlight");
            }
            if (blackHighlight == null)
            {
                blackHighlight = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\BlackHighlight");
            }
            if (greenBar == null)
            {
                greenBar = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\GridElements\GreenBar");
            }

            // Load the normal map texture.
            if (normalMapName != null)
            {
                normalMap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\" + normalMapName);
            }
        }

        private void GetWH(out int w, out int h)
        {
            w = 512;
            h = title != null ? 70 : 0;
            h += itemList.Count * (ItemFont().LineSpacing + additionalLineSpacing) + ItemFont().LineSpacing / 2;
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            if (!contentLoaded)
                return;

            int w, h;
            GetWH(out w, out h);

            height = h * width / w;
            // Create the geometry if new size is needed.
            if (geometry == null || geometry.Height != height || geometry.Width != width)
            {
                // Release any existing 9grid.
                BokuGame.Unload(geometry);

                geometry = new Base9Grid(width, height, edgeSize);

                BokuGame.Load(geometry, true);
            }

            if (diffuse == null || diffuse.Height != h)
            {
                ReleaseRenderTargets();
                CreateRenderTargets(device);
            }

            dirty = true;
        }

        public void UnloadContent()
        {
            ReleaseRenderTargets();

            BokuGame.Release(ref effect);
            BokuGame.Release(ref normalMap);
            BokuGame.Release(ref diffuse);
            BokuGame.Release(ref whiteTop);
            BokuGame.Release(ref whiteHighlight);
            BokuGame.Release(ref blackHighlight);
            BokuGame.Release(ref greenBar);

            BokuGame.Unload(geometry);
            geometry = null;
        }   // end of UIGridModularMenu UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            ReleaseRenderTargets();
            CreateRenderTargets(device);
        }

        private void CreateRenderTargets(GraphicsDevice device)
        {
            int w, h;
            GetWH(out w, out h);

            diffuse = new RenderTarget2D(device, 512, h, false, SurfaceFormat.Color, DepthFormat.None);
            InGame.GetRT("ModularMenu", diffuse);

            dirty = true;
            RefreshTexture();
        }

        private void ReleaseRenderTargets()
        {
            InGame.RelRT("ModularMenu", diffuse);
            BokuGame.Release(ref diffuse);
        }

    }   // end of class UIGridModularMenu

}   // end of namespace Boku.UI2D






