// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Input;

using Boku.Base;

namespace Boku.Common
{
    /// <summary>
    /// A container for gamepad controller button textures 
    /// since they can potentially be used all over.
    /// </summary>
    public class ButtonTextures // : INeedsDeviceReset does this statically...
    {
        #region Members

        static Texture2D aButton;
        static Texture2D bButton;
        static Texture2D xButton;
        static Texture2D yButton;
        static Texture2D dpadUpDown;
        static Texture2D dpadRightLeft;
        static Texture2D dpadUp;
        static Texture2D dpadDown;
        static Texture2D dpadRight;
        static Texture2D dpadLeft;
        static Texture2D leftStick;
        static Texture2D leftStickShadow;
        static Texture2D leftTrigger;
        static Texture2D rightStick;
        static Texture2D rightStickShadow;
        static Texture2D rightTrigger;
        static Texture2D leftTriggerArrow;
        static Texture2D rightTriggerArrow;
        static Texture2D leftShoulderArrow;
        static Texture2D rightShoulderArrow;
        static Texture2D startButtonXbox360;
        static Texture2D backButtonXbox360;
        static Texture2D startButtonXboxOne;
        static Texture2D backButtonXboxOne;
        static Texture2D gamepad;

        static Texture2D keyboard;
        static Texture2D mouse;
        static Texture2D leftMouse;
        static Texture2D middleMouse;
        static Texture2D rightMouse;

        static Texture2D keyFace;
        static Texture2D arrowLeft;
        static Texture2D arrowRight;
        static Texture2D arrowUp;
        static Texture2D arrowDown;

        static Texture2D drag;
        static Texture2D doubleDrag;
        static Texture2D rotate;
        static Texture2D pinch;
        static Texture2D tap;
        static Texture2D doubleTap;
        static Texture2D touchHold;

        static Texture2D brushBigger;
        static Texture2D brushSmaller;

        static Texture2D undo;
        static Texture2D redo;

        static Texture2D touchCursor;

        static Texture2D apple;
        static Texture2D ball;
        static Texture2D balloon;
        static Texture2D blimp;
        static Texture2D boat;
        static Texture2D boku;
        static Texture2D bullet;
        static Texture2D castle;
        static Texture2D clam;
        static Texture2D cloud;
        static Texture2D coin;
        static Texture2D cursor;
        static Texture2D drum;
        static Texture2D factory;
        static Texture2D fan;
        static Texture2D fastbot;
        static Texture2D flyfish;
        static Texture2D heart;
        static Texture2D hut;
        static Texture2D iceBerg;
        static Texture2D inkjet;
        static Texture2D jet;
        static Texture2D light;
        static Texture2D lilypad;
        static Texture2D mine;
        static Texture2D missile;
        static Texture2D octopus;
        static Texture2D pad;
        static Texture2D pipe;
        static Texture2D puck;
        static Texture2D rock;
        static Texture2D rockLowValue;
        static Texture2D rockHighValue;
        static Texture2D rockLowValueUnknown;
        static Texture2D rockHighValueUnknown;
        static Texture2D satellite;
        static Texture2D saucer;
        static Texture2D seagrass;
        static Texture2D star;
        static Texture2D starfish;
        static Texture2D stick;
        static Texture2D sub;
        static Texture2D swimfish;
        static Texture2D terracannon;
        static Texture2D tree;
        static Texture2D turtle;
        static Texture2D rover;
        static Texture2D wisp;

        static Texture2D play;
        static Texture2D homeMenu;
        static Texture2D cameraMove;
        static Texture2D objectEdit;
        static Texture2D objectSettings;
        static Texture2D paths;
        static Texture2D terrainPaint;
        static Texture2D terrainUpDown;
        static Texture2D terrainSmoothLevel;
        static Texture2D terrainSpikeyHilly;
        static Texture2D deleteObjects;
        static Texture2D water;
        static Texture2D worldSettings;
        static Texture2D waterType;
        static Texture2D materialType;
        static Texture2D brushType;

