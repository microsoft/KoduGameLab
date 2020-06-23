using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Boku.Base;
using System.Xml.Serialization;
using System.IO;
using Boku.Common.Xml;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;

namespace Boku.Common
{
    /// <summary>
    /// Responsible for loading, unloading, reseting and giving access to all
    /// actors used within Boku. Actor information is stored in StaticActors
    /// who can than be used to initialize instances of GameActors.
    /// </summary>
    public static class ActorManager
    {
        private const string XmlActorsListFileName = @"Content\xml\Actors.xml";

        public readonly static IDictionary<string, StaticActor> Actors = new Dictionary<string, StaticActor>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads Actors.xml and initializes the list of static actors.
        /// </summary>
        /// <remarks>
        /// The actors' model and xml file isn't loaded until LoadModels is called.
        /// </remarks>
        public static void LoadActors()
        {
            if (Storage4.FileExists(XmlActorsListFileName, StorageSource.TitleSpace))
            {
                // Read our actor list from file
                var stream = Storage4.OpenRead(XmlActorsListFileName, StorageSource.TitleSpace);
                var serializer = new XmlSerializer(typeof(XmlActorsList));
                var xmlActors = serializer.Deserialize(stream) as XmlActorsList;
                Storage4.Close(stream);

                if (xmlActors != null)
                {
                    for (int i = 0; i < xmlActors.Actors.Length; i++)
                    {
                        var xmlStaticActor = xmlActors.Actors[i];
                        Actors.Add(xmlStaticActor.NonLocalizedName, new StaticActor(xmlStaticActor));
                    }
                }
            }
            else
            {
                Debug.Assert(false, "Missing actor file.");
            }
        }

        /// <summary>
        /// Calls BokuGame.Load on all actor models
        /// </summary>
        /// <remarks>
        /// This must be called after the call to CreateSurfaces
        /// because the model's xml files bind there surfaces
        /// when loaded.
        /// </remarks>
        public static void LoadModels()
        {
            var a = Actors.Values;
            for (int i = 0; i < a.Count; i++)
            {
                BokuGame.Load(a.ElementAt(i).Model);

				//make sure we load the reveal version of the model if applicable
                if (a.ElementAt(i).XmlGameActor.CanReveal)
                {
                    BokuGame.Load(a.ElementAt(i).ModelRevealed);
                }
            }
        }

        /// <summary>
        /// Calls BokuGame.Unload on all actor models
        /// </summary>
        public static void UnloadModels()
        {
            var a = Actors.Values;
            for (int i = 0; i < a.Count; i++)
            {
                BokuGame.Unload(a.ElementAt(i).Model);
            }
        }

        /// <summary>
        /// Calls BokuGame.DeviceResetIfLoaded on all actor models
        /// </summary>
        public static void ModelsResetIfLoaded()
        {
            var a = Actors.Values;
            for (int i = 0; i < a.Count; i++)
            {
                BokuGame.DeviceResetIfLoaded(a.ElementAt(i).Model);
            }
        }

        /// <summary>
        /// Returns all static actors with the given group name.
        /// </summary>
        /// <param name="group">Group name. Case insensitive.</param>
        public static IList<StaticActor> GetActorsInGroup(string group)
        {
            var result = new List<StaticActor>();

            var a = Actors.Values;
            for (int i = 0; i < a.Count; i++)
            {
                var e = a.ElementAt(i);
                if (e.Group.Equals(group, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(e);
                }
            }

            return result;
        }

        public static StaticActor GetActor(string nonLocalizedName)
        {
            StaticActor result = null;

            Actors.TryGetValue(nonLocalizedName, out result);
            
            return result;
        }
    }
}
