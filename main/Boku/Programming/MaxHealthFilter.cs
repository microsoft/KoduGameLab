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

namespace Boku.Programming
{
    /// <summary>
    /// Filter that just exists as a placeholder.  Actual value is 
    /// extracted when scores are accumulated.
    /// 
    /// </summary>
    public class MaxHealthFilter : Filter
    {
        public override ProgrammingElement Clone()
        {
            MaxHealthFilter clone = new MaxHealthFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MaxHealthFilter clone)
        {
            base.CopyTo(clone);
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return false;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
        }

    }   // end of class MaxHealthFilter

}   // end of namespace Boku.Programming
