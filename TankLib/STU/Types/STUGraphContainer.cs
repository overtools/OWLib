// Generated by TankLibHelper
using TankLib.Math;

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0xB67D66ED, 80)]
    public class STUGraphContainer : STUGraphItem
    {
        [STUField(0x52730CFE, 16, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 16
        public STUGraphItem[] m_52730CFE;

        [STUField(0xC65AA24E, 32)] // size: 16
        public teString m_C65AA24E;

        [STUField(0xE3C80AC9, 48)] // size: 16
        public teColorRGBA m_E3C80AC9 = new teColorRGBA(0f, 0f, 0f, 0f);

        [STUField(0xB8938E78, 64)] // size: 16
        public teColorRGBA m_backgroundColor;
    }
}
