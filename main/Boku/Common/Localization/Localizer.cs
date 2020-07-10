// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Xml;
using System.Diagnostics;
using System.IO;

#if NETFX_CORE
    using Windows.UI.Popups;
#endif

namespace Boku.Common.Localization
{
    public static class Localizer
    {
        public const string DefaultLanguage = LocalizationResourceManager.DefaultLanguage;
        public const string DefaultLanguageDir = LocalizationResourceManager.DefaultLanguageDir;
        private const string languageDir = LocalizationResourceManager.LanguageDir;
        public static bool ShouldReportMissing = false;

        private static TextWriter reportWriter;
        private static string reportPath;
        public const string ReportFile = "MissingLocalization.txt";

        /// <summary>
        /// The two-letter ISO 639-1 language code for the run-time culture.
        /// </summary>
        public static string LocalLanguage
        {
            get
            {
                // DebugLog.WriteLine("LocalLanguage.Get");
                // DebugLog.WriteLine("    localLanguage : " + (localLanguage != null ? localLanguage : "null"));
                if (localLanguage == null)
                {
                    localLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                    // DebugLog.WriteLine("    localLanguage : " + localLanguage);
                }

                // See if we want the settings to override.
                if (BokuSettings.Settings.Language != null && BokuSettings.Settings.Language.Length == 2)
                {
                    localLanguage = BokuSettings.Settings.Language;
                    // DebugLog.WriteLine("    localLanguage override : " + localLanguage);
                }

                // DebugLog.WriteLine("    localLanguage return : " + localLanguage);

                return localLanguage;
            }
            set
            {
                // Debug.WriteLine("Warning!! We're forcing the local language!");

                value = value.ToUpper();

                // DebugLog.WriteLine("LocalLanguage.Set");
                // DebugLog.WriteLine("    localLanguage : " + (value != null ? value : "null"));

                localLanguage = value;
            }
        }
        private static string localLanguage = null;

        /// <summary>
        /// The directory where local language-specific XML content is kept. If no
        /// directory exists for the current language, we return null.
        /// </summary>
        public static string LocalLanguageDir
        {
            get
            {
                // DebugLog.WriteLine("LocalLanguageDir.Get");
                // DebugLog.WriteLine("    languageDir : " + languageDir);
                // DebugLog.WriteLine("    LocalLanguage : " + LocalLanguage);

                var path = Path.Combine(languageDir, LocalLanguage);

                if (Storage4.DirExists(path, StorageSource.All))
                {
                    // DebugLog.WriteLine("    path : " + path);
                    return path;
                }
                else
                {
                    // DebugLog.WriteLine("    path : null");
                    return null;
                }
            }
        }

