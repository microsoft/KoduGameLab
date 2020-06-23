using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using Boku.Base;
using Boku.Common.Xml;

using BokuShared;

namespace Boku.Common
{
    public static class XmlDataHelper
    {
        /// <summary>
        /// Take a world packet from the community server and write it to our downloads area
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public static bool WriteWorldPacketToDisk(BokuShared.Wire.WorldPacket packet)
        {
            packet.Data.WorldId = packet.Info.WorldId;
            return WriteWorldDataPacketToDisk(packet.Data, packet.Info.ThumbnailBytes, packet.Info.Modified);
        }

        public static bool WriteWorldDataPacketToDisk(BokuShared.Wire.WorldDataPacket packet, byte[] thumbnailBytes, DateTime timeStamp)
        {
            Stream file = null;

            try
            {
                // Check for presence of the essential world data
                if (packet == null)
                    return false;
                if (packet.WorldXmlBytes == null)
                    return false;
                if (packet.StuffXmlBytes == null)
                    return false;

                // Read in contents of world xml buffer
                Xml.XmlWorldData xmlWorldData = Xml.XmlWorldData.Load(packet.WorldXmlBytes);
                if (xmlWorldData == null)
                    return false;

                xmlWorldData.overrideLastWriteTime = timeStamp;

                xmlWorldData.id = packet.WorldId;
                xmlWorldData.stuffFilename = BokuGame.DownloadsStuffPath + xmlWorldData.Filename;

                // Non-essential file: Write thumbnail image to disk
                if (thumbnailBytes != null)
                {
                    string ext = Storage4.TextureExt(thumbnailBytes);
                    string thumbFilename = xmlWorldData.GetImageFilenameWithoutExtension() + "." + ext;
                    file = Storage4.OpenWrite(BokuGame.Settings.MediaPath + BokuGame.DownloadsPath + thumbFilename);
                    file.Write(thumbnailBytes, 0, thumbnailBytes.Length);
                    Storage4.Close(file);
                    file = null;
                }

                // Cubeworld virtual terrain map
                if (packet.VirtualMapBytes != null)
                {
                    file = Storage4.OpenWrite(BokuGame.Settings.MediaPath + xmlWorldData.xmlTerrainData2.virtualMapFile);
                    file.Write(packet.VirtualMapBytes, 0, packet.VirtualMapBytes.Length);
                    Storage4.Close(file);
                    file = null;
                }

                // Write stuff xml to disk
                file = Storage4.OpenWrite(BokuGame.Settings.MediaPath + xmlWorldData.stuffFilename);
                file.Write(packet.StuffXmlBytes, 0, packet.StuffXmlBytes.Length);
                Storage4.Close(file);
                file = null;

                // Clear virtual genre bits because they should not be stored.
                xmlWorldData.genres &= ~(int)Genres.Virtual;
                xmlWorldData.genres &= ~(int)Genres.Favorite;

                // Serialize xmlWorldData to disk
                string fullPath = BokuGame.Settings.MediaPath + BokuGame.DownloadsPath + xmlWorldData.Filename;
                xmlWorldData.Save(fullPath, XnaStorageHelper.Instance);

                Instrumentation.RecordEvent(Instrumentation.EventId.LevelDownloaded, xmlWorldData.name);

                return true;
            }
            catch
            {
                if (file != null)
                    Storage4.Close(file);

                return false;
            }
        }

        /// <summary>
        /// Package up a world for send to the community server
        /// </summary>
        /// <param name="worldFullPathAndName"></param>
        /// <returns></returns>
        public static BokuShared.Wire.WorldPacket ReadWorldPacketFromDisk(string worldFullPathAndName)
        {
            return ReadWorldPacketFromDisk(worldFullPathAndName, BokuGame.MyWorldsPath);
        }

