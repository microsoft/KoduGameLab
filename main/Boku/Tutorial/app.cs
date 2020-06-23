using System;
using System.Collections.Generic;
using System.Text;

namespace Boku.Tutorial
{
    /// <summary>
    /// This singleton class wraps and exposes application information in a very easy 
    /// Tutorial interface.  
    /// </summary>
    public class App
    {
        private App()
        {
        }
        public static App Instance = new App();

        /// <summary>
        /// This property returns a string that describes the current UI mode.  
        /// All UI modes are unique.  These are defined by the CommandMap id; 
        /// either from the dummy command map created in code or the command maps 
        /// loaded from the input XML files.  In some cases due to different Input systems, 
        /// it is required to call the Sim.UiMode to get the correct mode.  
        /// 
        /// </summary>
        public string UiMode
        {
            get
            {
                Boku.Input.CommandMap map = Boku.Input.CommandStack.Peek(0);
                if (map != null)
                {
                    return map.name;
                }
                return null;
            }
        }
    }
}
