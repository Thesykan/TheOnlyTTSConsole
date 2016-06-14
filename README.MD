TheOnlyTTSConsole Is a C# Console Application For Connecting to Twich.tv IRC chat. 

## Features

* Chat Print Out
    * Up Time
    * Followers
    * Viewers
* Text To Speech Chat
* Repond To Chat Through Console
* Internal Polling / Voting System.
* Console Stays On Top
* Twitch Updates Shown on Console / Twitch Variable Updates (Ex. Number Of Viewer etc...
* Utilizes SQLite Database to Records User Points Per Day.
* Connect Simutaniously to Other Twitch Chats. [!AddChat / !RemoveChat]
* Followers Show up as Magenta
* Message Alerts on first message when they were last in chat.
* User Point System.
* Detect Voice Chat on Mic and pause TTS
* Voice Commands!!!! Credit:{Twitch:junicus}
* Ctrl key now starts and stops Speech Recognition.

## On First Start Up

* Enter UserName
* Enter Twitch Token
* Enter Channel Name Or Blank


## Commands

* !Voice 
	Get list of Voices.
* !Voice {Voice Name}
	Select Voice.
* !NewPoll {Name},{Option 1},{Option 2},etc.
	Start New Poll
* !EndPoll {Name}
	End Poll
* !{PollName} {Option Name}
    Vote On Poll
* !Lexicon {Phoneme}  --BETA-- -Only for 32Bit-
    Select Lexicon




Using NAudio GitHub project in this project. [https://github.com/naudio/NAudio]