// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.Gesture;
using Boku.Fx;
using Boku.UI;
using Boku.UI2D;
using Boku.Common.Xml;

namespace Boku.Programming
{
    /// <summary>
    /// The primary responsibility of this class is to render and maintain the internal state of
    /// any buttons required by Kode containing TouchButtonFilter tiles. When any such filter exists, we 
    /// need to draw the button on the screen and respond to touches by the user. The TouchButtonFilters 
    /// will be querying this class to find out if their button conditions have been met.
    /// </summary>
    public class TouchVirtualController
    {
        //List of all the button types to be handled
        public enum TouchButtonType
        {
            //Virtual Controller 
            Button_A,
            Button_B,
            Button_X,
            Button_Y,

            // Add your buttons above this comment line
            SIZEOF,
        }

        public enum TouchButtonCrossPattern
        {
            CrossPattern_Top,
            CrossPattern_Bottom,
            CrossPattern_Left,
            CrossPattern_Right,

            CrossPattern_Count
        }

        private const int kNumButtons = (int)TouchButtonType.SIZEOF;

        private static AABB2D[] buttonHitBoxes = new AABB2D[kNumButtons];
        private static Texture2D[] buttonTextures = new Texture2D[kNumButtons];
        
        private static ButtonState[] buttonState = new ButtonState[kNumButtons];

        private static bool isInitialized = false;
        private static Vector2 defaultButtonSize = new Vector2(128, 128);

        /*
         * This scale gets initialized at startup and compares 
        *the width/height of the view-port to an expected resolution.
        *If the target resolution is bigger we scale down.
         */
        private static float ControllerScale = 1.0f; 


        private const float kButtonHorizOffsetToScreenEdge = 15; //In Px the offset from the side of the screen the button cross will render.
        private const float kButtonVertOffsetToScreenEdge = 50;

        private const float kThumbStickOuterDiameterPx = 256.0f; //Size in Px of the outter circle (Size of the texture)
        private const float kThumbStickInnerDiameterPx = 128.0f; //Size in Px of the inner circle (Size of the texture and hit area)
        
        private const float kThumbStickTouchDiameterPx = 115.0f; //Diameter in pixels of the area the touch takes effect.  If we want to make this smaller/Bigger than art.
        //If the value is 100. then it would take 100 px to go from -1 input to 1 input on one axis.


        private static Texture2D ThumbStickTop_Texture = null;
        private static Texture2D ThumbStickBottom_Texture = null;

        private static int ThumbstickFingerID = -1;

        private static Vector2 LeftStickCenterPos = Vector2.Zero; //Screen Space pos of center of thumb-stick
        private static Vector2 LeftStickStartTouchPos = Vector2.Zero; //Offset when player starts to touch the thumbstick so art we can adjust from start touch pos.
        private static Vector2 LeftStickValue = Vector2.Zero; //Value from -1 to 1 in both X and Y for left thumb-stick.  This will be fed in the gamepad input if we're in touch mode.

        private static Vector2 GetInnerStickCenterPos()
        {

            Vector2 deltaPos = LeftStickValue * kThumbStickTouchDiameterPx * ControllerScale * 0.5f;

            Vector2 innerStickPos = LeftStickCenterPos + deltaPos;

            return innerStickPos;
        }

        /// <summary>
        /// Returns the state of a given touch button type.
        /// </summary>
        public static ButtonState GetButtonState(TouchButtonType buttonType)
        {
            return buttonState[(int)buttonType];
        }

        public static Vector2 GetLeftStickValue()
        {
            //Flip Y value for real input value so positive value is up.
            return new Vector2( LeftStickValue.X, -(LeftStickValue.Y) );
        }

