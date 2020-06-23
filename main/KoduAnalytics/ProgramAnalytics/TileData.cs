using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoduAnalytics.ProgramAnalytics
{
    public class TileData
    {
        public static string modifier = "modifier";
        public static string modifer = "modifer";
        public static string sensor = "sensor";
        public static string actuator = "actuator";
        public static string filter = "filter";
        public static string selector = "selector";
        
        public TileData()
        {
            tiles = new Dictionary<string, int>();
            totalTiles = 0;
        }
        public Dictionary<string, int> tiles;
        private int TotalTiles;
        public int totalTiles
        {
            get
            {
                TotalTiles = 0;
                foreach (KeyValuePair<string, int> kp in tiles)
                {
                    TotalTiles += kp.Value;
                }
                return TotalTiles;
            }
            set
            {
                TotalTiles = value;
            }
        }


        public void addTile(String s, int i)
        {
            tiles.Add(s, i);
        }

        public List<String> getDistinctModifiers()
        {
            List<String> mods = new List<string>();

            foreach (KeyValuePair<string, int> kp in tiles)
            {
                if (kp.Key.Contains("modifier") || kp.Key.Contains("modifer"))
                {
                    if (!mods.Contains(kp.Key))
                    {
                        mods.Add(kp.Key);
                    }
                }
            }

            return mods;
        }

        public int getSumByType(String type)
        {
            var found = (from t in tiles
                         where t.Key.Contains(type)
                         select t.Value).Sum();
            return found;
        }



        public List<String> getTileType(String type)
        {
            List<String> sensors = new List<string>();

            if (type == "modifier")
            {
                var found = (from kp in tiles
                             where kp.Key.Contains(TileData.modifier)
                            || kp.Key.Contains(TileData.modifer)
                             select kp.Key);
                foreach (var item in found)
                {
                    sensors.Add(item);
                }
            }
            else
            {
                var found = (from kp in tiles
                             where kp.Key.Contains(type)
                             select kp.Key);
                foreach (var item in found)
                {
                    sensors.Add(item);
                }
            }
            return sensors;
        }

        public List<String> getDistinctTileType(String type)
        {
            List<String> sensors = new List<string>();

            if (type == "modifier")
            {
                var found = (from kp in tiles
                             where kp.Key.Contains(TileData.modifier) 
                            || kp.Key.Contains(TileData.modifer)
                             select kp.Key).Distinct();
                foreach (var item in found)
                {
                    sensors.Add(item);
                }
            }
            else
            {
                var found = (from kp in tiles
                             where kp.Key.Contains(type)
                             select kp.Key).Distinct();
                foreach (var item in found)
                {
                    sensors.Add(item);
                }
            }
            return sensors;
        }

        public List<String> getNonClassifiedTiles()
        {
            List<String> sensors = new List<string>();

            var found = (from kp in tiles
                         where !kp.Key.Contains(TileData.sensor) 
                         && !kp.Key.Contains(TileData.filter) 
                         && !kp.Key.Contains(TileData.selector) 
                         && !kp.Key.Contains(TileData.actuator) 
                         && !kp.Key.Contains(TileData.modifier)
                         && !kp.Key.Contains(TileData.modifer)
                         select kp.Key).Distinct();

            foreach (var item in found)
            {
                sensors.Add(item);
            }
            return sensors;
        }
    }
}
