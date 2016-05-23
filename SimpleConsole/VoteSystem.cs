using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleConsole
{
    public class VoteSystem
    {
        public static List<Poll> ActivePolls;

        /// <summary>
        /// Start New Poll
        /// </summary>
        /// <returns>Poll Id</returns>
        public static void StartPoll(Poll pPoll)
        {
            if(!ActivePolls.Contains(pPoll))
                ActivePolls.Add(pPoll);

            pPoll.Start();
        }
        
        /// <summary>
        /// End Poll and Get Results
        /// </summary>
        /// <param name="pPollId"></param>
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
        public static bool HandleMessages(String pUserName, String pMessage)
        {
            pMessage = pMessage.ToLower();

            var result = false;
            //New Polls?
            var split = pMessage.Split(new String[] { "!newpoll" }, StringSplitOptions.None);
            if(split.Length> 1)
            {
                //New Poll Detected.
                var nameAndOptions = split[1].Split(',');
                if (nameAndOptions.Length > 2)
                {
                    String name = nameAndOptions[0].Trim();
                    var poll = new Poll(name, "!"+ name);
                    for (int i = 1; i < nameAndOptions.Length; i++)
                    {
                        poll.AddOption(nameAndOptions[i]);
                    }

                    StartPoll(poll);

                    var Options = String.Join(" OR ", nameAndOptions, 1, nameAndOptions.Length - 1);

                    _messageSend(name + " Poll Created!!!");
                    _messageSend("Type !" + name + " and one of these options [" + Options + "] to vote");
                    result = true;
                }
                else
                {
                    _messageSend("Not Enough Poll Options >.<");
                    //Print Angry Message
                }
            }

            //End Polls?
            var split2 = pMessage.Split(new String[] { "!endpoll" }, StringSplitOptions.None);
            if (split2.Length > 1)
            {
                var poll = ActivePolls.Where(w => split2[1].Contains(w.PollName)).FirstOrDefault();

                //New Poll Detected.
                if (poll != null)
                {
                    var msg = EndPoll(poll);
                    _messageSend(msg);
                    result = true;
                }
                else
                {
                    _messageSend("Poll Not Found >.<");
                    //Print Angry Message
                }
            }


            foreach (var poll in ActivePolls)
            {
                if(poll.HandleMessages(pUserName, pMessage))
                {
                    result = true;
                }
            }
            return result;
        }

        private static SendIRCMessage _messageSend;
        public static void Init(SendIRCMessage pIrcConnect)
        {
            ActivePolls = new List<Poll>();
            _messageSend = pIrcConnect;

            Thread thread = new Thread(new ThreadStart(VoteThread));
        }

        public static void VoteThread()
        {
            while (true)
            {
                Poll[] pollArray = ActivePolls.ToArray();
                for (int i = 0; i < pollArray.Length; i++)
                {
                    if (pollArray[i].TimesUp())
                    {
                        var msg = EndPoll(pollArray[i]);
                        _messageSend(msg);
                    }
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
                }
                else if (option.Users.Count > Winner.Users.Count)
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
        public bool HandleMessages(String pUserName, String pMessage)
        {
            //Check Active
            if (!_Active)
                return false;

            //Already Entered
            if (EnteredUsers.Contains(pUserName))
                return false;

            var result = false;
            var split = pMessage.Split(Commands.ToArray(), StringSplitOptions.None);
            //found a command
            if(split.Length > 1)
            {
                var option = split[1];
                var foundOption = Options.Where(w => option.Contains(w.Name)).FirstOrDefault();
                //Valid Option
                if(foundOption != null)
                {
                    EnteredUsers.Add(pUserName);
                    foundOption.Users.Add(pUserName);
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
