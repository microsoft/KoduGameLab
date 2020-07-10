// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;

using BokuShared;


namespace Boku.Common
{
    public partial class LocalLevelBrowser
    {
        private void ScrubTerrainFiles()
        {
            //  Build a list of all terrain files in user storage.
            //  For each world in local storage (all three bins)
            //      get the XmlWorldData file
            //      from the XmlWorldData file, get the terrain filename
            //      remove that filename from the list
            //
            //  Any terrain filenames still on the list should be deleted
            //  since they're no longer referenced by any files.

            string[] terrainFiles = Storage4.GetFiles(BokuGame.Settings.MediaPath + BokuGame.TerrainPath, @"*.raw", StorageSource.UserSpace);

            // If nothing to scrub, just return.
            if (terrainFiles == null)
            {
                return;
            }

            string[] undoFiles = Storage4.GetFiles(BokuGame.Settings.MediaPath + BokuGame.UnDoPath, @"*.Xml", StorageSource.UserSpace);
            string[] myWorldsFiles = Storage4.GetFiles(BokuGame.Settings.MediaPath + BokuGame.MyWorldsPath, @"*.Xml", StorageSource.UserSpace);
            string[] starterWorldsFiles = Storage4.GetFiles(BokuGame.Settings.MediaPath + BokuGame.BuiltInWorldsPath, @"*.Xml", StorageSource.TitleSpace);
            string[] downloadsFiles = Storage4.GetFiles(BokuGame.Settings.MediaPath + BokuGame.DownloadsPath, @"*.Xml", StorageSource.UserSpace);

            /// Undo/Resume files. We might have to fall back on these if the user deletes the
            /// world they are editing and then back back to it.
            if (undoFiles != null)
            {
                for (int i = 0; i < undoFiles.Length; ++i)
                {
                    string filename = undoFiles[i];
                    XmlWorldData xmlWorldData = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                    if (xmlWorldData == null)
                        continue;

                    if (xmlWorldData.xmlTerrainData2 != null)
                    {
                        string terrainName = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, xmlWorldData.xmlTerrainData2.virtualMapFile);
                        for (int j = 0; j < terrainFiles.Length; ++j)
                        {
                            if (terrainName == terrainFiles[j])
                            {
                                // Remove this file.
                                terrainFiles[j] = null;
                                break;
                            }
                        }
                    }
                }
            }

            // MyWorlds
            if (myWorldsFiles != null)
            {
                for (int i = 0; i < myWorldsFiles.Length; ++i)
                {
                    string filename = myWorldsFiles[i];
                    XmlWorldData xmlWorldData = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                    if (xmlWorldData == null)
                        continue;

                    if (xmlWorldData.xmlTerrainData2 != null)
                    {
                        string terrainName = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, xmlWorldData.xmlTerrainData2.virtualMapFile);
                        for (int j = 0; j < terrainFiles.Length; ++j)
                        {
                            if (terrainName == terrainFiles[j])
                            {
                                // Remove this file.
                                terrainFiles[j] = null;
                                break;
                            }
                        }
                    }
                }
            }

