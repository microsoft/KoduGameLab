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

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Fx;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;
using Boku.SimWorld.Terra;
using Boku.Scenes.InGame.MouseEditTools;
using Boku.Scenes.InGame.Tools;
using Boku.Common.Gesture;

namespace Boku
{
    /// <summary>
    /// Tool bar as displayed in MouseEdit mode.
    /// </summary>
    public class ToolBar : INeedsDeviceReset
    {
        public class ToolBarSubElement
        {
            #region Members

            private Texture2D texture = null;
            private BasePicker picker = null;           // The picker to activate when this sub-menu is picked.
            private ToolBarElement parent = null;

            private Vector2 maxOffset = Vector2.Zero;   // Offset when fully deployed.
            private Vector2 minOffset = Vector2.Zero;   // Offset when hidden.

            private AABB2D hitBox = new AABB2D();

            #endregion

            #region Accessors

            // Mouse hit box for hover/selection.
            // Units are pixels in coord system of rendertarget.
            public AABB2D HitBox
            {
                get { return hitBox; }
            }

            public BasePicker Picker
            {
                get { return picker; }
            }

            #endregion

            #region Public

            public ToolBarSubElement(Texture2D texture, BasePicker picker, ToolBarElement parent, Vector2 maxOffset, Vector2 minOffset)
            {
                this.texture = texture;
                this.picker = picker;
                this.parent = parent;
                this.maxOffset = maxOffset;
                this.minOffset = minOffset;
            }

            /// <summary>
            /// Check if the mouse clicked this element.  If so, un-hide the associated picker.
            /// Return true if picker is un-hid.
            /// </summary>
            /// <param name="mouseHit"></param>
            public bool CheckMouseHit(Vector2 mouseHit)
            {
                bool result = false;

                if (hitBox.LeftPressed(mouseHit))
                {
                    if (picker != null)
                    {
                        picker.Hidden = false;
                        result = true;
                    }
                }

                return result;
            }

            /// <summary>
            /// Check if a touch tapped this element.  If so, un-hide the associated picker.
            /// Return true if picker is un-hid.
            /// </summary>
            /// <param name="touchHit"></param>
            public bool CheckTouchHit(TouchContact touch, Vector2 touchHit)
            {
                bool result = false;

                if (hitBox.Touched(touch, touchHit, false))
                {
                    if (picker != null)
                    {
                        picker.Hidden = false;
                        result = true;
                    }
                }

                return result;
            }

            /// <summary>
            /// Render the submenu.
            /// </summary>
            /// <param name="parentPosition">Position of center of parent.  Rendering should be an offset from here.</param>
            /// <param name="t">0 == fully hidden, 1 == fully deployed</param>
            public void Render(Vector2 parentPosition, float t)
            {
                Vector2 pos = parentPosition + MyMath.Lerp(minOffset, maxOffset, t);
                Vector2 size = 1.0f * t * new Vector2(texture.Width, texture.Height);
                pos -= size / 2.0f;

                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                ssquad.Render(texture, pos, size, "TexturedRegularAlpha");

                // Set hit box position/size.  Shrink a bit to better fit art.
                pos += size / 2.0f;
                size *= 0.7f;
                pos -= size / 2.0f;
                hitBox.Set(pos, pos + size);

                // debug helper
                //ssquad.Render(new Vector4(1, 0, 0, 0.5f), pos, size);

            }   // end of Render()

            #endregion
        }
        
        /// <summary>
        /// Elements that live in the ToolBar.  All measurements are in pixels.
        /// </summary>
        public class ToolBarElement
        {
            #region Members

            private ToolBar parent = null;
            private string name = null;
            private InGame.BaseEditUpdateObj.ToolMode mode = InGame.BaseEditUpdateObj.ToolMode.CameraMove;
            private Vector2 position;
            private Vector2 size;
            private Vector2 _size;      // Actual value (as opposed to twitched one);
            private Texture2D texture;

            private AABB2D hitBox = new AABB2D();
            private bool visible = true;
            private bool disableForTouch = false;

            private ToolBarSubElement leftSub = null;
            private ToolBarSubElement rightSub = null;

            #endregion

            #region Accessors

            public Vector2 Position
            {
                get { return position; }
                set 
                {
                    if (position != value)
                    {
                        // No twitch here.  The value depends on the Size and we use that twitch for smoothing
                        position = value; 
                        parent.dirty = true;
                    }
                }
            }
            public Vector2 Size
            {
                get { return size; }
                set
                {
                    if (_size != value)
                    {
                        _size = value;
                        TwitchManager.Set<Vector2> set = delegate(Vector2 val, Object param) { size = val; parent.dirty = true; };
                        TwitchManager.CreateTwitch<Vector2>(size, value, set, 0.2f, TwitchCurve.Shape.EaseInOut);
                    }
                }
            }

            public Texture2D Texture
            {
                get { return texture; }
            }

            public bool Visible
            {
                get { return visible; }
                set { visible = value; }
            }

            public bool DisableForTouch
            {
                get { return disableForTouch; }
            }

            public string Name
            {
                get { return name; }
            }

            public InGame.BaseEditUpdateObj.ToolMode Mode
            {
                get { return mode; }
            }

            // Mouse hit box for hover/selection.
            // Units are pixels in coord system of rendertarget.
            public AABB2D HitBox
            {
                get { return hitBox; }
            }

            public ToolBar ToolBar
            {
                get { return parent; }
            }

            #endregion

            #region Public

            // c'tor
            public ToolBarElement(string name, InGame.BaseEditUpdateObj.ToolMode mode, Texture2D texture, ToolBar parent, bool disableForTouch)
            {
                this.name = name;
                this.mode = mode;
                this.texture = texture;
                this.parent = parent;

                this.disableForTouch = disableForTouch;

                size = new Vector2(64, 64);
            }   // end of c'tor

            public void AddLeftSubElement(Texture2D subTexture, BasePicker picker, float xCenter)
            {
                leftSub = new ToolBarSubElement(subTexture, picker, this, new Vector2(-50.0f + xCenter, -35.0f), new Vector2(0.0f, 0.0f));
            }   // end of AddLeftSubElement()

            public void AddRightSubElement(Texture2D subTexture, BasePicker picker, float xCenter)
            {
                rightSub = new ToolBarSubElement(subTexture, picker, this, new Vector2(50.0f + xCenter, -35.0f), new Vector2(0.0f, 0.0f));
            }   // end of AddLeftSubElement()

            public void Render(Vector2 pos)
            {
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();

                Position = pos;

                // Render reticule if needed.
                if (parent.RevertMode == Mode)
                {
                    // Add an offset so it fits around the tile rather than under it.
                    Vector2 offset = 0.2f * Size;
                    ssquad.Render(parent.ReticuleTexture, pos - offset, Size + 2.0f * offset, "AdditiveBlendWithAlpha");
                }

                // Render any sub elements.
                float t = (size.X - 64.0f) / 64.0f;
                if (leftSub != null)
                {
                    leftSub.Render(pos + size / 2.0f, t);
                }
                if (rightSub != null)
                {
                    rightSub.Render(pos + size / 2.0f, t);
                }

                // Render this element.
                ssquad.Render(Texture, pos, Size, "TexturedRegularAlpha");

                HitBox.Set(pos, pos + Size);

            }   // end of Render()

            /// <summary>
            /// Check sub elements for touch hits.  Return true if a picker was activated.
            /// </summary>
            /// <param name="mouseHit"></param>
            /// <returns></returns>
            public bool CheckTouchSubElementActivation(TouchContact touch, Vector2 touchHit)
            {
                bool result = false;

                if (leftSub != null && leftSub.CheckTouchHit(touch, touchHit))
                {
                    // Turn off other picker if needed.
                    if (rightSub != null && rightSub.Picker != null)
                    {
                        rightSub.Picker.Hidden = true;
                    }
                    result = true;
                }
                if (rightSub != null && rightSub.CheckTouchHit(touch, touchHit))
                {
                    // Turn off other picker if needed.
                    if (leftSub != null && leftSub.Picker != null)
                    {
                        leftSub.Picker.Hidden = true;
                    }
                    result = true;
                }

                return result;
            }   // end of CheckTouchSubElementActivation()

            /// <summary>
            /// Check sub elements for mouse hits.  Return true if a picker was activated.
            /// </summary>
            /// <param name="mouseHit"></param>
            /// <returns></returns>
            public bool CheckSubElementActivation(Vector2 mouseHit)
            {
                bool result = false;

                if (leftSub != null && leftSub.CheckMouseHit(mouseHit))
                {
                    // Turn off other picker if needed.
                    if (rightSub != null && rightSub.Picker != null)
                    {
                        rightSub.Picker.Hidden = true;
                    }
                    result = true;
                }
                if (rightSub != null && rightSub.CheckMouseHit(mouseHit))
                {
                    // Turn off other picker if needed.
                    if (leftSub != null && leftSub.Picker != null)
                    {
                        leftSub.Picker.Hidden = true;
                    }
                    result = true;
                }

                return result;
            }   // end of CheckSubElementActivation()

