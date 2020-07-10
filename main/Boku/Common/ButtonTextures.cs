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
            aButton = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\AButtonWhite");
            bButton = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\BButtonWhite");
            xButton = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\XButtonWhite");
            yButton = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\YButtonWhite");
            dpadUpDown = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\DPadUpDown");
            dpadRightLeft = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\DPadRightLeft");
            dpadUp = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\DPadUp");
            dpadDown = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\DPadDown");
            dpadRight = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\DPadRight");
            dpadLeft = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\DPadLeft");
            leftStick = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\LeftStick");
            leftStickShadow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\LeftStickShadow");
            leftTrigger = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\LeftTrigger");
            rightStick = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\RightStick");
            rightStickShadow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\RightStickShadow");
            rightTrigger = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\RightTrigger");
            leftTriggerArrow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\LeftTriggerArrow");
            rightTriggerArrow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\RightTriggerArrow");
            leftShoulderArrow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\LeftShoulderArrow");
            rightShoulderArrow = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\RightShoulderArrow");
            startButtonXbox360 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\StartButton");
            backButtonXbox360 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\BackButton");
            startButtonXboxOne = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\StartButtonXboxOne");
            backButtonXboxOne = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\BackButtonXboxOne");
            
            gamepad = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Gamepad");
            keyboard = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Keyboard");
            mouse = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Mouse");
            leftMouse = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\LeftMouse");
            middleMouse = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\MiddleMouse");
            rightMouse = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\RightMouse");

            keyFace = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\KeyFace");
            arrowLeft = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\ArrowLeft");
            arrowRight = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\ArrowRight");
            arrowUp = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\ArrowUp");
            arrowDown = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\ArrowDown");

            drag = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Drag");
            doubleDrag = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\DoubleDrag");
            rotate = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Rotate");
            pinch = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Pinch");
            tap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Tap");
            doubleTap = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\DoubleTap");
            touchHold = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\TouchHold");

            brushBigger = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\BrushBigger");
            brushSmaller = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\BrushSmaller");

            undo = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\Undo");
            redo = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\Redo");

            touchCursor = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\UI2D\TouchCursor");

            apple = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Apple");
            ball = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Ball");
            balloon = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Balloon");
            blimp = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Blimp");
            boat = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Boat");
            boku = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Boku");
            bullet = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Bullet");
            castle = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Castle");
            cloud = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Cloud");
            coin = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Coin");
            clam = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Clam");
            cursor = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Cursor");
            drum = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Drum");
            factory = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Factory");
            fan = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Fan");
            fastbot = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Fastbot");
            flyfish = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Flyfish");
            heart = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Heart");
            hut = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Hut");
            iceBerg = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\IceBerg");
            inkjet = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Inkjet");
            jet = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Jet");
            light = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Light");
            lilypad = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Lilypad");
            mine = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Mine");
            missile = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Missile");
            octopus = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Octopus");
            pad = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Pad");
            pipe = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Pipe");
            puck = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Puck");
            rock = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Rock");
            rockLowValue = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\RockLowValue");
            rockHighValue = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\RockHighValue");
            rockLowValueUnknown = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\RockLowValueUnknown");
            rockHighValueUnknown = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\RockHighValueUnknown");
            satellite = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Satellite");
            saucer = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Saucer");
            seagrass = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Seagrass");
            star = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Star");
            starfish = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Starfish");
            stick = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Stick");
            sub = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Sub");
            swimfish = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Swimfish");
            terracannon = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Terracannon");
            tree = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Tree");
            turtle = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Turtle");
            rover = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Rover");
            wisp = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Wisp");

            play = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\play");
            homeMenu = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\HomeMenu");
            cameraMove = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\CameraMove");
            objectEdit = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\ObjectEdit");
            objectSettings = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\ObjectSettings");
            paths = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Paths");
            terrainPaint = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\TerrainPaint");
            terrainUpDown = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\TerrainUpDown");
            terrainSmoothLevel = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\TerrainSmoothLevel");
            terrainSpikeyHilly = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\TerrainSpikeyHilly");
            deleteObjects = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\DeleteObjects");
            water = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\Water");
            worldSettings = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\WorldSettings");
            waterType = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\WaterType");
            materialType = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\MaterialType");
            brushType = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\BrushType");

            heartIcon = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\HeartIcon");
            brokenHeartIcon = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\BrokenHeartIcon");
            reportAbuseIcon = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\ReportAbuseIcon");
            reportAbuseGreyIcon = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\Buttons\ReportAbuseGreyIcon");

        }   // end of ButtonTextures LoadContent()

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            BokuGame.Release(ref aButton);
            BokuGame.Release(ref bButton);
            BokuGame.Release(ref xButton);
            BokuGame.Release(ref yButton);
            BokuGame.Release(ref dpadUpDown);
            BokuGame.Release(ref dpadRightLeft);
            BokuGame.Release(ref dpadUp);
            BokuGame.Release(ref dpadDown);
            BokuGame.Release(ref dpadRight);
            BokuGame.Release(ref dpadLeft);
            BokuGame.Release(ref leftStick);
            BokuGame.Release(ref leftStickShadow);
            BokuGame.Release(ref leftTrigger);
            BokuGame.Release(ref rightStick);
            BokuGame.Release(ref rightStickShadow);
            BokuGame.Release(ref rightTrigger);
            BokuGame.Release(ref leftTriggerArrow);
            BokuGame.Release(ref rightTriggerArrow);
            BokuGame.Release(ref leftShoulderArrow);
            BokuGame.Release(ref rightShoulderArrow);
            BokuGame.Release(ref startButtonXbox360);
            BokuGame.Release(ref backButtonXbox360);
            BokuGame.Release(ref startButtonXboxOne);
            BokuGame.Release(ref backButtonXboxOne);

            BokuGame.Release(ref gamepad);
            BokuGame.Release(ref keyboard);
            BokuGame.Release(ref mouse);
            BokuGame.Release(ref leftMouse);
            BokuGame.Release(ref middleMouse);
            BokuGame.Release(ref rightMouse);

            BokuGame.Release(ref keyFace);
            BokuGame.Release(ref arrowLeft);
            BokuGame.Release(ref arrowRight);
            BokuGame.Release(ref arrowUp);
            BokuGame.Release(ref arrowDown);

            BokuGame.Release(ref drag);
            BokuGame.Release(ref doubleDrag);
            BokuGame.Release(ref rotate);
            BokuGame.Release(ref pinch);
            BokuGame.Release(ref tap);
            BokuGame.Release(ref doubleTap);
            BokuGame.Release(ref touchHold);

            BokuGame.Release(ref brushBigger);
            BokuGame.Release(ref brushSmaller);

            BokuGame.Release(ref undo);
            BokuGame.Release(ref redo);

            BokuGame.Release(ref touchCursor);

            BokuGame.Release(ref apple);
            BokuGame.Release(ref ball);
            BokuGame.Release(ref balloon);
            BokuGame.Release(ref blimp);
            BokuGame.Release(ref boat);
            BokuGame.Release(ref boku);
            BokuGame.Release(ref bullet);
            BokuGame.Release(ref castle);
            BokuGame.Release(ref cloud);
            BokuGame.Release(ref coin);
            BokuGame.Release(ref cursor);
            BokuGame.Release(ref drum);
            BokuGame.Release(ref factory);
            BokuGame.Release(ref fan);
            BokuGame.Release(ref fastbot);
            BokuGame.Release(ref flyfish);
            BokuGame.Release(ref heart);
            BokuGame.Release(ref hut);
            BokuGame.Release(ref iceBerg);
            BokuGame.Release(ref inkjet);
            BokuGame.Release(ref jet);
            BokuGame.Release(ref light);
            BokuGame.Release(ref mine);
            BokuGame.Release(ref missile);
            BokuGame.Release(ref octopus);
            BokuGame.Release(ref pad);
            BokuGame.Release(ref pipe);
            BokuGame.Release(ref puck);
            BokuGame.Release(ref rock);
            BokuGame.Release(ref rockLowValue);
            BokuGame.Release(ref rockHighValue);
            BokuGame.Release(ref rockLowValueUnknown);
            BokuGame.Release(ref rockHighValueUnknown);
            BokuGame.Release(ref satellite);
            BokuGame.Release(ref saucer);
            BokuGame.Release(ref star);
            BokuGame.Release(ref starfish);
            BokuGame.Release(ref stick);
            BokuGame.Release(ref sub);
            BokuGame.Release(ref swimfish);
            BokuGame.Release(ref terracannon);
            BokuGame.Release(ref tree);
            BokuGame.Release(ref turtle);
            BokuGame.Release(ref rover);
            BokuGame.Release(ref wisp);

            BokuGame.Release(ref play);
            BokuGame.Release(ref homeMenu);
            BokuGame.Release(ref cameraMove);
            BokuGame.Release(ref objectEdit);
            BokuGame.Release(ref objectSettings);
            BokuGame.Release(ref paths);
            BokuGame.Release(ref terrainPaint);
            BokuGame.Release(ref terrainUpDown);
            BokuGame.Release(ref terrainSmoothLevel);
            BokuGame.Release(ref terrainSpikeyHilly);
            BokuGame.Release(ref deleteObjects);
            BokuGame.Release(ref water);
            BokuGame.Release(ref worldSettings);
            BokuGame.Release(ref waterType);
            BokuGame.Release(ref materialType);
            BokuGame.Release(ref brushType);

            BokuGame.Release(ref heartIcon);
            BokuGame.Release(ref brokenHeartIcon);
            BokuGame.Release(ref reportAbuseIcon);
            BokuGame.Release(ref reportAbuseGreyIcon);

        }   // end of ButtonTextures UnloadContent()

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class ButtonTextures

}   // end of namespace Boku.Common   