        public static void LoadContent(bool immediate)
        {
            Debug.Assert(!isInitialized, "TouchButtons was already initialized!");
            isInitialized = true;

            //Thumb-stick textures.
            ThumbStickTop_Texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\VirtualController\ThumbStick_Light");
            ThumbStickBottom_Texture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\VirtualController\ThumbStick_Dark");


            for (int i = 0; i < kNumButtons; i++)
            {
                buttonHitBoxes[i] = new AABB2D();
            }

            //Buttons
            buttonTextures[(int)TouchButtonType.Button_A] = BokuGame.Load<Texture2D>(
                BokuGame.Settings.MediaPath + @"Textures\VirtualController\Button_A");

            buttonTextures[(int)TouchButtonType.Button_B] = BokuGame.Load<Texture2D>(
                BokuGame.Settings.MediaPath + @"Textures\VirtualController\Button_B");

            buttonTextures[(int)TouchButtonType.Button_X] = BokuGame.Load<Texture2D>(
                BokuGame.Settings.MediaPath + @"Textures\VirtualController\Button_X");

            buttonTextures[(int)TouchButtonType.Button_Y] = BokuGame.Load<Texture2D>(
                BokuGame.Settings.MediaPath + @"Textures\VirtualController\Button_Y");


//             //DPad
//             buttonTextures[(int)TouchButtonType.DPad_L] = BokuGame.Load<Texture2D>(
//                 BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton_ControllerDPad_L");
// 
//             buttonTextures[(int)TouchButtonType.DPad_R] = BokuGame.Load<Texture2D>(
//                 BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton_ControllerDPad_R");
// 
//             buttonTextures[(int)TouchButtonType.DPad_U] = BokuGame.Load<Texture2D>(
//                 BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton_ControllerDPad_U");
// 
//             buttonTextures[(int)TouchButtonType.DPad_D] = BokuGame.Load<Texture2D>(
//                 BokuGame.Settings.MediaPath + @"Textures\Programming\TouchButton_ControllerDPad_D");
        }

        public static void UnloadContent()
        {
            isInitialized = false;
            for (int i = 0; i < kNumButtons; i++)
            {
                BokuGame.Release(ref buttonTextures[i]);
            }
        }

        public static void ResetLeftThumbstick()
        {
            ThumbstickFingerID = -1;
            LeftStickStartTouchPos = Vector2.Zero;
            LeftStickValue = Vector2.Zero;
        }

        public static void ResetButtonState()
        {
            for (int i = 0; i < kNumButtons; i++)
            {
                buttonState[i] = ButtonState.Released;
            }
        }

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void DeviceReset(GraphicsDevice device)
        {
        }


        public static void Update()
        {
            // Handles touch input and keeping button state
            bool bCanUpdate = InGame.ShowVirtualController && InGame.UpdateMode.RunSim == InGame.inGame.CurrentUpdateMode;
            bCanUpdate = bCanUpdate && GamePadInput.InputMode.Touch == GamePadInput.ActiveMode;

            
            TouchContact[] touches = TouchInput.Touches;
            Debug.Assert(null != touches);

            if (bCanUpdate && touches.Length > 0 )
            {
                UpdateVirtualController_LeftThumbStick(touches);

                UpdateVirtualController_Buttons(touches);
            }
            else
            {
                //Not in touch mode or no input or not visible
                ResetLeftThumbstick();
                
                ResetButtonState();
            }

        }

        public static bool IsVirtualControllerActedOn()
        {
            bool bTouchOnController = ThumbstickFingerID >= 0;

            for (int i= 0; !bTouchOnController && i<buttonState.Length; ++i)
            {
                bTouchOnController = buttonState[i] == ButtonState.Pressed;
            }
            
            return bTouchOnController;
        }

        private static void UpdateVirtualController_LeftThumbStick(TouchContact[] touches)
        {
            TouchContact touch = null;

            //If the finger ID is valid then look for that finger.  If we can't find it then we look for a new one.
            if (ThumbstickFingerID >= 0)
            {
                for (int i = 0; i < touches.Length; ++i)
                {
                    if (touches[i].fingerId == ThumbstickFingerID)
                    {
                        touch = touches[i];
                        break;
                    }
                }
            }

            //No touch means we look for new Finger ID.
            //This means that we want a finger PRESS on the center circle in order to give that finger control of the stick.
            if (null == touch)
            {
                Vector2 innerCircleCenterPos = GetInnerStickCenterPos();
                
                for (int i = 0; i < touches.Length; ++i)
                {
                    if (TouchPhase.Began == touches[i].phase &&
                        InCircle(touches[i].position, innerCircleCenterPos, (kThumbStickInnerDiameterPx * 0.5f * ControllerScale)))
                    {
                        touch = touches[i];
                        LeftStickStartTouchPos = touches[i].position;
                        ThumbstickFingerID = touches[i].fingerId;
                        break;
                    }
                }
            }

            //If we have a good touch then we can use it's position for the thumbstick.
            if (touch != null)
            {
                //Get latest Stick Value using this touch.
                Vector2 stickToFinger = touch.position - LeftStickStartTouchPos;

                float radius = kThumbStickTouchDiameterPx * ControllerScale * 0.5f;
                Debug.Assert(radius != 0);
                radius = radius != 0 ? radius : 1.0f;

                LeftStickValue = stickToFinger / radius;


                Vector2 normalized = LeftStickValue;
                normalized.Normalize();

                //If values pass normalized values, clamp to normalized.
                if( (normalized.X > 0 && (LeftStickValue.X > normalized.X)) ||
                    (normalized.X < 0 && (LeftStickValue.X < normalized.X)))
                {
                    LeftStickValue.X = normalized.X;
                }

                if ((normalized.Y > 0 && (LeftStickValue.Y > normalized.Y)) ||
                    (normalized.Y < 0 && (LeftStickValue.Y < normalized.Y)))
                {
                    LeftStickValue.Y = normalized.Y;
                }
            }
            else
            {
                //Left stick is not touched.
                ResetLeftThumbstick();
            }
        }

