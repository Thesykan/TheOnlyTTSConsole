using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TTSConsoleLib.Modules;

namespace TTSConsoleLib.Audio
{
    //https://blogs.msdn.microsoft.com/dawate/2009/06/23/intro-to-audio-programming-part-2-demystifying-the-wav-format/
    public class Microphone
    {

        static WaveInEvent recorder;
        static Timer _timer;
        static int samplerate = 1;
        static SpeechRecognitionEngine recognizer;
        public static void Init()
        {
            //"boom shakalaka", "Wubba lubba dub dub", "Slow the frak down", "speed the hell up", "Play Normal Speed"
            //"Hit the sack jack",

            recorder = new WaveInEvent();
            samplerate = recorder.WaveFormat.SampleRate;
            recorder.DataAvailable += RecorderOnDataAvailable;
            recorder.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

            _timer = new Timer(x => UpdateThread(), null, 0, 500);
            recorder.StartRecording();


            // Create an in-process speech recognizer for the en-US locale.
            recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));

            Choices normalCommands = new Choices();
            normalCommands.Add(new string[] { "Clear Queue" });
            normalCommands.Add(new string[] { "Speed Up" });
            normalCommands.Add(new string[] { "Slow Down" });
            normalCommands.Add(new string[] { "Normal speed" });

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(normalCommands);


            //Stop TTS For a user.
            //BAN HAMMER and/or TIMEOUT?
            //Number of TTS's (default 2) 

            
            //DynamicCommands.Add(new string[] { "Repeat dfoxlive last 5 messages" });
            GrammarBuilder gb2 = new GrammarBuilder();
            gb2.Append("repeat");
            gb2.AppendDictation();
            gb2.Append("message");



            GrammarBuilder finalgb = new GrammarBuilder(new Choices(gb, gb2));

            // Create the Grammar instance.
            Grammar g = new Grammar(finalgb);

            recognizer.LoadGrammar(g);

            // Create and load a dictation grammar.
            //recognizer.LoadGrammar(new DictationGrammar());

            // Add a handler for the speech recognized event.
            recognizer.SpeechRecognized += Sr_SpeechRecognized;

            // Configure input to the speech recognizer.
            recognizer.SetInputToDefaultAudioDevice();

            //DEFAULT OFF.
            // Start asynchronous, continuous speech recognition.
            //recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        public static void ToggleListen(bool pListen)
        {
            if (recognizer.AudioState == AudioState.Stopped)
            {
                if (pListen)
                {
                    SoundSystem.StartASync(300,3);
                    recognizer.RecognizeAsync(RecognizeMode.Multiple);
                }
            }
            else
            {
                if (!pListen)
                {
                    //SoundSystem.StartASync(200);
                    recognizer.RecognizeAsyncStop();
                }
            }
        }

        private static void Sr_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                SoundSystem.StartASync(100);

                //if (e.Result.Confidence < 0.9f)
                //    return;

                var msg = e?.Result?.Text?.ToLower() ?? String.Empty;
                IRC.IRCClient.PrintConsoleMessage(msg + " - " + e.Result.Confidence.ToString());

                switch (msg)
                {
                    case "clear queue":
                        SyncPool.SkipALLMessages();
                        break;
                    case "slow down":
                        SyncPool.SlowDown();
                        break;
                    case "speed up":
                        SyncPool.SpeedUp();
                        break;
                    case "normal speed":
                        SyncPool.NormalSpeed();
                        break;
                    default:

                        //Advanced Command?

                        if(msg.Contains("repeat") && msg.Contains("message"))
                        {
                            var user = msg.Replace("repeat", "").Replace("message", "");
                            SyncPool.RepeatFuzzyUser(user);
                        }

                        break;
                }

            }
            catch (Exception ex)
            {
                Utils.Logger.Log(ex.ToString());
            }
        }

        static bool paused = false;
        static DateTime talkingTime = DateTime.Now;
        public static void UpdateThread()
        {
            //Check to see if I'm Talking...
            if (talking)
            {
                if (!paused)
                {
                    paused = true;
                    SyncPool.Pause();
                    //IRC.IRCClient.PrintConsoleMessage("Pause");
                }
            }
            else
            {
                if (paused)
                {
                    SyncPool.Resume();
                    //IRC.IRCClient.PrintConsoleMessage("Resume");
                    paused = false;
                }
            }
        }

        private static bool talking;
        private static void RecorderOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            float[] waveBuffer = new WaveBuffer(waveInEventArgs.Buffer);
            bool b = false;
            for (int i = 0; i < waveInEventArgs.BytesRecorded / 4; i++)
            {
                if (waveBuffer[i] > .10f) //Simple Gate.
                {
                    talking = true;
                    talkingTime = DateTime.Now;
                    b = true;
                    break;
                }
            }
            //Only set to false if everything is lower then threshold
            if (!b)
            {
                if ((DateTime.Now - talkingTime).TotalSeconds > 1)
                {
                    talking = false;
                }
            }
        }



    }
}
