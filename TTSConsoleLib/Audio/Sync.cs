﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Speech.Synthesis;
using System.Threading;
using System.Speech.AudioFormat;
using System.IO;
using NAudio.Wave;
using System.Linq;
using TTSConsoleLib.Utils;
using TTSConsoleLib.Modules;
using TTSConsoleLib.IRC;

namespace TTSConsoleLib.Audio
{
    internal class SyncPool
    {
        private static List<Sync> _syncList;

        public static void Init()
        {
            if (_syncList != null) return;

            _readyStateSyncQueue = new Queue<Sync>();
            _syncList = new List<Sync> { new Sync(Queue) { pan = -1 }, new Sync(Queue) { pan = 1 } };
            _readyThread = new Thread(SpeechThread);
            _readyThread.Start();
        }

        private static int thread = 1;
        private static void SpeechThread()
        {
            while (true)
            {
                if (queue.Count > 0)
                {
                    IRCMessage tup = null;
                    lock (queue)
                    {
                        if (queue.Count > 0)
                        {
                            if (thread > 0)
                            {
                                thread--;

                                tup = queue.Dequeue();
                                ThreadPool.QueueUserWorkItem((x) => { CreateThread(tup); });
                            }
                        }
                    }
                }
            }
        }

        private static DateTime _lastUserNameDateTime = DateTime.Now;
        private static String _lastUserName = String.Empty;
        private static void CreateThread(IRCMessage pMessageInfo)
        {
            Sync synth = null;
            try
            {
                var text = pMessageInfo.message;
                var username = pMessageInfo.userName;

                if (text.Length > 230)
                {
                    var split = text.Split(' ').ToList().Distinct().ToArray();
                    text = String.Join(" ", split);
                }

                bool speakUserName = false;
                while (true)
                {
                    if (_readyStateSyncQueue.Count > 0)
                    {
                        lock (_readyStateSyncQueue)
                        {
                            if (_readyStateSyncQueue.Count > 0)
                            {
                                if(_lastUserName != username)
                                {
                                    _lastUserName = username;
                                    speakUserName = true;
                                }

                                if((DateTime.Now - _lastUserNameDateTime).TotalMinutes > 2)
                                {
                                    speakUserName = true;
                                    _lastUserNameDateTime = DateTime.Now;
                                }

                                synth = _readyStateSyncQueue.Dequeue();
                                break;
                            }
                        }
                    }

                    ResetEvent.WaitOne(1000);
                    ResetEvent.Reset();
                }


                //Clean out addresses.
                var words = text.Split(' ').ToArray();
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].ToLower().Contains("http"))
                    {
                        words[i] = string.Empty;
                    }
                }
                text = string.Join(" ", words);
                //CLEAN TEXT 

                synth.SetRate(username, text);
                if (speakUserName)
                    synth.Synth.Speak(username);
                synth.RandomVoice(username);

