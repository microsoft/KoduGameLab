using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using BokuShared;

namespace BokuSetupTool
{
    /// <summary>
    /// This is a command-line utility program to aid BokuSetup's process of building the Kodu Installer.
    /// This program is able to:
    /// - Check out files from Source Depot.
    /// - Increment Kodu's build number:
    ///   - Updates Boku project's AssemblyInfo.cs with new build number.
    ///     - This file is the authoritative source of Kodu version information.
    ///       Other sources of version information are derived from this file.
    ///   - Updates BokuSetup project's Build.wxi with new build number.
    /// </summary>
    class Program
    {
        static CmdLine cmdline;
        static string solutionDir;

        static int Main(string[] args)
        {
            try
            {
                cmdline = new CmdLine(args);

                // Protect against running from the command line.
                if (!cmdline.Exists("Spawned"))
                    throw new Exception("This program may not be executed directly.");

                // The Boku solution folder. Visual Studio macro $(SolutionDir).
                if (!cmdline.Exists("SolutionDir"))
                    throw new Exception("Solution directory was not supplied.");

                // Get the rooted representation of the solution path (containing no relative path elements).
                solutionDir = cmdline.GetString("SolutionDir", "");
                solutionDir = solutionDir.Trim('"');
                solutionDir = Path.GetFullPath(solutionDir);

                // Dispatch the command

                string command = cmdline.GetString("Command", "");
                switch (command)
                {
                    // Increment Boku's version number
                    case "IncBuild":
                        return DoIncBuild();

                    // Checkout the Content.wxs file from Source Depot
                    case "CheckoutContentWxs":
                        return DoCheckoutContentWxs();

                    default:
                        throw new Exception("Unrecognized command: " + command);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return -1;
            }
        }

        private static int DoCheckoutContentWxs()
        {
            string filename = solutionDir + @"BokuSetup\Content.wxs";

            Console.WriteLine("Updating " + filename + "...");

            //TFS.EditFile(filename);

            return 0;
        }

        private enum VersionComponents
        {
            All,
            Major,
            Minor,
            Build,
            Revision
        }

        /// <summary>
        /// Increments Boku's build number. Reads from Boku's assembly info. Writes new assembly and WXS files.
        /// </summary>
        /// <returns></returns>
        private static int DoIncBuild()
        {
            int major = 1;
            int minor = 0;
            int build = -1;
            int revision = 0;

            Guid productCode = Guid.NewGuid();

            string assemFilename = solutionDir + @"Boku\Properties\AssemblyInfo.cs";
            string buildFilename = solutionDir + @"BokuSetup\Build.wxi";

            // Checkout Boku's AssemblyInfo.cs file from source control.

            Console.WriteLine("Updating " + assemFilename + "...");
            //TFS.EditFile(assemFilename);

            // Read the version number from the file, writing a new version of the file with an incremented build number.

            StreamReader assemSrc = new StreamReader(assemFilename);
            StreamWriter assemDst = new StreamWriter(assemFilename + ".tmp");

            while (!assemSrc.EndOfStream)
            {
                string line = assemSrc.ReadLine();

                if (line.Contains("AssemblyVersion"))
                {
                    Regex re = new Regex(@"(\d+)\.(\d+)\.(\d+)\.(\d+)");
                    Match m = re.Match(line);

                    if (m.Groups.Count != 5)
                        throw new Exception("Error parsing version number from string: " + line);

                    major = Int32.Parse(m.Groups[(int)VersionComponents.Major].Value);
                    minor = Int32.Parse(m.Groups[(int)VersionComponents.Minor].Value);

                    revision = Int32.Parse(m.Groups[(int)VersionComponents.Revision].Value);
                    build = Int32.Parse(m.Groups[(int)VersionComponents.Build].Value);
                    build += 1;

                    assemDst.WriteLine(String.Format("[assembly: AssemblyVersion(\"{0}.{1}.{2}.{3}\")]", major, minor, build, revision));
                }
                else
                {
                    assemDst.WriteLine(line);
                }
            }

            assemSrc.Close();
            assemDst.Close();

            // Did we get a build number from AssemblyInfo.cs?

            if (build < 0)
                throw new Exception("Failed to find version number in " + assemFilename);

            // Write an updated version of BokuSetup's Build.wxi file.

            Console.WriteLine("Updating " + buildFilename + "...");
            //TFS.EditFile(buildFilename);

            StreamWriter buildDst = new StreamWriter(buildFilename);
            buildDst.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            buildDst.WriteLine("<!-- This file is auto-generated by BokuSetupTool.exe -->");
            buildDst.WriteLine("<Include>");
            buildDst.WriteLine(String.Format("<?define ProductCode = \"{{{0}}}\"?>", productCode));
            buildDst.WriteLine(String.Format("<?define ProductMajor = \"{0}\" ?>", major));
            buildDst.WriteLine(String.Format("<?define ProductMinor = \"{0}\" ?>", minor));
            buildDst.WriteLine(String.Format("<?define ProductBuild = \"{0}\" ?>", build));
            buildDst.WriteLine(String.Format("<?define ProductRevision = \"{0}\" ?>", revision));
            buildDst.WriteLine("<?define ProductVersion3 = \"$(var.ProductMajor).$(var.ProductMinor).$(var.ProductBuild)\"?>");
            buildDst.WriteLine("<?define ProductVersion4 = \"$(var.ProductMajor).$(var.ProductMinor).$(var.ProductBuild).$(var.ProductRevision)\"?>");
            buildDst.WriteLine("</Include>");

            buildDst.Close();

            // Copy the updated AssemblyInfo.cs from its temporary filename.
            // This is essentially our "commit" operation, since this file
            // is the authoritative source of Kodu version information.

            File.Delete(assemFilename);
            File.Copy(assemFilename + ".tmp", assemFilename);
            File.Delete(assemFilename + ".tmp");

            return 0;
        }
    }
}
