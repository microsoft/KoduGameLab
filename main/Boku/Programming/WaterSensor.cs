
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
using Boku.SimWorld.Terra;

namespace Boku.Programming
{
    /// <summary>
    /// Senses what water we're over.
    /// 
    /// 
    /// </summary>
    public class WaterSensor : Sensor
    {
        #region Members
        /// <summary>
        /// internal cache of the water types our actor is currently touching. This
        /// is just so we aren't creating and releasing arrays all the time.
        /// </summary>
        private Terrain.TypeList _typeList = new Terrain.TypeList();

        #endregion Members

        #region Public

        #region Cloning

        /// <summary>
        /// Copy relevant parts to new guy.
        /// </summary>
        /// <returns></returns>
        public override ProgrammingElement Clone()
        {
            WaterSensor clone = new WaterSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(WaterSensor clone)
        {
            base.CopyTo(clone);
        }

        #endregion

        /// <summary>
        /// No action needed.
        /// </summary>
        /// <param name="gameActor"></param>
        public override void StartUpdate(GameActor gameActor)
        {
        }   // end of StartUpdate()

        /// <summary>
        /// No action needed.
        /// </summary>
        /// <param name="gameActor"></param>
        /// <param name="gameThing"></param>
        /// <param name="direction"></param>
        /// <param name="range"></param>
        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
        }   // end of ThingUpdate()

        /// <summary>
        /// No action needed.
        /// </summary>
        /// <param name="gameActor"></param>
        public override void FinishUpdate(GameActor gameActor)
        {
        }

        /// <summary>
        /// See if the gameActor 
        /// </summary>
        /// <param name="gameActor"></param>
        /// <param name="reflex"></param>
        /// <returns></returns>
        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            _typeList.Clear();

            /// We never fire until we have valid data to base a trigger on. We'll know when we have
            /// valid data because the typelist will be non-empty.
            if (gameActor.Chassis.TerrainDataValid)
            {
                gameActor.Chassis.GetWaterMaterials(_typeList);

                reflex.targetSet.Param = _typeList;

                // Test the water materials we are on with any water type filter.
                reflex.targetSet.Action = TestObjectSet(reflex);
            }
            else
            {
                reflex.targetSet.Action = false;
            }

        }   // end of ComposeSensorTargetSet()

        #endregion Public

        #region Internal
        /// <summary>
        /// See if the water types our object is touching match up with our filters.
        /// </summary>
        /// <param name="reflex"></param>
        /// <returns></returns>
        private new bool TestObjectSet(Reflex reflex)
        {
            GameActor gameActor = reflex.Task.GameActor;
            List<Filter> filters = reflex.Filters;

            bool haveType = false;
            bool match = true;
            object param;

            // Only check all this if we're not jumping.  If we are jumping
            // we can just skip this.
            if (!gameActor.Chassis.Jumping)
            {
                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    Filter filter = filters[indexFilter] as Filter;
                    if (!(filter is CountFilter))
                    {
                        if (filter is WaterFilter)
                        {
                            haveType = true;
                        }
                        if (!filter.MatchAction(reflex, out param))
                        {
                            match = false;
                            break;
                        }
                    }
                }


                if (match && !haveType)
                {
                    match = _typeList.HasAnyValid((t) => t != Water.InvalidType);
                }

                /// Manually apply the count filters, because they look at the sensorTargetSet.Count,
                /// which is always zero for non-object based sensors like this.
                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    if (filters[indexFilter] is CountFilter)
                    {
                        CountFilter countFilter = filters[indexFilter] as CountFilter;
                        int count = match ? 1 : 0;
                        match = countFilter.Compare(count);
                    }
                }
            }
            else
            {
                // Jumping.
                match = false;
            }

            if (reflex.Data.GetFilterCountByType(typeof(NotFilter)) > 0)
            {
                match = !match;
            }

            match = PostProcessAction(match, reflex);

            return match;
        }
        #endregion Internal

    }   // end of class WaterSensor

}   // end of namespace Boku.Programming
