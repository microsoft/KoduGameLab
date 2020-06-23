
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
    /// Senses when the physical KeyBoard is used
    /// 
    /// This sensor acts more like a manager than the true source of the sensor event.
    /// It will request the KeyBoardStick and KeyBoardButton filters to provide 
    /// the actual input.  This sensor demonstrates a break in the normal use of 
    /// the model but demonstrates how other elements can be used to solve problems.
    /// </summary>
    public class KeyBoardSensor : Sensor
    {
        List<Filter> movementBuiltins = new List<Filter>();

        public KeyBoardSensor()
        {
            // Builtin support for WASD and arrow key movement.
            KeyBoardKeyFilter W = new KeyBoardKeyFilter(Keys.W, new Vector2(0, 1));
            KeyBoardKeyFilter A = new KeyBoardKeyFilter(Keys.A, new Vector2(-1, 0));
            KeyBoardKeyFilter S = new KeyBoardKeyFilter(Keys.S, new Vector2(0, -1));
            KeyBoardKeyFilter D = new KeyBoardKeyFilter(Keys.D, new Vector2(1, 0));

            KeyBoardKeyFilter Up = new KeyBoardKeyFilter(Keys.Up, new Vector2(0, 1));
            KeyBoardKeyFilter Left = new KeyBoardKeyFilter(Keys.Left, new Vector2(-1, 0));
            KeyBoardKeyFilter Down = new KeyBoardKeyFilter(Keys.Down, new Vector2(0, -1));
            KeyBoardKeyFilter Right = new KeyBoardKeyFilter(Keys.Right, new Vector2(1, 0));

            movementBuiltins.Add(W);
            movementBuiltins.Add(A);
            movementBuiltins.Add(S);
            movementBuiltins.Add(D);
            movementBuiltins.Add(Up);
            movementBuiltins.Add(Left);
            movementBuiltins.Add(Down);
            movementBuiltins.Add(Right);
        }

        public override ProgrammingElement Clone()
        {
            KeyBoardSensor clone = new KeyBoardSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(KeyBoardSensor clone)
        {
            base.CopyTo(clone);
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

            reflex.targetSet.Action = TestObjectSet(reflex);
        }

        private new bool TestObjectSet(Reflex reflex)
        {
            bool match = true;

            if (reflex.Data.GetFilterCountByType(typeof(KeyBoardKeyFilter)) > 0)
            {
                List<Filter> filters = reflex.Filters;

                object param;
                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    Filter filter = filters[indexFilter] as Filter;
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
            else if (reflex.Actuator is MovementActuator || reflex.Actuator is TurnActuator)
            {
                match = false;

                // TODO All this boxing can't be a good thing.  Try to refactor it out.

                // For the built in keyboard args we need to blend their input.  So, start with
                // 0,0 and add in each active key.  Finally, normalize the result.
                reflex.targetSet.Param = Vector2.Zero;
                object param;
                for (int indexFilter = 0; indexFilter < movementBuiltins.Count; indexFilter++)
                {
                    Filter filter = movementBuiltins[indexFilter] as Filter;
                    if (filter.MatchAction(reflex, out param))
                    {
                        match = true;
                        if (param != null)
                        {
                            reflex.targetSet.Param = (Vector2)(reflex.targetSet.Param) + (Vector2)param;
                        }
                    }
                }

                ((Vector2)reflex.targetSet.Param).Normalize();
            }

            if (reflex.Data.GetFilterCountByType(typeof(NotFilter)) > 0)
                match = !match;

            match = PostProcessAction(match, reflex);

            return match;
        }
    }

}   // end of namespace Boku.Programming 
