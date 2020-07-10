// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoduAnalytics.ProgramAnalytics
{
    public class StateMachine
    {
        public List<State> states = new List<State>();
        public List<Transition> transitions = new List<Transition>();
        public State start;
    //    public State win;
    //    public State lose;

        /*
         * Returns a count of all connected states, including the special
         * win and lose states
         */
        public int stateCount
        {
            get
            {
                //int count = (from s in states
                //            where (s.incoming.Count() > 0 
                //            || s.outgoing.Count() > 0)
                //            select s).Count();
                int count = (from s in states where 
                                 s.used == true
                                 select s).Count();
                return count;
            }
        }
        public int statesIncoming2Plus
        {
            get
            {
                int count = (from s in states
                             where (s.incoming.Count() > 1) && s.used == true
                             select s).Count();
                return count;
            }
        }
        public int statesOutgoing2Plus
        {
            get
            {
                int count = (from s in states
                             where (s.outgoing.Count() > 1) && s.used == true
                             select s).Count();
                return count;
            }
        }
        public int getMaxFanOut
        {
            get
            {
                if (states.Count() == 1)
                {
                    return 0;
                }
                int fanout = (from s in states
                              where s.used == true
                              select s.outgoing.Count()).Max();
                return fanout;
            }
        }
        public int getMaxFanIn
        {
            get
            {
                if (states.Count() == 1)
                {
                    return 0;
                }
                int fanin = (from s in states
                             where s.used == true //s.name != "win" && s.name != "lose" && s.name != "s"
                             select s.incoming.Count()).Max();
                return fanin;
            }
        }



        public StateMachine()
        {
            start = new State("s");
       //     win = new State("win");
       //     lose = new State("lose");
            states.Add(start);
       //     states.Add(win);
       //     states.Add(lose);
        }

        public void Add(State s)
        {
            this.states.Add(s);
        }
        //adds the transition to the associated states
        public void Add(Transition t)
        {
            this.transitions.Add(t);
            t.src.outgoing.Add(t);
            t.dest.incoming.Add(t);
        }
        public State get(string name)
        {
            State state = (from s in states
                        where s.name == name
                        select s).First();
            return state;
        }

        public Boolean hasCycle()
        {
            Boolean cycle = false;
            if (states.Count() < 3)
            {
                return cycle;
            }
            State start = (from s in states
                           where s.name.Trim() == "1"
                           select s).First();
            return hasCycleStart(start);
        }

        //BFS traversal on the state machine to detect cycles
        private Boolean hasCycleStart(State tovisit)
        {
            List<String> visited = new List<string>();
            visited.Add(tovisit.name);
            int index = 0;
            String node;
            while (index < visited.Count() && index >= 0)
            {
                node = visited[index];
                State current = (from s in states
                                 where s.name.Trim() == node
                                 select s).First();
                List<Transition> trans = (from t in current.outgoing
                                          where t.visited == false
                                          select t).ToList<Transition>();
                if (trans.Count() > 0)
                {
                    Transition t = trans.First();
                    if (visited.Contains(t.dest.name))
                    {
                        return true;
                    }
                    else
                    {
                        visited.Add(t.dest.name);
                    }
                    t.visited = true;
                    index++;
                }
                else
                {
                    visited.RemoveAt(visited.Count() - 1);
                    index--;
                }
            }
            return false;
        }

    }
    public class State
    {
        public string name;
        public Boolean used = false;
        public List<Transition> outgoing = new List<Transition>();
        public List<Transition> incoming = new List<Transition>();
        public Boolean visited = false;

        public State(String n)
        {
            this.name = n;
        }
    }
    public class Transition
    {
        public State src;
        public State dest;
        public string Condition;
        public Boolean visited = false;
    }

}
