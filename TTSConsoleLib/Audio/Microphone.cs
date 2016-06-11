using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static void Init()
        {
            recorder = new WaveInEvent();
            samplerate = recorder.WaveFormat.SampleRate;
            recorder.DataAvailable += RecorderOnDataAvailable;
            recorder.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

            _timer = new Timer(x => UpdateThread(), null, 0, 500);
            recorder.StartRecording();
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
