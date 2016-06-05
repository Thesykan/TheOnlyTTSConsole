using System;
using System.IO;
using ChatSharp;
using TTSConsoleLib.Utils;
using System.Threading;
using TTSConsoleLib.Modules;

namespace TTSConsoleLib.IRC
{
    internal class IRCClient
    {
        public static IrcClient IRC_Client;

        public static void Connect(HandleIRCMessage pReponses = null, string pChannel = "#theonlysykan")
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

                var messageInfo = new IRCMessage() { userName = anotherUsername, message = e.IrcMessage.Parameters[1] };
                pReponses?.Invoke(messageInfo);
            };

            IRC_Client.ConnectAsync();
        }

        private static DateTime _lastSend = DateTime.Now;
        private static int _messagesSent = 0;
        public static void SendIRCMessage(String pMessage, String pChannel = null)
        {
            if((DateTime.Now - _lastSend).TotalSeconds > 45)
            {
                _messagesSent = 0;
            }

            if(_messagesSent > 6)
            {
                //Do not Send...
                Logger.Log("Sending To many Messages...." + pMessage);
                return;
            }

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

            _messagesSent++;
        }


        public static void CheckCommand(IRCMessage pMessageInfo, String[] Commands, HandleIRCMessage pMethod)
        {
            var split = pMessageInfo.message.Split(Commands, StringSplitOptions.None);
            if (split.Length > 1)
            {
                try
                {
                    pMessageInfo.commandParam = split[1];
                    ThreadPool.QueueUserWorkItem(x => pMethod(pMessageInfo));
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
            }
        }

        public static void PrintSystemMessage(String pMessage)
        {
            IRC.IRCClient.SendIRCMessage(pMessage);
            HandleSystemMessage(new IRCMessage() { userName = "~System~", message = pMessage });
        }

        public static void PrintConsoleMessage(String pMessage)
        {
            HandleSystemMessage(new IRCMessage() { userName = "~System~", message = pMessage });
        }

        private static HandleIRCMessage HandleSystemMessage;
        public static void Init(HandleIRCMessage pPrintMessage)
        {
            HandleSystemMessage = pPrintMessage;
        }
    }

    public class IRCMessage
    {
        public String userName;
        public String message;

        public String commandParam;
    }
}