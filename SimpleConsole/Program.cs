using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public delegate void StringInput(String p_username, String p_input);

namespace SimpleConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists("Username.USER"))
            {
                String username = GetResponseFromUser("UserName?");
                File.WriteAllText("Username.USER", username);
            }

            if (!File.Exists("SecretTokenDontLOOK.TOKEN"))
            {
                String password = GetResponseFromUser("Token?");
                File.WriteAllText("SecretTokenDontLOOK.TOKEN", password);
            }

            IrcExample ex = new IrcExample();
            ex.Connect(WriteLine);

            while (true)
            {
                String WriteMessage = Console.ReadLine();
                ex.client.SendMessage(WriteMessage, "#theonlysykan");
            }

        }
        
        static private String GetResponseFromUser(String p_question)
        {
            Console.WriteLine(p_question);
            return Console.ReadLine();
        }

        static SpeechSynthesizer _synth;
        static ReadOnlyCollection<InstalledVoice> _voices;
        static int index = 0;
        static int maxLengthSoFar = int.MinValue;
        static int minLengthSoFar = int.MaxValue;
        static private void WriteLine(String p_username, String p_text)
        {
            if (p_username.Contains("bot"))
                return;

            var words = p_text.Split(' ').ToArray();
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Contains("http"))
                {
                    words[i] = String.Empty;
                }
            }
            p_text = String.Join(" ",words);


            if (p_username.Length > maxLengthSoFar)
                maxLengthSoFar = p_username.Length;

            if (p_username.Length < minLengthSoFar)
                minLengthSoFar = p_username.Length;

            SyncPool.Init();

            ThreadPool.QueueUserWorkItem(new WaitCallback((x) => { SyncPool.SpeakText(p_username, p_text); }));

            Console.WriteLine(p_text);
        }



    }
}
