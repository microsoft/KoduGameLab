// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
using Boku.SimWorld;
using Boku.SimWorld.Chassis;

namespace Boku.Programming
{
    /// <summary>
    /// this modifier acts like a parameter and provides a value to be used 
    /// as a tracking mode to the actuator
    /// </summary>
    public class TrackingModifier : Modifier
    {
        [XmlAttribute]
        public MissileChassis.BehaviorFlags behavior;

        public override ProgrammingElement Clone()
        {
            TrackingModifier clone = new TrackingModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TrackingModifier clone)
        {
            base.CopyTo(clone);
            clone.behavior = this.behavior;
        }

        public override void GatherParams(ModifierParams param)
        {
            /// This is unusual because the default flag, TerrainFollowing, is non-zero,
            /// but the optional behavior is Zero. Larger issue to be addressed later,
            /// but currently our flag is either exactly TerrainFollowing, in which
            /// case we want to set the TerrainFollowing bit, or it is None, in which
            /// case we want to clear the TerrainFollowing bit.
            if (behavior == MissileChassis.BehaviorFlags.TerrainFollowing)
            {
                param.MissileBehavior |= MissileChassis.BehaviorFlags.TerrainFollowing;
            }
            else
            {
                param.MissileBehavior &= ~MissileChassis.BehaviorFlags.TerrainFollowing;
            }
        }

    }
}
