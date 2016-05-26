using System;
using System.IO;
using System.Linq;
using System.Threading;

public delegate void StringInput(string pUsername, string pInput);

namespace SimpleConsole
{
    public delegate void SendIRCMessage(String pMessage, String pChannel = null);

    internal class Program
    {

        static string _channel = "#timthetatman";
        static IRCClient IRCClient = new IRCClient();

        private static void Main(string[] args)
        {
            string username = string.Empty;

            if (!File.Exists("Username.USER"))
            {
                username = GetResponseFromUser("UserName?");
                File.WriteAllText("Username.USER", username);
            }

            if (!File.Exists("SecretTokenDontLOOK.TOKEN"))
            {
                var password = GetResponseFromUser("Token?");
                File.WriteAllText("SecretTokenDontLOOK.TOKEN", password);
            }

            if (string.IsNullOrEmpty(_channel))
                _channel = username; // join own channel.

            IRCClient.Connect(WriteToConsole, _channel);

            VoteSystem.Init(SendIRCMessage);

            while (true)
            {
                try
                {
                    var writeMessage = Console.ReadLine();
                    if (writeMessage?.Trim() == "q")
                        break;
                    if (writeMessage?.Trim() != string.Empty)
                        SendIRCMessage(writeMessage);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
            }
        }

        private static void SendIRCMessage(String pMessage, String pChannel = null)
        {
            try
            {
                if (pChannel == null)
                    IRCClient.Client.SendMessage(pMessage, _channel);
                else
                    IRCClient.Client.SendMessage(pMessage, pChannel);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        private static string GetResponseFromUser(string pQuestion)
        {
            Console.WriteLine(pQuestion);
            return Console.ReadLine();
        }

//        static SpeechSynthesizer _synth;
//        static ReadOnlyCollection<InstalledVoice> _voices;
//        static int _index = 0;
        public static int MaxLengthSoFar { get; private set; } = -256;
        public static int MinLengthSoFar { get; private set; } = 256;

        private static void WriteToConsole(string pUsername, string pMessage)
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

            if (pUsername.Length > MaxLengthSoFar)
                MaxLengthSoFar = pUsername.Length;

            if (pUsername.Length < MinLengthSoFar)
                MinLengthSoFar = pUsername.Length;

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
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{uptime}");
            // Spacer
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($" - ");

            // Followers
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{followers}");
            // Spacer
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($" - ");

            // Viewers
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{viewers}");
            // Spacer
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($" - ");

            // color coded for readability.
            // Time
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{hour}:{minutes}");
            // Spacer
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($" - ");
            // Username
            // TODO: Add some Color Randomization based off username
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(pUsername);
            // Spacer
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($": ");
            // Message
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(pMessage);
        }

        private static string FormatTime(int i)
        {
            return (i > 9 ? "" : "0") + i;
        }
    }
}