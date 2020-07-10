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
    /// This modifier is used when calculating scores.  No functionality here. 
    /// </summary>
    public class PercentModifier : Modifier
    {
        public override ProgrammingElement Clone()
        {
            PercentModifier clone = new PercentModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(PercentModifier clone)
        {
            base.CopyTo(clone);
        }

    }   // end of class PercentModifier
}   // end of namespace Boku.Programming
