using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;

public delegate void StringInput(string pUsername, string pInput);

namespace SimpleConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (!File.Exists("Username.USER"))
            {
                var username = GetResponseFromUser("UserName?");
                File.WriteAllText("Username.USER", username);
            }

            if (!File.Exists("SecretTokenDontLOOK.TOKEN"))
            {
                var password = GetResponseFromUser("Token?");
                File.WriteAllText("SecretTokenDontLOOK.TOKEN", password);
            }

            var ex = new IrcExample();
            ex.Connect(WriteLine);

            while (true)
            {
                var writeMessage = Console.ReadLine();
                ex.Client.SendMessage(writeMessage, "#theonlysykan");
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
        static int _maxLengthSoFar = -256;
        static int _minLengthSoFar = 256;

        private static void WriteLine(string pUsername, string pText)
        {
            if (pUsername.Contains("bot"))
                return;

            var words = pText.Split(' ').ToArray();
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Contains("http"))
                {
                    words[i] = string.Empty;
                }
            }
            pText = string.Join(" ", words);

            if (pUsername.Length > _maxLengthSoFar)
                _maxLengthSoFar = pUsername.Length;

            if (pUsername.Length < _minLengthSoFar)
                _minLengthSoFar = pUsername.Length;

            SyncPool.Init();

            ThreadPool.QueueUserWorkItem(x => { SyncPool.SpeakText(pUsername, pText); });

            Console.WriteLine(pText);
        }
    }
}