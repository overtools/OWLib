// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x7B6EA463, 40)]
    public class STUStatescriptGraphWithOverrides : STUInstance
    {
        [STUField(0xC71EA6BC, 0)] // size: 16
        public teStructuredDataAssetRef<ulong> m_graph;

        [STUField(0x1EB5A024, 16, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 16
        public STUStatescriptSchemaEntry[] m_1EB5A024;

        [STUField(0x75408640, 32)] // size: 1
        public byte m_75408640;
    }
}
