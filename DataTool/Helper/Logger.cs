using System;
using static DataTool.Program;

namespace DataTool.Helper {
    public static class Logger {
        public static void DebugLog(string syntax) {
            TankLib.Helpers.Logger.Debug(null, syntax);
        }

        public static void DebugLog(string syntax, params object[] payload) {
            TankLib.Helpers.Logger.Debug(null, syntax, payload);
        }

        public static void InfoLog(string syntax) {
            if (Flags.Quiet) {
                return;
            }
            TankLib.Helpers.Logger.Info(null, syntax);
        }

        public static void InfoLog(string syntax, params object[] payload) {
            if (Flags.Quiet) {
                return;
            }
            TankLib.Helpers.Logger.Info(null, syntax, payload);
        }
        
        public static void Log() {
            Console.Out.WriteLine();
        }
        
        public static void Log(string syntax) {
            TankLib.Helpers.Logger.Info(null, syntax);
        }
        
        public static void LogSL(string syntax) {
            TankLib.Helpers.Logger.Log(ConsoleColor.White, false, false, null, syntax);
        }
        
        public static void LoudLog(string syntax) {
            if (!Flags.Quiet)
                TankLib.Helpers.Logger.Info(null, syntax);
        }

        public static void Log(string syntax, params object[] payload) {
            TankLib.Helpers.Logger.Info(null, syntax, payload);
        }

        public static void ErrorLog() {
            Console.Error.WriteLine();
        }

        public static void ErrorLog(string syntax) {
            TankLib.Helpers.Logger.Error(null, syntax);
        }

        public static void ErrorLog(string syntax, params object[] payload) {
            TankLib.Helpers.Logger.Error(null, syntax, payload);
        }
    }
}