        private static void UpdateVirtualController_Buttons(TouchContact[] touches)
        {
            //Update button states.
            for (int i = 0; i < kNumButtons; i++)
            {
                TouchContact buttonTouch = null;
                for (int j = 0; j < touches.Length; ++j)
                {
                    if (buttonHitBoxes[i].Contains(touches[j].position))
                    {
                        buttonTouch = touches[j];
                        break;
                    }
                }

                if (null != buttonTouch)
                {
                    buttonState[i] = ButtonState.Pressed;
                }
                else
                {
                    buttonState[i] = ButtonState.Released;
                }
            }
        }

        public static void Render()
        {
            //----------
            //Don't render the buttons when not visible or if we're not in simulator mode.
            //
            if ( !InGame.ShowVirtualController || InGame.inGame.CurrentUpdateMode != InGame.UpdateMode.RunSim ||
                VictoryOverlay.Active )
            {
                return;
            }

            //Calculate scale based on Screen Resolution every frame.
            float scaleX = Math.Min((float)BokuGame.bokuGame.GraphicsDevice.Viewport.Width / 1024.0f, 1.0f);
            float scaleY = Math.Min((float)BokuGame.bokuGame.GraphicsDevice.Viewport.Height / 576.0f, 1.0f);

            ControllerScale = Math.Min(scaleX, scaleY);

            LeftStickCenterPos.Y = (float)BokuGame.bokuGame.GraphicsDevice.Viewport.Height;
            LeftStickCenterPos.Y -= (kButtonVertOffsetToScreenEdge + (kThumbStickOuterDiameterPx * ControllerScale * 0.5f));
            LeftStickCenterPos.X = kButtonHorizOffsetToScreenEdge + (kThumbStickOuterDiameterPx * ControllerScale * 0.5f);


            DrawThumbStick();


            Vector2 crossCenterOffset = new Vector2();
            Vector2 buttonSize = defaultButtonSize * ControllerScale;

            //Position the button crosses
            float yPosButtonCross = BokuGame.bokuGame.GraphicsDevice.Viewport.Height;// 600 * ControllerScale;
            yPosButtonCross -= (kButtonVertOffsetToScreenEdge + crossCenterOffset.Y + (buttonSize.Y * 1.5f));

            //---------------------
            //DRAW DPAD

//             float xPosButtonCross = 15 + crossCenterOffset.X + (buttonSize.X * 1.5f);
// 
//             List<TouchButtonType> controllerDPadIdList = new List<TouchButtonType>(){ 
//                 TouchButtonType.DPad_U,
//                 TouchButtonType.DPad_D,
//                 TouchButtonType.DPad_L,
//                 TouchButtonType.DPad_R };
// 
//             DrawButtonInCross(controllerDPadIdList, new Vector2(xPosButtonCross, yPosButtonCross) - (buttonSize * 0.5f), buttonSize, crossCenterOffset);

            //---------------------
            //Draw Buttons A,X,B,Y
            float xPosButtonCross = BokuGame.bokuGame.GraphicsDevice.Viewport.Width;
            xPosButtonCross -= (kButtonHorizOffsetToScreenEdge + crossCenterOffset.Y + (buttonSize.Y * 1.5f));

            List<TouchButtonType> controllerButtonIdList = new List<TouchButtonType>(){ 
                TouchButtonType.Button_Y,
                TouchButtonType.Button_A,
                TouchButtonType.Button_X,
                TouchButtonType.Button_B };

            DrawButtonInCross(controllerButtonIdList, new Vector2(xPosButtonCross, yPosButtonCross) - (buttonSize * 0.5f), buttonSize, crossCenterOffset);
        }