        static Texture2D heartIcon;
        static Texture2D brokenHeartIcon;
        static Texture2D reportAbuseIcon;
        static Texture2D reportAbuseGreyIcon;

        #endregion

        #region Accessors

        public static Texture2D AButton
        {
            get { return aButton; }
        }
        public static Texture2D BButton
        {
            get { return bButton; }
        }
        public static Texture2D XButton
        {
            get { return xButton; }
        }
        public static Texture2D YButton
        {
            get { return yButton; }
        }
        public static Texture2D DPadUpDown
        {
            get { return dpadUpDown; }
        }
        public static Texture2D DPadRightLeft
        {
            get { return dpadRightLeft; }
        }
        public static Texture2D DPadUp
        {
            get { return dpadUp; }
        }
        public static Texture2D DPadDown
        {
            get { return dpadDown; }
        }
        public static Texture2D DPadRight
        {
            get { return dpadRight; }
        }
        public static Texture2D DPadLeft
        {
            get { return dpadLeft; }
        }
        public static Texture2D LeftStick
        {
            get { return leftStick; }
        }
        public static Texture2D LeftStickShadow
        {
            get { return leftStickShadow; }
        }
        public static Texture2D RightStick
        {
            get { return rightStick; }
        }
        public static Texture2D RightStickShadow
        {
            get { return rightStickShadow; }
        }
        public static Texture2D LeftTrigger
        {
            get { return leftTrigger; }
        }
        public static Texture2D RightTrigger
        {
            get { return rightTrigger; }
        }
        public static Texture2D LeftTriggerArrow
        {
            get { return leftTriggerArrow; }
        }
        public static Texture2D RightTriggerArrow
        {
            get { return rightTriggerArrow; }
        }
        public static Texture2D LeftShoulderArrow
        {
            get { return leftShoulderArrow; }
        }
        public static Texture2D RightShoulderArrow
        {
            get { return rightShoulderArrow; }
        }
        public static Texture2D StartButton
        {
            get
            {
                if (GamePadInput.XboxOneControllerFound)
                {
                    return startButtonXboxOne;
                }
                else
                {
                    return startButtonXbox360;
                }
            }
        }
        public static Texture2D BackButton
        {
            get
            {
                if (GamePadInput.XboxOneControllerFound)
                {
                    return backButtonXboxOne;
                }
                else
                {
                    return backButtonXbox360;
                }
            }
        }
        public static Texture2D Gamepad
        {
            get { return gamepad; }
        }

        public static Texture2D Keyboard
        {
            get { return keyboard; }
        }
        public static Texture2D Mouse
        {
            get { return mouse; }
        }
        public static Texture2D LeftMouse
        {
            get { return leftMouse; }
        }
        public static Texture2D MiddleMouse
        {
            get { return middleMouse; }
        }
        public static Texture2D RightMouse
        {
            get { return rightMouse; }
        }

        public static Texture2D KeyFace
        {
            get { return keyFace; }
        }
        public static Texture2D ArrowLeft
        {
            get { return arrowLeft; }
        }
        public static Texture2D ArrowRight
        {
            get { return arrowRight; }
        }
        public static Texture2D ArrowUp
        {
            get { return arrowUp; }
        }
        public static Texture2D ArrowDown
        {
            get { return arrowDown; }
        }

        public static Texture2D Drag
        {
            get { return drag; }
        }
        public static Texture2D DoubleDrag
        {
            get { return doubleDrag; }
        }
        public static Texture2D Rotate
        {
            get { return rotate; }
        }
        public static Texture2D Pinch
        {
            get { return pinch; }
        }
        public static Texture2D Tap
        {
            get { return tap; }
        }
        public static Texture2D DoubleTap
        {
            get { return doubleTap; }
        }
        public static Texture2D TouchHold
        {
            get { return touchHold; }
        }

        public static Texture2D BrushBigger
        {
            get { return brushBigger; }
        }
        public static Texture2D BrushSmaller
        {
            get { return brushSmaller; }
        }

