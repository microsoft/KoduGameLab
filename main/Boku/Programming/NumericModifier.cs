// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.SimWorld;

namespace Boku.Programming
{
    /// <summary>
    /// This modifier acts as a parameter, providing a numeric literal to the actuator or verb.
    /// </summary>
    public class NumericModifier : Modifier
    {
        [XmlAttribute]
        public float value;

        public override ProgrammingElement Clone()
        {
            NumericModifier clone = new NumericModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(NumericModifier clone)
        {
            base.CopyTo(clone);
        }
    }
}
