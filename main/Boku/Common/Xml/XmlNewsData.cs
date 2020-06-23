using System;
using System.Collections.Generic;
using System.Text;

namespace Boku.Common.Xml
{
    public class XmlNewsData : BokuShared.XmlData<XmlNewsData>
    {
        public string title;
        public string text;
        public string creator;
        public string iconFilename;
    }
}