        //Draws the button in the Id list in a cross pattern based on the start pos, button size and center offset from start pos.
        private static void DrawButtonInCross( List<TouchButtonType> buttonIdList, Vector2 startPos, Vector2 buttonSize, Vector2 centerOffset )
        {
            if (null != buttonIdList)
            {
                Vector2 deltaPos = new Vector2();

                for (int i = 0; (i < buttonIdList.Count) && (i < (int)TouchButtonCrossPattern.CrossPattern_Count); ++i)
                {
                    //If button type is not valid skip over.
                    if (buttonIdList[i] >= TouchButtonType.SIZEOF)
                    {
                        Debug.Assert(false);
                        continue;
                    }
                   

                    deltaPos.X = 0.0f;
                    deltaPos.Y = 0.0f;

                    switch ((TouchButtonCrossPattern)i)
                    {
                        case TouchButtonCrossPattern.CrossPattern_Top:
                            deltaPos.Y = -(centerOffset.Y + buttonSize.Y);
                            break;

                        case TouchButtonCrossPattern.CrossPattern_Bottom:
                            deltaPos.Y = centerOffset.Y + buttonSize.Y;
                            break;

                        case TouchButtonCrossPattern.CrossPattern_Left:
                            deltaPos.X = -(centerOffset.X + buttonSize.X);
                            break;

                        case TouchButtonCrossPattern.CrossPattern_Right:
                            deltaPos.X = centerOffset.X + buttonSize.X;
                            break;

                        default:
                            Debug.Assert(false);
                            break;
                    }

                    DrawButton((int)buttonIdList[i], startPos + deltaPos, buttonSize);
                }
            }
        }

        //Draws button with ID at pos with Size.  Only renders if visible.
        private static void DrawButton(int buttonIdx, Vector2 pos, Vector2 size)
        {
            Debug.Assert(buttonIdx >= 0 && buttonIdx < kNumButtons);

            //Set Bounding box.
            buttonHitBoxes[buttonIdx].Set(pos, pos + size);

            Debug.Assert( null != ScreenSpaceQuad.GetInstance() );

            //DRAW
            ScreenSpaceQuad.GetInstance().Render(
                buttonTextures[buttonIdx],
                GetDrawColor((uint)buttonIdx),
                pos, 
                size,
                "TexturedRegularAlpha");
        }

        private static void DrawThumbStick()
        {
            //Bottom
            Vector2 pos = LeftStickCenterPos - new Vector2(kThumbStickOuterDiameterPx * ControllerScale * 0.5f);
            Vector2 size = new Vector2(kThumbStickOuterDiameterPx * ControllerScale);

            Debug.Assert(null != ScreenSpaceQuad.GetInstance());
            ScreenSpaceQuad.GetInstance().Render(
                ThumbStickBottom_Texture,
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                pos,
                size,
                "TexturedRegularAlpha");

            //Top
            pos = GetInnerStickCenterPos() - new Vector2(kThumbStickInnerDiameterPx * ControllerScale * 0.5f);
            size = new Vector2(kThumbStickInnerDiameterPx * ControllerScale);

            ScreenSpaceQuad.GetInstance().Render(
                ThumbStickTop_Texture,
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                pos,
                size,
                "TexturedRegularAlpha");
        }

        private static Vector4 GetDrawColor(uint index)
        {
            if (index < kNumButtons)
            {
                if (buttonState[index] == ButtonState.Released)
                {
                    return new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                }
                else if (buttonState[index] == ButtonState.Pressed)
                {
                    return new Vector4(0.7f, 0.7f, 0.7f, 0.4f);
                }
            }
            return new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        }



        private static bool InCircle(Vector2 testPoint, Vector2 circleCenter, float radius)
        {
            Vector2 dist = testPoint - circleCenter;
            return dist.LengthSquared() <= (radius * radius);
        }
    }

}
