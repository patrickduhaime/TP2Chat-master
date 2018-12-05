using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public abstract class LogBase
    {
        protected readonly object lockObj = new object();
        public abstract void Log(string message);
    }

    public class FileLogger : LogBase
    {
        public string filePath = "./TP2Chat.log";
    public override void Log(string message)
        {
            lock (lockObj)
            {
                using (StreamWriter streamWriter = new StreamWriter(filePath,true))
                {
                    var time = DateTime.Now;
                    string formattedTime = time.ToString("yyyy/MM/dd  hh:mm:ss");

                    streamWriter.WriteLine(message + "  " + formattedTime);
                    streamWriter.Close();
                }
            }
        }
    }

    public static class LogHelper
    {
        private static LogBase logger = null;
        public static void Log(string message)
        {
            logger = new FileLogger();
            logger.Log(message);
        }
    }
}

