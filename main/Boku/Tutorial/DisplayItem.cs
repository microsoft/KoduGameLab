using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;

namespace Boku.Tutorial
{
    public delegate bool Condition();
    public delegate void Modify();

    /// <summary>
    /// This represents the base class for instructions and hints
    /// It contains common concepts and functionality from both
    /// 
    /// </summary>
    public abstract class DisplayItem
    {
        public enum Alignment
        {
            Center = 0x0000,
            Left = 0x0001,
            Right = 0x0002,
            Top = 0x0004,
            Bottom = 0x0008,
        }
        /// <summary>
        /// This is the text that represents the title of the instruction/hint; 
        /// </summary>
        public string title;

        /// <summary>
        /// Alignment of the instruction or hint to the screen
        /// Docking support would be nice in the future
        /// </summary>
        public Alignment alignment = Alignment.Top | Alignment.Right;
        /// <summary>
        /// How long the instruction or hint should be displayed or stay active
        /// </summary>
        public float durationSeconds = -1.0f; // -1 means forever
        /// <summary>
        /// A set of conditions that define when this is finished or displayed
        /// Note:  It is a list for the purpose of automatic conditions being added, but
        /// the tutorial writer will just add one
        /// </summary>
        public List<Condition> conditions = new List<Condition>();
        /// <summary>
        /// This will be called when the instruction/hint became active and started
        /// </summary>
        public Modify Starting;
        /// <summary>
        /// This will be called when the instruction/hint is being stoped
        /// </summary>
        public Modify Finishing;

        public abstract void Update();
        public abstract void Render();
        public abstract void Activate();
        public abstract void Deactivate();
        
        public bool Complete
        {
            get
            {
                return this.status == Status.ConditionsMet;
            }
        }

        protected enum Status
        {
            ConditionsNotMet,
            ConditionsMet,
            Expired,
        }

        protected Status status;
        protected Boku.Base.GameTimer timer = new Boku.Base.GameTimer(Boku.Base.GameTimer.ClockType.WallClock);

        protected abstract void Hide(Boku.Base.GameTimer timer);
        public void Continue()
        {
            Deactivate();
            this.status = Status.ConditionsMet;
            TutorialManager.Instance.ActiveTutorial.Next();
        }

        protected void CalcTextPosition(out int x, out int y, Boku.Common.BitmapFont font, string label, int line, int lines)
        {
            int widthLabel = font.MeasureString(label);

            if ((this.alignment & Alignment.Left) != 0)
            {
                x = font.LineHeight; // some spacing
            }
            else if ((this.alignment & Alignment.Right) != 0)
            {
                x = BokuGame.width - widthLabel - font.LineHeight; // some spacing
            }
            else
            {
                x = (BokuGame.width - widthLabel) / 2;
            }
            if ((this.alignment & Alignment.Top) != 0)
            {
                y = font.LineHeight * line + font.LineHeight / 2; // some spacing
            }
            else if ((this.alignment & Alignment.Bottom) != 0)
            {
                y = BokuGame.height - font.LineHeight * (lines - line) - font.LineHeight / 2; // some spacing
            }
            else
            {
                y = (BokuGame.height - font.LineHeight) / 2 + (lines / 2 - line) * font.LineHeight;
            }
        }
        protected void DrawText(int x, int y, Boku.Common.BitmapFont font, string label, Color color)
        {
            font.DrawString(x - 2, y - 1, Color.DarkGray, label);
            font.DrawString(x + 2, y - 1, Color.Gray, label);
            font.DrawString(x - 2, y + 1, Color.Gray, label);
            font.DrawString(x + 2, y + 1, Color.DimGray, label);

            font.DrawString(x, y, color, label);
        }

    }
}
