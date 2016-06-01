using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSConsoleLib.Audio;
using TTSConsoleLib.IRC;
using TTSConsoleLib.Modules;
using TTSConsoleLib.Twitch;
using TTSConsoleLib.Utils;

namespace TTSConsoleLib
{
    public delegate String QuestionUser(String pMessage);
    public delegate void WriteLineToUser(String pMessage);
    public delegate void WriteToUser(String pMessage, ConsoleColor pColor);
    public delegate void HandleUserInput(String pUserName, String pMessage);

    public class Main
    {
        private String _userName;

        private WriteToUser Write;
        private WriteLineToUser WriteLine;
        public void Start(QuestionUser pMethod, WriteToUser pWriteMethod, WriteLineToUser pWriteLineMethod)
        {
            Write = pWriteMethod;
            WriteLine = pWriteLineMethod;

            string userName = string.Empty;
            if (!File.Exists("Username.USER"))
            {
                userName = pMethod("UserName?");
                File.WriteAllText("Username.USER", userName);
            }
            else
            {
                userName = File.ReadAllText("Username.USER");
            }

            if (!File.Exists("SecretTokenDontLOOK.TOKEN"))
            {
                var password = pMethod("Token?");
                File.WriteAllText("SecretTokenDontLOOK.TOKEN", password);
            }

            if (!File.Exists("Channel.JOIN"))
            {
                var channel = pMethod("Channel?(Leave blank for your own channel)");

                if (String.IsNullOrEmpty(channel))
                    channel = "#" + userName;

                if (!channel.StartsWith("#"))
                    channel = "#" + channel;

                File.WriteAllText("Channel.JOIN", channel);
                Twitch.TwitchAPI._channel = channel;
            }
            else
            {
                Twitch.TwitchAPI._channel = File.ReadAllText("Channel.JOIN");
            }

            IRCClient.Connect(WriteToConsole, Twitch.TwitchAPI._channel);

            VoteSystem.Init(WriteToConsole);
            UserManager.Init(WriteToConsole);

            while (true)
            {
                try
                {
                    var writeMessage = Console.ReadLine();
                    if (writeMessage?.Trim() == "q")
                        break;
                    if (writeMessage?.Trim() != string.Empty)
                    {
                        IRCClient.SendIRCMessage(writeMessage);
                        WriteToConsole(userName, writeMessage);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
            }
        }

        private void WriteToConsole(string pUsername, string pMessage)
        {
            if (pUsername.Contains("bot"))
                return;

            var message = pMessage;
            var words = message.Split(' ').ToArray();
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].ToLower().Contains("http"))
                {
                    words[i] = string.Empty;
                }
            }
            message = string.Join(" ", words);

            SyncPool.Init();

            bool SpeakText = true;

            if (VoteSystem.HandleMessages(pUsername, message))
                SpeakText = false;

            if (UserManager.IsSpeachBannedUser(pUsername))
                SpeakText = false;

            if (UserManager.HandleMessages(pUsername, message))
                SpeakText = false;

            if (SoundSystem.HandleMessages(pUsername, pMessage))
                SpeakText = false;

            if (message.Trim().StartsWith("!"))
                SpeakText = false;

            if (SpeakText)
                SyncPool.SpeakText(pUsername, message);

            string hour = FormatTime(DateTime.Now.Hour);
            string minutes = FormatTime(DateTime.Now.Minute);
            // default color
            //            Console.WriteLine($"{hour}:{minutes} - {pUsername}: {pText}");
            int viewers = TwitchAPI.GetNumberOfViewers();
            int followers = TwitchAPI.GetNumberOfFollowers();
            String uptime = TwitchAPI.GetUpdateTime();

            // UpTime
            Write($"{uptime}", ConsoleColor.Red);
            // Spacer
            Write($" - ", ConsoleColor.Yellow);

            // Followers
            Write($"{followers}", ConsoleColor.Red);
            // Spacer
            Write($" - ",ConsoleColor.Yellow);

            // Viewers
            Write($"{viewers}", ConsoleColor.Red);
            // Spacer
            Write($" - ", ConsoleColor.Yellow);

            // color coded for readability.
            // Time
            Write($"{hour}:{minutes}", ConsoleColor.Cyan);
            // Spacer
            Write($" - ", ConsoleColor.Yellow);
            // Username
            // TODO: Add some Color Randomization based off username
            Write(pUsername,ConsoleColor.Cyan);
            // Spacer
            Write($": ", ConsoleColor.Yellow);
            // Message
            WriteLine(pMessage);
        }

        private static string FormatTime(int i)
        {
            return (i > 9 ? "" : "0") + i;
        }
    }
}
