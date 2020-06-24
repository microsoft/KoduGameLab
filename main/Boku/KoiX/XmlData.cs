using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace KoiX
{
    /// <summary>
    /// Helper base class for objects that need serialization.
    /// </summary>
    /// <typeparam name="XmlDataClass"></typeparam>
    public abstract class XmlData<XmlDataClass>
    {
        public XmlData()
        {
            Debug.Assert(
                typeof(XmlDataClass).IsSubclassOf(typeof(XmlData<XmlDataClass>)),
                String.Format("{0} must derive from XmlData<{0}>", typeof(XmlDataClass).ToString())
            );
        }

        protected virtual void OnLoad()
        {
            // Override this function to perform any post-load data fixup in your class.
        }

        public virtual void OnLoadFromFile(string filename)
        {
            // Override this function to perform any post-load data fixup in your class
            // that requires the name of the file the data was loaded from. OnLoad
            // will be called before this callback.
        }

        public void Save(Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlDataClass));
            serializer.Serialize(stream, this);
        }

        public void Save(string filename)
        {
            using (Stream stream = File.Open(filename, FileMode.Create, FileAccess.Write))
            {
                Save(stream);
                stream.Close();
            }
        }

        public static XmlDataClass Load(string filename)
        {
            XmlDataClass data = default(XmlDataClass);
            try
            {
                using (Stream stream = File.OpenRead(filename))
                {
                    data = XmlData<XmlDataClass>.Load(stream);
                    stream.Close();
                }
                (data as XmlData<XmlDataClass>).OnLoadFromFile(filename);
            }
            catch (Exception e)
            {
                if (e != null)
                {
                }
            }

            return data;
        }

        public static XmlDataClass Load(byte[] buffer)
        {
            XmlDataClass data;
            using (Stream stream = new MemoryStream(buffer))
            {
                data = Load(stream);
                stream.Close();
            }
            return data;
        }

        public static XmlDataClass Load(Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlDataClass));
            XmlDataClass data = (XmlDataClass)serializer.Deserialize(stream);
            (data as XmlData<XmlDataClass>).OnLoad();
            return data;
        }
    }
}
