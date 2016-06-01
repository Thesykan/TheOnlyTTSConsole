using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

public delegate void StringInput(string pUsername, string pInput);

namespace SimpleConsole
{
    internal class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            int uFlags);

        private const int HWND_TOPMOST = -1;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;

        private static void Main(string[] args)
        {
            IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;
            SetWindowPos(hWnd,
                new IntPtr(HWND_TOPMOST),
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE);

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