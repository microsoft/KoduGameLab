
using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;
using Boku.Base;
using Boku.UI;
using Boku.Input;

namespace Boku
{
    /// <summary>
    /// This represents the abstract sub-object to a GameObject
    /// It should contain only those bits that are specific to logic/updating and 
    /// should reference other bits shared with other sub-objects oe the gameobject
    /// in a custom shared sub-object
    /// </summary>
    public abstract class UpdateObject : ArbitraryComparable
    {
        /// <summary>
        /// Called every frame to Update the logic of the object
        /// It should call Update on any contained child UpdateObjects
        /// </summary>
        /// <param name="camera"></param>
        public abstract void Update();

        /// <summary>
        /// Called to activate the Update object; this happens when it is added
        /// to a update list
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Called to deativate the update object; this happens when it is removed
        /// from a update list
        /// </summary>
        public abstract void Deactivate();

    }   // end of abstract class UpdateObject

}   // end of namespace Boku

