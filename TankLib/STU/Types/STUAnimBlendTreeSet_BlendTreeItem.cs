// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0xDDD18945, 120)]
    public class STUAnimBlendTreeSet_BlendTreeItem : STUInstance
    {
        [STUField(0x560940DC, 0, ReaderType = typeof(InlineInstanceFieldReader))] // size: 48
        public STUAnimBlendTree_OnFinished m_onFinished;

        [STUField(0xC0214513, 48)] // size: 16
        public teStructuredDataAssetRef<STUAnimBlendTree> m_C0214513;

        [STUField(0x95877FC5, 64)] // size: 16
        public teStructuredDataAssetRef<ulong> m_95877FC5;

        [STUField(0xF6E6D4B1, 80, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_3B150012 m_F6E6D4B1;

        [STUField(0x9AD6CC25, 88, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUAnimGameData_Base m_gameData;

        [STUField(0x274F833F, 96, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_89F8DBB3 m_274F833F;

        [STUField(0x384DE14F, 104, ReaderType = typeof(InlineInstanceFieldReader))] // size: 8
        public STUAnimBlendTreeSet_RetargetParams m_retargetParams;

        [STUField(0xE54B9419, 112)] // size: 4
        public uint m_uniqueID;

        [STUField(0x675813CE, 116)] // size: 4
        public uint m_675813CE;
    }
}
