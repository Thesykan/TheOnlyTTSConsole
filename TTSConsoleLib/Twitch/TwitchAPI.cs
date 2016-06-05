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
        private static Timer _updateTimer;
        public static void Init()
        {
            _updateTimer = new Timer(x=> UpdateTwitchVariables(), null, 0, 60000);
        }

        public static String _channel = String.Empty;
        private static TW_StreamInfo info;
        public static int GetNumberOfViewers()
        {
            return info?.stream?.viewers ?? 0;
        }

        public static int GetNumberOfFollowers()
        {
            return info?.stream?.channel.followers ?? 0;
        }

        public static String GetUpdateTime()
        {
            try
            {
                return (DateTime.UtcNow - (info?.stream?.created_at??DateTime.MinValue)).ToString(@"hh\:mm");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                return "hh:mm";
            }
        }


        /// <summary>
        /// Update Twitch Variables
        /// </summary>
        public static void UpdateTwitchVariables()
        {
            try
            {
                // Stripping # from channel name for API calls
                string channel = _channel.Replace("#", "");

                WebRequest request = WebRequest.Create("https://api.twitch.tv/kraken/streams/" + channel);
                var response = request.GetResponseAsync();
                response.ContinueWith(RequestComplete);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

        private static void RequestComplete(Task<WebResponse> obj)
        {
            try
            {
                var response = obj.Result;
                var responseStream = response.GetResponseStream();
                var streamReader = new StreamReader(responseStream);
                var text = streamReader.ReadToEnd();
                var twitchObj = JsonConvert.DeserializeObject<TW_StreamInfo>(text);
                response.Close();

                var oldInfo = info;
                info = twitchObj;
                if ((!info?.Equals(oldInfo)) ?? false)
                {
                    IRC.IRCClient.PrintConsoleMessage("Update Detected");
                }
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

}
