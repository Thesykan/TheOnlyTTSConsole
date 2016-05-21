using ChatSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleConsole
{
    class IrcExample
    {
        public IrcClient client = null;

        public void Connect(StringInput p_reponses = null, String p_channel = "#theonlysykan")
        {
            String username = File.ReadAllText("Username.USER");
            String password = File.ReadAllText("SecretTokenDontLOOK.TOKEN");

            client = new IrcClient("irc.chat.twitch.tv:6667", new IrcUser(username, username, password));

            client.ConnectionComplete += (s, e) =>
            {
                client.JoinChannel(p_channel);
            };

            client.ChannelMessageRecieved += (s, e) =>
            {
                //nskaarup!nskaarup@nskaarup.tmi.twitch.tv
                var anotherUsername = e.IrcMessage.Prefix.Split('!')[0];

                p_reponses(anotherUsername, e.IrcMessage.Parameters[1]);
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


            client.ConnectAsync();

        }
    }
}
