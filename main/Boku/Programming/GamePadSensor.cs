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
using Boku.SimWorld;

namespace Boku.Programming
{
    /// <summary>
    /// Senses when the physical GamePad is used
    /// 
    /// This sensor acts more like a manager than the true source of the sensor event.
    /// It will request the GamePadStick and GamePadButton filters to provide 
    /// the actual input.  This sensor demonstrates a break in the normal use of 
    /// the model but demonstrates how other elements can be used to solve problems.
    /// </summary>
    public class GamePadSensor : Sensor
    {
        public enum PlayerId
        {
            Dynamic = -1, // used to signify to use a filter to define player
            One,
            Two,
            Three,
            Four,
            All, 
        }
              
        [XmlAttribute]
        public PlayerId playerIndex = PlayerId.Dynamic;


        public GamePadSensor()
        {
        }

        public override ProgrammingElement Clone()
        {
            GamePadSensor clone = new GamePadSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(GamePadSensor clone)
        {
            base.CopyTo(clone);
            clone.playerIndex = this.playerIndex;
        }

        public override void StartUpdate(GameActor gameActor)
        {
        }

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
        }

        public override void FinishUpdate(GameActor gameActor)
        {
        }

        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            if (this.playerIndex == PlayerId.Dynamic)
            {
                PlayerId dynamicPlayerId = PlayerId.All;
                // check filters for the player
                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    PlayerFilter filter = filters[indexFilter] as PlayerFilter;
                    object param;
                    if (filter != null && filter.MatchAction(reflex, out param))
                    {
                        dynamicPlayerId = (PlayerId)param;
                        break;
                    }
                }
                reflex.targetSet.Param = dynamicPlayerId;
            }
            else
            {
                reflex.targetSet.Param = this.playerIndex;
            }

            reflex.targetSet.Action = TestObjectSet(reflex);
        }

        private new bool TestObjectSet(Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            bool match = true;
            object param;
            for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
            {
                Filter filter = filters[indexFilter] as Filter;
                // skip player filter
                if (!(filter is PlayerFilter))
                {
                    if (!filter.MatchAction(reflex, out param))
                    {
                        match = false;
                        break;
                    }
                    if (param != null)
                    {
                        reflex.targetSet.Param = param;
                    }
                }
            }

            if (reflex.Data.GetFilterCountByType(typeof(NotFilter)) > 0)
                match = !match;

            match = PostProcessAction(match, reflex);

            return match;
        }
    }
}
