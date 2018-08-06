using System.Runtime.InteropServices;

namespace TankLib.Math {  // probably isn't the right place for this
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teEntityID {
        public uint Value;
        
        public teEntityID(uint val) {
            Value = val;
        }
    }
}