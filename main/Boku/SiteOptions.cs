// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

using Boku.Common;

namespace Boku
{
    /// <summary>
    /// Holds per-site runtime options.
    /// </summary>
    public class SiteOptions
    {
        #region Internal

        private bool communityEnabled;

        private static readonly string MyFilename = @"Content\Xml\SiteOptions.Xml";

        private bool runningInDebugger;

        private SiteOptions()
        {
            // Some options we typically only want to enable when not developing, such as instrumentation and auto-update.
            runningInDebugger = System.Diagnostics.Debugger.IsAttached;
        }

        #endregion

        #region "Baked-in" settings (compiled into executable)

        private bool expires = false;

        private DateTime expireAt = new DateTime(2010, 12, 1);

        #endregion

        #region Xml settings (read from xml file)

        public string Product = "General";

        public string Community = "Default";
        
        public string KGLUrl = @"http://www.kodugamelab.com";

        public string SKAuthUrl = "";
        public string SKUrl = "";
        public string SKStorageUrl = "";

        public bool CommunityEnabled
        {
            get { return communityEnabled && NetworkEnabled; }
            set { communityEnabled = value; }
        }

        //public bool WebEnabled = true;
        public bool NetworkEnabled = true;

        // Accessor checks whether we're running in debugger
        // Stupidest. Name. Ever.  Do not trust this to be what
        // it says.  If this is true then the instrumentation box is
        // checked which means we do send instrumentation.
        // If this is false, then the instrumentation box is not checked
        // and we don't send instrumentation.
        // Want to default to true ie default to sending instrumentation.
        public bool InstrumentationUnchecked = true;

        public bool CheckForUpdates = true;

        public string IgnoreVersion = new Version(1, 0).ToString();

        public bool Logon = false;

        public bool UserFolder = false;

        /// <summary>
        /// Should we use fonts that support the Latin Extended-A characters.
        /// </summary>
        public bool InternationalCharacters = false;

        #endregion

        #region "Special" Accessors

        [XmlIgnore]
        public bool Instrumentation
        {
            get { return InstrumentationUnchecked && !runningInDebugger; }
        }

        [XmlIgnore]
        public bool AppHasExpired
        {
            get
            {
                if (expires)
                {
#if !DEBUG
                    const long kTicksPerDay = (long)10000000 * 60 * 60 * 24;
                    long now = DateTime.Now.Ticks;
                    long then = expireAt.Ticks;
                    long diff = then - now;
                    int daysRemaining = (int)(diff / kTicksPerDay);
                    return daysRemaining <= 0;
#endif
                }
                return false;
            }
        }

        #endregion

        #region Public Methods

        public void Save()
        {
            XmlSerializer xml = new XmlSerializer(typeof(SiteOptions));
            
            Stream stream = Storage4.OpenWrite(MyFilename);
            
            xml.Serialize(stream, this);
            
            stream.Close();
        }

        public static SiteOptions Load(StorageSource sources)
        {
            XmlSerializer xml = new XmlSerializer(typeof(SiteOptions));

            Stream stream = Storage4.OpenRead(MyFilename, sources);

            SiteOptions result = (SiteOptions)xml.Deserialize(stream);

            stream.Close();

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Encapsulates an id unique to this installation of Kodu.
    /// </summary>
    public class SiteID
    {
        private static readonly string MyFilename = @"Content\Xml\SiteID.Xml";

        public Guid Value;

        public static SiteID Instance;

        public static void Initialize()
        {
            if (!Load())
            {
                Instance = new SiteID();
                Instance.Value = Guid.NewGuid();
                Instance.Save();
            }
        }

        public void Save()
        {
            XmlSerializer xml = new XmlSerializer(typeof(SiteID));

            Stream stream = Storage4.OpenWrite(MyFilename);

            if (stream != null)
            {
                xml.Serialize(stream, this);

                stream.Close();
            }
        }

        public static bool Load()
        {
            bool result = false;

            Stream stream = null;

            try
            {
                XmlSerializer xml = new XmlSerializer(typeof(SiteID));

                stream = Storage4.OpenRead(MyFilename, StorageSource.UserSpace);

                Instance = (SiteID)xml.Deserialize(stream);

                result = true;
            }
            catch { }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            return result;
        }
    }
}
