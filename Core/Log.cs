using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core
{
    public class Log
    {
        public static string LogFilename = "work.log";
        public enum Level
        {
            Info,
            Success,
            Error
        }

        public static void Write(Level level, string text)
        {
            File.AppendAllLines(LogFilename, new[] { $"[{DateTime.Now:hh:mm:ss dd.MM.yyyy}] [{Enum.GetName(typeof(Level), level)}] - {text}" });
        }
    }
}