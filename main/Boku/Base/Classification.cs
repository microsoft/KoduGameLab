// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;

using Boku.Common;
using Boku.Programming;
using Boku.SimWorld;

namespace Boku.Base
{
    public delegate bool MatchCall(Classification right);

    public enum ClassificationType
    {
        None,
        Expression,
        Sight,
        Hearing,
        Touch,
        Object,
        Color,
        Moving,
        Camouflage,
        PathColor,
    }

    public partial class Classification : ArbitraryComparable
    {
        static public Color XnaColor(Colors colorConvert)
        {
            Color result = Microsoft.Xna.Framework.Color.White;
            switch (colorConvert)
            {
                case Colors.Black:
                    result = new Color(34, 37, 36);
                    break;
                case Colors.White:
                    result = new Color(250, 250, 233);
                    break;
                case Colors.Grey:
                    result = new Color(147, 138, 135);
                    break;
                case Colors.Red:
                    result = new Color(239, 59, 46);
                    break;
                case Colors.Green:
                    result = new Color(32, 206, 87);
                    break;
                case Colors.Blue:
                    result = new Color(61, 161, 224);
                    break;
                case Colors.Orange:
                    result = new Color(255, 160, 31);
                    break;
                case Colors.Yellow:
                    result = new Color(232, 255, 118);
                    break;
                case Colors.Purple:
                    result = new Color(109, 70, 123);
                    break;
                case Colors.Pink:
                    result = new Color(255, 149, 162);
                    break;
                case Colors.Brown:
                    result = new Color(161, 78, 41);
                    break;
                default:
                    break;
            }
            return result;
        }
        public bool HasColor(Colors b)
        {
            return ((1 << (int)b) & AllColors) != 0;
        }
        public UInt32 AllColors
        {
            get
            {
                UInt32 ret = ((1U << (int)color) | (1U << (int)glowColor) | otherColors) & ~1U;
                return ret != 0 ? ret : (1U << (int)Colors.NotApplicable);
            }
        }
        public Colors RandomColor()
        {
            return (Colors)BokuGame.bokuGame.rnd.Next(1, (int)Colors.SIZEOF);
        }

        [XmlAttribute]
        public Colors color;

        [XmlIgnore]
        private Vector4 colorRGBA;  // The color used in rendering.  This may be different than
                                    // the color above since we apply a twitch to change the
                                    // color smoothly.

        [XmlAttribute]
        public Colors glowColor;

        [XmlIgnore]
        public Colors GlowColor
        {
            get { return glowColor; }
            set { glowColor = value; }
        }

        /// <summary>
        /// otherColors is set behind the scenes by the owning class. 
        /// We don't want to save it, if the class changes its mind, we want otherColors
        /// to take the new value.
        /// </summary>
        private UInt32 otherColors = 0;
        public void AddOtherColor(Colors c) { otherColors |= (1U << (int)c); }

        public enum AudioImpression
        {
            NotApplicable,
            Noisy,
            Melodic,
        }
        [XmlAttribute]
        public AudioImpression audioImpression;

        public enum AudioVolume
        {
            NotApplicable,
            Loud,
            Normal,
            Soft,
            Silent,
        }
        [XmlAttribute]
        public AudioVolume audioVolume;

        [FlagsAttribute]
        public enum Physicalities
        {
            NotApplicable = 0x0000,
            Collectable = 0x0001,
            Static = 0x8000
        }
        [XmlAttribute]
        public Physicalities physicality;

        [XmlAttribute]
        public string name;

        public class Sounds
        {
            public const string NotApplicable = "NotApplicable";
            public const string None = "None";
            public const string Any = "Any";
        }

        [XmlAttribute]
        public string sound;


        public Classification()
        {
        }
        public Classification(string name)
        {
            this.name = name;
        }
        public Classification(Classification src)
        {
            src.CopyTo(this);
        }

        public static Vector4 ColorVector4(Colors color)
        {
            return XnaColor(color).ToVector4();
        }

        #region Accessors
        /// <summary>
        /// Sets/gets the color immediately.
        /// Don't set this directly on GameActors, use GameActor.ClassColor instead.
        /// </summary>
        public Classification.Colors Color
        {
            get { return color; }
            set { color = value; colorRGBA = ColorVector4(color); }
        }

        /// <summary>
        /// This is the twitched version of the color.  This should be used
        /// for rendering but should not be used for comparisons.
        /// </summary>
        public Vector4 ColorRGBA
        {
            get { return colorRGBA; }
        }
        #endregion

        [XmlAttribute]
        public Face.FaceState expression = Face.FaceState.NotApplicable;

        [XmlAttribute]
        public ExpressModifier.Emitters emitter = ExpressModifier.Emitters.NotApplicable;