            #endregion
        }

        
        /// <summary>
        /// Touch buttons associated with the ToolBar paint options/actions
        /// </summary>
        public class TouchControls
        {
            #region Members

            public enum BrushActionIDs
            {
                baPaintMaterial,
                baBrushMore,
                baBrushLess,
                baBrushCubic,
                baBrushSmooth,
                baTerrainRaise,
                baTerrainLower,
                baWaterRaise,
                baWaterLower,
                baSmooth,
                baFlatten,
                baSpikey,
                baHilly,
                baDelete,
                baNode,
                baAllPath, //operate on the entire path
                baNormalPath, //operate on a single path element
                baUndoButton,
                baRedoButton,
                NUMBER_OF_Buttons
            }

            /// <summary>
            ///  Detail of paint action buttons, including image resources
            /// </summary>
            class BrushActionButton
            {
                public bool isToggleBtn = false;
                private bool toggleOn = false;
                public Button button = null;
                public BrushActionIDs id;
                public bool visible = false;

                #region Accessors
                public bool ToggleOn
                {
                    get
                    {
                        return isToggleBtn ? toggleOn : false;
                    }
                    set
                    {
                        if (isToggleBtn)
                        {
                            toggleOn = value;
                        }
                    }
                }

                #endregion

                public BrushActionButton()
                {
                }

                public BrushActionButton(BrushActionIDs id, Button btn)
                {
                    this.id     = id;
                    this.button = btn;
                    this.isToggleBtn = false;
                }
            }

            private List<BrushActionButton> brushOptionButtons = null;
            private BrushActionButton undoButton = null;
            private BrushActionButton redoButton = null;

            private int currentToggleBtnIdx = -1;
            /// <summary>
            ///  Spaceing data for drawing buttons.
            /// </summary>
            private int pixelsFromEdge = 30;
            private int buttonGap = 8; //16;
            private int totalBtnLength = 0;
            private int totalBtnGap = 0;

            private int extraDeadSpace = 26;
            private int DefaultActionBtnWidth = 76;
            private int DefaultActionBtnHeight = 76;


            #endregion

            #region Accessors

            /// <summary>
            /// What is this an index into and if they really are toggle
            /// buttons why do we have an index?  Shouldn't they all be
            /// toggleable independently?
            /// </summary>
            public int CurrentToggleIndex
            {
                get { return currentToggleBtnIdx; }
            }

            private Vector2 ActionBtnSize
            {
                get {
                    // TODO (****) *** Does all the code support this changing????
                    if (BokuGame.ScreenSize.Y <= 800)
                        return new Vector2(42.0f, 42.0f);
                    if (BokuGame.ScreenSize.Y < 1024)                    
                        return new Vector2(64.0f, 64.0f);
                    else
                        return new Vector2(DefaultActionBtnWidth, DefaultActionBtnHeight); 
                }
            }

            #endregion

            #region Private
                private void RemoveButton(int buttonIndex)
                {
                    totalBtnLength -= (int)brushOptionButtons[buttonIndex].button.GetSize().X;
                    totalBtnGap -= buttonGap;
                    brushOptionButtons.RemoveAt(buttonIndex);
                }

                private void CreateButton(BrushActionIDs baId, string text, string path, UI2D.Shared.GetFont font, bool toggleButton, Vector2 labelOffset)
                {
                    Texture2D image = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + path);
                       
                    Color color= new Color(0.7f,0.7f,0.7f);
                    GetTexture getTexture = delegate() { return image; };
                    Button btn = new Button(text, color, getTexture, font);
                    btn.FixedSize = ActionBtnSize;
                    btn.UseFixedSize = true;

                    BrushActionButton brushActionBtn = new BrushActionButton(baId, btn);
                    brushActionBtn.isToggleBtn = toggleButton;
                    brushActionBtn.button.SetHoverState(new Vector2(-1.0f,-1.0f));
                    brushActionBtn.button.LabelOffset = labelOffset;

                    brushOptionButtons.Add(brushActionBtn);

                    if (brushOptionButtons.Count > 0)
                    {
                        totalBtnLength += (int)brushOptionButtons[brushOptionButtons.Count - 1].button.GetSize().X;
                    }
                }
   

            #endregion

            #region Public

                public void LoadTouchPaintButtonContent()
                {
                    UI2D.Shared.GetFont font = null;
                    UI2D.Shared.GetFont labelFont = UI2D.Shared.GetGameFont15_75;

                    //----------------------------------
                    //NOTE: The order the buttons are created in will be the order the buttons are displayed.

                    //Painting
                    CreateButton(BrushActionIDs.baPaintMaterial, "", @"Textures\ToolMenu\brushAction_paint", font, true, Vector2.Zero);
                    CreateButton(BrushActionIDs.baBrushCubic, "", @"Textures\ToolMenu\brushAction_brushCubic", font, false, Vector2.Zero);
                    CreateButton(BrushActionIDs.baBrushSmooth, "", @"Textures\ToolMenu\brushAction_brushSmooth", font, false, Vector2.Zero);
                    
                    //Terrain
                    CreateButton(BrushActionIDs.baTerrainRaise, "", @"Textures\ToolMenu\brushAction_terrainRaise", font, true, Vector2.Zero);
                    CreateButton(BrushActionIDs.baTerrainLower, "", @"Textures\ToolMenu\brushAction_terrainLower", font, true, Vector2.Zero);
                    CreateButton(BrushActionIDs.baSmooth, "", @"Textures\ToolMenu\brushAction_smooth", font, true, Vector2.Zero);
                    CreateButton(BrushActionIDs.baFlatten, "", @"Textures\ToolMenu\brushAction_flatten", font, true, Vector2.Zero);
                    CreateButton(BrushActionIDs.baSpikey, "", @"Textures\ToolMenu\brushAction_spikey", font, true, Vector2.Zero);
                    CreateButton(BrushActionIDs.baHilly, "", @"Textures\ToolMenu\brushAction_hilly", font, true, Vector2.Zero);

                    //Water
                    CreateButton(BrushActionIDs.baWaterRaise, "", @"Textures\ToolMenu\brushAction_waterRaise", font, true, Vector2.Zero);
                    CreateButton(BrushActionIDs.baWaterLower, "", @"Textures\ToolMenu\brushAction_waterLower", font, true, Vector2.Zero);
                    
                    //Path
                    CreateButton(BrushActionIDs.baAllPath, "", @"Textures\ToolMenu\brushAction_allPath", font, false, Vector2.Zero);
                    CreateButton(BrushActionIDs.baNormalPath, "", @"Textures\ToolMenu\brushAction_normalPath", font, false, Vector2.Zero);
                    CreateButton(BrushActionIDs.baNode, "", @"Textures\ToolMenu\brushAction_node", font, true, Vector2.Zero);
                    
                    //General Delete
                    CreateButton(BrushActionIDs.baDelete, "", @"Textures\ToolMenu\brushAction_delete", font, true, Vector2.Zero);

                    //Brush Size always at bottom of tool list.
                    CreateButton(BrushActionIDs.baBrushMore, "", @"Textures\ToolMenu\brushAction_brushBigger", font, false, Vector2.Zero);
                    CreateButton(BrushActionIDs.baBrushLess, "", @"Textures\ToolMenu\brushAction_brushSmaller", font, false, Vector2.Zero);



                    //Create the undo button
                    Texture2D image = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\Undo");
                       
                    Color color= new Color(0.7f,0.7f,0.7f);
                    GetTexture getTexture = delegate() { return image; };
                    Button undoBtn = new Button(Strings.Localize("undoStack.undo"), color, getTexture, labelFont);
                    undoBtn.FixedSize = ActionBtnSize;
                    undoBtn.UseFixedSize = true;
                    undoBtn.SetHoverState(new Vector2(-1.0f, -1.0f));
                    undoBtn.LabelOffset = new Vector2(-5.0f, ActionBtnSize.Y);

                    undoButton = new BrushActionButton(BrushActionIDs.baUndoButton, undoBtn);
                    undoButton.isToggleBtn = false;


                    //Create the redo button
                    Texture2D redoImage = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\Redo");

                    GetTexture redoGetTexture = delegate() { return redoImage; };
                    Button redoBtn = new Button(Strings.Localize("undoStack.redo"), color, redoGetTexture, labelFont);
                    redoBtn.FixedSize = ActionBtnSize;
                    redoBtn.UseFixedSize = true;
                    redoBtn.SetHoverState(new Vector2(-1.0f, -1.0f));
                    redoBtn.LabelOffset = new Vector2(-5.0f, ActionBtnSize.Y);

                    redoButton = new BrushActionButton(BrushActionIDs.baRedoButton, redoBtn);
                    redoButton.isToggleBtn = false;

                }


                public int GetButtonIndex(BrushActionIDs baId)
                {
                    for (int i = 0; i < brushOptionButtons.Count; i++)
                    {
                        if (brushOptionButtons[i].id == baId)
                        {
                            return i;
                        }
                    }
                    return -1;
                }

                public TouchControls()
                {
                    brushOptionButtons = new List<BrushActionButton>();
                }

