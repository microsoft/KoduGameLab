using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Diagnostics;

using Boku.Base;
using Boku.Common.Xml;

using BokuShared;

namespace Boku.Common
{
    public partial class LocalLevelBrowser
    {
        /// <summary>
        /// Delete a level from the local system.  Returns false if not yet initialized.
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="callback"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool StartDeletingLevel(
            Guid worldId,
            Genres bucket,
            BokuAsyncCallback callback,
            object param)
        {
            bool deleted = false;

            bucket &= Genres.SharableBins;

            // Verify exactly one bucket is specified
            Debug.Assert(bucket != 0);
            Debug.Assert((int)bucket == int.MinValue || MyMath.IsPowerOfTwo((int)bucket));

            string worldFilename = null;
            string stuffFilename = null;
            string thumbFilename = null;

            LevelMetadata record = null;

            string stuffPath = String.Empty;
            string worldPath = String.Empty;

            if (0 != (bucket & Genres.MyWorlds))
            {
                stuffPath = BokuGame.MyWorldsStuffPath;
                worldPath = BokuGame.MyWorldsPath;
            }
            else if (0 != (bucket & Genres.Downloads))
            {
                stuffPath = BokuGame.DownloadsStuffPath;
                worldPath = BokuGame.DownloadsPath;
            }


            lock (Synch)
            {
                for (int i = 0; i < allLevels.Count; ++i)
                {
                    record = allLevels[i];

                    if (record.WorldId == worldId && (record.Genres & bucket) == bucket)
                    {
                        worldFilename = Path.Combine(BokuGame.Settings.MediaPath, worldPath + worldId.ToString() + @".Xml");
                        stuffFilename = Path.Combine(BokuGame.Settings.MediaPath, stuffPath + worldId.ToString() + @".Xml");
                        thumbFilename = Path.Combine(BokuGame.Settings.MediaPath, worldPath + worldId.ToString());

                        // Need to get the terrain file before we delete the main file.  BUT the terrain should be
                        // deleted after, otherwise the usage test will find the main file and always thing that 
                        // the terrain file is in use.
                        string terrainFilename = null;
                        try
                        {
                            // Only delete terrain file if no longer referenced.
                            XmlWorldData xmlWorldData = XmlWorldData.Load(worldFilename, XnaStorageHelper.Instance);
                            terrainFilename = xmlWorldData.xmlTerrainData2.virtualMapFile;
                        }
                        catch { }

                        // Note : Delete() handles non-existent files just fine.
                        Storage4.Delete(worldFilename);
                        Storage4.Delete(stuffFilename);
                        Storage4.Delete(thumbFilename + @".dds");
                        Storage4.Delete(thumbFilename + @".jpg");
                        Storage4.Delete(thumbFilename + @".png");

                        // Only deletes terrain file if no other world is using it.  (including autosaves)
                        DeleteTerrainFile(terrainFilename);

                        LevelMetadata level = allLevels[i];
                        allLevels.RemoveAt(i);

                        LevelRemoved_Synched(level);

                        deleted = true;

                        break;
                    }
                }
            }

            AsyncResult result = new AsyncResult();

            result.Success = deleted;
            result.Param = param;
            result.Seconds = 0;

            if (callback != null)
                callback(result);

            return deleted;
        }

    }
}
