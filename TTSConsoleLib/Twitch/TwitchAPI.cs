using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using TTSConsoleLib.Utils;

namespace TTSConsoleLib.Twitch
{
    public class TwitchAPI
    {
        public static String _channel = String.Empty;


        private static DateTime _viewCheck = DateTime.MinValue;
        private static TW_StreamInfo info;
        private static bool _checking = false;
        public static int GetNumberOfViewers()
        {
            if (info == null)
            {
                UpdateTwitchVariables();
                return 0;
            }

            if (_checking)
            {
                return info.stream?.viewers ?? 0;
            }

            if ((DateTime.Now - _viewCheck).TotalMinutes < 5)
            {
                return info.stream.viewers;
            }
            _viewCheck = DateTime.Now;
            _checking = true;

            return info.stream?.viewers ?? 0;
        }

        public static int GetNumberOfFollowers()
        {
            if (info == null)
            {
                UpdateTwitchVariables();
                return 0;
            }

            if (_checking)
            {
                return info.stream?.channel.followers ?? 0;
            }
            else
            {
                if ((DateTime.Now - _viewCheck).TotalMinutes < 2)
                {
                    return info.stream.channel.followers;
                }
                _viewCheck = DateTime.Now;
                _checking = true;
            }
            return info.stream?.channel.followers ?? 0;
        }

        public static String GetUpdateTime()
        {
            if (info == null)
            {
                UpdateTwitchVariables();
                return "hh:mm";
            }
            //return "";
            try
            {
                return (DateTime.UtcNow - info.stream.created_at).ToString(@"hh\:mm");
            }
            catch (Exception ex)
            {
                return "hh:mm";
            }
        }


        private static void UpdateTwitchVariables()
        {
            try
            {
                // Stripping # from channel name for API calls
                string channel = _channel.Replace("#", "");

                //https://api.twitch.tv/kraken/streams/theonlysykan
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

                info = twitchObj;//.stream.viewers;
                _checking = false;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                _checking = false;
            }
        }
    }



    public class TW_StreamInfo
    {
        public TW_Stream stream;
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
