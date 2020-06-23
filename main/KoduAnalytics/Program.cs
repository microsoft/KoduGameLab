using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using KoduAnalytics.ProgramAnalytics;
using Boku;

namespace KoduAnalytics
{
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            RunAnalysis.loadProgram();

        }
    }
}
