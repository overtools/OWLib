using System;
using System.Diagnostics;
using static DataTool.Program;

namespace DataTool.Helper {
    public static class Logger {
        public static void DebugLog() {
            if (!Debugger.IsAttached) {
                return;
            }
            Console.Error.WriteLine();
        }

        public static void DebugLog(string syntax, params object[] payload) {
            if (!Debugger.IsAttached) {
                return;
            }
            Console.Error.WriteLine(syntax, payload);
        }

        public static void InfoLog() {
            if (Flags.Quiet) {
                return;
            }
            Console.Out.WriteLine();
        }

        public static void InfoLog(string syntax, params object[] payload) {
            if (Flags.Quiet) {
                return;
            }
            Console.Out.WriteLine(syntax, payload);
        }
        
        public static void Log() {
            Console.Out.WriteLine();
        }
        
        public static void Log(string syntax) {
            Console.Out.WriteLine(syntax);
        }
        
        public static void LoudLog(string syntax) {
            if (!Flags.Quiet)
                Log(syntax);
        }

        public static void Log(string syntax, params object[] payload) {
            Console.Out.WriteLine(syntax, payload);
        }
        
        public static void ErrorLog() {
            Console.Error.WriteLine();
        }

        public static void ErrorLog(string syntax, params object[] payload) {
            Console.Error.WriteLine(syntax, payload);
        }
    }
}