// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x7236F6E3, 440)]
    public class STUStatescriptGraph : STUInstance
    {
        [STUField(0xC71EA6BC, 8, ReaderType = typeof(InlineInstanceFieldReader))] // size: 72
        public STUGraph m_graph;

        [STUField(0x9B2111C5, 80, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 16
        public STUStatescriptBase[] m_nodes;

        [STUField(0xE6E7A042, 96, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 16
        public STUStatescriptState[] m_states;

        [STUField(0x16B4863C, 112, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 16
        public STUStatescriptEntry[] m_entries;

        [STUField(0xD9E7CAA2, 128)] // size: 16
        public teStructuredDataAssetRef<ulong>[] m_D9E7CAA2;

        [STUField(0x44D31832, 144)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier>[] m_44D31832;

        [STUField(0xA1183166, 160)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier>[] m_A1183166;

        [STUField(0xCC881252, 176)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier>[] m_CC881252;

        [STUField(0x0D92E2AF, 192)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier>[] m_0D92E2AF;

        [STUField(0x27651F70, 208)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier>[] m_27651F70;

        [STUField(0x2919DF46, 224)] // size: 16
        public int[] m_2919DF46;

        [STUField(0x680A2CB2, 240)] // size: 16
        public int[] m_680A2CB2;

        [STUField(0x9CEC6985, 256)] // size: 16
        public teStructuredDataAssetRef<STUIdentifier>[] m_9CEC6985;

        [STUField(0x41A83970, 272, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 16
        public STUStatescriptBase[] m_remoteSyncNodes;

        [STUField(0x434A5521, 288, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STUStatescriptSyncVar[] m_syncVars;

        [STUField(0x58CEA0CF, 304, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STUStatescriptOperationPattern[] m_58CEA0CF;

        [STUField(0x27FBFF72, 320, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STUStatescriptOperationPattern[] m_27FBFF72;

        [STUField(0xF3CFCE23, 336, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STU_D5316035[] m_F3CFCE23;

        [STUField(0x4E87690F, 352)] // size: 16
        public teStructuredDataAssetRef<STUMap> m_map;

        [STUField(0x85EE1391, 368, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUConfigVar m_85EE1391;

        [STUField(0xFF44FD8B, 376, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUStatescriptSchema m_FF44FD8B;

        [STUField(0x151AD444, 384, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUStatescriptSchema m_publicSchema;

        [STUField(0x36F7B64C, 392, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_21E27D68 m_36F7B64C;

        [STUField(0x0F3E5BB1, 400, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_00A87312 m_0F3E5BB1;

        [STUField(0xBE8E52CD, 408)] // size: 4
        public uint m_BE8E52CD;

        [STUField(0xEBCF63C6, 412)] // size: 4
        public int m_nodesBitCount;

        [STUField(0x1754C98D, 416)] // size: 4
        public int m_statesBitCount;

        [STUField(0x02FABAC2, 420)] // size: 4
        public int m_remoteSyncNodesBitCount;

        [STUField(0x475EA078, 424)] // size: 4
        public int m_syncVarsBitCount;

        [STUField(0xEC5B637A, 428)] // size: 4
        public int m_EC5B637A;

        [STUField(0x50AAA58E, 432)] // size: 4
        public int m_50AAA58E;

        [STUField(0x75408640, 436)] // size: 1
        public byte m_75408640;

        [STUField(0xCAA6714F, 437)] // size: 1
        public byte m_CAA6714F;
    }
}
