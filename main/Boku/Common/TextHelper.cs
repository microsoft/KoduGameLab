// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.UI2D;
using Boku.Fx;
using Boku.Programming;

namespace Boku.Common
{
    /// <summary>
    /// Gathering place for helpful text functions.
    /// </summary>
    public class TextHelper
    {
        /// <summary>
        /// Enum for controller inputs used when mapping alias
        /// strings to icons for displaying icons in text.
        /// </summary>
        public enum ControlInputs
        {
            none,
            aButton,
            bButton,
            xButton,
            yButton,
            leftStick,
            rightStick,
            leftShoulder,
            rightShoulder,
            leftTrigger,
            rightTrigger,
            dpadUpDown,
            dpadRightLeft,
            dpadUp,
            dpadDown,
            dpadRight,
            dpadLeft,
            start,
            back,

            apple,
            ball,
            balloon,
            blimp,
            boat,
            boku,
            bullet,
            castle,
            clam,
            cloud,
            coin,
            cursor,
            drum,
            factory,
            fan,
            fastbot,
            flyfish,
            heart,
            hut,
            inkjet,
            iceBerg,
            jet,
            light,
            lilypad,
            mine,
            missile,
            octopus,
            pad,
            pipe,
            puck,
            rock,
            rockLowValue,
            rockHighValue,
            rockLowValueUnknown,
            rockHighValueUnknown,
            rockUnknown,
            satellite,
            saucer,
            seagrass,
            star,
            starfish,
            stick,
            sub,
            swimfish,
            terracannon,
            tree,
            turtle,
            rover,
            wisp,

            key,        // Used for all key renderings.

            keyboard,
            mouse,
            leftmouse,
            middlemouse,
            rightmouse,
            gamepad,

            touch,        // Used for all touch bits
            drag,
            doubleDrag,
            rotate,
            pinch,
            tap,
            doubleTap,
            touchHold,

            brushBigger,
            brushSmaller,

            play,           // Tool menus.
            homeMenu,
            cameraMove,
            objectEdit,
            objectSettings,
            paths,
            terrainPaint,
            terrainUpDown,
            terrainSmoothLevel,
            terrainSpikeyHilly,
            deleteObjects,
            water,
            worldSettings,

            waterType,      // Picker submenus.
            materialType,
            brushType,

            heartIcon,
            brokenHeartIcon,
            reportAbuseIcon,
            reportAbuseGreyIcon,

            undo,
            redo,

            programmingTile,

            nonButton,  // All icons should go above here, text substitions below here.

            scoreblack,
            scorewhite,
            scoregrey,
            scorered,
            scoregreen,
            scoreblue,
            scoreorange,
            scorepurple,
            scoreyellow,
            scorepink,
            scorebrown,

            scorea,
            scoreb,
            scorec,
            scored,
            scoree,
            scoref,
            scoreg,
            scoreh,
            scorei,
            scorej,
            scorek,
            scorel,
            scorem,
            scoren,
            scoreo,
            scorep,
            scoreq,
            scorer,
            scores,
            scoret,
            scoreu,
            scorev,
            scorew,
            scorex,
            scorey,
            scorez,

            privatescorea,
            privatescoreb,
            privatescorec,
            privatescored,
            privatescoree,
            privatescoref,
            privatescoreg,
            privatescoreh,
            privatescorei,
            privatescorej,
            privatescorek,
            privatescorel,
            privatescorem,
            privatescoren,
            privatescoreo,
            privatescorep,
            privatescoreq,
            privatescorer,
            privatescores,
            privatescoret,
            privatescoreu,
            privatescorev,
            privatescorew,
            privatescorex,
            privatescorey,
            privatescorez,

            arrowleft,  // A bit of a special case since these are combined with the key icon.
            arrowright,
            arrowup,
            arrowdown,

        }

        private static TextBlob blob = null;

        /// <summary>
        /// Maps localized strings to the appropriate input enum.
        /// </summary>
        private static Dictionary<string, ControlInputs> buttonAliasDictionary = null;

