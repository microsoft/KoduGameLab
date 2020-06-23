
using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace Boku
{
    public abstract class CollideObject
    {

        public void Activate(ArrayList list)
        {
            list.Add(this);
        }

        public void Deactivate(ArrayList list)
        {
            list.Remove(this);
        }

    }   // end of abstract class CollideObject

}   // end of namespace Boku


