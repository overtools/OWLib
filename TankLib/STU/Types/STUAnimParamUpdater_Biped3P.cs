// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x9260236F, 144)]
    public class STUAnimParamUpdater_Biped3P : STU_72C48DD7
    {
        [STUField(0xD5AF8608, 8, ReaderType = typeof(InlineInstanceFieldReader))] // size: 48
        public STUAnimParamUpdater_TurnParameters m_D5AF8608;
        
        [STUField(0x6B655AEE, 56)] // size: 16
        public teStructuredDataAssetRef<STUAnimAlias>[] m_6B655AEE;
        
        [STUField(0x49E4067A, 72, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STUAnimParamUpdater_SnapTurnScriptedAnimAlias[] m_49E4067A;
        
        [STUField(0xA6400617, 88, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_90A7ED45 m_A6400617;
        
        [STUField(0x045D3D91, 96, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STU_E0AB3A20 m_045D3D91;
        
        [STUField(0xAFA9EED7, 104, ReaderType = typeof(InlineInstanceFieldReader))] // size: 20
        public STU_1AC9AD09 m_AFA9EED7;
        
        [STUField(0x37258175, 124)] // size: 4
        public float m_37258175;
        
        [STUField(0x8F2AC198, 128)] // size: 4
        public float m_8F2AC198;
        
        [STUField(0x5F21028B, 132)] // size: 4
        public float m_5F21028B;
        
        [STUField(0xB5CF1111, 136)] // size: 1
        public byte m_B5CF1111;
    }
}