        public static Texture2D Undo
        {
            get { return undo; }
        }
        public static Texture2D Redo
        {
            get { return redo; }
        }

        public static Texture2D TouchCursor
        {
            get { return touchCursor; }
        }

        public static Texture2D Apple
        {
          get { return apple; }
        }
        public static Texture2D Ball
        {
            get { return ball; }
        }
        public static Texture2D Balloon
        {
          get { return balloon; }
        }
        public static Texture2D Blimp
        {
          get { return blimp; }
        }
        public static Texture2D Boat
        {
          get { return boat; }
        }
        public static Texture2D Boku
        {
          get { return boku; }
        }
        public static Texture2D Bullet
        {
          get { return bullet; }
        }
        public static Texture2D Castle
        {
          get { return castle; }
        }
        public static Texture2D Clam
        {
            get { return clam; }
        }
        public static Texture2D Cloud
        {
          get { return cloud; }
        }
        public static Texture2D Coin
        {
          get { return coin; }
        }
        public static Texture2D Cursor
        {
          get { return cursor; }
        }
        public static Texture2D Drum
        {
          get { return drum; }
        }
        public static Texture2D Factory
        {
          get { return factory; }
        }
        public static Texture2D Fan
        {
            get { return fan; }
        }
        public static Texture2D Fastbot
        {
          get { return fastbot; }
        }
        public static Texture2D Flyfish
        {
          get { return flyfish; }
        }
        public static Texture2D Heart
        {
          get { return heart; }
        }
        public static Texture2D Hut
        {
          get { return hut; }
        }
        public static Texture2D IceBerg
        {
            get { return iceBerg; }
        }
        public static Texture2D InkJet
        {
            get { return inkjet; }
        }
        public static Texture2D Jet
        {
          get { return jet; }
        }
        public static Texture2D Light
        {
            get { return light; }
        }
        public static Texture2D Lilypad
        {
            get { return lilypad; }
        }
        public static Texture2D Mine
        {
          get { return mine; }
        }
        public static Texture2D Missile
        {
          get { return missile; }
        }
        public static Texture2D Octopus 
        {
            get { return octopus; }
        }
        public static Texture2D Pad
        {
          get { return pad; }
        }
        public static Texture2D Pipe
        {
            get { return pipe; }
        }
        public static Texture2D Puck
        {
          get { return puck; }
        }
        public static Texture2D Rock
        {
          get { return rock; }
        }
        public static Texture2D RockLowValue
        {
            get { return rockLowValue; }
        }
        public static Texture2D RockHighValue
        {
            get { return rockHighValue; }
        }
        public static Texture2D RockLowValueUnknown
        {
            get { return rockLowValueUnknown; }
        }
        public static Texture2D RockHighValueUnknown
        {
            get { return rockHighValueUnknown; }
        }
        public static Texture2D Satellite
        {
          get { return satellite; }
        }
        public static Texture2D Saucer
        {
          get { return saucer; }
        }
        public static Texture2D Seagrass
        {
            get { return seagrass; }
        }
        public static Texture2D Star
        {
          get { return star; }
        }
        public static Texture2D Starfish
        {
            get { return starfish; }
        }
        public static Texture2D Stick
        {
          get { return stick; }
        }
        public static Texture2D Sub
        {
          get { return sub; }
        }
        public static Texture2D Swimfish
        {
          get { return swimfish; }
        }
        public static Texture2D Terracannon
        {
          get { return terracannon; }
        }
        public static Texture2D Tree
        {
          get { return tree; }
        }
        public static Texture2D Turtle
        {
          get { return turtle; }
        }
        public static Texture2D Rover
        {
            get { return rover; }
        }
        public static Texture2D Wisp
        {
          get { return wisp; }
        }
        public static Texture2D Play
        {
          get { return play; }
        }
        public static Texture2D HomeMenu
        {
          get { return homeMenu; }
        }
        public static Texture2D CameraMove
        {
          get { return cameraMove; }
        }
        public static Texture2D ObjectEdit
        {
          get { return objectEdit; }
        }
        public static Texture2D ObjectSettings
        {
          get { return objectSettings; }
        }
        public static Texture2D Paths
        {
          get { return paths; }
        }
        public static Texture2D TerrainPaint
        {
          get { return terrainPaint; }
        }
        public static Texture2D TerrainUpDown
        {
          get { return terrainUpDown; }
        }
        public static Texture2D TerrainSmoothLevel
        {
          get { return terrainSmoothLevel; }
        }
        public static Texture2D TerrainSpikeyHilly
        {
          get { return terrainSpikeyHilly; }
        }
        public static Texture2D DeleteObjects
        {
          get { return deleteObjects; }
        }
        public static Texture2D Water
        {
          get { return water; }
        }
        public static Texture2D WorldSettings
        {
          get { return worldSettings; }
        }
        public static Texture2D WaterType
        {
          get { return waterType; }
        }
        public static Texture2D MaterialType
        {
          get { return materialType; }
        }
        public static Texture2D BrushType
        {
          get { return brushType; }
        }
        public static Texture2D HeartIcon
        {
            get { return heartIcon; }
        }
        public static Texture2D BrokenHeartIcon
        {
            get { return brokenHeartIcon; }
        }
        public static Texture2D ReportAbuseIcon
        {
            get { return reportAbuseIcon; }
        }
        public static Texture2D ReportAbuseGreyIcon
        {
            get { return reportAbuseGreyIcon; }
        }

