using System;
using System.IO;
using System.Linq;
using System.Threading;

public delegate void StringInput(string pUsername, string pInput);

namespace SimpleConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            TTSConsoleLib.Main main = new TTSConsoleLib.Main();
            main.Start(QuestionUser, Write, WriteLine);
        }

        private static String QuestionUser(String pString)
        {
            Console.WriteLine(pString);
            return Console.ReadLine();
        }

        private static void WriteLine(String pOuput)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(pOuput);
        }

        private static void Write(String pOuput, ConsoleColor pColor)
        {
            Console.ForegroundColor = pColor;
            Console.Write(pOuput);
        }
    }
}