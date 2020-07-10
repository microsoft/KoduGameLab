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
    /// Filters and returns positive action based upon the number of GameThings in the SensorSet
    /// 
    /// Including the lack of anything in the set.  
    /// It is also used as a hidden default to represent a filter to check 
    /// the presence of ANY in the set (included on all reflexes if not already 
    /// present in another form).
    /// 
    /// replaces AnythingFilter and NothingFilter
    /// </summary>
    public class CountFilter : Filter
    {
        [XmlAttribute]
        public int count1;

        [XmlAttribute]
        public int count2;

        [XmlAttribute]
        public Operand operand1;

        [XmlAttribute]
        public Operand operand2;


        public CountFilter()
        {
        }

        public override ProgrammingElement Clone()
        {
            CountFilter clone = new CountFilter();
            CopyTo(clone);            
            return clone;
        }

        protected void CopyTo(CountFilter clone)
        {
            base.CopyTo(clone);
            clone.count1 = this.count1;
            clone.count2 = this.count2;
            clone.operand1 = this.operand1;
            clone.operand2 = this.operand2;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return true; // not the right type, don't effect the filtering
        }
        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            
            bool match = Compare(reflex.targetSet.Count);

            return match;
        }
        public bool Compare(int count)
        {
            bool match = true;
            if (this.operand1 != Operand.NotApplicapble)
            {
                match = OperandCompare<float>(count, this.operand1, this.count1);
            }
            if (this.operand2 != Operand.NotApplicapble)
            {
                match = match && OperandCompare<float>(count, this.operand2, this.count2);
            }

            return match;
        }
    }
}
