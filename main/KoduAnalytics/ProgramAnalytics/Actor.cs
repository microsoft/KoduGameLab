using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoduAnalytics.ProgramAnalytics
{
    public class Actor
    {
        public Boolean counted = false;
        public List<Rule> allrules = new List<Rule>();
        public List<Page> allpages = new List<Page>();
        public StateMachine stateMachine;
        public Boolean HCIcontrolled = false;

        public void createStateMachine()
        {
            stateMachine = new StateMachine();

            if (allrules.Count() == 0)
            {
                return;
            }

            //add all states
            for(int i = 1; i <= 12; i++)
            {
                State s = new State(i.ToString());
                int pages = (from p in allpages
                                    where p.pageNumber == i
                                    select p).Count();
                if (pages > 0)
                {
                    s.used = true;
                }
                stateMachine.Add(s);
            }

            State one = stateMachine.get("1");
            Transition begin = new Transition();
            begin.Condition = "epsilon";
            begin.src = stateMachine.start;
            begin.dest = one;
            stateMachine.Add(begin);


            //add appropriate transitions
            foreach (Rule r in allrules)
            {
                int currentpage = r.Page;
                // if this is a transition
                if (r.actionTiles.Contains("actuator.switchtask")
                    && r.actionTiles.Count() > 1)
                {
                    //find the destination page
                    string destpage = getTaskId(r.Action);

                    //find the condition
                    String condition = r.Condition;
                    //this is a complex condition
                    Transition t = new Transition();
                    if (condition.Contains("tab"))
                    {
                        //if it's a whenalways

                        //if it's a logical and in the conditional
                        Console.WriteLine("We have a tabbed switch!");
                        t.Condition = "tabbed";
                    }
                    else
                    {
                        t.Condition = condition;
                    }    
                    State src = stateMachine.get(currentpage.ToString());
                    State dest = stateMachine.get(destpage);
                    t.src = src;
                    t.dest = dest;
                    stateMachine.Add(t);

                }
                ////look for transitions to terminal state here
                //if (r.Action.Contains("actuator.gameover") 
                //    || r.Action.Contains("actuator.victory"))
                //{
                //    Transition t = new Transition();
                //    if (r.Condition.Contains("tab"))
                //    {
                //        //if it's a whenalways

                //        //if it's a logical and in the conditional
                //        Console.WriteLine("We have a tabbed switch!");
                //        t.Condition = "tabbed";
                //    }
                //    else
                //    {
                //        t.Condition = r.Condition;
                //    }
                //    State src = stateMachine.get(currentpage.ToString());
                //    t.src = src;
                //    if(r.Action.Contains("actuator.gameover")){
                //        State dest = stateMachine.lose;
                //        t.dest = dest;
                //    }else {
                //        State dest = stateMachine.win;
                //        t.dest = dest;
                //    }
                //    stateMachine.Add(t);
                //}

            }
        }

        public int pagesUsed()
        {
            int used = 0;

            used = (from s in stateMachine.states
                   where (s.incoming.Count() > 0 || s.outgoing.Count() > 0) 
                   && s.name != "win" && s.name != "lose"
                   select s).Count();

            return used;
        }

        private string getTaskId(String action)
        {
            string taskid = "0";
            if (action.Contains("modifier.taska"))
            {
                taskid = "1";
            }
            else if (action.Contains("modifier.taskb"))
            {
                taskid = "2";
            }
            else if (action.Contains("modifier.taskc"))
            {
                taskid = "3";
            }
            else if (action.Contains("modifier.taskd"))
            {
                taskid = "4";
            }
            else if (action.Contains("modifier.taske"))
            {
                taskid = "5";
            }
            else if (action.Contains("modifier.taskf"))
            {
                taskid = "6";
            }
            else if (action.Contains("modifier.taskg"))
            {
                taskid = "7";
            }
            else if (action.Contains("modifier.taskh"))
            {
                taskid = "8";
            }
            else if (action.Contains("modifier.taski"))
            {
                taskid = "9";
            }
            else if (action.Contains("modifier.taskj"))
            {
                taskid = "10";
            }
            else if (action.Contains("modifier.taskk"))
            {
                taskid = "11";
            }
            else  if (action.Contains("modifier.taskl"))
            {
                taskid = "12";
            }

            return taskid;
        }

        public int tileCount
        {
            get
            {
                int counta = (from a in allrules
                             select a.actionTiles.Count()).Sum();
                int countc = (from c in allrules
                              select c.conditionTiles.Count()).Sum();
                return counta + countc;
            }
        }
        public List<int> pageNumbers;
        public Actor(String i)
        {
            this.id = i.Trim();
            allrules = new List<Rule>();
            pageNumbers = new List<int>();
        }
        public String id;
        public String type
        {
            get{
                if(id.Contains("Push Pad")){
                    return "Push Pad";
                }else {
                    string[] parts = id.Split(' ');
                    return parts[0];
                }
            }
        }
        private int rules = 0;
        public int rulecount
        {
            get
            {
                return rules;
            }
            set
            {
                rules = value;
            }
        }
    }
}
