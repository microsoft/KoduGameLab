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
    public class NullSelector : Selector
    {
        public NullSelector()
        {
            upid = ProgrammingElement.upidNull;
        }
        public override ProgrammingElement Clone()
        {
            NullSelector clone = new NullSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(NullSelector clone)
        {
            base.CopyTo(clone);
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            return actionSet; // need to return an action set always even if empty
        }
        public override void Used(bool newUse)
        {
        }
    }
}
