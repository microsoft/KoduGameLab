// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml;

namespace KoduAnalytics.ProgramAnalytics
{
    class TileAnalysis
    {
        public List<KoduGame> games = new List<KoduGame>();
        String filepath = RunAnalysis.directory + @"\";
        List<String> allActorTypes = new List<string>();

        /*
         * Processes the kode files from the Analytics package. Gets
         * the tile type information.
         */
        public void process()
        {
            this.readTileFile();
            this.countBlankGames();
            this.getAllTileCounts();
            getPercentTileTypes();
            allTileUsage();
            //not we have a populated structure, do something with it
            Console.WriteLine("Total Programs: " + games.Count);

        }

        /*
         * Runs all the analyses for the SIGCSE paper
         */
        public void getGameInformation()
        {
            //gets the tile count information
            this.readTileFile();

            String kodefileext = "kode.txt";
            //String analyticsfileext = "analytics.txt";
            getActorInformation(); //called once for the entire process
            String file;
            foreach (KoduGame g in games)
            {
                g.actordata = new ActorData();
                g.actordata.game = g;
                String id = g.upid;
                //file = filepath + id + analyticsfileext;
                //processAnalyticsFile(g, file);
                file = filepath + id + kodefileext;
                processKodeFile(g, file);

                //this takes a while
                //g.actordata.getActorUsageByType(allActorTypes);
            }
            calculateGameData();
            //processUnreachable();
            printGameData();

        }

        public void getActorInformation()
        {
            String fullpath = @"C:\BokuProject\Boku\KoduAnalytics\ProgramAnalytics\Strings.xml";
            
            //read the XML file to get actor names
            XmlTextReader reader = new XmlTextReader(fullpath);
            Boolean inActorNames = false;

             allActorTypes.Add("Balloon");
    //allActorTypes.Add("Tree");
    allActorTypes.Add("Tree");
    allActorTypes.Add("Kodu");
    allActorTypes.Add("Ammo");
    allActorTypes.Add("Cloud");
    allActorTypes.Add("Castle");
    allActorTypes.Add("Coin");
    allActorTypes.Add("Cycle");
    allActorTypes.Add("Ball");
    allActorTypes.Add("Daisy");
    allActorTypes.Add("Drum");
    allActorTypes.Add("Factory");
    allActorTypes.Add("Ship");
    allActorTypes.Add("Flower");
    allActorTypes.Add("Flyfish");
    allActorTypes.Add("Apple");
    allActorTypes.Add("Heart");
    allActorTypes.Add("Hut");
    allActorTypes.Add("Jet");
    allActorTypes.Add("Light");
    allActorTypes.Add("Mine");
    allActorTypes.Add("Popsy");
    allActorTypes.Add("Puck");
    allActorTypes.Add("Push Pad");
    allActorTypes.Add("Rock");
    allActorTypes.Add("Saucer");
    allActorTypes.Add("Sputnik");
    allActorTypes.Add("Star");
    allActorTypes.Add("Stick");
    allActorTypes.Add("Sub");
    allActorTypes.Add("Fish");
    allActorTypes.Add("Turtle");
    allActorTypes.Add("Cannon");
    allActorTypes.Add("Path");
    allActorTypes.Add("Blimp");
    allActorTypes.Add("Wisp");
    //allActorTypes.Add("Tree");
    //allActorTypes.Add("Tree");
    //allActorTypes.Add("Tree");
    allActorTypes.Add("plain");
    allActorTypes.Add("road");
    allActorTypes.Add("wall");
    allActorTypes.Add("flora");


            //while (reader.Read())
            //{
            //    switch (reader.NodeType)
            //    {
            //        case XmlNodeType.Element: //element
            //            switch (reader.Name)
            //            {
            //                case "actorNames":
            //                    //get the name from the attribute
            //                    while (reader.Read()) // Read the attributes.
            //                    {
            //                            // the value is the object type we want to record;
            //                            allActorTypes.Add(reader.Value);
            //                            Console.Write(reader.Value + "'");
                                    

            //                    }
            //                    break;
            //                case "XmlFile":
            //                    break;
            //                case "Classification":
            //                    break;
            //                case "ModelFile":
            //                    break;
            //                case "Group":
            //                    break;
            //                case "MenuTextureFile":
            //                    break;
            //                default:
            //                    break;
            //            }

            //            break;
            //        default: //for the XMLNodeType
            //            break;
            //    }
            //}
        }

