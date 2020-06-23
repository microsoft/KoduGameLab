using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BokuSetupActions;

namespace BokuCustomActionTest
{
    class Program
    {
        static void Main(string[] args)
        {
            bool netFramework = CustomActions.DetectNetFramework40();
            bool xnaFramework = CustomActions.DetectXnaFramework40();

            CustomActions.UninstallClickOnceKodu();
        }
    }
}
