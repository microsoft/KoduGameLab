// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
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
using Boku.Audio;

namespace Boku.Programming
{
    public class SoundModifier: Modifier
    {
        #region Members
        [XmlAttribute]
        public string sound;

        [XmlAttribute]
        public bool spatial = true;

        [XmlAttribute]
        public float loudness = 1.0f;

        [XmlAttribute]
        public float vlength = 0.0f;

        [XmlAttribute]
        public float previewLength = 0.0f;

        [XmlIgnore]
        protected float previewSeconds = 0.0f;
        #endregion Members

        #region Public
        public SoundModifier()
        {
            WhileHighlitDel += WhileHighlit;
            OnUnHighlightDel += OnUnHighlight;
        }

        public override ProgrammingElement Clone()
        {
            SoundModifier clone = new SoundModifier();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SoundModifier clone)
        {
            base.CopyTo(clone);
            clone.sound = this.sound;
            clone.spatial = this.spatial;
            clone.loudness = this.loudness;
            clone.vlength = this.vlength;
            clone.previewLength = this.previewLength;
        }

        public override void GatherParams(ModifierParams param)
        {
            if (!param.HasSoundUpid)
                param.SoundUpid = this.upid;
        }

        #endregion Public

        #region Internal

        #region AudioPreview
        /// <summary>
        /// Called each frame while this tile is selected (highlighted in UI).
        /// </summary>
        private void WhileHighlit()
        {
            if (previewLength > 0.0f)
            {
                previewSeconds += Time.WallClockFrameSeconds;
                if (previewSeconds > previewLength)
                {
                    OnUnHighlight();
                }
            }
            AudioCue cue = BokuGame.Audio.Play(upid, null, cues);
            if(cue != null)
                cue.OnComplete += OnAudioCueComplete;
        }

        /// <summary>
        /// Called once when this tile goes from highlighted to unhighlighted.
        /// </summary>
        private void OnUnHighlight()
        {
            BokuGame.Audio.Stop(upid, cues);
            previewSeconds = 0.0f;
        }

        private void OnAudioCueComplete(AudioCue cue)
        {
            cues.Remove(cue);
            previewSeconds = 0.0f;
        }

        private List<AudioCue> cues = new List<AudioCue>();
        #endregion AudioPreview
        #endregion Internal
    }
}
