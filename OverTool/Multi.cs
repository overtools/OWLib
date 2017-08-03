using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CASCExplorer;
using OverTool.Flags;

namespace OverTool {
    public class Multi : IOvertool {
        public string Title => "Multimode";
        public char Opt => '_';
        public string FullOpt => "multi";
        public string Help => "mode+mode[mode args]";
        public uint MinimumArgs => 1;
        public ushort[] Track => new ushort[0];
        public bool Display => true;

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string cmdLine) {
            int argc;
            IntPtr argv = CommandLineToArgvW(cmdLine, out argc);
            if (argv == IntPtr.Zero) {
                throw new System.ComponentModel.Win32Exception();
            }
            try {
                string[] args = new string[argc];
                for (int i = 0; i < argc; i++) {
                    IntPtr ptr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(ptr);
                }
                return args;
            } finally {
                Marshal.FreeHGlobal(argv);
            }
        }


        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string[] baseArgs = Environment.GetCommandLineArgs();
            List<string> args = new List<string>();
            string[] origFlags = baseArgs.Where(x => x[0] == '-').ToArray();
            args.Add(flags.Positionals[0]);
            Dictionary<string, string> tracking = new Dictionary<string, string>();
            
            tracking[Opt.ToString()] = string.Empty;
            tracking[FullOpt] = string.Empty;

            foreach (string modeargument in flags.Positionals.Skip(2)) {
                string modearg = modeargument;
                string subargs = null;
                if (modearg.Contains('[')) {
                    modearg = modearg.Substring(0, modearg.Length - 1);
                    subargs = modearg.Substring(modearg.IndexOf('[') + 1);
                    modearg = modearg.Substring(0, modearg.IndexOf('['));
                }
                string[] modes = modearg.Split('+');

                foreach (string mode in modes) {
                    tracking[mode] = subargs;
                }
            }

            foreach (KeyValuePair<string, string> modes in tracking) {
                string mode = modes.Key;
                if ((mode.Length == 1 && mode[0] == Opt) || mode == FullOpt) {
                    continue;
                }
                if (!Program.toolsMap.ContainsKey(mode)) {
                    continue;
                }
                string subargs = modes.Value;
                string global = tracking[Opt.ToString()] + " " + tracking[FullOpt];
                List<string> tmp = new List<string>();
                tmp.Add(baseArgs[0]);
                tmp.Add(mode.ToString());
                tmp.Add(global);
                tmp.Add(subargs);
                string[] newargs = CommandLineToArgs(string.Join(" ", tmp));
                tmp.Clear();
                tmp.AddRange(origFlags);
                tmp.AddRange(args);
                tmp.AddRange(newargs.Skip(1));
                OverToolFlags newflags = FlagParser.Parse<OverToolFlags>(null, tmp.ToArray());
                IOvertool tool = Program.toolsMap[mode];
                tool.Parse(track, map, handler, quiet, newflags);
            }
        }
    }
}
