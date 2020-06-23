
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
using Boku.SimWorld.Terra;

namespace Boku.Programming
{
    /// <summary>
    /// Filters based upon the position of the GameThing relative to the GameActor.
    /// </summary>
    public class RelativeFilter : Filter
    {
        public RelativeFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            RelativeFilter clone = new RelativeFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(RelativeFilter clone)
        {
            base.CopyTo(clone);
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            Vector3 actorPos = reflex.Task.GameActor.Movement.Position;
            actorPos.Z += reflex.Task.GameActor.EyeOffset;
            Vector3 actorFacing = reflex.Task.GameActor.Movement.Heading;
            Vector3 actorRight = Vector3.Cross(actorFacing, Vector3.UnitZ);
            Vector3 targetPos = sensorTarget.Position;
            if (sensorTarget.GameThing != null)
            {
                targetPos.Z += sensorTarget.GameThing.EyeOffset;
            }
            Vector3 toTarget = targetPos - actorPos;

            bool match = true;

            switch(upid)
            {
                case "filter.infront":
                    match = Vector3.Dot(actorFacing, toTarget) >= 0;
                    break;
                case "filter.behind":
                    match = Vector3.Dot(actorFacing, toTarget) <= 0;
                    break;
                case "filter.toleft":
                    match = Vector3.Dot(actorRight, toTarget) <= 0;
                    break;
                case "filter.toright":
                    match = Vector3.Dot(actorRight, toTarget) >= 0;
                    break;
                case "filter.over":
                    match = toTarget.Z > 0;
                    break;
                case "filter.under":
                    match = toTarget.Z < 0;
                    break;
                case "filter.lineofsight":
                    Vector3 hit = Vector3.Zero;
                    match = !Terrain.LOSCheckTerrainAndPath(actorPos, targetPos, ref hit);
                    break;

            }

            return match;
        }
        public override bool MatchAction(Reflex reflex, out object param)
        {
            // doesn't effect match action
            param = null;
            return true;
        }

    }   // end of class RelativeFilter

}   // end of namespace Boku.Programming
