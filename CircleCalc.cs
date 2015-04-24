//HMACSHA1
using System;
using System.Collections;
using GeniePlugin.Interfaces;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;

namespace Standalone_Circle_Calc
{
    public class CircleCalc : IPlugin
    {
        //Constant variable for the Properties of the plugin
        //At the top for easy changes.
        string _NAME = "Circle Calculator";
        string _VERSION = "4.0.6b";
        string _AUTHOR = "VTCifer";
        string _DESCRIPTION = "Calculcates the circle requirments for different guilds.  It will also sort skills form highest to lowest.";

        public IHost _host;                             //Required for plugin
        public System.Windows.Forms.Form _parent;       //Required for plugin

        bool _reqsLoaded = false;
        string _reqsAuthor = "";
        string _reqsVer = "0.0";

        bool _sortLoaded = false;
        string _sortAuthor = "";
        string _sortVer = "0.0";

        bool _debug = false;
        string _pluginPath = "";
        //Used in storing of Reqs, Hard, Soft, TopN
        class ReqType
        {
            public string Name;             //Display Name/Name of Skill
            public string Skillset;         //Group the skill belongs to in TopN
            public int Circles1to10;        //Circles 1-0
            public int Circles11to30;       //Cirlces 11-30
            public int Circles31to70;       //Circles 31-70
            public int Circles71to100;      //Circles 71-100
            public int Circles101to150;     //Circles101-150
            public int Circles151Up;        //Circles Higher than 150
        }
        class SortSkillGroup
        {
            public string Name;
            public ArrayList Skills = new ArrayList();
            public SkillSets Skillset;
        };

        //Stores all the requirements and the Groupings for a guild used in global hashtable GuildReqList
        class GuildType
        {
            public Hashtable Skillsets = new Hashtable();       //filled with Skillset names, key is Skill name
            public Hashtable HardReqs = new Hashtable();        //filled with ReqTypes, key is name
            public Hashtable SoftReqs = new Hashtable();        //filled with ReqTypes, key is name
            public ArrayList TopN = new ArrayList();            //filled with ReqTypes, no key, indexed by number
        }

        private Hashtable _GuildNameList = new Hashtable();     //filled with guild names, key is shortname
        private Hashtable _GuildReqList = new Hashtable();      //filled with guildtypes, key is string
        Hashtable _GroupNameList = new Hashtable();
        Hashtable _SortGroupList = new Hashtable();
        SortSkillGroup _CurrentSortGroup = new SortSkillGroup();

        #region Circle Calc Members

/*
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
        private Guilds _guild = Guilds.Commoner;        //
        private Guilds _calcGuildName = Guilds.Commoner;    //Default Guild set to Commonder
*/
        private string _calcGuildName = "";
        private GuildType _calcGuild = new GuildType();
        private Hashtable _calcSkillsets = new Hashtable();   //filled with hashtable of skills, key is skillset name.  internal hashtable is keyed on skill name

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

        private SkillSets _skillset = SkillSets.all;    //Default is sort all
        private int _calcCircle = 0;                    //
        private bool _calculating = false;              //
        private bool _sorting = false;                  //
        private bool _parsing = false;                  //

        private bool _enabled = true;                   // 

/*
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
*/
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

            _pluginPath = _host.get_Variable("PluginPath");
            if (_pluginPath != "\\")
                _pluginPath += "\\";