        private static void InitButtonAliasDictionary()
        {
            if (buttonAliasDictionary == null)
            {
                buttonAliasDictionary = new Dictionary<string,ControlInputs>();

                foreach(string str in Strings.LocalizeAll("buttonAliasStrings.aButton.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.aButton;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.bButton.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.bButton;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.xButton.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.xButton;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.yButton.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.yButton;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.leftStick.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.leftStick;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rightStick.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rightStick;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.leftShoulder.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.leftShoulder;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rightShoulder.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rightShoulder;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.leftTrigger.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.leftTrigger;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rightTrigger.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rightTrigger;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.dpadUpDown.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.dpadUpDown;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.dpadRightLeft.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.dpadRightLeft;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.dpadUp.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.dpadUp;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.dpadDown.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.dpadDown;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.dpadRight.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.dpadRight;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.dpadLeft.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.dpadLeft;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.start.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.start;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.back.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.back;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.apple.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.apple;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.ball.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.ball;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.balloon.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.balloon;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.blimp.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.blimp;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.boat.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.boat;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.boku.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.boku;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.bullet.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.bullet;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.castle.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.castle;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.clam.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.clam;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.cloud.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.cloud;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.coin.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.coin;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.cursor.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.cursor;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.drum.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.drum;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.factory.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.factory;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.fan.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.fan;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.fastbot.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.fastbot;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.flyfish.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.flyfish;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.heart.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.heart;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.hut.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.hut;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.iceBerg.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.iceBerg;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.inkjet.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.inkjet;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.jet.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.jet;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.light.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.light;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.lilypad.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.lilypad;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.mine.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.mine;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.missile.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.missile;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.octopus.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.octopus;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.pad.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.pad;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.pipe.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.pipe;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.puck.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.puck;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rock.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rock;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rockLowValue.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rockLowValue;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rockHighValue.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rockHighValue;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rockLowValueUnknown.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rockLowValueUnknown;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rockHighValueUnknown.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rockHighValueUnknown;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.satellite.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.satellite;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.saucer.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.saucer;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.seagrass.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.seagrass;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.star.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.star;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.starfish.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.starfish;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.stick.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.stick;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.sub.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.sub;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.swimfish.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.swimfish;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.terracannon.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.terracannon;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.tree.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.tree;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.turtle.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.turtle;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rover.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rover;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.wisp.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.wisp;
                }

                //
                // Non-Button aliases
                //

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scoreblack.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scoreblack;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scorewhite.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scorewhite;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scoregrey.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scoregrey;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scorered.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scorered;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scoregreen.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scoregreen;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scoreblue.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scoreblue;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scoreorange.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scoreorange;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scorepurple.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scorepurple;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scoreyellow.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scoreyellow;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scorebrown.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scorebrown;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.scorepink.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.scorepink;
                }

                buttonAliasDictionary["score a"] = ControlInputs.scorea;
                buttonAliasDictionary["score b"] = ControlInputs.scoreb;
                buttonAliasDictionary["score c"] = ControlInputs.scorec;
                buttonAliasDictionary["score d"] = ControlInputs.scored;
                buttonAliasDictionary["score e"] = ControlInputs.scoree;
                buttonAliasDictionary["score f"] = ControlInputs.scoref;
                buttonAliasDictionary["score g"] = ControlInputs.scoreg;
                buttonAliasDictionary["score h"] = ControlInputs.scoreh;
                buttonAliasDictionary["score i"] = ControlInputs.scorei;
                buttonAliasDictionary["score j"] = ControlInputs.scorej;
                buttonAliasDictionary["score k"] = ControlInputs.scorek;
                buttonAliasDictionary["score l"] = ControlInputs.scorel;
                buttonAliasDictionary["score m"] = ControlInputs.scorem;
                buttonAliasDictionary["score n"] = ControlInputs.scoren;
                buttonAliasDictionary["score o"] = ControlInputs.scoreo;
                buttonAliasDictionary["score p"] = ControlInputs.scorep;
                buttonAliasDictionary["score q"] = ControlInputs.scoreq;
                buttonAliasDictionary["score r"] = ControlInputs.scorer;
                buttonAliasDictionary["score s"] = ControlInputs.scores;
                buttonAliasDictionary["score t"] = ControlInputs.scoret;
                buttonAliasDictionary["score u"] = ControlInputs.scoreu;
                buttonAliasDictionary["score v"] = ControlInputs.scorev;
                buttonAliasDictionary["score w"] = ControlInputs.scorew;
                buttonAliasDictionary["score x"] = ControlInputs.scorex;
                buttonAliasDictionary["score y"] = ControlInputs.scorey;
                buttonAliasDictionary["score z"] = ControlInputs.scorez;

                buttonAliasDictionary["scorea"] = ControlInputs.scorea;
                buttonAliasDictionary["scoreb"] = ControlInputs.scoreb;
                buttonAliasDictionary["scorec"] = ControlInputs.scorec;
                buttonAliasDictionary["scored"] = ControlInputs.scored;
                buttonAliasDictionary["scoree"] = ControlInputs.scoree;
                buttonAliasDictionary["scoref"] = ControlInputs.scoref;
                buttonAliasDictionary["scoreg"] = ControlInputs.scoreg;
                buttonAliasDictionary["scoreh"] = ControlInputs.scoreh;
                buttonAliasDictionary["scorei"] = ControlInputs.scorei;
                buttonAliasDictionary["scorej"] = ControlInputs.scorej;
                buttonAliasDictionary["scorek"] = ControlInputs.scorek;
                buttonAliasDictionary["scorel"] = ControlInputs.scorel;
                buttonAliasDictionary["scorem"] = ControlInputs.scorem;
                buttonAliasDictionary["scoren"] = ControlInputs.scoren;
                buttonAliasDictionary["scoreo"] = ControlInputs.scoreo;
                buttonAliasDictionary["scorep"] = ControlInputs.scorep;
                buttonAliasDictionary["scoreq"] = ControlInputs.scoreq;
                buttonAliasDictionary["scorer"] = ControlInputs.scorer;
                buttonAliasDictionary["scores"] = ControlInputs.scores;
                buttonAliasDictionary["scoret"] = ControlInputs.scoret;
                buttonAliasDictionary["scoreu"] = ControlInputs.scoreu;
                buttonAliasDictionary["scorev"] = ControlInputs.scorev;
                buttonAliasDictionary["scorew"] = ControlInputs.scorew;
                buttonAliasDictionary["scorex"] = ControlInputs.scorex;
                buttonAliasDictionary["scorey"] = ControlInputs.scorey;
                buttonAliasDictionary["scorez"] = ControlInputs.scorez;

                buttonAliasDictionary["private score a"] = ControlInputs.privatescorea;
                buttonAliasDictionary["private score b"] = ControlInputs.privatescoreb;
                buttonAliasDictionary["private score c"] = ControlInputs.privatescorec;
                buttonAliasDictionary["private score d"] = ControlInputs.privatescored;
                buttonAliasDictionary["private score e"] = ControlInputs.privatescoree;
                buttonAliasDictionary["private score f"] = ControlInputs.privatescoref;
                buttonAliasDictionary["private score g"] = ControlInputs.privatescoreg;
                buttonAliasDictionary["private score h"] = ControlInputs.privatescoreh;
                buttonAliasDictionary["private score i"] = ControlInputs.privatescorei;
                buttonAliasDictionary["private score j"] = ControlInputs.privatescorej;
                buttonAliasDictionary["private score k"] = ControlInputs.privatescorek;
                buttonAliasDictionary["private score l"] = ControlInputs.privatescorel;
                buttonAliasDictionary["private score m"] = ControlInputs.privatescorem;
                buttonAliasDictionary["private score n"] = ControlInputs.privatescoren;
                buttonAliasDictionary["private score o"] = ControlInputs.privatescoreo;
                buttonAliasDictionary["private score p"] = ControlInputs.privatescorep;
                buttonAliasDictionary["private score q"] = ControlInputs.privatescoreq;
                buttonAliasDictionary["private score r"] = ControlInputs.privatescorer;
                buttonAliasDictionary["private score s"] = ControlInputs.privatescores;
                buttonAliasDictionary["private score t"] = ControlInputs.privatescoret;
                buttonAliasDictionary["private score u"] = ControlInputs.privatescoreu;
                buttonAliasDictionary["private score v"] = ControlInputs.privatescorev;
                buttonAliasDictionary["private score w"] = ControlInputs.privatescorew;
                buttonAliasDictionary["private score x"] = ControlInputs.privatescorex;
                buttonAliasDictionary["private score y"] = ControlInputs.privatescorey;
                buttonAliasDictionary["private score z"] = ControlInputs.privatescorez;

                buttonAliasDictionary["privatescorea"] = ControlInputs.privatescorea;
                buttonAliasDictionary["privatescoreb"] = ControlInputs.privatescoreb;
                buttonAliasDictionary["privatescorec"] = ControlInputs.privatescorec;
                buttonAliasDictionary["privatescored"] = ControlInputs.privatescored;
                buttonAliasDictionary["privatescoree"] = ControlInputs.privatescoree;
                buttonAliasDictionary["privatescoref"] = ControlInputs.privatescoref;
                buttonAliasDictionary["privatescoreg"] = ControlInputs.privatescoreg;
                buttonAliasDictionary["privatescoreh"] = ControlInputs.privatescoreh;
                buttonAliasDictionary["privatescorei"] = ControlInputs.privatescorei;
                buttonAliasDictionary["privatescorej"] = ControlInputs.privatescorej;
                buttonAliasDictionary["privatescorek"] = ControlInputs.privatescorek;
                buttonAliasDictionary["privatescorel"] = ControlInputs.privatescorel;
                buttonAliasDictionary["privatescorem"] = ControlInputs.privatescorem;
                buttonAliasDictionary["privatescoren"] = ControlInputs.privatescoren;
                buttonAliasDictionary["privatescoreo"] = ControlInputs.privatescoreo;
                buttonAliasDictionary["privatescorep"] = ControlInputs.privatescorep;
                buttonAliasDictionary["privatescoreq"] = ControlInputs.privatescoreq;
                buttonAliasDictionary["privatescorer"] = ControlInputs.privatescorer;
                buttonAliasDictionary["privatescores"] = ControlInputs.privatescores;
                buttonAliasDictionary["privatescoret"] = ControlInputs.privatescoret;
                buttonAliasDictionary["privatescoreu"] = ControlInputs.privatescoreu;
                buttonAliasDictionary["privatescorev"] = ControlInputs.privatescorev;
                buttonAliasDictionary["privatescorew"] = ControlInputs.privatescorew;
                buttonAliasDictionary["privatescorex"] = ControlInputs.privatescorex;
                buttonAliasDictionary["privatescorey"] = ControlInputs.privatescorey;
                buttonAliasDictionary["privatescorez"] = ControlInputs.privatescorez;

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.arrowleft.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.arrowleft;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.arrowright.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.arrowright;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.arrowup.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.arrowup;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.arrowdown.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.arrowdown;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.keyboard.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.keyboard;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.mouse.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.mouse;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.leftmouse.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.leftmouse;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.middlemouse.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.middlemouse;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rightmouse.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rightmouse;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.gamepad.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.gamepad;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.drag.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.drag;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.doubleDrag.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.doubleDrag;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.rotate.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.rotate;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.pinch.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.pinch;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.tap.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.tap;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.doubleTap.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.doubleTap;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.touchHold.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.touchHold;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.brushBigger.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.brushBigger;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.brushSmaller.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.brushSmaller;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.undo.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.undo;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.redo.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.redo;
                }

                //
                // Menu tools.
                //

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.play.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.play;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.homeMenu.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.homeMenu;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.cameraMove.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.cameraMove;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.objectEdit.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.objectEdit;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.objectSettings.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.objectSettings;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.paths.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.paths;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.terrainPaint.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.terrainPaint;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.terrainUpDown.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.terrainUpDown;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.terrainSmoothLevel.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.terrainSmoothLevel;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.terrainSpikeyHilly.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.terrainSpikeyHilly;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.deleteObjects.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.deleteObjects;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.water.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.water;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.worldSettings.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.worldSettings;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.waterType.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.waterType;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.materialType.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.materialType;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.brushType.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.brushType;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.heartIcon.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.heartIcon;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.brokenHeartIcon.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.brokenHeartIcon;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.reportAbuseIcon.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.reportAbuseIcon;
                }