                public bool IsToggled(BrushActionIDs baId)
                {
                    int buttonIndex = GetButtonIndex(baId);
                    if (buttonIndex < 0 || buttonIndex >= brushOptionButtons.Count || !brushOptionButtons[buttonIndex].visible)
                    {
                        return false;
                    }

                    return brushOptionButtons[buttonIndex].ToggleOn || brushOptionButtons[buttonIndex].button.IsPressed;

                }

                public void SetToggle( BrushActionIDs baId, bool toggledOn )
                {
                    int buttonIndex = GetButtonIndex(baId);
                    if (buttonIndex >= 0 && buttonIndex < brushOptionButtons.Count && brushOptionButtons[buttonIndex].visible)
                    {    
                        brushOptionButtons[buttonIndex].ToggleOn = toggledOn;

                        if( toggledOn )
                        {
                            currentToggleBtnIdx = buttonIndex;
                            brushOptionButtons[buttonIndex].button.SetHoverState( brushOptionButtons[buttonIndex].button.Box.Min );
                            InGame.inGame.shared.currentTouchAction = brushOptionButtons[buttonIndex].id;
                        }
                        else
                        {
                             brushOptionButtons[buttonIndex].button.SetHoverState( new Vector2(-1.0f, -1.0f) );
                        }
                    }
                }

                public bool IsTapped(BrushActionIDs baId, TouchContact touch, Vector2 adjustedTouchPos)
                {
                    int buttonIndex = GetButtonIndex(baId);
                    if (buttonIndex < 0 || buttonIndex >= brushOptionButtons.Count || !brushOptionButtons[buttonIndex].visible)
                    {
                        return false;
                    }

                    bool wasTouched = brushOptionButtons[buttonIndex].button.Box.Touched(touch, adjustedTouchPos);

                    if (wasTouched)
                    {
                        touch.TouchedObject = null;
                    }
                    return wasTouched;
                }

                public bool IsTouched(BrushActionIDs baId, TouchContact touch, Vector2 adjustedTouchPos)
                {
                    int buttonIndex = GetButtonIndex(baId);
                    if (buttonIndex < 0 || buttonIndex >= brushOptionButtons.Count || !brushOptionButtons[buttonIndex].visible)
                    {
                        return false;
                    }

                    return brushOptionButtons[buttonIndex].button.Box.Contains(adjustedTouchPos);
                }

                public void ClearPaintButtons()
                {
                    for (int i = 0; i < brushOptionButtons.Count; i++)
                    {
                        brushOptionButtons[i].ToggleOn = false;
                        if (brushOptionButtons[i].visible)
                        {
                            brushOptionButtons[i].button.SetHoverState(new Vector2(-1.0f, -1.0f));
                        }
                        brushOptionButtons[i].visible = false;
                    }
                    currentToggleBtnIdx = -1;
                    totalBtnLength = 0;
                    totalBtnGap = 0;
                }


                private void HideButton(int buttonIndex)
                {
                    if (buttonIndex>=0 && buttonIndex<brushOptionButtons.Count && brushOptionButtons[buttonIndex].visible)
                    {
                        brushOptionButtons[buttonIndex].visible = false;
                        brushOptionButtons[buttonIndex].button.SetHoverState(new Vector2(-1.0f, -1.0f));
                        totalBtnLength -= (int)brushOptionButtons[buttonIndex].button.GetSize().X;
                        totalBtnGap -= buttonGap;
                    }
                }

                public void ShowBrushActionButton(BrushActionIDs baId)
                {
                    for (int i = 0; i < brushOptionButtons.Count; i++)
                    {
                        if (brushOptionButtons[i].id == baId)
                        {
                            bool showit = true;
                            if (baId == BrushActionIDs.baBrushCubic || baId == BrushActionIDs.baBrushSmooth)
                            {
                                if (!TerrainMaterial.IsFabric(Terrain.CurrentMaterialIndex) && (baId == BrushActionIDs.baBrushSmooth))
                                {
                                    showit = false;
                                }
                                else if (TerrainMaterial.IsFabric(Terrain.CurrentMaterialIndex) && (baId == BrushActionIDs.baBrushCubic))
                                {
                                    showit = false;
                                }
                            }
                            else if (baId == BrushActionIDs.baNormalPath || baId == BrushActionIDs.baAllPath)
                            {
                                EditPathsTool tool = EditPathsTool.GetInstance() as EditPathsTool;
                                if (tool.ActOnPath && baId == BrushActionIDs.baNormalPath)
                                {
                                    showit = false;
                                }
                                else if (!tool.ActOnPath && baId == BrushActionIDs.baAllPath)
                                {
                                    showit = false;
                                }
                            }
                            else
                            {
                                brushOptionButtons[i].button.SetHoverState(new Vector2(-1.0f, -1.0f));
                            }

                            if (!brushOptionButtons[i].visible)
                            {
                                brushOptionButtons[i].visible = showit;

                                totalBtnLength += (int)brushOptionButtons[i].button.GetSize().X;
                                totalBtnGap += buttonGap;
                            }
                        }
                    }
                }

                /// <summary>
                /// Draw the touch version of the undo/redo buttons if any are needed
                /// </summary>
                public void ShowRedoUndo()
                {
                    undoButton.visible = InGame.UnDoStack.HaveUnDo;
                    redoButton.visible = InGame.UnDoStack.HaveReDo;
                }

                public bool IsOverUIButton(TouchContact touch)
                {
                    if (IsOverUIDeadZone(touch))
                        return true;


                    for (int i = 0; i < brushOptionButtons.Count; i++)
                    {
                        if (brushOptionButtons[i].visible)
                        {
                            if (brushOptionButtons[i].button.Box.Contains(touch.position) || brushOptionButtons[i].button.Box == touch.TouchedObject )
                            {
                                return true;
                            }
                        }
                    }

                    //check undo/redo
                    if (undoButton.button.Box.Contains(touch.position) || redoButton.button.Box.Contains(touch.position))
                    {
                        return true;
                    }

                    return false;
                }

                public bool IsOverUIDeadZone(TouchContact touch)
                {
                    Vector2 topLeft = new Vector2(0.0f);
                    Vector2 bottomRight = ActionBtnSize;// new Vector2(ActionBtnWidth, ActionBtnHeight);
                    bool showHorizontalButtons = false;
                    int centerW = BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 2;
                    int centerH = BokuGame.bokuGame.GraphicsDevice.Viewport.Height / 2;

                    if (showHorizontalButtons)
                    {
                        topLeft.X = centerW - ((totalBtnLength + totalBtnGap) / 2) - extraDeadSpace;
                        topLeft.Y = pixelsFromEdge - extraDeadSpace;
                        bottomRight.X = centerW + ((totalBtnLength + totalBtnGap) / 2) + extraDeadSpace;
                        bottomRight.Y = pixelsFromEdge + ActionBtnSize.X + extraDeadSpace + 30;

                    }
                    else
                    {
                        topLeft.X = pixelsFromEdge - extraDeadSpace;
                        topLeft.Y = centerH - ((totalBtnLength + totalBtnGap) / 2) - extraDeadSpace;
                        bottomRight.X = pixelsFromEdge + ActionBtnSize.X + extraDeadSpace + 30;
                        bottomRight.Y = centerH + ((totalBtnLength + totalBtnGap) / 2) + extraDeadSpace;
                    }

                    AABB2D deadZoneBox = new AABB2D( topLeft, bottomRight );

                    if (deadZoneBox.Contains(touch.position))
                    {
                        return true;
                    }

                    //check undo/redo
                    topLeft.X = BokuGame.bokuGame.GraphicsDevice.Viewport.Width - 
                                            undoButton.button.GetSize().X - 
                                            redoButton.button.GetSize().X - 
                                            buttonGap*2.0f - pixelsFromEdge - extraDeadSpace;
                    topLeft.Y = 0; //right to edge of screen
                    bottomRight.X = BokuGame.bokuGame.GraphicsDevice.Viewport.Width; //right to edge of screen
                    bottomRight.Y = pixelsFromEdge + undoButton.button.GetSize().Y + extraDeadSpace;

                    deadZoneBox = new AABB2D( topLeft, bottomRight );

                    if (deadZoneBox.Contains(touch.position))
                    {
                        return true;
                    }

                    return false;
                }

                public void UnToggleCurrentButton()
                {
                    // TODO (****) This is poorly designed.  We have both and index for which button is toggled as
                    // well as a ToggleOn flag on each button.  Are these radio buttons?  If so, then we only need
                    // the index.  If they allow multi-select then the index is useless.  Pick one.  As it sits, not
                    // all of the buttons actually act as toggles.  Need a complete rethink to make this right.
                    if ( currentToggleBtnIdx >= 0) // untoggle
                    {
                        brushOptionButtons[currentToggleBtnIdx].ToggleOn = false;
                        brushOptionButtons[currentToggleBtnIdx].button.SetHoverState(new Vector2(-1.0f, -1.0f));
                        currentToggleBtnIdx = -1;
                    }
                }

