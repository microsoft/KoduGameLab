using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

#if NETFX_CORE
    using Windows.Foundation;
#endif

using Boku;
using BokuShared;

namespace Boku.Common.Xml
{
    public class XmlPerUserData
    {
        public string userName = "";

        public bool set = false;
    };

    public class XmlInvertData : XmlPerUserData
    {
        public bool invert = false;
    };

    /// <summary>
    /// User's global options settings.
    /// </summary>
    public class XmlOptionsData
    {
        #region Members

        // Needs to be public for serialization.  Argh.
        public bool showToolTips = true;
        public bool showHints = true;
        public bool showFramerate = false;
        public bool modalToolMenu = true;

        public int helpLevel = 2;   // 0 = none, 1 = mid, 2 = full

        public string username = "";

        public List<XmlInvertData> invertYData = new List<XmlInvertData>();
        public List<XmlInvertData> invertXData = new List<XmlInvertData>();
        public List<XmlInvertData> invertCamX = new List<XmlInvertData>();
        public List<XmlInvertData> invertCamY = new List<XmlInvertData>();

        public float terrainSpeed = 1.0f;

        public float uiVolume = 1.0f;
        public float foleyVolume = 1.0f;
        public float musicVolume = 1.0f;

        // Allow other players to push worlds to you in a sharing session?
        public bool acceptSentLevels = false;

        // Automatically share downloaded levels?
        public bool autoReshareLevels = false;

        // Automatically download other player's favorite levels?
        public bool autoDownloadFavoriteLevels = false;

        public bool checkForUpdates = false;
        public bool checkForUpdatesWasSet = false;

        public bool sendInstrumentation = false;
        public bool sendInstrumentationWasSet = false;

        public bool showIntroVideo = true;

        public bool oldStoreLevelsCopied = false;

        /// <summary>
        /// Secret used with web auth.  We use this to sync with the
        /// server. This will be set to null if invalid.
        /// </summary>
        public string webUserSecret = string.Empty;

        /// <summary>
        /// Normally the user's web secret will be lost when exiting Kodu.
        /// If this is set to true, we will persist the secret so they don't
        /// need to log in each time they run Kodu.
        /// </summary>
        public bool persistWebLogin = false;

        /// <summary>
        /// Display debug info useful for people writing their own tutorials.
        /// </summary>
        public bool showTutorialDebug = false;

        /// <summary>
        /// This contains a list of IDs for hints that the user has disabled.
        /// Any hint not appearing on this list is assumed to be enabled.
        /// TODO (scoy)  Should add an option to the OptionsMenu to clear
        /// this list effectively reseting the disabled state for all hints.
        /// </summary>
        public List<string> disabledHintIDs = new List<string>();

        public int lastAutoSave = -1;

        //NOTE:Don't try to fix this. This value is read from xml based on this spelling.
        public string langauge = "";

        // If true, BBC micro:bit tiles will be included in the tile picker.
        public bool showMicrobitTiles = false;
        private bool temporarilyShowMicrobitTiles = false;

        // Should we save the creator idHash of the current user when Kodu exits?
        public bool keepSignedInOnExit = false;
        public string creatorName = Auth.DefaultCreatorName;
        public string creatorIdHash = Auth.DefaultCreatorHash;

        private static XmlOptionsData _instance = null;
        
        #endregion 

        #region Accessors

        //
        // Note when adding accessors for settings always call Save() at the end of the set function.
        //

        public static bool KeepSignedInOnExit
        {
            get { return Instance.keepSignedInOnExit; }
            set
            {
                Instance.keepSignedInOnExit = value;

                // If setting to true, save out current creator and id.
                if (Instance.keepSignedInOnExit)
                {
                    Instance.creatorName = Auth.CreatorName;
                    Instance.creatorIdHash = Auth.IdHash;
                }
                else
                {
                    // Else overwrite with defaults.
                    Instance.creatorName = Auth.DefaultCreatorName;
                    Instance.creatorIdHash = Auth.DefaultCreatorHash;
                }
                Save();
            }
        }

