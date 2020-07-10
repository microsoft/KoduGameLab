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
using Boku.Input;

namespace Boku.Programming
{
    /// <summary>
    /// Filter that returns a positive action a matching string is said by another character.
    /// 
    /// </summary>
    public class SaidFilter : Filter
    {
        const int kMaxCount = 10;

        [XmlAttribute]
        public string text;

        public override ProgrammingElement Clone()
        {
            SaidFilter clone = new SaidFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SaidFilter clone)
        {
            base.CopyTo(clone);
            clone.text = this.text;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            bool result = false;
            // Try matching each line.
            for (int i = 0; i < reflex.Data.saidStrings.Count; i++)
            {
                bool atBeginning = reflex.Data.saidMode == 0;
                if (SaidStringManager.MatchText(sensorTarget.GameThing as GameActor, reflex.Data.saidStrings[i], atBeginning))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
        }

    }   // end of class SaidFilter

}   // end of namespace Boku.Programming
