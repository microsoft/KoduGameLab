// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoduAnalytics.ProgramAnalytics
{
    public class KoduGame
    {
        public string upid;
        public TileData tiledata;
        public ActorData actordata;
        public List<KeyValuePair<string, int>> scorebucket = new List<KeyValuePair<string, int>>();
        public int totalTiles = 0;
        public int distinctTiles = 0;
        public string date = "";
        public List<string> distinctActors = new List<string>();
        public double percentblank = 0.0;
        public int distinctbuckets = 0;
        public double avgrules = 0.00;
        public int totalpages = 0;
        public double avgpageobj = 0.00;
        public int whenalways = 0;
        public double whenalwaysper = 0.00;
        public string author = "";
        public string title = "";
        public string file = "";
        public int randomAction = 0;
        public int randomCondition = 0;
        public int randomRule = 0;
        public int notAction = 0;
        public int notCondition = 0;
        public int notRule = 0;
        public int tabbedRules = 0;
        public int logicalAndCondition = 0;
        public int logicalAndAction = 0;
        public int logicalAndConditionProxies = 0;
        public int logicalAndConditionProxiesNoAlways = 0;
        public int logicalOr = 0;
        public int partOfConditional = 0;
        public double statesperactor = 0.0;
        public int HCItile = 0;
        public int HCIactors = 0;
        public int maxFanIn = 0;
        public int maxFanOut = 0;
        public int localvariablerules = 0;
        public int localvariableread = 0;
        public int localvariablewrite = 0;

        public DateTime creationDate
        {
            get
            {
                //parse 
                DateTime dt = DateTime.Now;
                if (date == "")
                {
                    return dt;
                }
                String[] dateTime = date.Split(' ');
                String d = dateTime[0].Trim();
                String[] dateTokens = d.Split('/');
                dt = new DateTime(Convert.ToInt32(dateTokens[2]), 
                    Convert.ToInt32(dateTokens[0]),
                    Convert.ToInt32(dateTokens[1]));
                return dt;
            }
        }

        public int objectsWith1State = 0;
        public int objectsWith2Plus = 0;
        public int objectsWithLinearFlow = 0;
        public int objectsWithCycles = 0;
        public int sumOfStates2Plus = 0;
        public int sumOfStatesHighIncoming2Plus = 0;
        public int sumOfStatesHighOutgoing2Plus = 0;
        public int docreaterules = 0;
        public int docreatecreatable = 0;

        public Boolean beforeIndentation
        {
            get
            {
                DateTime indentation = new DateTime(2010, 3, 19);
                if (creationDate < indentation)
                {
                    return true;
                }
                return false;
            }
        }

        public Dictionary<String, int> bucketreads = new Dictionary<string, int>();
        public Dictionary<String, int> bucketwrites = new Dictionary<string, int>();
        public int bucketreadorwriterulecount = 0;
        
        //object name, page not jumped to
        public List<KeyValuePair<string, int>> unreachablePages = new List<KeyValuePair<string, int>>();
        //object name, destination
        public List<KeyValuePair<string, int>> blankJumps = new List<KeyValuePair<string, int>>();
        
        public KoduGame(String uid)
        {
            upid = uid;
          //  tiledata = new TileData();
        }

        public List<Actor> getBlankActors()
        {
            List<Actor> blanks = new List<Actor>();

            foreach (Actor a in actordata.actors.Values)
            {
                if (a.allrules.Count() == 0)
                {
                    blanks.Add(a);
                }
            }

            return blanks;
        }

        public double averageReadsPerBucket()
        {
            if (bucketreads.Count() == 0)
            {
                return 0.00;
            }
            int sum = (from b in bucketreads
                       select b.Value).Sum();
            int count = bucketreads.Count();
            double avg = (double)sum / (double)count;
            return avg;
        }
        public double averageWritesPerBucket()
        {
            if (bucketwrites.Count() == 0)
            {
                return 0.00;
            }
            int sum = (from b in bucketwrites
                       select b.Value).Sum();
            int count = bucketwrites.Count();
            double avg = (double)sum / (double)count;
            return avg;
        }
    }
}