            // BuiltInWorlds
            if (starterWorldsFiles != null)
            {
                for (int i = 0; i < starterWorldsFiles.Length; ++i)
                {
                    try
                    {
                        string filename = starterWorldsFiles[i];
                        XmlWorldData xmlWorldData = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                        if (xmlWorldData == null)
                            continue;

                        if (xmlWorldData.xmlTerrainData2 != null)
                        {
                            string terrainName = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, xmlWorldData.xmlTerrainData2.virtualMapFile);
                            for (int j = 0; j < terrainFiles.Length; ++j)
                            {
                                if (terrainName == terrainFiles[j])
                                {
                                    // Remove this file.
                                    terrainFiles[j] = null;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            // Downloads
            if (downloadsFiles != null)
            {
                for (int i = 0; i < downloadsFiles.Length; ++i)
                {
                    string filename = downloadsFiles[i];
                    XmlWorldData xmlWorldData = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                    if (xmlWorldData == null)
                        continue;

                    if (xmlWorldData.xmlTerrainData2 != null)
                    {
                        string terrainName = Path.Combine(Storage4.UserLocation, BokuGame.Settings.MediaPath, xmlWorldData.xmlTerrainData2.virtualMapFile);
                        for (int j = 0; j < terrainFiles.Length; ++j)
                        {
                            if (terrainName == terrainFiles[j])
                            {
                                // Remove this file.
                                terrainFiles[j] = null;
                                break;
                            }
                        }
                    }
                }
            }

            int deleteCount = 0;
            // Now, anything that's left in the list should be fair game for deletion.
            for (int i = 0; i < terrainFiles.Length; i++)
            {
                if (terrainFiles[i] != null)
                {
                    if (Storage4.FileExists(terrainFiles[i], StorageSource.UserSpace))
                    {
                        if (Storage4.Delete(terrainFiles[i]))
                            deleteCount += 1;
                    }
                }
            }
            
            //System.Diagnostics.Debug.WriteLine(String.Format("Scrubbed {0} terrain files", deleteCount));
        }   // end of ScrubTerrainFiles()

        /// <summary>
        /// Deletes the given terrain file but only after verifying that no world is using it.
        /// It any world is found that is using the file then it is not deleted.
        /// 
        /// Note this still isn't perfect since when a level is deleted, the terrain file may still be left
        /// in the undo stack and hence not deleted.  No biggy.  It's better than deleting too much and it's 
        /// much quicker than the above ScrubTerrainFiles().
        /// </summary>
        /// <param name="terrainFile"></param>
        void DeleteTerrainFile(string terrainFile)
        {
            string[] undoFiles = Storage4.GetFiles(BokuGame.Settings.MediaPath + BokuGame.UnDoPath, @"*.Xml", StorageSource.UserSpace);
            string[] myWorldsFiles = Storage4.GetFiles(BokuGame.Settings.MediaPath + BokuGame.MyWorldsPath, @"*.Xml", StorageSource.UserSpace);
            string[] starterWorldsFiles = Storage4.GetFiles(BokuGame.Settings.MediaPath + BokuGame.BuiltInWorldsPath, @"*.Xml", StorageSource.TitleSpace);
            string[] downloadsFiles = Storage4.GetFiles(BokuGame.Settings.MediaPath + BokuGame.DownloadsPath, @"*.Xml", StorageSource.UserSpace);

            /// Undo/Resume files. We might have to fall back on these if the user deletes the
            /// world they are editing and then back back to it.
            if (undoFiles != null)
            {
                for (int i = 0; i < undoFiles.Length; ++i)
                {
                    string filename = undoFiles[i];
                    XmlWorldData xmlWorldData = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                    if (xmlWorldData == null || xmlWorldData.xmlTerrainData2 == null)
                        continue;

                    if (xmlWorldData.xmlTerrainData2.virtualMapFile == terrainFile)
                    {
                        // Found it, don't delete.
                        return;
                    }
                }
            }

            // MyWorlds
            if (myWorldsFiles != null)
            {
                for (int i = 0; i < myWorldsFiles.Length; ++i)
                {
                    string filename = myWorldsFiles[i];
                    XmlWorldData xmlWorldData = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                    if (xmlWorldData == null || xmlWorldData.xmlTerrainData2 == null)
                        continue;

                    if (xmlWorldData.xmlTerrainData2.virtualMapFile == terrainFile)
                    {
                        // Found it, don't delete.
                        return;
                    }
                }
            }

            // BuiltInWorlds
            if (starterWorldsFiles != null)
            {
                for (int i = 0; i < starterWorldsFiles.Length; ++i)
                {
                    try
                    {
                        string filename = starterWorldsFiles[i];
                        XmlWorldData xmlWorldData = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                    
                        if (xmlWorldData == null || xmlWorldData.xmlTerrainData2 == null)
                            continue;

                        if (xmlWorldData.xmlTerrainData2.virtualMapFile == terrainFile)
                        {
                            // Found it, don't delete.
                            return;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            // Downloads
            if (downloadsFiles != null)
            {
                for (int i = 0; i < downloadsFiles.Length; ++i)
                {
                    string filename = downloadsFiles[i];
                    XmlWorldData xmlWorldData = XmlWorldData.Load(filename, XnaStorageHelper.Instance);
                    if (xmlWorldData == null || xmlWorldData.xmlTerrainData2 == null)
                        continue;

                    if (xmlWorldData.xmlTerrainData2.virtualMapFile == terrainFile)
                    {
                        // Found it, don't delete.
                        return;
                    }
                }
            }

            // Nothing was found using this terrain file so we can delete it.
            try
            {
                terrainFile = Path.Combine(BokuGame.Settings.MediaPath, terrainFile);
                Storage4.Delete(terrainFile);
            }
            catch(Exception e)
            {
                if (e != null)
                {
                }
            }

        }   // end of DeleteTerrainFile()

    }   // end of class LocalLevelBrowser

}   // end of namespace Boku.Common
