using System.Diagnostics;
using System.IO;
using static DataTool.ConvertLogic.SoundUtils;

namespace DataTool.ConvertLogic.WEM {
    public class Packet {
        private readonly long m_offset;
        private readonly ushort m_size;
        private readonly uint m_absoluteGranule;
        private readonly bool m_noGranule;

        public Packet(BinaryReader reader, long offset, bool littleEndian, bool noGranule = false) {
            m_noGranule = noGranule;
            m_offset = offset;
            reader.BaseStream.Seek(m_offset, SeekOrigin.Begin);
            if (littleEndian) {
                m_size = reader.ReadUInt16();
                // _size = read_16_le(i);
                if (!m_noGranule) {
                    m_absoluteGranule = reader.ReadUInt32();
                }
            } else {
                Debugger.Break();
                m_size = SwapBytes(reader.ReadUInt16());
                if (!m_noGranule) {
                    m_absoluteGranule = SwapBytes(reader.ReadUInt32());
                }
            }
        }

        public long HeaderSize() {
            return m_noGranule ? 2 : 6;
        }

        public long Offset() {
            return m_offset + HeaderSize();
        }

        public ushort Size() {
            return m_size;
        }

        public uint Granule() {
            return m_absoluteGranule;
        }

        public long NextOffset() {
            return m_offset + HeaderSize() + m_size;
        }
    }
}