using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Common.Localization;

namespace Boku.Programming
{
    /// <summary>
    /// Contains the database of examples for the help system deserialized from XML.
    /// Provides an API for fetching a set of examples similar to an existing reflex,
    /// sorted by relevance, and compatible with the current game actor (if any).
    /// </summary>
    public class Help
    {
        #region Members

        private static Help instance;

        public List<ExamplePage> programmingExamples;

        public XmlSerializableDictionary<string, ActorHelp> actorHelp;

        private ActorHelp emptyHelp = new ActorHelp();

        #endregion

        #region Accessors

        [XmlIgnore]
        public static List<ExamplePage> ProgrammingExamples
        {
            get { return instance.programmingExamples; }
        }

        #endregion

        #region Public

        /// <summary>
        /// Rank and sort the set of examples relevant to the provided actor, reflex,
        /// and selection.
        /// </summary>
        /// <param name="actor">Optional</param>
        /// <param name="reflex">Required</param>
        /// <param name="selected">Optional</param>
        public static void RankAndSortProgrammingExamples(
            GameActor actor,
            ReflexData reflex,
            ProgrammingElement selected)
        {
            Debug.Assert(reflex != null, "The reflex argument must not be null.");
            instance.InternalRankAndSortProgrammingExamples(actor, reflex, selected);
        }

        /// <summary>
        /// Given the name of a bot type, return the set of associated sample programs.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static ActorHelp GetActorHelp(string typeName)
        {
            if (instance.actorHelp.ContainsKey(typeName))
            {
                return instance.actorHelp[typeName];
            }

            return instance.emptyHelp;
        }

        #endregion

        #region Internal

        private void InternalRankAndSortProgrammingExamples(
            GameActor actor,
            ReflexData reflex,
            ProgrammingElement selected)
        {
            RankSettings settings = new RankSettings();
            settings.sensorRank = 2;
            settings.selectorRank = 2;
            settings.filterRank = 2;
            settings.actuatorRank = 2;
            settings.modifierRank = 2;
            settings.otherFilterRank = 1;
            settings.otherModifierRank = 1;

            if (selected is Filter)
                settings.filterRank = 10;
            else if (selected is Actuator)
                settings.actuatorRank = 10;
            else if (selected is Modifier)
                settings.modifierRank = 10;
            else if (selected is Selector)
                settings.selectorRank = 10;
            else if (selected is Sensor)
                settings.sensorRank = 10;

            RankAndSortProgrammingExamples(
                settings,
                actor,
                selected,
                reflex
            );

        }

        private void RankAndSortProgrammingExamples(
            RankSettings settings,
            GameActor actor,
            ProgrammingElement selected,
            ReflexData reflex)
        {
            for (int i = 0; i < programmingExamples.Count; ++i)
            {
                ExamplePage example = programmingExamples[i];
                RankProgrammingExample(
                    example,
                    settings,
                    actor,
                    selected,
                    reflex);
            }

            programmingExamples.Sort(CompareByRank);
        }

