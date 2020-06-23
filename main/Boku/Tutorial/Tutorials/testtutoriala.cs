using System;
using System.Collections.Generic;
using System.Text;

namespace Boku.Tutorial.Tutorials
{
    /// <summary>
    /// This class is used by the TutorialTest.Xml file
    /// It demonstrates a simple tutorial that controls input and provides hints
    /// </summary>
    public class TestTutorialA : Tutorial
    {
        public TestTutorialA()
        {
            Instruction instruction;
            Hint hint;

            this.inputConstraint.restriction = Boku.Input.InputConstraint.RestrictionTypes.DisableAllExcept;

            // first instruction
            // Welcome and press A to continue
            instruction = new Instruction();
            instruction.title = "Tutorial on using tutorials";
            instruction.text = "This tutorial will walk you through using a tutorial.\n";
            instruction.text += "It will have all the intructions you need to accomplish using a tutorial.\n";
            instruction.text += "When you are ready to continue, you can press the A button on the gamepad.";
            instruction.modality = Instruction.Modality.ModalWithContinue;
            this.instructions.Add(instruction);

            // next instruction
            // We control and prove our control (timed)
            instruction = new Instruction();
            instruction.title = "We control the Vertical, we control the horizontal";
            instruction.text = "From time to time we will need to take full control. ";
            instruction.text += "Don't worry, will give you back some control as you progress; but right now you will notice that you can't do anything using the gamepad.\n";
            instruction.text += "Go ahead and try, we will wait...";
            instruction.modality = Instruction.Modality.Modal;
            instruction.durationSeconds = 30.0f;
            this.instructions.Add(instruction);

            // next instruction
            // Exit the running world with lots of hints
            instruction = new Instruction();
            instruction.title = "Lets do something real usefull";
            instruction.text = "Now I am going to teach you how to exit a running game (or tutorial) and return to the main menu. ";
            instruction.text += "You can this by pressing the Back button on the gamepad and select the Exit to Main Menu option.";
            instruction.modality = Instruction.Modality.Modeless;
            instruction.conditions.Add(delegate() { return App.Instance.UiMode == "App.TitleMenu"; });
            instruction.Starting += delegate() 
                { 
                    this.inputConstraint.exceptions.Add( "SaveMenu.NavPrev" );
                    this.inputConstraint.exceptions.Add( "SaveMenu.NavNext" );
                    this.inputConstraint.exceptions.Add( "SaveMenu.Select" );
                    this.inputConstraint.exceptions.Add( "Sim.SaveMenu" );
                };
            instruction.Finishing += delegate()
                {
                    this.inputConstraint.exceptions.Clear();
                    this.inputConstraint.restriction = Boku.Input.InputConstraint.RestrictionTypes.EnableAllExcept;
                };
            // add a hint
            hint = new Hint();
            hint.title = "Please press the back button on the game pad to get to the Save/Exit menu";
            hint.conditions.Add(delegate() { return Sim.Instance.UiMode == "RunSim" && App.Instance.UiMode != "App.MiniHub"; });
            hint.durationSeconds = 10.0f;
            hint.hintRepeatSeconds = 15.0f;
            instruction.hints.Add(hint);
            // add a hint
            hint = new Hint();
            hint.title = "Please grab the gamepad.";
            hint.title += "The back button is the small button to the left of the XBox button.";
            hint.conditions.Add(delegate() { return Sim.Instance.UiMode == "RunSim" && App.Instance.UiMode != "App.MiniHub"; });
            hint.durationSeconds = 10.0f;
            hint.hintRepeatSeconds = 40.0f;
            instruction.hints.Add(hint);
            // add a hint
            hint = new Hint();
            hint.title = "The gamepad is the input device you use to play all your favorite games with. ";
            hint.title += "At the top center of it is a big silver button with a green X on it. ";
            hint.title += "That button is the XBox button. ";
            hint.title += "The back button is the small button to the left of it. ";
            hint.conditions.Add(delegate() { return Sim.Instance.UiMode == "RunSim" && App.Instance.UiMode != "App.MiniHub"; });
            hint.durationSeconds = 20.0f;
            hint.hintRepeatSeconds = 70.0f;
            instruction.hints.Add(hint);
            // add a hint
            hint = new Hint();
            hint.title = "Wow, I did't think this was that hard.";
            hint.title += "If you just hit each button, one of them will be the back button and it will bring up the menu.";
            hint.conditions.Add(delegate() { return Sim.Instance.UiMode == "RunSim" && App.Instance.UiMode != "App.MiniHub"; });
            hint.durationSeconds = 20.0f;
            hint.hintRepeatSeconds = 200.0f;
            instruction.hints.Add(hint);
            // add a hint
            hint = new Hint();
            hint.title = "Use the left joystick to move the highlite to the \"Exit to Main Menu\" button.";
            hint.title += "Then press the A button";
            hint.conditions.Add(delegate() { return App.Instance.UiMode == "App.MiniHub"; });
            hint.durationSeconds = 10.0f;
            hint.hintRepeatSeconds = 20.0f;
            hint.repeat = true;
            instruction.hints.Add(hint);
            this.instructions.Add(instruction);


            // last instruction
            // Your done
            instruction = new Instruction();
            instruction.title = "Your done";
            instruction.text = "That was it, enjoy your stay.\nOh, and press the A button to continue";
            instruction.modality = Instruction.Modality.ModalWithContinue;
            this.instructions.Add(instruction);
        }
    }
}
