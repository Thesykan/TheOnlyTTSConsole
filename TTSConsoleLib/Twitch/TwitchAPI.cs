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

namespace TTSConsoleLib.Twitch
{
    public class TwitchAPI
    {
        public static String _channel = String.Empty;

        private static Timer _updateRealTimeTimer;
        private static Timer _updateNonRealTimeTimer;
        public static void Init()
        {
            _updateRealTimeTimer = new Timer(x => UpdateRealtimeTwitchVariables(), null, 0, 60000); // 1 min
            _updateNonRealTimeTimer = new Timer(x => UpdateNonRealtimeTwitchVariables(), null, 0, 600000); //10 mins
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

        private static TW_FollowerInfo FollowInfo;
        public static bool IsFollower(String pUserName)
        {
            return FollowInfo?.isFollower(pUserName) ?? false;
        }

        /// <summary>
        /// Update Twitch Variables
        /// </summary>
        public static void UpdateRealtimeTwitchVariables()
        {
            try
            {
                // Stripping # from channel name for API calls
                string channel = _channel.Replace("#", "");

                WebRequest request = WebRequest.Create("https://api.twitch.tv/kraken/streams/" + channel);
                var response = request.GetResponseAsync();
                response.ContinueWith(StreamRequestComplete);

                request = WebRequest.Create("http://tmi.twitch.tv/group/user/" + channel + "/chatters");
                response = request.GetResponseAsync();
                response.ContinueWith(ChatterRequestComplete);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        public static void UpdateNonRealtimeTwitchVariables()
        {
            try
            {
                // Stripping # from channel name for API calls
                string channel = _channel.Replace("#", "");

                var request = WebRequest.Create("https://api.twitch.tv/kraken/channels/" + channel + "/follows");
                var response = request.GetResponseAsync();
                response.ContinueWith(FollowersRequestComplete);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
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

                var oldInfo = ChatInfo;
                FollowInfo = twitchObj;

            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }
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
            return String.Join(",", otherlist.Except(currentlist).ToArray());
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
        public List<TW_Follower> follows;

        HashSet<String> userHashSet = null;
        public bool isFollower(String pUserName)
        {
            if (userHashSet != null)
                return userHashSet.Contains(pUserName);

            userHashSet = new HashSet<string>(follows.Select(s => s.user.name));
            return userHashSet.Contains(pUserName);
        }
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



}
