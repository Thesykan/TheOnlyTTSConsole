using System;
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

        private static int thread = 2;
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

                if (speakUserName)
                {
                    synth.Synth.Rate = 2;
                    synth.Synth.Speak(username);
                }

                synth.SetRate(username, text);
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

        public static void SpeakText(IRCMessage pMessageInfo)
        {
            lock (queue)
            {
                queue.Enqueue(pMessageInfo);
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

    }

    internal delegate void SyncOp(Sync pSync, bool pBool);

    internal class Sync
    {
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
            var settings = UserManager.GetUserSettings();
            if (!Directory.Exists("Lexicons"))
            {
                Directory.CreateDirectory("Lexicons");
            }
            foreach (var userSetting in settings)
            {
                if (userSetting.Lexicon != null)
                {
                    var path = $"Lexicons\\{userSetting.UserName}.pls";
                    if (!File.Exists(path))
                    {
                        String str = System.Text.UTF8Encoding.Default.GetString(Convert.FromBase64String(_lexiconXML));
                        str = str.Replace("#0#", userSetting.UserName);
                        str = str.Replace("#1#", userSetting.Lexicon);
                        File.WriteAllText(path, str);
                    }
                    else
                    {
                        var lexicon = File.ReadAllText(path);
                        if(!lexicon.Contains(userSetting.Lexicon))
                        {
                            File.Delete(path);

                            String str = System.Text.UTF8Encoding.Default.GetString(Convert.FromBase64String(_lexiconXML));
                            str = str.Replace("#0#", userSetting.UserName);
                            str = str.Replace("#1#", userSetting.Lexicon);
                            File.WriteAllText(path, str);
                        }
                    }
                    var uri = new Uri(Path.GetFullPath(path));
                    try
                    {
                        Synth.AddLexicon(uri, "application/pls+xml");
                    }
                    catch
                    {
                        try
                        {
                            Synth.RemoveLexicon(uri);
                            Synth.AddLexicon(uri, "application/pls+xml");
                        }
                        catch { }
                    }
                    LoadedLexicons.Add(uri);
                }
            }
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
            if (settings != null && _voices.Where(w=>w.VoiceInfo.Name.Contains(settings.Voice)).Any())
            {
                var voice = _voices.FirstOrDefault(w => w.VoiceInfo.Name.Contains(settings.Voice));
                Synth.SelectVoice(voice.VoiceInfo.Name);
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

            return v;
        }

        public void Speak(string pText)
        {
            Synth.Speak(pText);

            PlayAudio();

            if (AudioStream != null)
            {
                try { AudioStream.Dispose(); } catch { }
            }

            AudioStream = new MemoryStream();
            Synth.SetOutputToAudioStream(AudioStream, new SpeechAudioFormatInfo(44100, AudioBitsPerSample.Sixteen, AudioChannel.Stereo));
        }

        private DirectSoundOut audioOutput = new DirectSoundOut();
        public void PlayAudio()
        {
            AudioStream.Position = 0;
            using (WaveStream stream = new RawSourceWaveStream(AudioStream, new WaveFormat(44100, 16, 2)))
            using (WaveChannel32 wc = new WaveChannel32(stream, 1, pan) { PadWithZeroes = false })
            {
                audioOutput.Init(wc);

                audioOutput.Play();

                while (audioOutput.PlaybackState != PlaybackState.Stopped)
                {
                    Thread.Sleep(20);
                }

                audioOutput.Stop();
            }
        }

        public static void CopyStream(Stream input, Stream output, int bytes)
        {
            byte[] buffer = new byte[32768];
            int read;
            while (bytes > 0 &&
                   (read = input.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                output.Write(buffer, 0, read);
                bytes -= read;
            }
        }
    }
}