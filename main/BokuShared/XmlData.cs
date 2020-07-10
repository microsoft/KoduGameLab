// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

#if NETFX_CORE
    using System.Threading.Tasks;
#endif

namespace BokuShared
{
    public abstract partial class XmlData<XmlDataClass> where XmlDataClass : class
    {
        public XmlData()
        {
#if !NETFX_CORE
            Debug.Assert(
                typeof(XmlDataClass).IsSubclassOf(typeof(XmlData<XmlDataClass>)),
                String.Format("{0} must derive from XmlData<{0}>", typeof(XmlDataClass).ToString())
            );
#endif
        }

        /// <summary>
        /// Override this function to perform any post-load data fixup and validation in your class.
        /// </summary>
        /// <returns>false to indicate load failed and null should be returned from the Load method.</returns>
        protected virtual bool OnLoad()
        {
            return true;
        }

        public virtual void OnLoadFromFile(string filename)
        {
            // Override this function to perform any post-load data fixup in your class
            // that requires the name of the file the data was loaded from. OnLoad
            // will be called before this callback.
        }

        public virtual void OnBeforeSaveToFile()
        {
            // Only called before saving to file, if saving directly to stream this
            // is intentionally not called.
        }

        public virtual void OnBeforeSave()
        {
            // Called before save to stream. Also called before save to file, since
            // the save-to-file codepath calls the save-to-stream codepath.
        }

        public void Save(Stream stream, bool isDownload)
        {
            if (!isDownload)
            {
                OnBeforeSave();
            }
            XmlSerializer serializer = new XmlSerializer(typeof(XmlDataClass));
            serializer.Serialize(stream, this);
        }

        public void Save(BinaryWriter writer)
        {
            byte[] buffer = SaveToArray();
            writer.Write(buffer.Length);
            writer.Write(buffer);
        }

        public void Save(string filename, StorageHelper storage)
        {
            try
            {
                bool isDownload = filename.Contains("Downloads");
                OnBeforeSaveToFile();
                Stream stream = storage.OpenWrite(filename);
                Save(stream, isDownload);
                storage.Close(stream);
            }
            catch
            { }
        }

        public byte[] SaveToArray()
        {
            MemoryStream stream = new MemoryStream();
            Save(stream, false);
            stream.Position = 0;
            return stream.ToArray();
        }

        public static XmlDataClass Load(string filename, StorageHelper storage)
        {
            Stream stream = storage.OpenRead(filename);

            if (stream == null)
            {
                return null;
            }

            XmlDataClass data = null;
            try
            {
                data = XmlData<XmlDataClass>.Load(stream);
                if (data != null)
                    (data as XmlData<XmlDataClass>).OnLoadFromFile(filename);
            }
            catch { }
            finally
            {
                storage.Close(stream);
            }

            return data;
        }

        public static XmlDataClass Load(string filename, StorageHelper storage, int storageFlags)
        {
            Stream stream = storage.OpenRead(filename, storageFlags);
            XmlDataClass data = XmlData<XmlDataClass>.Load(stream);
            storage.Close(stream);
            (data as XmlData<XmlDataClass>).OnLoadFromFile(filename);
            return data;
        }

        public static XmlDataClass Load(byte[] buffer)
        {
            Stream stream = new MemoryStream(buffer);
            XmlDataClass data = Load(stream);
#if NETFX_CORE
            stream.Flush();
            stream.Dispose();
#else
            stream.Close();
#endif
            return data;
        }

        public static XmlDataClass Load(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            byte[] buffer = reader.ReadBytes(count);
            return Load(buffer);
        }

        public static XmlDataClass Load(Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlDataClass));
            XmlDataClass data = (XmlDataClass)serializer.Deserialize(stream);
            if (!(data as XmlData<XmlDataClass>).OnLoad())
                data = null;
            return data;
        }
    }
}
