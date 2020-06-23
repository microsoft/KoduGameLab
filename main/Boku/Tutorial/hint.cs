using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;

namespace Boku.Tutorial
{
    public class Hint : DisplayItem
    {
        /// <summary>
        /// How long to wait after the conditions have been met before the hint is shown,
        /// also, how often to repeat if the conditions remain met
        /// </summary>
        public float hintRepeatSeconds; 
        /// <summary>
        /// Should this hint repeat after it has been shown
        /// </summary>
        public bool repeat; 

        public Hint()
        {
            this.alignment = Alignment.Bottom | Alignment.Right;
        }

        public override void Update()
        {
            // check for complete
            bool conditionsActive = true;
            foreach (Condition condition in this.conditions)
            {
                if (!condition())
                {
                    conditionsActive = false;
                    break;
                }
            }

            if (this.status == Status.ConditionsNotMet && conditionsActive)
            {
                // start the timer
                this.timer.TimerElapsed += ShowHint;
                this.timer.Reset(this.hintRepeatSeconds);
                this.timer.Start();
                this.status = Status.ConditionsMet;
            }
            else if (this.status == Status.ConditionsMet && !conditionsActive)
            {
                // stop the timer
                this.timer.Stop();
                this.timer.TimerElapsed -= ShowHint;
                this.status = Status.ConditionsNotMet;
            }
        }
        public override void Render()
        {
            // render Hint
            //
            int x;
            int y;

            // draw the title
            Boku.Common.BitmapFont font = BokuGame.fontBerlinSansFBDemiBold20;
            ArrayList lines = Boku.Common.TextHelper.SplitMessage(this.title, BokuGame.width - font.LineHeight, font);
            for (int indexLine = 0; indexLine < lines.Count; indexLine++)
            {
                string textLine = lines[indexLine] as string;
                CalcTextPosition(out x, out y, font, textLine, indexLine, lines.Count);
                DrawText(x, y, font, textLine, Color.Yellow);
            }
        }

        protected void ShowHint(Boku.Base.GameTimer timer)
        {
            this.timer.TimerElapsed -= ShowHint;

            TutorialManager.Instance.RenderChildren += Render;
            TutorialManager.Instance.UpdateChildren -= Update;

            // start our show timer if we have one
            if (this.durationSeconds >= 0.0f)
            {
                this.timer.TimerElapsed += Hide;
                this.timer.Reset(this.durationSeconds);
                this.timer.Start();
            }
        }
        protected override void Hide(Boku.Base.GameTimer timer)
        {
            if (this.repeat)
            {
                Activate();
            }
            else
            {
                Deactivate();
            }
        }

        public override void Activate()
        {
            this.status = Status.ConditionsNotMet;

            TutorialManager.Instance.UpdateChildren -= Update; // make sure we aren't already in the list
            TutorialManager.Instance.RenderChildren -= Render;

            TutorialManager.Instance.UpdateChildren += Update;
        }

        public override void Deactivate()
        {
            TutorialManager.Instance.UpdateChildren -= Update;
            TutorialManager.Instance.RenderChildren -= Render;

            // stop the timer
            this.timer.Stop();
            this.timer.TimerElapsed -= ShowHint;
            this.timer.TimerElapsed -= Hide;

            this.status = Status.Expired;
        }
    }
}