        /// <summary>
        /// Is the local language the default language?
        /// </summary>
        public static bool IsLocalDefault
        {
            get
            {
                return String.Equals(DefaultLanguage, LocalLanguage, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static Dictionary<string, List<string>> ReadToDictionary(string fileName, string testName) 
        {
            // DebugLog.WriteLine("ReadToDictionary1");

            Dictionary<string, List<string>> dict = null;

            dict = ReadToDictionary(fileName, 1, StorageSource.All, testName);

            if(dict == null)
            {
                // DebugLog.WriteLine("    failed read, trying titlespace");
                // If we're here the file read failed.  This seems to happen when the 
                // version downloaded from the server is bad.  So, delete the file
                // so it gets downloaded again on next startup and then read the file
                // from the TitleSpace.
                Storage4.Delete(fileName);
                dict = ReadToDictionary(fileName, 1, StorageSource.TitleSpace, testName);
            }

            return dict; 
        }

        public static Dictionary<string, List<string>> ReadToDictionary(string fileName, int startDepth, StorageSource source, string testName)
        {
            // DebugLog.WriteLine("ReadToDictionary2");
            // Setup our result dictionary
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            // If we can't find the given file, we'll return the empty dictionary
            if (!Storage4.FileExists(fileName, source))
            {
                // DebugLog.WriteLine("    failed read, can't find file : " + fileName + " " + source.ToString());
                Debug.Assert(false, "We're trying to read a localization file that doesn't exist!");
                Localizer.ReportMissing(Path.GetFileName(fileName), "CAN'T FIND FILE!");
                return result;
            }

#if NETFX_CORE
            XmlReader reader = null;
#else
            XmlTextReader reader = null;
#endif
            Stream stream = null;

            try
            {

                // Open our file
                // DebugLog.WriteLine("    open : " + fileName);
                stream = Storage4.OpenRead(fileName, StorageSource.All);

                // Create our xml reader
#if NETFX_CORE
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                settings.IgnoreWhitespace = true;
                settings.IgnoreComments = true;
                reader = XmlReader.Create(stream, settings);
#else
                // DebugLog.WriteLine("    create reader");
                reader = new XmlTextReader(stream);
#endif

                // DebugLog.WriteLine("    advance reader");
                // Advance the reader to the specified start depth
                while (reader.Depth < startDepth && reader.Read()) ;

                // Create a stack that will be used as a scratch var for
                // keeping track of the current node tree.
                var nodeTree = new Stack<string>();

                var attributeDepth = 0;     // Tracks the number of attributes we've read
                var isEmpty = false;        // Is the element our reader is in empty?
                bool firstElement = true;   // Is this the first element we've found?

                // DebugLog.WriteLine("    read XML");

                // Read the XML
                {
                LoopStart:
                    // Push to the tree
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (!reader.IsEmptyElement || reader.HasAttributes)
                            {
                                const char trimChar = '_';
                                nodeTree.Push(reader.LocalName.TrimStart(trimChar));
                                isEmpty = reader.IsEmptyElement;
                            }
                            break;
                        case XmlNodeType.Attribute:
                            nodeTree.Push(reader.LocalName);
                            break;
                    }

                    // Grab reader value
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Attribute:
                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                            // Grab the element list
                            var es = nodeTree.ToArray();
                            // Change LIFO to FIFO ordering
                            Array.Reverse(es);
                            // Concat with dots
                            var key = String.Join(".", es);
                            // Get or create a dictionary value
                            List<string> val;
                            if (!result.TryGetValue(key, out val))
                                result[key] = val = new List<string>();
                            // Add localization text to dictionary
                            val.Add(reader.Value);
                            break;
                    }

                    // Pop from the tree
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.EndElement:
                            nodeTree.Pop();
                            break;
                        case XmlNodeType.Attribute:
                            nodeTree.Pop();
                            if (reader.AttributeCount == attributeDepth && isEmpty)
                                nodeTree.Pop();
                            break;
                    }

                    if (reader.IsEmptyElement) { }

                    // If this node has attributes or if the reader still has
                    // content at the desired depth, keep reading
                    if (reader.MoveToNextAttribute())
                    {
                        attributeDepth++;
                        goto LoopStart;
                    }
                    else if (reader.Read() && reader.Depth >= startDepth)
                    {
                        // The way this code is set up we can't tell the difference between one file and another.
                        // So, we pass in testName which should be the name of the first element in the file.
                        // If this doesn't match, assume we've got a bad file and bail.
                        if (firstElement)
                        {
                            if (!string.IsNullOrWhiteSpace(testName) && reader.Name != testName)
                            {
                                //throw new Exception();
                            }
                            firstElement = false;
                        }

                        attributeDepth = 0;
                        goto LoopStart;
                    }
                }

            }
            catch
            {
                result = null;
            }

#if NETFX_CORE
            if(reader != null)
            {
                reader.Dispose();
            }
#else
            if (reader != null)
            {
                reader.Close();
            }
#endif
            if (stream != null)
            {
                stream.Close();
            }

            return result;
        }

        /// <summary>
        /// Report missing localization information
        /// </summary>
        public static void ReportMissing(string fileName, string message)
        {
            if (ShouldReportMissing)
            {
                try
                {
                    if (reportWriter == null)
                    {
                        LoadContent(true);
                    }

                    // If anything is null, just ignore.
                    if (reportWriter == null || fileName == null || message == null)
                    {
                        return;
                    }

                    reportWriter.WriteLine(fileName + " - Missing: " + message);
                }
                catch (Exception e)
                {
                    Debug.Assert(false, e.Message);
                }
            }
        }

        /// <summary>
        /// Report identical localization information
        /// </summary>
        public static void ReportIdentical(string fileName, string message)
        {
            if (ShouldReportMissing)
            {
                try
                {
                    if (reportWriter == null)
                    {
                        LoadContent(true);
                    }

                    Debug.WriteLine(fileName + " - Identical to " + DefaultLanguage + ": " + message);

                    // If anything is null, just ignore.
                    if (reportWriter == null || fileName == null || message == null)
                    {
                        return;
                    }
                    
                    reportWriter.WriteLine(fileName + " - Identical to " + DefaultLanguage + ": " + message);
                }
                catch (Exception e)
                {
                    Debug.Assert(false, e.Message);
                }
            }
        }

        public static void LoadContent(bool immediate)
        {
            if (ShouldReportMissing)
            {
                if (reportWriter == null)
                {
                    reportPath = ReportFile;
                    try
                    {
                        reportWriter = Storage4.OpenStreamWriter(reportPath);
                    }
                    catch (Exception e)
                    {
                        if (e != null)
                        {
                        }

#if NETFX_CORE
                        MessageDialog dialog = new MessageDialog("reportPath = " + reportPath + "\nLoadContent failure in Localizer.");
#else
                        System.Windows.Forms.MessageBox.Show(
                            "reportPath = " + reportPath,
                            "LoadContent failure in Localizer.",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Asterisk);
#endif
                    }
                }
            }
        }

        public static void UnloadContent()
        {
            if (reportWriter != null)
            {
                reportWriter.Flush();
#if NETFX_CORE
                reportWriter.Dispose();
#else
                reportWriter.Close();
#endif
                reportWriter = null;
            }

            reportPath = null;
        }
    }
}
