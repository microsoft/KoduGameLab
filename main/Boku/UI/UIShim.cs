
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
using Boku.Fx;
using Boku.Common;
using Boku.SimWorld;

namespace Boku.UI
{
    /// <summary>
    /// Wrapper class to provide a home and camera for UI elements 
    /// that don't have an obvious parent.
    /// </summary>
    public class UIShim : GameObject, INeedsDeviceReset
    {
        // Delegate prototype
        public delegate void AddChildren(List<GameObject> childList, out UiSelector uiSelector, bool ignorePaths);

        public class Shared
        {
            public Camera camera = null;
            public Shared()
            {
                camera = new UiCamera();

                /*
                Vector3 from = camera.From;
                from.X = -4.0f;
                from.Y = 4.0f;
                from.Z = 4.0f;
                camera.From = from;
                */
            }
        }   // end of class Shared


        protected class UpdateObj : UpdateObject
        {
            private Shared shared = null;
            public List<UpdateObject> updateList = null;

            public UpdateObj(ref Shared shared)
            {
                this.shared = shared;
                updateList = new List<UpdateObject>();
            }   // end of UpdateObj c'tor

            public override void Update()
            {
                // Make sure camera resolution is always matching the screen resolution.
                shared.camera.Resolution = new Point((int)BokuGame.ScreenSize.X, (int)BokuGame.ScreenSize.Y);
                shared.camera.Update();
                for (int i = 0; i < updateList.Count; i++)
                {
                    UpdateObject obj = updateList[i];
                    obj.Update();
                }
            }

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

        }   // end of class UpdateObj


        public class RenderObj : RenderObject, INeedsDeviceReset
        {
            private Shared shared = null;
            private Effect effect = null;
            private bool active = false;
            public List<RenderObject> renderList = null;

            public bool Active
            {
                get { return active; }
                private set { active = value; }
            }

            public RenderObj(ref Shared shared)
            {
                this.shared = shared;
                renderList = new List<RenderObject>();
            }   // end of UpdateObj c'tor

            public override void Render(Camera camera)
            {
                // We don't want UI objects being thrown into the regular shadow pass but 
                // since they're in the same renderlist we just have to deal with it.  It 
                // seems to make more sense here to test and bail than to test every single 
                // shadow casting object.

                if (active && (InGame.inGame.renderEffects == InGame.RenderEffect.Normal))
                {
                    // Set up standard UI lighting for any wrapped objects.

                    string oldRig = BokuGame.bokuGame.shaderGlobals.PushLightRig(ShaderGlobals.UIRigName);
                    effect.Parameters["EyeLocation"].SetValue(new Vector4(shared.camera.ActualFrom, 1.0f));

                    effect.Parameters["Shininess"].SetValue(1.0f);
                    effect.Parameters["ShadowAttenuation"].SetValue(0.5f);

                    // Temp disable of blur
                    float dof_maxBlur = effect.Parameters["DOF_MaxBlur"].GetValueSingle();
                    effect.Parameters["DOF_MaxBlur"].SetValue(0.0f);

                    // Temporarily disable any batching. We want our stuff
                    // rendered immediatately.
                    bool batch = InGame.inGame.PushBatching(false);
                    // Make sure we render the best version we have.
                    FBXModel.LockLowLOD = FBXModel.LockLOD.kHigh;
                    // Render our children using the shared UI camera.
                    UiCamera c = shared.camera as UiCamera;
                    c.Offset = Vector3.Zero;
                    for (int i = 0; i < renderList.Count; i++)
                    {
                        RenderObject obj = renderList[i];
                        obj.Render(shared.camera);
                    }
                    FBXModel.LockLowLOD = FBXModel.LockLOD.kAny;
                    InGame.inGame.PopBatching(batch);

                    // Restore the real DOF_MaxBlur. 
                    effect.Parameters["DOF_MaxBlur"].SetValue(dof_maxBlur);

                    BokuGame.bokuGame.shaderGlobals.PopLightRig(oldRig);
                }
            }

            public override void Activate()
            {
                active = true;
            }

            public override void Deactivate()
            {
                active = false;
            }


            public void LoadContent(bool immediate)
            {
                // Init the effect.  Doesn't really matter which one, just
                // has to be one that uses the same shared parameters.
                if (effect == null)
                {
                    effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\Standard");
                    ShaderGlobals.RegisterEffect("Standard", effect);
                }

            }   // end of UIShim RenderObj LoadContent()

            public void InitDeviceResources(GraphicsDevice device)
            {
                
            }

            public void UnloadContent()
            {
                BokuGame.Release(ref effect);
            }   // end of UIShim RenderObj UnloadContent()

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                shared.camera = new UiCamera();
            }

        }   // end of class RenderObj

        //
        // UIShim
        //

        public enum States
        {
            Inactive,
            Active,
        }

        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private bool ignorePaths = false;

        public States State
        {
            get { return state; }
        }

        /// <summary>
        /// True if the selecter is in MouseEdit mode where paths are not displayed.
        /// </summary>
        public bool IgnorePaths
        {
            get { return ignorePaths; }
        }

        Shared shared = null;
        RenderObj renderObj = null;
        UpdateObj updateObj = null;
        List<GameObject> childList = null;  // List of children, must be of type GameObject

        // c'tor
        public UIShim(AddChildren addChildren, out UiSelector uiSelector, bool ignorePaths)
        {
            shared = new Shared();
            renderObj = new RenderObj(ref shared);
            InGame.inGame.SetUIShim(renderObj);

            updateObj = new UpdateObj(ref shared);
            
            childList = new List<GameObject>();

            this.ignorePaths = ignorePaths;
            addChildren(childList, out uiSelector, ignorePaths);
        }   // end of UIShim c'tor


        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;    // Did we delete ourself?

            // Check if state has changed.
            if (pendingState != state)
            {
                if (pendingState == States.Active)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    /// Don't add it to the renderlist, it's handed directly to the 
                    /// InGame so that it can be rendered at the right time. ***
                    renderObj.Activate();
                }
                else if (pendingState == States.Inactive)
                {
                    renderObj.Deactivate();
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                }
                state = pendingState;
            }

            // Refresh children.
            for (int i = 0; i < childList.Count; i++)
            {
                GameObject obj = (GameObject)childList[i];
                obj.Refresh(updateObj.updateList, renderObj.renderList);
            }

            return result;
        }

        public override void Activate()
        {
            pendingState = States.Active;
            for (int i = 0; i < childList.Count; i++)
            {
                GameObject obj = (GameObject)childList[i];
                obj.Activate();
            }
            BokuGame.objectListDirty = true;
        }

        public override void Deactivate()
        {
            for (int i = 0; i < childList.Count; i++)
            {
                GameObject obj = (GameObject)childList[i];
                obj.Deactivate();
            }
            pendingState = States.Inactive;
            BokuGame.objectListDirty = true;
        }


        public void LoadContent(bool immediate)
        {
            BokuGame.Load(renderObj, immediate);
        }   // end of UIShim LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            BokuGame.Unload(renderObj);
        }   // end of UIShim UnloadContent()

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(renderObj, device);
        }

    }   // end of class UIShim

}   // end of namespace Boku.UI
