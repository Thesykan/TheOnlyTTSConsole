using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleConsole
{
    class SyncPool
    {

        static List<Sync> _syncList = null;
        public static void Init()
        {
            _readyStateSyncQueue = new Queue<Sync>();
            if (_syncList == null)
            {
                _syncList = new List<Sync>();
                _syncList.Add(new Sync(Queue));
                _syncList.Add(new Sync(Queue));
                _syncList.Add(new Sync(Queue));
            }
        }

        static int syncIndex = 0;
        static ManualResetEvent _resetEvent = new ManualResetEvent(false);
        public static void SpeakText(String p_username, String p_text)
        {
            Sync synth = null;

            syncIndex++;
            if (syncIndex >= _syncList.Count)
                syncIndex = 0;

            synth = _syncList[syncIndex];

            //while (true)
            //{
            //    if (_readyStateSyncQueue.Count < 1)
            //    {
            //        _resetEvent.WaitOne(1000);
            //        _resetEvent.Reset();
            //    }

            //    lock (_resetEvent)
            //    {
            //        if (_readyStateSyncQueue.Count > 0)
            //        {
            //            synth = _readyStateSyncQueue.Dequeue();
            //            break;
            //        }
            //    }
            //}

            //if (synth == null)
            //{
            //    SpeakText(p_username, p_text);
            //    return;
            //}
            
            synth._synth.Rate = 2;
            synth._synth.Speak(p_username);

            synth.SetRate(p_username, p_text);
            synth.RandomVoice();

            // Speak a string.
            synth._synth.Speak(p_text);

            synth.enqueue();
        }

        private static Queue<Sync> _readyStateSyncQueue;
        private static void Queue(Sync p_sync, bool p_addOrRemove)
        {
            if (p_addOrRemove)
            {
                lock (_resetEvent)
                {
                    _readyStateSyncQueue.Enqueue(p_sync);
                }
                _resetEvent.Set();
            }
        }

    }

    delegate void SyncOp(Sync p_sync, bool p_bool);

    class Sync
    {
        SyncOp _op;
        public Sync(SyncOp p_op)
        {
            _synth = new SpeechSynthesizer();
            _synth.SetOutputToDefaultAudioDevice();
            _voices = _synth.GetInstalledVoices();

            _op = p_op;
            _op(this, true);
            //var path = Directory.GetCurrentDirectory() + "\\..\\..\\TheOnlySykanLexicon.pls";
            //_synth.AddLexicon(new Uri(path), "application/pls+xml");
        }

        public void enqueue()
        {
            _op(this, true);
        }

        public SpeechSynthesizer _synth;
        ReadOnlyCollection<InstalledVoice> _voices;
        int index = 0;
        static int maxLengthSoFar = int.MinValue;
        static int minLengthSoFar = int.MaxValue;

        public void RandomVoice()
        {
            index++;
            if (index >= _voices.Count)
                index = 0;

            _synth.SelectVoice(_voices[index].VoiceInfo.Name);
        }

        public void SetRate(String p_username, String p_message)
        {
            //int d = (( * 2) - 1) * 5;
            float n1 = (p_username.Length / maxLengthSoFar);
            float n2 = n1 * 2;
            float n3 = n2 - 1;
            float n4 = n3 * 5;

            int d = (int)n4;

            if (p_message.Length > 50)
            {
                d = d + (Math.Abs(d) / 2) + 2;

                if (p_message.Length > 100)
                {
                    d = d + (Math.Abs(d) / 2) + 2;

                    if (p_message.Length > 200)
                    {
                        d = d + (Math.Abs(d) / 2) + 2;

                        if (p_message.Length > 300)
                        {
                            d = d + (Math.Abs(d) / 2) + 2;
                        }
                    }
                }
            }

            d = Math.Min(d, 10);
            d = Math.Max(-10, d);
            _synth.Rate = d;
        }

    }
}
