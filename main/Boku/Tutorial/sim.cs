// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Boku.Tutorial
{
    /// <summary>
    /// This class wraps the gameThing class for easy tutorial writing 
    /// and programming restrictions setting
    /// </summary>
    public class Thing
    {
        protected Boku.Base.GameThing gameThing;

        public Thing(Boku.Base.GameThing gameThing)
        {
            this.gameThing = gameThing;
        }
        public Boku.Base.GameThing GameThing
        {
            get
            {
                return this.gameThing;
            }
        }
        public bool Locked; // can't be moved, edited, nor deleted

        //unused 1/10/2008 mattmac
        //List<string> atomsAvailable; // available in the list
        //List<string> atomsDisabled; // disabled in the list
    }

    /// <summary>
    /// This singleton class wraps and exposes simulation information and control in a 
    /// very easy Tutorial interface.  
    /// Today it has only a small set of features that are active
    /// </summary>
    public class Sim
    {
        private Sim()
        {
        }
        public static Sim Instance = new Sim();
        /// <summary>
        /// This is the set of identifiers for the things the user can see in the Selector 
        /// that then can be placed into the world.  Currently not implemented.
        /// </summary>
        public List<string> thingsAvailable; 
        /// <summary>
        /// This is a set of identifiers for the things that will be disabled in the Selector
        /// that then can not be placed into the world.  Currently not implemented.
        /// </summary>
        public List<string> thingsDisabled;  
        
        public class Cursor
        {
            public Thing Target
            {
                get
                {
                    return null;
                }
            }
            public void MoveTo(Thing thing, float standoff, float azimuth, float elevation)
            {
            }
            public void MoveTo(Vector2 position, float standoff, float azimuth, float elevation)
            {
            }
            public void RestrictToArea(List<Vector2> boundingPoly)
            {
            }
        }
        /// <summary>
        /// Although partially implemented, this class currently doesn�t do anything.  
        /// It is meant to allow the Tutorial to change the view to better suit the tutorial.
        /// </summary>
        public class Camera
        {
            public void ZoomTo(Thing thing, float standoff, float azimuth, float elevation)
            {
            }
            public void ZoomTo(Vector2 position, float standoff, float azimuth, float elevation)
            {
            }
            public void ZoomToCursor()
            {
            }
        }
        
        public Camera camera;

        /// <summary>
        /// This method will cause the sim to go into a paused mode.
        /// </summary>
        public void Pause()
        {
            Boku.Common.Time.Paused = true;
        }
        /// <summary>
        /// This method will cause the sim to go into a running mode.
        /// </summary>
        public void Play()
        {
            Boku.Common.Time.Paused = false;
        }
        /// <summary>
        /// This method will fine and return a Thing that represents the instance of a GameThing.
        /// The id of the instance can only be set today by modifying the saved game�s stuff
        /// file and adding the id property.
        /// </summary>
        /// <param name="idThing"></param>
        /// <returns></returns>
        public Thing FindThingById(string idThing )
        {
            IEnumerator gameThings = InGame.inGame.GetGameThingEnumerator();

            while (gameThings.MoveNext())
            {
                Boku.Base.GameThing gameThing = gameThings.Current as Boku.Base.GameThing;
                if (gameThing.id == idThing)
                {
                    return new Thing(gameThing);
                }
            }
            return null;
        }
        /// <summary>
        /// This method will find and return a Thing that represents the class thing.  
        /// Exact class name is not needed
        /// </summary>
        /// <param name="idClassThing"></param>
        /// <returns></returns>
        public Thing FindThingByClass(string idClassThing)
        {
            IEnumerator gameThings = InGame.inGame.GetGameThingEnumerator();

            while (gameThings.MoveNext())
            {
                Boku.Base.GameThing gameThing = gameThings.Current as Boku.Base.GameThing;
                if (gameThing.GetType().Name.Contains( idClassThing ))
                {
                    return new Thing(gameThing);
                }
            }
            return null;
        }
        /// <summary>
        /// This method is not implemented yet.  
        /// It will allow the tutorial to add things at specific locations to allow 
        /// tutorials to expand the set of things to interact with as it progresses.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="idClassThing"></param>
        /// <returns></returns>
        public Thing AddThing(Vector2 position, string idClassThing)
        {
            return null;
        }

        /// <summary>
        /// This property returns a string that describes the current UI mode in the sim.
        /// Due to the Sim not using the same mode model as the CommandMap, you must use this
        /// method rather than the App.UiMode method.
        /// </summary>
        public string UiMode
        {
            get
            {
                return InGame.inGame.CurrentUpdateMode.ToString();
            }
        }

    }
}
