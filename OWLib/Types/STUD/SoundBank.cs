using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class SoundBank : ISTUDInstance {
        public uint Id => 0xB1C3AA7F;
        public string Name => "SoundBank:Info";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SoundBankData {
            public STUDInstanceInfo instance;
            public ulong unk1;
            public ulong unk2;
            public OWRecord soundbank;
            public long sfxOffset;
            public long zero;
            public long musicOffset;
        }

        private SoundBankData data;
        private OWRecord[] sfx;
        private OWRecord[] music;

        public SoundBankData Data => data;
        public OWRecord[] SFX => sfx;
        public OWRecord[] Music => music;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<SoundBankData>();

                if (data.sfxOffset > 0) {
                    input.Position = data.sfxOffset;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    sfx = new OWRecord[info.count];
                    input.Position = (long)info.offset;
                    for (ulong i = 0; i < info.count; ++i) {
                        sfx[i] = reader.Read<OWRecord>();
                    }
                } else {
                    sfx = new OWRecord[0];
                }

                if (data.musicOffset > 0) {
                    input.Position = data.musicOffset;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    music = new OWRecord[info.count];
                    input.Position = (long)info.offset;
                    for (ulong i = 0; i < info.count; ++i) {
                        music[i] = reader.Read<OWRecord>();
                    }
                } else {
                    music = new OWRecord[0];
                }
            }
        }
    }
}