using System;

namespace TankLib.Helpers {
    public static class Logger {
        public static bool ShowTime = false;

#if DEBUG
        public static bool ShowDebug = true;
#else
        public static bool ShowDebug = false;
#endif

        public static bool Enabled = true;
        public static bool UseColor = true;
        
        private static void Log(ConsoleColor color, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (UseColor) {
                Console.ForegroundColor = color;
            }

            string output;
            if (ShowTime) {
                output = $"{DateTime.Now.ToLocalTime().ToLongTimeString()} [{category}] {message}";
            } else {
                output = $"[{category}] {message}";
            }
            Console.Out.WriteLine(output, arg);
            if (UseColor) {
                Console.ForegroundColor = ConsoleColor.Gray; // erm, reset
            }
        }
        
        public static void Success(string catgory, string message, params object[] arg) {
            Log(ConsoleColor.Green, catgory, message, arg);
        }
        
        public static void Info(string catgory, string message, params object[] arg) {
            Log(ConsoleColor.Gray, catgory, message, arg);
        }
        
        public static void Debug(string catgory, string message, params object[] arg) {
            if (!ShowDebug) return;
            Log(ConsoleColor.DarkGray, catgory, message, arg);
        }
        
        public static void Warn(string catgory, string message, params object[] arg) {
            Log(ConsoleColor.Yellow, catgory, message, arg);
        }
        
        public static void Error(string catgory, string message, params object[] arg) {
            Log(ConsoleColor.Red, catgory, message, arg);
        }
    }
}