        /// <summary>
        /// Sets the color of the actor using a twitch to smooth the transition.
        /// Don't set this directly on GameThings, use GameThing.SetColor instead.
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Classification.Colors value)
        {
            if (color != value)
            {
                color = value;
                // Twitch ColorRGBA from whatever the current value is toward the new value.
                TwitchManager.Set<Vector4> set = delegate(Vector4 val, Object param) { colorRGBA = val; };
                TwitchManager.CreateTwitch(ColorRGBA, ColorVector4(color), set, 0.25f, TwitchCurve.Shape.EaseInOut);
            }
        }   // end of SetColor

        public bool Match(Classification right)
        {
            /*
                        if (level != ContainerLevels.NotApplicable && level != right.level)
                        {
                            return false;
                        }
             */
            if (!MatchColor(right))
            {
                return false;
            }
            if (audioImpression != AudioImpression.NotApplicable && audioImpression != right.audioImpression)
            {
                return false;
            }
            if (audioVolume != AudioVolume.NotApplicable && audioVolume != right.audioVolume)
            {
                return false;
            }
            if (physicality != Physicalities.NotApplicable && physicality != right.physicality)
            {
                return false;
            }
            if (sound != Sounds.NotApplicable && sound != right.sound)
            {
                return false;
            }
            Debug.Assert(right.name != null);
            if (!String.IsNullOrEmpty(name) && !right.name.Contains(name))
            {
                return false;
            }
            return true;
        }

        public MatchCall MatchByType(ClassificationType type)
        {
            MatchCall call = null;
            switch (type)
            {
                case ClassificationType.Expression:
                    call = MatchExpression;
                    break;
                case ClassificationType.Sight:
                    call = MatchSight;
                    break;
                case ClassificationType.Hearing:
                    call = MatchHearing;
                    break;
                case ClassificationType.Touch:
                    call = MatchTouch;
                    break;
                case ClassificationType.Object:
                    call = MatchObject;
                    break;
                case ClassificationType.Color:
                    call = MatchColor;
                    break;
                default:
                    Debug.Assert(false, "sensor type not supported");
                    break;
            }
            return call;
        }

        public bool MatchObject(Classification right)
        {
            if (!MatchSight(right) ||
                !MatchTouch(right) ||
                !MatchHearing(right))
            {
                return false;
            }
            return true;
        }
        public bool MatchColor(Classification right)
        {
            ///
            /// Note that this is not a symmetric test. If all of my
            /// colors are NotApplicable, then I match anything.
            /// If right is NotApplicable, he matches nothing.
            if ((AllColors != (1U << (int)Colors.NotApplicable)) && ((AllColors & right.AllColors) == 0))
            {
                return false;
            }
            return true;
        }
        public bool MatchExpression(Classification right)
        {
            if (this.expression != Face.FaceState.NotApplicable && this.expression != right.expression)
            {
                return false;
            }
            if (this.emitter != ExpressModifier.Emitters.NotApplicable && this.emitter != right.emitter)
            {
                return false;
            }
            return true;
        }
        public bool MatchSight(Classification right)
        {
            if (!MatchColor(right))
            {
                return false;
            }
            if (physicality != Physicalities.NotApplicable && physicality != right.physicality)
            {
                return false;
            }
            Debug.Assert(right.name != null);
            if (!String.IsNullOrEmpty(name) && !right.name.Contains(name))
            {
                return false;
            }
            return true;
        }
        public bool MatchHearing(Classification right)
        {
            if (audioImpression != AudioImpression.NotApplicable && audioImpression != right.audioImpression)
            {
                return false;
            }
            if (audioVolume != AudioVolume.NotApplicable && audioVolume != right.audioVolume)
            {
                return false;
            }
            Debug.Assert(right.name != null);
            if (!String.IsNullOrEmpty(name) && !right.name.Contains(name))
            {
                return false;
            }
            return true;
        }
        public bool MatchTouch(Classification right)
        {
            // added shade and color to make programming easier
            // BUMP - RED - FRUIT
            if (!MatchColor(right))
            {
                return false;
            }

            if (physicality != Physicalities.NotApplicable && physicality != right.physicality)
            {
                return false;
            }
            Debug.Assert(right.name != null);
            if (!String.IsNullOrEmpty(name) && !right.name.Contains(name))
            {
                return false;
            }
            return true;
        }

        public bool IsCursor
        {
            get
            {
                return (this.name == "cursor");
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void CopyTo(Classification other)
        {
            other.name = this.name;
            other.color = this.color;
            other.otherColors = this.otherColors;
            other.audioImpression = this.audioImpression;
            other.audioVolume = this.audioVolume;
            other.physicality = this.physicality;
            other.sound = this.sound;
        }
    }

}
