// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
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
    /// this modifier will modify the selector output vectors length 
    /// </summary>
    public class SpeedModifier : Modifier
    {
        enum ModifierType
        {
            None,
            Speed,
            Angle,
            Strength,
        }

        ModifierType modifierType;

        [XmlAttribute]
        public int maxCount = 3;

        /// <summary>
        /// Heading vector is multiplied by this scalar value.
        /// </summary>
        [XmlAttribute]
        public float Multiplier = 1;

        public override void OnLoad()
        {
            base.OnLoad();

            if (Categories.Get((int)BrainCategories.SpeedModifier))
                modifierType = ModifierType.Speed;
            else if (Categories.Get((int)BrainCategories.AngleModifier))
                modifierType = ModifierType.Angle;
            else if (Categories.Get((int)BrainCategories.StrengthModifier))
                modifierType = ModifierType.Strength;
        }

        public override ProgrammingElement Clone()
        {
            SpeedModifier clone = new SpeedModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SpeedModifier clone)
        {
            base.CopyTo(clone);
            clone.Multiplier = this.Multiplier;
            clone.maxCount = this.maxCount;
            clone.modifierType = this.modifierType;
        }

        public override void GatherParams(ModifierParams param)
        {

            switch (modifierType)
            {
                case ModifierType.Speed:
                    if (param.SpeedModifier == 0.0f)
                        param.SpeedModifier = 1.0f;
                    param.SpeedModifier *= this.Multiplier;
                    break;

                case ModifierType.Angle:
                    if (param.Loft == 0.0f)
                        param.Loft = 1.0f;
                    param.Loft *= this.Multiplier;
                    break;

                case ModifierType.Strength:
                    if (param.Strength == 0.0f)
                        param.Strength = 1.0f;
                    param.Strength *= this.Multiplier;
                    break;
            }
        }

        public override bool ModifyHeading(Reflex reflex, GameActor gameActor, ref Vector3 heading)
        {
            if (modifierType == ModifierType.Speed)
            {
                float len = heading.Length();

                Vector3 unit = heading;

                if (unit != Vector3.Zero)
                {
                    unit.Normalize();
                }

                heading = unit * len * Multiplier;
            }

            return true;
        }
    }
}
