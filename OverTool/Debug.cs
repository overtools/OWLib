using System;
using System.Collections.Generic;
using CASCExplorer;
using STULib;

namespace OverTool {
    public class Debug : IOvertool {
        public string Help => null;
        public uint MinimumArgs => 0;
        public char Opt => '~';
        public string FullOpt => "debug";
        public string Title => "Debug";
        public ushort[] Track => null;
        public bool Display => System.Diagnostics.Debugger.IsAttached;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            STU stu = new STU(Util.OpenFile(map[0x0250000000000D5D], handler));
            System.Diagnostics.Debugger.Break();
        }
    }
}
