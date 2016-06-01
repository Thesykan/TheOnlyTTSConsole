using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSConsoleLib.Audio;
using TTSConsoleLib.Utils;

namespace TTSConsoleLib.Modules
{
    class UserManager
    {
        public static bool IsSpeachBannedUser(String pUserName)
        {
            return false;
        }

        static List<UserSettings> ListOfUserSettings;
        //Handle Incoming IRC Messages
        public static bool HandleMessages(String pUserName, String pMessage)
        {
            try
            {
                LoadSettings();

                pUserName = pUserName.ToLower();
                pMessage = pMessage.ToLower();
                var result = false;

                {
                    var split = pMessage.Split(new String[] { "!voice" }, StringSplitOptions.None);
                    if (split.Length > 1)
                    {
                        if (String.IsNullOrEmpty(split[1]))
                        {
                            PrintMessage("!Avaliable Voices are: " + String.Join(",", Sync.voiceArray));
                        }
                        else
                        {
                            var user = ListOfUserSettings.FirstOrDefault(x => x.UserName == pUserName);
                            if (user != null)
                            {
                                user.Voice = split[1];
                            }
                            else
                            {
                                ListOfUserSettings.Add(new UserSettings() { UserName = pUserName, Voice = split[1] });
                            }
                            SaveSettings();
                        }
                    }
                }

                {
                    var split = pMessage.Split(new String[] { "!lexicon" }, StringSplitOptions.None);
                    if (split.Length > 1)
                    {
                        var user = ListOfUserSettings.FirstOrDefault(x => x.UserName == pUserName);
                        if (user != null)
                        {
                            user.Lexicon = split[1].Trim();
                        }
                        else
                        {
                            ListOfUserSettings.Add(new UserSettings() { UserName = pUserName, Lexicon = split[1].Trim() });
                        }
                        SaveSettings();

                        SyncPool.ReloadLexicons();
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                return false;
            }
        }

        private static HandleUserInput HandleSystemMessage;
        public static void Init(HandleUserInput pInput)
        {
            HandleSystemMessage = pInput;
        }
        private static void PrintMessage(String pMessage)
        {
            IRC.IRCClient.SendIRCMessage(pMessage);
            HandleMessages("~System~", pMessage);
        }


        public static List<UserSettings> GetUserSettings()
        {
            LoadSettings();
            return ListOfUserSettings;
        }
        public static UserSettings GetUserSettings(String pUserName)
        {
            LoadSettings();
            return ListOfUserSettings.FirstOrDefault(w=>w.UserName == pUserName);
        }


        private static void LoadSettings()
        {
            if (ListOfUserSettings == null)
            {
                if (File.Exists("UserSettings.config"))
                {
                    var json = File.ReadAllText("UserSettings.config");
                    ListOfUserSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserSettings>>(json);
                }
                else
                {
                    ListOfUserSettings = new List<UserSettings>();
                }
            }
        }

        private static void SaveSettings()
        {
            if (File.Exists("UserSettings.config"))
            {
                File.Delete("UserSettings.config");
            }

            File.WriteAllText("UserSettings.config",
                Newtonsoft.Json.JsonConvert.SerializeObject(ListOfUserSettings));
        }

    }

    public class UserSettings
    {
        public String UserName;
        public String Lexicon;
        public String Voice;
    }


}