        private void RankProgrammingExample(
            ExamplePage example,
            RankSettings settings,
            GameActor actor,
            ProgrammingElement selected,
            ReflexData reflex)
        {
            if (!example.ActorCompatible(actor))
            {
                example.rank = -1;
                return;
            }

            string selectedUpid;

            if (selected != null)
                selectedUpid = selected.upid;
            else
                selectedUpid = String.Empty;


            example.rank = 0;

            for (int iReflex = 0; iReflex < example.reflexes.Length; ++iReflex)
            {
                ReflexData exampleReflex = example.reflexes[iReflex];

                // Rank sensor
                if (selected == exampleReflex.Sensor)
                {
                    example.rank += RankElements(exampleReflex.Sensor, selected, settings.sensorRank);
                }
                
                if (exampleReflex.Sensor != null && reflex.Sensor != null)
                {
                    example.rank += RankElements(exampleReflex.Sensor, reflex.Sensor, settings.sensorRank);
                }

                // Rank selector
                if (selected == exampleReflex.Selector)
                {
                    example.rank += RankElements(exampleReflex.Selector, selected, settings.selectorRank);
                }

                if (exampleReflex.Selector != null && reflex.Selector != null)
                {
                    example.rank += RankElements(exampleReflex.Selector, reflex.Selector, settings.selectorRank);
                }

                // Rank actuator
                if (selected == exampleReflex.Actuator)
                {
                    example.rank += RankElements(exampleReflex.Actuator, selected, settings.actuatorRank);
                }

                if (exampleReflex.Actuator != null && reflex.Actuator != null)
                {
                    example.rank += RankElements(exampleReflex.Actuator, reflex.Actuator, settings.actuatorRank);
                }

                // Rank filters
                for (int i = 0; i < exampleReflex.Filters.Count; ++i)
                {
                    if (selected == exampleReflex.Filters[i])
                    {
                        example.rank += RankElements(exampleReflex.Filters[i], selected, settings.filterRank);
                    }

                    for (int j = 0; j < reflex.Filters.Count; ++j)
                    {
                        example.rank += RankElements(exampleReflex.Filters[i], reflex.Filters[j], settings.filterRank);
                    }
                }

                // Rank modifiers
                for (int i = 0; i < exampleReflex.Modifiers.Count; ++i)
                {
                    if (selected == exampleReflex.Modifiers[i])
                    {
                        example.rank += RankElements(exampleReflex.Modifiers[i], selected, settings.modifierRank);
                    }

                    for (int j = 0; j < reflex.Modifiers.Count; ++j)
                    {
                        example.rank += RankElements(exampleReflex.Modifiers[i], reflex.Modifiers[j], settings.modifierRank);
                    }
                }
            }
        }

        private float RankElements(ProgrammingElement a, ProgrammingElement b, float scorage)
        {
            float rank = 0.0f;

            if (a != null && b != null)
            {
                if (a.upid == b.upid)
                    rank += scorage;

                if (a.helpGroups != null && b.helpGroups != null)
                {
                    for (int i = 0; i < a.helpGroups.Length; ++i)
                    {
                        for (int j = 0; j < b.helpGroups.Length; ++j)
                        {
                            if (a.helpGroups[i] == b.helpGroups[j])
                            {
                                rank += scorage / 2;
                            }
                        }
                    }
                }
            }

            return rank;
        }

        private int CompareByRank(ExamplePage lhs, ExamplePage rhs)
        {
            if (lhs.rank < rhs.rank)
                return 1;
            if (lhs.rank > rhs.rank)
                return -1;
            return 0;
        }

