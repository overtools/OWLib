using TankLib.Math;

// ReSharper disable All
namespace TankLib.STU.Types {
    [STU(0xF0E34581)]
    public class STU_F0E34581 : STUInstance {
        [STUField(0xB48F1D22, "m_name")]
        public string Name;

        [STUField(0x888BCD8B)]
        public teStructuredDataAssetRef<ulong> m_888BCD8B;  // STU_7A0B33DA

        [STUField(0xEDF0511C)]
        public teResourceGUID m_EDF0511C;

        [STUField(0xFF592924)]
        public teResourceGUID m_FF592924;

        [STUField(0xAF9D3A0C)]
        public teQuat m_rotation;

        [STUField(0x7DC1550F)]
        public teVec3 m_position;
    }
    
    [STU(0xBE9DDFDF)]
    public class STUModelHardpoint : STU_F0E34581 {
    }
}