        public void processUnreachable()
        {
            //process unreachavble file
            String file = filepath + "unreachablePages.txt";
            using (StreamReader sr = new StreamReader(file))
            {
                sr.ReadLine(); //eat first line
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    String[] parts = line.Split(' ');
                    String id = parts[2];
                    String page = parts[parts.Length - 1];
                    String actor = parts[parts.Length - 5] + " " + parts[parts.Length - 4];
                    actor = actor.Substring(0, actor.Length - 1);
                    actor.Trim();
                    var game = (from g in games
                                where g.upid == id
                                select g);
                    if (game != null)
                    {
                        KoduGame kg = (KoduGame)(game.ToList()[0]);
                        kg.unreachablePages.Add(new KeyValuePair<string, int>(actor, Convert.ToInt32(page.Trim())));
                    }

                }
                sr.Close();
            }

            //process blankjumps file
            file = filepath + "blankjumps.txt";
            using (StreamReader sr = new StreamReader(file))
            {
                sr.ReadLine();//eat the first line
                 String line;
                 while ((line = sr.ReadLine()) != null)
                 {
                     String[] parts = line.Split(' ');
                     String id = parts[2];
                     String page = parts[parts.Length - 1];
                     String actor = parts[parts.Length - 8] + " " + parts[parts.Length - 7];
                     actor = actor.Substring(0, actor.Length - 1);
                     actor.Trim();

                     var game = (from g in games
                                 where g.upid == id
                                 select g);
                     if (game != null)
                     {
                         KoduGame kg = (KoduGame)(game.ToList()[0]);
                         kg.blankJumps.Add(new KeyValuePair<string, int>(actor, Convert.ToInt32(page.Trim())));
                     }

                 }
                 sr.Close();
            }


            //summarize the frequency and object percentages
            int unreachable = (from g in games
                               where g.unreachablePages.Count() > 0
                               select g).Count();
            int total = games.Count();
            double unreachper = (double)unreachable / (double)total;
            Console.WriteLine("Unreach {0} / {1} = {2}", unreachable, total, unreachper);
            double unreachablecount = (from g in games
                                    where g.unreachablePages.Count() > 0
                                    select g.unreachablePages.Count()).Average();
            Console.WriteLine("Unreach pages per Game = {0}", unreachablecount);
            int blank = (from g in games
                         where g.blankJumps.Count() > 0
                         select g).Count();
            double blankper = (double)blank / (double)total;
            Console.WriteLine("Blank {0} / {1} = {2}", blank, total, blankper);
            double blankcount = (from g in games
                                       where g.blankJumps.Count() > 0
                                 select g.blankJumps.Count()).Average();
            Console.WriteLine("Blank pages per Game = {0}", blankcount);
        }

        public void processKodeFile(KoduGame g, String file)
        {
            StreamReader sr = new StreamReader(file);
            String line;
            g.file = sr.ReadLine(); //header
            g.title = sr.ReadLine(); // header
            g.author = sr.ReadLine();// header
            g.date = sr.ReadLine();//header

            while ((line = sr.ReadLine()) != null)
            {
                processKodeLine(g, line);
            }
            sr.Close();

            //create summary information for the game
            g.totalTiles = (from a in g.actordata.actors.Values
                           where a.allrules.Count() > 0
                           select a.tileCount).Sum();


        }

        private Actor currentactor = null;
        private Page currentpage = null;
        private int currentpagenumber = 0;

        private void processKodeLine(KoduGame g, String line)
        {
            if (line.Contains("--"))
            {
                line = line.Trim();
                //we have a rule
                Rule r = new Rule(line);
                r.Page = currentpagenumber;
                currentactor.allrules.Add(r); //add rule to actor
                currentpage.rules.Add(r); //add rule to page
            }
            else if (line.Contains("Page") && line.IndexOf("Page") < 10)
            {
                //we have a page heading
                currentpage = new Page();
                String[] parts = line.Trim().Split(' ');
                currentpagenumber = 0;
                for(int i = 1; i < parts.Length; i++) {
                    if(parts[i] != "") {
                        currentpagenumber = Convert.ToInt32(parts[i].Trim());
                        currentpage.pageNumber = currentpagenumber;
                        break;
                    }
                }
                Debug.Assert(currentpagenumber > 0, "Page Parsing Error");
                currentactor.pageNumbers.Add(currentpagenumber); //add page number to actor
                currentactor.allpages.Add(currentpage); //add page to actor
            }
            else if (line.Trim() == "")
            {
                //blank line. Eat it.
            }
            else
            {
                //we have an actor, reset page and get object
                currentpagenumber = 0;
                String name = line.Trim();
                if(g.actordata.actors.ContainsKey(name)){
                    currentactor = g.actordata.actors[name];
                }else {
                    //add actor to the list for the game
                    currentactor = new Actor(name);
                    g.actordata.actors.Add(name, currentactor);
                    //Debug.Assert(g.actordata.actors.ContainsKey(name), "Cannot find actor");
                }
            }
        }

