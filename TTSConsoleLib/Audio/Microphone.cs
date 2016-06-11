using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            recorder = new WaveInEvent();
            samplerate = recorder.WaveFormat.SampleRate;
            recorder.DataAvailable += RecorderOnDataAvailable;
            recorder.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

            _timer = new Timer(x => UpdateThread(), null, 0, 500);
            recorder.StartRecording();


            // Create an in-process speech recognizer for the en-US locale.
            recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));

            Choices colors = new Choices();
            colors.Add(new string[] { "boom shakalaka", "Wubba lubba dub dub", "Slow the frak down", "speed the hell up", "Play Normal Speed" });
            //"Hit the sack jack",

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(colors);

            // Create the Grammar instance.
            Grammar g = new Grammar(gb);

            recognizer.LoadGrammar(g);

            // Create and load a dictation grammar.
            //recognizer.LoadGrammar(new DictationGrammar());

            // Add a handler for the speech recognized event.
            recognizer.SpeechRecognized += Sr_SpeechRecognized;

            // Configure input to the speech recognizer.
            recognizer.SetInputToDefaultAudioDevice();

            // Start asynchronous, continuous speech recognition.
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        private static void Sr_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                if (e.Result.Confidence < 0.9f)
                    return;

                var msg = e?.Result?.Text ?? String.Empty;
                msg += " - " + e.Result.Confidence.ToString();
                IRC.IRCClient.PrintConsoleMessage(msg);

                switch (e?.Result?.Text.ToLower() ?? null)
                {
                    case "boom shakalaka":
                    case "wubba lubba dub dub":
                    case "hit the sack jack":
                        SyncPool.SkipOneMessage();
                        break;
                    case "slow the frak down":
                        SyncPool.SlowDown();
                        break;
                    case "speed the hell up":
                        SyncPool.SpeedUp();
                        break;
                    case "play normal speed":
                        SyncPool.NormalSpeed();
                        break;
                    default:
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