        public static string CreatorName
        {
            get { return Instance.creatorName; }
            set
            {
                if (Instance.creatorName != value)
                {
                    Instance.creatorName = value; 
                    Save();
                }
            }
        }
        public static string CreatorIdHash
        {
            get { return Instance.creatorIdHash; }
            set
            {
                if (Instance.creatorIdHash != value)
                {
                    Instance.creatorIdHash = value; 
                    Save();
                }
            }
        }

        public static bool ShowToolTips
        {
            get { return Instance.showToolTips; }
            set
            {
                if (Instance.showToolTips != value)
                {
                    Instance.showToolTips = value; 
                    Save();
                }
            }
        }

        public static bool ShowHints
        {
            get { return Instance.showHints; }
            set
            {
                if (Instance.showHints != value)
                {
                    Instance.showHints = value; 
                    Save();
                }
            }
        }

        public static bool ShowFramerate
        {
            get { return Instance.showFramerate; }
            set
            {
                if (Instance.showFramerate != value)
                {
                    Instance.showFramerate = value; 
                    Save();
                }
            }
        }

        public static bool ModalToolMenu
        {
            get { return Instance.modalToolMenu; }
            set
            {
                if (Instance.modalToolMenu != value)
                {
                    Instance.modalToolMenu = value; 
                    Save();
                }
            }
        }

#if NETFX_CORE
        public static string GetCurrentUsername()
        {
            IAsyncOperation<string> op = Windows.System.UserProfile.UserInformation.GetDisplayNameAsync();
            op.AsTask().ConfigureAwait(false);
            string username = op.GetResults();
            return username;
        }
#endif

        /// <summary>
        /// Username set by user at startup.
        /// </summary>
        public static string Username
        {
            get
            {
                if (String.IsNullOrEmpty(Instance.username))
                {
#if NETFX_CORE
                    Instance.username = GetCurrentUsername();
#else
                    Instance.username = System.Environment.UserName;
#endif
                }
                return Instance.username;
            }
            set
            {
                if (Instance.username != value)
                {
                    Instance.username = value; 
                    Save();
                }
            }
        }

        /// <summary>
        /// Help overlay level
        /// 0 = none
        /// 1 = mid
        /// 2 = full
        /// </summary>
        public static int HelpLevel
        {
            get { return Instance.helpLevel; }
            set 
            {
                if (Instance.helpLevel != value)
                {
                    Instance.helpLevel = value;
                    Save();
                }
            }
        }

        public static float TerrainSpeed
        {
            get { return Instance.terrainSpeed; }
            set
            {
                if (Instance.terrainSpeed != value)
                {
                    Instance.terrainSpeed = value;
                    Save();
                }
            }
        }

        public static float UIVolume
        {
            get { return Instance.uiVolume; }
            set 
            {
                if (Instance.uiVolume != value)
                {
                    Instance.uiVolume = value;
                    BokuGame.Audio.SetVolume("UI", value);
                    Save();
                }
            }
        }

        public static float FoleyVolume
        {
            get { return Instance.foleyVolume; }
            set 
            {
                if (Instance.foleyVolume != value)
                {
                    Instance.foleyVolume = value;
                    float total = value * InGame.LevelFoleyVolume;
                    BokuGame.Audio.SetVolume("Foley", total);
                    Save();
                }
            }
        }

        public static float MusicVolume
        {
            get { return Instance.musicVolume; }
            set 
            {
                if (Instance.musicVolume != value)
                {
                    Instance.musicVolume = value;
                    float total = value * InGame.LevelMusicVolume;
                    BokuGame.Audio.SetVolume("Music", total);
                    Save();
                }
            }
        }

