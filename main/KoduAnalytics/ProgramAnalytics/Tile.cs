using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoduAnalytics.ProgramAnalytics
{
    public class Tile
    {
        public Tile(String i, String t, int UI)
        {
            id = i;
            UIlevel = UI;
            type = t;
        }
        public String id;
        public int UIlevel;
        public String type;

    }
}
