using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OverTool.ExtractLogic;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;

namespace OverTool {
    class DumpAnnouncer : IOvertool {
        public string Help => "output";
        public uint MinimumArgs => 1;
        public char Opt => 'c';
        public string FullOpt => "announcer";
        public string Title => "Extract Announcer Sounds";
        public ushort[] Track => new ushort[1] { 0x9D };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string output = flags.Positionals[2];
            
            List<ulong> masters = track[0x9D];
            Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();
            foreach (ulong masterKey in masters) {
                if (!map.ContainsKey(masterKey)) {
                    continue;
                }
                STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
                if (masterStud.Instances == null || masterStud.Instances[0] == null) {
                    continue;
                }
                AnnouncerVoiceList master = (AnnouncerVoiceList)masterStud.Instances[0];
                if (master == null) {
                    continue;
                }
                string path = string.Format("{0}{1}{2:X}{1}Announcer{1}", output, Path.DirectorySeparatorChar, GUID.Index(masterKey));

                HashSet<ulong> done = new HashSet<ulong>();

                Console.Out.WriteLine("Dumping voice lines for announcer {0:X}", GUID.Index(masterKey));

                Dictionary<ulong, List<ulong>> soundData = new Dictionary<ulong, List<ulong>>();
                for (int i = 0; i < master.Entries.Length; ++i) {
                    AnnouncerVoiceList.AnnouncerFXEntry entry = master.Entries[i];
                    OWRecord[] primary = master.Primary[i];
                    OWRecord[] secondary = master.Secondary[i];
                    Sound.FindSoundsEx(entry.master, done, soundData, map, handler, replace, masterKey);
                    foreach (OWRecord record in primary) {
                        Sound.FindSoundsEx(record, done, soundData, map, handler, replace, masterKey);
                    }
                    foreach (OWRecord record in secondary) {
                        Sound.FindSoundsEx(record, done, soundData, map, handler, replace, masterKey);
                    }
                }

                DumpVoice.Save(path, soundData, map, handler, quiet);
            }
        }
    }
}
