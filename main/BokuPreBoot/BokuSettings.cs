// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Boku.Common;

namespace Boku
{

    /* HOW TO USE
     * 
     * // Load the settings by referencing the static Settings property.
     * // This will only hit the disk once and will cache a singleton
     * // of the BokuSettings class.
     * BokuSettings mySettings = BokuSettings.Settings;
     * 
     * // Look at properties on the settings object:
     * if(mySettings.Fullscreen) { ...
     * 
     * // Set settings directly on the settings object
     * mySettings.Bloom = true;
     * 
     * // Save using static functions - don't need to have a reference to the instance
     * // This writes the singleton to disk
     * BokuSettings.Save()
     * 
     * // Don't allocate your own BokuSettings object (you can't)
     * BokuSettings IWantMyOwnInstance = new BokuSettings() // ERROR - constructor is protected
     */
     
    public class BokuSettings
    {
        // These are the application settings. Keep them together here at the top of the file
        // They're accessed through the simple setters and getters in the next section,
        // although that's pretty much a formality as the exact same interface could
        // be exposed by just making these public.
        // The defaults provided here are for safety only on the pc; the real defaults are supplied in a file
        // that is part of the install. These will only take effect if the default settings files are missing.
        // On the XBox, settings are never loaded from disk, so these defaults are always used
        private bool fullScreen = false;
        private bool postFX = false;
        private bool lowModels = false;
        private bool antiAlias = false;
        private bool animation = false;
        private bool audio = false;
        private bool preferReach = false;
        private int resolutionX = 640;
        private int resolutionY = 480;
        private string userFolder = "";
        private int terrainRenderMethod = 2; //FewerDraws algorithm
        private bool vsync = false;
        private bool useSystemFontRendering = true;     // If false, revert to using SpriteFont rendering rather than SystemFont.

        private string language = null;

        #region simple setters and getters (no validation)
        public bool FullScreen
        {
            get { return fullScreen; }
            set { fullScreen = value; }
        }

        public bool PostEffects
        {
            get { return postFX; }
            set { postFX = value; }
        }

        public bool LowModels
        {
            get { return lowModels; }
            set { lowModels = value; }
        }

        public bool AntiAlias
        {
            get { return antiAlias; }
            set { antiAlias = value; }
        }

        public bool Animation
        {
            get { return animation; }
            set { animation = value; }
        }

        public bool Audio
        {
            get { return audio; }
            set { audio = value; }
        }

        /// <summary>
        /// The terrain rending code as it is set up now assumes
        /// that the graphics mode preference will be set before
        /// Terrain.LoadShared() is called. Also, the terrain
        /// classes require that the graphics mode preference 
        /// never be changed once set.
        /// 
        /// PreferReach should ONLY refer to the user's preference.
        /// This may be false even if the user has hw that only
        /// supports reach.  So, do not use this for in game stuff.
        /// It should only be used at game startup.
        /// </summary>
        public bool PreferReach
        {
            get { return preferReach; }
            set { preferReach = value; }
        }

        public int ResolutionX
        {
            get { return resolutionX; }
            set { resolutionX = value; }
        }

        public int ResolutionY
        {
            get { return resolutionY; }
            set { resolutionY = value; }
        }

        /// <summary>
        /// UserFolder allows an override of where the user's MyWorlds are stored.
        /// An empty string
        /// </summary>
        public string UserFolder
        {
            get { return userFolder; }
            set { userFolder = value; }
        }

        public int TerrainRenderMethod
        {
            get { return terrainRenderMethod; }
            set { terrainRenderMethod = value; }
        }

        public string Language
        {
            get { return language; }
            set { language = value; }
        }

        public bool Vsync
        {
            get { return vsync; }
            set { vsync = value; }
        }

        /// <summary>
        /// Default is true which uses the SystemFont rendering.
        /// If false, revert to using SpriteFont rendering rather than SystemFont.
        /// </summary>
        public bool UseSystemFontRendering
        {
#if NETFX_CORE
            get { return false; }
            set { }
#else
            get { return useSystemFontRendering; }
            set { useSystemFontRendering = value; }
#endif
        }

        #endregion

        // We maintain a single instance of the settings object, and use static
        // functions to access it.
        static private BokuSettings theInstance = null;

        // constructor is private so people don't inadvertently allocate one directly.
        // Use the BokuSettings.Load() 
        private BokuSettings()
        {
        }

        /* Access the settings object through this property. Only touches disk the first time.
         */
        static public BokuSettings Settings
        {
            get
            {
                if (null == theInstance)
                {
                    theInstance = Load(SettingsFileName());
                    System.Diagnostics.Debug.Assert(null != theInstance, "Default settings file is missing.");

                    if (theInstance.PreferReach)
                    {
                        ConstrainToReach();
                    }

                    // disaster recovery: preferred on-disk defaults are missing; throw in some workable options
                    if (null == theInstance)
                    {
                        theInstance = new BokuSettings();
                    }
                }
                return theInstance;
            }
            // no setter; set values on the instance and call Save()
        }

        /// <summary>
        /// One stop call to set the maximally limited set of constraints.
        /// </summary>
        static public void ConstrainToReach()
        {
            Settings.PreferReach = true;
            Settings.PostEffects = false;
            Settings.LowModels = true;
        }

        /* Save the settings singleton into a file, creating if necessary.
         * If the singleton has not been allocated, the user never accessed it,
         * which means it could not possibly have been modified.
         * We could do a load and then save, but that would be fairly meaningless.
         * We could also create a default settings and then save, but that might
         * overwrite an existing save file.
         */
        public static void Save()
        {
            if (null != theInstance)
            {
                string fileName = SettingsFileName();
                Stream stream = Storage4.OpenWrite(fileName);
                Save(stream);
                stream.Close();
            }
        }

        public static void Save(Stream stream)
        {
            XmlSerializer s = new XmlSerializer(typeof(BokuSettings));
            s.Serialize(stream, theInstance);
        }

        /* Reset the settings to application defaults.
         * Does not save to disk / persist: need to call Save() to do that
         */
        public static BokuSettings LoadDefaults()
        {
            string fileName = SettingsFileName();
            return Load(fileName, StorageSource.TitleSpace);
        }

        public static BokuSettings LoadLowestQuality()
        {
            return Load(@"Content\Xml\Lowest Boku Settings.xml", StorageSource.TitleSpace);
        }

        public static BokuSettings LoadHighestQuality()
        {
            return Load(@"Content\Xml\Highest Boku Settings.xml", StorageSource.TitleSpace);
        }

        /* Standard name used for the "current" settings file. A default one exists in title space;
         * once the user has changed it it's kept in user space.
         */
        private static string SettingsFileName()
        {
            return @"Content\Xml\Boku Settings.xml";
        }

        /* Load the settings object from disk if it exists; otherwise allocate a new
         * one in RAM and return that. Does not save, as you might change your mind.
         * This is internal only; we use a singleton that's kept in memory, so users
         * should not decide when to load.
         * use BokuSettings.Settings to get the current instance - it will load for you.
         * Looks first in user space, then in title space.
         */
        private static BokuSettings Load(string fileName)
        {
            return Load(fileName, StorageSource.All);
        }

        /* Load a settings object, but restrict the search to title space, not user space.
         * Used to load factory-installed settings
         */
        private static BokuSettings Load(string fileName, StorageSource sources)
        {
            if (Storage4.FileExists(fileName, sources))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BokuSettings));
                Stream stream = Storage4.OpenRead(fileName, sources);
                theInstance = (BokuSettings)serializer.Deserialize(stream);
                stream.Close();
            }
            return theInstance;
        }

    }
}

// #endif