        /// <summary>
        /// CheckForUpdatesWasSet keeps track of whether of not the user manually 
        /// set the CheckForUpdates flag.  If so, we use the flag.  If not we set
        /// the default value by looking for the site options file.
        /// The user can set the CheckForUpdates flag either via the OptionsMenu
        /// in the client or command line flags.
        /// </summary>
        public static bool CheckForUpdatesWasSet { get { return Instance.checkForUpdatesWasSet; } }
        public static bool CheckForUpdates
        {
            get
            {
                if (Instance.checkForUpdatesWasSet)
                    return Instance.checkForUpdates;
                else
                    return Program2.InstallerOptCheckForUpdates;
            }
            set
            {
                Instance.checkForUpdatesWasSet = true;
                if (Instance.checkForUpdates != value)
                {
                    Instance.checkForUpdates = value;
                }
                Save();
            }
        }

        /// <summary>
        /// SendInstrumentationWasSet keeps track of whether of not the user manually 
        /// set the SendInstrumentation flag.  If so, we use the flag.  If not we set
        /// the default value by looking for the site options file.
        /// The user can set the SendInstrumentation flag either via the OptionsMenu
        /// in the client or command line flags.
        /// </summary>
        public static bool SendInstrumentationWasSet { get { return Instance.sendInstrumentationWasSet; } }
        public static bool SendInstrumentation
        {
            get
            {
                if (Instance.sendInstrumentationWasSet)
                    return Instance.sendInstrumentation;
                else
                    return Program2.InstallerOptSendInstrumentation;
            }
            set
            {
                Instance.sendInstrumentationWasSet = true;
                if (Instance.sendInstrumentation != value)
                {
                    Instance.sendInstrumentation = value;
                }
                Save();
            }
        }

        public static bool ShowIntroVideo
        {
            get { return Instance.showIntroVideo; }
            set
            {
                if (Instance.showIntroVideo != value)
                {
                    Instance.showIntroVideo = value; 
                }
                Save();
            }
        }

        public static bool OldStoreLevelsCopied
        {
            get { return Instance.oldStoreLevelsCopied; }
            set
            {
                if (Instance.oldStoreLevelsCopied != value)
                {
                    Instance.oldStoreLevelsCopied = value;
                }
                Save();
            }
        }

        /// <summary>
        /// Secret used with web auth.  We use this to sync with the
        /// server. This will be set to null if invalid.
        /// </summary>
        public static string WebUserSecret
        {
            get { return Instance.webUserSecret; }
            set
            {
                if (Instance.webUserSecret != value)
                {
                    Instance.webUserSecret = value; 
                    Save();
                }
            }
        }

        /// <summary>
        /// Normally the user's web secret will be lost when exiting Kodu.
        /// If this is set to true, we will persist the secret so they don't
        /// need to log in each time they run Kodu.
        /// </summary>
        public static bool PersistWebLogin
        {
            get { return Instance.persistWebLogin; }
            set
            {
                if (Instance.persistWebLogin != value)
                {
                    Instance.persistWebLogin = value; 
                    Save();
                }
            }
        }

        /// <summary>
        /// Display debug info useful for people writing their own tutorials.
        /// </summary>
        public static bool ShowTutorialDebug
        {
            get { return Instance.showTutorialDebug; }
            set
            {
                if (Instance.showTutorialDebug != value)
                {
                    Instance.showTutorialDebug = value; 
                }
                Save();
            }
        }

        /// <summary>
        /// Returns the list of disabled hint ids.
        /// </summary>
        public static List<string> DisabledHintIDs
        {
            get { return Instance.disabledHintIDs; }
        }

        /// <summary>
        /// User chosen language ID.
        /// </summary>
        public static string Language
        {
            get { return Instance.langauge; }
            set
            {
                if (Instance.langauge != value)
                {
                    Instance.langauge = value; 
                    Save();
                }
            }
        }

