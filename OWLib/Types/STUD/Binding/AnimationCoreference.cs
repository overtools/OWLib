using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class AnimationCoreference : ISTUDInstance {
        public uint Id => 0xC811BADD;
        public string Name => "Binding:AnimationCoreference";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct AnimationCoreferenceHeader {
            public STUDInstanceInfo instance;
            public ulong arrayOffset;
            public ulong unk1;
            public ulong unk2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct AnimationCoreferenceEntry {
            public ulong unk1;
            public OWRecord animation;
            public float start;
            public float end;
            public uint unk2;
            public uint unk3;
        }

        private AnimationCoreferenceHeader header;
        private AnimationCoreferenceEntry[] entries;
        public AnimationCoreferenceHeader Header => header;
        public AnimationCoreferenceEntry[] Entries => entries;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<AnimationCoreferenceHeader>();
                if (header.arrayOffset == 0) {
                    entries = new AnimationCoreferenceEntry[0];
                    return;
                }
                input.Position = (long)header.arrayOffset;
                STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
                entries = new AnimationCoreferenceEntry[ptr.count];
                input.Position = (long)ptr.offset;
                for (ulong i = 0; i < ptr.count; ++i) {
                    entries[i] = reader.Read<AnimationCoreferenceEntry>();
                }
            }
        }
    }
}