                // Speak a string.
                synth.Speak(text);

            }
            catch(Exception ex)
            {
                Logger.Log(ex.ToString());
            }
            finally
            {
                if (synth != null)
                    synth.Enqueue();

                thread++;
            }
        }


        private static int _syncIndex;
        private static readonly ManualResetEvent ResetEvent = new ManualResetEvent(false);
        private static Queue<Sync> _readyStateSyncQueue;
        private static Thread _readyThread;

        private static Queue<IRCMessage> queue = new Queue<IRCMessage>();

        static Dictionary<String, LinkedList<IRCMessage>> userMessageHistory = new Dictionary<string, LinkedList<IRCMessage>>();
        public static void SpeakText(IRCMessage pMessageInfo)
        {
            lock (queue)
            {
                queue.Enqueue(pMessageInfo);

                var user = pMessageInfo?.userName ?? String.Empty;
                if (userMessageHistory.ContainsKey(user))
                {
                    //Keep Last Ten Messages 
                    if(userMessageHistory[user].Count > 3)
                        userMessageHistory[user].RemoveFirst();

                    userMessageHistory[user].AddLast(pMessageInfo);
                }
                else
                {
                    userMessageHistory.Add(user, new LinkedList<IRCMessage>());
                    userMessageHistory[user].AddLast(pMessageInfo);
                }
            }
        }


        private static void Queue(Sync pSync, bool pAddOrRemove)
        {
            lock (_readyStateSyncQueue)
            {
                _readyStateSyncQueue.Enqueue(pSync);
            }
            ResetEvent.Set();
        }

        public static void ReloadLexicons()
        {
            foreach(var sy in _syncList)
            {
                sy.ReloadLexicons();
            }
        }

        public static void Pause()
        {
            _syncList?.ForEach(x => x.pause = true);
        }
        public static void Resume()
        {
            _syncList?.ForEach(x => x.pause = false);
        }

        public static void RepeatFuzzyUser(String pUserName)
        {
            String lowestUserNameFound = String.Empty;
            int val = int.MaxValue;

            var realusernames = userMessageHistory.Keys.ToArray();
            for (int i = 0; i < realusernames.Length; i++)
            {

                var cval = LevenshteinDistance.Compute(pUserName, realusernames[i]);
                if(cval < val)
                {
                    lowestUserNameFound = realusernames[i];
                    val = cval;
                }
            }

            if(lowestUserNameFound != String.Empty)
            {
                var messages = userMessageHistory[lowestUserNameFound].ToArray();
                for (int i = 0; i < messages.Length; i++)
                {
                    SpeakText(messages[i]);
                }
            }

        }

        public static void SkipALLMessages()
        {
            lock (_readyStateSyncQueue)
            {
                queue.Clear();
            }
            _syncList?.ForEach(x => x.skip = true);
        }

        public static void SpeedUp()
        {
            _syncList?.ForEach(x => {
                if (x.speed == SyncSpeed.SortaFast)
                    x.speed = SyncSpeed.VeryFast;
                else
                    x.speed = SyncSpeed.SortaFast;
           });
        }
        public static void SlowDown()
        {
            _syncList?.ForEach(x => {
                if (x.speed == SyncSpeed.SortaSlow)
                    x.speed = SyncSpeed.VerySlow;
                else
                    x.speed = SyncSpeed.SortaSlow;
                });
        }
        public static void NormalSpeed()
        {
            _syncList?.ForEach(x => x.speed = SyncSpeed.Normal);
        }

    }

    /// <summary>
    /// Contains approximate string matching
    /// </summary>
    static class LevenshteinDistance
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }

    internal delegate void SyncOp(Sync pSync, bool pBool);


    enum SyncSpeed
    {
        Normal,
        SortaSlow = -5,
        VerySlow = -10,
        SortaFast = 5,
        VeryFast = 10
    }

    internal class Sync
    {
        public bool pause = false;
        public bool skip = false;
        public SyncSpeed speed = SyncSpeed.Normal;

        public int pan = 0;
        static Sync()
        {

        }
        public static String[] voiceArray = new String[0];

        private readonly SyncOp _op;

        private readonly String _lexiconXML = "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiID8+DQo8bGV4aWNvbiB2ZXJzaW9uPSIxLjAiDQogICAgICB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwNS8wMS9wcm9udW5jaWF0aW9uLWxleGljb24iDQogICAgICBhbHBoYWJldD0ieC1taWNyb3NvZnQtdXBzIiB4bWw6bGFuZz0iZW4tVVMiPg0KICA8bGV4ZW1lPg0KICAgIDxncmFwaGVtZT4jMCM8L2dyYXBoZW1lPg0KICAgIDxwaG9uZW1lPiMxIzwvcGhvbmVtZT4NCiAgPC9sZXhlbWU+DQo8L2xleGljb24+";

        private System.IO.Stream AudioStream;

        List<Uri> LoadedLexicons = new List<Uri>();
        public Sync(SyncOp pOp)
        {
            AudioStream = new MemoryStream();
            Synth = new SpeechSynthesizer();

            ReloadLexicons();

            Synth.SetOutputToAudioStream(AudioStream, new SpeechAudioFormatInfo(44100, AudioBitsPerSample.Sixteen, AudioChannel.Stereo) { });
            _voices = Synth.GetInstalledVoices();
            voiceArray = _voices?.Select(s => s.VoiceInfo.Name).ToArray();

            _op = pOp;
            _op(this, true);
        }

        public void ReloadLexicons()
        {
            //var settings = UserManager.GetUserSettings();
            //if (!Directory.Exists("Lexicons"))
            //{
            //    Directory.CreateDirectory("Lexicons");
            //}
            //foreach (var userSetting in settings)
            //{
            //    if (userSetting.Lexicon != null)
            //    {
            //        var path = $"Lexicons\\{userSetting.UserName}.pls";
            //        if (!File.Exists(path))
            //        {
            //            String str = System.Text.UTF8Encoding.Default.GetString(Convert.FromBase64String(_lexiconXML));
            //            str = str.Replace("#0#", userSetting.UserName);
            //            str = str.Replace("#1#", userSetting.Lexicon);
            //            File.WriteAllText(path, str);
            //        }
            //        else
            //        {
            //            var lexicon = File.ReadAllText(path);
            //            if(!lexicon.Contains(userSetting.Lexicon))
            //            {
            //                File.Delete(path);

            //                String str = System.Text.UTF8Encoding.Default.GetString(Convert.FromBase64String(_lexiconXML));
            //                str = str.Replace("#0#", userSetting.UserName);
            //                str = str.Replace("#1#", userSetting.Lexicon);
            //                File.WriteAllText(path, str);
            //            }
            //        }
            //        var uri = new Uri(Path.GetFullPath(path));
            //        try
            //        {
            //            Synth.AddLexicon(uri, "application/pls+xml");
            //        }
            //        catch
            //        {
            //            try
            //            {
            //                Synth.RemoveLexicon(uri);
            //                Synth.AddLexicon(uri, "application/pls+xml");
            //            }
            //            catch { }
            //        }
            //        LoadedLexicons.Add(uri);
            //    }
            //}
        }

        public void Enqueue()
        {
            _op(this, true);
        }

        public SpeechSynthesizer Synth;
        private readonly ReadOnlyCollection<InstalledVoice> _voices;
        private int _index;

        public void RandomVoice(String pUsername)
        {
            var settings = UserManager.GetUserSettings(pUsername);

            if (settings != null && _voices.Any(w=>w.VoiceInfo.Name.ToLower().Contains(settings.Voice.Trim().ToLower())))
            {
                try
                {
                    var voice = _voices.FirstOrDefault(w => w.VoiceInfo.Name.ToLower().Contains(settings.Voice.Trim().ToLower()));
                    Synth.SelectVoice(voice.VoiceInfo.Name);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
            }
            else
            {
                _index++;
                if (_index >= _voices.Count)
                    _index = 0;

                try
                {
                    var voice = _voices[_index];
                    Synth.SelectVoice(voice.VoiceInfo.Name);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
            }
        }

        private static int MaxLengthSoFar = 1;
        public void SetRate(string pUsername, string pMessage)
        {
            if (pUsername.Length > MaxLengthSoFar)
                MaxLengthSoFar = pUsername.Length;

            //Disable UserName Rate Change... For Now.
            //var n1 = (float)(pUsername.Length / MaxLengthSoFar);
            //var n2 = n1 * 2;
            //var n3 = n2 - 1;
            //var n4 = n3 * 5;

            var d = 0;// (int)n4;

            d = SpeedUp(d, pMessage);

            d = Math.Min(d, 10);
            d = Math.Max(-10, d);
            Synth.Rate = d;
        }

        public int SpeedUp(int v, string m)
        {
            switch (speed) {
                case SyncSpeed.Normal:
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
                        v = v + (Math.Abs(v) / 2) + 2;
                    }

                    break;
                default:
                    return (int)(speed);
            }
            return v;
        }

        public void Speak(string pText)
        {
            Synth.Speak(pText);

            PlayAudio();

            AudioStream.Position = 0;
            //if (AudioStream != null)
            //{
            //    try { AudioStream.Dispose(); } catch { }
            //}
            //AudioStream = new MemoryStream();
            //Synth.SetOutputToAudioStream(AudioStream, new SpeechAudioFormatInfo(44100, AudioBitsPerSample.Sixteen, AudioChannel.Stereo));
        }

        private DirectSoundOut audioOutput = new DirectSoundOut();
        public void PlayAudio()
        {
            skip = false;

            long textPosition = AudioStream.Position;
            AudioStream.Position = 0;
            using (WaveStream stream = new RawSourceWaveStream(AudioStream, new WaveFormat(44100, 16, 2)))
            using (WaveChannel32 wc = new WaveChannel32(stream, 1, pan) { PadWithZeroes = false })
            {
                audioOutput.Init(wc);

                audioOutput.Play();

                while (audioOutput.PlaybackState != PlaybackState.Stopped)
                {
                    if (skip)
                        audioOutput.Stop();

                    if((AudioStream.Position >= textPosition))
                    {
                        audioOutput.Stop();
                    }

                    Thread.Sleep(20);
                    if (pause)
                    {
                        audioOutput.Pause();
                        while (pause)
                        {
                            if (skip)
                                audioOutput.Stop();

                            Thread.Sleep(20);
                        }
                        audioOutput.Play();
                    }
                }

                audioOutput.Stop();
            }

        }

        //public static void CopyStream(Stream input, Stream output, int bytes)
        //{
        //    byte[] buffer = new byte[32768];
        //    int read;
        //    while (bytes > 0 &&
        //           (read = input.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
        //    {
        //        output.Write(buffer, 0, read);
        //        bytes -= read;
        //    }
        //}
    }
}