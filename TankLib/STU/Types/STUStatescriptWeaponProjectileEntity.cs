// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x6CF1089B, 40)]
    public class STUStatescriptWeaponProjectileEntity : STUInstance
    {
        [STUField(0x915AF62D, 8)] // size: 16
        public teStructuredDataAssetRef<STUEntityDefinition>[] m_915AF62D;

        [STUField(0x7DD89F4F, 24, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUConfigVar m_entityDef;

        [STUField(0xA9A62FD0, 32)] // size: 1
        public byte m_A9A62FD0 = 0x1;
    }
}
