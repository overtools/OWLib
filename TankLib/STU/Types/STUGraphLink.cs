// Generated by TankLibHelper
using TankLib.Math;

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x6E63F8E1, 40)]
    public class STUGraphLink : STUInstance
    {
        [STUField(0xE3B4FA5C, 8)] // size: 16
        public teUUID m_uniqueID;

        [STUField(0x498B0009, 24, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUGraphPlug m_outputPlug;

        [STUField(0xEA1269DF, 32, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUGraphPlug m_inputPlug;
    }
}