        public static BokuShared.Wire.WorldPacket ReadWorldPacketFromDisk(string worldFullPathAndName, string bucket)
        {
            BokuShared.Wire.WorldPacket packet = null;
            Stream file = null;

            try
            {
                string localLevelPath = BokuGame.Settings.MediaPath + bucket;
                string worldFilename = Path.GetFileName(worldFullPathAndName);

                // Read contents of world xml to retrieve the names of the dependent
                // files we need to upload
                Xml.XmlWorldData xmlWorldData = XmlWorldData.Load(localLevelPath + worldFilename, XnaStorageHelper.Instance);
                if (xmlWorldData == null)
                    return null;

                // Clear virtual genre bits in case they got saved (server clears them too).
                xmlWorldData.genres &= ~(int)Genres.Virtual;

                packet = new BokuShared.Wire.WorldPacket();
                packet.Info.WorldId = packet.Data.WorldId = xmlWorldData.id;
                packet.Info.Name = xmlWorldData.name;
                packet.Info.Description = xmlWorldData.description;
                packet.Info.Creator = xmlWorldData.creator;
                packet.Info.IdHash = "";
                packet.Info.Genres = xmlWorldData.genres;

                string imageFileName = xmlWorldData.GetImageFilenameWithoutExtension();

                // VirtualMap
                file = Storage4.OpenRead(BokuGame.Settings.MediaPath + xmlWorldData.xmlTerrainData2.virtualMapFile, StorageSource.All);
                packet.Data.VirtualMapBytes = new byte[file.Length];
                file.Read(packet.Data.VirtualMapBytes, 0, (int)file.Length);
                Storage4.Close(file);

                // Stuff xml
                file = Storage4.OpenRead(BokuGame.Settings.MediaPath + xmlWorldData.stuffFilename, StorageSource.All);
                packet.Data.StuffXmlBytes = new byte[file.Length];
                file.Read(packet.Data.StuffXmlBytes, 0, (int)file.Length);
                Storage4.Close(file);

                // Optional: don't worry if we don't have a thumbnail image.
                try
                {
                    file = null;
                    file = Storage4.TextureFileOpenRead(localLevelPath + imageFileName);

                    if (file != null)
                    {
                        packet.Info.ThumbnailBytes = new byte[file.Length];
                        file.Read(packet.Info.ThumbnailBytes, 0, (int)file.Length);
                        Storage4.Close(file);
                    }
                }
                catch { }


                // Try To load Snapshot image.
                try
                {
                    file = null;
                    file = Storage4.TextureFileOpenRead(localLevelPath + imageFileName, Storage4.TextureFileType.jpg);

                    if (file != null)
                    {
                        packet.Info.ScreenshotBytes = new byte[file.Length];
                        file.Read(packet.Info.ScreenshotBytes, 0, (int)file.Length);
                        Storage4.Close(file);
                    }
                }
                catch { }


                // We've successfully read all required files. We may now upload them to the server.
                file = Storage4.OpenRead(localLevelPath + worldFilename, StorageSource.All);
                packet.Data.WorldXmlBytes = new byte[file.Length];
                file.Read(packet.Data.WorldXmlBytes, 0, (int)file.Length);
                Storage4.Close(file);

                Instrumentation.RecordEvent(Instrumentation.EventId.LevelUploaded, xmlWorldData.name);
            }
            catch
            {
                if (file != null)
                    Storage4.Close(file);
                packet = null;
            }

            return packet;
        }

        public static LevelMetadata LoadMetadataUnknownGenre(Guid worldId)
        {
            LevelMetadata result = null;
            if (CheckWorldExistsByGenre(worldId, Genres.MyWorlds))
            {
                result = LoadMetadataByGenre(worldId, Genres.MyWorlds);
                result.Genres |= Genres.MyWorlds;
            }
            else if (CheckWorldExistsByGenre(worldId, Genres.Downloads))
            {
                result = LoadMetadataByGenre(worldId, Genres.Downloads);
                result.Genres |= Genres.Downloads;
            }
            else if (CheckWorldExistsByGenre(worldId, Genres.BuiltInWorlds))
            {
                result = LoadMetadataByGenre(worldId, Genres.BuiltInWorlds);
                result.Genres |= Genres.BuiltInWorlds;
            }

            return result;
        }

