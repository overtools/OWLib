using System;
using System.Collections.Generic;
using System.Linq;
using TankLibHelper.Modes;

namespace TankLibHelper {
    internal class Program {
        public static void Main(string[] args) {
            // TankLibHelper createclasses {out} [data dir]
            // TankLibHelper updateclasses {out} [data dir]
            // TankLibHelper testclasses [args]
            // args:
            //     abc.003
            //     *.003
            //     *.003 6A0BTFA(instance hash)

            if (args.Length < 1) {
                Console.Out.WriteLine("Usage: TankLibHelper {mode} [mode args]");
                return;
            }
            string mode = args[0];

            IMode modeObject;
            Dictionary<string, Type> modes = GetModes();

            if (modes.ContainsKey(mode)) {
                modeObject = (IMode)Activator.CreateInstance(modes[mode]);
            } else {
                Console.Out.WriteLine($"Unknown mode: {mode}");
                Console.Out.WriteLine("Valid modes are:");
                foreach (string modeName in modes.Keys) {
                    Console.Out.WriteLine($"    {modeName}");
                }
                return;
            }

            ModeResult result = modeObject.Run(args);

            if (result == ModeResult.Fail) {
                Console.Out.WriteLine($"\r\n{mode} failed to execute successfully");
            } else if (result == ModeResult.Success) {
                Console.Out.WriteLine("\r\nDone");
            }
        }

        public static Dictionary<string, Type> GetModes() {
            Dictionary<string, Type> modes = new Dictionary<string, Type>();
            foreach (Type modeType in typeof(IMode).Assembly.GetTypes().Where(x => typeof(IMode).IsAssignableFrom(x))) {
                if (modeType.IsInterface) continue;
                IMode inst = (IMode)Activator.CreateInstance(modeType);
                modes[inst.Mode] = modeType;
            }

            return modes;
        }
    }
}