// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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
    public class ScoreBucketModifier : Modifier
    {
        [XmlAttribute]
        public ScoreBucket bucket;

        [XmlIgnore]
        public bool isPrivate = false;  // Is this a private score?

        [XmlAttribute]
        public Classification.Colors color
        {
            get { return (Classification.Colors)bucket; }
            set { bucket = (ScoreBucket)value; }
        }

        [XmlIgnore]
        public bool IsColor
        {
            get { return bucket >= ScoreBucket.ColorFirst && bucket <= ScoreBucket.ColorLast; }
        }

        [XmlIgnore]
        public bool IsLetter
        {
            get { return bucket >= ScoreBucket.ScoreA && bucket <= ScoreBucket.ScoreZ; }
        }

        public override ProgrammingElement Clone()
        {
            ScoreBucketModifier clone = new ScoreBucketModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(ScoreBucketModifier clone)
        {
            base.CopyTo(clone);
            clone.bucket = this.bucket;

            clone.isPrivate = this.upid.Contains("local");
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasScoreBucket)
                param.ScoreBucket = this.bucket;
        }

    }
}
