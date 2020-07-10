// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;


namespace Boku.Tutorial
{
    public delegate void GameAction(); // render or update

    /// <summary>
    /// This singleton class represents the manager for any running tutorials
    /// 
    /// </summary>
    public class TutorialManager
    {
        private TutorialManager()
        {
        }
        public static TutorialManager Instance = new TutorialManager();

        public event GameAction RenderChildren;
        public event GameAction UpdateChildren;
        protected Tutorial tutorial;
        bool running; // protects us from multiple Start calls by the sim

        public void Update()
        {
            if (this.UpdateChildren != null)
            {
                this.UpdateChildren();
            }
        }

        public void Render()
        {
            if (this.RenderChildren != null)
            {
                this.RenderChildren();
            }
        }

        public void PrepareTutorial(string tutorialId)
        {
            if (tutorialId == null)
            {
                Stop();
            }
            else 
            {
                if (this.tutorial == null || tutorialId != this.tutorial.GetType().Name)
                {
                    Stop();
                    string fullname = string.Format("Boku.Tutorial.Tutorials.{0}", tutorialId);

                    this.tutorial = (Tutorial)Assembly.GetExecutingAssembly().CreateInstance(fullname);
                }
            }
        }

        public void Start()
        {
            if (this.tutorial != null && !this.running)
            {
                this.tutorial.Activate();
                this.running = true;
            }
        }

        public void Stop()
        {
            if (this.tutorial != null)
            {
                this.tutorial.Deactivate();
                this.tutorial = null;
                this.running = false;
            }
        }

        public Tutorial ActiveTutorial
        {
            get
            {
                return this.tutorial;
            }
        }

 
    }
}
