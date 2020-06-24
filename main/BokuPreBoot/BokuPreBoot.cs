using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;

using Boku.Common;

namespace BokuPreBoot
{
    static class BokuPreBoot
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                string patchPath = "";

                // TODO (scoy) Are we ever running a patched binary???
                /*
                // Figure out whether we are the patched exe by looking at our location on disk.
                string patchPath = Storage4.UserLocation + Path.DirectorySeparatorChar + @"Patch" + Path.DirectorySeparatorChar;
                string titleLoc = Path.GetFullPath(Storage4.TitleLocation);
                if (!titleLoc.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    titleLoc += Path.DirectorySeparatorChar;

                // If the running exe is located in the patch folder, then we are the patch.
                bool titleIsPatch = (0 == String.Compare(titleLoc, patchPath, true));
                */

                Storage4.Init();
                Storage4.StartupDir = Application.StartupPath;

                bool titleIsPatch = false;

                if (!titleIsPatch)
                {
                    try
                    {
                        // Check version numbers
                        Assembly myAssem = Assembly.GetExecutingAssembly();
                        AssemblyName myAssemName = myAssem.GetName();

                        string patchFilename = patchPath + myAssem.ManifestModule.Name;
                        AssemblyName patchAssemName = AssemblyName.GetAssemblyName(patchFilename);

                        if (myAssemName.Version < patchAssemName.Version)
                        {
                            // Spawn the patched version.
                            Process proc = new Process();
                            proc.StartInfo.FileName = patchPath + myAssem.ManifestModule.Name;
                            proc.StartInfo.WorkingDirectory = patchPath;

                            proc.Start();

                            // Stop executing.
                            return;
                        }
                    }
                    catch 
                    {
                        // Nothing to see here.  Move along.
                        // We get here if there is no patched version (ie all the time).
                    }
                }

                // If we get here, then either:
                //  - We are the patched exe.
                //  - We are newer than the patched exe.
                //  - Something bad happened while trying to launch the patched exe.

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainWindow());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n" + e.InnerException.Message);
            }

        }
    }
}