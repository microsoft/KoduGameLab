
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace Boku
{
    /// <summary>
    /// This represents an abstract game object
    /// It could be UI element, a scene, a charactor, almost anything
    /// 
    /// Note that it is left upto the implementor to provide the "sub" objects that 
    /// represent the Rendering or Logic (see RenderObject and UpdateObject) or
    /// any shared state between them
    /// 
    /// It is an important design issue that Logic and Rendering are separated from this 
    /// object and that anything the objects reference often should be kept in a custom
    /// shared sub-object and not referenced back to this object
    /// 
    /// It is ok to not oftenly referenced information in this object
    /// </summary>
    public abstract class GameObject : ArbitraryComparable
    {
        /// <summary>
        /// This is called by the GameListManager as needed to refresh the object 
        /// and allow it to change state; often adding/removing sub-objects as needed
        /// 
        /// It is not called every frame let alone very often like RenderObject.Render and
        /// UpdateObject.Update
        /// 
        /// Returns true if object removes itself from object list.
        /// </summary>
        public abstract bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList);

        /// <summary>
        /// Called to have the object add sub-objects as needed
        /// </summary>
        public abstract void Activate();
        
        /// <summary>
        /// Called to have the object remove sub-objects as needed
        /// </summary>
        public abstract void Deactivate();


        /// <summary>
        /// Called to have the object remove sub-objects as needed.  Generally
        /// this will remove the update object while leaving the render object.
        /// </summary>
        public virtual void Pause() { }

    }   // end of abstract class GameObject

}   // end of namespace Boku
