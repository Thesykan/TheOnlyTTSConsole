using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.IO;

namespace SimpleConsole
{
    public class TwitchAPI
    {

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
                return info.stream.viewers;
            }
            else
            {
                if((DateTime.Now - _viewCheck).TotalMinutes < 5)
                {
                    return info.stream.viewers;
                }
                _viewCheck = DateTime.Now;
                _checking = true;
            }
            return info.stream.viewers;
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
                return info.stream.channel.followers;
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
            return info.stream.channel.followers;
        }

        public static String GetUpdateTime()
        {
            if (info == null)
            {
                UpdateTwitchVariables();
                return "";
            }
            //return "";
            try
            {
                return (DateTime.UtcNow - info.stream.created_at).ToString(@"hh\:mm");
            }
            catch (Exception ex)
            {
                return "";
            }
        }


        private static void UpdateTwitchVariables()
        {
            try
            {
                //https://api.twitch.tv/kraken/streams/theonlysykan
                WebRequest request = WebRequest.Create("https://api.twitch.tv/kraken/streams/theonlysykan");
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
                var StreamReader = new StreamReader(responseStream);
                var text = StreamReader.ReadToEnd();
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