        private void processAnalyticsFile(KoduGame g, String file)
        {
            StreamReader sr = new StreamReader(file);
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                processAnalyticsLine(g, line);
            }
            sr.Close();
        }

        private void processAnalyticsLine(KoduGame g, String line)
        {
            if (line.Contains("Actor = "))
            {
                //this is an actor line - process it
                String[] firstsplit = line.Split(',');
                String[] actorsplit = firstsplit[0].Split('=');
                String[] rulessplit = firstsplit[1].Split('=');
                Actor a = new Actor(actorsplit[1]);
                a.rulecount = Convert.ToInt32(rulessplit[1].Trim());
                while (g.actordata.actors.ContainsKey(a.id))
                {
                    a.id = a.id + "a";
                }
                g.actordata.actors.Add(a.id, a);
            }
        }

        public void allTileUsage()
        {
            modifierAnalysis(Utils.upidModifiers,   TileData.modifer);
     //       modifierAnalysis(Utils.upidActuators,   TileData.actuator);
            modifierAnalysis(Utils.upidFilters,     TileData.filter);
      //      modifierAnalysis(Utils.upidSelectors,   TileData.selector);
            modifierAnalysis(Utils.upidSensors,     TileData.sensor);
        }

        public void modifierAnalysis(string[] tiles, String type)
        {
            int[] totalcountallgames = new int[tiles.Length];
            int[] totalnumbergames = new int[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                totalcountallgames[i] = 0;
                totalnumbergames[i] = 0;
            }

            foreach (KoduGame g in games)
            {
                
                for (int i = 0; i < tiles.Length; i++)
                {
                    var found = (from s in g.tiledata.tiles
                                     where s.Key.Contains(tiles[i])
                                     select s.Value);
                    if (found.Count() > 0)
                    {
                        totalcountallgames[i] += found.First();
                        totalnumbergames[i] += 1;
                    }
                }
            } //end games foreach

            //do some pretty printing
            string s1, s2;
            TextWriter tw1 = new StreamWriter(RunAnalysis.directory + @"/" + type + "totalcountallgames.txt");
            TextWriter tw2 = new StreamWriter(RunAnalysis.directory + @"/" + type + "totalnumbergames.txt");
            for (int i = 0; i < tiles.Length; i++)
            {
                var found = (from u in Utils.upidDeprecated
                             where u.id == tiles[i]
                             select u).Take(1);
                if (found == null) //remove deprecated from output
                {
                    s1 = tiles[i] + ", " + totalcountallgames[i];
                    s2 = tiles[i] + ", " + totalnumbergames[i];
                    tw1.WriteLine(s1);
                    tw2.WriteLine(s2);
                }
            }
            tw1.Close();
            tw2.Close();

        }

        private string extractcolor(String action)
        {
            String color = "";

            if (action.Contains("yellow"))
            {
                return "yellow";
            }
            if (action.Contains("Grey"))
            {
                return "grey";
            }
            if (action.Contains("Black"))
            {
                return "black";
            }
            if (action.Contains("white"))
            {
                return "white";
            }
            if (action.Contains("red"))
            {
                return "red";
            }
            if (action.Contains("orange"))
            {
                return "orange";
            }
            if (action.Contains("green"))
            {
                return "green";
            }
            if (action.Contains("blue"))
            {
                return "blue";
            }
            if (action.Contains("purple"))
            {
                return "purple";
            }
            if (action.Contains("pink"))
            {
                return "pink";
            }
            if (action.Contains("brown"))
            {
                return "brown";
            }
            return color;
        }

        private string getScoreBucketName(String color)
        {
            return "scorebucket.color." + color;
        }

        /*
         * Looks at the rule interactions with each scorebucket, as a proxy for
         * global variables. We care here about the number of rules that read
         * versus the number of rules that write. Some rules may do both.
         */ 
        private void getScoreBucketInformation(KoduGame g)
        {
            List<KeyValuePair<string, int>> sb = (from t in g.tiledata.tiles
                                                  where t.Key.Contains("scorebucket")
                                                  select t).ToList();
            List<string> buckets = new List<string>();
            for (int i = 0; i < sb.Count(); i++)
            {
                String bucket = sb[i].Key;
                bucket = bucket.Substring(bucket.IndexOf('.')+1);
                if (!g.bucketreads.ContainsKey(bucket))
                {
                    g.bucketreads.Add(bucket, 0);
                    g.bucketwrites.Add(bucket, 0);
                    buckets.Add(bucket);
                }
                
            }
            String color;
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Rule r in a.allrules)
                {
                    if (r.Condition.Contains("sensor.scored"))
                    {
                        if ((color = extractcolor(r.Condition)) != "")
                        {
                            String bucket = getScoreBucketName(color);
                            if (!g.bucketreads.ContainsKey(bucket))
                            {
                                g.bucketreads.Add(bucket, 0);
                                g.bucketwrites.Add(bucket, 0);
                                buckets.Add(bucket);
                            }
                        }
                    }
                }
            }


            g.distinctbuckets = buckets.Count();

            Boolean contains = false;
            foreach (Actor a in g.actordata.actors.Values)
            {
                
                foreach (Rule r in a.allrules)
                {
                    contains = false;
                    foreach (string bucket in buckets)
                    {
                        if (r.Action.Contains(bucket))
                        {
                            contains = true;
                            g.bucketwrites[bucket]++;
                        }

                        if (r.Condition.Contains(bucket))
                        {
                            contains = true;
                            g.bucketreads[bucket]++;
                        }
                        else if (r.Condition.Contains("sensor.scored"))
                        {
                            if ((color = extractcolor(r.Condition)) != "")
                            {
                                String b = getScoreBucketName(color);
                                if (b == bucket)
                                {
                                    g.bucketreads[b]++;
                                    contains = true;
                               } 
                            }
                        }
                    }
                    if (contains)
                    {
                        g.bucketreadorwriterulecount++;
                    }
                }
            }
        }

        private void getLocalVariableInformation(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Rule r in a.allrules)
                {

                }
            }
        }

        private void getRandomVariableInformation(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Rule r in a.allrules)
                {
                    Boolean contains = false;
                    if (r.Action.Contains("random"))
                    {
                        g.randomAction++;
                        contains = true;
                    }
                    else if (r.Condition.Contains("random"))
                    {
                        g.randomCondition++;
                        contains = true;
                    }
                    if (contains)
                    {
                        g.randomRule++;
                    }
                }
            }
        }

        private void getNotData(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Rule r in a.allrules)
                {
                    Boolean contains = false;
                    if (r.Action.Contains(" filter.not "))
                    {
                        g.notAction++;
                        contains = true;
                    }
                    else if (r.Condition.Contains(" filter.not "))
                    {
                        g.notCondition++;
                        contains = true;
                    }
                    if (contains)
                    {
                        g.notRule++;
                    }
                }
            }
        }

        private void getTabbedData(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Rule r in a.allrules)
                {
                    if (r.Condition.Contains("tab"))
                    {
                        g.tabbedRules++;
                        //check for the type of 'and'

                        //if condition = always and action != null, we have action
                        //if condition != always, and action != null, we have cond.

                        if (r.Condition.Contains("sensor.always"))
                        {
                            if (r.actionTiles.Count() > 0)
                            {
                                g.logicalAndAction++;
                            }
                            else
                            {
                                Console.WriteLine("Check me Action: " + g.upid);
                            }
                        }
                        else
                        {
                            if (r.actionTiles.Count() > 0)
                            {
                                g.logicalAndCondition++;
                            }
                            else
                            {
                                Console.WriteLine("Check me Condition: " + g.upid);
                            }
                        }

                    }
                }
            }
        }

        private void getLogicalAndActionProxy(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Page p in a.allpages)
                {
                    for (int i = 0; i < p.rules.Count(); i++)
                    {
                        Rule current = p.rules[i];
                        if (current.conditionTiles.Contains("sensor.always"))
                        {
                            current.conditionIsAlways = true;
                        }
                        for (int j = i + 1; j < p.rules.Count(); j++)
                        {
                            Rule test = p.rules[j];
                            if (isSameSetOfStrings(current.conditionTiles, test.conditionTiles))
                            {
                                current.conditionDuplicatedInPage = true;
                                test.conditionDuplicatedInPage = true;
                            }
                        }
                    }
                    
                }
                //check for the rules that are duplicates
                int allDuplicates = (from rules in a.allrules
                                    where rules.conditionDuplicatedInPage == true
                                    select rules).Count();
                g.logicalAndConditionProxies += allDuplicates;

                int allDuplicatesNotAlways = (from rules in a.allrules
                                             where rules.conditionDuplicatedInPage == true 
                                             & rules.conditionIsAlways == false
                                             select rules).Count();
                g.logicalAndConditionProxiesNoAlways += allDuplicatesNotAlways;
            }
        }

        private void getLogicalOr(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Page p in a.allpages)
                {
                    for (int i = 0; i < p.rules.Count(); i++)
                    {
                        Rule current = p.rules[i];
                        for (int j = i + 1; j < p.rules.Count(); j++)
                        {
                            Rule test = p.rules[j];
                            if (isSameSetOfStrings(current.actionTiles, test.actionTiles))
                            {
                                current.actionDuplicatedInPage = true;
                                test.actionDuplicatedInPage = true;
                            }
                        }
                    }

                }
                //check for the rules that are duplicates
                int allDuplicates = (from rules in a.allrules
                                     where rules.actionDuplicatedInPage == true
                                     select rules).Count();
                g.logicalOr += allDuplicates;
            }
        }

        private void getConditional(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Page p in a.allpages)
                {
                    for (int i = 0; i < p.rules.Count(); i++)
                    {
                        Rule current = p.rules[i];
                        for (int j = i + 1; j < p.rules.Count(); j++)
                        {
                            Rule test = p.rules[j];
                            List<String> currentCondition = new List<string>();
                            Boolean testme = false;
                            foreach (String s in current.conditionTiles)
                            {
                                currentCondition.Add(s);
                            }
                            List<String> testCondition = new List<string>();
                            foreach (string s in test.conditionTiles)
                            {
                                testCondition.Add(s);
                            }
                            if (currentCondition.Contains("filter.not"))
                            {
                                currentCondition.Remove("filter.not");
                                testme = true;
                            }
                            else if (testCondition.Contains("filter.not"))
                            {
                                testCondition.Remove("filter.not");
                                testme = true;
                            }
                            if (testme && isSameSetOfStrings(currentCondition, testCondition))
                            {
                                current.partOfConditional = true;
                                test.partOfConditional = true;
                            }
                        }
                    }

                }
                //check for the rules that are duplicates
                int allDuplicates = (from rules in a.allrules
                                     where rules.partOfConditional == true
                                     select rules).Count();
                g.partOfConditional += allDuplicates;
            }
        }

        public void getStateMachines(KoduGame g)
        {
            int sum = 0;
            foreach (Actor a in g.actordata.actors.Values)
            {
                a.createStateMachine();
                int states = a.pagesUsed();
                //if (states == 4 && a.stateMachine.hasCycle() && 
                //    (a.stateMachine.get("win").incoming.Count() > 0
                //    || a.stateMachine.get("lose").incoming.Count() > 0))
                int used = (from s in a.stateMachine.states
                            where s.used == true
                            select s).Count();
                if(states > 1 && used > 1)
                {
                    printStateMachine(g, a);
                }
                sum += a.pagesUsed();
            }
            int actorsWithProgramming = (from a in g.actordata.actors.Values
                                         where a.allrules.Count() > 0
                                         select a).Count();
            double statesPerActor = (double)sum / (double)actorsWithProgramming;
            g.statesperactor = statesPerActor;
        }

        private void printStateMachine(KoduGame g, Actor a)
        {
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            + @"\analytics\Machines\";

            Directory.CreateDirectory(directory);
            
            directory += g.upid + "_" + a.id + ".dot";

            using (StreamWriter sw = File.CreateText(directory))
            {
                sw.WriteLine("digraph state {");
               // sw.WriteLine("rankdir=LR;");
              //  sw.WriteLine("node [shape = doublecircle]; stop;");
                sw.WriteLine("node [shape = circle];");
                foreach (Transition t in a.stateMachine.transitions)
                {
                    sw.WriteLine(t.src.name + " -> " + t.dest.name + "[ label = \"" + "\" ];");
                }
                sw.WriteLine("}");
                sw.Flush();
                sw.Close();
            }
        }


        private Boolean isSameSetOfStrings(List<string> a, List<string> b)
        {
            Boolean same = false;
            if (a.Count() != b.Count())
            {
                return same;
            }
            a.Sort();
            b.Sort();
            for (int i = 0; i < a.Count(); i++)
            {
                if (a[i] != b[i])
                {
                    return same;
                }
            }
            same = true;
            return same;
        }

        public void getControlFlowInformation(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                if (a.stateMachine.stateCount > 0)
                {
                    g.objectsWith1State++;
                }
                if (a.stateMachine.stateCount > 1)
                {
                    g.objectsWith2Plus++;
                }
                else
                {
                    continue;
                }
                if (a.stateMachine.hasCycle() == true)
                {
                    g.objectsWithCycles++;
                }
                else
                {
                    g.objectsWithLinearFlow++;
                }
                int states = a.stateMachine.stateCount;
                g.sumOfStates2Plus += states;
                int incoming = a.stateMachine.statesIncoming2Plus;
                g.sumOfStatesHighIncoming2Plus += incoming;
                int outgoing = a.stateMachine.statesOutgoing2Plus;
                g.sumOfStatesHighOutgoing2Plus += outgoing;
            }
            if (g.actordata.actors.Values.Count() > 0)
            {
                int faninmax = ((from a in g.actordata.actors.Values
                                 select a.stateMachine.getMaxFanIn).Max());
                g.maxFanIn = faninmax;
                int fanoutmax = ((from a in g.actordata.actors.Values
                                  select a.stateMachine.getMaxFanOut).Max());
                g.maxFanOut = fanoutmax;
            }
        }
        private void getHCIinformation(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Rule r in a.allrules)
                {
                    if (r.Condition.Contains("sensor.gamepad")
                        || r.Condition.Contains("sensor.keyboard") 
                        || r.Condition.Contains("sensor.mouse"))
                    {
                        g.HCItile++;
                        a.HCIcontrolled = true;
                    }
                }
            }
            int HCIactors = (from a in g.actordata.actors.Values
                             where a.HCIcontrolled == true
                             select a).Count();
            g.HCIactors = HCIactors;

        }

        private void getDoCreateInformation(KoduGame g)
        {
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Rule r in a.allrules)
                {
                    if (r.Action.Contains("actuator.make"))
                    {
                        g.docreaterules++;
                        if(r.Action.Contains("modifier.creatable.")){
                            g.docreatecreatable++;
                        }
                    }
                }
            }
        }

        private Boolean containsEmotion(String str)
        {
            Boolean contains = false;
            if (str.Contains("expresshappy")
                || str.Contains("expresssad")
                || str.Contains("expresscrazy")
                || str.Contains("expresshearts")
                || str.Contains("expressflowers")
                || str.Contains("expressstars")
                || str.Contains("expressswear")
                || str.Contains("expressnormal")
                || str.Contains("expressnone"))
            {
                contains = true;
            }
            return contains;
        }

        private void localVariables(KoduGame g)
        {
            Boolean found;
            foreach (Actor a in g.actordata.actors.Values)
            {
                foreach (Rule r in a.allrules)
                {
                    found = false;
                    //health, color, glow (color), emotion
                    if (r.Condition.Contains("sensor.health")
                        || extractcolor(r.Condition) != ""
                        || containsEmotion(r.Condition))
                    {
                        g.localvariableread++;
                        found = true;
                    }
                    if (r.Action.Contains("actuator.glow")
                        || r.Action.Contains("actuator.color")
                        || r.Action.Contains("actuator.express")
                        || r.Action.Contains("actuator.damage")
                        || r.Action.Contains("actuator.heal"))
                    {
                        g.localvariablewrite++;
                        found = true;
                    }
                    if (found)
                    {
                        g.localvariablerules++;
                    }
                }
            }
        }

        private void calculateGameData()
        {
            foreach (KoduGame g in games)
            {
                getScoreBucketInformation(g);
                getRandomVariableInformation(g);
                getNotData(g);
                getTabbedData(g);
                getLogicalAndActionProxy(g);
                getLogicalOr(g);
                getConditional(g);
                getStateMachines(g);
                getControlFlowInformation(g);
                getHCIinformation(g);
                getDoCreateInformation(g);
                localVariables(g);

                var tt = (from t in g.tiledata.tiles
                          select t.Value).Sum();
                g.totalTiles = tt;

                var dt = (from t in g.tiledata.tiles
                          select t.Key).Count();
                g.distinctTiles = dt;

                List<string> da = (List<string>)(from a in g.actordata.actors.Values
                                      select a.type).Distinct().ToList();
                g.distinctActors = da;
                if (g.actordata.numActors > 0)
                {
                    g.percentblank = (double)g.actordata.blankActorsCount / (double)g.actordata.numActors;
                }

                if (g.actordata.actors.Count() > 0)
                {
                    g.avgrules = (from a in g.actordata.actors
                                select a.Value.rulecount).Average();
                }
                var tp = (from w in g.actordata.actors.Values
                                  select w.pageNumbers.Count()).Sum();
                g.totalpages = tp;
                if (g.distinctActors.Count() > 0)
                {
                    g.avgpageobj = g.totalpages / g.distinctActors.Count();
                }


                int wa = (from w in g.actordata.actors.Values
                                  from r in w.allrules
                          where r.Condition.Contains("sensor.always") && r.Action != "Do"
                                  select r).Count();
                g.whenalways = wa;
                if (g.actordata.totalRules > 0)
                {
                    g.whenalwaysper = (double)g.whenalways / (double)g.actordata.totalRules;
                }

                //int whenalways2 = (from w in g.actordata.actors.Values
                //                   from r in w.allrules
                //                   where r.Condition.Contains("When always")
                //                   select r).Count();
            }
        }

        private void printGameData()
        {
            TextWriter tw = new StreamWriter(RunAnalysis.directory + @"/" + "games.csv");
            string delim = ", ";
            tw.WriteLine(
                "id" 
                + delim + "title" 
                + delim + "total scorebucket" + delim + "distinct scorebuckets"
                + delim + "total tiles" + delim + "distinct tiles" 
                + delim + "avgreads bucket" + delim + "avgwrites bucket" 
                + delim + "total actors" + delim + "distinct actors"
                + delim + "blank actors" //+ delim + "% blank actors"
                + delim + "total rules"  + delim + "rules per actor"
                + delim + "total pages" + delim + "pages per actor"
                + delim + "when always" + delim + "% when always"
                + delim + "random action" + delim + "random Condition" + delim + "random rule"
                + delim + "not action" + delim + "not Condition" + delim + "not rule"
                + delim + "tabbed rules"
                + delim + "and condition" + delim + "and action"
                + delim + "duplicated conditions" + delim + "duplicate cond not always"
                + delim + "logical or"
                + delim + "conditionalIf"
                + delim + "statesperactor"
                + delim + "obj1state" + delim + "objt2state" + delim + "objlinear"
                + delim + "objcycle" + delim + "states2plus" + delim + "objhighin" + delim + "objhighout"
                + delim + "maxfanin" + delim + "maxfanout"
                + delim + "HCItiles" + delim + "HCIactors"
                + delim + "beforeIndentation"
                + delim + "do create" + delim + "docreate creatable"
                + delim + "localrule" + delim + "localread" + delim + "localwrite"
                );
            foreach (KoduGame g in games)
            {
                tw.Write(g.upid + delim); //id
                tw.Write(g.title.Replace(" ", "") + delim);

                var sum = (from score in g.scorebucket
                          select score.Value).Sum();

                tw.Write(g.bucketreadorwriterulecount + delim); //all refs
                tw.Write(g.distinctbuckets + delim); //distinct buckets

                tw.Write(g.totalTiles + delim); //total tiles
                tw.Write(g.distinctTiles + delim); //distinct tiles

                tw.Write(g.averageReadsPerBucket().ToString() + delim);
                tw.Write(g.averageWritesPerBucket().ToString() + delim);

                tw.Write(g.actordata.numActors + delim); //total actors
                tw.Write(g.distinctActors.Count() + delim); //distinct actor types

                tw.Write((g.getBlankActors()).Count() + delim); //blank actors
                //tw.Write(g.percentblank.ToString() + delim); //% blank actors
                tw.Write(g.actordata.totalRules + delim); //total rules
                tw.Write(g.avgrules.ToString() + delim); //rules per actor
                tw.Write(g.totalpages + delim); //total pages for game
                tw.Write(g.avgpageobj.ToString() + delim); //pages per actor
                tw.Write(g.whenalways + delim); //when always conditions
                tw.Write(g.whenalwaysper.ToString() + delim); //% when always
                tw.Write(g.randomAction + delim);
                tw.Write(g.randomCondition + delim);
                tw.Write(g.randomRule + delim);
                tw.Write(g.notAction + delim);
                tw.Write(g.notCondition + delim);
                tw.Write(g.notRule + delim);
                tw.Write(g.tabbedRules + delim);
                tw.Write(g.logicalAndCondition + delim);
                tw.Write(g.logicalAndAction + delim);
                tw.Write(g.logicalAndConditionProxies + delim);
                tw.Write(g.logicalAndConditionProxiesNoAlways + delim);
                tw.Write(g.logicalOr + delim);
                tw.Write(g.partOfConditional + delim);
                tw.Write(g.statesperactor + delim);
                tw.Write(g.objectsWith1State + delim);
                tw.Write(g.objectsWith2Plus + delim);
                tw.Write(g.objectsWithLinearFlow + delim);
                tw.Write(g.objectsWithCycles + delim);
                tw.Write(g.sumOfStates2Plus + delim);
                tw.Write(g.sumOfStatesHighIncoming2Plus + delim);
                tw.Write(g.sumOfStatesHighOutgoing2Plus + delim);
                tw.Write(g.maxFanIn + delim);
                tw.Write(g.maxFanOut + delim);
                tw.Write(g.HCItile + delim);
                tw.Write(g.HCIactors + delim);
                if (g.beforeIndentation){ tw.Write("1" + delim);}
                else{ tw.Write("0" + delim);}
                tw.Write(g.docreaterules + delim);
                tw.Write(g.docreatecreatable + delim);
                tw.Write(g.localvariablerules + delim);
                tw.Write(g.localvariableread + delim);
                tw.Write(g.localvariablewrite + delim);

                tw.WriteLine(); //newline
            }
            tw.Close();

            fineEarlyLateDates();
        }

        private void fineEarlyLateDates()
        {
            DateTime first = DateTime.MaxValue;
            String firstgame = "";
            String lastgame = "";
            DateTime last = DateTime.MinValue;
            foreach (KoduGame g in games)
            {
                DateTime temp = g.creationDate;
                if (temp < first && temp > DateTime.MinValue)
                {
                    first = temp;
                    firstgame = g.upid;
                }
                if (temp > last)
                {
                    last = temp;
                    lastgame = g.upid;
                }
            }
            Console.WriteLine("First: " + first.ToLongDateString() + " " + firstgame);
            Console.WriteLine("Last: " + last.ToLongDateString() + " " + lastgame);
        }


        public void readTileFile()
        {
            String path = RunAnalysis.tileusage;

            StreamReader sr = new StreamReader(path);
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                processLine(line);
            }
            sr.Close();
        }

        private string formatdouble(double d)
        {
            if (d == 0.0)
            {
                return "0";
            }
            return d.ToString(".###");
        }
        public void getPercentTileTypes()
        {
            int total, mod, sel, sen, act, filt;
            double pmod, psel, psen, pact, pfilt;
            String delim = ", ";
            TextWriter tw = new StreamWriter(RunAnalysis.directory + @"/alltiletypecounts.txt");
            foreach (KoduGame g in games)
            {
                total = g.tiledata.totalTiles;
                mod = g.tiledata.getSumByType(TileData.modifier);
                sel = g.tiledata.getSumByType(TileData.selector);
                sen = g.tiledata.getSumByType(TileData.sensor);
                act = g.tiledata.getSumByType(TileData.actuator);
                filt = g.tiledata.getSumByType(TileData.filter);

                if (total != mod + sel + sen + act + filt)
                {
                    List<String> unclassified = g.tiledata.getNonClassifiedTiles();
                    foreach (string s in unclassified)
                    {
                        Console.WriteLine(s);
                    }
                }

                pmod = (double)mod / (double)total;
                psel = (double)sel / (double)total;
                psen = (double)sen / (double)total;
                pact = (double)act / (double)total;
                pfilt = (double)filt / (double)total;

                

                if (total > 0)
                {
                    tw.WriteLine(total + delim +
                        formatdouble(psen) + delim +
                        formatdouble(pfilt) + delim +
                        formatdouble(psel) + delim +
                        formatdouble(pact) + delim +
                        formatdouble(pfilt));
                }
            }

            tw.Close();
        }

        public void getAllTileCounts()
        {
            var maxvar = (from g in games
                       select g.tiledata.totalTiles).Max();
            int max = Convert.ToInt32(maxvar);
            TextWriter tw = new StreamWriter(RunAnalysis.directory + @"/alltilecounts.txt");
            String line;
            for (int i = 0; i <= max; i++)
            {
                var count = (from g in games
                             where g.tiledata.totalTiles == i
                             select g).Count();
                line = i + "," + count;
                if (count > 0)
                {
                    tw.WriteLine(line);
                    Console.WriteLine(line);
                }
            }
            tw.Close();
        }

        public void countBlankGames()
        {
            var blanks = (from g in games
                          where g.tiledata.totalTiles == 0
                          select g).Count();
            Console.WriteLine("Zero Tiles: " + blanks);
        }

        public void processLine(String line) 
        {
            
            String[] tokens = line.Split(',');
            KoduGame kg = new KoduGame(tokens[0]);
            TileData td = new TileData();
            for (int i = 1; i < tokens.Length; i++)
            {
                if(tokens[i].Contains(';'))
                {
                    continue;
                }
                String[] parts = tokens[i].Split(':');
                td.addTile(parts[0].Trim(), Convert.ToInt16(parts[1].Trim()));
            }
            kg.tiledata = td;
            var contains = from g in games
                           where g.upid == kg.upid
                           select g;
            if (contains.Count() == 0)
            {
                games.Add(kg); //ensures no dups
            }
        }

    }
}