                private bool SetButtonToggledState(TouchContact touch, int btnIdx)
                {
                    if (!brushOptionButtons[btnIdx].isToggleBtn)
                    {
                        return false;
                    }
                    if (touch.phase != TouchPhase.Ended)
                    {
                        if (touch.phase == TouchPhase.Began)
                        {
                            touch.TouchedObject = brushOptionButtons[btnIdx];
                        }
                        return true;
                    }
                    
                    if (btnIdx == currentToggleBtnIdx) // untoggle if same button
                    {
                        currentToggleBtnIdx = -1;
                        brushOptionButtons[btnIdx].ToggleOn = false;
                    }
                    else
                    {
                        /// de-toggle previouse toggle button
                        if ((currentToggleBtnIdx >= 0 && brushOptionButtons[currentToggleBtnIdx].ToggleOn)) // deselect current
                        {
                            brushOptionButtons[currentToggleBtnIdx].ToggleOn = false;
                        }

                        //Toggle boolean
                        brushOptionButtons[btnIdx].ToggleOn ^= true;
                        
                        currentToggleBtnIdx = btnIdx;
                        InGame.inGame.shared.currentTouchAction = brushOptionButtons[btnIdx].id;
                    }

                    // toggle state to the pressed button    
                    brushOptionButtons[btnIdx].button.SetHoverState( brushOptionButtons[btnIdx].ToggleOn ? touch.position : new Vector2(-1.0f, -1.0f) );
                    
                    return true;
                }

                private void SetTouchedState(TouchContact touch, int i)
                {
                    if ( touch.TouchedObject==null && touch.phase==TouchPhase.Began )
                    {
                        touch.TouchedObject = brushOptionButtons[i].button.Box;
                    }
                }

                public void ClearHoverState()
                {
                    for (int i = 0; i < brushOptionButtons.Count; i++)
                    {
                        //don't clear toggle buttons that are toggled on - they stay on until tapped off
                        if (!brushOptionButtons[i].ToggleOn)
                        {
                            brushOptionButtons[i].button.SetHoverState(new Vector2(-1.0f, -1.0f));
                        }
                    }

                    undoButton.button.SetHoverState(new Vector2(-1.0f, -1.0f));
                    redoButton.button.SetHoverState(new Vector2(-1.0f, -1.0f));
                }

                public void HandleTouchInput(TouchContact touch)
                {
                    bool actionHandled = false;
                    bool isOverAButton = false;
                    Brush2DManager.Brush2D brush = Brush2DManager.GetBrush(Boku.InGame.inGame.shared.editBrushIndex);


                    if (IsTapped(BrushActionIDs.baBrushCubic, touch, touch.position))
                    {
                        actionHandled = true;
                        HideButton(GetButtonIndex(BrushActionIDs.baBrushCubic));
                        Audio.Foley.PlayClickDown();

                        //Change settings for when cubic brush is tapped.
                        MaterialPicker.FabricMode = true;
                        Terrain.CurrentMaterialIndex = TerrainMaterial.GetFabric(Terrain.CurrentMaterialIndex);
                        Boku.InGame.IsLevelDirty = true;

                        //Show new button to take it's place
                        ShowBrushActionButton(BrushActionIDs.baBrushSmooth);
                    }
                    else if (IsTapped(BrushActionIDs.baBrushSmooth, touch, touch.position))
                    {
                        actionHandled = true;
                        HideButton(GetButtonIndex(BrushActionIDs.baBrushSmooth));
                        Audio.Foley.PlayClickDown();

                        MaterialPicker.FabricMode = false;
                        Terrain.CurrentMaterialIndex = TerrainMaterial.GetNonFabric(Terrain.CurrentMaterialIndex);
                        Boku.InGame.IsLevelDirty = true;

                        ShowBrushActionButton(BrushActionIDs.baBrushCubic);

                    }
                    else if (IsTapped(BrushActionIDs.baAllPath, touch, touch.position))
                    {
                        actionHandled = true;
                        HideButton(GetButtonIndex(BrushActionIDs.baAllPath));
                        Audio.Foley.PlayClickDown();

                        EditPathsTool tool = EditPathsTool.GetInstance() as EditPathsTool;
                        tool.ActOnPath = false;
                        Boku.InGame.IsLevelDirty = true;

                        ShowBrushActionButton(BrushActionIDs.baNormalPath);
                    }
                    else if (IsTapped(BrushActionIDs.baNormalPath, touch, touch.position))
                    {
                        actionHandled = true;
                        HideButton(GetButtonIndex(BrushActionIDs.baNormalPath));

                        EditPathsTool tool = EditPathsTool.GetInstance() as EditPathsTool;
                        tool.ActOnPath = true;
                        Boku.InGame.IsLevelDirty = true;

                        ShowBrushActionButton(BrushActionIDs.baAllPath);
                    }

                    for (int i = 0; i < brushOptionButtons.Count; i++)
                    {
                        if (brushOptionButtons[i].visible)
                        {
                            isOverAButton = true;
                            //Debug.WriteLine("TouchObj: " + touch.TouchedObject);

                            if (brushOptionButtons[i].button.Box.Contains(touch.position))
                            {

                                if (!actionHandled )
                                {
                                    //either way, update touched state so we know which object we're touching (only changes when touch is "Began")
                                    //SetTouchedState(touch, i);

                                    

                                    if (brushOptionButtons[i].button.Box.Touched(touch, touch.position))
                                    {
                                        //if we finished touching, it's a toggle button, and no hold recognized, toggle the button
                                        if ( brushOptionButtons[i].isToggleBtn && !TouchGestureManager.Get().TouchHoldGesture.WasRecognized )
                                        {
                                            SetButtonToggledState(touch, i);
                                        }
                                        else
                                        {
                                            //for non-toggle buttons, once the touch ends, make sure they don't appear highlighted
                                            brushOptionButtons[i].button.SetHoverState(new Vector2(-1.0f, -1.0f));
                                        }
                                        Audio.Foley.PlayClickDown();
                                    }
                                    else if (touch.TouchedObject != null && touch.TouchedObject == brushOptionButtons[i].button.Box)
                                    {
                                        //touching a button and not released, highlight that button
                                        brushOptionButtons[i].button.SetHoverState(touch.position);
                                    }
                                }
                            }
                            else if (!brushOptionButtons[i].ToggleOn)
                            {
                                //for non-toggle buttons, if we're not over the button, make sure it's off
                                brushOptionButtons[i].button.SetHoverState(new Vector2(-1.0f, -1.0f));
                            }
                        }
                    }

                    //check undo/redo
                    if (undoButton.visible && undoButton.button.Box.Contains(touch.position))
                    {
                        isOverAButton = true;
                        //work with the current system for now, which keeps track of the bounding box touched and verifies it's the same 
                        //touched object on untouch - needs some refactoring if there is time, quite a lot of duplication
                        if (touch.TouchedObject == null && touch.phase == TouchPhase.Began)
                        {
                            touch.TouchedObject = undoButton.button.Box;
                        }

                        if (undoButton.button.Box.Touched(touch, touch.position))
                        {
                            //clear out touched object
                            touch.TouchedObject = null;
                            DoUndo();

                            //Hide if we have zero undo's
                            if (InGame.UnDoStack.NumUnDo <= 0)
                            {
                                undoButton.visible = false;
                            }

                            if (InGame.UnDoStack.NumReDo > 0)
                            {
                                redoButton.visible = true;
                            }

                            undoButton.button.SetHoverState(new Vector2(-1.0f, -1.0f));
                            actionHandled = true;
                            Audio.Foley.PlayClickDown();
                        }
                        else if (touch.TouchedObject == undoButton.button.Box)
                        {
                            //for non-toggle buttons, once the touch ends, make sure they don't appear highlighted
                            undoButton.button.SetHoverState(touch.position);
                        }
                    }
                    else
                    {
                        undoButton.button.SetHoverState(new Vector2(-1.0f, -1.0f));
                    }
                    
                    if (redoButton.visible && redoButton.button.Box.Contains(touch.position))
                    {
                        isOverAButton = true;
                        if (touch.TouchedObject == null && touch.phase == TouchPhase.Began)
                        {
                            touch.TouchedObject = redoButton.button.Box;
                        }

                        if (redoButton.button.Box.Touched(touch, touch.position))
                        {
                            //clear out touched object
                            touch.TouchedObject = null;
                            DoRedo();

                            //Hide if we have zero redo's
                            if (InGame.UnDoStack.NumReDo == 0)
                            {
                                redoButton.visible = false;
                            }

                            if (InGame.UnDoStack.NumUnDo > 0)
                            {
                                undoButton.visible = true;
                            }

                            redoButton.button.SetHoverState(new Vector2(-1.0f, -1.0f));
                            actionHandled = true;
                            Audio.Foley.PlayClickDown();
                        }
                        else if (touch.TouchedObject == redoButton.button.Box)
                        {
                            //for non-toggle buttons, once the touch ends, make sure they don't appear highlighted
                            redoButton.button.SetHoverState(touch.position);
                        }
                    }
                    else
                    {
                        redoButton.button.SetHoverState(new Vector2(-1.0f, -1.0f));
                    }

                    //clear out touched object once the hover spot leaves the menu
                    if (!isOverAButton && InGame.inGame.shared.currentTouchAction != BrushActionIDs.NUMBER_OF_Buttons)
                    {
                        if (!(touch.TouchedObject is BrushActionButton))
                        {
                            touch.TouchedObject = null;
                        }
                    }
                }

