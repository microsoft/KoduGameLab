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

namespace Boku.Common
{
    /// <summary>
    /// This is a class for keeping track of the screen grab
    /// </summary>
    public class PictureManager
    {
        private bool bDoScreenGrab;
        private bool bScreenGrabEnabled;
        private GameActor screenGrabActor;
        private float screenGrabTimer;
        private float screenGrabDelay = 0.25f;
        private bool bWasFirstPerson;
        private bool bQuiet;

        public bool DoScreenGrab
        {
            get { return bDoScreenGrab; }
        }

        public PictureManager()
        {
            bDoScreenGrab = false;
            screenGrabActor = null;
            bWasFirstPerson = false;
            bQuiet = false;
        }

        public void SetScreenGrabEnabled(bool InDoScreenGrab, GameActor InGameActor, bool InQuiet)
        {
            if (bScreenGrabEnabled != InDoScreenGrab)
            {
                bScreenGrabEnabled = InDoScreenGrab;
                screenGrabActor = InGameActor;
                bQuiet = InQuiet || screenGrabActor.Mute;

                if (bScreenGrabEnabled && screenGrabActor != null)
                {
                    bWasFirstPerson = false;
                    if (InGame.inGame.IsTheFirstPerson(screenGrabActor))
                    {
                        screenGrabTimer = screenGrabDelay;
                        bWasFirstPerson = true;
                    }
                    else
                    {
                        screenGrabActor.DoCameraFirstPerson();
                        screenGrabTimer = 0.0f;
                    }
                }
            }
        }

        public void Update()
        {
            if (bScreenGrabEnabled)
            {
                float deltaTime = Time.GameTimeFrameSeconds;

                screenGrabTimer += deltaTime;
                if (screenGrabTimer > screenGrabDelay)
                {
                    bScreenGrabEnabled = false;
                    bDoScreenGrab = true;
                }
            }
        }

        public void ScreenGrabFinished()
        {
            if (bDoScreenGrab && screenGrabActor != null)
            {
                if (!bWasFirstPerson)
                {
                    screenGrabActor.DoCameraFollowMe();
                }
                bDoScreenGrab = false;
                if (!bQuiet)
                {
                    Boku.Audio.Foley.PlayPhoto(screenGrabActor);
                }
            }
        }
    }   // end of class PictureManager
}   // end of namespace Boku.Common
