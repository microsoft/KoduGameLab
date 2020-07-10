// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoduAnalytics.ProgramAnalytics
{
    public class Rule
    {

        public Boolean conditionDuplicatedInPage = false;
        public Boolean actionDuplicatedInPage = false;
        public Boolean conditionIsAlways = false;
        public Boolean partOfConditional = false;


        public Rule(String s)
        {
            int when = s.Trim().IndexOf(" ");
            String reflexNumber = s.Substring(0, when);
            index = Convert.ToInt32(reflexNumber.Trim());

            s = s.Substring(when);
            string[] parts = s.Split('-');

            condition = parts[0].Trim();
            action = parts[2].Trim();

            setTiles(condition, conditionTiles);
            setTiles(action, actionTiles);
        }

        private void setTiles(String toparse, List<String> pieces)
        {
            String[] tiles = toparse.Split(' ');
            for (int i = 0; i < tiles.Count(); i++)
            {
                String temp = tiles[i].Trim();
                if (temp != "When")
                {
                    //it's a real tile!
                    if (temp != "point" && temp != "seconds" && temp != "second")
                    {
                        //sometimes a tile has many words
                        if (temp == "stick" || temp == "button")
                        {
                            tiles[i - 1] = tiles[i - 1] + temp;
                        }
                        else if (temp == "player" 
                            || temp == "page" 
                            || temp == "close"
                            || temp == "far")
                        {
                            if (i + 1 < tiles.Count())
                            {
                                temp = temp + " " + tiles[i + 1];
                                i++;
                                pieces.Add(temp);
                            }
                            else
                            {
                                pieces.Add(temp);
                            }
                        }
                        else if (temp == "play")
                        {
                            pieces.Add(temp);
                            i++;
                            for (int j = i; i < tiles.Count(); j++, i++)
                            {
                                if (tiles[j] == "once")
                                {
                                    pieces.Add(tiles[j]);
                                }
                                else
                                {
                                    if (tiles.Count() != j + 1)
                                    {
                                        if (tiles[j + 1] != "once")
                                        {
                                            //two-word sound
                                            String temp2 = tiles[j] + " " + tiles[j + 1];
                                            j++;
                                            i++;
                                            pieces.Add(temp2);
                                        }
                                    }
                                }
                            }
                        }
                        else if (temp == "create" || temp == "launch")
                        {
                            pieces.Add(temp);
                            i++;
                            for (int j = i; i < tiles.Count(); j++, i++)
                            {
                                char before = tiles[j][0];
                                char after = char.ToUpper(before);
                                if (before == after)
                                {
                                    if (tiles[j] == "Push") //push pad is special case
                                    {
                                        //this is a creatable, bind it with the next
                                        String temp2 = tiles[j] + " " + tiles[j + 1] + " " + tiles[j + 2];
                                        j += 2;
                                        i += 2;
                                        pieces.Add(temp2);
                                    }
                                    else
                                    {
                                        //this is a creatable, bind it with the next
                                        String temp2 = tiles[j] + " " + tiles[j + 1];
                                        j++;
                                        i++;
                                        pieces.Add(temp2);
                                    }
                                }
                                else
                                {
                                    pieces.Add(tiles[j]);
                                }
                            }
                        }
                        else
                        {
                            pieces.Add(temp);
                        }
                    }
                }
            }
        }


        private int index;
        private int page = 0;
        private string condition;
        private string action;

        public List<String> conditionTiles = new List<string>();
        public List<String> actionTiles = new List<string>();

        public int Page
        {
            get
            {
                return page;
            }
            set
            {
                page = value;
            }
        }
        public int Index
        {
            get
            {
                return index;
            }
        }
        public string Condition
        {
            get
            {
                return condition;
            }
        }
        public string Action
        {
            get
            {
                return action;
            }
        }
    }
}