            #endregion

            public void RenderBrushButtons()
            {
                if (brushOptionButtons.Count == 0)
                    return;

                Vector2 pos = Vector2.Zero;
                int spaceUsed=0;
                bool showHorizontalButtons = false;
                int centerW = BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 2;
                int centerH = BokuGame.bokuGame.GraphicsDevice.Viewport.Height / 2;
                if (showHorizontalButtons)
                {
                    for (int i = 0; i < brushOptionButtons.Count; i++)
                    {
                        if (brushOptionButtons[i].visible)
                        {
                            pos.Y = pixelsFromEdge;
                            pos.X = centerW - ((totalBtnLength + totalBtnGap) / 2) + spaceUsed;

                            brushOptionButtons[i].button.Render(pos);
                            spaceUsed += (int)brushOptionButtons[i].button.GetSize().X + buttonGap;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < brushOptionButtons.Count; i++)
                    {
                        if (brushOptionButtons[i].visible)
                        {
                            pos.X = pixelsFromEdge;
                            pos.Y = centerH - ((totalBtnLength + totalBtnGap) / 2) + spaceUsed;

                            brushOptionButtons[i].button.Render(pos);
                            spaceUsed += (int)brushOptionButtons[i].button.GetSize().X + buttonGap+2;
                        }
                    }

                    //render undo/redo
                    pos.X = BokuGame.bokuGame.GraphicsDevice.Viewport.Width - 64.0f - pixelsFromEdge;
                    pos.Y = pixelsFromEdge;

                    if (undoButton.visible || redoButton.visible)
                    {
                        UI2D.Shared.SpriteBatch.Begin();
                    }

                    if (undoButton.visible)
                    {
                        //undoButton.button.Box.Set(pos, pos + undoButton.button.GetSize());
                        undoButton.button.Render(pos);

                        pos.X -= (64.0f + buttonGap + 2);
                    }

                    if (redoButton.visible)
                    {
                        //redoButton.button.Box.Set(pos, pos + redoButton.button.GetSize());
                        redoButton.button.Render(pos);
                    }

                    if (undoButton.visible || redoButton.visible)
                    {
                        UI2D.Shared.SpriteBatch.End();
                    }

                }
            }
        }
        #region Members

        private List<ToolBarElement> elements = new List<ToolBarElement>();
        private TouchControls touchBrushControls = null;
        private int focusIndex = -1;
        private RenderTarget2D rt = null;
        private int width = -1;             // Current width of rendered toolbar.
        
        private Vector2 position;           // Where the toolbar gets rendered based on centering and overscan.
        private float scale = 1.0f;         // Scaling factor applied to toolbar texture.  Generally will be 1.0
                                            // unless we're on a small screen where we need to shrink the toolbar.

        // Is the mouse over one of the elements?
        private bool hovering = false;

        // Mode to revert to when user stops hovering.
        private InGame.BaseEditUpdateObj.ToolMode revertMode = InGame.BaseEditUpdateObj.ToolMode.CameraMove;

        private Texture2D homeTexture = null;
        private Texture2D runGameTexture = null;
        private Texture2D cameraMoveTexture = null;
        private Texture2D objectEditTexture = null;
        private Texture2D pathsTexture = null;
        private Texture2D terrainPaintTexture = null;
        private Texture2D terrainRaiseLowerTexture = null;
        private Texture2D terrainSpikeyHillyTexture = null;
        private Texture2D terrainSmoothLevelTexture = null;
        private Texture2D waterRaiseLowerTexture = null;
        private Texture2D deleteObjectsTexture = null;
        private Texture2D worldTweakTexture = null;

        private Texture2D subBrushesTexture = null;
        private Texture2D subMaterialsTexture = null;
        private Texture2D subWaterTexture = null;

        private Texture2D reticuleTexture = null;

        private bool dirty = true;
        InGame.BaseEditUpdateObj.ToolMode lastToolMode = InGame.BaseEditUpdateObj.ToolMode.WorldTweak;

        private Vector2 selectedSize = new Vector2(128, 128);
        private Vector2 unselectedSize = new Vector2(64, 64);
        
        #endregion

        #region Accessors
        public TouchControls TouchBrushControls
        {
            get { return touchBrushControls; }
        }

