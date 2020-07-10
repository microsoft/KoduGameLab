// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;

namespace Boku
{
    /// <summary>
    /// Tool bar as displayed in MouseEdit mode.
    /// </summary>
    public class ToolBar : INeedsDeviceReset
    {       
               
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

 
            private int currentToggleBtnIdx = -1;
            /// <summary>
            ///  Spaceing data for drawing buttons.
            /// </summary>
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


        }   // end of class TouchControls

        #region Members

        private TouchControls touchBrushControls = null;
        private float scale = 1.0f;         // Scaling factor applied to toolbar texture.  Generally will be 1.0
                                            // unless we're on a small screen where we need to shrink the toolbar.

        // Is the mouse over one of the elements?
        private bool hovering = false;
        
        private Vector2 selectedSize = new Vector2(128, 128);
        private Vector2 unselectedSize = new Vector2(64, 64);
        
        #endregion

        #region Accessors
        public TouchControls TouchBrushControls
        {
            get { return touchBrushControls; }
        }

        /// <summary>
        /// Is the mouse currently over one of the elements.
        /// </summary>
        public bool Hovering
        {
            get { return hovering; }
        }

        #endregion

        #region Public

        // c'tor
        public ToolBar()
        {
        }   // end of c'tor

        
        public bool IsOverUIButton(TouchContact touch, bool ignoreOnDrag)
        {
            
            bool bOverUI = false;

            if (null != touch)
            {
                //Always want to check the sub tool, even on drag.
                //bOverUI = touchBrushControls.IsOverUIButton(touch);
            }

            if( !bOverUI && !(ignoreOnDrag && TouchGestureManager.Get().DragGesture.IsDragging) && null != touch )
            {

                Vector2 touchHitUV = touch.position;
                // Transform touch position into rt coords.
                //touchHitUV -= position;
                touchHitUV /= scale;

                /*
                for (int j = 0; j < elements.Count; j++)
                {
                    if (elements[j].HitBox.Contains(touchHitUV))
                    {
                        bOverUI = true;
                        break;
                    }
                }
                */
            }

            return bOverUI;
        }

        public bool IsButtonActionToggledOn(TouchControls.BrushActionIDs btnId)
        {
            return false;
        }

        public bool IsAnyButtonActionToggledOn()
        {
            return false;
        }
        


        #endregion

        #region Internal

        public void LoadContent(bool immediate)
        {

        }   // end of LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }   // end of InitDeviceResources()

        public void UnloadContent()
        {
        }   // end of UnloadContent()

        public void DeviceReset(GraphicsDevice device)
        {
        }   // end of DeviceReset()

        #endregion
    }   // end of class ToolBar

}   // end of namepsace Boku
