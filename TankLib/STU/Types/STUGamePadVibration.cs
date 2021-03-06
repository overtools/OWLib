// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x7068CCE6, 32)]
    public class STUGamePadVibration : STUInstance
    {
        [STUField(0x1FB75847, 8, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUAnimCurve m_1FB75847;
        
        [STUField(0x54053299, 16, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUAnimCurve m_54053299;
        
        [STUField(0x890536B8, 24)] // size: 4
        public float m_duration;
    }
}
