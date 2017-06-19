using System.Collections.Generic;
using System.IO;
using CASCExplorer;

namespace OverTool {
    class DumpAudio : IOvertool {
        public string Help => "output";
        public uint MinimumArgs => 1;
        public char Opt => 'A';
        public string Title => "Extract Audio referened by 01B";
        public ushort[] Track => new ushort[1] { 0x1B };
        public bool Display => true;
        
        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string output = string.Format("{0}{1}Audio{1}", flags.Positionals[2], Path.DirectorySeparatorChar);

            Dictionary<ulong, List<ulong>> soundData = new Dictionary<ulong, List<ulong>>();
            HashSet<ulong> done = new HashSet<ulong>();

            foreach (ulong key in track[0x1B]) {
                ExtractLogic.Sound.FindSoundsEx(key, done, soundData, map, handler, new Dictionary<ulong, ulong>(), key);
            }

            DumpVoice.Save(output, soundData, map, handler, quiet);
        }
    }
}
