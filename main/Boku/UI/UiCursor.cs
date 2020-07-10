// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

namespace Boku.UI
{
    public class UiCursor : RenderObject, ITransform
    {

        static protected UiCursor cursor;
        static public UiCursor ActiveCursor
        {
            get
            {
                return cursor;
            }
            set
            {
                cursor = value;
            }
        }

        protected ControlRenderObj renderObj;
        protected bool visable = true;

        public UiCursor(ControlRenderObj renderObj)
        {
            this.renderObj = renderObj;
        }
        public Object Parent
        {
            set
            {
                // Since the cursor is parented we always want the translation to be zero.
                // So, in order to smoothly move the cursor we need to calc the difference 
                // between the old and the new parent's positions and add this to the cursor's
                // current translation.  We then twitch this toward 0.

                ITransform transformCursor = renderObj as ITransform;
                ITransform transformOldParent = renderObj.Parent as ITransform;
                ITransform transformNewParent = value as ITransform;
                Vector3 delta = transformOldParent.World.Translation - transformNewParent.World.Translation;
                transformCursor.Local.Translation += delta;
                transformCursor.Compose();

                TwitchManager.Set<Vector3> set = delegate(Vector3 val, Object param)
                {
                    transformCursor.Local.Translation = val;
                    transformCursor.Compose();
                };
                TwitchManager.CreateTwitch<Vector3>(transformCursor.Local.Translation, Vector3.Zero, set, 0.2, TwitchCurve.Shape.EaseOut);

                transformCursor.Parent = transformNewParent;

            }
            get
            {
                return renderObj.Parent;
            }
        }

        Transform ITransform.Local
        {
            get
            {
                ITransform transform = renderObj as ITransform;
                return transform.Local;
            }
            set
            {
                ITransform transform = renderObj as ITransform;
                transform.Local = value;
            }
        }
        Matrix ITransform.World
        {
            get
            {
                ITransform transform = renderObj as ITransform;
                return transform.World;
            }
        }
        bool ITransform.Compose()
        {
            ITransform transform = renderObj as ITransform;
            return transform.Compose();
        }
        void ITransform.Recalc(ref Matrix parentMatrix)
        {
            ITransform transform = renderObj as ITransform;
            transform.Recalc(ref parentMatrix);
        }
        ITransform ITransform.Parent
        {
            get
            {
                return Parent as ITransform;
            }
            set
            {
                Parent = value;
            }
        }
        public string State
        {
            set
            {
                renderObj.State = value;
            }
        }
        
        public override void Render(Camera camera)
        {
            if (visable)
            {
                renderObj.Render(camera);
            }
        }
        public override void Activate()
        {
            renderObj.Activate();
            visable = true;
        }
        public override void Deactivate()
        {
            renderObj.Deactivate();
            visable = false;
        }
    }
}
