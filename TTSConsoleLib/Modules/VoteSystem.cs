using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TTSConsoleLib.IRC;

namespace TTSConsoleLib.Modules
{
    /// <summary>
    /// New Voting system to use SQLite
    /// </summary>
    public class VoteSystem_v2
    {
        public static bool HandleMessages(IRCMessage pMessageInfo)
        {
            //New Polls?
            IRCClient.CheckCommand(pMessageInfo, new String[] { "!newpoll" }, x =>
            {
                if (x.NumberOfParameters > 2)
                {
                    String name = x.commandParamSeperated[0].Trim();
                    List<String> pollOptions = new List<string>();
                    for (int i = 1; i < x.NumberOfParameters; i++)
                    {
                        pollOptions.Add(x.commandParamSeperated[i]);
                    }
                    MemorySystem._instance.NewPoll(name, pollOptions.ToArray(), DateTime.Now.AddMinutes(5));

                    var Options = String.Join(" OR ", pollOptions);
                    IRCClient.SendIRCAnPrintConsoleMessage(name + " Poll Created!!!");
                    IRCClient.SendIRCAnPrintConsoleMessage("Type !" + name + " and one of these options [" + Options + "] to vote");
                }
                else
                {
                    IRCClient.SendIRCAnPrintConsoleMessage("Not Enough Poll Options >.<");
                    //Print Angry Message
                }
            });

            IRCClient.CheckCommand(pMessageInfo, new String[] { "!endpoll" }, x =>
            {
                var msg = MemorySystem._instance.EndPoll(x.commandParam);
                if(msg == null)
                    IRCClient.SendIRCAnPrintConsoleMessage("Poll Not Found >.<");
                else
                    IRCClient.SendIRCAnPrintConsoleMessage(msg);
            });

            IRCClient.CheckCommand(pMessageInfo, MemorySystem._instance.CurrentPolls(), x =>
            {
                MemorySystem._instance.AddUserToPollWithOption(pMessageInfo.command, 
                    pMessageInfo.userName, 
                    pMessageInfo.commandParam);
                //Handle Voting On Poll Via SQLite
            });

            return false;
        }
    }


    public class VoteSystem
    {
        public static List<Poll> ActivePolls;

        /// <summary>
        /// Start New Poll
        /// </summary>
        /// <returns>Poll Id</returns>
        public static void StartPoll(Poll pPoll)
        {
            if (!ActivePolls.Contains(pPoll))
                ActivePolls.Add(pPoll);

            pPoll.Start();
        }

        /// <summary>
        /// End Poll and Get Results
        /// </summary>
        /// <param name="pPoll"></param>
        /// <returns>String Results</returns>
        public static String EndPoll(Poll pPoll)
        {
            if (ActivePolls.Contains(pPoll))
                ActivePolls.Remove(pPoll);

            pPoll.End();

            //Add mods 

            return pPoll.GetResults();

            //return "N/A";
        }

        //Handle Incoming IRC Messages
        public static bool HandleMessages(IRCMessage pMessageInfo)
        {
            var message = pMessageInfo.message.ToLower();

            var result = false;
            //New Polls?
            var split = message.Split(new String[] { "!newpoll" }, StringSplitOptions.None);
            if (split.Length > 1)
            {
                //New Poll Detected.
                var nameAndOptions = split[1].Split(',');
                if (nameAndOptions.Length > 2)
                {
                    String name = nameAndOptions[0].Trim();
                    var poll = new Poll(name, "!" + name);
                    for (int i = 1; i < nameAndOptions.Length; i++)
                    {
                        poll.AddOption(nameAndOptions[i]);
                    }

                    StartPoll(poll);

                    var Options = String.Join(" OR ", nameAndOptions, 1, nameAndOptions.Length - 1);

                    IRCClient.SendIRCAnPrintConsoleMessage(name + " Poll Created!!!");
                    IRCClient.SendIRCAnPrintConsoleMessage("Type !" + name + " and one of these options [" + Options + "] to vote");
                    result = true;
                }
                else
                {
                    IRCClient.SendIRCAnPrintConsoleMessage("Not Enough Poll Options >.<");
                    //Print Angry Message
                }
            }

            //End Polls?
            var split2 = message.Split(new String[] { "!endpoll" }, StringSplitOptions.None);
            if (split2.Length > 1)
            {
                var poll = ActivePolls.FirstOrDefault(w => split2[1].Contains(w.PollName));

                //New Poll Detected.
                if (poll != null)
                {
                    var msg = EndPoll(poll);
                    IRCClient.SendIRCAnPrintConsoleMessage(msg);
                    result = true;
                }
                else
                {
                    IRCClient.SendIRCAnPrintConsoleMessage("Poll Not Found >.<");
                    //Print Angry Message
                }
            }


            foreach (var poll in ActivePolls)
            {
                if (poll.HandleMessages(pMessageInfo))
                {
                    result = true;
                }
            }
            return result;
        }

