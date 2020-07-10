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
    public class HearingDevice : SensorDevice
    {
        /// <summary>
        /// normal vector relative to owning game actor
        /// </summary>
        public Vector3 normal;
        /// <summary>
        /// arc of the sensor
        /// </summary>
        public float arc;
        /// <summary>
        /// at a normal volume the max distance something is heard
        /// </summary>
        public float range;

        public HearingDevice(int index)
            : base(Sensor.Category.Hearing, index)
        { }
    }
    /// <summary>
    /// Senses other GameThings by sound
    /// 
    /// Currently this sensor is archived and thus not used.  
    /// 
    /// It was originally planned to be the hearing for any gameactor; 
    /// but without sound in the game its a little abstract
    /// </summary>
    public class AudioSensor : Sensor
    {
        private SensorTargetSet heardSet = new SensorTargetSet();

        public AudioSensor()
        {
            this.category = Sensor.Category.Hearing;
        }
        public override ProgrammingElement Clone()
        {
            AudioSensor clone = new AudioSensor();
            clone.group = this.group;
            clone.helpGroups = this.helpGroups;
            clone.category = this.category;
            clone.upid = this.upid;
            // specifically do not clone the devices
            return clone;
        }

        public override void Reset(Reflex reflex)
        {
            heardSet.Clear();
            base.Reset(reflex);
        }

        public override void StartUpdate(GameActor gameActor)
        {
            heardSet.Clear();
        }

        public override void ThingUpdate(GameActor gameActor, GameThing gameThing, Vector3 direction, float range)
        {
            if (gameThing.Classification.audioVolume != Classification.AudioVolume.Silent &&
                    gameThing.Classification.audioVolume != Classification.AudioVolume.NotApplicable)
            {
                float normalizedRange = range;

                if (gameThing.Classification.audioVolume == Classification.AudioVolume.Loud)
                {
                    normalizedRange *= 0.66f;
                }
                else if (gameThing.Classification.audioVolume == Classification.AudioVolume.Soft)
                {
                    normalizedRange *= 1.33f;
                }
                // for each device check if the object is withing its sense space
                // and add it to the set if it is
                for (int indexDevice = 0; indexDevice < gameActor.HearingDevices.Count; ++indexDevice)
                {
                    HearingDevice device = gameActor.HearingDevices[indexDevice];

                    if (normalizedRange < device.range)
                    {
                        // calc device heading in world space
                        Vector3 deviceHeading = Vector3.TransformNormal(device.normal, gameActor.Movement.LocalMatrix);

                        // check the arc angle
                        float dot = Vector3.Dot(direction, deviceHeading);
                        float angle = (float)Math.Acos(dot);
                        float deviceRadius = device.arc / 2.0f;
                        if (angle < deviceRadius)
                        {
                            heardSet.Add(gameThing, direction, normalizedRange);
                        }
                    }
                }
            }
        }

        public override void FinishUpdate(GameActor gameActor)
        {
            heardSet.Finialize();
        }


        public override SensorTargetSet ComposeSensorTargetSet(GameActor gameActor, Reflex reflex)
        {
            List<Filter> filters = reflex.Filters;

            targetSet.Clear();

            for (int iSet = 0; iSet < heardSet.Count; iSet++)
            {
                bool match = true;
                bool cursorFilterPresent = false;
                SensorTarget target = heardSet[iSet];
                for (int indexFilter = 0; indexFilter < filters.Count; indexFilter++)
                {
                    Filter filter = filters[indexFilter] as Filter;
                    ClassificationFilter cursorFilter = filter as ClassificationFilter;
                    if (cursorFilter != null && cursorFilter.classification.IsCursor)
                    {
                        cursorFilterPresent = true;
                    }

                    if (!filter.MatchTarget(reflex, target, this.category))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    if (!target.Classification.IsCursor || cursorFilterPresent)
                    {
                        targetSet.Add(target.Ref());
                    }
                }
            }

            targetSet.Finialize();
            targetSet.Action = TestObjectSet(reflex);
            return targetSet;
        }
    }
}
