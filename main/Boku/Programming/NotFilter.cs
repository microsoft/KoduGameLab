// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

namespace Boku.Programming
{
    /// <summary>
    /// Negates the when phrase, causing a trigger when the sensor phrase is a non-match.
    /// </summary>
    public class NotFilter : Filter
    {
        public override ProgrammingElement Clone()
        {
            NotFilter clone = new NotFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(NotFilter clone)
        {
            base.CopyTo(clone);
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return true; // not the right type, don't effect the filtering
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null; // doesn't effect match action
            return true; 
        }
    }
}
