using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class Lootbox : ISTUDInstance {
        public uint Id => 0x0A6886A1;
        public string Name => "Lootbox";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MasterRecord {
            public STUDInstanceInfo instance;
            public OWRecord model;
            public OWRecord alternate;
            public OWRecord norm;
            public OWRecord spec;
            public OWRecord alpha;
            public OWRecord material;
            public OWRecord alternateMaterial;
            public OWRecord title;
            public OWRecord hidden;
            public long bundles;
            public long zero;
            public OWRecord effectMaterial;
            public uint eventID;
            public uint unk;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Bundle {
            public OWRecord definition;
            public OWRecord title;
        }

        private MasterRecord master;
        private Bundle[] bundles;

        public MasterRecord Master => master;
        public Bundle[] Bundles => bundles;
        public string EventNameNormal => ItemEvents.GetInstance().GetEventNormal(master.eventID);
        public string EventName => ItemEvents.GetInstance().GetEvent(master.eventID);

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                master = reader.Read<MasterRecord>();

                if (master.bundles > 0) {
                    input.Position = master.bundles;
                    STUDArrayInfo array = reader.Read<STUDArrayInfo>();
                    bundles = new Bundle[array.count];
                    input.Position = (long)array.offset;
                    for (ulong i = 0; i < array.count; ++i) {
                        bundles[i] = reader.Read<Bundle>();
                    }
                } else {
                    bundles = new Bundle[0];
                }
            }
        }
    }
}
