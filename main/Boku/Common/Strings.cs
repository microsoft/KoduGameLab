
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;

using Boku.Base;
using Boku.Common.Localization;

namespace Boku.Common
{
    /// <summary>
    /// A class to contain the string constants used in Boku 
    /// in the hope that this will ease localization issues.
    /// </summary>
    public static class Strings
    {
        public const string AliasString = "alias";

        private static IDictionary<string, List<string>> strings;
        private static bool hasLoaded = false;

        public static void Load()
        {
            var fileName = Path.Combine(Localizer.DefaultLanguageDir, LocalizationResourceManager.StringsResource.Name);

            strings = Localizer.ReadToDictionary(fileName, "foo");

            // We need to validate that we actually read in the corect file but the ReadToDictionary()
            // method does nothing for us.

            // Is our run-time local language different from the default?
            if (!Localizer.IsLocalDefault)
            {
                var localPath = Localizer.LocalLanguageDir;

                // Do we have a directory for the local language?
                if (localPath != null)
                {
                    var localDict = Localizer.ReadToDictionary(Path.Combine(localPath, LocalizationResourceManager.StringsResource.Name), "foo");

                    var keys = strings.Keys.ToArray();
                    foreach (var k in keys)
                    {
                        if (localDict.ContainsKey(k))
                        {
                            if (Localizer.ShouldReportMissing && !localDict[k].Any((s) => !strings[k].Contains(s)))
                            {
                                Localizer.ReportIdentical(LocalizationResourceManager.StringsResource.Name, k);
                            }

                            if (k.EndsWith(AliasString, StringComparison.OrdinalIgnoreCase))
                            {
                                // For aliases, we just want to append the aliases instead
                                // of overriding them
                                foreach (var alias in localDict[k])
                                {
                                    if (!strings[k].Contains(alias))
                                    {
                                        strings[k].Add(alias);
                                    }
                                }
                            }
                            else
                            {
                                strings[k] = localDict[k];
                            }
                        }
                        else if (!k.EndsWith(AliasString, StringComparison.OrdinalIgnoreCase))
                        {
                            Localizer.ReportMissing(LocalizationResourceManager.StringsResource.Name, k);
                        }
                    }
                }
                else
                {
                    Localizer.ReportMissing(localPath, "CAN'T FIND PATH FOR THIS LANGUAGE!");
                }
            }

            // Since we're possible modifying some entries, we have to create
            // a list to iterate through rather than just using foreach.
            List<KeyValuePair<string, List<string>>> entryList = strings.ToList();
            for(int i=0; i<entryList.Count; i++)
            {
                KeyValuePair<string, List<string>> entry = entryList[i];

                // Button Alias Strings may legitimately have multiple
                // entries.  Just skip them.
                if (entry.Key.StartsWith("buttonAliasStrings."))
                {
                    continue;
                }

                // For some reason, some entries come with the value
                // string split up into several sub-strings.  If this
                // hapeens, glue them back together.
                if (entry.Value.Count > 1)
                {
                    for (int j = 1; j < entry.Value.Count; j++)
                    {
                        entry.Value[0] += entry.Value[j];
                    }
                }
            }

            hasLoaded = true;
        }   // end of Strings Load()

        public static string Localize(string internalKey)
        {
            if (!hasLoaded)
                Load();

            List<string> result;
            if (strings.TryGetValue(internalKey, out result))
            {
                /*
                if (Localizer.IsLocalRtoL)
                {
                    result[0] = TextHelper.ConvertToRtoL(result[0]);
                }
                */
                return result[0];
            }
            else
            {
                Debug.Assert(false, "Missing string.");
                return "";
            }
        }
        public static IEnumerable<string> LocalizeAll(string internalKey)
        {
            if (!hasLoaded)
                Load();

            List<string> result;
            if (strings.TryGetValue(internalKey, out result))
            {
                /*
                if (Localizer.IsLocalRtoL)
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        result[i] = TextHelper.ConvertToRtoL(result[i]);
                    }
                }
                */
                return result;
            }
            else
            {
                Debug.Assert(false, "Missing string.");
                return new List<string>();
            }
        }

