using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimpleConsole
{
    class Logger
    {
        static StreamWriter _logStream;
        static Logger()
        {
            var date = DateTime.Now.ToShortDateString();
            var file = File.Open(date+".log", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            file.Seek(0, SeekOrigin.End);

            _logStream = new StreamWriter(file);
        }

        public static void Log(String p_string)
        {
            lock (_logStream)
            {
                _logStream.WriteLine(p_string);
            }
        }

    }
}
