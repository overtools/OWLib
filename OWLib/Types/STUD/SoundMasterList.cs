using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    public class SoundMasterList : ISTUDInstance {
        public uint Id => 0xBAD42A8D;
        public string Name => "Sound Master:List";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SoundMasterListData {
            public STUDInstanceInfo instance;
            public long infoOffset;
            public ulong zero1;
            public OWRecord unknown1;
            public OWRecord unknown2;
            public OWRecord unknown3;
            public long ownerOffset;
            public ulong zero2;
            public long soundOffset;
            public ulong zero3;
        }

        private SoundMasterListData data;
        private long[] infos;
        private ulong[] owners;
        private ulong[] sounds;

        public SoundMasterListData Data => data;
        public long[] Info => infos;
        public ulong[] Owner => owners;
        public ulong[] Sound => sounds;

        public void Read(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<SoundMasterListData>();

                if (data.infoOffset > 0) {
                    input.Position = data.infoOffset;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    infos = new long[info.count];
                    input.Position = (long)info.offset;
                    for (ulong i = 0; i < info.count; ++i) {
                        infos[i] = reader.ReadInt64();
                    }
                } else {
                    infos = new long[0];
                }

                if (data.ownerOffset > 0) {
                    input.Position = data.ownerOffset;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    owners = new ulong[info.count];
                    input.Position = (long)info.offset;
                    for (ulong i = 0; i < info.count; ++i) {
                        owners[i] = reader.ReadUInt64();
                    }
                } else {
                    owners = new ulong[0];
                }

                if (data.soundOffset > 0) {
                    input.Position = data.soundOffset;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    sounds = new ulong[info.count];
                    input.Position = (long)info.offset;
                    for (ulong i = 0; i < info.count; ++i) {
                        sounds[i] = reader.ReadUInt64();
                    }
                } else {
                    sounds = new ulong[0];
                }
            }
        }
    }
}