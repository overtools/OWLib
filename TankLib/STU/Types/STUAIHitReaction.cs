// Generated by TankLibHelper
using TankLib.STU.Types.Enums;

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x543CF5E7, 104)]
    public class STUAIHitReaction : STUInstance
    {
        [STUField(0x2DDFCB42, 0)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier> m_2DDFCB42;
        
        [STUField(0xB6A970ED, 16, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STU_FC2C8334[] m_B6A970ED;
        
        [STUField(0x22A48A23, 32)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier>[] m_22A48A23;
        
        [STUField(0x1E958CD3, 48, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_EAB333BD m_1E958CD3;
        
        [STUField(0xE8BD3F70, 56, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUConfigVar m_E8BD3F70;
        
        [STUField(0xF11E914C, 64, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUConfigVar m_F11E914C;
        
        [STUField(0xA58E69E4, 72, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUConfigVar m_A58E69E4;
        
        [STUField(0x0B7962CB, 80, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUConfigVar m_0B7962CB;
        
        [STUField(0x6A79BD98, 88, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_AC5B11ED m_6A79BD98;
        
        [STUField(0xBB16810A, 96)] // size: 4
        public Enum_5014E5C9 m_priority;
    }
}
