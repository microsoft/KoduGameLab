using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Boku;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Analyses;

namespace KoduAnalytics.ProgramAnalytics
{
    public class RunAnalysis
    {
        public static string LevelsPath = @"Xml\Levels\";
        public static string MyWorldsPath = LevelsPath + @"MyWorlds\";
        public static string executable = @"..\..\..\Boku\bin\x86\Debug\Boku.exe";
        public static string directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                   + @"\analytics";
        public static string tileusage = directory + @"\" + "tileusage.txt";

        public static void loadProgram()
        {
            // Uncomment this line to load new files for analytics.
            //runTheStuff();

            //now, we should launch into an analysis of the community data?

            //tile analysis

            TileAnalysis ta = new TileAnalysis();

            //read in community files
            //set up LINQ structures
           
            //Does the tile type analysis
            // ta.process();

            //find interesting stuff. It's that easy, right?
            ta.getGameInformation();
            

        }


        public static void runTheStuff()
        {
            List<String> files = getFileToAnalyze();
            foreach (String file in files)
            {
                String args = "-Import \"" + file + "\" -analytics";
                Console.WriteLine(file);
                DateTime date1 = DateTime.Now;
                DateTime date2;
                TimeSpan ts = new TimeSpan();
                var process = System.Diagnostics.Process.Start(executable, args);
                while (!process.HasExited)
                {
                    date2 = DateTime.Now;
                    ts = date2 - date1;
                    if (ts.Seconds > 30)
                    {
                        process.Kill();
                    }
                }
                process.WaitForExit();
                //TimeSpan ts = process.UserProcessorTime;
                Console.WriteLine("Seconds: " + ts.Seconds);
                Console.WriteLine("Exit Code: " + process.ExitCode);
            }
        }

        public static List<String> getFileToAnalyze()
        {
            List<String> files = new List<String>();
            // create and show an open file dialog
            OpenFileDialog dlog = new OpenFileDialog();

            dlog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SavedGames\Boku\Player1";
            dlog.CheckFileExists = true;
            dlog.ValidateNames = true;
            dlog.Multiselect = true;
            if (dlog.ShowDialog() == DialogResult.OK)
            {
                foreach( String file in dlog.FileNames) {
                    string path = file;
               //     path = System.IO.Path.ChangeExtension(path, null);
                    files.Add(path);
                }
                
            }
           // string fullPathToLevelFile = dlog.FileName;//// +Path.DirectorySeparatorChar + BokuGame.Settings.MediaPath + MyWorldsPath + level.WorldId.ToString() + ".Xml";   
           // getXML(fullPathToLevelFile);
            return files;
        }

        public static void getXML(String filename)
        {
            
            Boku.Common.Xml.XmlWorldData xml = Boku.Common.Xml.XmlWorldData.Load(filename, null);
        }
    }
}
