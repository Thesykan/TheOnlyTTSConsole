using System;
using System.IO;
using System.Linq;
using System.Threading;

public delegate void StringInput(string pUsername, string pInput);

namespace SimpleConsole
{
    internal class Program
    {
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


            if (string.IsNullOrEmpty(channel))
                channel = username; // join own channel.

            var ex = new IrcExample();
            ex.Connect(WriteLine, channel);

            while (true)
            {
                var writeMessage = Console.ReadLine();
                if (writeMessage?.Trim() == "q")
                    break;
                if (writeMessage?.Trim() != string.Empty)
                    ex.Client.SendMessage(writeMessage, channel);
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

        private static void WriteLine(string pUsername, string pText)
        {
            if (pUsername.Contains("bot"))
                return;

            var words = pText.Split(' ').ToArray();
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].ToLower().Contains("http"))
                {
                    words[i] = string.Empty;
                }
            }
            pText = string.Join(" ", words);

            if (pUsername.Length > MaxLengthSoFar)
                MaxLengthSoFar = pUsername.Length;

            if (pUsername.Length < MinLengthSoFar)
                MinLengthSoFar = pUsername.Length;

            SyncPool.Init();

            ThreadPool.QueueUserWorkItem(x => { SyncPool.SpeakText(pUsername, pText); });

            string hour = FormatTime(DateTime.Now.Hour);
            string minutes = FormatTime(DateTime.Now.Minute);
            // default color
            //            Console.WriteLine($"{hour}:{minutes} - {pUsername}: {pText}");

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
            Console.WriteLine(pText);
        }

        private static string FormatTime(int i)
        {
            return (i > 9 ? "" : "0") + i;
        }
    }
}