using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using TTSConsoleLib.Utils;

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

        //private static void Main(string[] args)
        //{
        //    //IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;
        //    //SetWindowPos(hWnd,
        //    //    new IntPtr(HWND_TOPMOST),
        //    //    0, 0, 0, 0,
        //    //    SWP_NOMOVE | SWP_NOSIZE);

        //    //TTSConsoleLib.Main main = new TTSConsoleLib.Main();
        //    //main.Start(QuestionUser, Write, WriteLine);
        //}

        private static String QuestionUser(String pString)
        {
            Console.WriteLine(pString);
            return Console.ReadLine();
        }

        private static void WriteLine(String pOuput, ConsoleColor pColor)
        {
            if (pOuput == null)
                return;

            Console.ForegroundColor = pColor;

            pOuput = pOuput.Replace(":)", "\x001").Replace(":]", "\x002");
            Console.WriteLine(pOuput);
        }

        private static void Write(String pOuput, ConsoleColor pColor)
        {
            if (pOuput == null)
                return;

            Console.ForegroundColor = pColor;

            pOuput = pOuput.Replace(":)", "\x001").Replace(":]", "\x002");
            Console.Write(pOuput);
        }



        #region ALL THIS JUST TO GET GLOBAL KEY CAPTURE

        ///////////////////////////////////////////////////////////
        //A bunch of DLL Imports to set a low level keyboard hook
        ///////////////////////////////////////////////////////////
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        ////////////////////////////////////////////////////////////////
        //Some constants to make handling our hook code easier to read
        ////////////////////////////////////////////////////////////////
        private const int WH_KEYBOARD_LL = 13;                    //Type of Hook - Low Level Keyboard
        private const int WM_KEYDOWN = 0x0100;                    //Value passed on KeyDown
        private const int WM_KEYUP = 0x0101;                      //Value passed on KeyUp
        private static LowLevelKeyboardProc _proc = HookCallback; //The function called when a key is pressed
        private static IntPtr _hookID = IntPtr.Zero;
        private static bool CONTROL_DOWN = false;                 //Bool to use as a flag for control key

        static TTSConsoleLib.Main main;
        public static void Main(string[] args)
        {
            IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;
            SetWindowPos(hWnd,
                new IntPtr(HWND_TOPMOST),
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE);

            main = new TTSConsoleLib.Main();
            main.Start(QuestionUser, Write, WriteLine);

            _hookID = SetHook(_proc);  //Set our hook
            Application.Run();         //Start a standard application method loop

            while (true)
            {
                try
                {
                    var writeMessage = Console.ReadLine();
                    if (writeMessage?.Trim() == "q")
                        break;
                    if (writeMessage?.Trim() != string.Empty)
                    {
                        main.SendIRCMessage(writeMessage);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
            }
            
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) //A Key was pressed down
            {
                int vkCode = Marshal.ReadInt32(lParam);           //Get the keycode
                if(vkCode == 192)
                {
                    main.CommandKey();
                }

                //string theKey = ((Keys)vkCode).ToString();        //Name of the key
                //Console.WriteLine(theKey);                            //Display the name of the key

                //if (theKey.Contains("ControlKey"))                //If they pressed control
                //{
                //    CONTROL_DOWN = true;                          //Flag control as down
                //}
                //else if (CONTROL_DOWN && theKey == "B")           //If they held CTRL and pressed B
                //{
                //    Console.WriteLine("\n***HOTKEY PRESSED***");  //Our hotkey was pressed
                //}
                //else if (theKey == "Escape")                      //If they press escape
                //{
                //    UnhookWindowsHookEx(_hookID);                 //Release our hook
                //    Environment.Exit(0);                          //Exit our program
                //}
            }
            //else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP) //KeyUP
            //{
            //    int vkCode = Marshal.ReadInt32(lParam);        //Get Keycode
            //    string theKey = ((Keys)vkCode).ToString();     //Get Key name
            //    if (theKey.Contains("ControlKey"))             //If they let go of control
            //    {
            //        CONTROL_DOWN = false;                      //Unflag control
            //    }
            //}
            return CallNextHookEx(_hookID, nCode, wParam, lParam); //Call the next hook
        }

        #endregion
    }
}