        /// <summary>
        /// The index of the last AutoSave made. If -1, there has been no
        /// AutoSave.
        /// </summary>
        public static int LastAutoSave
        {
            get { return Instance.lastAutoSave; }
            set
            {
                if (Instance.lastAutoSave != value)
                {
                    Instance.lastAutoSave = value;
                    Save();
                }
            }
        }

        /// <summary>
        /// Whether or not to show BBC micro:bit tiles in the tile picker.
        /// </summary>
        public static bool ShowMicrobitTiles
        {
            get { return Instance.showMicrobitTiles || Instance.temporarilyShowMicrobitTiles; }
            set
            {
                if (Instance.showMicrobitTiles != value)
                {
                    Instance.showMicrobitTiles = value; 
                    Save();
                }
            }
        }

        [XmlIgnore]
        public bool TemporarilyShowMicrobitTiles
        {
            set { temporarilyShowMicrobitTiles = value; }
        }

        /// <summary>
        /// Private accessor for current instance.  Note we use lazy evaluation 
        /// to put off the creation as long as possible to give Storage4 time 
        /// to get settled.
        /// </summary>
        private static XmlOptionsData Instance
        {
            get
            {
                if (_instance == null)
                {
                    Init();
                }
                return _instance;
            }
        }

        #endregion

        #region Public

        public static bool GetInvertXAxis(string gamerTag, bool defInvert)
        {
            return Instance.GetInvertData(Instance.invertXData, gamerTag, defInvert);
        }
        public static void SetInvertXAxis(string gamerTag, bool invert)
        {
            Instance.SetInvertData(Instance.invertXData, gamerTag, invert);
            Save();
        }
        public static bool GetInvertYAxis(string gamerTag, bool defInvert)
        {
            return Instance.GetInvertData(Instance.invertYData, gamerTag, defInvert);
        }
        public static void SetInvertYAxis(string gamerTag, bool invert)
        {
            Instance.SetInvertData(Instance.invertYData, gamerTag, invert);
            Save();
        }
        public static bool GetInvertCamX(string gamerTag, bool defInvert)
        {
            return Instance.GetInvertData(Instance.invertCamX, gamerTag, defInvert);
        }
        public static void SetInvertCamX(string gamerTag, bool invert)
        {
            Instance.SetInvertData(Instance.invertCamX, gamerTag, invert);
            Save();
        }
        public static bool GetInvertCamY(string gamerTag, bool defInvert)
        {
            return Instance.GetInvertData(Instance.invertCamY, gamerTag, defInvert);
        }
        public static void SetInvertCamY(string gamerTag, bool invert)
        {
            Instance.SetInvertData(Instance.invertCamY, gamerTag, invert);
            Save();
        }

        /// <summary>
        /// Add the given hint id to the disabled list.
        /// </summary>
        /// <param name="id"></param>
        public static void SetHintAsDisabled(string id)
        {
            if (!DisabledHintIDs.Contains(id))
            {
                DisabledHintIDs.Add(id);
            }
            Save();
        }

        /// <summary>
        /// Resets the 'disabled' status of all hints.
        /// </summary>
        public static void RestoreDisabledHints()
        {
            DisabledHintIDs.Clear();    // Clear the saved list.
            Hints.RestoreAllHints();    // Clear the in-use list.
            Save();
        }

        #endregion

        #region Internal

        /// <summary>
        /// Load or create the options data.
        /// </summary>
        private static void Init()
        {
            _instance = Load();

            // If not loaded, create a new default. 
            if (_instance == null)
            {
                _instance = new XmlOptionsData();

                //Set default language from InstallerLanguage file(if any).
                _instance.langauge = GetInstallerLanguageOrDefault();

                //Save new config.
                Save();
            }

        }   // end of Init()

        private static string FileName()
        {
            return BokuGame.Settings.MediaPath + @"Xml\OptionsData" + Storage4.UniqueMachineID + @".Xml";
        }

