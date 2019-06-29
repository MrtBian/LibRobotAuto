using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LibRobotAuto.Common
{
    class MyTraceListener : TraceListener
    {
        private static string LogFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";

        public override void Write(string message)
        {
            File.AppendAllText("log_I&Q\\" + LogFileName, message);
        }

        public override void WriteLine(string message)
        {
            File.AppendAllText("log_I&Q\\" + LogFileName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss  ") + message + Environment.NewLine);
        }
    }
}
