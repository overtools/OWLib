// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x19BF28AB, 80)]
    public class STUAnimNode_BlendPartition_ExtraPartitionBoneItem : STUInstance
    {
        [STUField(0x3A4E6E8A, 0, ReaderType = typeof(InlineInstanceFieldReader))] // size: 32
        public STU_25B808BD m_3A4E6E8A;

        [STUField(0x9CDDC24D, 32, ReaderType = typeof(InlineInstanceFieldReader))] // size: 24
        public STU_ABD8FE73 m_weight;

        [STUField(0xF97609C8, 56)] // size: 16
        public teStructuredDataAssetRef<STUBoneLabel> m_bone;

        [STUField(0x9C09FA51, 72)] // size: 1
        public byte m_9C09FA51;
    }
}
