// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x6CBC2E3F, 32)]
    public class STULootBoxCelebrationOverride : STUInstance
    {
        [STUField(0xED999C8B, 0)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier> m_celebrationType;

        [STUField(0xB0EDB99C, 16)] // size: 16
        public teStructuredDataAssetRef<STUUnlock> m_lootBox;
    }
}
