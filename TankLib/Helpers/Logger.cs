using System;
using System.IO;
using System.Text;
using static TankLib.Helpers.ConsoleSwatch;
// ReSharper disable UnusedMember.Local

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

        public static void Log4Bit(ConsoleColor color, bool newLine, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (UseColor) {
                Console.ForegroundColor = color;
            }
            

            string output = message;

            if (arg.Length > 0) {
                output = string.Format(message, arg);
            }

            if (!string.IsNullOrWhiteSpace(category)) {
                output = $"[{category}] {output}";
            }

            if (ShowTime) {
                output = $"{DateTime.Now.ToLocalTime().ToLongTimeString()} {output}";
            }

            writer.Write(output);
            
            if (UseColor) {
                Console.ForegroundColor = ConsoleColor.Gray; // erm, reset
            }

            if (newLine) {
                writer.WriteLine();
            }
        }

        private static void Log24Bit(ConsoleColor color, string category, string message, params object[] arg) {
            Log24Bit(color, true, Console.Out, category, message, arg);
        }

        public static void Log24Bit(ConsoleColor color, bool newline, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(color, newline, writer, category, message, arg);
                return;
            }

            Log24Bit(color.AsDOSColor().AsXTermColor().ToForeground(), null, newline, writer, category, message, arg);
        }

        private static void Log24Bit(DOSColor color, string category, string message, params object[] arg) {
            Log24Bit(color, true, Console.Out, category, message, arg);
        }

        public static void Log24Bit(DOSColor color, bool newline, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(color.AsConsoleColor(), newline, writer, category, message, arg);
                return;
            }

            Log24Bit(color.AsXTermColor().ToForeground(), null, newline, writer, category, message, arg);
        }

        private static void Log24Bit(XTermColor color, string category, string message, params object[] arg) {
            Log24Bit(color, true, Console.Out, category, message, arg);
        }

        public static void Log24Bit(XTermColor color, bool newline, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(ConsoleColor.Gray, newline, writer, category, message, arg);
                return;
            }

            Log24Bit(color.ToForeground(), null, newline, writer, category, message, arg);
        }

        private static void Log24Bit(string foreground, string background, bool newLine, string category, string message, params object[] arg) {
            Log24Bit(foreground, background, newLine, Console.Out, category, message, arg);
        }

        public static void Log24Bit(string foreground, string background, bool newLine, TextWriter writer, string category, string message, params object[] arg) {
            if (!Enabled) return;
            if (!EnableVT()) {
                Log4Bit(ConsoleColor.Gray, newLine, writer, category, message, arg);
                return;
            }

            if (UseColor && !string.IsNullOrWhiteSpace(foreground)) {
                writer.Write(foreground);
            }

            if (UseColor && !string.IsNullOrWhiteSpace(background)) {
                writer.Write(background);
            }

            string output = message;

            if (arg.Length > 0) {
                output = string.Format(message, arg);
            }

            if (!string.IsNullOrWhiteSpace(category)) {
                output = $"[{category}] {output}";
            }

            if (ShowTime) {
                output = $"{DateTime.Now.ToLocalTime().ToLongTimeString()} {output}";
            }

            writer.Write(output);

            if (UseColor && (!string.IsNullOrWhiteSpace(foreground) || !string.IsNullOrWhiteSpace(background))) {
                writer.Write(ColorReset);
            }

            if (newLine) {
                writer.WriteLine();
            }
        }

        public static void Log(ConsoleColor color, bool newline, bool stderr, string category, string message, params object[] arg) {
            Log24Bit(color, newline, stderr ? Console.Error : Console.Out, category, message, arg);
        }

        public static void Success(string category, string message, params object[] arg) {
            Log(ConsoleColor.Green, true, false, category, message, arg);
        }

        public static void Info(string category, string message, params object[] arg) {
            Log(ConsoleColor.White, true, false, category, message, arg);
        }

        public static void Debug(string category, string message, params object[] arg) {
            if (!ShowDebug) return;
            Log(ConsoleColor.DarkGray, true, false, category, message, arg);
        }

        public static void Warn(string category, string message, params object[] arg) {
            Log(ConsoleColor.DarkYellow, true, false, category, message, arg);
        }

        public static void Error(string category, string message, params object[] arg) {
            Log(ConsoleColor.Red, true, true, category, message, arg);
        }
        
        public static string ReadLine(TextWriter writer, bool @private) {
            StringBuilder builder = new StringBuilder();
            ConsoleKeyInfo ch;
            while ((ch = Console.ReadKey(true)).Key != ConsoleKey.Enter) {
                if (ch.Key == ConsoleKey.Backspace) {
                    if (builder.Length > 0) {
                        if (!@private) {
                            writer.Write(ch.KeyChar);
                            writer.Write(" ");
                            writer.Write(ch.KeyChar);
                        }
                        
                        builder.Remove(builder.Length - 1, 1);
                    } else {
                        Console.Beep();
                    }
                } else {
                    builder.Append(ch.KeyChar);
                    
                    if (!@private) writer.Write(ch.KeyChar);
                }
            }
            writer.WriteLine();
            return builder.ToString();
        }
    }
}
