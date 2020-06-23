using System;
using System.Collections.Generic;
using System.Text;

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
    /// Triggers actions (verb) on the GameActor
    /// 
    /// this general purpose class can be used to expose actions that 
    /// GameActors support.  
    /// There are three types (valency) of Verbs supported; 
    /// Intransitive, Transitive, and Ditransitive (see definitions of a verb).  
    /// The triggering of the different valency will cause the GameActor and 
    /// an alternate to be called with one of several Verb handling calls.  
    /// These are DoDirectObjectVerb and DoSubjectVerb and the order and subject will be
    /// different based upon the valency.
    /// </summary>
    public class VerbActuator : Actuator
    {
        [XmlAttribute]
        public GameThing.Verbs Verb;

        public override ProgrammingElement Clone()
        {
            VerbActuator clone = new VerbActuator();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(VerbActuator clone)
        {
            base.CopyTo(clone);
            clone.Verb = this.Verb;
        }


        protected override bool ActuatorUpdate(Reflex reflex)
        {
            bool actedOn = false;

            scratchActionList.Clear();
            ComposeActions(scratchActionList);

            // If the pronoun wasn't specified, use our default from CardSpace.
            if (!reflex.ModifierParams.HasPronoun)
            {
                reflex.ModifierParams.Pronoun = this.defaultPronoun;
            }

            for (int i = 0; i < scratchActionList.Count; ++i)
            {
                BaseAction eff = scratchActionList[i];

                if (eff.Reflex == null)
                    continue;

                if (eff.ActedOn)
                    continue;

                eff.ActedOn = actedOn = true;

                // Who will be acted upon?
                GameThing directObject = null;

                switch (reflex.ModifierParams.Pronoun)
                {
                    case PronounModifier.Pronouns.Me:
                    case PronounModifier.Pronouns.None:
                        directObject = reflex.Task.GameActor;
                        break;

                    case PronounModifier.Pronouns.It:
                        directObject = eff.GameThing;
                        break;

                    default:
                        throw new Exception("Unrecognized pronoun");
                }

                // Perform the action
                eff.ActedOn = actedOn = reflex.Task.GameActor.ExecuteVerb(this.Verb, directObject as GameActor, eff);

            } // end while

            return actedOn;
        }
    }
}
