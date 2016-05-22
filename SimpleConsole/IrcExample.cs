using System.IO;
using ChatSharp;

namespace SimpleConsole
{
    internal class IRCClient
    {
        public IrcClient Client;

        public void Connect(StringInput pReponses = null, string pChannel = "#theonlysykan")
        {
            var username = File.ReadAllText("Username.USER");
            var password = File.ReadAllText("SecretTokenDontLOOK.TOKEN");

            if (!password.ToLower().Contains("oauth:"))
                password = "oauth:" + password;

            Client = new IrcClient("irc.chat.twitch.tv:6667",
                new IrcUser(username, username, password));

            Client.ConnectionComplete += (s, e) => { Client.JoinChannel(pChannel); };

            Client.ChannelMessageRecieved += (s, e) =>
            {
                //nskaarup!nskaarup@nskaarup.tmi.twitch.tv
                var anotherUsername = e.IrcMessage.Prefix.Split('!')[0];
                pReponses?.Invoke(anotherUsername, e.IrcMessage.Parameters[1]);
            };

            Client.ConnectAsync();
        }
    }
}