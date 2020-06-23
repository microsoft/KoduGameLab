
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Common.Localization;

namespace Boku.Common.TutorialSystem
{
    /// <summary>
    /// Crumbs are the edges of our navigation graph.  They are specified by two modes, curMode which
    /// is the starting mode and targetMode which is the mode we are transitioning to.  TargetMode may
    /// be left Unknown.  In that case, the crumb is just expected to give the user context relevant 
    /// information about the current mode they are in.
    /// curModes is implemented as an array to help cut down on the explosion of potential edges when
    /// working with the edit mode tools.
    /// </summary>
    public class Crumb
    {
        #region Members

        [XmlAttribute("id")]
        public string id = null;

        // The current mode we're in.
        [XmlArrayItem("mode")]
        public TutorialManager.GameMode[] curModes = null;

        // The mode this crumb allows us to transition to.
        public TutorialManager.GameMode targetMode = TutorialManager.GameMode.Unknown;

        public string gamepadText = null;
        public string mouseText = null;
        public string touchText = null;

        // List of modes used when trying to find a path
        // from the current mode to the desired mode.
        private static List<TutorialManager.GameMode> modeList = new List<TutorialManager.GameMode>();

        #endregion

        #region Accessors

        /// <summary>
        /// The current mode we're in.
        /// </summary>
        [XmlIgnore]
        public TutorialManager.GameMode[] CurModes
        {
            get { return curModes; }
        }

        /// <summary>
        /// TargetMode is the mode we are transitioning to.  TargetMode may be
        /// Unknown.  In that case the crumb is just expected to give the user
        /// context relevant information about the current mode they are in.
        /// </summary>
        [XmlIgnore]
        public TutorialManager.GameMode TargetMode
        {
            get { return targetMode; }
        }

        /// <summary>
        /// Text to be displayed to the user if gamepad is current input device.
        /// </summary>
        [XmlIgnore]
        public string GamepadText
        {
            get { return gamepadText; }
        }

        /// <summary>
        /// Text to be displayed to the user if mouse is current input device.
        /// </summary>
        [XmlIgnore]
        public string MouseText
        {
            get { return mouseText; }
        }

        
        /// <summary>
        /// Text to be displayed to the user if touch is current input device.
        /// </summary>
        [XmlIgnore]
        public string TouchText
        {
            get { return touchText==null ? "" : touchText; }
        }
        

        #endregion

        #region Public

        /// <summary>
        /// c'tor for Crumbs.  Probably never used in code 
        /// but needs to be public for deserialization.
        /// </summary>
        public Crumb()
        {
        }

        public sealed class CrumbList : BokuShared.XmlData<CrumbList>
        {
            [XmlArrayItem("Crumb")]
            public Crumb[] Crumbs = null;
        }

