
using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Common;

namespace Boku
{
    /// <summary>
    /// This represents the abstract sub-object to a GameObject
    /// It should contain only those bits that are specific to rendering and 
    /// should reference other bits shared with other sub-object or the gameobject
    /// in a custom shared sub-object.
    /// </summary>
    public abstract class RenderObject : ArbitraryComparable
    {
        /// <summary>
        /// Called every frame to Render the object
        /// It should call Render on any contained child RenderObjects
        /// </summary>
        /// <param name="camera"></param>
        public abstract void Render(Camera camera);

        /// <summary>
        /// Called to activate the render object; this happens when it is added
        /// to a render list
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Called to deativate the render object; this happens when it is removed
        /// from a render list
        /// </summary>
        public abstract void Deactivate();

    }   // end of abstract class RenderObject



}   // end of namespace Boku