        #region Helpers
        public static string GetActorName(string nonLocalizedName)
        {
            if (nonLocalizedName == "BalloonBot")
                return Localize("actorNames.balloon");
            if (nonLocalizedName == "BigYucca1")
                return Localize("actorNames.bigYucca1");
            //if (nonLocalizedName == "BigYucca2")
            //    return Localize("actorNames.bigYucca2");
            if (nonLocalizedName == "BokuBot")
                return Localize("actorNames.boku");
            if (nonLocalizedName == "Bullet")
                return Localize("actorNames.bullet");
            if (nonLocalizedName == "Clam")
                return Localize("actorNames.clam");
            if (nonLocalizedName == "Cloud")
                return Localize("actorNames.cloud");
            if (nonLocalizedName == "Castle")
                return Localize("actorNames.castle");
            if (nonLocalizedName == "Coin")
                return Localize("actorNames.coin");
            if (nonLocalizedName == "SoccerBall")
                return Localize("actorNames.soccerBall");
            if (nonLocalizedName == "Daisy")
                return Localize("actorNames.daisy");
            if (nonLocalizedName == "Drum")
                return Localize("actorNames.drum");
            if (nonLocalizedName == "Factory")
                return Localize("actorNames.factory");
            if (nonLocalizedName == "Fan")
                return Localize("actorNames.fan");
            if (nonLocalizedName == "FastBot")
                return Localize("actorNames.cycle");
            if (nonLocalizedName == "FloatBot")
                return Localize("actorNames.floatBot");
            if (nonLocalizedName == "Flower")
                return Localize("actorNames.flower");
            if (nonLocalizedName == "FlyFish")
                return Localize("actorNames.flyFish");
            if (nonLocalizedName == "Fruit")
                return Localize("actorNames.fruit");
            if (nonLocalizedName == "Heart")
                return Localize("actorNames.heart");
            if (nonLocalizedName == "Hut")
                return Localize("actorNames.hut");
            if (nonLocalizedName == "IceBerg")
                return Localize("actorNames.iceBerg");
            if (nonLocalizedName == "InkJet")
                return Localize("actorNames.inkjet");
            if (nonLocalizedName == "Jet")
                return Localize("actorNames.jet");
            if (nonLocalizedName == "Light")
                return Localize("actorNames.light");
            if (nonLocalizedName == "Lilypad")
                return Localize("actorNames.lilypad");
            if (nonLocalizedName == "LilypadSingle")
                return Localize("actorNames.lilypad");
            if (nonLocalizedName == "Mine")
                return Localize("actorNames.mine");
            if (nonLocalizedName == "Octopus")
                return Localize("actorNames.octopus");
            if (nonLocalizedName == "Popsy")
                return Localize("actorNames.popsy");
            if (nonLocalizedName == "Puck")
                return Localize("actorNames.puck");
            if (nonLocalizedName == "PushPad")
                return Localize("actorNames.pushPad");
            if (nonLocalizedName == "Rock")
                return Localize("actorNames.rock");
            if (nonLocalizedName == "RockLowValue")
                return Localize("actorNames.rockLowValue");
            if (nonLocalizedName == "RockHighValue")
                return Localize("actorNames.rockHighValue");
            if (nonLocalizedName == "RockLowValueUnknown")
                return Localize("actorNames.rockLowValueUnknown");
            if (nonLocalizedName == "RockHighValueUnknown")
                return Localize("actorNames.rockHighValueUnknown");
            if (nonLocalizedName == "RockUnknown")
                return Localize("actorNames.rockUnknown");
            if (nonLocalizedName == "Saucer")
                return Localize("actorNames.saucer");
            if (nonLocalizedName == "Seagrass")
                return Localize("actorNames.seagrass");
            if (nonLocalizedName == "SeagrassSingle")
                return Localize("actorNames.seagrass");
            if (nonLocalizedName == "Sputnik")
                return Localize("actorNames.sputnik");
            if (nonLocalizedName == "Star")
                return Localize("actorNames.star");
            if (nonLocalizedName == "Starfish")
                return Localize("actorNames.starfish");
            if (nonLocalizedName == "StickBoy")
                return Localize("actorNames.stick");
            if (nonLocalizedName == "SubBot")
                return Localize("actorNames.sub");
            if (nonLocalizedName == "SwimFish")
                return Localize("actorNames.swimFish");
            if (nonLocalizedName == "TerraCannon")
                return Localize("actorNames.terraCannon");
            if (nonLocalizedName == "Turtle")
                return Localize("actorNames.turtle");
            if (nonLocalizedName == "Rover")
                return Localize("actorNames.rover");
            if (nonLocalizedName == "SimWorld.Path.WayPoint")
                return Localize("actorNames.wayPoint");
            if (nonLocalizedName == "WindBlimp")
                return Localize("actorNames.windBlimp");
            if (nonLocalizedName == "Wisp")
                return Localize("actorNames.wisp");
            if (nonLocalizedName == "Yucca1")
                return Localize("actorNames.yucca1");
            if (nonLocalizedName == "Yucca2")
                return Localize("actorNames.yucca2");
            if (nonLocalizedName == "Yucca3")
                return Localize("actorNames.yucca3");

            if (nonLocalizedName == "PipeStraight" )
                return Localize("actorNames.pipestraight");
            if (nonLocalizedName == "PipeCorner")
                return Localize("actorNames.pipecorner");
            if (nonLocalizedName == "PipeCross")
                return Localize("actorNames.pipecross");

            return "";
        }
        /// <summary>
        /// Given a classification color, returns the localized string for that color.
        /// </summary>
        public static string GetColorName(Classification.Colors color)
        {
            return Localize("colorNames." + color.ToString());
        }   // end of GetColorName()

#if !NETFX_CORE
        public static string GetNetworkSessionEndReason(Microsoft.Xna.Framework.Net.NetworkSessionEndReason reason)
        {
            switch (reason)
            {
                case Microsoft.Xna.Framework.Net.NetworkSessionEndReason.ClientSignedOut:
                    return Localize("networkSessionEndReason.clientSignedOut");
                case Microsoft.Xna.Framework.Net.NetworkSessionEndReason.Disconnected:
                    return Localize("networkSessionEndReason.disconnected");
                case Microsoft.Xna.Framework.Net.NetworkSessionEndReason.HostEndedSession:
                    return Localize("networkSessionEndReason.hostEndedSession");
                case Microsoft.Xna.Framework.Net.NetworkSessionEndReason.RemovedByHost:
                    return Localize("networkSessionEndReason.removedByHost");
                default:
                    throw new Exception("invalid reason");
            }
        }
#endif

