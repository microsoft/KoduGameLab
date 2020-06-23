
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.SimWorld;

namespace Boku
{
    /// <summary>
    /// A basic message box.
    /// </summary>
    public class MessageBox : GameObject, INeedsDeviceReset
    {
        private static Color backgroundColor = Color.Black;

        public static MessageBox Instance = null;

        public class Shared
        {
            public MessageBox parent = null;
            public Camera camera = new PerspectiveUICamera();

            public MessageBoxElement backdrop = null;

            public String message = @"This is a test, this is just a test...";

            // c'tor
            public Shared(MessageBox parent, String message, Color color)
            {
                this.parent = parent;
                this.message = message;

                GraphicsDevice device = BokuGame.Graphics.GraphicsDevice;

                // Create the backdrop.
                backdrop = new MessageBoxElement(message);

                // Push the viewpoint in slightly.  This will cause the 
                // Z depth to be slightly less than other 2D UI elements 
                // so we get correct rendering.
                Vector3 from = camera.From;
                from.Z -= 0.2f;
                camera.From = from;
            }   // end of Shared c'tor


        }   // end of class Shared

        protected class UpdateObj : UpdateObject
        {
            private MessageBox parent = null;
            private Shared shared = null;

            public UpdateObj(MessageBox parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public override void Update()
            {
                GamePadInput pad = GamePadInput.GetGamePad0();

                if ((pad.ButtonB.IsPressed && !pad.ButtonB.WasPressed) || (pad.Back.IsPressed && !pad.Back.WasPressed) || KeyboardInput.WasPressed(Keys.Back) || KeyboardInput.WasPressed(Keys.Escape))
                {
                    parent.Deactivate();

                    GamePadInput.ClearAllWasPressedState(3);
                }

                shared.backdrop.Update();

            }   // end of Update()

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

        }   // end of class MessageBox UpdateObj  

        protected class RenderObj : RenderObject
        {
            private Shared shared;

            public RenderObj(Shared shared)
            {
                this.shared = shared;
            }

            public override void Render(Camera camera)
            {
                GraphicsDevice device = BokuGame.Graphics.GraphicsDevice;

                // Set up params for rendering UI with this camera.
                Fx.ShaderGlobals.SetCamera(shared.camera);

                // Render the backdrop.
                shared.backdrop.Render(shared.camera);
            }

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

        }   // end of class MessageBox RenderObj     


        // List objects.
        public Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private CommandMap commandMap = new CommandMap("App.MessageBox");   // Placeholder for stack.

        #region Accessors
        public bool Active
        {
            get { return (state == States.Active); }
        }
        #endregion

        // c'tor
        public MessageBox(String message, Color color)
        {
            MessageBox.Instance = this;

            shared = new Shared(this, message, color);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(shared);

            Init();
        }   // end of MessageBox c'tor

        private void Init()
        {
            // Create children
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();

                    // Do stack handling here.  If we do it in the update object we have no
                    // clue which order things get pushed and popped and madness ensues.
                    // If we do this in the Activate/Deactivate calls then we get garbaged
                    // handling if we're polling.  What can happen is that we pop ourselves
                    // because of a 'B' button that we polled but then we no longer have input
                    // focus and the next object in the update list may also look for and 
                    // find the B button being pressed.
                    CommandStack.Push(commandMap);
                }
                else
                {
                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);

                    // Do stack handling here.  If we do it in the update object we have no
                    // clue which order things get pushed and popped and madness ensues.
                    // If we do this in the Activate/Deactivate calls then we get garbaged
                    // handling if we're polling.  What can happen is that we pop ourselves
                    // because of a 'B' button that we polled but then we no longer have input
                    // focus and the next object in the update list may also look for and 
                    // find the B button being pressed.
                    CommandStack.Pop(commandMap);
                }

                state = pendingState;
            }

            return result;
        }

        override public void Activate()
        {
            if (state != States.Active)
            {
                pendingState = States.Active;
                BokuGame.objectListDirty = true;
            }
        }

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;
            }
        }


        public void LoadContent(bool immediate)
        {
            BokuGame.Load(shared.backdrop, immediate);
        }   // end of MessageBox LoadContent()

        public void InitDeviceResources(GraphicsDeviceManager graphics)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared.backdrop);
        }   // end of MessageBox UnloadContent()

        public void DeviceReset(GraphicsDeviceManager graphics)
        {
            BokuGame.DeviceReset(shared.backdrop, graphics);
        }

    }   // end of class MessageBox

}   // end of namespace Boku