        private static Timer _voteTimer;
        public static void Init()
        {
            ActivePolls = new List<Poll>();
            _voteTimer = new Timer(x => VoteThread(), null, 0, 60000);
        }
        private static void VoteThread()
        {
            Poll[] pollArray = ActivePolls.ToArray();
            for (int i = 0; i < pollArray.Length; i++)
            {
                if (pollArray[i].TimesUp())
                {
                    var msg = EndPoll(pollArray[i]);
                    IRCClient.SendIRCAnPrintConsoleMessage(msg);
                }
            }
        }

    }

    public class Poll
    {
        public Poll(String pName, params String[] pCommands)
        {
            PollName = pName;
            Options = new List<PollOption>();
            Commands = new List<String>();
            EnteredUsers = new List<String>();
            for (int i = 0; i < pCommands.Length; i++)
            {
                Commands.Add(pCommands[i].ToLower());
            }
        }
        public void AddOption(String pOption)
        {
            Options.Add(new PollOption(pOption.ToLower()));
        }

        public String PollName;
        public List<PollOption> Options; // 1 Or 2 Or 3
        public List<String> Commands; //!Poll Or !P 
        public List<String> EnteredUsers;
        public DateTime EndPollTime;

        private bool _Active = false;
        public void Start()
        {
            _Active = true;
            EndPollTime = DateTime.Now.AddMinutes(10);
        }
        public void End()
        {
            _Active = false;
        }

        public bool TimesUp()
        {
            if(EndPollTime == null)
                return true;
            return (DateTime.Now > EndPollTime);
        }

        public String GetResults()
        {
            if(Options.Count == 0 || Commands.Count == 0)
            {
                return "You F~ up";
            }

            if (EnteredUsers.Count == 0)
            {
                return "No One Cares About this Poll ~.~";
            }

            String ReturnChar = "    ";
            String result = String.Empty;
            result = PollName + ReturnChar;
            PollOption Winner = null;
            List<PollOption> SameCountAsWinner = new List<PollOption>();
            foreach (var option in Options)
            {
                result += option.Name + " has " + option.Users.Count + " Votes, " + ReturnChar;

                if (Winner == null)
                {
                    Winner = option;
                    SameCountAsWinner = new List<PollOption>();
                    continue;
                }

                if (option.Users.Count > Winner.Users.Count)
                {
                    Winner = option;
                    SameCountAsWinner = new List<PollOption>();
                }
                else if (option.Users.Count == Winner.Users.Count)
                {
                    SameCountAsWinner.Add(option);
                }
            }

            if(SameCountAsWinner.Count > 0)
            {
                result += "There was a DRAW Nobody WINS >D";
            }
            else
            {
                result += Winner.Name + " Wins With " + Winner.Users.Count + ReturnChar;
            }
            

            //PollName
            //Option1 : #
            //Option2 : #
            //Option1 Wins
            //return "N/A";
            return result;
        }

        //Handle Incoming IRC Messages
        public bool HandleMessages(IRCMessage pMessageInfo)
        {
            //Check Active
            if (!_Active)
                return false;

            //Already Entered
            if (EnteredUsers.Contains(pMessageInfo.userName))
                return false;

            var result = false;
            var split = pMessageInfo.message.Split(Commands.ToArray(), StringSplitOptions.None);
            //found a command
            if(split.Length > 1)
            {
                var option = split[1];
                var foundOption = Options.Where(w => option.Contains(w.Name)).FirstOrDefault();
                //Valid Option
                if(foundOption != null)
                {
                    EnteredUsers.Add(pMessageInfo.userName);
                    foundOption.Users.Add(pMessageInfo.userName);
                    result = true;
                }
            }

            return result;
        }
    }

    public class PollOption
    {
        public PollOption(String pName)
        {
            Name = pName;
            Users = new List<string>();
        }

        public String Name; // 1
        public List<String> Users;
    }
}
