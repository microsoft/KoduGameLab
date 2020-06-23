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

using Boku.Base;
using Boku.Common;
using Boku.Programming;

namespace Boku.Programming
{
    public class TestTask : Task
    {
        public TestTask()
        {
           
        }

        public TestTask(string id )
        {
            this.upid = id;
        }
        public void InitWithTestData()
        {
            Fixup();
        }
        protected override void InternalFixup()
        {
            Reflex moveReflex;

            moveReflex = (Reflex)GetReflex("reflex.memory-red-fruit-nearby-towardclosest-movement");
            if (moveReflex != null)
            {
                moveReflex.Use += delegate() { brain.GameActor.BalloonExpress(GameActor.BalloonExpressions.RememberedRedPlumpkin); };
            }

            moveReflex = (Reflex)GetReflex("reflex.eyes-red-fruit-towardclosest-movement");
            if (moveReflex != null)
            {
                moveReflex.Use += delegate() { brain.GameActor.BalloonExpress(GameActor.BalloonExpressions.SeeRedPlumpkin); };
            }

            moveReflex = (Reflex)GetReflex("reflex.wander-movement");
            if (moveReflex != null)
            {
                moveReflex.Use += delegate() { brain.GameActor.BalloonExpress(GameActor.BalloonExpressions.Searching); };
            }
            
        }

        protected void Eat(GameThing gameThing, ref Cue cueEat)
        {
            if (gameThing.Classification.taste == Classification.Tastes.Sweet)
            {
                brain.GameActor.BalloonExpress(GameActor.BalloonExpressions.TastyEats);
            }
            else if (gameThing.Classification.taste == Classification.Tastes.Sour)
            {
                brain.GameActor.BalloonExpress(GameActor.BalloonExpressions.SourEats);
            }
            else
            {
                brain.GameActor.BalloonExpress(GameActor.BalloonExpressions.Why);
            }
            // must be "retrieved" every time before a play
            cueEat = BokuGame.Audio.Foley.GetCue("Sqiush");
            cueEat.Play();
        }

       
    }
}
