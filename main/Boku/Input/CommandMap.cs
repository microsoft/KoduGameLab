using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Common;

namespace Boku.Input
{
    /// <summary>
    /// This class has been severly gutted from it's original form.  Now basically 
    /// used as nothing more than a class to hold a string as a stack element.
    /// </summary>
    public class CommandMap
    {
        public static CommandMap Empty = new CommandMap( "empty" );

        [XmlAttribute]
        public string name;

        public CommandMap()
        {
        }
        public CommandMap(string name)
        {
            this.name = name;
        }
    }   // end of class CommandMap
}
