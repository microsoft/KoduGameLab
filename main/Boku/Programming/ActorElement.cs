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
    /// Per actor programming element
    /// </summary>
    public abstract class ActorElement : ProgrammingElement
    {
        [XmlAttribute]
        public string[] mountkey; // list of bots this element works on

        [XmlAttribute]
        public string[] mountlock; // list of bots this element will not work


        protected void CopyTo(ActorElement clone)
        {
            base.CopyTo(clone);
            clone.mountkey = this.mountkey;
            clone.mountlock = this.mountlock;
        }

        /// <summary>
        /// Called on the programming element to test if the this element is compatible
        /// </summary>
        /// <param name="gameActor"></param>
        /// <returns></returns>
        public virtual bool ActorCompatible(GameActor gameActor)
        {
            bool compatible = true;
            // check mountkey for inclusions
            if (gameActor != null && this.mountkey != null && this.mountkey.Length > 0)
            {
                compatible = false;
                // a single match means compatible
                for (int indexKey = 0; indexKey < this.mountkey.Length; indexKey++)
                {
                    if (gameActor.Classification.name == mountkey[indexKey] )
                    {
                        compatible = true;
                    }
                }
            }
            // check the mountlock for exclusions
            if (compatible)
            {
                if (gameActor != null && this.mountlock != null && this.mountlock.Length > 0)
                {
                    // a single match means not compatible
                    for (int indexLock = 0; indexLock < this.mountlock.Length; indexLock++)
                    {
                        if (gameActor.Classification.name == mountlock[indexLock])
                        {
                            compatible = false;
                        }
                    }
                }
            }
            return compatible;
        }
    }
}
