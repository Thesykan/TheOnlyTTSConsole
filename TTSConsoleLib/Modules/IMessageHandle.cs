using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSConsoleLib.IRC;

namespace TTSConsoleLib.Modules
{
    public delegate void HandleIRCMessage(IRCMessage pMessage);

    interface IMessageHandle
    {
        bool HandleMessages(IRCMessage pMessageInfo);
    }
}
