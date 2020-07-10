// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Boku.Tutorial
{
    /// <summary>
    /// This represents the base class to create a specific tutorial from
    /// </summary>
    public class Tutorial
    {
        /// <summary>
        /// This member is the list of instructions to step through.  
        /// The tutorial writer will create these and add them to the list.  
        /// Currently today this is a flat list and not a graph; which has some 
        /// limitations but with our goal to provide simple tutorials this is 
        /// not a big restriction.  See Class Instruction for more details.
        /// </summary>
        protected List<Instruction> instructions = new List<Instruction>();
        protected int indexActiveInstruction;
        /// <summary>
        /// This member describes the active input constraint to apply to the input system.  
        /// This allows the tutorial writer to focus and restrict the user to the set 
        /// of actions the tutorial can handle and thus help limit the complexity of 
        /// state handling to direct the user.  See Class InputConstraint for more details.
        /// </summary>
        protected Boku.Input.InputConstraint inputConstraint = new Boku.Input.InputConstraint();

        public Tutorial()
        {
        }

        public void Activate()
        {
            Boku.Input.CommandStack.AttachInputConstraint(this.inputConstraint);
            this.indexActiveInstruction = 0;
            this.instructions[this.indexActiveInstruction].Activate();
        }

        public void Deactivate()
        {
            if (this.indexActiveInstruction < this.instructions.Count)
            {
                this.instructions[this.indexActiveInstruction].Deactivate();
            }
            Boku.Input.CommandStack.DetachInputConstraint(this.inputConstraint);
        }

        /// <summary>
        /// go on to the next instruction
        /// </summary>
        public void Next()
        {
            Debug.Assert(this.instructions[this.indexActiveInstruction].Complete);
            this.indexActiveInstruction++;
            if (this.indexActiveInstruction < this.instructions.Count)
            {
                this.instructions[this.indexActiveInstruction].Activate();
            }
            else
            {
                TutorialManager.Instance.Stop();
            }
        }
        // helper functions

    }
}