        private static string BackCompatFileName()
        {
            return BokuGame.Settings.MediaPath + @"Xml\OptionsData.Xml";
        }

        private static string InstalledLanguageFileName()
        {
            return Storage4.TitleLocation + @"\InstallerLanguage.txt";
        }

        private static string GetInstallerLanguageOrDefault()
        {
            if (Storage4.FileExists(InstalledLanguageFileName(), StorageSource.All))
            {
                var lines = Storage4.ReadAllLines(InstalledLanguageFileName(), StorageSource.All);
                if (lines.Any())
                {
                    return(lines[0]);
                }
            }
            return "";//Default
        }

        /// <summary>
        /// Load current options data.
        /// </summary>
        /// <returns></returns>
        private static XmlOptionsData Load()
        {
            XmlOptionsData xmlData = null;
            
            Stream stream = null;

            DateTime xmlFileTime= new DateTime();//used to compare against InstallLanguage.txt
            try
            {
                // Try the real filename first.
                string xmlFileName = FileName();

                if (Storage4.FileExists(xmlFileName, StorageSource.All))
                {
                    xmlFileTime = Storage4.GetLastWriteTimeUtc(xmlFileName, StorageSource.All);
                    stream = Storage4.OpenRead(xmlFileName, StorageSource.All);
                    XmlSerializer serializer = new XmlSerializer(typeof(XmlOptionsData));
                    xmlData = serializer.Deserialize(stream) as XmlOptionsData;
                }
                else
                {
                    // If not there, try the back compat version.
                    xmlFileName = BackCompatFileName();

                    if (Storage4.FileExists(xmlFileName, StorageSource.All))
                    {
                        xmlFileTime = Storage4.GetLastWriteTimeUtc(xmlFileName, StorageSource.All);
                        stream = Storage4.OpenRead(xmlFileName, StorageSource.All);
                        XmlSerializer serializer = new XmlSerializer(typeof(XmlOptionsData));
                        xmlData = serializer.Deserialize(stream) as XmlOptionsData;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            if (stream != null)
            {
                Storage4.Close(stream);
            }

            //Check for override of language.
            if (xmlData!=null && Storage4.FileExists(InstalledLanguageFileName(), StorageSource.All))
            {
                var langFileTime = Storage4.GetLastWriteTimeUtc(InstalledLanguageFileName(), StorageSource.All);
                //If InstallerLanguage.txt is later than selected language.
                if (langFileTime > xmlFileTime)
                {
                    //Update language to match newly installed version.
                    xmlData.langauge = GetInstallerLanguageOrDefault();
                    Save();
                }
            }

            return xmlData;
        }   // end of Load()

        private static void Save()
        {
            Stream stream = null;

            try
            {
                string xmlFileName = FileName();

                XmlSerializer serializer = new XmlSerializer(typeof(XmlOptionsData));
                stream = Storage4.OpenWrite(xmlFileName);
                serializer.Serialize(stream, _instance);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            if (stream != null)
                Storage4.Close(stream);
        }   // end of Save()

        private static XmlInvertData Find(List<XmlInvertData> list, string userName)
        {
            foreach (XmlInvertData datum in list)
            {
                if (datum.userName == userName)
                    return datum;
            }
            return null;
        }

        private bool GetInvertData(List<XmlInvertData> list, string gamerTag, bool defValue)
        {
            XmlInvertData datum = Find(list, gamerTag);
            if ((datum != null) && (datum.set))
            {
                return datum.invert;
            }
            return defValue;
        }
        public void SetInvertData(List<XmlInvertData> list, string gamerTag, bool invert)
        {
            XmlInvertData datum = Find(list, gamerTag);
            if (datum == null)
            {
                datum = new XmlInvertData();
                datum.userName = gamerTag;
                list.Add(datum);
            }
            datum.set = true;
            datum.invert = invert;
        }

        #endregion

    }   // end of class XmlOptionsData

}   // end of namespace Boku.Common.Xml