        public InGame.BaseEditUpdateObj.ToolMode CurrentMode
        {
            get 
            {
                if (focusIndex >= 0)
                {
                    return elements[focusIndex].Mode;
                }
                else
                {
                    return InGame.BaseEditUpdateObj.ToolMode.None;
                }
            }
            set
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    if (elements[i].Mode == value)
                    {
                        // If we're changing tools, make an undo checkpoint.
                        if (focusIndex != i && focusIndex != -1 && InGame.IsLevelDirty)
                        {
                            InGame.UnDoStack.Store();
                        }

                        focusIndex = i;
                        dirty = true;
                    }
                }
                revertMode = CurrentMode;
            }
        }

        public InGame.BaseEditUpdateObj.ToolMode RevertMode
        {
            get { return revertMode; }
        }

        private InGame.BaseEditUpdateObj.ToolMode CurrentModeInternal
        {
            get { return elements[focusIndex].Mode; }
            set
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    if (elements[i].Mode == value)
                    {
                        if (focusIndex != i)
                        {
                            Audio.Foley.PlayShuffle();
                        }

                        focusIndex = i;
                        dirty = true;
                    }
                }
            }
        }

        /// <summary>
        /// Is the mouse currently over one of the elements.
        /// </summary>
        public bool Hovering
        {
            get { return hovering; }
        }

        public Texture2D ReticuleTexture
        {
            get { return reticuleTexture; }
        }

        #endregion

        #region Public

        // c'tor
        public ToolBar()
        {
            touchBrushControls = new TouchControls();
        }   // end of c'tor

        public void Add(ToolBarElement e)
        {
            elements.Add(e);
            dirty = true;
        }   // end of Add()

        public bool SetVisible(string name, bool isVisible)
        {
            for (int i=0;i<elements.Count;i++)
            {
                if(elements[i].Name== name)
                {
                    elements[i].Visible = isVisible;
                    return true;
                }
            }
            return false;
        }

        public void SetAllToolsVisible(bool isVisible)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                elements[i].Visible = isVisible;
            }
        }

        public ToolBarElement GetToolBarElement(InGame.BaseEditUpdateObj.ToolMode mode)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Mode == mode)
                    return elements[i];
            }

            return null;
        }

        /// <summary>
        /// Returns true is a new tool has been selected.
        /// </summary>
        /// <returns></returns>
        public bool Update()
        {
            if (AuthUI.IsModalActive)
            {
                return false;
            }

            // Ignore input if the tutorial mode's modal display is active.
            if (CommandStack.Peek().name == "ModalDisplay")
            {
                return false;
            }

            //every frame check if undo/redo state has changed
            TouchBrushControls.ShowRedoUndo();

            bool newTool = false;
            if (GamePadInput.ActiveMode == GamePadInput.InputMode.KeyboardMouse)
            {
                // Mouse hover / mouse clicks
                newTool = HandleMouseInput();
            }
            else if ((GamePadInput.ActiveMode == GamePadInput.InputMode.Touch))
            {
                newTool = HandleTouchInput();
            }

            // Undo/Redo stack
            if (Actions.Undo.WasPressed )
            {
                Actions.Undo.ClearAllWasPressedState();

                DoUndo();
            }
            if (Actions.Redo.WasPressed )
            {
                Actions.Redo.ClearAllWasPressedState();

                DoRedo();
            }

            // Make sure all elements are the correct size.
            for (int i = 0; i < elements.Count; i++)
            {
                elements[i].Size = i == focusIndex ? selectedSize : unselectedSize;
            }
            bool resetPaintActionButtons = (lastToolMode != CurrentMode);

            // Set HelpOverlay state.
            switch(CurrentMode)
            {
                case InGame.BaseEditUpdateObj.ToolMode.Home:
                    HelpOverlay.ReplaceTop("MouseEditHome");
                    if (lastToolMode != CurrentMode)
                    {
                        TouchBrushControls.ClearPaintButtons();
                    }
                    break;

                case InGame.BaseEditUpdateObj.ToolMode.RunGame:
                    HelpOverlay.ReplaceTop("MouseEditPlay");
                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                    }
                    break;

                case InGame.BaseEditUpdateObj.ToolMode.CameraMove:
                    HelpOverlay.ReplaceTop("MouseEditCameraMove");
                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                    }
                    break;

                case InGame.BaseEditUpdateObj.ToolMode.EditObject:
                    HelpOverlay.ReplaceTop("MouseEditEditObject");
                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                    }
                    break;
                case InGame.BaseEditUpdateObj.ToolMode.Paths:
                    
                    EditPathsTool pathTool = EditPathsTool.GetInstance() as EditPathsTool;
                    if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baNode))
                    {
                        //add mode always wins
                        pathTool.HelpOverlayID = "MouseEditPathsAddPath";
                    }
                    else if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baDelete))
                    {
                        if (pathTool.ActOnPath)
                        {
                            pathTool.HelpOverlayID = "MouseEditPathsDeletePath";
                        }
                        else
                        {
                            pathTool.HelpOverlayID = "MouseEditPathsDeleteNode";
                        }
                    }
                    else if (pathTool.ActOnPath)
                    {
                        //otherwise - specific overlay for act on path
                        pathTool.HelpOverlayID = "MouseEditPathsAllPath";
                    }
                    else
                    {
                        //and the default otherwise
                        pathTool.HelpOverlayID = "MouseEditPaths";
                    }
                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baNode);
                        if (pathTool.ActOnPath)
                        {
                            TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baAllPath);
                        }
                        else
                        {
                            TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baNormalPath);
                        }
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baDelete);
                    }

                    // Set current help overlay.
                    HelpOverlay.ReplaceTop(pathTool.HelpOverlayID);

                    break;

                case InGame.BaseEditUpdateObj.ToolMode.TerrainPaint:
                    if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baPaintMaterial))
                    {
                        if (MaterialPicker.FabricMode == true)
                        {
                            PaintTool.GetInstance().HelpOverlayID = "MouseEditTerrainPaintSmooth";
                        }
                        else
                        {
                            PaintTool.GetInstance().HelpOverlayID = "MouseEditTerrainPaintCubic"; 
                        }
                    }
                    else if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baDelete))
                    {
                        PaintTool.GetInstance().HelpOverlayID = "MouseEditTerrainDelete";
                    }
                    else
                    {
                        PaintTool.GetInstance().HelpOverlayID = "MouseEditTerrainPaint";
                    }

                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushMore);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushLess);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushCubic);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushSmooth);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baPaintMaterial);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baDelete);            
            
                        
                        if( GamePadInput.InputMode.Touch == GamePadInput.ActiveMode )
                        {
                            TouchBrushControls.SetToggle( TouchControls.BrushActionIDs.baPaintMaterial, true );
                        }
                    }

                    // Set current help overlay.
                    HelpOverlay.ReplaceTop(PaintTool.GetInstance().HelpOverlayID);

                    break;

                case InGame.BaseEditUpdateObj.ToolMode.TerrainRaiseLower:
                    
                    if( TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baTerrainRaise) )
                    {
                        RaiseLowerTool.GetInstance().HelpOverlayID = "MouseEditTerrainRaising";
                    }
                    else if( TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baTerrainLower) )
                    {
                        RaiseLowerTool.GetInstance().HelpOverlayID = "MouseEditTerrainLowering";
                    }
                    else
                    {
                        RaiseLowerTool.GetInstance().HelpOverlayID = "MouseEditTerrainRaiseLower";
                    }

                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushMore);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushLess);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baTerrainRaise);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baTerrainLower);

                        if( GamePadInput.InputMode.Touch == GamePadInput.ActiveMode )
                        {
                            TouchBrushControls.SetToggle( TouchControls.BrushActionIDs.baTerrainRaise, true );
                        }
                    }

                    // Set current help overlay.
                    HelpOverlay.ReplaceTop(RaiseLowerTool.GetInstance().HelpOverlayID);

                    break;

                case InGame.BaseEditUpdateObj.ToolMode.TerrainSpikeyHilly:
                    
                    
                    if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baSmooth))
                    {
                        SpikeyHillyTool.GetInstance().HelpOverlayID = "MouseEditTerrainSpikeyHilly_ToggleSmooth";
                    }
                    else if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baHilly))
                    {
                        SpikeyHillyTool.GetInstance().HelpOverlayID = "MouseEditTerrainSpikeyHilly_ToggleHilly";
                    }
                    else if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baSpikey))
                    {
                        SpikeyHillyTool.GetInstance().HelpOverlayID = "MouseEditTerrainSpikeyHilly_ToggleSpikey";
                    }
                    else
                    {
                        SpikeyHillyTool.GetInstance().HelpOverlayID = "MouseEditTerrainSpikeyHilly";
                    }
                    
                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushMore);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushLess);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baSpikey);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baHilly);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baSmooth);
                    }
                    
                    // Set current help overlay.
                    HelpOverlay.ReplaceTop(SpikeyHillyTool.GetInstance().HelpOverlayID);

                    break;

                case InGame.BaseEditUpdateObj.ToolMode.TerrainSmoothLevel:
                    if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baFlatten))
                    {
                        SmoothLevelTool.GetInstance().HelpOverlayID = "MouseEditTerrainSmoothLevel_ToggleFlatten";
                    }
                    else if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baSmooth))
                    {
                        SmoothLevelTool.GetInstance().HelpOverlayID = "MouseEditTerrainSmoothLevel_ToggleSmooth";
                    }
                    else
                    {
                        SmoothLevelTool.GetInstance().HelpOverlayID = "MouseEditTerrainSmoothLevel";
                    }

                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushMore);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushLess);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baSmooth);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baFlatten);
                    }

                    // Set current help overlay.
                    HelpOverlay.ReplaceTop(SmoothLevelTool.GetInstance().HelpOverlayID);

                    break;

                case InGame.BaseEditUpdateObj.ToolMode.WaterRaiseLower:

                    if(TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baWaterRaise))
                    {
                        //Debug.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>> Setting Water RAISE.");
                        WaterTool.GetInstance().HelpOverlayID = "MouseEditWaterRaiseLower_ToggleRaise";
                    }
                    else if(TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baWaterLower))
                    {
                        //Debug.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>> Setting Water LOWER.");
                        WaterTool.GetInstance().HelpOverlayID = "MouseEditWaterRaiseLower_ToggleLower";
                    }
                    else
                    {
                        //Debug.WriteLine(">Setting Water Default.");
                        WaterTool.GetInstance().HelpOverlayID = "MouseEditWaterRaiseLower" ;
                    }
                    
                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baWaterRaise);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baWaterLower);
                    }

                    // Set current help overlay.
                    HelpOverlay.ReplaceTop(WaterTool.GetInstance().HelpOverlayID);

                    break;

                case InGame.BaseEditUpdateObj.ToolMode.DeleteObjects:

                    if (TouchBrushControls.IsToggled(TouchControls.BrushActionIDs.baDelete))
                    {
                        DeleteObjectsTool.GetInstance().HelpOverlayID = "MouseEditDeleteObjects_Deleting";
                    }
                    else
                    {
                        DeleteObjectsTool.GetInstance().HelpOverlayID = "MouseEditDeleteObjects";
                    }
                    
                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushMore);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baBrushLess);
                        TouchBrushControls.ShowBrushActionButton(TouchControls.BrushActionIDs.baDelete);
                    }

                    // Set current help overlay.
                    HelpOverlay.ReplaceTop(DeleteObjectsTool.GetInstance().HelpOverlayID);

                    break;

                case InGame.BaseEditUpdateObj.ToolMode.WorldTweak:
                    HelpOverlay.ReplaceTop("MouseEditWorldTweak");
                    if (resetPaintActionButtons)
                    {
                        TouchBrushControls.ClearPaintButtons();
                    }
                    break;

                default:
                    HelpOverlay.ReplaceTop("MouseEditBase");
                    TouchBrushControls.ClearPaintButtons();
                    break;
            }
            lastToolMode = CurrentMode;

            if (dirty)
            {
                RefreshTexture();
            }

            if (newTool && InGame.IsLevelDirty)
            {
                InGame.UnDoStack.Store();
            }

            /*
            if (Time.FrameCounter % 20 == 0)
            {
                Debug.Print("====");
                Debug.Print("focusIndex " + focusIndex.ToString());
                Debug.Print("CurrentMode " + CurrentMode.ToString());
                Debug.Print("CurrentModeIntrnal " + CurrentModeInternal.ToString());
                Debug.Print("RevertMode " + RevertMode.ToString());
            }
            */

            return newTool;

        }   // end of Update()

        private static void DoUndo()
        {
            // If we've made any changes, take a snapshot.
            if (InGame.IsLevelDirty)
            {
                
                InGame.UnDoStack.Store();
            }
            GameActor actor = InGame.inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor;
            if (actor != null)
            {
                actor.MakeSelected(false, Vector4.Zero);
                InGame.inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor = null;
                Boku.InGame.ColorPalette.Active = false;
            }
            InGame.UnDoStack.UnDo();
        }

        private static void DoRedo()
        {
            // If we've made any changes, take a snapshot.
            if (InGame.IsLevelDirty)
            {
                InGame.UnDoStack.Store();
            }
            GameActor actor = InGame.inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor;
            if (actor != null)
            {
                actor.MakeSelected(false, Vector4.Zero);
                InGame.inGame.mouseEditUpdateObj.ToolBox.EditObjectsToolInstance.FocusActor = null;
                Boku.InGame.ColorPalette.Active = false;
            }

            InGame.UnDoStack.ReDo();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if user made a selection.  False if nothing or hovering.</returns>
        public bool HandleMouseInput()
        {
            // First check for space being used to temporarily go into grab mode.
            if (Actions.CameraMove.IsPressed)
            {
                if (CurrentModeInternal != InGame.BaseEditUpdateObj.ToolMode.CameraMove)
                {
                    revertMode = CurrentMode;
                    CurrentModeInternal = InGame.BaseEditUpdateObj.ToolMode.CameraMove;
                }
                return false;
            }

            // Get mouse position.
            Vector2 rawMouseHit = new Vector2(MouseInput.Position.X, MouseInput.Position.Y);

            if (MouseInput.Middle.WasPressed)
            {
            }

            // Check for undo/redo.
            if (InGame.UnDoStack.RedoHitBox.LeftPressed(rawMouseHit + BokuGame.ScreenPosition))
            {
                // If we've made any changes, take a snapshot.
                if (InGame.IsLevelDirty)
                {
                    InGame.UnDoStack.Store();
                }
                
                InGame.UnDoStack.ReDo();
            }
            if (InGame.UnDoStack.UndoHitBox.LeftPressed(rawMouseHit + BokuGame.ScreenPosition))
            {
                // If we've made any changes, take a snapshot.
                if (InGame.IsLevelDirty)
                {
                    InGame.UnDoStack.Store();
                }

                InGame.UnDoStack.UnDo();
            }

            // Handle change of text color for hover.
            InGame.UnDoStack.UpdateTextColor(rawMouseHit + BokuGame.ScreenPosition);

            // Transform mouse position into rt coords.
            Vector2 mouseHit = rawMouseHit - position;
            mouseHit /= scale;

            // Check each element against the mouse.
            hovering = false;
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Visible)
                {
                    // If this is already the current tool, check any of it's sub elements instead.
                    if (revertMode == elements[i].Mode)
                    {
                        if (elements[i].CheckSubElementActivation(mouseHit))
                        {
                            hovering = false;
                            return false;
                        }
                    }
                    else
                    {
                        // If user selected this tool.
                        if (elements[i].HitBox.LeftPressed(mouseHit))
                        {
                            CurrentModeInternal = elements[i].Mode;
                            revertMode = CurrentMode;
                            Audio.Foley.PlayPressA();

                            return true;
                        }
                    }
                }

                // Don't check for hover if the current tool is in action.
                BaseMouseEditTool activeTool = InGame.inGame.mouseEditUpdateObj.ToolBox.ActiveTool;
                if(activeTool == null || !activeTool.IsInAction)
                {
                    // If mouse is over this tool.
                    if (elements[i].HitBox.Contains(mouseHit))
                    {
                        CurrentModeInternal = elements[i].Mode;
                        hovering = true;

                        break;
                    }
                }
            }

            // If not hovering, revert to the last selected mode unless that was Home, Run or WorldTweak
            // in which case just go back to camera mode.
            if (!hovering && CurrentMode != revertMode)
            {
                CurrentModeInternal = revertMode;
                if (CurrentMode == InGame.BaseEditUpdateObj.ToolMode.Home
                    || CurrentMode == InGame.BaseEditUpdateObj.ToolMode.RunGame
                    || CurrentMode == InGame.BaseEditUpdateObj.ToolMode.WorldTweak)
                {
                    CurrentModeInternal = revertMode = InGame.BaseEditUpdateObj.ToolMode.CameraMove;
                }

                // Always treat reverting as a strong selection.  This ensure that the
                // active tool is properly set up.
                return true;
            }

            return false;

        }   // end of HandleMouseInput

        public bool IsOverUIButton(TouchContact touch, bool ignoreOnDrag)
        {
            
            bool bOverUI = false;

            if (null != touch)
            {
                //Always want to check the sub tool, even on drag.
                bOverUI = touchBrushControls.IsOverUIButton(touch);
            }

            if( !bOverUI && !(ignoreOnDrag && TouchGestureManager.Get().DragGesture.IsDragging) && null != touch )
            {

                Vector2 touchHitUV = touch.position;
                // Transform touch position into rt coords.
                touchHitUV -= position;
                touchHitUV /= scale;


                if (!InGame.inGame.mouseEditUpdateObj.ToolBox.PickersActive)
                {
                    for (int j = 0; j < elements.Count; j++)
                    {
                        if (elements[j].HitBox.Contains(touchHitUV))
                        {
                            bOverUI = true;
                            break;
                        }
                    }
                }
            }

            return bOverUI;
        }

        public bool IsButtonActionToggledOn(TouchControls.BrushActionIDs btnId)
        {
            return TouchBrushControls.IsToggled(btnId);
        }

        public bool IsAnyButtonActionToggledOn()
        {
            int numButtons = (int)TouchControls.BrushActionIDs.NUMBER_OF_Buttons;
            for (int i = 0; i < numButtons; ++i)
            {
                if (TouchBrushControls.IsToggled((TouchControls.BrushActionIDs)i))
                {
                    return true;
                }
            }

            return false;
        }
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if user made a selection.  False if nothing .</returns>
        public bool HandleTouchInput()
        {
            // First check for space being used to temporarily go into grab mode.
            if (Actions.CameraMove.IsPressed)
            {
                if (CurrentModeInternal != InGame.BaseEditUpdateObj.ToolMode.CameraMove)
                {
                    revertMode = CurrentMode;
                    CurrentModeInternal = InGame.BaseEditUpdateObj.ToolMode.CameraMove;
                }
                return false;
            }
            hovering = false;

            if (CurrentModeInternal != InGame.BaseEditUpdateObj.ToolMode.EditObject)
            {
                GameActor focus = TouchEdit.HitInfo.ActorHit;
                if (TouchGestureManager.Get().TapGesture.WasRecognized && focus != null && Boku.InGame.inGame.TouchEdit.HasNonUITouch())
                {
                    //double tap on game object detected while in another mode
                    CurrentModeInternal = revertMode = InGame.BaseEditUpdateObj.ToolMode.EditObject;

                    return true;
                }
            }

            if (CurrentModeInternal == InGame.BaseEditUpdateObj.ToolMode.EditObject)
            {
                if (TouchGestureManager.Get().RotateGesture.IsRotating || 
                    TouchGestureManager.Get().PinchGesture.IsPinching ||
                    TouchGestureManager.Get().DragGesture.IsDragging )
                return false;
            }
            bool isDragging = TouchGestureManager.Get().DragGesture.IsDragging;
            bool skipPaintActionUI = false;

            if (TouchInput.TouchCount <= 0)
            {
                //no input - make sure we clear hover state
                touchBrushControls.ClearHoverState();
            }

            // Get touch position.
            for (int i = 0; i < TouchInput.TouchCount; i++)
            {
                TouchContact touch = TouchInput.GetTouchContactByIndex(i);
                
                // Touch input
                // If the user touched the menu, move the selection index to the item under the touch.
                // On touch down, make the item (if any) under the contact the touchedItem.
                // On touch up, if the touch is still over the touchedItem, activate it.  If not, just clear touchedItem. 
                Vector2 touchHitUV = touch.position;// TouchInput.GetHitUV(touch.position, camera, ref invWorldMatrix, width, height, affectedByOverscan);

                // Handle change of text color for hover.
                InGame.UnDoStack.UpdateTextColor(touchHitUV);

                // Transform touch position into rt coords.
                touchHitUV -= position;
                touchHitUV /= scale;
                if (!isDragging)
                {
                    for (int j = 0; j < elements.Count; j++)
                    {
                        //FIXME: currently skipping buttons we don't have fully implemented for touch
                        if (elements[j].DisableForTouch)
                        {
                            continue;
                        }

                        // TODO (****) WTF?  Why is revertMode even a thing here?  revertMode is designed to save
                        // the state of the toolBar when a user hovers over a non-selected tool.  In this case
                        // we're doing touch input which doesn't have hover.

                        // If this is already the current tool, check any of it's sub elements instead.
                        if (revertMode == elements[j].Mode)
                        {
                            if (elements[j].CheckTouchSubElementActivation(touch, touchHitUV))
                            {
                                touch.TouchedObject = elements[j];
                                if (touchBrushControls.CurrentToggleIndex >= 0)
                                {
                                    touchBrushControls.UnToggleCurrentButton();
                                    InGame.inGame.shared.currentTouchAction = TouchControls.BrushActionIDs.NUMBER_OF_Buttons;
                                }
                                hovering = false;
                                return false;
                            }
                        }
                        else
                        {
                            // If user selected this tool.
                            if (TouchGestureManager.Get().TapGesture.WasTapped()) // WasReleased)
                            {
                                if (elements[j].HitBox.Contains(touchHitUV))
                                {
                                    skipPaintActionUI = true;
                                    CurrentModeInternal = elements[j].Mode;
                                    revertMode = CurrentMode;
                                    //  hovering = false;
                                    if (touch.TouchedObject == null)
                                    {
                                        touch.TouchedObject = this;
                                    }

                                    Audio.Foley.PlayPressA();

                                    return true;
                                }
                            }
                        }
                    }
                }

                if (!skipPaintActionUI)
                {
                    touchBrushControls.HandleTouchInput(touch);
                }

            }

            // If not hovering, revert to the last selected mode unless that was Home, Run or WorldTweak
            // in which case just go back to camera mode.
            if (!hovering && CurrentMode != revertMode)
            {
                CurrentModeInternal = revertMode;
                if (CurrentMode == InGame.BaseEditUpdateObj.ToolMode.Home
                    || CurrentMode == InGame.BaseEditUpdateObj.ToolMode.RunGame
                    || CurrentMode == InGame.BaseEditUpdateObj.ToolMode.WorldTweak)
                {
                    CurrentModeInternal = revertMode = InGame.BaseEditUpdateObj.ToolMode.CameraMove;
                }

                // Always treat reverting as a strong selection.  This ensure that the
                // active tool is properly set up.
                return true;
            }

            return false;
        }// end of HandleTouchInput

        public void RefreshTexture()
        {
            InGame.SetRenderTarget(rt);

            InGame.Clear(Color.Transparent);

            width = 1;
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Visible)
                {
                    Vector2 pos = new Vector2(width, 64.0f + (128 - elements[i].Size.Y) / 2.0f);

                    elements[i].Render(pos);

                    width += (int)elements[i].Size.X;
                }
            }
            // Add a single pixel cushion.
            ++width;

            InGame.RestoreRenderTarget();

            dirty = false;

        }   // end of RefreshTexture()

        public void Render()
        {
            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

            // Center on screen and just high enough to clear bottom help overlay text.
            Vector2 screenSize = BokuGame.ScreenSize;

            scale = 1.0f; // Only shrink if needed to fit in the width.
            position = Vector2.Zero;
            if (width > screenSize.X)
            {
                scale = screenSize.X / width;
                position.X = 0;
            }
            else
            {
                position.X = (screenSize.X - width) / 2.0f;
            }

            // Offset adjusts height releative to help overlay text.
            int offset = (int)(-20 + rt.Height * (1.0f - scale));
            position.Y = screenSize.Y - rt.Height + offset;

            // Another case where after device reset the system thinks that the rt is still set on the device
            // and throws when GetTexture2D is called.  This only happens for the first frame and so is
            // easily ignored.  
            try
            {
                Vector2 size = scale * new Vector2(rt.Width, rt.Height);
                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                Rectangle rect = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
                
                batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                {
                    batch.Draw(rt, rect, Color.White);
                }
                batch.End();
            }
            catch
            {
            }
            
            if ((GamePadInput.ActiveMode == GamePadInput.InputMode.Touch))
            {
                if ( touchBrushControls != null)
                {
                    touchBrushControls.RenderBrushButtons();
                }
            }

        }   // end of Render()

        #endregion

        #region Internal

        public void LoadContent(bool immediate)
        {
            homeTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\Home");
            runGameTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\Play");
            cameraMoveTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\Hand");
            objectEditTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\ObjectEdit");
            pathsTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\Paths");
            terrainPaintTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\TerrainMaterial");
            terrainRaiseLowerTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\TerrainUpDown");
            terrainSpikeyHillyTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\TerrainRoughHill");
            terrainSmoothLevelTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\TerrainFlatten");
            waterRaiseLowerTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\TerrainWater");
            deleteObjectsTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\DeleteObject");
            worldTweakTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\WorldSettings");

            subBrushesTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\SubBrushes");
            subMaterialsTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\SubMaterials");
            subWaterTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ToolMenu\SubWater");

            reticuleTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\Tools\SelectionReticule2");

            touchBrushControls.LoadTouchPaintButtonContent();

        }   // end of LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
            if (rt == null)
            {
                rt = new RenderTarget2D(device, 1280, 192, false, SurfaceFormat.Color, DepthFormat.None);
                dirty = true;
            }

            // Add elements to the tool bar.
            elements.Clear();

            ToolBarElement home = new ToolBarElement(Strings.Localize("toolBar.home"), InGame.BaseEditUpdateObj.ToolMode.Home, homeTexture, this, false);
            ToolBarElement runGame = new ToolBarElement(Strings.Localize("toolBar.runGame"), InGame.BaseEditUpdateObj.ToolMode.RunGame, runGameTexture, this, false);
            ToolBarElement cameraMove = new ToolBarElement(Strings.Localize("toolBar.cameraMove"), InGame.BaseEditUpdateObj.ToolMode.CameraMove, cameraMoveTexture, this, false);
            ToolBarElement objectEdit = new ToolBarElement(Strings.Localize("toolBar.objectEdit"), InGame.BaseEditUpdateObj.ToolMode.EditObject, objectEditTexture, this, false);
            ToolBarElement paths = new ToolBarElement(Strings.Localize("toolBar.paths"), InGame.BaseEditUpdateObj.ToolMode.Paths, pathsTexture, this, false);
            ToolBarElement terrainPaint = new ToolBarElement(Strings.Localize("toolBar.terrainPaint"), InGame.BaseEditUpdateObj.ToolMode.TerrainPaint, terrainPaintTexture, this, false);
                            terrainPaint.AddLeftSubElement(subMaterialsTexture, InGame.inGame.mouseEditUpdateObj.ToolBox.MaterialPicker, 10.0f);
                            terrainPaint.AddRightSubElement(subBrushesTexture, InGame.inGame.mouseEditUpdateObj.ToolBox.BrushPicker, 10.0f);
            ToolBarElement terrainRaiseLower = new ToolBarElement(Strings.Localize("toolBar.terrainRaiseLower"), InGame.BaseEditUpdateObj.ToolMode.TerrainRaiseLower, terrainRaiseLowerTexture, this, false);
                            terrainRaiseLower.AddRightSubElement(subBrushesTexture, InGame.inGame.mouseEditUpdateObj.ToolBox.BrushPicker, 0.0f);
            ToolBarElement terrainSpikeyHilly = new ToolBarElement(Strings.Localize("toolBar.terrainSpikeyHilly"), InGame.BaseEditUpdateObj.ToolMode.TerrainSpikeyHilly, terrainSpikeyHillyTexture, this, false);
                            terrainSpikeyHilly.AddRightSubElement(subBrushesTexture, InGame.inGame.mouseEditUpdateObj.ToolBox.BrushPicker, 0.0f);
            ToolBarElement terrainSmoothLevel = new ToolBarElement(Strings.Localize("toolBar.terrainSmoothLevel"), InGame.BaseEditUpdateObj.ToolMode.TerrainSmoothLevel, terrainSmoothLevelTexture, this, false);
                            terrainSmoothLevel.AddRightSubElement(subBrushesTexture, InGame.inGame.mouseEditUpdateObj.ToolBox.BrushPicker, 0.0f);
            ToolBarElement waterRaiseLower = new ToolBarElement(Strings.Localize("toolBar.waterRaiseLower"), InGame.BaseEditUpdateObj.ToolMode.WaterRaiseLower, waterRaiseLowerTexture, this, false);
                            waterRaiseLower.AddLeftSubElement(subWaterTexture, InGame.inGame.mouseEditUpdateObj.ToolBox.WaterPicker, 0.0f);
            ToolBarElement deleteObjects = new ToolBarElement(Strings.Localize("toolBar.deleteObjects"), InGame.BaseEditUpdateObj.ToolMode.DeleteObjects, deleteObjectsTexture, this, false);
                            deleteObjects.AddRightSubElement(subBrushesTexture, InGame.inGame.mouseEditUpdateObj.ToolBox.BrushPicker, 0.0f);
            ToolBarElement worldTweak = new ToolBarElement(Strings.Localize("toolBar.worldTweak"), InGame.BaseEditUpdateObj.ToolMode.WorldTweak, worldTweakTexture, this, false);

            elements.Add(home);
            elements.Add(runGame);
            elements.Add(cameraMove);
            elements.Add(objectEdit);
            elements.Add(paths);
            elements.Add(terrainPaint);
            elements.Add(terrainRaiseLower);
            elements.Add(terrainSmoothLevel);
            elements.Add(terrainSpikeyHilly);
            elements.Add(waterRaiseLower);
            elements.Add(deleteObjects);
            elements.Add(worldTweak);

            CurrentMode = InGame.BaseEditUpdateObj.ToolMode.CameraMove;

        }   // end of InitDeviceResources()

        public void UnloadContent()
        {
            BokuGame.Release(ref homeTexture);
            BokuGame.Release(ref runGameTexture);
            BokuGame.Release(ref cameraMoveTexture);
            BokuGame.Release(ref objectEditTexture);
            BokuGame.Release(ref pathsTexture);
            BokuGame.Release(ref terrainPaintTexture);
            BokuGame.Release(ref terrainRaiseLowerTexture);
            BokuGame.Release(ref terrainSpikeyHillyTexture);
            BokuGame.Release(ref terrainSmoothLevelTexture);
            BokuGame.Release(ref waterRaiseLowerTexture);
            BokuGame.Release(ref deleteObjectsTexture);
            BokuGame.Release(ref worldTweakTexture);

            BokuGame.Release(ref subBrushesTexture);
            BokuGame.Release(ref subMaterialsTexture);
            BokuGame.Release(ref subWaterTexture);

            BokuGame.Release(ref reticuleTexture);

            BokuGame.Release(ref rt);

        }   // end of UnloadContent()

        public void DeviceReset(GraphicsDevice device)
        {
            if (rt != null)
                BokuGame.Release(ref rt);
            rt = new RenderTarget2D(device, 1280, 192, false, SurfaceFormat.Color, DepthFormat.None);
            dirty = true;
        }   // end of DeviceReset()

        #endregion
    }   // end of class ToolBar

}   // end of namepsace Boku