            LoadReqsfromXML();
            LoadSortfromXML();
/*
            if (_reqsLoaded)
            {
                _host.EchoText("");
                _host.EchoText("Beginning debug output:");
                ICollection Keys = _GuildNameList.Keys;
                GuildType tempGuild;
                ArrayList tempSkillset;
                ReqType tempReq;
                foreach (string key in Keys)
                    _host.EchoText("Shortname: " + key + " Guildname: " + _GuildNameList[key].ToString());

                Keys = _GuildReqList.Keys;
                foreach (string key in Keys)
                {
                    tempGuild = (GuildType)_GuildReqList[key];
                    _host.EchoText("Guild: " + key);
                    _host.EchoText("Skillsets:");
                    ICollection Keys2 = tempGuild.Skillsets.Keys;
                    foreach (string key2 in Keys2)
                    {
                        _host.EchoText("  Skillset: " + key2);
                        tempSkillset = (ArrayList)tempGuild.Skillsets[key2];
                        foreach (string skill in tempSkillset)
                            _host.EchoText("    " + skill);
                    }
                    _host.EchoText("Reqs:");
                    _host.EchoText("  Hard:");
                    Keys2 = tempGuild.HardReqs.Keys;
                    foreach (string key2 in Keys2)
                    {
                        _host.EchoText("    " + key2 + ":");
                        tempReq = (ReqType)tempGuild.HardReqs[key2];
                        _host.EchoText("      1-10:" + tempReq.Circles1to10.ToString());
                        _host.EchoText("      11-30:" + tempReq.Circles11to30.ToString());
                        _host.EchoText("      31-70:" + tempReq.Circles31to70.ToString());
                        _host.EchoText("      71-100:" + tempReq.Circles71to100.ToString());
                        _host.EchoText("      101-150:" + tempReq.Circles101to150.ToString());
                        _host.EchoText("      151+:" + tempReq.Circles151Up.ToString());
                    }
                    _host.EchoText("  Soft:");
                    Keys2 = tempGuild.SoftReqs.Keys;
                    foreach (string key2 in Keys2)
                    {
                        _host.EchoText("    " + key2 + ":");
                        tempReq = (ReqType)tempGuild.SoftReqs[key2];
                        _host.EchoText("      1-10:" + tempReq.Circles1to10.ToString());
                        _host.EchoText("      11-30:" + tempReq.Circles11to30.ToString());
                        _host.EchoText("      31-70:" + tempReq.Circles31to70.ToString());
                        _host.EchoText("      71-100:" + tempReq.Circles71to100.ToString());
                        _host.EchoText("      101-150:" + tempReq.Circles101to150.ToString());
                        _host.EchoText("      151+:" + tempReq.Circles151Up.ToString());
                    }
                    _host.EchoText("  N:");
                    foreach (ReqType Req in tempGuild.TopN)
                    {
                        _host.EchoText("    " + Req.Name + ":");
                        _host.EchoText("      Skillset: " + Req.Skillset);
                        _host.EchoText("      1-10:" + Req.Circles1to10.ToString());
                        _host.EchoText("      11-30:" + Req.Circles11to30.ToString());
                        _host.EchoText("      31-70:" + Req.Circles31to70.ToString());
                        _host.EchoText("      71-100:" + Req.Circles71to100.ToString());
                        _host.EchoText("      101-150:" + Req.Circles101to150.ToString());
                        _host.EchoText("      151+:" + Req.Circles151Up.ToString());
                    }
                }
            }
*/
        }