        public static string GetGenreName(int flag)
        {
            if (flag == -1)
                return Strings.Localize("genres.all");

            if (0 != (flag & (int)BokuShared.Genres.Action))
                return Strings.Localize("genres.action");
            if (0 != (flag & (int)BokuShared.Genres.Adventure))
                return Strings.Localize("genres.adventure");
            if (0 != (flag & (int)BokuShared.Genres.Puzzle))
                return Strings.Localize("genres.puzzle");
            if (0 != (flag & (int)BokuShared.Genres.Racing))
                return Strings.Localize("genres.racing");
            if (0 != (flag & (int)BokuShared.Genres.RPG))
                return Strings.Localize("genres.rpg");
            if (0 != (flag & (int)BokuShared.Genres.Shooter))
                return Strings.Localize("genres.shooter");
            if (0 != (flag & (int)BokuShared.Genres.Sports))
                return Strings.Localize("genres.sports");
            if (0 != (flag & (int)BokuShared.Genres.Strategy))
                return Strings.Localize("genres.strategy");
            if (0 != (flag & (int)BokuShared.Genres.Multiplayer))
                return Strings.Localize("genres.multiplayer");
            if (0 != (flag & (int)BokuShared.Genres.Favorite))
                return Strings.Localize("genres.favorite");
            if (0 != (flag & (int)BokuShared.Genres.Keyboard))
                return Strings.Localize("genres.keyboard");
            if (0 != (flag & (int)BokuShared.Genres.Controller))
                return Strings.Localize("genres.controller");
            if (0 != (flag & (int)BokuShared.Genres.Touch))
                return Strings.Localize("genres.touch");

            if (0 != (flag & (int)BokuShared.Genres.Lessons))
                return Strings.Localize("genres.lessons");
            if (0 != (flag & (int)BokuShared.Genres.SampleWorlds))
                return Strings.Localize("genres.sampleWorlds");
            if (0 != (flag & (int)BokuShared.Genres.StarterWorlds))
                return Strings.Localize("genres.starterWorlds");
            if (0 != (flag & (int)BokuShared.Genres.FinishedWorlds))
                return Strings.Localize("genres.finishedWorlds");

            if (0 != (flag & (int)BokuShared.Genres.BuiltInWorlds))
                return Strings.Localize("genres.builtInWorlds");
            if (0 != (flag & (int)BokuShared.Genres.MyWorlds))
                return Strings.Localize("genres.myWorlds");
            if (0 != (flag & (int)BokuShared.Genres.Downloads))
                return Strings.Localize("genres.downloads");

            return null;
        }
        public static string[] GetGenreNames(int flags)
        {
            List<string> list = new List<string>();

            for (int i = 1; i != 0; i <<= 1)
            {
                string name = GetGenreName(i & flags);
                if (!String.IsNullOrEmpty(name))
                    list.Add(GetGenreName(i));
            }
            return list.ToArray();
        }
        #endregion
    }   // end of class Strings
}   // end of namespace Boku.Common