        public static LevelMetadata LoadMetadataByGenre(Guid worldId, Genres genres)
        {
            string bucket = BokuGame.MyWorldsPath;

            if (genres != 0)
            {
                bucket = Utils.FolderNameFromFlags(genres);
            }

            string fullPath = BokuGame.Settings.MediaPath + bucket + worldId.ToString() + @".Xml";

            Xml.XmlWorldData xml = XmlWorldData.Load(fullPath, XnaStorageHelper.Instance);
            if (xml != null)
            {
                LevelMetadata data = new LevelMetadata();
                data.FromXml(xml);

                //minor hackery - seems previous versions of kodu will sometimes leave the genres set to 0
                //even though they should be updated for the bucket the level is in.  Then at load time, a run-time
                //genre is set.  This code will maintain that behavior for levels loaded through this helper
                if (bucket == BokuGame.DownloadsPath)
                {
                    //ensure downloads always have the downloads flag
                    data.Genres |= Genres.Downloads;
                }
                else if (bucket == BokuGame.BuiltInWorldsPath)
                {
                    //ensure built in worlds always have the built in flag
                    data.Genres |= Genres.BuiltInWorlds;
                }
                else if (bucket == BokuGame.MyWorldsPath)
                {
                    data.Genres |= Genres.MyWorlds;
                }

                return data;
            }

            return null;
        }

        public static bool CheckWorldExistsByGenre(Guid worldId, Genres genres)
        {
            string bucket = BokuGame.MyWorldsPath;

            if (genres != 0)
            {
                bucket = Utils.FolderNameFromFlags(genres);

                if (bucket == null)
                {
                    return false;
                }
            }

            string fullPath = BokuGame.Settings.MediaPath + bucket + worldId.ToString() + @".Xml";

            StorageSource sources = StorageSource.All;
            if ((genres & Genres.Downloads) != 0)
            {
                sources = StorageSource.UserSpace;
            }

            return Storage4.FileExists(fullPath, sources);
        }

        /// <summary>
        /// Update the level's metadata on disk.
        /// Does not change the level's timestamp.
        /// </summary>
        /// <param name="level"></param>
        public static void UpdateWorldMetadata(LevelMetadata level)
        {
            try
            {
                string bucket = Utils.FolderNameFromFlags(level.Genres);

                string fullPath = BokuGame.Settings.MediaPath + bucket + level.WorldId.ToString() + @".Xml";

                Xml.XmlWorldData xml = XmlWorldData.Load(fullPath, XnaStorageHelper.Instance);
                if (xml != null)
                {
                    level.ToXml(xml);

                    bool isDownload = (level.Genres & Genres.Downloads) != 0;

                    // Manage the stream ourselves so avoid level timestamp being changed.
                    Stream stream = Storage4.OpenWrite(fullPath);
                    xml.Save(stream, isDownload);
                    Storage4.Close(stream);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Update the level's xml on disk.
        /// Does not change the level's timestamp.
        /// </summary>
        /// <param name="level"></param>
        public static void UpdateWorldXml(XmlWorldData xml)
        {
            try
            {
                string bucket = Utils.FolderNameFromFlags((Genres)xml.genres);

                string fullPath = BokuGame.Settings.MediaPath + bucket + xml.id.ToString() + @".Xml";

                //make sure the world exists
                if (!Storage4.FileExists(fullPath, StorageSource.All))
                {
                    return;
                }

                if (xml != null)
                {
                    bool isDownload = (xml.genres & (int)Genres.Downloads) != 0;

                    // Manage the stream ourselves so avoid level timestamp being changed.
                    Stream stream = Storage4.OpenWrite(fullPath);
                    xml.Save(stream, isDownload);
                    Storage4.Close(stream);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }


    }
}