        public static void LoadContent(bool immediate)
        {
            if (instance != null)
                return;

            try
            {
                var defaultFile = Path.Combine(Localizer.DefaultLanguageDir, LocalizationResourceManager.HelpResource.Name);

                // Load for the default language
                instance = Load(defaultFile);

                // Is our run-time local language different from the default?
                if (!Localizer.IsLocalDefault)
                {
                    var localPath = Localizer.LocalLanguageDir;

                    // Do we have a directory for the local language?
                    if (localPath != null)
                    {
                        var localFile = Path.Combine(localPath, LocalizationResourceManager.HelpResource.Name);

                        if (Storage4.FileExists(localFile, StorageSource.All))
                        {
                            var localData = Load(localFile);

                            // Override the defaults with localized text
                            var keys = instance.actorHelp.Keys.ToArray();
                            foreach (var key in keys)
                                if (localData.actorHelp.ContainsKey(key))
                                {
                                    if (Localizer.ShouldReportMissing && !localData.actorHelp[key].description.Equals(instance.actorHelp[key].description, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Localizer.ReportIdentical(LocalizationResourceManager.HelpResource.Name, key);
                                    }

                                    instance.actorHelp[key] = localData.actorHelp[key];
                                }
                                else
                                    Localizer.ReportMissing(LocalizationResourceManager.HelpResource.Name, key);

                            // For the programming examples, we'll just override everything
                            //ToDo (DZ): Is this the behavior we want? Comparing examples
                            // is harder than most localization info because we don't have
                            // upids. This comparision is possible but it would end up
                            // being more fragile and it may not be worth the trouble.
                            // An ideal solution would be to come up with a guid for each
                            // example.
                            instance.programmingExamples = localData.programmingExamples;
                        }
                        else
                            Localizer.ReportMissing(LocalizationResourceManager.HelpResource.Name, "CAN'T FIND FILE!");
                    }
                    else
                        Localizer.ReportMissing(localPath, "CAN'T FIND PATH FOR THIS LANGUAGE!");
                }
            }
            catch
            {
                instance = new Help();
            }

            if (instance.programmingExamples == null)
            {
                instance.programmingExamples = new List<ExamplePage>();
            }

            if (instance.actorHelp == null)
            {
                instance.actorHelp = new XmlSerializableDictionary<string, ActorHelp>();
            }

#if DEBUG
            //if (instance.programmingExamples.Count == 0)
                //instance.LoadWithSampleData();
#endif

            // Make sure we only have valid characters in our descriptions.
            List<ExamplePage> set = instance.programmingExamples;
            for (int i = 0; i < set.Count; i++)
            {
                set[i].description = TextHelper.FilterInvalidCharacters(set[i].description);
            }
        }

        /// <summary>
        /// Loads the Help information.  Will first try the downloaded
        /// version.  If that fails, will then try the TitleSpace version.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static Help Load(string filename)
        {
            Help data = null;
            Stream stream = null;

            // First try with StorageSoruce.All so we get the version downloaded
            // from the servers.  If that fails then get the TitleSpace version.
            try
            {
                stream = Storage4.OpenRead(filename, StorageSource.All);

                XmlSerializer serializer = new XmlSerializer(typeof(Help));
                data = (Help)serializer.Deserialize(stream);
            }
            catch (Exception e)
            {
                data = null;
                if (e != null)
                {
#if !NETFX_CORE
                    string message = e.Message;
                    if (e.InnerException != null)
                    {
                        message += e.InnerException.Message;
                    }
                    System.Windows.Forms.MessageBox.Show(
                        message,
                        "Error reading " + filename,
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error
                        );
#endif
                }

            }
            finally
            {
                Storage4.Close(stream);
            }

            // If we don't have data.  Delete the server version of 
            // the file and try loading the TitleSpace version.
            if (data == null)
            {
                // Don't delete the server version since this might actually be someone 
                // trying to do a localization.
                //Storage4.Delete(filename);

                try
                {
                    stream = Storage4.OpenRead(filename, StorageSource.TitleSpace);

                    XmlSerializer serializer = new XmlSerializer(typeof(Help));
                    data = (Help)serializer.Deserialize(stream);
                }
                catch (Exception)
                {
                    data = null;
                }
                finally
                {
                    Storage4.Close(stream);
                }
            }

            return data;
        }   // end of Load()

        public static void InitDeviceResources(GraphicsDevice device)
        {
        }

        public static void UnloadContent()
        {
            if (instance == null)
                return;
#if DEBUG
            try
            {
                Stream stream = Storage4.OpenWrite(LocalizationResourceManager.HelpResource.Name);
                XmlSerializer serializer = new XmlSerializer(typeof(Help));
                serializer.Serialize(stream, instance);
                Storage4.Close(stream);
            }
            catch { }
#endif
        }

        /// <summary>
        /// Recreate render targets.
        /// </summary>
        public static void DeviceReset(GraphicsDevice device)
        {
        }


#if DEBUG
        private void LoadWithSampleData()
        {
            ExamplePage page = new ExamplePage(
                "When I see fruit, move toward it.\nWhen I bump fruit, eat it.",
                new ReflexDesc[] {
                    new ReflexDesc(
                        new string[] {
                            "sensor.eyes",
                            "filter.fruit",
                            "actuator.movement",
                            "selector.towardclosest",
                        }),
                    new ReflexDesc(
                        new string[] {
                            "sensor.bumpers",
                            "filter.fruit",
                            "actuator.eat",
                        }),
                });


            ActorHelp example = new ActorHelp();
            example.description = "This is the actor description.";
            ExampleProgram program = new ExampleProgram();
            program.pages = new List<ExamplePage>();
            program.pages.Add(page);
            example.programs = new List<ExampleProgram>();
            example.programs.Add(program);
            instance.actorHelp.Add("BokuBot", example);
        }
#endif

        #endregion
    }   // end of class Help

