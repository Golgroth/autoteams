using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace autoteams
{
    public static class Logger
    {
        private static bool _update;
        private const string LOG_FILE = "logfile.log";

        private static readonly SemaphoreSlim _fileSemaphore = new(1, 1);

        public static void Log(LogLevel level, string msg)
        {
            if (level == LogLevel.DEBUG && !ConfigurationManager.CONFIG.OutputDebug)
                return;

            var date = DateTime.Now;

            //Reset the current line if in update mode
            if (_update)
                Console.Write("\r");

            Console.Write($"{GetColor(level)}[{date:HH:mm:ss}, {level}]\x1b[0m => {msg}{(_update ? "" : Environment.NewLine)}");

            WriteToFile($"[{date:dd.MM.yy HH:mm:ss}, {level}] => {msg}").ConfigureAwait(false);
        }

        //Asynchronously write to a file
        public static async Task WriteToFile(string msg)
        {
            await _fileSemaphore.WaitAsync();
            await File.AppendAllTextAsync(LOG_FILE, msg + "\n", Encoding.UTF8);
            _fileSemaphore.Release();
        }

        public static void Debug(string msg) => Log(LogLevel.DEBUG, msg);
        public static void Warn(string msg) => Log(LogLevel.WARN, msg);
        public static void Error(string msg) => Log(LogLevel.ERROR, msg);
        public static void Info(string msg) => Log(LogLevel.INFO, msg);

        public static void EnterUpdateMode() => _update = true;
        public static void ExitUpdateMode()
        {
            if (!_update) return;

            _update = false;
            Console.Write(Environment.NewLine);
        }

        public static void SetUpdateMode(bool update)
        {
            if (update)
                EnterUpdateMode();
            else
                ExitUpdateMode();
        }

        //Associate an ANSI color code with each log level
        public static string GetColor(LogLevel level) => level switch
        {
            LogLevel.DEBUG => "\x1b[1;32m",
            LogLevel.INFO => "\x1b[1;37m",
            LogLevel.WARN => "\x1b[1;33m",
            LogLevel.ERROR => "\x1b[1;31m",
            _ => string.Empty
        };

        public enum LogLevel : byte
        {
            DEBUG = 0b0001, //0x1
            INFO = 0b0010, //0x2
            WARN = 0b0110, //0x6
            ERROR = 0b1110 //0xe
        }
    }
}