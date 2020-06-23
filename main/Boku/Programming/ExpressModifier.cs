
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

using Boku;
using Boku.Base;
using Boku.Common;
using Boku.SimWorld;

namespace Boku.Programming
{
    /// <summary>
    /// this modifier acts like a parameter and provides the expression to the actuator
    /// </summary>
    public class ExpressModifier : Modifier
    {
        public enum Emitters
        {
            NotApplicable,
            Hearts,
            Flowers,
            Stars,
            Swears,
            None,
        }

        [XmlAttribute]
        public Face.FaceState facial = Face.FaceState.NotApplicable;
        [XmlAttribute]
        public Emitters emitter = Emitters.NotApplicable;

        public override ProgrammingElement Clone()
        {
            ExpressModifier clone = new ExpressModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ExpressModifier clone)
        {
            base.CopyTo(clone);
            clone.facial = this.facial;
            clone.emitter = this.emitter;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasFacial)
                param.Facial = this.facial;
            if (!param.HasExpressEmitter)
                param.ExpressEmitter = this.emitter;
        }

    }
}
