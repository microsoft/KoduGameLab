using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Boku.Common.Xml
{
    public class XmlStaticActor : BokuShared.XmlData<XmlStaticActor>
    {
        [XmlAttribute("name")]
        public string NonLocalizedName;
        [XmlAttribute("specialType")]
        public string SpecialType;

        public string Classification;
        public string ClassificationRevealed;
        public string XmlFile;
        public string XmlFileRevealed;
        public string Group;
        public string ModelFile;
        public string ModelRevealedFile;
        public string MenuTextureFile;
    }

    public sealed class XmlActorsList : BokuShared.XmlData<XmlActorsList>
    {
        [XmlArrayItem("Actor")]
        public XmlStaticActor[] Actors = null;
    }
}
