// Generated by TankLibHelper
using TankLib.STU.Types.Enums;

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0xC2DC60F6, 128)]
    public class STUStatescriptBase : STUGraphNode
    {
        [STUField(0xBF5B22B7, 72, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STUStatescriptRemoteSyncVar[] m_BF5B22B7;

        [STUField(0x8BF03679, 88, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STUStatescriptRemoteSyncVar[] m_8BF03679;

        [STUField(0xA2287776, 104)] // size: 4
        public Enum_46B5ADCA m_A2287776 = Enum_46B5ADCA.x1F0B0C4D;

        [STUField(0x871A8203, 108)] // size: 4
        public int m_871A8203;

        [STUField(0x602E1F8F, 112)] // size: 4
        public int m_602E1F8F;

        [STUField(0xAED90719, 116)] // size: 4
        public Enum_CEBBB217 m_AED90719;

        [STUField(0x2BBEEAB8, 120)] // size: 1
        public byte m_2BBEEAB8;

        [STUField(0xADEB6E05, 121)] // size: 1
        public byte m_ADEB6E05;

        [STUField(0xE1B0C07F, 122)] // size: 1
        public byte m_clientOnly;

        [STUField(0x9A861B79, 123)] // size: 1
        public byte m_serverOnly;
    }
}