        #endregion

        public static void LoadContent(bool immediate)
        {
            aButton = KoiLibrary.LoadTexture2D(@"Textures\UI2D\AButtonWhite");
            bButton = KoiLibrary.LoadTexture2D(@"Textures\UI2D\BButtonWhite");
            xButton = KoiLibrary.LoadTexture2D(@"Textures\UI2D\XButtonWhite");
            yButton = KoiLibrary.LoadTexture2D(@"Textures\UI2D\YButtonWhite");
            dpadUpDown = KoiLibrary.LoadTexture2D(@"Textures\UI2D\DPadUpDown");
            dpadRightLeft = KoiLibrary.LoadTexture2D(@"Textures\UI2D\DPadRightLeft");
            dpadUp = KoiLibrary.LoadTexture2D(@"Textures\UI2D\DPadUp");
            dpadDown = KoiLibrary.LoadTexture2D(@"Textures\UI2D\DPadDown");
            dpadRight = KoiLibrary.LoadTexture2D(@"Textures\UI2D\DPadRight");
            dpadLeft = KoiLibrary.LoadTexture2D(@"Textures\UI2D\DPadLeft");
            leftStick = KoiLibrary.LoadTexture2D(@"Textures\UI2D\LeftStick");
            leftStickShadow = KoiLibrary.LoadTexture2D(@"Textures\UI2D\LeftStickShadow");
            leftTrigger = KoiLibrary.LoadTexture2D(@"Textures\UI2D\LeftTrigger");
            rightStick = KoiLibrary.LoadTexture2D(@"Textures\UI2D\RightStick");
            rightStickShadow = KoiLibrary.LoadTexture2D(@"Textures\UI2D\RightStickShadow");
            rightTrigger = KoiLibrary.LoadTexture2D(@"Textures\UI2D\RightTrigger");
            leftTriggerArrow = KoiLibrary.LoadTexture2D(@"Textures\UI2D\LeftTriggerArrow");
            rightTriggerArrow = KoiLibrary.LoadTexture2D(@"Textures\UI2D\RightTriggerArrow");
            leftShoulderArrow = KoiLibrary.LoadTexture2D(@"Textures\UI2D\LeftShoulderArrow");
            rightShoulderArrow = KoiLibrary.LoadTexture2D(@"Textures\UI2D\RightShoulderArrow");
            startButtonXbox360 = KoiLibrary.LoadTexture2D(@"Textures\UI2D\StartButton");
            backButtonXbox360 = KoiLibrary.LoadTexture2D(@"Textures\UI2D\BackButton");
            startButtonXboxOne = KoiLibrary.LoadTexture2D(@"Textures\UI2D\StartButtonXboxOne");
            backButtonXboxOne = KoiLibrary.LoadTexture2D(@"Textures\UI2D\BackButtonXboxOne");
            
            gamepad = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Gamepad");
            keyboard = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Keyboard");
            mouse = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Mouse");
            leftMouse = KoiLibrary.LoadTexture2D(@"Textures\Buttons\LeftMouse");
            middleMouse = KoiLibrary.LoadTexture2D(@"Textures\Buttons\MiddleMouse");
            rightMouse = KoiLibrary.LoadTexture2D(@"Textures\Buttons\RightMouse");

            keyFace = KoiLibrary.LoadTexture2D(@"Textures\UI2D\KeyFace");
            arrowLeft = KoiLibrary.LoadTexture2D(@"Textures\UI2D\ArrowLeft");
            arrowRight = KoiLibrary.LoadTexture2D(@"Textures\UI2D\ArrowRight");
            arrowUp = KoiLibrary.LoadTexture2D(@"Textures\UI2D\ArrowUp");
            arrowDown = KoiLibrary.LoadTexture2D(@"Textures\UI2D\ArrowDown");

            drag = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Drag");
            doubleDrag = KoiLibrary.LoadTexture2D(@"Textures\Buttons\DoubleDrag");
            rotate = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Rotate");
            pinch = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Pinch");
            tap = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Tap");
            doubleTap = KoiLibrary.LoadTexture2D(@"Textures\Buttons\DoubleTap");
            touchHold = KoiLibrary.LoadTexture2D(@"Textures\Buttons\TouchHold");

            brushBigger = KoiLibrary.LoadTexture2D(@"Textures\Buttons\BrushBigger");
            brushSmaller = KoiLibrary.LoadTexture2D(@"Textures\Buttons\BrushSmaller");

            undo = KoiLibrary.LoadTexture2D(@"Textures\UI2D\Undo");
            redo = KoiLibrary.LoadTexture2D(@"Textures\UI2D\Redo");

            touchCursor = KoiLibrary.LoadTexture2D(@"Textures\UI2D\TouchCursor");

            apple = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Apple");
            ball = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Ball");
            balloon = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Balloon");
            blimp = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Blimp");
            boat = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Boat");
            boku = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Boku");
            bullet = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Bullet");
            castle = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Castle");
            cloud = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Cloud");
            coin = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Coin");
            clam = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Clam");
            cursor = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Cursor");
            drum = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Drum");
            factory = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Factory");
            fan = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Fan");
            fastbot = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Fastbot");
            flyfish = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Flyfish");
            heart = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Heart");
            hut = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Hut");
            iceBerg = KoiLibrary.LoadTexture2D(@"Textures\Buttons\IceBerg");
            inkjet = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Inkjet");
            jet = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Jet");
            light = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Light");
            lilypad = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Lilypad");
            mine = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Mine");
            missile = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Missile");
            octopus = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Octopus");
            pad = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Pad");
            pipe = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Pipe");
            puck = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Puck");
            rock = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Rock");
            rockLowValue = KoiLibrary.LoadTexture2D(@"Textures\Buttons\RockLowValue");
            rockHighValue = KoiLibrary.LoadTexture2D(@"Textures\Buttons\RockHighValue");
            rockLowValueUnknown = KoiLibrary.LoadTexture2D(@"Textures\Buttons\RockLowValueUnknown");
            rockHighValueUnknown = KoiLibrary.LoadTexture2D(@"Textures\Buttons\RockHighValueUnknown");
            satellite = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Satellite");
            saucer = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Saucer");
            seagrass = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Seagrass");
            star = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Star");
            starfish = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Starfish");
            stick = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Stick");
            sub = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Sub");
            swimfish = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Swimfish");
            terracannon = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Terracannon");
            tree = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Tree");
            turtle = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Turtle");
            rover = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Rover");
            wisp = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Wisp");

            play = KoiLibrary.LoadTexture2D(@"Textures\Buttons\play");
            homeMenu = KoiLibrary.LoadTexture2D(@"Textures\Buttons\HomeMenu");
            cameraMove = KoiLibrary.LoadTexture2D(@"Textures\Buttons\CameraMove");
            objectEdit = KoiLibrary.LoadTexture2D(@"Textures\Buttons\ObjectEdit");
            objectSettings = KoiLibrary.LoadTexture2D(@"Textures\Buttons\ObjectSettings");
            paths = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Paths");
            terrainPaint = KoiLibrary.LoadTexture2D(@"Textures\Buttons\TerrainPaint");
            terrainUpDown = KoiLibrary.LoadTexture2D(@"Textures\Buttons\TerrainUpDown");
            terrainSmoothLevel = KoiLibrary.LoadTexture2D(@"Textures\Buttons\TerrainSmoothLevel");
            terrainSpikeyHilly = KoiLibrary.LoadTexture2D(@"Textures\Buttons\TerrainSpikeyHilly");
            deleteObjects = KoiLibrary.LoadTexture2D(@"Textures\Buttons\DeleteObjects");
            water = KoiLibrary.LoadTexture2D(@"Textures\Buttons\Water");
            worldSettings = KoiLibrary.LoadTexture2D(@"Textures\Buttons\WorldSettings");
            waterType = KoiLibrary.LoadTexture2D(@"Textures\Buttons\WaterType");
            materialType = KoiLibrary.LoadTexture2D(@"Textures\Buttons\MaterialType");
            brushType = KoiLibrary.LoadTexture2D(@"Textures\Buttons\BrushType");

            heartIcon = KoiLibrary.LoadTexture2D(@"Textures\Buttons\HeartIcon");
            brokenHeartIcon = KoiLibrary.LoadTexture2D(@"Textures\Buttons\BrokenHeartIcon");
            reportAbuseIcon = KoiLibrary.LoadTexture2D(@"Textures\Buttons\ReportAbuseIcon");
            reportAbuseGreyIcon = KoiLibrary.LoadTexture2D(@"Textures\Buttons\ReportAbuseGreyIcon");

        }   // end of ButtonTextures LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            DeviceResetX.Release(ref aButton);
            DeviceResetX.Release(ref bButton);
            DeviceResetX.Release(ref xButton);
            DeviceResetX.Release(ref yButton);
            DeviceResetX.Release(ref dpadUpDown);
            DeviceResetX.Release(ref dpadRightLeft);
            DeviceResetX.Release(ref dpadUp);
            DeviceResetX.Release(ref dpadDown);
            DeviceResetX.Release(ref dpadRight);
            DeviceResetX.Release(ref dpadLeft);
            DeviceResetX.Release(ref leftStick);
            DeviceResetX.Release(ref leftStickShadow);
            DeviceResetX.Release(ref leftTrigger);
            DeviceResetX.Release(ref rightStick);
            DeviceResetX.Release(ref rightStickShadow);
            DeviceResetX.Release(ref rightTrigger);
            DeviceResetX.Release(ref leftTriggerArrow);
            DeviceResetX.Release(ref rightTriggerArrow);
            DeviceResetX.Release(ref leftShoulderArrow);
            DeviceResetX.Release(ref rightShoulderArrow);
            DeviceResetX.Release(ref startButtonXbox360);
            DeviceResetX.Release(ref backButtonXbox360);
            DeviceResetX.Release(ref startButtonXboxOne);
            DeviceResetX.Release(ref backButtonXboxOne);

