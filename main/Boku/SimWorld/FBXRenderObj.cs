// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;

using Boku.Base;
using Boku.Common;

namespace Boku.SimWorld
{
    public abstract class FBXRenderObj : RenderObject, INeedsDeviceReset
    {
        protected GameThing parent = null;
        protected FBXModel sro = null;
        protected GetModelInstance getModelInstance;

        #region Accessors
        public FBXModel SRO
        {
            get { return sro; }
        }

        public GameThing Parent
        {
            get { return parent; }
        }
        #endregion Accessors

        // c'tor
        public FBXRenderObj(GameThing parent, GetModelInstance getModelInstance)
        {
            this.parent = parent;
            this.getModelInstance = getModelInstance;
        }   // end of RenderObj c'tor

        public override void Render(Camera camera)
        {
            if (!parent.Visible)
                return;

            Frustum.CullResult cull = camera.Frustum.CullTest(parent.BoundingSphere.Center + parent.Movement.Position, parent.BoundingSphere.Radius);
            if (cull != Frustum.CullResult.TotallyOutside)
            {
                sro.PreRender = parent.PreRender;
                sro.Animators = parent.Animators;
                sro.RenderColor = parent.Classification.ColorRGBA;
                GameActor parentActor = parent as GameActor;
                if (parentActor != null)
                {
                    sro.GlowEmissiveColor = parentActor.GlowEmissivity * parentActor.GlowColor;
                }

                Matrix local = Matrix.CreateScale(parent.ReScale) * parent.Movement.LocalMatrix;

                // Add in squashed transform if appropriate.
                if (parentActor.SquashScale != Vector3.One)
                {
                    if (parent.CurrentState == GameThing.State.Squashed && false)
                    {
                        // Translate down to the ground.
                        // TODO (****) should probably make this happen smoothly.
                        float terrainHeight = Boku.SimWorld.Terra.Terrain.GetHeight(parent.Movement.Position);
                        if (terrainHeight > 0)
                        {
                            Vector3 translation = local.Translation;
                            translation.Z = terrainHeight;
                            local.Translation = translation;
                            parent.Movement.Altitude = translation.Z;
                        }
                    }
                    local = Matrix.CreateScale(parentActor.SquashScale) * local;
                }

                sro.Render(camera, ref local, null);

                sro.PreRender = null;

                parent.DebugDisplay(camera);
            }
        }

        public override void Activate()
        {
        }

        public override void Deactivate()
        {
        }


        public virtual void LoadContent(bool immediate)
        {
            this.sro = this.getModelInstance();
        }

        public virtual void InitDeviceResources(GraphicsDevice device)
        {
        }

        public virtual void UnloadContent()
        {
            this.sro = null;
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

    }   // end of class RenderObj

}
