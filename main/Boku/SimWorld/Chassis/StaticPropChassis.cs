
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using Boku.Base;
using Boku.SimWorld;

namespace Boku.SimWorld.Chassis
{
    /// <summary>
    /// A do-nothing chassis suitable for static props such as
    /// trees and castles.
    /// </summary>
    public class StaticPropChassis : BaseChassis
    {
        #region Public

        public override bool SupportsStrafing { get { return false; } }

        public StaticPropChassis()
        {
            fixedPosition = true;
        }

        #endregion

    }   // end of class StaticPropChassis

}   // end of namespace Boku.SimWorld.Chassis
