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
using Boku.SimWorld.Collision;

namespace Boku.Programming
{
    /// <summary>
    /// This represents the "Behavior" piece of a reflex.
    /// It will take a given SensorTargetSet and Modifiers and create/provide an ActionSet for an Actuator
    /// </summary>
    public abstract class Selector : ProgrammingElement
    {
        public enum BlendMode
        {
            Never,
            ReflexUserControlled,
            TaskUserControlled,
            Always,
        }

        [XmlAttribute]
        public BlendMode blendMode = BlendMode.Never;

        [XmlIgnore]
        protected bool canBlend;

        protected ActionSet actionSet = new ActionSet();

        [XmlIgnore]
        public ActionSet ActionSet { get { return actionSet; } }


        /// <summary>
        /// Copy base properties when cloning to reduce risk of error when adding a new selector
        /// </summary>
        /// <param name="other"></param>
        protected void CopyTo(Selector clone)
        {
            base.CopyTo(clone);
            clone.blendMode = this.blendMode;
        }

        protected void UpdateCanBlend(Reflex reflex)
        {
            switch (blendMode)
            {
                case BlendMode.Always:
                    canBlend = true;
                    break;

                case BlendMode.ReflexUserControlled:
                    canBlend = reflex.IsUserControlled;
                    break;

                case BlendMode.TaskUserControlled:
                    canBlend = reflex.Task.IsUserControlled;
                    break;

                default:
                    canBlend = false;
                    break;
            }
        }

        protected void ClearActionSet(ActionSet set)
        {
            for (int i = 0; i < set.Actions.Count; ++i)
            {
                set.Actions[i].Free();
            }
            set.Actions.Clear();
        }

        /// <summary>
        /// A shot at caching anything useful, called once on entering RunSim.
        /// </summary>
        /// <param name="reflex"></param>
        public virtual void Fixup(Reflex reflex)
        {
        }

        /// <summary>
        /// This is called to have the Selector reset any state it has
        /// primarily override for balistic Selectors that have state
        /// </summary>
        public override void Reset(Reflex reflex)
        {
            ClearActionSet(actionSet);
        }

        /// <summary>
        /// This is called when this selector is actively used by the Actuators Arbitrator
        /// primarily override for balistic Selectors that manage state
        /// </summary>
        /// <param name="newUse"></param>
        public abstract void Used(bool newUse);
        /// <summary>
        /// This is called with a given actor, target set, and set of modifiers to provide
        /// an ActionSet that will used with a Actuator/Arbitrator
        /// </summary>
        /// <param name="gameActor">associated GameActor</param>
        /// <returns>return the ActionSet to hand to Arbitrator</returns>
        public abstract ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor);

        private static BitArray scratchOutputs = new BitArray((int)SensorOutputType.SIZEOF);

        /// <summary>
        /// Check if this selector is compatible within the given reflex and replacing the given element
        /// </summary>
        /// <param name="reflex">reflex to check if it works within</param>
        /// <param name="replacedElement">optional, if provided this will replace it</param>
        /// <returns>true if compatible</returns>
        public override bool ReflexCompatible(GameActor actor, ReflexData reflex, ProgrammingElement replacedElement, bool allowArchivedCategories)
        {
            // Actuator must exist
            {
                if (reflex.Actuator == null || reflex.Actuator is NullActuator)
                    return false;
            }

            // Check actuator compatibility
            {
                if (!this.ElementCompatible(reflex.Actuator))
                    return false;
            }

            return base.ReflexCompatible(actor, reflex, replacedElement, allowArchivedCategories);
        }

        protected static bool BlockedFrom(GameActor actor, Vector3 toward, float closest)
        {
            Vector3 from = actor.WorldCollisionCenter;

            Vector3 ray = toward - from;
            float len = ray.Length();
            if (len > closest)
            {
                ray /= len;
                ray *= closest;
            }
            toward = from + ray;

            SimWorld.Terra.Terrain.HitBlock hitBlock = new Boku.SimWorld.Terra.Terrain.HitBlock();

            Vector2 minMaxZ = new Vector2(0.0f, Single.MaxValue);

            float height = actor.Movement.Altitude - SimWorld.Terra.Terrain.GetTerrainAndPathHeight(actor.Movement.Position);

            Vector4 maxStep = new Vector4(
                height + actor.Chassis.WaistOffset, // max single step up
                Single.MinValue, // max step down
                -1.0f, // water depth at which transition land to water occurs (-1 to ignore)
                -1.0f); // water depth at which transition water to land occurs (-1 to ignore)

            switch (actor.Domain)
            {
                case GameActor.MovementDomain.Air:
                    minMaxZ.X = -1.0f;
                    break;
                case GameActor.MovementDomain.Land:
                    maxStep.Z = 1.0f;
                    break;
                case GameActor.MovementDomain.Water:
                    maxStep.X = Single.MaxValue;
                    maxStep.W = 1.0f;
                    break;
            }

            if (SimWorld.Terra.Terrain.Blocked(from, toward, minMaxZ, maxStep, ref hitBlock, actor.Movement.Altitude))
            {
                if (Vector3.DistanceSquared(from, hitBlock.Position) < closest * closest)
                {
                    actor.AddLOSLine(from, hitBlock.Position, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    return true;
                }
                actor.AddLOSLine(from, hitBlock.Position, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
            }
            else
            {
                actor.AddLOSLine(from, toward, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
            }

            return false;

        }   // end of BlockedFrom()

    }   // end of class Selector

}   // end of namespace Boku.Programming
