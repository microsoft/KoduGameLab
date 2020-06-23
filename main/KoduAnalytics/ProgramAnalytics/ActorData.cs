using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace KoduAnalytics.ProgramAnalytics
{
    public class ActorData
    {
        public KoduGame game;
        public Dictionary<String, Actor> actors;
        public Dictionary<String, int> usage = new Dictionary<string,int>() ;
        public static string actorsfile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
           + @"\analytics" + @"\" + "actors.txt";

        public ActorData()
        {
            actors = new Dictionary<string, Actor>();
        }
        public int numActors
        {
            get
            {
                return actors.Count();
            }
        }
        public int blankActorsCount
        {
            get
            {
                var blanks = (from a in actors
                              where a.Value.rulecount == 0
                              select a).Count();
                return blanks;
            }
        }
        public List<Actor> blankActors
        {
            get
            {
                var blanks = from a in actors
                             where a.Value.rulecount == 0
                             select a.Value;
                return blanks.ToList();
            }
        }
        public int totalRules
        {
            get
            {
                var rules = (from a in actors
                             select a.Value.allrules.Count()).Sum();
                return rules;
            }
        }

        public List<Actor> getActorsByType(string type)
        {           
            List<Actor> acts = (from a in actors.Values
                      where a.type == type
                      select a).ToList();
            
            return acts;
        }

        public void markActorsByType(string type)
        {
            List<Actor> acts = getActorsByType(type);
            foreach (Actor a in acts)
            {
                a.counted = true;
            }
        }

        public void getActorUsageByType(List<String> allActorTypes)
        {

            //for each actor name, look for them in the community
            foreach (string s in allActorTypes)
            {
                int count = getActorsByType(s).Count();
                markActorsByType(s);
                usage.Add(s, count);
            }
            printActorUsageToFile(usage);

            var uncounted = from a in actors.Values
                            where a.counted == false
                            select a;
            foreach (Actor a in uncounted)
            {
                Console.WriteLine(game.upid + " " + a.id);
            }


        }

        public void printActorUsageToFile(Dictionary<String, int> actors)
        {
            var items = from s in actors
                        orderby s.Key ascending
                        select s;

            string path = actorsfile;
            if (File.Exists(path) == false)
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.Write("game, ");
                    foreach (string s in actors.Keys)
                    {
                        sw.Write(s + ", ");
                    }
                    sw.WriteLine();
                }
            }
            if (File.Exists(path) == true) //file exists, just append
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.Write(game.upid + ", ");
                    foreach (string s in actors.Keys)
                    {
                        sw.Write(actors[s] + ", ");
                    }
                    sw.WriteLine();
                    sw.Flush();
                    sw.Close();
                }
            }
        }

    }
}
