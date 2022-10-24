namespace DataTool.ConvertLogic.WEM {
    public class VorbisPacketHeader {
        public byte m_type;

        public static readonly char[] VORBIS_STR = { 'v', 'o', 'r', 'b', 'i', 's' };

        public VorbisPacketHeader(byte type) {
            m_type = type;
        }
    }
}