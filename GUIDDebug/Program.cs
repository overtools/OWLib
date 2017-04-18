using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib;

namespace GUIDDebug {
    class Program {
        static void Main(string[] args) {
            if (args.Length < 1) {
                Console.Out.WriteLine("Usage: GUIDDebug keys...");
                return;
            }

            HashSet<ulong> keys = new HashSet<ulong>();
            for (long i = 0; i < args.LongLength; ++i) {
                ulong key = 0;
                if (args[i][0] == 'x') {
                    ulong.TryParse(args[i].Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out key);
                } else if (args[i].Substring(0, 2) == "0x") {
                    ulong.TryParse(args[i].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out key);
                } else {
                    ulong.TryParse(args[i], NumberStyles.Number, CultureInfo.InvariantCulture, out key);
                }
                if (key != 0) {
                    keys.Add(key);
                }
            }

            foreach (ulong key in keys) {
                Console.Out.WriteLine($"Info for key: 0x{key:X16}");
                GUID.DumpAttributes(Console.Out, key);
            }
        }
    }
}
