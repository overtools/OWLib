// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x9783E116, 24)]
    public class STUGameModeVarValuePair : STUInstance
    {
        [STUField(0x3E783677, 0)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier> m_3E783677;

        [STUField(0x07DD813E, 16, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_02E4C615 m_value;
    }
}
