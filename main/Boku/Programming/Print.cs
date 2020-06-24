using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

#if NETFX_CORE
    using Windows.Graphics.Printing;
#else
    using System.Windows.Forms;
#endif

using System.IO;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.Common.TutorialSystem;
using Boku.SimWorld.Terra;

namespace Boku.Programming
{
    /// <summary>
    /// Static class with methods for outputting Kode to .txt file and printer.
    /// </summary>
    public class Print
    {
        #region Public

        /// <summary>
        /// Prints the programming for the whole level.
        /// </summary>
        static public void PrintProgramming()
        {
            TextWriter tw = null;

            // Try back compat path version first.
            string fullPath = GetFullPath(false);
            try
            {
                tw = OpenFile(fullPath);
            }
            catch { }

            // If prev version failed, try new approach.
            if (tw == null)
            {
                fullPath = GetFullPath(true);
                try
                {
                    tw = OpenFile(fullPath);
                }
                catch { }
            }

            if (tw != null)
            {
                PrintHeading(tw);

                for (int i = 0; i < InGame.inGame.gameThingList.Count; i++)
                {
                    GameActor actor = InGame.inGame.gameThingList[i] as GameActor;
                    if (actor != null)
                    {
                        PrintActorProgramming(tw, actor);
                    }
                }

                // If this level is a tutorial, also print out the tutorial text.
                TutorialManager.Print(tw);

#if NETFX_CORE
                tw.Flush();
                tw.Dispose();
#else
                tw.Close();
#endif

                Instrumentation.IncrementCounter(Instrumentation.CounterId.PrintKode);

                SendToPrinter(fullPath);
            }
        }   // end of PrintProgramming()

        /// <summary>
        /// Prints the programming for the specific actor.
        /// </summary>
        /// <param name="actor"></param>
        static public void PrintProgramming(GameActor actor)
        {
            TextWriter tw = null;

            // Try back compat path version first.
            string fullPath = GetFullPath(false);
            try
            {
                tw = OpenFile(fullPath);
            }
            catch { }

            // If prev version failed, try new approach.
            if (tw == null)
            {
                fullPath = GetFullPath(true);
                try
                {
                    tw = OpenFile(fullPath);
                }
                catch { }
            }

            if (tw != null)
            {
                PrintHeading(tw);
                PrintActorProgramming(tw, actor);
#if NETFX_CORE
                tw.Flush();
                tw.Dispose();
#else
                tw.Close();
#endif
            
                SendToPrinter(fullPath);
            }

        }   // end of PrintProgramming(GameActor)

        #endregion

        #region Internal

        static private string GetFullPath(bool fixup)
        {
            string filename = NewFileName(fixup);
            string fullPath = Path.Combine(Storage4.UserLocation, filename);
            
            return fullPath;
        }   // end of GetFullPath()

        static private TextWriter OpenFile(string fullPath)
        {
            TextWriter tw = null;
#if NETFX_CORE
            tw = Storage4.OpenStreamWriter(fullPath);
#else
            tw = new StreamWriter(fullPath);
#endif

            return tw;
        }   // end of OpenFile

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fixup">Tells system to not use version with .. in it.  Pure hack uglisness to maintain back compatibility.</param>
        /// <returns></returns>
        static private string NewFileName(bool fixup)
        {
            // If using the default path, step up one level in the tree.  Else just use the path given by the user.
            string root = fixup ? @"Kode" : @"..\Kode";
            string ext = @".txt";
            string fmt = "D4";

            int i = 0;
            while (Storage4.FileExists(root + i.ToString(fmt) + ext, StorageSource.UserSpace))
            {
                ++i;
            }
            return root + i.ToString(fmt) + ext;
        }   // end of NewFileName()

