// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Boku.Base;
using Boku.Common;
using Boku.Programming;
using Boku.SimWorld.Terra;

namespace Boku.Analyses
{
    public class WriteToFile
    {
#if !NETFX_CORE
        public static string directory = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            + @"\analytics";
        public string unreachableFile = "unreachablePages.txt";
        public string blankjumps = "blankjumps.txt";
        public static string tileusage = directory + @"\" + "tileusage.txt";

        public void writeFile(string text)
        {
            TextWriter tw = new StreamWriter(directory + @"\analytics.txt");
            tw.WriteLine(text);

            tw.Close();
        }

        public void logBlankJumps(GameActor actor, int sourcepage, int destpage)
        {
            string path = directory + @"\" + blankjumps;
            string contents = "Game = " + InGame.XmlWorldData.id + " " + InGame.XmlWorldData.name + " Actor = " + actor.DisplayNameNumber + ", source = " + sourcepage + ", destination = " + destpage;
            
            //File.AppendAllText(path, contents);
            if (File.Exists(path) == false)
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Game, Actor, Page");
                }
            }
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine(contents);
                sw.Flush();
                sw.Close();
            }
        }

        public void logUnreachable(GameActor actor, int taskid)
        {
            string path = directory + @"\" + unreachableFile;
            string contents = "Game = " + InGame.XmlWorldData.id + " " + InGame.XmlWorldData.name + " Actor = " + actor.DisplayNameNumber + ", Page = " +  taskid;

            //File.AppendAllText(path, contents);
            if (File.Exists(path) == false)
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Game, Actor, Page");
                }
            }
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine(contents);
                sw.Flush();
                sw.Close();
            }
        }

        public void logTileUsage(Dictionary<string, int> usage)
        {
            string path = tileusage;
            if (File.Exists(path) == false)
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.Write("");
                }
            }
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.Write(InGame.XmlWorldData.id + ", ");
                foreach (KeyValuePair<string, int> pair in usage)
                {
                    sw.Write(pair.Key + ": " + pair.Value + ", ");
                }
                sw.WriteLine(";");
                sw.Close();
            }
        }

        public void writeKode(string program)
        {
            TextWriter tw = new StreamWriter(directory
               + @"\" + InGame.XmlWorldData.id + @"kode.txt");
            tw.WriteLine(program);
            tw.WriteLine(InGame.XmlWorldData.name);
            tw.WriteLine(InGame.XmlWorldData.creator);
            tw.WriteLine(InGame.XmlWorldData.lastWriteTime.ToString());

            for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
            {
                GameActor actor = InGame.inGame.gameThingList[i] as GameActor;
                if (actor != null)
                {
                    PrintActorProgramming(tw, actor);
                }
            }

            tw.Close();
        }

        static private void PrintActorProgramming(TextWriter tw, GameActor actor)
        {
            Task task = actor.Brain.tasks[0];

            // Skip actors that don't have any programming.
   //         if (task.reflexes.Count == 0)
   //         {
   //             return;
   //         }

            tw.WriteLine("");

            if (actor.CreatableId != Guid.Empty)
            {
                tw.WriteLine(actor.DisplayNameNumber + " " + "cloned " + actor.CreatableId);
            }
            else
            {
                tw.WriteLine(actor.DisplayNameNumber);
            }
            for (int i = 0; i < actor.Brain.tasks.Count; i++)
            {
                PrintTaskProgramming(tw, actor, i);
            }

        }   // end of PrintActorProgramming()

        static private void PrintTaskProgramming(TextWriter tw, GameActor actor, int taskID)
        {
            Task task = actor.Brain.tasks[taskID];

            // Skip tasks that don't have any kode.
            if (task.reflexes.Count == 0)
            {
                return;
            }

            string gap = " ";
            if (taskID < 9)
            {
                gap = "  ";
            }
            tw.WriteLine("    " + Strings.Localize("programming.page") + gap + (taskID + 1).ToString());
            for (int i = 0; i < task.reflexes.Count; i++)
            {
                Reflex reflex = task.reflexes[i] as Reflex;
                tw.WriteLine("        " + (i + 1).ToString("###") + "    " + GetTileString(reflex));

                // If we have a say verb, also output the text.
                //if (reflex.Data.Actuator != null && reflex.Data.Actuator.upid == "actuator.say")
                //{
                //    string indent = "                ";
                //    for (int j = 0; j < reflex.Indentation; j++)
                //    {
                //        indent += " tab ";
                //    }

                //    for (int j = 0; j < reflex.SayStrings.Count; j++)
                //    {
                //        tw.WriteLine(indent + reflex.SayStrings[j]);
                //    }
                //}
            }

        }   // end of PrintTaskProgramming()

        static private string GetTileString(Reflex reflex)
        {
            string tiles = "";

            // Ad blanks at beginning for indentation level.
            for (int i = 0; i < reflex.Indentation; i++)
            {
                tiles += " tab ";
            }

            //tiles += Strings.Localize("programming.when") + " ";

            if (reflex.Data.Sensor != null)
                tiles += reflex.Data.Sensor.upid + " ";
                //tiles += reflex.Data.Sensor.label + " ";
            else
                tiles += "sensor.always ";

            for (int i = 0; i < reflex.Data.Filters.Count; i++)
            {
                tiles += reflex.Data.Filters[i].upid + " ";
                //ScoreFilter sf = reflex.Data.Filters[i] as ScoreFilter;
                //if (sf != null)
                //{
                //    tiles += sf.points.ToString() + " ";
                //}
                //tiles += reflex.Data.Filters[i].label + " ";

                //TerrainFilter tf = reflex.Data.Filters[i] as TerrainFilter;
                //if (tf != null)
                //{
                //    int matIdx = reflex.MaterialType;
                //    int mat = Terrain.MaterialIndexToLabel(matIdx);
                //    tiles += mat.ToString() + " ";
                //}
            }

            tiles += "-- ";// +Strings.Localize("programming.do") + " ";

            if (reflex.Data.Actuator != null)
                tiles += reflex.Data.Actuator.upid + " ";
                //tiles += reflex.Data.Actuator.label + " ";

            if (reflex.Data.Selector != null)
                tiles += reflex.Data.Selector.upid + " ";
                //tiles += reflex.Data.Selector.label + " ";

            for (int i = 0; i < reflex.Data.Modifiers.Count; i++)
            {
                tiles += reflex.Data.Modifiers[i].upid + " ";
                //ScoreModifier sm = reflex.Data.Modifiers[i] as ScoreModifier;
                //if (sm != null)
                //{
                //    tiles += sm.points.ToString() + " ";
                //}

                //CreatableModifier cm = reflex.Data.Modifiers[i] as CreatableModifier;
                //if (cm != null)
                //{
                //    GameActor actor = InGame.inGame.GetCreatable(cm.CreatableId);
                //    if (actor != null)
                //    {
                //        tiles += actor.CreatableName + " ";
                //    }
                //    else
                //    {
                //        tiles += "? ";
                //    }
                //}
                //else
                //{
                //    tiles += reflex.Data.Modifiers[i].label + " ";
                //}

            }

            return tiles;
        }   // end of GetTileString()
#endif
    }
}
