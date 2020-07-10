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
    /// <summary>
    /// The cluster filter looks for a group of objects (>3) that are near each other.  In 
    /// this case "near" is determined by 2 * the sum of the bounding radii of the objects.
    /// By using the bounding radius we get this to work correctly as objects are scaled.
    /// </summary>
    public class ClusterFilter : Filter
    {
        [XmlAttribute]
        public int count;

        [XmlAttribute]
        public Operand operand;

        /// <summary>
        /// Temp list for generating clusters.  Shared by all instances of ClusterFilter.
        /// </summary>
        [XmlIgnore]
        static List<SensorTarget> cluster = new List<SensorTarget>();

        public ClusterFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            ClusterFilter clone = new ClusterFilter();
            CopyTo(clone);            
            return clone;
        }

        protected void CopyTo(ClusterFilter clone)
        {
            base.CopyTo(clone);
            clone.count = this.count;
            clone.operand = this.operand;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return true; // not the right type, don't effect the filtering
        }
        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;

            bool match = false;


            // Do we have enough targets to even bother?
            if (reflex.targetSet.NearestTargets.Count > count)
            {
                for (int i = 0; i < reflex.targetSet.NearestTargets.Count - count; i++)
                {
                    // Start with the cluster containing just the current element.
                    cluster.Clear();
                    cluster.Add(reflex.targetSet.NearestTargets[i]);

                    // For each of the remaining elements, see if they are near enough
                    // to cluster with any of the elements already in the cluster.
                    for (int j = i + 1; j < reflex.targetSet.NearestTargets.Count; j++)
                    {
                        SensorTarget target = reflex.targetSet.NearestTargets[j];

                        foreach (SensorTarget clusterElement in cluster)
                        {
                            float dist = (target.Position - clusterElement.Position).Length();
                            float range = 2.0f * (target.GameThing.BoundingSphere.Radius + clusterElement.GameThing.BoundingSphere.Radius);
                            if (dist < range)
                            {
                                cluster.Add(target);
                                break;
                            }
                        }
                    }

                    // Did we find a cluster?
                    if (cluster.Count > count)
                    {
                        match = true;

                        // Change targetSet.NearestTargets to just hold the cluster.
                        reflex.targetSet.NearestTargets.Clear();
                        foreach (SensorTarget target in cluster)
                        {
                            reflex.targetSet.NearestTargets.Add(target);
                        }

                        break;
                    }
                }   // end of outer loop over elements.
            }   // if we have enough elements to form a cluster.

            // Not strictly needed but it does prevent refs being held longer than needed.
            cluster.Clear();

            return match;
        }   // end of MatchAction()
    }
}
