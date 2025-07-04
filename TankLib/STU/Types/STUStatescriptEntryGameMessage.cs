// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x8A7E9DA8, 168)]
    public class STUStatescriptEntryGameMessage : STUStatescriptEntry
    {
        [STUField(0xBC2A8DA3, 104, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 16
        public STU_EF930FD7[] m_params;

        [STUField(0x92A85396, 120, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUConfigVar m_gameMessage;

        [STUField(0xB60ABDA4, 128, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_076E0DBA m_B60ABDA4;

        [STUField(0xFF703F20, 136, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_076E0DBA m_FF703F20;

        [STUField(0x44D0D060, 144, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_076E0DBA m_44D0D060;

        [STUField(0xF8274C10, 152, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUConfigVarFilter m_filter;

        [STUField(0xFDBDCB70, 160)] // size: 1
        public byte m_FDBDCB70;
    }
}
