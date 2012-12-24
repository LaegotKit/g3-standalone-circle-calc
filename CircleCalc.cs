﻿using System;
using System.Collections;
using GeniePlugin.Interfaces;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Standalone_Circle_Calc
{
    public class Class1 : IPlugin
    {
        //Constant variable for the Properties of the plugin
        //At the top for easy changes.
        string _NAME = "Circle Calculator";
        string _VERSION = "3.0.3";
        string _AUTHOR = "VTCifer";
        string _DESCRIPTION = "Calculcates the circle requirments for different guilds.  It will also sort skills form highest to lowest.";

        public IHost _host;                             //Required for plugin
        public System.Windows.Forms.Form _parent;       //Required for plugin
        
        #region Circle Calc Members

        private Guilds _guild = Guilds.Commoner;        //
        private Guilds _calcGuild = Guilds.Commoner;    //Default Guild set to Commonder
        private SkillSets _skillset = SkillSets.all;    //Default is sort all
        private int _calcCircle = 0;                    //
        private bool _calculating = false;              //
        private bool _sorting = false;                  //
        private bool _parsing = false;                  //

        private bool _enabled = true;                   // enabled appears unused at the moment

        private enum Guilds
        {
            None,
            Commoner,
            Barbarian,
            Bard,
            Thief,
            Empath,
            MoonMage,
            Trader,
            Paladin,
            Ranger,
            Cleric,
            WarriorMage,
            Necromancer
        };

        private enum SkillSets
        {
            armor,
            weapons,
            magic,
            survival,
            lore,
            all,
            none
        };

        //Class Skill
        //Used for storing all skill related info
        //Used in a hashtable whose key is the name of the skill
        private class Skill
        {
            public double rank = 0;                 //Rank of the skill
        }

        //Class Sortskill
        //Used for sorting the skills for display in the Experience window
        //Used in an array list for sorting, which is fed from a hashtable
        public class Sortskill
        {
            public string name = "";    //Name of skill
            public int sortLR = 0;      //Ordered value based on Reading sort (Left to Right)
            public int sortTB = 0;      //Ordered value based on top to bottom, THEN left to right 
        }
        #endregion

        #region IPlugin Properties

        //Required for Plugin - Called when Genie needs the name of the plugin (On menu)
        //Return Value:
        //              string: Text that is the name of the Plugin
        public string Name
        {
            get { return _NAME; }
        }

        //Required for Plugin - Called when Genie needs the plugin version (error text
        //                      or the plugins window)
        //Return Value:
        //              string: Text that is the version of the plugin
        public string Version
        {
            get { return _VERSION; }
        }

        //Required for Plugin - Called when Genie needs the plugin Author (plugins window)
        //Return Value:
        //              string: Text that is the Author of the plugin
        public string Author
        {
            get { return _AUTHOR; }
        }

        //Required for Plugin - Called when Genie needs the plugin Description (plugins window)
        //Return Value:
        //              string: Text that is the description of the plugin
        //                      This can only be up to 200 Characters long, else it will appear
        //                      "truncated"
        public string Description
        {
            get { return _DESCRIPTION; }
        }

        //Required for Plugin - Called when Genie needs disable/enable the plugin (Plugins window,
        //                      or when Gneie needs to know the status of the plugin (???)
        //Get:
        //      Not Known what it is used for
        //Set:
        //      Used by Plugins Window 
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }

        }

        #endregion

        #region IPlugin Methods

        //Required for Plugin - Called on first load
        //Parameters:
        //              IHost Host:  The host (instance of Genie) making the call
        public void Initialize(IHost Host)
        {
            //Set Decimal Seperator to a period (.) if not set that way
            if (System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator != ".")
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            }

            //Set _host variable to the Instance of Genie that started the plugin (so can call host API commands)
            _host = Host;

            //Set Genie Variables if not already set
            if (_host.get_Variable("CircleCalc.Display") == "")
                _host.SendText("#var CircleCalc.Display 0");
            if (_host.get_Variable("CircleCalc.Sort") == "")
                _host.SendText("#var CircleCalc.Sort 0");
            if (_host.get_Variable("CircleCalc.GagFunny") == "")
                _host.SendText("#var CircleCalc.GagFunny 0");

        }

        //Required for Plugin - Called when user enters text in the command box
        //Parameters:
        //              string Text:  The text the user entered in the command box
        //Return Value:
        //              string: Text that will be sent to the game
        public string ParseInput(string Text)
        {
            //User asking for help with commands 
            if (Text == "/cc ?" || Text == "/calc ?")
            {
                DisplaySyntax();
                return "";
            }

            //Start Calculating circle
            if (Text.StartsWith("/calc"))
            {
                //Clean Input of leading/trailing whitespace
                Text = Text.Trim();

                string guild = "";
                Regex exp = new Regex(" ");
                int space = exp.Matches(Text).Count;
                //check for proper syntax (more than two spaces = bad syntax)
                if (space > 2)
                {
                    DisplaySyntax();
                    return "";
                }
                //If there is at least one space, means guild or circle, or both are on the line
                if (Text.Contains(" "))
                {
                    try
                    {
                        //circle should always be at the end, unless only guild specified
                        //if only guild is specified, should throw an exception to be caught later
                        _calcCircle = Convert.ToInt32(Text.Substring(Text.LastIndexOf(" "), Text.Length - Text.LastIndexOf(" ")));
                        //circle over 500 or under 2 are not supported
                        if (_calcCircle > 500)
                        {
                            _host.SendText("#echo");
                            _host.SendText("#echo Circle Calculator: maximum circle is 500");
                            return "";
                        }
                        else if (_calcCircle < 2)
                        {
                            _host.SendText("#echo");
                            _host.SendText("#echo Circle Calculator: minimum circle is 2");
                            return "";
                        }
                        //if two spaces, then guild is also included
                        if (space == 2)
                        {
                            //read guild from the line and convert it to a Guilds type (class Guilds)
                            guild = Text.Substring(Text.IndexOf(" ") + 1, Text.LastIndexOf(" ") - Text.IndexOf(" ") - 1);
                            _calcGuild = GetGuild(guild);

                            //commoner not a supported guild to calc agaisnt nor is none
                            if (_calcGuild == Guilds.Commoner || _calcGuild == Guilds.None)
                            {
                                DisplaySyntax();
                                return "";
                            }

                            //set Calculating to tue, used in parsing 
                            _calculating = true;
                            //Sends exp 0 to get all skills with ranks
                            Text = "exp 0";
                            return Text;
                        }

                        //set Calculating to tue, used in parsing
                        _calculating = true;
                        //Sends info to get the guild to calculate against
                        Text = "info";
                        return Text;
                    }
                    //catch the thrown exception if trying to convert text to a number
                    //means guild is at end and not a circle
                    catch (Exception ex)
                    {
                        //if last item is a guild, and there is more than one space, syntax is wrong
                        if (space > 1)
                        {
                            DisplaySyntax();
                            return "";
                        }


                        //get the guild from the line to calc against
                        guild = Text.Substring(Text.IndexOf(" ") + 1, Text.Length - 1 - Text.IndexOf(" "));
                        _calcGuild = GetGuild(guild);

                        //Invalid guilds to specify: Commoner and None
                        if (_calcGuild == Guilds.Commoner || _calcGuild == Guilds.None)
                        {
                            DisplaySyntax();
                            return "";
                        }
                        //set Calculating to tue, used in parsing
                        _calculating = true;
                        //Sends exp 0 to get all skills with ranks
                        Text = "exp 0";
                        return Text;
                    }
                }
                else
                {
                    //set Calculating to tue, used in parsing
                    _calculating = true;
                    Text = "info";
                    //Sends info to get the guild to calculate against
                    return Text;
                }
            }
            //start sorting skills
            if (Text.StartsWith("/sort"))
            {
                
                //clear leading/trailing spaces
                Text = Text.Trim();
                _skillset = SkillSets.all;
                int _calcRank = 1;
                Regex exp = new Regex(" ");
                int space = exp.Matches(Text).Count;
                //check for proper syntax (more than two spaces = bad syntax)
                if (space > 2)
                {
                    DisplaySyntax();
                    return "";
                }


                //if there is a space, means there is something after /sort (either skillset or rank or both)
                if (Text.Contains(" "))
                {
                    try
                    {
                        //rank should always be last, unless it is not specified
                        _calcRank = Convert.ToInt32(Text.Substring(Text.LastIndexOf(" "), Text.Length - Text.LastIndexOf(" ")));
                        
                        //Min skill needs to be at least 1
                        if ( _calcRank < 1)
                        {
                            DisplaySyntax();
                            return "";
                        }
                        //if two spaces, then skillset is also included
                        if (space == 2)
                        {
                            //read skillset from the line and convert it to a skillset type (enum _Skillset)
                            string skillset = Text.Substring(Text.IndexOf(" ") + 1, Text.LastIndexOf(" ") - Text.IndexOf(" ") - 1);
                            _skillset = GetSkillSet(skillset);
                            if (_skillset == SkillSets.none)
                            {
                                DisplaySyntax();
                                return "";
                            }

                            Text = "exp " + _skillset.ToString() + " " + _calcRank.ToString();
                            _sorting = true;
                            return Text;
                        }

                        Text = "exp " + _calcRank.ToString();
                        _sorting = true;
                        return Text;

                    }
                    //catch the thrown exception if trying to convert text to a number
                    //means skillset should be at the end of the line
                    catch (Exception ex)
                    {
                        //if last item is not a number, and there is more than one spce, syntax is wrong
                        if(space > 1)
                        {
                            DisplaySyntax();
                            return "";
                        }

                        string skillset = Text.Substring(Text.IndexOf(" ") + 1, Text.Length - 1 - Text.IndexOf(" "));
                        _skillset = GetSkillSet(skillset);
                        if (_skillset == SkillSets.none)
                        {
                            DisplaySyntax();
                            return "";
                        }

                        Text = "exp " + _skillset.ToString() + " all";
                        _sorting = true;
                        return Text;
                    }
                }
                else
                {
                    Text = "exp " + _skillset.ToString() + " all";
                    _sorting = true;
                    return Text;
                }
            }
            //means no special arguments, send command on to game
            return Text;
        }

        private void DisplaySyntax()
        {
            _host.SendText("#echo");
            _host.SendText(@"#echo Standalone Circle Calculator(Ver:" + _VERSION + ") Usage:");
            _host.SendText(@"#echo /calc [guild] [circle]");
            _host.SendText(@"#echo """"    """" /calc (will calculate to one circle above you)");
            _host.SendText(@"#echo """"    """" /calc <guild> (will calculate based on the guild you input)");
            _host.SendText(@"#echo """"    """" /calc <circle> (will calculate what you need for the circle you input)");
            _host.SendText(@"#echo """"    """" /calc <guild> <circle> (combination of the two above)");
            _host.SendText(@"#echo """"  """" The guild name must be spelled out completely, but with no spaces(moonmage, warriormage).");
            _host.SendText(@"#echo /sort [skillset] [rank]");
            _host.SendText(@"#echo """"    """" /sort will sort your all sills");
            _host.SendText(@"#echo """"    """" /sort <skillset> will sort the skills in the skillset");
            _host.SendText(@"#echo """"    """" /sort <rank> will sort the skills greather than rank");
            _host.SendText(@"#echo """"    """" /sort <skillset> <rank> will sort the skills in the skillset");
            _host.SendText(@"#echo """"    """" <rank> must always be a positive integer");
        }

        //Required for Plugin - 
        //Parameters:
        //              string Text:  That DIRECT text comes from the game (non-"xml")
        //Return Value:
        //              string: Text that will be sent to the to the windows as if from the game
        public string ParseText(string Text, string Window)
        {
            try
            {
                if (_host != null)
                {
                    if (_calculating == true && Text.StartsWith("Name: ") && Text.Contains("Guild: "))
                    {
                        switch (Text.Substring(Text.IndexOf("Guild: ") + 7).Trim())
                        {
                            case "Barbarian":
                                _guild = Guilds.Barbarian;
                                break;
                            case "Bard":
                                _guild = Guilds.Bard;
                                break;
                            case "Moon Mage":
                                _guild = Guilds.MoonMage;
                                break;
                            case "Thief":
                                _guild = Guilds.Thief;
                                break;
                            case "Empath":
                                _guild = Guilds.Empath;
                                break;
                            case "Trader":
                                _guild = Guilds.Trader;
                                break;
                            case "Ranger":
                                _guild = Guilds.Ranger;
                                break;
                            case "Cleric":
                                _guild = Guilds.Cleric;
                                break;
                            case "Warrior Mage":
                                _guild = Guilds.WarriorMage;
                                break;
                            case "Necromancer":
                                _guild = Guilds.Necromancer;
                                break;
                            case "Paladin":
                                _guild = Guilds.Paladin;
                                break;
                            case "Commoner":
                                _guild = Guilds.Commoner;
                                break;
                            default:
                                _guild = Guilds.Commoner;
                                break;
                        }
                        _calcGuild = _guild;
                        _host.SendText("exp 0");
                    }


                    if ((_calculating == true || _sorting == true) && _parsing == true)
                    {

                        if (Text.StartsWith("EXP HELP for more information"))
                        {
                            _parsing = false;
                            try
                            {
                                if (_calculating)
                                    CalculateCircle();
                                if (_sorting)
                                    SortSkills();
                            }
                            catch (Exception ex)
                            {
                                _host.SendText("#echo " + ex.ToString());
                            }
                        }
                        else if (Text.Contains("%"))
                        {
                            int i = Text.IndexOf("%");
                            string part = Text.Substring(0, i + 15).Trim();
                            ParseExperience(part);
                            part = Text.Substring(i + 23).Trim();
                            if (part.Contains("%"))
                            {
                                i = part.Contains("(") ? part.IndexOf("(") : part.Length;
                                part = part.Substring(0, i);
                                ParseExperience(part);
                            }
                        }

                    }
                    else if ((_sorting || _calculating) && Text.StartsWith("Circle: "))
                        _parsing = true;
                }
            }
            catch (Exception ex)
            {
            }
            return Text;
        }

        //Required for Plugin - 
        //Parameters:
        //              string Text:  That "xml" text comes from the game
        public void ParseXML(string XML)
        {
        }

        //Required for Plugin - Opens the settings window for the plugin
        public void Show()
        {
            OpenSettingsWindow(_host.ParentForm);
        }

        public void VariableChanged(string Variable)
        {

        }

        public void ParentClosing()
        {
        }

        public void OpenSettingsWindow(System.Windows.Forms.Form parent)
        {
            Form1 form = new Form1(ref _host);

            if (_host.get_Variable("CircleCalc.Display") == "1")
                form.Post200Circle.Checked = true;
            else if(_host.get_Variable("CircleCalc.Display") == "2")
                form.NextCircle.Checked=true;
            else
                form.Normal.Checked = true;

            if (_host.get_Variable("CircleCalc.GagFunny") == "1")
                form.chkGag.Checked = true;
            else
                form.chkGag.Checked = false;
            if (_host.get_Variable("CircleCalc.Sort") == "1")
                form.cboSort.Text = "Bottom";
            else
                form.cboSort.Text = "Top";

                if (parent != null)
                    form.MdiParent = parent;

            form.Show();
        }

        #endregion

        #region Custom Parse/Display methods

        private int GetLearningRateInt(string skillRate)
        {
            switch (skillRate)
            {
                case "clear":
                    return 0;
                case "dabbling":
                    return 1;
                case "perusing":
                    return 2;
                case "learning":
                    return 3;
                case "thoughtful":
                    return 4;
                case "thinking":
                    return 5;
                case "considering":
                    return 6;
                case "pondering":
                    return 7;
                case "ruminating":
                    return 8;
                case "concentrating":
                    return 9;
                case "attentive":
                    return 10;
                case "deliberative":
                    return 11;
                case "interested":
                    return 12;
                case "examining":
                    return 13;
                case "understanding":
                    return 14;
                case "absorbing":
                    return 15;
                case "intrigued":
                    return 16;
                case "scrutinizing":
                    return 17;
                case "analyzing":
                    return 18;
                case "studious":
                    return 19;
                case "focused":
                    return 20;
                case "very focused":
                    return 21;
                case "engaged":
                    return 22;
                case "very engaged":
                    return 23;
                case "cogitating":
                    return 24;
                case "fascinated":
                    return 25;
                case "captivated":
                    return 26;
                case "engrossed":
                    return 27;
                case "riveted":
                    return 28;
                case "very riveted":
                    return 29;
                case "rapt":
                    return 30;
                case "very rapt":
                    return 31;
                case "enthralled":
                    return 32;
                case "nearly locked":
                    return 33;
                case "mind lock":
                    return 34;
                default:
                    return 0;
            }

        }

        private void ParseExperience(string line)
        {
            if (System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator != ".")
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            }

            string name = "";

            //End of name is ':'
            int i = line.IndexOf(":");
            //If no :, no name, return.
            if (i == -1) return;
            //name is from the trimed version, from 0 - i(trim remvoes leading/trailing spaces)
            name = line.Substring(0, i).Trim();

            // Skip lines with broke names - Conny
            if (name.Contains("(")) return;

            int j = line.IndexOf("%");
            if (j == -1) return;

            string rank = line.Substring(i + 1, j - i - 1).Trim();

            //DR uses a space for the decimal seperator, this replaces the space with a decimal
            rank = rank.Replace(" ", System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            //Gets loc of Decimal Seperator
            int k = rank.IndexOf(System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            //If K is 0 or positive, a decimal was found
            if (k > -1)
            {
                //
                if (rank.Substring(k + 1).Length == 3)
                {
                    rank = rank.Substring(0, k + 1) + rank.Substring(k + 2);
                }
            }

            //Converts string rank to a double
            double dRank = Double.Parse(rank);

            if (_calculating)
                _calcSkillList.Add(name, Convert.ToInt32(Math.Floor(dRank)));
            if (_sorting)
                _sortSkillList.Add(name, dRank);
        }

        #endregion

        #region Circle Calculator/Skill Sorter

        private Hashtable _calcSkillList = new Hashtable();
        private Hashtable _sortSkillList = new Hashtable();
        private ArrayList reqList;
        private ArrayList sortList;
        private int totalTDPs;
        private int totalRanks;
        private int MaxRankLen;
        private int MaxDigitLen;

        private class CircleReq
        {
            public int circle;
            public string name;
            public int ranksNeeded;
            public int ranks;
            public int currentCircle;
            //constructor
            public CircleReq(int c, int cc, int rn, string n, int r)
            {
                circle = c;
                ranksNeeded = rn;
                name = n;
                ranks = r;
                currentCircle = cc;
            }


        }
        private class SkillRanks
        {
            public double rank;
            public string name;

            public SkillRanks(double r, string n)
            {
                rank = r;
                name = n;
            }
        }

        private class ReqComparer : IComparer
        {

            public int Compare(object x, object y)
            {
                CircleReq req1 = (CircleReq)x;
                CircleReq req2 = (CircleReq)y;
                return req1.currentCircle.CompareTo(req2.currentCircle);
            }
        }
        private class ReqComparerBottom : IComparer
        {
            public int Compare(object x, object y)
            {
                CircleReq req1 = (CircleReq)y;
                CircleReq req2 = (CircleReq)x;
                return req1.currentCircle.CompareTo(req2.currentCircle);
            }
        }
        private class RankComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                SkillRanks req1 = (SkillRanks)x;
                SkillRanks req2 = (SkillRanks)y;
                return req2.rank.CompareTo(req1.rank);
            }
        }

        private Guilds GetGuild(string guild)
        {
            switch (guild.ToLower())
            {

                case "barbarian":
                case "barbaria":
                case "barbari":
                case "barbar":
                case "barba":
                case "barb":
                    return Guilds.Barbarian;
                case "bard":
                    return Guilds.Bard;
                case "moonmage":
                case "moonmag":
                case "moonm":
                case "moon":
                case "mm":
                case "mmage":
                    return Guilds.MoonMage;
                case "thief":
                case "thie":
                    return Guilds.Thief;
                case "empath":
                case "empat":
                case "empa":
                case "path":
                    return Guilds.Empath;
                case "trader":
                case "trade":
                case "trad":
                    return Guilds.Trader;
                case "paladin":
                case "paladi":
                case "palad":
                case "pala":
                    return Guilds.Paladin;
                case "ranger":
                case "range":
                case "rang":
                    return Guilds.Ranger;
                case "cleric":
                case "cleri":
                case "cler":
                    return Guilds.Cleric;
                case "necromancer":
                case "necromance":
                case "necroman":
                case "necroma":
                case "necrom":
                case "necro":
                case "necr":
                    return Guilds.Necromancer;
                case "commoner":
                case "commone":
                case "common":
                case "commo":
                case "comm":
                    return Guilds.Commoner;
                case "warriormage":
                case "warriormag":
                case "warriorm":
                case "wmage":
                case "wm":
                    return Guilds.WarriorMage;
                default:
                    return Guilds.None;
            }

        }

        private SkillSets GetSkillSet(string skillset)
        {
            switch (skillset.ToLower())
            {
                case "armor":
                case "armo":
                case "arm":
                    return SkillSets.armor;
                case "weapons":
                case "weapon":
                case "weapo":
                case "weap":
                case "wea":
                    return SkillSets.weapons;
                case "magic":
                case "magi":
                case "mag":
                    return SkillSets.magic;
                case "survival":
                case "surviva":
                case "surviv":
                case "survi":
                case "surv":
                case "sur":
                    return SkillSets.survival;
                case "lore":
                case "lor":
                    return SkillSets.lore;
                case "all":
                    return SkillSets.all;
                default:
                    return SkillSets.none;
            }
        }
        
        private void ShowReqs()
        {
            int circle;
            bool LineBreak = false;
            _calcCircle = 0;

            if (_host.get_Variable("CircleCalc.Sort") == "0")
                circle = ((CircleReq)reqList[0]).circle;
            else
                circle = ((CircleReq)reqList[reqList.Count-1]).circle;

            //if (_host.get_Variable("CircleCalc.Sort") == "0")
                _host.SendText("#echo Requirements for Circle " + circle.ToString() + ":");
            _host.SendText("#echo");

            foreach (CircleReq req in reqList)
            {
                if (((_host.get_Variable("CircleCalc.Sort") == "0" && req.circle != circle && LineBreak == false) ||
                     (_host.get_Variable("CircleCalc.Sort") == "1" && req.circle == circle && LineBreak == false)) && 
                     _host.get_Variable("CircleCalc.Display") != "2" )
                {
                    _host.SendText("#echo");
                    LineBreak = true;
                }

                if ((_host.get_Variable("CircleCalc.Display") == "1" || req.circle <= 200) && ((_host.get_Variable("CircleCalc.Display") != "2") || req.circle == circle) )
                    _host.SendText("#echo You have enough " + req.name + " for Circle " + req.currentCircle + " and need " + (req.ranksNeeded - req.ranks).ToString() + " (" + req.ranksNeeded + ") ranks for Circle " + req.circle);
            }
            /*
            if (_host.get_Variable("CircleCalc.Sort") == "1")
            {
                _host.SendText("#echo");
                _host.SendText("#echo Requirements for Circle " + circle.ToString() + ".");
            }
            */

            _host.SendText("#echo");
            _host.SendText("#echo TDPs Gained: " + String.Format("{0,6}", totalTDPs.ToString()));
            _host.SendText("#echo Total Ranks: " + String.Format("{0,6}", totalRanks.ToString()));
            if (_host.get_Variable("CircleCalc.GagFunny") != "1")
            {
                int seed = 0;
                System.Random randomizer;
                seed = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
//                seed = Convert.ToInt32(_host.get_Variable("gametime"));
                randomizer = new System.Random(seed);
                int rand = randomizer.Next();
                switch (_calcGuild)
                {
                    case Guilds.None:
                        _host.SendText("#echo Join a guild you loser.");
                        break;
                    case Guilds.Barbarian:

                        break;
                    case Guilds.Bard:
                        if (rand % 2 == 0)
                            _host.SendText("#echo P.S. Bards suck(even with your poorly designed screams). Reroll.");
                        else
                            _host.SendText("#echo Shouldn't you be down at the pub?");
                        break;
                    case Guilds.Cleric:
                        _host.SendText("#echo Rezz Plz?");
                        break;
                    case Guilds.Commoner:
                        _host.SendText("#echo Join a guild you loser.");
                        break;
                    case Guilds.Empath:
                        break;
                    case Guilds.MoonMage:
                        break;
                    case Guilds.Necromancer:
                        if (rand % 2 == 0)
                            _host.SendText("#echo Don't you think it's time to give up the evil tea parties");
                        else
                            _host.SendText("#echo Sacrified enough puppies today?");
                        break;
                    case Guilds.Paladin:
                        break;
                    case Guilds.Ranger:
                        break;
                    case Guilds.Thief:
                        break;
                    case Guilds.Trader:
                        break;
                    case Guilds.WarriorMage:
                        _host.SendText("#echo WM Strategy:  10 prep TC, 20 cast area, 30 prep CL, 40 cast area, 50 goto 10");
                        break;
                    default:
                        _host.SendText("#echo Do you break everything you touch?");
                        break;
                }
            }
            _calcGuild = Guilds.None;
            _calculating = false;
        }

        private void ShowRanks()
        {
            string format = "{0," + (-MaxRankLen).ToString() + "} - {1," + (MaxDigitLen) + ":F2}";
            _host.SendText("#echo ");
            string ListText = "";
            string ParseSkill = "";
            foreach (SkillRanks sr in sortList)
            {
                ListText = "#echo \"" + String.Format(format, sr.name, sr.rank) + "\"";
                ParseSkill = "#parse " + String.Format(format, sr.name, sr.rank);
                _host.SendText(ListText);
                _host.SendText(ParseSkill);
            }

            _host.SendText("#echo");
            string TDPText = "";
            string TotalRanksText = "";
            TDPText = "#echo TDPs Gained from ";
            TotalRanksText = "#echo Total Ranks in ";
            if (_skillset == SkillSets.all)
            {
                TDPText = TDPText + _skillset.ToString() + " skillsets";
                TotalRanksText = TotalRanksText + _skillset.ToString() + " skillsets";
            }
            else
            {
                TDPText = TDPText + "the " + _skillset.ToString() + " skillset";
                TotalRanksText = TotalRanksText + "the " + _skillset.ToString() + " skillset";
            }
            TDPText = TDPText + ": " + String.Format("{0,6}", totalTDPs.ToString());
            TotalRanksText = TotalRanksText + ": " + String.Format("{0,6}", totalRanks.ToString());
            _host.SendText(TDPText);
            _host.SendText(TotalRanksText);

            _skillset = SkillSets.all;
            _sorting = false;
        }

        private void SortSkills()
        {
            sortList = new ArrayList();
            totalRanks = 0;
            totalTDPs = 0;
            MaxRankLen = 0;
            MaxDigitLen = 0;
            int ranks;
            foreach (DictionaryEntry skill in _sortSkillList)
            {
                ranks = Convert.ToInt32(Math.Floor(Convert.ToDouble(skill.Value)));
                totalTDPs += ranks * (ranks + 1) / 2;
                totalRanks += ranks;
            }
            totalTDPs = Convert.ToInt32(totalTDPs / 200);
            string skname = "";
            double skrank = 0;
            while (_sortSkillList.Count != 0)
            {
                skname = HighestSkill(_sortSkillList);
                skrank = Convert.ToDouble(_sortSkillList[skname]);
                sortList.Add(new SkillRanks(skrank, skname));
                if (skname.Length > MaxRankLen)
                    MaxRankLen = skname.Length;
                if (skrank.ToString().Length > MaxDigitLen)
                    MaxDigitLen = skrank.ToString().Length;
                _sortSkillList.Remove(skname);
            }

            RankComparer rankComparer = new RankComparer();
            sortList.Sort(rankComparer);

            ShowRanks();
            _sortSkillList.Clear();
        }

        private void CalculateCircle()
        {
            totalTDPs = 0;
            totalRanks = 0;
            foreach (DictionaryEntry skill in _calcSkillList)
            {
                int ranks = Convert.ToInt32(skill.Value);
                totalTDPs += ranks * (ranks + 1) / 2;
                totalRanks += ranks;
            }

            totalTDPs = Convert.ToInt32(totalTDPs / 200);
 
            if (_calcSkillList.Contains("Stealth"))
            {
                switch (_calcGuild)
                {
                    case Guilds.Barbarian:
                        CalculateBarbarian3_0();
                        break;
                    case Guilds.Bard:
                        CalculateBard3_0();
                        break;
                    case Guilds.Cleric:
                        CalculateCleric3_0();
                        break;
                    case Guilds.Empath:
                        CalculateEmpath3_0();
                        break;
                    case Guilds.MoonMage:
                        CalculateMoonMage3_0();
                        break;
                    case Guilds.Necromancer:
                        CalculateNecromancer3_0();
                        break;
                    case Guilds.Paladin:
                        CalculatePaladin3_0();
                        break;
                    case Guilds.Ranger:
                        CalculateRanger3_0();
                        break;
                    case Guilds.Thief:
                        CalculateThief3_0();
                        break;
                    case Guilds.Trader:
                        CalculateTrader3_0();
                        break;
                    case Guilds.WarriorMage:
                        CalculateWarriorMage3_0();
                        break;
                    case Guilds.Commoner:
                    default:
                        _host.SendText("#echo");
                        _host.SendText("#echo /calc: Try joining a guild first.");
                        return;
                }
            }
            else
            {
                switch (_calcGuild)
                {
                    case Guilds.Barbarian:
                        CalculateBarbarian();
                        break;
                    case Guilds.Bard:
                        CalculateBard();
                        break;
                    case Guilds.MoonMage:
                        CalculateMoonMage();
                        break;
                    case Guilds.Thief:
                        CalculateThief();
                        break;
                    case Guilds.Empath:
                        CalculateEmpath();
                        break;
                    case Guilds.Trader:
                        CalculateTrader();
                        break;
                    case Guilds.Paladin:
                        CalculatePaladin();
                        break;
                    case Guilds.Ranger:
                        CalculateRanger();
                        break;
                    case Guilds.Cleric:
                        CalculateCleric();
                        break;
                    case Guilds.WarriorMage:
                        CalculateWarriorMage();
                        break;
                    case Guilds.Necromancer:
                        CalculateNecromancer();
                        break;
                    case Guilds.Commoner:
                    default:
                        _host.SendText("#echo");
                        _host.SendText("#echo /calc: Try joining a guild first.");
                        return;
                }
            }
            IComparer reqCompareSort;
            if (_host.get_Variable("CircleCalc.Sort") == "1")
            {
                reqCompareSort = new ReqComparerBottom();
                reqList.Sort(reqCompareSort);

                while (((CircleReq)reqList[reqList.Count - 1]).circle == 500)
                    reqList.RemoveAt(reqList.Count - 1);
            }
            else
            {
                reqCompareSort = new ReqComparer();
                reqList.Sort(reqCompareSort);

                while (((CircleReq)reqList[0]).circle == 500)
                    reqList.RemoveAt(0);
            }

            //ReqComparer reqComparer = new ReqComparer();
            

            ShowReqs();
            _calcSkillList.Clear();

        }

        private string HighestSkill(Hashtable skills)
        {
            string skillName = "";
            double ranks = 0.0;
            foreach (DictionaryEntry skill in skills)
            {
                if (Convert.ToDouble(skill.Value) > ranks)
                {
                    skillName = skill.Key.ToString();
                    ranks = Convert.ToDouble(skill.Value);
                }
            }
            return skillName;
        }

        #region DR3.0Functions
        
        private string HighestArmor3_0(Hashtable skills)
        {
            string skillName = "";
            int ranks = 0;

            switch (_calcGuild)
            {
                case Guilds.Cleric:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Brigandine":
                            case "Chain Armor":
                            case "Light Armor":
                            case "Plate Armor":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                //case Guilds.Barbarian:
                //case Guilds.Bard:
                //case Guilds.Empath:
                //case Guilds.MoonMage:
                //case Guilds.Necromancer:
                //case Guilds.Paladin:
                //case Guilds.Ranger:
                //case Guilds.Thief:
                //case Guilds.Trader:
                //case Guilds.WarriorMage:
                default:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Brigandine":
                            case "Chain Armor":
                            case "Light Armor":
                            case "Plate Armor":
                            case "Shield Usage":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
            }

            return skillName;
        }

        private string HighestWeapon3_0(Hashtable skills)
        {
            string skillName = "";
            int ranks = 0;

            switch (_calcGuild)
            {
                case Guilds.Necromancer:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Bow":
                            case "Brawling":
                            case "Crossbow":
                            case "Heavy Thrown":
                            case "Large Blunt":
                            case "Large Edged":
                            case "Light Thrown":
                            case "Polearms":
                            case "Slings":
                            case "Small Blunt":
                            case "Staves":
                            case "Twohanded Blunt":
                            case "Twohanded Edged":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                
                //case Guilds.Barbarian:
                //case Guilds.Bard:
                //case Guilds.Cleric:
                //case Guilds.Empath:
                //case Guilds.MoonMage:
                //case Guilds.Paladin:
                //case Guilds.Ranger:
                //case Guilds.Thief:
                //case Guilds.Trader:
                //case Guilds.WarriorMage:
                default:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Bow":
                            case "Brawling":
                            case "Crossbow":
                            case "Heavy Thrown":
                            case "Large Blunt":
                            case "Large Edged":
                            case "Light Thrown":
                            case "Polearms":
                            case "Slings":
                            case "Small Blunt":
                            case "Small Edged":
                            case "Staves":
                            case "Twohanded Blunt":
                            case "Twohanded Edged":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
            }

            return skillName;
        }

        private string HighestMagic3_0(Hashtable skills)
        {
            string skillName = "";
            int ranks = 0;

            switch (_calcGuild)
            {
                case Guilds.Barbarian:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Augemntation":
                            case "Debilitation":
                            case "Warding":
                            case "Arcana":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case Guilds.Bard:
                case Guilds.MoonMage:
                case Guilds.Necromancer:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Attunement":
                            case "Arcana":
                            case "Targeted Magic":
                            case "Augmentation":
                            case "Debilitation":
                            case "Utility":
                            case "Warding":
                            case "Sorcery":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                
                //case Guilds.Cleric:
                //case Guilds.Empath:
                //case Guilds.Paladin:
                //case Guilds.Ranger:
                //case Guilds.Thief:
                //case Guilds.Trader:
                //case Guilds.WarriorMage:
                default:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Attunement":
                            case "Arcana":
                            case "Targeted Magic":
                            case "Augmentation":
                            case "Debilitation":
                            case "Utility":
                            case "Warding":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;                
            }

            return skillName;

        }

        private string HighestSurvival3_0(Hashtable skills)
        {
            string skillName = "";
            int ranks = 0;

            switch (_calcGuild)
            {
                case Guilds.Barbarian:
                case Guilds.Paladin:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Athletics":
                            case "First Aid":
                            case "Locksmithing":
                            case "Outdoorsmanship":
                            case "Perception":
                            case "Skinning":
                            case "Stealth":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                
                case Guilds.Bard:
                case Guilds.Necromancer:
                case Guilds.Trader:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Athletics":
                            case "Evasion":
                            case "First Aid":
                            case "Locksmithing":
                            case "Outdoorsmanship":
                            case "Perception":
                            case "Skinning":
                            case "Stealth":
                            case "Thievery":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case Guilds.Empath:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Athletics":
                            case "Evasion":
                            case "Locksmithing":
                            case "Outdoorsmanship":
                            case "Perception":
                            case "Skinning":
                            case "Stealth":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case Guilds.Ranger:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Athletics":
                            case "Evasion":
                            case "First Aid":
                            case "Locksmithing":
                            case "Outdoorsmanship":
                            case "Perception":
                            case "Scouting":
                            case "Skinning":
                            case "Stealth":
                            case "Thievery":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case Guilds.Thief:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Athletics":
                            case "Backstab":
                            case "Evasion":
                            case "First Aid":
                            case "Locksmithing":
                            case "Outdoorsmanship":
                            case "Perception":
                            case "Skinning":
                            case "Stealth":
                            case "Thievery":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                //case Guilds.Cleric:
                //case Guilds.MoonMage:
                //case Guilds.WarriorMage:
                default:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Athletics":
                            case "Evasion":
                            case "First Aid":
                            case "Locksmithing":
                            case "Outdoorsmanship":
                            case "Perception":
                            case "Skinning":
                            case "Stealth":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
            }

            return skillName;
        }

        private string HighestLore3_0(Hashtable skills)
        {
            string skillName = "";
            int ranks = 0;
            switch (_calcGuild)
            {
                case Guilds.Barbarian:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Alchemy":
                            case "Appraisal":
                            case "Enchanting":
                            case "Engineering":
                            case "Forging":
                            case "Mechanical Lore":
                            case "Outfitting":
                            case "Performance":
                            case "Scholarship":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case Guilds.Bard:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Alchemy":
                            case "Appraisal":
                            case "Enchanting":
                            case "Engineering":
                            case "Forging":
                            case "Mechanical Lore":
                            case "Outfitting":
                            case "Scholarship":
                            case "Tactics":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case Guilds.Empath:
                case Guilds.MoonMage:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Alchemy":
                            case "Appraisal":
                            case "Enchanting":
                            case "Engineering":
                            case "Forging":
                            case "Mechanical Lore":
                            case "Outfitting":
                            case "Performance":
                            case "Tactics":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;

                case Guilds.Trader:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Alchemy":
                            case "Enchanting":
                            case "Engineering":
                            case "Forging":
                            case "Mechanical Lore":
                            case "Outfitting":
                            case "Performance":
                            case "Scholarship":
                            case "Tactics":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                    
                //case Guilds.Cleric:
                //case Guilds.Necromancer:
                //case Guilds.Paladin:
                //case Guilds.Ranger:
                //case Guilds.Thief:
                //case Guilds.WarriorMage:
                default:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Alchemy":
                            case "Appraisal":
                            case "Enchanting":
                            case "Engineering":
                            case "Forging":
                            case "Mechanical Lore":
                            case "Outfitting":
                            case "Performance":
                            case "Scholarship":
                            case "Tactics":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
            }

            return skillName;
        }

        private void CalculateReq3_0(ref int circle, ref int currentCircle, double rank1, double rank2, double rank3, double rank4, double rank5, double rank6, double ranks, ref double ranksNeeded)
        {
            //rank1: 001-010
            //rank2: 011-030
            //rank3: 031-070
            //rank4: 071-100
            //rank5: 101-150
            //rank6: 150-200+

            int i;
            ranksNeeded = 0;
            circle = 0;
            currentCircle = 0;

            //rank1: 001-010
            for (i = 1; i <= 10; i++)
            {
                ranksNeeded += rank1;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank1) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            //rank2: 011-030
            for (i = 11; i <= 30; i++)
            {
                ranksNeeded += rank2;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank2) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            //rank3: 031-070
            for (i = 31; i <= 70; i++)
            {
                ranksNeeded += rank3;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank3) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            //rank4: 071-100
            for (i = 71; i <= 100; i++)
            {
                ranksNeeded += rank4;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank4) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            //rank5: 101-150
            for (i = 101; i <= 150; i++)
            {
                ranksNeeded += rank5;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank5) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            //rank6: 151-200(+)
            for (i = 151; i <= 500; i++)
            {
                ranksNeeded += rank6;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank6) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            circle = 500;
        }

        private void CalculateBarbarian3_0()
        {

            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard & Soft Skills:
            //          Parry   Expertise   IF      Evasion     Tactics
            //001-010:  4       4           1       3           1
            //011-030:  4       5           2       4           1
            //031-070:  4       6           3       4           2
            //071-100:  4       6           4       5           2
            //101-150:  5       6           4       6           3
            //151 +  :  13      15          10      15          8

            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq3_0(ref circle, ref currentCircle, 4, 5, 6, 6, 6, 15, Convert.ToInt32(_calcSkillList["Expertise"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Expertise", Convert.ToInt32(_calcSkillList["Expertise"])));
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList["Inner Fire"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Inner Fire", Convert.ToInt32(_calcSkillList["Inner Fire"])));
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList["Evasion"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Evasion", Convert.ToInt32(_calcSkillList["Evasion"])));
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList["Tactics"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tactics", Convert.ToInt32(_calcSkillList["Tactics"])));

            //Armor Skills:
            //          1st     2nd
            //001-010:  3       1
            //011-030:  4       2
            //031-070:  4       2
            //071-100:  5       3
            //101-150:  5       4
            //151 +  :  13      10
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd     3rd     4th
            //001-010:  4       4       2       1
            //011-030:  5       5       3       2
            //031-070:  6       6       3       2
            //071-100:  6       6       4       3
            //101-150:  6       6       5       4
            //151 +  :  15      15      13      10
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 5, 6, 6, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 5, 4, 6, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Supernatural Skills:
            //          1st     2nd
            //001-010:  1       0
            //011-030:  2       0
            //031-070:  2       2
            //071-100:  3       2
            //101-150:  3       4
            //151 +  :  8       10
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Supernatural (" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill); 
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 2, 2, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Supernatural (" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st     2nd     3rd     4th
            //001-010:  2       2       2       1
            //011-030:  2       2       2       1
            //031-070:  3       3       2       2
            //071-100:  3       3       3       2
            //101-150:  3       3       3       2
            //151 +  :  8       8       8       5
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st
            //001-010:  1
            //011-030:  1
            //031-070:  2
            //071-100:  2
            //101-150:  3
            //151 +  :  8
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateBard3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            // Hard and Soft Skills:
            //          Parry   Performance Tactics [Bardic Lore tbd]
            //001-010:  2       3           2
            //011-030:  3       3           3
            //031-070:  3       4           3
            //071-100:  4       4           4
            //101-150:  5       6           5
            //151 +  :  13      15          13
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 6, 15, Convert.ToInt32(_calcSkillList["Performance"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Performance", Convert.ToInt32(_calcSkillList["Performance"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList["Tactics"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tactics", Convert.ToInt32(_calcSkillList["Tactics"])));

            //Armor Skills:
            //          1st     
            //001-010:  2
            //011-030:  2
            //031-070:  2
            //071-100:  3
            //101-150:  3
            //151 +  :  8
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);


            //Weapon Skills:
            //          1st     2nd
            //001-010:  3       2
            //011-030:  3       3
            //031-070:  4       3
            //071-100:  4       4
            //101-150:  5       4
            //151 +  :  13      10
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd     3rd     4th     5th
            //001-010:  3       2       2       1       0
            //011-030:  3       2       2       2       0
            //031-070:  4       3       3       2       2
            //071-100:  4       4       3       3       3
            //101-150:  5       5       4       4       3
            //151 +  :  13      13      10      10      8
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);


            //Lore Skills:
            //          1st     2nd     3rd
            //001-010:  3       3       2
            //011-030:  3       3       2
            //031-070:  4       3       3
            //071-100:  4       4       3
            //101-150:  5       5       4
            //151 +  :  13      13      10
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

        }

        private void CalculateCleric3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard and Soft Skills:
            //          Shield  Parry   Augmentation    Theurgy
            //001-010:  1       2       2               3
            //011-030:  2       3       2               4
            //031-070:  2       3       3               4
            //071-100:  3       3       3               5
            //101-150:  4       4       4               6
            //151 +  :  10      10      10              15
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 4, 10, Convert.ToInt32(_calcSkillList["Shield Usage"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Shield Usage", Convert.ToInt32(_calcSkillList["Shield Usage"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList["Augmentation"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Augmentation", Convert.ToInt32(_calcSkillList["Augmentation"])));
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList["Theurgy"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Theurgy", Convert.ToInt32(_calcSkillList["Theurgy"])));

            //Armor Skills:
            //          1st
            //001-010:  2
            //011-030:  2
            //031-070:  3
            //071-100:  3
            //101-150:  4
            //151 +  :  10
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd
            //001-010:  3       0
            //011-030:  3       0
            //031-070:  4       2
            //071-100:  4       2
            //101-150:  5       3
            //151 +  :  13      8
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd     3rd     4th     5th
            //001-010:  4       4       3       0       0
            //011-030:  4       4       3       3       0
            //031-070:  5       4       4       3       3
            //071-100:  5       5       4       4       4
            //101-150:  6       6       5       5       5
            //151 +  :  15      15      13      13      13
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 5, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiar Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st     2nd     3rd     4th
            //001-010:  1       1       1       1
            //011-030:  2       1       1       1
            //031-070:  2       2       1       1
            //071-100:  3       2       2       2
            //101-150:  3       3       2       2
            //151 +  :  8       8       5       5
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd     3rd     4th
            //001-010:  2       2       1       0
            //011-030:  3       2       2       0
            //031-070:  3       3       2       2
            //071-100:  4       3       3       3
            //101-150:  5       4       3       3
            //151 +  :  13      10      8       8
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

        }

        private void CalculateEmpath3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard and Soft Skills:
            //          FA      Outdoors    Empathy     Scholar 
            //001-010:  2       1           4           3
            //011-030:  3       1           5           3
            //031-070:  3       2           6           4
            //071-100:  3       2           6           5
            //101-150:  4       2           7           5
            //151 +  :  10      5           15          13
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList["First Aid"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "First Aid", Convert.ToInt32(_calcSkillList["First Aid"])));
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 2, 5, Convert.ToInt32(_calcSkillList["Outdoorsmanship"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Outdoorsmanship", Convert.ToInt32(_calcSkillList["Outdoorsmanship"])));
            CalculateReq3_0(ref circle, ref currentCircle, 4, 5, 6, 6, 7, 15, Convert.ToInt32(_calcSkillList["Empathy"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Empathy", Convert.ToInt32(_calcSkillList["Empathy"])));
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 5, 5, 13, Convert.ToInt32(_calcSkillList["Scholarship"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scholarship", Convert.ToInt32(_calcSkillList["Scholarship"])));

            //Magic Skills:
            //          1st     2nd     3rd     4th     5th
            //001-010:  3       2       2       0       0
            //011-030:  3       3       3       2       0
            //031-070:  4       3       3       3       3
            //071-100:  4       4       4       3       3
            //101-150:  5       5       4       4       4
            //151 +  :  13      13      10      10      10
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st     2nd     3rd     4th     5th
            //001-010:  1       1       1       1       1
            //011-030:  2       2       1       1       1
            //031-070:  2       2       2       1       1
            //071-100:  3       3       3       2       2
            //101-150:  4       4       3       2       2
            //151 +  :  10      10      8       5       5
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiar Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd     3rd
            //001-010:  3       2       2
            //011-030:  3       3       2
            //031-070:  4       3       3
            //071-100:  4       4       3
            //101-150:  5       4       4
            //151 +  :  13      10      10
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Seconday Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

        }

        private void CalculateMoonMage3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard and Soft Skills:
            //          Astro   Scholarship
            //001-010:  2       3
            //011-030:  3       3
            //031-070:  3       3
            //071-100:  4       4
            //101-150:  5       4
            //151 +  :  15      10
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 15, Convert.ToInt32(_calcSkillList["Astrology"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Astrology", Convert.ToInt32(_calcSkillList["Astrology"])));
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList["Scholarship"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scholarship", Convert.ToInt32(_calcSkillList["Scholarship"])));

            //Magic Skills:
            //          1st     2nd     3rd     4th     5th     6th     7th
            //001-010:  4       4       3       2       0       0       0
            //011-030:  4       4       4       3       3       3       0
            //031-070:  5       4       4       4       3       3       3
            //071-100:  6       5       5       5       4       4       3
            //101-150:  7       6       5       5       5       5       4
            //151 +  :  18      15      13      13      13      13      10
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 5, 6, 7, 18, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Seconday Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 4, 5, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st     2nd     3rd     4th     5th
            //001-010:  2       2       2       2       0
            //011-030:  3       3       2       2       2
            //031-070:  3       3       3       2       2
            //071-100:  4       4       4       3       3
            //101-150:  5       4       4       3       3
            //151 +  :  13      10      10      8       8
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd     3rd
            //001-010:  2       2       1
            //011-030:  3       2       2
            //031-070:  3       3       2
            //071-100:  4       3       3
            //101-150:  5       4       3
            //151 +  :  13      10      8
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateNecromancer3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard and Soft Skills:
            //          Small Edged     TM      Than
            //001-010:  1               2       3
            //011-030:  2               2       4
            //031-070:  2               3       4
            //071-100:  2               4       5
            //101-150:  2               5       6
            //151 +  :  5               13      15
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 2, 2, 5, Convert.ToInt32(_calcSkillList["Small Edged"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Small Edged", Convert.ToInt32(_calcSkillList["Small Edged"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 4, 5, 11, Convert.ToInt32(_calcSkillList["Targeted Magic"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Targeted Magic", Convert.ToInt32(_calcSkillList["Targeted Magic"])));
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList["Thanatology"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Thanatology", Convert.ToInt32(_calcSkillList["Thanatology"])));

            //Armor Skills:
            //          1st
            //001-010:  1
            //011-030:  2
            //031-070:  2
            //071-100:  2
            //101-150:  3
            //151 +  :  8
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd     3rd     4th     5th
            //001-010:  3       3       2       2       0
            //011-030:  4       3       3       3       0
            //031-070:  4       4       3       3       3
            //071-100:  5       5       4       4       4
            //101-150:  6       6       5       5       5
            //151 +  :  15      15      13      13      13
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st     2nd     3rd     4th     5th     6th     7th
            //001-010:  4       4       3       3       3       3       2
            //011-030:  4       4       4       4       4       3       3
            //031-070:  5       5       4       4       4       4       3
            //071-100:  5       5       5       5       5       4       4
            //101-150:  6       6       5       5       5       5       4
            //151 +  :  15      15      13      13      13      13      10
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 5, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 5, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd
            //001-010:  2       2
            //011-030:  2       2
            //031-070:  3       2
            //071-100:  3       3
            //101-150:  3       3
            //151 +  :  8       8
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculatePaladin3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard and Soft Skills:
            //          Defending   Shield  Parry   Evasion Scholarship Tactics
            //001-010:  3           2       3       2       1           1
            //011-030:  3           2       3       3       2           2
            //031-070:  4           3       4       3       2           3
            //071-100:  4           3       4       4       3           3
            //101-150:  5           4       5       4       3           4
            //151 +  :  13          10      13      10      8           10
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList["Defending"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Defending", Convert.ToInt32(_calcSkillList["Defending"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList["Shield Usage"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Shield Usage", Convert.ToInt32(_calcSkillList["Shield Usage"])));
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList["Evasion"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Evasion", Convert.ToInt32(_calcSkillList["Evasion"])));
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList["Scholarship"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scholarship", Convert.ToInt32(_calcSkillList["Scholarship"])));
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 3, 3, 4, 19, Convert.ToInt32(_calcSkillList["Tactics"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tactics", Convert.ToInt32(_calcSkillList["Tactics"])));

            //Armor Skills:
            //          1st     2nd
            //001-010:  4       2
            //011-030:  5       3
            //031-070:  5       3
            //071-100:  5       4
            //101-150:  6       5
            //151 +  :  15      13
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 5, 5, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd
            //001-010:  3       0
            //011-030:  4       2
            //031-070   4       3
            //071-100:  5       4
            //101-150:  5       4
            //151 +  :  13      10
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 2, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd     3rd
            //001-010:  1       1       1
            //011-030:  2       1       1
            //031-070:  2       2       1
            //071-100:  3       2       2
            //101-150:  3       3       2
            //151 +  :  8       8       5
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st     2nd     3rd     4th
            //001-010:  1       1       1       1
            //011-030:  2       1       1       1
            //031-070:  2       2       1       1
            //071-100:  3       2       2       2
            //101-150:  3       3       2       2
            //151 +  :  8       8       5       5
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd     3rd
            //001-010:  2       1       1
            //011-030:  3       2       1
            //031-070:  3       3       2
            //071-100:  4       3       2
            //101-150:  4       4       3
            //151 +  :  10      10      8
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateRanger3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard and Soft Skills:
            //          Defending   Parry   Scouting
            //001-010:  1           2       2
            //011-030:  2           2       3
            //031-070:  2           2       3
            //071-100:  3           3       4
            //101-150:  4           3       4
            //151 +  :  10          8       10
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 4, 10, Convert.ToInt32(_calcSkillList["Defending"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Defending", Convert.ToInt32(_calcSkillList["Defending"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 2, 4, 4, 10, Convert.ToInt32(_calcSkillList["Scouting"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scouting", Convert.ToInt32(_calcSkillList["Scouting"])));

            //Armor Skills:
            //          1st     2nd
            //001-010:  2       0
            //011-030:  3       1
            //031-070:  3       2
            //071-100:  4       3
            //101-150:  5       3
            //151 +  :  13      8
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 1, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd
            //001-010:  3       1
            //011-030:  3       2
            //031-070:  4       3
            //071-100:  4       3
            //101-150:  5       4
            //151 +  :  13      10
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd     3rd
            //001-010:  1       1       1
            //011-030:  2       2       1
            //031-070:  2       2       2
            //071-100:  3       3       2
            //101-150:  3       3       3
            //151 +  :  8       8       8
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st-2nd     3rd     4th-5th     6th-7th     8th
            //001-010:  4           3       3           2           2
            //011-030:  4           4       4           3           2
            //031-070:  4           4       4           3           3
            //071-100:  5           5       4           4           3
            //101-150:  6           6       5           4           4
            //151 +  :  15          15      13          10          10
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "8th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd
            //001-010:  1       0
            //011-030:  1       1
            //031-070:  2       1
            //071-100:  2       2
            //101-150:  3       2
            //151 +  :  8       5
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateThief3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard and Soft Skills:
            //          Parry       Stealth     Thievery
            //001-010:  1           2           2
            //011-030:  2           2           3
            //031-070:  2           3           3
            //071-100:  3           3           4
            //101-150:  3           4           4
            //151 +  :  8           10          10
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList["Stealth"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Stealth", Convert.ToInt32(_calcSkillList["Stealth"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList["Thievery"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Thievery", Convert.ToInt32(_calcSkillList["Thievery"])));

            //Armor Skills:
            //          1st
            //001-010:  2
            //011-030:  2
            //031-070:  2
            //071-100:  3
            //101-150:  3
            //151 +  :  8
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd
            //001-010:  3       1
            //011-030:  3       2
            //031-070:  4       3
            //071-100:  4       3
            //101-150:  5       4
            //151 +  :  13      10
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st     2nd     3rd-4th     5th     6th     7th     8th
            //001-010:  4       4       3           3       2       2       1
            //011-030:  4       4       4           4       3       3       2
            //031-070:  5       4       4           4       4       3       2
            //071-100:  5       5       5           4       4       4       3
            //101-150:  6       6       6           5       5       5       3
            //151 +  :  15      15      15          13      13      13      8
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 5, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "8th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd     3rd
            //001-010:  1       1       1
            //011-030:  2       2       1
            //031-070:  3       2       2
            //071-100:  3       3       2
            //101-150:  4       3       3
            //151 +  :  10      8       8
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateTrader3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard and Soft Skills:
            //          App     Trading
            //001-010:  3       4
            //011-030:  3       5
            //031-070:  4       6
            //071-100:  5       6
            //101-150:  6       7
            //151 +  :  15      15
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList["Appraisal"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Appraisal", Convert.ToInt32(_calcSkillList["Appraisal"])));
            CalculateReq3_0(ref circle, ref currentCircle, 4, 5, 6, 6, 7, 15, Convert.ToInt32(_calcSkillList["Trading"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Trading", Convert.ToInt32(_calcSkillList["Trading"])));

            //Armor Skills:
            //          1st     2nd
            //001-010:  2       1
            //011-030:  3       2
            //031-070:  3       2
            //071-100:  3       3
            //101-150:  4       3
            //151 +  :  10      8
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st
            //001-010:  1
            //011-030:  2
            //031-070:  2
            //071-100:  3
            //101-150:  3
            //151 +  :  8
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st     2nd-3rd     4th     5th     6th
            //001-010:  3       2           1       1       1
            //011-030:  3       3           2       2       1
            //031-070:  4       3           2       2       1
            //071-100:  4       4           3       3       2
            //101-150:  5       4           4       3       2
            //151 +  :  13      10          10      8       5
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd     3rd
            //001-010:  3       2       2
            //011-030:  3       3       2
            //031-070:  4       3       3
            //071-100:  4       4       4
            //101-150:  5       4       4
            //151 +  :  13      10      10
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateWarriorMage3_0()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard and Soft Skills:
            //          Defending   Parry   Summon  TM      Scholarship
            //001-010:  1           2       3       4       1
            //011-030:  1           3       4       4       1
            //031-070:  2           3       5       4       2
            //071-100:  2           4       5       5       2
            //101-150:  3           4       5       6       3
            //151 +  :  8           10      13      15      8
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList["Defending"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Defending", Convert.ToInt32(_calcSkillList["Defending"])));
            CalculateReq3_0(ref circle, ref currentCircle, 2, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 5, 5, 5, 13, Convert.ToInt32(_calcSkillList["Summoning"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Summoning", Convert.ToInt32(_calcSkillList["Summoning"])));
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList["Targeted Magic"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Targeted Magic", Convert.ToInt32(_calcSkillList["Targeted Magic"])));
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList["Scholarship"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scholarship", Convert.ToInt32(_calcSkillList["Scholarship"])));

            //Armor Skills:
            //          1st
            //001-010:  2
            //011-030:  2
            //031-070:  3
            //071-100:  3
            //101-150:  4
            //151 +  :  10
            skill = HighestArmor3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd     3rd
            //001-010:  3       0       0
            //011-030:  4       3       0
            //031-070   4       3       2
            //071-100:  5       4       3
            //101-150:  5       4       4
            //151 +  :  13      10      10
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 4, 4, 5, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 3, 3, 4, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 2, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd     3rd     4th     5th
            //001-010:  4       4       3       0       0
            //011-030:  4       4       3       3       0
            //031-070:  5       4       4       3       3
            //071-100:  5       5       4       4       4
            //101-150:  6       6       5       5       5
            //151 +  :  15      15      13      13      13
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 5, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 4, 5, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 3, 3, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 3, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 0, 0, 3, 4, 5, 13, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st-2nd     3rd-4th
            //001-010:  1           1
            //011-030:  1           1
            //031-070:  2           1
            //071-100:  2           2
            //101-150:  3           2
            //151 +  :  8           5
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 2, 2, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 1, 1, 2, 2, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd     3rd
            //001-010:  2       2       1
            //011-030:  2       2       2
            //031-070:  3       2       2
            //071-100:  3       3       3
            //101-150:  4       3       3
            //151 +  :  10      8       8
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 2, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 2, 3, 3, 8, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        #endregion

        #region DR2.0Functions
        private string HighestMagic(Hashtable skills)
        {
            string skillName = "";
            int ranks = 0;

            foreach (DictionaryEntry skill in skills)
            {
                switch (skill.Key.ToString())
                {
                    case "Harness Ability":
                    case "Arcana":
                    case "Power Perceive":
                    case "Targeted Magic":
                    case "Primary Magic":
                    case "Lunar Magic":
                    case "Life Magic":
                    case "Holy Magic":
                    case "Elemental Magic":
                    case "Inner Magic":
                    case "Arcane Magic":
                        if (Convert.ToInt32(skill.Value) > ranks)
                        {
                            skillName = skill.Key.ToString();
                            ranks = Convert.ToInt32(skill.Value);
                        }
                        break;
                    default:
                        break;
                }

            }

            return skillName;
        }

        private string HighestWeapon(Hashtable skills)
        {
            string skillName = "";
            int ranks = 0;

            foreach (DictionaryEntry skill in skills)
            {
                switch (skill.Key.ToString())
                {
                    case "Brawling":
                    case "Composite Bow":
                    case "Halberds":
                    case "Heavy Blunt":
                    case "Heavy Crossbow":
                    case "Heavy Edged":
                    case "Heavy Thrown":
                    case "Light Blunt":
                    case "Light Crossbow":
                    case "Light Edged":
                    case "Light Thrown":
                    case "Long Bow":
                    case "Medium Blunt":
                    case "Medium Edged":
                    case "Offhand Weapon":
                    case "Pikes":
                    case "Quarter Staff":
                    case "Short Bow":
                    case "Short Staff":
                    case "Slings":
                    case "Staff Sling":
                    case "Twohanded Blunt":
                    case "Twohanded Edged":
                        if (Convert.ToInt32(skill.Value) > ranks)
                        {
                            skillName = skill.Key.ToString();
                            ranks = Convert.ToInt32(skill.Value);
                        }
                        break;
                    default:
                        break;


                }


            }

            return skillName;
        }

        private string HighestArmor(Hashtable skills)
        {
            string skillName = "";
            int ranks = 0;

            foreach (DictionaryEntry skill in skills)
            {
                switch (skill.Key.ToString())
                {
                    case "Bone Armor":
                    case "Cloth Armor":
                    case "Heavy Chain":
                    case "Heavy Plate":
                    case "Leather Armor":
                    case "Light Chain":
                    case "Light Plate":
                    case "Shield Usage":
                        if (Convert.ToInt32(skill.Value) > ranks)
                        {
                            skillName = skill.Key.ToString();
                            ranks = Convert.ToInt32(skill.Value);
                        }
                        break;
                    default:
                        break;
                }


            }

            return skillName;
        }

        private string HighestSurvival(Hashtable skills)
        {
            string skillName = "";
            int ranks = 0;

            switch (_calcGuild)
            {
                case Guilds.Thief:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Climbing":
                            case "Disarm Traps":
                            case "Escaping":
                            case "Evasion":
                            case "Stealing":
                            case "Hiding":
                            case "Lockpicking":
                            case "Perception":
                            case "Backstab":
                            case "Stalking":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }
                    }
                    break;
                case Guilds.Necromancer:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Climbing":
                            case "Disarm Traps":
                            case "Escaping":
                            case "Evasion":
                            case "First Aid":
                            case "Foraging":
                            case "Hiding":
                            case "Lockpicking":
                            case "Perception":
                            case "Skinning":
                            case "Stalking":
                            case "Swimming":
                            case "Stealing":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;
                        }

                    }
                    break;
                case Guilds.Empath:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Climbing":
                            case "Disarm Traps":
                            case "Escaping":
                            case "Evasion":
                            case "First Aid":
                            case "Foraging":
                            case "Hiding":
                            case "Lockpicking":
                            case "Perception":
                            case "Skinning":
                            case "Stalking":
                            case "Swimming":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }
                    }
                    break;
                case Guilds.Ranger:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Climbing":
                            case "Disarm Traps":
                            case "Escaping":
                            case "Evasion":
                            case "First Aid":
                            case "Foraging":
                            case "Hiding":
                            case "Lockpicking":
                            case "Scouting":
                            case "Perception":
                            case "Skinning":
                            case "Stalking":
                            case "Stealing":
                            case "Swimming":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }
                    }
                    break;
                case Guilds.Bard:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Climbing":
                            case "Disarm Traps":
                            case "Escaping":
                            case "Evasion":
                            case "First Aid":
                            case "Foraging":
                            case "Hiding":
                            case "Lockpicking":
                            case "Perception":
                            case "Skinning":
                            case "Stalking":
                            case "Stealing":
                            case "Swimming":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }
                    }
                    break;
                default:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Climbing":
                            case "Disarm Traps":
                            case "Escaping":
                            case "Evasion":
                            case "First Aid":
                            case "Foraging":
                            case "Hiding":
                            case "Lockpicking":
                            case "Perception":
                            case "Skinning":
                            case "Stalking":
                            case "Swimming":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }
                    }
                    break;
            }

            return skillName;

        }

        private string HighestLore(Hashtable skills)
        {

            string skillName = "";
            int ranks = 0;

            switch (_calcGuild)
            {
                case Guilds.Barbarian:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Mechanical Lore":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Teaching":
                            case "Scholarship":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.Bard:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Mechanical Lore":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Teaching":
                            case "Scholarship":
                            case "Musical Theory":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.MoonMage:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Mechanical Lore":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.Thief:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Teaching":
                            case "Scholarship":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.Empath:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Mechanical Lore":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Scholarship":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.Trader:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Mechanical Lore":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Teaching":
                            case "Scholarship":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.Paladin:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Mechanical Lore":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Teaching":
                            case "Scholarship":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.Ranger:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Mechanical Lore":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Teaching":
                            case "Scholarship":
                            case "Animal Lore":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.Cleric:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Mechanical Lore":
                            case "Teaching":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Scholarship":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.WarriorMage:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Mechanical Lore":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Teaching":
                            case "Scholarship":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                case Guilds.Necromancer:
                    foreach (DictionaryEntry skill in skills)
                    {
                        switch (skill.Key.ToString())
                        {
                            case "Appraisal":
                            case "Mechanical Lore":
                            case "Percussions":
                            case "Strings":
                            case "Winds":
                            case "Vocals":
                            case "Teaching":
                            case "Scholarship":
                                if (Convert.ToInt32(skill.Value) > ranks)
                                {
                                    skillName = skill.Key.ToString();
                                    ranks = Convert.ToInt32(skill.Value);
                                }
                                break;
                            default:
                                break;

                        }

                    }
                    break;
                default:
                    break;

            }

            return skillName;

        }

        private int TotalMagic(Hashtable skills)
        {
            return Convert.ToInt32(skills["Power Perceive"]) + Convert.ToInt32(skills["Targeted Magic"]) + Convert.ToInt32(skills["Arcana"]) + Convert.ToInt32(skills["Harness Ability"]) + Convert.ToInt32(skills["Lunar Magic"]) + Convert.ToInt32(skills["Life Magic"]) + Convert.ToInt32(skills["Holy Magic"]) + Convert.ToInt32(skills["Elemental Magic"]) + Convert.ToInt32(skills["Inner Magic"]) + Convert.ToInt32(skills["Arcane Magic"]);
        }

        private int TotalSurvival(Hashtable skills)
        {
            int total = 0;
            foreach (DictionaryEntry skill in skills)
            {
                switch (skill.Key.ToString())
                {
                    case "Climbing":
                    case "Disarm Traps":
                    case "Escaping":
                    case "Evasion":
                    case "First Aid":
                    case "Foraging":
                    case "Hiding":
                    case "Scouting":
                    case "Perception":
                    case "Skinning":
                    case "Stalking":
                    case "Swimming":
                        total = total + Convert.ToInt32(skill.Value);
                        break;
                    default:
                        break;
                }

            }
            return total;
        }

        private int TotalLore(Hashtable skills)
        {
            if (_calcGuild == Guilds.Empath)
                return Convert.ToInt32(skills["Teaching"]) + Convert.ToInt32(skills["Scholarship"]) + Convert.ToInt32(skills["Mechanical Lore"]) + Convert.ToInt32(skills["Appraisal"]) + Convert.ToInt32(skills["Empathy"]);

            if (_calcGuild == Guilds.Trader)
                return Convert.ToInt32(skills["Teaching"]) + Convert.ToInt32(skills["Scholarship"]) + Convert.ToInt32(skills["Mechanical Lore"]) + Convert.ToInt32(skills["Appraisal"]) + Convert.ToInt32(skills["Trading"]);

            return 0;
        }

        private void CalculateReq(ref int circle, ref int currentCircle, double rank1, double rank2, double rank3, double rank4, double rank5, double ranks, ref double ranksNeeded)
        {

            int i;
            ranksNeeded = 0;
            circle = 0;
            currentCircle = 0;

            for (i = 1; i <= 10; i++)
            {
                ranksNeeded += rank1;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank1) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            for (i = 11; i <= 30; i++)
            {
                ranksNeeded += rank2;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank2) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            for (i = 31; i <= 70; i++)
            {
                ranksNeeded += rank3;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank3) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            for (i = 71; i <= 100; i++)
            {
                ranksNeeded += rank4;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank4) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            for (i = 101; i <= 500; i++)
            {
                ranksNeeded += rank5;

                if (Convert.ToInt32(ranksNeeded) > ranks)
                {
                    if (_calcCircle > i)
                    {
                        if (currentCircle == 0)
                            currentCircle = i - 1;

                        continue;
                    }
                    if (Convert.ToInt32(ranksNeeded - rank5) <= ranks)
                        currentCircle = i - 1;
                    circle = i;
                    return;
                }
            }

            circle = 500;
        }

        private void CalculateBarbarian()
        {

            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          Parry   Multi   Shield  App     Mech    Scholar
            //001-010:  4       2       2       .5      .5      .5
            //011-030:  4       3       0       0       0       0
            //031-070:  4       3       0       0       0       0
            //071-100:  4       4       0       0       0       0
            //100 +  :  5       4       0       0       0       0
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 4, 5, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 4, Convert.ToInt32(_calcSkillList["Multi Opponent"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Multi Opponent", Convert.ToInt32(_calcSkillList["Multi Opponent"])));
            CalculateReq(ref circle, ref currentCircle, 2, 0, 0, 0, 0, Convert.ToInt32(_calcSkillList["Shield Usage"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Shield Usage", Convert.ToInt32(_calcSkillList["Shield Usage"])));
            CalculateReq(ref circle, ref currentCircle, .5, 0, 0, 0, 0, Convert.ToInt32(_calcSkillList["Appraisal"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Appraisal", Convert.ToInt32(_calcSkillList["Appraisal"])));
            CalculateReq(ref circle, ref currentCircle, .5, 0, 0, 0, 0, Convert.ToInt32(_calcSkillList["Mechanical Lore"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Mechanical Lore", Convert.ToInt32(_calcSkillList["Mechanical Lore"])));
            CalculateReq(ref circle, ref currentCircle, .5, 0, 0, 0, 0, Convert.ToInt32(_calcSkillList["Scholarship"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scholarship", Convert.ToInt32(_calcSkillList["Scholarship"])));

            //Weapons Skills
            //          1st     2nd     3rd     4th     5th
            //001-010:  4       4       0       0       0
            //010-030:  5       5       0       0       0
            //031-070:  6       4       4       0       0
            //071-100:  6       6       4       0       0
            //100 +  :  6       6       6       6       6
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 5, 6, 6, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 5, 4, 6, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 4, 4, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 0, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 0, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Armor Skills
            //          1st     2nd
            //001-010:  3       0
            //010-030:  4       0
            //031-070:  4       0
            //071-100:  5       4
            //100 +  :  6       4
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 4, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills
            //          1st/2nd     3rd-6th     7th-8th     9th-10th
            //001-010:  2           2           2           2
            //010-030:  2           2           0           0
            //031-070:  3           2           0           0
            //071-100:  3           2           2           0
            //100 +  :  3           3           3           0
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 0, 0, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 0, 0, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "8th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 0, 0, 0, 0, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "9th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 0, 0, 0, 0, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "10th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills
            //          1st     2nd
            //001-010:  0       0
            //010-030:  0       0
            //031-070:  0       0
            //071-100:  2       0
            //100 +  :  2       2
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 0, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateBard()
        {
            reqList = new ArrayList();


            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;
            //Hard Skills:
            //          Multi   Parry   Music Theory    Vocals  Percussion  Winds   Strings
            //001-010:  1       1       2               2       2           2       2
            //011-030:  1       2       2               2       2           2       2
            //031-070:  2       3       3               3       3           3       3
            //071-100:  2       3       4               4       4           4       4
            //100 +  :  3       4       4               4       4           4       4
            CalculateReq(ref circle, ref currentCircle, 1, 1, 2, 2, 3, Convert.ToInt32(_calcSkillList["Multi Opponent"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Multi Opponent", Convert.ToInt32(_calcSkillList["Multi Opponent"])));
            CalculateReq(ref circle, ref currentCircle, 1, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 4, 4, Convert.ToInt32(_calcSkillList["Musical Theory"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Musical Theory", Convert.ToInt32(_calcSkillList["Musical Theory"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 4, 4, Convert.ToInt32(_calcSkillList["Vocals"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Vocals", Convert.ToInt32(_calcSkillList["Vocals"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 4, 4, Convert.ToInt32(_calcSkillList["Percussions"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Percussions", Convert.ToInt32(_calcSkillList["Percussions"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 4, 4, Convert.ToInt32(_calcSkillList["Winds"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Winds", Convert.ToInt32(_calcSkillList["Winds"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 4, 4, Convert.ToInt32(_calcSkillList["Strings"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Strings", Convert.ToInt32(_calcSkillList["Strings"])));

            //Lore Skills:
            //          1st/2nd     3rd/4th     5th/6th
            //001-010:  4           3           3
            //011-030:  4           3           3
            //031-070:  4           4           3
            //071-100:  5           5           4
            //100 +  :  6           6           5
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st-6th
            //001-010:  1
            //011-030:  1
            //031-070:  1
            //071-100:  2
            //100 +  :  2
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd          
            //001-010:  3       1
            //011-030:  3       2
            //031-070:  3       3
            //071-100:  4       3
            //100 +  :  5       4
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Armor Skills:
            //          1st     2nd
            //001-010:  2       0
            //011-030:  2       0
            //031-070:  2       1
            //071-100:  2       2
            //100 +  :  3       3
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 1, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd/3rd
            //001-010:  2       1
            //011-030:  3       2
            //031-070:  3       3
            //071-100:  4       3
            //100 +  :  4       4
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateCleric()
        {
            reqList = new ArrayList();

            int ranks = 0;
            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          Harness     Teaching    Shield  Parry   TotalMagic
            //001-010:  4           2           1       2       20
            //011-030:  4           3           2       3       20
            //031-070:  5           3           0       3       28
            //071-100:  6           4           0       3       33
            //100 +  :  7           4           0       3       38
            CalculateReq(ref circle, ref currentCircle, 4, 4, 5, 6, 7, Convert.ToInt32(_calcSkillList["Harness Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Harness Ability", Convert.ToInt32(_calcSkillList["Harness Ability"])));
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 4, Convert.ToInt32(_calcSkillList["Teaching"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Teaching", Convert.ToInt32(_calcSkillList["Teaching"])));
            CalculateReq(ref circle, ref currentCircle, 1, 2, 0, 0, 0, Convert.ToInt32(_calcSkillList["Shield Usage"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Shield Usage", Convert.ToInt32(_calcSkillList["Shield Usage"])));
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 3, 3, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            ranks = TotalMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 20, 20, 28, 33, 38, ranks, ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Overall Magic", ranks));

            //Lore Skills:
            //          1st/2nd     3rd     4th
            //001-010:  2           1       1
            //011-030:  3           2       1
            //031-070:  3           3       2
            //071-100:  3           3       2
            //100 +  :  4           4       3
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st-6th     7th/8th
            //001-010:  1           0
            //011-030:  1           0
            //031-070:  1           0
            //071-100:  2           0
            //100 +  :  2           2
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 0, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 0, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "8th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st
            //001-010:  3
            //011-030:  3
            //031-070:  4
            //071-100:  4
            //100 +  :  5
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Armor Skills:
            //          
            //001-010:  2
            //011-030:  2
            //031-070:  2
            //071-100:  3
            //100 +  :  3
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateEmpath()
        {
            reqList = new ArrayList();

            int ranks = 0;
            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          Teach   Empathy     Scholar     FA      Evasion     Forage      Lore (Empathy+Teach+Scholar+App+Mech)
            //001-010:  2       4           2           2       1           1           12
            //011-030:  3.5     5           2.5         2.5     0           1           16
            //031-070:  4.5     6           3           3       0           0           19
            //071-100:  4.5     6           3           3       0           0           21
            //100 +  :  5       7           3           3.5     0           0           26
            CalculateReq(ref circle, ref currentCircle, 2, 3.5, 4.5, 4.5, 5, Convert.ToInt32(_calcSkillList["Teaching"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Teaching", Convert.ToInt32(_calcSkillList["Teaching"])));
            CalculateReq(ref circle, ref currentCircle, 4, 5, 6, 6, 7, Convert.ToInt32(_calcSkillList["Empathy"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Empathy", Convert.ToInt32(_calcSkillList["Empathy"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2.5, 3, 3, 3, Convert.ToInt32(_calcSkillList["Scholarship"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scholarship", Convert.ToInt32(_calcSkillList["Scholarship"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2.5, 3, 3, 3.5, Convert.ToInt32(_calcSkillList["First Aid"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "First Aid", Convert.ToInt32(_calcSkillList["First Aid"])));
            CalculateReq(ref circle, ref currentCircle, 1, 0, 0, 0, 0, Convert.ToInt32(_calcSkillList["Evasion"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Evasion", Convert.ToInt32(_calcSkillList["Evasion"])));
            CalculateReq(ref circle, ref currentCircle, 1, 1, 0, 0, 0, Convert.ToInt32(_calcSkillList["Foraging"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Foraging", Convert.ToInt32(_calcSkillList["Foraging"])));
            ranks = TotalLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 12, 16, 19, 21, 26, ranks, ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Overall Lore", ranks));


            //Survival Skills: (No stealing/Backstab)
            //          1st/2nd     3rd/4th     5th/6th
            //001-010:  1           1           1
            //011-030:  2           1           1
            //031-070:  2           2           1
            //071-100:  3           2           2
            //100 +  :  4           3           2
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "2nd Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "3rd Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd/3rd
            //001-010:  3       2
            //011-030:  4       3
            //031-070:  5       4
            //071-100:  5       4
            //100 +  :  6       5
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 4, 5, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "2nd Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "3rd Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }
        
        private void CalculateMoonMage()
        {
            reqList = new ArrayList();

            int ranks = 0;
            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          PP      Astro   Scholar     Teach       Magic
            //001-010:  4       2       3           2           20
            //011-030:  4       2       3           2           20
            //031-070:  5       3       3           2           28
            //071-100:  6       4       4           2           33
            //100 +  :  7       5       4           3           38
            CalculateReq(ref circle, ref currentCircle, 4, 4, 5, 6, 7, Convert.ToInt32(_calcSkillList["Power Perceive"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Power Perceive", Convert.ToInt32(_calcSkillList["Power Perceive"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 4, 5, Convert.ToInt32(_calcSkillList["Astrology"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Astrology", Convert.ToInt32(_calcSkillList["Astrology"])));
            CalculateReq(ref circle, ref currentCircle, 3, 3, 3, 4, 4, Convert.ToInt32(_calcSkillList["Scholarship"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scholarship", Convert.ToInt32(_calcSkillList["Scholarship"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList["Teaching"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Teaching", Convert.ToInt32(_calcSkillList["Teaching"])));
            ranks = TotalMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 20, 20, 28, 33, 38, ranks, ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Overall Magic", ranks));

            //Lore Skills:
            //          1st     2nd
            //001-010:  2       0
            //011-030:  2       0
            //031-070:  3       0
            //071-100:  3       2
            //100 +  :  4       3
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st/2nd     3rd-6th     7th/8th
            //001-010:  2           2           2
            //011-030:  2           2           0
            //031-070:  3           2           0
            //071-100:  3           2           2
            //100 +  :  3           3           3
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 0, 0, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 0, 0, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "8th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateNecromancer()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          LE      Skin    Evasion     Thanatology     FA
            //001-010:  1       2       2           3               2
            //011-030:  2       2       2           3               2
            //031-070:  2       3       3           3               2
            //071-100:  2       3       3           4               3
            //100 +  :  2       3       3           5               3
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 2, 2, Convert.ToInt32(_calcSkillList["Light Edged"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Light Edged", Convert.ToInt32(_calcSkillList["Light Edged"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList["Skinning"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Skinning", Convert.ToInt32(_calcSkillList["Skinning"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList["Evasion"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Evasion", Convert.ToInt32(_calcSkillList["Evasion"])));
            CalculateReq(ref circle, ref currentCircle, 3, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList["Thanatology"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Thanatology", Convert.ToInt32(_calcSkillList["Thanatology"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList["First Aid"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "First Aid", Convert.ToInt32(_calcSkillList["First Aid"])));


            //Survival Skills:
            //          1st/2nd     3rd/4th     5th/6th     7th/8th 
            //001-010:  4           3           3           2
            //011-030:  4           4           3           3
            //031-070:  4           4           4           3
            //071-100:  5           4           4           4
            //100 +  :  6           5           4           4
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "2nd Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 4, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "3rd Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 4, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "8th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd     3rd     4th     5th
            //001-010:  3       3       2       2       1
            //011-030:  4       3       3       2       2
            //031-070:  4       4       3       3       2
            //071-100:  5       5       4       4       3
            //100 +  :  6       6       5       5       3
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "2nd Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "3rd Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Armor Skills:
            //          1st
            //001-010:  1
            //011-030:  2
            //031-070:  2
            //071-100:  2
            //100 +  :  3
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Lore Skills:
            //          1st     2nd
            //001-010:  2       2
            //011-030:  2       2
            //031-070:  3       2
            //071-100:  3       3
            //100 +  :  3       3
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "2nd Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculatePaladin()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          Teach   FA      Scholar     Muliti      Parry       Shield
            //001-010:  1       1       1           2           3           2
            //011-030:  2       1       2           2           3           2
            //031-070:  0       0       0           2           4           0
            //071-100:  0       0       0           3           4           0
            //100 +  :  0       0       0           4           5           0
            CalculateReq(ref circle, ref currentCircle, 1, 2, 0, 0, 0, Convert.ToInt32(_calcSkillList["Teaching"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Teaching", Convert.ToInt32(_calcSkillList["Teaching"])));
            CalculateReq(ref circle, ref currentCircle, 1, 1, 0, 0, 0, Convert.ToInt32(_calcSkillList["First Aid"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "First Aid", Convert.ToInt32(_calcSkillList["First Aid"])));
            CalculateReq(ref circle, ref currentCircle, 1, 2, 0, 0, 0, Convert.ToInt32(_calcSkillList["Scholarship"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scholarship", Convert.ToInt32(_calcSkillList["Scholarship"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 3, 4, Convert.ToInt32(_calcSkillList["Multi Opponent"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Multi Opponent", Convert.ToInt32(_calcSkillList["Multi Opponent"])));
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 0, 0, 0, Convert.ToInt32(_calcSkillList["Shield Usage"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Shield Usage", Convert.ToInt32(_calcSkillList["Shield Usage"])));

            //Skills:
            //          1st-3rd     4th
            //001-010:  1           1
            //011-030:  2           0
            //031-070:  2           2
            //071-100:  3           0
            //100 +  :  3           3
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 0, 2, 0, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd     3rd     4th/5th
            //001-010:  3       0       0       0
            //011-030:  4       2       0       0
            //031-070:  4       3       0       0
            //071-100:  5       3       3       0
            //100 +  :  5       4       3       6
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 4, 4, 5, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 0, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 0, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Armor Skills:
            //          1st     2nd
            //001-010:  4       (2)
            //011-030:  5       (2)
            //031-070:  5       3
            //071-100:  5       4
            //100 +  :  6       5
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 5, 5, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            if (ranksNeeded > 0)
            {
                CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            }
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st-4th     5th-6th
            //001-010:  1           1
            //011-030:  1           0
            //031-070:  1           0
            //071-100:  2           0
            //100 +  :  2           2
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 0, 0, 0, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 0, 0, 0, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st-3rd
            //001-010:  1
            //011-030:  1
            //031-070:  1
            //071-100:  1
            //100 +  :  2
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 1, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 1, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 1, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

        }

        private void CalculateRanger()
        {
            reqList = new ArrayList();


            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          MO      Parry   Skin    Stalk   Scout
            //001-010:  2       2       2       2       2
            //011-030:  2       2       2       2       2
            //031-070:  2       2       3       3       3
            //071-100:  2       3       3       3       3
            //100 +  :  2       3       3       3       3
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 2, Convert.ToInt32(_calcSkillList["Multi Opponent"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Multi Opponent", Convert.ToInt32(_calcSkillList["Multi Opponent"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList["Skinning"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Skinning", Convert.ToInt32(_calcSkillList["Skinning"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList["Stalking"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Stalking", Convert.ToInt32(_calcSkillList["Stalking"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList["Scouting"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Scouting", Convert.ToInt32(_calcSkillList["Scouting"])));


            //Lore Skills:
            //          1st     2nd
            //001-010:  1       0
            //011-030:  1       1
            //031-070:  2       1
            //071-100:  2       2
            //100 +  :  2       2
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 2, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st/2nd     3rd/4th     5th/6th     7th/8th     9th/10th
            //001-010:  4           3           3           2           0
            //011-030:  4           4           3           3           0
            //031-070:  4           4           4           3           2
            //071-100:  5           4           4           4           2
            //100 +  :  6           5           5           5           3
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 4, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 4, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "8th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "9th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "10th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd     3rd
            //001-010:  3       1       0
            //011-030:  3       2       0
            //031-070:  4       3       2
            //071-100:  4       3       2
            //100 +  :  5       4       3
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Armor Skills:
            //          1st     2nd
            //001-010:  2       0
            //011-030:  3       0
            //031-070:  3       2
            //071-100:  4       3
            //100 +  :  5       3
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Magic Skills:
            //          1st     2nd
            //001-010:  1       0
            //011-030:  2       2
            //031-070:  2       2
            //071-100:  3       3
            //100 +  :  3       3
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Magic(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateThief()
        {
            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          MO  Parry   Steal   Hide    Stalk   Lock    Mech    Shield
            //001-010:  1   1       2       2       2       2       2       1
            //011-030:  1   1       2       2       2       2       2       0
            //031-070:  1   1       3       3       3       3       3       0
            //071-100:  2   2       3       3       3       3       4       0
            //100 +  :  2   2       3       3       3       3       5       0
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList["Multi Opponent"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Multi Opponent", Convert.ToInt32(_calcSkillList["Multi Opponent"])));
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList["Stealing"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Stealing", Convert.ToInt32(_calcSkillList["Stealing"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList["Hiding"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Hiding", Convert.ToInt32(_calcSkillList["Hiding"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList["Stalking"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Stalking", Convert.ToInt32(_calcSkillList["Stalking"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList["Lockpicking"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Lockpicking", Convert.ToInt32(_calcSkillList["Lockpicking"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 3, 4, 5, Convert.ToInt32(_calcSkillList["Mechanical Lore"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Mechanical Lore", Convert.ToInt32(_calcSkillList["Mechanical Lore"])));
            CalculateReq(ref circle, ref currentCircle, 1, 0, 0, 0, 0, Convert.ToInt32(_calcSkillList["Shield Usage"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Shield Usage", Convert.ToInt32(_calcSkillList["Shield Usage"])));

            //Lore Skills:
            //          1st     2nd/3rd
            //001-010:  1       1
            //011-030:  2       1
            //031-070:  3       1
            //071-100:  3       2
            //100 +  :  3       2
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 3, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //          1st/2nd     3rd/4th     5th/6th     7th/8th
            //001-010:  4           3           3           2
            //011-030:  4           3           3           3
            //031-070:  4           4           4           3
            //071-100:  5           5           4           4
            //100 +  :  6           6           5           5
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 5, 6, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "8th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd     3rd
            //001-010:  2       0       0
            //011-030:  3       2       0
            //031-070:  3       3       1
            //071-100:  4       3       2
            //100 +  :  4       4       2
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Armor Skills:
            //          1st
            //001-010:  2
            //011-030:  2
            //031-070:  2
            //071-100:  3
            //100 +  :  3
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        private void CalculateTrader()
        {
            reqList = new ArrayList();

            int ranks = 0;
            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          Appraisal   Trading     Lore
            //001-010:  3           4           12
            //011-030:  3           5           16
            //031-070:  4           6           19
            //071-100:  5           7           21
            //100 +  :  6           8           26
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 5, 6, Convert.ToInt32(_calcSkillList["Appraisal"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Appraisal", Convert.ToInt32(_calcSkillList["Appraisal"])));
            CalculateReq(ref circle, ref currentCircle, 4, 5, 6, 7, 8, Convert.ToInt32(_calcSkillList["Trading"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Trading", Convert.ToInt32(_calcSkillList["Trading"])));
            ranks = TotalLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 12, 16, 19, 21, 26, ranks, ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Overall Lore", ranks));


            //Survival Skills:
            //          1st/2nd     3rd/4th     5th/6th
            //001-010:  1           1           1
            //011-030:  2           1           1
            //031-070:  2           2           1
            //071-100:  3           2           2
            //100 +  :  4           3           2
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Tertiary Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 2, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Armor Skills:
            //          1st     2nd
            //001-010:  1       1
            //011-030:  2       1
            //031-070:  2       1
            //071-100:  2       1
            //100 +  :  2       1
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 1, 1, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Secondary Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

        }

        private void CalculateWarriorMage()
        {
            reqList = new ArrayList();

            int ranks = 0;
            int circle = 0;
            double ranksNeeded = 0;
            string skill = "";
            int currentCircle = 0;

            //Hard Skills:
            //          TM      Parry       Shield      MO      Magic
            //001-010:  4       2           1           2       20
            //011-030:  4       3           0           2       20
            //031-070:  4       3           0           2       28
            //071-100:  5       4           0           2       33
            //100 +  :  6       4           0           4       38
            CalculateReq(ref circle, ref currentCircle, 4, 4, 4, 5, 6, Convert.ToInt32(_calcSkillList["Targeted Magic"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Targeted Magic", Convert.ToInt32(_calcSkillList["Targeted Magic"])));
            CalculateReq(ref circle, ref currentCircle, 2, 3, 3, 4, 4, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            CalculateReq(ref circle, ref currentCircle, 1, 0, 0, 0, 0, Convert.ToInt32(_calcSkillList["Shield Usage"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Shield Usage", Convert.ToInt32(_calcSkillList["Shield Usage"])));
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 2, 4, Convert.ToInt32(_calcSkillList["Multi Opponent"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Multi Opponent", Convert.ToInt32(_calcSkillList["Multi Opponent"])));
            ranks = TotalMagic(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 20, 20, 28, 33, 38, ranks, ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Overall Magic", ranks));

            //Lore Skills:
            //          1st-3rd     4th
            //001-010:  1           1
            //011-030:  2           0
            //031-070:  2           2
            //071-100:  3           0
            //100 +  :  3           3
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "2nd Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 2, 2, 3, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "3rd Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestLore(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 0, 2, 0, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Lore(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Weapon Skills:
            //          1st     2nd     3rd
            //001-010:  3       0       0
            //011-030:  3       2       0
            //031-070:  4       3       0
            //071-100:  4       3       2
            //100 +  :  5       4       3
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 3, 3, 4, 4, 5, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 2, 3, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "2nd Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 2, 3, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "3rd Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Armor Skills:
            //          1st
            //001-010:  2
            //011-030:  2
            //031-070:  2
            //071-100:  3
            //100 +  :  4
            skill = HighestArmor(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 2, 2, 2, 3, 4, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Armor(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);

            //Survival Skills:
            //         1st-6th      7th-8th 
            //001-010:  1           0
            //011-030:  1           0
            //031-070:  1           0
            //071-100:  2           0
            //100 +  :  2           2
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "1st Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "2nd Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "3rd Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "4th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "5th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 1, 1, 1, 2, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "6th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 0, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "7th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestSurvival(_calcSkillList);
            CalculateReq(ref circle, ref currentCircle, 0, 0, 0, 0, 2, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "8th Survival(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
        }

        #endregion

        #endregion
    }
}
