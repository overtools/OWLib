using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TankLib {
    public class teSubtitleThing {
        public const int COUNT = 4;

        public List<string> m_strings;

        public teSubtitleThing(Stream stream) {
            using (var reader = new BinaryReader(stream)) {
                Read(reader);
            }
        }

        public teSubtitleThing(BinaryReader reader) {
            Read(reader);
        }

        private void Read(BinaryReader reader) {
            var offsets = reader.ReadArray<ushort>(COUNT);
            
            m_strings = new List<string>();

            for (int i = 0; i < COUNT; i++) {
                var offset = offsets[i];
                if (offset == 0) continue;

                reader.BaseStream.Position = offset;

                while (reader.ReadByte() != 0)
                {
                }
                var end = (int) reader.BaseStream.Position - 1;
                
                reader.BaseStream.Position = offset;
                var bytes = reader.ReadBytes(end - offset);
                m_strings.Add(Encoding.UTF8.GetString(bytes));
            }
        }
    }
}
