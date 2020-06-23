
using System;
using System.Collections;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;

namespace Boku.Programming
{
    /// <summary>
    /// this modifier acts like a parameter and provides the task id to the actuator
    /// </summary>
    public class TaskModifier : Modifier
    {
        public enum TaskIds
        {
            TaskA,
            TaskB,
            TaskC,
            TaskD,
            TaskE,
            TaskF,
            TaskG,
            TaskH,
            TaskI,
            TaskJ,
            TaskK,
            TaskL,
            SIZEOF
        }
        [XmlAttribute]
        public TaskIds taskid;

        public override ProgrammingElement Clone()
        {
            TaskModifier clone = new TaskModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TaskModifier clone)
        {
            base.CopyTo(clone);
            clone.taskid = this.taskid;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasTaskId)
                param.TaskId = this.taskid;
        }

    }
}
