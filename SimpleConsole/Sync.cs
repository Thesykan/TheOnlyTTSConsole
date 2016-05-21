using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading;

namespace SimpleConsole
{
    internal class SyncPool
    {
        private static List<Sync> _syncList;

        public static void Init()
        {
            _readyStateSyncQueue = new Queue<Sync>();
            if (_syncList != null) return;
            _syncList = new List<Sync> {new Sync(Queue), new Sync(Queue), new Sync(Queue)};
        }

        private static int _syncIndex;
        private static readonly ManualResetEvent ResetEvent = new ManualResetEvent(false);

        public static void SpeakText(string pUsername, string pText)
        {
            _syncIndex++;
            if (_syncIndex >= _syncList.Count)
                _syncIndex = 0;

            var synth = _syncList[_syncIndex];

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

            synth.Synth.Rate = 2;
            synth.Synth.Speak(pUsername);

            synth.SetRate(pUsername, pText);
            synth.RandomVoice();

            // Speak a string.
            synth.Synth.Speak(pText);

            synth.Enqueue();
        }

        private static Queue<Sync> _readyStateSyncQueue;

        private static void Queue(Sync pSync, bool pAddOrRemove)
        {
            if (!pAddOrRemove) return;
            lock (ResetEvent)
            {
                _readyStateSyncQueue.Enqueue(pSync);
            }
            ResetEvent.Set();
        }
    }

    internal delegate void SyncOp(Sync pSync, bool pBool);

    internal class Sync
    {
        private readonly SyncOp _op;

        public Sync(SyncOp pOp)
        {
            Synth = new SpeechSynthesizer();
            Synth.SetOutputToDefaultAudioDevice();
            _voices = Synth.GetInstalledVoices();

            _op = pOp;
            _op(this, true);
            //var path = Directory.GetCurrentDirectory() + "\\..\\..\\TheOnlySykanLexicon.pls";
            //_synth.AddLexicon(new Uri(path), "application/pls+xml");
        }

        public void Enqueue()
        {
            _op(this, true);
        }

        public SpeechSynthesizer Synth;
        private readonly ReadOnlyCollection<InstalledVoice> _voices;
        private int _index;
        private static int _maxLengthSoFar = -256;
//        private static int _minLengthSoFar = 256;

        public void RandomVoice()
        {
            _index++;
            if (_index >= _voices.Count)
                _index = 0;

            Synth.SelectVoice(_voices[_index].VoiceInfo.Name);
        }

        public void SetRate(string pUsername, string pMessage)
        {
//            int d = (( * 2) - 1) * 5;
            // ReSharper disable once PossibleLossOfFraction
            var n1 = (float) (pUsername.Length/_maxLengthSoFar);
            var n2 = n1*2;
            var n3 = n2 - 1;
            var n4 = n3*5;

            var d = (int) n4;

            d = SpeedUp(d, pMessage);

            d = Math.Min(d, 10);
            d = Math.Max(-10, d);
            Synth.Rate = d;
        }

        public int SpeedUp(int v, string m)
        {
            int j = 0;
            if (m.Length > 50)
                j++;
            if (m.Length > 100)
                j++;
            if (m.Length > 200)
                j++;
            if (m.Length > 300)
                j++;

            for (int i = 0; i < j; i++)
            {
                v = v + (Math.Abs(v)/2) + 2;
            }

            return v;
        }
    }
}