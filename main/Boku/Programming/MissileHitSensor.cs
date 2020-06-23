
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
    public class MissileHitTargetParam
    {
        #region Public
        public int defaultDamage;
        public GameThing.Verbs defaultPayload;
        public Vector3 hitPosition;
        public int hitStrength;

        public static MissileHitTargetParam Alloc()
        {
            if (_ready.Count > 0)
            {
                MissileHitTargetParam param = _ready[_ready.Count -1 ];
                _ready.RemoveAt(_ready.Count-1);
                return param;
            }
            return new MissileHitTargetParam();
        }

        public void Release()
        {
            _ready.Add(this);
        }
        #endregion Public

        #region Internal
        private MissileHitTargetParam()
        {
        }
        private static List<MissileHitTargetParam> _ready = new List<MissileHitTargetParam>();
        #endregion Internal
    }

    public class MissileHitSensor : Sensor
    {
        private SensorTargetSet senseSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator senseSetIter = null;

        public MissileHitSensor()
        {
            senseSetIter = (SensorTargetSet.Enumerator)senseSet.GetEnumerator();
        }

        public override ProgrammingElement Clone()
        {
            MissileHitSensor clone = new MissileHitSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MissileHitSensor clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            senseSet.Clear();
            base.Reset(reflex);
        }

        public override void StartUpdate(GameActor gameActor)
        {
            senseSet.Clear();
        }

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
        }

        public override void FinishUpdate(GameActor gameActor)
        {
            senseSet.Add(gameActor.MissileHitSet);
        }

        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            // If we have a "me" filter we need to look at the ShooterHitSet to 
            // see if anything hit us.
            if (reflex.Data.HasTile("filter.me"))
            {
                SensorTargetSet.Enumerator shooterSetIter = (SensorTargetSet.Enumerator)gameActor.ShooterHitSet.GetEnumerator();
                shooterSetIter.Reset();
                while (shooterSetIter.MoveNext())
                {
                    SensorTarget target = (SensorTarget)shooterSetIter.Current;

                    // Filtering doesn't make much sense since the "target" is me.  What
                    // I really want to filter on are the characteristics of the shooter.
                    // Which means I need to know the shooter.
                    SensorTarget shooter = SensorTargetSpares.Alloc();
                    shooter.Init(target.Shooter, target.Direction, target.Range);
                    shooter.Tag = target.Tag;

                    bool match = true;
                    for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                    {
                        Filter filter = filters[indexFilter] as Filter;
                        // Ignore me filter since it shouldn't be possible to shoot yourself.
                        if (filter.upid != "filter.me")
                        {
                            if (!filter.MatchTarget(reflex, shooter))
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                    if (match)
                    {
                        reflex.targetSet.Add(target);
                    }
                }
            }
            else
            {
                // add sensorSet of items to the targetset
                senseSetIter.Reset();
                while (senseSetIter.MoveNext())
                {
                    SensorTarget target = (SensorTarget)senseSetIter.Current;

                    bool match = true;
                    for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                    {
                        Filter filter = filters[indexFilter] as Filter;
                        if (!filter.MatchTarget(reflex, target))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        reflex.targetSet.Add(target);

                        // Until we enable payloads on the 'shot hit' reflex, leave the target in the hit set
                        // so that it will have the payload from the shoot reflex applied to it.
                        //gameActor.MissileHitSet.Remove(target.GameThing);

                        MissileHitTargetParam param = target.Tag as MissileHitTargetParam;

                        SetUpShotDamage(reflex, param);
                    }
                }
            }

            reflex.targetSet.Action = TestObjectSet(reflex);
        }

        private void SetUpShotDamage(Reflex reflex, MissileHitTargetParam param)
        {
            if (param.defaultDamage != 0)
                param.hitStrength = param.defaultDamage;

            VerbActuator verbActuator = reflex.Actuator as VerbActuator;

            if (verbActuator != null)
            {
                // If a damage actuator exists, then do not apply damage and instead let the damage actuator change hit points.
                if (verbActuator.Verb == GameThing.Verbs.Damage)
                {
                    param.hitStrength = 1;
                    param.defaultDamage = 0;
                }
                if (verbActuator.Verb == GameThing.Verbs.Heal)
                {
                    param.hitStrength = -1;
                    param.defaultDamage = 0;
                }
            }
        }
    }
}