        /// <summary>
        /// Prints the level heding information to the Textwriter
        /// </summary>
        /// <param name="tw"></param>
        static private void PrintHeading(TextWriter tw)
        {
            tw.WriteLine(Strings.Localize("loadLevelMenu.title") + " : " + InGame.XmlWorldData.name);
            tw.WriteLine(Strings.Localize("loadLevelMenu.creator") + " : " + InGame.XmlWorldData.creator);
            tw.WriteLine(Strings.Localize("loadLevelMenu.description") + " : " + InGame.XmlWorldData.description);
#if NETFX_CORE
            tw.WriteLine(Strings.Localize("loadLevelMenu.date") + " : " + InGame.XmlWorldData.lastWriteTime.ToString() + " " + InGame.XmlWorldData.lastWriteTime.ToString());
#else
            tw.WriteLine(Strings.Localize("loadLevelMenu.date") + " : " + InGame.XmlWorldData.lastWriteTime.ToShortDateString() + " " + InGame.XmlWorldData.lastWriteTime.ToShortTimeString());
#endif

            tw.WriteLine("========");
        }   // end of PrintHeading()

        static public void PrintActorProgramming(TextWriter tw, GameActor actor)
        {
            Task task = actor.Brain.tasks[0];

            // Skip actors that don't have any programming.
            if (task.reflexes.Count == 0)
            {
                return;
            }

            tw.WriteLine("");
            tw.WriteLine(actor.DisplayNameNumber);
            for (int i = 0; i < actor.Brain.tasks.Count; i++)
            {
                PrintTaskProgramming(tw, actor, i);
            }

        }   // end of PrintActorProgramming()
        static public void SerializeActorProgramming(TextWriter tw, GameActor actor)
        {
            Task task = actor.Brain.tasks[0];

            // Skip actors that don't have any programming.
            if (task.reflexes.Count == 0)
            {
                return;
            }

            tw.Write("'kode':{");
            tw.Write("'actorName':'"+actor.DisplayNameNumber+"'");
            tw.Write(",'pages':{");
            for (int i = 0; i < actor.Brain.tasks.Count; i++)
            {
                SerializeTaskProgramming(tw, actor, i);
            }
            tw.Write("}");//pages
            tw.Write("}");//kode

        }   // end of PrintActorProgramming()

        static private void SerializeTaskProgramming(TextWriter tw, GameActor actor, int taskID)
        {
            Task task = actor.Brain.tasks[taskID];

            // Skip tasks that don't have any kode.
            if (task.reflexes.Count == 0)
            {
                return;
            }
            tw.Write("'pageNumber':"+ (taskID + 1).ToString());
            tw.Write(",'lines':[");
            for (int i = 0; i < task.reflexes.Count; i++)
            {
                Reflex reflex = task.reflexes[i] as Reflex;
                tw.WriteLine(SerializeTileString(reflex));
            }
            tw.Write("]");//lines
        }   // end of PrintTaskProgramming()

        static private string SerializeTileString(Reflex reflex)
        {
            string tiles = "{'when':{";

            if (reflex.Data.Sensor != null)
            {
                tiles += "'sensor':'" + reflex.Data.Sensor.label + "',";
                if (reflex.Data.Sensor.label == "hear")    //if this is a hear sensor.
                {
                    for (int i = 0; i < reflex.Data.Filters.Count; i++)
                    {
                        if (reflex.Data.Filters[i].label == "said")//see if there is a specific hear text.
                        {
                            tiles += "'hearText':[";
                            for (int j = 0; j < reflex.SaidStrings.Count; j++)
                            {
                                tiles += "'" + reflex.SaidStrings[j] + "',";
                            }
                            tiles += "],";
                        }
                    }
                }
            }
            else
                tiles += "'sensor':" + "'always',";

            tiles += "'filters':[";
            for (int i = 0; i < reflex.Data.Filters.Count; i++)
            {
                ScoreFilter sf = reflex.Data.Filters[i] as ScoreFilter;
                if (sf != null)
                {
                    tiles += "'"+sf.points.ToString() + "',";
                }
                tiles += "'" + reflex.Data.Filters[i].label + "',";

                TerrainFilter tf = reflex.Data.Filters[i] as TerrainFilter;
                if (tf != null)
                {
                    ushort matIdx = (ushort)reflex.MaterialType;
                    int matLabel = Terrain.MaterialIndexToLabel(matIdx);
                    tiles += "'" + matLabel.ToString() + "',";
                }

            }
            tiles += "]";//filters
            tiles += "},";//when

            tiles += "'do':{";

            if (reflex.Data.Actuator != null)
            {
                tiles += "'action':'" + reflex.Data.Actuator.label + "',";
                // If we have a say verb, also output the text.
                if (reflex.Data.Actuator.upid == "actuator.say")
                {
                    tiles += "'sayText':[";
                    for (int j = 0; j < reflex.SayStrings.Count; j++)
                    {
                        tiles+="'" + reflex.SayStrings[j] + "',";
                    }
                    tiles += "],";
                }
            }
            if (reflex.Data.Selector != null)
                tiles += "'selector':'" + reflex.Data.Selector.label + "',";

            tiles += "'modifiers':[";
            for (int i = 0; i < reflex.Data.Modifiers.Count; i++)
            {
                ScoreModifier sm = reflex.Data.Modifiers[i] as ScoreModifier;
                if (sm != null)
                {
                    tiles += "points:" + sm.points.ToString() + ",";
                }

                CreatableModifier cm = reflex.Data.Modifiers[i] as CreatableModifier;
                if (cm != null)
                {
                    GameActor actor = InGame.inGame.GetCreatable(cm.CreatableId);
                    if (actor != null)
                    {
                        tiles += "actorName:"+actor.DisplayNameNumber + ",";
                    }
                    else
                    {
                        tiles += "<unknown>";
                    }
                }
                else
                {
                    tiles += "'"+reflex.Data.Modifiers[i].label + "',";
                }
            }
            tiles += "]";//modifiers
            tiles += "}";//do
            tiles += "},";//line
            return tiles;
        }   // end of GetTileString()

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
                tw.WriteLine("        " + (i+1).ToString("###") + "    " + GetTileString(reflex));

