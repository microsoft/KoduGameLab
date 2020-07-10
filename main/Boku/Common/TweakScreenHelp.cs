// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


// Uncomment this to see debug spew about the help helpText stack.
//#define DEBUG_SPEW

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
using Boku.Common.Localization;

namespace Boku.Common
{
    /// <summary>
    /// A static class which manages the help text for the tweak screens.
    /// </summary>
    public class TweakScreenHelp
    {
        public class HelpText
        {
            [XmlAttribute]
            public string id = null;
            public string desc = null;

            // c'tor
            public HelpText()
            {
            }
        }   // end of class HelpText

        // Dictionary of available help with id as the key and desc as the value.
        private static Dictionary<string, string> helpTextDict = null;

        #region Accessors

        #endregion

        // private c'tor
        private TweakScreenHelp()
        {
        }

        /// <summary>
        /// Loads the available help helpTexts as listed in the inputFile.
        /// </summary>
        /// <param name="mediaPath"></param>
        /// <param name="inputFile"></param>
        public static void Init()
        {
            // Init list.
            helpTextDict = new Dictionary<string, string>();

            // Read in helpText information.
            XmlHelpTextData helpTextData = new XmlHelpTextData();
            helpTextData.ReadFromXml(LocalizationResourceManager.TweakScreenHelpResource.Name);

            // Copy data into dictionary.
            foreach(HelpText ht in helpTextData.helpText)
            {
                helpTextDict.Add(ht.id, ht.desc);
            }

        }   // end of TweakScreenHelp Init()

        /// <summary>
        /// Returns the help text associated with the given id.
        /// Note that this is the localized version.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetHelp(string id)
        {
            Debug.Assert(helpTextDict != null);

            if (id == null)
                return null;

            string desc = null;
            if (!helpTextDict.TryGetValue(id, out desc))
            {
                desc = null;
            }

            if (desc != null && desc.Length == 0)
            {
                desc = null;
            }

            return desc;
        }   // end of GetHelp()

    }   // end of class TweakScreenHelp

    //
    //
    // Xml file reading.
    //
    //

    public class XmlHelpTextData
    {
        [XmlElement(Type = typeof(TweakScreenHelp.HelpText))]
        public List<TweakScreenHelp.HelpText> helpText = null;

        public XmlHelpTextData()
        {
            helpText = new List<TweakScreenHelp.HelpText>();
        }

        /// <summary>
        /// Returns true on success, false if failed.
        /// </summary>
        public bool ReadFromXml(string filename)
        {
            bool success = true;

            // Fix up the filename with the full path.
            var defaultFile = Path.Combine(Localizer.DefaultLanguageDir, filename);

            // Read the Xml file into local data.
            XmlHelpTextData data = Load(defaultFile);

            // Build a dictionary with the default info
            var dict = new Dictionary<string, TweakScreenHelp.HelpText>(data.helpText.Count);
            foreach (var helpText in data.helpText)
                dict[helpText.id] = helpText;

            // Is our run-time local language different from the default?
            if (!Localizer.IsLocalDefault)
            {
                var localPath = Localizer.LocalLanguageDir;

                // Do we have a directory for the local language?
                if (localPath != null)
                {
                    var localFile = Path.Combine(localPath, filename);

                    if (Storage4.FileExists(localFile, StorageSource.All))
                    {
                        var localData = Load(localFile);
                        var localDict = new Dictionary<string, TweakScreenHelp.HelpText>(localData.helpText.Count);
                        foreach (var helpText in localData.helpText)
                            localDict[helpText.id] = helpText;

                        // Replace as much of the default data as we can with localized data
                        var keys = dict.Keys.ToArray();
                        foreach (var key in keys)
                            if (localDict.ContainsKey(key))
                            {
                                if (Localizer.ShouldReportMissing && localDict[key].desc.Equals(dict[key].desc, StringComparison.OrdinalIgnoreCase))
                                {
                                    Localizer.ReportIdentical(filename, key);
                                }

                                dict[key] = localDict[key];
                            }
                            else
                                Localizer.ReportMissing(filename, key);

                        data.helpText = dict.Values.ToList();
                    }
                    else
                        Localizer.ReportMissing(filename, "CAN'T FIND FILE!");
                }
                else
                    Localizer.ReportMissing(localPath, "CAN'T FIND PATH FOR THIS LANGUAGE!");
            }

            if (data == null)
            {
                success = false;
            }
            else
            {
                this.helpText = data.helpText;
            }

            return success;
        }   // end of XmlHelpTextData ReadFromXml()


        private static XmlHelpTextData Load(string filename)
        {
            XmlHelpTextData data = null;
            Stream stream = null;

            // First try with StorageSoruce.All so we get the version downloaded
            // from the servers.  If that fails then get the TitleSpace version.
            try
            {
                stream = Storage4.OpenRead(filename, StorageSource.All);

                XmlSerializer serializer = new XmlSerializer(typeof(XmlHelpTextData));
                data = (XmlHelpTextData)serializer.Deserialize(stream);
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
                        "Error reading " + filename,
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
            if(data == null)
            {
                // Don't delete the server version since this might actually be someone 
                // trying to do a localization.
                //Storage4.Delete(filename);

                try
                {
                    stream = Storage4.OpenRead(filename, StorageSource.TitleSpace);

                    XmlSerializer serializer = new XmlSerializer(typeof(XmlHelpTextData));
                    data = (XmlHelpTextData)serializer.Deserialize(stream);
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
        }   // end of XmlHelpTextData Load()


    }   // end of class XmlHelpTextData


}   // end of namespace Boku.Common
