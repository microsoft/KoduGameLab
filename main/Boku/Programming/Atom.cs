#if DEBUG
#define Debug_ShowMissingStrings
#endif

using System;
using System.Collections;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;

namespace Boku.Programming
{
    /// <summary>
    /// This represents the base for all cardspace pieces.
    /// </summary>
    public abstract class Atom : ArbitraryComparable
    {
        public const string upidNull = "null";
        public const string upidSensor = "sensor";
        public const string upidFilter = "filter";
        public const string upidSelector = "selector";
        public const string upidModifier = "modifier";
        public const string upidActuator = "actuator";

        /// <summary>
        /// The unique identifier for the element; this starts with one of the above constants and
        /// then ends with descriptive id.  This is used for the XML serialization lookup for reflexes
        /// Examples:
        ///     null    - always alone
        ///     sensor.eys
        ///     actuator.movement
        /// </summary>
        [XmlAttribute]
        public string upid;

#if Debug_ShowMissingStrings
        [XmlAttribute]
        public string label = "MISSING LABEL";
#else
        [XmlAttribute]
        public string label;
#endif

        [XmlAttribute]
        public string icon;

        public string TextureName
        {
            get
            {
                if (this.icon == null)
                {
                    return this.upid;
                }
                else
                {
                    return this.icon;
                }
            }
        }
        /// <summary>
        /// Build a "image.id" card face without the label for other uses than just on the tiles
        /// </summary>
        [XmlAttribute]
        public bool noLabelIcon;

#if Debug_ShowMissingStrings
        [XmlElement(ElementName = "description", DataType = "string")]
        public string description = "MISSING DESCRIPTION";
#else
        [XmlElement(ElementName = "description", DataType = "string")]
        public string description;
#endif

        protected void CopyTo(Atom clone)
        {
            clone.upid = this.upid;
            clone.label = this.label;
            clone.icon = this.icon;
            clone.noLabelIcon = this.noLabelIcon;
            clone.description = this.description;
        }

        public abstract void OnLoad();
    }
}
