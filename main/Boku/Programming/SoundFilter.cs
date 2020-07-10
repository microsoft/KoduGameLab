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
    /// <summary>
    /// Filters based upon whether a named sound (or one of its descendents) is currently playing.
    /// 
    /// 
    /// </summary>
    public class SoundFilter : Filter
    {
        #region Members
        [XmlAttribute]
        public string sound;

        [XmlAttribute]
        public float previewLength = 0.0f;

        [XmlIgnore]
        protected float previewSeconds = 0.0f;
        #endregion Members

        private SoundFilter()
        {
        }

        #region Public
        public SoundFilter(SoundModifier soundMod)
        {
            WhileHighlitDel += WhileHighlit;
            OnUnHighlightDel += OnUnHighlight;

            this.sound = soundMod.upid;
            this.upid = soundMod.upid.Replace("modifier.", "filter.");
            this.icon = soundMod.icon;
            this.label = soundMod.label;
            this.previewLength = soundMod.previewLength;

            this.XmlInputs.Add(SensorOutputType.SoundSensor);
            this.XmlCategories.Add(BrainCategories.SoundFilter);
            this.XmlExclusions.Add(BrainCategories.SoundFilter);
            this.OnLoad();
        }

        public override ProgrammingElement Clone()
        {
            SoundFilter clone = new SoundFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(SoundFilter clone)
        {
            base.CopyTo(clone);
            clone.sound = this.sound;
            clone.previewLength = this.previewLength;
        }

        /// <summary>
        /// Is the target playing our sound?
        /// </summary>
        /// <param name="sensorTarget"></param>
        /// <param name="sensorCategory"></param>
        /// <returns></returns>
        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return BokuGame.Audio.IsPlaying(sensorTarget.GameThing, sound);
        }
        /// <summary>
        /// Pass through.
        /// </summary>
        /// <param name="targetSet"></param>
        /// <param name="sensorCategory"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public override bool MatchAction(Reflex reflex, out object param)
        {
            param = null;
            return true;
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
            AudioCue cue = BokuGame.Audio.Play(sound, null, cues);
            if (cue != null)
                cue.OnComplete += OnAudioCueComplete;
        }

        /// <summary>
        /// Called once when this tile goes from highlighted to unhighlighted.
        /// </summary>
        private void OnUnHighlight()
        {
            BokuGame.Audio.Stop(sound, cues);
            previewSeconds = 0.0f;
        }

        /// <summary>
        /// Our preview cue is done, remove and reset.
        /// </summary>
        /// <param name="cue"></param>
        private void OnAudioCueComplete(AudioCue cue)
        {
            cues.Remove(cue);
            previewSeconds = 0.0f;
        }
        /// <summary>
        /// This is just a scratch list for our cue to be put into while it's being previewed.
        /// </summary>
        private List<AudioCue> cues = new List<AudioCue>();
        #endregion AudioPreview

        #endregion Internal

    }
}
