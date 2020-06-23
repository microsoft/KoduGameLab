
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;
using System.Windows.Forms;

namespace Boku
{
    /// <summary>
    /// Simple, static class for being able to write debug lines to a file.
    /// Should probably not be used at all for real releases, just for test builds.
    /// </summary>
    public static class DebugLog
    {
        /*
        static string filename = "KoduDebug.txt";

        /// <summary>
        /// Writes a header to the debug file to indicate the new run.
        /// </summary>
        static public void NewRun()
        {
            WriteLine("\n\n\n====================\n");
            WriteLine("        Kodu");
            WriteLine("        " + Program2.ThisVersion.ToString());
            WriteLine("        " + DateTime.Now.ToShortDateString());
            WriteLine("        " + DateTime.Now.ToShortTimeString());
            WriteLine("\n");

        }   // end of NewRun()

        static public void WriteLine(string str)
        {
            lock (filename)
            {
                
                try
                {
                    string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), @"SavedGames\Boku\Player1");
                    string filePath = Path.Combine(dirPath, filename);

                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }

                    var t = Thread.CurrentThread;
                    File.AppendAllText(filePath, t.ManagedThreadId.ToString() + str + "\n");
                }
                catch (Exception e)
                {
                    if (e != null)
                    {
                        string msg = "";
                        msg += "Error logging to debug file.\n";
                        if (!string.IsNullOrEmpty(e.Message))
                        {
                            msg += e.Message + "\n";
                        }
                        if (e.InnerException != null)
                        {
                            if (!string.IsNullOrEmpty(e.InnerException.Message))
                            {
                                msg += e.InnerException.Message + "\n";
                            }
                        }

                        MessageBox.Show(msg);

                    }
                }

            }   // end of lock
        }   // end of WriteLine()

        public static void WriteException(Exception e, string str)
        {
            if (e != null)
            {
                string msg = "";
                msg += "Exception.  " + str + "\n";
                if (!string.IsNullOrEmpty(e.Message))
                {
                    msg += e.Message + "\n";
                }
                if (e.InnerException != null)
                {
                    if (!string.IsNullOrEmpty(e.InnerException.Message))
                    {
                        msg += e.InnerException.Message + "\n";
                    }
                }

                DebugLog.WriteLine(msg);
            }
        }   // end of WriteException()

        */

    }   // end of class DebugLog

}   // end of namespace Boku