        public static Crumb[] Init()
        {
            CrumbList crumbList = null;
            CrumbList crumbStrings = null;
            const string stringsFilename = @"TutorialStrings.Xml";
            const string crumbsFilename = @"TutorialCrumbs.Xml";

            // Get the actual crumb list.  This contains all the mode links.
            // Note, since this no longer contains strings it is no longer inthe Localizable folder...
            string filename = Path.Combine(@"Content\Xml", crumbsFilename);
            if (Storage4.FileExists(filename, StorageSource.TitleSpace))
            {
                crumbList = Load(filename, StorageSource.TitleSpace);
            }
            else
            {
                Debug.Assert(false, "Missing file!");
            }

            // Now get the strings for each of the crumbs.
            // First we need to get the default, English version.  Look in both user and title space
            // since the shipped version may have been superceeded by a downloaded one.
            if (Storage4.FileExists(Path.Combine(Localizer.DefaultLanguageDir, stringsFilename), StorageSource.All))
            {
                crumbStrings = Load(Path.Combine(Localizer.DefaultLanguageDir, stringsFilename), StorageSource.All);
            }
            else
            {
                Debug.Assert(false, "Missing file!");
            }

            Crumb[] result = null;

            if (crumbStrings != null)
            {
                // Start with list of crumbs.
                result = crumbList.Crumbs;

                // Now match up crumbs with their strings.
                foreach (Crumb crumb in result)
                {
                    // Find matching strings and copy over.
                    for (int i = 0; i < crumbStrings.Crumbs.Length; i++)
                    {
                        Crumb strCrumb = crumbStrings.Crumbs[i];
                        if (crumb.id == strCrumb.id)
                        {
                            crumb.gamepadText = strCrumb.gamepadText;
                            crumb.mouseText = strCrumb.mouseText;
                            crumb.touchText = strCrumb.touchText;
                        }
                    }
                }
            }

            // Is our run-time local language different from the default?
            if (!Localizer.IsLocalDefault)
            {
                var localPath = Localizer.LocalLanguageDir;

                // Do we have a directory for the local language?
                if (localPath != null)
                {
                    var localFile = Path.Combine(localPath, stringsFilename);

                    if (Storage4.FileExists(localFile, StorageSource.All))
                    {
                        CrumbList localCrumbs = Load(localFile, StorageSource.All);

                        if (result != null && localCrumbs != null)
                        {
                            // Loop over each of the crumbs we currently have...
                            for (int i = 0; i < result.Length; i++)
                            {
                                var defCrumb = result[i];
                                var foundLocalCrumb = false;

                                // Loop over each of the localized crumbs searching for a match to existing crumbs.
                                for (int j = 0; j < localCrumbs.Crumbs.Length; j++)
                                {
                                    var localCrumb = localCrumbs.Crumbs[j];

                                    // If the crumbs are the same, override the default with the localized
                                    if (localCrumb.id == defCrumb.id)
                                    {
                                        if (Localizer.ShouldReportMissing
                                            && localCrumb.GamepadText.Equals(defCrumb.GamepadText, StringComparison.OrdinalIgnoreCase)
                                            && localCrumb.MouseText.Equals(defCrumb.MouseText, StringComparison.OrdinalIgnoreCase)
                                            && localCrumb.TouchText.Equals(defCrumb.TouchText, StringComparison.OrdinalIgnoreCase)
                                            )
                                        {
                                            Localizer.ReportIdentical(stringsFilename, "TargetMode: " + defCrumb.TargetMode.ToString());
                                        }

                                        // Copy any localized strings over.
                                        if (!string.IsNullOrEmpty(localCrumb.GamepadText))
                                        {
                                            result[i].gamepadText = localCrumb.GamepadText;
                                        }
                                        if (!string.IsNullOrEmpty(localCrumb.MouseText))
                                        {
                                            result[i].mouseText = localCrumb.MouseText;
                                        }
                                        if (!string.IsNullOrEmpty(localCrumb.TouchText))
                                        {
                                            result[i].touchText = localCrumb.TouchText;
                                        }
                                        foundLocalCrumb = true;
                                        break;
                                    }
                                }

                                if (!foundLocalCrumb)
                                {
                                    Localizer.ReportMissing(stringsFilename, "TargetMode: " + defCrumb.TargetMode.ToString());
                                }
                            }
                        }
                        else
                        {
                            Localizer.ReportMissing(stringsFilename, "CAN'T LOAD CRUMBS!");
                        }
                    }
                    else
                    {
                        Localizer.ReportMissing(stringsFilename, "CAN'T FIND FILE!");
                    }
                }
                else
                {
                    Localizer.ReportMissing(localPath, "CAN'T FIND PATH FOR THIS LANGUAGE!");
                }
            }

            /*
            // Output crumb data for GraphViz
            foreach (Crumb crumb in result)
            {
                foreach (TutorialManager.GameMode curmode in crumb.CurModes)
                {
                    Debug.Print(curmode.ToString() + " -> " + crumb.targetMode.ToString() + " [ label = \" \" ];");
                }
            }
            */

            return result;
        }   // end of Init()

        #endregion

        #region Internal

        /// <summary>
        /// Loads the Crumb information.  Will first try the downloaded
        /// version.  If that fails, will then try the TitleSpace version.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="source">Where to look for the file.</param>
        /// <returns></returns>
        private static CrumbList Load(string filename, StorageSource source)
        {
            CrumbList data = null;
            Stream stream = null;

            // First try with StorageSoruce.All so we get the version downloaded
            // from the servers.  If that fails then get the TitleSpace version.
            try
            {
                stream = Storage4.OpenRead(filename, source);

                XmlSerializer serializer = new XmlSerializer(typeof(CrumbList));
                data = (CrumbList)serializer.Deserialize(stream);
            }
            catch (Exception e)
            {
                data = null;
                if (e != null)
                {
#if !NETFX_CORE
                    string message = e.Message;
                    if (e.InnerException != null)
                    {
                        message += e.InnerException.Message;
                    }
                    System.Windows.Forms.MessageBox.Show(
                        message,
                        "Error reading TutorialCrumbs.Xml",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error
                        );
#endif
                    }
            }
            finally
            {
                Storage4.Close(stream);
            }

            // If we don't have data.  Delete the server version of 
            // the file and try loading the TitleSpace version.
            if (data == null)
            {
                // Don't delete the server version since this might actually be someone 
                // trying to do a localization.
                //Storage4.Delete(filename);

                try
                {
                    stream = Storage4.OpenRead(filename, StorageSource.TitleSpace);

                    XmlSerializer serializer = new XmlSerializer(typeof(CrumbList));
                    data = (CrumbList)serializer.Deserialize(stream);
                }
                catch (Exception)
                {
                    data = null;
                }
                finally
                {
                    Storage4.Close(stream);
                }
            }
        
            return data;
        }   // end of Load()

        #endregion

    }   // end of class Crumb

}   // end of namespace Boku.Common.TutorialSystem
