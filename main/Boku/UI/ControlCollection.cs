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
    public class ControlCollection : INeedsDeviceReset
    {
//        private Model model;
        private Dictionary<String, ClassRenderObj> collection = new Dictionary<String, ClassRenderObj>();

        public const string PanelClassName = "panel:";
        public const string ButtonClassName = "button:";
        public const string CursorClassName = "cursor:";
        public const string Horizontal3GridClassName = "h3g:";

        public const string H3gLeftPartName = "h3gleft:";
        public const string H3gRepeatPartName = "h3grepeat:";
        public const string H3gRightPartName = "h3gright:";

        public const string H3gLeftPartTag = ".left";
        public const string H3gRepeatPartTag = ".repeat";
        public const string H3gRightPartTag = ".right";

        private string modelName = null;

        public ControlCollection(string modelName)
        {
            this.modelName = modelName;

            BokuGame.Load(this);
        }

        protected void AddClassRender(string classname, Model model, ModelBone bone)
        {
            string controlName;
            controlName = bone.Name.Substring(classname.Length).Trim();
            // create a class render object
            ClassRenderObj classRender = new ClassRenderObj(classname, model, bone);
            collection.Add(controlName, classRender);
        }

        protected void AddClassPartRender(string classname, string partname, Model model, ModelBone bone)
        {
            string controlName;
            controlName = bone.Parent.Name.Substring(classname.Length).Trim() + partname;
            // create a class render object
            ClassRenderObj classRender = new ClassRenderObj(classname, model, bone);
            collection.Add(controlName, classRender);
        }

        public ControlRenderObj InstanceControlRenderObj(Object parent, string controlName )
        {
            ControlRenderObj controlRenderObj = null;
            ClassRenderObj classRenderObj;
            if (collection.TryGetValue(controlName, out classRenderObj))
            {
                controlRenderObj = new ControlRenderObj(parent, classRenderObj);
            }
            return controlRenderObj;
        }


        public void LoadContent(bool immediate)
        {
            Model model = BokuGame.Load<Model>(BokuGame.Settings.MediaPath + modelName);
            ModelHelper.DumpBoneTree(model, model.Root, true);

            foreach (ModelBone bone in model.Root.Children)
            {
                if (bone.Name.StartsWith(PanelClassName, StringComparison.OrdinalIgnoreCase))
                {
                    AddClassRender(PanelClassName, model, bone);
                }
                else if (bone.Name.StartsWith(ButtonClassName, StringComparison.OrdinalIgnoreCase))
                {
                    AddClassRender(ButtonClassName, model, bone);
                }
                else if (bone.Name.StartsWith(CursorClassName, StringComparison.OrdinalIgnoreCase))
                {
                    AddClassRender(CursorClassName, model, bone);
                }
                else if (bone.Name.StartsWith(Horizontal3GridClassName, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ModelBone bonePart in bone.Children)
                    {
                        if (bonePart.Name.StartsWith(H3gLeftPartName, StringComparison.OrdinalIgnoreCase))
                        {
                            AddClassPartRender(Horizontal3GridClassName, H3gLeftPartTag, model, bonePart);
                        }
                        else if (bonePart.Name.StartsWith(H3gRepeatPartName, StringComparison.OrdinalIgnoreCase))
                        {
                            AddClassPartRender(Horizontal3GridClassName, H3gRepeatPartTag, model, bonePart);
                        }
                        else if (bonePart.Name.StartsWith(H3gRightPartName, StringComparison.OrdinalIgnoreCase))
                        {
                            AddClassPartRender(Horizontal3GridClassName, H3gRightPartTag, model, bonePart);
                        }
                    }
                }
                // add other supported classes here otherwise they get
                // tossed out and ingored
            }
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
            collection.Clear();
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }
    }
}
