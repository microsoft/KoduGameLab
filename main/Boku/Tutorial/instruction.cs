// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;

namespace Boku.Tutorial
{
    /// <summary>
    /// This represents the base class for a single instruction.  
    /// Only one instruction will be active at a time within the tutorial.  
    /// As of today, all instructions are text only (no icons or graphics).
    /// 
    /// </summary>
    public class Instruction : DisplayItem
    {
        public enum Modality
        {
            Modeless, // does not effect input
            Modal,    // all input is blocked
            ModalWithContinue, // all input is blocked except standard continue
        }
        /// <summary>
        /// This is the body of text for the instruction.
        /// </summary>
        public string text;
        
        /// <summary>
        /// How the input should be handled when this instruction is active
        /// </summary>
        public Modality modality; // stop all other input
        public List<Hint> hints = new List<Hint>();

        public override void Update()
        {
            // check for complete
            bool complete = true;
            foreach (Condition condition in this.conditions)
            {
                if (!condition())
                {
                    complete = false;
                    break;
                }
            }
            if (complete)
            {
                Deactivate();
                this.status = Status.ConditionsMet;
                TutorialManager.Instance.ActiveTutorial.Next();
            }
        }

        public override void Render()
        {
            // render instruction
            //
            int x;
            int y;

            // draw the title
            Boku.Common.BitmapFont font = BokuGame.fontBerlinSansFBDemiBold24;
            CalcTextPosition(out x, out y, font, this.title, 0, 1);
            DrawText(x, y, font, this.title, Color.White);
            
            // draw the text
            font = BokuGame.fontBerlinSansFBDemiBold20;
            ArrayList lines = Boku.Common.TextHelper.SplitMessage(this.text, BokuGame.width - font.LineHeight, font);
            for (int indexLine = 0; indexLine < lines.Count; indexLine++)
            {
                string textLine = lines[indexLine] as string;
                CalcTextPosition(out x, out y, font, textLine, indexLine, lines.Count);
                y += font.LineHeight * 2; // offset from title
                DrawText(x, y, font, textLine, Color.SkyBlue);
            }
        }

        public override void Activate()
        {
            this.status = Status.ConditionsNotMet;

            TutorialManager.Instance.UpdateChildren -= Update; // make sure we aren't already in the list
            TutorialManager.Instance.RenderChildren -= Render;

            TutorialManager.Instance.UpdateChildren += Update;
            TutorialManager.Instance.RenderChildren += Render;

            // let the hints know we are starting up
            foreach( Hint hint in this.hints)
            {
                hint.Activate();
            }

            // start our show timer if we have one
            if (this.durationSeconds >= 0.0f)
            {
                this.timer.TimerElapsed += Hide;
                this.timer.Reset(this.durationSeconds);
                this.timer.Start();
            }

            // configure our modality
            if (this.modality == Modality.Modal)
            {
                this.commandMap = Boku.Input.CommandMap.Empty;
            }
            else if (this.modality == Modality.ModalWithContinue)
            {
                this.commandMap = Boku.Input.CommandMap.Deserialize(this, "TutorialModal.xml");
            }
            if (this.modality != Modality.Modeless)
            {
                Boku.Input.CommandStack.AttachCommandOverride(this.commandMap);
                // add a blocking condition
                this.conditions.Add(delegate() { return false; });
            }
            if (this.Starting != null)
            {
                this.Starting();
            }
        }

        public override void Deactivate()
        {
            if (this.modality != Modality.Modeless)
            {
                Boku.Input.CommandStack.DetachCommandOverride(this.commandMap);
                // remove the blocking condition
                this.conditions.RemoveAt(this.conditions.Count - 1);
            }

            // let the hints know we are going away
            foreach (Hint hint in this.hints)
            {
                hint.Deactivate();
            }

            TutorialManager.Instance.UpdateChildren -= Update;
            TutorialManager.Instance.RenderChildren -= Render;

            this.timer.Stop();
            this.timer.TimerElapsed -= Hide;

            this.status = Status.Expired;
            if (this.Finishing != null)
            {
                this.Finishing();
            }
        }

        protected Boku.Input.CommandMap commandMap;

        protected override void Hide(Boku.Base.GameTimer timer)
        {
            Deactivate();
            this.status = Status.ConditionsMet;
            TutorialManager.Instance.ActiveTutorial.Next();
        }
    }
}