                // If we have a said filter, also output the text.
                if (reflex.Data.Sensor != null && reflex.Data.Sensor.upid == "sensor.ears")
                {
                    string indent = "                ";
                    for (int j = 0; j < reflex.Indentation; j++)
                    {
                        indent += "    ";
                    }

                    tw.WriteLine(indent + "said filter text");
                    for (int j = 0; j < reflex.SaidStrings.Count; j++)
                    {
                        tw.WriteLine(indent + reflex.SaidStrings[j]);
                    }
                }

                // If we have a say verb, also output the text.
                if (reflex.Data.Actuator != null && reflex.Data.Actuator.upid == "actuator.say")
                {
                    string indent = "                ";
                    for (int j = 0; j < reflex.Indentation; j++)
                    {
                        indent += "    ";
                    }

                    tw.WriteLine(indent + "say verb text");
                    for (int j = 0; j < reflex.SayStrings.Count; j++)
                    {
                        tw.WriteLine(indent + reflex.SayStrings[j]);
                    }
                }
            }

        }   // end of PrintTaskProgramming()

        static private string GetTileString(Reflex reflex)
        {
            string tiles = "";

            // Ad blanks at beginning for indentation level.
            for (int i = 0; i < reflex.Indentation; i++)
            {
                tiles += "    ";
            }

            tiles += Strings.Localize("programming.when") + " ";

            if (reflex.Data.Sensor != null)
                tiles += reflex.Data.Sensor.label + " ";
            else
                tiles += "always ";

            for (int i = 0; i < reflex.Data.Filters.Count; i++)
            {
                ScoreFilter sf = reflex.Data.Filters[i] as ScoreFilter;
                if(sf != null)
                {
                    tiles += sf.points.ToString() + " ";
                }
                tiles += reflex.Data.Filters[i].label + " ";

                TerrainFilter tf = reflex.Data.Filters[i] as TerrainFilter;
                if (tf != null)
                {
                    ushort matIdx = (ushort)reflex.MaterialType;
                    int matLabel = Terrain.MaterialIndexToLabel(matIdx);
                    tiles += matLabel.ToString() + " ";
                }

                TimerFilter tif = reflex.Data.Filters[i] as TimerFilter;
                if (tif != null)
                {
                    tiles += tif.seconds.ToString() + " ";
                }
            }

            tiles += "-- " + Strings.Localize("programming.do") + " ";

            if (reflex.Data.Actuator != null)
                tiles += reflex.Data.Actuator.label + " ";

            if (reflex.Data.Selector != null)
                tiles += reflex.Data.Selector.label + " ";

            for (int i = 0; i < reflex.Data.Modifiers.Count; i++)
            {
                ScoreModifier sm = reflex.Data.Modifiers[i] as ScoreModifier;
                if (sm != null)
                {
                    tiles += sm.points.ToString() + " ";
                }

                CreatableModifier cm = reflex.Data.Modifiers[i] as CreatableModifier;
                if (cm != null)
                {
                    GameActor actor = InGame.inGame.GetCreatable(cm.CreatableId);
                    if (actor != null)
                    {
                        tiles += actor.DisplayNameNumber + " ";
                    }
                    else
                    {
                        tiles += "? ";
                    }
                }
                else
                {
                    tiles += reflex.Data.Modifiers[i].label + " ";
                }

            }

            return tiles;
        }   // end of GetTileString()