            DeviceResetX.Release(ref gamepad);
            DeviceResetX.Release(ref keyboard);
            DeviceResetX.Release(ref mouse);
            DeviceResetX.Release(ref leftMouse);
            DeviceResetX.Release(ref middleMouse);
            DeviceResetX.Release(ref rightMouse);

            DeviceResetX.Release(ref keyFace);
            DeviceResetX.Release(ref arrowLeft);
            DeviceResetX.Release(ref arrowRight);
            DeviceResetX.Release(ref arrowUp);
            DeviceResetX.Release(ref arrowDown);

            DeviceResetX.Release(ref drag);
            DeviceResetX.Release(ref doubleDrag);
            DeviceResetX.Release(ref rotate);
            DeviceResetX.Release(ref pinch);
            DeviceResetX.Release(ref tap);
            DeviceResetX.Release(ref doubleTap);
            DeviceResetX.Release(ref touchHold);

            DeviceResetX.Release(ref brushBigger);
            DeviceResetX.Release(ref brushSmaller);

            DeviceResetX.Release(ref undo);
            DeviceResetX.Release(ref redo);

            DeviceResetX.Release(ref touchCursor);

            DeviceResetX.Release(ref apple);
            DeviceResetX.Release(ref ball);
            DeviceResetX.Release(ref balloon);
            DeviceResetX.Release(ref blimp);
            DeviceResetX.Release(ref boat);
            DeviceResetX.Release(ref boku);
            DeviceResetX.Release(ref bullet);
            DeviceResetX.Release(ref castle);
            DeviceResetX.Release(ref cloud);
            DeviceResetX.Release(ref coin);
            DeviceResetX.Release(ref cursor);
            DeviceResetX.Release(ref drum);
            DeviceResetX.Release(ref factory);
            DeviceResetX.Release(ref fan);
            DeviceResetX.Release(ref fastbot);
            DeviceResetX.Release(ref flyfish);
            DeviceResetX.Release(ref heart);
            DeviceResetX.Release(ref hut);
            DeviceResetX.Release(ref iceBerg);
            DeviceResetX.Release(ref inkjet);
            DeviceResetX.Release(ref jet);
            DeviceResetX.Release(ref light);
            DeviceResetX.Release(ref mine);
            DeviceResetX.Release(ref missile);
            DeviceResetX.Release(ref octopus);
            DeviceResetX.Release(ref pad);
            DeviceResetX.Release(ref pipe);
            DeviceResetX.Release(ref puck);
            DeviceResetX.Release(ref rock);
            DeviceResetX.Release(ref rockLowValue);
            DeviceResetX.Release(ref rockHighValue);
            DeviceResetX.Release(ref rockLowValueUnknown);
            DeviceResetX.Release(ref rockHighValueUnknown);
            DeviceResetX.Release(ref satellite);
            DeviceResetX.Release(ref saucer);
            DeviceResetX.Release(ref star);
            DeviceResetX.Release(ref starfish);
            DeviceResetX.Release(ref stick);
            DeviceResetX.Release(ref sub);
            DeviceResetX.Release(ref swimfish);
            DeviceResetX.Release(ref terracannon);
            DeviceResetX.Release(ref tree);
            DeviceResetX.Release(ref turtle);
            DeviceResetX.Release(ref rover);
            DeviceResetX.Release(ref wisp);

            DeviceResetX.Release(ref play);
            DeviceResetX.Release(ref homeMenu);
            DeviceResetX.Release(ref cameraMove);
            DeviceResetX.Release(ref objectEdit);
            DeviceResetX.Release(ref objectSettings);
            DeviceResetX.Release(ref paths);
            DeviceResetX.Release(ref terrainPaint);
            DeviceResetX.Release(ref terrainUpDown);
            DeviceResetX.Release(ref terrainSmoothLevel);
            DeviceResetX.Release(ref terrainSpikeyHilly);
            DeviceResetX.Release(ref deleteObjects);
            DeviceResetX.Release(ref water);
            DeviceResetX.Release(ref worldSettings);
            DeviceResetX.Release(ref waterType);
            DeviceResetX.Release(ref materialType);
            DeviceResetX.Release(ref brushType);

            DeviceResetX.Release(ref heartIcon);
            DeviceResetX.Release(ref brokenHeartIcon);
            DeviceResetX.Release(ref reportAbuseIcon);
            DeviceResetX.Release(ref reportAbuseGreyIcon);

        }   // end of ButtonTextures UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class ButtonTextures

}   // end of namespace Boku.Common   