    /// <summary>
    /// Represents a page in a sample program, containing a set of reflexes.
    /// </summary>
    public class ExamplePage
    {
        #region Private

        private ReflexData[] _reflexes;

        #endregion

        #region Public

        [XmlElement]
        public string description;  // Description of what the page does.  This is not used 
                                    // in ActorHelp but is used in ProgrammingHelp.

        [XmlArray("reflexes")]
        [XmlArrayItem("reflex")]
        public List<ReflexDesc> _reflexDescList;

        [XmlIgnore]
        public ReflexData[] reflexes
        {
            get
            {
                if (_reflexes == null)
                {
                    _reflexes = new ReflexData[_reflexDescList.Count];
                    for (int i = 0; i < _reflexDescList.Count; ++i)
                        _reflexes[i] = new ReflexData(_reflexDescList[i]);
                }
                return _reflexes;
            }
        }

        [XmlIgnore]
        public float rank;


        public ExamplePage()
        {
        }

        public ExamplePage(string description, ReflexDesc[] reflexes)
        {
            this.description = description;
            this._reflexDescList = new List<ReflexDesc>(reflexes);
        }

        public bool ActorCompatible(GameActor actor)
        {
            for (int i = 0; i < reflexes.Length; ++i)
            {
                if (!reflexes[i].ActorCompatible(actor))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Build an ExamplePage from the specified task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static ExamplePage FromTask(Task task)
        {
            ExamplePage page = new ExamplePage();
            page._reflexDescList = new List<ReflexDesc>();

            foreach (Reflex reflex in task.reflexes)
            {
                ReflexDesc desc = reflex.Data.ToDesc();
                page._reflexDescList.Add(desc);
            }

            return page;
        }


        #endregion
    }

    /// <summary>
    /// Represents a sample program, containing a set of sample pages.
    /// </summary>
    public class ExampleProgram
    {
        #region Public

        [XmlElement]
        public string description;  // Description of what the program does.

        [XmlElement]
        public string tileUpid;     // Upid of tile to display with this program.

        public List<ExamplePage> pages;

        public bool ActorCompatible(GameActor actor)
        {
            for (int i = 0; i < pages.Count; ++i)
            {
                ExamplePage page = pages[i];
                for (int j = 0; j < page.reflexes.Length; ++i)
                {
                    if (!page.reflexes[j].ActorCompatible(actor))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Build an example program from the specified brain contents.
        /// </summary>
        /// <param name="brain"></param>
        /// <returns></returns>
        public static ExampleProgram FromBrain(Brain brain)
        {
            ExampleProgram program = new ExampleProgram();
            program.pages = new List<ExamplePage>();

            foreach (Task task in brain.tasks)
            {
                ExamplePage page = ExamplePage.FromTask(task);
                program.pages.Add(page);
            }

            return program;
        }

        #endregion
    }

    public class ActorHelp
    {
        public string description;  // Description of the actor and its attributes.

        [XmlElement]
        public string upid;         // Upid of bot's tile to display with help.

        public List<ExampleProgram> programs;
    }

    #region Internal

    struct RankSettings
    {
        #region Internal

        public float sensorRank;
        public float selectorRank;
        public float filterRank;
        public float actuatorRank;
        public float modifierRank;
        public float otherFilterRank;
        public float otherModifierRank;

        #endregion
    }

    #endregion


}   // end of namespace Boku.Programming
