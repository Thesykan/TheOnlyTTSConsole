using System;
using System.IO;
using ChatSharp;
using TTSConsoleLib.Utils;
using System.Threading;
using TTSConsoleLib.Modules;
using System.Collections.Generic;

namespace TTSConsoleLib.IRC
{
    internal class IRCClient
    {

        private static Dictionary<String, IrcClient> connectedClients = new Dictionary<String, IrcClient>();
        private static String[] _addCommands = new String[] { "!addChat" };
        private static String[] _removeCommands = new String[] { "!removeChat" };
        public static bool HandleMessages(IRCMessage pMessageInfo)
        {
            CheckCommand(pMessageInfo, _addCommands, x =>
              {
                  if (!x.commandParam.StartsWith("#"))
                      x.commandParam = "#" + x.commandParam;

                  var client = Connect(PrintConsoleMessage, x.commandParam);
                  connectedClients.Add(x.commandParam, client);
              });

            CheckCommand(pMessageInfo, _removeCommands, x =>
            {
                if (!x.commandParam.StartsWith("#"))
                    x.commandParam = "#" + x.commandParam;

                if (connectedClients.ContainsKey(x.commandParam))
                {
                    connectedClients[x.commandParam]?.PartChannel(x.commandParam);
                    connectedClients[x.commandParam]?.Quit();
                    connectedClients.Remove(x.commandParam);
                }
            });
            return false;
        }

        public static IrcClient MainIRC_Client;

        public static void Start(HandleIRCMessage pReponses = null, string pChannel = "#theonlysykan")
        {
            MainIRC_Client = Connect(pReponses, pChannel);
        }

        private static IrcClient Connect(HandleIRCMessage pReponses = null, string pChannel = "#theonlysykan")
        {
            var username = File.ReadAllText("Username.USER");
            var password = File.ReadAllText("SecretTokenDontLOOK.TOKEN");

            if (!password.ToLower().Contains("oauth:"))
                password = "oauth:" + password;

            var client = new IrcClient("irc.chat.twitch.tv:6667",
                new IrcUser(username, username, password));

            client.ConnectionComplete += (s, e) => 
            {
                client.JoinChannel(pChannel);
            };

            client.ChannelMessageRecieved += (s, e) =>
            {
                //nskaarup!nskaarup@nskaarup.tmi.twitch.tv
                var anotherUsername = e.IrcMessage.Prefix.Split('!')[0];

                var messageInfo = new IRCMessage() { userName = anotherUsername,
                    message = e.IrcMessage.Parameters[1],
                    channel = e.IrcMessage.Parameters[0]
                };
                pReponses?.Invoke(messageInfo);
            };

            client.ConnectAsync();
            return client;
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
                    MainIRC_Client.SendMessage(pMessage, Twitch.TwitchAPI._channel);
                else
                    MainIRC_Client.SendMessage(pMessage, pChannel);
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
                    pMessageInfo.commandParam = split[1]?.Trim() ?? String.Empty;
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
            HandlePrintConsoleMessage(new IRCMessage() { userName = "~System~", message = pMessage });
        }

        public static void PrintConsoleMessage(String pMessage)
        {
            HandlePrintConsoleMessage(new IRCMessage() { channel = Twitch.TwitchAPI._channel, userName = "~System~", message = pMessage });
        }
        public static void PrintConsoleMessage(IRCMessage pMessage)
        {
            HandlePrintConsoleMessage(pMessage);
        }

        private static HandleIRCMessage HandlePrintConsoleMessage;
        public static void Init(HandleIRCMessage pPrintMessage)
        {
            HandlePrintConsoleMessage = pPrintMessage;
        }
    }

    public class IRCMessage
    {
        public bool isChannelMessage
        {
            get
            {
                return (channel == Twitch.TwitchAPI._channel);
            }
        }

        public String channel;
        public String userName;
        public String message;

        public String commandParam;
    }
}