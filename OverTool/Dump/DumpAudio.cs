using System.Collections.Generic;
using CASCExplorer;

namespace OverTool {
    class DumpAudio : IOvertool {
        public string Help => "output";
        public uint MinimumArgs => 1;
        public char Opt => 'A';
        public string Title => "Extract 04A Audio";
        public ushort[] Track => new ushort[1] { 0x4A };
        public bool Display => true;
        
        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string output = flags.Positionals[2];

            Dictionary<ulong, List<ulong>> soundData = new Dictionary<ulong, List<ulong>>();
            HashSet<ulong> done = new HashSet<ulong>();

            foreach (ulong key in track[0x4A]) {
                ExtractLogic.Sound.FindSoundsExD(key, done, soundData, map, handler, new Dictionary<ulong, ulong>(), key);
            }

            DumpVoice.Save(output, soundData, map, handler, quiet);
        }
    }
}
