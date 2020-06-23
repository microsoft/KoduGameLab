
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
    public class ScanDevice : SensorDevice
    {
        #region Members

        private Vector3 normal;             // Facing direction of sensor relative to actor.
        private float arc;                  // Sensor arc in radians.
        private float range = 1000.0f;      // Range of sensor

        private float arcCosine = 1.0f;     // Cosine of arc/2 used to compare against the 
                                            // dot product of the device normal and the 
                                            // direction to the object being sensed.  This
                                            // is just here so we arent' calling Math.Cos()
                                            // for every object * every sensor * every frame.

        private Vector3 worldSpaceNormal;   // This is the device normal transformed from the actor's local 
                                            // space into world space.  We cache this here so it's only 
                                            // calculated once per frame.

        #endregion

        #region Accessors

        /// <summary>
        /// The facing direction of the sensor relative to the actor.
        /// </summary>
        public Vector3 Normal
        {
            get { return normal; }
            set { normal = value; }
        }

        /// <summary>
        /// The arc width of the sensor in radians.  An arc equal to Pi 
        /// would cover a full sphere.  Pi/2 would be a hemisphere.
        /// </summary>
        public float Arc
        {
            get { return arc; }
            set
            {
                arc = value;
                arcCosine = (float)Math.Cos(arc / 2.0);
            }
        }

        /// <summary>
        /// The effective range of this sensor.
        /// </summary>
        public float Range
        {
            get { return range; }
            set { range = value; }
        }

        /// <summary>
        /// The device normal which has been transformed from
        /// local actor space into world space.
        /// </summary>
        public Vector3 WorldSpaceNormal
        {
            get { return worldSpaceNormal; }
            set { worldSpaceNormal = value; }
        }

        /// <summary>
        /// Cosine of half the arc angle.
        /// </summary>
        public float ArcCosine
        {
            get { return arcCosine; }
        }

        #endregion

        #region Public

        public ScanDevice(int index)
            : base(Sensor.Category.Scan, index)
        {
        }

        #endregion

    }

    /// <summary>
    /// Senses GameThings without any occluders
    /// 
    /// This sensor demonstrates the MountKey feature, and is only usable on a Saucer or Blimp
    /// 
    /// TODO This is way too much like the SightSensor to leave as a seperate class...
    /// 
    /// </summary>
    public class ScanSensor : Sensor
    {
        #region Members

        private SensorTargetSet senseSet = new SensorTargetSet();
        private SensorTargetSet.Enumerator senseSetIter = null;

        #endregion

        #region Public

        public ScanSensor()
        {
            WantThingUpdate = true;
            category = Sensor.Category.Scan;
            senseSetIter = (SensorTargetSet.Enumerator)senseSet.GetEnumerator();
        }

        public override ProgrammingElement Clone()
        {
            ScanSensor clone = new ScanSensor();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ScanSensor clone)
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

            // Calc device normals in world space.
            for (int i = 0; i < gameActor.ScanDevices.Count; i++)
            {
                ScanDevice device = gameActor.ScanDevices[i];
                device.WorldSpaceNormal = Vector3.TransformNormal(device.Normal, gameActor.Movement.LocalMatrix);
            }
        }   // end of StartUpdate()

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
            // For each device check if the object is withing its sense space
            // and add it to the set if it is
            for (int indexDevice = 0; indexDevice < gameActor.ScanDevices.Count; ++indexDevice)
            {
                ScanDevice device = gameActor.ScanDevices[indexDevice];

                if (range < device.Range)
                {
                    float dot = Vector3.Dot(direction, device.WorldSpaceNormal);
                    if (dot > device.ArcCosine)
                    {
                        // Add object to scan set.
                        senseSet.Add(gameThing, direction, range);
                    }
                }
            }
        }   // end of ThingUpdate()

        public override void FinishUpdate(GameActor gameActor)
        {
            senseSet.Finialize();
        }


        public override void ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            // add normal senseSet of items to the targetset
            //
            senseSetIter.Reset();
            while (senseSetIter.MoveNext())
            {
                SensorTarget target = (SensorTarget)senseSetIter.Current;

                bool match = true;
                bool cursorFilterPresent = false;
                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    Filter filter = filters[indexFilter] as Filter;
                    ClassificationFilter cursorFilter = filter as ClassificationFilter;
                    if (cursorFilter != null && cursorFilter.classification.IsCursor)
                    {
                        cursorFilterPresent = true;
                    }

                    if (!filter.MatchTarget(reflex, target))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    if (!target.Classification.IsCursor || cursorFilterPresent)
                    {
                        reflex.targetSet.Add(target);
                    }
                }
            }

            reflex.targetSet.Finialize();
            if (reflex.targetSet.Nearest == null)
            {
                /// Didn't come up with anything, try searching our memory
                if (SearchMemory(gameActor, reflex))
                {
                    /// Find nearest again, but don't check LOS. This matches old 
                    /// behavior, which assumes that if we remember it, we could see it.
                    reflex.targetSet.Finialize();
                }
            }
            else
            {
                /// We got something, memorize it.
                gameActor.Brain.Memory.MemorizeThing(reflex.targetSet.Nearest.GameThing);
            }
            reflex.targetSet.Action = TestObjectSet(reflex);
            
        }   // end of ComposeSensorTargetSet()

        #endregion

    }   // end of class ScanSensor

}   // end of namespace Boku.Programming
