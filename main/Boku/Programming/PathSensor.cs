
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
    /// Senses is we're over a path.
    /// </summary>
    public class PathSensor : Sensor
    {
        #region Members
        #endregion Members

        #region Public

        #region Cloning

        /// <summary>
        /// Copy relevant parts to new guy.
        /// </summary>
        /// <returns></returns>
        public override ProgrammingElement Clone()
        {
            PathSensor clone = new PathSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(PathSensor clone)
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
            // Unlike water or terrain sensors, we don't filter on specific path types
            // but we can filter on path color.
            bool match = gameActor.Chassis.OverPath && !gameActor.Chassis.Jumping;

            if (match)
            {
                List<Filter> filters = reflex.Filters;

                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    Filter filter = filters[indexFilter] as Filter;
                    ClassificationFilter classificationFilter = filter as ClassificationFilter;
                    if (classificationFilter != null && classificationFilter.classification.Color != gameActor.Chassis.PathColor)
                    {
                        match = false;
                    }
                }

            }

            if (reflex.Data.GetFilterCountByType(typeof(NotFilter)) > 0)
            {
                match = !match;
            }

            reflex.targetSet.Action = match;

        }   // end of ComposeSensorTargetSet()

        #endregion Public

        #region Internal
        #endregion Internal

    }   // end of class PathSensor

}   // end of namespace Boku.Programming