        //Required for Plugin - Called when user enters text in the command box
        //Parameters:
        //              string Text:  The text the user entered in the command box
        //Return Value:
        //              string: Text that will be sent to the game
        public string ParseInput(string Text)
        {
            //User asking for help with commands 
            if (Text == "/cc ?" || Text == "/calc ?" || Text == "/cc")
            {
                DisplaySyntax();
                return "";
            }

            //help/system commands
            if (Text.StartsWith("/cc "))
            {

                //Clean Input of leading/trailing whitespace
                Text = Text.Trim();

                if (Text == "/cc reload")
                {
                    LoadReqsfromXML();
                    LoadSortfromXML();
                }
                else if (Text == "/cc reloadreqs")
                    LoadReqsfromXML();
                else if (Text == "/cc reloadsort")
                    LoadSortfromXML();
                else if (Text == "/cc debug")
                {
                    _debug = !_debug;
                    SendOutput("Debug toggled.  Now set to " + _debug+ ".");
                }
                else
                    DisplaySyntax();
                return "";
            }

            //Start Calculating circle
            if (Text.StartsWith("/calc ") || Text == "/calc")
            {
                _calcGuildName = _host.get_Variable("CircleCalc.Guild");
                    
                //Clean Input of leading/trailing whitespace
                Text = Text.Trim();

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
                            SendOutput("");
                            SendOutput("Circle Calculator: maximum circle is 500");
                            return "";
                        }
                        else if (_calcCircle < 2)
                        {
                            SendOutput("");
                            SendOutput("Circle Calculator: minimum circle is 2");
                            return "";
                        }
                        //if two spaces, then guild is also included
                        if (space == 2)
                        {
                            //read guild from the line
                            _calcGuildName = Text.Substring(Text.IndexOf(" ") + 1, Text.LastIndexOf(" ") - Text.IndexOf(" ") - 1);

                            //if you can't find the guild
                            if (!GetGuild(_calcGuildName))
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

                        //check if default guild was set in Genie
                        if (_calcGuildName != "")
                        {
                            //if you can't find the guild
                            if (!GetGuild(_calcGuildName))
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
                    catch 
                    {
                        //if last item is a guild, and there is more than one space, syntax is wrong
                        if (space > 1)
                        {
                            DisplaySyntax();
                            return "";
                        }


                        //get the guild from the line to calc against
                        _calcGuildName = Text.Substring(Text.IndexOf(" ") + 1, Text.Length - 1 - Text.IndexOf(" "));
                        
                        //If you cannot find the guild to calculate against
                        if (!GetGuild(_calcGuildName))
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
                    //check if default guild was set in Genie
                    if (_calcGuildName != "")
                    {
                        //if you can't find the guild
                        if (!GetGuild(_calcGuildName))
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

                    //if you got this far, it means the command was simply "calc"
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

                //clear out any double spaces in the command line
                while (Text.Contains("  "))
                    Text = Text.Replace("  ", " ");

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
                                if (!_sortLoaded)
                                {
                                    SendOutput("Invalid sorting group!");
                                    SendOutput("Custom sorting is disabled due to no sorting file loaded.");
                                    return "";
                                }
                                else if (!_GroupNameList.ContainsKey(skillset))
                                {
                                    SendOutput("Invalid sorting group!");
                                    return "";
                                }
                                else
                                {
                                    _CurrentSortGroup = ((SortSkillGroup)_SortGroupList[_GroupNameList[skillset]]);
                                    if (_CurrentSortGroup.Skillset != SkillSets.all)
                                        Text = "exp " + _CurrentSortGroup.Skillset.ToString() + " " + _calcRank.ToString();
                                    else
                                        Text = "exp " + _calcRank.ToString();
                                    _sorting = true;
                                    return Text;
                                }
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
                    catch 
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
                            if (!_sortLoaded)
                            {
                                SendOutput("Invalid sorting group!");
                                SendOutput("Custom sorting is disabled due to no sorting file loaded.");
                                return "";
                            }
                            else if (!_GroupNameList.ContainsKey(skillset))
                            {
                                SendOutput("Invalid sorting group!");
                                return "";
                            }
                            else
                            {
                                _CurrentSortGroup = ((SortSkillGroup)_SortGroupList[_GroupNameList[skillset]]);
                                Text = "exp " + _CurrentSortGroup.Skillset.ToString() + " all";
                                _sorting = true;
                                return Text;
                            }
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
            SendOutput("");
            SendOutput("Standalone Circle Calculator(Ver:" + _VERSION + ") Usage:");
            SendOutput("/cc ? (shows this help");
            SendOutput("/cc reload[reqs|sort] (attempts to reload the reqs and/or the sorting data)");
            SendOutput("/calc [guild] [circle]");
            SendOutput("   /calc (will calculate to one circle above you)");
            SendOutput("   /calc <guild> (will calculate based on the guild you input)");
            SendOutput("   /calc <circle> (will calculate what you need for the circle you input)");
            SendOutput("   /calc <guild> <circle> (combination of the two above)");
            SendOutput("   The guild name must be spelled out completely, but with no spaces(moonmage, warriormage).");
            SendOutput("/sort [skillset] [rank]");
            SendOutput("   /sort (will sort your all sills)");
            SendOutput("   /sort <skillset> (will sort the skills in the skillset)");
            SendOutput("   /sort <rank> (will sort the skills greather than rank)");
            SendOutput("   /sort <skillset> <rank> (will sort the skills in the skillset)");
            SendOutput("   <rank> must always be a positive integer");
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
                        _calcGuildName = Text.Substring(Text.IndexOf("Guild: ") + 7).Trim();
                        if (!GetGuild(_calcGuildName))
                        {
                            DisplaySyntax();
                            _calculating = false;
                            return Text;
                        }
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
                                {
                                    CalculateCirclebyXML();
                                    //CalculateCircle();
                                }
                                if (_sorting)
                                    SortSkills();
                            }
                            catch (Exception ex)
                            {
                                SendOutput(ex.ToString());
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
            catch
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
            frmCicleCalc form = new frmCicleCalc(ref _host);

            if (_host.get_Variable("CircleCalc.Sort") == "1")
                form.cboSort.Text = "Bottom";
            else
                form.cboSort.Text = "Top";

            if (_host.get_Variable("CircleCalc.Display") == "1")
                form.Post200Circle.Checked = true;
            else if(_host.get_Variable("CircleCalc.Display") == "2")
                form.NextCircle.Checked=true;
            else
                form.Normal.Checked = true;

            if (_host.get_Variable("CircleCalc.Echo") == "1")
                form.chkEcho.Checked = true;
            else
                form.chkEcho.Checked = false;
            if (_host.get_Variable("CircleCalc.Log") == "1")
                form.chkLog.Checked = true;
            else
                form.chkLog.Checked = false;
            if (_host.get_Variable("CircleCalc.Parse") == "1")
                form.chkParse.Checked = true;
            else
                form.chkParse.Checked = false;
            
            if (_host.get_Variable("CircleCalc.GagFunny") == "1")
                form.chkGag.Checked = true;
            else
                form.chkGag.Checked = false;

                if (parent != null)
                    form.MdiParent = parent;

            form.Show();
        }

        #endregion

        #region Custom Parse/Display methods


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
            {
                _calcSkillList.Add(name, dRank);
                DebugOutput("Added skill " + name + " at rank " + dRank.ToString());
            }
            if (_sorting)
            {
                _sortSkillList.Add(name, dRank);
                DebugOutput("Added skill " + name + " at rank " + dRank.ToString());
            }
        }

        private void DebugOutput(string output)
        {
            if (!_debug) return;
            output = "DBG: "+output;
            if (_host.get_Variable("CircleCalc.Parse") != "0")
                _host.SendText("#parse " + output);
            if (_host.get_Variable("CircleCalc.Log") != "0")
                _host.SendText("#log \"" + output + "\"");
            if (_host.get_Variable("CircleCalc.Echo") != "0")
                _host.SendText("#echo red \"" + output + "\"");
        }


        private void SendOutput(string output)
        {
            if (_host.get_Variable("CircleCalc.Parse") != "0")
                _host.SendText("#parse " + output);
            if (_host.get_Variable("CircleCalc.Log") != "0")
                _host.SendText("#log \"" + output + "\"");
            if (_host.get_Variable("CircleCalc.Echo") != "0")
                _host.SendText("#echo \"" + output + "\"");
        }

        #endregion

        #region Circle Calculator/Skill Sorter

        private Hashtable _calcSkillList = new Hashtable();
        private Hashtable _sortSkillList = new Hashtable();
        private ArrayList reqList = new ArrayList();
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
                currentCircle = cc;
                ranksNeeded = rn;
                name = n;
                ranks = r;
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

        private bool GetGuild(string guild)
        {
            string guildcheck = guild.Trim().ToLower();
            if (_GuildNameList.ContainsKey(guildcheck))
            {
                _calcGuild = (GuildType)_GuildReqList[(string)_GuildNameList[guildcheck]];
                return true;
            }
            return false;
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
                SendOutput("Requirements for Circle " + circle.ToString() + ":");
            SendOutput("");

            foreach (CircleReq req in reqList)
            {
                if (((_host.get_Variable("CircleCalc.Sort") == "0" && req.circle != circle && LineBreak == false) ||
                     (_host.get_Variable("CircleCalc.Sort") == "1" && req.circle == circle && LineBreak == false)) && 
                     _host.get_Variable("CircleCalc.Display") != "2" )
                {
                    SendOutput("");
                    LineBreak = true;
                }

                if ((_host.get_Variable("CircleCalc.Display") == "1" || req.circle <= 200) && ((_host.get_Variable("CircleCalc.Display") != "2") || req.circle == circle) )
                    SendOutput("You have enough " + req.name + " for Circle " + req.currentCircle + " and need " + (req.ranksNeeded - req.ranks).ToString() + " (" + req.ranksNeeded + ") ranks for Circle " + req.circle);
            }
            /*
            if (_host.get_Variable("CircleCalc.Sort") == "1")
            {
                _host.SendText("#echo");
                _host.SendText("#echo Requirements for Circle " + circle.ToString() + ".");
            }
            */

            SendOutput("");
            SendOutput("TDPs Gained: " + String.Format("{0,6}", totalTDPs.ToString()));
            SendOutput("Total Ranks: " + String.Format("{0,6}", totalRanks.ToString()));
            if (_host.get_Variable("CircleCalc.GagFunny") != "1")
            {
                int seed = 0;
                System.Random randomizer;
                seed = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                randomizer = new System.Random(seed);
                int rand = randomizer.Next();
                switch (_calcGuildName)
                {
                    case "Barbarian":

                        break;
                    case "Bard":
                        if (rand % 2 == 0)
                            SendOutput("P.S. Bards suck(even with your poorly designed screams). Reroll.");
                        else
                            SendOutput("Lush.");
                        break;
                    case "Cleric":
                        SendOutput("Rezz Plz?");
                        break;
                    case "Commoner":
                        SendOutput("Join a guild you loser.");
                        break;
                    case "Empath":
                        break;
                    case "Moon Mage":
                        break;
                    case "Necromancer":
                        if (rand % 2 == 0)
                            SendOutput("Don't you think it's time to give up the evil tea parties");
                        else
                            SendOutput("Sacrified enough puppies today?");
                        break;
                    case "Paladin":
                        break;
                    case "Ranger":
                        break;
                    case "Thief":
                        break;
                    case "Trader":
                        break;
                    case "Warrior Mage":
                        SendOutput("WM Strategy:  10 prep TC, 20 cast area, 30 prep CL, 40 cast area, 50 goto 10");
                        break;
                    default:
                        break;
                }
            }
            _calcGuildName = "";
            _calculating = false;
        }

        private void ShowRanks()
        {
            string format = "{0," + (-MaxRankLen).ToString() + "} - {1," + (MaxDigitLen) + ":F2}";
            SendOutput("");
            string ListText = "";
            foreach (SkillRanks sr in sortList)
            {
                ListText = String.Format(format, sr.name, sr.rank);
                SendOutput(ListText);
            }

            SendOutput("");
            string TDPText = "";
            string TotalRanksText = "";
            TDPText = "TDPs Gained from ";
            TotalRanksText = "Total Ranks in ";
            if (_skillset == SkillSets.all)
            {
                TDPText = TDPText + _skillset.ToString() + " skillsets";
                TotalRanksText = TotalRanksText + _skillset.ToString() + " skillsets";
            }
            else if (_skillset != SkillSets.none)
            {
                TDPText = TDPText + "the " + _skillset.ToString() + " skillset";
                TotalRanksText = TotalRanksText + "the " + _skillset.ToString() + " skillset";
            }
            else
            {
                TDPText = TDPText + _CurrentSortGroup.Name;
                TotalRanksText = TotalRanksText + _CurrentSortGroup.Name;

            }
            TDPText = TDPText + ": " + String.Format("{0,6}", totalTDPs.ToString());
            TotalRanksText = TotalRanksText + ":   " + String.Format("{0,6}", totalRanks.ToString());
            SendOutput(TDPText);
            SendOutput(TotalRanksText);

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
//            foreach (DictionaryEntry skill in _sortSkillList)
//            {
//                ranks = Convert.ToInt32(Math.Floor(Convert.ToDouble(skill.Value)));
//                totalTDPs += ranks * (ranks + 1) / 2;
//                totalRanks += ranks;
//            }
//            totalTDPs = Convert.ToInt32(totalTDPs / 200);
            string skname = "";
            double skrank = 0;
            while (_sortSkillList.Count != 0)
            {
                skname = HighestSkill(_sortSkillList);
                skrank = Convert.ToDouble(_sortSkillList[skname]);
                if (_skillset != SkillSets.none || _CurrentSortGroup.Skills.Contains(skname))
                {
                    sortList.Add(new SkillRanks(skrank, skname));
                    ranks = Convert.ToInt32(Math.Floor(skrank));
                    totalTDPs += ranks * (ranks + 1) / 2;
                    totalRanks += ranks;

                    if (skname.Length > MaxRankLen)
                        MaxRankLen = skname.Length;
                    if (skrank.ToString().Length > MaxDigitLen)
                        MaxDigitLen = skrank.ToString().Length;
                }
                _sortSkillList.Remove(skname);
            }
            
            totalTDPs = Convert.ToInt32(totalTDPs / 200);

            RankComparer rankComparer = new RankComparer();
            sortList.Sort(rankComparer);
  
            ShowRanks();
            _sortSkillList.Clear();
        }

        private void CalculateCirclebyXML()
        {

            reqList = new ArrayList();

            int circle = 0;
            double ranksNeeded = 0.0;
            string skill = "";
            int currentCircle = 0;

            totalRanks = 0;
            totalTDPs = 0;
            int ranks;
            ICollection Keys = _calcSkillList.Keys;

            try
            {
                foreach (string key in Keys)
                {
                    DebugOutput("Checking Key " + key);
                    ranks = Convert.ToInt32(Math.Floor(Convert.ToDouble(_calcSkillList[key])));
                    DebugOutput("Ranks for key " + key + " are " + ranks);
                    totalTDPs += ranks * (ranks + 1) / 2;
                    totalRanks += ranks;

                    if (_calcGuild.HardReqs.ContainsKey(key))
                    {
                        DebugOutput("Key is a hard req");
                        CalculateReq3_0(ref circle, ref currentCircle, ((ReqType)_calcGuild.HardReqs[key]).Circles1to10, ((ReqType)_calcGuild.HardReqs[key]).Circles11to30, ((ReqType)_calcGuild.HardReqs[key]).Circles31to70, ((ReqType)_calcGuild.HardReqs[key]).Circles71to100, ((ReqType)_calcGuild.HardReqs[key]).Circles101to150, ((ReqType)_calcGuild.HardReqs[key]).Circles151Up, (int)(double)_calcSkillList[key], ref ranksNeeded);
                        reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), key, Convert.ToInt32(Math.Floor(Convert.ToDouble(_calcSkillList[key])))));
                        continue;
                    }
                    if (_calcGuild.SoftReqs.ContainsKey(key))
                    {
                        DebugOutput("Key is a soft req");
                        CalculateReq3_0(ref circle, ref currentCircle, ((ReqType)_calcGuild.SoftReqs[key]).Circles1to10, ((ReqType)_calcGuild.SoftReqs[key]).Circles11to30, ((ReqType)_calcGuild.SoftReqs[key]).Circles31to70, ((ReqType)_calcGuild.SoftReqs[key]).Circles71to100, ((ReqType)_calcGuild.SoftReqs[key]).Circles101to150, ((ReqType)_calcGuild.SoftReqs[key]).Circles151Up, (int)(double)_calcSkillList[key], ref ranksNeeded);
                        reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), key, (int)(double)_calcSkillList[key]));
                    }
                    //check if skill belongs in a skillset, if so add it in.
                    if (_calcGuild.Skillsets.ContainsKey(key))
                    {
                        string tempskillset = (string)_calcGuild.Skillsets[key];
                        DebugOutput("Key is a skilllset - " + tempskillset);
                        if (!_calcSkillsets.ContainsKey(tempskillset))
                        {
                            DebugOutput("inserting first key into " + tempskillset + " skillset");
                            Hashtable temphash = new Hashtable();
                            temphash.Add(key, (double)_calcSkillList[key]);
                            _calcSkillsets.Add(tempskillset, temphash);
                        }
                        else
                        {
                            ((Hashtable)_calcSkillsets[tempskillset]).Add(key, (double)_calcSkillList[key]);
                            DebugOutput("inserting non-first key into " + tempskillset + " skillset");

                        }
                    }

                }
                totalTDPs = Convert.ToInt32(totalTDPs / 200);
                DebugOutput("Finished checking all skills, now calculating against n reqs");
                foreach (ReqType Req in _calcGuild.TopN)
                {
                    DebugOutput("Checking N Req for " + Req.Name);
                    skill = HighestSkill(((Hashtable)_calcSkillsets[Req.Skillset]));
                    DebugOutput("Highest Skill is " + skill);
                    CalculateReq3_0(ref circle, ref currentCircle, Req.Circles1to10, Req.Circles11to30, Req.Circles31to70, Req.Circles71to100, Req.Circles101to150, Req.Circles151Up, (int)(double)(((Hashtable)_calcSkillsets[Req.Skillset])[skill]), ref ranksNeeded);
                    reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), Req.Name + " (" + skill + ")", (int)(double)(((Hashtable)_calcSkillsets[Req.Skillset])[skill])));
                    ((Hashtable)_calcSkillsets[Req.Skillset]).Remove(skill);
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
                ShowReqs();
            }
            catch (Exception ex)
            {
                SendOutput(ex.ToString());
            }
            finally
            {
                reqList.Clear();
                _calcSkillList.Clear();
                _calcSkillsets.Clear();
            }
        }

        private string HighestSkill(Hashtable skills)
        {
            string skillName = "";
            double ranks = -1.0;
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

        private void CalculateReq3_0(ref int circle, ref int currentCircle, double rank1, double rank2, double rank3, double rank4, double rank5, double rank6, int ranks, ref double ranksNeeded)
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
        
/*        private void CalculateBarbarian3_0()
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
            //071-100:  4       6           3       5           2
            //101-150:  5       6           4       6           3
            //151 +  :  13      15          10      15          8

            CalculateReq3_0(ref circle, ref currentCircle, 4, 4, 4, 4, 5, 13, Convert.ToInt32(_calcSkillList["Parry Ability"]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Parry Ability", Convert.ToInt32(_calcSkillList["Parry Ability"])));
            //CalculateReq3_0(ref circle, ref currentCircle, 4, 5, 6, 6, 6, 15, Convert.ToInt32(_calcSkillList["Expertise"]), ref ranksNeeded);
            //reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Expertise", Convert.ToInt32(_calcSkillList["Expertise"])));
            CalculateReq3_0(ref circle, ref currentCircle, 1, 2, 3, 3, 4, 10, Convert.ToInt32(_calcSkillList["Inner Fire"]), ref ranksNeeded);
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
            //          1st-2nd     3rd     4th
            //001-010:  4           2       1
            //011-030:  5           3       2
            //031-070:  6           3       2
            //071-100:  6           4       3
            //101-150:  6           5       4
            //151 +  :  15         13      10
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 5, 6, 6, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
            reqList.Add(new CircleReq(circle, currentCircle, Convert.ToInt32(ranksNeeded), "Primary Weapon(" + skill + ")", Convert.ToInt32(_calcSkillList[skill])));
            _calcSkillList.Remove(skill);
            skill = HighestWeapon3_0(_calcSkillList);
            CalculateReq3_0(ref circle, ref currentCircle, 4, 5, 6, 6, 6, 15, Convert.ToInt32(_calcSkillList[skill]), ref ranksNeeded);
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
            //          1st-2nd     3rd     4th
            //001-010:  2           2       1
            //011-030:  2           2       1
            //031-070:  3           2       2
            //071-100:  3           3       2
            //101-150:  3           3       2
            //151 +  :  8           8       5
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
*/
        #endregion

        private void LoadReqsfromXML()
        {
            bool reloading = _reqsLoaded;
            string Reqs = _pluginPath + @"CircleReqs.xml";
            if (!File.Exists(Reqs))
            {
                _host.EchoText("Can't open: " + Reqs);
                if (!_reqsLoaded)
                    _host.EchoText("Calculating will be disabled until a requirements file is succesfully loaded.");
                else
                    _host.EchoText("Reqs reload failed, continuing ot use old reqs.");
                return;
            }
            XmlTextReader reader = null;
            Hashtable temp_GuildNameList = new Hashtable(); //filled with guildnames, key is shortname
            Hashtable temp_GuildReqList = new Hashtable();  //filled with guilds, key is string

            try
            {
                ReqType tempReq = new ReqType();
                GuildType tempGuild = new GuildType();
                string tempguildname = "";
                string tempskillsetname = "";
                string tempreqtype = "";
                reader = new XmlTextReader(Reqs);
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "Guild")
                            {
                                tempGuild = new GuildType();
                                tempguildname = reader["name"];
                                temp_GuildNameList.Add(tempguildname, tempguildname);
                                break;
                            }
                            else if (reader.Name == "Shortname")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Shortname"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        temp_GuildNameList.Add(reader.Value.ToLower(), tempguildname);
                                }
                                break;
                            }
                            else if (reader.Name == "Skillset")
                            {
                                tempskillsetname = reader["name"];
                                break;
                            }
                            else if (reader.Name == "Skill")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Skill"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text && tempskillsetname != "")
                                        tempGuild.Skillsets.Add(reader.Value.ToString(), tempskillsetname);
                                }
                                break;
                            }
                            else if (reader.Name == "Req")
                            {
                                tempReq = new ReqType();
                                tempreqtype = reader["type"];
                                tempReq.Name = reader["name"];
                                if (tempreqtype == "N")
                                    tempReq.Skillset = reader["Skillset"];
                                else
                                    tempReq.Skillset = "";
                                break;
                            }
                            else if (reader.Name == "Circles1-10")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Circles1-10"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        tempReq.Circles1to10 = Convert.ToInt32(reader.Value);
                                }
                                break;
                            }
                            else if (reader.Name == "Circles11-30")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Circles11-30"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        tempReq.Circles11to30 = Convert.ToInt32(reader.Value);
                                }
                                break;
                            }
                            else if (reader.Name == "Circles31-70")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Circles31-70"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        tempReq.Circles31to70 = Convert.ToInt32(reader.Value);
                                }
                                break;
                            }
                            else if (reader.Name == "Circles71-100")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Circles71-100"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        tempReq.Circles71to100 = Convert.ToInt32(reader.Value);
                                }
                                break;
                            }
                            else if (reader.Name == "Circles101-150")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Circles101-150"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        tempReq.Circles101to150 = Convert.ToInt32(reader.Value);
                                }
                                break;
                            }
                            else if (reader.Name == "Circles151Up")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Circles151Up"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        tempReq.Circles151Up = Convert.ToInt32(reader.Value);
                                }
                                break;
                            }
                            if (reader.Name == "Reqs")
                            {
                                _reqsAuthor = reader["author"];
                                _reqsVer = reader["ver"];
                                break;
                            }
                            _host.EchoText("Unhandled Element: " + reader.Name);
                            if (!_reqsLoaded)
                                _host.EchoText("Calculating will be disabled until a requirements file is succesfully loaded.");
                            else
                                _host.EchoText("Reqs reload failed, continuing ot use old reqs.");
                            return;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "Guild")
                            {
                                temp_GuildReqList.Add(tempguildname, tempGuild);
                                break;
                            }
                            else if (reader.Name == "Skillset")
                            {
                                tempskillsetname = "";
                                break;
                            }
                            else if (reader.Name == "Req")
                            {
                                if (tempreqtype == "Hard")
                                    tempGuild.HardReqs.Add(tempReq.Name, tempReq);
                                else if (tempreqtype == "Soft")
                                    tempGuild.SoftReqs.Add(tempReq.Name, tempReq);
                                else
                                    tempGuild.TopN.Add(tempReq);
                                break;
                            }
                            else if (reader.Name == "Reqs")
                            {
                                break;
                            }
                            _host.EchoText("Unhandled End Element: " + reader.Name);
                            if (!_reqsLoaded)
                                _host.EchoText("Calculating will be disabled until a requirements file is succesfully loaded.");
                            else
                                _host.EchoText("Reqs reload failed, continuing ot use old reqs.");
                            return;
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.Comment:
                            break;
                        default:
                            _host.EchoText("UnhandledType: " + reader.NodeType.ToString());
                            if (!_reqsLoaded)
                                _host.EchoText("Calculating will be disabled until a requirements file is succesfully loaded.");
                            else
                                _host.EchoText("Reqs reload failed, continuing ot use old reqs.");
                            return;
                    }

                }
            }
            catch (Exception ex)
            {
                if (reader != null)
                    reader.Close();
                _host.EchoText("Exception during requirements file load.");
                _host.EchoText(ex.ToString());
                if (!_reqsLoaded)
                    _host.EchoText("Calculating will be disabled until a requirements file is succesfully loaded.");
                else
                    _host.EchoText("Reqs reload failed, continuing ot use old reqs.");
                return;
            }
            if (reader != null)
                reader.Close();
            _GuildNameList = temp_GuildNameList;
            _GuildReqList = temp_GuildReqList;
            _reqsLoaded = true;
            if (reloading)
                _host.EchoText("Reqs reload succeded.");
        }

        private void LoadSortfromXML()
        {
            bool reloading = _sortLoaded;
            string SortFile = _pluginPath + @"SortGroups.xml";
            if (!File.Exists(SortFile))
            {
                _host.EchoText("Can't open: " + SortFile);
                if (!_sortLoaded)
                    _host.EchoText("Custom sorting will be disabled until a sorting file is succesfully loaded.");
                else
                    _host.EchoText("Sorting reload failed, continuing ot use old sorting file.");
                return;
            }
            XmlTextReader reader = null;
            Hashtable temp_GroupNameList = new Hashtable();
            Hashtable temp_SortGroupList = new Hashtable();

            try
            {
                string tempgroupname = "";
                string tempskillset = "";
                SortSkillGroup tempGroup = new SortSkillGroup();
                reader = new XmlTextReader(SortFile);
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "Sort")
                            {
                                _sortAuthor = reader["author"];
                                _sortVer = reader["ver"];
                                break;
                            }
                            else if (reader.Name == "SkillGroup")
                            {
                                tempGroup = new SortSkillGroup();
                                tempGroup.Name = reader["name"];
                                tempgroupname = tempGroup.Name;
                                tempskillset = reader["skillset"];
                                tempGroup.Skillset = GetSkillSet(tempskillset);
                                break;
                            }
                            else if (reader.Name == "Shortname")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Shortname"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        temp_GroupNameList.Add(reader.Value.ToLower(), tempgroupname);
                                }
                                break;
                            }
                            else if (reader.Name == "Skill")
                            {
                                while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Skill"))
                                {
                                    reader.Read();
                                    if (reader.NodeType == XmlNodeType.Text)
                                        tempGroup.Skills.Add(reader.Value.ToString());
                                }
                                break;
                            }
                            Console.WriteLine("Unhandled Element: " + reader.Name);
                            if (!_sortLoaded)
                                _host.EchoText("Custom sorting will be disabled until a sorting file is succesfully loaded.");
                            else
                                _host.EchoText("Sorting reload failed, continuing ot use old sorting file.");
                            return;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "Sort")
                            {
                                break;
                            }
                            else if (reader.Name == "SkillGroup")
                            {
                                temp_SortGroupList.Add(tempgroupname, tempGroup);
                                tempGroup = new SortSkillGroup();
                                tempgroupname = "";
                                break;
                            }
                            else if (reader.Name == "Shortname")
                            {
                                break;
                            }
                            else if (reader.Name == "Skill")
                            {
                                break;
                            }
                            if (!_sortLoaded)
                                _host.EchoText("Custom sorting will be disabled until a sorting file is succesfully loaded.");
                            else
                                _host.EchoText("Sorting reload failed, continuing ot use old sorting file.");
                            return;
                        case XmlNodeType.Comment:
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.XmlDeclaration:
                            break;
                        default:
                            Console.WriteLine("UnhandledType: " + reader.NodeType.ToString());
                            if (!_sortLoaded)
                                _host.EchoText("Custom sorting will be disabled until a sorting file is succesfully loaded.");
                            else
                                _host.EchoText("Sorting reload failed, continuing ot use old sorting file.");
                            return;
                    }
                }

            }
            catch (Exception ex)
            {
                if (reader != null)
                    reader.Close();
                _host.EchoText("Exception during sorting file load.");
                _host.EchoText(ex.ToString());
                if (!_sortLoaded)
                    _host.EchoText("Custom sorting will be disabled until a sorting file is succesfully loaded.");
                else
                    _host.EchoText("Sorting reload failed, continuing ot use old sorting file.");
                return;
            }
            if (reader != null)
                reader.Close();
            _GroupNameList = temp_GroupNameList;
            _SortGroupList = temp_SortGroupList;
            _sortLoaded = true;
            if (reloading)
                _host.EchoText("Sorting reload succeded.");
        }
        #endregion
    }
}