                foreach (string str in Strings.LocalizeAll("buttonAliasStrings.reportAbuseGreyIcon.alias"))
                {
                    string alias = str.ToLower();
                    buttonAliasDictionary[alias] = ControlInputs.reportAbuseGreyIcon;
                }

            }
        }   // end of InitButtonAliasDictionary()

        /// <summary>
        /// Checks if the input alias string matches a control input (button/stick/trigger).  
        /// If so that input is returned.  If not, ControlInputs.none is returned.
        /// If the alias string contains the angle brackets, these are stripped off.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public static ControlInputs MatchControlAlias(string alias)
        {
            InitButtonAliasDictionary();

            // Strip angle brackets if needed.
            if (alias.Length >= 2 && alias[0] == '<' && alias[alias.Length - 1] == '>')
            {
                alias = alias.Substring(1, alias.Length - 2);
            }

            // Ensure lower case.
            alias = alias.ToLower();

            ControlInputs button;
            if (!buttonAliasDictionary.TryGetValue(alias, out button))
            {
                // HACK Need to check specifically for arrow keys and wasd keys.
                // For some reason they don't show up in CardSpace.
                if (alias == "filter.arrowkeys")
                {
                    button = ControlInputs.programmingTile;
                }
                else if (alias == "filter.wasdkeys")
                {
                    button = ControlInputs.programmingTile;
                }
                else if (CardSpace.Cards.CardFaceTexture(alias) != null)
                {
                    // May also be a programming tile.
                    button = ControlInputs.programmingTile;
                }
                else
                {
                    button = ControlInputs.none;
                }
            }

            return button;
        }   // end of MatchControlAlias()

        /// <summary>
        /// Used by the ThoughtBalloons to allow icons and scores to 
        /// be "said" by actors.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="actor"></param>
        /// <returns></returns>
        public static string ApplyStringSubstitutions(string text, GameActor actor)
        {
            string result = string.Empty;

            while (!string.IsNullOrEmpty(text))
            {
                int pos = text.IndexOf('<');
                // Done?
                if (pos == -1)
                {
                    result += text;
                    break;
                }

                // Strip off text before '<'
                result += text.Substring(0, pos);
                text = text.Substring(pos);

                pos = text.IndexOf('>');
                // Done?
                if (pos == -1)
                {
                    result += text;
                    break;
                }

                // Include the '>'
                ++pos;
                string alias = text.Substring(0, pos);
                text = text.Substring(pos);

                ControlInputs icon = ControlInputs.none;
                icon = TextHelper.MatchControlAlias(alias);

                if (icon > ControlInputs.nonButton)
                {
                    result += GetStringSubstitution(icon, actor);
                }
                else
                {
                    // No match, just add the '<' back and keep moving.
                    result += '<';
                    text = alias.Substring(1) + text;
                }

            }

            return result;
        }   // end of ApplyStringSubstitutions()

        public static string GetStringSubstitution(ControlInputs alias, GameActor actor)
        {
            string result = string.Empty;

            switch (alias)
            {
                case TextHelper.ControlInputs.scoreblack:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Black).ToString();
                    break;
                case TextHelper.ControlInputs.scorewhite:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.White).ToString();
                    break;
                case TextHelper.ControlInputs.scoregrey:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Grey).ToString();
                    break;
                case TextHelper.ControlInputs.scorered:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Red).ToString();
                    break;
                case TextHelper.ControlInputs.scoregreen:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Green).ToString();
                    break;
                case TextHelper.ControlInputs.scoreblue:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Blue).ToString();
                    break;
                case TextHelper.ControlInputs.scoreorange:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Orange).ToString();
                    break;
                case TextHelper.ControlInputs.scorepurple:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Purple).ToString();
                    break;
                case TextHelper.ControlInputs.scoreyellow:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Yellow).ToString();
                    break;
                case TextHelper.ControlInputs.scorebrown:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Brown).ToString();
                    break;
                case TextHelper.ControlInputs.scorepink:
                    result = Scoreboard.GetGlobalScore((ScoreBucket)Classification.Colors.Pink).ToString();
                    break;

                case TextHelper.ControlInputs.scorea:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreA).ToString();
                    break;
                case TextHelper.ControlInputs.scoreb:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreB).ToString();
                    break;
                case TextHelper.ControlInputs.scorec:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreC).ToString();
                    break;
                case TextHelper.ControlInputs.scored:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreD).ToString();
                    break;
                case TextHelper.ControlInputs.scoree:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreE).ToString();
                    break;
                case TextHelper.ControlInputs.scoref:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreF).ToString();
                    break;
                case TextHelper.ControlInputs.scoreg:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreG).ToString();
                    break;
                case TextHelper.ControlInputs.scoreh:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreH).ToString();
                    break;
                case TextHelper.ControlInputs.scorei:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreI).ToString();
                    break;
                case TextHelper.ControlInputs.scorej:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreJ).ToString();
                    break;
                case TextHelper.ControlInputs.scorek:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreK).ToString();
                    break;
                case TextHelper.ControlInputs.scorel:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreL).ToString();
                    break;
                case TextHelper.ControlInputs.scorem:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreM).ToString();
                    break;
                case TextHelper.ControlInputs.scoren:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreN).ToString();
                    break;
                case TextHelper.ControlInputs.scoreo:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreO).ToString();
                    break;
                case TextHelper.ControlInputs.scorep:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreP).ToString();
                    break;
                case TextHelper.ControlInputs.scoreq:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreQ).ToString();
                    break;
                case TextHelper.ControlInputs.scorer:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreR).ToString();
                    break;
                case TextHelper.ControlInputs.scores:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreS).ToString();
                    break;
                case TextHelper.ControlInputs.scoret:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreT).ToString();
                    break;
                case TextHelper.ControlInputs.scoreu:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreU).ToString();
                    break;
                case TextHelper.ControlInputs.scorev:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreV).ToString();
                    break;
                case TextHelper.ControlInputs.scorew:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreW).ToString();
                    break;
                case TextHelper.ControlInputs.scorex:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreX).ToString();
                    break;
                case TextHelper.ControlInputs.scorey:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreY).ToString();
                    break;
                case TextHelper.ControlInputs.scorez:
                    result = Scoreboard.GetGlobalScore(ScoreBucket.ScoreZ).ToString();
                    break;

                case TextHelper.ControlInputs.privatescorea:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreA).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoreb:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreB).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorec:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreC).ToString();
                    break;
                case TextHelper.ControlInputs.privatescored:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreD).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoree:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreE).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoref:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreF).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoreg:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreG).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoreh:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreH).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorei:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreI).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorej:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreJ).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorek:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreK).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorel:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreL).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorem:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreM).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoren:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreN).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoreo:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreO).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorep:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreP).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoreq:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreQ).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorer:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreR).ToString();
                    break;
                case TextHelper.ControlInputs.privatescores:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreS).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoret:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreT).ToString();
                    break;
                case TextHelper.ControlInputs.privatescoreu:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreU).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorev:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreV).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorew:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreW).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorex:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreX).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorey:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreY).ToString();
                    break;
                case TextHelper.ControlInputs.privatescorez:
                    result = actor.localScores.GetScore(ScoreBucket.ScoreZ).ToString();
                    break;

            }

            return result;

        }   // end of GetStringSubstitution()

        /// <summary>
        /// When 'say' is used with a tag in it, we don't want to display 
        /// the tag in run mode so we use this to remove the tag before 
        /// displaying the text in a thought balloon.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string RemoveTags(string text)
        {
            bool found = false;

            do
            {
                found = false;
                // Look for beginning of tag.
                int begin = text.IndexOf("<tag", StringComparison.CurrentCultureIgnoreCase);
                if (begin != -1)
                {
                    // Look for end.
                    int end = text.IndexOf(">", begin + 4);
                    if (end != -1)
                    {
                        // Remove tag.
                        string str = "";
                        if (begin > 0)
                        {
                            str = text.Substring(0, begin);
                        }
                        if (end + 1 < text.Length)
                        {
                            str += text.Substring(end + 1);
                        }
                        text = str;
                        found = true;
                    }
                }
            } while (found);

            return text;
        }   // end of RemoveTags()

        /// <summary>
        /// Trims a sting down to fit within the given width.
        /// </summary>
        /// <param name="Wrapper">Font to use for measuring.</param>
        /// <param name="width">Max allowed width in pixels.</param>
        /// <param name="text">The text string to modify.</param>
        public static void ClipStringToWidth(UI2D.Shared.GetFont Wrapper, int width, ref string text)
        {
            text = TextHelper.FilterInvalidCharacters(text);
            int w = (int)Wrapper().MeasureString(text).X;
            if (w <= width)
                return;

            int numChars = 1;
            for (; ; )
            {
                w = (int)Wrapper().MeasureString(text.Substring(0, numChars)).X;
                if (w > width)
                    break;
                ++numChars;
            }
            text = text.Substring(0, numChars - 1);
        }   // end of ClipStringToWidth()


        /// <summary>
        /// Split a long message into several lines.
        /// We try and do this in such a way that the number of characters is kept constant
        /// so that when we're doing editing the cursor doesn't get lost.
        /// </summary>
        /// <param name="inputMessage">The string to split up.</param>
        /// <param name="maxLineWidth">Max allowable width of a line in pixels.</param>
        /// <param name="font">The spritefont used to base our spacing on.</param>
        /// <param name="preserveCharacterCount">Set to true if you need to have the character count exactly preserved.</param>
        /// <returns>A List of Strings each with a line of text.</returns>
        public static void SplitMessage(string inputMessage, int maxLineWidth, UI2D.Shared.GetFont Font, bool preserveCharacterCount, List<string> lines)
        {
            lines.Clear();

            inputMessage = TextHelper.FilterInvalidCharacters(inputMessage);

            if (String.IsNullOrEmpty(inputMessage))
                return;

            // Get rid of any characters we don't want to handle.
            string message = "";
            for (int i = 0; i < inputMessage.Length; i++)
            {
                if (inputMessage[i] == 9)
                {
                    if (preserveCharacterCount)
                    {
                        message += " ";
                    }
                }
                else
                {
                    message += inputMessage[i];
                }
            }

            // first split the the text into paragraphs
            string[] paragraphs = message.Split(delimitersParagraphs, StringSplitOptions.None);
            // Line feeds are lost when the string is seperated into paragraphs.  We
            // need to put them back in so that the character counts stay correct.
            for (int p = 0; p < paragraphs.Length - 1; p++)
            {
                paragraphs[p] += '\n';
            }

            // Split the paragraphs into words
            for (int p=0; p<paragraphs.Length; p++)
            {
                string paragraph = paragraphs[p];
                String[] words = paragraph.Split(delimitersWord, preserveCharacterCount ? StringSplitOptions.None : StringSplitOptions.RemoveEmptyEntries);
                // Add the removed spaces back in.
                for (int w = 0; w < words.Length - 1; w++)
                {
                    if (!words[w].EndsWith("\n"))
                    {
                        words[w] += ' ';
                    }
                }

                // Now dole out the words into new, shorter strings.
                string str = "";
                if (words.Length > 0)
                {
                    string tmp = null;
                    for (int i = 0; i < words.Length; i++)
                    {
                        // If we've come to a \n so output the line with the \n and start a new one.
                        if (words[i] == "\n")
                        {
                            lines.Add(str + "\n");
                            str = "";
                            tmp = "";
                        }
                        else
                        {
                            if (i == 0)
                            {
                                // Start tmp with first word.
                                tmp = words[i];
                            }
                            else
                            {
                                // Add the next word to the tmp string.
                                tmp = tmp + words[i];
                            }
                            // Check the length.
                            Vector2 size = Font().MeasureString(tmp);
                            if (size.X > maxLineWidth)
                            {
                                // Too long, so add str as it is and reset tmp 
                                // to just the one word that didn't fit.
                                lines.Add(str);
                                tmp = words[i];
                                str = tmp;
                            }
                            else
                            {
                                str = tmp;
                            }
                        }
                    }
                }

                // If anything is left over, add it.
                if (str.Length > 0)
                {
                    lines.Add(str);
                }
            }

            // If the final line ends in a \n then add an extra blank line.  
            // This makes the layout and cursor positioning easier.
            if (preserveCharacterCount && (lines.Count == 0 || lines[lines.Count - 1].EndsWith("\n")))
            {
                lines.Add("");
            }
        }   // end of SplitMessage

        static Char[] delimitersParagraphs = new Char[] { '\n' };
        static Char[] delimitersWord = new Char[] { ' ' };

    
        /// <summary>
        /// Based on the justification, this calculate how far the text needs to be offset in X for rendering.
        /// </summary>
        /// <param name="margin">The amount of desired margin in pixels.</param>
        /// <param name="width">The width of the space to fit the text into in pixels.</param>
        /// <param name="textWidth">The width of the text string in pixels.</param>
        /// <param name="justification">The justification to apply to the text.</param>
        /// <returns>The offset in pixels needed achive the justification.</returns>
        public static int CalcJustificationOffset(int margin, int width, int textWidth, UIGridElement.Justification justification)
        {
            int x = 0;

            switch (justification)
            {
                case UIGridElement.Justification.Left:
                    x = margin;
                    break;
                case UIGridElement.Justification.Center:
                    x = (width - textWidth) / 2;
                    break;
                case UIGridElement.Justification.Right:
                    x = width - textWidth - margin;
                    break;
            }

            return x;
        }   // end of TextHelper CalcJustificationOffset()

        /// <summary>
        /// A "valid" char is one that we have a glyph for.
        /// Currently that ranges are :
        /// [0020, 007F] (Latin)
        /// [00A0, 017F] (Latin)
        /// [0301, 0301] (accent for Cyrillic)
        /// [0370, 0451] (Greek, Cyrillic)
        /// [0590, 05FF] (Hebrew)
        /// [0600, 06FF] (Arabic)
        /// [0750, 077f] (Arabic Supplement)
        /// [10A0, 10FF] (Georgian)
        /// [2022, 20b9] (punctuation, symbols, currency)
        /// [FB00, FB4F] (Hebrew presentation forms including Latin & Armenian ligatures)
        /// [FB50, FDFF] (Arabic presentation forms A)
        /// [FE70, FEFF] (Arabic presentation forms B)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool CharIsValid(int c)
        {
            bool result = false;

            // Always rule out control characters for printing.
            if (c < 0x0020)
            {
                return result;
            }

            // Throw away characters that are invalid according to our Unicode data.
            Unicode.UnicodeCharData data = Unicode.GetCharInfo((char)c);
            if (data == null)
            {
                return result;
            }

            if (BokuSettings.Settings.UseSystemFontRendering)
            {
                result = true;
            }
            else
            {
                if (
                    (c >= 0x0020 && c <= 0x007e)
                    || (c >= 0x00a0 && c <= 0x017f)
                    || (c >= 0x0301 && c <= 0x0301)
                    || (c >= 0x0370 && c <= 0x0451)
                    || (c >= 0x0590 && c <= 0x05ff)
                    // || (c >= 0x0600 && c <= 0x06ff)
                    // || (c >= 0x0750 && c <= 0x077f)
                    || (c >= 0x10a0 && c <= 0x10ff)
                    || (c >= 0x2022 && c <= 0x20b9)
                    || (c >= 0xfb00 && c <= 0xfb4f)
                    // || (c >= 0xfb50 && c <= 0xfdff)
                    // || (c >= 0xfe70 && c <= 0xfeff)
                    )
                {
                    result = true;
                }
            }

            return result;
        }   // end of CharIsValid()

        /// <summary>
        /// Returns true if all the chars in the string are considered valid.
        /// "Valid" menas we can render them.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool StringIsValid(string str)
        {
            foreach (char c in str)
            {
                if (!CharIsValid(c))
                {
                    return false;
                }
            }

            return true;
        }   // end of StringIsValid()

        /// <summary>
        /// Filters out any characters that aren't in the normal range we use for our fonts.
        /// See CharIsValid above for supported ranges.
        /// If we find and invalid characters we replace them with 
        /// a space unless we happen to know a better replacement.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FilterInvalidCharacters(string str)
        {
            if (str == null)
                return null;

            char[] array = str.ToCharArray();
            bool changed = false;

            for (int i = 0; i < str.Length; i++)
            {
                int c = str[i];
                if (!CharIsValid(c))
                {
                    if (c == 0x0a )
                    {
                        // Leave returns alone.
                    }
                    else if( c == 0x08 ) //Set backspace as NULL for test below.
                    {
                        array[i] = '\0';
                    }
                    else if (c == 0x2019)   // Fancy apostrophe.
                    {
                        array[i] = '\'';
                    }
                    else
                    {
                        array[i] = ' ';
                    }

                    changed = true;
                }
            }

            if (changed)
            {
                str = "";
                for( int i=0; i<array.Length; ++i )
                {
                    if( '\0' == array[i] ) //Catch NULL (was placed in array when a backspace is hit)
                    {
                        break;
                    }
                    str += array[i];
                }
            }

            return str;
        }   // end FilterInvalidCharacters()

        /// <summary>
        /// Same as SpriteBatch.DrawString() 
        /// Always uses the UI2D.Shared spritebatch.
        /// Uses a static TextBlob to ensure bidi support for the text.
        /// </summary>
        /// <param name="font"></param>
        /// <param name="text"></param>
        /// <param name="pos"></param>
        /// <param name="textColor"></param>
        /// <param name="maxWidth">Width to wrap text at.</param>
        public static void DrawString(UI2D.Shared.GetFont Font, string text, Vector2 pos, Color textColor, Color outlineColor=default(Color), float outlineWidth = 0, int maxWidth = 2048)
        {
            SpriteBatch batch = UI2D.Shared.SpriteBatch;

            if (blob == null)
            {
                blob = new TextBlob(Font, text, int.MaxValue);
            }

            blob.Width = maxWidth;
            blob.Font = Font;
            blob.RawText = text;

            // TODO (****) This function was designed to be an easy, drop in replacement for
            // SpriteBatch.DrawString() which requires that it be called from within a Begin/End
            // pair.  The TextBlob rendering has it's own Begin/End pair.  So, to keep things
            // straight, we end the current batch and then start a new one once the blob is done.
            // Since, (I think) that the only place we regularly use SpriteBatch is for text
            // it might make sense to get rid of this and clear out all the places that SpriteBatch
            // is used.
            batch.End();
            blob.RenderWithButtons(pos, textColor, outlineColor: outlineColor, outlineWidth: outlineWidth);
            batch.Begin();

        }   // end of DrawString()

        /// <summary>
        /// Version of DrawString that does not assume it is called from within
        /// a SpriteBatch Begin/End pair.
        /// </summary>
        /// <param name="Font"></param>
        /// <param name="text"></param>
        /// <param name="pos"></param>
        /// <param name="textColor"></param>
        public static void DrawStringNoBatch(UI2D.Shared.GetFont Font, string text, Vector2 pos, Color textColor, Color outlineColor=default(Color), float outlineWidth = 0)
        {
            SpriteBatch batch = UI2D.Shared.SpriteBatch;

            if (blob == null)
            {
                blob = new TextBlob(Font, text, int.MaxValue);
            }

            blob.Font = Font;
            blob.RawText = text;

            blob.RenderWithButtons(pos, textColor, outlineColor: outlineColor, outlineWidth: outlineWidth);

        }   // end of DrawString()

        public static void DrawStringWithShadow(UI2D.Shared.GetFont Font, SpriteBatch batch, Vector2 pos, string text, Color textColor, Color shadowColor, bool invertDropShadow)
        {
            DrawStringWithShadow(Font, batch, (int)pos.X, (int)pos.Y, text, textColor, shadowColor, invertDropShadow);
        }

        /// <summary>
        /// Renders a text string with a drop shadow.
        /// </summary>
        /// <param name="Font">Delegate used to get the font for rendering.</param>
        /// <param name="x">x position in pixels.</param>
        /// <param name="y">y position in pixels.</param>
        /// <param name="text">The string to render.</param>
        /// <param name="textColor">The foreground color for the text.</param>
        /// <param name="shadowColor">The color for the dropshadow.</param>
        /// <param name="invertDropShadow">If true, puts the shadow above the text rather than below.</param>
        public static void DrawStringWithShadow(UI2D.Shared.GetFont Font, SpriteBatch batch, int x, int y, string text, Color textColor, Color shadowColor, bool invertDropShadow)
        {
            text = TextHelper.FilterInvalidCharacters(text);

            // Draw the shadow.
            if (invertDropShadow)
            {
                TextHelper.DrawString(Font, text, new Vector2(x + 1, y - 1), shadowColor);
            }
            else
            {
                TextHelper.DrawString(Font, text, new Vector2(x + 1, y + 1), shadowColor);
            }
            // Draw the text on top of the shadow.
            TextHelper.DrawString(Font, text, new Vector2(x, y), textColor);
        }   // end of TextHelper DrawStringWithShadow()


        /// <summary>
        /// Truncates and adds "..." to the string if longer than maxSize.
        /// </summary>
        /// <param name="Font">Delegate to get font to use in measurements.</param>
        /// <param name="input"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static string AddEllipsis(UI2D.Shared.GetFont Font, string input, float maxSize)
        {
            if(input == null || input.Length == 0)
            {
                return "";
            }

            string str = TextHelper.FilterInvalidCharacters(input);
            string workStr = str;

            float strSize = Font().MeasureString(str).X;

            while (strSize > maxSize && str.Length > 0)
            {
                str = str.Remove(str.Length - 1, 1);
                workStr = str + "...";
                strSize = Font().MeasureString(workStr).X;
            }

            return workStr;
        }   // end of AddEllipsis()

        /// <summary>
        /// Takes the input text, remvoes all whitespace and punctuation
        /// and then converts to all lowercase.  This is meant to aid
        /// string matching for the said filter.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string WhitespaceCompress(string text)
        {
            string str = text.ToLower();
            char[] white = { ' ', '\t', '\n', '.', ',', '!', '?', '-', '_', ':', ';' };

            int i;
            do
            {
                i = str.IndexOfAny(white);
                if (i != -1)
                {
                    str = str.Remove(i, 1);
                }
            } while (i != -1);

            return str;
        }   // end of WhitespaceCompress()

        /// <summary>
        /// FNV hashes a string.
        /// Different versions of the .Net Framework return different hash codes
        /// for the same string. Use this wherever that would be a problem.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int HashString(string str)
        {
            uint h = 2166136261;

            for (int i = 0; i < str.Length; i++)
                h = (h * 16777619) ^ str[i];

            return (int)h;
        }

        /// <summary>
        /// Attempts to parse the given value as an enum
        /// </summary>
        public static bool EnumTryParse<T>(string value, out T result, bool ignoreCase)
        {
            if (Enum.IsDefined(typeof(T), value))
            {
                result = (T)Enum.Parse(typeof(T), value);

                return true;
            }
            else if (ignoreCase)
            {
                var names = Enum.GetNames(typeof(T));
                foreach (string name in names)
                {
                    if (name.Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        result = (T)Enum.Parse(typeof(T), name, true);
                        return true;
                    }
                }
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Looks for URLs in the imput string and replaces the characters of 
        /// the URL with the replacement chartacter.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="replacementCharacter"></param>
        /// <returns></returns>
        public static string FilterURLs(string str, char replacementCharacter = '*')
        {
            string pattern = @"(?i)\b((?:https?:(?:/{1,3}|[a-z0-9%])|[a-z0-9.\-]+[.](?:com|net|org|edu|gov|mil|aero|asia|biz|cat|coop|info|int|jobs|mobi|museum|name|post|pro|tel|travel|xxx|ac|ad|ae|af|ag|ai|al|am|an|ao|aq|ar|as|at|au|aw|ax|az|ba|bb|bd|be|bf|bg|bh|bi|bj|bm|bn|bo|br|bs|bt|bv|bw|by|bz|ca|cc|cd|cf|cg|ch|ci|ck|cl|cm|cn|co|cr|cs|cu|cv|cx|cy|cz|dd|de|dj|dk|dm|do|dz|ec|ee|eg|eh|er|es|et|eu|fi|fj|fk|fm|fo|fr|ga|gb|gd|ge|gf|gg|gh|gi|gl|gm|gn|gp|gq|gr|gs|gt|gu|gw|gy|hk|hm|hn|hr|ht|hu|id|ie|il|im|in|io|iq|ir|is|it|je|jm|jo|jp|ke|kg|kh|ki|km|kn|kp|kr|kw|ky|kz|la|lb|lc|li|lk|lr|ls|lt|lu|lv|ly|ma|mc|md|me|mg|mh|mk|ml|mm|mn|mo|mp|mq|mr|ms|mt|mu|mv|mw|mx|my|mz|na|nc|ne|nf|ng|ni|nl|no|np|nr|nu|nz|om|pa|pe|pf|pg|ph|pk|pl|pm|pn|pr|ps|pt|pw|py|qa|re|ro|rs|ru|rw|sa|sb|sc|sd|se|sg|sh|si|sj|Ja|sk|sl|sm|sn|so|sr|ss|st|su|sv|sx|sy|sz|tc|td|tf|tg|th|tj|tk|tl|tm|tn|to|tp|tr|tt|tv|tw|tz|ua|ug|uk|us|uy|uz|va|vc|ve|vg|vi|vn|vu|wf|ws|ye|yt|yu|za|zm|zw)/)(?:[^\s()<>{}\[\]]+|\([^\s()]*?\([^\s()]+\)[^\s()]*?\)|\([^\s]+?\))+(?:\([^\s()]*?\([^\s()]+\)[^\s()]*?\)|\([^\s]+?\)|[^\s`!()\[\]{};:'"".,<>?������])|(?:(?<!@)[a-z0-9]+(?:[.\-][a-z0-9]+)*[.](?:com|net|org|edu|gov|mil|aero|asia|biz|cat|coop|info|int|jobs|mobi|museum|name|post|pro|tel|travel|xxx|ac|ad|ae|af|ag|ai|al|am|an|ao|aq|ar|as|at|au|aw|ax|az|ba|bb|bd|be|bf|bg|bh|bi|bj|bm|bn|bo|br|bs|bt|bv|bw|by|bz|ca|cc|cd|cf|cg|ch|ci|ck|cl|cm|cn|co|cr|cs|cu|cv|cx|cy|cz|dd|de|dj|dk|dm|do|dz|ec|ee|eg|eh|er|es|et|eu|fi|fj|fk|fm|fo|fr|ga|gb|gd|ge|gf|gg|gh|gi|gl|gm|gn|gp|gq|gr|gs|gt|gu|gw|gy|hk|hm|hn|hr|ht|hu|id|ie|il|im|in|io|iq|ir|is|it|je|jm|jo|jp|ke|kg|kh|ki|km|kn|kp|kr|kw|ky|kz|la|lb|lc|li|lk|lr|ls|lt|lu|lv|ly|ma|mc|md|me|mg|mh|mk|ml|mm|mn|mo|mp|mq|mr|ms|mt|mu|mv|mw|mx|my|mz|na|nc|ne|nf|ng|ni|nl|no|np|nr|nu|nz|om|pa|pe|pf|pg|ph|pk|pl|pm|pn|pr|ps|pt|pw|py|qa|re|ro|rs|ru|rw|sa|sb|sc|sd|se|sg|sh|si|sj|Ja|sk|sl|sm|sn|so|sr|ss|st|su|sv|sx|sy|sz|tc|td|tf|tg|th|tj|tk|tl|tm|tn|to|tp|tr|tt|tv|tw|tz|ua|ug|uk|us|uy|uz|va|vc|ve|vg|vi|vn|vu|wf|ws|ye|yt|yu|za|zm|zw)\b/?(?!@)))";
            while(true)
            {
                Match match = Regex.Match(str, pattern);

                if(!match.Success)
                {
                    break;
                }

                char[] a = str.ToCharArray();
                for(int i=match.Index; i<match.Index + match.Length; i++)
                {
                    a[i] = replacementCharacter;
                }
                str = new string(a);
            }

            return str;
        }   // end of FilterURLs()

        /// <summary>
        /// Looks for email addresses in the imput string and replaces the characters of 
        /// the URL with the replacement chartacter.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="replacementCharacter"></param>
        /// <returns></returns>
        public static string FilterEmail(string str, char replacementCharacter = '*')
        {
            string pattern = @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b";

            while (true)
            {
                Match match = Regex.Match(str, pattern, RegexOptions.IgnoreCase);

                if (!match.Success)
                {
                    break;
                }

                char[] a = str.ToCharArray();
                for (int i = match.Index; i < match.Index + match.Length; i++)
                {
                    a[i] = replacementCharacter;
                }
                str = new string(a);
            }

            return str;
        }   // end of FilterEmail()

    }   // end of class TextHelper
}   // end of namespace Boku.Common
