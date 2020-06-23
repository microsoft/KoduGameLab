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
using Boku.Input;

using Boku.SimWorld.Terra;

namespace Boku.Programming
{
    /// <summary>
    /// Select a specific water type to look out for.
    /// </summary>
    public class WaterFilter : Filter
    {
        #region Members
        #endregion Members

        #region Accessors
        #endregion Accessors

        #region Public

        #region Cloning

        /// <summary>
        /// Stock clone op.
        /// </summary>
        /// <returns></returns>
        public override ProgrammingElement Clone()
        {
            WaterFilter clone = new WaterFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(WaterFilter clone)
        {
            base.CopyTo(clone);
        }

        #endregion Cloning

        /// <summary>
        /// No-op, water filter doesn't know or care about objects.
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="sensorTarget"></param>
        /// <param name="sensorCategory"></param>
        /// <returns></returns>
        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }

        /// <summary>
        /// Look and see if the waters touuched (cached in reflex.Param) match the water type we're looking for (cached
        /// in Reflex.MaterialType).
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="targetSet"></param>
        /// <param name="sensorCategory"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public override bool MatchAction(Reflex reflex, out object param)
        {
            if (reflex.targetSet.Param is Terrain.TypeList)
            {
                Terrain.TypeList typeList = reflex.targetSet.Param as Terrain.TypeList;
                if (typeList != null)
                {
                    if (typeList.HasType(reflex.WaterType))
                    {
                        param = reflex.WaterType;
                        return true;
                    }
                }
            }
            else if (reflex.targetSet.Param is int)
            {
                int type = (int)reflex.targetSet.Param;
                if (type == reflex.WaterType)
                {
                    param = reflex.WaterType;
                    return true;
                }
            }
            param = null;
            return false;
        }

        #endregion Public
    }
}
