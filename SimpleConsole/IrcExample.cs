using ChatSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleConsole
{
    internal class IrcExample
    {
        public IrcClient Client = null;

        public void Connect(StringInput pReponses = null, string pChannel = "#theonlysykan")
        {
            var username = File.ReadAllText("Username.USER");
            var password = File.ReadAllText("SecretTokenDontLOOK.TOKEN");

            Client = new IrcClient("irc.chat.twitch.tv:6667", new IrcUser(username, username, password));

            Client.ConnectionComplete += (s, e) =>
            {
                Client.JoinChannel(pChannel);
            };

            Client.ChannelMessageRecieved += (s, e) =>
            {
                //nskaarup!nskaarup@nskaarup.tmi.twitch.tv
                var anotherUsername = e.IrcMessage.Prefix.Split('!')[0];

                pReponses?.Invoke(anotherUsername, e.IrcMessage.Parameters[1]);
                //var channel = client.Channels[e.PrivateMessage.Source];

                //if (e.PrivateMessage.Message == ".list")
                //    channel.SendMessage(string.Join(", ", channel.Users.Select(u => u.Nick)));
                //else if (e.PrivateMessage.Message.StartsWith(".ban "))
                //{
                //    if (!channel.UsersByMode['@'].Contains(client.User))
                //    {
                //        channel.SendMessage("I'm not an op here!");
                //        return;
                //    }
                //    var target = e.PrivateMessage.Message.Substring(5);
                //    client.WhoIs(target, whois => channel.ChangeMode("+b *!*@" + whois.User.Hostname));
                //}
            };


            Client.ConnectAsync();

        }
    }
}
