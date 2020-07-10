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

namespace Boku.Programming
{
    /// <summary>
    /// Filters based upon the distance of the GameThing from the GameActor
    /// 
    /// 
    /// </summary>
    public class DistanceFilter : Filter
    {
        public enum TweakBinding
        {
            None,
            NearByDistance,
            FarAwayDistance,
        }

        [XmlAttribute]
        public Operand operand;

        [XmlAttribute]
        public TweakBinding tweakBinding;

        public DistanceFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            DistanceFilter clone = new DistanceFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(DistanceFilter clone)
        {
            base.CopyTo(clone);
            clone.tweakBinding = this.tweakBinding;
            clone.operand = this.operand;
        }

        private float GetRangeFromTweakSettings(GameActor actor)
        {
            switch (tweakBinding)
            {
                case TweakBinding.NearByDistance:
                    return actor.NearByDistance;

                case TweakBinding.FarAwayDistance:
                    return actor.FarAwayDistance;

                default:
                    throw new ArgumentException("Invalid tweakBinding in DistanceFilter");
            }
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            //if ((sensorCategory & Sensor.Category.Distance) != 0)
            {
                float range = GetRangeFromTweakSettings(reflex.Task.GameActor);
                bool match = OperandCompare<float>(sensorTarget.Range, this.operand, range);
                return match;
            }
            //return true; // not the right type, don't effect the filtering
        }
        public override bool MatchAction(Reflex reflex, out object param)
        {
            // doesn't effect match action
            param = null;
            return true;
        }
    }
}