#if NETFX_CORE
        static void printManager_PrintTaskRequested(PrintManager printManager, PrintTaskRequestedEventArgs args)
        {
            PrintTask printTask = args.Request.CreatePrintTask("Kode", PrintTaskSourceRequested);
        }

        static void PrintTaskSourceRequested(PrintTaskSourceRequestedArgs args)
        {
            IPrintDocumentSource source = null;

            // TODO How do we get from our text file to an IPrintDocumentSource???

            args.SetSource(source);
        }

        static bool firstTime = true;
#endif
        private static void SendToPrinter(string fullPath)
        {
#if NETFX_CORE
            // Do nothing here.  At least the users can go find the Kode.txt
            // file on disk and print it manually.
            /*
            PrintManager printManager = PrintManager.GetForCurrentView();
            if (firstTime)
            {
                printManager.PrintTaskRequested += printManager_PrintTaskRequested;
                firstTime = false;
            }

            PrintManager.ShowPrintUIAsync();
            */
#else
            try
            {
                DialogResult? print = DialogResult.OK;

                // Skip the print dialog if in full screen mode.
                // TODO (****) *** Aren't we always windowed now???
                //if (!BokuGame.Graphics.IsFullScreen)
                {
                    PrintDialog dialog = new PrintDialog();

                    print = dialog.ShowDialog();

                    if (print == DialogResult.OK)
                    {
                        PageToPrinter(fullPath, dialog);
                        /*
                        Process myProcess = new Process();

                        myProcess.StartInfo.FileName = fullPath;
                        myProcess.StartInfo.Verb = "Print";
                        myProcess.StartInfo.CreateNoWindow = true;
                        myProcess.Start();
                        */
                    }
                }
            }
            catch(Exception e)
            {
                if (e != null)
                {
                }
            }
#endif
        }   // end of SendToPrinter()

#if!NETFX_CORE
        static System.IO.StreamReader fileToPrint;
        static System.Drawing.Font printFont;
        static System.Drawing.Printing.PrintDocument printDocument;

        private static void PageToPrinter(string fullPath, PrintDialog dialog)
        {
            try
            {
                printDocument = new System.Drawing.Printing.PrintDocument();
                // Set the output to go to the print the user picked.
                printDocument.PrinterSettings = dialog.PrinterSettings;

                printDocument.PrintPage += PrintDocumentPrintPage;

                // This suppresses the status dialog which can cause full screen Kodu to minimize.
                printDocument.PrintController = new System.Drawing.Printing.StandardPrintController();

                fileToPrint = new System.IO.StreamReader(fullPath);
                printFont = new System.Drawing.Font("Arial", 10);
                printDocument.Print();
                fileToPrint.Close();
            }
            catch (Exception e)
            {
                if (e != null)
                {
                }
            }
        }

        private static void PrintDocumentPrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            float yPos = 0f;
            int count = 0;
            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            string line = null;
            float linesPerPage = e.MarginBounds.Height / printFont.GetHeight(e.Graphics);
            while (count < linesPerPage)
            {
                line = fileToPrint.ReadLine();
                if (line == null)
                {
                    break;
                }
                yPos = topMargin + count * printFont.GetHeight(e.Graphics);
                e.Graphics.DrawString(line, printFont, System.Drawing.Brushes.Black, leftMargin, yPos, new System.Drawing.StringFormat());
                count++;
            }
            if (line != null)
            {
                e.HasMorePages = true;
            }
        }

#endif

        #endregion

    }   // end of class Print


}   // end of namespace Boku.Programming
