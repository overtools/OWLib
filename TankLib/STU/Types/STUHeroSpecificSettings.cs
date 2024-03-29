// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x24DF4688, 32)]
    public class STUHeroSpecificSettings : STUInstance
    {
        [STUField(0x37AB13D3, 0)] // size: 16
        public teStructuredDataAssetRef<STUHero> m_hero;

        [STUField(0x27315EFA, 16, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 16
        public STUHeroSettingBase[] m_settings;
    }
}
