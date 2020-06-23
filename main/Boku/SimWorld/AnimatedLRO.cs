using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Xclna.Xna.Animation;

using Boku.Common;

namespace Boku.SimWorld
{
    public class AnimatedSRO : FBXModel
    {

        protected AnimatedSRO(String resourceName)
            : base(resourceName)
        {
        }

        // debug function for looking at translations coming out of the animation system...
        //public void DumpTranslations()
        //{
        //    for (int i = 0; i < model.Bones.Count; i++)
        //    {
        //        Console.WriteLine("Name: " + animator.BonePoses[i].Name + " Index: " + i);
        //        Matrix a = animator.BonePoses[i].DefaultTransform;
        //        Matrix b = model.Bones[i].Transform;
        //        Console.WriteLine("- aX: " + a.Translation.X + " aY: " + a.Translation.Y + " aZ: " + a.Translation.Z);
        //        Console.WriteLine("- bX: " + b.Translation.X + " bY: " + b.Translation.Y + " bZ: " + b.Translation.Z);
        //    }
        //}


    }
}
