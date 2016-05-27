using System;
using System.IO;
using ChatSharp;
using TTSConsoleLib.Utils;

namespace TTSConsoleLib.IRC
{
    public delegate void StringInput(string pUsername, string pInput);

    internal class IRCClient
    {
        public static IrcClient IRC_Client;

        public static void Connect(StringInput pReponses = null, string pChannel = "#theonlysykan")
        {
            var username = File.ReadAllText("Username.USER");
            var password = File.ReadAllText("SecretTokenDontLOOK.TOKEN");

            if (!password.ToLower().Contains("oauth:"))
                password = "oauth:" + password;

            IRC_Client = new IrcClient("irc.chat.twitch.tv:6667",
                new IrcUser(username, username, password));

            IRC_Client.ConnectionComplete += (s, e) => 
            {
                IRC_Client.JoinChannel(pChannel);
            };

            IRC_Client.ChannelMessageRecieved += (s, e) =>
            {
                //nskaarup!nskaarup@nskaarup.tmi.twitch.tv
                var anotherUsername = e.IrcMessage.Prefix.Split('!')[0];
                pReponses?.Invoke(anotherUsername, e.IrcMessage.Parameters[1]);
            };

            IRC_Client.ConnectAsync();
        }


        public static void SendIRCMessage(String pMessage, String pChannel = null)
        {
            try
            {
                if (pChannel == null)
                    IRC_Client.SendMessage(pMessage, Twitch.TwitchAPI._channel);
                else
                    IRC_Client.SendMessage(pMessage, pChannel);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        }

    }
}