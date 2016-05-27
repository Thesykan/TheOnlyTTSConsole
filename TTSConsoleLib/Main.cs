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

    public class Main
    {
        private String _userName;

        private WriteToUser Write;
        private WriteLineToUser WriteLine;
        public void Start(QuestionUser pMethod, WriteToUser pWriteMethod, WriteLineToUser pWriteLineMethod)
        {
            Write = pWriteMethod;
            WriteLine = pWriteLineMethod;

            string _userName = string.Empty;

            if (!File.Exists("Username.USER"))
            {
                _userName = pMethod("UserName?");
                File.WriteAllText("Username.USER", _userName);
            }

            if (!File.Exists("SecretTokenDontLOOK.TOKEN"))
            {
                var password = pMethod("Token?");
                File.WriteAllText("SecretTokenDontLOOK.TOKEN", password);
            }

            if (string.IsNullOrEmpty(Twitch.TwitchAPI._channel))
                Twitch.TwitchAPI._channel = _userName; // join own channel.

            IRCClient.Connect(WriteToConsole, Twitch.TwitchAPI._channel);

            //VoteSystem.Init(SendIRCMessage);

            while (true)
            {
                try
                {
                    var writeMessage = Console.ReadLine();
                    if (writeMessage?.Trim() == "q")
                        break;
                    if (writeMessage?.Trim() != string.Empty)
                        WriteToConsole(_userName, writeMessage);
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

            var words = pMessage.Split(' ').ToArray();
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].ToLower().Contains("http"))
                {
                    words[i] = string.Empty;
                }
            }
            pMessage = string.Join(" ", words);

            SyncPool.Init();

            bool SpeakText = true;

            if (VoteSystem.HandleMessages(pUsername, pMessage))
                SpeakText = false;

            if (UserManager.IsSpeachBannedUser(pUsername))
                SpeakText = false;

            if (pMessage.Trim().StartsWith("!"))
                SpeakText = false;

            if (SpeakText)
                SyncPool.SpeakText(pUsername, pMessage);

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
