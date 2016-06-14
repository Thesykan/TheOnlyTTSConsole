﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSConsoleLib.Audio;
using TTSConsoleLib.IRC;
using TTSConsoleLib.Modules;
using TTSConsoleLib.Twitch;
using TTSConsoleLib.Utils;

namespace TTSConsoleLib
{
    public delegate String QuestionUser(String pMessage);
    public delegate void WriteLineToUser(String pMessage, ConsoleColor pColor);
    public delegate void WriteToUser(String pMessage, ConsoleColor pColor);

    public class Main
    {
        private WriteToUser Write;
        private WriteLineToUser WriteLine;
        public void Start(QuestionUser pMethod, WriteToUser pWriteMethod, WriteLineToUser pWriteLineMethod)
        {
            Write = pWriteMethod;
            WriteLine = pWriteLineMethod;

            string userName = string.Empty;
            if (!File.Exists("Username.USER"))
            {
                userName = pMethod("UserName?");
                File.WriteAllText("Username.USER", userName);
            }
            else
            {
                userName = File.ReadAllText("Username.USER");
            }

            if (!File.Exists("SecretTokenDontLOOK.TOKEN"))
            {
                var password = pMethod("Token?");
                File.WriteAllText("SecretTokenDontLOOK.TOKEN", password);
            }

            if (!File.Exists("Channel.JOIN"))
            {
                var channel = pMethod("Channel?(Leave blank for your own channel)");

                if (String.IsNullOrEmpty(channel))
                    channel = "#" + userName;

                if (!channel.StartsWith("#"))
                    channel = "#" + channel;

                File.WriteAllText("Channel.JOIN", channel);
                Twitch.TwitchAPI._channel = channel;
            }
            else
            {
                Twitch.TwitchAPI._channel = File.ReadAllText("Channel.JOIN");
            }

            IRCClient.Start(HandleUserCommands, Twitch.TwitchAPI._channel);

            TwitchAPI.Init();
            VoteSystem.Init();
            UserManager.Init();
            IRCClient.Init(ConsoleWrite);
            MemorySystem._instance.Init();
            Microphone.Init();

            //IRCMessage message = new IRCMessage();
            //message.userName = userName;
            //message.channel = Twitch.TwitchAPI._channel;
          
        }

        public void SendIRCMessage(String writeMessage)
        {
            IRCMessage message = new IRCMessage();
            message.userName = Twitch.TwitchAPI._channel.Replace("#","");
            message.channel = Twitch.TwitchAPI._channel;

            IRCClient.SendIRCMessage(writeMessage);
            message.message = writeMessage;
            HandleUserCommands(message);

            //System Only Commands
            IRCClient.HandleMessages(message);
        }

        private void HandleUserCommands(IRCMessage pMessageInfo)
        {
            if (pMessageInfo.userName.Contains("bot"))
                return;
            
            SyncPool.Init();

            bool SpeakText = true;

            if (VoteSystem.HandleMessages(pMessageInfo))
                SpeakText = false;

            if (UserManager.IsSpeachBannedUser(pMessageInfo.userName))
                SpeakText = false;

            if (UserManager.HandleMessages(pMessageInfo))
                SpeakText = false;

            if (SoundSystem.HandleMessages(pMessageInfo))
                SpeakText = false;

            if (pMessageInfo.message.Trim().StartsWith("!"))
                SpeakText = false;

            if (SpeakText)
                SyncPool.SpeakText(pMessageInfo);

            ConsoleWrite(pMessageInfo);
        }

        private void ConsoleWrite(IRCMessage pMessage)
        {
            string hour = FormatTime(DateTime.Now.Hour);
            string minutes = FormatTime(DateTime.Now.Minute);
            // default color
            //            Console.WriteLine($"{hour}:{minutes} - {pUsername}: {pText}");
            int viewers = TwitchAPI.GetNumberOfViewers();
            int followers = TwitchAPI.GetNumberOfFollowers();
            String uptime = TwitchAPI.GetUpdateTime();

            //Dont show info in other channel.
            if (pMessage.isChannelMessage)
            {
                // UpTime
                Write($"{uptime}", ConsoleColor.Red);
                // Spacer
                Write($" - ", ConsoleColor.Yellow);

                // Followers
                Write($"{followers}", ConsoleColor.Red);
                // Spacer
                Write($" - ", ConsoleColor.Yellow);

                // Viewers
                Write($"{viewers}", ConsoleColor.Red);
                // Spacer
                Write($" - ", ConsoleColor.Yellow);

                // color coded for readability.
                // Time
                Write($"{hour}:{minutes}", ConsoleColor.Cyan);
                // Spacer
                Write($" - ", ConsoleColor.Yellow);
            }
            else
            {
                // Show Channal
                Write(pMessage.channel, ConsoleColor.Red);
                // Spacer
                Write($" - ", ConsoleColor.Yellow);
            }

            // Username
            // TODO: Add some Color Randomization based off username
            ConsoleColor userColor = ConsoleColor.Cyan;
            if (!pMessage.isChannelMessage)
                userColor = ConsoleColor.DarkRed;
            if (TwitchAPI.IsFollower(pMessage.userName))
                userColor = ConsoleColor.Magenta;   
            Write(pMessage.userName, userColor);

            // Spacer
            Write($": ", ConsoleColor.Yellow);
            // Message
            
            WriteLine(pMessage.message, pMessage.isChannelMessage ? ConsoleColor.White : ConsoleColor.Gray);
        }

        private static string FormatTime(int i)
        {
            return (i > 9 ? "" : "0") + i;
        }

        public void CommandKey(bool pDown)
        {
            Microphone.ToggleListen(pDown);
        }
    }
}
