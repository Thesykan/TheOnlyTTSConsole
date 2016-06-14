using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TTSConsoleLib.IRC;
using TTSConsoleLib.Utils;

namespace TTSConsoleLib.Modules
{
    class SoundSystem
    {
        private static WaveOut waveOut;

        public static void StartASync(int frequency, float duration = 5.0f)
        {
            ThreadPool.QueueUserWorkItem(x => Start(frequency,duration));
        }

        private static void Start(int frequency, float pDuration)
        {
            try
            {
                if (waveOut != null)
                {
                    try
                    {
                        waveOut.Stop();
                        waveOut.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex.ToString());
                    }
                    finally
                    {
                        waveOut = null;
                    }
                }

                frequency = Math.Min(700, frequency);

                var sineWaveProvider = new SineWaveProvider32(frequency, 0.05f, pDuration);
                sineWaveProvider.SetWaveFormat(44100, 2); // 16kHz mono
                using (waveOut = new WaveOut())
                {
                    waveOut.Init(sineWaveProvider);
                    waveOut.Play();

                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(2000);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        //Handle Incoming IRC Messages
        private static String[] _commands = { "!sin" };
        public static bool HandleMessages(IRCMessage pMessageInfo)
        {
            IRC.IRCClient.CheckCommand(pMessageInfo , _commands, ((x) =>
            {
                int frequency = 100;
                if (int.TryParse(x.commandParam, out frequency))
                {
                    StartASync(frequency);
                }
            }));
            return false;
        }
    }


    public abstract class WaveProvider32 : IWaveProvider
    {
        protected float _duration = 5.0f;

        private WaveFormat waveFormat;

        public WaveProvider32()
            : this(44100, 1)
        {
        }

        public WaveProvider32(int sampleRate, int channels)
        {
            SetWaveFormat(sampleRate, channels);
        }

        public void SetWaveFormat(int sampleRate, int channels)
        {
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            WaveBuffer waveBuffer = new WaveBuffer(buffer);
            int samplesRequired = count / 4;
            int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
            return samplesRead * 4;
        }

        public abstract int Read(float[] buffer, int offset, int sampleCount);

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }
    }

    public class SineWaveProvider32 : WaveProvider32
    {
        int sample;
        float frequencyOffset;

        public SineWaveProvider32()
        {
            Frequency = 1000;
            Amplitude = 0.25f; // let's not hurt our ears            
        }

        public SineWaveProvider32(float pFrequency, float pAmplitude, float pDuration)
        {
            _duration = pDuration;
            Frequency = pFrequency;
            Amplitude = Math.Min(0.25f, pAmplitude); // let's not hurt our ears        
            frequencyOffset = Frequency / _duration;
        }

        //private String expression;
        //public SineWaveProvider32(float pFrequency, float pAmplitude, String pExpression)
        //{
        //    Frequency = pFrequency;
        //    Amplitude = Math.Min(0.25f, pAmplitude); // let's not hurt our ears        
        //    frequencyOffset = Frequency / _duration;

        //    expression = pExpression;
        //}

        public float Frequency { get; protected set; }
        public float Amplitude { get; protected set; }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            //x^4 - x^3 + 1

            //buffer[n + offset] = (float)(Amplitude * Math.Sin((2 * Math.PI * sample * Frequency) / sampleRate));


            int sampleRate = WaveFormat.SampleRate;
            for (int n = 0; n < sampleCount; n++)
            {
                buffer[n + offset] = (float)(Amplitude * Math.Sin((2 * Math.PI * sample * Frequency) / sampleRate));
                sample++;
                if (sample >= sampleRate)
                {
                    sample = 0;
                    //Frequency-= frequencyOffset;
                }
                
                Frequency -= Math.Max(float.Epsilon,frequencyOffset / sampleCount);

                Frequency = Math.Max(5f, Frequency);
            }

            if(Frequency < 15)
            {
                return 0;
            }

            return sampleCount;
        }
    }

}
