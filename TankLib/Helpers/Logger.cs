using System;
using static TankLib.Helpers.ConsoleSwatch;

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

        public static void Log4Bit(ConsoleColor color, bool newLine, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (UseColor) {
                Console.ForegroundColor = color;
            }

            string output = message;
            if (!string.IsNullOrWhiteSpace(category)) {
                output = $"[{category}] {output}";
            }
            if (ShowTime) {
                output = $"{DateTime.Now.ToLocalTime().ToLongTimeString()} {output}";
            }
            Console.Out.Write(output, arg);
            if (UseColor) {
                Console.ForegroundColor = ConsoleColor.Gray; // erm, reset
            }
            if(newLine) {
                Console.Out.WriteLine();
            }
        }

        private static void Log24Bit(ConsoleColor color, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(color, true, category, message, arg);
                return;
            }
            Log24Bit(color.AsDOSColor().AsXTermColor().ToForeground(), null, true, category, message, arg);
        }

        private static void Log24Bit(DOSColor color, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(color.AsConsoleColor(), true, category, message, arg);
                return;
            }
            Log24Bit(color.AsXTermColor().ToForeground(), null, true, category, message, arg);
        }

        private static void Log24Bit(XTermColor color, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(ConsoleColor.Gray, true, category, message, arg);
                return;
            }
            Log24Bit(color.ToForeground(), null, true, category, message, arg);
        }

        public static void Log24Bit(string foreground, string background, bool newLine, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(ConsoleColor.Gray, true, category, message, arg);
                return;
            }
            if (UseColor && !string.IsNullOrWhiteSpace(foreground)) {
                Console.Out.Write(foreground);
            }
            if (UseColor && !string.IsNullOrWhiteSpace(background)) {
                Console.Out.Write(background);
            }
            string output = message;
            if (!string.IsNullOrWhiteSpace(category)) {
                output = $"[{category}] {output}";
            }
            if (ShowTime) {
                output = $"{DateTime.Now.ToLocalTime().ToLongTimeString()} {output}";
            }
            Console.Out.Write(output, arg);
            if (UseColor && (!string.IsNullOrWhiteSpace(foreground) || !string.IsNullOrWhiteSpace(background))) {
                Console.Out.Write(ColorReset);
            }
            if (newLine) {
                Console.Out.WriteLine();
            }
        }

        public static void Log(ConsoleColor color, string category, string message, params object[] arg) {
            Log24Bit(color, category, message, arg);
        }
        
        public static void Success(string catgory, string message, params object[] arg) {
            Log(ConsoleColor.Green, catgory, message, arg);
        }
        
        public static void Info(string catgory, string message, params object[] arg) {
            Log(ConsoleColor.White, catgory, message, arg);
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