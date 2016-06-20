using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using TTSConsoleLib.Utils;
using System.Threading;
using System.Net.Cache;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace TTSConsoleLib.Twitch
{
    public class TwitchAPI
    {
        public static String _channel = String.Empty;

        private static Timer _updateRealTimeTimer;

        private static TW_EmotesInfo _emotes;

        public static void Init()
        {
            _updateRealTimeTimer = new Timer(x => UpdateRealtimeTwitchVariables(), null, 0, 60000); // 1 min

            var emotesFileName = "TwitchEmotes.EMOTES";
            if (File.Exists(emotesFileName))
            {
                _emotes = JsonConvert.DeserializeObject<TW_EmotesInfo>(File.ReadAllText(emotesFileName));
            }
            else
            {
                var request = WebRequest.Create("https://api.twitch.tv/kraken/chat/emoticons");
                request.GetResponseAsync().ContinueWith(LoadEmotes);
            }
        }

        private static void LoadEmotes(Task<WebResponse> obj)
        {
            var response = obj.Result;
            var responseStream = response.GetResponseStream();
            var streamReader = new StreamReader(responseStream);
            var text = streamReader.ReadToEnd();

            File.WriteAllText("TwitchEmotes.EMOTES", text);
            response.Close();
            _emotes = JsonConvert.DeserializeObject<TW_EmotesInfo>(text);
        }


        private static TW_StreamInfo StreamInfo;
        public static int GetNumberOfViewers()
        {
            return StreamInfo?.stream?.viewers ?? 0;
        }
        public static int GetNumberOfFollowers()
        {
            return StreamInfo?.stream?.channel.followers ?? 0;
        }
        public static String GetUpdateTime()
        {
            try
            {
                return (DateTime.UtcNow - (StreamInfo?.stream?.created_at ?? DateTime.MinValue)).ToString(@"hh\:mm");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                return "hh:mm";
            }
        }

        private static TW_ChatInfo ChatInfo;
        private static String[] EmptyStringArray = new string[] { };
        public static String[] GetAllChatters()
        {
            return ChatInfo?.chatters?.GetAllChatters() ?? EmptyStringArray;
        }

        public static bool IsFollower(String pUserName)
        {
            return followers?.Any(x => x.user.name == pUserName) ?? false;
        }

        /// <summary>
        /// Update Twitch Variables
        /// </summary>
        /// 
        private static int FollowOffset = 0;
        public static void UpdateRealtimeTwitchVariables()
        {
            try
            {
                // Stripping # from channel name for API calls
                string channel = _channel.Replace("#", "");
                GetData("https://api.twitch.tv/kraken/streams/" + channel, StreamRequestComplete);
                GetData("http://tmi.twitch.tv/group/user/" + channel + "/chatters", ChatterRequestComplete);
                GetData("https://api.twitch.tv/kraken/channels/" + channel + "/follows?direction=ASC&limit=1000&offset=" + FollowOffset.ToString(), FollowersRequestComplete);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }


        public static bool TextHasEmote(String pText)
        {
            return _emotes?.HasEmoteInText(pText) ?? false;
        }

        public static List<TextOrImage> ConvertText(String pText)
        {
            return _emotes?.ConvertText(pText) ?? new List<TextOrImage>();
        }


        private static void StreamRequestComplete(Task<WebResponse> obj)
        {
            try
            {
                var response = obj.Result;
                var responseStream = response.GetResponseStream();
                var streamReader = new StreamReader(responseStream);
                var text = streamReader.ReadToEnd();
                var twitchObj = JsonConvert.DeserializeObject<TW_StreamInfo>(text);
                response.Close();

                var oldInfo = StreamInfo;
                StreamInfo = twitchObj;
                if ((!StreamInfo?.Equals(oldInfo)) ?? false)
                {
                    IRC.IRCClient.PrintConsoleMessage("Update Detected");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        private static void ChatterRequestComplete(Task<WebResponse> obj)
        {
            try
            {
                var response = obj.Result;
                var responseStream = response.GetResponseStream();
                var streamReader = new StreamReader(responseStream);
                var text = streamReader.ReadToEnd();
                var twitchObj = JsonConvert.DeserializeObject<TW_ChatInfo>(text);
                response.Close();

                var oldInfo = ChatInfo;
                ChatInfo = twitchObj;
                if ((!StreamInfo?.Equals(oldInfo)) ?? false)
                {
                    var newUsers = ChatInfo.GetNewUsers(oldInfo);
                    if (newUsers != String.Empty)
                    {
                        IRC.IRCClient.PrintConsoleMessage("New Chatter(s) Detected: " + newUsers);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        static List<TW_Follower> followers = new List<TW_Follower>();
        static bool NewFollowers = false;
        static int LastTotal = 0;
        private static void FollowersRequestComplete(Task<WebResponse> obj)
        {
            try
            {
                var response = obj.Result;
                var responseStream = response.GetResponseStream();
                var streamReader = new StreamReader(responseStream);
                var text = streamReader.ReadToEnd();
                var twitchObj = JsonConvert.DeserializeObject<TW_FollowerInfo>(text);
                response.Close();

                if (twitchObj != null && twitchObj.follows != null)
                {
                    FollowOffset += twitchObj.follows.Count;

                    if (twitchObj.follows.Count == 0)
                        NewFollowers = true;

                    followers.AddRange(twitchObj.follows);

                    if (NewFollowers)
                    {
                        foreach (var follow in twitchObj.follows)
                        {
                            IRC.IRCClient.PrintConsoleMessage("!New Follower! " + follow.user.name + " @ " + follow.created_at.ToLocalTime().ToString());
                        }
                    }

                    if(LastTotal != twitchObj._total)
                    {
                        LastTotal = twitchObj._total;
                        if (LastTotal > FollowOffset)
                            FollowOffset = LastTotal;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }


        private static void GetData(String pUrl, Action<Task<WebResponse>> pOnReponse)
        {
            if (pUrl.Contains("?"))
            {
                pUrl += "&utcnow=" + DateTime.UtcNow.Ticks;
            }
            else
            {
                pUrl += "?utcnow=" + DateTime.UtcNow.Ticks;
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(pUrl);

            String oauth = String.Empty;
            String ClientId = @"ci2tc032wxaprwrcpz7qaqi8u70n7t8";
            //request.Method = "application/vnd.twitchtv.v3+json";
            request.ContentType = "application/json";
            request.Headers.Add("Client-ID", ClientId);
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.52 Safari/537.36 TheOnlyTTSConsole/2016";
            //request.Headers.Add("Authorization", "OAuth " + oauth);
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            request.Timeout = 2 * 1000;

            var response = request.GetResponseAsync();
            response.ContinueWith(pOnReponse);

        }
    }

    public class TextOrImage
    {
        public bool isText = true;
        public Image Image = null;
        public String Text = null;
    }

    public class TW_StreamInfo : IEquatable<TW_StreamInfo>
    {
        public TW_Stream stream;

        public bool Equals(TW_StreamInfo other)
        {
            if ((stream?.viewers ?? 0) != (other?.stream?.viewers ?? 0))
            {
                return false;
            }
            if ((stream?.channel?.followers ?? 0) != (other?.stream?.channel?.followers ?? 0))
            {
                return false;
            }
            return true;
        }
    }
    public class TW_Stream
    {
        public int viewers;
        public DateTime created_at;
        public TW_Channel channel;
    }
    public class TW_Channel
    {
        public int followers;
    }


    public class TW_ChatInfo : IEquatable<TW_ChatInfo>
    {
        public TW_Chatter chatters;
        public int chatter_count;

        public bool Equals(TW_ChatInfo other)
        {
            var otherlist = other.chatters.GetAllChatters();
            var currentlist = chatters.GetAllChatters();
            if (otherlist.Intersect(currentlist).Count() == currentlist.Length)
            {
                return true;
            }
            return false;
        }

        public string GetNewUsers(TW_ChatInfo other)
        {
            var currentlist = chatters.GetAllChatters();

            if (other == null)
                return String.Join(",", currentlist);

            var otherlist = other.chatters.GetAllChatters();
            return String.Join(",", currentlist.Except(otherlist).ToArray());
        }
    }

    public class TW_Chatter
    {
        private static List<String> empty = new List<string>();

        public List<String> moderators;
        public List<String> viewers;
        public List<String> staff;
        public List<String> admins;
        public List<String> global_mods;

        private String[] builtList = null;
        public String[] GetAllChatters()
        {
            if (builtList != null)
                return builtList;

            List<String> fulllist = new List<string>();
            fulllist.AddRange(moderators ?? empty);
            fulllist.AddRange(viewers ?? empty);
            fulllist.AddRange(staff ?? empty);
            fulllist.AddRange(admins ?? empty);
            fulllist.AddRange(global_mods ?? empty);
            builtList = fulllist.ToArray();
            return builtList;
        }
    }


    public class TW_FollowerInfo
    {
        public int _total;
        public List<TW_Follower> follows;
    }

    public class TW_Follower
    {
        /// <summary>
        /// Started Following
        /// </summary>
        public DateTime created_at;
        public TW_FollowerUser user;
    }
    public class TW_FollowerUser
    {
        public String name;
    }



    public class TW_EmotesInfo
    {
        public List<TW_Emotes> emoticons;

        Regex reg = null;
        public bool HasEmoteInText(String pText)
        {
            if (emoticons == null)
                return false;

            pText = " " + pText + " ";

            if (reg == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("(");

                for (int i = 0; i < emoticons.Count; i++)
                {
                    sb.Append("[^a-zA-Z]" + Regex.Escape(emoticons[i].regex) + "[^a-zA-Z]");
                    if (i != emoticons.Count - 1)
                        sb.Append("|");
                }

                sb.Append(")");
                reg = new Regex(sb.ToString(), RegexOptions.IgnoreCase);
            }
            return reg.IsMatch(pText);
        }

        public List<TextOrImage> ConvertText(String pText)
        {
            pText = " " + pText + " ";

            List<TextOrImage> list = new List<TextOrImage>();
            //var matches = reg.Match(pTest);
            var split = reg.Split(pText);

            for (int i = 0; i < split.Length; i++)
            {
                if (_hashSetImages.ContainsKey(split[i].ToLower().Trim()))
                {
                    Image image = new Image();
                    image.Source = _hashSetImages[split[i].ToLower().Trim()];
                    image.Height = 14;
                    //image.Width = 20;

                    list.Add(new TextOrImage() { Image = image, isText = false });
                }
                else if (hashSet.ContainsKey(split[i].ToLower().Trim()))
                {
                    string url = hashSet[split[i].ToLower().Trim()].url;
                    BitmapImage bitmap = new BitmapImage(new Uri(url));
                    Image image = new Image();
                    image.Source = bitmap;
                    image.Height = 14;
                    //image.Width = 20;

                    _hashSetImages.Add(split[i].ToLower().Trim(), bitmap);

                    list.Add(new TextOrImage() { Image = image, isText = false });
                }
                else
                {
                    list.Add(new TextOrImage() { Text = split[i] });
                }
            }


            //Match item = matches;
            //for (int i = 0; i < split.Length; i++)
            //{
            //    list.Add(new TextOrImage() { Text = split[i] });

            //    if (item.Success)
            //    {
            //        //This is an emote...
            //        if (_hashSetImages.ContainsKey(item.Value.ToLower()))
            //        {
            //            Image image = new Image();
            //            image.Source = _hashSetImages[item.Value.ToLower()];
            //            image.Width = 20;

            //            list.Add(new TextOrImage() { Image = image, isText = false });
            //        }
            //        else if (hashSet.ContainsKey(item.Value.ToLower()))
            //        {
            //            string url = hashSet[item.Value.ToLower()].url;
            //            BitmapImage bitmap = new BitmapImage(new Uri(url));
            //            Image image = new Image();
            //            image.Source = bitmap;
            //            image.Width = 20;

            //            _hashSetImages.Add(item.Value.ToLower(), bitmap);

            //            list.Add(new TextOrImage() { Image = image, isText = false });
            //        }
            //        else
            //        {
            //            list.Add(new TextOrImage() { Text = item.Value });
            //        }
            //    }
            //    else
            //    {
            //        list.Add(new TextOrImage() { Text = item.Value });
            //    }
            //item = item.NextMatch();
            //}

            //Match item = matches;
            //while(item != null && item.Length > 0)
            //{
            //    if (item.Success)
            //    {
            //        //This is an emote...
            //        if (_hashSetImages.ContainsKey(item.Value.ToLower()))
            //        {
            //            Image image = new Image();
            //            image.Source = _hashSetImages[item.Value.ToLower()];
            //            image.Width = 20;

            //            list.Add(new TextOrImage() { Image = image, isText = false });
            //        }
            //        else if (hashSet.ContainsKey(item.Value.ToLower()))
            //        {
            //            string url = hashSet[item.Value.ToLower()].url;
            //            BitmapImage bitmap = new BitmapImage(new Uri(url));
            //            Image image = new Image();
            //            image.Source = bitmap;
            //            image.Width = 20;

            //            _hashSetImages.Add(item.Value.ToLower(), bitmap);

            //            list.Add(new TextOrImage() { Image = image, isText = false });
            //        }
            //        else
            //        {
            //            list.Add(new TextOrImage() { Text = item.Value });
            //        }
            //        //list.Add(new TextOrImage() { Text = item.Value });
            //    }
            //    else
            //    {
            //        list.Add(new TextOrImage() { Text = item.Value });
            //    }

            //    item = item.NextMatch();
            //}

            return list;
        }

        private Dictionary<String, BitmapImage> _hashSetImages = new Dictionary<string, BitmapImage>();
        private Dictionary<String, TW_Emote> _hashSet;
        public Dictionary<String, TW_Emote> hashSet
        {
            get
            {
                if(_hashSet == null)
                {
                    _hashSet = new Dictionary<string, TW_Emote>();
                    foreach (var item in emoticons)
                    {
                        if (!_hashSet.ContainsKey(item.regex.ToLower()))
                            _hashSet.Add(item.regex.ToLower(), item.images.FirstOrDefault());
                    }
                    //_hashSet = emoticons.ToDictionary(k => k.regex.ToLower(), v => v.images.FirstOrDefault());
                }
                return _hashSet;
            }
        }


    }
    public class TW_Emotes
    {
        public String regex;
        public List<TW_Emote> images;
    }
    public class TW_Emote
    {
        public int? width;
        public int? height;
        public String url;
        public int? emoticon_set;
    }



}
